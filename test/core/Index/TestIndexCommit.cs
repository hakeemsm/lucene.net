using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestIndexCommit : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestEqualsHashCode()
		{
			// LUCENE-2417: equals and hashCode() impl was inconsistent
			Directory dir = NewDirectory();
			IndexCommit ic1 = new AnonymousIndexCommit(dir);
			IndexCommit ic2 = new AnonymousIndexCommit2(dir);
			AreEqual(ic1, ic2);
			AssertEquals("hash codes are not equals", ic1.GetHashCode(), ic2
				.GetHashCode());
			dir.Dispose();
		}

		private sealed class AnonymousIndexCommit : IndexCommit
		{
			public AnonymousIndexCommit(Directory dir)
			{
				this.dir = dir;
			}

			public override string SegmentsFileName
			{
			    get { return "a"; }
			}

			public override Directory Directory
			{
			    get { return dir; }
			}

			public override ICollection<string> FileNames
			{
			    get { return null; }
			}

			public override void Delete()
			{
			}

			public override long Generation
			{
			    get { return 0; }
			}

			public override IDictionary<string, string> UserData
			{
			    get { return null; }
			}

			public override bool IsDeleted
			{
			    get { return false; }
			}

			public override int SegmentCount
			{
			    get { return 2; }
			}

			private readonly Directory dir;
		}

		private sealed class AnonymousIndexCommit2 : IndexCommit
		{
			public AnonymousIndexCommit2(Directory dir)
			{
				this.dir = dir;
			}

			public override string SegmentsFileName
			{
			    get { return "b"; }
			}

			public override Directory Directory
			{
			    get { return dir; }
			}

			public override ICollection<string> FileNames
			{
			    get { return null; }
			}

			public override void Delete()
			{
			}

			public override long Generation
			{
			    get { return 0; }
			}

			public override IDictionary<string, string> UserData
			{
			    get { return null; }
			}

			public override bool IsDeleted
			{
			    get { return false; }
			}

			public override int SegmentCount
			{
			    get { return 2; }
			}

			private readonly Directory dir;
		}
	}
}
