using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Utility class for doing vocabulary-based stemming tests</summary>
	public class VocabularyAssert
	{
		/// <summary>Run a vocabulary test against two data files.</summary>
		public static void AssertVocabulary(Analyzer a, InputStream voc, InputStream @out)
		{
			BufferedReader vocReader = new BufferedReader(new InputStreamReader(voc, StandardCharsets
				.UTF_8));
			BufferedReader outputReader = new BufferedReader(new InputStreamReader(@out, StandardCharsets
				.UTF_8));
			string inputWord = null;
			while ((inputWord = vocReader.ReadLine()) != null)
			{
				string expectedWord = outputReader.ReadLine();
				//HM:revisit 
				//assert.assertNotNull(expectedWord);
				BaseTokenStreamTestCase.CheckOneTerm(a, inputWord, expectedWord);
			}
		}

		/// <summary>Run a vocabulary test against one file: tab separated.</summary>
		/// <remarks>Run a vocabulary test against one file: tab separated.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void AssertVocabulary(Analyzer a, InputStream vocOut)
		{
			BufferedReader vocReader = new BufferedReader(new InputStreamReader(vocOut, StandardCharsets
				.UTF_8));
			string inputLine = null;
			while ((inputLine = vocReader.ReadLine()) != null)
			{
				if (inputLine.StartsWith("#") || inputLine.Trim().Length == 0)
				{
					continue;
				}
				string[] words = inputLine.Split("\t");
				BaseTokenStreamTestCase.CheckOneTerm(a, words[0], words[1]);
			}
		}

		/// <summary>Run a vocabulary test against two data files inside a zip file</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static void AssertVocabulary(Analyzer a, FileInfo zipFile, string voc, string @out)
		{
			ZipFile zip = new ZipFile(zipFile);
			InputStream v = zip.GetInputStream(zip.GetEntry(voc));
			InputStream o = zip.GetInputStream(zip.GetEntry(@out));
			AssertVocabulary(a, v, o);
			v.Close();
			o.Close();
			zip.Close();
		}

		/// <summary>Run a vocabulary test against a tab-separated data file inside a zip file
		/// 	</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static void AssertVocabulary(Analyzer a, FilePath zipFile, string vocOut)
		{
			ZipFile zip = new ZipFile(zipFile);
			InputStream vo = zip.GetInputStream(zip.GetEntry(vocOut));
			AssertVocabulary(a, vo);
			vo.Close();
			zip.Close();
		}
	}
}
