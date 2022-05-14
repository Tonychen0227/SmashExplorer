import logging

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("schema_update")
    operations = OperationsManager(logger)
    logger.log("Starting Schema Upgrade")

    event_count = 0
    event_ids = [x["id"] for x in list(operations.get_all_events_from_db())]
    total_events = len(event_ids)

    for event_id in [str(x) for x in event_ids]:
        event_count += 1
        try:
            cosmos_sets = list(operations.cosmos.get_event_sets(event_id))
            sets = [x for x in cosmos_sets if x["id"].startswith("preview")]

            logger.log(f"Schema upgrade progress ({event_id}): {event_count} of {total_events}, found {len(sets)} sets - {[x['id'] for x in sets]}")
            continue
            total_sets = len(sets)
            for db_set in sets:
                try:
                    db_set["isUpsetOrNotable"] = True
                    operations.cosmos.create_set(db_set)
                except:
                    logger.log(f"Failed to upgrade set {db_set['id']}")
        except:
            logging.exception("")
            logger.log(f"Failed to update sets for {event_id}")
