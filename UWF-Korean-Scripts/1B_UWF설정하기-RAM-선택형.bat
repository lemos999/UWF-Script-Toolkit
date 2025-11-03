@echo off
chcp 65001 > nul
echo ==========================================================
echo           UWF (Unified Write Filter) [RAM 모드] 설정
echo ==========================================================
echo.
echo == !!     경고     !! ==
echo == RAM 모드는 시스템 메모리를 오버레이로 사용합니다. ==
echo == 설정하는 크기만큼 RAM이 점유되며, 매우 위험할 수 있습니다! ==
echo == !! 반드시 [관리자 권한]으로 실행해야 합니다 !! ==
echo.
echo --- 1. 원하는 RAM 오버레이 크기를 선택하세요 (숫자만 입력) ---
echo.
echo    1. 1GB (1024MB) - (기본값 / 최소 8GB RAM PC 권장)
echo    2. 4GB (4096MB) - (최소 16GB RAM PC 권장)
echo    3. 8GB (8192MB) - (최소 32GB RAM PC 권장)
echo    4. 16GB (16384MB) - (매우 위험! / 64GB RAM PC 권장)
echo    5. 32GB (32768MB) - (매우 위험! / 64GB 이상 RAM PC 권장)
echo.
echo    0. 취소 (스크립트 종료)
echo.
set /p size_choice="숫자 입력 (1, 2, 3, 4, 5, 0): "

if "%size_choice%"=="1" set overlay_size=1024
if "%size_choice%"=="2" set overlay_size=4096
if "%size_choice%"=="3" set overlay_size=8192
if "%size_choice%"=="4" set overlay_size=16384
if "%size_choice%"=="5" set overlay_size=32768
if "%size_choice%"=="0" (
    echo ... 작업을 취소합니다.
    pause
    exit /b
)
if not defined overlay_size (
    echo ... 잘못된 숫자를 입력했습니다. 1~5 또는 0을 입력하세요.
    pause
    exit /b
)

echo.
echo --- 2. [경고] 및 [요주의] 임계값을 자동으로 설정합니다... ---
set /a default_warn = %overlay_size% * 80 / 100
set /a default_crit = %overlay_size% * 95 / 100
echo (1차 경고[80%%]: %default_warn%MB)
echo (2차 경고[95%%]: %default_crit%MB)

echo.
echo --- 3. 설정 적용 중... ---
uwfmgr.exe overlay set-type RAM
uwfmgr.exe overlay set-size %overlay_size%
uwfmgr.exe overlay set-warningthreshold %default_warn%
uwfmgr.exe overlay set-criticalthreshold %default_crit%
uwfmgr.exe filter enable
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [RAM 모드] 설정이 모두 '예약'되었습니다!
echo ==========================================================
echo.
echo == 선택한 크기: %overlay_size%MB
echo == 1차 경고(80%%): %default_warn%MB
echo == 2차 경고(95%%): %default_crit%MB
echo.
echo == (이 버전은 기본 예외가 없습니다. 필요시 '4번' 스크립트로 추가하세요.) ==
echo == 지금 바로 컴퓨터를 [재부팅]하면 모든 설정이 적용됩니다. ==
echo.
pause
