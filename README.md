# 🚀 Windows 11/10 UWF All-In-One Manager (v9)

This is a single, powerful script that allows you to manage Windows' **UWF (Unified Write Filter)** feature with a simple menu, no complex commands required.

Instead of multiple files, you now only need to run **`UWF-Manager-Unified.bat`** for everything. The script will first ask you to select a language (English/Korean) and then provide a full menu of options.

### 🎯 What This Script Does

*   **Makes Your PC Like a Public Lab Computer:** Resets your C: drive to a clean, initial state every time you reboot.
*   **Provides Easy Management:** Handles all complex tasks like "Enable/Disable Protection," "Update/Persistent Mode," and "Reset Settings" through a simple, number-based menu.

---

## ⛔ [MUST READ] Before You Start: Check 2 Things!

### 1. Do I Have the Right Windows Version? (Crucial! ⭐️)

UWF **ONLY** works on **Windows 11/10 Enterprise** or **Windows 11/10 Education** editions.
It will **NOT** work on `Home` or `Pro` versions! (Even if the feature appears to install, it will not function. 😥)

#### ✅ [How to Check Your Windows Version]

1.  Press the `Win Key + R` to open the "Run" box.
2.  Type `winver` and press Enter.
3.  A small window will pop up. Check if it says "Windows 11 **Enterprise**" or "Windows 11 **Education**".
4.  (If it says 'Home' or 'Pro', these scripts will not work for you. 😭)

### 2. Run Scripts as Administrator!

This script modifies core system settings. You **MUST** run it by **Right-Clicking -> Run as Administrator**. (Double-clicking normally will 100% cause an error!)

---

## 🤔 What is UWF, Anyway?

In short, it's a feature that **"Makes your PC like a public library or school lab computer!"** 💻

*   **[The Public Library/School Lab Analogy]**
    You know how a computer at a public library or university lab is always clean? Even if you install a program or save files to the desktop, what happens when you reboot? Everything you did disappears, and the PC is back to its original, clean state.
    UWF is the official Windows feature that does exactly that: **"Reboot to Restore"**.

*   **[The Glass Pane Analogy]**
    Technically, it places a thin, transparent **'glass pane'** over your C: drive (the original data).
    All changes you make (downloading files, installing programs) are written onto this 'glass pane', not your actual C: drive.
    When you **reboot**, Windows simply **throws away** that dirty 'glass pane' and puts on a fresh, clean one.

*   **[The Result]**
    With a single reboot, your PC is **always back to its perfect, clean, initial state!** (Just like that library computer!)

---

## ✨ So, What's This Good For?

1.  **[Highly Recommended] "Freezing" a Perfect State (Your Personal "Fresh-Start" PC)**
    Right after a clean Windows format, install all your drivers, Steam, Discord, etc., to get your PC in its **"perfect, clean state"**. Then, use this script to turn on UWF.
    From now on, even if you get a virus or the system gets slow, **one reboot** instantly restores that perfect, clean state. (It feels like using a brand new PC every day!)

2.  **[Testing] Using Your PC Like a Safe Sandbox**
    Ever wonder, "Is this file a virus?" or "Is this program safe to install?"
    Just run it while UWF is active. Even if it's malware, **a simple reboot will make it vanish** as if it never existed. You can test anything without fear of breaking your system.

---

## 🧠 Disk Mode vs. RAM Mode (Your Key Choice!)

You need to decide *where* to create that 'transparent glass pane' (the overlay). You will choose the one that fits your needs from the main menu during setup.

### 1. Disk Mode (Select Menu `2. Setup UWF [Disk Mode]`)

*   **What is it?:** It uses a piece of your C: drive's free space (e.g., 30GB) to create a large 'temporary storage file'.
*   **👍 Pros:**
    *   **Large Capacity:** You can set it to 30GB, 50GB, or more.
    *   **Stability:** It can easily handle large game updates, shader caches, and other big temporary files without crashing.
*   **👎 Cons:**
    *   **Uses Drive Space:** A 30GB setting will "use up" 30GB of your C: drive's free space.
*   **👉 Recommended For:** Gamers and users of heavy software (The most stable option!)

