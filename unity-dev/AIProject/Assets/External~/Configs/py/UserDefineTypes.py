#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import re
from collections import OrderedDict
import importlib

# from abc import ABCMeta

# noinspection PyUnreachableCode
if False:
    from TableEnv import TableEnv

bracketPat = re.compile(r"([^(]+)\((.*)\)")
listPat = re.compile("list<(.*)>")
enumDefPat = re.compile("enumdef<(.*)>")
enumPat = re.compile("enum<(.*)>")
dictPat = re.compile("dict<([^,]*),(.*)>")
tuplePat = re.compile(r"tuple<(.*)>")
arrayPat = re.compile(r"(.*)\[]")
listSplitStr = re.compile(r"\s*\|\s*")


class UserDefineType:
    @staticmethod
    def convert_value(s, key_val=None, field_name=None):
        return str(s)

    @staticmethod
    def value_to_client_str(v):
        return f"\"{v}\""

    @staticmethod
    def all_res_names():
        return []

    @staticmethod
    def check_res_exist(val):
        return True


class DefaultStr(UserDefineType):
    def __init__(self, can_null, depend_tab):
        self.canNull = can_null
        self.dependTab = depend_tab

    def convert_value(self, s, key_val=None, field_name=None):
        if s is None or s == '':
            if self.canNull:
                return None
            else:
                return ""
        return str(s)

    @staticmethod
    def server_type_name():
        return "string"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "string?"
        return "string"

    def value_to_client_str(self, v):
        # ss = re.escape(v) # no!
        if v is None and self.canNull:
            return "null"
        ss = v.translate(str.maketrans({"\"": r"\"", "\n": r"\n", "\\": r"\\"}))
        return f"\"{ss}\""

    def has_depend(self):
        return self.dependTab is not None


class DefaultLong(UserDefineType):
    def __init__(self, can_null, depend_tab):
        self.canNull = can_null
        self.dependTab = depend_tab

    def convert_value(self, s, key_val=None, field_name=None):
        if self.canNull and s is None:
            return None
        return int(s)

    @staticmethod
    def server_type_name():
        return "long"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "long?"
        return "long"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        return f"{v}"

    def has_depend(self):
        return self.dependTab is not None


class DefaultInt(UserDefineType):
    def __init__(self, can_null, depend_tab=None):
        self.canNull = can_null
        self.dependTab = depend_tab

    def convert_value(self, s, key_val=None, field_name=None):
        if s is None or s == '':
            if self.canNull:
                return None
            else:
                return 0
        return int(s)

    @staticmethod
    def server_type_name():
        return "int"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "int?"
        return "int"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        return f"{v}"

    def has_depend(self):
        return self.dependTab is not None


class DefaultFloat(UserDefineType):
    def __init__(self, can_null, depend_tab=None):
        self.canNull = can_null
        self.dependTab = depend_tab

    def convert_value(self, s, key_val=None, field_name=None):
        if s is None or s == '':
            if self.canNull:
                return None
            else:
                return 0
        return float(s)

    @staticmethod
    def server_type_name():
        return "float"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "float?"
        return "float"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        return f"{v}f"

    def has_depend(self):
        return self.dependTab is not None


class DefaultBool(UserDefineType):
    def __init__(self, can_null, depend_tab=None):
        self.canNull = can_null
        self.dependTab = depend_tab

    def convert_value(self, s, key_val=None, field_name=None):
        if s is None or s == '':
            return False
        # 如果已经是布尔类型，直接返回
        if isinstance(s, bool):
            return s
        # 如果是字符串类型，进行多种格式的解析
        if isinstance(s, str):
            s_lower = s.lower().strip()
            # 支持中文：是/否
            if s_lower == "是":
                return True
            elif s_lower == "否":
                return False
            # 支持英文：true/false
            elif s_lower == "true":
                return True
            elif s_lower == "false":
                return False
            # 支持数字：1/0
            elif s_lower == "1":
                return True
            elif s_lower == "0":
                return False
            else:
                return False
        else:
            return False

    @staticmethod
    def server_type_name():
        return "bool"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "bool?"
        return "bool"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        return "true" if v else "false"

    def has_depend(self):
        return self.dependTab is not None


cur_output_name = None


