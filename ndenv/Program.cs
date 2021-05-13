
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using System.IO.Compression;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Windows.Forms;
using CommandLine;
using Alphaleonis;

namespace ndenv
{
    public class Program
    {
        static readonly string _VersionFile = @".node-version";

        static List<string> _VersionList = new List<string>();
        static string _AppDir = "";
        static string _NodeDir = "";

        public enum ErrorCode
        {
            NO_ERROR,
            INVALID_ARGS,
            WITH_NOT_PARSED
        }


        [Verb("--version", HelpText = "ndenvのバージョンを出力します。")]
        public class ShowVersion
        {
        }

        [Verb("install", HelpText = "指定されたバージョンのnode.exeをインストール、もしくは listでインストール可能なバージョンを出力します。")]
        public class Install
        {
            [CommandLine.Option('l', "list", Required = false, HelpText = "インストール可能なバージョンを出力します。")]
            public bool IsList { get; set; }
            [CommandLine.Option('L', "LTS", Required = false, HelpText = "LTSバージョンをインストールします。")]
            public bool IsLTS { get; set; }
            [CommandLine.Option('S', "STABLE", Required = false, HelpText = "STABLEバージョンをインストールします。")]
            public bool IsSTABLE { get; set; }
            [CommandLine.Value(0, MetaName = "Version", HelpText = "インストールするバージョン")]
            public string Version { get; set; }
        }
        [Verb("uninstall", HelpText = "指定されたバージョンのnode.exeをアンインストールします。")]
        public class Uninstall
        {
            [CommandLine.Value(0, MetaName = "Version", HelpText = "アンインストールするバージョン")]
            public string Version { get; set; }
        }
        [Verb("version", HelpText = "現在のバージョンを出力する。")]
        public class Version
        {
        }
        [Verb("versions", HelpText = "インストール済みのバージョンを表示する。")]
        public class Versions
        {
        }
        [Verb("global", HelpText = "全体で使用するNode.jsのバージョンを指定する。")]
        public class Global
        {
            [CommandLine.Value(0, MetaName = "Version", HelpText = "使用するバージョン")]
            public string Version { get; set; }
        }
        [Verb("local", HelpText = "フォルダーで使用するNode.jsのバージョンを指定する。")]
        public class Local
        {
            [CommandLine.Value(0, MetaName = "Version", HelpText = "使用するバージョン")]
            public string Version { get; set; }
        }

        static void Main(string[] args)
        {
            ErrorCode error_code = ErrorCode.INVALID_ARGS;
            _AppDir = System.AppDomain.CurrentDomain.BaseDirectory;
            _NodeDir = System.IO.Path.Combine(_AppDir, "versions");

            //var parser = new Parser(config => { config.IgnoreUnknownArguments = false; config.HelpWriter = Console.Out; });

            var parser = new Parser(config => { config.IgnoreUnknownArguments = false; config.AutoVersion = false; config.HelpWriter = Console.Out; });
            var result = parser.ParseArguments<ShowVersion, Install, Uninstall, Version, Versions, Global, Local>(args)
                .WithParsed<ShowVersion>(opts => {
                    error_code = ProcShowVersion(opts);
                })
                .WithParsed<Install>(opts => {
                    error_code = ProcInstall(opts);
                })
                .WithParsed<Uninstall>(opts => {
                    error_code = ProcUninstall(opts);
                })
                .WithParsed<Version>(opts => {
                    error_code = ProcVersion(opts);
                })
                .WithParsed<Versions>(opts => {
                    error_code = ProcVersions(opts);
                })
                .WithParsed<Global>(opts => {
                    error_code = ProcGlobal(opts);
                })
                .WithParsed<Local>(opts => {
                    error_code = ProcLocal(opts);
                })
                .WithNotParsed(errs => {
                    error_code = ErrorCode.WITH_NOT_PARSED;
                });


            if (error_code == ErrorCode.INVALID_ARGS)
            {
                parser.ParseArguments<ShowVersion, Install, Version, Versions, Global, Local>(new string[] { "" });
            }

#if DEBUG
            Console.WriteLine("続行するには何かキーを押してください．．．");
            Console.ReadKey();
#endif
            Environment.Exit((error_code != ErrorCode.NO_ERROR) ? 1 : 0);
        }

