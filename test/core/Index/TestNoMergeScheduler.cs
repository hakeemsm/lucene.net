/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Reflection;
using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;
using Sharpen.Reflect;

namespace Lucene.Net.Test.Index
{
	public class TestNoMergeScheduler : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNoMergeScheduler()
		{
			MergeScheduler ms = NoMergeScheduler.INSTANCE;
			ms.Dispose();
			ms.Merge(null, RandomPicks.RandomFrom(Random(), MergeTrigger.Values()), Random().
				NextBoolean());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFinalSingleton()
		{
			IsTrue(Modifier.IsFinal(typeof(NoMergeScheduler).GetModifiers
				()));
			Constructor<object>[] ctors = typeof(NoMergeScheduler).GetDeclaredConstructors();
			AreEqual("expected 1 private ctor only: " + Arrays.ToString
				(ctors), 1, ctors.Length);
			IsTrue("that 1 should be private: " + ctors[0], Modifier.IsPrivate
				(ctors[0].GetModifiers()));
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
					IsTrue(m + " is not overridden !", m.DeclaringType == typeof(
						NoMergeScheduler));
				}
			}
		}
	}
}
