import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/minutely")
    operations = OperationsManager(logger)
    logger.log("Starting Minutely Script")

    event_count = 0
    event_ids = list(operations.get_active_event_ids())
    events_size = len(event_ids)
    for event_id in event_ids:
        event_count += 1
        logger.log(f"Minutely operation on {event_id} ({event_count} of {events_size})")
        if operations.get_and_create_event(event_id) is None:
            logger.log(f"Event {event_id} has been deleted")
            continue
        operations.get_and_create_entrants_for_event(event_id)
        operations.update_event_sets(event_id)

    logger.log("Minutely Script Complete")
