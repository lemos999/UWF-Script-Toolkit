@echo off
chcp 65001 > nul
:LANG_SELECT
cls
echo ==================================================
echo.     UWF All-In-One Manager v9 by fewweekslater
echo ==================================================
echo.
echo ## 🧑‍💻 제작자 정보 / Creator Information ##
echo.
echo * 제작자 (Creator): fewweekslater
echo * 깃허브 (GitHub) : https://github.com/lemos999
echo * 이메일 (Email)  : lemoaxtoria@gmail.com
echo * 후원 (Donate)  : https://ctee.kr/place/fewweekslater
echo.
echo ==================================================
echo     Please select your language. / 언어를 선택하세요.
echo ==================================================
echo.
echo    1. 한국어 (Korean)
echo    2. English
echo.
set /p lang_choice="Enter number (1-2): "
if "%lang_choice%"=="1" goto MENU_KO
if "%lang_choice%"=="2" goto MENU_EN
echo Invalid selection. / 잘못된 선택입니다.
pause
goto LANG_SELECT

:MENU_KO
cls
echo ==========================================================
echo           UWF (Unified Write Filter) 통합 관리 스크립트 (v9)
echo ==========================================================

echo    1. UWF 기능 설치 (최초 1회)
echo    2. UWF 설정하기 [Disk 모드]
echo    3. UWF 설정하기 [RAM 모드]
echo    4. UWF 보호 [켜기]
echo    5. UWF 보호 [끄기]
echo    6. UWF [예외 경로 추가]
echo    7. UWF [예외 경로 제거]
echo    8. UWF [모든 설정 초기화]
echo    9. UWF [현재 설정 확인]
echo    
echo    99. 언어 선택으로 돌아가기
echo    0. 종료

echo ==========================================================
set /p choice="원하는 작업의 번호를 입력하세요: "
echo.

if "%choice%"=="1" goto FUNC_KO_0
if "%choice%"=="2" goto FUNC_KO_1
if "%choice%"=="3" goto FUNC_KO_2
if "%choice%"=="4" goto FUNC_KO_3
if "%choice%"=="5" goto FUNC_KO_4
if "%choice%"=="6" goto FUNC_KO_5
if "%choice%"=="7" goto FUNC_KO_6
if "%choice%"=="8" goto FUNC_KO_7
if "%choice%"=="9" goto FUNC_KO_8
if "%choice%"=="0" exit /b
if "%choice%"=="99" goto LANG_SELECT
echo 잘못된 번호입니다. 메뉴에 있는 숫자만 입력하세요.
pause
goto MENU_KO

:FUNC_KO_0
cls
echo ==========================================================
echo           UWF (Unified Write Filter) [기능 설치] 스크립트
echo ==========================================================
echo.
echo == !! 중요 !! ==
echo == 이 스크립트는 UWF '기능 자체'를 윈도우에 설치합니다. ==
echo == 반드시 [관리자 권한]으로 실행해야 합니다!! ==
echo.
pause

echo --- 1. DISM을 사용하여 '통합 쓰기 필터' 기능을 설치합니다... ---
DISM /Online /Enable-Feature /FeatureName:Client-UnifiedWriteFilter

echo.
echo ==========================================================
echo      UWF 기능 설치가 완료되었습니다!
echo ==========================================================
echo.
echo == [필수] 이제 컴퓨터를 [재부팅]해야 합니다. ==
echo == 재부팅한 후에, [2번] 또는 [3번] 설정 메뉴를 실행해서 ==
echo == 세부 설정(Disk/RAM, 용량)을 진행하세요! ==
echo.
pause

goto MENU_KO

