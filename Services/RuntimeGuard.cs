using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TEMO.AI;

internal static class RuntimeGuard
{
    [DllImport("kernel32.dll")]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

    public static void EnsureSafe()
    {
#if DEBUG
        return;
#else
        if (Debugger.IsAttached || IsDebuggerPresent())
            Environment.Exit(0);

        var remote = false;
        if (CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref remote) && remote)
            Environment.Exit(0);
#endif
    }
}
