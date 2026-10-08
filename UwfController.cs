using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PortableUwfManager
{
    internal sealed class UwfController
    {
        private const int MinimumOverlaySizeMb = 1024;
        private const int DefaultWarningThresholdMb = 512;
        private const int DefaultCriticalThresholdMb = 1024;
        private const uint TokenQuery = 0x0008;
        private readonly CommandRunner runner;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, out TokenElevation tokenInformation, int tokenInformationLength, out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private enum TokenInformationClass
        {
            TokenElevation = 20
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenElevation
        {
            public int TokenIsElevated;
        }

        public UwfController()
        {
            runner = new CommandRunner();
        }

        public UwfStatus GetStatus()
        {
            var status = new UwfStatus();
            status.IsAdministrator = IsAdministrator();
            status.OsCaption = GetOsCaption();
            status.IsLikelySupportedEdition = IsLikelySupportedEdition(status.OsCaption);
            status.UwfToolPath = GetUwfMgrPath();
            status.UwfToolExists = File.Exists(status.UwfToolPath);
            status.Snapshot = QuerySnapshot();

            var report = new StringBuilder();
            report.AppendLine(UiText.T("포터블 UWF 관리자 진단 보고서", "Portable UWF Manager diagnostic report"));
            report.AppendLine(UiText.T("시간: ", "Timestamp: ") + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine(UiText.T("관리자 권한: ", "Administrator: ") + (status.IsAdministrator ? UiText.T("예", "yes") : UiText.T("아니오", "no")));
            report.AppendLine("OS: " + status.OsCaption);
            report.AppendLine(UiText.T("지원 가능 Edition: ", "Likely supported edition: ") + (status.IsLikelySupportedEdition ? UiText.T("예", "yes") : UiText.T("확인 필요", "check edition")));
            if (!status.IsLikelySupportedEdition && status.Snapshot != null && status.Snapshot.HasObservedState())
            {
                report.AppendLine(UiText.T("Edition 참고: 공식 지원 Edition은 확인 필요하지만 현재 UWF WMI 상태는 읽혔습니다.",
                    "Edition note: official support should be verified, but the current UWF WMI state was readable."));
            }
            report.AppendLine(UiText.T("OS 비트수: ", "OS bitness: ") + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));
            report.AppendLine(UiText.T("프로세스 비트수: ", "Process bitness: ") + (Environment.Is64BitProcess ? "64-bit" : "32-bit"));
            report.AppendLine("uwfmgr.exe: " + (status.UwfToolExists ? status.UwfToolPath : UiText.T("없음", "not found")));
            report.AppendLine();

            report.AppendLine(UiText.T("WMI 상태 스냅샷", "WMI snapshot"));
            report.AppendLine(status.Snapshot.ToReport());
            report.AppendLine();

            if (!status.UwfToolExists)
            {
                report.AppendLine(UiText.T("UWF 명령줄 도구가 없습니다. 먼저 Client-UnifiedWriteFilter 기능을 설치하세요.",
                    "UWF command-line tool is not present. Install the Client-UnifiedWriteFilter feature first."));
                status.Report = report.ToString();
                return status;
            }

            AppendCommand(report, "uwfmgr.exe get-config", RunReadOnly("get-config"));
            AppendCommand(report, "uwfmgr.exe overlay get-config", RunReadOnly("overlay get-config"));
            AppendCommand(report, "uwfmgr.exe overlay get-consumption", RunReadOnly("overlay get-consumption"));
            AppendCommand(report, "uwfmgr.exe overlay get-availablespace", RunReadOnly("overlay get-availablespace"));
            AppendCommand(report, "uwfmgr.exe servicing get-config", RunReadOnly("servicing get-config"));
            AppendCommand(report, "uwfmgr.exe file get-exclusions all", RunReadOnly("file get-exclusions all"));
            AppendCommand(report, "uwfmgr.exe registry get-exclusions", RunReadOnly("registry get-exclusions"));

            status.Report = report.ToString();
            return status;
        }

        public OperationPlan CreateInstallFeaturePlan()
        {
            var plan = new OperationPlan(UiText.T("UWF 기능 설치", "Install UWF feature"));
            plan.RequiresAdministrator = true;
            plan.Warning = UiText.T("Windows 선택 기능 Client-UnifiedWriteFilter를 설치합니다. 안정적인 UWF 설정을 위해 설치 후 재부팅이 필요합니다.",
                "Installs the Windows optional feature Client-UnifiedWriteFilter. A reboot is required before UWF can be configured reliably.");
            plan.Commands.Add(new CommandSpec("dism.exe", "/Online /Enable-Feature /FeatureName:Client-UnifiedWriteFilter /NoRestart"));
            return plan;
        }

        public OperationPlan CreateSetupPlan(string overlayType, VolumeSelection volumes, int sizeMb, int warningMb, int criticalMb, UwfSnapshot snapshot)
        {
            if (!String.Equals(overlayType, "RAM", StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(overlayType, "DISK", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid overlay type.");
            }

            if (volumes == null || (!volumes.IsAll && volumes.Volumes.Count == 0))
            {
                throw new InvalidOperationException("At least one volume is required.");
            }
            if (sizeMb < MinimumOverlaySizeMb)
            {
                throw new InvalidOperationException(UiText.T("오버레이 크기는 최소 1024MB여야 합니다.", "The overlay size must be at least 1024 MB."));
            }
            if (warningMb <= 0 || warningMb >= criticalMb || criticalMb >= sizeMb)
            {
                throw new InvalidOperationException(UiText.T("임계값은 0 < 경고 < 위험 < 오버레이 크기 순서여야 합니다.",
                    "Thresholds must satisfy 0 < warning < critical < overlay size."));
            }
            if (!CanChangeOverlayConfigNow(snapshot))
            {
                throw new InvalidOperationException(UiText.T(
                    "오버레이 설정은 현재 세션에서 UWF가 꺼진 상태여야 변경할 수 있습니다. 필터를 끄고 재시작한 뒤 상태를 다시 확인하세요.",
                    "Overlay configuration requires UWF to be disabled in the current session. Disable the filter, restart, and refresh status first."));
            }

            var plan = new OperationPlan(UiText.T("UWF " + overlayType + " 오버레이 설정", "Configure UWF " + overlayType + " overlay"));
            plan.RequiresAdministrator = true;
            plan.Warning =
                UiText.T("주의: 오버레이 유형과 최대 크기 변경은 현재 세션에서 UWF가 꺼져 있어야 합니다. " +
                    "아래 설정 중 일부는 다음 부팅에 적용되도록 예약됩니다.",
                    "Review carefully: overlay type and maximum size changes require UWF to be disabled in the current session. " +
                    "All listed settings are staged for the next boot where applicable.");
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-type " + overlayType.ToUpperInvariant()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-size " + sizeMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-warningthreshold " + warningMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-criticalthreshold " + criticalMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter enable"));
            AddVolumeCommands(plan, "protect", volumes);
            return plan;
        }

        public OperationPlan CreateVolumeProtectionPlan(VolumeSelection volumes, bool protect)
        {
            if (volumes == null || (!volumes.IsAll && volumes.Volumes.Count == 0))
            {
                throw new InvalidOperationException("At least one volume is required.");
            }
            if (!protect && volumes.IsAll)
            {
                throw new InvalidOperationException("Unprotect all is not supported by uwfmgr.exe.");
            }

            var actionText = protect ? UiText.T("볼륨 보호 ", "Protect volume ") : UiText.T("볼륨 보호 해제 ", "Unprotect volume ");
            var plan = new OperationPlan(actionText + volumes.DisplayText());
            plan.RequiresAdministrator = true;
            plan.Warning = protect
                ? UiText.T("필터가 켜져 있으면 다음 재시작 후 선택한 볼륨이 보호됩니다.", "The selected volumes will be protected after the next restart if the filter is enabled.")
                : UiText.T("다음 재시작 후 선택한 볼륨의 보호가 해제됩니다.", "The selected volumes will stop being protected after the next restart.");
            AddVolumeCommands(plan, protect ? "protect" : "unprotect", volumes);
            return plan;
        }

        public OperationPlan CreateDiskOverlayCleanupPlan(UwfSnapshot snapshot)
        {
            var plan = new OperationPlan(UiText.T("DISK 오버레이 공간 정리", "Clean DISK overlay space"));
            if (!CanChangeOverlayConfigNow(snapshot))
            {
                plan.Warning = UiText.T(
                    "현재 세션에서 UWF가 켜져 있거나 상태를 확인할 수 없습니다. 먼저 필터 끄기를 예약합니다. 재부팅 후 이 작업을 다시 실행하면 DISK 예약 공간을 RAM/1024MB 기준으로 되돌립니다.",
                    "UWF is enabled in the current session, or the current state is unknown. This first schedules the filter to turn off. After reboot, run this action again to release DISK overlay reservation by returning to RAM/1024MB.");
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
                return plan;
            }

            plan.Warning = UiText.T(
                "현재 세션에서 UWF가 꺼져 있어야 실행됩니다. DISK 오버레이 예약 파일을 회수하기 위해 다음 세션 오버레이를 RAM, 최소 1024MB, 기본 임계값으로 바꿉니다. 적용 후 재부팅하세요.",
                "This requires UWF to be disabled in the current session. It releases the DISK overlay reservation by staging RAM overlay, the 1024MB minimum size, and default thresholds for the next session. Reboot after applying.");
            AddDiskOverlayReleaseCommands(plan);
            return plan;
        }

        public OperationPlan CreateFullDisablePlan(UwfSnapshot snapshot)
        {
            var plan = new OperationPlan(UiText.T("UWF 완전 끄기", "UWF full off"));
            if (!CanChangeOverlayConfigNow(snapshot))
            {
                plan.Warning = UiText.T(
                    "현재 세션에서 UWF가 켜져 있거나 상태를 확인할 수 없습니다. 1단계로 필터 끄기만 예약합니다. 재부팅 후 다시 실행하면 보호 볼륨 해제와 DISK 오버레이 공간 정리를 마칩니다.",
                    "UWF is enabled in the current session, or the current state is unknown. Step 1 only schedules the filter to turn off. After reboot, run this again to unprotect volumes and clean DISK overlay reservation.");
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
                return plan;
            }

            plan.Warning = UiText.T(
                "UWF를 꺼진 상태로 고정하고, 보호 볼륨을 해제하고, 서비스 모드를 끄고, DISK 오버레이 예약 공간을 RAM/1024MB 기준으로 되돌립니다. 적용 후 재부팅하세요.",
                "Keeps UWF off, unprotects protected volumes, disables servicing mode, and releases DISK overlay reservation by returning to RAM/1024MB. Reboot after applying.");
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "servicing disable", 120000, true));
            AddVolumeUnprotectCommands(plan, snapshot);
            AddPersistentOverlayClearCommands(plan);
            AddDiskOverlayReleaseCommands(plan);
            return plan;
        }

        public OperationPlan CreateFullResetPlan(UwfSnapshot snapshot)
        {
            var plan = new OperationPlan(UiText.T("UWF 완전 초기화", "UWF full reset"));
            if (!CanChangeOverlayConfigNow(snapshot))
            {
                plan.Warning = UiText.T(
                    "현재 세션에서 UWF가 켜져 있거나 상태를 확인할 수 없습니다. 1단계로 필터 끄기만 예약합니다. 재부팅 후 다시 실행하면 UWF 초기화와 수동 정리를 진행합니다.",
                    "UWF is enabled in the current session, or the current state is unknown. Step 1 only schedules the filter to turn off. After reboot, run this again to reset UWF and apply manual cleanup.");
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
                return plan;
            }

            plan.Warning = UiText.T(
                "가능하면 UWF의 기본 reset-settings를 먼저 요청하고, 실패해도 수동 초기화를 계속합니다. 최종적으로 필터 끄기, 보호 볼륨 해제, 서비스 모드 끄기, DISK 오버레이 공간 정리를 예약합니다. 적용 후 재부팅하세요.",
                "Attempts UWF reset-settings first when available, then continues manual reset even if that command is unsupported. Final staged state turns the filter off, unprotects volumes, disables servicing, and cleans DISK overlay reservation. Reboot after applying.");
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter reset-settings", 120000, true));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "filter disable"));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "servicing disable", 120000, true));
            AddVolumeUnprotectCommands(plan, snapshot);
            AddPersistentOverlayClearCommands(plan);
            AddDiskOverlayReleaseCommands(plan);
            return plan;
        }

        public OperationPlan CreateSimplePlan(string title, string warning, string fileName, string args)
        {
            var plan = new OperationPlan(title);
            plan.RequiresAdministrator = true;
            plan.Warning = warning;
            plan.Commands.Add(new CommandSpec(fileName, args));
            return plan;
        }

        public OperationPlan CreateFileExclusionPlan(string path, bool add, string warning)
        {
            var title = add
                ? UiText.T("폴더/파일 예외 추가", "Add folder/file exclusion")
                : UiText.T("폴더/파일 예외 제거", "Remove folder/file exclusion");
            var plan = new OperationPlan(title);
            plan.RequiresAdministrator = true;
            plan.Warning = warning + Environment.NewLine + UiText.T(
                "이 장비에서는 uwfmgr.exe 파일 예외 명령이 권한 오류를 낼 수 있어 UWF WMI 공급자로 직접 적용합니다.",
                "This device can return access denied for uwfmgr.exe file exclusion commands, so this operation applies through the UWF WMI provider directly.");
            var action = add ? "add-exclusion" : "remove-exclusion";
            plan.Commands.Add(new CommandSpec("UWF WMI", "folder " + action + " " + Elevation.QuoteArgumentForCreateProcess(path)));
            return plan;
        }

        private static void AddVolumeCommands(OperationPlan plan, string action, VolumeSelection volumes)
        {
            if (volumes.IsAll)
            {
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "volume " + action + " all"));
                return;
            }

            for (int i = 0; i < volumes.Volumes.Count; i++)
            {
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "volume " + action + " " + volumes.Volumes[i]));
            }
        }

        private static bool CanChangeOverlayConfigNow(UwfSnapshot snapshot)
        {
            return snapshot != null && snapshot.FilterCurrentEnabled == false;
        }

        private static void AddDiskOverlayReleaseCommands(OperationPlan plan)
        {
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-type RAM"));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-size " + MinimumOverlaySizeMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-warningthreshold " + DefaultWarningThresholdMb.ToString()));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-criticalthreshold " + DefaultCriticalThresholdMb.ToString()));
        }

        private static void AddPersistentOverlayClearCommands(OperationPlan plan)
        {
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay set-persistent off", 120000, true));
            plan.Commands.Add(new CommandSpec("uwfmgr.exe", "overlay reset-persistentstate on", 120000, true));
        }

        private static void AddVolumeUnprotectCommands(OperationPlan plan, UwfSnapshot snapshot)
        {
            var volumes = GetKnownProtectedVolumes(snapshot);
            for (int i = 0; i < volumes.Count; i++)
            {
                plan.Commands.Add(new CommandSpec("uwfmgr.exe", "volume unprotect " + volumes[i], 120000, true));
            }
        }

        private static List<string> GetKnownProtectedVolumes(UwfSnapshot snapshot)
        {
            var volumes = new List<string>();
            if (snapshot == null)
            {
                return volumes;
            }

            AddUniqueVolumes(volumes, snapshot.CurrentProtectedVolumes);
            AddUniqueVolumes(volumes, snapshot.NextProtectedVolumes);
            return volumes;
        }

        private static void AddUniqueVolumes(List<string> target, List<string> source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                var volume = NormalizeVolumeToken(source[i]);
                if (String.IsNullOrEmpty(volume))
                {
                    continue;
                }

                bool exists = false;
                for (int j = 0; j < target.Count; j++)
                {
                    if (String.Equals(target[j], volume, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    target.Add(volume);
                }
            }
        }

        private static string NormalizeVolumeToken(string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return String.Empty;
            }

            volume = volume.Trim();
            if (volume.Length >= 2 && volume[1] == ':')
            {
                return Char.ToUpperInvariant(volume[0]) + ":";
            }

            return volume;
        }

        public CommandResult RunReadOnly(string args)
        {
            return runner.Run(ResolveExecutable("uwfmgr.exe"), args, 30000);
        }

        private CommandResult TryRunWmiFallback(CommandSpec command)
        {
            if (command == null || !String.Equals(command.FileName, "uwfmgr.exe", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var normalized = NormalizeCommandArguments(command.Arguments);
            if (String.Equals(normalized, "filter enable", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeUwfFilterMethod("Enable");
            }
            if (String.Equals(normalized, "filter disable", StringComparison.OrdinalIgnoreCase))
            {
                return InvokeUwfFilterMethod("Disable");
            }
            var parts = SplitCommandLine(command.Arguments);
            if (parts.Length == 3 &&
                String.Equals(parts[0], "file", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(parts[1], "add-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("AddExclusion", parts[2]);
                }
                if (String.Equals(parts[1], "remove-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("RemoveExclusion", parts[2]);
                }
            }
            if (parts.Length == 3 &&
                String.Equals(parts[0], "registry", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(parts[1], "add-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfRegistryExclusionMethod("AddExclusion", parts[2]);
                }
                if (String.Equals(parts[1], "remove-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfRegistryExclusionMethod("RemoveExclusion", parts[2]);
                }
            }
            if (parts.Length == 3 &&
                String.Equals(parts[0], "overlay", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(parts[1], "set-type", StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(parts[2], "RAM", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfOverlayConfigMethod("SetType", 0);
                }
                if (String.Equals(parts[2], "DISK", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfOverlayConfigMethod("SetType", 1);
                }
            }
            if (parts.Length == 3 &&
                String.Equals(parts[0], "overlay", StringComparison.OrdinalIgnoreCase))
            {
                uint value;
                if (UInt32.TryParse(parts[2], out value))
                {
                    if (String.Equals(parts[1], "set-size", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeUwfOverlayConfigMethod("SetMaximumSize", value);
                    }
                    if (String.Equals(parts[1], "set-warningthreshold", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeUwfOverlayMethod("SetWarningThreshold", value);
                    }
                    if (String.Equals(parts[1], "set-criticalthreshold", StringComparison.OrdinalIgnoreCase))
                    {
                        return InvokeUwfOverlayMethod("SetCriticalThreshold", value);
                    }
                }
            }

            return null;
        }

        private static string[] SplitCommandLine(string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return new string[0];
            }

            var parts = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < arguments.Length; i++)
            {
                char c = arguments[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && Char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Length = 0;
                    }
                    continue;
                }

                if (c == '\\' && inQuotes && i + 1 < arguments.Length && arguments[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }
            return parts.ToArray();
        }

        private static string[] SplitWhitespace(string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return new string[0];
            }

            return arguments.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizeCommandArguments(string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return String.Empty;
            }

            var parts = SplitCommandLine(arguments);
            if (parts.Length == 2)
            {
                return parts[0].ToLowerInvariant() + " " + parts[1].ToLowerInvariant();
            }

            return arguments.Trim();
        }

        private static CommandResult InvokeUwfFilterMethod(string methodName)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_Filter";
            result.Arguments = methodName + "()";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Filter"))
                {
                    using (obj)
                    {
                        using (var outParams = obj.InvokeMethod(methodName, (ManagementBaseObject)null, null))
                        {
                            if (outParams != null && outParams["ReturnValue"] != null)
                            {
                                result.ExitCode = unchecked((int)Convert.ToUInt32(outParams["ReturnValue"]));
                            }
                            else
                            {
                                result.ExitCode = 0;
                            }
                        }
                    }

                    result.Output = "UWF_Filter." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "UWF_Filter WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static CommandResult InvokeUwfOverlayConfigMethod(string methodName, uint value)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_OverlayConfig";
            result.Arguments = methodName + "(" + value.ToString() + ")";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_OverlayConfig"))
                {
                    using (obj)
                    {
                        if (GetNullableBool(obj, "CurrentSession") == true)
                        {
                            continue;
                        }

                        using (var inParams = obj.GetMethodParameters(methodName))
                        {
                            if (String.Equals(methodName, "SetType", StringComparison.OrdinalIgnoreCase))
                            {
                                inParams["type"] = value;
                            }
                            else
                            {
                                inParams["size"] = value;
                            }

                            using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                            {
                                result.ExitCode = GetReturnValue(outParams);
                            }
                        }
                    }

                    result.Output = "UWF_OverlayConfig." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "Next-session UWF_OverlayConfig WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static CommandResult InvokeUwfOverlayMethod(string methodName, uint value)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_Overlay";
            result.Arguments = methodName + "(" + value.ToString() + ")";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Overlay"))
                {
                    using (obj)
                    using (var inParams = obj.GetMethodParameters(methodName))
                    {
                        inParams["size"] = value;
                        using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                        {
                            result.ExitCode = GetReturnValue(outParams);
                        }
                    }

                    result.Output = "UWF_Overlay." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "UWF_Overlay WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static CommandResult InvokeUwfFileExclusionMethod(string methodName, string fileName)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_Volume";
            result.Arguments = methodName + "(" + fileName + ")";

            try
            {
                var volume = GetPathVolume(fileName);
                if (String.IsNullOrEmpty(volume))
                {
                    result.ExitCode = -1;
                    result.Error = "Could not determine the drive letter for the file exclusion path.";
                    return result;
                }

                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                var volumeRelativePath = GetVolumeRelativePath(fileName);
                if (String.IsNullOrEmpty(volumeRelativePath))
                {
                    result.ExitCode = -1;
                    result.Error = "Could not determine the volume-relative path for the file exclusion.";
                    return result;
                }

                using (var obj = GetUwfVolumeByDriveLetter(scope, volume, false))
                {
                    result.ExitCode = InvokeUwfVolumeFileMethod(obj, methodName, volumeRelativePath);
                }

                result.Output = "UWF_Volume(next session " + volume + ")." + methodName + "(" + volumeRelativePath + ") returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static int InvokeUwfVolumeFileMethod(ManagementObject obj, string methodName, string fileName)
        {
            using (var inParams = obj.GetMethodParameters(methodName))
            {
                inParams["FileName"] = fileName;
                using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                {
                    return GetReturnValue(outParams);
                }
            }
        }

        private static ManagementObject GetUwfVolumeByDriveLetter(ManagementScope scope, string volume, bool currentSession)
        {
            var path = BuildUwfVolumeObjectPath(volume, currentSession);
            var obj = new ManagementObject(scope, new ManagementPath(path), null);
            obj.Get();
            return obj;
        }

        internal static string BuildUwfVolumeObjectPath(string volume, bool currentSession)
        {
            return "UWF_Volume.CurrentSession=" + (currentSession ? "True" : "False") +
                ",DriveLetter=\"" + EscapeWmiKeyString(volume) + "\",VolumeName=\"\"";
        }

        private static string EscapeWmiKeyString(string value)
        {
            if (value == null)
            {
                return String.Empty;
            }
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static CommandResult InvokeUwfRegistryExclusionMethod(string methodName, string registryKey)
        {
            var result = new CommandResult();
            result.FileName = "WMI UWF_RegistryFilter";
            result.Arguments = methodName + "(" + registryKey + ")";

            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_RegistryFilter"))
                {
                    using (obj)
                    using (var inParams = obj.GetMethodParameters(methodName))
                    {
                        inParams["RegistryKey"] = registryKey;
                        using (var outParams = obj.InvokeMethod(methodName, inParams, null))
                        {
                            result.ExitCode = GetReturnValue(outParams);
                        }
                    }

                    result.Output = "UWF_RegistryFilter." + methodName + " returned " + CommandResult.FormatExitCode(result.ExitCode) + ".";
                    return result;
                }

                result.ExitCode = -1;
                result.Error = "UWF_RegistryFilter WMI object was not found.";
            }
            catch (Exception ex)
            {
                result.ExitCode = Marshal.GetHRForException(ex);
                result.Error = ex.Message;
            }

            return result;
        }

        private static string GetPathVolume(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return String.Empty;
            }

            try
            {
                var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
                if (fullPath.Length >= 2 && fullPath[1] == ':' && Char.IsLetter(fullPath[0]))
                {
                    return Char.ToUpperInvariant(fullPath[0]) + ":";
                }
            }
            catch
            {
            }

            return String.Empty;
        }

        internal static string GetVolumeRelativePath(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return String.Empty;
            }

            try
            {
                var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
                if (fullPath.Length >= 3 && fullPath[1] == ':' && fullPath[2] == '\\')
                {
                    return fullPath.Substring(2);
                }
            }
            catch
            {
            }

            return String.Empty;
        }

        private static int GetReturnValue(ManagementBaseObject outParams)
        {
            if (outParams != null && outParams["ReturnValue"] != null)
            {
                return unchecked((int)Convert.ToUInt32(outParams["ReturnValue"]));
            }

            return 0;
        }

        public string ExecutePlan(OperationPlan plan)
        {
            var output = new StringBuilder();
            output.AppendLine(plan.Title);
            output.AppendLine(plan.Warning);
            output.AppendLine();

            if (plan.RequiresAdministrator && !IsAdministrator())
            {
                output.AppendLine(UiText.T("상승된 관리자 권한이 확인되지 않아 변경 명령을 실행하지 않았습니다.",
                    "Elevated administrator rights were not confirmed, so no configuration command was executed."));
                output.AppendLine(UiText.T("앱을 관리자 권한으로 다시 실행한 뒤 작업을 다시 시도하세요.",
                    "Relaunch the app as administrator and try the operation again."));
                return output.ToString();
            }

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                var command = plan.Commands[i];
                output.AppendLine("> " + command.FileName + " " + command.Arguments);
                var result = IsUwfWmiCommand(command)
                    ? ExecuteUwfWmiCommand(command)
                    : runner.Run(ResolveExecutable(command.FileName), command.Arguments, command.TimeoutMilliseconds);
                output.AppendLine(result.ToDisplayText());
                bool failed = result.TimedOut || result.ExitCode != 0;
                if (failed && !IsUwfWmiCommand(command))
                {
                    var fallback = TryRunWmiFallback(command);
                    if (fallback != null)
                    {
                        output.AppendLine(UiText.T("uwfmgr.exe가 실패하여 UWF WMI 공급자로 한 번 더 시도합니다.",
                            "uwfmgr.exe failed, so retrying through the UWF WMI provider."));
                        output.AppendLine("> " + fallback.FileName + " " + fallback.Arguments);
                        output.AppendLine(fallback.ToDisplayText());
                        result = fallback;
                        failed = result.TimedOut || result.ExitCode != 0;
                    }
                }

                if (failed)
                {
                    if (command.ContinueOnFailure)
                    {
                        output.AppendLine(UiText.T("선택 명령이 실패했지만 다음 명령을 계속 실행합니다.", "Optional command failed; continuing with the next command."));
                        continue;
                    }
                    output.AppendLine(UiText.T("명령이 실패하여 중단했습니다.", "Stopped because the command failed."));
                    break;
                }
            }

            return output.ToString();
        }

        private static bool IsUwfWmiCommand(CommandSpec command)
        {
            return command != null && String.Equals(command.FileName, "UWF WMI", StringComparison.OrdinalIgnoreCase);
        }

        private static CommandResult ExecuteUwfWmiCommand(CommandSpec command)
        {
            var result = new CommandResult();
            result.FileName = command.FileName;
            result.Arguments = command.Arguments;

            var parts = SplitCommandLine(command.Arguments);
            if (parts.Length == 3 &&
                (String.Equals(parts[0], "folder", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(parts[0], "file", StringComparison.OrdinalIgnoreCase)))
            {
                if (String.Equals(parts[1], "add-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("AddExclusion", parts[2]);
                }
                if (String.Equals(parts[1], "remove-exclusion", StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeUwfFileExclusionMethod("RemoveExclusion", parts[2]);
                }
            }

            result.ExitCode = -1;
            result.Error = "Unsupported UWF WMI command.";
            return result;
        }

        public static bool IsAdministrator()
        {
            bool isAdministrator;
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                isAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }

            if (!isAdministrator)
            {
                return false;
            }

            bool elevated;
            if (TryGetProcessElevation(out elevated))
            {
                return elevated;
            }

            return isAdministrator;
        }

        private static bool TryGetProcessElevation(out bool elevated)
        {
            elevated = false;
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TokenQuery, out token))
                {
                    return false;
                }

                TokenElevation elevation;
                int returnLength;
                int size = Marshal.SizeOf(typeof(TokenElevation));
                if (!GetTokenInformation(token, TokenInformationClass.TokenElevation, out elevation, size, out returnLength))
                {
                    return false;
                }

                elevated = elevation.TokenIsElevated != 0;
                return true;
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    CloseHandle(token);
                }
            }
        }

        private static string GetOsCaption()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        using (obj)
                        {
                            var caption = Convert.ToString(obj["Caption"]);
                            if (!String.IsNullOrWhiteSpace(caption))
                            {
                                return caption;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return Environment.OSVersion.ToString();
        }

        private static bool IsLikelySupportedEdition(string caption)
        {
            if (String.IsNullOrEmpty(caption))
            {
                return false;
            }

            return caption.IndexOf("Enterprise", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   caption.IndexOf("Education", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   caption.IndexOf("IoT", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetUwfMgrPath()
        {
            return GetSystemToolPath("uwfmgr.exe");
        }

        private static string ResolveExecutable(string fileName)
        {
            if (String.Equals(fileName, "uwfmgr.exe", StringComparison.OrdinalIgnoreCase))
            {
                var full = GetUwfMgrPath();
                if (File.Exists(full))
                {
                    return full;
                }
            }

            if (String.Equals(fileName, "dism.exe", StringComparison.OrdinalIgnoreCase))
            {
                var full = GetSystemToolPath("dism.exe");
                if (File.Exists(full))
                {
                    return full;
                }
            }

            return fileName;
        }

        private static string GetSystemToolPath(string exeName)
        {
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                var sysnative = Path.Combine(windows, "Sysnative\\" + exeName);
                if (File.Exists(sysnative))
                {
                    return sysnative;
                }
            }

            var system32 = Path.Combine(windows, "System32\\" + exeName);
            if (File.Exists(system32))
            {
                return system32;
            }

            return system32;
        }

        private static void AppendCommand(StringBuilder report, string title, CommandResult result)
        {
            report.AppendLine("== " + title + " ==");
            report.AppendLine(result.ToDisplayText());
            report.AppendLine();
        }

        private static UwfSnapshot QuerySnapshot()
        {
            var snapshot = new UwfSnapshot();
            try
            {
                var scope = new ManagementScope(@"\\.\root\standardcimv2\embedded");
                scope.Connect();

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Filter"))
                {
                    using (obj)
                    {
                        snapshot.FilterCurrentEnabled = GetNullableBool(obj, "CurrentEnabled");
                        snapshot.FilterNextEnabled = GetNullableBool(obj, "NextEnabled");
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_OverlayConfig"))
                {
                    using (obj)
                    {
                        bool current = GetNullableBool(obj, "CurrentSession") == true;
                        if (current)
                        {
                            snapshot.CurrentOverlayType = OverlayTypeText(GetNullableInt(obj, "Type"));
                            snapshot.CurrentMaximumSizeMb = GetNullableInt(obj, "MaximumSize");
                        }
                        else
                        {
                            snapshot.NextOverlayType = OverlayTypeText(GetNullableInt(obj, "Type"));
                            snapshot.NextMaximumSizeMb = GetNullableInt(obj, "MaximumSize");
                        }
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Overlay"))
                {
                    using (obj)
                    {
                        snapshot.OverlayConsumptionMb = GetNullableInt(obj, "OverlayConsumption");
                        snapshot.AvailableSpaceMb = GetNullableInt(obj, "AvailableSpace");
                        snapshot.WarningThresholdMb = GetNullableInt(obj, "WarningOverlayThreshold");
                        snapshot.CriticalThresholdMb = GetNullableInt(obj, "CriticalOverlayThreshold");
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Servicing"))
                {
                    using (obj)
                    {
                        bool current = GetNullableBool(obj, "CurrentSession") == true;
                        bool? enabled = GetNullableBool(obj, "ServicingEnabled");
                        if (!enabled.HasValue)
                        {
                            enabled = GetNullableBool(obj, "ServiceEnabled");
                        }
                        if (current)
                        {
                            snapshot.ServicingCurrentEnabled = enabled;
                        }
                        else
                        {
                            snapshot.ServicingNextEnabled = enabled;
                        }
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_Volume"))
                {
                    using (obj)
                    {
                        var drive = Convert.ToString(obj["DriveLetter"]);
                        if (String.IsNullOrWhiteSpace(drive))
                        {
                            continue;
                        }
                        bool current = GetNullableBool(obj, "CurrentSession") == true;
                        bool isProtected = GetNullableBool(obj, "Protected") == true;
                        if (isProtected && current)
                        {
                            snapshot.CurrentProtectedVolumes.Add(drive);
                        }
                        if (isProtected && !current)
                        {
                            snapshot.NextProtectedVolumes.Add(drive);
                        }
                    }
                }

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_ExcludedFile"))
                {
                    using (obj)
                    {
                        AddUniqueText(snapshot.FileExclusions, Convert.ToString(obj["FileName"]));
                    }
                }

                AddFileExclusionsFromKnownVolumes(scope, snapshot);

                foreach (ManagementObject obj in QueryWmi(scope, "UWF_ExcludedRegistryKey"))
                {
                    using (obj)
                    {
                        AddUniqueText(snapshot.RegistryExclusions, Convert.ToString(obj["RegistryKey"]));
                    }
                }
            }
            catch (Exception ex)
            {
                snapshot.Error = ex.Message;
            }

            return snapshot;
        }

        private static void AddFileExclusionsFromKnownVolumes(ManagementScope scope, UwfSnapshot snapshot)
        {
            var volumes = GetCandidateDriveLetters(snapshot);
            for (int i = 0; i < volumes.Count; i++)
            {
                AddFileExclusionsFromVolume(scope, snapshot, volumes[i], false);
                AddFileExclusionsFromVolume(scope, snapshot, volumes[i], true);
            }
        }

        private static List<string> GetCandidateDriveLetters(UwfSnapshot snapshot)
        {
            var volumes = new List<string>();
            AddUniqueVolumes(volumes, snapshot.CurrentProtectedVolumes);
            AddUniqueVolumes(volumes, snapshot.NextProtectedVolumes);

            try
            {
                var drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    var name = drives[i].Name;
                    if (!String.IsNullOrEmpty(name) && name.Length >= 2 && name[1] == ':')
                    {
                        AddUniqueVolume(volumes, Char.ToUpperInvariant(name[0]) + ":");
                    }
                }
            }
            catch
            {
            }

            return volumes;
        }

        private static void AddUniqueVolume(List<string> target, string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return;
            }

            volume = NormalizeVolumeToken(volume);
            for (int i = 0; i < target.Count; i++)
            {
                if (String.Equals(target[i], volume, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            target.Add(volume);
        }

        private static void AddFileExclusionsFromVolume(ManagementScope scope, UwfSnapshot snapshot, string volume, bool currentSession)
        {
            try
            {
                using (var obj = GetUwfVolumeByDriveLetter(scope, volume, currentSession))
                using (var outParams = obj.InvokeMethod("GetExclusions", (ManagementBaseObject)null, null))
                {
                    if (outParams == null || outParams["ExcludedFiles"] == null)
                    {
                        return;
                    }

                    var exclusions = outParams["ExcludedFiles"] as Array;
                    if (exclusions == null)
                    {
                        return;
                    }

                    foreach (var exclusion in exclusions)
                    {
                        var item = exclusion as ManagementBaseObject;
                        var fileName = item == null ? Convert.ToString(exclusion) : Convert.ToString(item["FileName"]);
                        AddUniqueText(snapshot.FileExclusions, ToFullPathFromVolume(volume, fileName));
                    }
                }
            }
            catch
            {
            }
        }

        internal static string ToFullPathFromVolume(string volume, string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
            {
                return String.Empty;
            }

            fileName = fileName.Trim();
            if (fileName.Length >= 2 && fileName[1] == ':')
            {
                return fileName;
            }

            if (String.IsNullOrWhiteSpace(volume))
            {
                return fileName;
            }

            volume = NormalizeVolumeToken(volume);
            if (fileName.StartsWith("\\", StringComparison.Ordinal))
            {
                return volume + fileName;
            }

            return volume + "\\" + fileName;
        }

        private static List<ManagementObject> QueryWmi(ManagementScope scope, string className)
        {
            var results = new List<ManagementObject>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM " + className)))
                using (var objects = searcher.Get())
                {
                    foreach (ManagementObject obj in objects)
                    {
                        results.Add(obj);
                    }
                }
            }
            catch
            {
                for (int i = 0; i < results.Count; i++)
                {
                    results[i].Dispose();
                }
                throw;
            }
            return results;
        }

        private static void AddUniqueText(List<string> target, string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return;
            }

            value = value.Trim();
            for (int i = 0; i < target.Count; i++)
            {
                if (String.Equals(target[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            target.Add(value);
        }

        private static bool? GetNullableBool(ManagementObject obj, string property)
        {
            try
            {
                var value = obj[property];
                if (value == null)
                {
                    return null;
                }
                return Convert.ToBoolean(value);
            }
            catch
            {
                return null;
            }
        }

        private static int? GetNullableInt(ManagementObject obj, string property)
        {
            try
            {
                var value = obj[property];
                if (value == null)
                {
                    return null;
                }
                return Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        private static string OverlayTypeText(int? type)
        {
            if (!type.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return type.Value == 1 ? "DISK" : "RAM";
        }
    }
}
