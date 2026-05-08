from typing import Optional

from ConfigSide import ConfigSide
from KeyType import KeyType
from UserDefineTypes import find_type, UserDefineEnum, UserDefineList
from UserDefineTypes import UserDefineDict, UserDefineTuple, DefaultStr, DefaultLong, DefaultFloat, DefaultInt, DefaultBool, UserDefineArray
from UserDefineTypes import Vector3
from UtilForConfig import index_to_col_name

legalKeyType = {
    "client": True,
    "server": True,
    "both": True,
    "key": True,
    "arr": True,
}


class FieldInfo:
    def __init__(self,
                 sheet_idx: int,
                 field_class_str: Optional[str],
                 key_type: Optional[str],
                 field_t: str,
                 field_n,
                 field_comment: str,
                 ):
        self.sheetIdx = sheet_idx  # 该字段在sheet的第几列 / 在Const中的第几行
        self.fieldClass = field_class_str  # 类名
        if key_type == "id":
            key_type = "key"
        if key_type not in legalKeyType and key_type != "" and key_type is not None:
            raise Exception(f"非法的key类型:{key_type}")
        if field_t is None:
            raise Exception(f"字段类型不能为空")
        if key_type == "client":
            self.side = ConfigSide.CLIENT
            self.keyType = KeyType.NONE
        elif key_type == "server":
            self.side = ConfigSide.SERVER
            self.keyType = KeyType.NONE
        else:
            self.side = ConfigSide.BOTH
            if key_type == "arr":
                if field_t.find("?") != -1:
                    raise Exception(f"作为arr的列，类型不支持为可null")
                self.keyType = KeyType.ARR
            elif key_type == "key":
                if field_t.find("?") != -1:
                    raise Exception(f"作为key的列，类型不支持为可null")
                self.keyType = KeyType.KEY
            else:
                self.keyType = KeyType.NONE
        self.fieldName: str = field_n  # 字段名
        self.fieldCommentName: str = field_comment  # 字段中文名
        self.defType = find_type(field_t)  # 字段类型
        self.fieldType = field_t  # 字段类型文本（主要用于调试）
        if self.keyType == KeyType.KEY or self.keyType == KeyType.ARR:
            if isinstance(self.defType, UserDefineEnum) and self.defType.isDefine:
                raise Exception(f"枚举的定义不能作为key/arr的第一列{self.defType}")

    def __str__(self):
        key_type_str = self.keyType if self.keyType is not None else "None"
        class_str = self.fieldClass if self.fieldClass is not None else "None"
        return f"(class:{class_str}, name:{self.fieldName},type:{self.fieldType},keyType:{key_type_str},{self.side})"

    def __repr__(self):
        return self.__str__()

    def __eq__(self, other):
        return self.fieldName == other.fieldName \
            and self.fieldType == other.fieldType \
            and self.keyType == other.keyType \
            and self.side == other.side

    def convert_value(self, value, key_val=None, field_name=None):
        if value is None:
            if self.defType.canNull:
                return self.defType.convert_value(value, key_val, field_name)
            elif isinstance(self.defType, UserDefineList) or \
                    isinstance(self.defType, UserDefineDict) or \
                    isinstance(self.defType, UserDefineArray) or \
                    isinstance(self.defType, UserDefineTuple) or \
                    isinstance(self.defType, Vector3):
                return self.defType.convert_value(value, key_val, field_name)
            elif isinstance(self.defType, DefaultStr) or \
                    isinstance(self.defType, DefaultInt) or \
                    isinstance(self.defType, DefaultFloat) or \
                    isinstance(self.defType, DefaultBool):
                return self.defType.convert_value(value, key_val, field_name)
            else:
                raise Exception(f"{self.fieldName}字段类型为非null且值为空，请改字段类型或填入配置值")
        return self.defType.convert_value(value, key_val, field_name)

    def client_type_name(self, as_left_type=False):
        return self.defType.client_type_name(as_left_type)

    def value_to_client_str(self, v):
        return self.defType.value_to_client_str(v)

    def get_class_name(self) -> str:
        """
        输出字段类型字符串（仅用于辅助debug）
        :return: str
        """
        return self.fieldClass

    def is_enum_define_field(self):
        return isinstance(self.defType, UserDefineEnum) and self.defType.isDefine

    def get_col_name(self):
        return index_to_col_name(self.sheetIdx)

    def has_depend(self):
        return self.defType.has_depend()

    def has_depend_method(self):
        def_type = self.defType
        if isinstance(def_type, DefaultStr) \
                or isinstance(def_type, DefaultLong) \
                or isinstance(def_type, DefaultInt) \
                or isinstance(def_type, DefaultFloat) \
                or isinstance(def_type, UserDefineTuple):
            # 不要将下面一行替换成 def_type.has_depend()
            return def_type.dependTab is not None
        return False

    def check_res_exist(self, val):
        if val is None:
            return True
        return self.defType.check_res_exist(val)
