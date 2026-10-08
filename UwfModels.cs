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
    internal sealed class UwfStatus
    {
        public bool IsAdministrator;
        public bool UwfToolExists;
        public bool IsLikelySupportedEdition;
        public string UwfToolPath;
        public string OsCaption;
        public string Report;
        public UwfSnapshot Snapshot;
        public long TotalPhysicalMemoryMb;
        public long SystemVolumeFreeSpaceMb;
    }

    internal sealed class UwfSnapshot
    {
        public bool? FilterCurrentEnabled;
        public bool? FilterNextEnabled;
        public string CurrentOverlayType = UiText.T("확인 불가", "Unknown");
        public string NextOverlayType = UiText.T("확인 불가", "Unknown");
        public int? CurrentMaximumSizeMb;
        public int? NextMaximumSizeMb;
        public int? OverlayConsumptionMb;
        public int? AvailableSpaceMb;
        public int? WarningThresholdMb;
        public int? CriticalThresholdMb;
        public bool? ServicingCurrentEnabled;
        public bool? ServicingNextEnabled;
        public readonly List<string> CurrentProtectedVolumes = new List<string>();
        public readonly List<string> NextProtectedVolumes = new List<string>();
        public readonly List<string> FileExclusions = new List<string>();
        public readonly List<string> RegistryExclusions = new List<string>();
        public string Error;

        public bool HasPendingChanges()
        {
            if (FilterCurrentEnabled != FilterNextEnabled)
            {
                return true;
            }
            if (!String.Equals(CurrentOverlayType, NextOverlayType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (CurrentMaximumSizeMb != NextMaximumSizeMb)
            {
                return true;
            }
            if (ServicingCurrentEnabled != ServicingNextEnabled)
            {
                return true;
            }
            if (!String.Equals(CurrentProtectedVolumesText(), NextProtectedVolumesText(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public bool CanDeterminePendingChanges()
        {
            return String.IsNullOrEmpty(Error) &&
                FilterCurrentEnabled.HasValue && FilterNextEnabled.HasValue &&
                !String.Equals(CurrentOverlayType, UiText.T("확인 불가", "Unknown"), StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(NextOverlayType, UiText.T("확인 불가", "Unknown"), StringComparison.OrdinalIgnoreCase) &&
                CurrentMaximumSizeMb.HasValue && NextMaximumSizeMb.HasValue &&
                ServicingCurrentEnabled.HasValue && ServicingNextEnabled.HasValue;
        }

        public bool HasObservedState()
        {
            if (!String.IsNullOrEmpty(Error))
            {
                return false;
            }

            return FilterCurrentEnabled.HasValue ||
                FilterNextEnabled.HasValue ||
                CurrentMaximumSizeMb.HasValue ||
                NextMaximumSizeMb.HasValue ||
                OverlayConsumptionMb.HasValue ||
                AvailableSpaceMb.HasValue ||
                WarningThresholdMb.HasValue ||
                CriticalThresholdMb.HasValue ||
                ServicingCurrentEnabled.HasValue ||
                ServicingNextEnabled.HasValue ||
                CurrentProtectedVolumes.Count > 0 ||
                NextProtectedVolumes.Count > 0 ||
                FileExclusions.Count > 0 ||
                RegistryExclusions.Count > 0;
        }

        public string CurrentProtectedVolumesText()
        {
            return JoinVolumes(CurrentProtectedVolumes);
        }

        public string NextProtectedVolumesText()
        {
            return JoinVolumes(NextProtectedVolumes);
        }

        public string FileExclusionsText()
        {
            return JoinItems(FileExclusions);
        }

        public string RegistryExclusionsText()
        {
            return JoinItems(RegistryExclusions);
        }

        public int GetOverlayUsagePercent()
        {
            int? maximum = CurrentMaximumSizeMb.HasValue ? CurrentMaximumSizeMb : NextMaximumSizeMb;
            if (!OverlayConsumptionMb.HasValue || !maximum.HasValue || maximum.Value <= 0)
            {
                return 0;
            }
            return (int)Math.Round((double)OverlayConsumptionMb.Value * 100.0 / (double)maximum.Value);
        }

        public string GetOverlayUsagePercentText()
        {
            int percent = GetOverlayUsagePercent();
            if (percent <= 0 && !OverlayConsumptionMb.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return percent.ToString() + "%";
        }

        public string ToReport()
        {
            var report = new StringBuilder();
            if (!String.IsNullOrEmpty(Error))
            {
                report.AppendLine(UiText.T("WMI 오류: ", "WMI error: ") + Error);
                return report.ToString();
            }

            report.AppendLine("UWF_Filter: CurrentEnabled=" + FormatBool(FilterCurrentEnabled) + ", NextEnabled=" + FormatBool(FilterNextEnabled));
            report.AppendLine("UWF_OverlayConfig: CurrentType=" + CurrentOverlayType + ", NextType=" + NextOverlayType +
                ", CurrentMaximumSize=" + FormatMb(CurrentMaximumSizeMb) + ", NextMaximumSize=" + FormatMb(NextMaximumSizeMb));
            report.AppendLine("UWF_Overlay: Consumption=" + FormatMb(OverlayConsumptionMb) + ", AvailableSpace=" + FormatMb(AvailableSpaceMb) +
                ", Warning=" + FormatMb(WarningThresholdMb) + ", Critical=" + FormatMb(CriticalThresholdMb));
            report.AppendLine("UWF_Volume: CurrentProtected=" + CurrentProtectedVolumesText() + ", NextProtected=" + NextProtectedVolumesText());
            report.AppendLine("UWF_ExcludedFile: " + FileExclusionsText());
            report.AppendLine("UWF_ExcludedRegistryKey: " + RegistryExclusionsText());
            report.AppendLine("UWF_Servicing: Current=" + FormatBool(ServicingCurrentEnabled) + ", Next=" + FormatBool(ServicingNextEnabled));
            report.AppendLine("PendingRebootOrNextSessionChanges=" + (HasPendingChanges() ? "Yes" : "No"));
            return report.ToString();
        }

        private static string JoinVolumes(List<string> volumes)
        {
            return JoinItems(volumes);
        }

        private static string JoinItems(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return UiText.T("없음", "None");
            }
            return String.Join(", ", values.ToArray());
        }

        private static string FormatBool(bool? value)
        {
            if (!value.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return value.Value ? UiText.T("켜짐", "On") : UiText.T("꺼짐", "Off");
        }

        private static string FormatMb(int? value)
        {
            if (!value.HasValue)
            {
                return UiText.T("확인 불가", "Unknown");
            }
            return value.Value.ToString() + " MB";
        }
    }

    internal sealed class OperationPlan
    {
        public readonly string Title;
        public readonly List<CommandSpec> Commands;
        public string Warning;
        public bool RequiresAdministrator;

        public OperationPlan(string title)
        {
            Title = title;
            Warning = String.Empty;
            Commands = new List<CommandSpec>();
            RequiresAdministrator = true;
        }

        public string ToPayload()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(1);
                writer.Write(Title ?? String.Empty);
                writer.Write(Warning ?? String.Empty);
                writer.Write(RequiresAdministrator);
                writer.Write(Commands.Count);
                for (int i = 0; i < Commands.Count; i++)
                {
                    writer.Write(Commands[i].FileName ?? String.Empty);
                    writer.Write(Commands[i].Arguments ?? String.Empty);
                    writer.Write(Commands[i].TimeoutMilliseconds);
                    writer.Write(Commands[i].ContinueOnFailure);
                }
                writer.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static bool TryFromPayload(string payload, out OperationPlan plan, out string error)
        {
            plan = null;
            error = UiText.T("작업 데이터가 올바르지 않습니다.", "The operation payload is invalid.");
            if (String.IsNullOrWhiteSpace(payload) || payload.Length > 30000)
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(payload);
                if (bytes.Length > 22000)
                {
                    return false;
                }

                using (var stream = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadInt32() != 1)
                    {
                        return false;
                    }

                    var title = reader.ReadString();
                    var warning = reader.ReadString();
                    bool requiresAdministrator = reader.ReadBoolean();
                    int count = reader.ReadInt32();
                    if (title.Length > 1024 || warning.Length > 8192 || !requiresAdministrator || count < 1 || count > 128)
                    {
                        return false;
                    }

                    var result = new OperationPlan(title);
                    result.Warning = warning;
                    result.RequiresAdministrator = true;
                    for (int i = 0; i < count; i++)
                    {
                        var fileName = reader.ReadString();
                        var arguments = reader.ReadString();
                        int timeout = reader.ReadInt32();
                        bool continueOnFailure = reader.ReadBoolean();
                        if (arguments.Length > 16000 || timeout < 1 || timeout > 1800000 || !IsAllowedCommand(fileName, arguments))
                        {
                            return false;
                        }
                        result.Commands.Add(new CommandSpec(fileName, arguments, timeout, continueOnFailure));
                    }

                    if (stream.Position != stream.Length)
                    {
                        return false;
                    }

                    plan = result;
                    error = String.Empty;
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = UiText.T("작업 데이터를 읽지 못했습니다: ", "Could not read operation data: ") + ex.Message;
                return false;
            }
        }

        private static bool IsAllowedCommand(string fileName, string arguments)
        {
            if (String.Equals(fileName, "uwfmgr.exe", StringComparison.OrdinalIgnoreCase))
            {
                return !String.IsNullOrWhiteSpace(arguments);
            }
            if (String.Equals(fileName, "dism.exe", StringComparison.OrdinalIgnoreCase))
            {
                return String.Equals(arguments,
                    "/Online /Enable-Feature /FeatureName:Client-UnifiedWriteFilter /NoRestart",
                    StringComparison.OrdinalIgnoreCase);
            }
            if (String.Equals(fileName, "UWF WMI", StringComparison.OrdinalIgnoreCase))
            {
                return arguments.StartsWith("folder add-exclusion ", StringComparison.OrdinalIgnoreCase) ||
                       arguments.StartsWith("folder remove-exclusion ", StringComparison.OrdinalIgnoreCase) ||
                       arguments.StartsWith("file add-exclusion ", StringComparison.OrdinalIgnoreCase) ||
                       arguments.StartsWith("file remove-exclusion ", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public string ToDisplayText()
        {
            var text = new StringBuilder();
            text.AppendLine(Title);
            text.AppendLine();
            if (!String.IsNullOrEmpty(Warning))
            {
                text.AppendLine(Warning);
                text.AppendLine();
            }
            text.AppendLine(UiText.T("실행 명령:", "Commands:"));
            for (int i = 0; i < Commands.Count; i++)
            {
                var commandText = "  " + Commands[i].FileName + " " + Commands[i].Arguments;
                if (Commands[i].ContinueOnFailure)
                {
                    commandText += UiText.T(" (실패해도 계속)", " (continue on failure)");
                }
                text.AppendLine(commandText);
            }
            text.AppendLine();
            text.AppendLine(UiText.T("계속할까요?", "Continue?"));
            return text.ToString();
        }
    }

    internal sealed class CommandSpec
    {
        public readonly string FileName;
        public readonly string Arguments;
        public readonly int TimeoutMilliseconds;
        public readonly bool ContinueOnFailure;

        public CommandSpec(string fileName, string arguments)
            : this(fileName, arguments, 120000)
        {
        }

        public CommandSpec(string fileName, string arguments, int timeoutMilliseconds)
            : this(fileName, arguments, timeoutMilliseconds, false)
        {
        }

        public CommandSpec(string fileName, string arguments, int timeoutMilliseconds, bool continueOnFailure)
        {
            FileName = fileName;
            Arguments = arguments;
            TimeoutMilliseconds = timeoutMilliseconds;
            ContinueOnFailure = continueOnFailure;
        }
    }

    internal sealed class CommandRunner
    {
        public CommandResult Run(string fileName, string arguments, int timeoutMilliseconds)
        {
            var result = new CommandResult();
            result.FileName = fileName;
            result.Arguments = arguments;
            var output = new StringBuilder();
            var error = new StringBuilder();

            try
            {
                var info = new ProcessStartInfo();
                info.FileName = fileName;
                info.Arguments = arguments;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                using (var process = new Process())
                {
                    process.StartInfo = info;
                    process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                    {
                        if (e.Data != null)
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutMilliseconds))
                    {
                        result.TimedOut = true;
                        result.ExitCode = -1;
                        try
                        {
                            process.Kill();
                            if (process.WaitForExit(5000))
                            {
                                process.WaitForExit();
                                result.ExitCode = process.ExitCode;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        process.WaitForExit();
                        result.ExitCode = process.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ExitCode = -1;
                error.AppendLine(ex.Message);
            }

            result.Output = output.ToString();
            result.Error = error.ToString();
            return result;
        }
    }

    internal sealed class CommandResult
    {
        private const int HResultAccessDenied = unchecked((int)0x80070005);

        public string FileName;
        public string Arguments;
        public int ExitCode;
        public string Output;
        public string Error;
        public bool TimedOut;

        public string ToDisplayText()
        {
            var text = new StringBuilder();
            text.AppendLine(UiText.T("종료 코드: ", "ExitCode: ") + FormatExitCode(ExitCode) + (TimedOut ? UiText.T(" (시간 초과)", " (timed out)") : String.Empty));
            var hint = DecodeExitCode(ExitCode);
            if (!String.IsNullOrEmpty(hint))
            {
                text.AppendLine(UiText.T("해석: ", "Meaning: ") + hint);
            }
            if (!String.IsNullOrWhiteSpace(Output))
            {
                text.AppendLine("[stdout]");
                text.AppendLine(Output.TrimEnd());
            }
            if (!String.IsNullOrWhiteSpace(Error))
            {
                text.AppendLine("[stderr]");
                text.AppendLine(Error.TrimEnd());
            }
            return text.ToString();
        }

        public static string FormatExitCode(int exitCode)
        {
            if (exitCode < 0)
            {
                return exitCode.ToString() + " (" + ToHResultHex(exitCode) + ")";
            }

            return exitCode.ToString();
        }

        public static string DecodeExitCode(int exitCode)
        {
            if (exitCode == 0)
            {
                return String.Empty;
            }

            if (exitCode == HResultAccessDenied)
            {
                return UiText.T(
                    "권한 거부(E_ACCESSDENIED)입니다. WMI 상태 스냅샷이 정상이라면 대시보드는 WMI 값을 기준으로 보고, uwfmgr.exe CLI 상세 출력만 보조 진단으로 취급하세요.",
                    "Access denied (E_ACCESSDENIED). If the WMI snapshot is populated, treat the dashboard as WMI-backed and the uwfmgr.exe CLI output as supplemental diagnostics.");
            }

            if (exitCode < 0)
            {
                return UiText.T("Windows HRESULT 오류입니다. 위 16진수 코드로 권한, 정책, 실행 비트수 문제를 확인하세요.",
                    "Windows HRESULT failure. Use the hexadecimal code above to check permissions, policy, and process bitness.");
            }

            return String.Empty;
        }

        private static string ToHResultHex(int exitCode)
        {
            return "0x" + ((uint)exitCode).ToString("X8");
        }
    }

    internal sealed class ValidationResult
    {
        public readonly bool Allowed;
        public readonly string Message;

        public ValidationResult(bool allowed, string message)
        {
            Allowed = allowed;
            Message = message;
        }
    }
}
