using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Dewy
{
    class Program
    {
        public const RegexOptions GeneralUseRegex = RegexOptions.IgnoreCase | RegexOptions.Compiled;
        public const string Name = "Dewy";
        public static List<Command> Cmds = new List<Command>();
        public static Thread Current = null;
        public static Object Return = null;
        static void Main(string[] args)
        {
            Terminal.Setup();
            Commands.Load();
            ConsoleCancelEventHandler BlockCtrlC = delegate (object o, ConsoleCancelEventArgs e)
            {
                if (Current != null && Current.IsAlive)
                    Current.Abort();
                e.Cancel = true;
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
            Return = null;
            Parser p = new Parser();
            p.Parse(C);
            string Cmd = p.Get(0).ToLower();
            Command ToRun = FindCommand(Cmd);
            if (ToRun == null)
            {
                Terminal.CWriteLine("$cCommand not found");
                return null;
            }
            p = new Parser();
            p.Switches = new Dictionary<string, bool>(ToRun.Switches);
            p.Parameters = new Dictionary<string, string>(ToRun.Parameters);
            p.Parse(C);


            Current = new Thread(() => { ToRun.Callback.Invoke(p); });

            Console.CursorVisible = false;

            Current.Start();
            Current.Join();

            Console.CursorVisible = true;

            return Return;
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
            if (C.Aliases.Count > 0)
                Terminal.CWriteLine("\n$fAlso known as: $e{0}", Util.ArrayToStr(C.Aliases.Select(o => o.ToUpper()), ", "));
        }
        public static void HelpAll()
        {
            foreach(Command C in Cmds)
            {
                if (C.Aliases.Count > 0)
                {
                    string Aliases = Util.ArrayToStr(C.Aliases.Select(o => o.ToUpper()), ", ");
                    Terminal.CWriteLine("$e{0}, {1} - $8{2}", C.Name.ToUpper(), Aliases, C.Description);
                }
                else
                {
                    Terminal.CWriteLine("$e{0} - $8{1}", C.Name.ToUpper(), C.Description);
                }
            }
        }
    }
}
