using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Analysis.TokenAttributes
{
    [TestFixture]
    public class TestCharTermAttributeImpl : LuceneTestCase
	{
        [Test]
		public virtual void TestResize()
		{
			ICharTermAttribute t = new CharTermAttribute();
			char[] content = "hello".ToCharArray();
			t.CopyBuffer(content, 0, content.Length);
			for (int i = 0; i < 2000; i++)
			{
				t.ResizeBuffer(i);
				IsTrue(i <= t.Buffer.Length);
				AreEqual("hello", t.ToString());
			}
		}

        [Test]
		public virtual void TestGrow()
		{
			ICharTermAttribute t = new CharTermAttribute();
			StringBuilder buf = new StringBuilder("ab");
			for (int i = 0; i < 20; i++)
			{
				char[] content = buf.ToString().ToCharArray();
				t.CopyBuffer(content, 0, content.Length);
				AreEqual(buf.Length, t.Length);
				AreEqual(buf.ToString(), t.ToString());
				buf.Append(buf.ToString());
			}
			AreEqual(1048576, t.Length);
			// now as a StringBuilder, first variant
			t = new CharTermAttribute();
			buf = new StringBuilder("ab");
			for (int i_1 = 0; i_1 < 20; i_1++)
			{
				t.SetEmpty().Append(buf);
				AreEqual(buf.Length, t.Length);
				AreEqual(buf.ToString(), t.ToString());
				buf.Append(t);
			}
			AreEqual(1048576, t.Length);
			// Test for slow growth to a long term
			t = new CharTermAttribute();
			buf = new StringBuilder("a");
			for (int i_2 = 0; i_2 < 20000; i_2++)
			{
				t.SetEmpty().Append(buf);
				AreEqual(buf.Length, t.Length);
				AreEqual(buf.ToString(), t.ToString());
				buf.Append("a");
			}
			AreEqual(20000, t.Length);
		}

		[Test]
		public virtual void TestToString()
		{
			char[] b = new char[] { 'a', 'l', 'o', 'h', 'a' };
			ICharTermAttribute t = new CharTermAttribute();
			t.CopyBuffer(b, 0, 5);
			AreEqual("aloha", t.ToString());
			t.SetEmpty().Append("hi there");
			AreEqual("hi there", t.ToString());
		}

		[Test]
		public virtual void TestClone()
		{
			var t = new CharTermAttribute();
			char[] content = "hello".ToCharArray();
			t.CopyBuffer(content, 0, 5);
			char[] buf = t.Buffer;
			ICharTermAttribute copy = TestToken.AssertCloneIsEqual(t);
			AreEqual(t.ToString(), copy.ToString());
			AreNotSame(buf, copy.Buffer);
		}

		[Test]
		public virtual void TestEquals()
		{
			ICharTermAttribute t1a = new CharTermAttribute();
			char[] content1a = "hello".ToCharArray();
			t1a.CopyBuffer(content1a, 0, 5);
			ICharTermAttribute t1b = new CharTermAttribute();
			char[] content1b = "hello".ToCharArray();
			t1b.CopyBuffer(content1b, 0, 5);
			ICharTermAttribute t2 = new CharTermAttribute();
			char[] content2 = "hello2".ToCharArray();
			t2.CopyBuffer(content2, 0, 6);
			IsTrue(t1a.Equals(t1b));
			IsFalse(t1a.Equals(t2));
			IsFalse(t2.Equals(t1b));
		}

		[Test]
		public virtual void TestCopyTo()
		{
			var t = new CharTermAttribute();
			ICharTermAttribute copy = TestToken.AssertCopyIsEqual(t);
			AreEqual(string.Empty, t.ToString());
			AreEqual(string.Empty, copy.ToString());
			t = new CharTermAttribute();
			char[] content = "hello".ToCharArray();
			t.CopyBuffer(content, 0, 5);
			char[] buf = t.Buffer;
			copy = TestToken.AssertCopyIsEqual(t);
			AreEqual(t.ToString(), copy.ToString());
			AreNotSame(buf, copy.Buffer);
		}

		[Test]
		public virtual void TestAttributeReflection()
		{
			var t = new CharTermAttribute();
			t.Append("foobar");
			TestUtil.AssertAttributeReflection(t, new AnonObjDictionary());
		}

		private sealed class AnonObjDictionary : Dictionary<string, object>
		{
			public AnonObjDictionary()
			{
				{
					this[typeof(CharTermAttribute).FullName + "#term"] = "foobar";
					this[typeof(ITermToBytesRefAttribute).FullName + "#bytes"] =  new BytesRef("foobar");
				}
			}
		}

        [Test]
		public virtual void TestCharSequenceInterface()
		{
			string s = "0123456789";
			ICharTermAttribute t = new CharTermAttribute();
			t.Append(s);
			AreEqual(s.Length, t.Length);
			AreEqual("12", t.SubSequence(1, 3).ToString());
			AreEqual(s, t.SubSequence(0, s.Length).ToString());
			IsTrue(Regex.IsMatch(t.ToString(),"01\\d+"));
			IsTrue(Regex.IsMatch(t.SubSequence(3, 5).ToString(),"34"));
			AreEqual(s.Substring(3, 7), t.SubSequence(3, 7).ToString());
			for (int i = 0; i < s.Length; i++)
			{
				IsTrue(t.CharAt(i) == s[i]);
			}
		}

        //[Test]
        //public virtual void TestAppendableInterface()
        //{
        //    ICharTermAttribute t = new CharTermAttribute();
        //    var sb = new StringBuilder();
        //    Formatter f = new CharSequenceFormatter();
        //    Formatter formatter = new Formatter(t, CultureInfo.ROOT);
        //    formatter.Format("%d", 1234);
        //    AreEqual("1234", t.ToString());
        //    formatter.Format("%d", 5678);
        //    AreEqual("12345678", t.ToString());
        //    t.Append('9');
        //    AreEqual("123456789", t.ToString());
        //    t.Append((ICharSequence)"0");
        //    AreEqual("1234567890", t.ToString());
        //    t.AppendRange((CharSequence)"0123456789", 1, 3);
        //    AreEqual("123456789012", t.ToString());
        //    t.AppendRange((CharSequence)CharBuffer.Wrap("0123456789".ToCharArray()), 3, 5);
        //    AreEqual("12345678901234", t.ToString());
        //    t.Append((CharSequence)t);
        //    AreEqual("1234567890123412345678901234", t.ToString());
        //    t.AppendRange((CharSequence)new StringBuilder("0123456789"), 5, 7);
        //    AreEqual("123456789012341234567890123456", t.ToString());
        //    t.Append((CharSequence)new StringBuilder(t));
        //    AreEqual("123456789012341234567890123456123456789012341234567890123456"
        //        , t.ToString());
        //    // very wierd, to test if a subSlice is wrapped correct :)
        //    CharBuffer buf = CharBuffer.Wrap("0123456789".ToCharArray(), 3, 5);
        //    AreEqual("34567", buf.ToString());
        //    t.SetEmpty().AppendRange((CharSequence)buf, 1, 2);
        //    AreEqual("4", t.ToString());
        //    CharTermAttribute t2 = new CharTermAttribute();
        //    t2.Append("test");
        //    t.Append((CharSequence)t2);
        //    AreEqual("4test", t.ToString());
        //    t.AppendRange((CharSequence)t2, 1, 2);
        //    AreEqual("4teste", t.ToString());
        //    try
        //    {
        //        t.AppendRange((CharSequence)t2, 1, 5);
        //        Fail("Should throw IndexOutOfBoundsException");
        //    }
        //    catch (IndexOutOfRangeException)
        //    {
        //    }
        //    try
        //    {
        //        t.AppendRange((CharSequence)t2, 1, 0);
        //        Fail("Should throw IndexOutOfBoundsException");
        //    }
        //    catch (IndexOutOfRangeException)
        //    {
        //    }
        //    t.Append((CharSequence)null);
        //    AreEqual("4testenull", t.ToString());
        //}

        [Test]
		public virtual void TestAppendableInterfaceWithLongSequences()
		{
			ICharTermAttribute t = new CharTermAttribute();
			t.Append("01234567890123456789012345678901234567890123456789");
			t.Append(("01234567890123456789012345678901234567890123456789"), 3, 50);
			AreEqual("0123456789012345678901234567890123456789012345678934567890123456789012345678901234567890123456789"
				, t.ToString());
			t.SetEmpty().Append(new StringBuilder("01234567890123456789"), 5, 17);
			AreEqual("567890123456", t.ToString());
			t.Append(new StringBuilder(t.ToString()));
			AreEqual("567890123456567890123456", t.ToString());
			// very wierd, to test if a subSlice is wrapped correct :)
            Char[] buf =
                (char[])
                    ByteBuffer.Wrap(Array.ConvertAll(("012345678901234567890123456789").ToCharArray(), Convert.ToByte),
                        3, 15).Array;
            
            
			AreEqual("345678901234567", buf.ToString());
			t.SetEmpty().Append(new StringCharSequenceWrapper(buf.ToString()), 1, 14);
			AreEqual("4567890123456", t.ToString());
			// finally use a completely custom CharSequence that is not catched by instanceof checks
			string longTestString = "012345678901234567890123456789";
			t.Append(new AnonCharSequence(longTestString));
			AreEqual("4567890123456" + longTestString, t.ToString());
		}

		private sealed class AnonCharSequence : ICharSequence
		{
			public AnonCharSequence(string longTestString)
			{
				this.longTestString = longTestString;
			}

			public char CharAt(int i)
			{
				return longTestString[i];
			}

			public int Length
			{
				get
				{
					return longTestString.Length;
				}
			}

			public ICharSequence SubSequence(int start, int end)
			{
				return new StringCharSequenceWrapper(longTestString.Substring(start, end));
			}

			public override string ToString()
			{
				return longTestString;
			}

			private readonly string longTestString;
		}

        [Test]
		public virtual void TestNonCharSequenceAppend()
		{
			ICharTermAttribute t = new CharTermAttribute();
			t.Append("0123456789");
			t.Append("0123456789");
			AreEqual("01234567890123456789", t.ToString());
			t.Append(new StringBuilder("0123456789"));
			AreEqual("012345678901234567890123456789", t.ToString());
			CharTermAttribute t2 = new CharTermAttribute();
			t2.Append("test");
			t.Append(t2);
			AreEqual("012345678901234567890123456789test", t.ToString(
				));
			t.Append((string)null);
			t.Append((StringBuilder)null);
			t.Append((CharTermAttribute)null);
			AreEqual("012345678901234567890123456789testnullnullnull", 
				t.ToString());
		}

        [Test]
		public virtual void TestExceptions()
		{
			ICharTermAttribute t = new CharTermAttribute();
			t.Append("test");
			AreEqual("test", t.ToString());
			try
			{
			    t.CharAt(-1);
				Fail("Should throw IndexOutOfBoundsException");
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				t.CharAt(4);
				Fail("Should throw IndexOutOfBoundsException");
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				t.SubSequence(0, 5);
				Fail("Should throw IndexOutOfBoundsException");
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				t.SubSequence(5, 0);
				Fail("Should throw IndexOutOfBoundsException");
			}
			catch (IndexOutOfRangeException)
			{
			}
		}
	}

   
}
