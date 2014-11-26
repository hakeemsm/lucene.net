/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Sandbox.Queries.Regex;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries.Regex
{
	/// <summary>Defines methods for regular expression supporting Querys to use.</summary>
	/// <remarks>Defines methods for regular expression supporting Querys to use.</remarks>
	public interface RegexQueryCapable
	{
		/// <summary>
		/// Defines which
		/// <see cref="RegexCapabilities">RegexCapabilities</see>
		/// implementation is used by this instance.
		/// </summary>
		/// <seealso cref="GetRegexImplementation()">GetRegexImplementation()</seealso>
		void SetRegexImplementation(RegexCapabilities impl);

		/// <summary>Returns the implementation used by this instance.</summary>
		/// <remarks>Returns the implementation used by this instance.</remarks>
		/// <seealso cref="SetRegexImplementation(RegexCapabilities)">SetRegexImplementation(RegexCapabilities)
		/// 	</seealso>
		RegexCapabilities GetRegexImplementation();
	}
}
