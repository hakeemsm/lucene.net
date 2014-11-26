/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>
	/// reads plaintext stored fields
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextStoredFieldsReader : StoredFieldsReader
	{
		private long offsets;

		private IndexInput @in;

		private BytesRef scratch = new BytesRef();

		private CharsRef scratchUTF16 = new CharsRef();

		private readonly FieldInfos fieldInfos;

		/// <exception cref="System.IO.IOException"></exception>
		public SimpleTextStoredFieldsReader(Directory directory, SegmentInfo si, FieldInfos
			 fn, IOContext context)
		{
			this.fieldInfos = fn;
			bool success = false;
			try
			{
				@in = directory.OpenInput(IndexFileNames.SegmentFileName(si.name, string.Empty, SimpleTextStoredFieldsWriter
					.FIELDS_EXTENSION), context);
				success = true;
			}
			finally
			{
				if (!success)
				{
					try
					{
						Close();
					}
					catch
					{
					}
				}
			}
			// ensure we throw our original exception
			ReadIndex(si.GetDocCount());
		}

		internal SimpleTextStoredFieldsReader(long[] offsets, IndexInput @in, FieldInfos 
			fieldInfos)
		{
			// used by clone
			this.offsets = offsets;
			this.@in = @in;
			this.fieldInfos = fieldInfos;
		}

		// we don't actually write a .fdx-like index, instead we read the 
		// stored fields file in entirety up-front and save the offsets 
		// so we can seek to the documents later.
		/// <exception cref="System.IO.IOException"></exception>
		private void ReadIndex(int size)
		{
			ChecksumIndexInput input = new BufferedChecksumIndexInput(@in);
			offsets = new long[size];
			int upto = 0;
			while (!scratch.Equals(END))
			{
				SimpleTextUtil.ReadLine(input, scratch);
				if (StringHelper.StartsWith(scratch, DOC))
				{
					offsets[upto] = input.GetFilePointer();
					upto++;
				}
			}
			SimpleTextUtil.CheckFooter(input);
		}

		//HM:revisit 
		//assert upto == offsets.length;
		/// <exception cref="System.IO.IOException"></exception>
		public override void VisitDocument(int n, StoredFieldVisitor visitor)
		{
			@in.Seek(offsets[n]);
			ReadLine();
			//HM:revisit 
			//assert StringHelper.startsWith(scratch, NUM);
			int numFields = ParseIntAt(NUM.length);
			for (int i = 0; i < numFields; i++)
			{
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, FIELD);
				int fieldNumber = ParseIntAt(FIELD.length);
				FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, NAME);
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, TYPE);
				BytesRef type;
				if (EqualsAt(TYPE_STRING, scratch, TYPE.length))
				{
					type = TYPE_STRING;
				}
				else
				{
					if (EqualsAt(TYPE_BINARY, scratch, TYPE.length))
					{
						type = TYPE_BINARY;
					}
					else
					{
						if (EqualsAt(TYPE_INT, scratch, TYPE.length))
						{
							type = TYPE_INT;
						}
						else
						{
							if (EqualsAt(TYPE_LONG, scratch, TYPE.length))
							{
								type = TYPE_LONG;
							}
							else
							{
								if (EqualsAt(TYPE_FLOAT, scratch, TYPE.length))
								{
									type = TYPE_FLOAT;
								}
								else
								{
									if (EqualsAt(TYPE_DOUBLE, scratch, TYPE.length))
									{
										type = TYPE_DOUBLE;
									}
									else
									{
										throw new RuntimeException("unknown field type");
									}
								}
							}
						}
					}
				}
				switch (visitor.NeedsField(fieldInfo))
				{
					case StoredFieldVisitor.Status.YES:
					{
						ReadField(type, fieldInfo, visitor);
						break;
					}

					case StoredFieldVisitor.Status.NO:
					{
						ReadLine();
						//HM:revisit 
						//assert StringHelper.startsWith(scratch, VALUE);
						break;
					}

					case StoredFieldVisitor.Status.STOP:
					{
						return;
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadField(BytesRef type, FieldInfo fieldInfo, StoredFieldVisitor visitor
			)
		{
			ReadLine();
			//HM:revisit 
			//assert StringHelper.startsWith(scratch, VALUE);
			if (type == TYPE_STRING)
			{
				visitor.StringField(fieldInfo, new string(scratch.bytes, scratch.offset + VALUE.length
					, scratch.length - VALUE.length, StandardCharsets.UTF_8));
			}
			else
			{
				if (type == TYPE_BINARY)
				{
					byte[] copy = new byte[scratch.length - VALUE.length];
					System.Array.Copy(scratch.bytes, scratch.offset + VALUE.length, copy, 0, copy.Length
						);
					visitor.BinaryField(fieldInfo, copy);
				}
				else
				{
					if (type == TYPE_INT)
					{
						UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length
							 - VALUE.length, scratchUTF16);
						visitor.IntField(fieldInfo, System.Convert.ToInt32(scratchUTF16.ToString()));
					}
					else
					{
						if (type == TYPE_LONG)
						{
							UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length
								 - VALUE.length, scratchUTF16);
							visitor.LongField(fieldInfo, long.Parse(scratchUTF16.ToString()));
						}
						else
						{
							if (type == TYPE_FLOAT)
							{
								UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length
									 - VALUE.length, scratchUTF16);
								visitor.FloatField(fieldInfo, float.ParseFloat(scratchUTF16.ToString()));
							}
							else
							{
								if (type == TYPE_DOUBLE)
								{
									UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + VALUE.length, scratch.length
										 - VALUE.length, scratchUTF16);
									visitor.DoubleField(fieldInfo, double.ParseDouble(scratchUTF16.ToString()));
								}
							}
						}
					}
				}
			}
		}

		public override StoredFieldsReader Clone()
		{
			if (@in == null)
			{
				throw new AlreadyClosedException("this FieldsReader is closed");
			}
			return new Lucene.Net.Codecs.Simpletext.SimpleTextStoredFieldsReader(offsets
				, ((IndexInput)@in.Clone()), fieldInfos);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				IOUtils.Close(@in);
			}
			finally
			{
				@in = null;
				offsets = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadLine()
		{
			SimpleTextUtil.ReadLine(@in, scratch);
		}

		private int ParseIntAt(int offset)
		{
			UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + offset, scratch.length - 
				offset, scratchUTF16);
			return ArrayUtil.ParseInt(scratchUTF16.chars, 0, scratchUTF16.length);
		}

		private bool EqualsAt(BytesRef a, BytesRef b, int bOffset)
		{
			return a.length == b.length - bOffset && ArrayUtil.Equals(a.bytes, a.offset, b.bytes
				, b.offset + bOffset, b.length - bOffset);
		}

		public override long RamBytesUsed()
		{
			return 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
		}
	}
}
