using Lucene.Net.Documents;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
	/// <summary>An in-place update to a DocValues field.</summary>
	public abstract class DocValuesUpdate
	{
		private static readonly long RAW_SIZE_IN_BYTES = 8 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER
			 + 8 * RamUsageEstimator.NUM_BYTES_OBJECT_REF + 8 * RamUsageEstimator.NUM_BYTES_INT;

		internal readonly DocValuesFieldUpdates.Type type;

		internal readonly Term term;

		internal readonly string field;

		internal readonly object value;

		internal int docIDUpto = -1;

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		/// <param name="term">
		/// the
		/// <see cref="Term">Term</see>
		/// which determines the documents that will be updated
		/// </param>
		/// <param name="field">
		/// the
		/// <see cref="NumericDocValuesField">Lucene.Net.Document.NumericDocValuesField
		/// 	</see>
		/// to update
		/// </param>
		/// <param name="value">the updated value</param>
		protected internal DocValuesUpdate(DocValuesFieldUpdates.Type type, Term term, string
			 field, object value)
		{
			// unassigned until applied, and confusing that it's here, when it's just used in BufferedDeletes...
			this.type = type;
			this.term = term;
			this.field = field;
			this.value = value;
		}

		internal abstract long ValueSizeInBytes();

		internal long SizeInBytes()
		{
			long sizeInBytes = RAW_SIZE_IN_BYTES;
			sizeInBytes += term.Field.Length * RamUsageEstimator.NUM_BYTES_CHAR;
			sizeInBytes += term.bytes.bytes.Length;
			sizeInBytes += field.Length * RamUsageEstimator.NUM_BYTES_CHAR;
			sizeInBytes += ValueSizeInBytes();
			return sizeInBytes;
		}

		public override string ToString()
		{
			return "term=" + term + ",field=" + field + ",value=" + value;
		}

		/// <summary>An in-place update to a binary DocValues field</summary>
		public sealed class BinaryDocValuesUpdate : DocValuesUpdate
		{
			private static readonly long RAW_VALUE_SIZE_IN_BYTES = RamUsageEstimator.NUM_BYTES_ARRAY_HEADER
				 + 2 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator.NUM_BYTES_OBJECT_REF;

			internal static readonly BytesRef MISSING = new BytesRef();

			internal BinaryDocValuesUpdate(Term term, string field, BytesRef value) : base(DocValuesFieldUpdates.Type
				.BINARY, term, field, value == null ? MISSING : value)
			{
			}

			internal override long ValueSizeInBytes()
			{
				return RAW_VALUE_SIZE_IN_BYTES + ((BytesRef)value).bytes.Length;
			}
		}

		/// <summary>An in-place update to a numeric DocValues field</summary>
		public sealed class NumericDocValuesUpdate : DocValuesUpdate
		{
			internal static readonly long MISSING = System.Convert.ToInt64(0);

			internal NumericDocValuesUpdate(Term term, string field, long value) : base(DocValuesFieldUpdates.Type
				.NUMERIC, term, field, value == null ? MISSING : value)
			{
			}

			internal override long ValueSizeInBytes()
			{
				return RamUsageEstimator.NUM_BYTES_LONG;
			}
		}
	}
}
