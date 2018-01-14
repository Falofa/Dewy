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
            #region HANG
            Command
                .Make("hang", true)
                .SetFunc((a) => { Thread.Sleep(Int32.MaxValue); });
            #endregion
            #region COLORS
            Command
                .Make("colors", true)
                .SetFunc((a) => {
                    string Demo = Util.GetAlphabet("a-zA-Z0-9");
                    foreach (KeyValuePair<char, ConsoleColor> ColorPair in Terminal.Colors)
                    {
                        Terminal.CWriteLine("$f{0} - " + string.Format("${0}█████████ " + Demo, ColorPair.Key), ColorPair.Key);
                    }
                });
            #endregion
            #region HANG
            Command
                .Make("todo", true)
                .SetFunc((a) => {
                    foreach(Command c in Program.Cmds)
                    {
                        if (!(c.Documented || c.Debug))
                        {
                            Terminal.CWriteLine("$a{0}", c.Name.ToUpper());
                        }
                    }
                });
            #endregion
            #region DEBUG
            Command
                .Make("debug")
                .SetFunc((a) => {
                    Program.Debug = !Program.Debug;
                    Terminal.CWriteLine("$eDebug mode is " + Util.Enabled(Program.Debug));
                })
                .Describe("Enable debug mode");
            #endregion
            #region PARSE
            Command
                .Make("parse", true)
                .SetFunc((a) => {
                    Terminal.CWriteLine("$eString count:    $f{0}", a.Count());
                    Terminal.CWriteLine("$eQuoted count:    $f{0}", a.Count(ArgTypes.Quoted));
                    Terminal.CWriteLine("$eParameter count: $f{0}", a.Count(ArgTypes.ParamValue));
                    Terminal.CWriteLine("$eSwitch count:    $f{0}", a.Count(ArgTypes.Switch));
                    for (int i = 0; i < a.Parsed.Count; i++)
                    {
                        ParsedArg p = a.Parsed[i];
                        Terminal.CWriteLine("$a[{0}] $d[$f{1}$d] $e({2})", i, p.Value, p.TypeName());
                    }
                });
            #endregion
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
                            Regex r = Util.GenericRegex(a.Get(1), a, true);
                            List<Command> Matched = new List<Command>();
                            foreach(Command c in Program.Cmds)
                            {
                                if (r.IsMatch(c.Name) || c.Aliases.Any(s => r.IsMatch(s)))
                                {
                                    Matched.Add(c);
                                }
                            }
                            if (Matched.Count > 0)
                            {
                                Terminal.CWriteLine("$aFound commands:");
                                foreach (Command c in Matched)
                                    Program.SmallHelp(c);
                            } else
                            {
                                Terminal.CWriteLine("$cNothing found");
                            }
                            return;
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

            #region STRING
            #region ALP
            Command
                .Make("alp")
                .SetFunc((a) =>
                {
                    if (a.FullArg().Length != 0)
                    {
                        try
                        {
                            string Alp = Util.GetAlphabet(a.FullArg());
                            a.PrintReturn("$a{0}", Alp);
                        }
                        catch (Exception)
                        {
                            Terminal.CWriteLine("$cInvalid input");
                            a.Return(""); return;
                        }
                    }
                    a.PrintReturn("$a{0}", "");
                })
                .Raw(true)
                .Describe("Returns all ASCII characters that match regex")
                .AddArg("Regex", "Match to be used", true);
            #endregion
            #region RSTR
            // \[([^\]]+?)\]\{(\d+)(?:,(\d+))?\}
            Regex FindKey = new Regex("\\[([^\\]]+?)\\]\\{(\\d+)(?:,(\\d+))?\\}", Program.GeneralUseRegex);
            Command
                .Make("rstr")
                .Alias("keygen")
                .SetFunc((a) => {
                    int Count = a.IntParam("c", 1);
                    for (int i = 0; i < Count; i++)
                    {
                        string KeyFormat = a.Get(1);
                        string Value = FindKey.Replace(KeyFormat, (m) =>
                        {
                            string Range = m.Groups[1].Value;
                            int Min = Util.ParseInt(m.Groups[2].Value);
                            int Max = Util.ParseInt(m.Groups[3].Value, Min);
                            int Len = Min == Max ? Min : Util.Rnd(Min, Max);
                            return Util.RandomString(Len, Util.GetAlphabet(Range));
                        });
                        Terminal.CWriteLine("$a{0}", Value);
                    }
                })
                .Param("c", "1")
                .Describe("Generates a random string based on a regex-style template")
                .DescribeParam("c", "Ammount of strings to generate")
                .AddArg("Template", "[Characters]{MinLength,MaxLength} e.g: [a-z\\d]{5,8}", true);
            #endregion
            #endregion

            #region FILE SYSTEM
            #region OL
            Command
                .Make("ol", false)
                .SetFunc((a) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = Program.Executable.Directory.FullName
                    });
                })
                .Describe("Opens folder containing Dewy's executable");
            #endregion
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
                            try
                            {
                                if ((f.Attributes & Fa.Value) == Fa.Value)
                                    Attr += Fa.Key;
                                else
                                    Attr += '-';
                            } catch(Exception)
                            {
                                Attr += '?';
                            }
                        }
                        Terminal.CWriteLine("$f[$a{0}$f] $a{1}", Attr, f.Name);
                    }
                })
                .Preset(Preset.FetchFile)
                .Describe("Lists files and directories")
                .AddArg("Match", "Matches file or directory name");
            #endregion
            #region ATTR
            Dictionary<char, FileAttributes> Attrs = new Dictionary<char, FileAttributes>()
            {
                { 'r', FileAttributes.ReadOnly },
                { 'h', FileAttributes.Hidden },
                { 's', FileAttributes.System },
                { 't', FileAttributes.Temporary },
                { 'o', FileAttributes.Offline },
                { 'i', FileAttributes.NotContentIndexed },
            };
            Attrs['*'] = Attrs.Select(o => o.Value).Aggregate((a, b) => (a | b));
            Dictionary<char, Action<FileInfo, char>> AttrActions = new Dictionary<char, Action<FileInfo, char>>()
            {
                { '+', (f, m) => { f.Attributes = f.Attributes | Attrs[m]; } },
                { '-', (f, m) => { f.Attributes = f.Attributes & ~Attrs[m]; } },
                { '~', (f, m) => { f.Attributes = f.Attributes ^ Attrs[m]; } },
            };
            Regex AttrReg = new Regex(string.Format("([{0}][^{0}]*)", string.Join( "", AttrActions.Select(o => "\\" + o.Key).ToArray())), Program.GeneralUseRegex);
            Command
                .Make("attr")
                .SetFunc((a) =>
                {
                    foreach (string Filn in Util.FetchFiles(a.Get(1), a))
                    {
                        FileInfo f = new FileInfo(Filn);
                        foreach (string s in a.VarArgs().Skip(2))
                        {
                            foreach (Match m in AttrReg.Matches("+" + s))
                            {
                                string Arg = m.Groups[0].Value.ToLower();
                                Terminal.CWriteLine(Arg);
                                if (Arg.Length == 1) continue;
                                char Mode = Arg[0];
                                string Att = Arg.Substring(1);
                                foreach (char c in Att)
                                    AttrActions[Mode](f, c);
                            }
                        }
                        string Attr = Util.FilAttr(f, Attrs);
                        Terminal.CWriteLine("$f[$a{0}$f] $a{1}", Attr, f.Name);
                    }
                })
                .Preset(Preset.FetchFile)
                .Describe("View and modify file/directory attributes")
                .AddArg("Filename", "File to be matched", true)
                .AddArg("Attributes", "Format: [[Operator][Attributes]]+ i.e: +hr-s", false, true)
                .AddText("Attributes:",
                    string.Join("\n", Attrs.Select(o => string.Format("$a{0} $f- {1}", o.Key, Util.EnumName(typeof(FileAttributes), (int)o.Value))).ToArray()))
                .AddText("\n$fOperators: $a" + string.Join(" ", AttrActions.Select(o => o.Key + "").ToArray()));
            #endregion
            #region CD
            Command
                .Make("cd")
                .SetFunc((a) =>
                {
                    if (a.FullArg().Length != 0)
                    {
                        DirectoryInfo Dir = Util.FindDir(a.FullArg());
                        if (Dir != null)
                            Environment.CurrentDirectory = Util.PrettyDir(Dir.FullName);
                    }
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
                .Make("del", true)
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
                            string Pre = M.Groups[1].Value.TrimStart();
                            string Mat = M.Groups[2].Value;
                            string Pos = M.Groups[3].Value.TrimEnd();
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
            Regex Passw = new Regex(".*(passw|password|login|senha|begin ssh|begin pgp).*", Program.GeneralUseRegex);
            Environment.SpecialFolder[] UserFolders = new Environment.SpecialFolder[]
            {
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolder.MyPictures,
                Environment.SpecialFolder.MyMusic,
                Environment.SpecialFolder.MyDocuments,
                Environment.SpecialFolder.StartMenu,
                Environment.SpecialFolder.DesktopDirectory,
                Environment.SpecialFolder.Favorites,
                Environment.SpecialFolder.Personal,
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolder.InternetCache,
                Environment.SpecialFolder.History,
                Environment.SpecialFolder.Cookies,
            };
            string[] IgnoreFormats = new string[]
            {
                "png", "jpg", "gif", "jpe", "jpeg", "mp3", "wav", "mkv", "mp4", "avi", "dll", "exe"
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
                })
                .Describe("Prints all directories in the PATH environment variable");
            #endregion
            #region SEARCH
            Command
                .Make("search")
                .SetFunc((a) =>
                {
                    a.Return(Util.Search(a.Get(1), a, 2));
                })
                .Preset(Preset.Regex)
                .Describe("Searches directories recursively for files")
                .AddArg("Match", "Match for filename");
            #endregion
            #region DL
            Command
                .Make("dl", true)
                .SetFunc((a) =>
                {
                    Action<string> Dl = (Url) =>
                    {
                        try
                        {
                            if (!Url.Contains("://"))
                            {
                                Url = "http://" + Url;
                            }
                            Uri DlUrl = new Uri(Url);
                            string Out = a.Get(2);
                            Util.DownloadFile(DlUrl, Out);
                        }
                        catch (FieldAccessException e)
                        {
                            Terminal.CWriteLine("$c{0}", e.Message);
                        }
                    };
                    if (a.Param("l").Length > 0)
                    {
                        FileInfo Urls = Util.FindFile(a.Param("l"));
                        string[] Lines = File.ReadAllLines(Urls.FullName);
                        foreach (string LUrl in Lines)
                        {
                            Dl(LUrl);
                        }
                    }
                    else
                    {
                        Dl(a.Get(1));
                    }
                })
                .Cleanup(()=> {
                    foreach (WebClient wc in Util.WorkingWC)
                    {
                        try
                        {
                            wc.CancelAsync();
                            wc.Dispose();
                        } catch(Exception) { }
                    }
                    Util.WorkingWC.Clear();
                })
                .Param("l");
            #endregion
            #region INSTALL
            Command
                .Make("install")
                .SetFunc((a) =>
                {
                    try
                    {
                        FileInfo Old = new FileInfo(Util.DewyPath());
                        FileInfo New = new FileInfo(Path.Combine(Environment.ExpandEnvironmentVariables("%systemroot%"), "Dewy.exe"));
                        if (New.Exists)
                            New.Delete();
                        Old.CopyTo(New.FullName);
                        Process p = Process.Start(New.FullName);
                        Environment.Exit(0);
                    }
                    catch (Exception)
                    {
                        Terminal.CWriteLine("$cAccess denied");
                    }
                })
                .Describe("Installs dewy into Windows directory");
            #endregion
            #region NAV
            Dictionary<char, FileAttributes> NavA = new Dictionary<char, FileAttributes>()
            {
                { 'h', FileAttributes.Hidden },
                { 'r', FileAttributes.ReadOnly },
                { 's', FileAttributes.System },
            };
            Command
                .Make("nav")
                .SetFunc((a) =>
                {
                    while(!a.Exit)
                    {
                        string Accessing = "";
                        try
                        {
                            Accessing = Environment.CurrentDirectory;
                            DirectoryInfo Parent = new DirectoryInfo(Environment.CurrentDirectory);
                            List<DirectoryInfo> Directories = Parent.GetDirectories().ToList();
                            if (Parent.Parent != null)
                                Directories = Util.Array(Parent.Parent).Concat(Directories).ToList();
                            int Cur = 0;

                            Terminal.CWriteLine("$e> {0}", Parent.FullName);
                            foreach (DirectoryInfo Dir in Directories)
                            {
                                string Attr = Util.DirAttr(Dir, NavA);
                                if (Parent.Parent != null && Dir.FullName == Parent.Parent.FullName)
                                {
                                    Terminal.CWriteLine("$a[{0}] $e{1} $f{2}", (Cur++).ToString().PadLeft(3), Attr, "..");
                                }
                                else
                                {
                                    Terminal.CWriteLine("$a[{0}] $e{1} $f{2}", (Cur++).ToString().PadLeft(3), Attr, Dir.Name);
                                }
                            }
                            int Index = -1;
                            while (Index < 0 || Index >= Directories.Count())
                            {
                                string Input = Terminal.PrettyInput();
                                if (Input == null)
                                {
                                    a.Exit = true;
                                    break;
                                }
                                Index = Util.ParseInt(Input, -1);
                            }
                            if (a.Exit) break;
                            Accessing = Directories[Index].FullName;
                            Environment.CurrentDirectory = Directories[Index].FullName;
                        } catch(UnauthorizedAccessException)
                        {
                            Terminal.CWriteLine("$cError accessing {0}", new DirectoryInfo(Accessing).Name);
                            Environment.CurrentDirectory = Path.Combine(Accessing, "..");
                        }
                    }
                    Terminal.CWriteLine("$cClosed nav mode");
                })
                .Describe("Makes navigating through directories easier");
            #endregion
            #region RN
            Command
                .Make("rn")
                .SetFunc((a) =>
                {
                    Dictionary<string, string> TempFiles = new Dictionary<string, string>();
                    Util.FetchFiles(a.Get(1), a).Each(f => {
                        FileInfo F = new FileInfo(f);
                        string Original = F.FullName;
                        string NewName = Util.RandomString(18) + ".tmp";
                        F.MoveTo(NewName);
                        TempFiles[Original] = new FileInfo(Path.Combine(F.Directory.FullName, NewName)).FullName;
                    });
                    int i = 0;
                    Regex Re = Util.GenericRegex(a.Get(1), a, true);
                    Regex Replacer = new Regex("\\$([ir\\d\\$])", RegexOptions.IgnoreCase);
                    int[] Randoms = new int[TempFiles.Count]
                        .Select((v, k) => k)
                        .OrderBy((p => Util.Rnd()))
                        .ToArray();
                    foreach (KeyValuePair<string, string> Fil in TempFiles)
                    {
                        FileInfo Original = new FileInfo(Fil.Key);
                        FileInfo Temp = new FileInfo(Fil.Value);
                        Match Metch = Re.Match(Original.Name);
                        string Result = Replacer.Replace(a.Get(2), (g) =>
                        {
                            int w = -1;
                            if (int.TryParse(g.Groups[1].Value, out w))
                            {
                                if (Metch.Groups.Count < 2) return "";
                                return Metch.Groups[w].Value;
                            }
                            switch(g.Groups[1].Value)
                            {
                                case "i": return i.ToString();
                                case "r": return Randoms[i].ToString();
                                case "$": return "$";
                            }
                            return "";
                        });
                        i++;
                        char p = (a.Param("m").Length > 0) ? (a.Param("m").ToLower()[0]) : '\0';
                        switch(p)
                        {
                            case 'u': Result = Result.ToUpper(); break;
                            case 'l': Result = Result.ToLower(); break;
                        }
                        Terminal.CWriteLine("$a{0} $c=> $a{1}", Original.Name, Result);
                        Temp.MoveTo(Result);
                    }
                })
                .Param("m")
                .DescribeParam("m", "String modifiers: [l]owercase [U]PPERCASE")
                .Describe("Renames files with regex like replace")
                .AddArg("Match", "Filename match i.e: *.*")
                .AddArg("Replace", "Replace pattern i.e: $1.png")
                .Preset(Preset.FetchFile);
            #endregion
            #region SRCH
            Command
                .Make("refind")
                .SetFunc((a) =>
                {
                    string Pattern = Util.GenericRegex(a.Get(1), a).ToString();
                    Regex Mc = new Regex(Pattern.Substring(1, Pattern.Length - 2));
                    Action <DirectoryInfo, int> Recur = null;
                    Recur = (d, l) =>
                    {
                        try
                        {
                            foreach (FileInfo f in d.GetFiles())
                            {
                                string Text = File.ReadAllText(f.FullName);
                                int Count = Mc.Matches(Text).Count;
                                if (Count > 0)
                                    Terminal.CWriteLine("$a{0}", Util.MakeRelative(f.FullName, Environment.CurrentDirectory));
                            }
                            if (l <= a.IntParam("l", 3))
                            {
                                foreach (DirectoryInfo D in d.GetDirectories())
                                {
                                    Recur(D, l + 1);
                                }
                            }
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    };
                    DirectoryInfo Cur = new DirectoryInfo(Environment.CurrentDirectory);
                    Recur(Cur, 0);
                })
                .Param("l")
                .DescribeParam("l", "How many subdirectories to travel, default: 3")
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
            #region LIP
            Command
                .Make("lip")
                .Alias("localhost")
                .SetFunc((a) =>
                {
                    Terminal.CWriteLine("$dNAME $f| $eADDRESSES $f| $aDNS ADDRESSES\n");
                    foreach( NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces() )
                    {
                        Terminal.CWriteLine("$d{0}", n.Name);
                        IEnumerable<IPAddress> ips = n.GetIPProperties().UnicastAddresses.Select(i => i.Address);
                        IEnumerable<IPAddress> dns = n.GetIPProperties().DnsAddresses;
                        if (!a.Switch("6"))
                        {
                            ips = ips.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                            dns = dns.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        }
                        foreach (IPAddress d in ips) Terminal.CWriteLine("$e{0}", d.ToString());
                        foreach (IPAddress d in dns) Terminal.CWriteLine("$a{0}", d.ToString());
                        Terminal.WriteLine();
                    }
                })
                .Switch("6")
                .DescribeSwitch("6", "Show IPV6 addresses")
                .Describe("Prints information about network interfaces");
            #endregion
            #region IP
            Command
                .Make("ip")
                .SetFunc((a) =>
                {
                    using (WebClient w = new WebClient())
                    {
                        try
                        {
                            a.PrintReturn("$a{0}", w.DownloadString("https://api.ipify.org"));
                            return;
                        }
                        catch (Exception e)
                        {
                            Terminal.CWriteLine("$c{0}", e.Message);
                        }
                    }
                })
                .Describe("Prints external Ip Address");
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
            #region PKILL
            Command
                .Make("pkillid")
                .Alias("pkid")
                .SetFunc((a) =>
                {
                    int[] Pids = a.VarArgs().Select(o => Util.ParseInt(o, -1)).ToArray();
                    foreach (Process Proc in Process.GetProcesses().OrderBy(o => o.Id))
                    {
                        if (Pids.Contains(Proc.Id))
                        {
                            try
                            {
                                Proc.Kill();
                                Terminal.CWriteLine("$f[{0}] $e{1} $f- $aKilled", Proc.Id, Proc.ProcessName);
                            }
                            catch (Exception e)
                            {
                                Terminal.CWriteLine("$f[{0}] $e{1} $f- $c{2}", Proc.Id, Proc.ProcessName, e.GetType().Name);
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
            #region PRIORITY
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
                })
                .Describe("Sets Dewy's process priority to maximum");
            #endregion
            #endregion

            #region PHP
            #region PHPSETUP
            PhpLib[] PhpLibs = null;

            Command
                .Make("phpsetup")
                .SetFunc((a) =>
                {
                    if (PhpLibs == null)
                    {
                        PhpLibs = Php.GetLibs();
                        int c = PhpLibs.Length;
                        Terminal.CWriteLine("$aLoaded {0} lib{1}", c, Util.Sor(c));
                    }
                    Terminal.CWriteLine("$eCreating directories...");
                    foreach (string Folder in Php.Folders)
                    {
                        try
                        {
                            string FullName = Path.Combine(Environment.CurrentDirectory, Folder);
                            if (!Directory.Exists(FullName))
                            {
                                Directory.CreateDirectory(FullName);
                                Terminal.CWriteLine("$aCreated directory: $f{0}/", Folder);
                            }
                        } catch(Exception) { Terminal.CWriteLine("$cFailed to create directory: $f{0}", Folder); return; }
                    }
                    Terminal.CWriteLine("$eChecking libs...");
                    string Includes = "";
                    string IncFormat = "\t\t{0}\r\n";
                    using (WebClient w = new WebClient())
                    {
                        foreach (PhpLib p in PhpLibs)
                        {
                            string FullName = Path.Combine(Environment.CurrentDirectory, p.FileName);
                            if (!File.Exists(FullName))
                            {
                                w.DownloadFile(p.Url, FullName);
                                Terminal.CWriteLine("$aDownloaded $f{0}", p.Name);
                            }
                            string Ext = Path.GetExtension(p.FileName).TrimStart('.');
                            if (Php.Imports.ContainsKey(Ext))
                            {
                                Includes += string.Format(IncFormat, string.Format(Php.Imports[Ext], p.FileName));
                            } else
                            {
                                Terminal.CWriteLine("$cUnknown lib format: $f{0}", Ext);
                            }
                        }
                    }
                    Terminal.CWriteLine("$eCreating files...");
                    File.WriteAllText("sys/header.php", string.Format(Php.Header, Includes));
                    Terminal.CWriteLine("$aCreated file: $f{0}", "sys/header.php");
                    foreach(KeyValuePair<string, string> Fil in Php.BaseFiles)
                    {
                        string FullName = Path.Combine(Environment.CurrentDirectory, Fil.Key);
                        if (!File.Exists(FullName))
                        {
                            File.WriteAllText(FullName, Fil.Value);
                            Terminal.CWriteLine("$aCreated file: $f{0}", Fil.Key);
                        }
                    }
                })
                .Describe("Creates php template with basic libraries");
            #endregion

            #region PHPMAKE
            Command
                .Make("phpmake")
                .SetFunc((a) =>
                {
                    string Name = a.Get(1);
                    string[] Files = new string[]
                    {
                         "{0}.php",
                         "js/{0}.js",
                         "css/{0}.css",
                    };
                    foreach (string FileName in Files)
                    {
                        string Template = "";
                        try
                        {
                            Template = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, string.Format(FileName, "template")));
                        } catch(IOException) { }
                        File.WriteAllText(Path.Combine(Environment.CurrentDirectory, string.Format(FileName, Name)), string.Format(Template, Name));
                        Terminal.CWriteLine("$aCreated file: $f{0}", string.Format(FileName, Name));
                    }
                })
                .Describe("Creates php file and corresponding css and js")
                .AddArg("FileName", "Name for the file", true);
            #endregion
            #endregion
        }
    }
}