:FUNC_KO_1
cls
echo ==========================================================
echo           UWF (Unified Write Filter) [Disk 모드] 설정
echo ==========================================================
echo.
echo --- 0. 현재 C: 드라이브 용량 확인 중... (PowerShell) ---
for /f "usebackq" %%i in (`powershell -Command "[math]::Round((Get-Volume -DriveLetter C).Size / 1MB)"`) do set total_disk_mb=%%i
for /f "usebackq" %%i in (`powershell -Command "[math]::Round((Get-Volume -DriveLetter C).SizeRemaining / 1MB)"`) do set free_disk_mb=%%i
set /a disk_reco = %free_disk_mb% * 50 / 100
echo.
echo [정보] C: 드라이브 총 용량: %total_disk_mb% MB
echo [정보] C: 드라이브 남은 여유 공간: %free_disk_mb% MB
echo [추천] 안전한 추천 용량 (여유 공간의 50%%): %disk_reco% MB
echo.
echo == !!     경고     !! ==
echo == 선택한 용량만큼 C드라이브 용량이 [미리 차지]됩니다! ==
echo == [여유 공간]보다 크게 설정하면 안 됩니다!! ==
echo == !! 반드시 [관리자 권한]으로 실행해야 합니다 !! ==
echo.
echo --- 1. 원하는 Disk 오버레이 크기를 선택하세요 (숫자만 입력) ---
echo.
echo    1. 20GB (20480MB) - (가벼운 사용/테스트용)
echo    2. 30GB (30720MB) - (기본 게이밍 추천)
echo    3. 40GB (40960MB) - (넉넉한 게이밍 추천)
echo    4. 60GB (61440MB) - (대규모 패치/여러 게임 설치용)
echo    5. 80GB (81920MB) - (초대용량 / C드라이브에 100GB 이상 여유 있을 때)
echo    6. [커스텀] 용량 직접 입력 (MB 단위)
echo.
echo    0. 이전 메뉴로
echo.
set /p size_choice="숫자 입력 (1, 2, 3, 4, 5, 6, 0): "

if "%size_choice%"=="1" set overlay_size=20480
if "%size_choice%"=="2" set overlay_size=30720
if "%size_choice%"=="3" set overlay_size=40960
if "%size_choice%"=="4" set overlay_size=61440
if "%size_choice%"=="5" set overlay_size=81920
if "%size_choice%"=="6" goto :custom_size_ko
if "%size_choice%"=="0" (
    echo ... 작업을 취소하고 이전 메뉴로 돌아갑니다.
    pause
    goto MENU_KO
)
if not defined overlay_size (
    echo ... 잘못된 숫자를 입력했습니다. 1~6 또는 0을 입력하세요.
    pause
    goto MENU_KO
)

goto :apply_settings_ko

:custom_size_ko
echo.
echo --- [커스텀] 원하는 용량을 MB 단위로 입력하세요 (예: 25000) ---
echo (현재 C: 여유 공간: %free_disk_mb% MB)
set /a max_disk_size = %free_disk_mb% * 90 / 100
echo (안전 최대 용량: %max_disk_size% MB)
set /p overlay_size="[커스텀 용량(MB)]: "
if not defined overlay_size (
    echo.
    echo ... !! 용량을 반드시 입력해야 합니다. !!
    pause
    goto :custom_size_ko
)
if "%overlay_size%"=="" (
    echo.
    echo ... !! 용량을 반드시 입력해야 합니다. !!
    pause
    goto :custom_size_ko
)
if %overlay_size% GEQ %max_disk_size% (
    echo.
    echo !! 오류 !! 여유 공간의 90%%(%max_disk_size%MB) 이상은 설정할 수 없습니다.
    echo 더 작은 값을 입력해주세요.
    pause
    goto :custom_size_ko
)
goto :apply_settings_ko

:apply_settings_ko
echo.
echo --- 2. [경고] 및 [요주의] 임계값을 자동으로 설정합니다... ---
set /a default_warn = %overlay_size% * 80 / 100
set /a default_crit = %overlay_size% * 95 / 100
echo (1차 경고[80%%]: %default_warn%MB)
echo (2차 경고[95%%]: %default_crit%MB)

echo.
echo --- 3. 설정 적용 중... ---
uwfmgr.exe overlay set-type Disk
uwfmgr.exe overlay set-size %overlay_size%
uwfmgr.exe overlay set-warningthreshold %default_warn%
uwfmgr.exe overlay set-criticalthreshold %default_crit%
uwfmgr.exe filter enable
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [Disk 모드] 설정이 모두 '예약'되었습니다!
echo ==========================================================
echo.
echo == 선택한 크기: %overlay_size%MB
echo == 1차 경고(80%%): %default_warn%MB
echo == 2차 경고(95%%): %default_crit%MB
echo.
echo == (기본 예외 없음. 필요시 '6번' 메뉴로 추가하세요.) ==
echo == 지금 바로 컴퓨터를 [재부팅]하면 모든 설정이 적용됩니다. ==
echo.
pause

goto MENU_KO

