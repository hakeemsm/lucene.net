/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Wikipedia;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Wikipedia
{
	/// <summary>Extension of StandardTokenizer that is aware of Wikipedia syntax.</summary>
	/// <remarks>
	/// Extension of StandardTokenizer that is aware of Wikipedia syntax.  It is based off of the
	/// Wikipedia tutorial available at http://en.wikipedia.org/wiki/Wikipedia:Tutorial, but it may not be complete.
	/// <p/>
	/// <p/>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class WikipediaTokenizer : Tokenizer
	{
		public static readonly string INTERNAL_LINK = "il";

		public static readonly string EXTERNAL_LINK = "el";

		public static readonly string EXTERNAL_LINK_URL = "elu";

		public static readonly string CITATION = "ci";

		public static readonly string CATEGORY = "c";

		public static readonly string BOLD = "b";

		public static readonly string ITALICS = "i";

		public static readonly string BOLD_ITALICS = "bi";

		public static readonly string HEADING = "h";

		public static readonly string SUB_HEADING = "sh";

		public const int ALPHANUM_ID = 0;

		public const int APOSTROPHE_ID = 1;

		public const int ACRONYM_ID = 2;

		public const int COMPANY_ID = 3;

		public const int EMAIL_ID = 4;

		public const int HOST_ID = 5;

		public const int NUM_ID = 6;

		public const int CJ_ID = 7;

		public const int INTERNAL_LINK_ID = 8;

		public const int EXTERNAL_LINK_ID = 9;

		public const int CITATION_ID = 10;

		public const int CATEGORY_ID = 11;

		public const int BOLD_ID = 12;

		public const int ITALICS_ID = 13;

		public const int BOLD_ITALICS_ID = 14;

		public const int HEADING_ID = 15;

		public const int SUB_HEADING_ID = 16;

		public const int EXTERNAL_LINK_URL_ID = 17;

		/// <summary>String token types that correspond to token type int constants</summary>
		public static readonly string[] TOKEN_TYPES = new string[] { "<ALPHANUM>", "<APOSTROPHE>"
			, "<ACRONYM>", "<COMPANY>", "<EMAIL>", "<HOST>", "<NUM>", "<CJ>", INTERNAL_LINK, 
			EXTERNAL_LINK, CITATION, CATEGORY, BOLD, ITALICS, BOLD_ITALICS, HEADING, SUB_HEADING
			, EXTERNAL_LINK_URL };

		/// <summary>Only output tokens</summary>
		public const int TOKENS_ONLY = 0;

		/// <summary>Only output untokenized tokens, which are tokens that would normally be split into several tokens
		/// 	</summary>
		public const int UNTOKENIZED_ONLY = 1;

		/// <summary>Output the both the untokenized token and the splits</summary>
		public const int BOTH = 2;

		/// <summary>
		/// This flag is used to indicate that the produced "Token" would, if
		/// <see cref="TOKENS_ONLY">TOKENS_ONLY</see>
		/// was used, produce multiple tokens.
		/// </summary>
		public const int UNTOKENIZED_TOKEN_FLAG = 1;

		/// <summary>A private instance of the JFlex-constructed scanner</summary>
		private readonly WikipediaTokenizerImpl scanner;

		private int tokenOutput = TOKENS_ONLY;

		private ICollection<string> untokenizedTypes = Sharpen.Collections.EmptySet();

		private Iterator<AttributeSource.State> tokens = null;

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private readonly TypeAttribute typeAtt = AddAttribute<TypeAttribute>();

		private readonly PositionIncrementAttribute posIncrAtt = AddAttribute<PositionIncrementAttribute
			>();

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly FlagsAttribute flagsAtt = AddAttribute<FlagsAttribute>();

		private bool first;

		/// <summary>
		/// Creates a new instance of the
		/// <see cref="WikipediaTokenizer">WikipediaTokenizer</see>
		/// . Attaches the
		/// <code>input</code> to a newly created JFlex scanner.
		/// </summary>
		/// <param name="input">The Input Reader</param>
		protected WikipediaTokenizer(StreamReader input) : this(input, TOKENS_ONLY, Sharpen.Collections
			.EmptySet<string>())
		{
		}

		/// <summary>
		/// Creates a new instance of the
		/// <see cref="WikipediaTokenizer">WikipediaTokenizer</see>
		/// .  Attaches the
		/// <code>input</code> to a the newly created JFlex scanner.
		/// </summary>
		/// <param name="input">The input</param>
		/// <param name="tokenOutput">
		/// One of
		/// <see cref="TOKENS_ONLY">TOKENS_ONLY</see>
		/// ,
		/// <see cref="UNTOKENIZED_ONLY">UNTOKENIZED_ONLY</see>
		/// ,
		/// <see cref="BOTH">BOTH</see>
		/// </param>
		public WikipediaTokenizer(StreamReader input, int tokenOutput, ICollection<string
			> untokenizedTypes) : base(input)
		{
			//The URL part of the link, i.e. the first token
			this.scanner = new WikipediaTokenizerImpl(this.input);
			Init(tokenOutput, untokenizedTypes);
		}

		/// <summary>
		/// Creates a new instance of the
		/// <see cref="WikipediaTokenizer">WikipediaTokenizer</see>
		/// .  Attaches the
		/// <code>input</code> to a the newly created JFlex scanner. Uses the given
		/// <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory">Lucene.Net.Util.AttributeSource.AttributeFactory
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="input">The input</param>
		/// <param name="tokenOutput">
		/// One of
		/// <see cref="TOKENS_ONLY">TOKENS_ONLY</see>
		/// ,
		/// <see cref="UNTOKENIZED_ONLY">UNTOKENIZED_ONLY</see>
		/// ,
		/// <see cref="BOTH">BOTH</see>
		/// </param>
		public WikipediaTokenizer(AttributeSource.AttributeFactory factory, StreamReader 
			input, int tokenOutput, ICollection<string> untokenizedTypes) : base(factory, input
			)
		{
			this.scanner = new WikipediaTokenizerImpl(this.input);
			Init(tokenOutput, untokenizedTypes);
		}

		private void Init(int tokenOutput, ICollection<string> untokenizedTypes)
		{
			// TODO: cutover to enum
			if (tokenOutput != TOKENS_ONLY && tokenOutput != UNTOKENIZED_ONLY && tokenOutput 
				!= BOTH)
			{
				throw new ArgumentException("tokenOutput must be TOKENS_ONLY, UNTOKENIZED_ONLY or BOTH"
					);
			}
			this.tokenOutput = tokenOutput;
			this.untokenizedTypes = untokenizedTypes;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override bool IncrementToken()
		{
			if (tokens != null && tokens.HasNext())
			{
				AttributeSource.State state = tokens.Next();
				RestoreState(state);
				return true;
			}
			ClearAttributes();
			int tokenType = scanner.GetNextToken();
			if (tokenType == WikipediaTokenizerImpl.YYEOF)
			{
				return false;
			}
			string type = WikipediaTokenizerImpl.TOKEN_TYPES[tokenType];
			if (tokenOutput == TOKENS_ONLY || untokenizedTypes.Contains(type) == false)
			{
				SetupToken();
			}
			else
			{
				if (tokenOutput == UNTOKENIZED_ONLY && untokenizedTypes.Contains(type) == true)
				{
					CollapseTokens(tokenType);
				}
				else
				{
					if (tokenOutput == BOTH)
					{
						//collapse into a single token, add it to tokens AND output the individual tokens
						//output the untokenized Token first
						CollapseAndSaveTokens(tokenType, type);
					}
				}
			}
			int posinc = scanner.GetPositionIncrement();
			if (first && posinc == 0)
			{
				posinc = 1;
			}
			// don't emit posinc=0 for the first token!
			posIncrAtt.SetPositionIncrement(posinc);
			typeAtt.SetType(type);
			first = false;
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CollapseAndSaveTokens(int tokenType, string type)
		{
			//collapse
			StringBuilder buffer = new StringBuilder(32);
			int numAdded = scanner.SetText(buffer);
			//TODO: how to know how much whitespace to add
			int theStart = scanner.Yychar();
			int lastPos = theStart + numAdded;
			int tmpTokType;
			int numSeen = 0;
			IList<AttributeSource.State> tmp = new AList<AttributeSource.State>();
			SetupSavedToken(0, type);
			tmp.AddItem(CaptureState());
			//while we can get a token and that token is the same type and we have not transitioned to a new wiki-item of the same type
			while ((tmpTokType = scanner.GetNextToken()) != WikipediaTokenizerImpl.YYEOF && tmpTokType
				 == tokenType && scanner.GetNumWikiTokensSeen() > numSeen)
			{
				int currPos = scanner.Yychar();
				//append whitespace
				for (int i = 0; i < (currPos - lastPos); i++)
				{
					buffer.Append(' ');
				}
				numAdded = scanner.SetText(buffer);
				SetupSavedToken(scanner.GetPositionIncrement(), type);
				tmp.AddItem(CaptureState());
				numSeen++;
				lastPos = currPos + numAdded;
			}
			//trim the buffer
			// TODO: this is inefficient
			string s = buffer.ToString().Trim();
			termAtt.SetEmpty().Append(s);
			offsetAtt.SetOffset(CorrectOffset(theStart), CorrectOffset(theStart + s.Length));
			flagsAtt.SetFlags(UNTOKENIZED_TOKEN_FLAG);
			//The way the loop is written, we will have proceeded to the next token.  We need to pushback the scanner to lastPos
			if (tmpTokType != WikipediaTokenizerImpl.YYEOF)
			{
				scanner.Yypushback(scanner.Yylength());
			}
			tokens = tmp.Iterator();
		}

		private void SetupSavedToken(int positionInc, string type)
		{
			SetupToken();
			posIncrAtt.SetPositionIncrement(positionInc);
			typeAtt.SetType(type);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CollapseTokens(int tokenType)
		{
			//collapse
			StringBuilder buffer = new StringBuilder(32);
			int numAdded = scanner.SetText(buffer);
			//TODO: how to know how much whitespace to add
			int theStart = scanner.Yychar();
			int lastPos = theStart + numAdded;
			int tmpTokType;
			int numSeen = 0;
			//while we can get a token and that token is the same type and we have not transitioned to a new wiki-item of the same type
			while ((tmpTokType = scanner.GetNextToken()) != WikipediaTokenizerImpl.YYEOF && tmpTokType
				 == tokenType && scanner.GetNumWikiTokensSeen() > numSeen)
			{
				int currPos = scanner.Yychar();
				//append whitespace
				for (int i = 0; i < (currPos - lastPos); i++)
				{
					buffer.Append(' ');
				}
				numAdded = scanner.SetText(buffer);
				numSeen++;
				lastPos = currPos + numAdded;
			}
			//trim the buffer
			// TODO: this is inefficient
			string s = buffer.ToString().Trim();
			termAtt.SetEmpty().Append(s);
			offsetAtt.SetOffset(CorrectOffset(theStart), CorrectOffset(theStart + s.Length));
			flagsAtt.SetFlags(UNTOKENIZED_TOKEN_FLAG);
			//The way the loop is written, we will have proceeded to the next token.  We need to pushback the scanner to lastPos
			if (tmpTokType != WikipediaTokenizerImpl.YYEOF)
			{
				scanner.Yypushback(scanner.Yylength());
			}
			else
			{
				tokens = null;
			}
		}

		private void SetupToken()
		{
			scanner.GetText(termAtt);
			int start = scanner.Yychar();
			offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + termAtt.Length));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			base.Close();
			scanner.Yyreset(input);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			scanner.Yyreset(input);
			tokens = null;
			scanner.Reset();
			first = true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void End()
		{
			base.End();
			// set final offset
			int finalOffset = CorrectOffset(scanner.Yychar() + scanner.Yylength());
			this.offsetAtt.SetOffset(finalOffset, finalOffset);
		}
	}
}
