using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
    internal class ReadersAndUpdates
    {
        public readonly SegmentCommitInfo Info;

        private int refCount = 1;

        private readonly IndexWriter writer;

        private SegmentReader reader;

        private SegmentReader mergeReader;

        private IBits liveDocs;

        private int pendingDeleteCount;

        private bool liveDocsShared;

        private bool isMerging = false;

        private readonly IDictionary<string, DocValuesFieldUpdates> mergingDVUpdates = new
            Dictionary<string, DocValuesFieldUpdates>();

        public ReadersAndUpdates(IndexWriter writer, SegmentCommitInfo info)
        {
            // Used by IndexWriter to hold open SegmentReaders (for
            // searching or merging), plus pending deletes and updates,
            // for a given segment
            // Not final because we replace (clone) when we need to
            // change it and it's been shared:
            // Tracks how many consumers are using this instance:
            // Set once (null, and then maybe set, and never set again):
            // TODO: it's sometimes wasteful that we hold open two
            // separate SRs (one for merging one for
            // reading)... maybe just use a single SR?  The gains of
            // not loading the terms index (for merging in the
            // non-NRT case) are far less now... and if the app has
            // any deletes it'll open real readers anyway.
            // Set once (null, and then maybe set, and never set again):
            // Holds the current shared (readable and writable)
            // liveDocs.  This is null when there are no deleted
            // docs, and it's copy-on-write (cloned whenever we need
            // to change it but it's been shared to an external NRT
            // reader).
            // How many further deletions we've done against
            // liveDocs vs when we loaded it or last wrote it:
            // True if the current liveDocs is referenced by an
            // external NRT reader:
            // Indicates whether this segment is currently being merged. While a segment
            // is merging, all field updates are also registered in the
            // mergingNumericUpdates map. Also, calls to writeFieldUpdates merge the 
            // updates with mergingNumericUpdates.
            // That way, when the segment is done merging, IndexWriter can apply the
            // updates on the merged segment too.
            this.Info = info;
            this.writer = writer;
            liveDocsShared = true;
        }

        public virtual void IncRef()
        {
            Interlocked.Increment(ref refCount);
        }

        //HM:revisit 
        //assert rc > 1;
        public virtual void DecRef()
        {
            Interlocked.Decrement(ref refCount);
        }

        //HM:revisit 
        //assert rc >= 0;
        public virtual int RefCount
        {
            get { return refCount; }
        }

        public virtual int PendingDeleteCount
        {
            get
            {
                //Java had a lock here. not sure why
                return pendingDeleteCount;
            }
        }

        
        public virtual bool VerifyDocCounts()
        {
            lock (this)
            {
                int count;
                if (liveDocs != null)
                {
                    count = 0;
                    for (int docID = 0; docID < Info.info.DocCount; docID++)
                    {
                        if (liveDocs[docID])
                        {
                            count++;
                        }
                    }
                }
                else
                {
                    count = Info.info.DocCount; //what is this for?
                }
                //HM:revisit 
                //assert info.info.getDocCount() - info.getDelCount() - pendingDeleteCount == count: "info.docCount=" + info.info.getDocCount() + " info.getDelCount()=" + info.getDelCount() + " pendingDeleteCount=" + pendingDeleteCount + " count=" + count;
                return true;
            }
        }

        /// <summary>
        /// Returns a
        /// <see cref="SegmentReader">SegmentReader</see>
        /// .
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual SegmentReader GetReader(IOContext context)
        {
            if (reader == null)
            {
                // We steal returned ref:
                reader = new SegmentReader(Info, writer.Config.ReaderTermsIndexDivisor,context);
                if (liveDocs == null)
                {
                    liveDocs = reader.LiveDocs;
                }
            }
            // Ref for caller
            reader.IncRef();
            return reader;
        }

        // Get reader for merging (does not load the terms
        // index):
        /// <exception cref="System.IO.IOException"></exception>
        public virtual SegmentReader GetMergeReader(IOContext context)
        {
            lock (this)
            {
                //System.out.println("  livedocs=" + rld.liveDocs);
                if (mergeReader == null)
                {
                    if (reader != null)
                    {
                        // Just use the already opened non-merge reader
                        // for merging.  In the NRT case this saves us
                        // pointless double-open:
                        //System.out.println("PROMOTE non-merge reader seg=" + rld.info);
                        // Ref for us:
                        reader.IncRef();
                        mergeReader = reader;
                    }
                    else
                    {
                        //System.out.println(Thread.currentThread().getName() + ": getMergeReader share seg=" + info.name);
                        //System.out.println(Thread.currentThread().getName() + ": getMergeReader seg=" + info.name);
                        // We steal returned ref:
                        mergeReader = new SegmentReader(Info, -1, context);
                        if (liveDocs == null)
                        {
                            liveDocs = mergeReader.LiveDocs;
                        }
                    }
                }
                // Ref for caller
                mergeReader.IncRef();
                return mergeReader;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public virtual void Release(SegmentReader sr)
        {
            lock (this)
            {
                //HM:revisit 
                //assert info == sr.getSegmentInfo();
                sr.DecRef();
            }
        }

        public virtual bool Delete(int docID)
        {
            lock (this)
            {
                //HM:revisit 
                //assert liveDocs != null;
                //HM:revisit 
                //assert Thread.holdsLock(writer);
                //HM:revisit 
                //assert docID >= 0 && docID < liveDocs.length() : "out of bounds: docid=" + docID + " liveDocsLength=" + liveDocs.length() + " seg=" + info.info.name + " docCount=" + info.info.getDocCount();
                //HM:revisit 
                //assert !liveDocsShared;
                bool didDelete = liveDocs[docID];
                if (didDelete)
                {
                    ((IMutableBits)liveDocs).Clear(docID);
                    pendingDeleteCount++;
                }
                //System.out.println("  new del seg=" + info + " docID=" + docID + " pendingDelCount=" + pendingDeleteCount + " totDelCount=" + (info.docCount-liveDocs.count()));
                return didDelete;
            }
        }

        // NOTE: removes callers ref
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void DropReaders()
        {
            lock (this)
            {
                // TODO: can we somehow use IOUtils here...?  problem is
                // we are calling .decRef not .close)...
                try
                {
                    if (reader != null)
                    {
                        //System.out.println("  pool.drop info=" + info + " rc=" + reader.getRefCount());
                        try
                        {
                            reader.DecRef();
                        }
                        finally
                        {
                            reader = null;
                        }
                    }
                }
                finally
                {
                    if (mergeReader != null)
                    {
                        //System.out.println("  pool.drop info=" + info + " merge rc=" + mergeReader.getRefCount());
                        try
                        {
                            mergeReader.DecRef();
                        }
                        finally
                        {
                            mergeReader = null;
                        }
                    }
                }
                DecRef();
            }
        }

        /// <summary>Returns a ref to a clone.</summary>
        /// <remarks>
        /// Returns a ref to a clone. NOTE: you should decRef() the reader when you're
        /// dont (ie do not call close()).
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual SegmentReader GetReadOnlyClone(IOContext context)
        {
            lock (this)
            {
                if (reader == null)
                {
                    reader = GetReader(context);
                    reader.DecRef();
                }
                //HM:revisit 
                //assert reader != null;
                liveDocsShared = true;
                if (liveDocs != null)
                {
                    return new SegmentReader(reader.SegmentInfo, reader, liveDocs, Info.info.DocCount - Info.DelCount - pendingDeleteCount);
                }
                //HM:revisit 
                //assert reader.getLiveDocs() == liveDocs;
                reader.IncRef();
                return reader;
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public virtual void InitWritableLiveDocs()
        {
            lock (this)
            {
                //HM:revisit 
                //assert Thread.holdsLock(writer);
                //HM:revisit 
                //assert info.info.getDocCount() > 0;
                //System.out.println("initWritableLivedocs seg=" + info + " liveDocs=" + liveDocs + " shared=" + shared);
                if (liveDocsShared)
                {
                    // Copy on write: this means we've cloned a
                    // SegmentReader sharing the current liveDocs
                    // instance; must now make a private clone so we can
                    // change it:
                    LiveDocsFormat liveDocsFormat = Info.info.Codec.LiveDocsFormat;
                    if (liveDocs == null)
                    {
                        //System.out.println("create BV seg=" + info);
                        liveDocs = liveDocsFormat.NewLiveDocs(Info.info.DocCount);
                    }
                    else
                    {
                        liveDocs = liveDocsFormat.NewLiveDocs(liveDocs);
                    }
                    liveDocsShared = false;
                }
            }
        }

        public virtual IBits LiveDocs
        {
            get
            {
                lock (this)
                {
                    //HM:revisit 
                    //assert Thread.holdsLock(writer);
                    return liveDocs;
                }
            }
        }

        public virtual IBits ReadOnlyLiveDocs
        {
            get
            {
                lock (this)
                {
                    //System.out.println("getROLiveDocs seg=" + info);
                    //HM:revisit 
                    //assert Thread.holdsLock(writer);
                    liveDocsShared = true;
                    //if (liveDocs != null) {
                    //System.out.println("  liveCount=" + liveDocs.count());
                    //}
                    return liveDocs;
                }
            }
        }

        public virtual void DropChanges()
        {
            lock (this)
            {
                // Discard (don't save) changes when we are dropping
                // the reader; this is used only on the sub-readers
                // after a successful merge.  If deletes had
                // accumulated on those sub-readers while the merge
                // is running, by now we have carried forward those
                // deletes onto the newly merged segment, so we can
                // discard them on the sub-readers:
                pendingDeleteCount = 0;
                DropMergingUpdates();
            }
        }

        // Commit live docs (writes new _X_N.del files) and field updates (writes new
        // _X_N updates files) to the directory; returns true if it wrote any file
        // and false if there were no new deletes or updates to write:
        // TODO (DVU_RENAME) to writeDeletesAndUpdates
        /// <exception cref="System.IO.IOException"></exception>
        public virtual bool WriteLiveDocs(Directory dir)
        {
            lock (this)
            {
                //HM:revisit 
                //assert Thread.holdsLock(writer);
                //System.out.println("rld.writeLiveDocs seg=" + info + " pendingDelCount=" + pendingDeleteCount + " numericUpdates=" + numericUpdates);
                if (pendingDeleteCount == 0)
                {
                    return false;
                }
                // We have new deletes
                //HM:revisit 
                //assert liveDocs.length() == info.info.getDocCount();
                // Do this so we can delete any created files on
                // exception; this saves all codecs from having to do
                // it:
                TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(dir);
                // We can write directly to the actual name (vs to a
                // .tmp & renaming it) because the file is not live
                // until segments file is written:
                bool success = false;
                try
                {
                    Codec codec = Info.info.Codec;
                    codec.LiveDocsFormat.WriteLiveDocs((IMutableBits)liveDocs, trackingDir, Info, pendingDeleteCount
                        , IOContext.DEFAULT);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Advance only the nextWriteDelGen so that a 2nd
                        // attempt to write will write to a new file
                        Info.AdvanceNextWriteDelGen();
                        // Delete any partially created file(s):
                        foreach (string fileName in trackingDir.CreatedFiles)
                        {
                            try
                            {
                                dir.DeleteFile(fileName);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                // Ignore so we throw only the first exc
                // If we hit an exc in the line above (eg disk full)
                // then info's delGen remains pointing to the previous
                // (successfully written) del docs:
                Info.AdvanceDelGen();
                Info.DelCount = Info.DelCount + pendingDeleteCount;
                pendingDeleteCount = 0;
                return true;
            }
        }

        // Writes field updates (new _X_N updates files) to the directory
        /// <exception cref="System.IO.IOException"></exception>
        public virtual void WriteFieldUpdates(Directory dir, DocValuesFieldUpdates.Container
             dvUpdates)
        {
            lock (this)
            {
                //HM:revisit 
                //assert Thread.holdsLock(writer);
                //System.out.println("rld.writeFieldUpdates: seg=" + info + " numericFieldUpdates=" + numericFieldUpdates);
                //HM:revisit 
                //assert dvUpdates.any();
                // Do this so we can delete any created files on
                // exception; this saves all codecs from having to do
                // it:
                TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(dir);
                FieldInfos fieldInfos = null;
                bool success = false;
                try
                {
                    Codec codec = Info.info.Codec;
                    // reader could be null e.g. for a just merged segment (from
                    // IndexWriter.commitMergedDeletes).
                    SegmentReader segReader = this.reader ?? new SegmentReader(Info, writer.Config.ReaderTermsIndexDivisor, 
                        IOContext.READONCE);
                    try
                    {
                        // clone FieldInfos so that we can update their dvGen separately from
                        // the reader's infos and write them to a new fieldInfos_gen file
                        FieldInfos.Builder builder = new FieldInfos.Builder(writer.globalFieldNumberMap);
                        // cannot use builder.add(reader.getFieldInfos()) because it does not
                        // clone FI.attributes as well FI.dvGen
                        foreach (FieldInfo fi in segReader.FieldInfos)
                        {
                            FieldInfo clone = builder.Add(fi);
                            // copy the stuff FieldInfos.Builder doesn't copy
                            if (fi.Attributes != null)
                            {
                                foreach (KeyValuePair<string, string> e in fi.Attributes)
                                {
                                    clone.PutAttribute(e.Key, e.Value);
                                }
                            }
                            clone.DocValuesGen = fi.DocValuesGen;
                        }
                        // create new fields or update existing ones to have NumericDV type
                        foreach (string f in dvUpdates.numericDVUpdates.Keys)
                        {
                            builder.AddOrUpdate(f, NumericDocValuesField.TYPE);
                        }
                        // create new fields or update existing ones to have BinaryDV type
                        foreach (string s in dvUpdates.binaryDVUpdates.Keys)
                        {
                            builder.AddOrUpdate(s, BinaryDocValuesField.TYPE);
                        }
                        fieldInfos = builder.Finish();
                        long nextFieldInfosGen = Info.NextFieldInfosGen;
                        string segmentSuffix = Convert.ToString(nextFieldInfosGen, Character.MAX_RADIX);
                        SegmentWriteState state = new SegmentWriteState(null, trackingDir, Info.info, fieldInfos
                            , writer.Config.TermIndexInterval, null, IOContext.DEFAULT, segmentSuffix
                            );
                        DocValuesFormat docValuesFormat = codec.DocValuesFormat;
                        DocValuesConsumer fieldsConsumer = docValuesFormat.FieldsConsumer(state);
                        bool fieldsConsumerSuccess = false;
                        try
                        {
                            //          System.out.println("[" + Thread.currentThread().getName() + "] RLD.writeFieldUpdates: applying numeric updates; seg=" + info + " updates=" + numericFieldUpdates);
                            foreach (KeyValuePair<string, NumericDocValuesFieldUpdates> e in dvUpdates.numericDVUpdates)
                            {
                                string field = e.Key;
                                NumericDocValuesFieldUpdates fieldUpdates = e.Value;
                                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                                //HM:revisit 
                                //assert fieldInfo != null;
                                fieldInfo.DocValuesGen = nextFieldInfosGen;
                                // write the numeric updates to a new gen'd docvalues file
                                fieldsConsumer.AddNumericField(fieldInfo, new AnonymousEnumerableNumber(this, segReader, field,
                                    fieldUpdates));
                            }
                            // this document has an updated value
                            // either null (unset value) or updated value
                            // prepare for next round
                            // no update for this document
                            //HM:revisit 
                            //assert curDoc < updateDoc;
                            // only read the current value if the document had a value before
                            //        System.out.println("[" + Thread.currentThread().getName() + "] RAU.writeFieldUpdates: applying binary updates; seg=" + info + " updates=" + dvUpdates.binaryDVUpdates);
                            foreach (KeyValuePair<string, BinaryDocValuesFieldUpdates> e_1 in dvUpdates.binaryDVUpdates)
                            {
                                string field = e_1.Key;
                                BinaryDocValuesFieldUpdates dvFieldUpdates = e_1.Value;
                                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                                //HM:revisit 
                                //assert fieldInfo != null;
                                //          System.out.println("[" + Thread.currentThread().getName() + "] RAU.writeFieldUpdates: applying binary updates; seg=" + info + " f=" + dvFieldUpdates + ", updates=" + dvFieldUpdates);
                                fieldInfo.DocValuesGen = nextFieldInfosGen;
                                // write the numeric updates to a new gen'd docvalues file
                                fieldsConsumer.AddBinaryField(fieldInfo, new AnonymousBytesRefEnumerable(this, segReader, field, dvFieldUpdates
                                    ));
                            }
                            // this document has an updated value
                            // either null (unset value) or updated value
                            // prepare for next round
                            // no update for this document
                            //HM:revisit 
                            //assert curDoc < updateDoc;
                            // only read the current value if the document had a value before
                            codec.FieldInfosFormat.FieldInfosWriter.Write(trackingDir, Info.info.name,
                                segmentSuffix, fieldInfos, IOContext.DEFAULT);
                            fieldsConsumerSuccess = true;
                        }
                        finally
                        {
                            if (fieldsConsumerSuccess)
                            {
                                fieldsConsumer.Dispose();
                            }
                            else
                            {
                                IOUtils.CloseWhileHandlingException((IDisposable)fieldsConsumer);
                            }
                        }
                    }
                    finally
                    {
                        if (segReader != this.reader)
                        {
                            //          System.out.println("[" + Thread.currentThread().getName() + "] RLD.writeLiveDocs: closeReader " + reader);
                            segReader.Dispose();
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Advance only the nextWriteDocValuesGen so that a 2nd
                        // attempt to write will write to a new file
                        Info.AdvanceNextWriteFieldInfosGen();
                        // Delete any partially created file(s):
                        foreach (string fileName in trackingDir.CreatedFiles)
                        {
                            try
                            {
                                dir.DeleteFile(fileName);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                // Ignore so we throw only the first exc
                Info.AdvanceFieldInfosGen();
                // copy all the updates to mergingUpdates, so they can later be applied to the merged segment
                if (isMerging)
                {
                    foreach (KeyValuePair<string, NumericDocValuesFieldUpdates> e in dvUpdates.numericDVUpdates)
                    {
                        DocValuesFieldUpdates updates = mergingDVUpdates[e.Key];
                        if (updates == null)
                        {
                            mergingDVUpdates[e.Key] = e.Value;
                        }
                        else
                        {
                            updates.Merge(e.Value);
                        }
                    }
                    foreach (KeyValuePair<string, BinaryDocValuesFieldUpdates> kv in dvUpdates.binaryDVUpdates)
                    {
                        DocValuesFieldUpdates updates = mergingDVUpdates[kv.Key];
                        if (updates == null)
                        {
                            mergingDVUpdates[kv.Key] = kv.Value;
                        }
                        else
                        {
                            updates.Merge(kv.Value);
                        }
                    }
                }
                // create a new map, keeping only the gens that are in use
                IDictionary<long, ICollection<string>> genUpdatesFiles = Info.GetUpdatesFiles();
                IDictionary<long, ICollection<string>> newGenUpdatesFiles = new Dictionary<long,
                    ICollection<string>>();
                long fieldInfosGen = Info.FieldInfosGen;
                foreach (FieldInfo fi1 in fieldInfos)
                {
                    long dvGen = fi1.DocValuesGen;
                    if (dvGen != -1 && !newGenUpdatesFiles.ContainsKey(dvGen))
                    {
                        if (dvGen == fieldInfosGen)
                        {
                            newGenUpdatesFiles[fieldInfosGen] = trackingDir.CreatedFiles;
                        }
                        else
                        {
                            newGenUpdatesFiles[dvGen] = genUpdatesFiles[dvGen];
                        }
                    }
                }
                Info.SetGenUpdatesFiles(newGenUpdatesFiles);
                // wrote new files, should checkpoint()
                writer.Checkpoint();
                // if there is a reader open, reopen it to reflect the updates
                if (reader != null)
                {
                    SegmentReader newReader = new SegmentReader(Info, reader, liveDocs, Info.info.DocCount - Info.DelCount - pendingDeleteCount);
                    bool reopened = false;
                    try
                    {
                        reader.DecRef();
                        reader = newReader;
                        reopened = true;
                    }
                    finally
                    {
                        if (!reopened)
                        {
                            newReader.DecRef();
                        }
                    }
                }
            }
        }

        private sealed class AnonymousEnumerableNumber : IEnumerable<long?>
        {
            public AnonymousEnumerableNumber(ReadersAndUpdates _enclosing, SegmentReader reader, string field
                , NumericDocValuesFieldUpdates fieldUpdates)
            {
                this._enclosing = _enclosing;
                this.reader = reader;
                this.field = field;
                this.fieldUpdates = fieldUpdates;
                this.currentValues = reader.GetNumericDocValues(field);
                this.docsWithField = reader.GetDocsWithField(field);
                this.maxDoc = reader.MaxDoc;
                this.updatesIter = ((NumericDocValuesFieldUpdates.Iterator)fieldUpdates.GetIterator());
            }

            internal readonly NumericDocValues currentValues;

            internal readonly IBits docsWithField;

            internal readonly int maxDoc;

            internal readonly NumericDocValuesFieldUpdates.Iterator updatesIter;

            public IEnumerator<long?> GetEnumerator()
            {
                this.updatesIter.Reset();
                return new AnonymousNumberEnumerator(this);
            }

            private sealed class AnonymousNumberEnumerator : IEnumerator<long?>
            {
                public AnonymousNumberEnumerator(AnonymousEnumerableNumber _enclosing)
                {
                    this._enclosing = _enclosing;
                    this.curDoc = -1;
                    this.updateDoc = this._enclosing.updatesIter.NextDoc();
                }

                internal int curDoc;

                internal int updateDoc;

                public bool MoveNext()
                {
                    return this.curDoc < this._enclosing.maxDoc - 1;
                }

                public long? Current
                {
                    get
                    {
                        if (++this.curDoc >= this._enclosing.maxDoc)
                        {
                            //throw new NoSuchElementException("no more documents to return values for");
                            throw new Exception("no more documents to return values for");
                        }
                        if (this.curDoc == this.updateDoc)
                        {
                            long value = ((long) this._enclosing.updatesIter.Value());
                            this.updateDoc = this._enclosing.updatesIter.NextDoc();
                            return value;
                        }
                        if (this._enclosing.currentValues != null && this._enclosing.docsWithField[this.curDoc])
                        {
                            return this._enclosing.currentValues.Get(this.curDoc);
                        }
                        return 0; //is 0 an acceptable value?
                    }
                }

                public void Reset()
                {
                    throw new NotSupportedException("this iterator does not support removing elements");
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                private readonly AnonymousEnumerableNumber _enclosing;
                public void Dispose()
                {
                    //Nothing to dispose
                }
            }

            private readonly ReadersAndUpdates _enclosing;

            private readonly SegmentReader reader;

            private readonly string field;

            private readonly NumericDocValuesFieldUpdates fieldUpdates;
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class AnonymousBytesRefEnumerable : IEnumerable<BytesRef>
        {
            public AnonymousBytesRefEnumerable(ReadersAndUpdates enclosing, SegmentReader reader, string field
                , BinaryDocValuesFieldUpdates dvFieldUpdates)
            {
                this.currentValues = reader.GetBinaryDocValues(field);
                this.docsWithField = reader.GetDocsWithField(field);
                this.maxDoc = reader.MaxDoc;
                this.updatesIter = ((BinaryDocValuesFieldUpdates.Iterator)dvFieldUpdates.GetIterator());
            }

            internal readonly BinaryDocValues currentValues;

            internal readonly IBits docsWithField;

            internal readonly int maxDoc;

            internal readonly BinaryDocValuesFieldUpdates.Iterator updatesIter;

            public IEnumerator<BytesRef> GetEnumerator()
            {
                this.updatesIter.Reset();
                return new AnonymousBytesRefEnumerator(this);
            }

            private sealed class AnonymousBytesRefEnumerator : IEnumerator<BytesRef>
            {
                public AnonymousBytesRefEnumerator(AnonymousBytesRefEnumerable _enclosing)
                {
                    this._enclosing = _enclosing;
                    this.curDoc = -1;
                    this.updateDoc = this._enclosing.updatesIter.NextDoc();
                    this.scratch = new BytesRef();
                }

                internal int curDoc;

                internal int updateDoc;

                internal BytesRef scratch;

                public bool MoveNext()
                {
                    return this.curDoc < this._enclosing.maxDoc - 1;
                }

                public BytesRef Current
                {
                    get
                    {
                        if (++this.curDoc >= this._enclosing.maxDoc)
                        {
                            throw new Exception("no more documents to return values for");
                            //throw new NoSuchElementException("no more documents to return values for");
                        }
                        if (this.curDoc == this.updateDoc)
                        {
                            BytesRef value = ((BytesRef) this._enclosing.updatesIter.Value());
                            this.updateDoc = this._enclosing.updatesIter.NextDoc();
                            return value;
                        }
                        if (this._enclosing.currentValues != null && this._enclosing.docsWithField[this.curDoc])
                        {
                            this._enclosing.currentValues.Get(this.curDoc, this.scratch);
                            return this.scratch;
                        }
                        return null;
                    }
                }

                public void Reset()
                {
                    throw new NotSupportedException("this iterator does not support removing elements");
                }

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                private readonly AnonymousBytesRefEnumerable _enclosing;
                public void Dispose()
                {
                    
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>Returns a reader for merge.</summary>
        /// <remarks>
        /// Returns a reader for merge. This method applies field updates if there are
        /// any and marks that this segment is currently merging.
        /// </remarks>
        /// <exception cref="System.IO.IOException"></exception>
        internal virtual SegmentReader GetReaderForMerge(IOContext context)
        {
            lock (this)
            {
                //HM:revisit 
                //assert Thread.holdsLock(writer);
                // must execute these two statements as atomic operation, otherwise we
                // could lose updates if e.g. another thread calls writeFieldUpdates in
                // between, or the updates are applied to the obtained reader, but then
                // re-applied in IW.commitMergedDeletes (unnecessary work and potential
                // bugs).
                isMerging = true;
                return GetReader(context);
            }
        }

        /// <summary>Drops all merging updates.</summary>
        /// <remarks>
        /// Drops all merging updates. Called from IndexWriter after this segment
        /// finished merging (whether successfully or not).
        /// </remarks>
        public virtual void DropMergingUpdates()
        {
            lock (this)
            {
                mergingDVUpdates.Clear();
                isMerging = false;
            }
        }

        /// <summary>Returns updates that came in while this segment was merging.</summary>
        /// <remarks>Returns updates that came in while this segment was merging.</remarks>
        public virtual IDictionary<string, DocValuesFieldUpdates> GetMergingFieldUpdates(
            )
        {
            lock (this)
            {
                return mergingDVUpdates;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ReadersAndLiveDocs(seg=").Append(Info);
            sb.Append(" pendingDeleteCount=").Append(pendingDeleteCount);
            sb.Append(" liveDocsShared=").Append(liveDocsShared);
            return sb.ToString();
        }
    }
}
