#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os
from collections import OrderedDict


# def make_dir_clean(data_path):
#     if not os.path.exists(data_path):
#         os.mkdir(data_path)
#     else:
#         shutil.rmtree(data_path)
#         os.mkdir(data_path)


def check_path_exists(dst_path):
    if not os.path.exists(dst_path):
        raise Exception(f"目录/文件不存在：{dst_path}")


# def clear_file(file_name):
#     if os.path.exists(file_name):
#         os.remove(file_name)
#     return file_name
#
#

class AppendableDict(OrderedDict):
    def __init__(self, *args, **kwargs):
        self.append_key = 0
        super(OrderedDict, self).__init__(*args, **kwargs)

    def append_value(self, value):
        self[self.append_key] = value
        self.append_key = self.append_key + 1


def index_to_col_name(index):
    left = (index - 1) % 26
    a = (index - 1) // 26
    if a == 0:
        return chr(ord('A') + left)
    else:
        return chr(ord('A') + a - 1) + chr(ord('A') + left)
