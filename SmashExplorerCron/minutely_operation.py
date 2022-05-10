import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/minutely")
    operations = OperationsManager(logger)
    logger.log("Starting Minutely Script")

    event_count = 1
    event_ids = list(operations.get_active_event_ids())
    events_size = len(event_ids)
    for event_id in event_ids:
        logger.log(f"Minutely operation on {event_id} ({event_count} of {events_size})")
        operations.update_event_sets(event_id)
        event_count += 1

    logger.log("Minutely Script Complete")
