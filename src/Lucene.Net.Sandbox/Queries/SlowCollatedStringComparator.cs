/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>
	/// Sorts by a field's value using the given Collator
	/// <p><b>WARNING</b>: this is very slow; you'll
	/// get much better performance using the
	/// CollationKeyAnalyzer or ICUCollationKeyAnalyzer.
	/// </summary>
	/// <remarks>
	/// Sorts by a field's value using the given Collator
	/// <p><b>WARNING</b>: this is very slow; you'll
	/// get much better performance using the
	/// CollationKeyAnalyzer or ICUCollationKeyAnalyzer.
	/// </remarks>
	[System.ObsoleteAttribute(@"Index collation keys with CollationKeyAnalyzer or ICUCollationKeyAnalyzer instead. This class will be removed in Lucene 5.0"
		)]
	public sealed class SlowCollatedStringComparator : FieldComparator<string>
	{
		private readonly string[] values;

		private BinaryDocValues currentDocTerms;

		private Bits docsWithField;

		private readonly string field;

		internal readonly Collator collator;

		private string bottom;

		private string topValue;

		private readonly BytesRef tempBR = new BytesRef();

		public SlowCollatedStringComparator(int numHits, string field, Collator collator)
		{
			values = new string[numHits];
			this.field = field;
			this.collator = collator;
		}

		public override int Compare(int slot1, int slot2)
		{
			string val1 = values[slot1];
			string val2 = values[slot2];
			if (val1 == null)
			{
				if (val2 == null)
				{
					return 0;
				}
				return -1;
			}
			else
			{
				if (val2 == null)
				{
					return 1;
				}
			}
			return collator.Compare(val1, val2);
		}

		public override int CompareBottom(int doc)
		{
			currentDocTerms.Get(doc, tempBR);
			string val2 = tempBR.length == 0 && docsWithField.Get(doc) == false ? null : tempBR
				.Utf8ToString();
			if (bottom == null)
			{
				if (val2 == null)
				{
					return 0;
				}
				return -1;
			}
			else
			{
				if (val2 == null)
				{
					return 1;
				}
			}
			return collator.Compare(bottom, val2);
		}

		public override void Copy(int slot, int doc)
		{
			currentDocTerms.Get(doc, tempBR);
			if (tempBR.length == 0 && docsWithField.Get(doc) == false)
			{
				values[slot] = null;
			}
			else
			{
				values[slot] = tempBR.Utf8ToString();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<string> SetNextReader(AtomicReaderContext context
			)
		{
			currentDocTerms = FieldCache.DEFAULT.GetTerms(((AtomicReader)context.Reader()), field
				, true);
			docsWithField = FieldCache.DEFAULT.GetDocsWithField(((AtomicReader)context.Reader
				()), field);
			return this;
		}

		public override void SetBottom(int bottom)
		{
			this.bottom = values[bottom];
		}

		public override void SetTopValue(string value)
		{
			this.topValue = value;
		}

		public override string Value(int slot)
		{
			return values[slot];
		}

		public override int CompareValues(string first, string second)
		{
			if (first == null)
			{
				if (second == null)
				{
					return 0;
				}
				return -1;
			}
			else
			{
				if (second == null)
				{
					return 1;
				}
				else
				{
					return collator.Compare(first, second);
				}
			}
		}

		public override int CompareTop(int doc)
		{
			currentDocTerms.Get(doc, tempBR);
			string docValue;
			if (tempBR.length == 0 && docsWithField.Get(doc) == false)
			{
				docValue = null;
			}
			else
			{
				docValue = tempBR.Utf8ToString();
			}
			return CompareValues(topValue, docValue);
		}
	}
}
