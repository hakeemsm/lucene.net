/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexCommit : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestEqualsHashCode()
		{
			// LUCENE-2417: equals and hashCode() impl was inconsistent
			Directory dir = NewDirectory();
			IndexCommit ic1 = new _IndexCommit_34(dir);
			IndexCommit ic2 = new _IndexCommit_45(dir);
			NUnit.Framework.Assert.AreEqual(ic1, ic2);
			NUnit.Framework.Assert.AreEqual("hash codes are not equals", ic1.GetHashCode(), ic2
				.GetHashCode());
			dir.Close();
		}

		private sealed class _IndexCommit_34 : IndexCommit
		{
			public _IndexCommit_34(Directory dir)
			{
				this.dir = dir;
			}

			public override string GetSegmentsFileName()
			{
				return "a";
			}

			public override Directory GetDirectory()
			{
				return dir;
			}

			public override ICollection<string> GetFileNames()
			{
				return null;
			}

			public override void Delete()
			{
			}

			public override long GetGeneration()
			{
				return 0;
			}

			public override IDictionary<string, string> GetUserData()
			{
				return null;
			}

			public override bool IsDeleted()
			{
				return false;
			}

			public override int GetSegmentCount()
			{
				return 2;
			}

			private readonly Directory dir;
		}

		private sealed class _IndexCommit_45 : IndexCommit
		{
			public _IndexCommit_45(Directory dir)
			{
				this.dir = dir;
			}

			public override string GetSegmentsFileName()
			{
				return "b";
			}

			public override Directory GetDirectory()
			{
				return dir;
			}

			public override ICollection<string> GetFileNames()
			{
				return null;
			}

			public override void Delete()
			{
			}

			public override long GetGeneration()
			{
				return 0;
			}

			public override IDictionary<string, string> GetUserData()
			{
				return null;
			}

			public override bool IsDeleted()
			{
				return false;
			}

			public override int GetSegmentCount()
			{
				return 2;
			}

			private readonly Directory dir;
		}
	}
}
