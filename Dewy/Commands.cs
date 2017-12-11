using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dewy
{
    class Commands
    {
        public static void Load()
        {
            #region LS
            Dictionary<char, FileAttributes> FileA = new Dictionary<char, FileAttributes>()
            {
                { 'd', FileAttributes.Directory },
                { 'h', FileAttributes.Hidden },
                { 'r', FileAttributes.ReadOnly },
                { 's', FileAttributes.System },
            };
            Command
                .Make("ls")
                .SetFunc((a) =>
                {
                    string[] Files = new string[0];
                    Terminal.WriteLine("{0} {1}", a.Switch("f"), a.Switch("d"));
                    if (a.Switch("d") || !a.Switch("f"))
                        Files = Files.ToList<string>().Concat(Directory.GetDirectories(Environment.CurrentDirectory)).ToArray();
                    if (a.Switch("f") || !a.Switch("d"))
                        Files = Files.ToList<string>().Concat(Directory.GetFiles(Environment.CurrentDirectory)).ToArray();
                    foreach (string F in Files)
                    {
                        FileInfo f = new FileInfo(F);
                        string Attr = "";
                        foreach (KeyValuePair<char, FileAttributes> Fa in FileA)
                        {
                            if ((f.Attributes & Fa.Value) == Fa.Value)
                                Attr += Fa.Key;
                            else
                                Attr += '-';
                        }
                        Terminal.CWriteLine("$f[$a{0}$f] $a{1}", Attr, f.Name);
                    }
                })
                .AddSwitch(Util.List("d", "f"));
            #endregion
            #region CD
            Command
                .Make("cd")
                .SetFunc((a) =>
                {
                    DirectoryInfo Dir = Util.FindDir(a.FullArg());
                    if (Dir != null)
                        Environment.CurrentDirectory = Util.PrettyDir( Dir.FullName );
                    Terminal.CWriteLine("$a{0}", Environment.CurrentDirectory);
                })
                .Raw(true);
            #endregion
            #region MKDIR
            Command
                .Make("mkdir")
                .Raw(true)
                .SetFunc((a) =>
                {
                    foreach (string Arg in a.VarArgs())
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, Arg));
                            Terminal.CWriteLine("$a{0}/", Arg);
                        }
                        catch (Exception e)
                        {
                            Terminal.CWriteLine("$c{0}/ - {1}", Arg, e.GetType().Name);
                        }
                    }
                });
            #endregion
        }
    }
}
