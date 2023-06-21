import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/daily")
    operations = OperationsManager(logger)
    logger.log("Starting Daily Script")

    events_count = 0
    new_events = operations.get_new_events()
    events_size = len(new_events)
    added_events = []
    for event_id in new_events:
        events_count += 1
        logger.log(f"Daily discovery creating new events - {events_count} of {events_size}")
        existing_event = operations.get_event_from_db(event_id)

        if existing_event is not None:
            logger.log(f"Skipping existing event {event_id}")
            continue

        created_event = operations.get_and_create_event(event_id)
        operations.get_and_create_entrants_for_event(event_id, created_event)
        operations.update_event_sets(event_id, created_event)
        added_events.append(event_id)

    events_count = 0
    event_ids = list(operations.get_open_event_ids())
    events_size = len(event_ids)
    for event_id in event_ids:
        events_count += 1
        logger.log(f"Daily operation on {event_id} ({events_count} of {events_size})")
        if event_id in added_events:
            logger.log(f"Skipping discovered event {event_id}")
            continue

        created_event = operations.get_and_create_event(event_id)
        if created_event is None:
            logger.log(f"Event {event_id} has been deleted")
            continue
        operations.get_and_create_entrants_for_event(event_id, created_event, is_minutely_operation=False)
        operations.update_event_sets(event_id, created_event)

    logger.log("Daily Script Complete")