:FUNC_KO_2
cls
echo ==========================================================
echo           UWF (Unified Write Filter) [RAM 모드] 설정
echo ==========================================================
echo.
echo --- 0. 현재 총 시스템 RAM 확인 중... (PowerShell) ---
for /f "usebackq" %%i in (`powershell -Command "[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1MB)"`) do set total_ram_mb=%%i
echo.
echo [정보] 현재 총 시스템 RAM: %total_ram_mb% MB
echo == !!     경고     !! ==
echo == RAM 모드는 이 [총 시스템 RAM]에서 용량을 떼어 씁니다! ==
echo == PC가 쓸 RAM을 남겨두고, 아래 옵션에서 신중하게 선택하세요! ==
echo == !! 반드시 [관리자 권한]으로 실행해야 합니다 !! ==
echo.
echo --- 1. 원하는 RAM 오버레이 크기를 선택하세요 (숫자만 입력) ---
echo.
echo    1. 1GB (1024MB) - (기본값 / 최소 8GB RAM PC 권장)
echo    2. 4GB (4096MB) - (최소 16GB RAM PC 권장)
echo    3. 8GB (8192MB) - (최소 32GB RAM PC 권장)
echo    4. 16GB (16384MB) - (매우 위험! / 64GB RAM PC 권장)
echo    5. 32GB (32768MB) - (매우 위험! / 64GB 이상 RAM PC 권장)
echo    6. [커스텀] 용량 직접 입력 (MB 단위)
echo.
echo    0. 이전 메뉴로
echo.
set /p size_choice="숫자 입력 (1, 2, 3, 4, 5, 6, 0): "

if "%size_choice%"=="1" set overlay_size=1024
if "%size_choice%"=="2" set overlay_size=4096
if "%size_choice%"=="3" set overlay_size=8192
if "%size_choice%"=="4" set overlay_size=16384
if "%size_choice%"=="5" set overlay_size=32768
if "%size_choice%"=="6" goto :custom_size_ko
if "%size_choice%"=="0" (
    echo ... 작업을 취소하고 이전 메뉴로 돌아갑니다.
    pause
    goto MENU_KO
)
if not defined overlay_size (
    echo ... 잘못된 숫자를 입력했습니다. 1~6 또는 0을 입력하세요.
    pause
    goto MENU_KO
)

goto :apply_settings_ko

:custom_size_ko
echo.
echo --- [커스텀] 원하는 용량을 MB 단위로 입력하세요 (예: 2048) ---
echo (현재 총 RAM: %total_ram_mb% MB)
set /a max_ram_size = %total_ram_mb% * 90 / 100
echo (안전 최대 용량: %max_ram_size% MB)
set /p overlay_size="[커스텀 용량(MB)]: "
if not defined overlay_size (
    echo.
    echo ... !! 용량을 반드시 입력해야 합니다. !!
    pause
    goto :custom_size_ko
)
if "%overlay_size%"=="" (
    echo.
    echo ... !! 용량을 반드시 입력해야 합니다. !!
    pause
    goto :custom_size_ko
)
if %overlay_size% GEQ %max_ram_size% (
    echo.
    echo !! 오류 !! 총 RAM의 90%%(%max_ram_size%MB) 이상은 설정할 수 없습니다.
    echo 더 작은 값을 입력해주세요.
    pause
    goto :custom_size_ko
)
goto :apply_settings_ko

:apply_settings_ko
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
echo == (기본 예외 없음. 필요시 '6번' 메뉴로 추가하세요.) ==
echo == 지금 바로 컴퓨터를 [재부팅]하면 모든 설정이 적용됩니다. ==
echo.
pause

goto MENU_KO

:FUNC_KO_3
cls
echo ==========================================================
echo            UWF 보호 [다시 활성화] 스크립트
echo ==========================================================
echo.
echo == !! 중요 !! ==
echo == 이 스크립트는 반드시 [관리자 권한]으로 실행해야 합니다. ==
echo.
pause

echo --- 1. UWF 필터 기능을 다시 켭니다 (재부팅 필요) ---
echo --- (저장된 설정[Disk/RAM, 용량, 임계값]을 그대로 불러옵니다) ---
uwfmgr.exe filter enable

echo.
echo ==========================================================
echo           UWF 기능 켜기가 '예약'되었습니다!
echo ==========================================================
echo.
echo == 지금 바로 컴퓨터를 [재부팅]하면 보호가 다시 시작됩니다. ==
echo.
pause

goto MENU_KO

