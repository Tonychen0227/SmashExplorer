from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("datafix")
    operations = OperationsManager(logger)
    logger.log("Starting Datafix")

    event_count = 0
    event_ids = []

    for event_id in [str(x) for x in event_ids]:
        event = operations.cosmos.get_event(event_id)
        event["state"] = "ACTIVE"
        event["setsLastUpdated"] = 1
        operations.cosmos.create_event_datafix(event)
