@echo off
chcp 65001 > nul
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
    exit /b
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
echo == '7_UWF설정확인.bat'로 '다음 세션'에 추가됐는지 확인하세요. ==
echo == (이 작업은 [다음 재부팅] 시 적용됩니다) ==
echo.
pause
