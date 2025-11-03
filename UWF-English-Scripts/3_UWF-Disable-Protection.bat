@echo off
echo ==========================================================
echo            UWF Protection [Disable] Script
echo ==========================================================
echo.
echo == !! IMPORTANT !! ==
echo == You MUST run this script as an [Administrator]!! ==
echo.
pause

echo --- 1. Disabling UWF Filter (Requires Reboot) ---
uwfmgr.exe filter disable

echo.
echo ==========================================================
echo           UWF Protection is 'Scheduled' to DISABLE!
echo ==========================================================
echo.
echo == You must [REBOOT] your computer now to enter 'Persistent Mode'. ==
echo == (Your settings [Disk, 30GB, etc.] are NOT deleted.) ==
echo.
pause
