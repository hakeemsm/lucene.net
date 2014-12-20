/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Consumes a TokenStream and outputs the dot (graphviz) string (graph).</summary>
	/// <remarks>Consumes a TokenStream and outputs the dot (graphviz) string (graph).</remarks>
	public class TokenStreamToDot
	{
		private readonly TokenStream @in;

		private readonly CharTermAttribute termAtt;

		private readonly PositionIncrementAttribute posIncAtt;

		private readonly PositionLengthAttribute posLengthAtt;

		private readonly OffsetAttribute offsetAtt;

		private readonly string inputText;

		protected internal readonly PrintWriter @out;

		/// <summary>
		/// If inputText is non-null, and the TokenStream has
		/// offsets, we include the surface form in each arc's
		/// label.
		/// </summary>
		/// <remarks>
		/// If inputText is non-null, and the TokenStream has
		/// offsets, we include the surface form in each arc's
		/// label.
		/// </remarks>
		public TokenStreamToDot(string inputText, TokenStream @in, StreamWriter @out)
		{
			this.@in = @in;
			this.@out = @out;
			this.inputText = inputText;
			termAtt = @in.AddAttribute<CharTermAttribute>();
			posIncAtt = @in.AddAttribute<PositionIncrementAttribute>();
			posLengthAtt = @in.AddAttribute<PositionLengthAttribute>();
			if (@in.HasAttribute(typeof(OffsetAttribute)))
			{
				offsetAtt = @in.AddAttribute<OffsetAttribute>();
			}
			else
			{
				offsetAtt = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ToDot()
		{
			@in.Reset();
			WriteHeader();
			// TODO: is there some way to tell dot that it should
			// make the "main path" a straight line and have the
			// non-sausage arcs not affect node placement...
			int pos = -1;
			int lastEndPos = -1;
			while (@in.IncrementToken())
			{
				bool isFirst = pos == -1;
				int posInc = posIncAtt.GetPositionIncrement();
				if (isFirst && posInc == 0)
				{
					// TODO: hmm are TS's still allowed to do this...?
					System.Console.Error.WriteLine("WARNING: first posInc was 0; correcting to 1");
					posInc = 1;
				}
				if (posInc > 0)
				{
					// New node:
					pos += posInc;
					WriteNode(pos, Sharpen.Extensions.ToString(pos));
				}
				if (posInc > 1)
				{
					// Gap!
					WriteArc(lastEndPos, pos, null, "dotted");
				}
				if (isFirst)
				{
					WriteNode(-1, null);
					WriteArc(-1, pos, null, null);
				}
				string arcLabel = termAtt.ToString();
				if (offsetAtt != null)
				{
					int startOffset = offsetAtt.StartOffset();
					int endOffset = offsetAtt.EndOffset();
					//System.out.println("start=" + startOffset + " end=" + endOffset + " len=" + inputText.length());
					if (inputText != null)
					{
						arcLabel += " / " + Sharpen.Runtime.Substring(inputText, startOffset, endOffset);
					}
					else
					{
						arcLabel += " / " + startOffset + "-" + endOffset;
					}
				}
				WriteArc(pos, pos + posLengthAtt.PositionLength, arcLabel, null);
				lastEndPos = pos + posLengthAtt.PositionLength;
			}
			@in.End();
			if (lastEndPos != -1)
			{
				// TODO: should we output any final text (from end
				// offsets) on this arc...?
				WriteNode(-2, null);
				WriteArc(lastEndPos, -2, null, null);
			}
			WriteTrailer();
		}

		protected internal virtual void WriteArc(int fromNode, int toNode, string label, 
			string style)
		{
			@out.Write("  " + fromNode + " -> " + toNode + " [");
			if (label != null)
			{
				@out.Write(" label=\"" + label + "\"");
			}
			if (style != null)
			{
				@out.Write(" style=\"" + style + "\"");
			}
			@out.WriteLine("]");
		}

		protected internal virtual void WriteNode(int name, string label)
		{
			@out.Write("  " + name);
			if (label != null)
			{
				@out.Write(" [label=\"" + label + "\"]");
			}
			else
			{
				@out.Write(" [shape=point color=white]");
			}
			@out.WriteLine();
		}

		private static readonly string FONT_NAME = "Helvetica";

		/// <summary>Override to customize.</summary>
		/// <remarks>Override to customize.</remarks>
		protected internal virtual void WriteHeader()
		{
			@out.WriteLine("digraph tokens {");
			@out.WriteLine("  graph [ fontsize=30 labelloc=\"t\" label=\"\" splines=true overlap=false rankdir = \"LR\" ];"
				);
			@out.WriteLine("  // A2 paper size");
			@out.WriteLine("  size = \"34.4,16.5\";");
			//out.println("  // try to fill paper");
			//out.println("  ratio = fill;");
			@out.WriteLine("  edge [ fontname=\"" + FONT_NAME + "\" fontcolor=\"red\" color=\"#606060\" ]"
				);
			@out.WriteLine("  node [ style=\"filled\" fillcolor=\"#e8e8f0\" shape=\"Mrecord\" fontname=\""
				 + FONT_NAME + "\" ]");
			@out.WriteLine();
		}

		/// <summary>Override to customize.</summary>
		/// <remarks>Override to customize.</remarks>
		protected internal virtual void WriteTrailer()
		{
			@out.WriteLine("}");
		}
	}
}
