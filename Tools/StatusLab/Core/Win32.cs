using System.Runtime.InteropServices;
using System.Text;

namespace StatusLab
{
    /// <summary>Win32 calls for the window source and the dialog answer. Text reads use SendMessageTimeout,
    /// because GetWindowText can block for seconds on a hung window. No call gives a window the focus.</summary>
    public static class Win32
    {
        delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, EnumProc proc, IntPtr lParam);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool IsHungAppWindow(IntPtr hwnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageTimeoutW")]
        static extern IntPtr SendText(IntPtr hwnd, uint msg, IntPtr wParam, StringBuilder lParam, uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
        static extern IntPtr SendPlain(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

        const uint WmGetText = 0x000D, BmClick = 0x00F5, AbortIfHung = 0x0002;

        public static List<IntPtr> TopWindows(int pid)
        {
            var list = new List<IntPtr>();
            EnumWindows((h, _) =>
            {
                GetWindowThreadProcessId(h, out var p);
                if (p == pid && IsWindowVisible(h)) list.Add(h);
                return true;
            }, IntPtr.Zero);
            return list;
        }

        public static string ClassName(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string Text(IntPtr hwnd)
        {
            var sb = new StringBuilder(512);
            SendText(hwnd, WmGetText, (IntPtr)sb.Capacity, sb, AbortIfHung, 200, out _);
            return sb.ToString();
        }

        public static bool Hung(IntPtr hwnd) => IsHungAppWindow(hwnd);

        public static List<(IntPtr Handle, string Label)> Buttons(IntPtr dialog)
        {
            var list = new List<(IntPtr, string)>();
            EnumChildWindows(dialog, (h, _) =>
            {
                if (ClassName(h) == "Button") list.Add((h, Text(h).Replace("&", "")));
                return true;
            }, IntPtr.Zero);
            return list;
        }

        /// <summary>Presses a button with BM_CLICK. The window does not get the focus.</summary>
        public static void Click(IntPtr button) => SendPlain(button, BmClick, IntPtr.Zero, IntPtr.Zero, AbortIfHung, 2000, out _);
    }
}
