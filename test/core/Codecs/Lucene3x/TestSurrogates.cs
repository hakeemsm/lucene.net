/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Codecs.Lucene3x
{
	public class TestSurrogates : LuceneTestCase
	{
		/// <summary>we will manually instantiate preflex-rw here</summary>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		private static string MakeDifficultRandomUnicodeString(Random r)
		{
			int end = r.Next(20);
			if (end == 0)
			{
				// allow 0 length
				return string.Empty;
			}
			char[] buffer = new char[end];
			for (int i = 0; i < end; i++)
			{
				int t = r.Next(5);
				if (0 == t && i < end - 1)
				{
					// hi
					buffer[i++] = (char)(unchecked((int)(0xd800)) + r.Next(2));
					// lo
					buffer[i] = (char)(unchecked((int)(0xdc00)) + r.Next(2));
				}
				else
				{
					if (t <= 3)
					{
						buffer[i] = (char)('a' + r.Next(2));
					}
					else
					{
						if (4 == t)
						{
							buffer[i] = (char)(unchecked((int)(0xe000)) + r.Next(2));
						}
					}
				}
			}
			return new string(buffer, 0, end);
		}

		private string ToHexString(Term t)
		{
			return t.Field() + ":" + UnicodeUtil.ToHexString(t.Text());
		}

		private string GetRandomString(Random r)
		{
			string s;
			if (r.Next(5) == 1)
			{
				if (r.Next(3) == 1)
				{
					s = MakeDifficultRandomUnicodeString(r);
				}
				else
				{
					s = TestUtil.RandomUnicodeString(r);
				}
			}
			else
			{
				s = TestUtil.RandomRealisticUnicodeString(r);
			}
			return s;
		}

		private class SortTermAsUTF16Comparator : IComparer<Term>
		{
			private static readonly IComparer<BytesRef> legacyComparator = BytesRef.GetUTF8SortedAsUTF16Comparator
				();

			public virtual int Compare(Term term1, Term term2)
			{
				if (term1.Field().Equals(term2.Field()))
				{
					return legacyComparator.Compare(term1.Bytes(), term2.Bytes());
				}
				else
				{
					return Sharpen.Runtime.CompareOrdinal(term1.Field(), term2.Field());
				}
			}
		}

		private static readonly TestSurrogates.SortTermAsUTF16Comparator termAsUTF16Comparator
			 = new TestSurrogates.SortTermAsUTF16Comparator();

		// single straight enum
		/// <exception cref="System.IO.IOException"></exception>
		private void DoTestStraightEnum(IList<Term> fieldTerms, IndexReader reader, int uniqueTermCount
			)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: top now enum reader=" + reader);
			}
			Fields fields = MultiFields.GetFields(reader);
			{
				// Test straight enum:
				int termCount = 0;
				foreach (string field in fields)
				{
					Terms terms = fields.Terms(field);
					NUnit.Framework.Assert.IsNotNull(terms);
					TermsEnum termsEnum = terms.Iterator(null);
					BytesRef text;
					BytesRef lastText = null;
					while ((text = termsEnum.Next()) != null)
					{
						Term exp = fieldTerms[termCount];
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got term=" + field + ":" + UnicodeUtil.ToHexString
								(text.Utf8ToString()));
							System.Console.Out.WriteLine("       exp=" + exp.Field() + ":" + UnicodeUtil.ToHexString
								(exp.Text().ToString()));
							System.Console.Out.WriteLine();
						}
						if (lastText == null)
						{
							lastText = BytesRef.DeepCopyOf(text);
						}
						else
						{
							NUnit.Framework.Assert.IsTrue(lastText.CompareTo(text) < 0);
							lastText.CopyBytes(text);
						}
						NUnit.Framework.Assert.AreEqual(exp.Field(), field);
						NUnit.Framework.Assert.AreEqual(exp.Bytes(), text);
						termCount++;
					}
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  no more terms for field=" + field);
					}
				}
				NUnit.Framework.Assert.AreEqual(uniqueTermCount, termCount);
			}
		}

		// randomly seeks to term that we know exists, then next's
		// from there
		/// <exception cref="System.IO.IOException"></exception>
		private void DoTestSeekExists(Random r, IList<Term> fieldTerms, IndexReader reader
			)
		{
			IDictionary<string, TermsEnum> tes = new Dictionary<string, TermsEnum>();
			// Test random seek to existing term, then enum:
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: top now seek");
			}
			int num = AtLeast(100);
			for (int iter = 0; iter < num; iter++)
			{
				// pick random field+term
				int spot = r.Next(fieldTerms.Count);
				Term term = fieldTerms[spot];
				string field = term.Field();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: exist seek field=" + field + " term=" + UnicodeUtil
						.ToHexString(term.Text()));
				}
				// seek to it
				TermsEnum te = tes.Get(field);
				if (te == null)
				{
					te = MultiFields.GetTerms(reader, field).Iterator(null);
					tes.Put(field, te);
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  done get enum");
				}
				// seek should find the term
				NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.FOUND, te.SeekCeil(term.Bytes
					()));
				// now .next() this many times:
				int ct = TestUtil.NextInt(r, 5, 100);
				for (int i = 0; i < ct; i++)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now next()");
					}
					if (1 + spot + i >= fieldTerms.Count)
					{
						break;
					}
					term = fieldTerms[1 + spot + i];
					if (!term.Field().Equals(field))
					{
						NUnit.Framework.Assert.IsNull(te.Next());
						break;
					}
					else
					{
						BytesRef t = te.Next();
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got term=" + (t == null ? null : UnicodeUtil.ToHexString
								(t.Utf8ToString())));
							System.Console.Out.WriteLine("       exp=" + UnicodeUtil.ToHexString(term.Text().
								ToString()));
						}
						NUnit.Framework.Assert.AreEqual(term.Bytes(), t);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoTestSeekDoesNotExist(Random r, int numField, IList<Term> fieldTerms
			, Term[] fieldTermsArray, IndexReader reader)
		{
			IDictionary<string, TermsEnum> tes = new Dictionary<string, TermsEnum>();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: top random seeks");
			}
			{
				int num = AtLeast(100);
				for (int iter = 0; iter < num; iter++)
				{
					// seek to random spot
					string field = string.Intern(("f" + r.Next(numField)));
					Term tx = new Term(field, GetRandomString(r));
					int spot = System.Array.BinarySearch(fieldTermsArray, tx);
					if (spot < 0)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: non-exist seek to " + field + ":" + UnicodeUtil
								.ToHexString(tx.Text()));
						}
						// term does not exist:
						TermsEnum te = tes.Get(field);
						if (te == null)
						{
							te = MultiFields.GetTerms(reader, field).Iterator(null);
							tes.Put(field, te);
						}
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got enum");
						}
						spot = -spot - 1;
						if (spot == fieldTerms.Count || !fieldTerms[spot].Field().Equals(field))
						{
							NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.END, te.SeekCeil(tx.Bytes())
								);
						}
						else
						{
							NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, te.SeekCeil(tx.Bytes
								()));
							if (VERBOSE)
							{
								System.Console.Out.WriteLine("  got term=" + UnicodeUtil.ToHexString(te.Term().Utf8ToString
									()));
								System.Console.Out.WriteLine("  exp term=" + UnicodeUtil.ToHexString(fieldTerms[spot
									].Text()));
							}
							NUnit.Framework.Assert.AreEqual(fieldTerms[spot].Bytes(), te.Term());
							// now .next() this many times:
							int ct = TestUtil.NextInt(r, 5, 100);
							for (int i = 0; i < ct; i++)
							{
								if (VERBOSE)
								{
									System.Console.Out.WriteLine("TEST: now next()");
								}
								if (1 + spot + i >= fieldTerms.Count)
								{
									break;
								}
								Term term = fieldTerms[1 + spot + i];
								if (!term.Field().Equals(field))
								{
									NUnit.Framework.Assert.IsNull(te.Next());
									break;
								}
								else
								{
									BytesRef t = te.Next();
									if (VERBOSE)
									{
										System.Console.Out.WriteLine("  got term=" + (t == null ? null : UnicodeUtil.ToHexString
											(t.Utf8ToString())));
										System.Console.Out.WriteLine("       exp=" + UnicodeUtil.ToHexString(term.Text().
											ToString()));
									}
									NUnit.Framework.Assert.AreEqual(term.Bytes(), t);
								}
							}
						}
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSurrogatesOrder()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetCodec(new PreFlexRWCodec()));
			int numField = TestUtil.NextInt(Random(), 2, 5);
			int uniqueTermCount = 0;
			int tc = 0;
			IList<Term> fieldTerms = new AList<Term>();
			for (int f = 0; f < numField; f++)
			{
				string field = "f" + f;
				int numTerms = AtLeast(200);
				ICollection<string> uniqueTerms = new HashSet<string>();
				for (int i = 0; i < numTerms; i++)
				{
					string term = GetRandomString(Random()) + "_ " + (tc++);
					uniqueTerms.AddItem(term);
					fieldTerms.AddItem(new Term(field, term));
					Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
						();
					doc.Add(NewStringField(field, term, Field.Store.NO));
					w.AddDocument(doc);
				}
				uniqueTermCount += uniqueTerms.Count;
			}
			IndexReader reader = w.GetReader();
			if (VERBOSE)
			{
				fieldTerms.Sort(termAsUTF16Comparator);
				System.Console.Out.WriteLine("\nTEST: UTF16 order");
				foreach (Term t in fieldTerms)
				{
					System.Console.Out.WriteLine("  " + ToHexString(t));
				}
			}
			// sorts in code point order:
			fieldTerms.Sort();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: codepoint order");
				foreach (Term t in fieldTerms)
				{
					System.Console.Out.WriteLine("  " + ToHexString(t));
				}
			}
			Term[] fieldTermsArray = Sharpen.Collections.ToArray(fieldTerms, new Term[fieldTerms
				.Count]);
			//SegmentInfo si = makePreFlexSegment(r, "_0", dir, fieldInfos, codec, fieldTerms);
			//FieldsProducer fields = codec.fieldsProducer(new SegmentReadState(dir, si, fieldInfos, 1024, 1));
			//assertNotNull(fields);
			DoTestStraightEnum(fieldTerms, reader, uniqueTermCount);
			DoTestSeekExists(Random(), fieldTerms, reader);
			DoTestSeekDoesNotExist(Random(), numField, fieldTerms, fieldTermsArray, reader);
			reader.Close();
			w.Close();
			dir.Close();
		}
	}
}
