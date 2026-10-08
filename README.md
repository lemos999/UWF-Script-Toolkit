# Windows UWF Manager

A simple Windows app for checking and configuring Unified Write Filter (UWF). It shows a plan before applying changes.

## Requirements

- Windows 10/11 Enterprise, Education, or IoT Enterprise with UWF support
- .NET Framework 4.8
- Administrator approval for system changes. The app does not require it just to start.

## What UWF Does

UWF keeps changes to selected drives in temporary storage. Those changes are usually removed when Windows restarts. Excluded files and changes you choose to save can remain. UWF is not a backup.

## Getting Started

1. Run **UWFManager.exe**.
2. Open **Status** and check whether UWF is available.
3. If UWF is not installed, choose **Install UWF** and restart Windows when asked.
4. In **Setup**, choose RAM or DISK, the drives to protect, and the storage size.
5. Review the plan, approve the Windows prompt, and restart if requested.

## Pages

- **Home:** Quick guide and common actions
- **Status:** Current settings and settings that will apply after restart
- **Setup:** RAM or DISK mode, protected drives, and protection controls
- **Exclusions:** Files, folders, and registry entries to keep
- **Advanced:** Save selected changes, prepare Windows updates, reset UWF, or restart
- **Activity:** Recent operation results

## Important

- DISK mode uses free space on the Windows system drive. RAM mode uses system memory.
- Turn UWF protection off for the current session before changing the overlay type or size.
- Exclusions and saved changes can remain after restart. Check each path and plan before applying.
- Windows servicing requires passwords on all user accounts. Keep the device powered on while it runs.
- Unexpected app errors are logged to **%LOCALAPPDATA%\PortableUwfManager\logs\crash.log**.

## Build

Run these commands in PowerShell from the project folder:

~~~powershell
.\Build.cmd
.\Build.cmd -SelfTest
~~~

The project uses .NET Framework 4.8 and Windows Forms. Direct Visual Studio or MSBuild builds may require the .NET Framework 4.8 Developer Pack.

## Microsoft Documentation

- [UWF overview and supported editions](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/)
- [UWF overlay](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwfoverlay)
- [UWF servicing and Windows updates](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/service-uwf-protected-devices)

## Creator

- GitHub: [lemos999](https://github.com/lemos999)
- Email: lemoaxtoria@gmail.com
- Support: [fewweekslater](https://ctee.kr/place/fewweekslater)

---

# Windows UWF 관리자

Unified Write Filter(UWF)를 확인하고 설정하는 간단한 Windows 앱입니다. 설정을 바꾸기 전에 작업 계획을 보여줍니다.

## 실행 조건

- UWF를 지원하는 Windows 10/11 Enterprise, Education 또는 IoT Enterprise
- .NET Framework 4.8
- 시스템 설정 변경 시 관리자 승인 필요. 앱 실행 자체에는 필요하지 않습니다.

## UWF란?

선택한 드라이브의 변경 내용을 임시 저장 공간에 보관하는 Windows 기능입니다. 보통 Windows를 다시 시작하면 임시 변경 내용이 사라집니다. 예외로 지정한 파일이나 직접 저장한 변경 내용은 남을 수 있습니다. UWF는 백업 기능이 아닙니다.

## 사용 방법

1. **UWFManager.exe**를 실행합니다.
2. **상태**에서 UWF를 사용할 수 있는지 확인합니다.
3. UWF가 설치되지 않았다면 **UWF 기능 설치**를 선택하고, 안내가 나오면 Windows를 다시 시작합니다.
4. **설정**에서 RAM 또는 DISK, 보호할 드라이브, 저장 공간 크기를 선택합니다.
5. 작업 계획을 확인하고 Windows 승인 창에서 허용합니다. 재시작 안내가 나오면 Windows를 다시 시작합니다.

## 화면 안내

- **홈:** 간단한 안내와 자주 쓰는 작업
- **상태:** 현재 설정과 재시작 후 적용될 설정
- **설정:** RAM/DISK 모드, 보호 드라이브, 보호 기능 제어
- **예외:** 재시작 후에도 유지할 파일·폴더·레지스트리 항목
- **고급:** 변경 내용 저장, Windows 업데이트 준비, UWF 초기화, 재시작
- **활동 기록:** 최근 작업 결과

## 주의할 점

- DISK 모드는 Windows 시스템 드라이브의 여유 공간을 사용합니다. RAM 모드는 시스템 메모리를 사용합니다.
- 오버레이 유형이나 크기를 바꾸려면 현재 세션에서 UWF 보호를 꺼야 합니다.
- 예외 경로와 직접 저장한 변경 내용은 재시작 후에도 남을 수 있습니다. 적용 전에 경로와 작업 계획을 확인하세요.
- Windows 서비스 작업을 하려면 모든 사용자 계정에 암호가 있어야 합니다. 작업 중에는 전원을 끄지 마세요.
- 예기치 않은 앱 오류는 **%LOCALAPPDATA%\PortableUwfManager\logs\crash.log**에 기록됩니다.

## 빌드

프로젝트 폴더에서 PowerShell로 실행합니다.

~~~powershell
.\Build.cmd
.\Build.cmd -SelfTest
~~~

.NET Framework 4.8과 Windows Forms를 사용합니다. Visual Studio나 MSBuild로 직접 빌드하려면 .NET Framework 4.8 Developer Pack이 필요할 수 있습니다.

## Microsoft 문서

- [UWF 개요 및 지원 에디션](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/)
- [UWF 오버레이](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwfoverlay)
- [UWF 서비스 및 Windows 업데이트](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/service-uwf-protected-devices)

## 제작자

- GitHub: [lemos999](https://github.com/lemos999)
- 이메일: lemoaxtoria@gmail.com
- 후원: [fewweekslater](https://ctee.kr/place/fewweekslater)
