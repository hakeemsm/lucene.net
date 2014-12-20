/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// A
	/// <see cref="System.IDisposable">System.IDisposable</see>
	/// that attempts to remove a given file/folder.
	/// </summary>
	internal sealed class RemoveUponClose : IDisposable
	{
		private readonly FilePath file;

		private readonly TestRuleMarkFailure failureMarker;

		private readonly string creationStack;

		public RemoveUponClose(FilePath file, TestRuleMarkFailure failureMarker)
		{
			this.file = file;
			this.failureMarker = failureMarker;
			StringBuilder b = new StringBuilder();
			foreach (StackTraceElement e in Sharpen.Thread.CurrentThread().GetStackTrace())
			{
				b.Append('\t').Append(e.ToString()).Append('\n');
			}
			creationStack = b.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public void Close()
		{
			// only if there were no other test failures.
			if (failureMarker.WasSuccessful())
			{
				if (file.Exists())
				{
					try
					{
						TestUtil.Rm(file);
					}
					catch (IOException e)
					{
						throw new IOException("Could not remove temporary location '" + file.GetAbsolutePath
							() + "', created at stack trace:\n" + creationStack, e);
					}
				}
			}
		}
	}
}
