

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Codecs.PerField;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Store;
using Attribute = System.Attribute;
using Directory = System.IO.Directory;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>General utility methods for Lucene unit tests.</summary>
	/// <remarks>General utility methods for Lucene unit tests.</remarks>
	public static class TestUtil
	{

		//
		/// <summary>Deletes one or more files or directories (and everything underneath it).
		/// 	</summary>
		/// <remarks>Deletes one or more files or directories (and everything underneath it).
		/// 	</remarks>
		/// <exception cref="System.IO.IOException">
		/// if any of the given files (or their subhierarchy files in case
		/// of directories) cannot be removed.
		/// </exception>
		public static void Rm(params FileInfo[] locations)
		{
			LinkedList<FileInfo> unremoved = Rm(new LinkedList<FileInfo>(), locations);
			if (unremoved.Any())
			{
				StringBuilder b = new StringBuilder("Could not remove the following files (in the order of attempts):\n");
				foreach (FileInfo f in unremoved)
				{
					b.Append("   ").Append(f.FullName).Append("\n");
				}
				throw new IOException(b.ToString());
			}
		}

		private static LinkedList<FileInfo> Rm(LinkedList<FileInfo> unremoved, params FileInfo[] locations)
		{
			foreach (var location in locations)
			{
				if (location.Exists)
				{
					if (Directory.Exists(location.FullName))
					{
						Rm(unremoved, new FileInfo(location.FullName));
					}
                    location.Delete();

					if (location.Exists)
					{
						unremoved.AddLast(location);
					}
				}
			}
			return unremoved;
		}

		

		public static void SyncConcurrentMerges(IndexWriter writer)
		{
			SyncConcurrentMerges(writer.Config.MergeScheduler);
		}

		public static void SyncConcurrentMerges(MergeScheduler ms)
		{
			if (ms is ConcurrentMergeScheduler)
			{
				((ConcurrentMergeScheduler)ms).Sync();
			}
		}

		/// <summary>This runs the CheckIndex tool on the index in.</summary>
		/// <remarks>
		/// This runs the CheckIndex tool on the index in.  If any
		/// issues are hit, a SystemException is thrown; else,
		/// true is returned.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static CheckIndex.Status CheckIndex(Lucene.Net.Store.Directory dir)
		{
			return CheckIndex(dir, true);
		}

		/// <exception cref="System.IO.IOException"></exception>
        public static CheckIndex.Status CheckIndex(Lucene.Net.Store.Directory dir, bool crossCheckTermVectors)
		{
		    var memoryStream = new MemoryStream(); //Should this be MemoryStream really?
		    CheckIndex checker = new CheckIndex(dir) {CrossCheckTermVectors = crossCheckTermVectors};
		    checker.SetInfoStream(new StreamWriter(memoryStream,Encoding.UTF8,1024), false);
			CheckIndex.Status indexStatus = checker.CheckIndex_Renamed_Method(null);
			if (indexStatus == null || indexStatus.clean == false)
			{
				Console.Out.WriteLine("CheckIndex failed");
			    byte[] bytes = new byte[1024];
			    memoryStream.Write(bytes, 0, 1024);
				Console.Out.WriteLine(bytes);
				throw new SystemException("CheckIndex failed");
			}
		    if (LuceneTestCase.INFOSTREAM)
		    {
		        Console.Out.WriteLine(memoryStream.ToString());
		    }
		    return indexStatus;
		}

		/// <summary>This runs the CheckIndex tool on the Reader.</summary>
		/// <remarks>
		/// This runs the CheckIndex tool on the Reader.  If any
		/// issues are hit, a SystemException is thrown
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckReader(IndexReader reader)
		{
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				CheckReader(((AtomicReader)context.Reader), true);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckReader(AtomicReader reader, bool crossCheckTermVectors)
		{
            
		    var bos = new MemoryStream();
		    
			var infoStream = new StreamWriter(bos, Encoding.UTF8,1024);
			reader.CheckIntegrity();
			CheckIndex.Status.FieldNormStatus fieldNormStatus = Lucene.Net.Index.CheckIndex.TestFieldNorms(reader, infoStream);
            CheckIndex.Status.TermIndexStatus termIndexStatus = Lucene.Net.Index.CheckIndex.TestPostings(reader, infoStream);
            CheckIndex.Status.StoredFieldStatus storedFieldStatus = Lucene.Net.Index.CheckIndex.TestStoredFields(reader, infoStream);
            CheckIndex.Status.TermVectorStatus termVectorStatus = Lucene.Net.Index.CheckIndex.TestTermVectors(reader, infoStream, false, crossCheckTermVectors);
			CheckIndex.Status.DocValuesStatus docValuesStatus = Lucene.Net.Index.CheckIndex.TestDocValues(reader, infoStream);
			if (fieldNormStatus.error != null || termIndexStatus.error != null || storedFieldStatus
				.error != null || termVectorStatus.error != null || docValuesStatus.error != null)
			{
				System.Console.Out.WriteLine("CheckReader failed");

			    byte[] bytes=new byte[1024];
			    bos.Write(bytes,0,1024);
				System.Console.Out.WriteLine(bytes);
				throw new Exception("CheckReader failed");
			}
		    if (LuceneTestCase.INFOSTREAM)
		    {
		        System.Console.Out.WriteLine(bos);
		    }
		}

		/// <summary>start and end are BOTH inclusive</summary>
		public static int NextInt(this Random r, int start, int end)
		{
			return RandomInts.RandomIntBetween(r, start, end);
		}

		/// <summary>start and end are BOTH inclusive</summary>
		public static long NextLong(this Random r, long start, long end)
		{
			//assert end >= start;
            
		    var sum = BigInteger.Add(new BigInteger(end),BigInteger.One);
		    var range = BigInteger.Subtract(sum, new BigInteger(start));
			if (range.CompareTo(new BigInteger(int.MaxValue)) <= 0)
			{
				return start + r.Next(int.Parse(range.ToString())); //.NET Port. hacky but that was the only way around!
			}
		    // probably not evenly distributed when range is large, but OK for tests
		    BigInteger augend = BigInteger.Multiply(range, new BigInteger(r.NextDouble()));
		        
		    long result = long.Parse((BigInteger.Add(new BigInteger(start),augend)).ToString());
				
		    //assert result >= start;
				
		    //assert result <= end;
		    return result;
		}

        public static double NextGaussian(this Random r, double mu = 0, double sigma = 1)
        {
            var u1 = r.NextDouble();
            var u2 = r.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2);

            return (mu + sigma * randStdNormal);
        }


		public static string RandomSimpleString(Random r, int maxLength)
		{
			return RandomSimpleString(r, 0, maxLength);
		}

		public static string RandomSimpleString(Random r, int minLength, int maxLength)
		{
			int end = NextInt(r, minLength, maxLength);
			if (end == 0)
			{
				// allow 0 length
				return string.Empty;
			}
			char[] buffer = new char[end];
			for (int i = 0; i < end; i++)
			{
				buffer[i] = (char)NextInt(r, 'a', 'z');
			}
			return new string(buffer, 0, end);
		}

		public static string RandomSimpleStringRange(Random r, char minChar, char maxChar
			, int maxLength)
		{
			int end = NextInt(r, 0, maxLength);
			if (end == 0)
			{
				// allow 0 length
				return string.Empty;
			}
			char[] buffer = new char[end];
			for (int i = 0; i < end; i++)
			{
				buffer[i] = (char)NextInt(r, minChar, maxChar);
			}
			return new string(buffer, 0, end);
		}

		public static string RandomSimpleString(Random r)
		{
			return RandomSimpleString(r, 0, 10);
		}

		/// <summary>Returns random string, including full unicode range.</summary>
		/// <remarks>Returns random string, including full unicode range.</remarks>
		public static string RandomUnicodeString(Random r)
		{
			return RandomUnicodeString(r, 20);
		}

		/// <summary>Returns a random string up to a certain length.</summary>
		/// <remarks>Returns a random string up to a certain length.</remarks>
		public static string RandomUnicodeString(Random r, int maxLength)
		{
			int end = NextInt(r, 0, maxLength);
			if (end == 0)
			{
				// allow 0 length
				return string.Empty;
			}
			char[] buffer = new char[end];
			RandomFixedLengthUnicodeString(r, buffer, 0, buffer.Length);
			return new string(buffer, 0, end);
		}

		/// <summary>
		/// Fills provided char[] with valid random unicode code
		/// unit sequence.
		/// </summary>
		/// <remarks>
		/// Fills provided char[] with valid random unicode code
		/// unit sequence.
		/// </remarks>
		public static void RandomFixedLengthUnicodeString(Random random, char[] chars, int
			 offset, int length)
		{
			int i = offset;
			int end = offset + length;
			while (i < end)
			{
				int t = random.Next(5);
				if (0 == t && i < length - 1)
				{
					// Make a surrogate pair
					// High surrogate
					chars[i++] = (char)NextInt(random, unchecked((int)(0xd800)), unchecked((int)(0xdbff
						)));
					// Low surrogate
					chars[i++] = (char)NextInt(random, unchecked((int)(0xdc00)), unchecked((int)(0xdfff
						)));
				}
				else
				{
					if (t <= 1)
					{
						chars[i++] = (char)random.Next(unchecked((int)(0x80)));
					}
					else
					{
						if (2 == t)
						{
							chars[i++] = (char)NextInt(random, unchecked((int)(0x80)), unchecked((int)(0x7ff)
								));
						}
						else
						{
							if (3 == t)
							{
								chars[i++] = (char)NextInt(random, unchecked((int)(0x800)), unchecked((int)(0xd7ff
									)));
							}
							else
							{
								if (4 == t)
								{
									chars[i++] = (char)NextInt(random, unchecked((int)(0xe000)), unchecked((int)(0xffff
										)));
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns a String thats "regexpish" (contains lots of operators typically found in regular expressions)
		/// If you call this enough times, you might get a valid regex!
		/// </summary>
		public static string RandomRegexpishString(Random r)
		{
			return RandomRegexpishString(r, 20);
		}

		/// <summary>
		/// Maximum recursion bound for '+' and '*' replacements in
		/// <see cref="RandomRegexpishString(Sharpen.Random, int)">RandomRegexpishString(Sharpen.Random, int)
		/// 	</see>
		/// .
		/// </summary>
		private const int maxRecursionBound = 5;

		/// <summary>
		/// Operators for
		/// <see cref="RandomRegexpishString(Sharpen.Random, int)">RandomRegexpishString(Sharpen.Random, int)
		/// 	</see>
		/// .
		/// </summary>
		private static readonly IList<string> ops = Arrays.AsList(".", "?", "{0," + maxRecursionBound
			 + "}", "{1," + maxRecursionBound + "}", "(", ")", "-", "[", "]", "|");

		// bounded replacement for '*'
		// bounded replacement for '+'
		/// <summary>
		/// Returns a String thats "regexpish" (contains lots of operators typically found in regular expressions)
		/// If you call this enough times, you might get a valid regex!
		/// <P>Note: to avoid practically endless backtracking patterns we replace asterisk and plus
		/// operators with bounded repetitions.
		/// </summary>
		/// <remarks>
		/// Returns a String thats "regexpish" (contains lots of operators typically found in regular expressions)
		/// If you call this enough times, you might get a valid regex!
		/// <P>Note: to avoid practically endless backtracking patterns we replace asterisk and plus
		/// operators with bounded repetitions. See LUCENE-4111 for more info.
		/// </remarks>
		/// <param name="maxLength">A hint about maximum length of the regexpish string. It may be exceeded by a few characters.
		/// 	</param>
		public static string RandomRegexpishString(Random r, int maxLength)
		{
			StringBuilder regexp = new StringBuilder(maxLength);
			for (int i = NextInt(r, 0, maxLength); i > 0; i--)
			{
				if (r.NextBoolean())
				{
					regexp.Append((char)RandomInts.RandomIntBetween(r, 'a', 'z'));
				}
				else
				{
					regexp.Append(RandomPicks.RandomFrom(r, ops));
				}
			}
			return regexp.ToString();
		}

		private static readonly string[] HTML_CHAR_ENTITIES = new string[] { "AElig", "Aacute"
			, "Acirc", "Agrave", "Alpha", "AMP", "Aring", "Atilde", "Auml", "Beta", "COPY", 
			"Ccedil", "Chi", "Dagger", "Delta", "ETH", "Eacute", "Ecirc", "Egrave", "Epsilon"
			, "Eta", "Euml", "Gamma", "GT", "Iacute", "Icirc", "Igrave", "Iota", "Iuml", "Kappa"
			, "Lambda", "LT", "Mu", "Ntilde", "Nu", "OElig", "Oacute", "Ocirc", "Ograve", "Omega"
			, "Omicron", "Oslash", "Otilde", "Ouml", "Phi", "Pi", "Prime", "Psi", "QUOT", "REG"
			, "Rho", "Scaron", "Sigma", "THORN", "Tau", "Theta", "Uacute", "Ucirc", "Ugrave"
			, "Upsilon", "Uuml", "Xi", "Yacute", "Yuml", "Zeta", "aacute", "acirc", "acute", 
			"aelig", "agrave", "alefsym", "alpha", "amp", "and", "ang", "apos", "aring", "asymp"
			, "atilde", "auml", "bdquo", "beta", "brvbar", "bull", "cap", "ccedil", "cedil", 
			"cent", "chi", "circ", "clubs", "cong", "copy", "crarr", "cup", "curren", "dArr"
			, "dagger", "darr", "deg", "delta", "diams", "divide", "eacute", "ecirc", "egrave"
			, "empty", "emsp", "ensp", "epsilon", "equiv", "eta", "eth", "euml", "euro", "exist"
			, "fnof", "forall", "frac12", "frac14", "frac34", "frasl", "gamma", "ge", "gt", 
			"hArr", "harr", "hearts", "hellip", "iacute", "icirc", "iexcl", "igrave", "image"
			, "infin", "int", "iota", "iquest", "isin", "iuml", "kappa", "lArr", "lambda", "lang"
			, "laquo", "larr", "lceil", "ldquo", "le", "lfloor", "lowast", "loz", "lrm", "lsaquo"
			, "lsquo", "lt", "macr", "mdash", "micro", "middot", "minus", "mu", "nabla", "nbsp"
			, "ndash", "ne", "ni", "not", "notin", "nsub", "ntilde", "nu", "oacute", "ocirc"
			, "oelig", "ograve", "oline", "omega", "omicron", "oplus", "or", "ordf", "ordm", 
			"oslash", "otilde", "otimes", "ouml", "para", "part", "permil", "perp", "phi", "pi"
			, "piv", "plusmn", "pound", "prime", "prod", "prop", "psi", "quot", "rArr", "radic"
			, "rang", "raquo", "rarr", "rceil", "rdquo", "real", "reg", "rfloor", "rho", "rlm"
			, "rsaquo", "rsquo", "sbquo", "scaron", "sdot", "sect", "shy", "sigma", "sigmaf"
			, "sim", "spades", "sub", "sube", "sum", "sup", "sup1", "sup2", "sup3", "supe", 
			"szlig", "tau", "there4", "theta", "thetasym", "thinsp", "thorn", "tilde", "times"
			, "trade", "uArr", "uacute", "uarr", "ucirc", "ugrave", "uml", "upsih", "upsilon"
			, "uuml", "weierp", "xi", "yacute", "yen", "yuml", "zeta", "zwj", "zwnj" };

		public static string RandomHtmlishString(Random random, int numElements)
		{
			int end = NextInt(random, 0, numElements);
			if (end == 0)
			{
				// allow 0 length
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < end; i++)
			{
				int val = random.Next(25);
				switch (val)
				{
					case 0:
					{
						sb.Append("<p>");
						break;
					}

					case 1:
					{
						sb.Append("<");
                        sb.Append("    ".Substring(NextInt(random, 0, 4)));
						sb.Append(RandomSimpleString(random));
						for (int j = 0; j < NextInt(random, 0, 10); ++j)
						{
							sb.Append(' ');
							sb.Append(RandomSimpleString(random));
                            sb.Append(" ".Substring(NextInt(random, 0, 1)));
							sb.Append('=');
                            sb.Append(" ".Substring(NextInt(random, 0, 1)));
                            sb.Append("\"".Substring(NextInt(random, 0, 1)));
							sb.Append(RandomSimpleString(random));
                            sb.Append("\"".Substring(NextInt(random, 0, 1)));
						}
                        sb.Append("    ".Substring(NextInt(random, 0, 4)));
                        sb.Append("/".Substring(NextInt(random, 0, 1)));
                        sb.Append(">".Substring(NextInt(random, 0, 1)));
						break;
					}

					case 2:
					{
						sb.Append("</");
                        sb.Append("    ".Substring(NextInt(random, 0, 4)));
						sb.Append(RandomSimpleString(random));
                        sb.Append("    ".Substring(NextInt(random, 0, 4)));
                        sb.Append(">".Substring(NextInt(random, 0, 1)));
						break;
					}

					case 3:
					{
						sb.Append(">");
						break;
					}

					case 4:
					{
						sb.Append("</p>");
						break;
					}

					case 5:
					{
						sb.Append("<!--");
						break;
					}

					case 6:
					{
						sb.Append("<!--#");
						break;
					}

					case 7:
					{
						sb.Append("<script><!-- f('");
						break;
					}

					case 8:
					{
						sb.Append("</script>");
						break;
					}

					case 9:
					{
						sb.Append("<?");
						break;
					}

					case 10:
					{
						sb.Append("?>");
						break;
					}

					case 11:
					{
						sb.Append("\"");
						break;
					}

					case 12:
					{
						sb.Append("\\\"");
						break;
					}

					case 13:
					{
						sb.Append("'");
						break;
					}

					case 14:
					{
						sb.Append("\\'");
						break;
					}

					case 15:
					{
						sb.Append("-->");
						break;
					}

					case 16:
					{
						sb.Append("&");
						switch (NextInt(random, 0, 2))
						{
							case 0:
							{
								sb.Append(RandomSimpleString(random));
								break;
							}

							case 1:
							{
								sb.Append(HTML_CHAR_ENTITIES[random.Next(HTML_CHAR_ENTITIES.Length)]);
								break;
							}
						}
                        sb.Append(";".Substring(NextInt(random, 0, 1)));
						break;
					}

					case 17:
					{
						sb.Append("&#");
						if (0 == NextInt(random, 0, 1))
						{
							sb.Append(NextInt(random, 0, int.MaxValue - 1));
                            sb.Append(";".Substring(NextInt(random, 0, 1)));
						}
						break;
					}

					case 18:
					{
						sb.Append("&#x");
						if (0 == NextInt(random, 0, 1))
						{
                            sb.Append(NextInt(random, 0, int.MaxValue - 1).ToString("x"));
                            sb.Append(";".Substring(NextInt(random, 0, 1)));
						}
						break;
					}

					case 19:
					{
						sb.Append(";");
						break;
					}

					case 20:
					{
						sb.Append(NextInt(random, 0, int.MaxValue - 1));
						break;
					}

					case 21:
					{
						sb.Append("\n");
						break;
					}

					case 22:
					{
                        sb.Append("          ".Substring(NextInt(random, 0, 10)));
						break;
					}

					case 23:
					{
						sb.Append("<");
						if (0 == NextInt(random, 0, 3))
						{
                            sb.Append("          ".Substring(NextInt(random, 1, 10)));
						}
						if (0 == NextInt(random, 0, 1))
						{
							sb.Append("/");
							if (0 == NextInt(random, 0, 3))
							{
                                sb.Append("          ".Substring(NextInt(random, 1, 10)));
							}
						}
						switch (NextInt(random, 0, 3))
						{
							case 0:
							{
								sb.Append(RandomlyRecaseCodePoints(random, "script"));
								break;
							}

							case 1:
							{
								sb.Append(RandomlyRecaseCodePoints(random, "style"));
								break;
							}

							case 2:
							{
								sb.Append(RandomlyRecaseCodePoints(random, "br"));
								break;
							}
						}
						// default: append nothing
                        sb.Append(">".Substring(NextInt(random, 0, 1)));
						break;
					}

					default:
					{
						sb.Append(RandomSimpleString(random));
						break;
					}
				}
			}
			return sb.ToString();
		}

		/// <summary>Randomly upcases, downcases, or leaves intact each code point in the given string
		/// 	</summary>
		public static string RandomlyRecaseCodePoints(Random random, string str)
		{
			StringBuilder builder = new StringBuilder();
			int pos = 0;
            
			while (pos < str.Length)
			{
				int codePoint = str.ToCharArray()[pos];
				pos += Character.CharCount(codePoint);
				switch (NextInt(random, 0, 2))
				{
					case 0:
					{
						builder.Append(char.ToUpper(str[codePoint]));
						break;
					}

					case 1:
					{
						builder.Append(char.ToLower(str[codePoint]));
						break;
					}

					case 2:
					{
						builder.Append(str[codePoint]);
					}
				        break;
				}
			}
			// leave intact
			return builder.ToString();
		}

		private static readonly int[] blockStarts = new int[] { unchecked((int)(0x0000)), 
			unchecked((int)(0x0080)), unchecked((int)(0x0100)), unchecked((int)(0x0180)), unchecked(
			(int)(0x0250)), unchecked((int)(0x02B0)), unchecked((int)(0x0300)), unchecked((int
			)(0x0370)), unchecked((int)(0x0400)), unchecked((int)(0x0500)), unchecked((int)(
			0x0530)), unchecked((int)(0x0590)), unchecked((int)(0x0600)), unchecked((int)(0x0700
			)), unchecked((int)(0x0750)), unchecked((int)(0x0780)), unchecked((int)(0x07C0))
			, unchecked((int)(0x0800)), unchecked((int)(0x0900)), unchecked((int)(0x0980)), 
			unchecked((int)(0x0A00)), unchecked((int)(0x0A80)), unchecked((int)(0x0B00)), unchecked(
			(int)(0x0B80)), unchecked((int)(0x0C00)), unchecked((int)(0x0C80)), unchecked((int
			)(0x0D00)), unchecked((int)(0x0D80)), unchecked((int)(0x0E00)), unchecked((int)(
			0x0E80)), unchecked((int)(0x0F00)), unchecked((int)(0x1000)), unchecked((int)(0x10A0
			)), unchecked((int)(0x1100)), unchecked((int)(0x1200)), unchecked((int)(0x1380))
			, unchecked((int)(0x13A0)), unchecked((int)(0x1400)), unchecked((int)(0x1680)), 
			unchecked((int)(0x16A0)), unchecked((int)(0x1700)), unchecked((int)(0x1720)), unchecked(
			(int)(0x1740)), unchecked((int)(0x1760)), unchecked((int)(0x1780)), unchecked((int
			)(0x1800)), unchecked((int)(0x18B0)), unchecked((int)(0x1900)), unchecked((int)(
			0x1950)), unchecked((int)(0x1980)), unchecked((int)(0x19E0)), unchecked((int)(0x1A00
			)), unchecked((int)(0x1A20)), unchecked((int)(0x1B00)), unchecked((int)(0x1B80))
			, unchecked((int)(0x1C00)), unchecked((int)(0x1C50)), unchecked((int)(0x1CD0)), 
			unchecked((int)(0x1D00)), unchecked((int)(0x1D80)), unchecked((int)(0x1DC0)), unchecked(
			(int)(0x1E00)), unchecked((int)(0x1F00)), unchecked((int)(0x2000)), unchecked((int
			)(0x2070)), unchecked((int)(0x20A0)), unchecked((int)(0x20D0)), unchecked((int)(
			0x2100)), unchecked((int)(0x2150)), unchecked((int)(0x2190)), unchecked((int)(0x2200
			)), unchecked((int)(0x2300)), unchecked((int)(0x2400)), unchecked((int)(0x2440))
			, unchecked((int)(0x2460)), unchecked((int)(0x2500)), unchecked((int)(0x2580)), 
			unchecked((int)(0x25A0)), unchecked((int)(0x2600)), unchecked((int)(0x2700)), unchecked(
			(int)(0x27C0)), unchecked((int)(0x27F0)), unchecked((int)(0x2800)), unchecked((int
			)(0x2900)), unchecked((int)(0x2980)), unchecked((int)(0x2A00)), unchecked((int)(
			0x2B00)), unchecked((int)(0x2C00)), unchecked((int)(0x2C60)), unchecked((int)(0x2C80
			)), unchecked((int)(0x2D00)), unchecked((int)(0x2D30)), unchecked((int)(0x2D80))
			, unchecked((int)(0x2DE0)), unchecked((int)(0x2E00)), unchecked((int)(0x2E80)), 
			unchecked((int)(0x2F00)), unchecked((int)(0x2FF0)), unchecked((int)(0x3000)), unchecked(
			(int)(0x3040)), unchecked((int)(0x30A0)), unchecked((int)(0x3100)), unchecked((int
			)(0x3130)), unchecked((int)(0x3190)), unchecked((int)(0x31A0)), unchecked((int)(
			0x31C0)), unchecked((int)(0x31F0)), unchecked((int)(0x3200)), unchecked((int)(0x3300
			)), unchecked((int)(0x3400)), unchecked((int)(0x4DC0)), unchecked((int)(0x4E00))
			, unchecked((int)(0xA000)), unchecked((int)(0xA490)), unchecked((int)(0xA4D0)), 
			unchecked((int)(0xA500)), unchecked((int)(0xA640)), unchecked((int)(0xA6A0)), unchecked(
			(int)(0xA700)), unchecked((int)(0xA720)), unchecked((int)(0xA800)), unchecked((int
			)(0xA830)), unchecked((int)(0xA840)), unchecked((int)(0xA880)), unchecked((int)(
			0xA8E0)), unchecked((int)(0xA900)), unchecked((int)(0xA930)), unchecked((int)(0xA960
			)), unchecked((int)(0xA980)), unchecked((int)(0xAA00)), unchecked((int)(0xAA60))
			, unchecked((int)(0xAA80)), unchecked((int)(0xABC0)), unchecked((int)(0xAC00)), 
			unchecked((int)(0xD7B0)), unchecked((int)(0xE000)), unchecked((int)(0xF900)), unchecked(
			(int)(0xFB00)), unchecked((int)(0xFB50)), unchecked((int)(0xFE00)), unchecked((int
			)(0xFE10)), unchecked((int)(0xFE20)), unchecked((int)(0xFE30)), unchecked((int)(
			0xFE50)), unchecked((int)(0xFE70)), unchecked((int)(0xFF00)), unchecked((int)(0xFFF0
			)), unchecked((int)(0x10000)), unchecked((int)(0x10080)), unchecked((int)(0x10100
			)), unchecked((int)(0x10140)), unchecked((int)(0x10190)), unchecked((int)(0x101D0
			)), unchecked((int)(0x10280)), unchecked((int)(0x102A0)), unchecked((int)(0x10300
			)), unchecked((int)(0x10330)), unchecked((int)(0x10380)), unchecked((int)(0x103A0
			)), unchecked((int)(0x10400)), unchecked((int)(0x10450)), unchecked((int)(0x10480
			)), unchecked((int)(0x10800)), unchecked((int)(0x10840)), unchecked((int)(0x10900
			)), unchecked((int)(0x10920)), unchecked((int)(0x10A00)), unchecked((int)(0x10A60
			)), unchecked((int)(0x10B00)), unchecked((int)(0x10B40)), unchecked((int)(0x10B60
			)), unchecked((int)(0x10C00)), unchecked((int)(0x10E60)), unchecked((int)(0x11080
			)), unchecked((int)(0x12000)), unchecked((int)(0x12400)), unchecked((int)(0x13000
			)), unchecked((int)(0x1D000)), unchecked((int)(0x1D100)), unchecked((int)(0x1D200
			)), unchecked((int)(0x1D300)), unchecked((int)(0x1D360)), unchecked((int)(0x1D400
			)), unchecked((int)(0x1F000)), unchecked((int)(0x1F030)), unchecked((int)(0x1F100
			)), unchecked((int)(0x1F200)), unchecked((int)(0x20000)), unchecked((int)(0x2A700
			)), unchecked((int)(0x2F800)), unchecked((int)(0xE0000)), unchecked((int)(0xE0100
			)), unchecked((int)(0xF0000)), unchecked((int)(0x100000)) };

		private static readonly int[] blockEnds = new int[] { unchecked((int)(0x007F)), unchecked(
			(int)(0x00FF)), unchecked((int)(0x017F)), unchecked((int)(0x024F)), unchecked((int
			)(0x02AF)), unchecked((int)(0x02FF)), unchecked((int)(0x036F)), unchecked((int)(
			0x03FF)), unchecked((int)(0x04FF)), unchecked((int)(0x052F)), unchecked((int)(0x058F
			)), unchecked((int)(0x05FF)), unchecked((int)(0x06FF)), unchecked((int)(0x074F))
			, unchecked((int)(0x077F)), unchecked((int)(0x07BF)), unchecked((int)(0x07FF)), 
			unchecked((int)(0x083F)), unchecked((int)(0x097F)), unchecked((int)(0x09FF)), unchecked(
			(int)(0x0A7F)), unchecked((int)(0x0AFF)), unchecked((int)(0x0B7F)), unchecked((int
			)(0x0BFF)), unchecked((int)(0x0C7F)), unchecked((int)(0x0CFF)), unchecked((int)(
			0x0D7F)), unchecked((int)(0x0DFF)), unchecked((int)(0x0E7F)), unchecked((int)(0x0EFF
			)), unchecked((int)(0x0FFF)), unchecked((int)(0x109F)), unchecked((int)(0x10FF))
			, unchecked((int)(0x11FF)), unchecked((int)(0x137F)), unchecked((int)(0x139F)), 
			unchecked((int)(0x13FF)), unchecked((int)(0x167F)), unchecked((int)(0x169F)), unchecked(
			(int)(0x16FF)), unchecked((int)(0x171F)), unchecked((int)(0x173F)), unchecked((int
			)(0x175F)), unchecked((int)(0x177F)), unchecked((int)(0x17FF)), unchecked((int)(
			0x18AF)), unchecked((int)(0x18FF)), unchecked((int)(0x194F)), unchecked((int)(0x197F
			)), unchecked((int)(0x19DF)), unchecked((int)(0x19FF)), unchecked((int)(0x1A1F))
			, unchecked((int)(0x1AAF)), unchecked((int)(0x1B7F)), unchecked((int)(0x1BBF)), 
			unchecked((int)(0x1C4F)), unchecked((int)(0x1C7F)), unchecked((int)(0x1CFF)), unchecked(
			(int)(0x1D7F)), unchecked((int)(0x1DBF)), unchecked((int)(0x1DFF)), unchecked((int
			)(0x1EFF)), unchecked((int)(0x1FFF)), unchecked((int)(0x206F)), unchecked((int)(
			0x209F)), unchecked((int)(0x20CF)), unchecked((int)(0x20FF)), unchecked((int)(0x214F
			)), unchecked((int)(0x218F)), unchecked((int)(0x21FF)), unchecked((int)(0x22FF))
			, unchecked((int)(0x23FF)), unchecked((int)(0x243F)), unchecked((int)(0x245F)), 
			unchecked((int)(0x24FF)), unchecked((int)(0x257F)), unchecked((int)(0x259F)), unchecked(
			(int)(0x25FF)), unchecked((int)(0x26FF)), unchecked((int)(0x27BF)), unchecked((int
			)(0x27EF)), unchecked((int)(0x27FF)), unchecked((int)(0x28FF)), unchecked((int)(
			0x297F)), unchecked((int)(0x29FF)), unchecked((int)(0x2AFF)), unchecked((int)(0x2BFF
			)), unchecked((int)(0x2C5F)), unchecked((int)(0x2C7F)), unchecked((int)(0x2CFF))
			, unchecked((int)(0x2D2F)), unchecked((int)(0x2D7F)), unchecked((int)(0x2DDF)), 
			unchecked((int)(0x2DFF)), unchecked((int)(0x2E7F)), unchecked((int)(0x2EFF)), unchecked(
			(int)(0x2FDF)), unchecked((int)(0x2FFF)), unchecked((int)(0x303F)), unchecked((int
			)(0x309F)), unchecked((int)(0x30FF)), unchecked((int)(0x312F)), unchecked((int)(
			0x318F)), unchecked((int)(0x319F)), unchecked((int)(0x31BF)), unchecked((int)(0x31EF
			)), unchecked((int)(0x31FF)), unchecked((int)(0x32FF)), unchecked((int)(0x33FF))
			, unchecked((int)(0x4DBF)), unchecked((int)(0x4DFF)), unchecked((int)(0x9FFF)), 
			unchecked((int)(0xA48F)), unchecked((int)(0xA4CF)), unchecked((int)(0xA4FF)), unchecked(
			(int)(0xA63F)), unchecked((int)(0xA69F)), unchecked((int)(0xA6FF)), unchecked((int
			)(0xA71F)), unchecked((int)(0xA7FF)), unchecked((int)(0xA82F)), unchecked((int)(
			0xA83F)), unchecked((int)(0xA87F)), unchecked((int)(0xA8DF)), unchecked((int)(0xA8FF
			)), unchecked((int)(0xA92F)), unchecked((int)(0xA95F)), unchecked((int)(0xA97F))
			, unchecked((int)(0xA9DF)), unchecked((int)(0xAA5F)), unchecked((int)(0xAA7F)), 
			unchecked((int)(0xAADF)), unchecked((int)(0xABFF)), unchecked((int)(0xD7AF)), unchecked(
			(int)(0xD7FF)), unchecked((int)(0xF8FF)), unchecked((int)(0xFAFF)), unchecked((int
			)(0xFB4F)), unchecked((int)(0xFDFF)), unchecked((int)(0xFE0F)), unchecked((int)(
			0xFE1F)), unchecked((int)(0xFE2F)), unchecked((int)(0xFE4F)), unchecked((int)(0xFE6F
			)), unchecked((int)(0xFEFF)), unchecked((int)(0xFFEF)), unchecked((int)(0xFFFF))
			, unchecked((int)(0x1007F)), unchecked((int)(0x100FF)), unchecked((int)(0x1013F)
			), unchecked((int)(0x1018F)), unchecked((int)(0x101CF)), unchecked((int)(0x101FF
			)), unchecked((int)(0x1029F)), unchecked((int)(0x102DF)), unchecked((int)(0x1032F
			)), unchecked((int)(0x1034F)), unchecked((int)(0x1039F)), unchecked((int)(0x103DF
			)), unchecked((int)(0x1044F)), unchecked((int)(0x1047F)), unchecked((int)(0x104AF
			)), unchecked((int)(0x1083F)), unchecked((int)(0x1085F)), unchecked((int)(0x1091F
			)), unchecked((int)(0x1093F)), unchecked((int)(0x10A5F)), unchecked((int)(0x10A7F
			)), unchecked((int)(0x10B3F)), unchecked((int)(0x10B5F)), unchecked((int)(0x10B7F
			)), unchecked((int)(0x10C4F)), unchecked((int)(0x10E7F)), unchecked((int)(0x110CF
			)), unchecked((int)(0x123FF)), unchecked((int)(0x1247F)), unchecked((int)(0x1342F
			)), unchecked((int)(0x1D0FF)), unchecked((int)(0x1D1FF)), unchecked((int)(0x1D24F
			)), unchecked((int)(0x1D35F)), unchecked((int)(0x1D37F)), unchecked((int)(0x1D7FF
			)), unchecked((int)(0x1F02F)), unchecked((int)(0x1F09F)), unchecked((int)(0x1F1FF
			)), unchecked((int)(0x1F2FF)), unchecked((int)(0x2A6DF)), unchecked((int)(0x2B73F
			)), unchecked((int)(0x2FA1F)), unchecked((int)(0xE007F)), unchecked((int)(0xE01EF
			)), unchecked((int)(0xFFFFF)), unchecked((int)(0x10FFFF)) };

		/// <summary>Returns random string of length between 0-20 codepoints, all codepoints within the same unicode block.
		/// 	</summary>
		/// <remarks>Returns random string of length between 0-20 codepoints, all codepoints within the same unicode block.
		/// 	</remarks>
		public static string RandomRealisticUnicodeString(Random r)
		{
			return RandomRealisticUnicodeString(r, 20);
		}

		/// <summary>Returns random string of length up to maxLength codepoints , all codepoints within the same unicode block.
		/// 	</summary>
		/// <remarks>Returns random string of length up to maxLength codepoints , all codepoints within the same unicode block.
		/// 	</remarks>
		public static string RandomRealisticUnicodeString(Random r, int maxLength)
		{
			return RandomRealisticUnicodeString(r, 0, maxLength);
		}

		/// <summary>Returns random string of length between min and max codepoints, all codepoints within the same unicode block.
		/// 	</summary>
		/// <remarks>Returns random string of length between min and max codepoints, all codepoints within the same unicode block.
		/// 	</remarks>
		public static string RandomRealisticUnicodeString(Random r, int minLength, int maxLength)
		{
			int end = NextInt(r, minLength, maxLength);
			int block = r.Next(blockStarts.Length);
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < end; i++)
			{
				sb.Append(NextInt(r, blockStarts[block], blockEnds[block]));
			}
			return sb.ToString();
		}

		/// <summary>Returns random string, with a given UTF-8 byte length</summary>
		public static string RandomFixedByteLengthUnicodeString(Random r, int length)
		{
			char[] buffer = new char[length * 3];
			int bytes = length;
			int i = 0;
			for (; i < buffer.Length && bytes != 0; i++)
			{
				int t;
				if (bytes >= 4)
				{
					t = r.Next(5);
				}
				else
				{
					if (bytes >= 3)
					{
						t = r.Next(4);
					}
					else
					{
						if (bytes >= 2)
						{
							t = r.Next(2);
						}
						else
						{
							t = 0;
						}
					}
				}
				if (t == 0)
				{
					buffer[i] = (char)r.Next(unchecked((int)(0x80)));
					bytes--;
				}
				else
				{
					if (1 == t)
					{
						buffer[i] = (char)NextInt(r, unchecked((int)(0x80)), unchecked((int)(0x7ff)));
						bytes -= 2;
					}
					else
					{
						if (2 == t)
						{
							buffer[i] = (char)NextInt(r, unchecked((int)(0x800)), unchecked((int)(0xd7ff)));
							bytes -= 3;
						}
						else
						{
							if (3 == t)
							{
								buffer[i] = (char)NextInt(r, unchecked((int)(0xe000)), unchecked((int)(0xffff)));
								bytes -= 3;
							}
							else
							{
								if (4 == t)
								{
									// Make a surrogate pair
									// High surrogate
									buffer[i++] = (char)NextInt(r, unchecked((int)(0xd800)), unchecked((int)(0xdbff))
										);
									// Low surrogate
									buffer[i] = (char)NextInt(r, unchecked((int)(0xdc00)), unchecked((int)(0xdfff)));
									bytes -= 4;
								}
							}
						}
					}
				}
			}
			return new string(buffer, 0, i);
		}

		/// <summary>
		/// Return a Codec that can read any of the
		/// default codecs and formats, but always writes in the specified
		/// format.
		/// </summary>
		/// <remarks>
		/// Return a Codec that can read any of the
		/// default codecs and formats, but always writes in the specified
		/// format.
		/// </remarks>
		public static Codec AlwaysPostingsFormat(PostingsFormat format)
		{
			// TODO: we really need for postings impls etc to announce themselves
			// (and maybe their params, too) to infostream on flush and merge.
			// otherwise in a real debugging situation we won't know whats going on!
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("forcing postings format to:" + format);
			}
			return new AnonymousLucene46Codec(format);
		}

		private sealed class AnonymousLucene46Codec : Lucene46Codec
		{
			public AnonymousLucene46Codec(PostingsFormat format)
			{
				this.format = format;
			}

			public override PostingsFormat GetPostingsFormatForField(string field)
			{
				return format;
			}

			private readonly PostingsFormat format;
		}

		/// <summary>
		/// Return a Codec that can read any of the
		/// default codecs and formats, but always writes in the specified
		/// format.
		/// </summary>
		/// <remarks>
		/// Return a Codec that can read any of the
		/// default codecs and formats, but always writes in the specified
		/// format.
		/// </remarks>
		public static Codec AlwaysDocValuesFormat(DocValuesFormat format)
		{
			// TODO: we really need for docvalues impls etc to announce themselves
			// (and maybe their params, too) to infostream on flush and merge.
			// otherwise in a real debugging situation we won't know whats going on!
			if (LuceneTestCase.VERBOSE)
			{
				System.Console.Out.WriteLine("forcing docvalues format to:" + format);
			}
			return new AnonymousLucene46CodecDocFormat(format);
		}

		private sealed class AnonymousLucene46CodecDocFormat : Lucene46Codec
		{
			public AnonymousLucene46CodecDocFormat(DocValuesFormat format)
			{
				this.format = format;
			}

			public override DocValuesFormat GetDocValuesFormatForField(string field)
			{
				return format;
			}

			private readonly DocValuesFormat format;
		}

		// TODO: generalize all 'test-checks-for-crazy-codecs' to
		// annotations (LUCENE-3489)
		public static string GetPostingsFormat(string field)
		{
			return GetPostingsFormat(Codec.Default, field);
		}

		public static string GetPostingsFormat(Codec codec, string field)
		{
			PostingsFormat p = codec.PostingsFormat;
			if (p is PerFieldPostingsFormat)
			{
				return ((PerFieldPostingsFormat)p).GetPostingsFormatForField(field).Name;
			}
		    return p.Name;
		}

		public static string GetDocValuesFormat(string field)
		{
			return GetDocValuesFormat(Codec.Default, field);
		}

		public static string GetDocValuesFormat(Codec codec, string field)
		{
			DocValuesFormat f = codec.DocValuesFormat;
			if (f is PerFieldDocValuesFormat)
			{
				return ((PerFieldDocValuesFormat)f).GetDocValuesFormatForField(field).Name;
			}
		    return f.Name;
		}

		// TODO: remove this, push this test to Lucene40/Lucene42 codec tests
		public static bool FieldSupportsHugeBinaryDocValues(string field)
		{
			string dvFormat = GetDocValuesFormat(field);
			if (dvFormat.Equals("Lucene40") || dvFormat.Equals("Lucene42") || dvFormat.Equals
				("Memory"))
			{
				return false;
			}
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static bool AnyFilesExceptWriteLock(Lucene.Net.Store.Directory dir)
		{
			string[] files = dir.ListAll();
			return files.Length > 1 || (files.Length == 1 && !files[0].Equals("write.lock"));
		}

		/// <summary>
		/// just tries to configure things to keep the open file
		/// count lowish
		/// </summary>
		public static void ReduceOpenFiles(IndexWriter w)
		{
			// keep number of open files lowish
			MergePolicy mp = w.Config.MergePolicy;
			if (mp is LogMergePolicy)
			{
				LogMergePolicy lmp = (LogMergePolicy)mp;
				lmp.MergeFactor = (Math.Min(5, lmp.MergeFactor));
				lmp.SetNoCFSRatio(1.0);
			}
			else
			{
				if (mp is TieredMergePolicy)
				{
					TieredMergePolicy tmp = (TieredMergePolicy)mp;
					tmp.SetMaxMergeAtOnce(Math.Min(5, tmp.MaxMergeAtOnce));
					tmp.SetSegmentsPerTier(Math.Min(5, tmp.SegmentsPerTier));
					tmp.SetNoCFSRatio(1.0);
				}
			}
			MergeScheduler ms = w.Config.MergeScheduler;
			if (ms is ConcurrentMergeScheduler)
			{
				// wtf... shouldnt it be even lower since its 1 by default?!?!
				((ConcurrentMergeScheduler)ms).SetMaxMergesAndThreads(3, 2);
			}
		}

		/// <summary>Checks some basic behaviour of an AttributeImpl</summary>
		/// <param name="reflectedValues">contains a map with "AttributeClass#key" as values</param>
		public static void AssertAttributeReflection<T>(Net.Util.Attribute att, IDictionary<string, T> reflectedValues)
		{
			IDictionary<string, object> map = new Dictionary<string, object>();
			att.ReflectWith(new AnonymousAttributeReflectorImpl(map));
		}

		private sealed class AnonymousAttributeReflectorImpl : IAttributeReflector
		{
			public AnonymousAttributeReflectorImpl(IDictionary<string, object> map)
			{
				this.map = map;
			}

		    public void Reflect<T>(string key, object value) where T : IAttribute
		    {
		        throw new NotImplementedException();
		    }

		    public void Reflect(Type attClass, string key, object value)
			{
				map[attClass.FullName + '#' + key] = value;
			}

			private readonly IDictionary<string, object> map;
		}

		 
		//assert.assertEquals("Reflection does not produce same map", reflectedValues, map);
		public static void AssertEquals(TopDocs expected, TopDocs actual)
		{
			 
			//assert.assertEquals("wrong total hits", expected.totalHits, actual.totalHits);
			 
			//assert.assertEquals("wrong maxScore", expected.getMaxScore(), actual.getMaxScore(), 0.0);
			 
			//assert.assertEquals("wrong hit count", expected.scoreDocs.length, actual.scoreDocs.length);
			for (int hitIDX = 0; hitIDX < expected.ScoreDocs.Length; hitIDX++)
			{
				ScoreDoc expectedSD = expected.ScoreDocs[hitIDX];
				ScoreDoc actualSD = actual.ScoreDocs[hitIDX];
				 
				//assert.assertEquals("wrong hit docID", expectedSD.doc, actualSD.doc);
				 
				//assert.assertEquals("wrong hit score", expectedSD.score, actualSD.score, 0.0);
				if (expectedSD is FieldDoc)
				{
				}
			}
		}

		 
		//assert.assertTrue(actualSD instanceof FieldDoc);
		 
		 
		//assert.assertFalse(actualSD instanceof FieldDoc);
		// NOTE: this is likely buggy, and cannot clone fields
		// with tokenStreamValues, etc.  Use at your own risk!!
		// TODO: is there a pre-existing way to do this!!!
		public static Document CloneDocument(Document doc1)
		{
			var doc2 = new Document();
			foreach (IIndexableField f in doc1.GetFields())
			{
				var field1 = f;
				Field field2;
				FieldInfo.DocValuesType dvType = field1.FieldTypeValue.DocValueType.Value;
			    FieldType.NumericType numType = FieldType.NumericType.INT;
			    switch (field1.NumericValue.GetType().Name)
			    {
			        
                    case "Int64":
                        numType = FieldType.NumericType.LONG;
                        break;
                    case "Single":
                        numType = FieldType.NumericType.FLOAT;
                        break;
                    case "Double":
                        numType = FieldType.NumericType.DOUBLE;
                        break;
			    }
			    if (dvType != null)
				{
					switch (dvType)
					{
						case FieldInfo.DocValuesType.NUMERIC:
						{
							field2 = new NumericDocValuesField(field1.Name, (long) field1.NumericValue);
							break;
						}

						case FieldInfo.DocValuesType.BINARY:
						{
							field2 = new BinaryDocValuesField(field1.Name, field1.BinaryValue);
							break;
						}

						case FieldInfo.DocValuesType.SORTED:
						{
							field2 = new SortedDocValuesField(field1.Name, field1.BinaryValue);
							break;
						}

						default:
						{
							throw new InvalidOperationException("unknown Type: " + dvType);
						}
					}
				}
				else
			    {
			        switch (numType)
			        {
			            case FieldType.NumericType.INT:
			            {
			                field2 = new IntField(field1.Name, (int) field1.NumericValue, (FieldType) field1.FieldTypeValue);
			                break;
			            }

			            case FieldType.NumericType.FLOAT:
			            {
			                field2 = new FloatField(field1.Name, (float) field1.NumericValue, (FieldType) field1.FieldTypeValue);
			                break;
			            }

			            case FieldType.NumericType.LONG:
			            {
			                field2 = new LongField(field1.Name, (long) field1.NumericValue, (FieldType) field1.FieldTypeValue);
			                break;
			            }

			            case FieldType.NumericType.DOUBLE:
			            {
			                field2 = new DoubleField(field1.Name, (double) field1.NumericValue, (FieldType) field1.FieldTypeValue);
			                break;
			            }

			            default:
			            {
			                throw new InvalidOperationException("unknown Type: " + numType);
			            }
			        }
			    }
			    doc2.Add(field2);
			}
			return doc2;
		}

		// Returns a DocsEnum, but randomly sometimes uses a
		// DocsAndFreqsEnum, DocsAndPositionsEnum.  Returns null
		// if field/term doesn't exist:
		/// <exception cref="System.IO.IOException"></exception>
		public static DocsEnum Docs(Random random, IndexReader r, string field, BytesRef term, IBits liveDocs, DocsEnum reuse, int flags)
		{
			Terms terms = MultiFields.GetTerms(r, field);
			if (terms == null)
			{
				return null;
			}
			TermsEnum termsEnum = terms.Iterator(null);
			if (!termsEnum.SeekExact(term))
			{
				return null;
			}
			return Docs(random, termsEnum, liveDocs, reuse, flags);
		}

		// Returns a DocsEnum from a positioned TermsEnum, but
		// randomly sometimes uses a DocsAndFreqsEnum, DocsAndPositionsEnum.
		/// <exception cref="System.IO.IOException"></exception>
		public static DocsEnum Docs(Random random, TermsEnum termsEnum, IBits liveDocs, DocsEnum
			 reuse, int flags)
		{
			if (random.NextBoolean())
			{
				if (random.NextBoolean())
				{
					int posFlags;
					switch (random.Next(4))
					{
						case 0:
						{
							posFlags = 0;
							break;
						}

						case 1:
						{
							posFlags = DocsAndPositionsEnum.FLAG_OFFSETS;
							break;
						}

						case 2:
						{
							posFlags = DocsAndPositionsEnum.FLAG_PAYLOADS;
							break;
						}

						default:
						{
							posFlags = DocsAndPositionsEnum.FLAG_OFFSETS | DocsAndPositionsEnum.FLAG_PAYLOADS;
							break;
						}
					}
					// TODO: cast to DocsAndPositionsEnum?
					DocsAndPositionsEnum docsAndPositions = termsEnum.DocsAndPositions(liveDocs, null
						, posFlags);
					if (docsAndPositions != null)
					{
						return docsAndPositions;
					}
				}
				flags |= DocsEnum.FLAG_FREQS;
			}
			return termsEnum.Docs(liveDocs, reuse, flags);
		}

		public static ICharSequence StringToCharSequence(string @string, Random random)
		{
			return BytesToCharSequence(new BytesRef(@string), random);
		}

		public static ICharSequence BytesToCharSequence(BytesRef @ref, Random random)
		{
			switch (random.Next(5))
			{
				case 4:
				{
					CharsRef chars = new CharsRef(@ref.length);
					UnicodeUtil.UTF8toUTF16(@ref.bytes, @ref.offset, @ref.length, chars);
					return chars;
				}

				case 3:
				{
					return new StringCharSequenceWrapper(@ref.Utf8ToString()); 
				}

				default:
			    {
			        return StringToCharSequence(@ref.Utf8ToString(), LuceneTestCase.Random());
			    }
			}
		}

		
		public static void ShutdownExecutorService(TaskScheduler ex)
		{
            //if (ex != null)
            //{
            //    try
            //    {
            //        ex.Shutdown();
            //        ex.AwaitTermination(1, TimeUnit.SECONDS);
            //    }
            //    catch (Exception e)
            //    {
            //        // Just report it on the syserr.
            //        Console.Error.WriteLine("Could not properly shutdown executor service.");
					
            //    }
            //}
		}

		

		// Loop trying until we hit something that compiles.
		public static FilteredQuery.RandomAccessFilterStrategy RandomFilterStrategy(Random random)
		{
			switch (random.Next(6))
			{
				case 5:
				case 4:
				{
					return new AnonymousRandAccessFilterStrategy();
				}

				case 3:
				{
					return (FilteredQuery.RandomAccessFilterStrategy) FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY;
				}

				case 2:
				{
					return (FilteredQuery.RandomAccessFilterStrategy) FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY;
				}

				case 1:
				{
					return (FilteredQuery.RandomAccessFilterStrategy) FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY;
				}

				case 0:
				{
					return (FilteredQuery.RandomAccessFilterStrategy) FilteredQuery.QUERY_FIRST_FILTER_STRATEGY;
				}

				default:
				{
					return (FilteredQuery.RandomAccessFilterStrategy) FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY;
				}
			}
		}

		private sealed class AnonymousRandAccessFilterStrategy : FilteredQuery.RandomAccessFilterStrategy
		{
		    protected override bool UseRandomAccess(IBits bits, int firstFilterDoc)
			{
				return LuceneTestCase.Random().NextBoolean();
			}
		}

		/// <summary>
		/// Returns a random string in the specified length range consisting
		/// entirely of whitespace characters
		/// </summary>
		/// <seealso cref="WHITESPACE_CHARACTERS">WHITESPACE_CHARACTERS</seealso>
		public static string RandomWhitespace(Random r, int minLength, int maxLength)
		{
			int end = NextInt(r, minLength, maxLength);
			StringBuilder @out = new StringBuilder();
			for (int i = 0; i < end; i++)
			{
				int offset = NextInt(r, 0, WHITESPACE_CHARACTERS.Length - 1);
				char c = WHITESPACE_CHARACTERS[offset];
				// sanity check
				
				//assert.assertTrue("Not really whitespace? (@"+offset+"): " + c, Character.isWhitespace(c));
				@out.Append(c);
			}
			return @out.ToString();
		}

		public static string RandomAnalysisString(Random random, int maxLength, bool simple)
		{
			
			//assert maxLength >= 0;
			// sometimes just a purely random string
			if (random.Next(31) == 0)
			{
				return RandomSubString(random, random.Next(maxLength), simple);
			}
			// otherwise, try to make it more realistic with 'words' since most tests use MockTokenizer
			// first decide how big the string will really be: 0..n
			maxLength = random.Next(maxLength);
			int avgWordLength = Lucene.Net.TestFramework.Util.TestUtil.NextInt(random, 3, 8);
			StringBuilder sb = new StringBuilder();
			while (sb.Length < maxLength)
			{
				if (sb.Length > 0)
				{
					sb.Append(' ');
				}
				int wordLength = -1;
				while (wordLength < 0)
				{
					wordLength = (int)(random.NextGaussian() * 3 + avgWordLength);
				}
				wordLength = System.Math.Min(wordLength, maxLength - sb.Length);
				sb.Append(RandomSubString(random, wordLength, simple));
			}
			return sb.ToString();
		}

		public static string RandomSubString(Random random, int wordLength, bool simple)
		{
			if (wordLength == 0)
			{
				return string.Empty;
			}
			int evilness = NextInt(random, 0, 20);
			StringBuilder sb = new StringBuilder();
			while (sb.Length < wordLength)
			{
				if (simple)
				{
					sb.Append(random.NextBoolean() ? RandomSimpleString
						(random, wordLength) : RandomHtmlishString(random
						, wordLength));
				}
				else
				{
					if (evilness < 10)
					{
						sb.Append(RandomSimpleString(random, wordLength));
					}
					else
					{
						if (evilness < 15)
						{
							
							//assert sb.length() == 0; // we should always get wordLength back!
							sb.Append(RandomRealisticUnicodeString(random, wordLength, wordLength));
						}
						else
						{
							if (evilness == 16)
							{
								sb.Append(RandomHtmlishString(random, wordLength));
							}
							else
							{
								if (evilness == 17)
								{
									// gives a lot of punctuation
									sb.Append(Lucene.Net.TestFramework.Util.TestUtil.RandomRegexpishString(random, wordLength
										));
								}
								else
								{
									sb.Append(Lucene.Net.TestFramework.Util.TestUtil.RandomUnicodeString(random, wordLength)
										);
								}
							}
						}
					}
				}
			}
			if (sb.Length > wordLength)
			{
				sb.Length = wordLength;
				if (char.IsHighSurrogate(sb[wordLength - 1]))
				{
					sb.Length = wordLength - 1;
				}
			}
			if (random.Next(17) == 0)
			{
				// mix up case
				string mixedUp = Lucene.Net.TestFramework.Util.TestUtil.RandomlyRecaseCodePoints(random, 
					sb.ToString());
				 
				//assert mixedUp.length() == sb.length();
				return mixedUp;
			}
			else
			{
				return sb.ToString();
			}
		}

		/// <summary>
		/// List of characters that match
		/// <see cref="char.IsWhiteSpace(char)">char.IsWhiteSpace(char)</see>
		/// 
		/// </summary>
		public static readonly char[] WHITESPACE_CHARACTERS = new char[] { '\u0009', '\n'
			, '\u000B', '\u000C', '\r', '\u001C', '\u001D', '\u001E', '\u001F', '\u0020', '\u1680'
			, '\u180E', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006'
			, '\u2008', '\u2009', '\u200A', '\u2028', '\u2029', '\u205F', '\u3000' };
		// :TODO: is this list exhaustive?
		// '\u0085', faild sanity check?
	}
}
