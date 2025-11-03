@echo off
chcp 65001 > nul
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

echo --- 4. (참고) 이 스크립트는 %user_path%에 등록된 예외를 자동으로 지우지 않습니다. ---
echo ---    필요시 [5_UWF예외제거.bat]로 직접 제거해주세요. ---

echo.
echo ==========================================================
echo      모든 UWF 설정이 '초기화 예약'되었습니다!
echo ==========================================================
echo.
echo == 지금 바로 컴퓨터를 [재부팅]하면 모든 설정이
echo == 윈도우 기본값으로 돌아가고, 기능이 꺼진 상태가 됩니다. ==
echo.
pause
