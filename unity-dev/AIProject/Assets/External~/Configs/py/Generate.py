import json
import sys
import os
import argparse
from typing import List

from openpyxl import load_workbook

from UtilForConfig import check_path_exists
from ConverterConst import ConverterConst
from ConverterNormal import ConverterNormal
from UserDefineTypes import set_output_name
from TimeUtil import get_time_consume
from TimeUtil import clear_time_consume
from WorksheetCache import WorksheetCache

#
MIN_COL = 3
MIN_ROW = 6
MIN_ROW_OF_CONST = 5


def all_data_to_client_code(conv_ls, type_dir, data_dir):
    for conv in conv_ls:
        conv.write_type_code(type_dir)
        conv.write_data_code(data_dir)


def all_data_to_client_json(conv_ls, j_dir):
    for conv in conv_ls:
        if conv.is_export:
            conv.write_json_code(j_dir)


def all_data_to_translate(conv_ls, t_dir):
    for conv in conv_ls:
        cn_trans_map = {}
        conv.update_i18n_map(cn_trans_map)
        if len(cn_trans_map) > 0:
            wb_name = conv.wb_name.replace(".xlsx", "")
            cn_file_name = os.path.join(t_dir, f"{wb_name}_cn.json")
            cn_json_object = json.dumps(cn_trans_map, indent=4, ensure_ascii=False)
            with open(cn_file_name, "w", encoding='utf-8', newline='\n') as outfile:
                outfile.write(cn_json_object)
        jp_trans_map = {}
        conv.update_i18n2_map(jp_trans_map)
        if len(jp_trans_map) > 0:
            wb_name = conv.wb_name.replace(".xlsx", "")
            jp_file_name = os.path.join(t_dir, f"{wb_name}_jp.json")
            jp_json_object = json.dumps(jp_trans_map, indent=4, ensure_ascii=False)
            with open(jp_file_name, "w", encoding='utf-8', newline='\n') as outfile:
                outfile.write(jp_json_object)


