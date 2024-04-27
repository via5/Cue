set "cslist=%~1"

call list.bat "%cslist%"

rem for /f %%s in ('dir /s /b ..\res\*.json') do (
rem 	jslint "%%s"
rem )

