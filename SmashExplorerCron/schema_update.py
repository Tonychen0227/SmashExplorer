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
        sets = [x for x in list(operations.get_all_sets_from_db(event_id)) if "isUpsetOrNotable" not in x or "entrantIds" not in x or "detailedScore" not in x]

        logger.log(f"Schema upgrade progress: {event_count} of {total_events}, found {len(sets)} sets")

        updated_sets = [operations.api.schema_migration_for_sets(x) for x in sets]

        set_count = 0
        total_sets = len(updated_sets)
        for updated_set in updated_sets:
            set_count += 1
            if set_count % 50 == 0:
                logger.log(f"Sets upgrade progress: {set_count} of {total_sets}")
            operations.cosmos.create_set(updated_set)
