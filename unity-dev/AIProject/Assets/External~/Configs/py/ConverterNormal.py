import json
import os
import sys
from string import Template
from typing import List, Optional
from collections import OrderedDict

from openpyxl.workbook import Workbook
from openpyxl.worksheet.worksheet import Worksheet

from ConfigSide import ConfigSide
from FieldInfo import FieldInfo
from UserDefineTypes import I18n, I18n2
from UtilForConfig import index_to_col_name, AppendableDict
from WorksheetCache import WorksheetCache


class ConverterNormal:
    def __init__(self, wb, sheet, sheet_cache, wb_name, sheet_name, output_name, type_name, is_export, is_write):
        self.workbook: Workbook = wb
        self.sheet: Worksheet = sheet
        self.wb_name: str = wb_name
        self.sheet_name: str = sheet_name
        self.output_name = output_name
        self.type_name = type_name
        self.table: dict = {}
        self.side: ConfigSide = ConfigSide.BOTH
        self.field_ls: List[FieldInfo] = []
        self.data_map: OrderedDict = AppendableDict()
        self.is_export = is_export
        self.sheet_cache: WorksheetCache = sheet_cache
        self.is_write = is_write

    def read_sheet(self):
        self._read_all_field_defines()
        self._read_all_datas()

    def close_wb(self):
        self.workbook.close()

    def name(self):
        return f"{self.wb_name}[{self.sheet_name}]"

    def _read_all_field_defines(self):
        sheet = self.sheet
        column_count = sheet.max_column
        # print("column_count", column_count)
        exist_fields = {}
        for i in range(2, column_count + 1):  # 从第二列开始是数据
            try:
                field_key_type: Optional[str] = self.sheet_cache.cell(2, i)  # 导出目标/是否为key
                if field_key_type is not None and field_key_type.lower() == "none":
                    continue
                field_name = self.sheet_cache.cell(4, i)  # 字段名字
                if field_name is None:
                    continue
                field_type = self.sheet_cache.cell(5, i)  # 字段类型
                if field_type is None:
                    continue
                field_comment = self.sheet_cache.cell(6, i)  # 配置备注
                if field_comment is None or type(field_comment) != str:
                    field_comment = ""
                field_comment = field_comment.replace("\n", "")
                field = FieldInfo(i, self.type_name, field_key_type, field_type, field_name, field_comment)
                if field.fieldName in exist_fields:
                    raise Exception(f"存在同名的字段:{field.fieldName}")
                exist_fields[field.fieldName] = True
                self.field_ls.append(field)
            except Exception as e:
                exc_s = f"{self.name()} 的第 {index_to_col_name(i)} 列出错! {e.__str__()}!\n"
                sys.stderr.write(exc_s)
                raise Exception(exc_s)

    def _read_all_datas(self):
        sheet = self.sheet
        row_count = sheet.max_row
        r = 7
        cur_map = self.data_map
        c = 0
        try:
            while r <= row_count:
                start_field = self.field_ls[0]
                c = start_field.sheetIdx
                key_val = self.sheet_cache.cell(r, c)
                if key_val is None:
                    r = r + 1
                    continue
                d = OrderedDict()
                d["__class_name"] = self.type_name
                d["__sheet_name"] = self.name()
                d["__cell_row"] = r
                d[start_field.fieldName] = start_field.convert_value(key_val) #ID

                for field in self.field_ls:
                    c = field.sheetIdx
                    if c == start_field.sheetIdx:
                        continue
                    value = self.sheet_cache.cell(r, c)
                    field_name = field.fieldName
                    d[field_name] = field.convert_value(value, key_val, field_name)
                if key_val in cur_map:
                    raise Exception(f" {key_val} 是重复的key")
                cur_map[key_val] = d
                r = r + 1
        except Exception as e:
            exc_s = f"{self.name()}的第{r}行第{index_to_col_name(c)}列出错! {e.__str__()}!\n"
            sys.stderr.write(exc_s)
            raise Exception(exc_s)

    def write_type_code(self, client_type_dir):
        st = Template('''using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PartialTypeWithSinglePart
// 对应的Excel表格 $wb_name
#nullable enable
namespace GameLogic {
\tpublic partial class $class_name
\t{
$fields

\t\tpublic $class_name($params)
\t\t{
$assignment
\t\t}
\t\tpublic override string ToString()
\t\t{
\t\t\treturn $$"$class_name($field_names)";
\t\t}
\t}
}
''')
        fields = []  # 类中定义的字段
        params = []  # 构造函数的参数
        assign_ls = []  # 够着函数赋值语句
        to_string_ls = []  # ToString
        indexer_method_str = ""
        for i, field in enumerate(self.field_ls):
            if isinstance(field.defType, I18n2):
                continue
            field_name = field.fieldName
            field_type = field.client_type_name(True)
            p_str = f"{field_type} {field_name}"
            params.append(p_str)
            fields.append(f"\t\tpublic readonly {field_type} {field_name}; // {field.fieldCommentName}")
            assign_ls.append(f"\t\t\tthis.{field_name} = {field_name};")
            to_string_ls.append(f"{field_name}={{{field_name}}}")
        fields_str = "\n".join(fields)
        params_str = ",\n\t\t\t".join(params)
        field_names_str = ",".join(to_string_ls)
        assignment_str = "\n".join(assign_ls)
        s = st.substitute(class_name=self.type_name,
                          fields=fields_str,
                          params=params_str,
                          assignment=assignment_str,
                          field_names=field_names_str,
                          indexer_method=indexer_method_str,
                          wb_name=self.name(),
                          )
        file_name = os.path.join(client_type_dir, f"{self.type_name}.cs")
        with open(file_name, "w", encoding='utf-8', newline='\n') as f:
            f.write(s)

    def write_data_code(self, client_data_dir):
        file_name = os.path.join(client_data_dir, f"{self.output_name}.cs")
        with open(file_name, "w", encoding='utf-8', newline='\n') as f:
            s = self._calc_type_str()
            f.write(s)

    def _calc_type_str(self):
        file_st = Template('''using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PartialTypeWithSinglePart
// 对应的Excel表格 $wb_name
#nullable enable
namespace GameLogic {
\tpublic partial class $tab_name
\t{
\t\tpublic static $table_type Data = $data_values;
\t}
}
''')
        start_field = self.field_ls[0]
        key_data_type = start_field.defType.client_type_name(False)
        value_data_type = self.type_name
        if self.is_write:
            table_type_str = f"Dictionary<{key_data_type}, {value_data_type}>"
        else:
            table_type_str = f"readonly IReadOnlyDictionary<{key_data_type}, {value_data_type}>"
        data_values_str = self.calc_client_data_dict_code(key_data_type, value_data_type, 2)
        return file_st.substitute(tab_name=self.output_name,
                                  table_type=table_type_str,
                                  wb_name=self.name(),
                                  data_values=data_values_str)

    def calc_client_data_dict_code(self, key_data_type, value_base_type, tab_num):
        tab_str = "\t" * tab_num
        dict_st = Template('''new Dictionary<$key_data_type, $value_data_type>()
$tab_str{
$all_datas
$tab_str}''')
        all_datas = []
        start_field = self.field_ls[0]
        for key, value in self.data_map.items():
            value_data_type = value["__class_name"]
            key_str = start_field.value_to_client_str(key)
            data_fields = []
            for i, field in enumerate(self.field_ls):
                if isinstance(field.defType, I18n2):
                    continue
                val = value[field.fieldName]
                data_str = f"{field.defType.value_to_client_str(val)}"
                data_fields.append(data_str)
            tab_strEx = "\t" * (tab_num + 2)
            data_fields_str = f",\n{tab_strEx}".join(data_fields)
            data_str = f"\t{tab_str}{{{key_str},\n {tab_strEx}new {value_data_type}({data_fields_str})}},"
            all_datas.append(data_str)
        all_datas_str = "\n".join(all_datas)
        return dict_st.substitute(key_data_type=key_data_type,
                                  value_data_type=value_base_type,
                                  all_datas=all_datas_str,
                                  tab_str=tab_str)

    def write_json_code(self, client_type_dir):
        file_name = os.path.join(client_type_dir, f"{self.type_name}.json")
        data_map = {}
        for key, value in self.data_map.items():
            data = {}
            for i, field in enumerate(self.field_ls):
                val = value[field.fieldName]
                if isinstance(field.defType, I18n2):
                    continue
                if isinstance(field.defType, I18n):
                    if val is None:
                        data[field.fieldName] = ""
                        data[f"{field.fieldName}_i18n"] = ""
                    else:
                        str_id = val[0]
                        str_val = val[1]
                        data[field.fieldName] = str_val
                        data[f"{field.fieldName}_i18n"] = str_id
                else:
                    data[field.fieldName] = val
            data_map[key] = data
        json_object = json.dumps(data_map, indent=4, ensure_ascii=False)
        # Writing to sample.json
        with open(file_name, "w", encoding='utf-8', newline='\n') as outfile:
            outfile.write(json_object)

    def update_i18n_map(self, trans_map):
        for key, value in self.data_map.items():
            for i, field in enumerate(self.field_ls):
                val = value[field.fieldName]
                if isinstance(field.defType, I18n):
                    if val is not None:
                        str_id = val[0]
                        str_val = val[1]
                        trans_map[str_id] = str_val
        if self.output_name == "CfgErrorCode":
            for key, value in self.data_map.items():
                val = value["content"]
                if val is not None:
                    trans_map[f"error_{key}"] = val[1]

    def update_i18n2_map(self, trans_map):
        for key, value in self.data_map.items():
            for i, field in enumerate(self.field_ls):
                val = value[field.fieldName]
                if isinstance(field.defType, I18n2):
                    if val is not None:
                        str_id = val[0]
                        str_val = val[1]
                        trans_map[str_id] = str_val
        if self.output_name == "CfgErrorCode":
            for key, value in self.data_map.items():
                val = value["content2"]
                if val is not None:
                    trans_map[f"error_{key}"] = val[1]
