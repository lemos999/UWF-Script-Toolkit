@echo off
chcp 65001 > nul
echo ==========================================================
echo           UWF (Unified Write Filter) [RAM 모드] 설정
echo ==========================================================
echo.
echo == !!     경고     !! ==
echo == RAM 모드는 시스템 메모리를 오버레이로 사용합니다. ==
echo == 설정하는 크기만큼 RAM이 점유되며, 16GB, 32GB 같은 ==
echo == 거대한 크기는 시스템을 불안정하게 만들 수 있습니다! ==
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
echo --- 2. [ %overlay_size%MB ] 크기로 RAM 모드를 설정합니다 ---
uwfmgr.exe overlay set-type RAM
uwfmgr.exe overlay set-size %overlay_size%

echo --- 3. UWF 필터 기능을 켭니다 (재부팅 필요) ---
uwfmgr.exe filter enable

echo --- 4. C: 드라이브를 보호하도록 설정합니다 (재부팅 필요) ---
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [RAM 모드] 설정이 모두 '예약'되었습니다!
echo ==========================================================
echo.
echo == 선택한 크기: %overlay_size%MB ==
echo == (이 버전은 카톡/라인 예외가 없습니다. 필요시 '4번' 스크립트로 추가하세요.) ==
echo == 지금 바로 컴퓨터를 [재부팅]하면 모든 설정이 적용됩니다. ==
echo.
pause
