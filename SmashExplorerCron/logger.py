from datetime import datetime
import os


class Logger:
    def __init__(self, root_path):
        components = root_path.split("/")
        components.append(f"logs")

        self.root_path = "/".join(components)

        if self.root_path[0] == "/":
            self.root_path = self.root_path[1:]

        current_root = ""
        for component in components:
            if len(component) == 0:
                continue

            current_root += component
            current_root += "/"

            if not os.path.exists(current_root):
                os.mkdir(current_root)

    def __write_log(self, log_string):
        date_now = datetime.utcnow()

        path_root = f"{self.root_path}/{date_now.year}"
        file_name = f"{path_root}/{date_now.year}-{date_now.month}-{date_now.day}-{date_now.hour}.log"

        if not os.path.exists(path_root):
            os.mkdir(path_root)

        with open(file_name, "w+") as infile:
            final_log_string = f"{date_now} - {log_string}"
            infile.write(final_log_string)
            print(final_log_string)

    def log(self, log_string):
        self.__write_log(log_string)
