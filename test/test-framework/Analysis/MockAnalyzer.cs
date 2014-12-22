using System;
using System.IO;
using Lucene.Net.JavaCompatibility;
using Lucene.Net.Support;
using Lucene.Net.Util.Automaton;

namespace Lucene.Net.Analysis
{
/**
 * Analyzer for testing
 * <p>
 * This analyzer is a replacement for Whitespace/Simple/KeywordAnalyzers
 * for unit tests. If you are testing a custom component such as a queryparser
 * or analyzer-wrapper that consumes analysis streams, its a great idea to test
 * it with this analyzer instead. MockAnalyzer has the following behavior:
 * <ul>
 *   <li>By default, the assertions in {@link MockTokenizer} are turned on for extra
 *       checks that the consumer is consuming properly. These checks can be disabled
 *       with {@link #setEnableChecks(boolean)}.
 *   <li>Payload data is randomly injected into the stream for more thorough testing
 *       of payloads.
 * </ul>
 * @see MockTokenizer
 */

    public class MockAnalyzer : Analyzer
    {
        private CharacterRunAutomaton runAutomaton;
        private bool lowerCase;
        private CharacterRunAutomaton filter;
        private bool enablePositionIncrements;
        private int positionIncrementGap;
		private int offsetGap;
        private Random random;
        private HashMap<String, int> previousMappings = new HashMap<String, int>();
        private bool enableChecks = true;
        private int maxTokenLength = MockTokenizer.DEFAULT_MAX_TOKEN_LENGTH;
        private int _positionIncrementGap;
        

        /**
   * Creates a new MockAnalyzer.
   * 
   * @param random Random for payloads behavior
   * @param runAutomaton DFA describing how tokenization should happen (e.g. [a-zA-Z]+)
   * @param lowerCase true if the tokenizer should lowercase terms
   * @param filter DFA describing how terms should be filtered (set of stopwords, etc)
   * @param enablePositionIncrements true if position increments should reflect filtered terms.
   */

        public MockAnalyzer(Random random, CharacterRunAutomaton runAutomaton, bool lowerCase
            , CharacterRunAutomaton filter) : base(PER_FIELD_REUSE_STRATEGY)
        {
            // TODO: this should be solved in a different way; Random should not be shared (!).
            this.random = new Random();
            this.runAutomaton = runAutomaton;
            this.lowerCase = lowerCase;
            this.filter = filter;
        }

        /**
   * Calls {@link #MockAnalyzer(Random, CharacterRunAutomaton, boolean, CharacterRunAutomaton, boolean) 
   * MockAnalyzer(random, runAutomaton, lowerCase, MockTokenFilter.EMPTY_STOPSET, false}).
   */

        public MockAnalyzer(Random random, CharacterRunAutomaton runAutomaton, bool lowerCase
            ) : this(random, runAutomaton, lowerCase, MockTokenFilter.EMPTY_STOPSET)
        {
        }

        /** 
   * Create a Whitespace-lowercasing analyzer with no stopwords removal.
   * <p>
   * Calls {@link #MockAnalyzer(Random, CharacterRunAutomaton, boolean, CharacterRunAutomaton, boolean) 
   * MockAnalyzer(random, MockTokenizer.WHITESPACE, true, MockTokenFilter.EMPTY_STOPSET, false}).
   */

        public MockAnalyzer(Random random) :
            this(random, MockTokenizer.WHITESPACE, true)
        {
        }

        public override TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
        {
            MockTokenizer tokenizer = new MockTokenizer(reader, runAutomaton, lowerCase, maxTokenLength);
            tokenizer.setEnableChecks(enableChecks);
            MockTokenFilter filt = new MockTokenFilter(tokenizer, filter);
            return new TokenStreamComponents(tokenizer, MaybePayload(filt, fieldName));
        }

        // TODO synchronized
        private TokenFilter MaybePayload(TokenFilter stream, string fieldName)
        {
            var val = previousMappings[fieldName];

            val = -1; // no payloads
            if (LuceneTestCase.Rarely(random))
            {
                switch (random.NextInt(3))
                {
                    case 0:
                        val = -1; // no payloads
                        break;
                    case 1:
                        val = int.MaxValue; // variable length payload
                        break;
                    case 2:
                        val = random.Next(0, 12); // fixed length payload
                        break;
                }
            }

            if (LuceneTestCase.VERBOSE)
            {
                if (val == int.MaxValue)
                {
                    Console.WriteLine("MockAnalyzer: field=" + fieldName + " gets variable length payloads");
                }
                else if (val != -1)
                {
                    Console.WriteLine("MockAnalyzer: field=" + fieldName + " gets fixed length=" + val + " payloads");
                }
            }
            previousMappings[fieldName] = val; // save it so we are consistent for this field


            if (val == -1)
            {
                return stream;
            }
            return val == int.MaxValue
                ? (TokenFilter) new MockVariableLengthPayloadFilter(random, stream)
                : new MockFixedLengthPayloadFilter(random, stream, val);
        }

        public int PositionIncrementGap
        {
            get { return positionIncrementGap; }
            set { positionIncrementGap = value; }
        }

        public int OffsetGap
        {
            get { return offsetGap; }
            set { offsetGap = value; }
        }

        /**
   * Toggle consumer workflow checking: if your test consumes tokenstreams normally you
   * should leave this enabled.
   */

        public void SetEnableChecks(bool enableChecks)
        {
            this.enableChecks = enableChecks;
        }


        /** 
   * Toggle maxTokenLength for MockTokenizer
   */

        public void setMaxTokenLength(int length)
        {
            this.maxTokenLength = length;
        }
    }
}

