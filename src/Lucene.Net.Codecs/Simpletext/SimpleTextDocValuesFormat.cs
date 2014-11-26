/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>plain text doc values format.</summary>
	/// <remarks>
	/// plain text doc values format.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// <p>
	/// the .dat file contains the data.
	/// for numbers this is a "fixed-width" file, for example a single byte range:
	/// <pre>
	/// field myField
	/// type NUMERIC
	/// minvalue 0
	/// pattern 000
	/// 005
	/// T
	/// 234
	/// T
	/// 123
	/// T
	/// ...
	/// </pre>
	/// so a document's value (delta encoded from minvalue) can be retrieved by
	/// seeking to startOffset + (1+pattern.length()+2)*docid. The extra 1 is the newline.
	/// The extra 2 is another newline and 'T' or 'F': true if the value is real, false if missing.
	/// for bytes this is also a "fixed-width" file, for example:
	/// <pre>
	/// field myField
	/// type BINARY
	/// maxlength 6
	/// pattern 0
	/// length 6
	/// foobar[space][space]
	/// T
	/// length 3
	/// baz[space][space][space][space][space]
	/// T
	/// ...
	/// </pre>
	/// so a doc's value can be retrieved by seeking to startOffset + (9+pattern.length+maxlength+2)*doc
	/// the extra 9 is 2 newlines, plus "length " itself.
	/// the extra 2 is another newline and 'T' or 'F': true if the value is real, false if missing.
	/// for sorted bytes this is a fixed-width file, for example:
	/// <pre>
	/// field myField
	/// type SORTED
	/// numvalues 10
	/// maxLength 8
	/// pattern 0
	/// ordpattern 00
	/// length 6
	/// foobar[space][space]
	/// length 3
	/// baz[space][space][space][space][space]
	/// ...
	/// 03
	/// 06
	/// 01
	/// 10
	/// ...
	/// </pre>
	/// so the "ord section" begins at startOffset + (9+pattern.length+maxlength)*numValues.
	/// a document's ord can be retrieved by seeking to "ord section" + (1+ordpattern.length())*docid
	/// an ord's value can be retrieved by seeking to startOffset + (9+pattern.length+maxlength)*ord
	/// for sorted set this is a fixed-width file very similar to the SORTED case, for example:
	/// <pre>
	/// field myField
	/// type SORTED_SET
	/// numvalues 10
	/// maxLength 8
	/// pattern 0
	/// ordpattern XXXXX
	/// length 6
	/// foobar[space][space]
	/// length 3
	/// baz[space][space][space][space][space]
	/// ...
	/// 0,3,5
	/// 1,2
	/// 10
	/// ...
	/// </pre>
	/// so the "ord section" begins at startOffset + (9+pattern.length+maxlength)*numValues.
	/// a document's ord list can be retrieved by seeking to "ord section" + (1+ordpattern.length())*docid
	/// this is a comma-separated list, and its padded with spaces to be fixed width. so trim() and split() it.
	/// and beware the empty string!
	/// an ord's value can be retrieved by seeking to startOffset + (9+pattern.length+maxlength)*ord
	/// the reader can just scan this file when it opens, skipping over the data blocks
	/// and saving the offset/etc for each field.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextDocValuesFormat : DocValuesFormat
	{
		public SimpleTextDocValuesFormat() : base("SimpleText")
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			return new SimpleTextDocValuesWriter(state, "dat");
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			return new SimpleTextDocValuesReader(state, "dat");
		}
	}
}
