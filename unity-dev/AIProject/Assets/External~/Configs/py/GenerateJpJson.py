import json
import os
import os.path
import argparse


def check_path_exists(dst_path):
    if not os.path.exists(dst_path):
        raise Exception(f"目录/文件不存在：{dst_path}")


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('-i', '--input_dir', type=str, required=True, help='Json翻译目录')
    parser.add_argument('-j', '--json_from_web', type=str, required=True, help='Json翻译目录')
    parser.add_argument('-o', '--output_dir', type=str, required=True, help='Json翻译目录')

    args = parser.parse_args()

    json_from_web = args.json_from_web  # 从国际化后台同步回来的文案
    input_dir = args.input_dir
    output_dir = args.output_dir
    with open(json_from_web, "r", encoding='utf-8') as json_all:
        all_text_json_obj = json.load(json_all)
        for input_file in os.listdir(input_dir):
            if input_file.endswith("_cn.json"):
                continue
            print(f"processing {input_file}")
            input_file_path = os.path.join(input_dir, input_file)
            with open(input_file_path, "r", encoding='utf-8') as input_json:
                json_obj = json.load(input_json)
                data_map = {}
                for k, v in json_obj.items():
                    if len(v) >= 2:
                        data_map[k] = v
                    elif k in all_text_json_obj:
                        data_map[k] = all_text_json_obj[k]
                    else:
                        data_map[k] = v
                output_file_path = os.path.join(output_dir, input_file)
                with open(output_file_path, "w", encoding='utf-8', newline='\n') as output_json:
                    output_json_object = json.dumps(data_map, indent=4, ensure_ascii=False)
                    output_json.write(output_json_object)
                output_csv_path = os.path.join(output_dir, input_file.replace(".json", ".csv"))
                with open(output_csv_path, "w", encoding='utf-8', newline='\n') as output_csv:
                    for kk, vv in data_map.items():
                        ss = vv.translate(str.maketrans({"\"": r"\"", "\n": r"\n", "\\": r"\\"}))
                        # output_csv.write(f"{kk},{ss}\n")
                        output_csv.write(f"{ss}\n")
