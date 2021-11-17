@echo off
setlocal enabledelayedexpansion

set out=%~1
set ignore=(%2 %3 %4 %5 %6 %7 %8 %9)

> "%out%" (
	cd src
	call :dodir
	cd ..
)

goto :eof


:dodir
	setlocal

	for %%f in (*.cs) do (
		set skip=0
		for %%i in %ignore% do (
		 	if "%%i"=="%%f" (
				set skip=1
		 	)
		)

		if !skip!==0 (
			echo project\src\%dir%%%f
		)
	)


	for /D %%d in (*) do (
		set dir=%dir%%%d\
		cd %%d
		call :dodir
		cd ..
	)
	endlocal
exit /b
