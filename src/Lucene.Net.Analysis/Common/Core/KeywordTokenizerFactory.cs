using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public class KeywordTokenizerFactory : TokenizerFactory
    {
        public KeywordTokenizerFactory(IDictionary<String, String> args)
            : base(args)
        {
            if (args.Any())
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public override Tokenizer Create(Net.Util.AttributeSource.AttributeFactory factory, System.IO.TextReader input)
        {
            return new KeywordTokenizer(factory, input, KeywordTokenizer.DEFAULT_BUFFER_SIZE);
        }
    }
}
