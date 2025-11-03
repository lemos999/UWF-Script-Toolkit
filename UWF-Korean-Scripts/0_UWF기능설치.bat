@echo off
chcp 65001 > nul
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
echo == 재부팅한 후에, [1A] 또는 [1B] 설정 스크립트를 실행해서 ==
echo == 세부 설정(Disk/RAM, 용량)을 진행하세요! ==
echo.
pause
