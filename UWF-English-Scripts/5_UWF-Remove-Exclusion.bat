@echo off
echo ==========================================================
echo             UWF [Remove Exclusion] Script
echo ==========================================================
echo.
echo == !! IMPORTANT !! ==
echo == You MUST run this script as an [Administrator]!! ==
echo.
echo Please enter the [EXACT full path] of the [folder] or [file]
echo you want to remove from the exclusion list. (Copy/Paste recommended)
echo.
echo (Do not use quotes. Just paste the path.)
echo.
set /p user_path="Path to remove: "

if not defined user_path (
    echo.
    echo ... No path entered. Canceling operation.
    pause
    exit /b
)

echo.
echo --- 1. Removing the following path from the exclusion list... ---
echo "%user_path%"
uwfmgr.exe file remove-exclusion "%user_path%"

echo.
echo ==========================================================
echo           Exclusion has been 'Scheduled' to be REMOVED!
echo ==========================================================
echo.
echo == Run '7_UWF-Check-Status.bat' to confirm it's gone from 'Next Session'. ==
echo == (This change will apply on your [Next Reboot]) ==
echo.
pause
