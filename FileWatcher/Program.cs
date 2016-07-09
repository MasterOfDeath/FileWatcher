using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WindowsService1
{
    static class Program
    {
        private static FileSystemWatcher watcher;
        private static string watchDir;
        private static string filter;
        private static string exec;
        private static int execCount = 0;
        private static int execCountPause = 0;

        static void Main()
        {
            string settingsFile = "settings.ini";
            string settingsText = "";
            Regex settingsEx = new Regex("^watchDir=(.*)\n^filter=(.*)\n^exec=(.*)\n^execCount=(.*)\n^execCountPause=(.*)", RegexOptions.Multiline);
            
            try
            {
                using (StreamReader sr = new StreamReader(settingsFile))
                {
                    settingsText = sr.ReadToEnd();
                }
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }

            var match = settingsEx.Match(settingsText);
            try
            {
                watchDir = match.Groups[1].Value.Trim();
                filter = match.Groups[2].Value.Trim();
                exec = match.Groups[3].Value.Trim();
                Int32.TryParse(match.Groups[4].Value.Trim(), out execCount);
                Int32.TryParse(match.Groups[5].Value.Trim(), out execCountPause);
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }

            watchDir = ReplaceEnvVariables(watchDir);
            exec = ReplaceEnvVariables(exec);

            Console.WriteLine(watchDir);
            Console.WriteLine(exec);

            if (string.IsNullOrWhiteSpace(watchDir) || string.IsNullOrWhiteSpace(exec))
            {
                Environment.Exit(0);
            }

            if (!Directory.Exists(watchDir) || !File.Exists(exec))
            {
                Environment.Exit(0);
            }

            Watch();
            
            while (true)
            {
                System.Threading.Thread.Sleep(80000);
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private static void Watch()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = watchDir;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = filter;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                watcher.EnableRaisingEvents = false;

                Process myProcess = new Process();
                try
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.FileName = exec;
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.Start();
                }
                catch (Exception)
                {

                }
            }
            finally
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        private static string ReplaceEnvVariables(string str)
        {
            foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
            {
                string keyVal = (string)variable.Key;
                if (str.IndexOf(keyVal, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    Console.WriteLine(keyVal);
                    str = Regex.Replace(str, $"%{keyVal}%", variable.Value.ToString(), RegexOptions.IgnoreCase);
                }
            }

            return str;
        }
    }
}
