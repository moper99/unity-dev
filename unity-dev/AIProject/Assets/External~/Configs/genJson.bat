@echo off
call paths.bat

python py/Generate.py -e %EXCEL_DIR% -j %JSON_DIR% -c "%CLIENT_DIR%" -t %TRANSLATE_DIR% -m %TRANSLATE_MERGE_DIR% -n "ALL" --generate_json

pause
