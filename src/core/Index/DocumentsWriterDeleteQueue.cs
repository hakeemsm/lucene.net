using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    public sealed class DocumentsWriterDeleteQueue
    {
        private Node tail; // .NET port: can't use type without specifying type parameter, also not volatile due to Interlocked

        // .NET port: no need for AtomicReferenceFieldUpdater, we can use Interlocked instead

        private readonly DeleteSlice globalSlice;
        private readonly BufferedUpdates globalBufferedUpdates;
        /* only acquired to update the global deletes */
        private readonly ReentrantLock globalBufferLock = new ReentrantLock();

        internal readonly long generation;

        public DocumentsWriterDeleteQueue()
            : this(0)
        {
        }

        internal DocumentsWriterDeleteQueue(long generation)
            : this(new BufferedUpdates()
                , generation)
        {
        }

        internal DocumentsWriterDeleteQueue(BufferedUpdates globalBufferedUpdates, long generation
            )
        {
            this.globalBufferedUpdates = globalBufferedUpdates;
            this.generation = generation;
            tail = new DocumentsWriterDeleteQueue.Node<object>(null);
            /*
             * we use a sentinel instance as our initial tail. No slice will ever try to
             * apply this tail since the head is always omitted.
             */
            globalSlice = new DeleteSlice(tail);
        }

        internal void AddDelete(params Query[] queries)
        {
            Add(new QueryArrayNode(queries));
            TryApplyGlobalSlice();
        }

        internal void AddDelete(params Term[] terms)
        {
            Add(new TermArrayNode(terms));
            TryApplyGlobalSlice();
        }

        internal void AddNumericUpdate(DocValuesUpdate.NumericDocValuesUpdate update)
        {
            Add(new DocumentsWriterDeleteQueue.NumericUpdateNode(update));
            TryApplyGlobalSlice();
        }

        internal void AddBinaryUpdate(DocValuesUpdate.BinaryDocValuesUpdate update)
        {
            Add(new DocumentsWriterDeleteQueue.BinaryUpdateNode(update));
            TryApplyGlobalSlice();
        }
        internal void Add(Term term, DeleteSlice slice)
        {
            TermNode termNode = new TermNode(term);
            //    System.out.println(Thread.currentThread().getName() + ": push " + termNode + " this=" + this);
            Add(termNode);
            /*
             * this is an update request where the term is the updated documents
             * delTerm. in that case we need to guarantee that this insert is atomic
             * with regards to the given delete slice. This means if two threads try to
             * update the same document with in turn the same delTerm one of them must
             * win. By taking the node we have created for our del term as the new tail
             * it is guaranteed that if another thread adds the same right after us we
             * will apply this delete next time we update our slice and one of the two
             * competing updates wins!
             */
            slice.sliceTail = termNode;
            //assert slice.sliceHead != slice.sliceTail : "slice head and tail must differ after add";
            TryApplyGlobalSlice(); // TODO doing this each time is not necessary maybe
            // we can do it just every n times or so?
        }

        internal void Add(Node item)
        {
            /*
             * this non-blocking / 'wait-free' linked list add was inspired by Apache
             * Harmony's ConcurrentLinkedQueue Implementation.
             */
            while (true)
            {
                Node currentTail = this.tail;
                Node tailNext = currentTail.next;
                if (tail == currentTail)
                {
                    if (tailNext != null)
                    {
                        /*
                         * we are in intermediate state here. the tails next pointer has been
                         * advanced but the tail itself might not be updated yet. help to
                         * advance the tail and try again updating it.
                         */
                        Interlocked.CompareExchange(ref tail, tailNext, currentTail); // can fail
                    }
                    else
                    {
                        /*
                         * we are in quiescent state and can try to insert the item to the
                         * current tail if we fail to insert we just retry the operation since
                         * somebody else has already added its item
                         */
                        if (currentTail.CasNext(null, item))
                        {
                            /*
                             * now that we are done we need to advance the tail while another
                             * thread could have advanced it already so we can ignore the return
                             * type of this CAS call
                             */
                            Interlocked.CompareExchange(ref tail, item, currentTail);
                            return;
                        }
                    }
                }
            }
        }

        internal bool AnyChanges
        {
            get
            {
                globalBufferLock.Lock();
                try
                {
                    /*
                     * check if all items in the global slice were applied 
                     * and if the global slice is up-to-date
                     * and if globalBufferedDeletes has changes
                     */
                    return globalBufferedUpdates.Any() || !globalSlice.IsEmpty || globalSlice.sliceTail != tail
                        || tail.next != null;
                }
                finally
                {
                    globalBufferLock.Unlock();
                }
            }
        }

        internal void TryApplyGlobalSlice()
        {
            if (globalBufferLock.TryLock())
            {
                /*
                 * The global buffer must be locked but we don't need to update them if
                 * there is an update going on right now. It is sufficient to apply the
                 * deletes that have been added after the current in-flight global slices
                 * tail the next time we can get the lock!
                 */
                try
                {
                    if (UpdateSlice(globalSlice))
                    {
                        //          System.out.println(Thread.currentThread() + ": apply globalSlice");
                        globalSlice.Apply(globalBufferedUpdates, BufferedUpdates.MAX_INT);
                    }
                }
                finally
                {
                    globalBufferLock.Unlock();
                }
            }
        }

        internal FrozenBufferedUpdates FreezeGlobalBuffer(DeleteSlice callerSlice)
        {
            globalBufferLock.Lock();
            /*
             * Here we freeze the global buffer so we need to lock it, apply all
             * deletes in the queue and reset the global slice to let the GC prune the
             * queue.
             */
            Node currentTail = tail; // take the current tail make this local any
            // Changes after this call are applied later
            // and not relevant here
            if (callerSlice != null)
            {
                // Update the callers slices so we are on the same page
                callerSlice.sliceTail = currentTail;
            }
            try
            {
                if (globalSlice.sliceTail != currentTail)
                {
                    globalSlice.sliceTail = currentTail;
                    globalSlice.Apply(globalBufferedUpdates, BufferedUpdates.MAX_INT);
                }

                //      System.out.println(Thread.currentThread().getName() + ": now freeze global buffer " + globalBufferedDeletes);
                FrozenBufferedUpdates packet = new FrozenBufferedUpdates(globalBufferedUpdates, false
                    );
                globalBufferedUpdates.Clear();
                return packet;
            }
            finally
            {
                globalBufferLock.Unlock();
            }
        }

        internal DeleteSlice NewSlice()
        {
            return new DeleteSlice(tail);
        }

        internal bool UpdateSlice(DeleteSlice slice)
        {
            if (slice.sliceTail != tail)
            { // If we are the same just
                slice.sliceTail = tail;
                return true;
            }
            return false;
        }

        internal class DeleteSlice
        {
            internal Node sliceHead;
            internal Node sliceTail;

            public DeleteSlice(Node currentTail)
            {
                //assert currentTail != null;
                /*
                 * Initially this is a 0 length slice pointing to the 'current' tail of
                 * the queue. Once we update the slice we only need to assign the tail and
                 * have a new slice
                 */
                sliceHead = sliceTail = currentTail;
            }

            internal virtual void Apply(BufferedUpdates del, int docIDUpto)
            {
                if (sliceHead == sliceTail)
                {
                    // 0 length slice
                    return;
                }
                /*
                 * When we apply a slice we take the head and get its next as our first
                 * item to apply and continue until we applied the tail. If the head and
                 * tail in this slice are not equal then there will be at least one more
                 * non-null node in the slice!
                 */
                Node current = sliceHead;
                do
                {
                    current = current.next;
                    //assert current != null : "slice property violated between the head on the tail must not be a null node";
                    current.Apply(del, docIDUpto);
                    //        System.out.println(Thread.currentThread().getName() + ": pull " + current + " docIDUpto=" + docIDUpto);
                } while (current != sliceTail);
                Reset();
            }

            internal void Reset()
            {
                // Reset to a 0 length slice
                sliceHead = sliceTail;
            }

            internal bool IsTailItem(Object item)
            {
                return sliceTail.Item == item;
            }

            internal bool IsEmpty
            {
                get { return sliceHead == sliceTail; }
            }
        }

        public int NumGlobalTermDeletes
        {
            get { return globalBufferedUpdates.numTermDeletes.Get(); }
        }

        internal void Clear()
        {
            globalBufferLock.Lock();
            try
            {
                Node currentTail = tail;
                globalSlice.sliceHead = globalSlice.sliceTail = currentTail;
                globalBufferedUpdates.Clear();
            }
            finally
            {
                globalBufferLock.Unlock();
            }
        }

        internal class Node
        {
            internal Node next; // .NET Port: not using volatile due to Interlocked usage
            internal readonly object item; // .NET Port: can't use Node<?> without specifying type param, so not generic

            internal Node(object item)
            {
                this.item = item;
            }

            // .NET Port: no need for AtomicReferenceFieldUpdater here, we're using Interlocked hotness

            internal virtual void Apply(BufferedUpdates bufferedDeletes, int docIDUpto)
            {
                throw new InvalidOperationException("sentinel item must never be applied");
            }

            internal virtual bool CasNext(Node cmp, Node val)
            {
                // .NET port: Interlocked.CompareExchange(location, value, comparand) is backwards from
                // AtomicReferenceFieldUpdater.compareAndSet(obj, expect, update), so swapping val and cmp.
                // Also, it doesn't return bool if it was updated, so we need to compare to see if 
                // original == comparand to determine whether to return true or false here.
                Node original = next;
                return ReferenceEquals(Interlocked.CompareExchange(ref next, val, cmp), original);
            }

            public object Item
            {
                get { return item; }
            }
        }

        // .NET port: helper class to add back in some generic behavior
        private class Node<T> : Node
            where T : class
        {
            internal Node(T item)
                : base(item)
            {
            }

            public new T Item
            {
                get { return base.Item as T; }
            }
        }

        private sealed class TermNode : Node<Term>
        {
            public TermNode(Term term)
                : base(term)
            {
            }

            internal override void Apply(BufferedUpdates bufferedDeletes, int docIDUpto)
            {
                bufferedDeletes.AddTerm(Item, docIDUpto);
            }

            public override string ToString()
            {
                return "del=" + Item;
            }
        }

        private sealed class QueryArrayNode : Node<Query[]>
        {
            public QueryArrayNode(Query[] query)
                : base(query)
            {
            }

            internal override void Apply(BufferedUpdates bufferedUpdates, int docIDUpto)
            {
                foreach (var query in Item)
                {
                    bufferedUpdates.AddQuery(query, docIDUpto);
                }
            }
        }

        private sealed class TermArrayNode : Node<Term[]>
        {
            public TermArrayNode(Term[] term)
                : base(term)
            {
            }

            internal override void Apply(BufferedUpdates bufferedUpdates, int docIDUpto)
            {
                foreach (Term term in Item)
                {
                    bufferedUpdates.AddTerm(term, docIDUpto);
                }
            }

            public override string ToString()
            {
                return "dels=" + Arrays.ToString(Item);
            }
        }

        private sealed class NumericUpdateNode : Node<DocValuesUpdate.NumericDocValuesUpdate>
        {
            internal NumericUpdateNode(DocValuesUpdate.NumericDocValuesUpdate update)
                : base(update)
            {
            }

            internal override void Apply(BufferedUpdates bufferedUpdates, int docIDUpto)
            {
                bufferedUpdates.AddNumericUpdate((DocValuesUpdate.NumericDocValuesUpdate) item, docIDUpto);
            }

            public override string ToString()
            {
                return "update=" + item;
            }
        }
        private sealed class BinaryUpdateNode : Node<DocValuesUpdate.BinaryDocValuesUpdate
            >
        {
            internal BinaryUpdateNode(DocValuesUpdate.BinaryDocValuesUpdate update)
                : base(update)
            {
            }

            internal override void Apply(BufferedUpdates bufferedUpdates, int docIDUpto)
            {
                bufferedUpdates.AddBinaryUpdate((DocValuesUpdate.BinaryDocValuesUpdate) item, docIDUpto);
            }

            public override string ToString()
            {
                return "update=" + item;
            }
        }
        private bool ForceApplyGlobalSlice()
        {
            globalBufferLock.Lock();
            Node currentTail = tail;
            try
            {
                if (globalSlice.sliceTail != currentTail)
                {
                    globalSlice.sliceTail = currentTail;
                    globalSlice.Apply(globalBufferedUpdates, BufferedUpdates.MAX_INT);
                }
                return globalBufferedUpdates.Any();
            }
            finally
            {
                globalBufferLock.Unlock();
            }
        }

        public int BufferedUpdatesTermsSize
        {
            get
            {
                globalBufferLock.Lock();
                try
                {
                    ForceApplyGlobalSlice();
                    return globalBufferedUpdates.terms.Count;
                }
                finally
                {
                    globalBufferLock.Unlock();
                }
            }
        }

        public long BytesUsed
        {
            get { return globalBufferedUpdates.bytesUsed.Get(); }
        }

        public override string ToString()
        {
            return "DWDQ: [ generation: " + generation + " ]";
        }
    }
}