### 2. RAM Mode (Select Menu `3. Setup UWF [RAM Mode]`)

*   **What is it?:** It uses your computer's **actual RAM (memory)** (e.g., 4GB) as the 'temporary storage', not the C: drive.
*   **👍 Pros:**
    *   **Extreme Speed:** It's incredibly fast because it runs in RAM.
    *   **SSD Protection:** It performs zero write operations to your C: drive, which is great for your SSD's lifespan.
*   **👎 Cons:**
    *   **Dangerous:** It **consumes your valuable system RAM**. (If you have 16GB of RAM and set an 8GB overlay, your PC only has 8GB left for Windows and games! 😥)
    *   **Unstable:** If the overlay fills up, your **system may freeze or crash instantly**.
*   **👉 Recommended For:** Light web-browsing PCs or advanced users who know exactly what they're doing.

---

## 🎮 How to Use "Persistent Mode" (For Game Updates!)

"UWF is on, but I need to update my Steam game!" (Time to be the System Administrator!)
This is how you **permanently save changes** to your C: drive.

**[STEP 1] "Pause" Protection**
1.  From the main menu, select **`5. [Disable] UWF Protection`** and run it.
2.  When it's done, **Reboot** your PC. 🔄

**[STEP 2] Do Your Work in "Persistent Mode"**
1.  Your PC is now in a "normal" state where protection is off.
2.  Install your Steam games, run Windows Updates, install new drivers... **do all the tasks you want to save permanently.**
3.  Everything you do in this step **will be saved** to your C: drive.

**[STEP 3] "Resume" Protection**
1.  When all your installations and updates are finished,
2.  From the main menu, select **`4. [Enable] UWF Protection`** and run it.
3.  When it's done, **Reboot** your PC. 🔄

**Done!** 🎉
Your PC is now back in its **fully protected mode**, but *with* all your new games and updates included! (The system maintenance is complete!)

---

## 📜 Menu Guide

After launching the script and choosing your language, you will see the main menu. Here is what each option does:

### [STEP 1] Installation (Do this only once!)

#### `1. Install UWF Feature (Run Once)`

*   **What it does:** Installs the UWF feature itself onto Windows.
*   **How to Use:** Select this option first. **You must reboot** after it completes.

### [STEP 2] Initial Setup (Choose ONE of these, only once!)

#### `2. Setup UWF [Disk Mode]` (Recommended for Gaming 🏗️)

*   **What it does:** Sets up UWF in **[Disk Mode]** for the first time.
*   **How to Use:** Select this option and **choose a size** for the Disk Overlay (20GB-80GB or custom) when prompted. **Reboot** to apply.

#### `3. Setup UWF [RAM Mode]` (For Advanced Users 💽)

*   **What it does:** Sets up UWF in **[RAM Mode]** for the first time.
*   **How to Use:** Select this option and **choose a size** for the RAM Overlay (1GB-32GB or custom). Be careful not to use too much of your system RAM! **Reboot** to apply.

### [STEP 3] Daily Use (Toggling On/Off)

#### `4. [Enable] UWF Protection` (Enable Protection 💡)

*   **What it does:** **Turns UWF protection back ON** using your saved settings.
*   **How to Use:** Select this when you want to re-enable protection. **Reboot** to apply.

#### `5. [Disable] UWF Protection` (Disable Protection 🔌)

*   **What it does:** **Turns UWF protection OFF**, putting your PC into "Persistent Mode" for updates.
*   **How to Use:** Select this *before* you want to install games/updates. **Reboot** to apply.

### [STEP 4] Changing Settings (When Needed)

#### `6. [Add] Exclusion Path` (Add Exclusion ➕)

*   **What it does:** Adds a folder or file (like a game save folder) to the **"do not reset"** list.
*   **How to Use:** Select this option and **paste a path** (e.g., `C:\MyGame\Saves`) when prompted. **Reboot** to apply.

#### `7. [Remove] Exclusion Path` (Remove Exclusion ➖)

*   **What it does:** **Removes** a folder or file from the "do not reset" list.
*   **How to Use:** Select this option and **paste the exact path** you want to remove. **Reboot** to apply.

