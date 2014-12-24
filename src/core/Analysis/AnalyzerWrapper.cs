using System;
using System.IO;

namespace Lucene.Net.Analysis
{
    public abstract class AnalyzerWrapper : Analyzer
    {
		[Obsolete(@"Use AnalyzerWrapper(ReuseStrategy) and specify a valid ReuseStrategy , probably retrieved from the wrapped analyzer using Analyzer.GetReuseStrategy() .")]
        protected AnalyzerWrapper()
            : base(new PerFieldReuseStrategy())
        {
        }

		/// <summary>Creates a new AnalyzerWrapper with the given reuse strategy.</summary>
		/// <remarks>
		/// Creates a new AnalyzerWrapper with the given reuse strategy.
		/// <p>If you want to wrap a single delegate Analyzer you can probably
		/// reuse its strategy when instantiating this subclass:
		/// <code>super(delegate.getReuseStrategy());</code>
		/// .
		/// <p>If you choose different analyzers per field, use
		/// <see cref="Analyzer.PER_FIELD_REUSE_STRATEGY">Analyzer.PER_FIELD_REUSE_STRATEGY</see>
		/// .
		/// </remarks>
		/// <seealso cref="Analyzer.GetReuseStrategy()">Analyzer.GetReuseStrategy()</seealso>
		public AnalyzerWrapper(ReuseStrategy reuseStrategy) : base(reuseStrategy)
		{
		}

        protected abstract Analyzer GetWrappedAnalyzer(string fieldName);

        protected virtual TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
		{
			return components;
		}

		protected internal virtual TextReader WrapReader(string fieldName, TextReader reader)
		{
			return reader;
		}
		
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return WrapComponents(fieldName, GetWrappedAnalyzer(fieldName).CreateComponents(fieldName, reader));
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return GetWrappedAnalyzer(fieldName).GetPositionIncrementGap(fieldName);
        }

        public override int GetOffsetGap(string fieldName)
        {
            return GetWrappedAnalyzer(fieldName).GetOffsetGap(fieldName);
        }

        public override System.IO.TextReader InitReader(string fieldName, TextReader reader)
        {
			return GetWrappedAnalyzer(fieldName).InitReader(fieldName, WrapReader(fieldName, 
				reader));
        }
    }
}
