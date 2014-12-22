/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Cranky;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Cranky
{
	internal class CrankyStoredFieldsFormat : StoredFieldsFormat
	{
		internal readonly StoredFieldsFormat delegate_;

		internal readonly Random random;

		internal CrankyStoredFieldsFormat(StoredFieldsFormat delegate_, Random random)
		{
			this.delegate_ = delegate_;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo 
			si, FieldInfos fn, IOContext context)
		{
			return delegate_.FieldsReader(directory, si, fn, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo 
			si, IOContext context)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from StoredFieldsFormat.fieldsWriter()");
			}
			return new CrankyStoredFieldsFormat.CrankyStoredFieldsWriter(delegate_.FieldsWriter
				(directory, si, context), random);
		}

		internal class CrankyStoredFieldsWriter : StoredFieldsWriter
		{
			internal readonly StoredFieldsWriter delegate_;

			internal readonly Random random;

			internal CrankyStoredFieldsWriter(StoredFieldsWriter delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			public override void Abort()
			{
				delegate_.Abort();
				if (random.Next(100) == 0)
				{
					throw new RuntimeException(new IOException("Fake IOException from StoredFieldsWriter.abort()"
						));
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(FieldInfos fis, int numDocs)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.finish()");
				}
				delegate_.Finish(fis, numDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Merge(MergeState mergeState)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.merge()");
				}
				return base.Merge(mergeState);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				delegate_.Close();
				if (random.Next(1000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.close()");
				}
			}

			// per doc/field methods: lower probability since they are invoked so many times.
			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDocument(int numFields)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.startDocument()");
				}
				delegate_.StartDocument(numFields);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.finishDocument()"
						);
				}
				delegate_.FinishDocument();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void WriteField(FieldInfo info, IndexableField field)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.writeField()");
				}
				delegate_.WriteField(info, field);
			}
		}
	}
}
