/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>Prints how many ords are under each dimension.</summary>
	/// <remarks>Prints how many ords are under each dimension.</remarks>
	public class PrintTaxonomyStats
	{
		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public PrintTaxonomyStats()
		{
		}

		// java -cp ../build/core/classes/java:../build/facet/classes/java Lucene.Net.facet.util.PrintTaxonomyStats -printTree /s2/scratch/indices/wikibig.trunk.noparents.facets.Lucene41.nd1M/facets
		/// <summary>Command-line tool.</summary>
		/// <remarks>Command-line tool.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void Main(string[] args)
		{
			bool printTree = false;
			string path = null;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].Equals("-printTree"))
				{
					printTree = true;
				}
				else
				{
					path = args[i];
				}
			}
			if (args.Length != (printTree ? 2 : 1))
			{
				System.Console.Out.WriteLine("\nUsage: java -classpath ... Lucene.Net.facet.util.PrintTaxonomyStats [-printTree] /path/to/taxononmy/index\n"
					);
				System.Environment.Exit(1);
			}
			Directory dir = FSDirectory.Open(new FilePath(path));
			TaxonomyReader r = new DirectoryTaxonomyReader(dir);
			PrintStats(r, System.Console.Out, printTree);
			r.Close();
			dir.Close();
		}

		/// <summary>Recursively prints stats for all ordinals.</summary>
		/// <remarks>Recursively prints stats for all ordinals.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void PrintStats(TaxonomyReader r, TextWriter @out, bool printTree)
		{
			@out.WriteLine(r.GetSize() + " total categories.");
			TaxonomyReader.ChildrenIterator it = r.GetChildren(TaxonomyReader.ROOT_ORDINAL);
			int child;
			while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
			{
				TaxonomyReader.ChildrenIterator chilrenIt = r.GetChildren(child);
				int numImmediateChildren = 0;
				while (chilrenIt.Next() != TaxonomyReader.INVALID_ORDINAL)
				{
					numImmediateChildren++;
				}
				FacetLabel cp = r.GetPath(child);
				@out.WriteLine("/" + cp.components[0] + ": " + numImmediateChildren + " immediate children; "
					 + (1 + CountAllChildren(r, child)) + " total categories");
				if (printTree)
				{
					PrintAllChildren(@out, r, child, "  ", 1);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static int CountAllChildren(TaxonomyReader r, int ord)
		{
			int count = 0;
			TaxonomyReader.ChildrenIterator it = r.GetChildren(ord);
			int child;
			while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
			{
				count += 1 + CountAllChildren(r, child);
			}
			return count;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void PrintAllChildren(TextWriter @out, TaxonomyReader r, int ord, 
			string indent, int depth)
		{
			TaxonomyReader.ChildrenIterator it = r.GetChildren(ord);
			int child;
			while ((child = it.Next()) != TaxonomyReader.INVALID_ORDINAL)
			{
				@out.WriteLine(indent + "/" + r.GetPath(child).components[depth]);
				PrintAllChildren(@out, r, child, indent + "  ", depth + 1);
			}
		}
	}
}
