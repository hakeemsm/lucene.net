namespace Lucene.Net.Index
{
    public sealed class NoMergeScheduler : MergeScheduler
    {
        public static readonly MergeScheduler INSTANCE = new NoMergeScheduler();

        private NoMergeScheduler()
        {
            // prevent instantiation
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override void Merge(IndexWriter writer, MergePolicy.MergeTrigger trigger, bool newMergesFound)
        {
        }

        public override object Clone()
        {
            return this;
        }
    }
}