### [STEP 5] Management & Reset

#### `8. [Reset] All UWF Settings` (Factory Reset 🚨)

*   **What it does:** **Deletes all your custom settings** (Disk/RAM mode, size, all exclusions) and returns UWF to its 'factory default' state (and disables it).
*   **How to Use:** Select this when you want to start fresh or remove UWF. **Reboot** to apply.

#### `9. [Check] Current UWF Status` (Check Status 🔍)

*   **What it does:** Shows your current UWF settings ("Current Session") and what will be applied after reboot ("Next Session").
*   **How to Use:** Select this anytime to see what's going on.

### Other Options

*   `99. Back to Language Selection`: Returns you to the initial language selection screen.
*   `0. Exit`: Closes the script.

---

## ⌨️ [Reference] Key Commands

These scripts are just friendly managers for the real commands below. (All require Admin rights).

*   Install Feature: `DISM /Online /Enable-Feature /FeatureName:Client-UnifiedWriteFilter`
*   Enable Filter: `uwfmgr.exe filter enable`
*   Disable Filter: `uwfmgr.exe filter disable`
*   Protect Drive: `uwfmgr.exe volume protect C:`
*   Set Overlay Type: `uwfmgr.exe overlay set-type <Disk|RAM>`
*   Set Overlay Size: `uwfmgr.exe overlay set-size <MB>` (e.g., `30720`)
*   Add File Exclusion: `uwfmgr.exe file add-exclusion "C:\Path"`
*   Remove File Exclusion: `uwfmgr.exe file remove-exclusion "C:\Path"`
*   Check Config: `uwfmgr.exe get-config`
*   Servicing Mode: `uwfmgr.exe servicing enable` (A special mode just for Windows Updates)

---

## 🧑‍💻 Creator Info

