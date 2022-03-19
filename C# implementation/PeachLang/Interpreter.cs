using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PeachLang {
	public static partial class Interpreter {
		// These enums represent all the tokens, besides numbers, used in the language.
		private enum Seperator { Open, Close }
		private enum UnaryPrefixOp { Negate, IsPossible, IsCertain }
		private enum UnaryPostfixOp { GetOnePercent }
		private enum BinaryOp {
			IsEqual, IsLess, IsMore, IsNotEqual, IsLessOrEqual, IsMoreOrEqual,
			Add, Subtract, Multiply, Divide, Raise,
			Min, Avg, Max, Disjunct,
		}

		// Dictionary of nonnumeric tokens and corresponding strings
		private static readonly Dictionary<object, string> _tokensAndStringsDictionary = new () {
			{ Seperator.Open, "("},
			{ Seperator.Close, ")" },
			{ UnaryPrefixOp.Negate, "~" },
			{ UnaryPrefixOp.IsPossible, "<>" },
			{ UnaryPrefixOp.IsCertain, "[]" },
			{ UnaryPostfixOp.GetOnePercent, "%" },
			{ BinaryOp.IsEqual, "=" },
			{ BinaryOp.IsLess, "<" },
			{ BinaryOp.IsMore, ">" },
			{ BinaryOp.IsNotEqual, "~=" },
			{ BinaryOp.IsLessOrEqual, "<=" },
			{ BinaryOp.IsMoreOrEqual, ">=" },
			{ BinaryOp.Add, "+" },
			{ BinaryOp.Subtract, "-" },
			{ BinaryOp.Multiply, "*" },
			{ BinaryOp.Divide, "/" },
			{ BinaryOp.Raise, "^" },
			{ BinaryOp.Min, "!" },
			{ BinaryOp.Avg, "@" },
			{ BinaryOp.Max, "#" },
			{ BinaryOp.Disjunct, "$" }
		};

		// Converts parameter token to string using _tokensAndStringsDictionary. If token is not
		// in _tokensAndStringsDictionary, assumes token is a number and uses toString().
		private static string _tokenToString(object token) {
			if (_tokensAndStringsDictionary.TryGetValue (token, out string value)) return value;
			else return token.ToString ();
		}

		// Converts multiple tokens to a single string
		private static string _tokensToString (ArrayList tokens) {
			StringBuilder str = new (capacity: tokens.Count * 8); // should be plenty of capacity
			foreach (object token in tokens) {
				str.Append (_tokenToString (token) ?? string.Empty);
			}
			return str.ToString ();
		}

		// Tries to convert characters c and d to a nonnumeric token. If both can be converted,
		// they are, and the method returns 2; otherwise, if c can be converted, it is, and
		// the method returns 1; otherwise, the method returns 0. To convert a single character,
		// pass (char)0 or similar as d.
		private static int _tryConvert2CharsToToken(char c, char d, out object token) {
			switch(c) {
				case '(': token = Seperator.Open; return 1;
				case ')': token = Seperator.Close; return 1;
				case '!': token = BinaryOp.Min; return 1;
				case '@': token = BinaryOp.Avg; return 1;
				case '#': token = BinaryOp.Max; return 1;
				case '$': token = BinaryOp.Disjunct; return 1;
				case '%': token = UnaryPostfixOp.GetOnePercent; return 1;
				case '+': token = BinaryOp.Add; return 1;
				case '-': token = BinaryOp.Subtract; return 1;
				case '*': token = BinaryOp.Multiply; return 1;
				case '/': token = BinaryOp.Divide; return 1;
				case '^': token = BinaryOp.Raise; return 1;
				case '=': token = BinaryOp.IsEqual; return 1;
				case '<':
					switch(d) {
						case '>': token = UnaryPrefixOp.IsPossible; return 2;
						case '=': token = BinaryOp.IsLessOrEqual; return 2;
						default: token = BinaryOp.IsLess; return 1;
					}
				case '>':
					switch(d) {
						case '=': token = BinaryOp.IsMoreOrEqual; return 2;
						default: token = BinaryOp.IsMore; return 1;
					}
				case '[':
					switch (d) {
						case ']': token = UnaryPrefixOp.IsCertain; return 2;
						default: token = null; return 0;
					}
				case '~':
					switch (d) {
						case '=': token = BinaryOp.IsNotEqual; return 2;
						default: token = UnaryPrefixOp.Negate; return 1;
					}
				default: token = null; return 0;
			}
		}

		// Whether character c is one of the 11 numeric characters ".0123456789"
		private static bool _isNumericChar (char c) => '0' <= c && c <= '9' || c == '.';

		// Returns the number starting at index i in string input. Sets i to the index immediately
		// after the number. If a valid number is not found, returns -1 and sets err.
		private static decimal _scanNum (string input, ref int i, out string err) {
			int h = i;
			do ++i; while (i < input.Length && _isNumericChar (input[i]));
			string numStr = input[h..i];
			bool parsed = decimal.TryParse (numStr, out decimal num);
			err = parsed ? null : $"Could not parse number: {numStr}";
			return parsed ? num : -1m;
		}

		// Returns the token (number, variable, or nonnumeric token) starting at index i in string input.
		// Sets i to the index immediately after the token. If a valid token is not found, returns -1 and
		// sets err.
		private static object _scanToken(string input, ref int i, out string err) {
			char c = input[i];
			if (_isNumericChar (c)) {
				return _scanNum (input, ref i, out err);
			}
			else if (char.IsLetter(c)) {
				++i;
				return _getVar (c, out err);
			}
			else {
				char d = i + 1 < input.Length ? input[i + 1] : (char)0;
				int charsScanned = _tryConvert2CharsToToken (c, d, out object token);
				err = charsScanned > 0 ? null :
					$"Could not parse {c} {(d == (char)0 ? string.Empty : $"or {c}{d} ")}as token";
				i += (0 < charsScanned) ? charsScanned : (d == (char)0) ? 1 : 2;
				return token;
			}
		}

		// Attempts to convert the part of expr starting at index i to a single number. If
		// unsuccessful, sets err and errI and returns -1. If the error occured at a specific
		// location in expr, sets errI to that location; otherwise sets errI to -1.
		private static decimal _parseExpression (string expr, int i, out int errI, out string err) {
			ArrayList tokens = new ();
			err = null;
			errI = -1;
			while (i < expr.Length) {
				errI = i;
				object token = _scanToken (expr, ref i, out err);
				if (err != null) break;
				_append (tokens, token, out err);
				if (err != null) break;
			}
			errI = i < expr.Length ? errI : -1;
			if (err != null || tokens.Count != 1) {
				err = $"Expression reduced to \"" +_tokensToString (tokens) +
					(i < expr.Length ? "...\"" : '"') + (err == null ? string.Empty : $". {err}.");
				return -1;
			}
			if (tokens[0] is not decimal d) {
				err = $"Expression reduced to non-numeric token {_tokenToString (tokens[0])}";
				return -1;
			}
			return d;
		}

		// Memory for the language's 52 available variables, each represented by a single
		// character: {'A', 'B', ..., 'Z', 'a', 'b', ..., 'z'}
		private static decimal[] _vars = new decimal[52] {
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		};

		// Returns the index in _vars corresponding to character var. If var is not a valid
		// variable, returns -1.
		private static int _varIndex (char var) =>
			var < 'A' ? -1 :
			var <= 'Z' ? var - 'A' :
			var < 'a' ? -1 :
			var <= 'z' ? var - 'a' + 26 : -1;

		// Returns the value stored in var. If var is not a valid variable or has not been assigned,
		// returns -1 and sets err.
		private static decimal _getVar (char var, out string err) {
			int index = _varIndex (var);
			if (index < 0) {
				err = $"\"{var}\" is not a valid variable name";
				return -1;
			}
			decimal value = _vars[index];
			err = value >= 0 ? null : $"Variable {var} not assigned";
			return value;
		}

		// Parses a user-entered input string. If input is a valid expression, returns its result;
		// if input is a valid assignment, returns null; if input is invalid, sets err.
		public static object ParseInput (string input, out string err) {
			return ParseInput (input, out int _, out err);
		}

		// Parses a user-entered input string. If input is a valid expression, returns its result;
		// if input is a valid assignment, returns null. If input is invalid, sets err, and if the
		// error occured at a specific location in expr, sets errI to that location; otherwise sets
		// errI to -1.
		public static object ParseInput (string input, out int errI, out string err) {
			if (input.Length < 2 || input[1] != ':') {
				return _parseExpression (input, 0, out errI, out err);
			}
			else {
				char var = input[0];
				int index = _varIndex (var);
				if (index < 0) {
					err = $"\"{var}\" is not a valid variable name";
					errI = 0;
					return null;
				}
				decimal result = _parseExpression (input, 2, out errI, out err);
				if (err != null) return null;
				_vars[index] = result;
				return null;
			}
		}
	}
}
