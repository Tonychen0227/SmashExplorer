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

    def get_open_events(self):
        return self.cosmos.get_outstanding_events()

    def get_tournament_slugs(self, days_back=0, days_forward=7):
        date_now = datetime.datetime.now(datetime.timezone.utc)

        start_time = date_now - datetime.timedelta(days=days_back)
        end_time = date_now + datetime.timedelta(days=days_forward)

        upcoming_tournaments = self.api.get_upcoming_ult_tournaments(start_time, end_time)

        upcoming_tournaments_slugs = [tournament["slug"] for tournament in upcoming_tournaments]
        nominated_tournaments = [] if "NOMINATED_TOURNAMENTS" not in os.environ else os.environ["NOMINATED_TOURNAMENTS"].split(" ")

        self.logger.log(f"Returned {len(upcoming_tournaments_slugs)} upcoming tournaments "
                        f"and {len(nominated_tournaments)} nominated tournaments")

        upcoming_tournaments_slugs.extend(nominated_tournaments)
        return upcoming_tournaments_slugs

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

    def get_and_create_events_for_tournament(self, tournament_slug):
        events = self.api.get_ult_tournament_events(tournament_slug)

        self.logger.log(f"Creating {len(events)} events for tournament {tournament_slug}")

        return self.cosmos.create_events(events)

    def get_and_create_entrants_for_event(self, event_id):
        event_entrants = self.api.get_ult_event_entrants(event_id)
        event_entrant_ids = [entrant["id"] for entrant in event_entrants]
        database_entrant_ids = [x["id"] for x in self.cosmos.get_event_entrants(event_id)]

        entrants_added = 0
        entrants_deleted = 0

        for entrant in event_entrants:
            if entrant["id"] not in database_entrant_ids:
                self.cosmos.create_entrant(entrant)
                entrants_added += 1

        for db_id in database_entrant_ids:
            if db_id not in event_entrant_ids:
                self.cosmos.delete_entrant(db_id)
                entrants_deleted += 1

        self.logger.log(f"Processed {len(event_entrant_ids)} entrants for event {event_id} "
                        f"({entrants_added} added, {entrants_deleted} removed)")

    def update_event(self, event_id):
        existing_event = self.cosmos.get_event(event_id)
        new_event_data = self.api.get_ult_event(event_id)["event"]

        for key in ["state", "name", "startAt", "createdAt", "updatedAt", "slug", "numEntrants", "standings"]:
            existing_event[key] = new_event_data[key]

        self.cosmos.update_event(existing_event)

    def update_tracked_entrants_for_event(self, event_id):
        vanity_links = self.cosmos.get_vanity_links(event_id)

        entrants = set()
        for link in vanity_links:
            entrants |= set(link["entrantIds"])

        self.logger.log(f"Creating {len(entrants)} entrants based on vanity links for event {event_id}")

        for entrant in entrants:
            api_event, api_entrant = self.api.get_ult_entrant(entrant)

            self.cosmos.create_entrant(api_entrant)