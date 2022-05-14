import logging

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("schema_update")
    operations = OperationsManager(logger)
    logger.log("Starting Datafix")

    event_count = 0
    event_ids = [715686]

    for event_id in [str(x) for x in event_ids]:
        event = operations.cosmos.get_event(event_id)
        event["setsLastUpdated"] = 1
        operations.cosmos.create_event(event)
