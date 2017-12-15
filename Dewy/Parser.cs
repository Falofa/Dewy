using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dewy
{
    class Parser
    {
        Regex FindParam = new Regex("^-([a-z]+)$", Program.GeneralUseRegex);
        Regex FindSwitch = new Regex("^/([a-z]+)$", Program.GeneralUseRegex);
        string Delimiters = "\"'";
        string Original = "";
        List<string> Arguments = new List<string>();
        List<string> Raw = new List<string>();
        List<string> sRaw = new List<string>();
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public Dictionary<string, bool> Switches = new Dictionary<string, bool>();
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
            if (i >= Arguments.Count)
                return Default;
            return Arguments[i];
        }
        public string[] VarArgs(bool NeverEmpty = false)
        {
            string[] Ret = Arguments.Skip(1).ToArray();
            if (NeverEmpty && Ret.Length == 0) return Util.Array("");
            return Ret;
        }
        public string FullArg()
        {
            return Original.Substring(Get(0).Length + 1).Trim();
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
                if (c == '&' && c != '\0') { EscapeOnce = true; continue; }
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
            bool Ignore = false;
            for (int i = 0; i < Raw.Count; i++)
            {
                if (Ignore) { Ignore = false; continue; }
                string r = Raw[i];
                string s = sRaw[i];
                if (ParsePR)
                {
                    Match Param = FindParam.Match(s);
                    if (Param.Success)
                    {
                        string pName = Param.Groups[1].Value.ToLower();
                        string pValue = Raw.Count > i + 1 ? Raw[i + 1].ToLower() : "";
                        if (Parameters.ContainsKey(pName))
                            Parameters[pName] = pValue;
                        Ignore = true;
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
                        continue;
                    }
                }
                Arguments.Add(r);
            }
        }
    }
}
