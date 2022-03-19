using System;
using System.Diagnostics;

namespace PeachLang {
	static class PeachLangCommandPrompt {
		private static void _writeWithColor (string s, ConsoleColor c) {
			Console.ForegroundColor = c;
			Console.Write (s);
		}

		private static void _writeLineWithColor (string s, ConsoleColor c) {
			Console.ForegroundColor = c;
			Console.WriteLine (s);
		}

		private static void _writeWithColor (string[] s, params ConsoleColor[] c) {
			if (s.Length != c.Length) {
				throw new Exception ("Arrays s and c must have same length");
			}
			for (int i = 0; i < c.Length; ++i) {
				Console.ForegroundColor = c[i];
				Console.Write (s[i]);
			}
		}

		private static void _writeLineWithColor (string[] s, params ConsoleColor[] c) {
			_writeWithColor (s, c);
			Console.WriteLine ();
		}

		private static string _readLineWithColor (ConsoleColor c) {
			Console.ForegroundColor = c;
			return Console.ReadLine ();
		}

		private enum InputType { Help, Exit, Normal }

		private static InputType _getInputType (string input) {
			input = input.ToLower ();
			return (input == "?" || input == "help") ? InputType.Help :
				(input == string.Empty || input == "exit" || input == "halt" || input == "quit") ? InputType.Exit :
				InputType.Normal;
		}

		private const ConsoleColor
			_promptColor = ConsoleColor.Yellow,
			_inputColor = ConsoleColor.White,
			_outputColor = ConsoleColor.Green,
			_errorColor = ConsoleColor.Red;

		private const int _promptLength = 4;

		// Run once; return true if user wants to run again
		private static bool _runPeachLangPrompt() {
			_writeWithColor ("(`) ", _promptColor);
			string input = _readLineWithColor (_inputColor).Trim();
			switch (_getInputType (input)) {
				case InputType.Exit:
					return false;
				case InputType.Help:
					_writeWithColor (_help, _outputColor);
					return true;
			}
			while (input.EndsWith ('\\')) {
				input = input[..^1] + _readLineWithColor (_inputColor);
			}
			Stopwatch sw = new ();
			sw.Start ();
			object result = Interpreter.ParseInput (input, out int errI, out string err);
			sw.Stop ();
			if (err != null) {
				_writeLineWithColor (
					(errI < 0) ? err : new string (' ', _promptLength + errI) + "^\n" + err, _errorColor);
			}
			else if (result != null) {
				_writeLineWithColor ($"{result}\t[{sw.ElapsedMilliseconds} ms]", _outputColor);
			}
			return true;
		}

		public static void Main () {
			ConsoleColor originalColor = Console.ForegroundColor;
			while (_runPeachLangPrompt ());
			Console.ForegroundColor = originalColor;
		}

		private const string _help =
			"==== Peach (`) : An interpreted language for fuzzy logic ====\n" +
			"One data type: Nonnegative decimal number (stored in base 10, not base 2)\n" +
			"Unary operators\n" +
			"  Negation, possibility, certainity: ~ <> []\n" +
			"  One percent (postfix): %\n" +
			"Binary operators\n" +
			"  Standard arithmetic and comparison: + - * / ^ = ~= < <= > >=\n" +
			"  Minimum, average, maximum: ! @ #\n" +
			"  Disjunction (sum minus product): $\n" +
			"Operators do not have precedence. Use parentheses for grouping.\n" +
			"(`) [expression]\n" +
			"  Display result of expression\n" +
			"Use \"\\\" to continue an expression on the next line\n" +
			"(`) p:[expression]" +
			"  Assign result of expression to variable p\n" +
			"52 variables are available, represented by the 46 case-sensitive letters\n";
	}
}
