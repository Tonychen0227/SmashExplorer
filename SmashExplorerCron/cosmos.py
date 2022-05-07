from copy import copy
from azure.cosmos import CosmosClient, exceptions


class CosmosDB:
    def __init__(self, endpoint, key):
        self.database = CosmosClient(endpoint, key).get_database_client("smash-explorer-database")
        self.entrants = self.database.get_container_client("Entrants")
        self.events = self.database.get_container_client("Events")
        self.vanityLinks = self.database.get_container_client("VanityLinks")
        self.sets = self.database.get_container_client("Sets")
        self.entrants = self.database.get_container_client("Entrants")

    def __upsert_entrant(self, entrant):
        self.entrants.upsert_item(body=entrant)

    def create_entrant(self, event, entrant):
        root_json = {
            "eventId": event["id"],
            "eventName": event["name"],
            "eventSlug": event["slug"],
            "name": entrant["name"],
            "standing": entrant["standing"]["placement"],
            "id": str(entrant["id"]),
            "seeding": entrant["initialSeedNum"]
        }

        self.__upsert_entrant(root_json)

    def create_entrants(self, event, entrants):
        for entrant in entrants:
            self.create_entrant(event, entrant)

    def __upsert_event(self, event):
        self.events.upsert_item(body=event)

    def get_event(self, event_id):
        try:
            response = self.events.read_item(item=str(event_id), partition_key=id)
        except exceptions.CosmosResourceNotFoundError:
            response = None

        return response

    def get_outstanding_events(self):
        response = self.events.query_items(query="SELECT * FROM k WHERE k.state = \"COMPLETED\"",
                                           enable_cross_partition_query=True)
        return response

    def create_event(self, tournament, event):
        root_json = {
            "tournamentSlug": tournament["slug"],
            "tournamentName": tournament["name"],
            "tournamentLocation": f"{tournament['city']}, {tournament['countryCode']}",
            "state": event["state"],
            "id": str(event["id"]),
            "name": event["name"],
            "startAt": event["startAt"],
            "slug": event["slug"],
            "numEntrants": event["numEntrants"],
            "standings": event["standings"]["nodes"]
        }

        self.__upsert_event(root_json)

    def create_events(self, smashgg_tournament_with_events):
        for event in smashgg_tournament_with_events["tournament"]["events"]:
            self.create_event(smashgg_tournament_with_events["tournament"], event)

    def get_vanity_links(self, event_id):
        response = self.vanityLinks.query_items(query=f"SELECT * FROM r WHERE r.eventId = \"{event_id}\"", partition_key=event_id)
        return response
