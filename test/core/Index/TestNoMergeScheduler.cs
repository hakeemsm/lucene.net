using System;
using System.Reflection;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;

namespace Lucene.Net.Test.Index
{
	public class TestNoMergeScheduler : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void NoMergeSchedulerTest()
		{
			MergeScheduler ms = NoMergeScheduler.INSTANCE;
			ms.Dispose();
		    var mergeValue = Enum.GetNames(typeof(MergePolicy.MergeTrigger));
		    string randomPick = Random().RandomFrom(mergeValue);
		    MergePolicy.MergeTrigger mObj = (MergePolicy.MergeTrigger) Enum.Parse(typeof (MergePolicy.MergeTrigger), randomPick);
		    ms.Merge(null, mObj, Random().NextBoolean());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFinalSingleton()
		{
			IsTrue(typeof(NoMergeScheduler).IsSealed);
			var ctors = typeof(NoMergeScheduler).GetConstructors();
			AssertEquals("expected 1 private ctor only: " + Arrays.ToString(ctors), 1, ctors.Length);
			AssertTrue("that 1 should be private: " + ctors[0], ctors[0].IsPrivate);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestMethodsOverridden()
		{
			// Ensures that all methods of MergeScheduler are overridden. That's
			// important to ensure that NoMergeScheduler overrides everything, so that
			// no unexpected behavior/error occurs
			foreach (MethodInfo m in typeof(NoMergeScheduler).GetMethods())
			{
				// getDeclaredMethods() returns just those methods that are declared on
				// NoMergeScheduler. getMethods() returns those that are visible in that
				// context, including ones from Object. So just filter out Object. If in
				// the future MergeScheduler will extend a different class than Object,
				// this will need to change.
				if (m.DeclaringType != typeof(object))
				{
					AssertTrue(m + " is not overridden !", m.DeclaringType == typeof(
						NoMergeScheduler));
				}
			}
		}
	}
}
