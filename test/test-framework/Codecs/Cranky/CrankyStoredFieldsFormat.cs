using System;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = System.IO.Directory;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankyStoredFieldsFormat : StoredFieldsFormat
	{
		internal readonly StoredFieldsFormat storedFlds;

		internal readonly Random random;

		internal CrankyStoredFieldsFormat(StoredFieldsFormat del, Random random)
		{
			this.storedFlds = del;
			this.random = random;
		}

		
		public override StoredFieldsReader FieldsReader(Lucene.Net.Store.Directory directory, SegmentInfo 
			si, FieldInfos fn, IOContext context)
		{
			return storedFlds.FieldsReader(directory, si, fn, context);
		}

		
		public override StoredFieldsWriter FieldsWriter(Lucene.Net.Store.Directory directory, SegmentInfo si, IOContext context)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from StoredFieldsFormat.fieldsWriter()");
			}
			return new CrankyStoredFieldsWriter(storedFlds.FieldsWriter(directory, si, context), random);
		}

		internal class CrankyStoredFieldsWriter : StoredFieldsWriter
		{
			internal readonly StoredFieldsWriter sfWriter;

			internal readonly Random random;

			internal CrankyStoredFieldsWriter(StoredFieldsWriter delegate_, Random random)
			{
				this.sfWriter = delegate_;
				this.random = random;
			}

			public override void Abort()
			{
				sfWriter.Abort();
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.abort()");
				}
			}

			
			public override void Finish(FieldInfos fis, int numDocs)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.finish()");
				}
				sfWriter.Finish(fis, numDocs);
			}

			
			public override int Merge(MergeState mergeState)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.merge()");
				}
				return base.Merge(mergeState);
			}


		    protected override void Dispose(bool disposing)
			{
				sfWriter.Dispose();
				if (random.Next(1000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.close()");
				}
			}

			// per doc/field methods: lower probability since they are invoked so many times.
			
			public override void StartDocument(int numFields)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.startDocument()");
				}
				sfWriter.StartDocument(numFields);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.finishDocument()"
						);
				}
				sfWriter.FinishDocument();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void WriteField(FieldInfo info, IIndexableField field)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from StoredFieldsWriter.writeField()");
				}
				sfWriter.WriteField(info, field);
			}
		}
	}
}
