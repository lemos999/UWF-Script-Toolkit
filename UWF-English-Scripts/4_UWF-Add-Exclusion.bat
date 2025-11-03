@echo off
echo ==========================================================
echo             UWF [Add Exclusion] Script
echo ==========================================================
echo.
echo == !! IMPORTANT !! ==
echo == You MUST run this script as an [Administrator]!! ==
echo.
echo Please enter the [full path] of the [folder] or [file]
echo you want to exclude from protection.
echo (e.g., C:MyGamesSaveData or C:Dataconfig.ini)
echo.
echo (Do not use quotes. Just paste the path.)
echo.
set /p user_path="Path to add: "

if not defined user_path (
    echo.
    echo ... No path entered. Canceling operation.
    pause
    exit /b
)

echo.
echo --- 1. Adding the following path to the exclusion list... ---
echo "%user_path%"
uwfmgr.exe file add-exclusion "%user_path%"

echo.
echo ==========================================================
echo           Exclusion has been 'Scheduled' to be ADDED!
echo ==========================================================
echo.
echo == Run '7_UWF-Check-Status.bat' to confirm it's in the 'Next Session'. ==
echo == (This change will apply on your [Next Reboot]) ==
echo.
pause
