using System;
using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
	internal class BufferedUpdatesStream
	{
		private readonly IList<FrozenBufferedUpdates> updates = new List<FrozenBufferedUpdates>();

		private long nextGen = 1;

		private Term lastDeleteTerm;

		private readonly InfoStream infoStream;

		private readonly AtomicLong bytesUsed = new AtomicLong();

		private readonly AtomicInteger numTerms = new AtomicInteger();

		public BufferedUpdatesStream(InfoStream infoStream)
		{
			// TODO: maybe linked list?
			// Starts at 1 so that SegmentInfos that have never had
			// deletes applied (whose bufferedDelGen defaults to 0)
			// will be correct:
			// used only by 
			//HM:revisit 
			//assert
			this.infoStream = infoStream;
		}

		// Appends a new packet of buffered deletes to the stream,
		// setting its generation:
		public virtual long Push(FrozenBufferedUpdates packet)
		{
			lock (this)
			{
				packet.SetDelGen(nextGen++);
				//HM:revisit 
				//assert packet.any();
				//HM:revisit 
				//assert checkDeleteStats();
				//HM:revisit 
				//assert packet.delGen() < nextGen;
				//HM:revisit 
				//assert updates.isEmpty() || updates.get(updates.size()-1).delGen() < packet.delGen() : "Delete packets must be in order";
				updates.AddItem(packet);
				numTerms.AddAndGet(packet.numTermDeletes);
				bytesUsed.AddAndGet(packet.bytesUsed);
				if (infoStream.IsEnabled("BD"))
				{
					infoStream.Message("BD", "push deletes " + packet + " delGen=" + packet.DelGen() 
						+ " packetCount=" + updates.Count + " totBytesUsed=" + bytesUsed.Get());
				}
				//HM:revisit 
				//assert checkDeleteStats();
				return packet.DelGen();
			}
		}

		public virtual void Clear()
		{
			lock (this)
			{
				updates.Clear();
				nextGen = 1;
				numTerms.Set(0);
				bytesUsed.Set(0);
			}
		}

		public virtual bool Any()
		{
			return bytesUsed.Get() != 0;
		}

		public virtual int NumTerms
		{
		    get { return numTerms.Get(); }
		}

		public virtual long BytesUsed
		{
		    get { return bytesUsed.Get(); }
		}

		public class ApplyDeletesResult
		{
			public readonly bool anyDeletes;

			public readonly long gen;

			public readonly IList<SegmentCommitInfo> allDeleted;

			internal ApplyDeletesResult(bool anyDeletes, long gen, IList<SegmentCommitInfo> allDeleted
				)
			{
				// True if any actual deletes took place:
				// Current gen, for the merged segment:
				// If non-null, contains segments that are 100% deleted
				this.anyDeletes = anyDeletes;
				this.gen = gen;
				this.allDeleted = allDeleted;
			}
		}

		private sealed class _IComparer_148 : IComparer<SegmentCommitInfo>
		{
			public _IComparer_148()
			{
			}

			// Sorts SegmentInfos from smallest to biggest bufferedDelGen:
			public int Compare(SegmentCommitInfo si1, SegmentCommitInfo si2)
			{
				long cmp = si1.GetBufferedDeletesGen() - si2.GetBufferedDeletesGen();
				if (cmp > 0)
				{
					return 1;
				}
				else
				{
					if (cmp < 0)
					{
						return -1;
					}
					else
					{
						return 0;
					}
				}
			}
		}

		private static readonly IComparer<SegmentCommitInfo> sortSegInfoByDelGen = new _IComparer_148
			();

		/// <summary>
		/// Resolves the buffered deleted Term/Query/docIDs, into
		/// actual deleted docIDs in the liveDocs MutableBits for
		/// each SegmentReader.
		/// </summary>
		/// <remarks>
		/// Resolves the buffered deleted Term/Query/docIDs, into
		/// actual deleted docIDs in the liveDocs MutableBits for
		/// each SegmentReader.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual BufferedUpdatesStream.ApplyDeletesResult ApplyDeletesAndUpdates(IndexWriter.ReaderPool
			 readerPool, IList<SegmentCommitInfo> infos)
		{
			lock (this)
			{
				long t0 = Runtime.CurrentTimeMillis();
				if (infos.Count == 0)
				{
					return new BufferedUpdatesStream.ApplyDeletesResult(false, nextGen++, null);
				}
				//HM:revisit 
				//assert checkDeleteStats();
				if (!Any())
				{
					if (infoStream.IsEnabled("BD"))
					{
						infoStream.Message("BD", "applyDeletes: no deletes; skipping");
					}
					return new BufferedUpdatesStream.ApplyDeletesResult(false, nextGen++, null);
				}
				if (infoStream.IsEnabled("BD"))
				{
					infoStream.Message("BD", "applyDeletes: infos=" + infos + " packetCount=" + updates
						.Count);
				}
				long gen = nextGen++;
				IList<SegmentCommitInfo> infos2 = new AList<SegmentCommitInfo>();
				Sharpen.Collections.AddAll(infos2, infos);
				infos2.Sort(sortSegInfoByDelGen);
				CoalescedUpdates coalescedUpdates = null;
				bool anyNewDeletes = false;
				int infosIDX = infos2.Count - 1;
				int delIDX = updates.Count - 1;
				IList<SegmentCommitInfo> allDeleted = null;
				while (infosIDX >= 0)
				{
					//System.out.println("BD: cycle delIDX=" + delIDX + " infoIDX=" + infosIDX);
					FrozenBufferedUpdates packet = delIDX >= 0 ? updates[delIDX] : null;
					SegmentCommitInfo info = infos2[infosIDX];
					long segGen = info.GetBufferedDeletesGen();
					if (packet != null && segGen < packet.DelGen())
					{
						//        System.out.println("  coalesce");
						if (coalescedUpdates == null)
						{
							coalescedUpdates = new CoalescedUpdates();
						}
						if (!packet.isSegmentPrivate)
						{
							coalescedUpdates.Update(packet);
						}
						delIDX--;
					}
					else
					{
						if (packet != null && segGen == packet.DelGen())
						{
							//HM:revisit 
							//assert packet.isSegmentPrivate : "Packet and Segments deletegen can only match on a segment private del packet gen=" + segGen;
							//System.out.println("  eq");
							// Lock order: IW -> BD -> RP
							//HM:revisit 
							//assert readerPool.infoIsLive(info);
							ReadersAndUpdates rld = readerPool.Get(info, true);
							SegmentReader reader = rld.GetReader(IOContext.READ);
							int delCount = 0;
							bool segAllDeletes;
							try
							{
								DocValuesFieldUpdates.Container dvUpdates = new DocValuesFieldUpdates.Container();
								if (coalescedUpdates != null)
								{
									//System.out.println("    del coalesced");
									delCount += ApplyTermDeletes(coalescedUpdates.TermsIterable(), rld, reader);
									delCount += ApplyQueryDeletes(coalescedUpdates.QueriesIterable(), rld, reader);
									ApplyDocValuesUpdates(coalescedUpdates.numericDVUpdates.AsIterable(), rld, reader
										, dvUpdates);
									ApplyDocValuesUpdates(coalescedUpdates.binaryDVUpdates.AsIterable(), rld, reader, 
										dvUpdates);
								}
								//System.out.println("    del exact");
								// Don't delete by Term here; DocumentsWriterPerThread
								// already did that on flush:
								delCount += ApplyQueryDeletes(packet.QueriesIterable(), rld, reader);
								ApplyDocValuesUpdates(Arrays.AsList(packet.numericDVUpdates).AsIterable(), rld, reader
									, dvUpdates);
								ApplyDocValuesUpdates(Arrays.AsList(packet.binaryDVUpdates).AsIterable(), rld, reader
									, dvUpdates);
								if (dvUpdates.Any())
								{
									rld.WriteFieldUpdates(info.info.dir, dvUpdates);
								}
								int fullDelCount = rld.Info.GetDelCount() + rld.GetPendingDeleteCount();
								//HM:revisit 
								//assert fullDelCount <= rld.info.info.getDocCount();
								segAllDeletes = fullDelCount == rld.Info.info.GetDocCount();
							}
							finally
							{
								rld.Release(reader);
								readerPool.Release(rld);
							}
							anyNewDeletes |= delCount > 0;
							if (segAllDeletes)
							{
								if (allDeleted == null)
								{
									allDeleted = new AList<SegmentCommitInfo>();
								}
								allDeleted.AddItem(info);
							}
							if (infoStream.IsEnabled("BD"))
							{
								infoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " segDeletes=[" + 
									packet + "]; coalesced deletes=[" + (coalescedUpdates == null ? "null" : coalescedUpdates
									) + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted" : string.Empty
									));
							}
							if (coalescedUpdates == null)
							{
								coalescedUpdates = new CoalescedUpdates();
							}
							delIDX--;
							infosIDX--;
							info.SetBufferedDeletesGen(gen);
						}
						else
						{
							//System.out.println("  gt");
							if (coalescedUpdates != null)
							{
								// Lock order: IW -> BD -> RP
								//HM:revisit 
								//assert readerPool.infoIsLive(info);
								ReadersAndUpdates rld = readerPool.Get(info, true);
								SegmentReader reader = rld.GetReader(IOContext.READ);
								int delCount = 0;
								bool segAllDeletes;
								try
								{
									delCount += ApplyTermDeletes(coalescedUpdates.TermsIterable(), rld, reader);
									delCount += ApplyQueryDeletes(coalescedUpdates.QueriesIterable(), rld, reader);
									DocValuesFieldUpdates.Container dvUpdates = new DocValuesFieldUpdates.Container();
									ApplyDocValuesUpdates(coalescedUpdates.numericDVUpdates.AsIterable(), rld, reader
										, dvUpdates);
									ApplyDocValuesUpdates(coalescedUpdates.binaryDVUpdates.AsIterable(), rld, reader, 
										dvUpdates);
									if (dvUpdates.Any())
									{
										rld.WriteFieldUpdates(info.info.dir, dvUpdates);
									}
									int fullDelCount = rld.Info.GetDelCount() + rld.GetPendingDeleteCount();
									//HM:revisit 
									//assert fullDelCount <= rld.info.info.getDocCount();
									segAllDeletes = fullDelCount == rld.Info.info.GetDocCount();
								}
								finally
								{
									rld.Release(reader);
									readerPool.Release(rld);
								}
								anyNewDeletes |= delCount > 0;
								if (segAllDeletes)
								{
									if (allDeleted == null)
									{
										allDeleted = new AList<SegmentCommitInfo>();
									}
									allDeleted.AddItem(info);
								}
								if (infoStream.IsEnabled("BD"))
								{
									infoStream.Message("BD", "seg=" + info + " segGen=" + segGen + " coalesced deletes=["
										 + coalescedUpdates + "] newDelCount=" + delCount + (segAllDeletes ? " 100% deleted"
										 : string.Empty));
								}
							}
							info.SetBufferedDeletesGen(gen);
							infosIDX--;
						}
					}
				}
				//HM:revisit 
				//assert checkDeleteStats();
				if (infoStream.IsEnabled("BD"))
				{
					infoStream.Message("BD", "applyDeletes took " + (Runtime.CurrentTimeMillis() - t0
						) + " msec");
				}
				// 
				//HM:revisit 
				//assert infos != segmentInfos || !any() : "infos=" + infos + " segmentInfos=" + segmentInfos + " any=" + any;
				return new BufferedUpdatesStream.ApplyDeletesResult(anyNewDeletes, gen, allDeleted
					);
			}
		}

		internal virtual long GetNextGen()
		{
			lock (this)
			{
				return nextGen++;
			}
		}

		// Lock order IW -> BD
		public virtual void Prune(SegmentInfos segmentInfos)
		{
			lock (this)
			{
				//HM:revisit 
				//assert checkDeleteStats();
				long minGen = long.MaxValue;
				foreach (SegmentCommitInfo info in segmentInfos)
				{
					minGen = Math.Min(info.GetBufferedDeletesGen(), minGen);
				}
				if (infoStream.IsEnabled("BD"))
				{
					infoStream.Message("BD", "prune sis=" + segmentInfos + " minGen=" + minGen + " packetCount="
						 + updates.Count);
				}
				int limit = updates.Count;
				for (int delIDX = 0; delIDX < limit; delIDX++)
				{
					if (updates[delIDX].DelGen() >= minGen)
					{
						Prune(delIDX);
						//HM:revisit 
						//assert checkDeleteStats();
						return;
					}
				}
				// All deletes pruned
				Prune(limit);
			}
		}

		//HM:revisit 
		//assert !any();
		//HM:revisit 
		//assert checkDeleteStats();
		private void Prune(int count)
		{
			lock (this)
			{
				if (count > 0)
				{
					if (infoStream.IsEnabled("BD"))
					{
						infoStream.Message("BD", "pruneDeletes: prune " + count + " packets; " + (updates
							.Count - count) + " packets remain");
					}
					for (int delIDX = 0; delIDX < count; delIDX++)
					{
						FrozenBufferedUpdates packet = updates[delIDX];
						numTerms.AddAndGet(-packet.numTermDeletes);
						//HM:revisit 
						//assert numTerms.get() >= 0;
						bytesUsed.AddAndGet(-packet.bytesUsed);
					}
					//HM:revisit 
					//assert bytesUsed.get() >= 0;
					updates.SubList(0, count).Clear();
				}
			}
		}

		// Delete by Term
		/// <exception cref="System.IO.IOException"></exception>
		private long ApplyTermDeletes(Iterable<Term> termsIter, ReadersAndUpdates rld, SegmentReader
			 reader)
		{
			lock (this)
			{
				long delCount = 0;
				Fields fields = reader.Fields();
				if (fields == null)
				{
					// This reader has no postings
					return 0;
				}
				TermsEnum termsEnum = null;
				string currentField = null;
				DocsEnum docs = null;
				//HM:revisit 
				//assert checkDeleteTerm(null);
				bool any = false;
				//System.out.println(Thread.currentThread().getName() + " del terms reader=" + reader);
				foreach (Term term in termsIter)
				{
					// Since we visit terms sorted, we gain performance
					// by re-using the same TermsEnum and seeking only
					// forwards
					if (!term.Field().Equals(currentField))
					{
						//HM:revisit 
						//assert currentField == null || currentField.compareTo(term.field()) < 0;
						currentField = term.Field();
						Terms terms = fields.Terms(currentField);
						if (terms != null)
						{
							termsEnum = terms.Iterator(termsEnum);
						}
						else
						{
							termsEnum = null;
						}
					}
					if (termsEnum == null)
					{
						continue;
					}
					//HM:revisit 
					//assert checkDeleteTerm(term);
					// System.out.println("  term=" + term);
					if (termsEnum.SeekExact(term.Bytes()))
					{
						// we don't need term frequencies for this
						DocsEnum docsEnum = termsEnum.Docs(rld.GetLiveDocs(), docs, DocsEnum.FLAG_NONE);
						//System.out.println("BDS: got docsEnum=" + docsEnum);
						if (docsEnum != null)
						{
							while (true)
							{
								int docID = docsEnum.NextDoc();
								//System.out.println(Thread.currentThread().getName() + " del term=" + term + " doc=" + docID);
								if (docID == DocIdSetIterator.NO_MORE_DOCS)
								{
									break;
								}
								if (!any)
								{
									rld.InitWritableLiveDocs();
									any = true;
								}
								// NOTE: there is no limit check on the docID
								// when deleting by Term (unlike by Query)
								// because on flush we apply all Term deletes to
								// each segment.  So all Term deleting here is
								// against prior segments:
								if (rld.Delete(docID))
								{
									delCount++;
								}
							}
						}
					}
				}
				return delCount;
			}
		}

		// DocValues updates
		/// <exception cref="System.IO.IOException"></exception>
		private void ApplyDocValuesUpdates<_T0>(Iterable<_T0> updates, ReadersAndUpdates 
			rld, SegmentReader reader, DocValuesFieldUpdates.Container dvUpdatesContainer) where 
			_T0:DocValuesUpdate
		{
			lock (this)
			{
				Fields fields = reader.Fields();
				if (fields == null)
				{
					// This reader has no postings
					return;
				}
				// TODO: we can process the updates per DV field, from last to first so that
				// if multiple terms affect same document for the same field, we add an update
				// only once (that of the last term). To do that, we can keep a bitset which
				// marks which documents have already been updated. So e.g. if term T1
				// updates doc 7, and then we process term T2 and it updates doc 7 as well,
				// we don't apply the update since we know T1 came last and therefore wins
				// the update.
				// We can also use that bitset as 'liveDocs' to pass to TermEnum.docs(), so
				// that these documents aren't even returned.
				string currentField = null;
				TermsEnum termsEnum = null;
				DocsEnum docs = null;
				//System.out.println(Thread.currentThread().getName() + " numericDVUpdate reader=" + reader);
				foreach (DocValuesUpdate update in updates)
				{
					Term term = update.term;
					int limit = update.docIDUpto;
					// TODO: we traverse the terms in update order (not term order) so that we
					// apply the updates in the correct order, i.e. if two terms udpate the
					// same document, the last one that came in wins, irrespective of the
					// terms lexical order.
					// we can apply the updates in terms order if we keep an updatesGen (and
					// increment it with every update) and attach it to each NumericUpdate. Note
					// that we cannot rely only on docIDUpto because an app may send two updates
					// which will get same docIDUpto, yet will still need to respect the order
					// those updates arrived.
					if (!term.Field().Equals(currentField))
					{
						// if we change the code to process updates in terms order, enable this 
						//HM:revisit 
						//assert
						//        
						//HM:revisit 
						//assert currentField == null || currentField.compareTo(term.field()) < 0;
						currentField = term.Field();
						Terms terms = fields.Terms(currentField);
						if (terms != null)
						{
							termsEnum = terms.Iterator(termsEnum);
						}
						else
						{
							termsEnum = null;
							continue;
						}
					}
					// no terms in that field
					if (termsEnum == null)
					{
						continue;
					}
					// System.out.println("  term=" + term);
					if (termsEnum.SeekExact(term.Bytes()))
					{
						// we don't need term frequencies for this
						DocsEnum docsEnum = termsEnum.Docs(rld.GetLiveDocs(), docs, DocsEnum.FLAG_NONE);
						//System.out.println("BDS: got docsEnum=" + docsEnum);
						DocValuesFieldUpdates dvUpdates = dvUpdatesContainer.GetUpdates(update.field, update
							.type);
						if (dvUpdates == null)
						{
							dvUpdates = dvUpdatesContainer.NewUpdates(update.field, update.type, reader.MaxDoc
								());
						}
						int doc;
						while ((doc = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
						{
							//System.out.println(Thread.currentThread().getName() + " numericDVUpdate term=" + term + " doc=" + docID);
							if (doc >= limit)
							{
								break;
							}
							// no more docs that can be updated for this term
							dvUpdates.Add(doc, update.value);
						}
					}
				}
			}
		}

		public class QueryAndLimit
		{
			public readonly Query query;

			public readonly int limit;

			public QueryAndLimit(Query query, int limit)
			{
				this.query = query;
				this.limit = limit;
			}
		}

		// Delete by query
		/// <exception cref="System.IO.IOException"></exception>
		private static long ApplyQueryDeletes(Iterable<BufferedUpdatesStream.QueryAndLimit
			> queriesIter, ReadersAndUpdates rld, SegmentReader reader)
		{
			long delCount = 0;
			AtomicReaderContext readerContext = ((AtomicReaderContext)reader.GetContext());
			bool any = false;
			foreach (BufferedUpdatesStream.QueryAndLimit ent in queriesIter)
			{
				Query query = ent.query;
				int limit = ent.limit;
				DocIdSet docs = new QueryWrapperFilter(query).GetDocIdSet(readerContext, reader.GetLiveDocs
					());
				if (docs != null)
				{
					DocIdSetIterator it = docs.Iterator();
					if (it != null)
					{
						while (true)
						{
							int doc = it.NextDoc();
							if (doc >= limit)
							{
								break;
							}
							if (!any)
							{
								rld.InitWritableLiveDocs();
								any = true;
							}
							if (rld.Delete(doc))
							{
								delCount++;
							}
						}
					}
				}
			}
			return delCount;
		}

		// used only by 
		//HM:revisit 
		//assert
		private bool CheckDeleteTerm(Term term)
		{
			if (term != null)
			{
			}
			//HM:revisit 
			//assert lastDeleteTerm == null || term.compareTo(lastDeleteTerm) > 0: "lastTerm=" + lastDeleteTerm + " vs term=" + term;
			// TODO: we re-use term now in our merged iterable, but we shouldn't clone, instead copy for this 
			//HM:revisit 
			//assert
			lastDeleteTerm = term == null ? null : new Term(term.Field(), BytesRef.DeepCopyOf
				(term.bytes));
			return true;
		}

		// only for 
		//HM:revisit 
		//assert
		private bool CheckDeleteStats()
		{
			int numTerms2 = 0;
			long bytesUsed2 = 0;
			foreach (FrozenBufferedUpdates packet in updates)
			{
				numTerms2 += packet.numTermDeletes;
				bytesUsed2 += packet.bytesUsed;
			}
			//HM:revisit 
			//assert numTerms2 == numTerms.get(): "numTerms2=" + numTerms2 + " vs " + numTerms.get();
			//HM:revisit 
			//assert bytesUsed2 == bytesUsed.get(): "bytesUsed2=" + bytesUsed2 + " vs " + bytesUsed;
			return true;
		}
	}
}
