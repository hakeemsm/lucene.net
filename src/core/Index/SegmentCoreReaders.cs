using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ICoreClosedListener = Lucene.Net.Index.SegmentReader.ICoreClosedListener;

namespace Lucene.Net.Index
{
    public sealed class SegmentCoreReaders
    {
        // Counts how many other reader share the core objects
        // (freqStream, proxStream, tis, etc.) of this reader;
        // when coreRef drops to 0, these core objects may be
        // closed.  A given instance of SegmentReader may be
        // closed, even those it shares core objects with other
        // SegmentReaders:
        private int ref_renamed = 1;

        internal readonly FieldInfos fieldInfos;

        internal readonly FieldsProducer fields;

        internal readonly DocValuesProducer normsProducer;

        internal readonly int termsIndexDivisor;


        internal readonly StoredFieldsReader fieldsReaderOrig;
        internal readonly TermVectorsReader termVectorsReaderOrig;
        internal readonly CompoundFileDirectory cfsReader;

        // TODO: make a single thread local w/ a
        // Thingy class holding fieldsReader, termVectorsReader,
        // normsProducer, dvProducer


        private sealed class AnonymousFieldsReaderLocal : CloseableThreadLocal<StoredFieldsReader>
        {
            private readonly SegmentCoreReaders parent;

            public AnonymousFieldsReaderLocal(SegmentCoreReaders parent)
            {
                this.parent = parent;
            }

            public override StoredFieldsReader InitialValue()
            {
                return (StoredFieldsReader)parent.fieldsReaderOrig.Clone();
            }
        }

        internal readonly CloseableThreadLocal<StoredFieldsReader> fieldsReaderLocal;
        internal readonly CloseableThreadLocal<TermVectorsReader> termVectorsLocal;

        private sealed class AnonymousTermVectorsLocal : CloseableThreadLocal<TermVectorsReader>
        {
            private readonly SegmentCoreReaders parent;

            public AnonymousTermVectorsLocal(SegmentCoreReaders parent)
            {
                this.parent = parent;
            }

            public override TermVectorsReader InitialValue()
            {
                return (parent.termVectorsReaderOrig == null) ? null : (TermVectorsReader)parent.termVectorsReaderOrig.Clone();
            }
        }

        internal readonly CloseableThreadLocal<IDictionary<string, object>> docValuesLocal = new AnonymousDocValuesLocal();

        private sealed class AnonymousDocValuesLocal : CloseableThreadLocal<IDictionary<string, object>>
        {
            public override IDictionary<string, object> InitialValue()
            {
                return new HashMap<string, object>();
            }
        }

        internal readonly CloseableThreadLocal<IDictionary<string, object>> normsLocal = new AnonymousDocValuesLocal();

        private readonly ISet<ICoreClosedListener> coreClosedListeners = new ConcurrentHashSet<ICoreClosedListener>(new IdentityComparer<ICoreClosedListener>());

