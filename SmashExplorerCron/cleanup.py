import datetime
import logging

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("", enabled=False)
    operations = OperationsManager(logger)
    logger.log("Starting Database Cleanup")

    event_count = 0
    event_ids = [x for x in list(operations.get_open_event_ids())]
    total_events = len(event_ids)
    date_now = int((datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=7)).timestamp())

    for event_id in event_ids:
        event = operations.api.get_event(event_id)
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

