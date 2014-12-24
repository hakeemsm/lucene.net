/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexWriterUnicode : LuceneTestCase
	{
		internal readonly string[] utf8Data = new string[] { "ab\udc17cd", "ab\ufffdcd", 
			"\udc17abcd", "\ufffdabcd", "\udc17", "\ufffd", "ab\udc17\udc17cd", "ab\ufffd\ufffdcd"
			, "\udc17\udc17abcd", "\ufffd\ufffdabcd", "\udc17\udc17", "\ufffd\ufffd", "ab\ud917cd"
			, "ab\ufffdcd", "\ud917abcd", "\ufffdabcd", "\ud917", "\ufffd", "ab\ud917\ud917cd"
			, "ab\ufffd\ufffdcd", "\ud917\ud917abcd", "\ufffd\ufffdabcd", "\ud917\ud917", "\ufffd\ufffd"
			, "ab\udc17\ud917cd", "ab\ufffd\ufffdcd", "\udc17\ud917abcd", "\ufffd\ufffdabcd"
			, "\udc17\ud917", "\ufffd\ufffd", "ab\udc17\ud917\udc17\ud917cd", "ab\ufffd\ud917\udc17\ufffdcd"
			, "\udc17\ud917\udc17\ud917abcd", "\ufffd\ud917\udc17\ufffdabcd", "\udc17\ud917\udc17\ud917"
			, "\ufffd\ud917\udc17\ufffd" };

		// unpaired low surrogate
		// unpaired high surrogate
		// backwards surrogates
		private int NextInt(int lim)
		{
			return Random().Next(lim);
		}

		private int NextInt(int start, int end)
		{
			return start + NextInt(end - start);
		}

		private bool FillUnicode(char[] buffer, char[] expected, int offset, int count)
		{
			int len = offset + count;
			bool hasIllegal = false;
			if (offset > 0 && buffer[offset] >= unchecked((int)(0xdc00)) && buffer[offset] < 
				unchecked((int)(0xe000)))
			{
				// Don't start in the middle of a valid surrogate pair
				offset--;
			}
			for (int i = offset; i < len; i++)
			{
				int t = NextInt(6);
				if (0 == t && i < len - 1)
				{
					// Make a surrogate pair
					// High surrogate
					expected[i] = buffer[i++] = (char)NextInt(unchecked((int)(0xd800)), unchecked((int
						)(0xdc00)));
					// Low surrogate
					expected[i] = buffer[i] = (char)NextInt(unchecked((int)(0xdc00)), unchecked((int)
						(0xe000)));
				}
				else
				{
					if (t <= 1)
					{
						expected[i] = buffer[i] = (char)NextInt(unchecked((int)(0x80)));
					}
					else
					{
						if (2 == t)
						{
							expected[i] = buffer[i] = (char)NextInt(unchecked((int)(0x80)), unchecked((int)(0x800
								)));
						}
						else
						{
							if (3 == t)
							{
								expected[i] = buffer[i] = (char)NextInt(unchecked((int)(0x800)), unchecked((int)(
									0xd800)));
							}
							else
							{
								if (4 == t)
								{
									expected[i] = buffer[i] = (char)NextInt(unchecked((int)(0xe000)), unchecked((int)
										(0xffff)));
								}
								else
								{
									if (5 == t && i < len - 1)
									{
										// Illegal unpaired surrogate
										if (NextInt(10) == 7)
										{
											if (Random().NextBoolean())
											{
												buffer[i] = (char)NextInt(unchecked((int)(0xd800)), unchecked((int)(0xdc00)));
											}
											else
											{
												buffer[i] = (char)NextInt(unchecked((int)(0xdc00)), unchecked((int)(0xe000)));
											}
											expected[i++] = (char)unchecked((int)(0xfffd));
											expected[i] = buffer[i] = (char)NextInt(unchecked((int)(0x800)), unchecked((int)(
												0xd800)));
											hasIllegal = true;
										}
										else
										{
											expected[i] = buffer[i] = (char)NextInt(unchecked((int)(0x800)), unchecked((int)(
												0xd800)));
										}
									}
									else
									{
										expected[i] = buffer[i] = ' ';
									}
								}
							}
						}
					}
				}
			}
			return hasIllegal;
		}

		// both start & end are inclusive
		private int GetInt(Random r, int start, int end)
		{
			return start + r.Next(1 + end - start);
		}

		private string AsUnicodeChar(char c)
		{
			return "U+" + Sharpen.Extensions.ToHexString(c);
		}

		private string TermDesc(string s)
		{
			string s0;
			IsTrue(s.Length <= 2);
			if (s.Length == 1)
			{
				s0 = AsUnicodeChar(s[0]);
			}
			else
			{
				s0 = AsUnicodeChar(s[0]) + "," + AsUnicodeChar(s[1]);
			}
			return s0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckTermsOrder(IndexReader r, ICollection<string> allTerms, bool isTop
			)
		{
			TermsEnum terms = MultiFields.GetFields(r).Terms("f").Iterator(null);
			BytesRef last = new BytesRef();
			ICollection<string> seenTerms = new HashSet<string>();
			while (true)
			{
				BytesRef term = terms.Next();
				if (term == null)
				{
					break;
				}
				IsTrue(last.CompareTo(term) < 0);
				last.CopyBytes(term);
				string s = term.Utf8ToString();
				IsTrue("term " + TermDesc(s) + " was not added to index (count="
					 + allTerms.Count + ")", allTerms.Contains(s));
				seenTerms.AddItem(s);
			}
			if (isTop)
			{
				IsTrue(allTerms.Equals(seenTerms));
			}
			// Test seeking:
			Iterator<string> it = seenTerms.Iterator();
			while (it.HasNext())
			{
				BytesRef tr = new BytesRef(it.Next());
				AreEqual("seek failed for term=" + TermDesc(tr.Utf8ToString
					()), TermsEnum.SeekStatus.FOUND, terms.SeekCeil(tr));
			}
		}

		// LUCENE-510
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomUnicodeStrings()
		{
			char[] buffer = new char[20];
			char[] expected = new char[20];
			BytesRef utf8 = new BytesRef(20);
			CharsRef utf16 = new CharsRef(20);
			int num = AtLeast(100000);
			for (int iter = 0; iter < num; iter++)
			{
				bool hasIllegal = FillUnicode(buffer, expected, 0, 20);
				UnicodeUtil.UTF16toUTF8(buffer, 0, 20, utf8);
				if (!hasIllegal)
				{
					byte[] b = Sharpen.Runtime.GetBytesForString(new string(buffer, 0, 20), StandardCharsets
						.UTF_8);
					AreEqual(b.Length, utf8.length);
					for (int i = 0; i < b.Length; i++)
					{
						AreEqual(b[i], utf8.bytes[i]);
					}
				}
				UnicodeUtil.UTF8toUTF16(utf8.bytes, 0, utf8.length, utf16);
				AreEqual(utf16.length, 20);
				for (int i_1 = 0; i_1 < 20; i_1++)
				{
					AreEqual(expected[i_1], utf16.chars[i_1]);
				}
			}
		}

		// LUCENE-510
		/// <exception cref="System.Exception"></exception>
		public virtual void TestAllUnicodeChars()
		{
			BytesRef utf8 = new BytesRef(10);
			CharsRef utf16 = new CharsRef(10);
			char[] chars = new char[2];
			for (int ch = 0; ch < unchecked((int)(0x0010FFFF)); ch++)
			{
				if (ch == unchecked((int)(0xd800)))
				{
					// Skip invalid code points
					ch = unchecked((int)(0xe000));
				}
				int len = 0;
				if (ch <= unchecked((int)(0xffff)))
				{
					chars[len++] = (char)ch;
				}
				else
				{
					chars[len++] = (char)(((ch - unchecked((int)(0x0010000))) >> 10) + UnicodeUtil.UNI_SUR_HIGH_START
						);
					chars[len++] = (char)(((ch - unchecked((int)(0x0010000))) & unchecked((long)(0x3FFL
						))) + UnicodeUtil.UNI_SUR_LOW_START);
				}
				UnicodeUtil.UTF16toUTF8(chars, 0, len, utf8);
				string s1 = new string(chars, 0, len);
				string s2 = new string(utf8.bytes, 0, utf8.length, StandardCharsets.UTF_8);
				AreEqual("codepoint " + ch, s1, s2);
				UnicodeUtil.UTF8toUTF16(utf8.bytes, 0, utf8.length, utf16);
				AreEqual("codepoint " + ch, s1, new string(utf16.chars, 0, 
					utf16.length));
				byte[] b = Sharpen.Runtime.GetBytesForString(s1, StandardCharsets.UTF_8);
				AreEqual(utf8.length, b.Length);
				for (int j = 0; j < utf8.length; j++)
				{
					AreEqual(utf8.bytes[j], b[j]);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmbeddedFFFF()
		{
			Directory d = NewDirectory();
			IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("field", "a a\uffffb", Field.Store.NO));
			w.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewTextField("field", "a", Field.Store.NO));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			AreEqual(1, r.DocFreq(new Term("field", "a\uffffb")));
			r.Close();
			w.Close();
			d.Close();
		}

		// LUCENE-510
		/// <exception cref="System.Exception"></exception>
		public virtual void TestInvalidUTF16()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				TestIndexWriter.StringSplitAnalyzer()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			int count = utf8Data.Length / 2;
			for (int i = 0; i < count; i++)
			{
				doc.Add(NewTextField("f" + i, utf8Data[2 * i], Field.Store.YES));
			}
			w.AddDocument(doc);
			w.Close();
			IndexReader ir = DirectoryReader.Open(dir);
			Lucene.Net.Documents.Document doc2 = ir.Document(0);
			for (int i_1 = 0; i_1 < count; i_1++)
			{
				AreEqual("field " + i_1 + " was not indexed correctly", 1, 
					ir.DocFreq(new Term("f" + i_1, utf8Data[2 * i_1 + 1])));
				AreEqual("field " + i_1 + " is incorrect", utf8Data[2 * i_1
					 + 1], doc2.GetField("f" + i_1).StringValue = ));
			}
			ir.Close();
			dir.Close();
		}

		// Make sure terms, including ones with surrogate pairs,
		// sort in codepoint sort order by default
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTermUTF16SortOrder()
		{
			Random rnd = Random();
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(rnd, dir);
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			// Single segment
			Field f = NewStringField("f", string.Empty, Field.Store.NO);
			d.Add(f);
			char[] chars = new char[2];
			ICollection<string> allTerms = new HashSet<string>();
			int num = AtLeast(200);
			for (int i = 0; i < num; i++)
			{
				string s;
				if (rnd.NextBoolean())
				{
					// Single char
					if (rnd.NextBoolean())
					{
						// Above surrogates
						chars[0] = (char)GetInt(rnd, 1 + UnicodeUtil.UNI_SUR_LOW_END, unchecked((int)(0xffff
							)));
					}
					else
					{
						// Below surrogates
						chars[0] = (char)GetInt(rnd, 0, UnicodeUtil.UNI_SUR_HIGH_START - 1);
					}
					s = new string(chars, 0, 1);
				}
				else
				{
					// Surrogate pair
					chars[0] = (char)GetInt(rnd, UnicodeUtil.UNI_SUR_HIGH_START, UnicodeUtil.UNI_SUR_HIGH_END
						);
					IsTrue(((int)chars[0]) >= UnicodeUtil.UNI_SUR_HIGH_START &&
						 ((int)chars[0]) <= UnicodeUtil.UNI_SUR_HIGH_END);
					chars[1] = (char)GetInt(rnd, UnicodeUtil.UNI_SUR_LOW_START, UnicodeUtil.UNI_SUR_LOW_END
						);
					s = new string(chars, 0, 2);
				}
				allTerms.AddItem(s);
				f.StringValue = s);
				writer.AddDocument(d);
				if ((1 + i) % 42 == 0)
				{
					writer.Commit();
				}
			}
			IndexReader r = writer.GetReader();
			// Test each sub-segment
			foreach (AtomicReaderContext ctx in r.Leaves())
			{
				CheckTermsOrder(((AtomicReader)ctx.Reader()), allTerms, false);
			}
			CheckTermsOrder(r, allTerms, true);
			// Test multi segment
			r.Close();
			writer.ForceMerge(1);
			// Test single segment
			r = writer.GetReader();
			CheckTermsOrder(r, allTerms, true);
			r.Close();
			writer.Close();
			dir.Close();
		}
	}
}
