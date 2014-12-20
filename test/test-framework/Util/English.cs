/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>Converts numbers to english strings for testing.</summary>
	/// <remarks>Converts numbers to english strings for testing.</remarks>
	/// <lucene.internal></lucene.internal>
	public sealed class English
	{
		public English()
		{
		}

		// no instance
		public static string LongToEnglish(long i)
		{
			StringBuilder result = new StringBuilder();
			LongToEnglish(i, result);
			return result.ToString();
		}

		public static void LongToEnglish(long i, StringBuilder result)
		{
			if (i == 0)
			{
				result.Append("zero");
				return;
			}
			if (i < 0)
			{
				result.Append("minus ");
				i = -i;
			}
			if (i >= 1000000000000000000l)
			{
				// quadrillion
				LongToEnglish(i / 1000000000000000000l, result);
				result.Append("quintillion, ");
				i = i % 1000000000000000000l;
			}
			if (i >= 1000000000000000l)
			{
				// quadrillion
				LongToEnglish(i / 1000000000000000l, result);
				result.Append("quadrillion, ");
				i = i % 1000000000000000l;
			}
			if (i >= 1000000000000l)
			{
				// trillions
				LongToEnglish(i / 1000000000000l, result);
				result.Append("trillion, ");
				i = i % 1000000000000l;
			}
			if (i >= 1000000000)
			{
				// billions
				LongToEnglish(i / 1000000000, result);
				result.Append("billion, ");
				i = i % 1000000000;
			}
			if (i >= 1000000)
			{
				// millions
				LongToEnglish(i / 1000000, result);
				result.Append("million, ");
				i = i % 1000000;
			}
			if (i >= 1000)
			{
				// thousands
				LongToEnglish(i / 1000, result);
				result.Append("thousand, ");
				i = i % 1000;
			}
			if (i >= 100)
			{
				// hundreds
				LongToEnglish(i / 100, result);
				result.Append("hundred ");
				i = i % 100;
			}
			//we know we are smaller here so we can cast
			if (i >= 20)
			{
				switch (((int)i) / 10)
				{
					case 9:
					{
						result.Append("ninety");
						break;
					}

					case 8:
					{
						result.Append("eighty");
						break;
					}

					case 7:
					{
						result.Append("seventy");
						break;
					}

					case 6:
					{
						result.Append("sixty");
						break;
					}

					case 5:
					{
						result.Append("fifty");
						break;
					}

					case 4:
					{
						result.Append("forty");
						break;
					}

					case 3:
					{
						result.Append("thirty");
						break;
					}

					case 2:
					{
						result.Append("twenty");
						break;
					}
				}
				i = i % 10;
				if (i == 0)
				{
					result.Append(" ");
				}
				else
				{
					result.Append("-");
				}
			}
			switch ((int)i)
			{
				case 19:
				{
					result.Append("nineteen ");
					break;
				}

				case 18:
				{
					result.Append("eighteen ");
					break;
				}

				case 17:
				{
					result.Append("seventeen ");
					break;
				}

				case 16:
				{
					result.Append("sixteen ");
					break;
				}

				case 15:
				{
					result.Append("fifteen ");
					break;
				}

				case 14:
				{
					result.Append("fourteen ");
					break;
				}

				case 13:
				{
					result.Append("thirteen ");
					break;
				}

				case 12:
				{
					result.Append("twelve ");
					break;
				}

				case 11:
				{
					result.Append("eleven ");
					break;
				}

				case 10:
				{
					result.Append("ten ");
					break;
				}

				case 9:
				{
					result.Append("nine ");
					break;
				}

				case 8:
				{
					result.Append("eight ");
					break;
				}

				case 7:
				{
					result.Append("seven ");
					break;
				}

				case 6:
				{
					result.Append("six ");
					break;
				}

				case 5:
				{
					result.Append("five ");
					break;
				}

				case 4:
				{
					result.Append("four ");
					break;
				}

				case 3:
				{
					result.Append("three ");
					break;
				}

				case 2:
				{
					result.Append("two ");
					break;
				}

				case 1:
				{
					result.Append("one ");
					break;
				}

				case 0:
				{
					result.Append(string.Empty);
					break;
				}
			}
		}

		public static string IntToEnglish(int i)
		{
			StringBuilder result = new StringBuilder();
			LongToEnglish(i, result);
			return result.ToString();
		}

		public static void IntToEnglish(int i, StringBuilder result)
		{
			LongToEnglish(i, result);
		}
	}
}