def set_output_name(new_name):
    global cur_output_name
    cur_output_name = new_name


class I18n(DefaultStr):
    def __init__(self, can_null, depend_tab):
        super().__init__(can_null, depend_tab)

    def convert_value(self, s, key_val=None, field_name=None):
        if self.canNull and s is None:
            return None
        global cur_output_name
        str_val = super().convert_value(s, key_val, field_name)
        if cur_output_name == "WsErrorCodeMsgConfig" or cur_output_name == "CfgErrorCode":
            str_id = f"error_{key_val}"
        else:
            str_id = f"{cur_output_name}_{field_name}_{key_val}"
        return str_id, str_val

    @staticmethod
    def server_type_name():
        return "I18NString"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "I18NString?"
        return "I18NString"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        str_id = v[0]
        # str_val = super().value_to_client_str(v[1])
        return f"new I18NString(\"{str_id}\")"

    # @staticmethod
    # def all_res_names():
    #     return reversed_dict.keys()

    def has_depend(self):
        return False


i18n2Regex = re.compile(r"_*[Jj][Pp]$", re.IGNORECASE)

class I18n2(DefaultStr):
    def __init__(self, can_null, depend_tab):
        super().__init__(can_null, depend_tab)

    def convert_value(self, s, key_val=None, field_name=None):
        if self.canNull and s is None:
            return None
        global cur_output_name
        str_val = super().convert_value(s, key_val, field_name)
        if cur_output_name == "WsErrorCodeMsgConfig" or cur_output_name == "CfgErrorCode":
            str_id = f"error_{key_val}"
        else:
            field_name = re.sub(i18n2Regex, '', field_name)
            str_id = f"{cur_output_name}_{field_name}_{key_val}"
        return str_id, str_val

    @staticmethod
    def server_type_name():
        return "I18NString"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "I18NString?"
        return "I18NString"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        str_id = v[0]
        # str_val = super().value_to_client_str(v[1])
        return f"new I18NString(\"{str_id}\")"

    # @staticmethod
    # def all_res_names():
    #     return reversed_dict.keys()

    def has_depend(self):
        return False


effect_dict: OrderedDict[str, int] = OrderedDict()  # key:特效文件合法的值， value:无意义
effect_imported = False


def check_import_effect_dict():
    global effect_imported
    global effect_dict
    if not effect_imported:
        effect_imported = True
        # 如果没有 gen.ResEffectMap 这个模块的话:
        #   1. 先根据资源 生成 gen.ResEffectMap
        #   2. 不检查资源的话，可以直接运行 MainDummyRes.py
        mod = importlib.import_module("gen.ResEffectMap")
        effect_dict = mod.ResEffectMap


class EffectString(DefaultStr):
    def __init__(self, can_null, depend_tab):
        super().__init__(can_null, depend_tab)

    def has_depend(self):
        return False

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "EffString?"
        return "EffString"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        str_val = super().value_to_client_str(v[0])
        return f"new EffString({str_val})"

    @staticmethod
    def all_res_names():
        check_import_effect_dict()
        return effect_dict.keys()

    @staticmethod
    def check_res_exist(val):
        check_import_effect_dict()
        return val in effect_dict


sound_dict: OrderedDict[str, bool] = OrderedDict()  # key:声音文件合法的值， value:无意义
sound_imported = False


def check_import_sound_dict():
    global sound_imported
    global sound_dict
    if not sound_imported:
        sound_imported = True
        # 如果没有 gen.ResSoundMap 这个模块的话:
        #   1. 先根据资源 生成 gen.ResSoundMap
        #   2. 不检查资源的话，可以直接运行 MainDummyRes.py
        mod = importlib.import_module("gen.ResSoundMap")
        sound_dict = mod.ResSoundMap


class SoundString(DefaultStr):
    def __init__(self, can_null, depend_tab):
        super().__init__(can_null, depend_tab)

    def has_depend(self):
        return False

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "SoundString?"
        return "SoundString"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        str_val = super().value_to_client_str(v[0])
        return f"new SoundString({str_val})"

    @staticmethod
    def all_res_names():
        check_import_sound_dict()
        return sound_dict.keys()

    @staticmethod
    def check_res_exist(val):
        check_import_sound_dict()
        return val in sound_dict


