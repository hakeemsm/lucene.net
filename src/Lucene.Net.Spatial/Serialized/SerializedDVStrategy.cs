/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.IO;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.IO;
using Com.Spatial4j.Core.Shape;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Spatial.Serialized;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Serialized
{
	/// <summary>A SpatialStrategy based on serializing a Shape stored into BinaryDocValues.
	/// 	</summary>
	/// <remarks>
	/// A SpatialStrategy based on serializing a Shape stored into BinaryDocValues.
	/// This is not at all fast; it's designed to be used in conjuction with another index based
	/// SpatialStrategy that is approximated (like
	/// <see cref="Lucene.Net.Spatial.Prefix.RecursivePrefixTreeStrategy">Lucene.Net.Spatial.Prefix.RecursivePrefixTreeStrategy
	/// 	</see>
	/// )
	/// to add precision or eventually make more specific / advanced calculations on the per-document
	/// geometry.
	/// The serialization uses Spatial4j's
	/// <see cref="Com.Spatial4j.Core.IO.BinaryCodec">Com.Spatial4j.Core.IO.BinaryCodec</see>
	/// .
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SerializedDVStrategy : SpatialStrategy
	{
		/// <summary>A cache heuristic for the buf size based on the last shape size.</summary>
		/// <remarks>A cache heuristic for the buf size based on the last shape size.</remarks>
		private volatile int indexLastBufSize = 8 * 1024;

		/// <summary>Constructs the spatial strategy with its mandatory arguments.</summary>
		/// <remarks>Constructs the spatial strategy with its mandatory arguments.</remarks>
		public SerializedDVStrategy(SpatialContext ctx, string fieldName) : base(ctx, fieldName
			)
		{
		}

		//TODO do we make this non-volatile since it's merely a heuristic?
		//8KB default on first run
		public override Field[] CreateIndexableFields(Com.Spatial4j.Core.Shape.Shape shape
			)
		{
			int bufSize = Math.Max(128, (int)(this.indexLastBufSize * 1.5));
			//50% headroom over last
			ByteArrayOutputStream byteStream = new ByteArrayOutputStream(bufSize);
			BytesRef bytesRef = new BytesRef();
			//receiver of byteStream's bytes
			try
			{
				ctx.GetBinaryCodec().WriteShape(new DataOutputStream(byteStream), shape);
				//this is a hack to avoid redundant byte array copying by byteStream.toByteArray()
				byteStream.WriteTo(new _FilterOutputStream_84(bytesRef, null));
			}
			catch (IOException e)
			{
				throw new RuntimeException(e);
			}
			this.indexLastBufSize = bytesRef.length;
			//cache heuristic
			return new Field[] { new BinaryDocValuesField(GetFieldName(), bytesRef) };
		}

		private sealed class _FilterOutputStream_84 : FilterOutputStream
		{
			public _FilterOutputStream_84(BytesRef bytesRef, OutputStream baseArg1) : base(baseArg1
				)
			{
				this.bytesRef = bytesRef;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(byte[] b, int off, int len)
			{
				bytesRef.bytes = b;
				bytesRef.offset = off;
				bytesRef.length = len;
			}

			private readonly BytesRef bytesRef;
		}

		public override ValueSource MakeDistanceValueSource(Point queryPoint, double multiplier
			)
		{
			//TODO if makeShapeValueSource gets lifted to the top; this could become a generic impl.
			return new DistanceToShapeValueSource(MakeShapeValueSource(), queryPoint, multiplier
				, ctx);
		}

		public override Query MakeQuery(SpatialArgs args)
		{
			throw new NotSupportedException("This strategy can't return a query that operates"
				 + " efficiently. Instead try a Filter or ValueSource.");
		}

		/// <summary>
		/// Returns a Filter that should be used with
		/// <see cref="Lucene.Net.Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY">Lucene.Net.Search.FilteredQuery.QUERY_FIRST_FILTER_STRATEGY
		/// 	</see>
		/// .
		/// Use in another manner is likely to result in an
		/// <see cref="System.NotSupportedException">System.NotSupportedException</see>
		/// to prevent misuse because the filter can't efficiently work via iteration.
		/// </summary>
		public override Filter MakeFilter(SpatialArgs args)
		{
			ValueSource shapeValueSource = MakeShapeValueSource();
			ShapePredicateValueSource predicateValueSource = new ShapePredicateValueSource(shapeValueSource
				, args.GetOperation(), args.GetShape());
			return new SerializedDVStrategy.PredicateValueSourceFilter(predicateValueSource);
		}

		/// <summary>
		/// Provides access to each shape per document as a ValueSource in which
		/// <see cref="Lucene.Net.Queries.Function.FunctionValues.ObjectVal(int)">Lucene.Net.Queries.Function.FunctionValues.ObjectVal(int)
		/// 	</see>
		/// returns a
		/// <see cref="Com.Spatial4j.Core.Shape.Shape">Com.Spatial4j.Core.Shape.Shape</see>
		/// .
		/// </summary>
		public virtual ValueSource MakeShapeValueSource()
		{
			//TODO raise to SpatialStrategy
			return new SerializedDVStrategy.ShapeDocValueSource(GetFieldName(), ctx.GetBinaryCodec
				());
		}

		/// <summary>This filter only supports returning a DocSet with a bits().</summary>
		/// <remarks>
		/// This filter only supports returning a DocSet with a bits(). If you try to grab the
		/// iterator then you'll get an UnsupportedOperationException.
		/// </remarks>
		internal class PredicateValueSourceFilter : Filter
		{
			private readonly ValueSource predicateValueSource;

			public PredicateValueSourceFilter(ValueSource predicateValueSource)
			{
				//we call boolVal(doc)
				this.predicateValueSource = predicateValueSource;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return new _DocIdSet_145(this, context, acceptDocs);
			}

			private sealed class _DocIdSet_145 : DocIdSet
			{
				public _DocIdSet_145(PredicateValueSourceFilter _enclosing, AtomicReaderContext context
					, Bits acceptDocs)
				{
					this._enclosing = _enclosing;
					this.context = context;
					this.acceptDocs = acceptDocs;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocIdSetIterator Iterator()
				{
					throw new NotSupportedException("Iteration is too slow; instead try FilteredQuery.QUERY_FIRST_FILTER_STRATEGY"
						);
				}

				//Note that if you're truly bent on doing this, then see FunctionValues.getRangeScorer
				/// <exception cref="System.IO.IOException"></exception>
				public override Bits Bits()
				{
					//null Map context -- we simply don't have one. That's ok.
					FunctionValues predFuncValues = this._enclosing.predicateValueSource.GetValues(null
						, context);
					return new _Bits_158(acceptDocs, predFuncValues, context);
				}

				private sealed class _Bits_158 : Bits
				{
					public _Bits_158(Bits acceptDocs, FunctionValues predFuncValues, AtomicReaderContext
						 context)
					{
						this.acceptDocs = acceptDocs;
						this.predFuncValues = predFuncValues;
						this.context = context;
					}

					public override bool Get(int index)
					{
						if (acceptDocs != null && !acceptDocs.Get(index))
						{
							return false;
						}
						return predFuncValues.BoolVal(index);
					}

					public override int Length()
					{
						return ((AtomicReader)context.Reader()).MaxDoc();
					}

					private readonly Bits acceptDocs;

					private readonly FunctionValues predFuncValues;

					private readonly AtomicReaderContext context;
				}

				private readonly PredicateValueSourceFilter _enclosing;

				private readonly AtomicReaderContext context;

				private readonly Bits acceptDocs;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				SerializedDVStrategy.PredicateValueSourceFilter that = (SerializedDVStrategy.PredicateValueSourceFilter
					)o;
				if (!predicateValueSource.Equals(that.predicateValueSource))
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				return predicateValueSource.GetHashCode();
			}
			//PredicateValueSourceFilter
		}

		/// <summary>Implements a ValueSource by deserializing a Shape in from BinaryDocValues using BinaryCodec.
		/// 	</summary>
		/// <remarks>Implements a ValueSource by deserializing a Shape in from BinaryDocValues using BinaryCodec.
		/// 	</remarks>
		/// <seealso cref="SerializedDVStrategy.MakeShapeValueSource()">SerializedDVStrategy.MakeShapeValueSource()
		/// 	</seealso>
		internal class ShapeDocValueSource : ValueSource
		{
			private readonly string fieldName;

			private readonly BinaryCodec binaryCodec;

			private ShapeDocValueSource(string fieldName, BinaryCodec binaryCodec)
			{
				//spatial4j
				this.fieldName = fieldName;
				this.binaryCodec = binaryCodec;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
				 readerContext)
			{
				BinaryDocValues docValues = ((AtomicReader)readerContext.Reader()).GetBinaryDocValues
					(fieldName);
				return new _FunctionValues_212(this, docValues);
			}

			private sealed class _FunctionValues_212 : FunctionValues
			{
				public _FunctionValues_212(ShapeDocValueSource _enclosing, BinaryDocValues docValues
					)
				{
					this._enclosing = _enclosing;
					this.docValues = docValues;
					this.bytesRefDoc = -1;
					this.bytesRef = new BytesRef();
				}

				internal int bytesRefDoc;

				internal BytesRef bytesRef;

				//scratch
				internal bool FillBytes(int doc)
				{
					if (this.bytesRefDoc != doc)
					{
						docValues.Get(doc, this.bytesRef);
						this.bytesRefDoc = doc;
					}
					return this.bytesRef.length != 0;
				}

				public override bool Exists(int doc)
				{
					return this.FillBytes(doc);
				}

				public override bool BytesVal(int doc, BytesRef target)
				{
					if (this.FillBytes(doc))
					{
						target.bytes = this.bytesRef.bytes;
						target.offset = this.bytesRef.offset;
						target.length = this.bytesRef.length;
						return true;
					}
					else
					{
						target.length = 0;
						return false;
					}
				}

				public override object ObjectVal(int docId)
				{
					if (!this.FillBytes(docId))
					{
						return null;
					}
					DataInputStream dataInput = new DataInputStream(new ByteArrayInputStream(this.bytesRef
						.bytes, this.bytesRef.offset, this.bytesRef.length));
					try
					{
						return this._enclosing.binaryCodec.ReadShape(dataInput);
					}
					catch (IOException e)
					{
						throw new RuntimeException(e);
					}
				}

				public override Explanation Explain(int doc)
				{
					return new Explanation(float.NaN, this.ToString(doc));
				}

				public override string ToString(int doc)
				{
					return this._enclosing.Description() + "=" + this.ObjectVal(doc);
				}

				private readonly ShapeDocValueSource _enclosing;

				private readonly BinaryDocValues docValues;
			}

			//TODO truncate?
			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				SerializedDVStrategy.ShapeDocValueSource that = (SerializedDVStrategy.ShapeDocValueSource
					)o;
				if (!fieldName.Equals(that.fieldName))
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = fieldName.GetHashCode();
				return result;
			}

			public override string Description()
			{
				return "shapeDocVal(" + fieldName + ")";
			}
			//ShapeDocValueSource
		}
	}
}
