using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Net;
using System.Web;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Dewy
{
    class Util
    {
        public static string DewyPath()
        {
            FileInfo Self = new FileInfo(Environment.GetCommandLineArgs()[0]);
            return Path.Combine(Self.Directory.FullName, Self.Name.Replace(".vshost", ""));
        }
        public static bool IsAdmin()
        {
            using (WindowsIdentity Identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal Principal = new WindowsPrincipal(Identity);
                return Principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        public static string UserName()
        {
            using (WindowsIdentity Identity = WindowsIdentity.GetCurrent())
            {
                return Identity.Name;
            }
        }
        public static string Sor(int i)
        {
            return i == 1 ? "" : "s";
        }
        public static string GetAlphabet(string Reg)
        {
            string s = new string(new char[256].Select((t, i) => (char)i).ToArray());
            Regex Regx = new Regex(string.Format("[{0}]+", Reg));
            string Result = "";
            foreach (Match m in Regx.Matches(s))
            {
                Result += m.Value;
            }
            return Result;
        }
        public static List<T> List<T>(params T[] List)
        {
            return List.ToList<T>();
        }
        public static T[] Array<T>(params T[] List)
        {
            return List.ToArray<T>();
        }
        public static DirectoryInfo FindDir( string Input )
        {
            string In = Input.ToLower();
            HashSet<string> Ps = new HashSet<string>();
            string[] D = Environment.GetEnvironmentVariable("path").Split(';');
            //Ps = (HashSet<string>)Ps.Concat(Directory.GetDirectories(Environment.CurrentDirectory));
            if (Directory.Exists(In.TrimEnd('/') + "/"))
            {
                return new DirectoryInfo(In.TrimEnd('/') + "/");
            }
            if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, In + "/"))) {
                return new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, In));
            }
            foreach (string P in D)
            {
                try
                {
                    DirectoryInfo d = new DirectoryInfo(P);
                    if (d.Name.ToLower() == In)
                        return d;
                    Ps = new HashSet<string>(Ps.Concat(Directory.GetDirectories(P)));
                }catch(IOException) { }
            }
            foreach(string S in D)
            {
                DirectoryInfo d = new DirectoryInfo(S);
                if (d.Name.ToLower() == In) return d;
            }
            return null;
        }
        public static string EnumName(Type EnumType, int Value)
        {
            List<string> All = new List<string>();
            foreach (var t in Enum.GetValues(EnumType))
            {
                int Bit = (int)t;
                if (Bit == (Bit & Value))
                    All.Add(Enum.GetName(EnumType, t));
            };
            if (All.Count == 0)
                return Enum.GetName(EnumType, Value);
            return string.Join(" ", All.ToArray());
        }
        public static List<string> AllEntries(string Dir)
        {
            List<string> All = new List<string>();
            All = All.Concat(Directory.GetDirectories(Dir)).ToList();
            All = All.Concat(Directory.GetFiles(Dir)).ToList();
            return All;
        }
        public struct PathMatch
        {
            public string Path;
            public string Match;

        }
        public static PathMatch GetPathMatch(string Input)
        {
            char[] Sep = "\\/".ToCharArray();
            string[] Filn = Input.TrimEnd(Sep).Split(Sep);
            string Pathc = string.Join("\\", Filn.Reverse().Skip(1).Reverse().ToArray());
            string Mat = Filn.LastOrDefault();
            return new PathMatch()
            {
                Path = Pathc,
                Match = Mat
            };
        }
        public static string[] Search(string Input, Parser a, int Level = 3)
        {
            PathMatch Pm = GetPathMatch(Input);
            Regex Re = GenericRegex(Pm.Match, a);
            DirectoryInfo Dir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Pm.Path));
            HashSet<string> Results = new HashSet<string>();
            Action<DirectoryInfo> Recur = null;
            Recur = (d) =>
            {
                try
                {
                    foreach (FileInfo Entry in d.GetFiles())
                    {
                        if (Re.IsMatch(Entry.Name))
                        {
                            Results.Add(Entry.FullName);
                            Terminal.CWriteLine("$a{0}", Entry.FullName);
                        }
                    }
                }
                catch (Exception) { }
                try
                {
                    foreach (DirectoryInfo Entry in d.GetDirectories())
                    {
                        if (Re.IsMatch(Entry.Name))
                        {
                            Results.Add(Entry.FullName);
                            Terminal.CWriteLine("$a{0}", Entry.FullName);
                        }
                        Recur(Entry);
                    }
                }
                catch (Exception) { }
            };
            Recur(Dir);
            return Results.ToArray();
        }
        public static string[] FindAllRecur(string Input, Parser a)
        {
            PathMatch Pm = GetPathMatch(Input);
            string Pathc = Pm.Path;
            string Mat = Pm.Match;
            string Dir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Pathc)).FullName;
            Regex Re = GenericRegex(Mat, a);
            List<string> Matched = new List<string>();
            List<string> Final = new List<string>();

            foreach (string F in AllEntries(Dir))
            {
                FileInfo fi = new FileInfo(F);
                if (Re.IsMatch(fi.Name))
                    Matched.Add(F);
            }
            Action<IEnumerable<string>> Recur = null;
            Recur = (l) =>
            {
                foreach (string F in l)
                {
                    Final.Add(F);
                    if (Directory.Exists(F))
                    {
                        Recur(AllEntries(F));
                    }
                }
            };
            Recur(Matched);
            return Final.ToArray();
        }
        public static Regex FilenameMatch = new Regex("filename=\"([^\"]+)\"", Program.GeneralUseRegex);
        public static Regex InvalidPath = new Regex(string.Format("[{0}{1}]+", Path.GetInvalidFileNameChars(), Path.GetInvalidPathChars()), Program.GeneralUseRegex);
        public static int Rnd(int Min = 0, int Max = 256)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[8];
            rng.GetBytes(bytes);
            return Math.Abs(Min + BitConverter.ToInt32(bytes, 0) % (Max - Min));
        }
        public static T PickRnd<T>(IEnumerable<T> Arr)
        {
            return Arr.OrderBy(o => Rnd(0, 1024)).FirstOrDefault();
        }
        public static string RandomString(int Len, string Alp = "abcdefghijklmnopqrstuvxwyz")
        {
            string r = "";
            for (int i = 0; i < Len; i++)
            {
                r += Alp[Rnd(0, Alp.Length)];
            }
            return r;
        }
        public static int ParseInt(string Input, int Default = 0)
        {
            int Result = 0;
            if (!int.TryParse(Input, out Result))
                return Default;
            return Result;
        }
        public static string Enabled(bool b, bool color = true)
        {
            if (color)
                return b ? "$aEnabled" : "$cDisabled";
            return b ? "Enabled" : "Disabled";
        }
        public static string FilAttr(FileInfo f, Dictionary<char, FileAttributes> FileA)
        {
            string Attr = "";
            foreach (KeyValuePair<char, FileAttributes> Fa in FileA)
            {
                if ((f.Attributes & Fa.Value) == Fa.Value)
                    Attr += Fa.Key;
                else
                    Attr += '-';
            }
            return Attr;
        }
        public static string DirAttr(DirectoryInfo f, Dictionary<char, FileAttributes> FileA)
        {
            string Attr = "";
            foreach (KeyValuePair<char, FileAttributes> Fa in FileA)
            {
                if ((f.Attributes & Fa.Value) == Fa.Value)
                    Attr += Fa.Key;
                else
                    Attr += '-';
            }
            return Attr;
        }
        public static List<WebClient> WorkingWC = new List<WebClient>();
        public static bool DownloadFile(Uri DlUrl, string OutName = "")
        {
            bool Finished = false;
            Terminal.CWriteLine("$c [DOWNLOAD]");
            Terminal.CWriteLine("$fDownload Server: $a{0}", DlUrl.Host);

            string Filename = null;
            string MimeType = "";

            try
            {
                WebRequest hwr = HttpWebRequest.Create(DlUrl);
                hwr.Method = "HEAD";
                WebResponse wr = hwr.GetResponse();
                if (wr.Headers["Content-Disposition"] != null)
                {
                    Match m = FilenameMatch.Match(wr.Headers["Content-Disposition"]);
                    if (m.Success)
                        Filename = m.Groups[1].Value;
                }
                if (wr.Headers["Content-Type"] != null)
                    MimeType = wr.Headers["Content-Type"];
            } catch(Exception) { }
            if (Filename != null)
            {
                Filename = InvalidPath.Replace(Filename, "_");
                Terminal.CWriteLine("$fOnline Filename: $a{0}", Filename);
            } else
            {
                try
                {
                    Filename = Path.GetFileName(DlUrl.LocalPath);
                }
                catch (Exception)
                {
                    Filename = RandomString(4);
                };
            }
            if (MimeType.Length > 0)
                Terminal.CWriteLine("$fMime: $a{0}", Mime.ParseMime(MimeType));
            FileInfo FullFile = new FileInfo( OutName.Length == 0 ? Filename : OutName );
            if (FullFile.Extension.Length > 1)
            {
                Filename = FullFile.Name;
            } else
            {
                string Format = Mime.MimeToType(MimeType);
                Filename = Path.GetFileNameWithoutExtension(Filename) + "." + Format;
            }
            FileInfo Output = new FileInfo(Path.Combine(Environment.CurrentDirectory, Filename));
            Terminal.CWriteLine("$fOutput: $a{0}", Output.Name);
            //return false;

            WebClient wc = new WebClient();
            WorkingWC.Add(wc);
            wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36";
            CursorPosition p = Terminal.GetPos();

            int Begin = 0;
            int BarSize = 25 + 3;
            Action<int, float, bool> UpdateProgress = (b, speed, l) =>
            {
                int Progress = (int)Math.Round(((double)b / 100.0) * (BarSize - 3.0));
                Terminal.SetPos(p);
                Terminal.CWriteLine("$a[{0}] {1:0.00} kb/s", ">".PadLeft(Progress, '=').PadRight(BarSize - 3, '-'), speed);
            };
            AntiSpam Spam = new AntiSpam();
            wc.DownloadProgressChanged += (o, e) =>
            {
                if (Spam.Ignore()) return;
                float speed = (float)((double)e.BytesReceived / ((double)Environment.TickCount - (double)Begin));
                UpdateProgress(e.ProgressPercentage, speed, false);
            };
            wc.DownloadFileCompleted += (o, e) =>
            {
                Finished = true;
            };
            UpdateProgress(0, 0, true);
            Begin = Environment.TickCount;
            wc.DownloadFileAsync(DlUrl, Output.FullName);
            while(!Finished)
            {
                Thread.Sleep(250);
                if (wc == null)
                {
                    return false;
                }
            }
            Thread.Sleep(250);
            UpdateProgress(100, 0, true);
            Terminal.WriteLine();
            return true;
        }
        public static FileInfo FindFile(string Input)
        {
            string In = Input.ToLower();
            Regex WideSearch = new Regex("^" + Regex.Escape(In.TrimEnd('.')) + "\\.[^.]+$", Program.GeneralUseRegex);
            HashSet<string> Search = new HashSet<string>();
            Search.Add(Environment.CurrentDirectory);
            string[] Dirs = Environment.GetEnvironmentVariable("path").Split(';');
            foreach (string Dir in Dirs)
                Search.Add(Dir);
            foreach(string Dir in Search)
            {
                try
                {
                    string[] Files = Directory.GetFiles(Dir);
                    foreach (string Fi in Files)
                    {
                        FileInfo f = new FileInfo(Fi);
                        if (f.Name.ToLower() == In) return f;
                        if (WideSearch.IsMatch(f.Name.ToLower())) return f;
                    }
                } catch(Exception) { }
            }
            return null;
        }
        public static string PrettyDir(string Dir)
        {
            if (Dir.Length < 2) return Dir.ToUpper();
            return Dir.Substring(0, 1).ToUpper() + Dir.Substring(1);
        }
        public static string ArrayToStr(IEnumerable<string> Arr, string Separator = "\n")
        {
            return string.Join(Separator, Arr.ToArray());
        }
        public static string DictToStr<T,R>(Dictionary<T, R> Arr, string Separator = "\n")
        {
            string Result = "";
            foreach(KeyValuePair<T, R> K in Arr)
            {
                Result += string.Format("{0} = {1}{2}", K.Key, K.Value, Separator);
            }
            return Regex.Replace(Result, Regex.Escape(Separator) + "$", "");
        }
        public static bool IsDir(string s)
        {
            if (!File.Exists(s)) return false;
            return (File.GetAttributes(s) & FileAttributes.Directory) == FileAttributes.Directory;
        }
        public static string[] FetchFiles(string Input, Parser a)
        {
            string[] Files = new string[0];
            if (a.Switch("d") || !a.Switch("f"))
                Files = Files.ToList<string>().Concat(Directory.GetDirectories(Environment.CurrentDirectory)).ToArray();
            if (a.Switch("f") || !a.Switch("d"))
                Files = Files.ToList<string>().Concat(Directory.GetFiles(Environment.CurrentDirectory))
                    .Where(o => !IsDir(o)).ToArray();

            Regex m = GenericRegex(Input, a);
            List<string> Result = new List<string>();
            foreach (string F in Files)
            {
                FileInfo f = new FileInfo(F);
                Match mt = m.Match(f.Name);
                if (mt.Success) Result.Add(f.FullName);
            }
            return Result.ToArray();
        }
        public static Regex GenericRegex(string Input, Parser a, bool Greedy = false)
        {
            string Part = Greedy ? ".*" : "";
            if (Greedy && Input.Trim('*') != Input && !a.Switch("r"))
                Part = "";
            Regex m = null;
            if (a.Switch("r"))
            {
                m = new Regex(Input, RegexOptions.IgnoreCase);
            }
            else
            {
                if (Input.Length == 0) return new Regex(".+");
                m = new Regex("^" + Part + Regex.Escape(Input).Replace("\\*", "(.*)").Replace("\\?", ".") + Part + "$", RegexOptions.IgnoreCase);
            }
            return m;
        }
        public static string MakeRelative(string filePath, string referencePath)
        {
            var fileUri = new Uri(filePath);
            var referenceUri = new Uri(referencePath);
            return Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString());
        }
    }
    static class Extension
    {
        public static void Each<T>(this IEnumerable<T> Arr, Action<T> Act)
        {
            foreach(T Item in Arr)
            {
                Act.Invoke(Item);
            }
        }
    }
    public class AntiSpam
    {
        public int Delay = 0;
        public int Time = 0;
        public AntiSpam(int Delay = 100)
        {
            this.Delay = Delay;
        }
        public bool Ignore()
        {
            if (Environment.TickCount < Time + Delay) return true;
            Time = Environment.TickCount;
            return false;
        }
    }
}
