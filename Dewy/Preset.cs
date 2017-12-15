using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dewy
{
    class Preset
    {
        public static CommandPreset Regex = new CommandPreset
        {
            Switches = new List<string>() { "r" },
            HSwitches = new Dictionary<string, string>
            {
                { "r", "Regex mode" }
            },
        };
        public static CommandPreset FetchFile = new CommandPreset(Regex)
        {
            Switches = new List<string>() { "f", "d" },
            HSwitches = new Dictionary<string, string>
            {
                { "f", "Only files" },
                { "d", "Only directories" },
            },
        };
    }
}
