using System;
using System.Runtime.InteropServices;

namespace SharpVk.SaschaWillems
{
    public static class MemoryUtil
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void Copy(IntPtr dest, IntPtr src, uint count);
    }
}
