@if "%1" == "debug" GOTO debug
%windir%\microsoft.net\framework64\v4.0.30319\csc.exe /o /out:microj.exe microj.cs /define:CSSCRIPT /r:bin\CSScriptLibrary.dll
@GOTO end

:DEBUG
%windir%\microsoft.net\framework64\v4.0.30319\csc.exe /debug /out:microj.exe microj.cs /define:CSSCRIPT /r:bin\CSScriptLibrary.dll
@GOTO end

:END

