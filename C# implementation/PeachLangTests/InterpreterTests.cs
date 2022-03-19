using Microsoft.VisualStudio.TestTools.UnitTesting;
using static PeachLang.Interpreter;

namespace PeachLang.Tests {
	[TestClass ()]
	public class OperatorTests {
		[TestMethod ("Test certainty operator")]
		public void TestCertainty () {
			Assert.AreEqual (1m, ParseInput (string.Concat ("[]", 1), out string err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat ("[]", 0.5m), out err));
			Assert.IsNull (err);
		}

		[TestMethod ("Test possibility operator")]
		public void TestPossibility () {
			Assert.AreEqual (1m, ParseInput (string.Concat ("<>", 0.5m), out string err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat ("<>", 0), out err));
			Assert.IsNull (err);
		}

		[TestMethod ("Test percentage operator")]
		public void TestPercentage () {
			Assert.AreEqual (0.55m, ParseInput (string.Concat (55m, '%'), out string err));
			Assert.IsNull (err);
		}

		[TestMethod ("Test negation")]
		public void TestNegation () {
			Assert.AreEqual (0.2m, ParseInput (string.Concat ('~', 0.8m), out string err));
			Assert.IsNull (err);
			ParseInput (string.Concat ('~', 1.2m), out err);
			Assert.IsNotNull (err);
		}
		
		[TestMethod ("Test comparison operators")]
		public void TestComparison () {
			string err;
			Assert.AreEqual (1m, ParseInput (string.Concat (0, '<', 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat (1, '<', 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat (1, '<', 0), out err));
			Assert.IsNull (err);

			Assert.AreEqual (0m, ParseInput (string.Concat (0, '=', 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (1m, ParseInput (string.Concat (1, '=', 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat (1, '=', 0), out err));
			Assert.IsNull (err);

			Assert.AreEqual (0m, ParseInput (string.Concat (0, '>', 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat (1, '>', 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (1m, ParseInput (string.Concat (1, '>', 0), out err));
			Assert.IsNull (err);

			Assert.AreEqual (1m, ParseInput (string.Concat (0, "<=", 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (1m, ParseInput (string.Concat (1, "<=", 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat (1, "<=", 0), out err));
			Assert.IsNull (err);

			Assert.AreEqual (1m, ParseInput (string.Concat (0, "~=", 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (0m, ParseInput (string.Concat (1, "~=", 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (1m, ParseInput (string.Concat (1, "~=", 0), out err));
			Assert.IsNull (err);

			Assert.AreEqual (0m, ParseInput (string.Concat (0, ">=", 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (1m, ParseInput (string.Concat (1, ">=", 1), out err));
			Assert.IsNull (err);
			Assert.AreEqual (1m, ParseInput (string.Concat (1, ">=", 0), out err));
			Assert.IsNull (err);
		}
		
		[TestMethod ("Test addition and multiplication")]
		public void TestAddAndMultiply () {
			Assert.AreEqual (1.7m, ParseInput (string.Concat (0.5m, '+', 1.2m), out string err));
			Assert.IsNull (err);
			Assert.AreEqual (0.6m, ParseInput (string.Concat (0.5m, '*', 1.2m), out err));
			Assert.IsNull (err);
		}

		[TestMethod ("Test minimum, average, and maximum")]
		public void TestMinAvgMax () {
			Assert.AreEqual (0.2m, ParseInput (string.Concat (0.2m, '!', 0.8m), out string err));
			Assert.IsNull (err);
			Assert.AreEqual (0.5m, ParseInput (string.Concat (0.2m, '@', 0.8m), out err));
			Assert.IsNull (err);
			Assert.AreEqual (0.8m, ParseInput (string.Concat (0.2m, '#', 0.8m), out err));
			Assert.IsNull (err);
		}
		
		[TestMethod ("Test disjunction")]
		public void TestDisjunct () {
			Assert.AreEqual (0.75m, ParseInput (string.Concat (0.5m, '$', 0.5m), out string err));
			Assert.IsNull (err);
		}

		[TestMethod ("Test subtraction")]
		public void TestSubtraction () {
			Assert.AreEqual (0.6m, ParseInput (string.Concat (0.8m, '-', 0.2m), out string err));
			Assert.IsNull (err);
			ParseInput (string.Concat (0.8m, '-', 1), out err);
			Assert.IsNotNull (err);
		}

		[TestMethod ("Test division")]
		public void TestDivision () {
			Assert.AreEqual (0.4m, ParseInput (string.Concat (0.8m, '/', 2), out string err));
			Assert.IsNull (err);
			ParseInput (string.Concat (1, '/', 0), out err);
			Assert.IsNotNull (err);
		}

		[TestMethod ("Test raising to a power")]
		public void TestRaise () {
			Assert.AreEqual (0.25m, ParseInput (string.Concat (0.5m, '^', 2), out string err));
			Assert.IsNull (err);
			ParseInput (string.Concat (0, '^', 0), out err);
			Assert.IsNotNull (err);
		}
	}

	[TestClass ()]
	public class InterpreterTest {
		[TestMethod ("Test parse of a long, irreducible string with every token but ')' and '%'")]
		public void TestParseLongIrreducibleString () {
			string raw = "~([](<>(1!(2@(3#(4$(5+(6-(7*(8/(9=(0~=(0.1<(1.2<=(2.3>(3.4>=(";
			ParseInput (raw, out string err);
			Assert.IsTrue (err.Contains (raw),
				$"err = \"{err}\"\ndoes not contain \"{raw}\"");
		}

		[TestMethod ("Test quick math")]
		public void TestQuickMath () {
			string raw = "[]((2+2=4)!(2+2-1=3))";
			object result = ParseInput (raw, out string err);
			Assert.IsNull (err);
			Assert.AreEqual (1m, result);
		}

		[TestMethod ("Test parse of a long, reducible string with every token")]
		public void TestParseLongReducibleString () {
			string raw = "([](1@1)=<>(100%@(0%))~=<>(0!1))!(2#3#4#5=5)!~<>(1+2-3*4/5^6)!(2<3)!(3<=4)!(4>3)!(3>=2)";
			object result = ParseInput (raw, out string err);
			Assert.IsNull (err);
			Assert.AreEqual (1m, result);
		}

		[TestMethod ("Test variables")]
		public void TestVariables () {
			ParseInput ("x:2", out string err);
			Assert.IsNull (err);
			ParseInput ("y:7", out err);
			Assert.IsNull (err);
			ParseInput ("z:x^y", out err);
			Assert.IsNull (err);
			object result = ParseInput ("z", out err);
			Assert.IsNull (err);
			Assert.AreEqual (128m, result);
		}
	}
}
