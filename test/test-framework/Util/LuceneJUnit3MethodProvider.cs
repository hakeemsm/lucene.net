/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Com.Carrotsearch.Randomizedtesting;
using Lucene.Net.TestFramework.Util;
using Sharpen;
using Sharpen.Reflect;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>Backwards compatible test* method provider (public, non-static).</summary>
	/// <remarks>Backwards compatible test* method provider (public, non-static).</remarks>
	public sealed class LuceneJUnit3MethodProvider : TestMethodProvider
	{
		public ICollection<MethodInfo> GetTestMethods<_T0>(Type<_T0> suiteClass, ClassModel
			 classModel)
		{
			IDictionary<MethodInfo, ClassModel.MethodModel> methods = classModel.GetMethods();
			List<MethodInfo> result = new List<MethodInfo>();
			foreach (ClassModel.MethodModel mm in methods.Values)
			{
				// Skip any methods that have overrieds/ shadows.
				if (mm.GetDown() != null)
				{
					continue;
				}
				MethodInfo m = mm.element;
				if (m.Name.StartsWith("test") && Modifier.IsPublic(m.GetModifiers()) && !Modifier
					.IsStatic(m.GetModifiers()) && Sharpen.Runtime.GetParameterTypes(m).Length == 0)
				{
					result.Add(m);
				}
			}
			return result;
		}
	}
}
