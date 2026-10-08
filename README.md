# 🚀 Windows 11/10 UWF All-in-One Manager

A bilingual Windows desktop app for inspecting and configuring Microsoft's **Unified Write Filter (UWF)**. Run the app without administrator rights to review system status and changes. The app shows an operation plan before requesting UAC approval to apply it.

### 🎯 What This App Does

* **Review UWF at a glance:** See feature availability, filter state, protected volumes, overlay settings, and current/next-session configuration.
* **Configure protection:** Choose RAM or DISK overlay settings, set warning thresholds, and manage protected volumes.
* **Manage exclusions:** Add and remove file, folder, and registry exclusions.
* **Handle advanced operations:** Commit selected overlay changes, prepare Windows servicing, clean or reset UWF settings, and safely restart or shut down.
* **Use either language:** Switch between English and Korean from the app.

---

## ⛔ Before You Start: Check These First

### 1. Use a Windows edition that supports UWF

UWF is available on supported Windows Enterprise, Education, and IoT Enterprise editions. Availability can depend on the Windows image and configuration. It is not available on Home editions.

### 2. Keep a separate backup

UWF is not a backup system or a malware sandbox. A restart can discard changes written to protected volumes, while excluded paths and unprotected volumes can retain changes. Keep important files backed up separately.

### 3. Review every operation before applying it

The app starts without elevation. Configuration and recovery actions show their commands and warnings first, then use Windows UAC when administrator rights are needed. Advanced actions can permanently commit data or restart/shut down the device.

---

## 🤔 What Is UWF?

UWF protects selected volumes by redirecting writes to an overlay instead of immediately changing the protected volume.

* **While protection is on:** Changes written to protected volumes go to the overlay.
* **After a restart:** Overlay changes are normally discarded.
* **Exceptions and commits:** Excluded paths and changes explicitly committed from the overlay can persist.

This behavior can help keep a device in a known state, but it does not prevent data loss or make unsafe software safe to run.

---

## 🧠 Disk Mode vs. RAM Mode

Choose the overlay type based on the device's available storage, memory, and expected write workload.

### 1. DISK Mode

* **What it uses:** Free space on the Windows system volume for the overlay.
* **Consider:** The system volume needs more free space than the configured overlay size. The app checks this before applying DISK settings.
* **Often considered for:** Workloads with larger writes, such as software or game updates.

### 2. RAM Mode

* **What it uses:** System memory for the overlay.
* **Consider:** Memory assigned to the overlay is unavailable to Windows and other apps. If an overlay fills, the device may become unstable.
* **Often considered for:** Devices with enough available memory and lighter write workloads.

The app can suggest a starting size, but the right setting depends on the device and workload. Check overlay usage and available resources regularly.

---

## 🎮 First-Time Setup

1. Run **UWFManager.exe**. The app does not request administrator rights just to start.
2. Open **Status** and check the Windows edition, UWF feature, filter state, and current/next-session settings.
3. If the UWF feature is missing, choose **Install UWF** from Home or Status, review the plan, and restart Windows after installation.
4. Open **Setup**. Choose RAM or DISK, select the volumes to protect, review the overlay size and thresholds, then select **Apply setup plan**.
5. Read the commands and warnings in the review window. Continue only if the plan matches your intent, then approve the UAC prompt.
6. Restart Windows if the operation result says a restart is needed. Reopen **Status** to confirm the new state.

### 📜 Page Guide

* **Home:** First-use guidance, recommendations, and quick actions.
* **Status:** Current and next-session UWF settings, with refresh and report export.
* **Setup:** Overlay type and size, protected volumes, warning thresholds, and filter controls.
* **Exclusions:** File/folder and registry exclusion lists.
* **Advanced:** Commit overlay changes, service Windows, clean or reset UWF, and restart/shutdown actions.
* **Activity:** Recent operation results, which you can copy or clear.

---

## 💾 Keeping Selected Changes

### Exclusions

Add only the file, folder, or registry paths that must keep their changes across restarts. Exclusions can preserve writes, but they do not reduce overlay use. Keep the scope narrow and verify each path before applying it.

### Commit from the overlay

The Advanced page can commit a selected file, file deletion, registry key, or registry value from the overlay to the protected volume. A commit makes that selected change persistent. Review the target and operation plan carefully before continuing.

### Windows servicing

Use the Windows servicing action when preparing UWF for Windows updates. Follow the app's instructions, make sure user accounts meet Windows servicing requirements, and keep the device powered on while servicing runs.

