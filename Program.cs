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
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && String.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
            {
                return SelfTest.Run();
            }

            OperationPlan pendingPlan = null;
            if (args != null && args.Length > 0 && String.Equals(args[0], "--run-plan", StringComparison.OrdinalIgnoreCase))
            {
                string error = UiText.T("작업 데이터가 올바르지 않습니다.", "The operation payload is invalid.");
                if (args.Length != 2 || !OperationPlan.TryFromPayload(args[1], out pendingPlan, out error))
                {
                    MessageBox.Show(
                        UiText.T("관리자 작업 요청을 읽을 수 없습니다.\r\n\r\n" + error,
                            "The elevated operation request could not be read.\r\n\r\n" + error),
                        UiText.T("작업 요청 오류", "Invalid operation request"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return 2;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                CrashReporter.Report(e.Exception, true);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                CrashReporter.Report(e.ExceptionObject as Exception, false);
            };
            Application.Run(new MainForm(pendingPlan));
            return 0;
        }
    }

    internal static class CrashReporter
    {
        public static void Report(Exception exception, bool showMessage)
        {
            string logPath = String.Empty;
            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PortableUwfManager", "logs");
                Directory.CreateDirectory(directory);
                logPath = Path.Combine(directory, "crash.log");
                File.AppendAllText(logPath,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]" + Environment.NewLine +
                    (exception == null ? "Unknown unhandled exception." : exception.ToString()) + Environment.NewLine + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch
            {
            }

            if (showMessage)
            {
                MessageBox.Show(
                    UiText.T("예기치 않은 오류가 발생했습니다.\r\n오류 기록: ", "An unexpected error occurred.\r\nLog: ") +
                        (String.IsNullOrEmpty(logPath) ? UiText.T("기록하지 못했습니다.", "not available.") : logPath),
                    UiText.T("예기치 않은 오류", "Unexpected error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    internal static class Elevation
    {
        public static bool TryRelaunchCurrentProcessAsAdministrator(string[] args, out string error)
        {
            error = String.Empty;
            try
            {
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                var info = new ProcessStartInfo(exe);
                info.UseShellExecute = true;
                info.Verb = "runas";
                info.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                info.Arguments = BuildArgumentString(args);
                Process.Start(info);
                return true;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    error = UiText.T("사용자가 UAC 승인을 취소했습니다.", "The UAC elevation prompt was canceled.");
                }
                else
                {
                    error = ex.Message;
                }
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryRelaunchCurrentProcessAsAdministrator(OperationPlan pendingPlan, out string error)
        {
            if (pendingPlan == null)
            {
                error = UiText.T("다시 실행할 작업이 없습니다.", "There is no operation to resume.");
                return false;
            }

            var payload = pendingPlan.ToPayload();
            if (payload.Length > 30000)
            {
                error = UiText.T("작업 계획이 너무 커서 관리자 프로세스로 전달할 수 없습니다.",
                    "The operation plan is too large to pass to the elevated process.");
                return false;
            }

            return TryRelaunchCurrentProcessAsAdministrator(new[] { "--run-plan", payload }, out error);
        }

        public static string[] GetCurrentProcessArguments()
        {
            var all = Environment.GetCommandLineArgs();
            if (all == null || all.Length <= 1)
            {
                return new string[0];
            }

            var args = new string[all.Length - 1];
            Array.Copy(all, 1, args, 0, args.Length);
            return args;
        }

        public static string BuildArgumentString(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return String.Empty;
            }

            var text = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }
                text.Append(QuoteArgumentForCreateProcess(args[i]));
            }
            return text.ToString();
        }

        internal static string QuoteArgumentForCreateProcess(string argument)
        {
            if (String.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            bool needsQuotes = false;
            for (int i = 0; i < argument.Length; i++)
            {
                if (Char.IsWhiteSpace(argument[i]) || argument[i] == '"')
                {
                    needsQuotes = true;
                    break;
                }
            }

            if (!needsQuotes)
            {
                return argument;
            }

            var quoted = new StringBuilder();
            quoted.Append('"');
            int backslashCount = 0;
            for (int i = 0; i < argument.Length; i++)
            {
                char c = argument[i];
                if (c == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (c == '"')
                {
                    quoted.Append('\\', backslashCount * 2 + 1);
                    quoted.Append('"');
                    backslashCount = 0;
                    continue;
                }

                if (backslashCount > 0)
                {
                    quoted.Append('\\', backslashCount);
                    backslashCount = 0;
                }
                quoted.Append(c);
            }

            if (backslashCount > 0)
            {
                quoted.Append('\\', backslashCount * 2);
            }
            quoted.Append('"');
            return quoted.ToString();
        }
    }
}
