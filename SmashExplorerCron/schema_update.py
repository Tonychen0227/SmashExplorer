from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger("", enabled=False)
    operations = OperationsManager(logger)
    logger.log("Starting Schema Upgrade")

    event_count = 0
    #event_ids = [x["id"] for x in list(operations.get_all_events_from_db())]
    event_ids = ["400198"]
    total_events = len(event_ids)

    for event_id in event_ids:
        event_count += 1
        print(f"Schema upgrade progress: {event_count} of {total_events}")

        sets = list(operations.get_all_sets_from_db(event_id))

        print(f"Schema upgrade progress: {event_count} of {total_events}, found {len(sets)} sets")

        updated_sets = [operations.api.enrich_with_upsets_details(x) for x in sets]

        for updated_set in updated_sets:
            operations.cosmos.create_set(updated_set)