picture_dict: OrderedDict[str, bool] = OrderedDict()  # key:声音文件合法的值， value:无意义
picture_imported = False


def check_import_picture_dict():
    global picture_imported
    global picture_dict
    if not picture_imported:
        picture_imported = True
        # 如果没有 gen.ResPictureMap 这个模块的话:
        #   1. 先根据资源 生成 gen.ResPictureMap
        #   2. 不检查资源的话，可以直接运行 MainDummyRes.py
        mod = importlib.import_module("gen.ResPictureMap")
        picture_dict = mod.ResPictureMap


class PictureString(DefaultStr):
    def __init__(self, can_null, depend_tab):
        super().__init__(can_null, depend_tab)

    def has_depend(self):
        return False

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "PictureString?"
        return "PictureString"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        str_val = super().value_to_client_str(v)
        return f"new PictureString({str_val})"

    @staticmethod
    def all_res_names():
        check_import_picture_dict()
        return picture_dict.keys()

    @staticmethod
    def check_res_exist(val):
        check_import_picture_dict()
        return val in picture_dict


class PrefabString(DefaultStr):
    def __init__(self, can_null, depend_tab):
        super().__init__(can_null, depend_tab)

    def has_depend(self):
        return False

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return "PrefabString?"
        return "PrefabString"

    def value_to_client_str(self, v):
        if self.canNull and v is None:
            return "null"
        str_val = super().value_to_client_str(v)
        return f"new PrefabString({str_val})"

    @staticmethod
    def all_res_names():
        return []

    @staticmethod
    def check_res_exist(val):
        # check_import_picture_dict()
        # return val in picture_dict
        return True


class UserDefineEnum(UserDefineType):
    def __init__(self, t_name, depend_tab, is_define=False):
        self.tName = t_name
        self.canNull = False
        if depend_tab is not None:
            raise Exception("目前枚举配置项，不支持依赖定义")
        self.dependTab = depend_tab
        self.isDefine = is_define

    def server_type_name(self):
        return self.tName

    def client_type_name(self, as_left_type=False):
        return self.tName

    def convert_value(self, s, key_val=None, field_name=None):
        return f"{self.tName}.{s}"

    @staticmethod
    def value_to_client_str(v):
        return f"{v}"

    @staticmethod
    def has_depend():
        return False


class UserDefineList(UserDefineType):
    def __init__(self, t_name, can_null, depend_tab):
        self.tName = t_name
        self.canNull = can_null
        self.dependTab = depend_tab
        self.eleType = find_type(t_name)

    def server_type_name(self):
        return f"List<{self.eleType.server_type_name()}>"

    def client_type_name(self, as_left_type=False):
        if as_left_type:
            if self.canNull:
                return f"IReadOnlyList<{self.eleType.client_type_name()}>?"
            else:
                return f"IReadOnlyList<{self.eleType.client_type_name()}>"
        return f"List<{self.eleType.client_type_name()}>"

    def convert_value(self, s, key_val=None, field_name=None):
        if s is None or s == '':
            if self.canNull:
                return None
            else:
                return []
        if type(s) == "str":
            s = s.strip()
        str_ls = str(s).split(" ")  # 列表元素以空格隔开
        val_ls = [self.eleType.convert_value(v_str, key_val) for v_str in str_ls]
        return val_ls

    def value_to_client_str(self, val_ls):
        if self.canNull and val_ls is None:
            return "null"
        val_str_ls = [self.eleType.value_to_client_str(val) for val in val_ls]
        val_str = ",".join(val_str_ls)
        return f"new {self.client_type_name()}{{{val_str}}}"

    def has_depend(self):
        return self.dependTab is not None or self.eleType.has_depend()


