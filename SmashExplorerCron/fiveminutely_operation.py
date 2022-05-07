from operations_manager import OperationsManager


if __name__ == '__main__':
    operations = OperationsManager()

    for event in operations.get_open_events():
        operations.update_tracked_entrants_for_event(event["id"])
        operations.update_event_sets(event["id"])
