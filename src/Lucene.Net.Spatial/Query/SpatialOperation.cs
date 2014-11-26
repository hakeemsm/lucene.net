/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Com.Spatial4j.Core.Shape;
using Sharpen;

namespace Lucene.Net.Spatial.Query
{
	/// <summary>A clause that compares a stored geometry to a supplied geometry.</summary>
	/// <remarks>
	/// A clause that compares a stored geometry to a supplied geometry. For more
	/// explanation of each operation, consider looking at the source implementation
	/// of
	/// <see cref="Evaluate(Com.Spatial4j.Core.Shape.Shape, Com.Spatial4j.Core.Shape.Shape)
	/// 	">Evaluate(Com.Spatial4j.Core.Shape.Shape, Com.Spatial4j.Core.Shape.Shape)</see>
	/// .
	/// </remarks>
	/// <seealso><a href="http://edndoc.esri.com/arcsde/9.1/general_topics/understand_spatial_relations.htm">
	/// *   ESRIs docs on spatial relations</a></seealso>
	/// <lucene.experimental></lucene.experimental>
	[System.Serializable]
	public abstract class SpatialOperation
	{
		private static readonly IDictionary<string, Lucene.Net.Spatial.Query.SpatialOperation
			> registry = new Dictionary<string, Lucene.Net.Spatial.Query.SpatialOperation
			>();

		private static readonly IList<Lucene.Net.Spatial.Query.SpatialOperation> list
			 = new AList<Lucene.Net.Spatial.Query.SpatialOperation>();

		private sealed class _SpatialOperation_49 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_49(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			// Private registry
			// Geometry Operations
			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				return indexedShape.GetBoundingBox().Relate(queryShape).Intersects();
			}
		}

		/// <summary>Bounding box of the *indexed* shape.</summary>
		/// <remarks>Bounding box of the *indexed* shape.</remarks>
		public static readonly Lucene.Net.Spatial.Query.SpatialOperation BBoxIntersects
			 = new _SpatialOperation_49("BBoxIntersects", true, false, false);

		private sealed class _SpatialOperation_56 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_56(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				Rectangle bbox = indexedShape.GetBoundingBox();
				return bbox.Relate(queryShape) == SpatialRelation.WITHIN || bbox.Equals(queryShape
					);
			}
		}

		/// <summary>Bounding box of the *indexed* shape.</summary>
		/// <remarks>Bounding box of the *indexed* shape.</remarks>
		public static readonly Lucene.Net.Spatial.Query.SpatialOperation BBoxWithin
			 = new _SpatialOperation_56("BBoxWithin", true, false, false);

		private sealed class _SpatialOperation_63 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_63(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				return indexedShape.HasArea() && indexedShape.Relate(queryShape) == SpatialRelation
					.CONTAINS || indexedShape.Equals(queryShape);
			}
		}

		public static readonly Lucene.Net.Spatial.Query.SpatialOperation Contains = 
			new _SpatialOperation_63("Contains", true, true, false);

		private sealed class _SpatialOperation_69 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_69(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				return indexedShape.Relate(queryShape).Intersects();
			}
		}

		public static readonly Lucene.Net.Spatial.Query.SpatialOperation Intersects
			 = new _SpatialOperation_69("Intersects", true, false, false);

		private sealed class _SpatialOperation_75 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_75(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				return indexedShape.Equals(queryShape);
			}
		}

		public static readonly Lucene.Net.Spatial.Query.SpatialOperation IsEqualTo
			 = new _SpatialOperation_75("IsEqualTo", false, false, false);

		private sealed class _SpatialOperation_81 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_81(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				return !indexedShape.Relate(queryShape).Intersects();
			}
		}

		public static readonly Lucene.Net.Spatial.Query.SpatialOperation IsDisjointTo
			 = new _SpatialOperation_81("IsDisjointTo", false, false, false);

		private sealed class _SpatialOperation_87 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_87(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				return queryShape.HasArea() && (indexedShape.Relate(queryShape) == SpatialRelation
					.WITHIN || indexedShape.Equals(queryShape));
			}
		}

		public static readonly Lucene.Net.Spatial.Query.SpatialOperation IsWithin = 
			new _SpatialOperation_87("IsWithin", true, false, true);

		private sealed class _SpatialOperation_93 : Lucene.Net.Spatial.Query.SpatialOperation
		{
			public _SpatialOperation_93(string baseArg1, bool baseArg2, bool baseArg3, bool baseArg4
				) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			public override bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
				 queryShape)
			{
				return queryShape.HasArea() && indexedShape.Relate(queryShape).Intersects();
			}
		}

		public static readonly Lucene.Net.Spatial.Query.SpatialOperation Overlaps = 
			new _SpatialOperation_93("Overlaps", true, false, true);

		private readonly bool scoreIsMeaningful;

		private readonly bool sourceNeedsArea;

		private readonly bool targetNeedsArea;

		private readonly string name;

		protected internal SpatialOperation(string name, bool scoreIsMeaningful, bool sourceNeedsArea
			, bool targetNeedsArea)
		{
			// Member variables
			this.name = name;
			this.scoreIsMeaningful = scoreIsMeaningful;
			this.sourceNeedsArea = sourceNeedsArea;
			this.targetNeedsArea = targetNeedsArea;
			registry.Put(name, this);
			registry.Put(name.ToUpper(CultureInfo.ROOT), this);
			list.AddItem(this);
		}

		public static Lucene.Net.Spatial.Query.SpatialOperation Get(string v)
		{
			Lucene.Net.Spatial.Query.SpatialOperation op = registry.Get(v);
			if (op == null)
			{
				op = registry.Get(v.ToUpper(CultureInfo.ROOT));
			}
			if (op == null)
			{
				throw new ArgumentException("Unknown Operation: " + v);
			}
			return op;
		}

		public static IList<Lucene.Net.Spatial.Query.SpatialOperation> Values()
		{
			return list;
		}

		public static bool Is(Lucene.Net.Spatial.Query.SpatialOperation op, params 
			Lucene.Net.Spatial.Query.SpatialOperation[] tst)
		{
			foreach (Lucene.Net.Spatial.Query.SpatialOperation t in tst)
			{
				if (op == t)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Returns whether the relationship between indexedShape and queryShape is
		/// satisfied by this operation.
		/// </summary>
		/// <remarks>
		/// Returns whether the relationship between indexedShape and queryShape is
		/// satisfied by this operation.
		/// </remarks>
		public abstract bool Evaluate(Com.Spatial4j.Core.Shape.Shape indexedShape, Com.Spatial4j.Core.Shape.Shape
			 queryShape);

		// ================================================= Getters / Setters =============================================
		public virtual bool IsScoreIsMeaningful()
		{
			return scoreIsMeaningful;
		}

		public virtual bool IsSourceNeedsArea()
		{
			return sourceNeedsArea;
		}

		public virtual bool IsTargetNeedsArea()
		{
			return targetNeedsArea;
		}

		public virtual string GetName()
		{
			return name;
		}

		public override string ToString()
		{
			return name;
		}
	}
}
