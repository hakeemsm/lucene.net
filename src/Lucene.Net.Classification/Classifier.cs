/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Lucene.Net.Classification
{
	/// <summary>
	/// A classifier, see <code>http://en.wikipedia.org/wiki/Classifier_(mathematics)</code>, which assign classes of type
	/// <code>T</code>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public interface Classifier<T>
	{
		/// <summary>Assign a class (with score) to the given text String</summary>
		/// <param name="text">a String containing text to be classified</param>
		/// <returns>
		/// a
		/// <see cref="ClassificationResult{T}">ClassificationResult&lt;T&gt;</see>
		/// holding assigned class of type <code>T</code> and score
		/// </returns>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error.</exception>
		ClassificationResult<T> AssignClass(string text);

		/// <summary>Train the classifier using the underlying Lucene index</summary>
		/// <param name="atomicReader">the reader to use to access the Lucene index</param>
		/// <param name="textFieldName">the name of the field used to compare documents</param>
		/// <param name="classFieldName">the name of the field containing the class assigned to documents
		/// 	</param>
		/// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error.</exception>
		void Train(AtomicReader atomicReader, string textFieldName, string classFieldName
			, Analyzer analyzer);

		/// <summary>Train the classifier using the underlying Lucene index</summary>
		/// <param name="atomicReader">the reader to use to access the Lucene index</param>
		/// <param name="textFieldName">the name of the field used to compare documents</param>
		/// <param name="classFieldName">the name of the field containing the class assigned to documents
		/// 	</param>
		/// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
		/// <param name="query">the query to filter which documents use for training</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error.</exception>
		void Train(AtomicReader atomicReader, string textFieldName, string classFieldName
			, Analyzer analyzer, Query query);

		/// <summary>Train the classifier using the underlying Lucene index</summary>
		/// <param name="atomicReader">the reader to use to access the Lucene index</param>
		/// <param name="textFieldNames">the names of the fields to be used to compare documents
		/// 	</param>
		/// <param name="classFieldName">the name of the field containing the class assigned to documents
		/// 	</param>
		/// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
		/// <param name="query">the query to filter which documents use for training</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error.</exception>
		void Train(AtomicReader atomicReader, string[] textFieldNames, string classFieldName
			, Analyzer analyzer, Query query);
	}
}
