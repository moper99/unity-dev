import time

cur_time = time.time()
def get_time_consume():
    global cur_time
    consume_time = time.time() - cur_time
    cur_time = time.time()
    return int(consume_time * 1000)

def get_time_consume_str():
    return str(get_time_consume())

def clear_time_consume():
    global cur_time
    cur_time = time.time()