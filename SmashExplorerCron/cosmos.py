from azure.cosmos import CosmosClient, exceptions


class CosmosDB:
    def __init__(self, endpoint, key, logger):
        self.database = CosmosClient(endpoint, key).get_database_client("smash-explorer-database")
        self.entrants = self.database.get_container_client("Entrants")
        self.events = self.database.get_container_client("Events")
        self.vanityLinks = self.database.get_container_client("VanityLinks")
        self.sets = self.database.get_container_client("Sets")
        self.logger = logger

    # region Entrants
    def __upsert_entrant(self, entrant):
        return self.entrants.upsert_item(body=entrant)

    def create_entrant(self, entrant):
        self.__upsert_entrant(entrant)

    def get_event_entrants(self, event_id):
        response = self.entrants.query_items(query=f"SELECT k.id FROM k WHERE k.eventId = \"{event_id}\"",
                                             partition_key=event_id)
        return response

    def delete_entrant(self, entrant_id):
        self.entrants.delete_item(id=entrant_id, partition_key=entrant_id)
    # endregion Entrants

    # region Events
    def __upsert_event(self, event):
        return self.events.upsert_item(body=event)

    def create_event(self, event):
        existing_event = self.get_event(event["id"])
        event["setsLastUpdated"] = 1 if existing_event is None else existing_event["setsLastUpdated"]

        return self.__upsert_event(event)

    def get_event(self, event_id):
        try:
            response = self.events.read_item(item=str(event_id), partition_key=str(event_id))
        except exceptions.CosmosResourceNotFoundError:
            response = None

        return response

    def get_outstanding_event_ids(self):
        response = self.events.query_items(query="SELECT k.id FROM k WHERE k.state <> \"COMPLETED\"",
                                           enable_cross_partition_query=True)
        return [x["id"] for x in response]

    def update_event_sets_last_updated(self, event_id, last_updated):
        event = self.get_event(event_id)
        event["setsLastUpdated"] = last_updated

        self.__upsert_event(event)
    # endregion Events

    # region Sets
    def __upsert_set(self, tournament_set):
        return self.sets.upsert_item(body=tournament_set)

    def create_set(self, tournament_set):
        self.__upsert_set(tournament_set)
    # endregion Sets

    def get_vanity_links(self, event_id):
        response = self.vanityLinks.query_items(query=f"SELECT * FROM r WHERE r.eventId = \"{event_id}\"", partition_key=event_id)
        return response
