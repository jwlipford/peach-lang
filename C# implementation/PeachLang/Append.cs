using System.Collections;

namespace PeachLang {
	public static partial class Interpreter {
		/// <summary>
		/// Tries to append <c>token</c>, which should be a unary prefix operator or open seperator
		/// ("<c>(</c>"), to list <c>tokens</c>.
		/// If the previous token is a unary prefix operator, binary prefix operator, or open
		/// seperator, or does not exist (<c>tokens</c> is empty),
		/// then this method succeeds and <c>err</c> is set to null;
		/// otherwise, <c>err</c> is set to an appropriate error message.
		/// </summary>
		private static void _appendUnaryPrefixOpOrSeperatorOpen (ArrayList tokens, object token, out string err) {
			object prior = (tokens.Count == 0) ? null : tokens[^1];
			err = (prior == null || prior is UnaryPrefixOp || prior is BinaryOp || prior is Seperator.Open)
				? null : $"\"{_tokenToString (token)}\" appended after \"{_tokenToString (tokens[^1])}\"";
			if (err == null)
				tokens.Add (token);
		}

		/// <summary>
		/// Tries to append a close seperator ("<c>)</c>") to list <c>tokens</c>.
		/// If the previous two tokens are an open seperator ("<c>(</c>") and a number,
		/// then this method succeeds and <c>err</c> is set to null;
		/// otherwise, <c>err</c> is set to an appropriate error message.
		/// </summary>
		private static void _appendSeperatorClose (ArrayList tokens, out string err) {
			if (tokens.Count == 0) {
				err = "\")\" was first token";
			}
			else if (tokens[^1] is not decimal d) {
				err = $"\")\" appended after non-numeric token \"{_tokenToString (tokens[^1])}\"";
			}
			else if (tokens.Count == 1) {
				err = $"\")\" was second token, preceded by number {_tokenToString (tokens[^1])}";
			}
			else if (tokens[^2] is not Seperator.Open) {
				err = $"\")\" appended after non-\"(\" token \"{_tokenToString (tokens[^2])}\" and " +
					$"number {_tokenToString (tokens[^1])}";
			}
			else {
				tokens.RemoveRange (tokens.Count - 2, 2);
				_appendDecimal (tokens, d, out err);
			}
		}

		/// <summary>
		/// Tries to append <c>token</c>, a unary postfix operator, to list <c>tokens</c>.
		/// If the previous token is a number, then this method succeeds;
		/// otherwise, <c>err</c> is set to an appropriate error message.
		/// </summary>
		/// <remarks>
		/// Only one unary postfix operator is supported: the "one percent" operator "<c>%</c>".
		/// This method is included simply for the sake of extensibility.
		/// </remarks>
		private static void _appendUnaryPostfixOp (ArrayList tokens, UnaryPostfixOp u, out string err) {
			if (tokens.Count == 0) {
				err = $"Unary postfix operator \"{_tokenToString (u)}\" was first token";
			}
			else if (tokens[^1] is not decimal d) {
				err = $"Unary postfix operator \"{_tokenToString (u)}\" appended after non-numeric token "
					+ _tokenToString (tokens[^1]);
			}
			else {
				decimal result = _evalUnaryPostfixOp (u, d, out err);
				if (err == null) {
					tokens.RemoveAt (tokens.Count - 1);
					_appendDecimal (tokens, result, out err);
				}
			}
		}

		private static void _appendBinaryOp (ArrayList tokens, BinaryOp b, out string err) {
			if (tokens.Count == 0) {
				err = $"Binary operator \"{_tokenToString(b)}\" was first token";
			}
			else if (tokens[^1] is not decimal) {
				err = $"Binary operator \"{_tokenToString (b)}\" " +
					$"appended after non-numeric token \"{_tokenToString (tokens[^1])}\"";
			}
			else {
				tokens.Add (b);
				err = null;
			}
		}

		private static void _appendDecimal (ArrayList tokens, decimal d, out string err) {
			if (tokens.Count == 0) {
				tokens.Add (d);
				err = null;
			}
			else {
				object prior = tokens[^1];
				if (prior is Seperator.Open) {
					tokens.Add (d);
					err = null;
				}
				else if (prior is UnaryPrefixOp u) {
					decimal result = _evalUnaryPrefixOp (u, d, out err);
					if (err == null) {
						tokens.RemoveAt (tokens.Count - 1);
						_appendDecimal (tokens, result, out err);
					}
				}
				else if (prior is BinaryOp b) {
					if (tokens.Count <= 1 || tokens[^2] is not decimal x) {
						throw _unreachableLineException;
					}
					else {
						decimal result = _evalBinaryOp (b, x, d, out err);
						if (err == null) {
							tokens.RemoveRange (tokens.Count - 2, 2);
							_appendDecimal (tokens, result, out err);
						}
					}
				}
				else {
					err = $"number {d} appended after token \"{_tokenToString (prior)}\"";
				}
			}
		}

		private static void _append (ArrayList tokens, object newToken, out string err) {
			if (newToken is decimal d) {
				_appendDecimal (tokens, d, out err);
			}
			else if (newToken is BinaryOp b) {
				_appendBinaryOp (tokens, b, out err);
			}
			else if (newToken is UnaryPrefixOp || newToken is Seperator.Open) {
				_appendUnaryPrefixOpOrSeperatorOpen (tokens, newToken, out err);
			}
			else if (newToken is UnaryPostfixOp g) {
				_appendUnaryPostfixOp (tokens, g, out err);
			}
			else if (newToken is Seperator.Close) {
				_appendSeperatorClose (tokens, out err);
			}
			else {
				err = $"\"{newToken}\" is not a supported token";
			}
		}
	}
}
