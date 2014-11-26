namespace Lucene.Net.Index
{
	public enum MergeTrigger
	{
		SEGMENT_FLUSH,
		FULL_FLUSH,
		EXPLICIT,
		MERGE_FINISHED,
		CLOSING
	}
}
