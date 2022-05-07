import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/hourly_operation")
    operations = OperationsManager(logger)
    logger.log("Starting Minutely Script")

    event_count = 1
    open_events = list(operations.get_open_events())
    for event in open_events:
        logger.log(f"Minutely operation on {event['id']} ({event_count} of {len(open_events)})")
        operations.update_tracked_entrants_for_event(event["id"])
        operations.update_event_sets(event["id"])
        event_count += 1
