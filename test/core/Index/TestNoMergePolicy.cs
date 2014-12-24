/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Reflection;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;
using Sharpen.Reflect;

namespace Lucene.Net.Index
{
	public class TestNoMergePolicy : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNoMergePolicy()
		{
			MergePolicy mp = NoMergePolicy.NO_COMPOUND_FILES;
			IsNull(mp.FindMerges(null, (SegmentInfos)null));
			IsNull(mp.FindForcedMerges(null, 0, null));
			IsNull(mp.FindForcedDeletesMerges(null));
			IsFalse(mp.UseCompoundFile(null, null));
			mp.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestCompoundFiles()
		{
			IsFalse(NoMergePolicy.NO_COMPOUND_FILES.UseCompoundFile(null
				, null));
			IsTrue(NoMergePolicy.COMPOUND_FILES.UseCompoundFile(null, 
				null));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFinalSingleton()
		{
			IsTrue(Modifier.IsFinal(typeof(NoMergePolicy).GetModifiers
				()));
			Constructor<object>[] ctors = typeof(NoMergePolicy).GetDeclaredConstructors();
			AreEqual("expected 1 private ctor only: " + Arrays.ToString
				(ctors), 1, ctors.Length);
			IsTrue("that 1 should be private: " + ctors[0], Modifier.IsPrivate
				(ctors[0].GetModifiers()));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestMethodsOverridden()
		{
			// Ensures that all methods of MergePolicy are overridden. That's important
			// to ensure that NoMergePolicy overrides everything, so that no unexpected
			// behavior/error occurs
			foreach (MethodInfo m in typeof(NoMergePolicy).GetMethods())
			{
				// getDeclaredMethods() returns just those methods that are declared on
				// NoMergePolicy. getMethods() returns those that are visible in that
				// context, including ones from Object. So just filter out Object. If in
				// the future MergePolicy will extend a different class than Object, this
				// will need to change.
				if (m.Name.Equals("clone"))
				{
					continue;
				}
				if (m.DeclaringType != typeof(object) && !Modifier.IsFinal(m.GetModifiers()))
				{
					IsTrue(m + " is not overridden ! ", m.DeclaringType == typeof(
						NoMergePolicy));
				}
			}
		}
	}
}
