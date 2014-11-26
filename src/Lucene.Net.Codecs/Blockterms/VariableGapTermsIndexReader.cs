/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Blockterms;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>
	/// See
	/// <see cref="VariableGapTermsIndexWriter">VariableGapTermsIndexWriter</see>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class VariableGapTermsIndexReader : TermsIndexReaderBase
	{
		private readonly PositiveIntOutputs fstOutputs = PositiveIntOutputs.GetSingleton(
			);

		private int indexDivisor;

		private IndexInput @in;

		private volatile bool indexLoaded;

		internal readonly Dictionary<FieldInfo, VariableGapTermsIndexReader.FieldIndexData
			> fields = new Dictionary<FieldInfo, VariableGapTermsIndexReader.FieldIndexData>
			();

		private long dirOffset;

		private readonly int version;

		internal readonly string segment;

		/// <exception cref="System.IO.IOException"></exception>
		public VariableGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, string segment
			, int indexDivisor, string segmentSuffix, IOContext context)
		{
			// for toDot
			// for toDot
			// for toDot
			// for toDot
			// Closed if indexLoaded is true:
			// start of the field info data
			@in = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, VariableGapTermsIndexWriter
				.TERMS_INDEX_EXTENSION), new IOContext(context, true));
			this.segment = segment;
			bool success = false;
			//HM:revisit 
			//assert indexDivisor == -1 || indexDivisor > 0;
			try
			{
				version = ReadHeader(@in);
				this.indexDivisor = indexDivisor;
				if (version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
				{
					CodecUtil.ChecksumEntireFile(@in);
				}
				SeekDir(@in, dirOffset);
				// Read directory
				int numFields = @in.ReadVInt();
				if (numFields < 0)
				{
					throw new CorruptIndexException("invalid numFields: " + numFields + " (resource="
						 + @in + ")");
				}
				for (int i = 0; i < numFields; i++)
				{
					int field = @in.ReadVInt();
					long indexStart = @in.ReadVLong();
					FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
					VariableGapTermsIndexReader.FieldIndexData previous = fields.Put(fieldInfo, new VariableGapTermsIndexReader.FieldIndexData
						(this, fieldInfo, indexStart));
					if (previous != null)
					{
						throw new CorruptIndexException("duplicate field: " + fieldInfo.name + " (resource="
							 + @in + ")");
					}
				}
				success = true;
			}
			finally
			{
				if (indexDivisor > 0)
				{
					@in.Close();
					@in = null;
					if (success)
					{
						indexLoaded = true;
					}
				}
			}
		}

		public override int GetDivisor()
		{
			return indexDivisor;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int ReadHeader(IndexInput input)
		{
			int version = CodecUtil.CheckHeader(input, VariableGapTermsIndexWriter.CODEC_NAME
				, VariableGapTermsIndexWriter.VERSION_START, VariableGapTermsIndexWriter.VERSION_CURRENT
				);
			if (version < VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
			{
				dirOffset = input.ReadLong();
			}
			return version;
		}

		private class IndexEnum : TermsIndexReaderBase.FieldIndexEnum
		{
			private readonly BytesRefFSTEnum<long> fstEnum;

			private BytesRefFSTEnum.InputOutput<long> current;

			public IndexEnum(FST<long> fst)
			{
				fstEnum = new BytesRefFSTEnum<long>(fst);
			}

			public override BytesRef Term()
			{
				if (current == null)
				{
					return null;
				}
				else
				{
					return current.input;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Seek(BytesRef target)
			{
				//System.out.println("VGR: seek field=" + fieldInfo.name + " target=" + target);
				current = fstEnum.SeekFloor(target);
				//System.out.println("  got input=" + current.input + " output=" + current.output);
				return current.output;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Next()
			{
				//System.out.println("VGR: next field=" + fieldInfo.name);
				current = fstEnum.Next();
				if (current == null)
				{
					//System.out.println("  eof");
					return -1;
				}
				else
				{
					return current.output;
				}
			}

			public override long Ord()
			{
				throw new NotSupportedException();
			}

			public override long Seek(long ord)
			{
				throw new NotSupportedException();
			}
		}

		public override bool SupportsOrd()
		{
			return false;
		}

		private sealed class FieldIndexData
		{
			private readonly long indexStart;

			private volatile FST<long> fst;

			/// <exception cref="System.IO.IOException"></exception>
			public FieldIndexData(VariableGapTermsIndexReader _enclosing, FieldInfo fieldInfo
				, long indexStart)
			{
				this._enclosing = _enclosing;
				// Set only if terms index is loaded:
				this.indexStart = indexStart;
				if (this._enclosing.indexDivisor > 0)
				{
					this.LoadTermsIndex();
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void LoadTermsIndex()
			{
				if (this.fst == null)
				{
					IndexInput clone = ((IndexInput)this._enclosing.@in.Clone());
					clone.Seek(this.indexStart);
					this.fst = new FST<long>(clone, this._enclosing.fstOutputs);
					clone.Close();
					if (this._enclosing.indexDivisor > 1)
					{
						// subsample
						IntsRef scratchIntsRef = new IntsRef();
						PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
						Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
						BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(this.fst);
						BytesRefFSTEnum.InputOutput<long> result;
						int count = this._enclosing.indexDivisor;
						while ((result = fstEnum.Next()) != null)
						{
							if (count == this._enclosing.indexDivisor)
							{
								builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(result.input, scratchIntsRef
									), result.output);
								count = 0;
							}
							count++;
						}
						this.fst = builder.Finish();
					}
				}
			}

			/// <summary>Returns approximate RAM bytes used</summary>
			public long RamBytesUsed()
			{
				return this.fst == null ? 0 : this.fst.SizeInBytes();
			}

			private readonly VariableGapTermsIndexReader _enclosing;
		}

		public override TermsIndexReaderBase.FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo
			)
		{
			VariableGapTermsIndexReader.FieldIndexData fieldData = fields.Get(fieldInfo);
			if (fieldData.fst == null)
			{
				return null;
			}
			else
			{
				return new VariableGapTermsIndexReader.IndexEnum(fieldData.fst);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (@in != null && !indexLoaded)
			{
				@in.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SeekDir(IndexInput input, long dirOffset)
		{
			if (version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
			{
				input.Seek(input.Length() - CodecUtil.FooterLength() - 8);
				dirOffset = input.ReadLong();
			}
			else
			{
				if (version >= VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
				{
					input.Seek(input.Length() - 8);
					dirOffset = input.ReadLong();
				}
			}
			input.Seek(dirOffset);
		}

		public override long RamBytesUsed()
		{
			long sizeInBytes = 0;
			foreach (VariableGapTermsIndexReader.FieldIndexData entry in fields.Values)
			{
				sizeInBytes += entry.RamBytesUsed();
			}
			return sizeInBytes;
		}
	}
}
