import datetime
import logging

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("", enabled=False)
    operations = OperationsManager(logger)
    logger.log("Starting Database Cleanup")

    event_count = 0
    events = [x for x in list(operations.get_all_events_from_db())]
    total_events = len(events)
    date_now = int((datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=7)).timestamp())

    for event in events:
        event_id = str(event["id"])
        event_count += 1
        logger.log(f"Data cleanup: {event_count} of {total_events}")
        if event["state"] == "ACTIVE":
            if event["startAt"] < date_now:
                num_entrants = event["numEntrants"]
                event_sets = list(operations.cosmos.get_event_sets(event_id))
                if len(event_sets) > num_entrants:
                    logger.log(f"Event {event_id} forced complete")
                    event["state"] = "COMPLETED_FORCED"
                    operations.cosmos.create_event(event)
                    continue
        if operations.api.get_event(event_id) is None:
            try:
                logger.log(f"Event {event_id} marked for cleanup")
                operations.delete_event(event_id)
            except:
                logging.exception("")
                logger.log(f"Error cleaning up {event_id}")
                pass

