@echo off
echo ==========================================================
echo               UWF [Reset All Settings] Script
echo ==========================================================
echo.
echo == !!     WARNING     !! ==
echo == This script will [DELETE] all your custom UWF settings. ==
echo == (Disk -> RAM, 30GB -> 1024MB, all exclusions will be removed!) ==
echo == !! You MUST run this script as an [Administrator] !! ==
echo.
pause

echo --- 1. Disabling UWF Filter (Requires Reboot) ---
uwfmgr.exe filter disable

echo --- 2. Un-protecting C: Drive ---
uwfmgr.exe volume unprotect C:

echo --- 3. Resetting overlay to default [RAM, 1024MB] ---
uwfmgr.exe overlay set-type RAM
uwfmgr.exe overlay set-size 1024

echo --- 4. Resetting thresholds to default ---
uwfmgr.exe overlay set-warningthreshold 512
uwfmgr.exe overlay set-criticalthreshold 1024

echo --- 5. (Note) This script does not auto-delete custom exclusions. ---
echo ---    Please use [5_UWF-Remove-Exclusion.bat] to remove them manually. ---

echo.
echo ==========================================================
echo      All UWF settings have been 'Scheduled' to RESET!
echo ==========================================================
echo.
echo == You must [REBOOT] your computer now. After reboot,
echo == UWF will be OFF and reset to factory defaults. ==
echo.
pause
