using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
    }
    static class Extension
    {
        public static string Format(this string Str, params object[] Obj)
        {
            return string.Format(Str, Obj);
        }
    }
}
