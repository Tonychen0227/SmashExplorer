import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/daily_operation")
    operations = OperationsManager(logger)
    logger.log("Starting Hourly Script")

    events_count = 0
    new_events = operations.get_new_events()
    events_size = len(new_events)
    for event_id in new_events:
        events_count += 1
        logger.log(f"Hourly operation creating new events - {events_count} of {events_size}")
        existing_event = operations.get_event_from_db(event_id)

        if existing_event is not None:
            logger.log(f"Skipping existing event {event_id}")
            continue

        operations.get_and_create_event(event_id)
        operations.get_and_create_entrants_for_event(event_id)
        operations.update_event_sets(event_id)

    events_count = 1
    event_ids = list(operations.get_open_event_ids())
    events_size = len(event_ids)
    for event_id in event_ids:
        logger.log(f"Hourly operation on {event_id} ({events_count} of {events_size})")
        operations.get_and_create_event(event_id)
        operations.get_and_create_entrants_for_event(event_id)
        operations.update_event_sets(event_id)
        events_count += 1

    logger.log("Hourly Script Complete")
