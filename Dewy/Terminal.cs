using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

namespace Dewy
{
    class Terminal
    {
        public static Regex ColorRegex = null;
        public static List<TextWriter> Captures = new List<TextWriter>();
        public static Dictionary<char, ConsoleColor> Colors = new Dictionary<char, ConsoleColor>
        {
            { '0', ConsoleColor.Black },
            { '1', ConsoleColor.DarkBlue },
            { '2', ConsoleColor.DarkGreen },
            { '3', ConsoleColor.DarkCyan },
            { '4', ConsoleColor.DarkRed },
            { '5', ConsoleColor.DarkMagenta },
            { '6', ConsoleColor.DarkYellow },
            { '7', ConsoleColor.Gray },
            { '8', ConsoleColor.DarkGray },
            { '9', ConsoleColor.Blue },
            { 'a', ConsoleColor.Green },
            { 'b', ConsoleColor.Cyan },
            { 'c', ConsoleColor.Red },
            { 'd', ConsoleColor.Magenta },
            { 'e', ConsoleColor.Yellow },
            { 'f', ConsoleColor.White },
        };

        public static void Setup()
        {
            ColorRegex = new Regex("(?<!\\\\)\\$[a-fr\\d]", Program.GeneralUseRegex);
            Console.Title = Program.Name;
            Console.BufferHeight = 800;
            Console.CursorSize = 1;
            Fingerprint();
            ResetColors();
        }

        public static ConsoleColor DefaultForeground = ConsoleColor.White;
        public static ConsoleColor DefaultBackground = ConsoleColor.Black;
        public static void SetForeColor(ConsoleColor c)
        {
            Console.ForegroundColor = c;
        }
        public static void SetForeColor(char c)
        {
            char b = c.ToString().ToLower()[0];
            if (b == 'r')
                Console.ForegroundColor = DefaultForeground;
            if (!Colors.ContainsKey(c))
                return;
            Console.ForegroundColor = Colors[b];
        }
        public static void SetBackColor(ConsoleColor c)
        {
            Console.BackgroundColor = c;
        }
        public static void SetBackColor(char c)
        {
            char b = c.ToString().ToLower()[0];
            if (b == 'r')
                Console.BackgroundColor = DefaultBackground;
            if (!Colors.ContainsKey(c))
                return;
            Console.BackgroundColor = Colors[b];
        }
        public static void ResetColors()
        {
            Console.BackgroundColor = DefaultBackground;
            Console.ForegroundColor = DefaultForeground;
        }
        public static void Reset()
        {
            ResetColors();
            Terminal.Hide = false;
        }

        public static bool Hide = false;
        public static bool CanWrite()
        {
            if (Hide) return false;
            return true;
        }

        public static void RegisterWriter(TextWriter Tw)
        {
            Captures.Add(Tw);
        }

        public static void UnregisterWriter(TextWriter Tw)
        {
            Captures.Remove(Tw);
        }

        public static void iWrite(string Format = "", params object[] Args)
        {
            if (!CanWrite()) return;
            int i = 0;
            foreach (string ToWrite in Format.Split('\n'))
            {
                string CLineBreak = ((i   > 0) ? "\n" : "");
                string FLineBreak = ((i++ > 0) ? "\r\n" : "");
                Console.Write(ToWrite + CLineBreak, Args);
                foreach (TextWriter Writer in Captures)
                    Writer.Write(ToWrite + FLineBreak, Args);
            }
        }

        public static void Fingerprint()
        {
            string[] Mark = new string[] {
                @"  $f╔$f══════════════════════════════$f╗",
                @"  $f║$c  _____                       $f║",
                @"  $f║$c |  __ \                      $f║",
                @"  $f║$c | |  | | _____      ___   _  $f║",
                @"  $f║$c | |  | |/ _ \ \ /\ / / | | | $f║",
                @"  $f║$c | |__| |  __/\ V  V /| |_| | $f║",
                @"  $f║$c |_____/ \___| \_/\_/  \__, | $f║",
                @"  $f║$c                        __/ | $f║",
                @"  $f║$c  $aDeveloped by Falofa  $c|___/  $f║",
                @"  $f╚$f══════════════════════════════$f╝"
            };
            WriteLine();
            foreach (string Line in Mark)
                CWriteLine(Line);
            WriteLine();
        }

        public static void WriteLine(string Format = "", params object[] Args)
        {
            iWrite(Format + '\n', Args);
        }
        public static void Write(string Format = "", params object[] Args)
        {
            iWrite(Format, Args);
        }

        public static void CWriteLine(string Format = "", params object[] Args)
        {
            CWrite(Format + '\n', Args);
        }
        public static void CWrite(string Format = "", params object[] Args)
        {
            ConsoleColor Original = Console.ForegroundColor;
            int Position = 0;
            string[] Split = ColorRegex.Split(Format);
            foreach (string c in Split)
            {
                if (Position > 0)
                {
                    char Col = Format.ToLower()[Position - 1];
                    switch(Col)
                    {
                        case 'r':
                            Console.ForegroundColor = DefaultForeground;
                            break;
                        default:
                            Console.ForegroundColor = Colors[Col];
                            break;
                    }
                }
                iWrite(c.Replace("\\$", "$"), Args);
                Position += c.Length + 2;
            }
            Console.ForegroundColor = Original;
        }

        public static string ReadLine()
        {
            return Console.ReadLine();
        }
        public static ConsoleKeyInfo ReadKey(bool Hide = false)
        {
            return Console.ReadKey(Hide);
        }
        public static void Wait()
        {
            Console.ReadKey();
        }

        public static string PrettyInput()
        {
            if (Console.CursorLeft != 0)
                WriteLine();
            Write("> ");
            return ReadLine();
        }
        public static void Clear()
        {
            Console.Clear();
        }
        public static CursorPosition GetPos()
        {
            return new CursorPosition()
            {
                x = Console.CursorLeft,
                y = Console.CursorTop
            };
        }
        public static void SetPos(CursorPosition Pos)
        {
            Console.SetCursorPosition(Pos.x, Pos.y);
        }
    }
    public struct CursorPosition
    {
        public int x;
        public int y;
    }
}