:FUNC_KO_4
cls
echo ==========================================================
echo                 UWF 보호 [비활성화] 스크립트
echo ==========================================================
echo.
echo == !! 중요 !! ==
echo == 이 스크립트는 반드시 [관리자 권한]으로 실행해야 합니다. ==
echo.
pause

echo --- 1. UWF 필터 기능을 끕니다 (재부팅 필요) ---
uwfmgr.exe filter disable

echo.
echo ==========================================================
echo           UWF 기능 끄기가 '예약'되었습니다!
echo ==========================================================
echo.
echo == 지금 바로 컴퓨터를 [재부팅]하면 보호가 해제됩니다. ==
echo == (저장된 설정값[Disk/RAM]은 지워지지 않습니다!) ==
echo.
pause

goto MENU_KO

:FUNC_KO_5
cls
echo ==========================================================
echo             UWF [예외 경로 추가] 스크립트
echo ==========================================================
echo.
echo == !! 중요 !! ==
echo == 이 스크립트는 반드시 [관리자 권한]으로 실행해야 합니다! ==
echo.
echo 예외로 추가할 [폴더]나 [파일]의 [전체 경로]를 입력하세요.
echo (예: C:MyGamesSaveData 또는 C:Dataconfig.ini)
echo.
echo (따옴표 없이, 경로만 입력하거나 붙여넣으세요.)
echo.
set /p user_path="추가할 경로: "

if not defined user_path (
    echo.
    echo ... 아무것도 입력되지 않았습니다. 작업을 취소합니다.
    pause
    goto MENU_KO
)
if "%user_path%"=="" (
    echo.
    echo ... 아무것도 입력되지 않았습니다. 작업을 취소합니다.
    pause
    goto MENU_KO
)

echo.
echo --- 1. 다음 경로를 예외 처리 목록에 추가합니다... ---
echo "%user_path%"
uwfmgr.exe file add-exclusion "%user_path%"

echo.
echo ==========================================================
echo           예외 경로 추가가 '예약'되었습니다!
echo ==========================================================
echo.
echo == '9번' 메뉴로 '다음 세션'에 추가됐는지 확인하세요. ==
echo == (이 작업은 [다음 재부팅] 시 적용됩니다) ==
echo.
pause

goto MENU_KO

:FUNC_KO_6
cls
echo ==========================================================
echo             UWF [예외 경로 제거] 스크립트
echo ==========================================================
echo.
echo == !! 중요 !! ==
echo == 이 스크립트는 반드시 [관리자 권한]으로 실행해야 합니다! ==
echo.
echo 예외에서 제거할 [폴더]나 [파일]의 [전체 경로]를
echo 정확하게! 입력하세요. (복사/붙여넣기 권장)
echo.
echo (따옴표 없이, 경로만 입력하거나 붙여넣으세요.)
echo.
set /p user_path="제거할 경로: "

if not defined user_path (
    echo.
    echo ... 아무것도 입력되지 않았습니다. 작업을 취소합니다.
    pause
    goto MENU_KO
)
if "%user_path%"=="" (
    echo.
    echo ... 아무것도 입력되지 않았습니다. 작업을 취소합니다.
    pause
    goto MENU_KO
)

echo.
echo --- 1. 다음 경로를 예외 처리 목록에서 제거합니다... ---
echo "%user_path%"
uwfmgr.exe file remove-exclusion "%user_path%"

echo.
echo ==========================================================
echo           예외 경로 제거가 '예약'되었습니다!
echo ==========================================================
echo.
echo == '9번' 메뉴로 '다음 세션'에서 제거됐는지 확인하세요. ==
echo == (이 작업은 [다음 재부팅] 시 적용됩니다) ==
echo.
pause

goto MENU_KO

:FUNC_KO_7
cls
echo ==========================================================
echo               UWF [모든 설정 초기화] 스크립트
echo ==========================================================
echo.
echo == !!     경고     !! ==
echo == 이 스크립트는 당신이 설정한 모든 UWF 값을 [초기화]합니다. ==
echo == (Disk -> RAM, 30GB -> 1024MB, 모든 예외 삭제 등) ==
echo == !! 반드시 [관리자 권한]으로 실행해야 합니다 !! ==
echo.
pause

echo --- 1. UWF 필터 기능을 끕니다 (재부팅 필요) ---
uwfmgr.exe filter disable

echo --- 2. C: 드라이브 보호 설정을 해제합니다 ---
uwfmgr.exe volume unprotect C:

