using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.TestFramework
{
    public abstract partial class LuceneTestCase
    {
        public static void AssertTrue(bool condition)
        {
            Assert.IsTrue(condition);
        }

        public static void AssertTrue(string message, bool condition)
        {
            Assert.IsTrue(condition, message);
        }

        public static void AssertFalse(bool condition)
        {
            Assert.IsFalse(condition);
        }

        public static void AssertFalse(string message, bool condition)
        {
            Assert.IsFalse(condition, message);
        }

        public static void AssertEquals(object expected, object actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void AssertEquals(string message, object expected, object actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void AssertEquals(long expected, long actual)
        {
            Assert.AreEqual(expected, actual);
        }

        public static void AssertEquals(string message, long expected, long actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void AssertNotSame(object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual);
        }

        public static void AssertNotSame(string message, object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual, message);
        }

        protected static void AssertEquals(double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        protected static void AssertEquals(string msg, double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        protected static void AssertNotNull(object o)
        {
            Assert.NotNull(o);
        }

        protected static void AssertNotNull(string msg, object o)
        {
            Assert.NotNull(o, msg);
        }

        protected static void AssertNull(object o)
        {
            Assert.Null(o);
        }

        protected static void AssertNull(string msg, object o)
        {
            Assert.Null(o, msg);
        }

        protected static void AssertArrayEquals(IEnumerable a1, IEnumerable a2)
        {
            CollectionAssert.AreEqual(a1, a2);
        }

        protected static void fail()
        {
            Fail();
        }

        protected static void fail(string message)
        {
            Fail(message);
        }

        public static Random Random()
        {
            return new Random();
        }
    }
}
