/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Com.Spatial4j.Core.Context;
using Com.Spatial4j.Core.Distance;
using Lucene.Net.Spatial.Prefix.Tree;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix.Tree
{
	/// <summary>
	/// Abstract Factory for creating
	/// <see cref="SpatialPrefixTree">SpatialPrefixTree</see>
	/// instances with useful
	/// defaults and passed on configurations defined in a Map.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class SpatialPrefixTreeFactory
	{
		private const double DEFAULT_GEO_MAX_DETAIL_KM = 0.001;

		public static readonly string PREFIX_TREE = "prefixTree";

		public static readonly string MAX_LEVELS = "maxLevels";

		public static readonly string MAX_DIST_ERR = "maxDistErr";

		protected internal IDictionary<string, string> args;

		protected internal SpatialContext ctx;

		protected internal int maxLevels;

		//1m
		/// <summary>The factory  is looked up via "prefixTree" in args, expecting "geohash" or "quad".
		/// 	</summary>
		/// <remarks>
		/// The factory  is looked up via "prefixTree" in args, expecting "geohash" or "quad".
		/// If its neither of these, then "geohash" is chosen for a geo context, otherwise "quad" is chosen.
		/// </remarks>
		public static SpatialPrefixTree MakeSPT(IDictionary<string, string> args, ClassLoader
			 classLoader, SpatialContext ctx)
		{
			SpatialPrefixTreeFactory instance;
			string cname = args.Get(PREFIX_TREE);
			if (cname == null)
			{
				cname = ctx.IsGeo() ? "geohash" : "quad";
			}
			if (Sharpen.Runtime.EqualsIgnoreCase("geohash", cname))
			{
				instance = new GeohashPrefixTree.Factory();
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase("quad", cname))
				{
					instance = new QuadPrefixTree.Factory();
				}
				else
				{
					try
					{
						Type c = classLoader.LoadClass(cname);
						instance = (SpatialPrefixTreeFactory)System.Activator.CreateInstance(c);
					}
					catch (Exception e)
					{
						throw new RuntimeException(e);
					}
				}
			}
			instance.Init(args, ctx);
			return instance.NewSPT();
		}

		protected internal virtual void Init(IDictionary<string, string> args, SpatialContext
			 ctx)
		{
			this.args = args;
			this.ctx = ctx;
			InitMaxLevels();
		}

		protected internal virtual void InitMaxLevels()
		{
			string mlStr = args.Get(MAX_LEVELS);
			if (mlStr != null)
			{
				maxLevels = Sharpen.Extensions.ValueOf(mlStr);
				return;
			}
			double degrees;
			string maxDetailDistStr = args.Get(MAX_DIST_ERR);
			if (maxDetailDistStr == null)
			{
				if (!ctx.IsGeo())
				{
					return;
				}
				//let default to max
				degrees = DistanceUtils.Dist2Degrees(DEFAULT_GEO_MAX_DETAIL_KM, DistanceUtils.EARTH_MEAN_RADIUS_KM
					);
			}
			else
			{
				degrees = double.ParseDouble(maxDetailDistStr);
			}
			maxLevels = GetLevelForDistance(degrees);
		}

		/// <summary>
		/// Calls
		/// <see cref="SpatialPrefixTree.GetLevelForDistance(double)">SpatialPrefixTree.GetLevelForDistance(double)
		/// 	</see>
		/// .
		/// </summary>
		protected internal abstract int GetLevelForDistance(double degrees);

		protected internal abstract SpatialPrefixTree NewSPT();
	}
}
