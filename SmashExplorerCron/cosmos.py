import datetime

from azure.cosmos import CosmosClient, exceptions
from azure.cosmos.exceptions import CosmosResourceNotFoundError


class CosmosDB:
    def __init__(self, endpoint, key, logger):
        self.database = CosmosClient(endpoint, key).get_database_client("smash-explorer-database")
        self.entrants = self.database.get_container_client("Entrants")
        self.events = self.database.get_container_client("Events")
        self.vanityLinks = self.database.get_container_client("VanityLinks")
        self.sets = self.database.get_container_client("Sets")
        self.mutex = self.database.get_container_client("Mutex")
        self.logger = logger

    def remove_mutex(self, name_mutex):
        try:
            self.mutex.delete_item(item=name_mutex, partition_key=name_mutex)
        except CosmosResourceNotFoundError:
            self.logger.log("Cannot delete mutex as it is already gone")
            pass

        return

    def ensure_and_add_mutex(self, name_mutex):
        try:
            self.mutex.read_item(item=name_mutex, partition_key=name_mutex)
        except CosmosResourceNotFoundError:
            self.mutex.upsert_item(body={"id": name_mutex})
            return True

        return False

    # region Entrants
    def __upsert_entrant(self, entrant):
        return self.entrants.upsert_item(body=entrant)

    def create_entrants(self, event_id, entrants, db_entrants_dict):
        for entrant in entrants:
            if entrant["id"] in db_entrants_dict:
                entrant["_self"] = db_entrants_dict[entrant["id"]]
        return self.entrants.scripts.execute_stored_procedure("bulkImport2", partition_key=event_id, params=[entrants])

    def create_entrant(self, entrant):
        self.__upsert_entrant(entrant)

    def get_event_entrants(self, event_id):
        response = self.entrants.query_items(query=f"SELECT k.id, k._self FROM k WHERE k.eventId = \"{event_id}\"",
                                             partition_key=event_id)
        return response

    def get_entrant(self, event_id, entrant_id):
        response = self.entrants.read_item(item=str(entrant_id), partition_key=event_id)
        return response

    def delete_entrant(self, event_id, entrant_id):
        self.entrants.delete_item(item=entrant_id, partition_key=event_id)

    def delete_entrants(self, event_id):
        for entrant in self.get_event_entrants(event_id):
            self.delete_entrant(event_id, entrant["id"])

    # endregion Entrants

    # region Events
    def __upsert_event(self, event):
        return self.events.upsert_item(body=event)

    def create_event_datafix(self, event):
        return self.__upsert_event(event)

    def create_event(self, event):
        existing_event = self.get_event(event["id"])
        event["setsLastUpdated"] = 1 if existing_event is None else existing_event["setsLastUpdated"]
        event["entrantsLastUpdated"] = 1 if (existing_event is None or "entrantsLastUpdated" not in existing_event) else existing_event["entrantsLastUpdated"]
        event["upsetThreadRoot"] = None if existing_event is None or "upsetThreadRoot" not in existing_event else existing_event["upsetThreadRoot"]
        event["doUpsetThread"] = False if existing_event is None or "doUpsetThread" not in existing_event else existing_event["doUpsetThread"]
        event["upsetThreadFactor"] = False if existing_event is None or "upsetThreadFactor" not in existing_event else  existing_event["upsetThreadFactor"]
        event["setsAlreadyTweeted"] = [] if existing_event is None or "setsAlreadyTweeted" not in existing_event else existing_event["setsAlreadyTweeted"]
        return self.create_event_datafix(event)

    def get_all_events(self):
        return self.events.query_items(query="SELECT * FROM k",
                                       enable_cross_partition_query=True)

    def get_event(self, event_id):
        try:
            response = self.events.read_item(item=str(event_id), partition_key=str(event_id))
        except exceptions.CosmosResourceNotFoundError:
            response = None

        return response

    def delete_event(self, event_id):
        return self.events.delete_item(item=event_id, partition_key=event_id)

    def get_active_event_ids(self):
        date_now_minus_6_hours = int((datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(hours=6)).timestamp())
        date_now_minus_1_week = int((datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=7)).timestamp())
        date_now_plus_8_hours = int((datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(hours=8)).timestamp())

        response = self.events.query_items(query=f"SELECT k.id FROM k WHERE (k.state = \"ACTIVE\" AND k.startAt > {date_now_minus_1_week})"
                                                 f"OR (k.startAt > {date_now_minus_6_hours} and k.startAt < {date_now_plus_8_hours})",
                                           enable_cross_partition_query=True)
        return [x["id"] for x in response]

    def get_outstanding_event_ids(self):
        response = self.events.query_items(query="SELECT k.id FROM k WHERE NOT STARTSWITH(k.state, \"COMPLETED\")",
                                           enable_cross_partition_query=True)
        return [x["id"] for x in response]

    def update_event_sets_last_updated(self, event_id, last_updated):
        event = self.get_event(event_id)
        event["setsLastUpdated"] = last_updated

        self.__upsert_event(event)

    def update_event_entrants_last_updated(self, event_id, last_updated):
        event = self.get_event(event_id)
        event["entrantsLastUpdated"] = last_updated
        self.__upsert_event(event)

    def update_event_upset_thread_root(self, event_id, upset_thread_root, sets_tweeted_out):
        event = self.get_event(event_id)
        event["upsetThreadRoot"] = upset_thread_root
        event["setsAlreadyTweeted"] = sets_tweeted_out
        self.__upsert_event(event)

    # endregion Events

    # region Sets
    def __upsert_set(self, tournament_set):
        return self.sets.upsert_item(body=tournament_set)

    def get_all_sets(self):
        return self.sets.query_items(query="SELECT * FROM k",
                                       enable_cross_partition_query=True)

    def get_event_sets(self, event_id):
        return self.sets.query_items(query=f"SELECT * FROM k WHERE k.eventId = \"{event_id}\"",
                                     partition_key=event_id)

    def create_sets(self, event_id, sets):
        return self.sets.scripts.execute_stored_procedure("bulkImport", partition_key=event_id, params=[sets])

    def create_set(self, tournament_set):
        self.__upsert_set(tournament_set)

    def delete_set(self, event_id, set_id):
        self.sets.delete_item(item=set_id, partition_key=event_id)

    def delete_sets(self, event_id):
        for db_set in self.get_event_sets(event_id):
            self.delete_set(event_id, db_set)
    # endregion Sets
