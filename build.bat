@if "%1" == "debug" GOTO debug
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild microj.msbuild
@GOTO end

:DEBUG
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild microj.msbuild /p:Configuration=Debug
@GOTO end

:END

