using System;
using System.Collections.Generic;

namespace PeachLang {
	public static partial class Interpreter {
		// Unless this code has a bug, this error should never be thrown. It is only included to
		// make the compiler happy.
		private static readonly Exception _unreachableLineException =
			new ("Peach internal error: This line should never be reached");

		// Returns the result of applying unary prefix operator u to number x. If the operation is
		// not valid, sets err.
		private static decimal _evalUnaryPrefixOp (UnaryPrefixOp u, decimal x, out string err) {
			switch (u) {
				case UnaryPrefixOp.IsCertain: err = null; return x >= 1 ? 1 : 0;
				case UnaryPrefixOp.IsPossible: err = null; return x > 0 ? 1 : 0;
				case UnaryPrefixOp.Negate:
					err = x <= 1 ? null : $"{_tokensAndStringsDictionary.GetValueOrDefault (u)}{x} resulted in a negative number";
					return 1 - x;
				default:
					throw _unreachableLineException;
			}
		}

		// Returns the result of applying unary postfix operator u to number x. If the operation is
		// not valid, sets err.
		private static decimal _evalUnaryPostfixOp (UnaryPostfixOp u, decimal x, out string err) {
			switch (u) {
				case UnaryPostfixOp.GetOnePercent: err = null; return x / 100;
				default: throw _unreachableLineException;
			}
		}

		// Returns the result of applying binary operator b to numbers x and y. If the operation is
		// not valid, sets err.
		private static decimal _evalBinaryOp(BinaryOp b, decimal x, decimal y, out string err) {
			switch (b) {
				case BinaryOp.IsEqual:
					err = null; return x == y ? 1 : 0;
				case BinaryOp.IsLess:
					err = null; return x < y ? 1 : 0;
				case BinaryOp.IsMore:
					err = null; return x > y ? 1 : 0;
				case BinaryOp.IsNotEqual:
					err = null; return x != y ? 1 : 0;
				case BinaryOp.IsLessOrEqual:
					err = null; return x <= y ? 1 : 0;
				case BinaryOp.IsMoreOrEqual:
					err = null; return x >= y ? 1 : 0;
				case BinaryOp.Add:
					err = null; return x + y;
				case BinaryOp.Multiply:
					err = null; return x * y;
				case BinaryOp.Min:
					err = null; return x <= y ? x : y;
				case BinaryOp.Avg:
					err = null; return (x + y) / 2;
				case BinaryOp.Max:
					err = null; return x >= y ? x : y;
				case BinaryOp.Disjunct:
					err = null; return x + y - (x * y);
				case BinaryOp.Subtract:
					err = x >= y ? null : $"{x} - {y} produces negative number {x - y}";
					return x - y;
				case BinaryOp.Divide:
					err = y > 0 ? null : $"Attempted to divide by non-positive number {y}";
					return y > 0 ? x / y : 0;
				case BinaryOp.Raise:
					err = x > 0 || y > 0 ? null :
						$"Attempted to raise {(x >= 0 ? x : "A negative number")} " +
						$"to {(y >= 0 ? $"the {y}th" : "a negative")} power";
					return (decimal)Math.Pow ((double)x, (double)y);
				default:
					throw _unreachableLineException;
			}
		}
	}
}
