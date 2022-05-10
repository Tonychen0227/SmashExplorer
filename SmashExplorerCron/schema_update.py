from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("", enabled=False)
    operations = OperationsManager(logger)
    logger.log("Starting Schema Upgrade")

    event_count = 0
    event_ids = [x["id"] for x in list(operations.get_all_events_from_db())]
    total_events = len(event_ids)

    for event_id in event_ids:
        event_count += 1
        sets = [x for x in list(operations.get_all_sets_from_db(event_id))]

        logger.log(f"Schema upgrade progress ({event_id}): {event_count} of {total_events}, found {len(sets)} sets")

        set_count = 0
        total_sets = len(sets)
        for set_id in [x["id"] for x in sets]:
            set_count += 1
            if set_count % 50 == 0:
                logger.log(f"Sets upgrade progress: {set_count} of {total_sets}")
            set_from_smash_gg = operations.api.get_set(set_id)
            #operations.cosmos.create_set(updated_set)

