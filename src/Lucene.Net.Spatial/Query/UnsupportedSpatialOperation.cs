/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Spatial.Query;
using Sharpen;

namespace Lucene.Net.Spatial.Query
{
	/// <summary>
	/// Exception thrown when the
	/// <see cref="Lucene.Net.Spatial.SpatialStrategy">Lucene.Net.Spatial.SpatialStrategy
	/// 	</see>
	/// cannot implement the requested operation.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	[System.Serializable]
	public class UnsupportedSpatialOperation : NotSupportedException
	{
		public UnsupportedSpatialOperation(SpatialOperation op) : base(op.GetName())
		{
		}
	}
}
