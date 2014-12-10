//using System.Collections.Generic;
//using Lucene.Net.Analysis;
//using Lucene.Net.Search;
//using Lucene.Net.Store;

//namespace Lucene.Net.Index
//{
//    /// <summary>
//    /// Class that tracks changes to a delegated
//    /// IndexWriter, used by
//    /// <see cref="Lucene.Net.Search.ControlledRealTimeReopenThread{T}">Lucene.Net.Search.ControlledRealTimeReopenThread&lt;T&gt;
//    /// 	</see>
//    /// to ensure specific
//    /// changes are visible.   Create this class (passing your
//    /// IndexWriter), and then pass this class to
//    /// <see cref="Lucene.Net.Search.ControlledRealTimeReopenThread{T}">Lucene.Net.Search.ControlledRealTimeReopenThread&lt;T&gt;
//    /// 	</see>
//    /// .
//    /// Be sure to make all changes via the
//    /// TrackingIndexWriter, otherwise
//    /// <see cref="Lucene.Net.Search.ControlledRealTimeReopenThread{T}">Lucene.Net.Search.ControlledRealTimeReopenThread&lt;T&gt;
//    /// 	</see>
//    /// won't know about the changes.
//    /// </summary>
//    /// <lucene.experimental></lucene.experimental>
//    public class TrackingIndexWriter
//    {
//        private readonly IndexWriter _writer;

//        private long indexingGen = 1;