        static ErrorCode ProcShowVersion(Program.ShowVersion opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            System.Diagnostics.FileVersionInfo ver =
                System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            System.Console.WriteLine(ver.FileVersion);
            return ret_code;
        }
        static ErrorCode ProcInstall(Program.Install opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            int num_opts = 0;
            System.Diagnostics.Debug.WriteLine("ProcInstall");
            System.Diagnostics.Debug.WriteLine("opt.List:" + opt.IsList);
            System.Diagnostics.Debug.WriteLine("opt.Version:" + opt.Version);
            
            string Regex_LTS = "^" + Properties.Settings.Default.LTSVersion; 
            
            if (opt.IsList)
            {
                ++num_opts;
            }
            if (opt.Version != null)
            {
                ++num_opts;
            }
            if (opt.IsLTS)
            {
                ++num_opts;
            }
            if (opt.IsSTABLE)
            {
                ++num_opts;
            }
            if (num_opts != 1)
            {
                return ErrorCode.INVALID_ARGS;
            }
            GetList();
            if (opt.IsList)
            {
                ret_code = ShowList();
            }
            if (opt.IsLTS)
            {
                var list = _VersionList.Where(w => Regex.IsMatch(w, Regex_LTS)).Select(s => s).ToList();
                list.Sort((a, b) => string.Compare(Version2String(a), Version2String(b)));
                InstallNodeJs(list[list.Count - 1]);
            }
            if (opt.IsSTABLE)
            {
                InstallNodeJs(_VersionList[_VersionList.Count - 1]);
            }
            if (opt.Version != null)
            {
                ret_code = InstallNodeJs(opt.Version);
            }
            return ret_code;
        }

        static ErrorCode ProcUninstall(Program.Uninstall opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (opt.Version != null)
            {
                var version = opt.Version.ToLower();
                if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^v[0-9]+(\.[0-9]+)*"))
                {
                    System.Console.Error.WriteLine("不正なバージョン文字列です。");
                    return ErrorCode.INVALID_ARGS;
                }
                var node_path = System.IO.Path.Combine(_NodeDir, opt.Version);
                if (!System.IO.Directory.Exists(node_path))
                {
                    System.Console.Error.WriteLine("指定されたバージョンはインストールされていません。");
                    return ErrorCode.INVALID_ARGS;
                }
                DeleteDirectory(node_path);
            }
            else
            {
                ret_code = ErrorCode.INVALID_ARGS;
            }
            return ret_code;
        }
        static void DeleteDirectory(string dir)
        {
            Alphaleonis.Win32.Filesystem.DirectoryInfo di = new Alphaleonis.Win32.Filesystem.DirectoryInfo(dir);

            RemoveReadonlyAttribute(di);

            di.Delete(true);

            return;
        }

