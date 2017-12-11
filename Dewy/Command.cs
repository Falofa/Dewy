using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dewy
{
    class Command
    {
        public string Name = null;
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public Dictionary<string, bool> Switches = new Dictionary<string, bool>();
        public List<string> Aliases = new List<string>();
        public Action<Parser> Callback = null;
        public string Description = "";
        public bool ParsePR = true;
        public bool ParseSW = true;

        public Command(string Name)
        {
            this.Name = Name;
            Callback = (a) => Terminal.WriteLine("Not implemented!");
            Program.Cmds.Add(this);
        }
        public static Command Make(string Name)
        {
            return new Command(Name);
        }
        public Command Alias(string Name)
        {
            Aliases.Add(Name.ToLower());
            return this;
        }
        public Command Param(string Name, string Default = "")
        {
            Parameters[Name] = Default;
            return this;
        }
        public Command Param(IEnumerable<string> Names)
        {
            foreach (string Name in Names)
            {
                Parameters[Name] = "";
            }
            return this;
        }
        public Command Params(params string[] Names)
        {
            foreach (string Name in Names)
            {
                Parameters[Name] = "";
            }
            return this;
        }
        public Command Switch(string Name)
        {
            Switches[Name] = false;
            return this;
        }
        public Command Switch(IEnumerable<string> Names)
        {
            foreach (string Name in Names)
            {
                Switches[Name] = false;
            }
            return this;
        }
        public Command Switch(params string[] Names)
        {
            foreach (string Name in Names)
            {
                Switches[Name] = false;
            }
            return this;
        }
        public Command SetFunc( Action<Parser> Fun )
        {
            Callback = Fun;
            return this;
        }
        public Command Desc(string Text)
        {
            Description = Text;
            return this;
        }
        public Command ParseParameters(bool Parse)
        {
            ParsePR = Parse;
            return this;
        }
        public Command ParseSwitches(bool Parse)
        {
            ParseSW = Parse;
            return this;
        }
        public Command Raw(bool Parse)
        {
            ParsePR = !Parse;
            ParseSW = !Parse;
            return this;
        }
        /*public Command Save()
        {
            if (!Program.Cmds.Contains(this))
                Program.Cmds.Add(this);
            return this;
        }*/
    }
}
