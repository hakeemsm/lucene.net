/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	/// <summary>Writes and Merges Lucene 3.x norms format</summary>
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWNormsConsumer : DocValuesConsumer
	{
		/// <summary>norms header placeholder</summary>
		private static readonly byte[] NORMS_HEADER = new byte[] { (byte)('N'), (byte)('R'
			), (byte)('M'), unchecked((byte)(-1)) };

		/// <summary>Extension of norms file</summary>
		private static readonly string NORMS_EXTENSION = "nrm";

		/// <summary>Extension of separate norms file</summary>
		[System.ObsoleteAttribute(@"Only for reading existing 3.x indexes")]
		[Obsolete]
		private static readonly string SEPARATE_NORMS_EXTENSION = "s";

		private readonly IndexOutput @out;

		private int lastFieldNumber = -1;

		/// <exception cref="System.IO.IOException"></exception>
		public PreFlexRWNormsConsumer(Directory directory, string segment, IOContext context
			)
		{
			// only for 
			 
			//assert
			string normsFileName = IndexFileNames.SegmentFileName(segment, string.Empty, NORMS_EXTENSION
				);
			bool success = false;
			IndexOutput output = null;
			try
			{
				output = directory.CreateOutput(normsFileName, context);
				output.WriteBytes(NORMS_HEADER, 0, NORMS_HEADER.Length);
				@out = output;
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(output);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddNumericField(FieldInfo field, Iterable<Number> values)
		{
			 
			//assert field.number > lastFieldNumber : "writing norms fields out of order" + lastFieldNumber + " -> " + field.number;
			foreach (Number n in values)
			{
				if (n < byte.MinValue || n > byte.MaxValue)
				{
					throw new NotSupportedException("3.x cannot index norms that won't fit in a byte, got: "
						 + n);
				}
				@out.WriteByte(n);
			}
			lastFieldNumber = field.number;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			IOUtils.Close(@out);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddBinaryField(FieldInfo field, Iterable<BytesRef> values)
		{
			throw new Exception();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedField(FieldInfo field, Iterable<BytesRef> values, Iterable
			<Number> docToOrd)
		{
			throw new Exception();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedSetField(FieldInfo field, Iterable<BytesRef> values
			, Iterable<Number> docToOrdCount, Iterable<Number> ords)
		{
			throw new Exception();
		}
	}
}
