import os
import time

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/minutely")
    operations = OperationsManager(logger)

    file_name = "minutely.txt"

    if os.path.exists(file_name):
        logger.log("Quitting out because there is an ongoing minutely script")
        exit()

    logger.log("Starting Minutely Script")
    logger.log("Writing lock file")

    f = open(file_name, "a")
    f.write("Running")
    f.close()

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

    attempt = 0

    while attempt < 5:
        try:
            logger.log(f"Removing lock file retry #{attempt}")
            os.remove(file_name)
            attempt += 1
            time.sleep(1)
        except OSError:
            logger.log("Lock file successfully removed")
            logger.log("Minutely Script Complete")
            exit()

    logger.log("Lock file not successfully removed")
    exit()
