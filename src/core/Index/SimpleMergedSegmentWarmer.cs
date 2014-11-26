using System;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
	/// <summary>
	/// A very simple merged segment warmer that just ensures
	/// data structures are initialized.
	/// </summary>
	/// <remarks>
	/// A very simple merged segment warmer that just ensures
	/// data structures are initialized.
	/// </remarks>
	public class SimpleMergedSegmentWarmer : IndexWriter.IndexReaderWarmer
	{
		private readonly InfoStream infoStream;

		/// <summary>Creates a new SimpleMergedSegmentWarmer</summary>
		/// <param name="infoStream">InfoStream to log statistics about warming.</param>
		public SimpleMergedSegmentWarmer(InfoStream infoStream)
		{
			this.infoStream = infoStream;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Warm(AtomicReader reader)
		{
			long startTime = DateTime.Now.CurrentTimeMillis();
			int indexedCount = 0;
			int docValuesCount = 0;
			int normsCount = 0;
			foreach (FieldInfo info in reader.FieldInfos)
			{
				if (info.IsIndexed)
				{
					reader.Terms(info.name);
					indexedCount++;
					if (info.HasNorms)
					{
						reader.GetNormValues(info.name);
						normsCount++;
					}
				}
				if (info.HasDocValues)
				{
					switch (info.GetDocValuesType())
					{
						case FieldInfo.DocValuesType.NUMERIC:
						{
							reader.GetNumericDocValues(info.name);
							break;
						}

						case FieldInfo.DocValuesType.BINARY:
						{
							reader.GetBinaryDocValues(info.name);
							break;
						}

						case FieldInfo.DocValuesType.SORTED:
						{
							reader.GetSortedDocValues(info.name);
							break;
						}

						case FieldInfo.DocValuesType.SORTED_SET:
						{
							reader.GetSortedSetDocValues(info.name);
							break;
						}

						default:
						{
							break;
						}
					}
					//HM:revisit 
					//assert false; // unknown dv type
					docValuesCount++;
				}
			}
			reader.Document(0);
			reader.GetTermVectors(0);
			if (infoStream.IsEnabled("SMSW"))
			{
				infoStream.Message("SMSW", "Finished warming segment: " + reader + ", indexed=" +
					 indexedCount + ", docValues=" + docValuesCount + ", norms=" + normsCount + ", time="
					 + (DateTime.Now.CurrentTimeMillis() - startTime));
			}
		}
	}
}
