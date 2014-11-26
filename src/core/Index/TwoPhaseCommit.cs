namespace Lucene.Net.Index
{
	/// <summary>An interface for implementations that support 2-phase commit.</summary>
	/// <remarks>
	/// An interface for implementations that support 2-phase commit. You can use
	/// <see cref="TwoPhaseCommitTool">TwoPhaseCommitTool</see>
	/// to execute a 2-phase commit algorithm over several
	/// <see cref="TwoPhaseCommit">TwoPhaseCommit</see>
	/// s.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public interface TwoPhaseCommit
	{
		/// <summary>The first stage of a 2-phase commit.</summary>
		/// <remarks>
		/// The first stage of a 2-phase commit. Implementations should do as much work
		/// as possible in this method, but avoid actual committing changes. If the
		/// 2-phase commit fails,
		/// <see cref="Rollback()">Rollback()</see>
		/// is called to discard all changes
		/// since last successful commit.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		void PrepareCommit();

		/// <summary>The second phase of a 2-phase commit.</summary>
		/// <remarks>
		/// The second phase of a 2-phase commit. Implementations should ideally do
		/// very little work in this method (following
		/// <see cref="PrepareCommit()">PrepareCommit()</see>
		/// , and
		/// after it returns, the caller can assume that the changes were successfully
		/// committed to the underlying storage.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		void Commit();

		/// <summary>Discards any changes that have occurred since the last commit.</summary>
		/// <remarks>
		/// Discards any changes that have occurred since the last commit. In a 2-phase
		/// commit algorithm, where one of the objects failed to
		/// <see cref="Commit()">Commit()</see>
		/// or
		/// <see cref="PrepareCommit()">PrepareCommit()</see>
		/// , this method is used to roll all other objects
		/// back to their previous state.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		void Rollback();
	}
}