echo --- 3. 오버레이 설정을 [RAM, 1024MB] 기본값으로 되돌립니다 ---
uwfmgr.exe overlay set-type RAM
uwfmgr.exe overlay set-size 1024

echo --- 4. 경고/요주의 임계값을 기본값으로 되돌립니다 ---
uwfmgr.exe overlay set-warningthreshold 512
uwfmgr.exe overlay set-criticalthreshold 1024

echo --- 5. (참고) 이 스크립트는 등록된 예외를 자동으로 지우지 않습니다. ---
echo ---    필요시 [7번] 메뉴로 직접 제거해주세요. ---

echo.
echo ==========================================================
echo      모든 UWF 설정이 '초기화 예약'되었습니다!
echo ==========================================================
echo.
echo == 지금 바로 컴퓨터를 [재부팅]하면 모든 설정이
echo == 윈도우 기본값으로 돌아가고, 기능이 꺼진 상태가 됩니다. ==
echo.
pause

goto MENU_KO

:FUNC_KO_8
cls
echo ==========================================================
echo                UWF [현재 설정 확인] 스크립트
echo ==========================================================
echo.
echo == !! 중요 !! ==
echo == 이 스크립트는 반드시 [관리자 권한]으로 실행해야 합니다. ==
echo == (권한이 없으면 아무 내용도 뜨지 않습니다!) ==
echo.
pause

echo --- UWF의 [현재 세션] 및 [다음 세션] 설정을 표시합니다 ---
uwfmgr.exe get-config

echo.
echo ==========================================================
echo                 설정 확인이 완료되었습니다.
echo ==========================================================
echo.
pause

goto MENU_KO

:MENU_EN
cls
echo ==========================================================
echo           UWF (Unified Write Filter) All-In-One Manager (v9)
echo ==========================================================

echo    1. Install UWF Feature (Run Once)
echo    2. Setup UWF [Disk Mode]
echo    3. Setup UWF [RAM Mode]
echo    4. [Enable] UWF Protection
echo    5. [Disable] UWF Protection
echo    6. [Add] Exclusion Path
echo    7. [Remove] Exclusion Path
echo    8. [Reset] All UWF Settings
echo    9. [Check] Current UWF Status
echo    
echo    99. Back to Language Selection
echo    0. Exit

echo ==========================================================
set /p choice="Please enter the number for the desired action: "
echo.

if "%choice%"=="1" goto FUNC_EN_0
if "%choice%"=="2" goto FUNC_EN_1
if "%choice%"=="3" goto FUNC_EN_2
if "%choice%"=="4" goto FUNC_EN_3
if "%choice%"=="5" goto FUNC_EN_4
if "%choice%"=="6" goto FUNC_EN_5
if "%choice%"=="7" goto FUNC_EN_6
if "%choice%"=="8" goto FUNC_EN_7
if "%choice%"=="9" goto FUNC_EN_8
if "%choice%"=="0" exit /b
if "%choice%"=="99" goto LANG_SELECT
echo Invalid choice. Please enter a number from the menu.
pause
goto MENU_EN

:FUNC_EN_0
cls
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
echo == After rebooting, run menu [2] or [3] to configure ==
echo == your overlay (Disk/RAM, size). ==
echo.
pause

goto MENU_EN

:FUNC_EN_1
cls
echo ==========================================================
echo           UWF (Unified Write Filter) [Disk Mode] Setup
echo ==========================================================
echo.
echo --- 0. Checking C: Drive Capacity... (PowerShell) ---
for /f "usebackq" %%i in (`powershell -Command "[math]::Round((Get-Volume -DriveLetter C).Size / 1MB)"`) do set total_disk_mb=%%i
for /f "usebackq" %%i in (`powershell -Command "[math]::Round((Get-Volume -DriveLetter C).SizeRemaining / 1MB)"`) do set free_disk_mb=%%i
set /a disk_reco = %free_disk_mb% * 50 / 100
echo.
echo [INFO] C: Drive Total Size: %total_disk_mb% MB
echo [INFO] C: Drive Free Space: %free_disk_mb% MB
echo [RECO] Safe Recommendation (50%% of free space): %disk_reco% MB
echo.
echo == !!     WARNING     !! ==
echo == The size you choose will be [pre-allocated] from your free space! ==
echo == !! Do NOT set a size larger than your C: drive's free space!! ==
echo == !! You MUST run this script as an [Administrator] !! ==
echo.
echo --- 1. Choose your desired Disk Overlay size (Enter a number) ---
echo.
echo    1. 20GB (20480MB) - (For light use / testing)
echo    2. 30GB (30720MB) - (Standard Gaming Recommendation)
echo    3. 40GB (40960MB) - (Generous Gaming Recommendation)
echo    4. 60GB (61440MB) - (For large patches / multiple games)
echo    5. 80GB (81920MB) - (Huge / Only if C: has >100GB free)
echo    6. [Custom] Enter size manually (in MB)
echo.
echo    0. Back to Main Menu
echo.
set /p size_choice="Enter number (1, 2, 3, 4, 5, 6, 0): "

