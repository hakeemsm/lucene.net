using System.Collections.Generic;
using Lucene.Net.Grouping;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Grouping.Term
{
	/// <summary>
	/// A base implementation of
	/// <see cref="AbstractAllGroupHeadsCollector{GH}">Lucene.Net.Search.Grouping.AbstractAllGroupHeadsCollector&lt;GH&gt;
	/// 	</see>
	/// for retrieving the most relevant groups when grouping
	/// on a string based group field. More specifically this all concrete implementations of this base implementation
	/// use
	/// <see cref="Lucene.Net.Index.SortedDocValues">Lucene.Net.Index.SortedDocValues
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class TermAllGroupHeadsCollector<GH> : AbstractAllGroupHeadsCollector
		<GH> where GH:GroupHead<IMutableValue>
	{
		private const int DEFAULT_INITIAL_SIZE = 128;

		internal readonly string groupField;

		internal readonly BytesRef scratchBytesRef = new BytesRef();

		internal SortedDocValues groupIndex;

		internal AtomicReaderContext readerContext;

		protected internal TermAllGroupHeadsCollector(string groupField, int numberOfSorts
			) : base(numberOfSorts)
		{
			this.groupField = groupField;
		}

		/// <summary>Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// 	</summary>
		/// <remarks>
		/// Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// This factory method decides with implementation is best suited.
		/// Delegates to
		/// <see cref="TermAllGroupHeadsCollector{GH}.Create(string, Lucene.Net.Search.Sort, int)
		/// 	">TermAllGroupHeadsCollector&lt;GH&gt;.Create(string, Lucene.Net.Search.Sort, int)
		/// 	</see>
		/// with an initialSize of 128.
		/// </remarks>
		/// <param name="groupField">The field to group by</param>
		/// <param name="sortWithinGroup">The sort within each group</param>
		/// <returns>an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments
		/// 	</returns>
		public static AbstractAllGroupHeadsCollector<GroupHead<IMutableValue>> Create(string groupField, Sort
			 sortWithinGroup)
		{
			return Create(groupField, sortWithinGroup, DEFAULT_INITIAL_SIZE);
		}

		/// <summary>Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// 	</summary>
		/// <remarks>
		/// Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// This factory method decides with implementation is best suited.
		/// </remarks>
		/// <param name="groupField">The field to group by</param>
		/// <param name="sortWithinGroup">The sort within each group</param>
		/// <param name="initialSize">
		/// The initial allocation size of the internal int set and group list which should roughly match
		/// the total number of expected unique groups. Be aware that the heap usage is
		/// 4 bytes * initialSize.
		/// </param>
		/// <returns>an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments
		/// 	</returns>
		public static AbstractAllGroupHeadsCollector<GroupHead<IMutableValue>> Create(string groupField, Sort
			 sortWithinGroup, int initialSize)
		{
			bool sortAllScore = true;
			bool sortAllFieldValue = true;
			foreach (SortField sortField in sortWithinGroup.GetSort())
			{
				if (sortField.Type == SortField.Type_e.SCORE)
				{
					sortAllFieldValue = false;
				}
				else
				{
					if (NeedGeneralImpl(sortField))
					{
						return new GeneralAllGroupHeadsCollector(groupField, sortWithinGroup);
					}
					else
					{
						sortAllScore = false;
					}
				}
			}
			if (sortAllScore)
			{
				return new ScoreAllGroupHeadsCollector(groupField, sortWithinGroup, initialSize);
			}
		    if (sortAllFieldValue)
		    {
		        return new OrdAllGroupHeadsCollector(groupField, sortWithinGroup, initialSize);
		    }
		    return new OrdScoreAllGroupHeadsCollector(groupField, sortWithinGroup, initialSize);
		}

		// Returns when a sort field needs the general impl.
		private static bool NeedGeneralImpl(SortField sortField)
		{
			SortField.Type_e sortType = sortField.Type;
			// Note (MvG): We can also make an optimized impl when sorting is SortField.DOC
			return sortType != SortField.Type_e.STRING_VAL && sortType != SortField.Type_e.STRING
				 && sortType != SortField.Type_e.SCORE;
		}

		internal class GeneralAllGroupHeadsCollector : TermAllGroupHeadsCollector<GeneralAllGroupHeadsCollector.GroupHead>
		{
			private readonly Sort sortWithinGroup;

			private readonly IDictionary<BytesRef, GroupHead> groups;

			private Scorer scorer;

			internal GeneralAllGroupHeadsCollector(string groupField, Sort sortWithinGroup) : 
				base(groupField, sortWithinGroup.GetSort().Length)
			{
				// A general impl that works for any group sort.
				this.sortWithinGroup = sortWithinGroup;
				groups = new Dictionary<BytesRef, GroupHead
					>();
				SortField[] sortFields = sortWithinGroup.GetSort();
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].Reverse ? -1 : 1;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int ord = groupIndex.GetOrd(doc);
				BytesRef groupValue;
				if (ord == -1)
				{
					groupValue = null;
				}
				else
				{
					groupIndex.LookupOrd(ord, scratchBytesRef);
					groupValue = scratchBytesRef;
				}
				GroupHead groupHead = groups[groupValue];
				if (groupHead == null)
				{
					groupHead = new GroupHead(this, groupValue, sortWithinGroup, doc);
					groups[groupValue == null ? null : BytesRef.DeepCopyOf(groupValue)] = groupHead;
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
				}
				temporalResult.groupHead = groupHead;
			}

			protected internal override ICollection<GroupHead> GetCollectedGroupHeads()
			{
				return groups.Values;
			}

			/// <exception cref="System.IO.IOException"></exception>
            public override AtomicReaderContext NextReader
			{
			    set
			    {
			        this.readerContext = value;
			        groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader) value.Reader), groupField
			            );
			        foreach (var groupHead in groups.Values)
			        {
			            for (int i = 0; i < groupHead.comparators.Length; i++)
			            {
			                groupHead.comparators[i] = groupHead.comparators[i].SetNextReader(value);
			            }
			        }
			    }
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Scorer Scorer
			{
			    set
			    {
			        this.scorer = value;
			        foreach (var groupHead in groups.Values)
			        {
			            foreach (FieldComparator<object> comparator in groupHead.comparators)
			            {
			                comparator.Scorer = (value);
			            }
			        }
			    }
			}

			internal class GroupHead : GroupHead<BytesRef>
			{
				internal readonly FieldComparator<object>[] comparators;

				/// <exception cref="System.IO.IOException"></exception>
				private GroupHead(GeneralAllGroupHeadsCollector _enclosing, BytesRef groupValue, 
					Sort sort, int doc) : base(groupValue, doc + _enclosing.readerContext.DocBase
					)
				{
					this._enclosing = _enclosing;
					SortField[] sortFields = sort.GetSort();
					this.comparators = new FieldComparator<object>[sortFields.Length];
					for (int i = 0; i < sortFields.Length; i++)
					{
						this.comparators[i] = sortFields[i].GetComparator(1, i).SetNextReader(this._enclosing
							.readerContext);
						this.comparators[i].Scorer = (this._enclosing.scorer);
						this.comparators[i].Copy(0, doc);
						this.comparators[i].Bottom = (0);
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
					return this.comparators[compIDX].CompareBottom(doc);
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					foreach (FieldComparator<object> comparator in this.comparators)
					{
						comparator.Copy(0, doc);
						comparator.Bottom=(0);
					}
					this.doc = doc + this._enclosing.readerContext.DocBase;
				}

				private readonly GeneralAllGroupHeadsCollector _enclosing;
			}
		}

		internal class OrdScoreAllGroupHeadsCollector : TermAllGroupHeadsCollector<OrdScoreAllGroupHeadsCollector.GroupHead>
		{
			private readonly SentinelIntSet ordSet;

			private readonly IList<GroupHead> collectedGroups;

			private readonly SortField[] fields;

			private SortedDocValues[] sortsIndex;

			private Scorer scorer;

			private GroupHead[] segmentGroupHeads;

			internal OrdScoreAllGroupHeadsCollector(string groupField, Sort sortWithinGroup, 
				int initialSize) : base(groupField, sortWithinGroup.GetSort().Length)
			{
				// AbstractAllGroupHeadsCollector optimized for ord fields and scores.
				ordSet = new SentinelIntSet(initialSize, -2);
				collectedGroups = new List<GroupHead>(initialSize);
				SortField[] sortFields = sortWithinGroup.GetSort();
				fields = new SortField[sortFields.Length];
				sortsIndex = new SortedDocValues[sortFields.Length];
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].Reverse ? -1 : 1;
					fields[i] = sortFields[i];
				}
			}

			protected internal override ICollection<GroupHead> GetCollectedGroupHeads()
			{
				return collectedGroups;
			}

		    public override Scorer Scorer
		    {
		        set { scorer = value; }
		    }

		    

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int key = groupIndex.GetOrd(doc);
				GroupHead groupHead;
				if (!ordSet.Exists(key))
				{
					ordSet.Put(key);
					BytesRef term;
					if (key == -1)
					{
						term = null;
					}
					else
					{
						term = new BytesRef();
						groupIndex.LookupOrd(key, term);
					}
					groupHead = new GroupHead(this, doc, term);
					collectedGroups.AddItem(groupHead);
					segmentGroupHeads[key + 1] = groupHead;
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
					groupHead = segmentGroupHeads[key + 1];
				}
				temporalResult.groupHead = groupHead;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				this.readerContext = context;
				groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader), groupField
					);
				for (int i = 0; i < fields.Length; i++)
				{
					if (fields[i].Type == SortField.Type_e.SCORE)
					{
						continue;
					}
					sortsIndex[i] = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader)
						, fields[i].Field);
				}
				// Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
				ordSet.Clear();
				segmentGroupHeads = new GroupHead[groupIndex.ValueCount + 1];
				foreach (GroupHead collectedGroup
					 in collectedGroups)
				{
					int ord;
					if (collectedGroup.groupValue == null)
					{
						ord = -1;
					}
					else
					{
						ord = groupIndex.LookupTerm(collectedGroup.groupValue);
					}
					if (collectedGroup.groupValue == null || ord >= 0)
					{
						ordSet.Put(ord);
						segmentGroupHeads[ord + 1] = collectedGroup;
						for (int i_1 = 0; i_1 < sortsIndex.Length; i_1++)
						{
							if (fields[i_1].Type == SortField.Type_e.SCORE)
							{
								continue;
							}
							int sortOrd;
							if (collectedGroup.sortValues[i_1] == null)
							{
								sortOrd = -1;
							}
							else
							{
								sortOrd = sortsIndex[i_1].LookupTerm(collectedGroup.sortValues[i_1]);
							}
							collectedGroup.sortOrds[i_1] = sortOrd;
						}
					}
				}
			}

			internal class GroupHead : GroupHead<BytesRef>
			{
				internal BytesRef[] sortValues;

				internal int[] sortOrds;

				internal float[] scores;

				/// <exception cref="System.IO.IOException"></exception>
				private GroupHead(OrdScoreAllGroupHeadsCollector _enclosing, int doc, BytesRef groupValue
					) : base(groupValue, doc + _enclosing.readerContext.DocBase)
				{
					this._enclosing = _enclosing;
					this.sortValues = new BytesRef[this._enclosing.sortsIndex.Length];
					this.sortOrds = new int[this._enclosing.sortsIndex.Length];
					this.scores = new float[this._enclosing.sortsIndex.Length];
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						if (this._enclosing.fields[i].Type == SortField.Type_e.SCORE)
						{
							this.scores[i] = this._enclosing.scorer.Score();
						}
						else
						{
							this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
							this.sortValues[i] = new BytesRef();
							if (this.sortOrds[i] != -1)
							{
								this._enclosing.sortsIndex[i].Get(doc, this.sortValues[i]);
							}
						}
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
				    if (this._enclosing.fields[compIDX].Type == SortField.Type_e.SCORE)
					{
						float score = this._enclosing.scorer.Score();
						if (this.scores[compIDX] < score)
						{
							return 1;
						}
					    if (this.scores[compIDX] > score)
					    {
					        return -1;
					    }
					    return 0;
					}
				    if (this.sortOrds[compIDX] < 0)
				    {
				        // The current segment doesn't contain the sort value we encountered before. Therefore the ord is negative.
				        if (this._enclosing.sortsIndex[compIDX].GetOrd(doc) == -1)
				        {
				            this._enclosing.scratchBytesRef.Length = 0;
				        }
				        else
				        {
				            this._enclosing.sortsIndex[compIDX].Get(doc, this._enclosing.scratchBytesRef);
				        }
				        return this.sortValues[compIDX].CompareTo(this._enclosing.scratchBytesRef);
				    }
				    else
				    {
				        return this.sortOrds[compIDX] - this._enclosing.sortsIndex[compIDX].GetOrd(doc);
				    }
				}

			    /// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						if (this._enclosing.fields[i].Type == SortField.Type_e.SCORE)
						{
							this.scores[i] = this._enclosing.scorer.Score();
						}
						else
						{
							this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
							if (this.sortOrds[i] == -1)
							{
								this.sortValues[i].Length = 0;
							}
							else
							{
								this._enclosing.sortsIndex[i].Get(doc, this.sortValues[i]);
							}
						}
					}
					this.doc = doc + this._enclosing.readerContext.DocBase;
				}

				private readonly OrdScoreAllGroupHeadsCollector _enclosing;
			}
		}

		internal class OrdAllGroupHeadsCollector : TermAllGroupHeadsCollector<OrdAllGroupHeadsCollector.GroupHead>
		{
			private readonly SentinelIntSet ordSet;

			private readonly IList<GroupHead> collectedGroups;

			private readonly SortField[] fields;

			private SortedDocValues[] sortsIndex;

			private GroupHead[] segmentGroupHeads;

			internal OrdAllGroupHeadsCollector(string groupField, Sort sortWithinGroup, int initialSize
				) : base(groupField, sortWithinGroup.GetSort().Length)
			{
				// AbstractAllGroupHeadsCollector optimized for ord fields.
				ordSet = new SentinelIntSet(initialSize, -2);
				collectedGroups = new List<GroupHead>(initialSize);
				SortField[] sortFields = sortWithinGroup.GetSort();
				fields = new SortField[sortFields.Length];
				sortsIndex = new SortedDocValues[sortFields.Length];
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].Reverse ? -1 : 1;
					fields[i] = sortFields[i];
				}
			}

			protected internal override ICollection<GroupHead> GetCollectedGroupHeads()
			{
				return collectedGroups;
			}

		    public override Scorer Scorer
		    {
		        set {  }
		    }

		   

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int key = groupIndex.GetOrd(doc);
				GroupHead groupHead;
				if (!ordSet.Exists(key))
				{
					ordSet.Put(key);
					BytesRef term;
					if (key == -1)
					{
						term = null;
					}
					else
					{
						term = new BytesRef();
						groupIndex.LookupOrd(key, term);
					}
					groupHead = new GroupHead(this, doc, term);
					collectedGroups.Add(groupHead);
					segmentGroupHeads[key + 1] = groupHead;
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
					groupHead = segmentGroupHeads[key + 1];
				}
				temporalResult.groupHead = groupHead;
			}

		    public override AtomicReaderContext NextReader
		    {
		        set
		        {
                    this.readerContext = value;
                    groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)value.Reader), groupField
                        );
                    for (int i = 0; i < fields.Length; i++)
                    {
                        sortsIndex[i] = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)value.Reader)
                            , fields[i].Field);
                    }
                    // Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
                    ordSet.Clear();
                    segmentGroupHeads = new GroupHead[groupIndex.ValueCount + 1];
                    foreach (GroupHead collectedGroup in collectedGroups)
                    {
                        int groupOrd;
                        if (collectedGroup.groupValue == null)
                        {
                            groupOrd = -1;
                        }
                        else
                        {
                            groupOrd = groupIndex.LookupTerm(collectedGroup.groupValue);
                        }
                        if (collectedGroup.groupValue == null || groupOrd >= 0)
                        {
                            ordSet.Put(groupOrd);
                            segmentGroupHeads[groupOrd + 1] = collectedGroup;
                            for (int i_1 = 0; i_1 < sortsIndex.Length; i_1++)
                            {
                                int sortOrd;
                                if (collectedGroup.sortOrds[i_1] == -1)
                                {
                                    sortOrd = -1;
                                }
                                else
                                {
                                    sortOrd = sortsIndex[i_1].LookupTerm(collectedGroup.sortValues[i_1]);
                                }
                                collectedGroup.sortOrds[i_1] = sortOrd;
                            }
                        }
                    }
		        }
		    }

		   

			internal class GroupHead : GroupHead<BytesRef>
			{
				internal BytesRef[] sortValues;

				internal int[] sortOrds;

				private GroupHead(OrdAllGroupHeadsCollector _enclosing, int doc, BytesRef groupValue
					) : base(groupValue, doc + _enclosing.readerContext.DocBase)
				{
					this._enclosing = _enclosing;
					this.sortValues = new BytesRef[this._enclosing.sortsIndex.Length];
					this.sortOrds = new int[this._enclosing.sortsIndex.Length];
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
						this.sortValues[i] = new BytesRef();
						if (this.sortOrds[i] != -1)
						{
							this._enclosing.sortsIndex[i].Get(doc, this.sortValues[i]);
						}
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
					if (this.sortOrds[compIDX] < 0)
					{
						// The current segment doesn't contain the sort value we encountered before. Therefore the ord is negative.
						if (this._enclosing.sortsIndex[compIDX].GetOrd(doc) == -1)
						{
							this._enclosing.scratchBytesRef.Length = 0;
						}
						else
						{
							this._enclosing.sortsIndex[compIDX].Get(doc, this._enclosing.scratchBytesRef);
						}
						return this.sortValues[compIDX].CompareTo(this._enclosing.scratchBytesRef);
					}
					else
					{
						return this.sortOrds[compIDX] - this._enclosing.sortsIndex[compIDX].GetOrd(doc);
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
						if (this.sortOrds[i] == -1)
						{
							this.sortValues[i].Length = 0;
						}
						else
						{
							this._enclosing.sortsIndex[i].LookupOrd(this.sortOrds[i], this.sortValues[i]);
						}
					}
					this.doc = doc + this._enclosing.readerContext.DocBase;
				}

				private readonly OrdAllGroupHeadsCollector _enclosing;
			}
		}

		internal class ScoreAllGroupHeadsCollector : TermAllGroupHeadsCollector<ScoreAllGroupHeadsCollector.GroupHead>
		{
			private readonly SentinelIntSet ordSet;

			private readonly IList<GroupHead> collectedGroups;

			private readonly SortField[] fields;

			private Scorer scorer;

			private GroupHead[] segmentGroupHeads;

			internal ScoreAllGroupHeadsCollector(string groupField, Sort sortWithinGroup, int
				 initialSize) : base(groupField, sortWithinGroup.GetSort().Length)
			{
				// AbstractAllGroupHeadsCollector optimized for scores.
				ordSet = new SentinelIntSet(initialSize, -2);
				collectedGroups = new List<GroupHead>(initialSize);
				SortField[] sortFields = sortWithinGroup.GetSort();
				fields = new SortField[sortFields.Length];
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].Reverse ? -1 : 1;
					fields[i] = sortFields[i];
				}
			}

			protected internal override ICollection<GroupHead> GetCollectedGroupHeads()
			{
				return collectedGroups;
			}


		    public override Scorer Scorer
		    {
		        set { this.scorer = value; }
		    }

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int key = groupIndex.GetOrd(doc);
				GroupHead groupHead;
				if (!ordSet.Exists(key))
				{
					ordSet.Put(key);
					BytesRef term;
					if (key == -1)
					{
						term = null;
					}
					else
					{
						term = new BytesRef();
						groupIndex.LookupOrd(key, term);
					}
					groupHead = new GroupHead(this, doc, term);
					collectedGroups.Add(groupHead);
					segmentGroupHeads[key + 1] = groupHead;
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
					groupHead = segmentGroupHeads[key + 1];
				}
				temporalResult.groupHead = groupHead;
			}

			/// <exception cref="System.IO.IOException"></exception>
            public override AtomicReaderContext NextReader
			{
			    set
			    {
			        this.readerContext = value;
			        groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader) value.Reader), groupField);
			        // Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
			        ordSet.Clear();
			        segmentGroupHeads = new GroupHead
			            [groupIndex.ValueCount + 1];
			        foreach (GroupHead collectedGroup in collectedGroups)
			        {
			            int ord;
			            if (collectedGroup.groupValue == null)
			            {
			                ord = -1;
			            }
			            else
			            {
			                ord = groupIndex.LookupTerm(collectedGroup.groupValue);
			            }
			            if (collectedGroup.groupValue == null || ord >= 0)
			            {
			                ordSet.Put(ord);
			                segmentGroupHeads[ord + 1] = collectedGroup;
			            }
			        }
			    }
			}

			internal class GroupHead : GroupHead<BytesRef>
			{
				internal float[] scores;

				/// <exception cref="System.IO.IOException"></exception>
				private GroupHead(ScoreAllGroupHeadsCollector _enclosing, int doc, BytesRef groupValue
					) : base(groupValue, doc + _enclosing.readerContext.DocBase)
				{
					this._enclosing = _enclosing;
					this.scores = new float[this._enclosing.fields.Length];
					float score = this._enclosing.scorer.Score();
					for (int i = 0; i < this.scores.Length; i++)
					{
						this.scores[i] = score;
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
					float score = this._enclosing.scorer.Score();
					if (this.scores[compIDX] < score)
					{
						return 1;
					}
				    if (this.scores[compIDX] > score)
				    {
				        return -1;
				    }
				    return 0;
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					float score = this._enclosing.scorer.Score();
					for (int i = 0; i < this.scores.Length; i++)
					{
						this.scores[i] = score;
					}
					this.doc = doc + this._enclosing.readerContext.DocBase;
				}

				private readonly ScoreAllGroupHeadsCollector _enclosing;
			}
		}
	}
}
