/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;
using Lucene.Net.Store;
using Lucene.Net.Codecs;

namespace Lucene.Net.Index
{
    public sealed class SegmentReader : AtomicReader
    {
		private readonly SegmentCommitInfo si;
        private readonly IBits liveDocs;

        // Normally set to si.docCount - si.delDocCount, unless we
        // were created as an NRT reader from IW, in which case IW
        // tells us the docCount:
        private readonly int numDocs;

        public readonly SegmentCoreReaders core;

		internal readonly SegmentDocValues segDocValues;
		private sealed class AnonymousCTLDictionary1 : CloseableThreadLocal<IDictionary<string, object>>
		{
		    // Normally set to si.docCount - si.delDocCount, unless we
			// were created as an NRT reader from IW, in which case IW
			// tells us the docCount:
		    public override IDictionary<string, object> InitialValue()
			{
				return new Dictionary<string, object>();
			}
		}
		internal readonly CloseableThreadLocal<IDictionary<string, object>> docValuesLocal = new AnonymousCTLDictionary1();
		private sealed class AnonymousCTLBits : CloseableThreadLocal<IDictionary<string, IBits>>
		{
		    public override IDictionary<string, IBits> InitialValue()
			{
				return new Dictionary<string, IBits>();
			}
		}
		internal readonly CloseableThreadLocal<IDictionary<string, IBits>> docsWithFieldLocal = new AnonymousCTLBits();
		internal readonly IDictionary<string, DocValuesProducer> dvProducersByField = new HashMap<string, DocValuesProducer>();
        internal readonly ICollection<DocValuesProducer> dvProducers = new List<DocValuesProducer>();
		internal readonly FieldInfos fieldInfos;
		private readonly IList<long> dvGens = new List<long>();
        public SegmentReader(SegmentCommitInfo si, int termInfosIndexDivisor, IOContext context)
        {
            this.si = si;
			fieldInfos = ReadFieldInfos(si);
            core = new SegmentCoreReaders(this, si.info.dir, si, context, termInfosIndexDivisor);
			segDocValues = new SegmentDocValues();
            bool success = false;
			Codec codec = si.info.Codec;
            try
            {
                if (si.HasDeletions)
                {
                    // NOTE: the bitvector is stored using the regular directory, not cfs
					liveDocs = codec.LiveDocsFormat.ReadLiveDocs(Directory, si, IOContext.READONCE);
                }
                else
                {
                    //assert si.getDelCount() == 0;
                    liveDocs = null;
                }
                numDocs = si.info.DocCount - si.DelCount;
				if (fieldInfos.HasDocValues)
				{
					InitDocValuesProducers(codec);
				}
                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above.  In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
					DoClose();
                }
            }
        }

		/// <summary>
		/// Create new SegmentReader sharing core from a previous
		/// SegmentReader and loading new live docs from a new
		/// deletes file.
		/// </summary>
		internal SegmentReader(SegmentCommitInfo si, SegmentReader
			 sr) : this(si, sr, si.info.Codec.LiveDocsFormat.ReadLiveDocs(si.info.dir
			, si, IOContext.READONCE), si.info.DocCount - si.DelCount)
		{
		}

		internal SegmentReader(SegmentCommitInfo si, SegmentReader
			 sr, IBits liveDocs, int numDocs)
        {
            this.si = si;

            //assert liveDocs != null;
            this.liveDocs = liveDocs;

            this.numDocs = numDocs;
			this.core = sr.core;
			core.IncRef();
			this.segDocValues = sr.segDocValues;
			bool success = false;
			try
			{
				Codec codec = si.info.Codec;
				if (si.FieldInfosGen == -1)
				{
					fieldInfos = sr.fieldInfos;
				}
				else
				{
					fieldInfos = ReadFieldInfos(si);
				}
				if (fieldInfos.HasDocValues)
				{
					InitDocValuesProducers(codec);
				}
				success = true;
			}
			finally
			{
				if (!success)
				{
					DoClose();
				}
			}
        }

