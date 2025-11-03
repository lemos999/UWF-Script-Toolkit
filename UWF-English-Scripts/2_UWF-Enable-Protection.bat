@echo off
echo ==========================================================
echo            UWF Protection [Enable] Script
echo ==========================================================
echo.
echo == !! IMPORTANT !! ==
echo == You MUST run this script as an [Administrator]!! ==
echo.
pause

echo --- 1. Enabling UWF Filter (Requires Reboot) ---
echo --- (This will load your previously saved settings: Disk/RAM, size, etc.) ---
uwfmgr.exe filter enable

echo.
echo ==========================================================
echo           UWF Protection is 'Scheduled' to ENABLE!
echo ==========================================================
echo.
echo == You must [REBOOT] your computer now to re-activate protection. ==
echo.
pause
