﻿using System;
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
        public Dictionary<string, string> HParameters = new Dictionary<string, string>();
        public Dictionary<string, string> HSwitches = new Dictionary<string, string>();
        public List<Argument> Args = new List<Argument>();
        public List<string> Aliases = new List<string>();
        public Action<Parser> Callback = null;
        public string ExtraInfo = "";
        public Action Cancel = () => { };
        public bool Debug = false;
        public string Description = "Nothing here";
        public bool ParsePR = true;
        public bool ParseSW = true;
        public bool Documented = false;
        
        public Command(string Name, bool Debug = false)
        {
            this.Name = Name;
            this.Debug = Debug;
            Callback = (a) => Terminal.WriteLine("Not implemented!");
            Program.Cmds.Add(this);
        }
        public static Command Make(string Name, bool Debug = false)
        {
            return new Command(Name, Debug);
        }
        public Command AddArg(string Name, string Description = null, bool Required = false, bool Vararg = false)
        {
            Args.Add(new Argument
            {
                Name = Name,
                Description = Description,
                Required = Required,
                Vararg = Vararg
            });
            return this;
        }
        public Command AddText(params string[] Texts)
        {
            foreach (string Text in Texts)
            {
                ExtraInfo += Text + "\n";
            }
            return this;
        }
        public Command Describe(string Text)
        {
            Documented = true;
            Description = Text.TrimEnd('.');
            return this;
        }
        public Command Cleanup(Action Clean)
        {
            this.Cancel = Clean;
            return this;
        }
        public Command DescribeParam(string Key, string Text)
        {
            HParameters[Key] = Text.TrimEnd('.');
            return this;
        }
        public Command DescribeSwitch(string Key, string Text)
        {
            HSwitches[Key] = Text.TrimEnd('.');
            return this;
        }
        public Command DescribeParams(Dictionary<string, string> Help)
        {
            foreach (KeyValuePair<string, string> H in Help)
                HParameters[H.Key] = H.Value.TrimEnd('.');
            return this;
        }
        public Command DescribeSwitches(Dictionary<string, string> Help)
        {
            foreach (KeyValuePair<string, string> H in Help)
                HSwitches[H.Key] = H.Value.TrimEnd('.');
            return this;
        }
        public Command Alias(params string[] Names)
        {
            foreach(string Name in Names)
                Aliases.Add(Name.ToLower());
            return this;
        }
        public Command Alias(IEnumerable<string> Names)
        {
            foreach (string Name in Names)
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
        public Command SetFunc(Action<Parser> Fun)
        {
            Callback = Fun;
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
        public Command Preset(CommandPreset Preset)
        {
            foreach(KeyValuePair<string, string> Pr in Preset.Parameters)
                this.Params(Pr.Key, Pr.Value);
            foreach (string Sw in Preset.Switches)
                this.Switch(Sw);
            this.DescribeParams(Preset.HParameters);
            this.DescribeSwitches(Preset.HSwitches);
            foreach (CommandPreset p in Preset.Required)
            {
                if (p == Preset) continue;
                this.Preset(p);
            }
            return this;
        }
    }
    class Argument
    {
        public string Name = "";
        public string Description = "";
        public bool Required = false;
        public bool Vararg = false;
    }
    class CommandPreset
    {
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public List<string> Switches = new List<string>();
        public Dictionary<string, string> HParameters = new Dictionary<string, string>();
        public Dictionary<string, string> HSwitches = new Dictionary<string, string>();
        public CommandPreset[] Required = null;
        public CommandPreset(params CommandPreset[] Presets)
        {
            this.Required = Presets;
        }
    }
}
