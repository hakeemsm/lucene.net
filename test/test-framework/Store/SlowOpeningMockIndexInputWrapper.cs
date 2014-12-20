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
	/// <summary>
	/// Takes a while to open files: gives testThreadInterruptDeadlock
	/// a chance to find file leaks if opening an input throws exception
	/// </summary>
	internal class SlowOpeningMockIndexInputWrapper : MockIndexInputWrapper
	{
		/// <exception cref="System.IO.IOException"></exception>
		public SlowOpeningMockIndexInputWrapper(MockDirectoryWrapper dir, string name, IndexInput
			 delegate_) : base(dir, name, delegate_)
		{
			try
			{
				Sharpen.Thread.Sleep(50);
			}
			catch (Exception ie)
			{
				try
				{
					base.Close();
				}
				catch
				{
				}
				// we didnt open successfully
				throw new ThreadInterruptedException(ie);
			}
		}
	}
}
