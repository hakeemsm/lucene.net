/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Document
{
	public class TestField : LuceneTestCase
	{
		// sanity check some basics of fields
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDoubleField()
		{
			Field[] fields = new Field[] { new DoubleField("foo", 5d, Field.Store.NO), new DoubleField
				("foo", 5d, Field.Store.YES) };
			foreach (Field field in fields)
			{
				TrySetBoost(field);
				TrySetByteValue(field);
				TrySetBytesValue(field);
				TrySetBytesRefValue(field);
				field.SetDoubleValue(6d);
				// ok
				TrySetIntValue(field);
				TrySetFloatValue(field);
				TrySetLongValue(field);
				TrySetReaderValue(field);
				TrySetShortValue(field);
				TrySetStringValue(field);
				TrySetTokenStreamValue(field);
				NUnit.Framework.Assert.AreEqual(6d, field.NumericValue(), 0.0d);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDoubleDocValuesField()
		{
			DoubleDocValuesField field = new DoubleDocValuesField("foo", 5d);
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			field.SetDoubleValue(6d);
			// ok
			TrySetIntValue(field);
			TrySetFloatValue(field);
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(6d, double.LongBitsToDouble(field.NumericValue())
				, 0.0d);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFloatDocValuesField()
		{
			FloatDocValuesField field = new FloatDocValuesField("foo", 5f);
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			field.SetFloatValue(6f);
			// ok
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(6f, Sharpen.Runtime.IntBitsToFloat(field.NumericValue
				()), 0.0f);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFloatField()
		{
			Field[] fields = new Field[] { new FloatField("foo", 5f, Field.Store.NO), new FloatField
				("foo", 5f, Field.Store.YES) };
			foreach (Field field in fields)
			{
				TrySetBoost(field);
				TrySetByteValue(field);
				TrySetBytesValue(field);
				TrySetBytesRefValue(field);
				TrySetDoubleValue(field);
				TrySetIntValue(field);
				field.SetFloatValue(6f);
				// ok
				TrySetLongValue(field);
				TrySetReaderValue(field);
				TrySetShortValue(field);
				TrySetStringValue(field);
				TrySetTokenStreamValue(field);
				NUnit.Framework.Assert.AreEqual(6f, field.NumericValue(), 0.0f);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntField()
		{
			Field[] fields = new Field[] { new IntField("foo", 5, Field.Store.NO), new IntField
				("foo", 5, Field.Store.YES) };
			foreach (Field field in fields)
			{
				TrySetBoost(field);
				TrySetByteValue(field);
				TrySetBytesValue(field);
				TrySetBytesRefValue(field);
				TrySetDoubleValue(field);
				field.SetIntValue(6);
				// ok
				TrySetFloatValue(field);
				TrySetLongValue(field);
				TrySetReaderValue(field);
				TrySetShortValue(field);
				TrySetStringValue(field);
				TrySetTokenStreamValue(field);
				NUnit.Framework.Assert.AreEqual(6, field.NumericValue());
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNumericDocValuesField()
		{
			NumericDocValuesField field = new NumericDocValuesField("foo", 5L);
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			TrySetFloatValue(field);
			field.SetLongValue(6);
			// ok
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(6L, field.NumericValue());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLongField()
		{
			Field[] fields = new Field[] { new LongField("foo", 5L, Field.Store.NO), new LongField
				("foo", 5L, Field.Store.YES) };
			foreach (Field field in fields)
			{
				TrySetBoost(field);
				TrySetByteValue(field);
				TrySetBytesValue(field);
				TrySetBytesRefValue(field);
				TrySetDoubleValue(field);
				TrySetIntValue(field);
				TrySetFloatValue(field);
				field.SetLongValue(6);
				// ok
				TrySetReaderValue(field);
				TrySetShortValue(field);
				TrySetStringValue(field);
				TrySetTokenStreamValue(field);
				NUnit.Framework.Assert.AreEqual(6L, field.NumericValue());
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedBytesDocValuesField()
		{
			SortedDocValuesField field = new SortedDocValuesField("foo", new BytesRef("bar"));
			TrySetBoost(field);
			TrySetByteValue(field);
			field.SetBytesValue(Sharpen.Runtime.GetBytesForString("fubar", StandardCharsets.UTF_8
				));
			field.SetBytesValue(new BytesRef("baz"));
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			TrySetFloatValue(field);
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(new BytesRef("baz"), field.BinaryValue());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBinaryDocValuesField()
		{
			BinaryDocValuesField field = new BinaryDocValuesField("foo", new BytesRef("bar"));
			TrySetBoost(field);
			TrySetByteValue(field);
			field.SetBytesValue(Sharpen.Runtime.GetBytesForString("fubar", StandardCharsets.UTF_8
				));
			field.SetBytesValue(new BytesRef("baz"));
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			TrySetFloatValue(field);
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(new BytesRef("baz"), field.BinaryValue());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStringField()
		{
			Field[] fields = new Field[] { new StringField("foo", "bar", Field.Store.NO), new 
				StringField("foo", "bar", Field.Store.YES) };
			foreach (Field field in fields)
			{
				TrySetBoost(field);
				TrySetByteValue(field);
				TrySetBytesValue(field);
				TrySetBytesRefValue(field);
				TrySetDoubleValue(field);
				TrySetIntValue(field);
				TrySetFloatValue(field);
				TrySetLongValue(field);
				TrySetReaderValue(field);
				TrySetShortValue(field);
				field.SetStringValue("baz");
				TrySetTokenStreamValue(field);
				NUnit.Framework.Assert.AreEqual("baz", field.StringValue());
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTextFieldString()
		{
			Field[] fields = new Field[] { new TextField("foo", "bar", Field.Store.NO), new TextField
				("foo", "bar", Field.Store.YES) };
			foreach (Field field in fields)
			{
				field.SetBoost(5f);
				TrySetByteValue(field);
				TrySetBytesValue(field);
				TrySetBytesRefValue(field);
				TrySetDoubleValue(field);
				TrySetIntValue(field);
				TrySetFloatValue(field);
				TrySetLongValue(field);
				TrySetReaderValue(field);
				TrySetShortValue(field);
				field.SetStringValue("baz");
				field.SetTokenStream(new CannedTokenStream(new Token("foo", 0, 3)));
				NUnit.Framework.Assert.AreEqual("baz", field.StringValue());
				NUnit.Framework.Assert.AreEqual(5f, field.Boost(), 0f);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTextFieldReader()
		{
			Field field = new TextField("foo", new StringReader("bar"));
			field.SetBoost(5f);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			TrySetFloatValue(field);
			TrySetLongValue(field);
			field.SetReaderValue(new StringReader("foobar"));
			TrySetShortValue(field);
			TrySetStringValue(field);
			field.SetTokenStream(new CannedTokenStream(new Token("foo", 0, 3)));
			NUnit.Framework.Assert.IsNotNull(field.ReaderValue());
			NUnit.Framework.Assert.AreEqual(5f, field.Boost(), 0f);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStoredFieldBytes()
		{
			Field[] fields = new Field[] { new StoredField("foo", Sharpen.Runtime.GetBytesForString
				("bar", StandardCharsets.UTF_8)), new StoredField("foo", Sharpen.Runtime.GetBytesForString
				("bar", StandardCharsets.UTF_8), 0, 3), new StoredField("foo", new BytesRef("bar"
				)) };
			foreach (Field field in fields)
			{
				TrySetBoost(field);
				TrySetByteValue(field);
				field.SetBytesValue(Sharpen.Runtime.GetBytesForString("baz", StandardCharsets.UTF_8
					));
				field.SetBytesValue(new BytesRef("baz"));
				TrySetDoubleValue(field);
				TrySetIntValue(field);
				TrySetFloatValue(field);
				TrySetLongValue(field);
				TrySetReaderValue(field);
				TrySetShortValue(field);
				TrySetStringValue(field);
				TrySetTokenStreamValue(field);
				NUnit.Framework.Assert.AreEqual(new BytesRef("baz"), field.BinaryValue());
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStoredFieldString()
		{
			Field field = new StoredField("foo", "bar");
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			TrySetFloatValue(field);
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			field.SetStringValue("baz");
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual("baz", field.StringValue());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStoredFieldInt()
		{
			Field field = new StoredField("foo", 1);
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			TrySetDoubleValue(field);
			field.SetIntValue(5);
			TrySetFloatValue(field);
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(5, field.NumericValue());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStoredFieldDouble()
		{
			Field field = new StoredField("foo", 1D);
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			field.SetDoubleValue(5D);
			TrySetIntValue(field);
			TrySetFloatValue(field);
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(5D, field.NumericValue(), 0.0D);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStoredFieldFloat()
		{
			Field field = new StoredField("foo", 1F);
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			field.SetFloatValue(5f);
			TrySetLongValue(field);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(5f, field.NumericValue(), 0.0f);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStoredFieldLong()
		{
			Field field = new StoredField("foo", 1L);
			TrySetBoost(field);
			TrySetByteValue(field);
			TrySetBytesValue(field);
			TrySetBytesRefValue(field);
			TrySetDoubleValue(field);
			TrySetIntValue(field);
			TrySetFloatValue(field);
			field.SetLongValue(5);
			TrySetReaderValue(field);
			TrySetShortValue(field);
			TrySetStringValue(field);
			TrySetTokenStreamValue(field);
			NUnit.Framework.Assert.AreEqual(5L, field.NumericValue());
		}

		private void TrySetByteValue(Field f)
		{
			try
			{
				f.SetByteValue(unchecked((byte)10));
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetBytesValue(Field f)
		{
			try
			{
				f.SetBytesValue(new byte[] { 5, 5 });
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetBytesRefValue(Field f)
		{
			try
			{
				f.SetBytesValue(new BytesRef("bogus"));
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetDoubleValue(Field f)
		{
			try
			{
				f.SetDoubleValue(double.MaxValue);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetIntValue(Field f)
		{
			try
			{
				f.SetIntValue(int.MaxValue);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetLongValue(Field f)
		{
			try
			{
				f.SetLongValue(long.MaxValue);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetFloatValue(Field f)
		{
			try
			{
				f.SetFloatValue(float.MaxValue);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetReaderValue(Field f)
		{
			try
			{
				f.SetReaderValue(new StringReader("BOO!"));
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetShortValue(Field f)
		{
			try
			{
				f.SetShortValue(short.MaxValue);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetStringValue(Field f)
		{
			try
			{
				f.SetStringValue("BOO!");
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetTokenStreamValue(Field f)
		{
			try
			{
				f.SetTokenStream(new CannedTokenStream(new Token("foo", 0, 3)));
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// expected
		private void TrySetBoost(Field f)
		{
			try
			{
				f.SetBoost(5.0f);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentException)
			{
			}
		}
		// expected
	}
}