class UserDefineArray(UserDefineType):
    def __init__(self, t_name, can_null, depend_tab):
        self.tName = t_name
        self.canNull = can_null
        self.dependTab = depend_tab
        self.eleType = find_type(t_name)

    def server_type_name(self):
        return f"List<{self.eleType.server_type_name()}>"

    def client_type_name(self, as_left_type=False):
        if as_left_type:
            if self.canNull:
                return f"IReadOnlyList<{self.eleType.client_type_name()}>?"
            else:
                return f"IReadOnlyList<{self.eleType.client_type_name()}>"
        return f"List<{self.eleType.client_type_name()}>"

    def convert_value(self, s, key_val=None, field_name=None):
        if (s is None) or s == '':
            if self.canNull:
                return None
            else:
                return []
        str_ls = str(s).split("|")  # 列表元素以空格隔开
        val_ls = [self.eleType.convert_value(v_str, key_val) for v_str in str_ls]
        return val_ls

    def value_to_client_str(self, val_ls):
        if self.canNull and val_ls is None:
            return "null"
        val_str_ls = [self.eleType.value_to_client_str(val) for val in val_ls]
        val_str = ",".join(val_str_ls)
        return f"new {self.client_type_name()}{{{val_str}}}"

    def has_depend(self):
        return self.dependTab is not None or self.eleType.has_depend()


class UserDefineDict(UserDefineType):
    def __init__(self, k_type, v_type, can_null, depend_tab):
        self.kType = k_type
        self.vType = v_type
        self.canNull = can_null
        self.dependTab = depend_tab  # 定义为key的depend！
        self.keyType = find_type(k_type)
        self.eleType = find_type(v_type)

    def server_type_name(self):
        key_name = self.keyType.server_type_name()
        val_name = self.eleType.server_type_name()
        return f"Dictionary<{key_name}, {val_name}>"

    def client_type_name(self, as_left_type=False):
        key_name = self.keyType.client_type_name()
        val_name = self.eleType.client_type_name()
        if as_left_type:
            if self.canNull:
                return f"IReadOnlyDictionary<{key_name}, {val_name}>?"
            else:
                return f"IReadOnlyDictionary<{key_name}, {val_name}>?"
        return f"Dictionary<{key_name}, {val_name}>"

    def convert_value(self, s, key_val=None, field_name=None):
        d = OrderedDict()
        if s is None or s == '':
            if self.canNull:
                return None
            else:
                return d
        str_ls = s.split(" ")  # 列表元素以空格隔开
        for kv_str in str_ls:
            kv = kv_str.split(":")
            if len(kv) != 2:
                raise Exception(f"字典元素{s} 不符合字典格式要求")
            key_str = kv[0]
            value_str = kv[1]
            key_name = self.keyType.convert_value(key_str, key_val, field_name)
            val_name = self.eleType.convert_value(value_str, key_val, field_name)
            d[key_name] = val_name
        return d

    def value_to_client_str(self, d: OrderedDict):
        if self.canNull and d is None:
            return "null"
        kv_str_ls = []
        for k, v in d.items():
            key_str = self.keyType.value_to_client_str(k)
            value_str = self.eleType.value_to_client_str(v)
            kv_str = f"{{{key_str}, {value_str}}}"
            kv_str_ls.append(kv_str)
        key_name = self.keyType.client_type_name()
        val_name = self.eleType.client_type_name()
        all_kv_str = ",".join(kv_str_ls)
        return f"new Dictionary<{key_name}, {val_name}>{{{all_kv_str}}}"

    def has_depend(self):
        return self.dependTab is not None \
               or self.keyType.has_depend() \
               or self.eleType.has_depend()


class UserDefineTuple(UserDefineType):
    def __init__(self, ele_types_str, can_null, depend_tab):
        ele_type_str_ls = re.split(r"\s*,\s*", ele_types_str)
        self.canNull = can_null
        self.dependTab = depend_tab
        self.eleTypeLs = []
        ele_type_str_ls2 = []
        for type_str in ele_type_str_ls:
            sub_type = find_type(type_str)
            self.eleTypeLs.append(sub_type)
            ele_type_str_ls2.append(sub_type.client_type_name(True))
        if len(self.eleTypeLs) == 1:
            raise Exception(f"声明了tuple，但是类型只有一项")
        # 辅助变量
        self.eleLen = len(self.eleTypeLs)
        self.eleTypeStr = ",".join(ele_type_str_ls2)

    def convert_value(self, s, key_val=None, field_name=None):
        if (s is None or s == '' ) and self.canNull:
            return None
        s_ls = re.split(listSplitStr, s)
        if len(s_ls) != self.eleLen:
            raise Exception(f"值的个数不符合tuple<{self.eleTypeStr}>的格式")
        v_ls = []
        for eleType, s_str in zip(self.eleTypeLs, s_ls):
            v_ls.append(eleType.convert_value(s_str, key_val, field_name))
        return tuple(v_ls)

    def value_to_client_str(self, v_tup):
        if self.canNull and v_tup is None:
            return "null"
        v_str_ls = []
        for eleType, v_val in zip(self.eleTypeLs, list(v_tup)):
            v_str_ls.append(eleType.value_to_client_str(v_val))
        v_tup_str = ",".join(v_str_ls)
        return f"new Tuple<{self.eleTypeStr}>({v_tup_str})"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"Tuple<{self.eleTypeStr}>?"
        return f"Tuple<{self.eleTypeStr}>"

    def server_type_name(self):
        return f"Tuple<{self.eleTypeStr}>"

    def has_depend(self):
        return self.dependTab is not None or any(x.has_depend() for x in self.eleTypeLs)


