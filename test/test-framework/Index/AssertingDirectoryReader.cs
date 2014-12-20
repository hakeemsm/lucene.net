/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.TestFramework.Index;
using Sharpen;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// A
	/// <see cref="DirectoryReader">DirectoryReader</see>
	/// that wraps all its subreaders with
	/// <see cref="AssertingAtomicReader">AssertingAtomicReader</see>
	/// </summary>
	public class AssertingDirectoryReader : FilterDirectoryReader
	{
		internal class AssertingSubReaderWrapper : FilterDirectoryReader.SubReaderWrapper
		{
			public override AtomicReader Wrap(AtomicReader reader)
			{
				return new AssertingAtomicReader(reader);
			}
		}

		public AssertingDirectoryReader(DirectoryReader @in) : base(@in, new AssertingDirectoryReader.AssertingSubReaderWrapper
			())
		{
		}

		protected override DirectoryReader DoWrapDirectoryReader(DirectoryReader @in)
		{
			return new AssertingDirectoryReader(@in);
		}

		public override object GetCoreCacheKey()
		{
			return @in.GetCoreCacheKey();
		}

		public override object GetCombinedCoreAndDeletesKey()
		{
			return @in.GetCombinedCoreAndDeletesKey();
		}
	}
}
