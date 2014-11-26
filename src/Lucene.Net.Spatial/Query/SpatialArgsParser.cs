/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Com.Spatial4j.Core.Context;
using Lucene.Net.Spatial.Query;
using Sharpen;

namespace Lucene.Net.Spatial.Query
{
	/// <summary>
	/// Parses a string that usually looks like "OPERATION(SHAPE)" into a
	/// <see cref="SpatialArgs">SpatialArgs</see>
	/// object. The set of operations supported are defined in
	/// <see cref="SpatialOperation">SpatialOperation</see>
	/// , such
	/// as "Intersects" being a common one. The shape portion is defined by WKT
	/// <see cref="Com.Spatial4j.Core.IO.WktShapeParser">Com.Spatial4j.Core.IO.WktShapeParser
	/// 	</see>
	/// ,
	/// but it can be overridden/customized via
	/// <see cref="ParseShape(string, Com.Spatial4j.Core.Context.SpatialContext)">ParseShape(string, Com.Spatial4j.Core.Context.SpatialContext)
	/// 	</see>
	/// .
	/// There are some optional name-value pair parameters that follow the closing parenthesis.  Example:
	/// <pre>
	/// Intersects(ENVELOPE(-10,-8,22,20)) distErrPct=0.025
	/// </pre>
	/// <p/>
	/// In the future it would be good to support something at least semi-standardized like a
	/// variant of <a href="http://docs.geoserver.org/latest/en/user/filter/ecql_reference.html#spatial-predicate">
	/// [E]CQL</a>.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SpatialArgsParser
	{
		public static readonly string DIST_ERR_PCT = "distErrPct";

		public static readonly string DIST_ERR = "distErr";

		/// <summary>Writes a close approximation to the parsed input format.</summary>
		/// <remarks>Writes a close approximation to the parsed input format.</remarks>
		internal static string WriteSpatialArgs(SpatialArgs args)
		{
			StringBuilder str = new StringBuilder();
			str.Append(args.GetOperation().GetName());
			str.Append('(');
			str.Append(args.GetShape().ToString());
			if (args.GetDistErrPct() != null)
			{
				str.Append(" distErrPct=").Append(string.Format(CultureInfo.ROOT, "%.2f%%", args.
					GetDistErrPct() * 100d));
			}
			if (args.GetDistErr() != null)
			{
				str.Append(" distErr=").Append(args.GetDistErr());
			}
			str.Append(')');
			return str.ToString();
		}

		/// <summary>Parses a string such as "Intersects(ENVELOPE(-10,-8,22,20)) distErrPct=0.025".
		/// 	</summary>
		/// <remarks>Parses a string such as "Intersects(ENVELOPE(-10,-8,22,20)) distErrPct=0.025".
		/// 	</remarks>
		/// <param name="v">The string to parse. Mandatory.</param>
		/// <param name="ctx">The spatial context. Mandatory.</param>
		/// <returns>Not null.</returns>
		/// <exception cref="System.ArgumentException">if the parameters don't make sense or an add-on parameter is unknown
		/// 	</exception>
		/// <exception cref="Sharpen.ParseException">If there is a problem parsing the string
		/// 	</exception>
		/// <exception cref="Com.Spatial4j.Core.Exception.InvalidShapeException">When the coordinates are invalid for the shape
		/// 	</exception>
		public virtual SpatialArgs Parse(string v, SpatialContext ctx)
		{
			int idx = v.IndexOf('(');
			int edx = v.LastIndexOf(')');
			if (idx < 0 || idx > edx)
			{
				throw new ParseException("missing parens: " + v, -1);
			}
			SpatialOperation op = SpatialOperation.Get(Sharpen.Runtime.Substring(v, 0, idx).Trim
				());
			string body = Sharpen.Runtime.Substring(v, idx + 1, edx).Trim();
			if (body.Length < 1)
			{
				throw new ParseException("missing body : " + v, idx + 1);
			}
			Com.Spatial4j.Core.Shape.Shape shape = ParseShape(body, ctx);
			SpatialArgs args = NewSpatialArgs(op, shape);
			if (v.Length > (edx + 1))
			{
				body = Sharpen.Runtime.Substring(v, edx + 1).Trim();
				if (body.Length > 0)
				{
					IDictionary<string, string> aa = ParseMap(body);
					ReadNameValuePairs(args, aa);
					if (!aa.IsEmpty())
					{
						throw new ArgumentException("unused parameters: " + aa);
					}
				}
			}
			args.Validate();
			return args;
		}

		protected internal virtual SpatialArgs NewSpatialArgs(SpatialOperation op, Com.Spatial4j.Core.Shape.Shape
			 shape)
		{
			return new SpatialArgs(op, shape);
		}

		protected internal virtual void ReadNameValuePairs(SpatialArgs args, IDictionary<
			string, string> nameValPairs)
		{
			args.SetDistErrPct(ReadDouble(Sharpen.Collections.Remove(nameValPairs, DIST_ERR_PCT
				)));
			args.SetDistErr(ReadDouble(Sharpen.Collections.Remove(nameValPairs, DIST_ERR)));
		}

		/// <exception cref="Sharpen.ParseException"></exception>
		protected internal virtual Com.Spatial4j.Core.Shape.Shape ParseShape(string str, 
			SpatialContext ctx)
		{
			//return ctx.readShape(str);//still in Spatial4j 0.4 but will be deleted
			return ctx.ReadShapeFromWkt(str);
		}

		protected internal static double ReadDouble(string v)
		{
			return v == null ? null : double.ValueOf(v);
		}

		protected internal static bool ReadBool(string v, bool defaultValue)
		{
			return v == null ? defaultValue : System.Boolean.Parse(v);
		}

		/// <summary>Parses "a=b c=d f" (whitespace separated) into name-value pairs.</summary>
		/// <remarks>
		/// Parses "a=b c=d f" (whitespace separated) into name-value pairs. If there
		/// is no '=' as in 'f' above then it's short for f=f.
		/// </remarks>
		protected internal static IDictionary<string, string> ParseMap(string body)
		{
			IDictionary<string, string> map = new Dictionary<string, string>();
			StringTokenizer st = new StringTokenizer(body, " \n\t");
			while (st.HasMoreTokens())
			{
				string a = st.NextToken();
				int idx = a.IndexOf('=');
				if (idx > 0)
				{
					string k = Sharpen.Runtime.Substring(a, 0, idx);
					string v = Sharpen.Runtime.Substring(a, idx + 1);
					map.Put(k, v);
				}
				else
				{
					map.Put(a, a);
				}
			}
			return map;
		}
	}
}
