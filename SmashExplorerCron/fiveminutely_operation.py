from smashggapi import API
from cosmos import CosmosDB
import os


if __name__ == '__main__':
    endpoint = os.environ["COSMOS_ENDPOINT"]
    key = os.environ["COSMOS_KEY"]
    cosmos = CosmosDB(endpoint, key)
    api = API(os.environ["SMASHGG_KEYS"])

    outstanding_events = cosmos.get_outstanding_events()

    for event in outstanding_events:
        current_vanity_links = cosmos.get_vanity_links(event["id"])

        entrants_requiring_update = set()
        for link in current_vanity_links:
            entrants_requiring_update |= set(link["entrantIds"])

        for entrant in entrants_requiring_update:
            api_event, api_entrant = api.get_ult_entrant(entrant)

            cosmos.create_entrant(api_event, api_entrant)

    # Look through sets