import datetime
import os

from cosmos import CosmosDB
from smashggapi import API


class OperationsManager:
    def __init__(self):
        endpoint = os.environ["COSMOS_ENDPOINT"]
        key = os.environ["COSMOS_KEY"]
        cosmos = CosmosDB(endpoint, key)
        api = API(os.environ["SMASHGG_KEYS"])

        self.cosmos = cosmos
        self.api = api

    def get_new_tournament_slugs(self):
        date_now = datetime.datetime.utcnow()

        upcoming_tournaments = self.api.get_upcoming_ult_tournaments(date_now - datetime.timedelta(days=1), date_now + datetime.timedelta(weeks=1))

        upcoming_tournaments_slugs = [tournament["slug"] for tournament in upcoming_tournaments]

        if "NOMINATED_TOURNAMENTS" in os.environ:
            upcoming_tournaments_slugs.extend(os.environ["NOMINATED_TOURNAMENTS"].split(" "))

        return upcoming_tournaments_slugs

    def update_event_sets(self, event_id):
        start_time = int(datetime.datetime.utcnow().timestamp())

        event = self.cosmos.get_event(event_id)
        if start_time < event["setsLastUpdated"]:
            return

        self.cosmos.update_event_sets_last_updated(event_id, start_time)

    def get_and_create_events_for_tournament(self, tournament_slug):
        tournament_events = self.api.get_ult_tournament_events(tournament_slug)
        events = self.cosmos.create_events(tournament_events)

        for event in events:
            self.get_and_create_entrants_for_event(event["id"])
            self.update_event_sets(event["id"])

    def get_open_events(self):
        return self.cosmos.get_outstanding_events()

    def get_and_create_entrants_for_event(self, event_id):
        event_entrants = self.api.get_ult_event_entrants(event_id)
        self.cosmos.create_entrants(event_id, event_entrants)

    def update_tracked_entrants_for_event(self, event_id):
        vanity_links = self.cosmos.get_vanity_links(event_id)

        entrants = set()
        for link in vanity_links:
            entrants |= set(link["entrantIds"])

        for entrant in entrants:
            api_event, api_entrant = self.api.get_ult_entrant(entrant)

            self.cosmos.create_entrant(api_event, api_entrant)
