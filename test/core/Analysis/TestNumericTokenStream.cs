/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;
using NumericUtils = Lucene.Net.Util.NumericUtils;

namespace Lucene.Net.Test.Analysis
{
	
    [TestFixture]
	public class TestNumericTokenStream:BaseTokenStreamTestCase
	{
		
		internal const long lvalue = 4573245871874382L;
		internal const int ivalue = 123456;
		
        [Test]
		public virtual void TestLongStream()
		{
			NumericTokenStream stream = new NumericTokenStream().SetLongValue(lvalue);
			// use getAttribute to test if attributes really exist, if not an IAE will be throwed
			var bytesAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
            ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
			var numericAtt = stream.GetAttribute<NumericTokenStream.NumericTermAttribute>();
			BytesRef bytes = bytesAtt.BytesRef;
			stream.Reset();
			AreEqual(64, numericAtt.ValueSize);
			for (int shift = 0; shift < 64; shift += NumericUtils.PRECISION_STEP_DEFAULT)
			{
				IsTrue(stream.IncrementToken(), "New token is available");
				AreEqual(shift, numericAtt.Shift, "Shift value wrong");
				bytesAtt.FillBytesRef();
				AreEqual(lvalue & ~((1L << shift) - 1L), NumericUtils.PrefixCodedToLong(bytes), "Term is incorrectly encoded");
				AreEqual(lvalue & ~((1L << shift) - 1L), numericAtt.RawValue, "Term raw value is incorrectly encoded");
				AreEqual((shift == 0)?NumericTokenStream.TOKEN_TYPE_FULL_PREC:NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type, "Type incorrect");
			}
			IsFalse(stream.IncrementToken(), "No more tokens available");
			stream.End();
			stream.Dispose();
		}
		
        [Test]
		public virtual void  TestIntStream()
		{
			NumericTokenStream stream = new NumericTokenStream().SetIntValue(ivalue);
			// use getAttribute to test if attributes really exist, if not an IAE will be throwed
			ITermToBytesRefAttribute bytesAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
            ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
			var numericAtt = stream.GetAttribute<NumericTokenStream.NumericTermAttribute>();
			BytesRef bytes = bytesAtt.BytesRef;
			stream.Reset();
			AreEqual(32, numericAtt.ValueSize);
			for (int shift = 0; shift < 32; shift += NumericUtils.PRECISION_STEP_DEFAULT)
			{
				IsTrue(stream.IncrementToken(), "New token is available");
				AreEqual(shift, numericAtt.Shift, "Shift value wrong");
				bytesAtt.FillBytesRef();
				AreEqual(ivalue & ~((1 << shift) - 1), NumericUtils.PrefixCodedToInt(bytes), "Term is incorrectly encoded");
				AreEqual(ivalue & ~((1L << shift) - 1L), numericAtt.RawValue, "Term raw value is incorrectly encoded");
				AreEqual((shift == 0)?NumericTokenStream.TOKEN_TYPE_FULL_PREC:NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type, "Type correct");
			}
			IsFalse(stream.IncrementToken(), "No more tokens available");
			stream.End();
			stream.Dispose();
		}

        [Test]
        public virtual void TestNotInitialized()
        {
            NumericTokenStream stream = new NumericTokenStream();

            Throws<SystemException>(stream.Reset, "reset() should not succeed.");
            Throws<SystemException>(() => stream.IncrementToken(), "incrementToken() should not succeed.");
        }
		public interface ITestAttribute : ICharTermAttribute
		{
			// pass
		}

		public class TestAttributeImpl : CharTermAttribute, ITestAttribute
		{
		}

		[Test]
		public virtual void TestCTA()
		{
			NumericTokenStream stream = new NumericTokenStream();
			try
			{
				stream.AddAttribute<CharTermAttribute>();
				Fail("Succeeded to add CharTermAttribute.");
			}
			catch (ArgumentException iae)
			{
				IsTrue(iae.Message.StartsWith("NumericTokenStream does not support"
					));
			}
			try
			{
				stream.AddAttribute<TestNumericTokenStream.ITestAttribute>();
				Fail("Succeeded to add TestAttribute.");
			}
			catch (ArgumentException iae)
			{
				IsTrue(iae.Message.StartsWith("NumericTokenStream does not support"
					));
			}
		}
	}
}