using System.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using FlushedSegment = Lucene.Net.Index.DocumentsWriterPerThread.FlushedSegment;

namespace Lucene.Net.Index
{
    internal class DocumentsWriterFlushQueue
    {
        private readonly Queue<FlushTicket> queue = new Queue<FlushTicket>();
        // we track tickets separately since count must be present even before the ticket is
        // constructed ie. queue.size would not reflect it.
		private readonly AtomicInteger ticketCount = new AtomicInteger();
        private readonly ReentrantLock purgeLock = new ReentrantLock();

		internal virtual void AddDeletes(DocumentsWriterDeleteQueue deleteQueue)
        {
            lock (this)
            {
                IncTickets();// first inc the ticket count - freeze opens
                // a window for #anyChanges to fail
                bool success = false;
                try
                {
                    queue.Enqueue(new GlobalDeletesTicket(deleteQueue.FreezeGlobalBuffer(null)));
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        DecTickets();
                    }
                }
            }
            // don't hold the lock on the FlushQueue when forcing the purge - this blocks and deadlocks 
            // if we hold the lock.
        }

        private void IncTickets()
        {
			int numTickets = ticketCount.IncrementAndGet();
            Debug.Assert(numTickets > 0);
        }

        private void DecTickets()
        {
			int numTickets = ticketCount.DecrementAndGet();
            Debug.Assert(numTickets >= 0);
        }

        internal SegmentFlushTicket AddFlushTicket(DocumentsWriterPerThread dwpt)
        {
            lock (this)
            {
                // Each flush is assigned a ticket in the order they acquire the ticketQueue
                // lock
                IncTickets();
                bool success = false;
                try
                {
                    // prepare flush freezes the global deletes - do in synced block!
                    SegmentFlushTicket ticket = new SegmentFlushTicket(dwpt.PrepareFlush());
                    queue.Enqueue(ticket);
                    success = true;
                    return ticket;
                }
                finally
                {
                    if (!success)
                    {
                        DecTickets();
                    }
                }
            }
        }

        internal void AddSegment(SegmentFlushTicket ticket, FlushedSegment segment)
        {
            lock (this)
            {
                // the actual flush is done asynchronously and once done the FlushedSegment
                // is passed to the flush ticket
                ticket.SetSegment(segment);
            }
        }

        internal void MarkTicketFailed(SegmentFlushTicket ticket)
        {
            lock (this)
            {
                // to free the queue we mark tickets as failed just to clean up the queue.
                ticket.SetFailed();
            }
        }

        internal bool HasTickets
        {
            get
            {
                //assert ticketCount.get() >= 0 : "ticketCount should be >= 0 but was: " + ticketCount.get();
			return ticketCount.Get() != 0;
            }
        }

