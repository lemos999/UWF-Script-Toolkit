@echo off
echo ==========================================================
echo           UWF (Unified Write Filter) [Feature Installer]
echo ==========================================================
echo.
echo == !! IMPORTANT !! ==
echo == This script installs the UWF feature onto Windows. ==
echo == You MUST run this script as an [Administrator]!! ==
echo.
pause

echo --- 1. Installing the 'Unified Write Filter' feature using DISM... ---
DISM /Online /Enable-Feature /FeatureName:Client-UnifiedWriteFilter

echo.
echo ==========================================================
echo      UWF Feature Installation Complete!
echo ==========================================================
echo.
echo == [REQUIRED] You must [REBOOT] your computer now. ==
echo == After rebooting, run one of the 'Setup' scripts (1A or 1B) ==
echo == to configure your overlay (Disk/RAM, size). ==
echo.
pause
