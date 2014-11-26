using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public sealed class KeywordAnalyzer : Analyzer
    {
        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return new TokenStreamComponents(new KeywordTokenizer(reader));
        }
    }
}
