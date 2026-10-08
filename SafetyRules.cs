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
    internal static class SafetyRules
    {
        public static ValidationResult ValidateFileExclusion(string path, bool adding)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return Block(UiText.T("파일 또는 폴더의 전체 경로를 입력하세요.", "Enter a fully qualified file or folder path."));
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
            }
            catch (Exception ex)
            {
                return Block(UiText.T("경로 오류: ", "Invalid path: ") + ex.Message);
            }

            if (!Path.IsPathRooted(fullPath) || fullPath.Length < 3 || fullPath[1] != ':')
            {
                return Block(UiText.T("C:\\ProgramData\\Vendor 같은 드라이브 포함 전체 경로를 입력하세요.", "Use a fully qualified drive path such as C:\\ProgramData\\Vendor."));
            }

            var normalized = NormalizePath(fullPath);
            var root = Char.ToUpperInvariant(normalized[0]) + ":\\";
            if (String.Equals(normalized, root, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("C: 또는 C:\\ 같은 볼륨 루트는 예외로 지정할 수 없습니다.", "Volume roots such as C: or C:\\ are unsupported as exclusions."));
            }

            string drive = root;
            string windows = NormalizePath(Path.Combine(drive, "Windows"));
            string system32 = NormalizePath(Path.Combine(windows, "System32"));
            string drivers = NormalizePath(Path.Combine(system32, "Drivers"));

            if (EqualsPath(normalized, windows) || EqualsPath(normalized, system32) || EqualsPath(normalized, drivers))
            {
                return Block(UiText.T("Windows, System32, System32\\Drivers 자체는 예외로 지정하지 마세요. 안전한 하위 폴더나 특정 파일만 지정하세요.",
                    "Do not exclude Windows, System32, or System32\\Drivers itself. Use a specific safe subfolder or file only."));
            }

            string[] blockedExact = new string[]
            {
                Path.Combine(system32, "config\\DEFAULT"),
                Path.Combine(system32, "config\\SAM"),
                Path.Combine(system32, "config\\SECURITY"),
                Path.Combine(system32, "config\\SOFTWARE"),
                Path.Combine(system32, "config\\SYSTEM"),
                Path.Combine(windows, "BOOTSTAT.DAT"),
                Path.Combine(drive, "Boot\\BOOTSTAT.DAT"),
                Path.Combine(drive, "EFI\\Microsoft\\Boot\\BOOTSTAT.DAT"),
                Path.Combine(drive, "pagefile.sys"),
                Path.Combine(drive, "swapfile.sys"),
                Path.Combine(drive, "hiberfil.sys")
            };

            for (int i = 0; i < blockedExact.Length; i++)
            {
                if (EqualsPath(normalized, blockedExact[i]))
                {
                    return Block(UiText.T("이 경로는 UWF 예외로 지원되지 않거나 안전하지 않습니다.", "This path is unsupported or unsafe for UWF exclusions."));
                }
            }

            if (normalized.EndsWith("\\NTUSER.DAT", StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("NTUSER.DAT 같은 사용자 프로필 레지스트리 하이브는 예외로 지정하면 안 됩니다.",
                    "User profile registry hives such as NTUSER.DAT must not be excluded."));
            }

            if (adding)
            {
                if (!File.Exists(normalized) && !Directory.Exists(normalized))
                {
                    var parent = Path.GetDirectoryName(normalized);
                    if (String.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                    {
                        return Block(UiText.T("경로 또는 상위 폴더가 없습니다. 먼저 폴더를 만든 뒤 예외를 추가하세요.",
                            "The path or its parent folder does not exist. Create the folder first, then add the exclusion."));
                    }
                }
            }

            return Allow(UiText.T("허용 가능한 경로입니다. 예외는 소량의 설정/데이터 보존용이며 오버레이 사용량을 줄이는 기능이 아닙니다.",
                "Allowed path. Remember: exclusions persist small configuration data; they do not reduce overlay consumption."));
        }

        public static ValidationResult ValidateRegistryExclusion(string key)
        {
            var shape = ValidateRegistryKeyShape(key);
            if (!shape.Allowed)
            {
                return shape;
            }

            var normalized = NormalizeRegistryKey(key);
            if (String.Equals(normalized, @"HKEY_LOCAL_MACHINE\SECURITY\Policy\Secrets\$MACHINE.ACC", StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("머신 계정 비밀 키는 예외로 지정하지 마세요.", "Do not exclude the machine account secret."));
            }

            string[] allowedRoots = new string[]
            {
                @"HKEY_LOCAL_MACHINE\BCD00000000",
                @"HKEY_LOCAL_MACHINE\SYSTEM",
                @"HKEY_LOCAL_MACHINE\SOFTWARE",
                @"HKEY_LOCAL_MACHINE\SAM",
                @"HKEY_LOCAL_MACHINE\SECURITY",
                @"HKEY_LOCAL_MACHINE\COMPONENTS"
            };

            for (int i = 0; i < allowedRoots.Length; i++)
            {
                if (IsSameOrChild(normalized, allowedRoots[i]) && !String.Equals(normalized, allowedRoots[i], StringComparison.OrdinalIgnoreCase))
                {
                    return Allow(UiText.T("허용 가능한 레지스트리 키입니다. 이 키 아래의 모든 하위 키도 UWF 필터링을 우회합니다.",
                        "Allowed registry key. All subkeys below this key will also bypass UWF filtering."));
                }
            }

            return Block(UiText.T("레지스트리 예외는 지원되는 HKEY_LOCAL_MACHINE 루트 아래의 하위 키여야 합니다.",
                "Registry exclusions must be subkeys under supported HKEY_LOCAL_MACHINE roots."));
        }

        public static ValidationResult ValidateRegistryKeyShape(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
            {
                return Block(UiText.T("레지스트리 키 경로를 입력하세요.", "Enter a registry key path."));
            }

            var normalized = NormalizeRegistryKey(key);
            if (!normalized.StartsWith(@"HKEY_LOCAL_MACHINE\", StringComparison.OrdinalIgnoreCase))
            {
                return Block(UiText.T("HKLM / HKEY_LOCAL_MACHINE 레지스트리 키를 사용하세요.", "Use an HKLM / HKEY_LOCAL_MACHINE registry key."));
            }

            return Allow(UiText.T("레지스트리 키 형식이 유효합니다.", "Registry key shape is valid."));
        }

        public static string NormalizeRegistryKey(string key)
        {
            key = key.Trim().TrimEnd('\\');
            if (key.StartsWith(@"HKLM\", StringComparison.OrdinalIgnoreCase))
            {
                key = @"HKEY_LOCAL_MACHINE\" + key.Substring(5);
            }
            return key;
        }

        private static bool IsSameOrChild(string path, string root)
        {
            return String.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd('\\');
        }

        private static bool EqualsPath(string left, string right)
        {
            return String.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
        }

        private static ValidationResult Block(string message)
        {
            return new ValidationResult(false, message);
        }

        private static ValidationResult Allow(string message)
        {
            return new ValidationResult(true, message);
        }
    }
}
