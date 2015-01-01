using System;
using System.Linq;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
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
					return new TestTerms(base.Terms(field));
				}
			}

			private class TestTerms : FilterTerms
			{
				public TestTerms(Terms @in) : base(@in)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum Iterator(TermsEnum reuse)
				{
					return new TestTermsEnum(base.Iterator(reuse));
				}
			}

			private class TestTermsEnum : FilterTermsEnum
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
					while ((text = instance.Next()) != null)
					{
						if (text.Utf8ToString().IndexOf('e') != -1)
						{
							return text;
						}
					}
					return null;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					return new TestPositions(base.DocsAndPositions(
						liveDocs, reuse, flags));
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
					while ((doc = instance.NextDoc()) != NO_MORE_DOCS)
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
			public override Fields Fields
			{
			    get { return new TestFilterAtomicReader.TestReader.TestFields(base.Fields); }
			}
		}

		/// <summary>Tests the IndexReader.getFieldNames implementation</summary>
		[Test]
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
				IsTrue(terms.Term.Utf8ToString().IndexOf('e') != -1);
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

		/// <exception cref="NoSuchMethodException"></exception>
		/// <exception cref="System.Security.SecurityException"></exception>
		private static void CheckOverrideMethods(Type clazz)
		{
			Type superClazz = clazz.BaseType;
			foreach (MethodInfo m in superClazz.GetMethods().Where(m=>!(m.IsAbstract || m.IsStatic || m.IsFinal || m.Name.Equals("attributes",StringComparison.CurrentCultureIgnoreCase))))
			{
                
				
				// The point of these checks is to ensure that methods that have a default
				// impl through other methods are not overridden. This makes the number of
				// methods to override to have a working impl minimal and prevents from some
				// traps: for example, think about having getCoreCacheKey delegate to the
				// filtered impl by default
			    MethodInfo subM = clazz.GetMethod(m.Name);
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
