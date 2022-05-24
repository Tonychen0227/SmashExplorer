from logger import Logger
from operations_manager import OperationsManager
import requests


logger = Logger("", enabled=False)
operations = OperationsManager(logger)

for character in operations.api.get_game_characters("1")['videogame']['characters']:
    with open(f'{character["id"]}.jpg', 'wb') as handle:
        response = requests.get(character['images'][1]['url'], stream=True)

        if not response.ok:
            print(response)

        for block in response.iter_content(1024):
            if not block:
                break

            handle.write(block)
