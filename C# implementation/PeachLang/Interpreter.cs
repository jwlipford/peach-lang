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
			Add, AbsDiff, Multiply, Divide, Raise,
			Min, Avg, Max, Disjunct,
		}

		/// <summary>
		/// Dictionary of nonnumeric tokens and corresponding strings
		/// </summary>
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
			{ BinaryOp.AbsDiff, "-" },
			{ BinaryOp.Multiply, "*" },
			{ BinaryOp.Divide, "/" },
			{ BinaryOp.Raise, "^" },
			{ BinaryOp.Min, "!" },
			{ BinaryOp.Avg, "@" },
			{ BinaryOp.Max, "#" },
			{ BinaryOp.Disjunct, "$" }
		};

		/// <summary>
		/// Converts parameter <c>token</c> to a string using <c>_tokensAndStringsDictionary</c>.
		/// If <c>token</c> is not in <c>_tokensAndStringsDictionary</c>, assumes token is a number and
		/// uses <c>toString()</c>.
		/// </summary>
		private static string _tokenToString(object token) {
			if (_tokensAndStringsDictionary.TryGetValue (token, out string value)) return value;
			else return token.ToString ();
		}

		/// <summary>
		/// Converts multiple tokens to a single using the <c>tokenToString</c> method
		/// </summary>
		private static string _tokensToString (ArrayList tokens) {
			StringBuilder str = new (capacity: tokens.Count * 8); // should be plenty of capacity
			foreach (object token in tokens) {
				str.Append (_tokenToString (token) ?? string.Empty);
			}
			return str.ToString ();
		}

		/// <summary>
		/// Tries to convert characters <c>c</c> and <c>d</c> to a nonnumeric <c>token</c>.
		/// To convert a single character, pass it as <c>c</c> and <c>(char)0</c> or similar as
		/// <c>d</c>.
		/// If both characters can be converted, they are, and the method returns 2;
		/// otherwise, if <c>c</c> can be converted, it is, and the method returns 1;
		/// otherwise, the method returns 0.
		/// </summary>
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
				case '-': token = BinaryOp.AbsDiff; return 1;
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

		/// <summary>
		/// Checks whether a character is one of the 11 numeric characters ".0123456789"
		/// </summary>
		private static bool _isNumericChar (char c) => '0' <= c && c <= '9' || c == '.';

		/// <summary>
		/// Returns the number starting at index <c>i</c> in string <c>input</c>.
		/// Sets <c>i</c> to the index immediately after the number.
		/// If a valid number is not found, returns -1 and sets <c>err</c>.
		/// </summary>
		private static decimal _scanNum (string input, ref int i, out string err) {
			int h = i;
			do ++i; while (i < input.Length && _isNumericChar (input[i]));
			string numStr = input[h..i];
			bool parsed = decimal.TryParse (numStr, out decimal num);
			err = parsed ? null : $"Could not parse number: {numStr}";
			return parsed ? num : -1m;
		}

		/// <summary>
		/// Returns the token (number, variable, or nonnumeric token) starting at index <c>i</c> in
		/// string <c>input</c>.
		/// Sets <c>i</c> to the index immediately after the token. If a valid token is not found,
		/// returns -1 and sets <c>err</c>.
		/// </summary>
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

		/// <summary>
		/// Attempts to convert the part of <c>expr</c> starting at index <c>i</c> to a single
		/// number.
		/// If unsuccessful, sets <c>err</c> and <c>errI</c> and returns -1;
		/// additionally, if the error occured at a specific location in <c>expr</c>,
		/// sets <c>errI</c> to that location; otherwise sets <c>errI</c> to -1.
		/// </summary>
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
			if (err != null) {
				err = $"Expression reduced to \"{_tokensToString (tokens)}...\" {err}.";
				return -1;
			}

			errI = -1;
			if (tokens.Count != 1) {
				err = $"Expression reduced to \"{_tokensToString (tokens)}\"";
				return -1;
			}
			if (tokens[0] is not decimal d) {
				err = $"Expression reduced to non-numeric token \"{_tokenToString (tokens[0])}\"";
				return -1;
			}
			return d;
		}

		/// <summary>
		/// Memory for the language's 52 available variables, each represented by a single
		/// character: {'A', 'B', ..., 'Z', 'a', 'b', ..., 'z'}
		/// </summary>
		private static readonly decimal[] _vars = new decimal[52] {
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		};

		/// <summary>
		/// Returns the index in <c>_vars</c> corresponding to character <c>var</c>. If <c>var</c>
		/// is not a valid variable, returns -1.
		/// </summary>
		private static int _varIndex (char var) =>
			var < 'A' ? -1 :
			var <= 'Z' ? var - 'A' :
			var < 'a' ? -1 :
			var <= 'z' ? var - 'a' + 26 : -1;

		/// <summary>
		/// Returns the value stored in <c>var</c>. If <c>var</c> is not a valid variable or has
		/// not been assigned, returns -1 and sets err.
		/// </summary>
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

		/// <summary>
		/// Parses a user-entered input string.
		/// If <c>input</c> is a valid expression, its result is returned.
		/// If <c>input</c> is a valid assignment, <c>null</c> is returned.
		/// If <c>input</c> is invalid, <c>err</c> is set and some object is returned.
		/// </summary>
		public static object ParseInput (string input, out string err) {
			return ParseInput (input, out int _, out err);
		}

		/// <summary>
		/// Parses a user-entered input string.
		/// If <c>input</c> is a valid expression, its result is returned.
		/// If <c>input</c> is a valid assignment, <c>null</c> is returned.
		/// If <c>input</c> is invalid, <c>err</c> is set, and some object is returned;
		/// additionally, if the error occured at a specific location in input,
		/// <c>errI</c> is set to that location; otherwise, <c>errI</c> is set to -1.
		/// </summary>
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
