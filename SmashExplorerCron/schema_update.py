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
            sets = [x for x in list(operations.api.get_event_sets_updated_after_timestamp(event_id, 1))]

            logger.log(f"Schema upgrade progress ({event_id}): {event_count} of {total_events}, found {len(sets)} sets")

            set_count = 0
            total_sets = len(sets)
            for set_from_smash_gg in sets:
                set_count += 1
                if set_count % 50 == 0:
                    logger.log(f"Sets upgrade progress: {set_count} of {total_sets}")
                operations.cosmos.create_set(set_from_smash_gg)
        except:
            logging.exception("")
            logger.log(f"Failed to update sets for {event_id}")

    set_ids = [46371992, 46283785, 46283747, 46283857, 46755337, 46610794, 46328549]
    for set_id in [str(x) for x in set_ids]:
        operations.cosmos.create_set(operations.api.get_set(set_id))
