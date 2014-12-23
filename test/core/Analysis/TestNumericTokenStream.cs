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
using Lucene.Net.Test.Analysis.TokenAttributes;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using NumericUtils = Lucene.Net.Util.NumericUtils;

namespace Lucene.Net.Analysis
{
	
    [TestFixture]
	public class TestNumericTokenStream:BaseTokenStreamTestCase
	{
		
		internal const long lvalue = 4573245871874382L;
		internal const int ivalue = 123456;
		
        [Test]
		public virtual void  TestLongStream()
		{
			NumericTokenStream stream = new NumericTokenStream().SetLongValue(lvalue);
			// use getAttribute to test if attributes really exist, if not an IAE will be throwed
			TermToBytesRefAttribute bytesAtt = stream.GetAttribute<TermToBytesRefAttribute>();
            ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
			NumericTokenStream.NumericTermAttribute numericAtt = stream.GetAttribute<NumericTokenStream.NumericTermAttribute
				>();
			BytesRef bytes = bytesAtt.GetBytesRef();
			stream.Reset();
			NUnit.Framework.Assert.AreEqual(64, numericAtt.GetValueSize());
			for (int shift = 0; shift < 64; shift += NumericUtils.PRECISION_STEP_DEFAULT)
			{
				Assert.IsTrue(stream.IncrementToken(), "New token is available");
				NUnit.Framework.Assert.AreEqual("Shift value wrong", shift, numericAtt.GetShift()
					);
				bytesAtt.FillBytesRef();
				Assert.AreEqual(NumericUtils.LongToPrefixCoded(lvalue, shift), termAtt.Term, "Term is correctly encoded");
				NUnit.Framework.Assert.AreEqual("Term raw value is incorrectly encoded", lvalue &
					 ~((1L << shift) - 1L), numericAtt.GetRawValue());
				Assert.AreEqual((shift == 0)?NumericTokenStream.TOKEN_TYPE_FULL_PREC:NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type, "Type incorrect");
			}
			Assert.IsFalse(stream.IncrementToken(), "No more tokens available");
		}
		
        [Test]
		public virtual void  TestIntStream()
		{
			NumericTokenStream stream = new NumericTokenStream().SetIntValue(ivalue);
			// use getAttribute to test if attributes really exist, if not an IAE will be throwed
			TermToBytesRefAttribute bytesAtt = stream.GetAttribute<TermToBytesRefAttribute>();
            ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
			NumericTokenStream.NumericTermAttribute numericAtt = stream.GetAttribute<NumericTokenStream.NumericTermAttribute
				>();
			BytesRef bytes = bytesAtt.GetBytesRef();
			stream.Reset();
			NUnit.Framework.Assert.AreEqual(32, numericAtt.GetValueSize());
			for (int shift = 0; shift < 32; shift += NumericUtils.PRECISION_STEP_DEFAULT)
			{
				Assert.IsTrue(stream.IncrementToken(), "New token is available");
				NUnit.Framework.Assert.AreEqual("Shift value wrong", shift, numericAtt.GetShift()
					);
				bytesAtt.FillBytesRef();
				Assert.AreEqual(NumericUtils.IntToPrefixCoded(ivalue, shift), termAtt.Term, "Term is incorrectly encoded");
				NUnit.Framework.Assert.AreEqual("Term raw value is incorrectly encoded", ((long)ivalue
					) & ~((1L << shift) - 1L), numericAtt.GetRawValue());
				Assert.AreEqual((shift == 0)?NumericTokenStream.TOKEN_TYPE_FULL_PREC:NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type, "Type correct");
			}
			Assert.IsFalse(stream.IncrementToken(), "No more tokens available");
		}

        [Test]
        public virtual void TestNotInitialized()
        {
            NumericTokenStream stream = new NumericTokenStream();

            Assert.Throws<SystemException>(stream.Reset, "reset() should not succeed.");
            Assert.Throws<SystemException>(() => stream.IncrementToken(), "incrementToken() should not succeed.");
        }
		public interface TestAttribute : CharTermAttribute
		{
			// pass
		}

		public class TestAttributeImpl : CharTermAttributeImpl, TestNumericTokenStream.TestAttribute
		{
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCTA()
		{
			NumericTokenStream stream = new NumericTokenStream();
			try
			{
				stream.AddAttribute<CharTermAttribute>();
				NUnit.Framework.Assert.Fail("Succeeded to add CharTermAttribute.");
			}
			catch (ArgumentException iae)
			{
				NUnit.Framework.Assert.IsTrue(iae.Message.StartsWith("NumericTokenStream does not support"
					));
			}
			try
			{
				stream.AddAttribute<TestNumericTokenStream.TestAttribute>();
				NUnit.Framework.Assert.Fail("Succeeded to add TestAttribute.");
			}
			catch (ArgumentException iae)
			{
				NUnit.Framework.Assert.IsTrue(iae.Message.StartsWith("NumericTokenStream does not support"
					));
			}
		}
	}
}