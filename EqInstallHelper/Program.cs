using System.Linq;
using System.IO;
using System.Xml.Linq;

namespace EqInstallHelper
{
    using System.Diagnostics;
    using System.Text;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    class Program
    {
        private static bool _publicServer = true;
        private static string _serverShortName = string.Empty;
        private static string _serverLongName = string.Empty;
        private static string _lsUser = string.Empty;
        private static string _lsPassword = string.Empty;
        private static string _sqlPassword = string.Empty;

        static void Main(string[] args)
        {
            //validate I'm in the correct location, just use zone.exe as a marker
            if (!File.Exists("zone.exe"))
            {
                System.Console.WriteLine("Error. Are you sure you have launched this from the same folder as your EQEmu binaries (such as zone.exe)?");
                return;
            }

            System.Console.WriteLine("Will this be a private or public server? Type in a number and hit enter. (Default is public)");
            System.Console.WriteLine("1 - Public. This server will use the EqEmulator login server.");
            System.Console.WriteLine("2 - Private. This server will use your own login server. Good for local/LAN installations.");
            var response = System.Console.ReadLine();
            int selection;
            if (int.TryParse(response, out selection))
            {
                if (selection == 2) _publicServer = false;
            }
            try
            {
                CopyStartupFile();
            }
            catch (FileNotFoundException ex)
            {
                System.Console.WriteLine(ex);
                return;
            }

            System.Console.WriteLine("Please enter a short name for your server, then hit enter.");
            _serverShortName = System.Console.ReadLine();
            System.Console.WriteLine("Please enter a long name for your server, then hit enter.");
            _serverLongName = System.Console.ReadLine();

            System.Console.WriteLine("Please enter a MySQL server password for the user 'root', then hit enter.");
            _sqlPassword = System.Console.ReadLine();

            try
            {
                CopyStartupFile();
            }
            catch (FileNotFoundException ex)
            {
                System.Console.WriteLine(ex);
                return;
            }

            System.Console.WriteLine("Please enter your login server user name, then hit enter.");
            if (!_publicServer) System.Console.WriteLine("This login will be created for you automatically.");
            _lsUser = System.Console.ReadLine();
            System.Console.WriteLine("Please enter your login server password, then hit enter.");
            _lsPassword = System.Console.ReadLine();

            WriteConfig();
            System.Console.WriteLine("Setting up the peq database...");
            SetupDatabase();
            System.Console.WriteLine("Finished setting up the peq database.");
        }

        private static void SetupDatabase()
        {
            const string mysql = "C:\\Program Files\\MariaDB 10.0\\bin\\mysql.exe";
            Process.Start(mysql, string.Format("-uroot --execute=\"SET PASSWORD = PASSWORD('{0}')", _sqlPassword));
            Process.Start(mysql, string.Format("-uroot -p{0} --execute=\"CREATE DATABASE peq;\"", _sqlPassword));
            var sb = new StringBuilder();
            sb.Append(string.Format("\"C:\\Program Files\\MariaDB 10.0\\bin\\mysql.exe\" -uroot -p{0} -v --database=peq < c:\\EqEmu\\sql\\peq.sql", _sqlPassword));
            sb.Append(System.Environment.NewLine);
            sb.Append(string.Format("\"C:\\Program Files\\MariaDB 10.0\\bin\\mysql.exe\" -uroot -p{0} -v --database=peq < c:\\EqEmu\\sql\\player_tables.sql", _sqlPassword));
            sb.Append(System.Environment.NewLine);
            sb.Append(string.Format("\"C:\\Program Files\\MariaDB 10.0\\bin\\mysql.exe\" -uroot -p{0} -v --database=peq < c:\\EqEmu\\sql\\load_bots.sql", _sqlPassword));
            sb.Append(System.Environment.NewLine);
            sb.Append(string.Format("\"C:\\Program Files\\MariaDB 10.0\\bin\\mysql.exe\" -uroot -p{0} -v --database=peq < c:\\EqEmu\\sql\\mercs.sql", _sqlPassword));
            sb.Append(System.Environment.NewLine);
            if (!_publicServer)
            {
                sb.Append(string.Format("\"C:\\Program Files\\MariaDB 10.0\\bin\\mysql.exe\" -uroot -p{0} -v --database=peq < c:\\EqEmu\\sql\\load_login.sql", _sqlPassword));
            }
            File.WriteAllText("SetupDatabase.bat",sb.ToString());
            Process.Start("SetupDatabase.bat");
            Process.Start(mysql, string.Format("-uroot -p{0} --execute=\"UPDATE `peq`.`rule_values` SET `rule_value`='true' WHERE `ruleset_id`=10 AND `rule_name`='Mercs:AllowMercs';", _sqlPassword));
        }
        private static void CopyStartupFile()
        {
            if (_publicServer)
            {
                if (File.Exists("startup_public.bat"))
                {
                    File.Copy("startup_public.bat", "startup.bat", true);
                }
                else
                {
                    throw new FileNotFoundException("startup_public.bat");
                }
            }
            else
            {
                if (File.Exists("startup_private.bat"))
                {
                    File.Copy("startup_private.bat", "startup.bat", true);
                }
                else
                {
                    throw new FileNotFoundException("startup_private.bat");
                }
            }
        }

        private static void WriteConfig()
        {
            var doc = XDocument.Load("eqemu_config.xml");
            // doc.Load("eqemu_config.xml");
            var y = doc.Descendants("shortname").Single();
            y.Value = _serverShortName;
            y = doc.Descendants("longname").Single();
            y.Value = _serverLongName;
            var ls = doc.Descendants("loginserver").Single();
            y = ls.Descendants("host").Single();
            if (_publicServer)
            {
                y.Value = "login.eqemulator.net";
            }
            else
            {
                y.Value = "127.0.0.1";
            }
            y = ls.Descendants("account").Single();
            y.Value = _lsUser;
            y = ls.Descendants("password").Single();
            y.Value = _lsPassword;
            var key = doc.Descendants("key").Single();
            key.Value = System.Guid.NewGuid().ToString();

            var db = doc.Descendants("database").Single();
            y = db.Descendants("username").Single();
            y.Value = "root";
            y = db.Descendants("password").Single();
            y.Value = _sqlPassword;
            db = doc.Descendants("qsdatabase").Single();
            y = db.Descendants("username").Single();
            y.Value = "root";
            y = db.Descendants("password").Single();
            y.Value = _sqlPassword;
            doc.Save("eqemu_config.xml");
        }
    }
}
