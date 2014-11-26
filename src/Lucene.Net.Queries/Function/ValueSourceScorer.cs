/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queries.Function
{
	/// <summary>
	/// <see cref="Lucene.Net.Search.Scorer">Lucene.Net.Search.Scorer</see>
	/// which returns the result of
	/// <see cref="FunctionValues.FloatVal(int)">FunctionValues.FloatVal(int)</see>
	/// as
	/// the score for a document.
	/// </summary>
	public class ValueSourceScorer : Scorer
	{
		protected internal readonly IndexReader reader;

		private int doc = -1;

		protected internal readonly int maxDoc;

		protected internal readonly FunctionValues values;

		protected internal bool checkDeletes;

		private readonly Bits liveDocs;

		protected internal ValueSourceScorer(IndexReader reader, FunctionValues values) : 
			base(null)
		{
			this.reader = reader;
			this.maxDoc = reader.MaxDoc();
			this.values = values;
			SetCheckDeletes(true);
			this.liveDocs = MultiFields.GetLiveDocs(reader);
		}

		public virtual IndexReader GetReader()
		{
			return reader;
		}

		public virtual void SetCheckDeletes(bool checkDeletes)
		{
			this.checkDeletes = checkDeletes && reader.HasDeletions();
		}

		public virtual bool Matches(int doc)
		{
			return (!checkDeletes || liveDocs.Get(doc)) && MatchesValue(doc);
		}

		public virtual bool MatchesValue(int doc)
		{
			return true;
		}

		public override int DocID()
		{
			return doc;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int NextDoc()
		{
			for (; ; )
			{
				doc++;
				if (doc >= maxDoc)
				{
					return doc = NO_MORE_DOCS;
				}
				if (Matches(doc))
				{
					return doc;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Advance(int target)
		{
			// also works fine when target==NO_MORE_DOCS
			doc = target - 1;
			return NextDoc();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override float Score()
		{
			return values.FloatVal(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Freq()
		{
			return 1;
		}

		public override long Cost()
		{
			return maxDoc;
		}
	}
}
