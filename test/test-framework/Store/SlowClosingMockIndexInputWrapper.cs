/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Store
{
	/// <summary>hangs onto files a little bit longer (50ms in close).</summary>
	/// <remarks>
	/// hangs onto files a little bit longer (50ms in close).
	/// MockDirectoryWrapper acts like windows: you can't delete files
	/// open elsewhere. so the idea is to make race conditions for tiny
	/// files (like segments) easier to reproduce.
	/// </remarks>
	internal class SlowClosingMockIndexInputWrapper : MockIndexInputWrapper
	{
		public SlowClosingMockIndexInputWrapper(MockDirectoryWrapper dir, string name, IndexInput
			 delegate_) : base(dir, name, delegate_)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				Sharpen.Thread.Sleep(50);
			}
			catch (Exception ie)
			{
				throw new ThreadInterruptedException(ie);
			}
			finally
			{
				base.Close();
			}
		}
	}
}
