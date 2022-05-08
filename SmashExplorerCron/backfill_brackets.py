import os

from logger import Logger
from operations_manager import OperationsManager


if __name__ == '__main__':
    logger = Logger(f"{os.environ['SMASH_EXPLORER_LOG_ROOT']}/daily_operation")
    operations = OperationsManager(logger)
    logger.log("Starting Backfill")

    tournament_count = 1
    new_tournament_slugs = operations.get_tournament_slugs(days_back=30, days_forward=0)
    tournaments_size = len(new_tournament_slugs)
    for tournament_slug in new_tournament_slugs:
        logger.log(f"Daily operation creating new tournaments - {tournament_count} of {tournaments_size}")
        events = operations.get_and_create_events_for_tournament(tournament_slug)
        for event in events:
            operations.get_and_create_entrants_for_event(event["id"])
            operations.update_event_sets(event["id"])
        tournament_count += 1
