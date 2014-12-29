/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Store;
using Lucene.Net.TestFramework.Store;
using Sharpen;

namespace Lucene.Net.TestFramework.Store
{
	/// <summary>
	/// This class provides access to package-level features defined in the
	/// store package.
	/// </summary>
	/// <remarks>
	/// This class provides access to package-level features defined in the
	/// store package. It is used for testing only.
	/// </remarks>
	public sealed class TestHelper
	{
		public TestHelper()
		{
		}

		//
		/// <summary>
		/// Returns true if the instance of the provided input stream is actually
		/// an SimpleFSIndexInput.
		/// </summary>
		/// <remarks>
		/// Returns true if the instance of the provided input stream is actually
		/// an SimpleFSIndexInput.
		/// </remarks>
		public static bool IsSimpleFSIndexInput(IndexInput @is)
		{
			return @is is SimpleFSDirectory.SimpleFSIndexInput;
		}

		/// <summary>
		/// Returns true if the provided input stream is an SimpleFSIndexInput and
		/// is a clone, that is it does not own its underlying file descriptor.
		/// </summary>
		/// <remarks>
		/// Returns true if the provided input stream is an SimpleFSIndexInput and
		/// is a clone, that is it does not own its underlying file descriptor.
		/// </remarks>
		public static bool IsSimpleFSIndexInputClone(IndexInput @is)
		{
			if (IsSimpleFSIndexInput(@is))
			{
				return ((SimpleFSDirectory.SimpleFSIndexInput)@is).isClone;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Given an instance of SimpleFSDirectory.SimpleFSIndexInput, this method returns
		/// true if the underlying file descriptor is valid, and false otherwise.
		/// </summary>
		/// <remarks>
		/// Given an instance of SimpleFSDirectory.SimpleFSIndexInput, this method returns
		/// true if the underlying file descriptor is valid, and false otherwise.
		/// This can be used to determine if the OS file has been closed.
		/// The descriptor becomes invalid when the non-clone instance of the
		/// SimpleFSIndexInput that owns this descriptor is closed. However, the
		/// descriptor may possibly become invalid in other ways as well.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static bool IsSimpleFSIndexInputOpen(IndexInput @is)
		{
			if (IsSimpleFSIndexInput(@is))
			{
				SimpleFSDirectory.SimpleFSIndexInput fis = (SimpleFSDirectory.SimpleFSIndexInput)
					@is;
				return fis.IsFDValid();
			}
			else
			{
				return false;
			}
		}
	}
}
