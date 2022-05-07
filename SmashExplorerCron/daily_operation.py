from smashggapi import API
from cosmos import CosmosDB
import os


if __name__ == '__main__':
    endpoint = os.environ["COSMOS_ENDPOINT"]
    key = os.environ["COSMOS_KEY"]
    cosmos = CosmosDB(endpoint, key)
    api = API(os.environ["SMASHGG_KEYS"])

    for tournament_slug in ["genesis-8", "ubc-weekly-23-liam-s-long-hard-wood"]:
        if tournament_slug == "genesis-8":
            continue

        tournament_events = api.get_ult_tournament_events(tournament_slug)

        cosmos.create_events(tournament_events)

    outstanding_events = cosmos.get_outstanding_events()
    for event in outstanding_events:
        event_entrants = api.get_ult_event_entrants(event["slug"])
        cosmos.create_entrants(event, event_entrants)
