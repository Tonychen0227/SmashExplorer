import datetime
import os

from cosmos import CosmosDB
from smashggapi import API


class OperationsManager:
    def __init__(self, logger):
        endpoint = os.environ["COSMOS_ENDPOINT"]
        key = os.environ["COSMOS_KEY"]
        cosmos = CosmosDB(endpoint, key, logger)
        api = API(os.environ["SMASHGG_KEYS"], logger)

        self.cosmos = cosmos
        self.api = api
        self.logger = logger

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

    def get_new_events(self, days_back=1, days_forward=14):
        date_now = datetime.datetime.now(datetime.timezone.utc)

        start_time = date_now - datetime.timedelta(days=days_back)
        end_time = date_now + datetime.timedelta(days=days_forward)

        upcoming_event_lists = self.api.get_upcoming_ult_events(start_time, end_time)

        upcoming_event_ids = []
        for event_list in upcoming_event_lists:
            for event in event_list:
                if event["numEntrants"] is not None and event["numEntrants"] >= 10:
                    upcoming_event_ids.append(str(event["id"]))

        if "NOMINATED_TOURNAMENTS" in os.environ:
            for slug in os.environ["NOMINATED_TOURNAMENTS"].split(" "):
                upcoming_event_ids.extend([str(event["id"]) for event in self.api.get_ult_tournament_events(slug)["tournament"]["events"]])

        self.logger.log(f"Returned {len(upcoming_event_ids)} upcoming events")

        return upcoming_event_ids

    def update_event_sets(self, event_id):
        start_time = int(datetime.datetime.now(datetime.timezone.utc).timestamp())

        event = self.cosmos.get_event(event_id)
        if start_time < event["setsLastUpdated"]:
            return

        self.cosmos.update_event_sets_last_updated(event_id, start_time)

        sets = self.api.get_event_sets_updated_after_timestamp(event_id, event["setsLastUpdated"])

        self.logger.log(f"Updating {len(sets)} sets for event {event_id}")

        for tournament_set in sets:
            self.cosmos.create_set(tournament_set)

    def get_events_for_tournament(self, tournament_slug):
        return self.api.get_ult_tournament_events(tournament_slug)

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

    def get_and_create_entrants_for_event(self, event_id):
        event_entrants = self.api.get_ult_event_entrants(event_id)
        db_entrants = self.cosmos.get_event_entrants(event_id)

        event_entrant_ids = [entrant["id"] for entrant in event_entrants]
        database_entrant_ids = [entrant["id"] for entrant in db_entrants]

        entrants_added = 0
        entrants_deleted = 0

        for entrant in event_entrants:
            if entrant["id"] not in database_entrant_ids:
                self.cosmos.create_entrant(entrant)
                entrants_added += 1

        for entrant_id in database_entrant_ids:
            if entrant_id not in event_entrant_ids:
                self.cosmos.delete_entrant(event_id, entrant_id)
                entrants_deleted += 1

        self.logger.log(f"Processed {len(event_entrant_ids)} entrants for event {event_id} "
                        f"({entrants_added} added, {entrants_deleted} removed)")
