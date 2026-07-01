using System;
using System.Runtime.InteropServices;
using System.Text;

namespace myseq
{
    /// <summary>
    /// Small classes with static methods, and have little to no impact on experience.
    /// moved to parasoll file for easier overview and reduce clutter in the larger classes.
    /// </summary>
    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        internal static extern bool PlaySound(string sound, IntPtr module, IntPtr flags);
    }
}
