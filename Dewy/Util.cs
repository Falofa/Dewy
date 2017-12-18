using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Security;
using System.Security.Principal;

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
        public static string[] Search(string Input, Parser a, int Level = 3, TerminalWritable Out = null)
        {
            PathMatch Pm = GetPathMatch(Input);
            Regex Re = GenericRegex(Pm.Match, a);
            DirectoryInfo Dir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, Pm.Path));
            HashSet<string> Results = new HashSet<string>();
            Action<DirectoryInfo> Recur = null;
            Recur = (d) =>
            {
                if (Out != null)
                    Out.Write(d.Name);
                try
                {
                    foreach (FileInfo Entry in d.GetFiles())
                    {
                        if (Re.IsMatch(Entry.Name))
                            Results.Add(Entry.FullName);
                    }
                }
                catch (Exception) { }
                try
                {
                    foreach (DirectoryInfo Entry in d.GetDirectories())
                    {
                        if (Re.IsMatch(Entry.Name))
                            Results.Add(Entry.FullName);
                        Recur(Entry);
                    }
                }
                catch (Exception) { }
            };
            Recur(Dir);
            Out.Last("Done");
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
        public static Regex GenericRegex(string Input, Parser a)
        {
            Regex m = null;
            if (a.Switch("r"))
            {
                m = new Regex(Input, RegexOptions.IgnoreCase);
            }
            else
            {
                if (Input.Length == 0) return new Regex(".+");
                m = new Regex("^" + Regex.Escape(Input).Replace("\\*", "(.*)").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
            }
            return m;
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
}