class IDCount(UserDefineType):
    def __init__(self, can_null, depend_tab):
        if depend_tab is not None:
            raise Exception(f"IDCount 类型无需额外定义依赖:{depend_tab}")
        self.canNull = can_null
        self.dependTab = "CfgItem"
        self.tupleType = UserDefineTuple("int,int", self.canNull, None)

    def convert_value(self, s, key_val=None, field_name=None):
        return self.tupleType.convert_value(s, key_val, field_name)

    def value_to_client_str(self, tup_value):
        if self.canNull and tup_value is None:
            return "null"
        return f"new IDCount({tup_value[0]}, {tup_value[1]})"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"IDCount?"
        return f"IDCount"

    def server_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"IDCount?"
        return f"IDCount"

    @staticmethod
    def has_depend():
        return True


class IntVector3(UserDefineType):
    def __init__(self, can_null, depend_tab):
        self.canNull = can_null
        self.dependTab = depend_tab
        self.tupleType = UserDefineTuple("number,number,number", self.canNull, None)

    def convert_value(self, s, key_val=None, field_name=None):
        return self.tupleType.convert_value(s, key_val, field_name)

    def value_to_client_str(self, tup_value):
        if self.canNull and tup_value is None:
            return "null"
        return f"new IntVector3({tup_value[0]}, {tup_value[1]}, {tup_value[2]})"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"IntVector3?"
        return f"IntVector3"

    def server_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"IntVector3?"
        return f"IntVector3"

    @staticmethod
    def has_depend():
        return False


class Vector3(UserDefineType):
    def __init__(self, can_null, depend_tab):
        self.canNull = can_null
        self.dependTab = depend_tab
        self.tupleType = UserDefineTuple("float,float,float", self.canNull, None)

    def convert_value(self, s, key_val=None, field_name=None):
        if s is None or s == '':
            return (0.0, 0.0, 0.0)
        return self.tupleType.convert_value(s, key_val, field_name)

    def value_to_client_str(self, tup_value):
        if tup_value is None:
            val_str0 = self.tupleType.eleTypeLs[0].value_to_client_str(0)
            val_str1 = self.tupleType.eleTypeLs[1].value_to_client_str(0)
            val_str2 = self.tupleType.eleTypeLs[2].value_to_client_str(0)
            return f"new Vector3({val_str0}, {val_str1}, {val_str2})"
        val_str0 = self.tupleType.eleTypeLs[0].value_to_client_str(tup_value[0])
        val_str1 = self.tupleType.eleTypeLs[1].value_to_client_str(tup_value[1])
        val_str2 = self.tupleType.eleTypeLs[2].value_to_client_str(tup_value[2])
        return f"new Vector3({val_str0}, {val_str1}, {val_str2})"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"Vector3?"
        return f"Vector3"

    def server_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"Vector3?"
        return f"Vector3"

    @staticmethod
    def has_depend():
        return False


class IntVector2(UserDefineType):
    def __init__(self, can_null, depend_tab):
        self.canNull = can_null
        self.dependTab = depend_tab
        self.tupleType = UserDefineTuple("number,number", self.canNull, None)

    def convert_value(self, s, key_val=None, field_name=None):
        return self.tupleType.convert_value(s, key_val, field_name)

    def value_to_client_str(self, tup_value):
        if self.canNull and tup_value is None:
            return "null"
        return f"new IntVector2({tup_value[0]}, {tup_value[1]})"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"IntVector2?"
        return f"IntVector2"

    def server_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"IntVector2?"
        return f"IntVector2"

    @staticmethod
    def has_depend():
        return False


