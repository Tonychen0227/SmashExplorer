import datetime
import os

from cosmos import CosmosDB
from smashggapi import API
from twitter_engine import TwitterClient

class OperationsManager:
    def __init__(self, logger):
        endpoint = os.environ["COSMOS_ENDPOINT"]
        key = os.environ["COSMOS_KEY"]
        cosmos = CosmosDB(endpoint, key, logger)
        api = API(os.environ["SMASHGG_KEYS"], logger)

        self.cosmos = cosmos
        self.api = api
        self.logger = logger
        self.twitter = TwitterClient(logger)

    def get_all_events_from_db(self):
        return self.cosmos.get_all_events()

    def get_all_sets_from_db(self, event_id):
        return self.cosmos.get_event_sets(event_id)

    def get_event_from_db(self, event_id):
        return self.cosmos.get_event(event_id)

    def get_active_event_ids(self):
        return self.cosmos.get_active_event_ids()

    def get_open_event_ids(self):
        return self.cosmos.get_outstanding_event_ids()

    def get_new_events(self, days_back=2, days_forward=30):
        date_now = datetime.datetime.now(datetime.timezone.utc)

        start_time = date_now - datetime.timedelta(days=days_back)
        end_time = date_now + datetime.timedelta(days=days_forward)

        upcoming_event_lists = self.api.get_upcoming_ult_events(start_time, end_time)

        upcoming_event_ids = []
        for event_list in upcoming_event_lists:
            if event_list is None:
                continue
            for event in event_list:
                if event["numEntrants"] is not None and event["numEntrants"] >= 10:
                    upcoming_event_ids.append(str(event["id"]))

        if "NOMINATED_TOURNAMENTS" in os.environ:
            for slug in os.environ["NOMINATED_TOURNAMENTS"].split(" "):
                upcoming_event_ids.extend([str(event["id"]) for event in self.api.get_ult_tournament_events(slug)["tournament"]["events"]])

        self.logger.log(f"Returned {len(upcoming_event_ids)} upcoming events")

        return upcoming_event_ids

    def update_event_sets(self, event_id, created_event, bypass_last_updated=False, disable_backfill=False):
        start_time = int((datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(minutes=5)).timestamp())

        event = created_event
        if start_time < event["setsLastUpdated"] and not bypass_last_updated:
            return

        if bypass_last_updated:
            event["setsLastUpdated"] = 1

        sets = self.api.get_event_sets_updated_after_timestamp(event_id, event["setsLastUpdated"])

        self.cosmos.update_event_sets_last_updated(event_id, start_time)

        total_sets = len(sets)
        self.logger.log(f"Updating {total_sets} sets {[x['id'] for x in sets]} for event {event_id} with timestamp {event['setsLastUpdated']}")

        upset_thread_root = event["upsetThreadRoot"]

        tweeted_out = False
        sets_tweeted_out = event["setsAlreadyTweeted"]
        ordinal = lambda n: "%d%s" % (n, "tsnrhtdd"[(n // 10 % 10 != 1) * (n % 10 < 4) * n % 10::4])

        for set in sets:
            if disable_backfill or not event["doUpsetThread"]:
                break

            if set["id"] in sets_tweeted_out:
                continue

            if not set["isUpsetOrNotable"] or set["detailedScore"] is None or set["upsetFactor"] <= 2:
                continue

            detailed_score = set["detailedScore"]

            loser_id = [x["id"] for x in set["entrants"] if x["id"] != set["winnerId"]][0]

            winner_seed = [x["initialSeedNum"] for x in set["entrants"] if x["id"] == set["winnerId"]][0]
            loser_seed = [x["initialSeedNum"] for x in set["entrants"] if x["id"] != set["winnerId"]][0]

            if winner_seed < loser_seed:
                continue

            winner_entrant = self.cosmos.get_entrant(event_id, set["winnerId"])
            loser_entrant = self.cosmos.get_entrant(event_id, loser_id)

            if upset_thread_root is None:
                upset_thread_root = self.twitter.make_root_tweet(event)

            twitter_links = []

            for info in winner_entrant["additionalInfo"]:
                if "urls" not in info:
                    continue
                for url in info["urls"]:
                    if "twitter" in url["url"]:
                        twitter_handle = url["url"].split("/")[-1]
                        twitter_links.append(twitter_handle)

            winner_display = winner_entrant["name"]

            if len(twitter_links) > 0:
                winner_display = ' '.join([f"@{x}" for x in twitter_links])

            heading = f"UPSET FACTOR {set['upsetFactor']} - {'LOSERS' if set['round'] < 0 else 'WINNERS'} {set['phaseName']}"

            display_score = f"{winner_display} ({winner_entrant['seeding']}) {detailed_score[winner_entrant['id']]} - " \
                            f"{detailed_score[loser_id]} {loser_entrant['name']} ({loser_entrant['seeding']})"

            args = [heading, display_score]

            if set["round"] < 0:
                args.append(f"{loser_entrant['name']} -> out @ {ordinal(set['lPlacement'])} in {set['phaseName']}")

            upset_thread_root = self.twitter.make_tweet(args, upset_thread_root)

            sets_tweeted_out.append(set["id"])

            tweeted_out = True

        if tweeted_out:
            self.cosmos.update_event_upset_thread_root(event_id, upset_thread_root, sets_tweeted_out)

        try:
            num_added = self.cosmos.create_sets(event_id, sets)
            if num_added < total_sets:
                self.logger.log(f"WTF: Added fewer sets than expected for {event_id}")
                raise ValueError()
        except:
            self.logger.log(f"WTF: Something wrong happened with cosmos create sets on {event_id}, creating 1by1")
            for tournament_set in sets:
                self.cosmos.create_set(tournament_set)

    def delete_event(self, event_id):
        self.cosmos.delete_event(event_id)
        self.cosmos.delete_entrants(event_id)
        self.cosmos.delete_sets(event_id)

    def get_and_create_event(self, event_id):
        event = self.api.get_event(event_id)

        if event is None:
            self.logger.log(f"WTF: {event_id} no longer exists")
            return None

        return self.cosmos.create_event(event)

    def get_and_create_entrants_for_event(self, event_id, created_event, is_minutely_operation=True):
        event = created_event

        if event["state"] == "ACTIVE" or is_minutely_operation:
            start_time = int((datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(minutes=10)).timestamp())
        else:
            start_time = int((datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(hours=8)).timestamp())

        if "entrantsLastUpdated" in event and start_time < event["entrantsLastUpdated"] and not event["state"] == "COMPLETED":
            self.logger.log(f"Skip updating entrants for {event_id}")
            return

        event_entrants = self.api.get_ult_event_entrants(event_id)
        db_entrants = self.cosmos.get_event_entrants(event_id)

        event_entrant_ids = set([entrant["id"] for entrant in event_entrants])

        db_entrants_dict = {}
        for db_entrant in db_entrants:
            db_entrants_dict[db_entrant["id"]] = db_entrant["_self"]

        entrants_deleted = 0

        total_event_entrants = len(event_entrant_ids)
        try:
            num_added = self.cosmos.create_entrants(event_id, event_entrants, db_entrants_dict)
            if num_added < total_event_entrants:
                self.logger.log(f"WTF: Added fewer entrants than expected for {event_id}")
                raise ValueError()
        except:
            self.logger.log(f"WTF: Something wrong happened with cosmos create entrants on {event_id}, creating 1by1")
            for entrant in event_entrants:
                self.cosmos.create_entrant(entrant)

        for entrant_id in db_entrants_dict.keys():
            if entrant_id not in event_entrant_ids:
                self.cosmos.delete_entrant(event_id, entrant_id)
                entrants_deleted += 1

        self.logger.log(f"Processed {len(event_entrant_ids)} entrants for event {event_id} and {entrants_deleted} removed")

        self.cosmos.update_event_entrants_last_updated(event_id, int(datetime.datetime.now(datetime.timezone.utc).timestamp()))
