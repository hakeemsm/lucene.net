/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Reflection;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Store
{
	public class TestFilterDirectory : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestOverrides()
		{
			// verify that all methods of Directory are overridden by FilterDirectory,
			// except those under the 'exclude' list
			ICollection<MethodInfo> exclude = new HashSet<MethodInfo>();
			exclude.Add(typeof(Directory).GetMethod("copy", typeof(Directory), typeof(string
				), typeof(string), typeof(IOContext)));
			exclude.Add(typeof(Directory).GetMethod("createSlicer", typeof(string), typeof(
				IOContext)));
			exclude.Add(typeof(Directory).GetMethod("openChecksumInput", typeof(string), 
				typeof(IOContext)));
			foreach (MethodInfo m in typeof(FilterDirectory).GetMethods())
			{
				if (m.DeclaringType == typeof(Directory))
				{
					IsTrue("method " + m.Name + " not overridden!", exclude.Contains
						(m));
				}
			}
		}
	}
}
