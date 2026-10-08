# Portable UWF Manager

> **Pastel GUI redesign:** This repository includes UWFManager.exe built from the redesigned source. DESIGN_PREVIEW.html is a static mockup, not the Windows app. See DESIGN_NOTES.md for design details.


Windows의 Unified Write Filter(UWF)를 설정하고 점검하는 한국어/영어 Windows Forms 앱입니다. 관리자 권한 없이 상태를 볼 수 있고, 설정 변경은 실행 계획을 검토한 뒤 UAC 승인을 받아 적용합니다.

## 지원 환경

- UWF 기능을 지원하는 Windows 10/11 Enterprise, Education 또는 IoT Enterprise
- .NET Framework 4.8
- 설정 변경에는 관리자 권한이 필요합니다. 일반 사용자도 상태 조회와 계획 검토를 할 수 있습니다.

UWF는 시스템을 백업하거나 악성 코드를 안전하게 실행하는 샌드박스가 아닙니다. 보호된 볼륨에 쓴 내용은 재부팅 후 사라질 수 있고, 제외 경로와 보호되지 않은 볼륨의 변경은 유지될 수 있습니다. 중요한 데이터는 별도로 백업하세요.

## 시작

1. `UWFManager.exe`를 실행합니다. 앱이 자동으로 관리자 권한을 요구하지 않습니다.
2. **상태**에서 Windows 에디션, UWF 기능, 현재/다음 세션 설정을 확인합니다.
3. **설정**에서 오버레이 유형, 용량, 보호 볼륨을 선택합니다.
4. 실행 계획과 경고를 검토하고 계속을 누릅니다. 관리자 권한이 필요하면 UAC가 표시됩니다.
5. 적용 결과에 재부팅이 안내되면 Windows를 다시 시작합니다.

왼쪽 메뉴에서 홈, 상태, 설정, 예외, 고급 작업, 활동 기록을 열 수 있습니다. 오른쪽 위에서 한국어와 English를 바꿀 수 있습니다.

## 안전하게 사용하기

- UWF 설정 변경은 관리자 권한으로 실행됩니다. 계획은 적용 전에 명령과 경고를 보여 줍니다.
- 오버레이 유형이나 크기 변경은 현재 세션의 필터가 꺼져 있어야 합니다. 앱은 상태를 다시 확인하고 조건이 맞지 않으면 설정을 거부합니다.
- DISK 오버레이는 선택한 보호 볼륨이 아니라 Windows 시스템 볼륨의 여유 공간을 사용합니다. 설정할 크기보다 여유 공간이 커야 하며 앱은 이를 적용 직전에 다시 확인합니다.
- RAM/DISK 오버레이 크기와 보호 볼륨 변경은 재부팅 후 적용될 수 있습니다. 계획과 작업 결과를 확인한 뒤 재부팅하세요.
- 파일·레지스트리 제외 경로는 재부팅 뒤에도 쓰기를 보존할 수 있으므로 최소한으로 추가하세요. 제외는 오버레이 사용량을 줄이지 않습니다.
- Windows 업데이트는 **고급 → Windows 서비스 준비**로 UWF 서비스 모드를 예약한 뒤 진행하세요. 모든 사용자 계정에 암호가 있어야 하며, 서비스 진행 중 전원을 끄지 마세요.
- 상태 내보내기와 활동 기록 복사 기능이 있습니다. 예기치 않은 UI 예외는 `%LOCALAPPDATA%\PortableUwfManager\logs\crash.log`에 기록됩니다.

## 빌드 및 점검

Windows PowerShell에서 저장소 폴더로 이동해 실행합니다.

```powershell
.\Build.cmd
```

빌드 후 `bin\Release\UWFManager.exe`와 루트의 휴대용 `UWFManager.exe` 및 설정 파일이 갱신됩니다. 내장 자체 점검은 별도의 콘솔 실행 파일로 빌드되어 실행됩니다.

```powershell
.\Build.cmd -SelfTest
```

