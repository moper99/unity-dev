@echo off
set regPath=HKEY_CLASSES_ROOT\*\shell\excel config to CSharp
set regCommandPath=%regPath%\command

reg add "%regPath%" /ve /d "%regDefaultValue%" /f
reg add "%regCommandPath%" /ve /d "%~dp0genCSharpWithFile.bat %%1" /f

echo Your command has been added to the context menu.
pause