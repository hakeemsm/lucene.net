/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Reflection;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;
using Sharpen.Reflect;

namespace Lucene.Net.Test.Index
{
	public class TestFilterAtomicReader : LuceneTestCase
	{
		private class TestReader : FilterAtomicReader
		{
			/// <summary>Filter that only permits terms containing 'e'.</summary>
			/// <remarks>Filter that only permits terms containing 'e'.</remarks>
			private class TestFields : FilterAtomicReader.FilterFields
			{
				public TestFields(Fields @in) : base(@in)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Lucene.Net.Index.Terms Terms(string field)
				{
					return new TestFilterAtomicReader.TestReader.TestTerms(base.Terms(field));
				}
			}

			private class TestTerms : FilterAtomicReader.FilterTerms
			{
				public TestTerms(Terms @in) : base(@in)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum Iterator(TermsEnum reuse)
				{
					return new TestFilterAtomicReader.TestReader.TestTermsEnum(base.Iterator(reuse));
				}
			}

			private class TestTermsEnum : FilterAtomicReader.FilterTermsEnum
			{
				public TestTermsEnum(TermsEnum @in) : base(@in)
				{
				}

				/// <summary>Scan for terms containing the letter 'e'.</summary>
				/// <remarks>Scan for terms containing the letter 'e'.</remarks>
				/// <exception cref="System.IO.IOException"></exception>
				public override BytesRef Next()
				{
					BytesRef text;
					while ((text = @in.Next()) != null)
					{
						if (text.Utf8ToString().IndexOf('e') != -1)
						{
							return text;
						}
					}
					return null;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					return new TestFilterAtomicReader.TestReader.TestPositions(base.DocsAndPositions(
						liveDocs, reuse == null ? null : ((FilterAtomicReader.FilterDocsAndPositionsEnum
						)reuse).@in, flags));
				}
			}

			/// <summary>Filter that only returns odd numbered documents.</summary>
			/// <remarks>Filter that only returns odd numbered documents.</remarks>
			private class TestPositions : FilterAtomicReader.FilterDocsAndPositionsEnum
			{
				public TestPositions(DocsAndPositionsEnum @in) : base(@in)
				{
				}

				/// <summary>Scan for odd numbered documents.</summary>
				/// <remarks>Scan for odd numbered documents.</remarks>
				/// <exception cref="System.IO.IOException"></exception>
				public override int NextDoc()
				{
					int doc;
					while ((doc = @in.NextDoc()) != NO_MORE_DOCS)
					{
						if ((doc % 2) == 1)
						{
							return doc;
						}
					}
					return NO_MORE_DOCS;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public TestReader(IndexReader reader) : base(SlowCompositeReaderWrapper.Wrap(reader
				))
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Fields Fields()
			{
				return new TestFilterAtomicReader.TestReader.TestFields(base.Fields());
			}
		}

		/// <summary>Tests the IndexReader.getFieldNames implementation</summary>
		/// <exception cref="System.Exception">on error</exception>
		public virtual void TestFilterIndexReader()
		{
			Directory directory = NewDirectory();
			IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document(
				);
			d1.Add(NewTextField("default", "one two", Field.Store.YES));
			writer.AddDocument(d1);
			Lucene.Net.Documents.Document d2 = new Lucene.Net.Documents.Document(
				);
			d2.Add(NewTextField("default", "one three", Field.Store.YES));
			writer.AddDocument(d2);
			Lucene.Net.Documents.Document d3 = new Lucene.Net.Documents.Document(
				);
			d3.Add(NewTextField("default", "two four", Field.Store.YES));
			writer.AddDocument(d3);
			writer.Dispose();
			Directory target = NewDirectory();
			// We mess with the postings so this can fail:
			((BaseDirectoryWrapper)target).SetCrossCheckTermVectorsOnClose(false);
			writer = new IndexWriter(target, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			IndexReader reader = new TestFilterAtomicReader.TestReader(DirectoryReader.Open(directory
				));
			writer.AddIndexes(reader);
			writer.Dispose();
			reader.Dispose();
			reader = DirectoryReader.Open(target);
			TermsEnum terms = MultiFields.GetTerms(reader, "default").Iterator(null);
			while (terms.Next() != null)
			{
				IsTrue(terms.Term().Utf8ToString().IndexOf('e') != -1);
			}
			AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(new BytesRef
				("one")));
			DocsAndPositionsEnum positions = terms.DocsAndPositions(MultiFields.GetLiveDocs(reader
				), null);
			while (positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				IsTrue((positions.DocID % 2) == 1);
			}
			reader.Dispose();
			directory.Dispose();
			target.Dispose();
		}

		/// <exception cref="Sharpen.NoSuchMethodException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		private static void CheckOverrideMethods<_T0>(Type<_T0> clazz)
		{
			Type superClazz = clazz.BaseType;
			foreach (MethodInfo m in superClazz.GetMethods())
			{
				int mods = m.GetModifiers();
				if (Modifier.IsStatic(mods) || Modifier.IsAbstract(mods) || Modifier.IsFinal(mods
					) || m.IsSynthetic() || m.Name.Equals("attributes"))
				{
					continue;
				}
				// The point of these checks is to ensure that methods that have a default
				// impl through other methods are not overridden. This makes the number of
				// methods to override to have a working impl minimal and prevents from some
				// traps: for example, think about having getCoreCacheKey delegate to the
				// filtered impl by default
				MethodInfo subM = clazz.GetMethod(m.Name, Sharpen.Runtime.GetParameterTypes(m));
				if (subM.DeclaringType == clazz && m.DeclaringType != typeof(object) && m.DeclaringType
					 != subM.DeclaringType)
				{
					Fail(clazz + " overrides " + m + " although it has a default impl"
						);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOverrideMethods()
		{
			CheckOverrideMethods(typeof(FilterAtomicReader));
			CheckOverrideMethods(typeof(FilterAtomicReader.FilterFields));
			CheckOverrideMethods(typeof(FilterAtomicReader.FilterTerms));
			CheckOverrideMethods(typeof(FilterAtomicReader.FilterTermsEnum));
			CheckOverrideMethods(typeof(FilterAtomicReader.FilterDocsEnum));
			CheckOverrideMethods(typeof(FilterAtomicReader.FilterDocsAndPositionsEnum));
		}
	}
}