Visual Studio 또는 MSBuild로 `UWFManager.csproj`를 열어 빌드할 수도 있습니다. 프로젝트는 .NET Framework 4.8과 Windows Forms를 사용합니다. 프로젝트 파일을 직접 빌드할 때는 .NET Framework 4.8 Developer Pack(참조 어셈블리)이 필요할 수 있습니다. `Build.cmd`는 설치된 Framework C# 컴파일러를 사용합니다.

## 소스 구성

- `MainForm.cs`, `UiDialogs.cs`: 창, 페이지, 스타일 및 확인 대화상자
- `UwfController.cs`, `UwfModels.cs`: UWF 작업 계획, 명령 실행, 상태 모델
- `SafetyRules.cs`, `VolumeSelection.cs`, `SystemSizing.cs`: 입력 검증, 볼륨 선택, 오버레이 권장 용량
- `Program.cs`, `Localization.cs`, `SelfTest.cs`: 시작과 UAC 연결, 언어 문자열, 자체 점검

## Microsoft 문서

- [UWF 개요 및 지원 환경](https://learn.microsoft.com/en-gb/windows/configuration/unified-write-filter/)
- [UWF 명령줄 도구](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwfmgrexe)
- [오버레이 구성](https://learn.microsoft.com/en-au/windows/configuration/unified-write-filter/uwfoverlay)
- [오버레이 유형 설정](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-overlayconfigsettype)
- [오버레이 크기 설정](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-overlayconfigsetmaximumsize)
- [UWF 서비스 모드와 Windows 업데이트](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-servicingupdatewindows)

---

# English

A bilingual Windows Forms app for inspecting and configuring Unified Write Filter (UWF). Standard users can inspect status and review a plan. Changes require administrator approval through UAC.

## Requirements

- Windows 10/11 Enterprise, Education, or IoT Enterprise with UWF available
- .NET Framework 4.8
- Administrator rights for configuration changes

UWF is not a backup system or a safe malware sandbox. Writes to protected volumes can be discarded after restart; excluded paths and unprotected volumes can retain changes. Keep separate backups of important data.

## Use the app

1. Run `UWFManager.exe`; it does not request elevation at startup.
2. Review the Windows edition, UWF feature, and current/next-session settings on **Status**.
3. Choose overlay type, size, and protected volumes on **Setup**.
4. Read the operation plan and warnings, then select **Continue**. UAC appears when administrator rights are needed.
5. Restart Windows when the result says the staged changes require it.

The sidebar contains Home, Status, Setup, Exclusions, Advanced, and Activity. The language selector switches between Korean and English.

## Important behavior

- Overlay type and size can be changed only while the filter is disabled in the current session. The app refreshes status before planning those changes.
- A DISK overlay uses free space on the Windows system volume, not the selected protected volume. Free space must exceed the configured overlay size; the app checks again before applying.
- Some UWF configuration changes take effect after restart. Review the plan and result before rebooting.
- File and registry exclusions can preserve writes across restarts. Keep exclusions narrow; they do not reduce overlay use.
- For Windows updates, use **Advanced → Prepare Windows servicing** to schedule UWF servicing mode. Every user account must have a password, and the device must stay powered on during servicing.
- Status reports can be copied or exported, and the activity log can be copied. Unexpected UI exceptions are logged to `%LOCALAPPDATA%\PortableUwfManager\logs\crash.log`.

## Build and self-test

Run from Command Prompt or Windows PowerShell in the repository folder:

```powershell
.\Build.cmd
.\Build.cmd -SelfTest
```

The normal build refreshes `bin\Release\UWFManager.exe` and the portable `UWFManager.exe` plus its config file in the repository root. The self-test build runs the built-in checks in a console executable. You can also open `UWFManager.csproj` in Visual Studio or build it with MSBuild. It targets .NET Framework 4.8 and uses Windows Forms. Building the project file may require the .NET Framework 4.8 Developer Pack (reference assemblies); `Build.cmd` uses the installed Framework C# compiler and requires no external packages.

See the Microsoft links above for supported editions, overlay requirements, and servicing behavior.