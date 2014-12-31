/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Util;


namespace Lucene.Net.Util
{
	public class TestConstants : LuceneTestCase
	{
		private string GetVersionDetails()
		{
			return " (LUCENE_MAIN_VERSION=" + Constants.LUCENE_MAIN_VERSION + ", LUCENE_MAIN_VERSION(without alpha/beta)="
				 + Constants.MainVersionWithoutAlphaBeta() + ", LUCENE_VERSION=" + Constants.LUCENE_VERSION
				 + ")";
		}

		public virtual void TestLuceneMainVersionConstant()
		{
			IsTrue("LUCENE_MAIN_VERSION does not follow pattern: 'x.y' (stable release) or 'x.y.0.z' (alpha/beta version)"
				 + GetVersionDetails(), Constants.LUCENE_MAIN_VERSION.Matches("\\d+\\.\\d+(|\\.0\\.\\d+)"
				));
			IsTrue("LUCENE_VERSION does not start with LUCENE_MAIN_VERSION (without alpha/beta marker)"
				 + GetVersionDetails(), Constants.LUCENE_VERSION.StartsWith(Constants.MainVersionWithoutAlphaBeta
				()));
		}

		public virtual void TestBuildSetup()
		{
			// common-build.xml sets lucene.version, if not, we skip this test!
			string version = Runtime.GetProperty("lucene.version");
			AssumeTrue("Null lucene.version test property. You should run the tests with the official Lucene build file"
				, version != null);
			// remove anything after a "-" from the version string:
			version = version.ReplaceAll("-.*$", string.Empty);
			string versionConstant = Constants.LUCENE_VERSION.ReplaceAll("-.*$", string.Empty
				);
			IsTrue("LUCENE_VERSION should share the same prefix with lucene.version test property ('"
				 + version + "')." + GetVersionDetails(), versionConstant.StartsWith(version) ||
				 version.StartsWith(versionConstant));
		}
	}
}