        public SegmentCoreReaders(SegmentReader owner, Directory dir, SegmentCommitInfo si, IOContext context, int termsIndexDivisor)
        {
            // .NET Port: These lines are necessary as we can't use "this" inline above
            fieldsReaderLocal = new AnonymousFieldsReaderLocal(this);
            termVectorsLocal = new AnonymousTermVectorsLocal(this);

            if (termsIndexDivisor == 0)
            {
                throw new ArgumentException("indexDivisor must be < 0 (don't load terms index) or greater than 0 (got 0)");
            }

            Codec codec = si.info.Codec;
            Directory cfsDir; // confusing name: if (cfs) its the cfsdir, otherwise its the segment's directory.

            bool success = false;

            try
            {
                if (si.info.UseCompoundFile)
                {
                    cfsDir = cfsReader = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(si.info.name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), context, false);
                }
                else
                {
                    cfsReader = null;
                    cfsDir = dir;
                }
                FieldInfos fieldInfos = owner.fieldInfos;

                this.termsIndexDivisor = termsIndexDivisor;
                PostingsFormat format = codec.PostingsFormat;
                SegmentReadState segmentReadState = new SegmentReadState(cfsDir, si.info, fieldInfos, context, termsIndexDivisor);
                // Ask codec for its Fields
                fields = format.FieldsProducer(segmentReadState);
                //assert fields != null;
                // ask codec for its Norms: 
                // TODO: since we don't write any norms file if there are no norms,
                // kinda jaky to assume the codec handles the case of no norms file at all gracefully?!


                if (fieldInfos.HasNorms)
                {
                    normsProducer = codec.NormsFormat.NormsProducer(segmentReadState);
                    //assert normsProducer != null;
                }
                else
                {
                    normsProducer = null;
                }

                fieldsReaderOrig = si.info.Codec.StoredFieldsFormat.FieldsReader(cfsDir, si.info, fieldInfos, context);

                if (fieldInfos.HasVectors)
                { // open term vector files only as needed
                    termVectorsReaderOrig = si.info.Codec.TermVectorsFormat.VectorsReader(cfsDir, si.info, fieldInfos, context);
                }
                else
                {
                    termVectorsReaderOrig = null;
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    DecRef();
                }
            }

            // Must assign this at the end -- if we hit an
            // exception above core, we don't want to attempt to
            // purge the FieldCache (will hit NPE because core is
            // not assigned yet).
        }

        internal int RefCount
        {
            get { return ref_renamed; }
        }
        internal void IncRef()
        {
            int count;
            //could this be done with a simple Increment Op?
            while ((count = ref_renamed) > 0)
            {
                int refOld = Interlocked.CompareExchange(ref ref_renamed, count + 1, count);
                if (refOld != ref_renamed)
                {
                    return;
                }

            }
            throw new AlreadyClosedException("SegmentCoreReaders is already closed");
        }


        internal NumericDocValues GetNormValues(FieldInfo field)
        {
            //HM:revisit 
            //assert normsProducer != null;

            IDictionary<String, Object> normFields = normsLocal.Get();

            NumericDocValues norms = (NumericDocValues)normFields[field.name];
            if (norms == null)
            {
                norms = normsProducer.GetNumeric(field);
                normFields[field.name] = norms;
            }

            return norms;
        }

        internal void DecRef()
        {
            if (Interlocked.Decrement(ref ref_renamed) == 0)
            {
                //      System.err.println("--- closing core readers");
                Exception th = null;
                try
                {
                    IOUtils.Close(termVectorsLocal, fieldsReaderLocal, normsLocal, fields, termVectorsReaderOrig
                        , fieldsReaderOrig, cfsReader, normsProducer);
                }
                catch (Exception throwable)
                {
                    th = throwable;
                }
                finally
                {
                    NotifyCoreClosedListeners(th);
                }
            }
        }

        private void NotifyCoreClosedListeners(Exception th)
        {
            lock (coreClosedListeners)
            {
                foreach (ICoreClosedListener listener in coreClosedListeners)
                {
                    try
                    {
                        listener.OnClose(this);
                    }
                    catch (Exception t)
                    {
                        if (th == null)
                        {
                            th = t;
                        }
                        else
                        {
                            IOUtils.AddSuppressed(th, t);
                        }
                    }
                }
                throw th;
            }
        }

        internal void AddCoreClosedListener(ICoreClosedListener listener)
        {
            coreClosedListeners.Add(listener);
        }

        internal void RemoveCoreClosedListener(ICoreClosedListener listener)
        {
            coreClosedListeners.Remove(listener);
        }
        public long RamBytesUsed
        {
            get
            {
                return ((normsProducer != null) ? normsProducer.RamBytesUsed : 0) + 
                    ((fields != null) ? fields.RamBytesUsed : 0) + 
                    ((fieldsReaderOrig != null) ? fieldsReaderOrig.RamBytesUsed : 0) + 
                    ((termVectorsReaderOrig != null) ? termVectorsReaderOrig.RamBytesUsed : 0);
            }
        }

    }
}
