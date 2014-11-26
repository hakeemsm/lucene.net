/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */



namespace Lucene.Net.Classification
{
	/// <summary>
	/// The result of a call to
	/// <see cref="Classifier{T}.AssignClass(string)">Classifier&lt;T&gt;.AssignClass(string)
	/// 	</see>
	/// holding an assigned class of type <code>T</code> and a score.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class ClassificationResult<T>
	{
		private readonly T assignedClass;

		private readonly double score;

		/// <summary>Constructor</summary>
		/// <param name="assignedClass">
		/// the class <code>T</code> assigned by a
		/// <see cref="Classifier{T}">Classifier&lt;T&gt;</see>
		/// </param>
		/// <param name="score">the score for the assignedClass as a <code>double</code></param>
		public ClassificationResult(T assignedClass, double score)
		{
			this.assignedClass = assignedClass;
			this.score = score;
		}

		/// <summary>retrieve the result class</summary>
		/// <returns>a <code>T</code> representing an assigned class</returns>
		public virtual T GetAssignedClass()
		{
			return assignedClass;
		}

		/// <summary>retrieve the result score</summary>
		/// <returns>a <code>double</code> representing a result score</returns>
		public virtual double GetScore()
		{
			return score;
		}
	}
}