        static void RemoveReadonlyAttribute(Alphaleonis.Win32.Filesystem.DirectoryInfo dirInfo)
        {
            if ((dirInfo.Attributes & System.IO.FileAttributes.ReadOnly) ==　System.IO.FileAttributes.ReadOnly)
            {
                dirInfo.Attributes = System.IO.FileAttributes.Normal;
            }
            foreach (var fi in dirInfo.GetFiles())
            {
                if ((fi.Attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                {
                    fi.Attributes = System.IO.FileAttributes.Normal;
                }
            }
            foreach (var di in dirInfo.GetDirectories())
            {
                RemoveReadonlyAttribute(di);
            }
            return;
        }

        static ErrorCode ProcVersion(Program.Version opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_NodeDir))
            {
                System.Console.Error.WriteLine("Node.jsがインストールされていません。");
                return ret_code;
            }
            System.Diagnostics.Debug.WriteLine("ProcVersion");
            var ver_file = System.IO.Path.Combine(System.Environment.CurrentDirectory, _VersionFile);
            if (System.IO.File.Exists(ver_file))
            {
                string version = "";
                using (System.IO.StreamReader sr = new System.IO.StreamReader(ver_file, Encoding.GetEncoding("utf-8")))
                {
                    version = sr.ReadToEnd();
                }
                var versions = System.IO.Directory.GetDirectories(_NodeDir).ToList();
                if (versions.Where(w => System.IO.Path.GetFileName(w) == version).Any())
                {
                    System.Console.WriteLine(version + "(set by " + ver_file + ")");
                    return ret_code;
                }
            }
            ver_file = System.IO.Path.Combine(_AppDir, _VersionFile);
            if (System.IO.File.Exists(ver_file))
            {
                string version = "";
                using (System.IO.StreamReader sr = new System.IO.StreamReader(ver_file, Encoding.GetEncoding("utf-8")))
                {
                    version = sr.ReadToEnd();
                }
                var versions = System.IO.Directory.GetDirectories(_NodeDir).ToList();
                if (versions.Where(w => System.IO.Path.GetFileName(w) == version).Any())
                {
                    System.Console.WriteLine(version);
                    return ret_code;
                }
            }
            System.Console.Error.WriteLine("使用するバージョンが設定されていません。");
            return ret_code;
        }
        static ErrorCode ProcVersions(Program.Versions opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_NodeDir))
            {
                System.Console.Error.WriteLine("Node.jsがインストールされていません。");
                return ret_code;
            }
            var versions = System.IO.Directory.GetDirectories(_NodeDir).ToList();
            foreach (var ver in versions)
            {
                System.Console.WriteLine(System.IO.Path.GetFileName(ver));
            }
            return ret_code;
        }
        static ErrorCode ProcGlobal(Program.Global opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_NodeDir))
            {
                System.Console.Error.WriteLine("Node.jsがインストールされていません。");
                return ret_code;
            }
            var ver_file = System.IO.Path.Combine(_AppDir, _VersionFile);
            var versions = System.IO.Directory.GetDirectories(_NodeDir).ToList();
            if (versions.Where(w => System.IO.Path.GetFileName(w) == opt.Version).Any())
            {
                if (System.IO.File.Exists(ver_file))
                {
                    System.IO.File.Delete(ver_file);
                }
                var utf8_encoding = new System.Text.UTF8Encoding(false);
                using (var wr = new System.IO.StreamWriter(ver_file, false, utf8_encoding))
                {
                    wr.Write(opt.Version);
                }
            }
            else
            {
                System.Console.Error.WriteLine("指定されたバージョンはインストールされていません。");
                ret_code = ErrorCode.INVALID_ARGS;
            }
            return ret_code;
        }
        static ErrorCode ProcLocal(Program.Local opt)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            if (!System.IO.Directory.Exists(_NodeDir))
            {
                System.Console.Error.WriteLine("Node.jsがインストールされていません。");
                return ret_code;
            }
            var ver_file = System.IO.Path.Combine(System.Environment.CurrentDirectory, _VersionFile);
            var versions = System.IO.Directory.GetDirectories(_NodeDir).ToList();
            if (versions.Where(w => System.IO.Path.GetFileName(w) == opt.Version).Any())
            {
                if (System.IO.File.Exists(ver_file))
                {
                    System.IO.File.Delete(ver_file);
                }
                var utf8_encoding = new System.Text.UTF8Encoding(false);
                using (var wr = new System.IO.StreamWriter(ver_file, false, utf8_encoding))
                {
                    wr.Write(opt.Version);
                }
            }
            else
            {
                System.Console.Error.WriteLine("指定されたバージョンはインストールされていません。");
                ret_code = ErrorCode.INVALID_ARGS;
            }
            return ret_code;
        }
        static ErrorCode ShowList()
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            foreach (var ver in _VersionList)
            {
                Console.WriteLine(ver);
            }
            return ret_code;
        }

        static ErrorCode InstallNodeJs(string version)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            version = version.ToLower();
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^v[0-9]+(\.[0-9]+)*"))
            {
                System.Console.Error.WriteLine("不正なバージョン文字列です。");
                return ErrorCode.INVALID_ARGS;
            }
            if (!_VersionList.Where(w => w == version).Any())
            {
                System.Console.Error.WriteLine("指定されたバージョンが見つかりませんでした。");
                return ErrorCode.INVALID_ARGS;
            }
            if ((ret_code = DownloadNodeJs(version)) != ErrorCode.NO_ERROR)
            {
                return ret_code;
            }
            if ((ret_code = SetupNpm(version)) != ErrorCode.NO_ERROR)
            {
                return ret_code;
            }
            return ret_code;
        }

        static void GetList()
        {
            string source = "";
            System.Net.HttpWebRequest webreq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(Properties.Settings.Default.NodeJsURL);
            using (System.Net.HttpWebResponse webres = (System.Net.HttpWebResponse)webreq.GetResponse())
            {
                using (System.IO.Stream st = webres.GetResponseStream())
                {
                    //文字コードを指定して、StreamReaderを作成
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(st, System.Text.Encoding.UTF8))
                    {
                        source = sr.ReadToEnd();
                    }
                }
            }

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(source);
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a"))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(link.InnerHtml, @"^v[0-9]+(\.[0-9]+)*"))
                {
                    Regex re = new Regex(@"^v[0-9]+(\.[0-9]+)*/", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    Match m = re.Match(link.InnerHtml);
                    while (m.Success)
                    {
                        if (m.Value == link.InnerHtml)
                        {
                            var dir = m.Value.TrimEnd('/');
                            if (string.Compare(Version2String(dir), Version2String(Properties.Settings.Default.OldestVersion)) >= 0)
                            {
                                _VersionList.Add(dir);
                            }
                        }
                        m = m.NextMatch();
                    }
                }
            }
            _VersionList.Sort((a, b) => string.Compare(Version2String(a), Version2String(b)));
        }

        static string Version2String(string version)
        {
            Regex re = new Regex(@"[0-9]+", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = re.Match(version);
            string ver_string = "";
            while (m.Success)
            {
                ver_string += string.Format("{0:D4}", Convert.ToInt32(m.Value));
                m = m.NextMatch();
            }
            return ver_string;
        }

        static ErrorCode DownloadNodeJs(string version)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;

            string dir = System.IO.Path.Combine(_NodeDir, version);
            string download = System.IO.Path.Combine(_NodeDir, "node.zip");
            if (!System.IO.Directory.Exists(_NodeDir))
            {
                System.IO.Directory.CreateDirectory(_NodeDir);
            }
            if (!System.IO.Directory.Exists(dir))
            {
                System.Console.WriteLine("node(" + version + ")をインストール中");
                string url = "";
                if (Environment.Is64BitOperatingSystem)
                {
                    url = string.Format(Properties.Settings.Default.Win64URL, Properties.Settings.Default.NodeJsURL, version);
                }
                else
                {
                    url = string.Format(Properties.Settings.Default.Win32URL, Properties.Settings.Default.NodeJsURL, version);
                }
                System.Net.WebClient wc = new System.Net.WebClient();
                System.Diagnostics.Debug.WriteLine("ダウンロード: " + url + " -> " + download);
                wc.DownloadFile(url, download);
                wc.Dispose();
                var node_dir = ExtractToDirectoryExtensions(download, _NodeDir, true);
                node_dir = System.IO.Path.Combine(_NodeDir, node_dir);
                System.IO.File.Delete(download);
                System.IO.Directory.Move(node_dir, dir);
            }
            return ret_code;
        }

        static ErrorCode SetupNpm(string version)
        {
            ErrorCode ret_code = ErrorCode.NO_ERROR;
            string dir = System.IO.Path.Combine(_NodeDir, version);
            var npm = System.IO.Path.Combine(dir, "npm.cmd");
            var yarn = System.IO.Path.Combine(dir, "yarn.cmd");

            string cache_dir = System.IO.Path.Combine(_AppDir, "npm-cache");
            if (!System.IO.Directory.Exists(cache_dir))
            {
                System.IO.Directory.CreateDirectory(cache_dir);
            }
            if (!System.IO.Directory.Exists(dir))
            {
                System.Diagnostics.Debug.WriteLine("ディレクトリ作成: " + dir);
                System.IO.Directory.CreateDirectory(dir);
            }
            System.Console.WriteLine("パッケージ管理ツールを設定します。");
            System.Console.WriteLine("npmの設定中");

            Process p;
            p = new Process();
            p.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WorkingDirectory = dir;
            p.StartInfo.Arguments = string.Format("/c \"\"{0}\" config set cache {1}", npm, "\"" + cache_dir + "\"\"");
            p.Start();
            p.WaitForExit();
            p.Close();
                
            System.Console.WriteLine("yarnのインストール中");
            p = new Process();
            p.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WorkingDirectory = dir;
            p.StartInfo.Arguments = string.Format("/c \"\"{0}\" install -g yarn\"", npm);
            p.Start();
            p.WaitForExit();
            p.Close();

            cache_dir = System.IO.Path.Combine(_AppDir, "yarn-cache");
            if (!System.IO.Directory.Exists(cache_dir))
            {
                System.IO.Directory.CreateDirectory(cache_dir);
            }
            p = new Process();
            p.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WorkingDirectory = dir;
            p.StartInfo.Arguments = string.Format("/c \"\"{0}\" config set cache-folder {1}", yarn, "\"" + cache_dir + "\"\"");
            p.Start();
            p.WaitForExit();
            p.Close();

            return ret_code;
        }

        static string ExtractToDirectoryExtensions(string sourceArchiveFileName, string destinationDirectoryName, bool overwrite)
        {
            string top_name = "";
            using (ZipArchive archive = ZipFile.OpenRead(sourceArchiveFileName))
            {
                top_name = System.IO.Directory.GetParent(archive.Entries[0].FullName).FullName.Split('\\').Last();
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    var fullPath = System.IO.Path.Combine(destinationDirectoryName, entry.FullName).Replace('/', '\\');

                    var parent = System.IO.Directory.GetParent(fullPath).FullName;
                    if (!System.IO.Directory.Exists(parent))
                    {
                        System.IO.Directory.CreateDirectory(parent);
                    }
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        if (!System.IO.Directory.Exists(fullPath))
                        {
                            System.IO.Directory.CreateDirectory(fullPath);
                        }
                    }
                    else
                    {
                        if (overwrite)
                        {
                            entry.ExtractToFile(fullPath, true);
                        }
                        else
                        {
                            if (!System.IO.File.Exists(fullPath))
                            {
                                entry.ExtractToFile(fullPath, true);
                            }
                        }
                    }
                }
            }
            return top_name;
        }

        static bool ConsoleQuestion(string question)
        {
            string ans = "";
            bool ret = false;
            ConsoleKeyInfo response;
            while (true)
            {
                Console.Write(question);
                while (true)
                {
                    response = Console.ReadKey(true);
                    if (response.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                    else
                    {
                        Console.Write(response.KeyChar);
                        ans += response.KeyChar;
                    }
                }
                Console.WriteLine();
                if (ans.Equals("Y", StringComparison.InvariantCultureIgnoreCase)
                    || ans.Equals("N", StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
                else
                {
                    ans = "";
                }
            }
            if (ans.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
            {
                ret = true;
            }
            return ret;
        }
    }
}
