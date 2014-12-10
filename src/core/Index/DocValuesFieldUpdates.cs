using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
	/// <summary>Holds updates of a single DocValues field, for a set of documents.</summary>
	/// <remarks>Holds updates of a single DocValues field, for a set of documents.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class DocValuesFieldUpdates
	{
	    public enum Type
		{
			NUMERIC,
			BINARY
		}

		/// <summary>An iterator over documents and their updated values.</summary>
		/// <remarks>
		/// An iterator over documents and their updated values. Only documents with
		/// updates are returned by this iterator, and the documents are returned in
		/// increasing order.
		/// </remarks>
		internal abstract class Iterator
		{
			/// <summary>
			/// Returns the next document which has an update, or
			/// <see cref="Lucene.Net.Search.DocIdSetIterator.NO_MORE_DOCS">Lucene.Net.Search.DocIdSetIterator.NO_MORE_DOCS
			/// 	</see>
			/// if there are no more documents to
			/// return.
			/// </summary>
			internal abstract int NextDoc();

			/// <summary>Returns the current document this iterator is on.</summary>
			/// <remarks>Returns the current document this iterator is on.</remarks>
			internal abstract int Doc();

			/// <summary>
			/// Returns the value of the document returned from
			/// <see cref="NextDoc()">NextDoc()</see>
			/// . A
			/// <code>null</code>
			/// value means that it was unset for this document.
			/// </summary>
			internal abstract object Value();

			/// <summary>Reset the iterator's state.</summary>
			/// <remarks>
			/// Reset the iterator's state. Should be called before
			/// <see cref="NextDoc()">NextDoc()</see>
			/// and
			/// <see cref="Value()">Value()</see>
			/// .
			/// </remarks>
			internal abstract void Reset();
		}

		internal class Container
		{
			internal readonly IDictionary<string, NumericDocValuesFieldUpdates> numericDVUpdates
				 = new Dictionary<string, NumericDocValuesFieldUpdates>();

			internal readonly IDictionary<string, BinaryDocValuesFieldUpdates> binaryDVUpdates
				 = new Dictionary<string, BinaryDocValuesFieldUpdates>();

			internal virtual bool Any()
			{
				foreach (NumericDocValuesFieldUpdates updates in numericDVUpdates.Values)
				{
					if (updates.Any())
					{
						return true;
					}
				}
				foreach (BinaryDocValuesFieldUpdates updates2 in binaryDVUpdates.Values)
				{
					if (updates2.Any())
					{
						return true;
					}
				}
				return false;
			}

			internal virtual int Size()
			{
				return numericDVUpdates.Count + binaryDVUpdates.Count;
			}

			internal virtual DocValuesFieldUpdates GetUpdates(string field, Type type)
			{
				switch (type)
				{
					case Type.NUMERIC:
					{
						return numericDVUpdates[field];
					}

					case Type.BINARY:
					{
						return binaryDVUpdates[field];
					}

					default:
					{
						throw new ArgumentException("unsupported type: " + type);
					}
				}
			}

			internal virtual DocValuesFieldUpdates NewUpdates(string field, Type type, int maxDoc)
			{
				switch (type)
				{
					case Type.NUMERIC:
					{
						//HM:revisit 
						//assert numericDVUpdates.get(field) == null;
						var numericUpdates = new NumericDocValuesFieldUpdates(field, maxDoc);
						numericDVUpdates[field] = numericUpdates;
						return numericUpdates;
					}

					case Type.BINARY:
					{
						//HM:revisit 
						//assert binaryDVUpdates.get(field) == null;
						var binaryUpdates = new BinaryDocValuesFieldUpdates(field, maxDoc);
						binaryDVUpdates[field] = binaryUpdates;
						return binaryUpdates;
					}

					default:
					{
						throw new ArgumentException("unsupported type: " + type);
					}
				}
			}

			public override string ToString()
			{
				return "numericDVUpdates=" + numericDVUpdates + " binaryDVUpdates=" + binaryDVUpdates;
			}
		}

		internal readonly string field;

		internal readonly DocValuesFieldUpdates.Type type;

		protected internal DocValuesFieldUpdates(string field, DocValuesFieldUpdates.Type
			 type)
		{
			this.field = field;
			this.type = type;
		}

		/// <summary>Add an update to a document.</summary>
		/// <remarks>
		/// Add an update to a document. For unsetting a value you should pass
		/// <code>null</code>
		/// .
		/// </remarks>
		public abstract void Add(int doc, object value);

		/// <summary>
		/// Returns an
		/// <see cref="Iterator">Iterator</see>
		/// over the updated documents and their
		/// values.
		/// </summary>
		internal abstract Iterator GetIterator();

		/// <summary>
		/// Merge with another
		/// <see cref="DocValuesFieldUpdates">DocValuesFieldUpdates</see>
		/// . This is called for a
		/// segment which received updates while it was being merged. The given updates
		/// should override whatever updates are in that instance.
		/// </summary>
		public abstract void Merge(DocValuesFieldUpdates other);

		/// <summary>Returns true if this instance contains any updates.</summary>
		/// <remarks>Returns true if this instance contains any updates.</remarks>
		/// <returns>TODO</returns>
		public abstract bool Any();
	}
}
