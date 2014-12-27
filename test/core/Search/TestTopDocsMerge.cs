/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestTopDocsMerge : LuceneTestCase
	{
		private class ShardSearcher : IndexSearcher
		{
			private readonly IList<AtomicReaderContext> ctx;

			public ShardSearcher(AtomicReaderContext ctx, IndexReaderContext parent) : base(parent
				)
			{
				this.ctx = Sharpen.Collections.SingletonList(ctx);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Search(Weight weight, Collector collector)
			{
				Search(ctx, weight, collector);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual TopDocs Search(Weight weight, int topN)
			{
				return Search(ctx, weight, null, topN);
			}

			public override string ToString()
			{
				return "ShardSearcher(" + ctx[0] + ")";
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSort_1()
		{
			TestSort(false);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSort_2()
		{
			TestSort(true);
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void TestSort(bool useFrom)
		{
			IndexReader reader = null;
			Directory dir = null;
			int numDocs = AtLeast(1000);
			//final int numDocs = atLeast(50);
			string[] tokens = new string[] { "a", "b", "c", "d", "e" };
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: make index");
			}
			{
				dir = NewDirectory();
				RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
				// w.setDoRandomForceMerge(false);
				// w.w.getConfig().setMaxBufferedDocs(atLeast(100));
				string[] content = new string[AtLeast(20)];
				for (int contentIDX = 0; contentIDX < content.Length; contentIDX++)
				{
					StringBuilder sb = new StringBuilder();
					int numTokens = TestUtil.NextInt(Random(), 1, 10);
					for (int tokenIDX = 0; tokenIDX < numTokens; tokenIDX++)
					{
						sb.Append(tokens[Random().Next(tokens.Length)]).Append(' ');
					}
					content[contentIDX] = sb.ToString();
				}
				for (int docIDX = 0; docIDX < numDocs; docIDX++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(NewStringField("string", TestUtil.RandomRealisticUnicodeString(Random()), 
						Field.Store.NO));
					doc.Add(NewTextField("text", content[Random().Next(content.Length)], Field.Store.
						NO));
					doc.Add(new FloatField("float", Random().NextFloat(), Field.Store.NO));
					int intValue;
					if (Random().Next(100) == 17)
					{
						intValue = int.MinValue;
					}
					else
					{
						if (Random().Next(100) == 17)
						{
							intValue = int.MaxValue;
						}
						else
						{
							intValue = Random().Next();
						}
					}
					doc.Add(new IntField("int", intValue, Field.Store.NO));
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  doc=" + doc);
					}
					w.AddDocument(doc);
				}
				reader = w.GetReader();
				w.Dispose();
			}
			// NOTE: sometimes reader has just one segment, which is
			// important to test
			IndexSearcher searcher = NewSearcher(reader);
			IndexReaderContext ctx = searcher.GetTopReaderContext();
			TestTopDocsMerge.ShardSearcher[] subSearchers;
			int[] docStarts;
			if (ctx is AtomicReaderContext)
			{
				subSearchers = new TestTopDocsMerge.ShardSearcher[1];
				docStarts = new int[1];
				subSearchers[0] = new TestTopDocsMerge.ShardSearcher((AtomicReaderContext)ctx, ctx
					);
				docStarts[0] = 0;
			}
			else
			{
				CompositeReaderContext compCTX = (CompositeReaderContext)ctx;
				int size = compCTX.Leaves.Count;
				subSearchers = new TestTopDocsMerge.ShardSearcher[size];
				docStarts = new int[size];
				int docBase = 0;
				for (int searcherIDX = 0; searcherIDX < subSearchers.Length; searcherIDX++)
				{
					AtomicReaderContext leave = compCTX.Leaves[searcherIDX];
					subSearchers[searcherIDX] = new TestTopDocsMerge.ShardSearcher(leave, compCTX);
					docStarts[searcherIDX] = docBase;
					docBase += ((AtomicReader)leave.Reader).MaxDoc;
				}
			}
			IList<SortField> sortFields = new AList<SortField>();
			sortFields.AddItem(new SortField("string", SortField.Type.STRING, true));
			sortFields.AddItem(new SortField("string", SortField.Type.STRING, false));
			sortFields.AddItem(new SortField("int", SortField.Type.INT, true));
			sortFields.AddItem(new SortField("int", SortField.Type.INT, false));
			sortFields.AddItem(new SortField("float", SortField.Type.FLOAT, true));
			sortFields.AddItem(new SortField("float", SortField.Type.FLOAT, false));
			sortFields.AddItem(new SortField(null, SortField.Type.SCORE, true));
			sortFields.AddItem(new SortField(null, SortField.Type.SCORE, false));
			sortFields.AddItem(new SortField(null, SortField.Type.DOC, true));
			sortFields.AddItem(new SortField(null, SortField.Type.DOC, false));
			for (int iter = 0; iter < 1000 * RANDOM_MULTIPLIER; iter++)
			{
				// TODO: custom FieldComp...
				Query query = new TermQuery(new Term("text", tokens[Random().Next(tokens.Length)]
					));
				Sort sort;
				if (Random().Next(10) == 4)
				{
					// Sort by score
					sort = null;
				}
				else
				{
					SortField[] randomSortFields = new SortField[TestUtil.NextInt(Random(), 1, 3)];
					for (int sortIDX = 0; sortIDX < randomSortFields.Length; sortIDX++)
					{
						randomSortFields[sortIDX] = sortFields[Random().Next(sortFields.Count)];
					}
					sort = new Sort(randomSortFields);
				}
				int numHits = TestUtil.NextInt(Random(), 1, numDocs + 5);
				//final int numHits = 5;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: search query=" + query + " sort=" + sort + " numHits="
						 + numHits);
				}
				int from = -1;
				int size = -1;
				// First search on whole index:
				TopDocs topHits;
				if (sort == null)
				{
					if (useFrom)
					{
						TopScoreDocCollector c = TopScoreDocCollector.Create(numHits, Random().NextBoolean
							());
						searcher.Search(query, c);
						from = TestUtil.NextInt(Random(), 0, numHits - 1);
						size = numHits - from;
						TopDocs tempTopHits = c.TopDocs();
						if (from < tempTopHits.ScoreDocs.Length)
						{
							// Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
							// than TopDocs#merge currently has
							ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.ScoreDocs.Length
								 - from)];
							System.Array.Copy(tempTopHits.ScoreDocs, from, newScoreDocs, 0, newScoreDocs.Length
								);
							tempTopHits.ScoreDocs = newScoreDocs;
							topHits = tempTopHits;
						}
						else
						{
							topHits = new TopDocs(tempTopHits.TotalHits, new ScoreDoc[0], tempTopHits.GetMaxScore
								());
						}
					}
					else
					{
						topHits = searcher.Search(query, numHits);
					}
				}
				else
				{
					TopFieldCollector c = TopFieldCollector.Create(sort, numHits, true, true, true, Random
						().NextBoolean());
					searcher.Search(query, c);
					if (useFrom)
					{
						from = TestUtil.NextInt(Random(), 0, numHits - 1);
						size = numHits - from;
						TopDocs tempTopHits = c.TopDocs();
						if (from < tempTopHits.ScoreDocs.Length)
						{
							// Can't use TopDocs#topDocs(start, howMany), since it has different behaviour when start >= hitCount
							// than TopDocs#merge currently has
							ScoreDoc[] newScoreDocs = new ScoreDoc[Math.Min(size, tempTopHits.ScoreDocs.Length
								 - from)];
							System.Array.Copy(tempTopHits.ScoreDocs, from, newScoreDocs, 0, newScoreDocs.Length
								);
							tempTopHits.ScoreDocs = newScoreDocs;
							topHits = tempTopHits;
						}
						else
						{
							topHits = new TopDocs(tempTopHits.TotalHits, new ScoreDoc[0], tempTopHits.GetMaxScore
								());
						}
					}
					else
					{
						topHits = c.TopDocs(0, numHits);
					}
				}
				if (VERBOSE)
				{
					if (useFrom)
					{
						System.Console.Out.WriteLine("from=" + from + " size=" + size);
					}
					System.Console.Out.WriteLine("  top search: " + topHits.TotalHits + " TotalHits; hits="
						 + (topHits.ScoreDocs == null ? "null" : topHits.ScoreDocs.Length + " maxScore="
						 + topHits.GetMaxScore()));
					if (topHits.ScoreDocs != null)
					{
						for (int hitIDX = 0; hitIDX < topHits.ScoreDocs.Length; hitIDX++)
						{
							ScoreDoc sd = topHits.ScoreDocs[hitIDX];
							System.Console.Out.WriteLine("    doc=" + sd.Doc + " score=" + sd.score);
						}
					}
				}
				// ... then all shards:
				Weight w = searcher.CreateNormalizedWeight(query);
				TopDocs[] shardHits = new TopDocs[subSearchers.Length];
				for (int shardIDX = 0; shardIDX < subSearchers.Length; shardIDX++)
				{
					TopDocs subHits;
					TestTopDocsMerge.ShardSearcher subSearcher = subSearchers[shardIDX];
					if (sort == null)
					{
						subHits = subSearcher.Search(w, numHits);
					}
					else
					{
						TopFieldCollector c = TopFieldCollector.Create(sort, numHits, true, true, true, Random
							().NextBoolean());
						subSearcher.Search(w, c);
						subHits = c.TopDocs(0, numHits);
					}
					shardHits[shardIDX] = subHits;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  shard=" + shardIDX + " " + subHits.TotalHits + " TotalHits hits="
							 + (subHits.ScoreDocs == null ? "null" : subHits.ScoreDocs.Length));
						if (subHits.ScoreDocs != null)
						{
							foreach (ScoreDoc sd in subHits.ScoreDocs)
							{
								System.Console.Out.WriteLine("    doc=" + sd.Doc + " score=" + sd.score);
							}
						}
					}
				}
				// Merge:
				TopDocs mergedHits;
				if (useFrom)
				{
					mergedHits = TopDocs.Merge(sort, from, size, shardHits);
				}
				else
				{
					mergedHits = TopDocs.Merge(sort, numHits, shardHits);
				}
				if (mergedHits.ScoreDocs != null)
				{
					// Make sure the returned shards are correct:
					for (int hitIDX = 0; hitIDX < mergedHits.ScoreDocs.Length; hitIDX++)
					{
						ScoreDoc sd = mergedHits.ScoreDocs[hitIDX];
						AreEqual("doc=" + sd.Doc + " wrong shard", ReaderUtil.SubIndex
							(sd.Doc, docStarts), sd.shardIndex);
					}
				}
				TestUtil.AssertEquals(topHits, mergedHits);
			}
			reader.Dispose();
			dir.Dispose();
		}
	}
}
