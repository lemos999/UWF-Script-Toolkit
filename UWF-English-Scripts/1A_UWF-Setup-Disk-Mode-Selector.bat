@echo off
echo ==========================================================
echo           UWF (Unified Write Filter) [Disk Mode] Setup
echo ==========================================================
echo.
echo == !!     WARNING     !! ==
echo == Disk Mode [pre-allocates] free space from your C: drive! ==
echo == (e.g., Setting 30GB will reduce C: free space by 30GB) ==
echo == !! Do NOT set a size larger than your C: drive's free space!! ==
echo == !! You MUST run this script as an [Administrator] !! ==
echo.
echo --- 1. Choose your desired Disk Overlay size (Enter a number) ---
echo.
echo    1. 20GB (20480MB) - (For light use / testing)
echo    2. 30GB (30720MB) - (Standard Gaming Recommendation)
echo    3. 40GB (40960MB) - (Generous Gaming Recommendation)
echo    4. 60GB (61440MB) - (For large patches / multiple games)
echo    5. 80GB (81920MB) - (Huge / Only if C: has >100GB free)
echo.
echo    0. Cancel (Exit Script)
echo.
set /p size_choice="Enter number (1, 2, 3, 4, 5, 0): "

if "%size_choice%"=="1" set overlay_size=20480
if "%size_choice%"=="2" set overlay_size=30720
if "%size_choice%"=="3" set overlay_size=40960
if "%size_choice%"=="4" set overlay_size=61440
if "%size_choice%"=="5" set overlay_size=81920
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
echo --- 2. Setting Overlay to [Disk Mode] with [ %overlay_size%MB ] size ---
uwfmgr.exe overlay set-type Disk
uwfmgr.exe overlay set-size %overlay_size%

echo --- 3. Enabling UWF Filter (Requires Reboot) ---
uwfmgr.exe filter enable

echo --- 4. Setting C: Drive to be Protected (Requires Reboot) ---
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [Disk Mode] Setup is 'Scheduled'!
echo ==========================================================
echo.
echo == Selected Size: %overlay_size%MB ==
echo == (This version has no default exclusions. Use script #4 to add your own.) ==
echo == You must [REBOOT] your computer now to apply all settings. ==
echo.
pause