*   Created by: fewweekslater
*   GitHub: [https://github.com/lemos999](https://github.com/lemos999)
*   Email: lemoaxtoria@gmail.com
*   Support: [https://ctee.kr/place/fewweekslater](https://ctee.kr/place/fewweekslater)

---
---
---

# 🚀 윈도우 11/10 UWF 통합 관리 스크립트 (v9)

이것은 윈도우의 강력한 **'시스템 초기화(UWF)'** 기능을 복잡한 명령어 없이 간편한 메뉴 방식으로 관리해주는 단 하나의 스크립트입니다.

이제 여러 개의 파일 대신 **`UWF-Manager-Unified.bat`** 파일 하나만 실행하면 모든 것을 해결할 수 있습니다. 스크립트를 실행하면 먼저 언어(한글/영어)를 선택하고, 그 후에 모든 기능이 담긴 메뉴가 나타납니다.

### 🎯 이 스크립트가 해주는 일

*   **PC방 컴퓨터처럼 만들기:** 재부팅할 때마다 C드라이브를 항상 깨끗한 초기 상태로 되돌려줍니다.
*   **간편한 관리:** '보호 켜기/끄기', '게임 설치/업데이트 모드', '설정 초기화' 등 복잡한 모든 작업을 간단한 숫자 메뉴를 통해 해결해줍니다.

---

## ⛔ [필독] 시작하기 전, 딱 2가지만 확인해!

### 1. 내 윈도우 버전이 맞나? (가장 중요! ⭐️)

UWF는 오직 **윈도우 11/10 Enterprise(엔터프라이즈)** 또는 **Education(교육용)** 에디션에서만 작동합니다.
`Home(홈)`, `Pro(프로)` 버전에선 **절대 작동하지 않습니다!** (기능이 설치되는 것처럼 보여도 실제로는 동작하지 않아요 😥)

#### ✅ [내 PC가 Enterprise 버전인지 확인하는 법]

1.  키보드에서 `Win 키 + R 키`를 눌러 '실행' 창을 엽니다.
2.  `winver` 라고 입력하고 엔터를 칩니다.
3.  작은 창이 뜨면, 'Windows 11 **Enterprise**' 또는 'Windows 11 **Education**'이라고 쓰여 있는지 확인합니다.
4.  (만약 'Home'이나 'Pro'라고 쓰여있다면... 아쉽지만 이 스크립트는 작동하지 않습니다 😭)

### 2. 스크립트 실행은 [관리자 권한]으로!

이 스크립트는 윈도우의 핵심 설정을 변경하므로, 반드시 파일에 **마우스 오른쪽 클릭 -> [관리자 권_권한으로 실행]**을 눌러야 합니다. (그냥 더블클릭하면 100% 오류가 납니다!)

---

## 🤔 UWF가 도대체 뭐야?

쉽게 말해 **"PC방 컴퓨터처럼 만드는"** 기능입니다! 🎮

*   **[PC방]**
    PC방에서 컴퓨터를 켜면 깨끗하죠? 하지만 게임을 설치하고 바탕화면에 파일을 마구 저장하다가, 컴퓨터를 껐다 켜면? 내가 설치한 게임이나 파일이 싹 사라지고 원래의 깨끗한 상태로 돌아옵니다.
    UWF가 바로 그 '재부팅 초기화' 기능을 윈도우에 기본으로 넣어주는 것입니다.

*   **[유리판]**
    조금 더 자세히 말하면, 당신의 C드라이브(원본) 위에 아주 얇은 **'투명 유리판'**을 덮는 것입니다.
    컴퓨터를 쓰면서 생기는 모든 변경사항(파일 다운로드, 프로그램 설치)은 C드라이브 원본이 아닌, 그 '투명 유리판' 위에만 임시로 기록됩니다.
    그리고 재부팅하면, 그 더러워진 '투명 유리판'을 통째로 버리고 깨끗한 새 유리판으로 갈아 끼우는 것이죠.

*   **[결론]**
    재부팅 한 번이면, 당신의 PC는 **언제나 완벽하게 깨끗한 초기 상태로 돌아옵니다!** (PC방처럼!)

---

## ✨ 그래서 이걸 어디에 써?

1.  **[강력 추천] '완벽한 상태' 고정용 (나만의 PC방 만들기)**
    포맷 직후, 윈도우/드라이버/스팀/카톡 등 모든 필수 설치를 마친 '최상의 클린 상태'에서 이 스크립트로 UWF를 딱 켜 보세요.
    이제 컴퓨터가 바이러스에 걸리거나 느려져도, 재부팅 한 번이면 언제나 이 완벽한 상태로 즉시 돌아올 수 있습니다. (매일 PC방 새 자리 쓰는 기분!)

2.  **[테스트용] 안전한 '가상 PC'처럼 쓰기**
    "이 파일 바이러스 아냐?", "이 프로그램 깔아도 되나?"처럼 의심스러운 걸 확인할 때 UWF를 켠 상태로 실행해 보세요.
    그게 악성 코드라도, 재부팅하면 '없던 일'처럼 깨끗하게 사라집니다. 시스템 망가질 걱정 없이 마음껏 테스트할 수 있습니다!

---

## 🧠 Disk 모드 vs RAM 모드 (핵심 선택!)

'투명 유리판(오버레이)'을 어디에 만들지 정하는 것입니다. 당신의 용도에 맞는 것을 메뉴에서 딱 한 번만 골라서 설정하면 됩니다.

### 1. Disk 모드 (메뉴 `2. UWF 설정하기 [Disk 모드]` 선택)

*   **이게 뭐야?:** C드라이브의 여유 공간을 떼어(예: 30GB) 거대한 '임시 저장 파일'을 만듭니다.
*   **👍 장점:**
    *   **넉넉한 용량:** 30GB, 50GB처럼 아주 크게 설정할 수 있습니다.
    *   **안정성:** 게임 업데이트, 셰이더 캐시 등 용량 큰 작업도 재부팅 전까지 넉넉하게 버텨줍니다.
*   **👎 단점:**
    *   **C드라이브 공간 차지:** 설정한 30GB만큼 C드라이브 용량이 미리 줄어듭니다.
*   **👉 추천 대상:** 게이밍 PC, 무거운 프로그램 사용자 (가장 안정적!)

### 2. RAM 모드 (메뉴 `3. UWF 설정하기 [RAM 모드]` 선택)

*   **이게 뭐야?:** C드라이브가 아닌, 당신의 실제 RAM(메모리)을 떼어(예: 4GB) '임시 저장소'로 씁니다.
*   **👍 장점:**
    *   **최고 속도:** RAM에서 작동해서 엄청나게 빠릅니다.
    *   **SSD 보호:** C드라이브에 쓰기 작업을 전혀 안 해서 저장 장치 수명에 좋습니다.
*   **👎 단점:**
    *   **위험함:** 당신 PC의 소중한 RAM을 그대로 차지합니다. (16GB RAM PC에서 8GB 설정 시, 윈도우와 게임은 남은 8GB RAM으로 버텨야 합니다 😥)
    *   **불안정:** 용량이 꽉 차면 시스템이 즉시 멈추거나 강제 재부팅될 수 있습니다.
*   **👉 추천 대상:** 가벼운 웹서핑용 PC, 혹은 자신이 뭘 하는지 정확히 아는 고급 사용자

---

## 🎮 "영구 저장 모드" 사용법 (가장 자주 쓸 기능!)

"보호 상태인데... 스팀 게임 업데이트해야 해!" (PC방 사장님이 자리 관리하듯이!)
이럴 때 쓰는, 변경 사항을 C드라이브에 진짜로 저장하는 방법입니다.

**[1단계] 보호 '일시 정지'하기**
1.  메인 메뉴에서 **`5. UWF 보호 [끄기]`**를 선택하여 실행합니다.
2.  작업이 끝나면 컴퓨터를 **재부팅**합니다. 🔄

**[2단계] '영구 저장 모드'에서 할 일 하기**
1.  이제 PC는 보호가 풀린 '일반 PC' 상태입니다.
2.  스팀 게임 설치, 윈도우 업데이트, 드라이버 설치 등... **영구적으로 저장하고 싶은 모든 작업을 마음껏 합니다.**
3.  이때 한 모든 작업은 C드라이브에 영구적으로 저장됩니다.

**[3단계] 보호 '다시 시작'하기**
1.  모든 설치/업데이트가 끝났으면,
2.  메인 메뉴에서 **`4. UWF 보호 [켜기]`**를 선택하여 실행합니다.
3.  작업이 끝나면 컴퓨터를 **재부팅**합니다. 🔄

**끝!** 🎉
이제 PC는 당신이 새로 설치한 게임을 포함한 상태로, 다시 완벽하게 보호 모드(초기화 모드)로 돌아갑니다! (PC방 사장님이 새 게임 설치 완료!)

---

## 📜 메뉴 기능 상세 설명

스크립트를 실행하고 언어를 선택하면 메인 메뉴가 나타납니다. 각 메뉴 항목이 하는 일은 다음과 같습니다.

### [1단계] 설치 (맨 처음 딱 한 번!)

#### `1. UWF 기능 설치 (최초 1회)`

*   **뭐야?:** 윈도우에 UWF 기능 자체를 설치합니다.
*   **작동:** 가장 먼저 이 메뉴를 선택하세요. 작업 완료 후 반드시 **재부팅**해야 합니다!

### [2단계] 초기 설정 (둘 중 하나만 골라서 딱 한 번!)

#### `2. UWF 설정하기 [Disk 모드]` (게이밍 PC 권장 🏗️)

*   **뭐야?:** UWF를 **[Disk 모드]**로 처음 세팅해 줍니다.
*   **작동:** 이 메뉴를 선택하고, 안내에 따라 원하는 Disk 오버레이 크기(20GB~80GB 또는 직접 입력)를 선택하세요. 적용을 위해 **재부팅**이 필요합니다.

#### `3. UWF 설정하기 [RAM 모드]` (고급자용 💽)

*   **뭐야?:** UWF를 **[RAM 모드]**로 처음 세팅해 줍니다.
*   **작동:** 이 메뉴를 선택하고, 원하는 RAM 오버레이 크기(1GB~32GB 또는 직접 입력)를 선택하세요. (시스템 RAM 용량을 초과하지 않게 주의!) 적용을 위해 **재부팅**이 필요합니다.

### [3단계] 평상시 사용 (껐다 켰다)

#### `4. UWF 보호 [켜기]` (켜기 스크립트 💡)

*   **뭐야?:** 저장해 둔 설정(Disk든 RAM이든)을 그대로 불러와서 보호를 다시 켤 때 씁니다.
*   **작동:** 보호를 다시 켜고 싶을 때 선택하세요. 적용을 위해 **재부팅**이 필요합니다.

#### `5. UWF 보호 [끄기]` (끄기 스크립트 🔌)

*   **뭐야?:** UWF 보호를 비활성화해서 업데이트 등을 위한 '영구 저장 모드'로 만들 때 씁니다.
*   **작동:** 게임 설치/업데이트 전에 선택하세요. 적용을 위해 **재부팅**이 필요합니다.

### [4단계] 세부 설정 변경 (필요할 때)

#### `6. UWF [예외 경로 추가]` (예외 경로 추가 ➕)

*   **뭐야?:** 재부팅해도 '얼려지지 않을' 폴더나 파일(예: 게임 세이브, 카톡 데이터)을 추가할 때 씁니다.
*   **작동:** 메뉴 선택 후, 예외로 만들 경로(예: `C:\MyGame\Saves`)를 붙여넣으세요. 적용을 위해 **재부팅**이 필요합니다.

#### `7. UWF [예외 경로 제거]` (예외 경로 제거 ➖)

*   **뭐야?:** 예외로 설정했던 폴더나 파일을 다시 제거할 때 씁니다.
*   **작동:** 메뉴 선택 후, 제거할 정확한 경로를 붙여넣으세요. 적용을 위해 **재부팅**이 필요합니다.

### [5단계] 관리 및 초기화

#### `8. UWF [모든 설정 초기화]` (공장 초기화 🚨)

*   **뭐야?:** 당신이 설정한 모든 UWF 설정값(Disk/RAM, 용량, 예외)을 전부 삭제하고, 윈도우 '순정' 상태(비활성화)로 되돌려 줍니다.
*   **작동:** 처음부터 다시 설정하고 싶을 때 사용하세요. 적용을 위해 **재부팅**이 필요합니다.

#### `9. UWF [현재 설정 확인]` (상태 확인 🔍)

*   **뭐야?:** 지금 내 UWF 설정 상태가 어떤지 ("현재 세션" / "다음 세션") 자세히 보여줍니다.
*   **작동:** 설정이 잘 적용되었는지 궁금할 때 언제든 확인하세요.

### 기타 메뉴

*   `99. 언어 선택으로 돌아가기`: 처음의 한글/영어 선택 화면으로 돌아갑니다.
*   `0. 종료`: 스크립트를 닫습니다.

---

## ⌨️ [참고] 주요 명령어 목록

이 스크립트들은 사실 아래 명령어들을 대신 실행해주는 것입니다. (`< >` 안의 값은 상황에 맞게 변경)

*   기능 설치: `DISM /Online /Enable-Feature /FeatureName:Client-UnifiedWriteFilter`
*   기능 켜기: `uwfmgr.exe filter enable`
*   기능 끄기: `uwfmgr.exe filter disable`
*   드라이브 보호: `uwfmgr.exe volume protect C:`
*   오버레이 설정: `uwfmgr.exe overlay set-type <Disk|RAM>`
*   오버레이 크기: `uwfmgr.exe overlay set-size <MB>` (예: `30720`)
*   예외 파일 추가: `uwfmgr.exe file add-exclusion "경로"` (예: `"C:\Data"`)
*   예외 파일 제거: `uwfmgr.exe file remove-exclusion "경로"`
*   설정 확인: `uwfmgr.exe get-config`
*   서비스 모드: `uwfmgr.exe servicing enable` (윈도우 자동 업데이트 전용 모드)

---

## 🧑‍💻 제작자 정보

*   제작자: fewweekslater
*   깃허브: [https://github.com/lemos999](https://github.com/lemos999)
*   이메일: lemoaxtoria@gmail.com
*   후원: [https://ctee.kr/place/fewweekslater](https://ctee.kr/place/fewweekslater)