		private int InnerPurge(IndexWriter writer)
        {
            //assert purgeLock.isHeldByCurrentThread();
			int numPurged = 0;
            while (true)
            {
                FlushTicket head;
                bool canPublish;
                lock (this)
                {
                    head = queue.Count > 0 ? queue.Peek() : null;
                    canPublish = head != null && head.CanPublish; // do this synced 
                }
                if (canPublish)
                {
					numPurged++;
                    try
                    {
                        /*
                         * if we block on publish -> lock IW -> lock BufferedDeletes we don't block
                         * concurrent segment flushes just because they want to append to the queue.
                         * the downside is that we need to force a purge on fullFlush since ther could
                         * be a ticket still in the queue. 
                         */
                        head.Publish(writer);
                    }
                    finally
                    {
                        lock (this)
                        {
                            // finally remove the published ticket from the queue
                            FlushTicket poll = queue.Dequeue();
							ticketCount.DecrementAndGet();
                            //assert poll == head;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
			return numPurged;
        }

		internal virtual int ForcePurge(IndexWriter writer)
        {
            //assert !Thread.holdsLock(this);
            purgeLock.Lock();
            try
            {
				return InnerPurge(writer);
            }
            finally
            {
                purgeLock.Unlock();
            }
        }

		internal virtual int TryPurge(IndexWriter writer)
        {
            //assert !Thread.holdsLock(this);
            if (purgeLock.TryLock())
            {
                try
                {
					return InnerPurge(writer);
                }
                finally
                {
                    purgeLock.Unlock();
                }
            }
			return 0;
        }

        public int TicketCount
        {
            get { return ticketCount.Get(); }
        }

        internal void Clear()
        {
            lock (this)
            {
                queue.Clear();
                ticketCount.Set(0);
            }
        }

        internal abstract class FlushTicket
        {
			protected internal FrozenBufferedUpdates frozenUpdates;
            protected bool published = false;

			protected internal FlushTicket(FrozenBufferedUpdates frozenUpdates)
            {
                //assert frozenDeletes != null;
				this.frozenUpdates = frozenUpdates;
            }

			protected internal abstract void Publish(IndexWriter writer);
            public abstract bool CanPublish { get; }
			protected internal void PublishFlushedSegment(IndexWriter indexWriter, DocumentsWriterPerThread.FlushedSegment
				 newSegment, FrozenBufferedUpdates globalPacket)
			{
				//HM:revisit 
				//assert newSegment != null;
				//HM:revisit 
				//assert newSegment.segmentInfo != null;
				FrozenBufferedUpdates segmentUpdates = newSegment.segmentUpdates;
				//System.out.println("FLUSH: " + newSegment.segmentInfo.info.name);
				if (indexWriter.infoStream.IsEnabled("DW"))
				{
					indexWriter.infoStream.Message("DW", "publishFlushedSegment seg-private updates="
						 + segmentUpdates);
				}
				if (segmentUpdates != null && indexWriter.infoStream.IsEnabled("DW"))
				{
					indexWriter.infoStream.Message("DW", "flush: push buffered seg private updates: "
						 + segmentUpdates);
				}
				// now publish!
				indexWriter.PublishFlushedSegment(newSegment.segmentInfo, segmentUpdates, globalPacket
					);
			}

			protected internal void FinishFlush(IndexWriter indexWriter, DocumentsWriterPerThread.FlushedSegment
				 newSegment, FrozenBufferedUpdates bufferedUpdates)
			{
				// Finish the flushed segment and publish it to IndexWriter
				if (newSegment == null)
				{
					//HM:revisit 
					//assert bufferedUpdates != null;
					if (bufferedUpdates != null && bufferedUpdates.Any())
					{
						indexWriter.PublishFrozenUpdates(bufferedUpdates);
						if (indexWriter.infoStream.IsEnabled("DW"))
						{
							indexWriter.infoStream.Message("DW", "flush: push buffered updates: " + bufferedUpdates
								);
						}
					}
				}
				else
				{
					PublishFlushedSegment(indexWriter, newSegment, bufferedUpdates);
				}
			}
		}
        internal sealed class GlobalDeletesTicket : FlushTicket
        {
			protected internal GlobalDeletesTicket(FrozenBufferedUpdates frozenUpdates) : base
				(frozenUpdates)
            {
            }

			protected internal override void Publish(IndexWriter writer)
            {
                //assert !published : "ticket was already publised - can not publish twice";
                published = true;
                // its a global ticket - no segment to publish
				FinishFlush(writer, null, frozenUpdates);
            }

            public override bool CanPublish
            {
                get { return true; }
            }
        }

        internal sealed class SegmentFlushTicket : FlushTicket
        {
            private FlushedSegment segment;
            private bool failed = false;

			protected internal SegmentFlushTicket(FrozenBufferedUpdates frozenDeletes) : base
				(frozenDeletes)
            {
            }

			protected internal override void Publish(IndexWriter writer)
            {
                //assert !published : "ticket was already publised - can not publish twice";
                published = true;
				FinishFlush(writer, segment, frozenUpdates);
            }

            public void SetSegment(FlushedSegment segment)
            {
                //assert !failed;
                this.segment = segment;
            }

            public void SetFailed()
            {
                //assert segment == null;
                failed = true;
            }

            public override bool CanPublish
            {
                get { return segment != null || failed; }
            }
        }
    }
}
