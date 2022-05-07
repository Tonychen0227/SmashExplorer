from operations_manager import OperationsManager


if __name__ == '__main__':
    operations = OperationsManager()

    for tournament_slug in operations.get_new_tournament_slugs():
        operations.get_and_create_events_for_tournament(tournament_slug)

    for event in operations.get_open_events():
        operations.get_and_create_entrants_for_event(event["id"])
