import os
import traceback

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"backfill")
    operations = OperationsManager(logger, "SMASHGG_KEYS_BACKFILL")
    logger.log("Starting Backfill")

    events_count = 0
    new_events = [1020145, 1011026]
    hardcoded_events = len(new_events) != 0
    if len(new_events) == 0:
        days_back = 14
        days_forward = 3

        current_days_forward = days_back
        increment = 100

        while current_days_forward > days_forward:
            current_days_forward -= increment

            if current_days_forward < days_forward:
                current_days_forward = days_forward
            new_events.extend(operations.get_new_events(days_back=days_back, days_forward=(-1 * current_days_forward)))

            days_back = current_days_forward

    events_size = len(new_events)
    for event_id in new_events:
        event_id = str(event_id)
        events_count += 1
        logger.log(f"Backfill operation creating new events - {events_count} of {events_size}")
        existing_event = operations.get_event_from_db(event_id)

        if existing_event is not None and not hardcoded_events and len(new_events) == 0:
            logger.log(f"Skipping existing event {event_id}")
            continue
        try:
            created_event = operations.get_and_create_event(event_id)
            operations.get_and_create_entrants_for_event(event_id, created_event)
            operations.update_event_sets(event_id, created_event, bypass_last_updated=True, delete_bogus_sets=True)
        except:
            traceback.print_exc()
            logger.log(f"Issue backfilling {event_id}, skipping")
