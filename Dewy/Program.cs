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
        static void Main(string[] args)
        {
            Terminal.Setup();
            Commands.Load();
            while (true)
            {
                Terminal.ResetColors();
                string Input = Terminal.PrettyInput();
                RunCommand(Input);
            }
        }
        public static void RunCommand(string C)
        {
            Parser p = new Parser();
            p.Parse(C);
            string Cmd = p.Get(0).ToLower();
            Command ToRun = FindCommand(Cmd);
            if (ToRun == null)
            {
                Terminal.CWriteLine("$cCommand not found");
                return;
            }
            p = new Parser();
            p.Switches = new Dictionary<string, bool>(ToRun.Switches);
            p.Parameters = new Dictionary<string, string>(ToRun.Parameters);
            p.Parse(C);
            ToRun.Callback.Invoke(p);
            p.Get(0);
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
