@echo off
echo ==========================================================
echo           UWF (Unified Write Filter) [RAM Mode] Setup
echo ==========================================================
echo.
echo == !!     WARNING     !! ==
echo == RAM Mode uses your system's physical RAM as the overlay. ==
echo == The size you set will be RESERVED from your total RAM. ==
echo == Setting this too large (e.g., 16GB) CAN crash your system! ==
echo == !! You MUST run this script as an [Administrator] !! ==
echo.
echo --- 1. Choose your desired RAM Overlay size (Enter a number) ---
echo.
echo    1. 1GB (1024MB) - (Default / Min 8GB PC RAM recommended)
echo    2. 4GB (4096MB) - (Min 16GB PC RAM recommended)
echo    3. 8GB (8192MB) - (Min 32GB PC RAM recommended)
echo    4. 16GB (16384MB) - (Risky! / 64GB PC RAM recommended)
echo    5. 32GB (32768MB) - (Very Risky! / 64GB+ PC RAM recommended)
echo.
echo    0. Cancel (Exit Script)
echo.
set /p size_choice="Enter number (1, 2, 3, 4, 5, 0): "

if "%size_choice%"=="1" set overlay_size=1024
if "%size_choice%"=="2" set overlay_size=4096
if "%size_choice%"=="3" set overlay_size=8192
if "%size_choice%"=="4" set overlay_size=16384
if "%size_choice%"=="5" set overlay_size=32768
if "%size_choice%"=="0" (
    echo ... Canceling operation.
    pause
    exit /b
)
if not defined overlay_size (
    echo ... Invalid input. Please enter 1-5 or 0.
    pause
    exit /b
)

echo.
echo --- 2. Setting Overlay to [RAM Mode] with [ %overlay_size%MB ] size ---
uwfmgr.exe overlay set-type RAM
uwfmgr.exe overlay set-size %overlay_size%

echo --- 3. Enabling UWF Filter (Requires Reboot) ---
uwfmgr.exe filter enable

echo --- 4. Setting C: Drive to be Protected (Requires Reboot) ---
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [RAM Mode] Setup is 'Scheduled'!
echo ==========================================================
echo.
echo == Selected Size: %overlay_size%MB ==
echo == (This version has no default exclusions. Use script #4 to add your own.) ==
echo == You must [REBOOT] your computer now to apply all settings. ==
echo.
pause
