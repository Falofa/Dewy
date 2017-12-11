using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Dewy
{
    class Util
    {
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
        public static string PrettyDir(string Dir)
        {
            if (Dir.Length < 2) return Dir.ToUpper();
            return Dir.Substring(0, 1).ToUpper() + Dir.Substring(1);
        }
        public static string[] FetchFilesParam = Array("r", "f", "d");
        public static string ArrayToStr(IEnumerable<string> Arr)
        {
            return string.Join("\n", Arr.ToArray());
        }
        public static string DictToStr<T,R>(Dictionary<T, R> Arr)
        {
            string Result = "";
            foreach(KeyValuePair<T, R> K in Arr)
            {
                Result += string.Format("{0} = {1}\n", K.Key, K.Value);
            }
            return Result.TrimEnd('\n');
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
        public static string[] RegexParam = Array("r");
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
                m = new Regex("^" + Regex.Escape(Input).Replace("\\*", "(.+)").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
            }
            return m;
        }
    }
    static class Extension
    {
        public static string Format(this string Str, params object[] Obj)
        {
            return string.Format(Str, Obj);
        }
    }
}
