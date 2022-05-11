import logging

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("", enabled=False)
    operations = OperationsManager(logger)
    logger.log("Starting Database Cleanup")

    event_count = 0
    event_ids = [x["id"] for x in list(operations.get_all_events_from_db())]
    total_events = len(event_ids)

    for event_id in [str(x) for x in event_ids]:
        event_count += 1
        logger.log(f"Data cleanup: {event_count} of {total_events}")
        if operations.api.get_event(event_id) is None:
            try:
                logger.log(f"Event {event_id} marked for cleanup")
                operations.delete_event(event_id)
            except:
                logging.exception("")
                logger.log(f"Error cleaning up {event_id}")
                pass
