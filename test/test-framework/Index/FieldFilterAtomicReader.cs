/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// A
	/// <see cref="FilterAtomicReader">FilterAtomicReader</see>
	/// that exposes only a subset
	/// of fields from the underlying wrapped reader.
	/// </summary>
	public sealed class FieldFilterAtomicReader : FilterAtomicReader
	{
		private readonly ICollection<string> fields;

		private readonly bool negate;

		private readonly FieldInfos fieldInfos;

		public FieldFilterAtomicReader(AtomicReader @in, ICollection<string> fields, bool
			 negate) : base(@in)
		{
			this.fields = fields;
			this.negate = negate;
			AList<FieldInfo> filteredInfos = new AList<FieldInfo>();
			foreach (FieldInfo fi in @in.GetFieldInfos())
			{
				if (HasField(fi.name))
				{
					filteredInfos.AddItem(fi);
				}
			}
			fieldInfos = new FieldInfos(Sharpen.Collections.ToArray(filteredInfos, new FieldInfo
				[filteredInfos.Count]));
		}

		internal bool HasField(string field)
		{
			return negate ^ fields.Contains(field);
		}

		public override FieldInfos GetFieldInfos()
		{
			return fieldInfos;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.TestFramework.Index.Fields GetTermVectors(int docID)
		{
			Lucene.Net.TestFramework.Index.Fields f = base.GetTermVectors(docID);
			if (f == null)
			{
				return null;
			}
			f = new FieldFilterAtomicReader.FieldFilterFields(this, f);
			// we need to check for emptyness, so we can return
			// null:
			return f.Iterator().HasNext() ? f : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Document(int docID, StoredFieldVisitor visitor)
		{
			base.Document(docID, new _StoredFieldVisitor_74(this, visitor));
		}

		private sealed class _StoredFieldVisitor_74 : StoredFieldVisitor
		{
			public _StoredFieldVisitor_74(FieldFilterAtomicReader _enclosing, StoredFieldVisitor
				 visitor)
			{
				this._enclosing = _enclosing;
				this.visitor = visitor;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void BinaryField(FieldInfo fieldInfo, byte[] value)
			{
				visitor.BinaryField(fieldInfo, value);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StringField(FieldInfo fieldInfo, string value)
			{
				visitor.StringField(fieldInfo, value);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void IntField(FieldInfo fieldInfo, int value)
			{
				visitor.IntField(fieldInfo, value);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void LongField(FieldInfo fieldInfo, long value)
			{
				visitor.LongField(fieldInfo, value);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FloatField(FieldInfo fieldInfo, float value)
			{
				visitor.FloatField(fieldInfo, value);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void DoubleField(FieldInfo fieldInfo, double value)
			{
				visitor.DoubleField(fieldInfo, value);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override StoredFieldVisitor.Status NeedsField(FieldInfo fieldInfo)
			{
				return this._enclosing.HasField(fieldInfo.name) ? visitor.NeedsField(fieldInfo) : 
					StoredFieldVisitor.Status.NO;
			}

			private readonly FieldFilterAtomicReader _enclosing;

			private readonly StoredFieldVisitor visitor;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.TestFramework.Index.Fields Fields()
		{
			Lucene.Net.TestFramework.Index.Fields f = base.Fields();
			return (f == null) ? null : new FieldFilterAtomicReader.FieldFilterFields(this, f
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNumericDocValues(string field)
		{
			return HasField(field) ? base.GetNumericDocValues(field) : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BinaryDocValues GetBinaryDocValues(string field)
		{
			return HasField(field) ? base.GetBinaryDocValues(field) : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedDocValues GetSortedDocValues(string field)
		{
			return HasField(field) ? base.GetSortedDocValues(field) : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNormValues(string field)
		{
			return HasField(field) ? base.GetNormValues(field) : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Bits GetDocsWithField(string field)
		{
			return HasField(field) ? base.GetDocsWithField(field) : null;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("FieldFilterAtomicReader(reader=");
			sb.Append(@in).Append(", fields=");
			if (negate)
			{
				sb.Append('!');
			}
			return sb.Append(fields).Append(')').ToString();
		}

		private class FieldFilterFields : FilterAtomicReader.FilterFields
		{
			public FieldFilterFields(FieldFilterAtomicReader _enclosing, Fields @in) : base(@in
				)
			{
				this._enclosing = _enclosing;
			}

			public override int Size()
			{
				// this information is not cheap, return -1 like MultiFields does:
				return -1;
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				return new _FilterIterator_167(this, base.Iterator());
			}

			private sealed class _FilterIterator_167 : FilterIterator<string>
			{
				public _FilterIterator_167(FieldFilterFields _enclosing, Sharpen.Iterator<string>
					 baseArg1) : base(baseArg1)
				{
					this._enclosing = _enclosing;
				}

				protected override bool PredicateFunction(string field)
				{
					return this._enclosing._enclosing.HasField(field);
				}

				private readonly FieldFilterFields _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Lucene.Net.TestFramework.Index.Terms Terms(string field)
			{
				return this._enclosing.HasField(field) ? base.Terms(field) : null;
			}

			private readonly FieldFilterAtomicReader _enclosing;
		}
	}
}
