using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace Dewy
{
    class Commands
    {
        public static void Load()
        {
            #region DEBUG
            Command
                .Make("hang", true)
                .SetFunc((a) => { Thread.Sleep(Int32.MaxValue); });
            Command
                .Make("bench")
                .SetFunc((a) =>
                {
                    TerminalWritable Output = Terminal.Writable("Times ran: ", ConsoleColor.Green);
                    Output.PreventSpam = true;
                    Output.MinDelay = 1;
                    int i = 0;
                    while (true)
                    {
                        Output.Write("{0}", i++);
                    }
                });
            #endregion

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
                .Describe("Displays help message")
                .AddArg("Command", "Command to display help");
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
                .Describe("Lists files and directories")
                .AddArg("Match", "Matches file or directory name");
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
                .Describe("Changes working directory")
                .AddArg("Dir", "Directory name");
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
                .Describe("Creates directories")
                .AddArg("Dir", "Directory name", false, true);
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
                .Describe("Writes data to files")
                .AddArg("Filename", "File to write to", true)
                .AddArg("Text", "Text to be written");
            #endregion
            #region DEL
            Command
                .Make("del")
                .SetFunc((a) =>
                {
                    int time = Environment.TickCount;
                    string[] Files = Util.FindAllRecur(a.Get(1), a);
                    Terminal.CWriteLine("$aFile count: $f{0}",Files.Length);
                    Terminal.CWriteLine("$aTime: $f{0}ms", Environment.TickCount - time);
                })
                .Preset(Preset.Regex);
            #endregion
            #region FREGEX
            Regex FiMatch = new Regex("[\u0020-\u007E]+", Program.GeneralUseRegex);
            Command
                .Make("fregex")
                .Alias("fr")
                .SetFunc((a) =>
                {
                    Regex Re = null;
                    try
                    {
                        Re = new Regex("^(.*)(" + a.Get(2) + ")(.*)$", RegexOptions.IgnoreCase);
                    } catch(Exception)
                    {
                        Terminal.CWriteLine("$cInvalid regex");
                        return;
                    }
                    FileInfo Fi = Util.FindFile(a.Get(1));
                    if (Fi == null)
                    {
                        Terminal.CWriteLine("$cInvalid file");
                        return;
                    }
                    string Content = File.ReadAllText(Fi.FullName);
                    int MinLen = a.IntParam("l", 4);
                    Terminal.SetForeColor('8');
                    foreach (Match m in FiMatch.Matches(Content))
                    {
                        string Text = m.Value;
                        if (Text.Length < MinLen) continue;
                        if (Re.IsMatch(Text)) {
                            Match M = Re.Match(Text);
                            string Pre = M.Groups[1].Value;
                            string Mat = M.Groups[2].Value;
                            string Pos = M.Groups[3].Value;
                            Terminal.CWriteLine("$7{0}$a{1}$7{2}", Pre, Mat, Pos);
                        }
                    }
                })
                .Param("l")
                .DescribeParam("l", "Minimum match length to be processed")
                .AddArg("FileName", "File to be matched", true)
                .AddArg("Match", "Regex to be used")
                .Describe("Finds matches in files");
            #endregion
            #region DIGI
            Regex Passw = new Regex(".*(passw|password|login|senha).*", Program.GeneralUseRegex);
            Environment.SpecialFolder[] UserFolders = new Environment.SpecialFolder[]
            {
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolder.MyPictures,
                Environment.SpecialFolder.MyMusic,
                Environment.SpecialFolder.MyDocuments,
                Environment.SpecialFolder.StartMenu,
                Environment.SpecialFolder.Desktop,
            };
            string[] IgnoreFormats = new string[]
            {
                "png", "jpg", "gif", "jpe", "jpeg", "mp3", "wav", "mkv", "mp4", "avi"
            };
            Command
                .Make("digi")
                .SetFunc((a) =>
                {
                    HashSet<string> Matches = new HashSet<string>();
                    HashSet<string> ToSearch = new HashSet<string>();
                    bool Verbose = a.Switch("v");
                    ToSearch.Add(Environment.ExpandEnvironmentVariables("%appdata%\\Notepad++\\backup"));
                    DriveInfo.GetDrives()
                        .Where(o => o.DriveType == DriveType.Fixed && o.IsReady)
                        .Each(o => ToSearch.Add(o.Name));

                    foreach (Environment.SpecialFolder s in UserFolders)
                        ToSearch.Add(Environment.GetFolderPath(s));
                    List<string> Searched = new List<string>();
                    string[] FinalList = ToSearch.Where(o => o != string.Empty && Directory.Exists(o)).ToArray();

                    if (!Verbose)
                        Terminal.CWriteLine("$cWorking...");
                    int Level = a.IntParam("l", 2);
                    int time = Environment.TickCount;
                    Action<string, int> Iter = null;
                    Iter = (Dir, Count) =>
                    {
                        if (++Count > Level) return;
                        if (Searched.Contains(Dir)) return;
                        Searched.Add(Dir);
                        if (Verbose)
                            Terminal.CWriteLine("$c{0}", Dir);
                        string[] Files = null;
                        try
                        {
                            Files = Directory.GetFileSystemEntries(Dir);
                        } catch(Exception) { return; }
                        foreach (string Fil in Files)
                        {
                            if (Directory.Exists(Fil))
                            {
                                Iter(Fil, Count);
                                continue;
                            }
                            FileInfo fi = new FileInfo(Fil);
                            if (IgnoreFormats.Contains(fi.Name.Split('.').Last()))
                                continue;
                            if (Passw.IsMatch(fi.Name))
                            {
                                Matches.Add(Fil);
                                Terminal.CWriteLine("$a{0}", Fil);
                                continue;
                            }
                            if (fi.Length < 1024 * 1)
                            {
                                try
                                {
                                    string Data = File.ReadAllText(fi.FullName);
                                    if (Passw.IsMatch(Data))
                                    {
                                        Matches.Add(Fil);
                                        Terminal.CWriteLine("$a{0}", Fil);
                                    }
                                } catch(Exception) { }
                            }

                        }
                    };

                    foreach(string Dir in FinalList)
                    {
                        Iter(Dir, 0);
                    }
                    if (Verbose)
                    {
                        foreach (string Fil in Matches)
                            Terminal.CWriteLine("$a{0}", Fil);
                    }
                    if (a.Param("d").Trim().Length > 0)
                    {
                        try
                        {
                            DirectoryInfo Dir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, a.Param("d")));
                            if (!Dir.Exists)
                                Dir.Create();
                            Terminal.CWriteLine("$eDumping files...");
                            int i = 0;
                            foreach (string Fil in Matches)
                            {
                                FileInfo fi = new FileInfo(Fil);
                                try
                                {
                                    fi.CopyTo(Path.Combine(Dir.FullName, string.Format("{0}-{1}", ++i, fi.Name)));
                                } catch(IOException) { }
                            }
                        } catch(Exception)
                        {
                            Terminal.CWriteLine("$cFailed to dump");
                        }
                    }
                    Terminal.CWriteLine("$eProcess took {0}ms", Environment.TickCount - time);
                })
                .Params("d", "l")
                .Switch("v")
                .DescribeParam("d", "Dump directory")
                .DescribeParam("l", "Recursion level")
                .DescribeSwitch("v", "Print current directory - Verbose")
                .Describe("Digs computer for login information");
            #endregion
            #region PATH
            Command
                .Make("path")
                .SetFunc((a) =>
                {
                    string[] Paths = Environment.GetEnvironmentVariable("path").Split(';');
                    HashSet<string> NewPath = new HashSet<string>();
                    foreach(string S in Paths)
                        if (S.Length != 0  && Directory.Exists(S))
                            NewPath.Add(S);
                    foreach(string S in NewPath)
                    {
                        DirectoryInfo Dir = new DirectoryInfo(S);
                        Terminal.CWriteLine("$a{0}", Dir.FullName.TrimEnd('\\'));
                    }
                });
            #endregion
            #region CD
            Command
                .Make("search")
                .SetFunc((a) =>
                {
                    TerminalWritable Output = Terminal.Writable("Searching: ", ConsoleColor.Green);
                    Output.PreventSpam = true;
                    foreach (string Fil in Util.Search(a.Get(1), a, 2, Output))
                    {
                        Terminal.CWriteLine("$a{0}", Fil);
                    }
                })
                .Preset(Preset.Regex);
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
                .Describe("Kills processes")
                .AddArg("Match", "Matches process name", false, true);
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
                .Describe("Lists processes")
                .AddArg("Match", "Matches process name", false, true);
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
                    Process Prc = null;
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
                        Prc = Process.Start(new ProcessStartInfo
                        {
                            FileName = FileName.FullName,
                            Arguments = a.Get(2),
                            Verb = a.Switch("s") ? "runas" : "",
                            UseShellExecute = a.Switch("n")
                        });
                    }
                    catch (Exception e) { exc = e; }
                    ConsoleCancelEventHandler BlockCtrlC = delegate (object o, ConsoleCancelEventArgs e)
                    {
                        e.Cancel = true;
                    };
                    Console.CancelKeyPress += BlockCtrlC;
                    if (Prc == null || Prc.HasExited)
                        Terminal.CWriteLine("$cFailed to start process ({0})", exc);
                    else
                        Prc.WaitForExit();
                    Terminal.SetForeColor('r');
                    Console.CancelKeyPress -= BlockCtrlC;
                })
                .Switch("s", "n")
                .Describe("Runs processes")
                .DescribeSwitch("s", "Run as administrator")
                .DescribeSwitch("n", "Starts in a new window")
                .AddArg("FileName", "File to run", true)
                .AddArg("Arguments", "Arguments to start process");
            #endregion
            #region CD
            Command
                .Make("priority")
                .Alias("pr")
                .SetFunc((a) =>
                {
                    Terminal.CWriteLine("$eAquiring maximum priority...");
                    Process p = Process.GetCurrentProcess();
                    try
                    {
                        p.PriorityBoostEnabled = true;
                        p.PriorityClass = ProcessPriorityClass.RealTime;
                    }
                    catch (Exception) { }
                    Terminal.CWriteLine("$aPriority: $e{0}", Enum.GetName(typeof (ProcessPriorityClass), p.PriorityClass));
                    Terminal.CWriteLine("$aBoost: $e{0}", p.PriorityBoostEnabled);
                });
            #endregion
            #endregion

        }
    }
}
