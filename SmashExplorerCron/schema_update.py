import logging

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("", enabled=False)
    operations = OperationsManager(logger)
    logger.log("Starting Schema Upgrade")

    event_count = 0
    #event_ids = [x["id"] for x in list(operations.get_all_events_from_db())]
    event_ids = []
    total_events = len(event_ids)

    for event_id in [str(x) for x in event_ids]:
        event_count += 1
        try:
            cosmos_sets = list(operations.cosmos.get_event_sets(event_id))
            sets = [x for x in cosmos_sets if not x["id"].startswith("preview") and "createdAt" not in x.keys()]

            logger.log(f"Schema upgrade progress ({event_id}): {event_count} of {total_events}, found {len(sets)} sets")

            set_count = 0
            total_sets = len(sets)
            for db_set in sets:
                set_count += 1
                if set_count % 50 == 0:
                    logger.log(f"Sets upgrade progress: {set_count} of {total_sets}")

                try:
                    operations.cosmos.create_set(operations.api.get_set(db_set["id"], event_id))
                except:
                    logger.log(f"Failed to upgrade set {db_set['id']}")
        except:
            logging.exception("")
            logger.log(f"Failed to update sets for {event_id}")

    set_ids = [46371992]
    for set_id in [str(x) for x in set_ids]:
        operations.cosmos.create_set(operations.api.get_set(set_id))