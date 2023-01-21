import os
import time

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/minutely")
    operations = OperationsManager(logger)

    mutex_name = "minutely"

    can_continue = operations.ensure_and_add_mutex(mutex_name)

    if not can_continue:
        logger.log("Quitting out Minutely Script because there is an ongoing minutely script")
        exit()

    logger.log("Starting Minutely Script, Mutex ensured")

    event_count = 0
    event_ids = list(operations.get_active_event_ids())
    events_size = len(event_ids)
    for event_id in event_ids:
        event_count += 1
        logger.log(f"Minutely operation on {event_id} ({event_count} of {events_size})")
        created_event = operations.get_and_create_event(event_id)
        if created_event is None:
            logger.log(f"Event {event_id} has been deleted")
            continue
        operations.get_and_create_entrants_for_event(event_id, created_event)
        operations.update_event_sets(event_id, created_event)

    logger.log("Removing mutex lock")
    operations.remove_mutex(mutex_name)
    logger.log("mutex lock successfully removed")
    logger.log("Minutely Script Complete")
    exit()