---

## ⚠️ Important Behavior

* Changing the overlay type or size requires the filter to be off in the current session. The app refreshes status before planning those changes.
* A DISK overlay uses free space on the Windows system volume, not the selected protected volume.
* Some settings are staged for the next session and do not take effect until Windows restarts. Check the current and next-session values on **Status**.
* Unprotecting a volume or turning the filter off changes what UWF protects. Confirm the selected volumes and plan before applying.
* Reset, full-off, cleanup, commit, restart, and shutdown actions can have lasting effects. Read each review window before continuing.
* Unexpected UI exceptions are logged to **%LOCALAPPDATA%\PortableUwfManager\logs\crash.log**.

---

## 🧑‍💻 Build and Self-Test

On Windows, run these commands from the repository folder in PowerShell:

~~~powershell
.\Build.cmd
~~~

To build and run the included self-check:

~~~powershell
.\Build.cmd -SelfTest
~~~

The normal build refreshes **bin\Release\UWFManager.exe** and the portable **UWFManager.exe** with its configuration file in the repository root. You can also open **UWFManager.csproj** in Visual Studio or build it with MSBuild. The project targets .NET Framework 4.8 and Windows Forms. Direct project builds may require the .NET Framework 4.8 Developer Pack; **Build.cmd** uses the installed Framework C# compiler and does not require external packages.

### Source Files

* **MainForm.cs**, **UiDialogs.cs:** Main window, pages, styles, and review dialogs.
* **UwfController.cs**, **UwfModels.cs:** UWF plans, command execution, and status models.
* **SafetyRules.cs**, **VolumeSelection.cs**, **SystemSizing.cs:** Input checks, volume selection, and overlay sizing guidance.
* **Program.cs**, **Localization.cs**, **SelfTest.cs:** App startup, UAC handoff, language text, and self-checks.

---

## 📚 Microsoft References

