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
    internal enum UiLanguage
    {
        Korean,
        English
    }

    internal static class UiText
    {
        public static UiLanguage Current = UiLanguage.Korean;

        public static string T(string ko, string en)
        {
            return Current == UiLanguage.Korean ? ko : en;
        }
    }
}
