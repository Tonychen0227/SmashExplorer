import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/backfill")
    operations = OperationsManager(logger)
    logger.log("Starting Backfill")

    events_count = 1
    new_events = operations.get_new_events(days_back=45, days_forward=1)
    events_size = len(new_events)
    for event_id in new_events:
        events_count += 1
        logger.log(f"Backfill operation creating new events - {events_count} of {events_size}")
        existing_event = operations.get_event_from_db(event_id)

        if existing_event is not None:
            logger.log(f"Skipping existing event {event_id}")
            continue

        operations.get_and_create_event(event_id)
        operations.get_and_create_entrants_for_event(event_id)
        operations.update_event_sets(event_id)