		private void InitDocValuesProducers(Codec codec)
		{
			Directory dir = core.cfsReader ?? si.info.dir;
			DocValuesFormat dvFormat = codec.DocValuesFormat;
			IDictionary<long, IList<FieldInfo>> genInfos = GetGenInfos();
			//      System.out.println("[" + Thread.currentThread().getName() + "] SR.initDocValuesProducers: segInfo=" + si + "; gens=" + genInfos.keySet());
			// TODO: can we avoid iterating over fieldinfos several times and creating maps of all this stuff if dv updates do not exist?
			foreach (KeyValuePair<long, IList<FieldInfo>> e in genInfos)
			{
				long gen = e.Key;
				IList<FieldInfo> infos = e.Value;
				DocValuesProducer dvp = segDocValues.GetDocValuesProducer(gen, si, IOContext.READ
					, dir, dvFormat, infos, TermInfosIndexDivisor);
				dvGens.Add(gen);
				foreach (FieldInfo fi in infos)
				{
					dvProducersByField[fi.name] = dvp;
					dvProducers.Add(dvp);
				}
			}
		}
		internal static FieldInfos ReadFieldInfos(SegmentCommitInfo info)
		{
			Directory dir;
			bool closeDir;
			if (info.FieldInfosGen == -1 && info.info.UseCompoundFile)
			{
				// no fieldInfos gen and segment uses a compound file
				dir = new CompoundFileDirectory(info.info.dir, IndexFileNames.SegmentFileName(info
					.info.name, string.Empty, IndexFileNames.COMPOUND_FILE_EXTENSION), IOContext.READONCE
					, false);
				closeDir = true;
			}
			else
			{
				// gen'd FIS are read outside CFS, or the segment doesn't use a compound file
				dir = info.info.dir;
				closeDir = false;
			}
			try
			{
				string segmentSuffix = info.FieldInfosGen == -1 ? string.Empty : System.Convert.ToString
					(info.FieldInfosGen, Character.MAX_RADIX);
				Codec codec = info.info.Codec;
				FieldInfosFormat fisFormat = codec.FieldInfosFormat;
				return fisFormat.FieldInfosReader.Read(dir, info.info.name, segmentSuffix, IOContext
					.READONCE);
			}
			finally
			{
				if (closeDir)
				{
					dir.Dispose();
				}
			}
		}
		private IDictionary<long, IList<FieldInfo>> GetGenInfos()
		{
			IDictionary<long, IList<FieldInfo>> genInfos = new Dictionary<long, IList<FieldInfo>>();
			foreach (FieldInfo fi in fieldInfos)
			{
			    long gen = fi.DocValuesGen;
				IList<FieldInfo> infos = genInfos[gen];
				if (infos == null)
				{
					infos = new List<FieldInfo>();
					genInfos[gen] = infos;
				}
				infos.Add(fi);
			}
			return genInfos;
		}
        public override IBits LiveDocs
        {
            get
            {
                EnsureOpen();
                return liveDocs;
            }
        }