if "%size_choice%"=="1" set overlay_size=20480
if "%size_choice%"=="2" set overlay_size=30720
if "%size_choice%"=="3" set overlay_size=40960
if "%size_choice%"=="4" set overlay_size=61440
if "%size_choice%"=="5" set overlay_size=81920
if "%size_choice%"=="6" goto :custom_size_en
if "%size_choice%"=="0" (
    echo ... Canceling and returning to main menu.
    pause
    goto MENU_EN
)
if not defined overlay_size (
    echo ... Invalid input. Please enter 1-6 or 0.
    pause
    goto MENU_EN
)

goto :apply_settings_en

:custom_size_en
echo.
echo --- [Custom] Enter desired size in MB (e.g., 25000) ---
echo (Current C: Free Space: %free_disk_mb% MB)
set /a max_disk_size = %free_disk_mb% * 90 / 100
echo (Safe Maximum Size: %max_disk_size% MB)
set /p overlay_size="[Custom Size (MB)]: "
if not defined overlay_size (
    echo.
    echo ... !! You MUST enter a value. !!
    pause
    goto :custom_size_en
)
if "%overlay_size%"=="" (
    echo.
    echo ... !! You MUST enter a value. !!
    pause
    goto :custom_size_en
)
if %overlay_size% GEQ %max_disk_size% (
    echo.
    echo !! ERROR !! Cannot set size to 90%% (%max_disk_size%MB) or more of free space.
    echo Please enter a smaller value.
    pause
    goto :custom_size_en
)
goto :apply_settings_en

:apply_settings_en
echo.
echo --- 2. Automatically setting Warning and Critical thresholds... ---
set /a default_warn = %overlay_size% * 80 / 100
set /a default_crit = %overlay_size% * 95 / 100
echo (Warning [80%%]: %default_warn%MB)
echo (Critical [95%%]: %default_crit%MB)

