@echo off
chcp 65001 > nul
echo ==========================================================
echo           UWF (Unified Write Filter) [Disk 모드] 설정
echo ==========================================================
echo.
echo == !!     경고     !! ==
echo == Disk 모드는 C드라이브의 여유 공간을 [미리 차지]합니다! ==
echo == (예: 30GB 설정 시, C드라이브 남은 용량이 30GB 줄어듦) ==
echo == !! [현재 C드라이브 여유 공간]보다 크게 설정하면 안 됩니다!! ==
echo == !! 반드시 [관리자 권한]으로 실행해야 합니다 !! ==
echo.
echo --- 1. 원하는 Disk 오버레이 크기를 선택하세요 (숫자만 입력) ---
echo.
echo    1. 20GB (20480MB) - (가벼운 사용/테스트용)
echo    2. 30GB (30720MB) - (기본 게이밍 추천)
echo    3. 40GB (40960MB) - (넉넉한 게이밍 추천)
echo    4. 60GB (61440MB) - (대규모 패치/여러 게임 설치용)
echo    5. 80GB (81920MB) - (초대용량 / C드라이브에 100GB 이상 여유 있을 때)
echo.
echo    0. 취소 (스크립트 종료)
echo.
set /p size_choice="숫자 입력 (1, 2, 3, 4, 5, 0): "

if "%size_choice%"=="1" set overlay_size=20480
if "%size_choice%"=="2" set overlay_size=30720
if "%size_choice%"=="3" set overlay_size=40960
if "%size_choice%"=="4" set overlay_size=61440
if "%size_choice%"=="5" set overlay_size=81920
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
echo --- 2. [ %overlay_size%MB ] 크기로 Disk 모드를 설정합니다 ---
uwfmgr.exe overlay set-type Disk
uwfmgr.exe overlay set-size %overlay_size%

echo --- 3. UWF 필터 기능을 켭니다 (재부팅 필요) ---
uwfmgr.exe filter enable

echo --- 4. C: 드라이브를 보호하도록 설정합니다 (재부팅 필요) ---
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [Disk 모드] 설정이 모두 '예약'되었습니다!
echo ==========================================================
echo.
echo == 선택한 크기: %overlay_size%MB ==
echo == (이 버전은 카톡/라인 예외가 없습니다. 필요시 '4번' 스크립트로 추가하세요.) ==
echo == 지금 바로 컴퓨터를 [재부팅]하면 모든 설정이 적용됩니다. ==
echo.
pause
