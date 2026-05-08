import json
import os.path
import sys
from string import Template

from openpyxl.workbook import Workbook
from openpyxl.worksheet.worksheet import Worksheet

from ConfigSide import ConfigSide
from FieldInfo import FieldInfo
from UserDefineTypes import I18n, I18n2
from WorksheetCache import WorksheetCache


class ConverterConst:
    def __init__(self, wb, sheet, sheet_cache, wb_name, sheet_name, output_name, type_name, is_export, is_write):
        self.workbook: Workbook = wb
        self.sheet: Worksheet = sheet
        self.wb_name: str = wb_name
        self.sheet_name: str = sheet_name
        self.output_name = output_name
        self.type_name = type_name
        self.table: dict = {}
        self.side: ConfigSide = ConfigSide.BOTH
        self.sheet_cache: WorksheetCache = sheet_cache
        self.data_ls = []
        self.is_export = is_export
        self.is_write = is_write

    def name(self):
        return f"{self.wb_name}[{self.sheet_name}]"

    def close_wb(self):
        self.workbook.close()

    def read_sheet(self):
        sheet = self.sheet
        max_row = sheet.max_row
        for i in range(5, max_row + 1):
            try:
                field_name = self.sheet_cache.cell(i, 1)  # 字段名字
                if field_name is None:
                    continue
                field_type = self.sheet_cache.cell(i, 3)  # 字段类型
                field_comment = self.sheet_cache.cell(i, 2) or ""  # 字段类型
                field = FieldInfo(i, self.type_name, "both", field_type, field_name, field_comment)
                raw_value = self.sheet_cache.cell(i, 4)
                data = (field, field.convert_value(raw_value))
                self.data_ls.append(data)
            except Exception as e:
                exc_s = f"{self.name()} 的第 {i}行出错! {e.__str__()}!\n"
                sys.stderr.write(exc_s)
                raise Exception(exc_s)

    def write_type_code(self, client_type_dir):
        pass

    def write_data_code(self, client_data_dir):
        file_name = os.path.join(client_data_dir, f"{self.output_name}.cs")
        with open(file_name, "w", encoding='utf-8', newline='\n') as f:
            s = self._calc_data_str()
            f.write(s)

    def _calc_data_str(self):
        file_st = Template('''using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PartialTypeWithSinglePart
#nullable enable
namespace GameRuntime {
\tpublic static class $tab_name
\t{
$const_defines
\t}
}
''')
        const_defines = []
        for field, value in self.data_ls:
            if isinstance(field.defType, I18n2):
                continue
            type_str = field.client_type_name(True)
            val_str = field.value_to_client_str(value)
            comment_str = field.fieldCommentName.replace("\n", "")
            def_str = f"\t\tpublic static {type_str} {field.fieldName} = {val_str}; // {comment_str}"
            const_defines.append(def_str)
        const_defines_str = "\n".join(const_defines)
        return file_st.substitute(tab_name=self.output_name, const_defines=const_defines_str)

    def write_json_code(self, client_type_dir):
        file_name = os.path.join(client_type_dir, f"{self.type_name}.json")
        const_tab = {}
        for field, value in self.data_ls:
            if isinstance(field.defType, I18n2):
                continue
            const_tab[field.fieldName] = value
        d = {"const": const_tab}
        # Serializing json
        json_object = json.dumps(d, indent=4, ensure_ascii=False)

        # Writing to sample.json
        with open(file_name, "w", encoding='utf-8', newline='\n') as outfile:
            outfile.write(json_object)

    def update_i18n_map(self, trans_map):
        for field, value in self.data_ls:
            if isinstance(field.defType, I18n):
                if value is not None:
                    str_id = value[0]
                    str_val = value[1]
                    trans_map[str_id] = str_val

    def update_i18n2_map(self, trans_map):
        for field, value in self.data_ls:
            if isinstance(field.defType, I18n2):
                if value is not None:
                    str_id = value[0]
                    str_val = value[1]
                    trans_map[str_id] = str_val