echo.
echo --- 3. Applying settings... ---
uwfmgr.exe overlay set-type Disk
uwfmgr.exe overlay set-size %overlay_size%
uwfmgr.exe overlay set-warningthreshold %default_warn%
uwfmgr.exe overlay set-criticalthreshold %default_crit%
uwfmgr.exe filter enable
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [Disk Mode] Setup is 'Scheduled'!
echo ==========================================================
echo.
echo == Selected Size: %overlay_size%MB
echo == Warning (80%%): %default_warn%MB
echo == Critical (95%%): %default_crit%MB
echo.
echo == (No default exclusions. Use menu #6 to add your own.) ==
echo == You must [REBOOT] your computer now to apply all settings. ==
echo.
pause

goto MENU_EN

:FUNC_EN_2
cls
echo ==========================================================
echo           UWF (Unified Write Filter) [RAM Mode] Setup
echo ==========================================================
echo.
echo --- 0. Checking Total System RAM... (PowerShell) ---
for /f "usebackq" %%i in (`powershell -Command "[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1MB)"`) do set total_ram_mb=%%i
echo.
echo [INFO] Total System RAM: %total_ram_mb% MB
echo == !!     WARNING     !! ==
echo == RAM Mode will use a part of this [Total System RAM]! ==
echo == Choose an option SMALLER than your Total RAM! ==
echo == !! You MUST run this script as an [Administrator] !! ==
echo.
echo --- 1. Choose your desired RAM Overlay size (Enter a number) ---
echo.
echo    1. 1GB (1024MB) - (Default / Min 8GB PC RAM recommended)
echo    2. 4GB (4096MB) - (Min 16GB PC RAM recommended)
echo    3. 8GB (8192MB) - (Min 32GB PC RAM recommended)
echo    4. 16GB (16384MB) - (Risky! / 64GB PC RAM recommended)
echo    5. 32GB (32768MB) - (Very Risky! / 64GB+ PC RAM recommended)
echo    6. [Custom] Enter size manually (in MB)
echo.
echo    0. Back to Main Menu
echo.
set /p size_choice="Enter number (1, 2, 3, 4, 5, 6, 0): "

if "%size_choice%"=="1" set overlay_size=1024
if "%size_choice%"=="2" set overlay_size=4096
if "%size_choice%"=="3" set overlay_size=8192
if "%size_choice%"=="4" set overlay_size=16384
if "%size_choice%"=="5" set overlay_size=32768
if "%size_choice%"=="6" goto :custom_size_en
if "%size_choice%"=="0" (
    echo ... Canceling and returning to main menu.
    pause
    goto MENU_EN
)
if not defined overlay_size (
    echo ... Invalid input. Please enter 1-6 or 0.
    pause
    goto MENU_EN
)

goto :apply_settings_en

:custom_size_en
echo.
echo --- [Custom] Enter desired size in MB (e.g., 2048) ---
echo (Your Total RAM: %total_ram_mb% MB)
set /a max_ram_size = %total_ram_mb% * 90 / 100
echo (Safe Maximum Size: %max_ram_size% MB)
set /p overlay_size="[Custom Size (MB)]: "
if not defined overlay_size (
    echo.
    echo ... !! You MUST enter a value. !!
    pause
    goto :custom_size_en
)
if "%overlay_size%"=="" (
    echo.
    echo ... !! You MUST enter a value. !!
    pause
    goto :custom_size_en
)
if %overlay_size% GEQ %max_ram_size% (
    echo.
    echo !! ERROR !! Cannot set size to 90%% (%max_ram_size%MB) or more of total RAM.
    echo Please enter a smaller value.
    pause
    goto :custom_size_en
)
goto :apply_settings_en

:apply_settings_en
echo.
echo --- 2. Automatically setting Warning and Critical thresholds... ---
set /a default_warn = %overlay_size% * 80 / 100
set /a default_crit = %overlay_size% * 95 / 100
echo (Warning [80%%]: %default_warn%MB)
echo (Critical [95%%]: %default_crit%MB)

echo.
echo --- 3. Applying settings... ---
uwfmgr.exe overlay set-type RAM
uwfmgr.exe overlay set-size %overlay_size%
uwfmgr.exe overlay set-warningthreshold %default_warn%
uwfmgr.exe overlay set-criticalthreshold %default_crit%
uwfmgr.exe filter enable
uwfmgr.exe volume protect C:

echo.
echo ==========================================================
echo           UWF [RAM Mode] Setup is 'Scheduled'!
echo ==========================================================
echo.
echo == Selected Size: %overlay_size%MB
echo == Warning (80%%): %default_warn%MB
echo == Critical (95%%): %default_crit%MB
echo.
echo == (No default exclusions. Use menu #6 to add your own.) ==
echo == You must [REBOOT] your computer now to apply all settings. ==
echo.
pause

goto MENU_EN

:FUNC_EN_3
cls
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

goto MENU_EN

:FUNC_EN_4
cls
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

goto MENU_EN

:FUNC_EN_5
cls
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
    goto MENU_EN
)
if "%user_path%"=="" (
    echo.
    echo ... No path entered. Canceling operation.
    pause
    goto MENU_EN
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
echo == Run menu #9 to confirm it's in the 'Next Session'. ==
echo == (This change will apply on your [Next Reboot]) ==
echo.
pause

goto MENU_EN

:FUNC_EN_6
cls
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
    goto MENU_EN
)
if "%user_path%"=="" (
    echo.
    echo ... No path entered. Canceling operation.
    pause
    goto MENU_EN
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
echo == Run menu #9 to confirm it's gone from 'Next Session'. ==
echo == (This change will apply on your [Next Reboot]) ==
echo.
pause

goto MENU_EN

:FUNC_EN_7
cls
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
echo ---    Please use menu #7 to remove them manually. ---

echo.
echo ==========================================================
echo      All UWF settings have been 'Scheduled' to RESET!
echo ==========================================================
echo.
echo == You must [REBOOT] your computer now. After reboot,
echo == UWF will be OFF and reset to factory defaults. ==
echo.
pause

goto MENU_EN

:FUNC_EN_8
cls
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

goto MENU_EN

