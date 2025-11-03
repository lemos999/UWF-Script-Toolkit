@echo off
echo ==========================================================
echo                UWF [Check Status] Script
echo ==========================================================
echo.
echo == !! IMPORTANT !! ==
echo == You MUST run this script as an [Administrator]!! ==
echo == (Without Admin rights, this will show nothing!) ==
echo.
pause

echo --- Displaying UWF Config for [Current Session] and [Next Session] ---
uwfmgr.exe get-config

echo.
echo ==========================================================
echo                 Status check complete.
echo ==========================================================
echo.
pause
