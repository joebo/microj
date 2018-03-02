@if "%1" == "debug" GOTO debug
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild microj.msbuild /p:Configuration=Release /p:Platform=x64
@GOTO end

:DEBUG
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild microj.msbuild /p:Configuration=Debug
@GOTO end

:END

copy /y bin\microj.exe c:\d3\d3\dashboarddemo\
