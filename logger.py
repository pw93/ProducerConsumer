# -*- coding: utf-8 -*-
from loguru import logger
import os
import sys
import threading
from enum import Enum
from enum import IntEnum
from datetime import datetime
'''
change logdir:
base_dir = f"C:\\data\\code\\aistk_system\\database\\aistk\\log\\{dirymd}"

'''

class LogLevel(IntEnum):

    DEBUG = 1
    INFO = 2
    WARNING = 3
    ERROR = 4


class LogFormat(Enum):
    COMPACT  = 1
    FULL = 2

class LogSink(Enum):
    CONSOLE  = 1
    FILE = 2
    BOTH = 3
    NONE = 4

def LogLevel_to_string(level: LogLevel) -> str:

    if level == LogLevel.DEBUG:
        return "DEBUG"
    elif level == LogLevel.INFO:
        return "INFO"
    elif level == LogLevel.WARNING:
        return "WARNING"
    elif level == LogLevel.ERROR:
        return "ERROR"
    elif level == LogLevel.NONE:
        return "NONE"
    else:
        return "UNKNOWN"



#[21:48:21.140][INFO  logger.py:ff:169] this is info
def custom_format_compact(record):
    logger.level("DEBUG", color="<blue>")
    logger.level("INFO", color="<black>")
    logger.level("WARNING", color="<black><bg #ffe000>")
    logger.level("ERROR", color="<black><bg #ff6060>")
    time = record["time"].strftime("%H:%M:%S.%f")[:-3]
    level = record["level"].name
    file = record["file"].name
    func = record["function"]
    line = record["line"]
    message = record["message"]

    # Add colors using record["level"].color
    return (
        #f"<green>[{time}][{level:5} {pid}:{tid} {file}:{func}:{line}]\n</green>"
        f"<green><bg #eeeeee>[{time}][{level:5} {file}:{func}:{line}]</bg #eeeeee></green> "
        f"<level>{message}</level>\n"
    )

#[2025-07-27 21:49:28.512][INFO  27436:56908 logger.py:ff:169] this is info
def custom_format_full(record):
    #Foreground (text): <red>, <green>, <yellow>, <blue>, <black>, <white>, etc.
    logger.level("DEBUG", color="<blue>")
    logger.level("INFO", color="<black>")
    logger.level("WARNING", color="<black><bg #ffe000>")
    logger.level("ERROR", color="<black><bg #ff6060>")
    pid = os.getpid()
    tid = threading.get_ident()
    time = record["time"].strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]
    level = record["level"].name
    file = record["file"].name
    func = record["function"]
    line = record["line"]
    message = record["message"]

    # Add colors using record["level"].color
    return (
        f"<green><bg #eeeeee>[{time}][{level:5} {pid}:{tid} {file}:{func}:{line}]</bg #eeeeee></green> "
        f"<level>{message}</level>\n"
    )


def log_init(log_mode=LogFormat.COMPACT, level=LogLevel.DEBUG, sink = LogSink.BOTH):

    print('===============')
    print('log_init')
    print(f'log_mode: {log_mode}, level: {level.name}, sink: {sink}')


    now = datetime.now()
    dirymd = now.strftime("%Y%m%d")
    base_dir = f"C:\\data\\code\\aistk_system\\database\\aistk\\log\\{dirymd}"

    # 建立資料夾並找可用 index
    os.makedirs(base_dir, exist_ok=True)

    ind_max = -1
    fs = [d for d in os.listdir(base_dir) if not os.path.isdir(os.path.join(base_dir, d))]

    for d in fs:
        if len(d) >= 3:
            try:
                ind = int(d[:3])
                if ind > ind_max:
                    ind_max = ind
            except ValueError:
                continue


    timestr = now.strftime("%H%M%S")
    fname_err = os.path.join(base_dir, f"{ind_max+1:03}_{timestr}_err.log")
    fname_all = os.path.join(base_dir, f"{ind_max+1:03}_{timestr}_all.log")

    if log_mode==LogFormat.COMPACT:
        custom_format = custom_format_compact
    elif log_mode==LogFormat.FULL:
        custom_format = custom_format_full
    else:
        print("error: log_mode wrong. [log_mode: LogFormat.COMPACT, LogFormat.FULL]")

    slevel = LogLevel_to_string(level)

    to_console = 0
    to_file = 0
    if sink == LogSink.BOTH:
        to_console=1
        to_file=1
    if sink == LogSink.CONSOLE:
        to_console=1
    if sink == LogSink.FILE:
        to_file=1

    logger.remove()
    if to_console:
        logger.add(sys.stderr, format=custom_format, level=slevel)


    if to_file:
        print('log_err: ',fname_err)
        print('log_all: ',fname_all)

        if level >= LogLevel.WARNING:
            logger.add(fname_err, rotation="1 MB", retention="7 days", format=custom_format, level=level)
        else:
            logger.add(fname_err, rotation="1 MB", retention="7 days", format=custom_format, level="WARNING")
        logger.add(fname_all, rotation="1 MB", retention="7 days", format=custom_format, level=slevel)

    print('===============')
    sys.stdout.flush()


from types import MethodType

logger.verbose = MethodType(lambda self, msg, *a, **kw: self.log("VERBOSE", msg, *a, **kw), logger)

logger.logd = logger.debug
logger.logi = logger.info
logger.logw = logger.warning
logger.loge = logger.error

logd = logger.debug
logi = logger.info
logw = logger.warning
loge = logger.error


def ff():

    logger.logi("===================")

    logger.debug("this is debug")
    logger.info("this is info")
    logger.warning("this is warning")
    logger.error("this is error")

if __name__ == '__main__':
    log_init(LogFormat.FULL, level=LogLevel.DEBUG)
    ff()


