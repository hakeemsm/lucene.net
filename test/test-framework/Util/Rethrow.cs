/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// Sneaky: rethrowing checked exceptions as unchecked
	/// ones.
	/// </summary>
	/// <remarks>
	/// Sneaky: rethrowing checked exceptions as unchecked
	/// ones. Eh, it is sometimes useful...
	/// <p>Pulled from <a href="http://www.javapuzzlers.com">Java Puzzlers</a>.</p>
	/// </remarks>
	/// <seealso>"http://www.amazon.com/Java-Puzzlers-Traps-Pitfalls-Corner/dp/032133678X"
	/// 	</seealso>
	/// .NET Port changed class name to avoid conflict with method name
	public sealed class Thrower
	{
		/// <summary>Classy puzzler to rethrow any checked exception as an unchecked one.</summary>
		/// <remarks>Classy puzzler to rethrow any checked exception as an unchecked one.</remarks>
		private class Rethrower<T> where T:Exception
		{
			/// <exception cref="T"></exception>
			public void Rethrow(Exception t)
			{
				throw (T)t;
			}
		}

		/// <summary>Rethrows <code>t</code> (identical object).</summary>
		/// <remarks>Rethrows <code>t</code> (identical object).</remarks>
		public static void Rethrow(Exception t)
		{
			new Rethrower<Exception>().Rethrow(t);
		}
	}
}
