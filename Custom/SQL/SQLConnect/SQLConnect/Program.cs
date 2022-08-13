using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace SQLConnect
{
    class Program
    {
        static void Main(string[] args)
        {
            String sqlServer = "dc01";
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

            // Get system user
            String querysyslogin = "SELECT SYSTEM_USER;";
            SqlCommand sysusercommand = new SqlCommand(querysyslogin, con);
            SqlDataReader sysuserreader = sysusercommand.ExecuteReader();
            sysuserreader.Read();
            Console.WriteLine("[+] Logged in as: " + sysuserreader[0]);
            sysuserreader.Close();

            // Get current user
            String queryuserlogin = "SELECT CURRENT_USER;";
            SqlCommand usercommand = new SqlCommand(queryuserlogin, con);
            SqlDataReader userreader = usercommand.ExecuteReader();
            userreader.Read();
            Console.WriteLine("[+] Mapped to the user: " + userreader[0]);
            userreader.Close();

            // Find public role membership
            String querypublicrole = "SELECT IS_SRVROLEMEMBER('public');";
            SqlCommand publiccommand = new SqlCommand(querypublicrole, con);
            SqlDataReader publicreader = publiccommand.ExecuteReader();
            publicreader.Read();
            Int32 publicRole = Int32.Parse(publicreader[0].ToString());
            publicreader.Close();

            // Find sysadmin role membership
            String querysysadminrole = "SELECT IS_SRVROLEMEMBER('sysadmin');";
            SqlCommand sysadmincommand = new SqlCommand(querysysadminrole, con);
            SqlDataReader sysadminreader = sysadmincommand.ExecuteReader();
            sysadminreader.Read();
            Int32 sysadminRole = Int32.Parse(sysadminreader[0].ToString());
            sysadminreader.Close();

            if (publicRole == 1)
            {
                Console.WriteLine("[+] User is a member of a public role");
            }
            else
            {
                Console.WriteLine("[-] User is NOT a member of a public role");
            }

            if (sysadminRole == 1)
            {
                Console.WriteLine("[+] User is a member of a sysadmin role");
            }
            else
            {
                Console.WriteLine("[-] User is NOT a member of a sysadmin role");
            }

            con.Close();
        }
    }
}
