using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Dewy
{
    class Parser
    {
        /*
            ^-([a-z\d]+)$
            ^/([a-z\d]+)$
            (((?<!\\)['"])(.*?)(?<!\\)\2|([^\s]+))
            ^(['"])\\1$
        */
        public static Regex FindParam = new Regex("^-([a-z\\d]+)$", Program.GeneralUseRegex);
        public static Regex FindSwitch = new Regex("^/([a-z\\d]+)$", Program.GeneralUseRegex);
        public static string Delimiters = "\"'";
        public static Regex FixEmpty = new Regex(string.Format("^([{0}])\\1$", Regex.Escape(Delimiters)), Program.GeneralUseRegex);
        public static Regex RegParse = new Regex(string.Format("(((?<!\\\\)[{0}])(.*?)(?<!\\\\)\\2|([^\\s]+))", Regex.Escape(Delimiters)), Program.GeneralUseRegex);
        public string Original = "";
        public List<string> Arguments = new List<string>();
        public List<string> Raw = new List<string>();
        public List<string> sRaw = new List<string>();
        public List<ParsedArg> Parsed = new List<ParsedArg>();
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public Dictionary<string, bool> Switches = new Dictionary<string, bool>();
        public bool Exit = false;
        public object ReturnObject = null;
        public Type ReturnType = null;
        public bool ExpandVariables = true;

        public Parser(bool ExpandVariables = true)
        {
            this.ExpandVariables = ExpandVariables;
        }
        public ParsedArg[] Fetch(ArgTypes Type = ArgTypes.Raw)
        {
            return Parsed.Where(o => (o.Type & Type) == Type).ToArray();
        }
        public int Count(ArgTypes Type = ArgTypes.Raw)
        {
            return Fetch(Type).Length;
        }
        public int IntParam(string P, int Default = 0)
        {
            string s = Param(P);
            int r = 0;
            if(int.TryParse(s, out r))
                return r;
            return Default;
        }
        public int Int(int P, int Default = 0)
        {
            string s = Get(P);
            int r = 0;
            if (int.TryParse(s, out r))
                return r;
            return Default;
        }
        public void Return(Object Return = null)
        {
            if (null == Return)
            {
                this.ReturnObject = null;
                this.ReturnType = null;
                return;
            }
            this.ReturnObject = Return;
            this.ReturnType = Return.GetType();
        }
        public void PrintReturn(string Format, object Return)
        {
            Terminal.CWriteLine(Format, Return);
            this.Return(Return);
        }
        public void AddParameter(string Name, string Default = "")
        {
            Parameters[Name.ToLower()] = Default;
        }
        public void AddParameter(IEnumerable<string> Names)
        {
            foreach (string Name in Names)
                Parameters[Name.ToLower()] = "";
        }
        public void AddSwitch(string Name)
        {
            Switches[Name.ToLower()] = false;
        }
        public void AddSwitch(IEnumerable<string> Names)
        {
            foreach (string Name in Names)
                Switches[Name.ToLower()] = false;
        }
        public bool Switch(string Name)
        {
            if (Switches.ContainsKey(Name.ToLower()))
            {
                return Switches[Name.ToLower()];
            }
            return false;
        }
        public string Param(string Name)
        {
            if (Parameters.ContainsKey(Name.ToLower()))
            {
                return Parameters[Name.ToLower()];
            }
            return "";
        }
        public bool ParsePR = true;
        public bool ParseSW = true;
        public string Get(int i, string Default = "")
        {
            if (i >= Count())
                return Default;
            return Fetch()[i].Value;
        }
        public string[] VarArgs(bool NeverEmpty = false)
        {
            string[] Ret = Fetch().Select(o => o.Value).Skip(1).ToArray();
            if (NeverEmpty && Ret.Length == 0) return Util.Array("");
            return Ret;
        }
        public string FullArg()
        {
            try
            {
                return Original.Substring(Get(0).Length + 1).Trim();
            } catch(Exception) {  }
            return "";
        }
        public ParsedArg RegisterArg(ParsedArg Arg)
        {
            Arguments.Add(Arg.Value);
            Parsed.Add(Arg);
            return Arg;
        }
        public ParsedArg RegisterArg(string Value, string Raw, ArgTypes Type = ArgTypes.Raw)
        {
            return RegisterArg(new ParsedArg(Value, Raw, Type));
        }

        string[] Operators = new string[] { ">", ">>" };
        bool CaptureOutput = false;
        bool Overwrite = false;
        FileAttributes OutputAttr = 0;
        FileInfo OutFile = null;
        ParsedArg OutputFile = null;
        StreamWriter Output = null;
        string[] HideKeywords = new string[] { "nul", "null", "nil", "none" };
        public bool Halt = false;
        public void StartCapture()
        {
            if (CaptureOutput)
            {
                if (HideKeywords.Contains(OutputFile.Raw.ToLower()) || OutputFile.Value == string.Empty)
                {
                    Terminal.Hide = true;
                    return;
                }
                try
                {
                    OutFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, OutputFile.Value));
                    OutputAttr = OutFile.Attributes;
                    OutFile.Attributes = FileAttributes.Normal;
                    if (Overwrite) OutFile.Delete();
                    Output = new StreamWriter(OutFile.Open(FileMode.OpenOrCreate));
                    Output.AutoFlush = true;
                    Terminal.RegisterWriter(Output);
                }
                catch (Exception ex)
                {
                    Terminal.CWriteLine("$c{0}", ex.Message);
                    Halt = true;
                }
            }
        }
        public void StopCapture()
        {
            if (CaptureOutput && Output != null)
            {
                Terminal.UnregisterWriter(Output);
                Output.Flush();
                Output.Dispose();
                Output = null;
                OutFile.Attributes = OutputAttr;
            }
        }
        public void PostParse()
        {
            bool Ignore = false;
            for (int i = 0; i < Raw.Count; i++)
            {
                if (Ignore) { Ignore = false; continue; }
                string r = Raw[i];
                string s = sRaw[i];
                if (r == s && Operators.Contains(s))
                {
                    if (s == ">" || s == ">>")
                    {
                        Overwrite = s == ">";
                        RegisterArg(r, s, ArgTypes.Operator);
                        string pValue = Raw.Count > i + 1 ? Raw[i + 1].ToLower() : "";
                        string spValue = sRaw.Count > i + 1 ? sRaw[i + 1].ToLower() : "";
                        Arguments.Add(pValue);
                        ArgTypes Also = (pValue.Length != spValue.Length) ? ArgTypes.Quoted : 0;
                        CaptureOutput = true;
                        OutputFile = RegisterArg(Environment.ExpandEnvironmentVariables(pValue), spValue, ArgTypes.Output | Also);
                        break;
                    }
                }
                if (ParsePR)
                {
                    Match Param = FindParam.Match(s);
                    if (Param.Success)
                    {
                        string pName = Param.Groups[1].Value.ToLower();
                        string pValue = Raw.Count > i + 1 ? Raw[i + 1].ToLower() : "";
                        string spValue = sRaw.Count > i + 1 ? sRaw[i + 1].ToLower() : "";
                        if (Parameters.ContainsKey(pName))
                            Parameters[pName] = pValue;
                        Ignore = true;
                        RegisterArg(pName, s, ArgTypes.ParamName);
                        RegisterArg(pValue, spValue, ArgTypes.ParamValue);
                        continue;
                    }
                }
                if (ParseSW)
                {
                    Match Switch = FindSwitch.Match(s);
                    if (Switch.Success)
                    {
                        string sName = Switch.Groups[1].Value.ToLower();
                        if (Switches.ContainsKey(sName))
                            Switches[sName] = true;
                        RegisterArg(sName, s, ArgTypes.Switch);
                        continue;
                    }
                }
                Arguments.Add(r);
                ArgTypes Additional = (r.Length != s.Length) ? ArgTypes.Quoted : 0;
                if (Additional == 0 && FixEmpty.IsMatch(r)) r = "";
                RegisterArg(Environment.ExpandEnvironmentVariables(r), s, ArgTypes.Raw | Additional);
            }
        }
        public void Parse(string Input)
        {
            this.Original = Input;
            string Cur = "";
            int Pos = -1;
            bool Escaping = false;
            char EscapeChar = '\0';
            bool EscapeOnce = false;
            Raw = new List<string>();
            foreach (char c in Original + '\0')
            {
                Pos++;
                if (EscapeOnce) { Cur += c; EscapeOnce = false; continue; }
                //if (c == '&' && c != '\0') { EscapeOnce = true; continue; }
                if (Escaping)
                {
                    if (c == EscapeChar)
                    {
                        Escaping = false;
                        Raw.Add(Cur);
                        sRaw.Add(EscapeChar + Cur + EscapeChar);
                        Cur = "";
                        continue;
                    }
                    Cur += c;
                    continue;
                }
                if (Delimiters.Contains(c)) { Escaping = true; EscapeChar = c; continue; }
                if (c == ' ' || c == '\0')
                {
                    if (Cur.Length == 0) { continue; }
                    Raw.Add(Cur);
                    sRaw.Add(Cur);
                    Cur = "";
                    continue;
                }
                Cur += c;
            }
            PostParse();
        }
        public void ParseRegex(string Input)
        {
            this.Original = Input;
            MatchCollection c = RegParse.Matches(Input);
            foreach (Match m in c)
            {
                if (m.Groups[3].Value.Length > 0)
                    Raw.Add(m.Groups[3].Value
                        .Replace("\\\"", "\"")
                        .Replace("\\'", "'")
                    );
                else
                    Raw.Add(m.Groups[1].Value);
                sRaw.Add(m.Groups[1].Value);
            }
            PostParse();
        }
    }
    [Flags]
    public enum ArgTypes : byte
    {
        Raw = 1,
        Operator = 2,
        Quoted = 4,
        Switch = 8,
        ParamName = 16,
        ParamValue = 32,
        Output = 64
    }
    class ParsedArg
    {
        public string Raw = null;
        public string Value = null;
        public ArgTypes Type = ArgTypes.Raw;
        public ParsedArg(string Value, string Raw, ArgTypes Type = ArgTypes.Raw)
        {
            this.Value = Value;
            this.Raw = Raw;
            this.Type = Type;
        }
        public string TypeName()
        {
            List<string> All = new List<string>();
            foreach(ArgTypes t in Enum.GetValues(typeof(ArgTypes)))
                if (t == (t & Type))
                    All.Add(Enum.GetName(typeof(ArgTypes), t));
            return string.Join("|", All.ToArray());
        }
    }
}