def merge_all_translate_files(files_dir, merge_file):
    cn_map = {}
    jp_map = {}
    cn_file_name_ls = []
    jp_file_name_ls = []
    for file_name in os.listdir(files_dir):
        if file_name.endswith("_cn.json"):
            cn_file_name_ls.append(file_name)
        elif file_name.endswith("_jp.json"):
            jp_file_name_ls.append(file_name)
    # 确保文件名的遍历顺序
    cn_file_name_ls.sort()
    jp_file_name_ls.sort()
    for file_name in cn_file_name_ls:
        file_name_path = os.path.join(files_dir, file_name)
        with open(file_name_path, "r", encoding='utf-8') as json_file:
            json_obj = json.load(json_file)
            for k, v in json_obj.items():
                cn_map[k] = v
    for file_name in jp_file_name_ls:
        file_name_path = os.path.join(files_dir, file_name)
        with open(file_name_path, "r", encoding='utf-8') as json_file:
            json_obj = json.load(json_file)
            for k, v in json_obj.items():
                jp_map[k] = v
    cn_merge_file = f"{merge_file}/config_string_zh.json"
    jp_merge_file = f"{merge_file}/config_string_ja.json"
    cn_merge_file2 = f"{merge_file}/../../../External~/configs/json/config_string_zh.json"
    jp_merge_file2 = f"{merge_file}/../../../External~/configs/json/config_string_ja.json"
    cn_json_object = json.dumps(cn_map, indent=4, ensure_ascii=False)
    jp_json_object = json.dumps(jp_map, indent=4, ensure_ascii=False)
    with open(cn_merge_file, "w", encoding='utf-8', newline='\n') as cn_json_all_file:
        cn_json_all_file.write(cn_json_object)

    with open(jp_merge_file, "w", encoding='utf-8', newline='\n') as jp_json_all_file:
        jp_json_all_file.write(jp_json_object)

    with open(cn_merge_file2, "w", encoding='utf-8', newline='\n') as cn_json_all_file:
        cn_json_all_file.write(cn_json_object)

    with open(jp_merge_file2, "w", encoding='utf-8', newline='\n') as jp_json_all_file:
        jp_json_all_file.write(jp_json_object)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('-e', '--excel_dir', type=str, required=True, help='Excel目录')
    parser.add_argument('-j', '--json_dir', type=str, required=False, help='输出的json目录')
    parser.add_argument('-c', '--client_dir', type=str, required=False, help='输出的代码目录')
    parser.add_argument('-t', '--trans_dir', type=str, required=False, help='输出的翻译文案的目录')
    parser.add_argument('-m', '--trans_merge_file', type=str, required=False, help='输出的代码目录')
    parser.add_argument('-n', '--name', type=str, required=True, help='Excel名字')
    group = parser.add_argument_group()
    group.add_argument('--generate_json', action='store_true')
    group.add_argument('--generate_client', action='store_true')
    group.add_argument('--generate_translate', action='store_true')
    group.add_argument('--generate_translate_merge', action='store_true')
    # parser.add_argument('--check_report', default=False, action=argparse.BooleanOptionalAction)
    args = parser.parse_args()

    excel_dir = args.excel_dir
    print("excel_dir", excel_dir)
    check_path_exists(excel_dir)

    generate_json = args.generate_json
    json_dir = args.json_dir
    if generate_json:
        check_path_exists(json_dir)

    trans_dir = args.trans_dir
    trans_merge_file = args.trans_merge_file
    generate_translate = args.generate_translate
    generate_translate_merge = args.generate_translate_merge

    generate_client = args.generate_client
    client_dir = args.client_dir
    if generate_client:
        check_path_exists(client_dir)

    name = args.name or "*"
    if name == 'INPUT_NAME':
        name = input("请输入表名：")

    converter_ls = []
    excel_ls = os.listdir(excel_dir)
    for excel_file in excel_ls:
        if excel_file.startswith("~$") or excel_file.startswith("."):  # 忽略Excel自动保存的文件
            continue
        if excel_file.split(".")[0] == name or excel_file == name or name == '*' or name == 'ALL':
            pass
        else:
            continue
        excel_file_path = os.path.join(excel_dir, excel_file)
        wb = load_workbook(excel_file_path, read_only=True, data_only=True)
        for sheet_name in wb.sheetnames:
            if sheet_name.startswith("_"):
                continue
            sheet = wb[sheet_name]
            sheet_cache = WorksheetCache(sheet)
            print(excel_file)
            print(sheet_name)
            if sheet.max_column < MIN_COL:  # 表头信息不足
                continue
            # 导出的表格名字
            output_name = sheet_cache.cell(1, 2)
            if output_name is None or type(output_name) != str:
                continue
            if output_name.find("Const") == -1:
                if sheet.max_row < MIN_ROW:  # 表头信息不足
                    continue
            else:
                if sheet.max_row < MIN_ROW_OF_CONST:  # 表头信息不足
                    continue
            print(f"{excel_file}[{sheet_name}] {sheet.max_column}列，{sheet.max_row}行")
            # 是否导出
            export_str = sheet_cache.cell(3, 2)
            can_write_str = sheet_cache.cell(3, 3) or ""
            if export_str is None:
                continue
            is_write = (can_write_str == "可写")
            is_export = export_str.find("是") >= 0
            if not is_export:
                continue
            # 自定义类型名
            type_str = sheet_cache.cell(1, 3)
            if type_str is None or  type_str == '':
                type_name = output_name
            else:
                type_name = type_str
            if output_name.endswith("Const"):
                converter = ConverterConst(wb, sheet, sheet_cache, excel_file,
                                           sheet_name, output_name, type_name, is_export, is_write)
            else:
                converter = ConverterNormal(wb, sheet, sheet_cache, excel_file,
                                            sheet_name, output_name, type_name, is_export, is_write)
            converter_ls.append(converter)

    convert_len = len(converter_ls)
    if convert_len == 0:
        raise Exception(f"找不到需要输出的配置表:{name}")
    print("=============================")
    clear_time_consume()
    for converter in converter_ls:
        set_output_name(converter.output_name)
        converter.read_sheet()
        print(f"{converter.name()} 输出表格为{converter.output_name} {str(get_time_consume())}ms")

    if generate_client:
        client_type_dir = os.path.join(client_dir, "DataTypes")
        client_data_dir = os.path.join(client_dir, "Datas")
        all_data_to_client_code(converter_ls, client_type_dir, client_data_dir)

    if generate_json:
        all_data_to_client_json(converter_ls, json_dir)

    if generate_translate:
        all_data_to_translate(converter_ls, trans_dir)

    if generate_translate_merge:
        merge_all_translate_files(trans_dir, trans_merge_file)
