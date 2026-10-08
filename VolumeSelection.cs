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
    internal sealed class VolumeSelection
    {
        public bool IsAll;
        public readonly List<string> Volumes = new List<string>();

        public string DisplayText()
        {
            if (IsAll)
            {
                return "all";
            }
            return String.Join(", ", Volumes.ToArray());
        }
    }

    internal static class VolumeSelectionParser
    {
        public static bool TryParse(string text, bool allowAll, out VolumeSelection selection, out string error)
        {
            selection = null;
            error = String.Empty;

            if (String.IsNullOrWhiteSpace(text))
            {
                error = UiText.T("C: 또는 C:,D: 같은 드라이브 문자를 입력하세요.", "Use drive letters such as C: or C:,D:.");
                return false;
            }

            var tokens = text.Split(new char[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                error = UiText.T("C: 또는 C:,D: 같은 드라이브 문자를 입력하세요.", "Use drive letters such as C: or C:,D:.");
                return false;
            }

            var parsed = new VolumeSelection();
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (String.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
                {
                    if (!allowAll)
                    {
                        error = UiText.T("all은 볼륨 보호에만 사용할 수 있습니다. 보호 해제는 C:,D:처럼 볼륨을 직접 지정하세요.",
                            "all is supported only for protecting volumes. For unprotect, specify volumes such as C:,D:.");
                        return false;
                    }
                    if (tokens.Length > 1)
                    {
                        error = UiText.T("all은 다른 볼륨과 함께 입력할 수 없습니다.", "all cannot be combined with other volumes.");
                        return false;
                    }
                    parsed.IsAll = true;
                    selection = parsed;
                    return true;
                }

                var volume = NormalizeVolume(token);
                if (volume == null)
                {
                    error = UiText.T("볼륨은 C: 또는 D: 형식이어야 합니다. 여러 개는 C:,D:처럼 입력하세요.",
                        "Volumes must be in C: or D: form. Enter multiple volumes as C:,D:.");
                    return false;
                }

                AddUnique(parsed.Volumes, volume);
            }

            if (parsed.Volumes.Count == 0)
            {
                error = UiText.T("보호할 볼륨을 입력하세요.", "Enter at least one volume.");
                return false;
            }

            selection = parsed;
            return true;
        }

        private static string NormalizeVolume(string volume)
        {
            if (String.IsNullOrWhiteSpace(volume))
            {
                return null;
            }

            volume = volume.Trim();
            if (volume.Length == 1 && Char.IsLetter(volume[0]))
            {
                return Char.ToUpperInvariant(volume[0]) + ":";
            }

            if (volume.Length == 2 && Char.IsLetter(volume[0]) && volume[1] == ':')
            {
                return Char.ToUpperInvariant(volume[0]) + ":";
            }

            return null;
        }

        private static void AddUnique(List<string> volumes, string volume)
        {
            for (int i = 0; i < volumes.Count; i++)
            {
                if (String.Equals(volumes[i], volume, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            volumes.Add(volume);
        }
    }

    internal static class TextBoxCompat
    {
        private const int EmSetCueBanner = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public static void PlaceholderTextSafe(this TextBox textBox, string text)
        {
            if (textBox.IsHandleCreated)
            {
                SendMessage(textBox.Handle, EmSetCueBanner, IntPtr.Zero, text);
                return;
            }

            textBox.HandleCreated += delegate
            {
                SendMessage(textBox.Handle, EmSetCueBanner, IntPtr.Zero, text);
            };
        }
    }
}