* [UWF overview and supported editions](https://learn.microsoft.com/en-gb/windows/configuration/unified-write-filter/)
* [UWF command-line tool](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwfmgrexe)
* [UWF overlay](https://learn.microsoft.com/en-au/windows/configuration/unified-write-filter/uwfoverlay)
* [Set overlay type](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-overlayconfigsettype)
* [Set overlay size](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-overlayconfigsetmaximumsize)
* [UWF servicing mode and Windows updates](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-servicingupdatewindows)

---

## 🧑‍💻 Creator Info

* Created by: fewweekslater
* GitHub: [https://github.com/lemos999](https://github.com/lemos999)
* Email: lemoaxtoria@gmail.com
* Support: [https://ctee.kr/place/fewweekslater](https://ctee.kr/place/fewweekslater)

---

---

---

# 🚀 윈도우 11/10 UWF 통합 관리자

Microsoft **UWF(Unified Write Filter)** 를 확인하고 설정하는 한국어/영어 Windows 데스크톱 앱입니다. 관리자 권한 없이 앱을 실행해 현재 상태와 변경 계획을 확인할 수 있습니다. 계획을 검토한 뒤 UAC 승인을 받아 작업을 적용합니다.

### 🎯 이 앱으로 할 수 있는 일

* **UWF 한눈에 보기:** 기능 설치 여부, 필터 상태, 보호 볼륨, 오버레이 설정, 현재/다음 세션 설정을 확인합니다.
* **보호 설정:** RAM/DISK 오버레이와 경고 기준을 설정하고 보호 볼륨을 관리합니다.
* **예외 관리:** 파일·폴더 및 레지스트리 예외를 추가하거나 제거합니다.
* **고급 작업:** 오버레이 변경 커밋, Windows 서비스 준비, UWF 정리·초기화, 안전한 재시작·종료 작업을 실행합니다.
* **언어 전환:** 앱에서 한국어와 English를 바꿀 수 있습니다.

---

## ⛔ 시작 전 확인하세요

### 1. UWF를 지원하는 Windows 에디션인지 확인하세요

UWF는 지원되는 Windows Enterprise, Education, IoT Enterprise 에디션에서 사용할 수 있습니다. 실제 사용 가능 여부는 Windows 이미지와 구성에 따라 달라질 수 있으며 Home 에디션에서는 사용할 수 없습니다.

### 2. 중요한 데이터는 별도로 백업하세요

UWF는 백업 도구나 악성 코드 실행용 샌드박스가 아닙니다. 재시작하면 보호 볼륨에 기록된 변경이 사라질 수 있지만, 예외 경로와 보호되지 않는 볼륨의 변경은 남을 수 있습니다. 중요한 파일은 별도로 백업하세요.

### 3. 적용 전 작업 계획을 확인하세요

앱은 관리자 권한 없이 시작합니다. 설정·복구 작업은 명령과 경고를 먼저 보여주고, 관리자 권한이 필요하면 Windows UAC를 요청합니다. 고급 작업은 데이터를 영구 반영하거나 장치를 재시작·종료할 수 있습니다.

---

## 🤔 UWF란?

UWF는 선택된 볼륨의 쓰기 작업을 오버레이로 보내 보호 볼륨 자체가 바로 바뀌지 않도록 합니다.

* **보호가 켜져 있을 때:** 보호 볼륨에 대한 변경은 오버레이에 기록됩니다.
* **재시작 후:** 오버레이에 쌓인 변경은 일반적으로 폐기됩니다.
* **예외와 커밋:** 예외로 지정한 경로와 오버레이에서 명시적으로 커밋한 변경은 유지될 수 있습니다.

UWF는 장치를 일정한 상태로 유지하는 데 도움을 줄 수 있지만, 데이터 손실을 막거나 안전하지 않은 프로그램을 안전하게 만들지는 않습니다.

---

## 🧠 DISK 모드와 RAM 모드

장치의 저장 공간, 메모리, 예상 쓰기량에 맞춰 오버레이 유형을 선택하세요.

### 1. DISK 모드

* **사용 공간:** Windows 시스템 볼륨의 여유 공간을 오버레이에 사용합니다.
* **확인할 점:** 시스템 볼륨에는 설정한 오버레이 크기보다 더 많은 여유 공간이 필요합니다. 앱은 DISK 설정 적용 전에 이를 확인합니다.
* **사용 사례:** 프로그램이나 게임 업데이트처럼 쓰기량이 많은 작업에 고려할 수 있습니다.

### 2. RAM 모드

* **사용 공간:** 시스템 메모리를 오버레이에 사용합니다.
* **확인할 점:** 오버레이가 사용하는 메모리는 Windows와 다른 앱이 사용할 수 없습니다. 오버레이가 가득 차면 장치가 불안정해질 수 있습니다.
* **사용 사례:** 메모리 여유가 충분하고 쓰기량이 비교적 적은 환경에 고려할 수 있습니다.

앱이 시작 용량을 추천할 수 있지만, 적절한 설정은 장치와 사용량에 따라 다릅니다. 오버레이 사용량과 남은 자원을 주기적으로 확인하세요.

---

## 🎮 처음 설정하는 순서

1. **UWFManager.exe**를 실행합니다. 앱 시작만으로 관리자 권한을 요구하지 않습니다.
2. **상태**에서 Windows 에디션, UWF 기능, 필터 상태, 현재/다음 세션 설정을 확인합니다.
3. UWF 기능이 없다면 **홈** 또는 **상태**에서 **UWF 기능 설치**를 선택합니다. 계획을 확인하고 설치 후 Windows를 다시 시작합니다.
4. **설정**에서 RAM 또는 DISK, 보호 볼륨, 오버레이 크기와 경고 기준을 선택한 뒤 **설정 계획 적용**을 누릅니다.
5. 검토 창의 명령과 경고를 읽습니다. 계획이 의도와 맞을 때 계속하고 UAC를 승인합니다.
6. 작업 결과에 재시작이 필요하다고 표시되면 Windows를 다시 시작합니다. 이후 **상태**에서 적용 결과를 확인합니다.

### 📜 화면 안내

* **홈:** 첫 사용 안내, 추천 설정, 빠른 작업
* **상태:** 현재와 다음 세션의 UWF 설정, 새로고침 및 보고서 내보내기
* **설정:** 오버레이 유형·크기, 보호 볼륨, 경고 기준, 필터 제어
* **예외:** 파일·폴더 및 레지스트리 예외 목록
* **고급:** 오버레이 변경 커밋, Windows 서비스, UWF 정리·초기화, 재시작·종료 작업
* **활동 기록:** 최근 작업 결과 확인, 복사, 지우기

---

## 💾 필요한 변경만 유지하기

### 예외 경로

재시작 후에도 변경을 유지해야 하는 파일·폴더·레지스트리 경로만 추가하세요. 예외는 쓰기를 보존할 수 있지만 오버레이 사용량을 줄이지는 않습니다. 범위를 좁게 지정하고 적용 전에 경로를 확인하세요.

### 오버레이에서 커밋

고급 화면에서 오버레이의 파일, 파일 삭제, 레지스트리 키 또는 값을 보호 볼륨에 커밋할 수 있습니다. 커밋한 변경은 영구적으로 유지됩니다. 계속하기 전에 대상과 작업 계획을 꼼꼼히 확인하세요.

### Windows 서비스

Windows 업데이트를 위해 UWF 서비스 작업을 준비할 때 Windows 서비스 기능을 사용하세요. 앱 안내를 따르고, 사용자 계정이 Windows 서비스 요구 사항을 충족하는지 확인하며, 작업이 진행되는 동안 장치 전원을 유지하세요.

---

## ⚠️ 중요한 동작

* 오버레이 유형이나 크기를 바꾸려면 현재 세션에서 필터가 꺼져 있어야 합니다. 앱은 계획을 만들기 전에 상태를 다시 확인합니다.
* DISK 오버레이는 선택한 보호 볼륨이 아니라 Windows 시스템 볼륨의 여유 공간을 사용합니다.
* 일부 설정은 다음 세션에 예약되며 Windows 재시작 후 적용됩니다. **상태**에서 현재 값과 다음 세션 값을 확인하세요.
* 볼륨 보호 해제나 필터 끄기는 UWF가 보호하는 범위를 바꿉니다. 적용 전에 볼륨과 작업 계획을 확인하세요.
* 초기화, 전체 끄기, 공간 정리, 커밋, 재시작, 종료 작업은 되돌리기 어려운 영향을 줄 수 있습니다. 계속하기 전에 검토 창을 읽으세요.
* 예기치 않은 UI 예외는 **%LOCALAPPDATA%\PortableUwfManager\logs\crash.log**에 기록됩니다.

---

## 🧑‍💻 빌드 및 자체 점검

Windows에서 PowerShell을 열고 저장소 폴더에서 실행합니다.

~~~powershell
.\Build.cmd
~~~

내장 자체 점검을 빌드하고 실행하려면 다음 명령을 사용합니다.

~~~powershell
.\Build.cmd -SelfTest
~~~

일반 빌드는 **bin\Release\UWFManager.exe**와 저장소 루트의 휴대용 **UWFManager.exe** 및 설정 파일을 갱신합니다. Visual Studio에서 **UWFManager.csproj**를 열거나 MSBuild로 빌드할 수도 있습니다. 프로젝트는 .NET Framework 4.8과 Windows Forms를 사용합니다. 프로젝트 파일을 직접 빌드하려면 .NET Framework 4.8 Developer Pack이 필요할 수 있습니다. **Build.cmd**는 설치된 Framework C# 컴파일러를 사용하며 외부 패키지는 필요하지 않습니다.

### 소스 파일

* **MainForm.cs**, **UiDialogs.cs:** 메인 창, 화면, 스타일, 검토 대화상자
* **UwfController.cs**, **UwfModels.cs:** UWF 작업 계획, 명령 실행, 상태 모델
* **SafetyRules.cs**, **VolumeSelection.cs**, **SystemSizing.cs:** 입력 검증, 볼륨 선택, 오버레이 용량 안내
* **Program.cs**, **Localization.cs**, **SelfTest.cs:** 앱 시작, UAC 연결, 언어 문자열, 자체 점검

---

## 📚 Microsoft 참고 문서

* [UWF 개요 및 지원 에디션](https://learn.microsoft.com/en-gb/windows/configuration/unified-write-filter/)
* [UWF 명령줄 도구](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwfmgrexe)
* [UWF 오버레이](https://learn.microsoft.com/en-au/windows/configuration/unified-write-filter/uwfoverlay)
* [오버레이 유형 설정](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-overlayconfigsettype)
* [오버레이 크기 설정](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-overlayconfigsetmaximumsize)
* [UWF 서비스 모드와 Windows 업데이트](https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/uwf-servicingupdatewindows)

---

## 🧑‍💻 제작자 정보

* 제작자: fewweekslater
* GitHub: [https://github.com/lemos999](https://github.com/lemos999)
* 이메일: lemoaxtoria@gmail.com
* 후원: [https://ctee.kr/place/fewweekslater](https://ctee.kr/place/fewweekslater)
