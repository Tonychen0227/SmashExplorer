import os
import traceback

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"backfill")
    operations = OperationsManager(logger, "SMASHGG_KEYS_BOBC")
    logger.log("Starting Backfill")

    events_count = 0
    new_events = [829437, 829440, 839427, 839428]

    events_size = len(new_events)
    for event_id in new_events:
        event_id = str(event_id)
        events_count += 1
        logger.log(f"Backfill operation creating new events - {events_count} of {events_size}")
        existing_event = operations.get_event_from_db(event_id)

        try:
            created_event = operations.get_and_create_event(event_id)
            operations.get_and_create_entrants_for_event(event_id, created_event)
            operations.update_event_sets(event_id, created_event, bypass_last_updated=False, lookback_duration_minutes=30)
        except:
            traceback.print_exc()
            logger.log(f"Issue backfilling {event_id}, skipping")
