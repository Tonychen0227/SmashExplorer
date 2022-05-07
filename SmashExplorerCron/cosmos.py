from copy import copy
from datetime import datetime

from azure.cosmos import CosmosClient, exceptions


class CosmosDB:
    def __init__(self, endpoint, key, logger):
        self.database = CosmosClient(endpoint, key).get_database_client("smash-explorer-database")
        self.entrants = self.database.get_container_client("Entrants")
        self.events = self.database.get_container_client("Events")
        self.vanityLinks = self.database.get_container_client("VanityLinks")
        self.sets = self.database.get_container_client("Sets")
        self.entrants = self.database.get_container_client("Entrants")
        self.logger = logger

    def __upsert_entrant(self, entrant):
        entrant["lastUpdatedTime"] = datetime.utcnow().timestamp()

        return self.entrants.upsert_item(body=entrant)

    def create_entrant(self, event, entrant):
        if entrant["standing"] is None:
            standing = None
        else:
            standing = entrant["standing"]["placement"]

        root_json = {
            "eventId": event["id"],
            "eventName": event["name"],
            "eventSlug": event["slug"],
            "name": entrant["name"],
            "standing": standing,
            "id": str(entrant["id"]),
            "seeding": entrant["initialSeedNum"],
            "participants": entrant["participants"]
        }

        self.__upsert_entrant(root_json)

    def create_entrants(self, event_id, entrants):
        event = self.get_event(event_id)
        for entrant in entrants:
            self.create_entrant(event, entrant)

    def __upsert_event(self, event):
        event["lastUpdatedTime"] = datetime.utcnow().timestamp()
        return self.events.upsert_item(body=event)

    def get_event(self, event_id):
        try:
            response = self.events.read_item(item=str(event_id), partition_key=str(event_id))
        except exceptions.CosmosResourceNotFoundError:
            response = None

        return response

    def get_event_entrants(self, event_id):
        response = self.entrants.query_items(query=f"SELECT k.id FROM k WHERE k.eventId = \"{event_id}\"",
                                           partition_key=event_id)
        return response

    def get_outstanding_events(self):
        response = self.events.query_items(query="SELECT * FROM k WHERE k.state <> \"COMPLETED\"",
                                           enable_cross_partition_query=True)
        return response

    def create_event(self, tournament, event, sets_last_updated):
        root_json = {
            "tournamentSlug": tournament["slug"],
            "tournamentName": tournament["name"],
            "tournamentLocation": f"{tournament['city']}, {tournament['countryCode']}",
            "state": event["state"],
            "id": str(event["id"]),
            "name": event["name"],
            "startAt": event["startAt"],
            "createdAt": event["createdAt"],
            "updatedAt": event["updatedAt"],
            "slug": event["slug"],
            "numEntrants": event["numEntrants"],
            "standings": event["standings"]["nodes"],
            "setsLastUpdated": sets_last_updated
        }

        return self.__upsert_event(root_json)

    def update_event_sets_last_updated(self, event_id, last_updated):
        event = self.get_event(event_id)
        event["setsLastUpdated"] = last_updated

        self.__upsert_event(event)

    def __upsert_set(self, set):
        return self.sets.upsert_item(body=set)

    def create_set(self, event, set):
        set["id"] = str(set["id"])
        set["eventId"] = str(event["id"])
        set["phaseOrder"] = set["phaseGroup"]["phase"]["phaseOrder"]
        set["phaseName"] = set["phaseGroup"]["phase"]["name"]
        set["entrants"] = [
            {
                "name": None if x["entrant"] is None else x["entrant"]["name"],
                "id": None if x["entrant"] is None else x["entrant"]["id"],
                "seeding": None if x["entrant"] is None else x["entrant"]["initialSeedNum"],
                "prereqId": x["prereqId"],
                "prereqType": x["prereqType"]
            } for x in set["slots"]
        ]

        del set["slots"], set["phaseGroup"]
        self.__upsert_set(set)

    def create_events(self, smashgg_tournament_with_events):
        events = []

        for event in smashgg_tournament_with_events["tournament"]["events"]:
            existing_event = self.get_event(str(event["id"]))
            sets_last_updated = 1

            if existing_event is not None:
                sets_last_updated = existing_event["setsLastUpdated"]

            created_event = self.create_event(smashgg_tournament_with_events["tournament"], event, sets_last_updated)
            events.append(created_event)

        return events

    def get_vanity_links(self, event_id):
        response = self.vanityLinks.query_items(query=f"SELECT * FROM r WHERE r.eventId = \"{event_id}\"", partition_key=event_id)
        return response
