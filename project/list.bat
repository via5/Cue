@echo off

> ..\Cue.cslist (
	call :dodir
)

goto :eof


:dodir
	setlocal

	for %%f in (*.cs) do (
		if "%%f" neq "DummyMain.cs" (
			echo project\%dir%%%f
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
