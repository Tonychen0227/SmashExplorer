import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/daily_operation")
    operations = OperationsManager(logger)
    logger.log("Starting Daily Script")

    tournament_count = 1
    new_tournament_slugs = operations.get_tournament_slugs()
    tournaments_size = len(new_tournament_slugs)
    for tournament_slug in new_tournament_slugs:
        logger.log(f"Daily operation creating new tournaments - {tournament_count} of {tournaments_size}")
        events = operations.get_and_create_events_for_tournament(tournament_slug)
        for event in events:
            operations.get_and_create_entrants_for_event(event["id"])
            operations.update_event_sets(event["id"])
        tournament_count += 1

    event_count = 1
    open_events = list(operations.get_open_events())
    events_size = len(open_events)
    for event in open_events:
        logger.log(f"Daily operation on {event['id']} - {event_count} of {events_size}")
        operations.get_and_create_entrants_for_event(event["id"])
        event_count += 1

    logger.log("Daily Script Complete")