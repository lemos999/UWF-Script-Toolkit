@echo off
setlocal
set "ROOT=%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" goto :missing_compiler
set "OUTPUT=%ROOT%bin\Release"
set "TARGET=winexe"

if /i "%~1"=="-SelfTest" (
  set "OUTPUT=%ROOT%bin\SelfTest"
  set "TARGET=exe"
  goto :build
)

goto :build

:build
if not exist "%OUTPUT%" mkdir "%OUTPUT%"
"%CSC%" /nologo /langversion:5 /target:%TARGET% /platform:anycpu /main:PortableUwfManager.Program /win32manifest:"%ROOT%app.manifest" /out:"%OUTPUT%\UWFManager.exe" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Management.dll /r:System.Windows.Forms.dll "%ROOT%AssemblyInfo.cs" "%ROOT%Localization.cs" "%ROOT%MainForm.cs" "%ROOT%Program.cs" "%ROOT%SafetyRules.cs" "%ROOT%SelfTest.cs" "%ROOT%SystemSizing.cs" "%ROOT%UiDialogs.cs" "%ROOT%UwfController.cs" "%ROOT%UwfModels.cs" "%ROOT%VolumeSelection.cs"
if errorlevel 1 goto :failed
copy /y "%ROOT%App.config" "%OUTPUT%\UWFManager.exe.config" >nul
if errorlevel 1 goto :failed
if /i "%TARGET%"=="exe" goto :run_self_test
copy /y "%OUTPUT%\UWFManager.exe" "%ROOT%UWFManager.exe" >nul
if errorlevel 1 goto :failed
copy /y "%OUTPUT%\UWFManager.exe.config" "%ROOT%UWFManager.exe.config" >nul
if errorlevel 1 goto :failed
echo Built bin\Release\UWFManager.exe and refreshed the portable app.
exit /b 0

:run_self_test
"%OUTPUT%\UWFManager.exe" --self-test
if errorlevel 1 goto :failed
exit /b 0

:missing_compiler
echo C# compiler for .NET Framework 4.x was not found. 1>&2
exit /b 2

:failed
exit /b 1