        protected internal override void DoClose()
        {
			try
			{
				core.DecRef();
			}
			finally
			{
				dvProducersByField.Clear();
				try
				{
					IOUtils.Close(docValuesLocal, docsWithFieldLocal);
				}
				finally
				{
					segDocValues.DecRef(dvGens);
				}
			}
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                EnsureOpen();
                return core.fieldInfos;
            }
        }

        public StoredFieldsReader FieldsReader
        {
            get
            {
                EnsureOpen();
                return core.fieldsReaderLocal.Get();
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            CheckBounds(docID);
            FieldsReader.VisitDocument(docID, visitor);
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return core.fields;
            }
        }

        public override int NumDocs
        {
            get { return numDocs; }
        }

        public override int MaxDoc
        {
            get { return si.info.DocCount; }
        }

        public TermVectorsReader TermVectorsReader
        {
            get
            {
                EnsureOpen();
                return core.termVectorsLocal.Get();
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            TermVectorsReader termVectorsReader = this.TermVectorsReader;
            if (termVectorsReader == null)
            {
                return null;
            }
            CheckBounds(docID);
            return termVectorsReader.Get(docID);
        }

        private void CheckBounds(int docID)
        {
            if (docID < 0 || docID >= MaxDoc)
            {
                throw new IndexOutOfRangeException("docID must be >= 0 and < maxDoc=" + MaxDoc + " (got docID=" + docID + ")");
            }
        }

        public override string ToString()
        {
            // SegmentInfo.toString takes dir and number of
            // *pending* deletions; so we reverse compute that here:
            return si.ToString(si.info.dir, si.info.DocCount - numDocs - si.DelCount);
        }

        public string SegmentName
        {
            get
            {
                return si.info.name;
            }
        }

        public SegmentCommitInfo SegmentInfo
        {
            get
            {
                return si;
            }
        }

        public Directory Directory
        {
            get
            {
                // Don't ensureOpen here -- in certain cases, when a
                // cloned/reopened reader needs to commit, it may call
                // this method on the closed original reader
                return si.info.dir;
            }
        }

        public override object CoreCacheKey
        {
            get
            {
                return core;
            }
        }

        public override object CombinedCoreAndDeletesKey
        {
            get
            {
                return this;
            }
        }

        public int TermInfosIndexDivisor
        {
            get
            {
                return core.termsIndexDivisor;
            }
        }

		private FieldInfo GetDVField(string field, FieldInfo.DocValuesType type)
		{
			FieldInfo fi = fieldInfos.FieldInfo(field);
			if (fi == null)
			{
				// Field does not exist
				return null;
			}
		    return fi.GetDocValuesType() != type ? null : fi;
		}
        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
			FieldInfo fi = GetDVField(field, FieldInfo.DocValuesType.NUMERIC);
			if (fi == null)
			{
				return null;
			}
			IDictionary<string, object> dvFields = docValuesLocal.Get();
			NumericDocValues dvs = (NumericDocValues)dvFields[field];
			if (dvs == null)
			{
				DocValuesProducer dvProducer = dvProducersByField[field];
				//HM:revisit 
				//assert dvProducer != null;
				dvs = dvProducer.GetNumeric(fi);
				dvFields[field] = dvs;
			}
			return dvs;
        }

		public override IBits GetDocsWithField(string field)
		{
			EnsureOpen();
			FieldInfo fi = fieldInfos.FieldInfo(field);
			if (fi == null)
			{
				// Field does not exist
				return null;
			}
		    IDictionary<string, IBits> dvFields = docsWithFieldLocal.Get();
		    IBits dvs = dvFields[field];
			if (dvs == null)
			{
				DocValuesProducer dvProducer = dvProducersByField[field];
				//HM:revisit 
				//assert dvProducer != null;
				dvs = dvProducer.GetDocsWithField(fi);
				dvFields[field] = dvs;
			}
			return dvs;
		}
        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
			FieldInfo fi = GetDVField(field, FieldInfo.DocValuesType.BINARY);
			if (fi == null)
			{
				return null;
			}
			IDictionary<string, object> dvFields = docValuesLocal.Get();
			BinaryDocValues dvs = (BinaryDocValues)dvFields[field];
			if (dvs == null)
			{
				DocValuesProducer dvProducer = dvProducersByField[field];
				//HM:revisit 
				//assert dvProducer != null;
				dvs = dvProducer.GetBinary(fi);
				dvFields[field] = dvs;
			}
			return dvs;
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
			FieldInfo fi = GetDVField(field, FieldInfo.DocValuesType.SORTED);
			if (fi == null)
			{
				return null;
			}
			IDictionary<string, object> dvFields = docValuesLocal.Get();
			SortedDocValues dvs = (SortedDocValues)dvFields[field];
			if (dvs == null)
			{
				DocValuesProducer dvProducer = dvProducersByField[field];
				//HM:revisit 
				//assert dvProducer != null;
				dvs = dvProducer.GetSorted(fi);
				dvFields[field] = dvs;
			}
			return dvs;
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
			FieldInfo fi = GetDVField(field, FieldInfo.DocValuesType.SORTED_SET);
			if (fi == null)
			{
				return null;
			}
			IDictionary<string, object> dvFields = docValuesLocal.Get();
			SortedSetDocValues dvs = (SortedSetDocValues)dvFields[field];
			if (dvs == null)
			{
				DocValuesProducer dvProducer = dvProducersByField[field];
				//HM:revisit 
				//assert dvProducer != null;
				dvs = dvProducer.GetSortedSet(fi);
				dvFields[field] = dvs;
			}
			return dvs;
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
			FieldInfo fi = fieldInfos.FieldInfo(field);
			if (fi == null || !fi.HasNorms)
			{
				// Field does not exist or does not index norms
				return null;
			}
            return core.GetNormValues(fi);
        }

        public interface ICoreClosedListener
        {
            /** Invoked when the shared core of the provided {@link
             *  SegmentReader} has closed. */
            void OnClose(object ownerCoreCacheKey);
        }

        public void AddCoreClosedListener(ICoreClosedListener listener)
        {
            EnsureOpen();
            core.AddCoreClosedListener(listener);
        }

        public void RemoveCoreClosedListener(ICoreClosedListener listener)
        {
            EnsureOpen();
            core.RemoveCoreClosedListener(listener);
        }
		public long RamBytesUsed()
		{
			EnsureOpen();
			long ramBytesUsed = 0;
			if (dvProducers != null)
			{
				foreach (DocValuesProducer producer in dvProducers)
				{
					ramBytesUsed += producer.RamBytesUsed;
				}
			}
			if (core != null)
			{
				ramBytesUsed += core.RamBytesUsed;
			}
			return ramBytesUsed;
		}
		public override void CheckIntegrity()
		{
			EnsureOpen();
			// stored fields
			FieldsReader.CheckIntegrity();
			// term vectors
			TermVectorsReader termVectorsReader = TermVectorsReader;
			if (termVectorsReader != null)
			{
				termVectorsReader.CheckIntegrity();
			}
			// terms/postings
			if (core.fields != null)
			{
				core.fields.CheckIntegrity();
			}
			// norms
			if (core.normsProducer != null)
			{
				core.normsProducer.CheckIntegrity();
			}
			// docvalues
			if (dvProducers != null)
			{
				foreach (DocValuesProducer producer in dvProducers)
				{
					producer.CheckIntegrity();
				}
			}
		}
		}
}