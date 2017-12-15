using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Dewy
{
    class Commands
    {
        public static void Load()
        {
            #region GENERAL
            #region CLS
            Command
                .Make("cls")
                .Alias("clear")
                .SetFunc((a) =>
                {
                    Terminal.Clear();
                })
                .Describe("Clears console window");
            #endregion
            #region HELP
            Command
                .Make("help")
                .SetFunc((a) =>
                {
                    if (a.Get(1).Length > 0)
                    {
                        Command C = Program.FindCommand(a.Get(1));
                        if (C == null)
                        {
                            Terminal.CWriteLine("$cCommand '$8{0}$c' not found", a.Get(1).ToUpper());
                        }
                        Program.Help(C);
                    } else
                    {
                        Program.HelpAll();
                    }
                })
                .Describe("Displays help message");
            #endregion
            #region WHOAMI
            Command
                .Make("whoami")
                .SetFunc((a) =>
                {
                    Terminal.CWriteLine("Username: $a{0}", Util.UserName());
                    if (Util.IsAdmin())
                        Terminal.CWriteLine("Administrator: $aTrue");
                    else
                        Terminal.CWriteLine("Administrator: $cFalse");
                })
                .Describe("Prints process owner");
            #endregion
            #endregion

            #region FILE SYSTEM
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
                    string[] Files = Util.FetchFiles(a.Get(1), a);
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
                .Preset(Preset.FetchFile)
                .Describe("Lists files and directories");
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
                .Raw(true)
                .Describe("Changes working directory");
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
                })
                .Describe("Creates directories");
            #endregion
            #region WRITE
            Command
                .Make("write")
                .Alias("w")
                .SetFunc((a) =>
                {
                    string FileName = a.Get(1);
                    try
                    {
                        string Fil = Path.Combine(Environment.CurrentDirectory, a.Get(1));
                        File.WriteAllText(Fil, a.Get(2));
                        Terminal.CWriteLine("$aWrote {0} bytes.", new FileInfo(Fil).Length);
                    }
                    catch (Exception)
                    {
                        Terminal.CWriteLine("$cCouldn't write to file.");
                    }
                })
                .Describe("Writes data to files");
            #endregion
            #region FINFO
            Command
                .Make("finfo")
                .Alias("fi")
                .SetFunc((a) =>
                {

                });
            #endregion
            #endregion

            #region NETWORK
            #region NETSTAT
            Command
                .Make("netstat")
                .SetFunc((a) =>
                {
                    Ping p = new Ping();
                    PingReply Reply = p.Send("8.8.8.8", 500);
                    if (Reply.Status == IPStatus.Success)
                        Terminal.CWriteLine("$aConnection is up");
                    else
                        Terminal.CWriteLine("$cConnection is down");
                })
                .Describe("Simple way to tell if ethernet connection is up");
            #endregion
            #endregion

            #region PROCESS
            #region EXIT
            Command
                .Make("exit")
                .Alias("quit")
                .SetFunc((a) =>
                {
                    Environment.Exit(0);
                    Process.GetCurrentProcess().WaitForExit();
                })
                .Describe("Closes Dewy");
            #endregion
            #region DIE
            Command
                .Make("die")
                .SetFunc((a) =>
                {
                    Process.GetCurrentProcess().Kill();
                })
                .Describe("Forcibly closes Dewy");
            #endregion
            #region PKILL
            Command
                .Make("pkill")
                .Alias("pk")
                .SetFunc((a) =>
                {
                    foreach (string Pname in a.VarArgs())
                    {
                        Regex r = Util.GenericRegex(Pname, a);
                        foreach(Process Proc in Process.GetProcesses().OrderBy(o => o.Id))
                        {
                            if (r.IsMatch(Proc.ProcessName))
                            {
                                try
                                {
                                    Proc.Kill();
                                    Terminal.CWriteLine("$f[{0}] $e{1} $f- $aKilled", Proc.Id, Proc.ProcessName);
                                } catch(Exception e)
                                {
                                    Terminal.CWriteLine("$f[{0}] $e{1} $f- $c{2}", Proc.Id, Proc.ProcessName, e.GetType().Name);
                                }
                            }
                        }
                    }
                })
                .Preset(Preset.Regex)
                .Describe("Kills processes");
            #endregion
            #region PLIST
            Command
                .Make("plist")
                .Alias("pl")
                .SetFunc((a) =>
                {
                    foreach (string Pname in a.VarArgs(true))
                    {
                        Regex r = Util.GenericRegex(Pname, a);
                        foreach (Process Proc in Process.GetProcesses().OrderBy(o => o.Id))
                        {
                            if (r.IsMatch(Proc.ProcessName))
                            {
                                Terminal.CWriteLine("$f[{0}] $e{1}", Proc.Id, Proc.ProcessName);
                            }
                        }
                    }
                })
                .Preset(Preset.Regex)
                .Describe("Lists processes");
            #endregion
            #region ROOT
            Command
                .Make("root")
                .SetFunc((a) =>
                {
                    Process NewInstance = null;
                    try
                    {
                        NewInstance = Process.Start(new ProcessStartInfo
                        {
                            FileName = Util.DewyPath(),
                            Arguments = "",
                            Verb = "runas",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception) { }
                    if (NewInstance == null || NewInstance.HasExited)
                        Terminal.CWriteLine("$cFailed to start as administrator");
                    else
                        Process.GetCurrentProcess().Kill();
                })
                .Describe("Restarts Dewy as administrator");
            #endregion
            #region START
            Command
                .Make("start")
                .SetFunc((a) =>
                {
                    Process NewInstance = null;
                    FileInfo FileName = Util.FindFile(a.Get(1));
                    Exception exc = null;
                    Terminal.SetForeColor('8');
                    if (FileName == null)
                    {
                        Terminal.CWriteLine("$cFile not found");
                        return;
                    }
                    try
                    {
                        NewInstance = Process.Start(new ProcessStartInfo
                        {
                            FileName = FileName.FullName,
                            Arguments = a.Get(2),
                            Verb = a.Switch("s") ? "runas" : "",
                            UseShellExecute = false
                        });
                    }
                    catch (Exception e) { exc = e; }
                    ConsoleCancelEventHandler BlockCtrlC = delegate (object o, ConsoleCancelEventArgs e)
                    {
                        e.Cancel = true;
                    };
                    Console.CancelKeyPress += BlockCtrlC;
                    if (NewInstance == null || NewInstance.HasExited)
                        Terminal.CWriteLine("$cFailed to start process ({0})", exc);
                    else
                        NewInstance.WaitForExit();
                    Terminal.SetForeColor('r');
                    Console.CancelKeyPress -= BlockCtrlC;
                })
                .Switch("s")
                .Describe("Runs processes");
            #endregion
            #endregion

        }
    }
}