//        /// <summary>
//        /// Create a
//        /// <code>TrackingIndexWriter</code>
//        /// wrapping the
//        /// provided
//        /// <see cref="IndexWriter">IndexWriter</see>
//        /// .
//        /// </summary>
//        public TrackingIndexWriter(IndexWriter writer)
//        {
//            // javadocs
//            this._writer = writer;
//        }

		
//        public virtual long UpdateDocument<T>(Term t, IEnumerable<T> d, Analyzer a) where T:IIndexableField
//        {
//            _writer.UpdateDocument(t, d, a);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.UpdateDocument(Term, Sharpen.Iterable{T})">IndexWriter.UpdateDocument(Term, Sharpen.Iterable&lt;T&gt;)
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long UpdateDocument<_T0>(Term t, Iterable<_T0> d) where _T0:IndexableField
//        {
//            _writer.UpdateDocument(t, d);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.UpdateDocuments(Term, Sharpen.Iterable{T}, Lucene.Net.Analysis.Analyzer)
//        /// 	">IndexWriter.UpdateDocuments(Term, Sharpen.Iterable&lt;T&gt;, Lucene.Net.Analysis.Analyzer)
//        /// 	</see>
//        /// and returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long UpdateDocuments<_T0>(Term t, Iterable<_T0> docs, Analyzer a) where 
//            _T0:Iterable<IndexableField>
//        {
//            _writer.UpdateDocuments(t, docs, a);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.UpdateDocuments(Term, Sharpen.Iterable{T})">IndexWriter.UpdateDocuments(Term, Sharpen.Iterable&lt;T&gt;)
//        /// 	</see>
//        /// and returns
//        /// the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long UpdateDocuments<_T0>(Term t, Iterable<_T0> docs) where _T0:Iterable
//            <IndexableField>
//        {
//            _writer.UpdateDocuments(t, docs);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.DeleteDocuments(Term)">IndexWriter.DeleteDocuments(Term)</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long DeleteDocuments(Term t)
//        {
//            _writer.DeleteDocuments(t);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.DeleteDocuments(Term[])">IndexWriter.DeleteDocuments(Term[])
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long DeleteDocuments(params Term[] terms)
//        {
//            _writer.DeleteDocuments(terms);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.DeleteDocuments(Lucene.Net.Search.Query)">IndexWriter.DeleteDocuments(Lucene.Net.Search.Query)
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long DeleteDocuments(Query q)
//        {
//            _writer.DeleteDocuments(q);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.DeleteDocuments(Lucene.Net.Search.Query[])">IndexWriter.DeleteDocuments(Lucene.Net.Search.Query[])
//        /// 	</see>
//        /// and returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long DeleteDocuments(params Query[] queries)
//        {
//            _writer.DeleteDocuments(queries);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.DeleteAll()">IndexWriter.DeleteAll()</see>
//        /// and returns the
//        /// generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long DeleteAll()
//        {
//            _writer.DeleteAll();
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.AddDocument(Sharpen.Iterable{T}, Lucene.Net.Analysis.Analyzer)
//        /// 	">IndexWriter.AddDocument(Sharpen.Iterable&lt;T&gt;, Lucene.Net.Analysis.Analyzer)
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long AddDocument<_T0>(Iterable<_T0> d, Analyzer a) where _T0:IndexableField
//        {
//            _writer.AddDocument(d, a);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.AddDocuments(Sharpen.Iterable{T}, Lucene.Net.Analysis.Analyzer)
//        /// 	">IndexWriter.AddDocuments(Sharpen.Iterable&lt;T&gt;, Lucene.Net.Analysis.Analyzer)
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long AddDocuments<_T0>(Iterable<_T0> docs, Analyzer a) where _T0:Iterable
//            <IndexableField>
//        {
//            _writer.AddDocuments(docs, a);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.AddDocument(Sharpen.Iterable{T})">IndexWriter.AddDocument(Sharpen.Iterable&lt;T&gt;)
//        /// 	</see>
//        /// and returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long AddDocument<_T0>(Iterable<_T0> d) where _T0:IndexableField
//        {
//            _writer.AddDocument(d);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.AddDocuments(Sharpen.Iterable{T})">IndexWriter.AddDocuments(Sharpen.Iterable&lt;T&gt;)
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long AddDocuments<_T0>(Iterable<_T0> docs) where _T0:Iterable<IndexableField
//            >
//        {
//            _writer.AddDocuments(docs);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.AddIndexes(Lucene.Net.Store.Directory[])">IndexWriter.AddIndexes(Lucene.Net.Store.Directory[])
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long AddIndexes(params Directory[] dirs)
//        {
//            _writer.AddIndexes(dirs);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Calls
//        /// <see cref="IndexWriter.AddIndexes(IndexReader[])">IndexWriter.AddIndexes(IndexReader[])
//        /// 	</see>
//        /// and returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long AddIndexes(params IndexReader[] readers)
//        {
//            _writer.AddIndexes(readers);
//            // Return gen as of when indexing finished:
//            return indexingGen.Get();
//        }

//        /// <summary>Return the current generation being indexed.</summary>
//        /// <remarks>Return the current generation being indexed.</remarks>
//        public virtual long GetGeneration()
//        {
//            return indexingGen.Get();
//        }

//        /// <summary>
//        /// Return the wrapped
//        /// <see cref="IndexWriter">IndexWriter</see>
//        /// .
//        /// </summary>
//        public virtual IndexWriter GetIndexWriter()
//        {
//            return _writer;
//        }

//        /// <summary>Return and increment current gen.</summary>
//        /// <remarks>Return and increment current gen.</remarks>
//        /// <lucene.internal></lucene.internal>
//        public virtual long GetAndIncrementGeneration()
//        {
//            return indexingGen.GetAndIncrement();
//        }

//        /// <summary>
//        /// Cals
//        /// <see cref="IndexWriter.TryDeleteDocument(IndexReader, int)">IndexWriter.TryDeleteDocument(IndexReader, int)
//        /// 	</see>
//        /// and
//        /// returns the generation that reflects this change.
//        /// </summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        public virtual long TryDeleteDocument(IndexReader reader, int docID)
//        {
//            if (_writer.TryDeleteDocument(reader, docID))
//            {
//                return indexingGen.Get();
//            }
//            else
//            {
//                return -1;
//            }
//        }
//    }
//}
