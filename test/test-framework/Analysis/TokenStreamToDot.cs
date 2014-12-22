using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Consumes a TokenStream and outputs the dot (graphviz) string (graph).</summary>
	
	public class TokenStreamToDot
	{
		private readonly TokenStream tokenStream;

		private readonly CharTermAttribute termAtt;

		private readonly PositionIncrementAttribute posIncAtt;

		private readonly PositionLengthAttribute posLengthAtt;

		private readonly OffsetAttribute offsetAtt;

		private readonly string inputText;

		protected internal readonly StreamWriter streamWriter;

		/// <summary>
		/// If inputText is non-null, and the TokenStream has
		/// offsets, we include the surface form in each arc's
		/// label.
		/// </summary>
		public TokenStreamToDot(string inputText, TokenStream ts, StreamWriter sw)
		{
			this.tokenStream = ts;
			this.streamWriter = sw;
			this.inputText = inputText;
			termAtt = ts.AddAttribute<CharTermAttribute>();
			posIncAtt = ts.AddAttribute<PositionIncrementAttribute>();
			posLengthAtt = ts.AddAttribute<PositionLengthAttribute>();
			if (ts.HasAttribute(typeof(OffsetAttribute)))
			{
				offsetAtt = ts.AddAttribute<OffsetAttribute>();
			}
			else
			{
				offsetAtt = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ToDot()
		{
			tokenStream.Reset();
			WriteHeader();
			// TODO: is there some way to tell dot that it should
			// make the "main path" a straight line and have the
			// non-sausage arcs not affect node placement...
			int pos = -1;
			int lastEndPos = -1;
			while (tokenStream.IncrementToken())
			{
				bool isFirst = pos == -1;
				int posInc = posIncAtt.PositionIncrement;
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
					WriteNode(pos, pos.ToString());
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
					int startOffset = offsetAtt.StartOffset;
					int endOffset = offsetAtt.EndOffset;
					//System.out.println("start=" + startOffset + " end=" + endOffset + " len=" + inputText.length());
					if (inputText != null)
					{
						arcLabel += " / " + inputText.Substring(startOffset, endOffset);
					}
					else
					{
						arcLabel += " / " + startOffset + "-" + endOffset;
					}
				}
				WriteArc(pos, pos + posLengthAtt.PositionLength, arcLabel, null);
				lastEndPos = pos + posLengthAtt.PositionLength;
			}
			tokenStream.End();
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
			streamWriter.Write("  " + fromNode + " -> " + toNode + " [");
			if (label != null)
			{
				streamWriter.Write(" label=\"" + label + "\"");
			}
			if (style != null)
			{
				streamWriter.Write(" style=\"" + style + "\"");
			}
			streamWriter.WriteLine("]");
		}

		protected internal virtual void WriteNode(int name, string label)
		{
			streamWriter.Write("  " + name);
			if (label != null)
			{
				streamWriter.Write(" [label=\"" + label + "\"]");
			}
			else
			{
				streamWriter.Write(" [shape=point color=white]");
			}
			streamWriter.WriteLine();
		}

		private static readonly string FONT_NAME = "Helvetica";

		/// <summary>Override to customize.</summary>
		/// <remarks>Override to customize.</remarks>
		protected internal virtual void WriteHeader()
		{
			streamWriter.WriteLine("digraph tokens {");
			streamWriter.WriteLine("  graph [ fontsize=30 labelloc=\"t\" label=\"\" splines=true overlap=false rankdir = \"LR\" ];"
				);
			streamWriter.WriteLine("  // A2 paper size");
			streamWriter.WriteLine("  size = \"34.4,16.5\";");
			//out.println("  // try to fill paper");
			//out.println("  ratio = fill;");
			streamWriter.WriteLine("  edge [ fontname=\"" + FONT_NAME + "\" fontcolor=\"red\" color=\"#606060\" ]"
				);
			streamWriter.WriteLine("  node [ style=\"filled\" fillcolor=\"#e8e8f0\" shape=\"Mrecord\" fontname=\""
				 + FONT_NAME + "\" ]");
			streamWriter.WriteLine();
		}

		/// <summary>Override to customize.</summary>
		/// <remarks>Override to customize.</remarks>
		protected internal virtual void WriteTrailer()
		{
			streamWriter.WriteLine("}");
		}
	}
}
