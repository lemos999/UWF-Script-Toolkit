using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PortableUwfManager
{
    internal static class SelfTest
    {
        public static int Run()
        {
            var failures = new List<string>();

            if (UiText.Current != UiLanguage.Korean)
            {
                failures.Add("default language is not Korean");
            }
            if (UiText.T("한글", "English") != "한글")
            {
                failures.Add("Korean localization default failed");
            }
            ExpectEqualText(failures, "plain", Elevation.QuoteArgumentForCreateProcess("plain"), "plain elevation argument");
            ExpectEqualText(failures, "\"has space\"", Elevation.QuoteArgumentForCreateProcess("has space"), "spaced elevation argument");
            ExpectEqualText(failures, "\"quoted\\\"value\"", Elevation.QuoteArgumentForCreateProcess("quoted\"value"), "quoted elevation argument");
            ExpectEqualText(failures, "alpha \"beta gamma\"", Elevation.BuildArgumentString(new[] { "alpha", "beta gamma" }), "elevation argument string");

            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\", false), "volume root");
            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\Windows", false), "windows folder");
            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\Windows\System32", false), "system32 folder");
            ExpectBlocked(failures, SafetyRules.ValidateFileExclusion(@"C:\Users\Test\NTUSER.DAT", false), "ntuser.dat");
            ExpectAllowed(failures, SafetyRules.ValidateFileExclusion(@"C:\ProgramData\Vendor\settings.ini", false), "normal file path");
            ExpectBlocked(failures, SafetyRules.ValidateRegistryExclusion(@"HKCU\Software\Test"), "non-HKLM registry");
            ExpectBlocked(failures, SafetyRules.ValidateRegistryExclusion(@"HKLM\SECURITY\Policy\Secrets\$MACHINE.ACC"), "machine secret");
            ExpectAllowed(failures, SafetyRules.ValidateRegistryExclusion(@"HKLM\SOFTWARE\Vendor\Product"), "normal registry key");
            ExpectEqual(failures, 1024, SizingRules.RecommendRamOverlayMb(8192, WorkloadProfile.FromIndex(0)), "ram recommendation light 8GB");
            ExpectEqual(failures, 1792, SizingRules.RecommendRamOverlayMb(16384, WorkloadProfile.FromIndex(1)), "ram recommendation normal 16GB");
            ExpectEqual(failures, 5888, SizingRules.RecommendRamOverlayMb(32768, WorkloadProfile.FromIndex(2)), "ram recommendation heavy 32GB");
            ExpectEqual(failures, 1024, SizingRules.RecommendDiskOverlayMb(3000, WorkloadProfile.FromIndex(0)), "disk recommendation low space");
            ExpectEqual(failures, 6144, SizingRules.RecommendDiskOverlayMb(16384, WorkloadProfile.FromIndex(1)), "disk recommendation normal");
            ExpectEqual(failures, 119808, SizingRules.RecommendDiskOverlayMb(200000, WorkloadProfile.FromIndex(2)), "disk recommendation dynamic heavy");
            ExpectContains(failures, CommandResult.FormatExitCode(unchecked((int)0x80070005)), "0x80070005", "hresult hex display");
            ExpectContains(failures, new CommandResult { ExitCode = unchecked((int)0x80070005) }.ToDisplayText(), "권한 거부", "access denied hint");
            ExpectDirectory(failures, MainForm.GetInitialDirectory(String.Empty), "empty browse initial directory");
            var commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (!String.IsNullOrEmpty(commonData))
            {
                ExpectEqualText(failures, Path.GetFullPath(commonData).TrimEnd('\\'), MainForm.GetInitialDirectory(commonData).TrimEnd('\\'), "existing browse initial directory");
            }
            ExpectVolumeSelection(failures, "C:,D:", false, false, 2, "parse two volumes");
            ExpectVolumeSelection(failures, "c: C:", false, false, 1, "dedupe volumes");
            ExpectVolumeSelection(failures, "all", true, true, 0, "parse all for protect");
            ExpectVolumeParseBlocked(failures, "all", false, "block all for unprotect");
            ExpectEqualText(failures, "C:,D:", VolumeSelectionDialog.BuildSelectionText(false, new List<string> { "C:", "D:" }), "dialog multi-volume selection text");
            ExpectEqualText(failures, "all", VolumeSelectionDialog.BuildSelectionText(true, new List<string> { "C:" }), "dialog all selection text");
            ExpectEqualText(failures, "UWF_Volume.CurrentSession=False,DriveLetter=\"C:\",VolumeName=\"\"",
                UwfController.BuildUwfVolumeObjectPath("C:", false), "next-session volume WMI object path");
            ExpectEqualText(failures, @"\Users\Lemos\.codex",
                UwfController.GetVolumeRelativePath(@"C:\Users\Lemos\.codex"), "WMI file exclusion volume-relative path");
            ExpectEqualText(failures, @"C:\Users\Lemos\.codex",
                UwfController.ToFullPathFromVolume("C:", @"\Users\Lemos\.codex"), "WMI file exclusion full path display");
            try
            {
                using (var dialog = new VolumeSelectionDialog(new List<string> { "C:", "D:" }, "C:,D:"))
                {
                }
            }
            catch (Exception ex)
            {
                failures.Add("volume selection dialog construction failed: " + ex.Message);
            }

            try
            {
                using (var form = new MainForm())
                {
                    if (form.Controls.Count == 0)
                    {
                        failures.Add("main form did not build its shell");
                    }

                    var languageField = typeof(MainForm).GetField("languageBox", BindingFlags.Instance | BindingFlags.NonPublic);
                    var languageBox = languageField == null ? null : languageField.GetValue(form) as ComboBox;
                    if (languageBox == null)
                    {
                        failures.Add("language selector is missing");
                    }
                    else
                    {
                        languageBox.SelectedIndex = 1;
                        if (UiText.Current != UiLanguage.English)
                        {
                            failures.Add("switching to English failed");
                        }
                        languageBox.SelectedIndex = 0;
                        if (UiText.Current != UiLanguage.Korean)
                        {
                            failures.Add("switching back to Korean failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add("main form construction failed: " + ex.Message);
            }

            var controller = new UwfController();
            var setupSnapshot = new UwfSnapshot();
            setupSnapshot.FilterCurrentEnabled = false;
            var setup = controller.CreateSetupPlan("RAM", ParseRequired("C:"), 4096, 3276, 3891, setupSnapshot);
            if (setup.Commands.Count != 6)
            {
                failures.Add("setup plan command count");
            }
            try
            {
                using (var dialog = new OperationReviewDialog(setup))
                {
                    ExpectEqualText(failures, UiText.T("작업 검토", "Review operation"), dialog.Text, "operation review dialog title");
                }
            }
            catch (Exception ex)
            {
                failures.Add("operation review dialog construction failed: " + ex.Message);
            }

            OperationPlan decodedPlan;
            string payloadError;
            if (!OperationPlan.TryFromPayload(setup.ToPayload(), out decodedPlan, out payloadError))
            {
                failures.Add("valid operation payload rejected: " + payloadError);
            }
            else
            {
                ExpectEqualText(failures, setup.Title, decodedPlan.Title, "operation payload title roundtrip");
                ExpectEqual(failures, setup.Commands.Count, decodedPlan.Commands.Count, "operation payload command roundtrip");
            }
            var invalidPayloadPlan = new OperationPlan("invalid executable");
            invalidPayloadPlan.Commands.Add(new CommandSpec("cmd.exe", "/c whoami"));
            if (OperationPlan.TryFromPayload(invalidPayloadPlan.ToPayload(), out decodedPlan, out payloadError))
            {
                failures.Add("operation payload accepted an unapproved executable");
            }

            var fileExclusionPlan = controller.CreateFileExclusionPlan(@"C:\Users\Lemos\.codex", true, "test warning");
            ExpectContains(failures, fileExclusionPlan.Title, "폴더/파일", "file exclusion plan uses folder/file wording");
            if (fileExclusionPlan.Commands.Count != 1)
            {
                failures.Add("file exclusion WMI plan command count");
            }
            if (fileExclusionPlan.Commands.Count > 0)
            {
                ExpectEqualText(failures, "UWF WMI", fileExclusionPlan.Commands[0].FileName, "file exclusion plan uses WMI command");
                ExpectContains(failures, fileExclusionPlan.Commands[0].Arguments, "folder add-exclusion", "file exclusion plan uses folder command wording");
            }
            var multiSetup = controller.CreateSetupPlan("RAM", ParseRequired("C:,D:"), 4096, 3276, 3891, setupSnapshot);
            if (multiSetup.Commands.Count != 7)
            {
                failures.Add("multi-volume setup plan command count");
            }
            if (multiSetup.Commands.Count > 6)
            {
                ExpectEqualText(failures, "volume protect D:", multiSetup.Commands[6].Arguments, "multi-volume setup second protect command");
            }
            var protectAll = controller.CreateVolumeProtectionPlan(ParseRequired("all"), true);
            if (protectAll.Commands.Count != 1)
            {
                failures.Add("protect all command count");
            }
            if (protectAll.Commands.Count > 0)
            {
                ExpectEqualText(failures, "volume protect all", protectAll.Commands[0].Arguments, "protect all command");
            }
            var cleanupBlockedSnapshot = new UwfSnapshot();
            cleanupBlockedSnapshot.FilterCurrentEnabled = true;
            var cleanupBlocked = controller.CreateDiskOverlayCleanupPlan(cleanupBlockedSnapshot);
            ExpectEqual(failures, 1, cleanupBlocked.Commands.Count, "cleanup blocked command count");
            if (cleanupBlocked.Commands.Count > 0)
            {
                ExpectEqualText(failures, "filter disable", cleanupBlocked.Commands[0].Arguments, "cleanup blocked disables filter first");
            }
            var cleanupReadySnapshot = new UwfSnapshot();
            cleanupReadySnapshot.FilterCurrentEnabled = false;
            cleanupReadySnapshot.CurrentProtectedVolumes.Add("C:");
            cleanupReadySnapshot.NextProtectedVolumes.Add("D:");
            var cleanupReady = controller.CreateDiskOverlayCleanupPlan(cleanupReadySnapshot);
            ExpectEqual(failures, 4, cleanupReady.Commands.Count, "cleanup ready command count");
            if (cleanupReady.Commands.Count >= 4)
            {
                ExpectEqualText(failures, "overlay set-type RAM", cleanupReady.Commands[0].Arguments, "cleanup set RAM");
                ExpectEqualText(failures, "overlay set-size 1024", cleanupReady.Commands[1].Arguments, "cleanup minimum size");
                ExpectEqualText(failures, "overlay set-warningthreshold 512", cleanupReady.Commands[2].Arguments, "cleanup warning threshold");
                ExpectEqualText(failures, "overlay set-criticalthreshold 1024", cleanupReady.Commands[3].Arguments, "cleanup critical threshold");
            }
            var fullOff = controller.CreateFullDisablePlan(cleanupReadySnapshot);
            ExpectContains(failures, PlanArgumentsText(fullOff), "volume unprotect C:", "full off unprotects current volume");
            ExpectContains(failures, PlanArgumentsText(fullOff), "volume unprotect D:", "full off unprotects next volume");
            ExpectContains(failures, PlanArgumentsText(fullOff), "overlay set-type RAM", "full off releases disk overlay");
            var fullReset = controller.CreateFullResetPlan(cleanupReadySnapshot);
            if (fullReset.Commands.Count == 0 || !fullReset.Commands[0].ContinueOnFailure)
            {
                failures.Add("full reset reset-settings should continue on failure");
            }
            ExpectContains(failures, PlanArgumentsText(fullReset), "filter reset-settings", "full reset requests reset-settings");
            ExpectContains(failures, PlanArgumentsText(fullReset), "filter disable", "full reset disables filter");

            var snapshot = new UwfSnapshot();
            snapshot.FilterCurrentEnabled = true;
            snapshot.FilterNextEnabled = false;
            if (!snapshot.HasPendingChanges())
            {
                failures.Add("pending change detection");
            }
            if (!snapshot.HasObservedState())
            {
                failures.Add("observed WMI state detection");
            }
            var exclusionSnapshot = new UwfSnapshot();
            exclusionSnapshot.FileExclusions.Add(@"C:\ProgramData\Vendor\settings.ini");
            exclusionSnapshot.RegistryExclusions.Add(@"HKLM\SOFTWARE\Vendor\Product");
            ExpectContains(failures, exclusionSnapshot.ToReport(), @"C:\ProgramData\Vendor\settings.ini", "snapshot reports file exclusions");
            ExpectContains(failures, exclusionSnapshot.ToReport(), @"HKLM\SOFTWARE\Vendor\Product", "snapshot reports registry exclusions");

            if (failures.Count > 0)
            {
                for (int i = 0; i < failures.Count; i++)
                {
                    Console.Error.WriteLine(failures[i]);
                }
                return 2;
            }

            Console.WriteLine("Self-test passed.");
            return 0;
        }

        private static string PlanArgumentsText(OperationPlan plan)
        {
            var text = new StringBuilder();
            if (plan == null)
            {
                return String.Empty;
            }

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                text.AppendLine(plan.Commands[i].Arguments);
            }
            return text.ToString();
        }

        private static void ExpectBlocked(List<string> failures, ValidationResult result, string name)
        {
            if (result.Allowed)
            {
                failures.Add("Expected blocked: " + name);
            }
        }

        private static void ExpectAllowed(List<string> failures, ValidationResult result, string name)
        {
            if (!result.Allowed)
            {
                failures.Add("Expected allowed: " + name + " - " + result.Message);
            }
        }

        private static void ExpectEqual(List<string> failures, int expected, int actual, string name)
        {
            if (expected != actual)
            {
                failures.Add("Expected " + expected.ToString() + " but got " + actual.ToString() + ": " + name);
            }
        }

        private static void ExpectContains(List<string> failures, string value, string expected, string name)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
            {
                failures.Add("Expected text containing '" + expected + "': " + name);
            }
        }

        private static void ExpectDirectory(List<string> failures, string path, string name)
        {
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                failures.Add("Expected existing directory: " + name);
            }
        }

        private static void ExpectEqualText(List<string> failures, string expected, string actual, string name)
        {
            if (!String.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Expected '" + expected + "' but got '" + actual + "': " + name);
            }
        }

        private static VolumeSelection ParseRequired(string text)
        {
            VolumeSelection selection;
            string error;
            if (!VolumeSelectionParser.TryParse(text, true, out selection, out error))
            {
                throw new InvalidOperationException(error);
            }
            return selection;
        }

        private static void ExpectVolumeSelection(List<string> failures, string text, bool allowAll, bool expectedAll, int expectedCount, string name)
        {
            VolumeSelection selection;
            string error;
            if (!VolumeSelectionParser.TryParse(text, allowAll, out selection, out error))
            {
                failures.Add("Expected volume parse success: " + name + " - " + error);
                return;
            }
            if (selection.IsAll != expectedAll)
            {
                failures.Add("Unexpected all flag: " + name);
            }
            if (selection.Volumes.Count != expectedCount)
            {
                failures.Add("Unexpected volume count: " + name);
            }
        }

        private static void ExpectVolumeParseBlocked(List<string> failures, string text, bool allowAll, string name)
        {
            VolumeSelection selection;
            string error;
            if (VolumeSelectionParser.TryParse(text, allowAll, out selection, out error))
            {
                failures.Add("Expected volume parse blocked: " + name);
            }
        }
    }
}
