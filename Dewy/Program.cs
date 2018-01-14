using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace Dewy
{
    class Program
    {
        public const RegexOptions GeneralUseRegex = RegexOptions.IgnoreCase | RegexOptions.Compiled;
        public const string Name = "Dewy";
        public static FileInfo Executable = null;
        public static List<Command> Cmds = new List<Command>();
        public static Thread Current = null;
        public static Parser CurrentParser = null;
        public static bool Debug = false;
        static void Main(string[] args)
        {
            Executable = new FileInfo(Environment.GetCommandLineArgs()[0]);
            Terminal.Setup();
            Commands.Load();
            ConsoleCancelEventHandler BlockCtrlC = delegate (object o, ConsoleCancelEventArgs e)
            {
                if (Current != null && Current.IsAlive)
                    Current.Abort();
                e.Cancel = true;
                CurrentParser.Exit = true;
            };
            Console.CancelKeyPress += BlockCtrlC;
            while (true)
            {
                Terminal.ResetColors();
                string Input = Terminal.PrettyInput();
                bool Ran = false;
                while (!Ran)
                {
                    
                    if (Input == null)
                    {
                        Input = Terminal.ReadLine();
                        continue;
                    }
                    if (Input != string.Empty)
                        RunCommand(Input);
                    Ran = true;
                }
            }
        }
        public static object RunCommand(string C)
        {
            Parser p = new Parser(false);
            p.ParseRegex(C);
            string Cmd = p.Get(0).ToLower();
            Command ToRun = FindCommand(Cmd);
            if (ToRun == null)
            {
                Terminal.CWriteLine("$cCommand not found");
                return null;
            }
            CurrentParser = new Parser();
            CurrentParser.Switches = new Dictionary<string, bool>(ToRun.Switches);
            CurrentParser.Parameters = new Dictionary<string, string>(ToRun.Parameters);
            CurrentParser.ParseRegex(C);

            CurrentParser.StartCapture();

            if (CurrentParser.Halt)
            {
                CurrentParser.StopCapture();
                return null;
            }

            Current = new Thread(() => { ToRun.Callback.Invoke(CurrentParser); });

            Console.CursorVisible = false;

            Current.Start();
            Current.Join();

            Console.CursorVisible = true;

            ToRun.Cancel.Invoke();

            CurrentParser.StopCapture();
            Terminal.Reset();

            /*if (p.ReturnType != null)
            {
                Terminal.CWriteLine("$e> $e{0}", p.ReturnObject);
            }*/

            return p.ReturnObject;
        }
        public static Command FindCommand(string Name)
        {
            foreach (Command c in Cmds)
            {
                if (c.Name.ToLower() == Name || c.Aliases.Contains(Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    return c;
                }
            }
            return null;
        }
        public static void Help(Command C)
        {
            Terminal.CWriteLine("$e{0} - $8{1}", C.Name.ToUpper(), C.Description);
            if (C.Args.Count > 0)
            {
                Terminal.CWriteLine("\n$aSyntax:");
                Terminal.CWrite("$e{0} ", C.Name.ToUpper());
                foreach(Argument Arg in C.Args)
                {
                    string Format = "$8[{0}{1}] ";
                    if (Arg.Required)
                        Format = "$8({0}{1}) ";
                    Terminal.CWrite(Format, Arg.Name.ToUpper(), Arg.Vararg ? "..." : "");
                }
                Terminal.WriteLine("\n");
                foreach (Argument Arg in C.Args)
                {
                    Terminal.CWriteLine("$e{0} $8- {1}", Arg.Name.ToUpper(), Arg.Description);
                }
            }
            if (C.Parameters.Count > 0)
            {
                Terminal.CWriteLine("\n$aParameters:");
                foreach(KeyValuePair<string, string> p in C.Parameters)
                {
                    Terminal.CWriteLine("$e-{0} $8{1}", p.Key, C.HParameters[p.Key]);
                }
            }
            if (C.Switches.Count > 0)
            {
                Terminal.CWriteLine("\n$aSwitches:");
                foreach (KeyValuePair<string, bool> p in C.Switches)
                {
                    Terminal.CWriteLine("$e/{0} $8{1}", p.Key, C.HSwitches[p.Key]);
                }
            }
            if (C.ExtraInfo.Length > 0)
            {
                Terminal.WriteLine();
                Terminal.CWrite(C.ExtraInfo);
                Terminal.WriteLine();
            }
            if (C.Aliases.Count > 0)
                Terminal.CWriteLine("\n$fAlso known as: $e{0}", Util.ArrayToStr(C.Aliases.Select(o => o.ToUpper()), ", "));
        }
        public static void SmallHelp(Command C, bool Force = false)
        {
            string Color = "$e";
            if (C.Debug)
            {
                if (!Program.Debug && !Force) return;
                Color = "$d";
            }
            if (C.Aliases.Count > 0)
            {
                string Aliases = Util.ArrayToStr(C.Aliases.Select(o => o.ToUpper()), ", ");
                Terminal.CWriteLine(Color + "{0}, {1} - $8{2}", C.Name.ToUpper(), Aliases, C.Description);
            }
            else
            {
                Terminal.CWriteLine(Color + "{0} - $8{1}", C.Name.ToUpper(), C.Description);
            }
        }
        public static void HelpAll()
        {
            foreach(Command C in Cmds)
            {
                SmallHelp(C);
            }
        }
    }
}
