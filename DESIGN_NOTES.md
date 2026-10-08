# Pastel GUI redesign

This package contains the edited **Windows Forms source code** for Portable UWF Manager (.NET Framework 4.8, C# 5). It does not contain a rebuilt executable.

## Style changes
- Warm off-white canvas, cream/lavender/blush pastel gradient header, and a subtle tinted sidebar.
- Muted plum primary actions, rounded navigation/button silhouettes, pill status badges.
- Soft white cards used across Home, Dashboard, Setup, Exclusions, Advanced, and Activity.
- More consistent spacing, typography, form rows and wrap/scroll behavior for densely packed controls.
- The same theme is applied to operation review and protected-volume selection dialogs.
- All existing UWF command orchestration, permission checks, validations, language switching, and safety confirmation paths are preserved.

## Files changed
- `UiDialogs.cs` — shared palette, themed controls, gradient and card panels, dialog styles.
- `MainForm.cs` — themed shell, cards, action emphasis, and layout refinements.
- `DESIGN_PREVIEW.html` — **reference mockup only**. It does not use the Windows Forms runtime, read system settings, or execute UWF commands.

## Build on Windows

On a Windows machine with .NET Framework 4.8 and a supported C# compiler, from this folder run:

```bat
Build.cmd
Build.cmd -SelfTest
```

The first command produces `bin\Release\UWFManager.exe` and a portable `UWFManager.exe`. The self-test command runs existing tests. Windows with a UWF-capable edition is required to exercise the real system functionality.

**Why no `.exe` inside this archive?** The archive supplied with the request included binaries compiled before the redesign. Shipping them as though they reflected these source changes would be misleading. Rebuild to see the revised UI in the Windows application.

## Verification limits

The changed C# files passed a lexical delimiter balance scan. The standalone HTML concept preview was rendered in Chromium, and its navigation switched pages. Actual .NET Framework compilation and WinForms runtime testing could not be performed in this Linux environment.


## Repository update

- Build.cmd completed on Windows and refreshed the included UWFManager.exe.
- The app was not run on a UWF-enabled device, and no UWF settings were changed.