class Vector2(UserDefineType):
    def __init__(self, can_null, depend_tab):
        self.canNull = can_null
        self.dependTab = depend_tab
        self.tupleType = UserDefineTuple("float,float", self.canNull, None)

    def convert_value(self, s, key_val=None, field_name=None):
        return self.tupleType.convert_value(s, key_val, field_name)

    def value_to_client_str(self, tup_value):
        if self.canNull and tup_value is None:
            return "null"
        val_str0 = self.tupleType.eleTypeLs[0].value_to_client_str(tup_value[0])
        val_str1 = self.tupleType.eleTypeLs[1].value_to_client_str(tup_value[1])
        return f"new Vector2({val_str0}, {val_str1})"

    def client_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"Vector2?"
        return f"Vector2"

    def server_type_name(self, as_left_type=False):
        if self.canNull and as_left_type:
            return f"Vector2?"
        return f"Vector2"

    @staticmethod
    def has_depend():
        return False


TypeMap = {
    "string": DefaultStr,
    "long": DefaultLong,
    "number": DefaultInt,
    "float": DefaultFloat,
    "int": DefaultInt,
    "bool": DefaultBool,
    "I18n2": I18n2,
    "i18n2": I18n2,
    "I18N2": I18n2,
    "I18n": I18n,
    "i18n": I18n,
    "I18N": I18n,
    "i18nString": I18n,
    "I18nString": I18n,
    "I18NString": I18n,
    "IDCount": IDCount,
    "EffectString": EffectString,
    "effectString": EffectString,
    "SoundString": SoundString,
    "soundString": SoundString,
    "PictureString": PictureString,
    "pictureString": PictureString,
    "PrefabString": PrefabString,
    "IntVector3": IntVector3,
    "Vector3": Vector3,
    "IntVector2": IntVector2,
    "Vector2": Vector2,
}


def find_type(field_type: str):
    if field_type.find("?") != -1:
        can_null = True
    else:
        can_null = False
    field_type2 = field_type.replace("?", "")
    return find_type2(field_type2, can_null)


def find_type2(field_t: str, can_null):
    field_t = field_t.strip(" ").replace(" ", "")
    mo = bracketPat.match(field_t)
    if mo is not None:
        depend_tab = mo.group(2)
        field_type = mo.group(1)
        return find_type3(field_type, can_null, depend_tab)
    else:
        return find_type3(field_t, can_null, None)


def find_type3(s: str, can_null=False, depend_tab=None):
    if listPat.match(s) is not None:
        ele_type = listPat.match(s).group(1)
        return UserDefineList(ele_type, can_null, depend_tab)
    elif enumDefPat.match(s) is not None:
        if can_null:
            raise Exception("枚举类型不支持为can_null类型")
        ele_type = enumDefPat.match(s).group(1)
        return UserDefineEnum(ele_type, depend_tab, True)
    elif enumPat.match(s) is not None:
        if can_null:
            raise Exception("枚举类型不支持为can_null类型")
        ele_type = enumPat.match(s).group(1)
        return UserDefineEnum(ele_type, depend_tab)
    elif dictPat.match(s) is not None:
        dict_mo = dictPat.match(s)
        dict_k_type = dict_mo.group(1)
        dict_v_type = dict_mo.group(2)
        return UserDefineDict(dict_k_type, dict_v_type, can_null, depend_tab)
    elif tuplePat.match(s) is not None:
        mo = tuplePat.match(s)
        ele_types_str = mo.group(1)
        return UserDefineTuple(ele_types_str, can_null, depend_tab)
    elif arrayPat.match(s) is not None:
        mo = arrayPat.match(s)
        ele_types_str = mo.group(1)
        return UserDefineArray(ele_types_str, can_null, depend_tab)
    elif s in TypeMap:
        t = TypeMap[s]
        return t(can_null, depend_tab)
    else:
        raise Exception(f"非法的类型:{s}")
