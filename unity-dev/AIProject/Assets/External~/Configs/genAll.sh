#!/bin/bash
source paths.sh

python3 py/Generate.py -e $EXCEL_DIR -j $JSON_DIR -c "$CLIENT_DIR" -t $TRANSLATE_DIR -m $TRANSLATE_MERGE_DIR -n ALL --generate_json --generate_client --generate_translate --generate_translate_merge
