@echo off

if '%1'=='-h' goto help
if '%1'=='--help' goto help
if '%1'=='/?' goto help
if '%1'=='/h' goto help
if '%1'=='/help' goto help

powershell -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0\build.ps1' %*"
exit /B %errorlevel%

:help
powershell -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0\build.ps1' -help"
