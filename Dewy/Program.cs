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
        public static void RunCommand(string Command)
        {
            Parser p = new Parser();
            p.Parse(Command);
            string Cmd = p.Get(0).ToLower();
            Command ToRun = null;
            foreach ( Command c in Cmds)
            {
                if (c.Name.ToLower() == Cmd || c.Aliases.Contains(Cmd, StringComparer.CurrentCultureIgnoreCase))
                {
                    ToRun = c;
                    break;
                }
            }
            if (ToRun == null)
            {
                Terminal.CWriteLine("$cCommand not found.");
                return;
            }
            p = new Parser();
            p.Switches = ToRun.Switches;
            p.Parameters = ToRun.Parameters;
            p.Parse(Command);
            ToRun.Callback.Invoke(p);
            p.Get(0);
        }
    }
}
