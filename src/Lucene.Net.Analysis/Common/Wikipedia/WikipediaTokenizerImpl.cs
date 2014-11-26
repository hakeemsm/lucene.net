/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Wikipedia;
using Sharpen;

namespace Lucene.Net.Analysis.Wikipedia
{
	/// <summary>JFlex-generated tokenizer that is aware of Wikipedia syntax.</summary>
	/// <remarks>JFlex-generated tokenizer that is aware of Wikipedia syntax.</remarks>
	internal class WikipediaTokenizerImpl
	{
		/// <summary>This character denotes the end of file</summary>
		public const int YYEOF = -1;

		/// <summary>initial size of the lookahead buffer</summary>
		private const int ZZ_BUFFERSIZE = 4096;

		/// <summary>lexical states</summary>
		public const int YYINITIAL = 0;

		public const int CATEGORY_STATE = 2;

		public const int INTERNAL_LINK_STATE = 4;

		public const int EXTERNAL_LINK_STATE = 6;

		public const int TWO_SINGLE_QUOTES_STATE = 8;

		public const int THREE_SINGLE_QUOTES_STATE = 10;

		public const int FIVE_SINGLE_QUOTES_STATE = 12;

		public const int DOUBLE_EQUALS_STATE = 14;

		public const int DOUBLE_BRACE_STATE = 16;

		public const int STRING = 18;

		/// <summary>
		/// ZZ_LEXSTATE[l] is the state in the DFA for the lexical state l
		/// ZZ_LEXSTATE[l+1] is the state in the DFA for the lexical state l
		/// at the beginning of a line
		/// l is of the form l = 2*k, k a non negative integer
		/// </summary>
		private static readonly int ZZ_LEXSTATE = new int[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 
			4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9 };

		/// <summary>Translates characters to character classes</summary>
		private static readonly string ZZ_CMAP_PACKED = "\xb\x0\x1\x18\x1\x17\x1\x0\x1\x18\x1\x16\x16\x0\x1\x18\x1\x0\x1\xc"
			 + "\x1\x35\x2\x0\x1\x3\x1\x1\x4\x0\x1\xe\x1\x5\x1\x2\x1\xa\xc\x10" + "\x1\x1b\x1\x0\x1\x7\x1\xb\x1\xd\x1\x35\x1\x4\x2\xf\x1\x1e\x5\xf"
			 + "\x1\x29\x15\xf\x1\x19\x1\x0\x1\x1a\x1\x0\x1\x6\x1\x0\x1\x1f\x1\x2b" + "\x2\xf\x1\x21\x1\x28\x1\x22\x1\x32\x1\x29\x4\xf\x1\x2a\x1\x23\x1\x33"
			 + "\x1\xf\x1\x24\x1\x34\x1\x20\x3\xf\x1\x2c\x1\x25\x1\xf\x1\x2d\x1\x2f" + "\x1\x2e\x66\x0\x1b\xf\x1\x0\x25\xf\x1\x0\u0568\xf\xc\x11\xce\xf\xc\x11"
			 + "\u026c\xf\xc\x11\xa6\xf\xc\x11\xa6\xf\xc\x11\xa6\xf\xc\x11\xa6\xf\xc\x11" + 
			"\xa7\xf\xb\x11\xa6\xf\xc\x11\xa6\xf\xc\x11\xa6\xf\xc\x11\x154\xf\xc\x11" + "\xa6\xf\xc\x11\u0166\xf\xc\x11\x10a\xf\u0100\xf\u0e00\xf\u1040\x0\u0150\x15\x8c\x0"
			 + "\x14\x15\u0100\x0\xc8\x15\xc8\x0\u19c0\x15\x64\x0\u5200\x15\u0c00\x0\u2bb0\x14\u2150\x0"
			 + "\u0200\x15\u0465\x0\x49\x15\x4b\xf\x2b\x0";

		/// <summary>Translates characters to character classes</summary>
		private static readonly char[] ZZ_CMAP = ZzUnpackCMap(ZZ_CMAP_PACKED);

		/// <summary>Translates DFA states to action switch labels.</summary>
		/// <remarks>Translates DFA states to action switch labels.</remarks>
		private static readonly int[] ZZ_ACTION = ZzUnpackAction();

		private static readonly string ZZ_ACTION_PACKED_0 = "\xc\x0\x4\x1\x4\x2\x1\x3\x1\x4\x1\x1\x2\x5\x1\x6"
			 + "\x1\x5\x1\x7\x1\x5\x2\xa\x1\xb\x1\x5\x1\xc\x1\xb" + "\x1\xd\x1\xe\x1\xf\x1\x10\x1\xf\x1\x11\x1\x14\x1\xa"
			 + "\x1\x15\x1\xa\x4\x16\x1\x17\x1\x18\x1\x19\x1\x1a\x3\x0" + "\x1\x1b\xe\x0\x1\x1e\x1\x1f\x1\x20\x1\x21\x1\xb\x1\x0"
			 + "\x1\x22\x1\x23\x1\x24\x1\x0\x1\x25\x1\x0\x1\x28\x3\x0" + "\x1\x29\x1\x2a\x2\x2b\x1\x2a\x2\x2c\x2\x0\x1\x2b\x1\x0"
			 + "\xe\x2b\x1\x2a\x3\x0\x1\xb\x1\x2d\x3\x0\x1\x2e\x1\x2f" + "\x5\x0\x1\x32\x4\x0\x1\x32\x2\x0\x2\x32\x2\x0\x1\xb"
			 + "\x5\x0\x1\x1f\x1\x2a\x1\x2b\x1\x33\x3\x0\x1\xb\x2\x0" + "\x1\x34\x1e\x0\x1\x35\x2\x0\x1\x36\x1\x37\x1\x38";

		private static int[] ZzUnpackAction()
		{
			int[] result = new int[181];
			int offset = 0;
			offset = ZzUnpackAction(ZZ_ACTION_PACKED_0, offset, result);
			return result;
		}

		private static int ZzUnpackAction(string packed, int offset, int[] result)
		{
			int i = 0;
			int j = offset;
			int l = packed.Length;
			while (i < l)
			{
				int count = packed[i++];
				int value = packed[i++];
				do
				{
					result[j++] = value;
				}
				while (--count > 0);
			}
			return j;
		}

		/// <summary>Translates a state to a row index in the transition table</summary>
		private static readonly int[] ZZ_ROWMAP = ZzUnpackRowMap();

		private static readonly string ZZ_ROWMAP_PACKED_0 = "\x0\x0\x0\x36\x0\x82\x0\xcc\x0\x104\x0\x14e\x0\u0108\x0\u0134"
			 + "\x0\u0160\x0\u018c\x0\u01b8\x0\u01e4\x0\u0210\x0\u023c\x0\u0268\x0\u0294" + 
			"\x0\u02c0\x0\u02ec\x0\u01b8\x0\u0318\x0\u0344\x0\u01b8\x0\u0370\x0\u039c" + "\x0\u03c8\x0\u03f4\x0\u0420\x0\u01b8\x0\u0370\x0\u044c\x0\u0478\x0\u01b8"
			 + "\x0\u04a4\x0\u04d0\x0\u04fc\x0\u0528\x0\u0554\x0\u0580\x0\u05ac\x0\u05d8" + 
			"\x0\u0604\x0\u0630\x0\u065c\x0\u01b8\x0\u0688\x0\u0370\x0\u06b4\x0\u06e0" + "\x0\u070c\x0\u01b8\x0\u01b8\x0\u0738\x0\u0764\x0\u0790\x0\u01b8\x0\u07bc"
			 + "\x0\u07e8\x0\u0814\x0\u0840\x0\u086c\x0\u0898\x0\u08c4\x0\u08f0\x0\u091c" + 
			"\x0\u0948\x0\u0974\x0\u09a0\x0\u09cc\x0\u09f8\x0\u01b8\x0\u01b8\x0\u0a24" + "\x0\u0a50\x0\u0a7c\x0\u0a7c\x0\u01b8\x0\u0aa8\x0\u0ad4\x0\u0b00\x0\u0b2c"
			 + "\x0\u0b58\x0\u0b84\x0\u0bb0\x0\u0bdc\x0\u0c08\x0\u0c34\x0\u0c60\x0\u0c8c" + 
			"\x0\u0814\x0\u0cb8\x0\u0ce4\x0\u0d10\x0\u0d3c\x0\u0d68\x0\u0d94\x0\u0dc0" + "\x0\u0dec\x0\u0e18\x0\u0e44\x0\u0e70\x0\u0e9c\x0\u0ec8\x0\u0ef4\x0\u0f20"
			 + "\x0\u0f4c\x0\u0f78\x0\u0fa4\x0\u0fd0\x0\u0ffc\x0\u1028\x0\u1054\x0\u01b8" + 
			"\x0\u1080\x0\u10ac\x0\u10d8\x0\u1104\x0\u01b8\x0\u1130\x0\u115c\x0\u1188" + "\x0\u11b4\x0\u11e0\x0\u120c\x0\u1238\x0\u1264\x0\u1290\x0\u12bc\x0\u12e8"
			 + "\x0\u1314\x0\u1340\x0\u07e8\x0\u0974\x0\u136c\x0\u1398\x0\u13c4\x0\u13f0" + 
			"\x0\u141c\x0\u1448\x0\u1474\x0\u14a0\x0\u01b8\x0\u14cc\x0\u14f8\x0\u1524" + "\x0\u1550\x0\u157c\x0\u15a8\x0\u15d4\x0\u1600\x0\u162c\x0\u01b8\x0\u1658"
			 + "\x0\u1684\x0\u16b0\x0\u16dc\x0\u1708\x0\u1734\x0\u1760\x0\u178c\x0\u17b8" + 
			"\x0\u17e4\x0\u1810\x0\u183c\x0\u1868\x0\u1894\x0\u18c0\x0\u18ec\x0\u1918" + "\x0\u1944\x0\u1970\x0\u199c\x0\u19c8\x0\u19f4\x0\u1a20\x0\u1a4c\x0\u1a78"
			 + "\x0\u1aa4\x0\u1ad0\x0\u01b8\x0\u01b8\x0\u01b8";

		private static int[] ZzUnpackRowMap()
		{
			int[] result = new int[181];
			int offset = 0;
			offset = ZzUnpackRowMap(ZZ_ROWMAP_PACKED_0, offset, result);
			return result;
		}

		private static int ZzUnpackRowMap(string packed, int offset, int[] result)
		{
			int i = 0;
			int j = offset;
			int l = packed.Length;
			while (i < l)
			{
				int high = packed[i++] << 16;
				result[j++] = high | packed[i++];
			}
			return j;
		}

		/// <summary>The transition table of the DFA</summary>
		private static readonly int[] ZZ_TRANS = ZzUnpackTrans();

		private static readonly string ZZ_TRANS_PACKED_0 = "\x1\xd\x1\xe\x5\xd\x1\xf\x1\xd\x1\x10\x3\xd\x1\x11"
			 + "\x1\x14\x1\x15\x1\x16\x1\x17\x3\xd\x1\x18\x2\xd\xf\x11" + "\x1\x19\x2\xd\x3\x11\x1\xd\x7\x1a\x1\x1b\x5\x1a\x4\x1e"
			 + "\x5\x1a\x1\x1f\x1\x1a\xf\x1e\x3\x1a\x3\x1e\xa\x1a\x1\x1b" + "\x5\x1a\x4\x20\x5\x1a\x1\x21\x1\x1a\xf\x20\x3\x1a\x3\x20"
			 + "\x1\x1a\x7\x22\x1\x23\x5\x22\x4\x24\x1\x22\x1\x25\x2\x1a" + "\x1\x22\x1\x28\x1\x22\xf\x24\x3\x22\x1\x29\x2\x24\x2\x22"
			 + "\x1\x2a\x5\x22\x1\x23\x5\x22\x4\x2b\x4\x22\x1\x2c\x2\x22" + "\xf\x2b\x3\x22\x3\x2b\xa\x22\x1\x23\x5\x22\x4\x2d\x4\x22"
			 + "\x1\x2c\x2\x22\xf\x2d\x3\x22\x3\x2d\xa\x22\x1\x23\x5\x22" + "\x4\x2d\x4\x22\x1\x2e\x2\x22\xf\x2d\x3\x22\x3\x2d\xa\x22"
			 + "\x1\x23\x1\x22\x1\x2f\x3\x22\x4\x32\x7\x22\xf\x32\x3\x22" + "\x3\x32\xa\x22\x1\x33\x5\x22\x4\x34\x7\x22\xf\x34\x1\x22"
			 + "\x1\x35\x1\x22\x3\x34\x1\x22\x1\x36\x1\x37\x5\x36\x1\x38" + "\x1\x36\x1\x39\x3\x36\x4\x3c\x4\x36\x1\x3d\x2\x36\xf\x3c"
			 + "\x2\x36\x1\x3e\x3\x3c\x1\x36\x37\x0\x1\x3f\x3e\x0\x1\x40" + "\x4\x0\x4\x41\x7\x0\x6\x41\x1\x42\x6\x41\x3\x0\x3\x41"
			 + "\xc\x0\x1\x43\x2b\x0\x1\x46\x1\x47\x1\x48\x1\x49\x2\x4a" + "\x1\x0\x1\x4b\x3\x0\x1\x4b\x1\x11\x1\x14\x1\x15\x1\x16"
			 + "\x7\x0\xf\x11\x3\x0\x3\x11\x3\x0\x1\x4c\x1\x0\x1\x4d" + "\x2\x64\x1\x0\x1\x65\x3\x0\x1\x65\x3\x14\x1\x16\x7\x0"
			 + "\xf\x14\x3\x0\x3\x14\x2\x0\x1\x46\x1\x66\x1\x48\x1\x49" + "\x2\x64\x1\x0\x1\x65\x3\x0\x1\x65\x1\x15\x1\x14\x1\x15"
			 + "\x1\x16\x7\x0\xf\x15\x3\x0\x3\x15\x3\x0\x1\x67\x1\x0" + "\x1\x4d\x2\x4a\x1\x0\x1\x4b\x3\x0\x1\x4b\x4\x16\x7\x0"
			 + "\xf\x16\x3\x0\x3\x16\x1a\x0\x1\x68\x49\x0\x1\x69\x10\x0" + "\x1\x40\x4\x0\x4\x41\x7\x0\xf\x41\x3\x0\x3\x41\x10\x0"
			 + "\x4\x1e\x7\x0\xf\x1e\x3\x0\x3\x1e\x1b\x0\x1\x6a\x2a\x0" + "\x4\x20\x7\x0\xf\x20\x3\x0\x3\x20\x1b\x0\x1\x6b\x2a\x0"
			 + "\x4\x24\x7\x0\xf\x24\x3\x0\x3\x24\x18\x0\x1\x1a\x2d\x0" + "\x4\x24\x7\x0\x2\x24\x1\x6e\xc\x24\x3\x0\x3\x24\x2\x0"
			 + "\x1\x6f\x43\x0\x4\x2b\x7\x0\xf\x2b\x3\x0\x3\x2b\x1a\x0" + "\x1\x70\x2b\x0\x4\x2d\x7\x0\xf\x2d\x3\x0\x3\x2d\x1a\x0"
			 + "\x1\x71\x25\x0\x1\x72\x39\x0\x4\x32\x7\x0\xf\x32\x3\x0" + "\x3\x32\xb\x0\x1\x73\x4\x0\x4\x41\x7\x0\xf\x41\x3\x0"
			 + "\x3\x41\x10\x0\x4\x34\x7\x0\xf\x34\x3\x0\x3\x34\x2f\x0" + "\x1\x72\x6\x0\x1\x74\x3f\x0\x1\x75\x39\x0\x4\x3c\x7\x0"
			 + "\xf\x3c\x3\x0\x3\x3c\x1a\x0\x1\x78\x2b\x0\x4\x41\x7\x0" + "\xf\x41\x3\x0\x3\x41\xe\x0\x1\x22\x1\x0\x4\x79\x1\x0"
			 + "\x3\x7a\x3\x0\xf\x79\x3\x0\x3\x79\xe\x0\x1\x22\x1\x0" + "\x4\x79\x1\x0\x3\x7a\x3\x0\x3\x79\x1\x7b\xb\x79\x3\x0"
			 + "\x3\x79\x10\x0\x1\x7c\x1\x0\x1\x7c\xa\x0\xf\x7c\x3\x0" + "\x3\x7c\x10\x0\x1\x7d\x1\x7e\x1\x7f\x1\x82\x7\x0\xf\x7d"
			 + "\x3\x0\x3\x7d\x10\x0\x1\x83\x1\x0\x1\x83\xa\x0\xf\x83" + "\x3\x0\x3\x83\x10\x0\x1\x84\x1\x85\x1\x84\x1\x85\x7\x0"
			 + "\xf\x84\x3\x0\x3\x84\x10\x0\x1\x86\x2\x87\x1\x88\x7\x0" + "\xf\x86\x3\x0\x3\x86\x10\x0\x1\x4b\x2\x89\xa\x0\xf\x4b"
			 + "\x3\x0\x3\x4b\x10\x0\x1\x8c\x2\x8d\x1\x8e\x7\x0\xf\x8c" + "\x3\x0\x3\x8c\x10\x0\x4\x85\x7\x0\xf\x85\x3\x0\x3\x85"
			 + "\x10\x0\x1\x8f\x2\x90\x1\x91\x7\x0\xf\x8f\x3\x0\x3\x8f" + "\x10\x0\x1\x92\x2\x93\x1\x96\x7\x0\xf\x92\x3\x0\x3\x92"
			 + "\x10\x0\x1\x97\x1\x8d\x1\x98\x1\x8e\x7\x0\xf\x97\x3\x0" + "\x3\x97\x10\x0\x1\x99\x2\x7e\x1\x82\x7\x0\xf\x99\x3\x0"
			 + "\x3\x99\x1e\x0\x1\x9a\x1\x9b\x40\x0\x1\x9c\x1b\x0\x4\x24" + "\x7\x0\x2\x24\x1\x9d\xc\x24\x3\x0\x3\x24\x2\x0\x1\xa0"
			 + "\x65\x0\x1\xa1\x1\xa2\x28\x0\x4\x41\x7\x0\x6\x41\x1\xa3" + "\x6\x41\x3\x0\x3\x41\x2\x0\x1\xa4\x3f\x0\x1\xa5\x47\x0"
			 + "\x1\xa6\x1\xa7\x22\x0\x1\xaa\x1\x0\x1\x22\x1\x0\x4\x79" + "\x1\x0\x3\x7a\x3\x0\xf\x79\x3\x0\x3\x79\x10\x0\x4\xab"
			 + "\x1\x0\x3\x7a\x3\x0\xf\xab\x3\x0\x3\xab\xc\x0\x1\xaa" + "\x1\x0\x1\x22\x1\x0\x4\x79\x1\x0\x3\x7a\x3\x0\xa\x79"
			 + "\x1\xac\x4\x79\x3\x0\x3\x79\x2\x0\x1\x46\xd\x0\x1\x7c" + "\x1\x0\x1\x7c\xa\x0\xf\x7c\x3\x0\x3\x7c\x3\x0\x1\xad"
			 + "\x1\x0\x1\x4d\x2\xae\x6\x0\x1\x7d\x1\x7e\x1\x7f\x1\x82" + "\x7\x0\xf\x7d\x3\x0\x3\x7d\x3\x0\x1\xaf\x1\x0\x1\x4d"
			 + "\x2\xb0\x1\x0\x1\xb1\x3\x0\x1\xb1\x3\x7e\x1\x82\x7\x0" + "\xf\x7e\x3\x0\x3\x7e\x3\x0\x1\xc8\x1\x0\x1\x4d\x2\xb0"
			 + "\x1\x0\x1\xb1\x3\x0\x1\xb1\x1\x7f\x1\x7e\x1\x7f\x1\x82" + "\x7\x0\xf\x7f\x3\x0\x3\x7f\x3\x0\x1\xc9\x1\x0\x1\x4d"
			 + "\x2\xae\x6\x0\x4\x82\x7\x0\xf\x82\x3\x0\x3\x82\x3\x0" + "\x1\xca\x2\x0\x1\xca\x7\x0\x1\x84\x1\x85\x1\x84\x1\x85"
			 + "\x7\x0\xf\x84\x3\x0\x3\x84\x3\x0\x1\xca\x2\x0\x1\xca" + "\x7\x0\x4\x85\x7\x0\xf\x85\x3\x0\x3\x85\x3\x0\x1\xae"
			 + "\x1\x0\x1\x4d\x2\xae\x6\x0\x1\x86\x2\x87\x1\x88\x7\x0" + "\xf\x86\x3\x0\x3\x86\x3\x0\x1\xb0\x1\x0\x1\x4d\x2\xb0"
			 + "\x1\x0\x1\xb1\x3\x0\x1\xb1\x3\x87\x1\x88\x7\x0\xf\x87" + "\x3\x0\x3\x87\x3\x0\x1\xae\x1\x0\x1\x4d\x2\xae\x6\x0"
			 + "\x4\x88\x7\x0\xf\x88\x3\x0\x3\x88\x3\x0\x1\xb1\x2\x0" + "\x2\xb1\x1\x0\x1\xb1\x3\x0\x1\xb1\x3\x89\xa\x0\xf\x89"
			 + "\x3\x0\x3\x89\x3\x0\x1\x67\x1\x0\x1\x4d\x2\x4a\x1\x0" + "\x1\x4b\x3\x0\x1\x4b\x1\x8c\x2\x8d\x1\x8e\x7\x0\xf\x8c"
			 + "\x3\x0\x3\x8c\x3\x0\x1\x4c\x1\x0\x1\x4d\x2\x64\x1\x0" + "\x1\x65\x3\x0\x1\x65\x3\x8d\x1\x8e\x7\x0\xf\x8d\x3\x0"
			 + "\x3\x8d\x3\x0\x1\x67\x1\x0\x1\x4d\x2\x4a\x1\x0\x1\x4b" + "\x3\x0\x1\x4b\x4\x8e\x7\x0\xf\x8e\x3\x0\x3\x8e\x3\x0"
			 + "\x1\x4a\x1\x0\x1\x4d\x2\x4a\x1\x0\x1\x4b\x3\x0\x1\x4b" + "\x1\x8f\x2\x90\x1\x91\x7\x0\xf\x8f\x3\x0\x3\x8f\x3\x0"
			 + "\x1\x64\x1\x0\x1\x4d\x2\x64\x1\x0\x1\x65\x3\x0\x1\x65" + "\x3\x90\x1\x91\x7\x0\xf\x90\x3\x0\x3\x90\x3\x0\x1\x4a"
			 + "\x1\x0\x1\x4d\x2\x4a\x1\x0\x1\x4b\x3\x0\x1\x4b\x4\x91" + "\x7\x0\xf\x91\x3\x0\x3\x91\x3\x0\x1\x4b\x2\x0\x2\x4b"
			 + "\x1\x0\x1\x4b\x3\x0\x1\x4b\x1\x92\x2\x93\x1\x96\x7\x0" + "\xf\x92\x3\x0\x3\x92\x3\x0\x1\x65\x2\x0\x2\x65\x1\x0"
			 + "\x1\x65\x3\x0\x1\x65\x3\x93\x1\x96\x7\x0\xf\x93\x3\x0" + "\x3\x93\x3\x0\x1\x4b\x2\x0\x2\x4b\x1\x0\x1\x4b\x3\x0"
			 + "\x1\x4b\x4\x96\x7\x0\xf\x96\x3\x0\x3\x96\x3\x0\x1\xcb" + "\x1\x0\x1\x4d\x2\x4a\x1\x0\x1\x4b\x3\x0\x1\x4b\x1\x97"
			 + "\x1\x8d\x1\x98\x1\x8e\x7\x0\xf\x97\x3\x0\x3\x97\x3\x0" + "\x1\xcc\x1\x0\x1\x4d\x2\x64\x1\x0\x1\x65\x3\x0\x1\x65"
			 + "\x1\x98\x1\x8d\x1\x98\x1\x8e\x7\x0\xf\x98\x3\x0\x3\x98" + "\x3\x0\x1\xc9\x1\x0\x1\x4d\x2\xae\x6\x0\x1\x99\x2\x7e"
			 + "\x1\x82\x7\x0\xf\x99\x3\x0\x3\x99\x1f\x0\x1\x9b\x36\x0" + "\x1\xcd\x40\x0\x1\xce\x1a\x0\x4\x24\x7\x0\xf\x24\x3\x0"
			 + "\x1\x24\x1\xcf\x1\x24\x1f\x0\x1\xa2\x36\x0\x1\xd2\x23\x0" + "\x1\x22\x1\x0\x4\x79\x1\x0\x3\x7a\x3\x0\x3\x79\x1\xd3"
			 + "\xb\x79\x3\x0\x3\x79\x2\x0\x1\xd4\x66\x0\x1\xa7\x36\x0" + "\x1\xd5\x22\x0\x1\xd6\x34\x0\x1\xaa\x3\x0\x4\xab\x7\x0"
			 + "\xf\xab\x3\x0\x3\xab\xc\x0\x1\xaa\x1\x0\x1\xd7\x1\x0" + "\x4\x79\x1\x0\x3\x7a\x3\x0\xf\x79\x3\x0\x3\x79\x10\x0"
			 + "\x1\xd8\x1\x82\x1\xd8\x1\x82\x7\x0\xf\xd8\x3\x0\x3\xd8" + "\x10\x0\x4\x88\x7\x0\xf\x88\x3\x0\x3\x88\x10\x0\x4\x8e"
			 + "\x7\x0\xf\x8e\x3\x0\x3\x8e\x10\x0\x4\x91\x7\x0\xf\x91" + "\x3\x0\x3\x91\x10\x0\x4\x96\x7\x0\xf\x96\x3\x0\x3\x96"
			 + "\x10\x0\x1\xd9\x1\x8e\x1\xd9\x1\x8e\x7\x0\xf\xd9\x3\x0" + "\x3\xd9\x10\x0\x4\x82\x7\x0\xf\x82\x3\x0\x3\x82\x10\x0"
			 + "\x4\xdc\x7\x0\xf\xdc\x3\x0\x3\xdc\x21\x0\x1\xdd\x3d\x0" + "\x1\xde\x1e\x0\x4\x24\x6\x0\x1\xdf\xf\x24\x3\x0\x2\x24"
			 + "\x1\xe0\x21\x0\x1\xe1\x20\x0\x1\xaa\x1\x0\x1\x22\x1\x0" + "\x4\x79\x1\x0\x3\x7a\x3\x0\xa\x79\x1\xe2\x4\x79\x3\x0"
			 + "\x3\x79\x2\x0\x1\xe3\x68\x0\x1\xe6\x24\x0\x4\xe7\x7\x0" + "\xf\xe7\x3\x0\x3\xe7\x3\x0\x1\xad\x1\x0\x1\x4d\x2\xae"
			 + "\x6\x0\x1\xd8\x1\x82\x1\xd8\x1\x82\x7\x0\xf\xd8\x3\x0" + "\x3\xd8\x3\x0\x1\xcb\x1\x0\x1\x4d\x2\x4a\x1\x0\x1\x4b"
			 + "\x3\x0\x1\x4b\x1\xd9\x1\x8e\x1\xd9\x1\x8e\x7\x0\xf\xd9" + "\x3\x0\x3\xd9\x3\x0\x1\xca\x2\x0\x1\xca\x7\x0\x4\xdc"
			 + "\x7\x0\xf\xdc\x3\x0\x3\xdc\x22\x0\x1\xe8\x37\x0\x1\xe9" + "\x1a\x0\x1\xea\x3c\x0\x4\x24\x6\x0\x1\xdf\xf\x24\x3\x0"
			 + "\x3\x24\x22\x0\x1\xeb\x1f\x0\x1\xaa\x1\x0\x1\x72\x1\x0" + "\x4\x79\x1\x0\x3\x7a\x3\x0\xf\x79\x3\x0\x3\x79\x22\x0"
			 + "\x1\xec\x20\x0\x1\xed\x2\x0\x4\xe7\x7\x0\xf\xe7\x3\x0" + "\x3\xe7\x23\x0\x1\xf0\x3e\x0\x1\xf1\x14\x0\x1\xf2\x4d\x0"
			 + "\x1\xf3\x35\x0\x1\xf4\x20\x0\x1\x22\x1\x0\x4\xab\x1\x0" + "\x3\x7a\x3\x0\xf\xab\x3\x0\x3\xab\x24\x0\x1\xf5\x35\x0"
			 + "\x1\xf6\x21\x0\x4\xf7\x7\x0\xf\xf7\x3\x0\x3\xf7\x24\x0" + "\x1\xfa\x35\x0\x1\xfb\x36\x0\x1\xfc\x3d\x0\x1\xfd\xb\x0"
			 + "\x1\xfe\xc\x0\x4\xf7\x7\x0\xf\xf7\x3\x0\x3\xf7\x25\x0" + "\x1\xff\x35\x0\x1\x100\x36\x0\x1\x101\x16\x0\x1\xd\x3e\x0"
			 + "\x4\x104\x7\x0\xf\x104\x3\x0\x3\x104\x28\x0\x1\x105\x35\x0" + "\x1\x106\x2b\x0\x1\x107\x1a\x0\x2\x104\x1\x0\x2\x104\x1\x0"
			 + "\x2\x104\x2\x0\x5\x104\x7\x0\xf\x104\x3\x0\x4\x104\x1b\x0" + "\x1\x108\x35\x0\x1\x109\x18\x0";

		private static int[] ZzUnpackTrans()
		{
			int[] result = new int[6908];
			int offset = 0;
			offset = ZzUnpackTrans(ZZ_TRANS_PACKED_0, offset, result);
			return result;
		}

		private static int ZzUnpackTrans(string packed, int offset, int[] result)
		{
			int i = 0;
			int j = offset;
			int l = packed.Length;
			while (i < l)
			{
				int count = packed[i++];
				int value = packed[i++];
				value--;
				do
				{
					result[j++] = value;
				}
				while (--count > 0);
			}
			return j;
		}

		private const int ZZ_UNKNOWN_ERROR = 0;

		private const int ZZ_NO_MATCH = 1;

		private const int ZZ_PUSHBACK_2BIG = 2;

		private static readonly string ZZ_ERROR_MSG = new string[] { "Unkown internal scanner error"
			, "Error: could not match input", "Error: pushback value was too large" };

		/// <summary>ZZ_ATTRIBUTE[aState] contains the attributes of state <code>aState</code>
		/// 	</summary>
		private static readonly int[] ZZ_ATTRIBUTE = ZzUnpackAttribute();

		private static readonly string ZZ_ATTRIBUTE_PACKED_0 = "\xc\x0\x1\xb\x7\x1\x1\xb\x2\x1\x1\xb\x5\x1\x1\xb"
			 + "\x3\x1\x1\xb\xd\x1\x1\xb\x5\x1\x2\xb\x3\x0\x1\xb" + "\xe\x0\x2\x1\x2\xb\x1\x1\x1\x0\x2\x1\x1\xb\x1\x0"
			 + "\x1\x1\x1\x0\x1\x1\x3\x0\x7\x1\x2\x0\x1\x1\x1\x0" + "\xf\x1\x3\x0\x1\x1\x1\xb\x3\x0\x1\x1\x1\xb\x5\x0"
			 + "\x1\x1\x4\x0\x1\x1\x2\x0\x2\x1\x2\x0\x1\x1\x5\x0" + "\x1\xb\x3\x1\x3\x0\x1\x1\x2\x0\x1\xb\x1e\x0\x1\x1"
			 + "\x2\x0\x3\xb";

		private static int[] ZzUnpackAttribute()
		{
			int[] result = new int[181];
			int offset = 0;
			offset = ZzUnpackAttribute(ZZ_ATTRIBUTE_PACKED_0, offset, result);
			return result;
		}

		private static int ZzUnpackAttribute(string packed, int offset, int[] result)
		{
			int i = 0;
			int j = offset;
			int l = packed.Length;
			while (i < l)
			{
				int count = packed[i++];
				int value = packed[i++];
				do
				{
					result[j++] = value;
				}
				while (--count > 0);
			}
			return j;
		}

		/// <summary>the input device</summary>
		private StreamReader zzReader;

		/// <summary>the current state of the DFA</summary>
		private int zzState;

		/// <summary>the current lexical state</summary>
		private int zzLexicalState = YYINITIAL;

		/// <summary>
		/// this buffer contains the current text to be matched and is
		/// the source of the yytext() string
		/// </summary>
		private char zzBuffer = new char[ZZ_BUFFERSIZE];

		/// <summary>the textposition at the last accepting state</summary>
		private int zzMarkedPos;

		/// <summary>the current text position in the buffer</summary>
		private int zzCurrentPos;

		/// <summary>startRead marks the beginning of the yytext() string in the buffer</summary>
		private int zzStartRead;

		/// <summary>
		/// endRead marks the last character in the buffer, that has been read
		/// from input
		/// </summary>
		private int zzEndRead;

		/// <summary>number of newlines encountered up to the start of the matched text</summary>
		private int yyline;

		/// <summary>the number of characters up to the start of the matched text</summary>
		private int yychar;

		/// <summary>
		/// the number of characters from the last newline up to the start of the
		/// matched text
		/// </summary>
		private int yycolumn;

		/// <summary>zzAtBOL == true <=> the scanner is currently at the beginning of a line</summary>
		private bool zzAtBOL = true;

		/// <summary>zzAtEOF == true <=> the scanner is at the EOF</summary>
		private bool zzAtEOF;

		/// <summary>denotes if the user-EOF-code has already been executed</summary>
		private bool zzEOFDone;

		public const int ALPHANUM = WikipediaTokenizer.ALPHANUM_ID;

		public const int APOSTROPHE = WikipediaTokenizer.APOSTROPHE_ID;

		public const int ACRONYM = WikipediaTokenizer.ACRONYM_ID;

		public const int COMPANY = WikipediaTokenizer.COMPANY_ID;

		public const int EMAIL = WikipediaTokenizer.EMAIL_ID;

		public const int HOST = WikipediaTokenizer.HOST_ID;

		public const int NUM = WikipediaTokenizer.NUM_ID;

		public const int CJ = WikipediaTokenizer.CJ_ID;

		public const int INTERNAL_LINK = WikipediaTokenizer.INTERNAL_LINK_ID;

		public const int EXTERNAL_LINK = WikipediaTokenizer.EXTERNAL_LINK_ID;

		public const int CITATION = WikipediaTokenizer.CITATION_ID;

		public const int CATEGORY = WikipediaTokenizer.CATEGORY_ID;

		public const int BOLD = WikipediaTokenizer.BOLD_ID;

		public const int ITALICS = WikipediaTokenizer.ITALICS_ID;

		public const int BOLD_ITALICS = WikipediaTokenizer.BOLD_ITALICS_ID;

		public const int HEADING = WikipediaTokenizer.HEADING_ID;

		public const int SUB_HEADING = WikipediaTokenizer.SUB_HEADING_ID;

		public const int EXTERNAL_LINK_URL = WikipediaTokenizer.EXTERNAL_LINK_URL_ID;

		private int currentTokType;

		private int numBalanced = 0;

		private int positionInc = 1;

		private int numLinkToks = 0;

		private int numWikiTokensSeen = 0;

		public static readonly string[] TOKEN_TYPES = WikipediaTokenizer.TOKEN_TYPES;

		//Anytime we start a new on a Wiki reserved token (category, link, etc.) this value will be 0, otherwise it will be the number of tokens seen
		//this can be useful for detecting when a new reserved token is encountered
		//see https://issues.apache.org/jira/browse/LUCENE-1133
		/// <summary>Returns the number of tokens seen inside a category or link, etc.</summary>
		/// <remarks>Returns the number of tokens seen inside a category or link, etc.</remarks>
		/// <returns>the number of tokens seen inside the context of wiki syntax.</returns>
		public int GetNumWikiTokensSeen()
		{
			return numWikiTokensSeen;
		}

		public int Yychar()
		{
			return yychar;
		}

		public int GetPositionIncrement()
		{
			return positionInc;
		}

		/// <summary>Fills Lucene token with the current token text.</summary>
		/// <remarks>Fills Lucene token with the current token text.</remarks>
		internal void GetText(CharTermAttribute t)
		{
			t.CopyBuffer(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
		}

		internal int SetText(StringBuilder buffer)
		{
			int length = zzMarkedPos - zzStartRead;
			buffer.Append(zzBuffer, zzStartRead, length);
			return length;
		}

		internal void Reset()
		{
			currentTokType = 0;
			numBalanced = 0;
			positionInc = 1;
			numLinkToks = 0;
			numWikiTokensSeen = 0;
		}

		/// <summary>Creates a new scanner</summary>
		/// <param name="in">the java.io.Reader to read input from.</param>
		internal WikipediaTokenizerImpl(StreamReader @in)
		{
			this.zzReader = @in;
		}

		/// <summary>Unpacks the compressed character translation table.</summary>
		/// <remarks>Unpacks the compressed character translation table.</remarks>
		/// <param name="packed">the packed character translation table</param>
		/// <returns>the unpacked character translation table</returns>
		private static char[] ZzUnpackCMap(string packed)
		{
			char[] map = new char[unchecked((int)(0x10000))];
			int i = 0;
			int j = 0;
			while (i < 230)
			{
				int count = packed[i++];
				char value = packed[i++];
				do
				{
					map[j++] = value;
				}
				while (--count > 0);
			}
			return map;
		}

		/// <summary>Refills the input buffer.</summary>
		/// <remarks>Refills the input buffer.</remarks>
		/// <returns><code>false</code>, iff there was new input.</returns>
		/// <exception>
		/// java.io.IOException
		/// if any I/O-Error occurs
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		private bool ZzRefill()
		{
			if (zzStartRead > 0)
			{
				System.Array.Copy(zzBuffer, zzStartRead, zzBuffer, 0, zzEndRead - zzStartRead);
				zzEndRead -= zzStartRead;
				zzCurrentPos -= zzStartRead;
				zzMarkedPos -= zzStartRead;
				zzStartRead = 0;
			}
			if (zzCurrentPos >= zzBuffer.Length)
			{
				char[] newBuffer = new char[zzCurrentPos * 2];
				System.Array.Copy(zzBuffer, 0, newBuffer, 0, zzBuffer.Length);
				zzBuffer = newBuffer;
			}
			int numRead = zzReader.Read(zzBuffer, zzEndRead, zzBuffer.Length - zzEndRead);
			if (numRead > 0)
			{
				zzEndRead += numRead;
				return false;
			}
			// unlikely but not impossible: read 0 characters, but not at end of stream    
			if (numRead == 0)
			{
				int c = zzReader.Read();
				if (c == -1)
				{
					return true;
				}
				else
				{
					zzBuffer[zzEndRead++] = (char)c;
					return false;
				}
			}
			// numRead < 0
			return true;
		}

		/// <summary>Closes the input stream.</summary>
		/// <remarks>Closes the input stream.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public void Yyclose()
		{
			zzAtEOF = true;
			zzEndRead = zzStartRead;
			if (zzReader != null)
			{
				zzReader.Close();
			}
		}

		/// <summary>Resets the scanner to read from a new input stream.</summary>
		/// <remarks>
		/// Resets the scanner to read from a new input stream.
		/// Does not close the old reader.
		/// All internal variables are reset, the old input stream
		/// <b>cannot</b> be reused (internal buffer is discarded and lost).
		/// Lexical state is set to <tt>ZZ_INITIAL</tt>.
		/// Internal scan buffer is resized down to its initial length, if it has grown.
		/// </remarks>
		/// <param name="reader">the new input stream</param>
		public void Yyreset(StreamReader reader)
		{
			zzReader = reader;
			zzAtBOL = true;
			zzAtEOF = false;
			zzEOFDone = false;
			zzEndRead = zzStartRead = 0;
			zzCurrentPos = zzMarkedPos = 0;
			yyline = yychar = yycolumn = 0;
			zzLexicalState = YYINITIAL;
			if (zzBuffer.Length > ZZ_BUFFERSIZE)
			{
				zzBuffer = new char[ZZ_BUFFERSIZE];
			}
		}

		/// <summary>Returns the current lexical state.</summary>
		/// <remarks>Returns the current lexical state.</remarks>
		public int Yystate()
		{
			return zzLexicalState;
		}

		/// <summary>Enters a new lexical state</summary>
		/// <param name="newState">the new lexical state</param>
		public void Yybegin(int newState)
		{
			zzLexicalState = newState;
		}

		/// <summary>Returns the text matched by the current regular expression.</summary>
		/// <remarks>Returns the text matched by the current regular expression.</remarks>
		public string Yytext()
		{
			return new string(zzBuffer, zzStartRead, zzMarkedPos - zzStartRead);
		}

		/// <summary>
		/// Returns the character at position <tt>pos</tt> from the
		/// matched text.
		/// </summary>
		/// <remarks>
		/// Returns the character at position <tt>pos</tt> from the
		/// matched text.
		/// It is equivalent to yytext().charAt(pos), but faster
		/// </remarks>
		/// <param name="pos">
		/// the position of the character to fetch.
		/// A value from 0 to yylength()-1.
		/// </param>
		/// <returns>the character at position pos</returns>
		public char Yycharat(int pos)
		{
			return zzBuffer[zzStartRead + pos];
		}

		/// <summary>Returns the length of the matched text region.</summary>
		/// <remarks>Returns the length of the matched text region.</remarks>
		public int Yylength()
		{
			return zzMarkedPos - zzStartRead;
		}

		/// <summary>Reports an error that occured while scanning.</summary>
		/// <remarks>
		/// Reports an error that occured while scanning.
		/// In a wellformed scanner (no or only correct usage of
		/// yypushback(int) and a match-all fallback rule) this method
		/// will only be called with things that "Can't Possibly Happen".
		/// If this method is called, something is seriously wrong
		/// (e.g. a JFlex bug producing a faulty scanner etc.).
		/// Usual syntax/scanner level error handling should be done
		/// in error fallback rules.
		/// </remarks>
		/// <param name="errorCode">the code of the errormessage to display</param>
		private void ZzScanError(int errorCode)
		{
			string message;
			try
			{
				message = ZZ_ERROR_MSG[errorCode];
			}
			catch (IndexOutOfRangeException)
			{
				message = ZZ_ERROR_MSG[ZZ_UNKNOWN_ERROR];
			}
			throw new Error(message);
		}

		/// <summary>Pushes the specified amount of characters back into the input stream.</summary>
		/// <remarks>
		/// Pushes the specified amount of characters back into the input stream.
		/// They will be read again by then next call of the scanning method
		/// </remarks>
		/// <param name="number">
		/// the number of characters to be read again.
		/// This number must not be greater than yylength()!
		/// </param>
		public virtual void Yypushback(int number)
		{
			if (number > Yylength())
			{
				ZzScanError(ZZ_PUSHBACK_2BIG);
			}
			zzMarkedPos -= number;
		}

		/// <summary>
		/// Resumes scanning until the next regular expression is matched,
		/// the end of input is encountered or an I/O-Error occurs.
		/// </summary>
		/// <remarks>
		/// Resumes scanning until the next regular expression is matched,
		/// the end of input is encountered or an I/O-Error occurs.
		/// </remarks>
		/// <returns>the next token</returns>
		/// <exception>
		/// java.io.IOException
		/// if any I/O-Error occurs
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual int GetNextToken()
		{
			int zzInput;
			int zzAction;
			// cached fields:
			int zzCurrentPosL;
			int zzMarkedPosL;
			int zzEndReadL = zzEndRead;
			char[] zzBufferL = zzBuffer;
			char[] zzCMapL = ZZ_CMAP;
			int[] zzTransL = ZZ_TRANS;
			int[] zzRowMapL = ZZ_ROWMAP;
			int[] zzAttrL = ZZ_ATTRIBUTE;
			while (true)
			{
				zzMarkedPosL = zzMarkedPos;
				yychar += zzMarkedPosL - zzStartRead;
				zzAction = -1;
				zzCurrentPosL = zzCurrentPos = zzStartRead = zzMarkedPosL;
				zzState = ZZ_LEXSTATE[zzLexicalState];
				// set up zzAction for empty match case:
				int zzAttributes = zzAttrL[zzState];
				if ((zzAttributes & 1) == 1)
				{
					zzAction = zzState;
				}
				while (true)
				{
					if (zzCurrentPosL < zzEndReadL)
					{
						zzInput = zzBufferL[zzCurrentPosL++];
					}
					else
					{
						if (zzAtEOF)
						{
							zzInput = YYEOF;
							goto zzForAction_break;
						}
						else
						{
							// store back cached positions
							zzCurrentPos = zzCurrentPosL;
							zzMarkedPos = zzMarkedPosL;
							bool eof = ZzRefill();
							// get translated positions and possibly new buffer
							zzCurrentPosL = zzCurrentPos;
							zzMarkedPosL = zzMarkedPos;
							zzBufferL = zzBuffer;
							zzEndReadL = zzEndRead;
							if (eof)
							{
								zzInput = YYEOF;
								goto zzForAction_break;
							}
							else
							{
								zzInput = zzBufferL[zzCurrentPosL++];
							}
						}
					}
					int zzNext = zzTransL[zzRowMapL[zzState] + zzCMapL[zzInput]];
					if (zzNext == -1)
					{
						goto zzForAction_break;
					}
					zzState = zzNext;
					zzAttributes = zzAttrL[zzState];
					if ((zzAttributes & 1) == 1)
					{
						zzAction = zzState;
						zzMarkedPosL = zzCurrentPosL;
						if ((zzAttributes & 8) == 8)
						{
							goto zzForAction_break;
						}
					}
				}
zzForAction_break: ;
				// store back cached position
				zzMarkedPos = zzMarkedPosL;
				switch (zzAction < 0 ? zzAction : ZZ_ACTION[zzAction])
				{
					case 1:
					{
						numWikiTokensSeen = 0;
						positionInc = 1;
						break;
					}

					case 47:
					{
						break;
					}

					case 2:
					{
						positionInc = 1;
						return ALPHANUM;
					}

					case 48:
					{
						break;
					}

					case 3:
					{
						positionInc = 1;
						return CJ;
					}

					case 49:
					{
						break;
					}

					case 4:
					{
						numWikiTokensSeen = 0;
						positionInc = 1;
						currentTokType = EXTERNAL_LINK_URL;
						Yybegin(EXTERNAL_LINK_STATE);
						break;
					}

					case 50:
					{
						break;
					}

					case 5:
					{
						positionInc = 1;
						break;
					}

					case 51:
					{
						break;
					}

					case 6:
					{
						Yybegin(CATEGORY_STATE);
						numWikiTokensSeen++;
						return currentTokType;
					}

					case 52:
					{
						break;
					}

					case 7:
					{
						Yybegin(INTERNAL_LINK_STATE);
						numWikiTokensSeen++;
						return currentTokType;
					}

					case 53:
					{
						break;
					}

					case 8:
					{
						break;
					}

					case 54:
					{
						break;
					}

					case 9:
					{
						if (numLinkToks == 0)
						{
							positionInc = 0;
						}
						else
						{
							positionInc = 1;
						}
						numWikiTokensSeen++;
						currentTokType = EXTERNAL_LINK;
						Yybegin(EXTERNAL_LINK_STATE);
						numLinkToks++;
						return currentTokType;
					}

					case 55:
					{
						break;
					}

					case 10:
					{
						numLinkToks = 0;
						positionInc = 0;
						Yybegin(YYINITIAL);
						break;
					}

					case 56:
					{
						break;
					}

					case 11:
					{
						currentTokType = BOLD;
						Yybegin(THREE_SINGLE_QUOTES_STATE);
						break;
					}

					case 57:
					{
						break;
					}

					case 12:
					{
						currentTokType = ITALICS;
						numWikiTokensSeen++;
						Yybegin(STRING);
						return currentTokType;
					}

					case 58:
					{
						break;
					}

					case 13:
					{
						currentTokType = EXTERNAL_LINK;
						numWikiTokensSeen = 0;
						Yybegin(EXTERNAL_LINK_STATE);
						break;
					}

					case 59:
					{
						break;
					}

					case 14:
					{
						Yybegin(STRING);
						numWikiTokensSeen++;
						return currentTokType;
					}

					case 60:
					{
						break;
					}

					case 15:
					{
						currentTokType = SUB_HEADING;
						numWikiTokensSeen = 0;
						Yybegin(STRING);
						break;
					}

					case 61:
					{
						break;
					}

					case 16:
					{
						currentTokType = HEADING;
						Yybegin(DOUBLE_EQUALS_STATE);
						numWikiTokensSeen++;
						return currentTokType;
					}

					case 62:
					{
						break;
					}

					case 17:
					{
						Yybegin(DOUBLE_BRACE_STATE);
						numWikiTokensSeen = 0;
						return currentTokType;
					}

					case 63:
					{
						break;
					}

					case 18:
					{
						break;
					}

					case 64:
					{
						break;
					}

					case 19:
					{
						Yybegin(STRING);
						numWikiTokensSeen++;
						return currentTokType;
					}

					case 65:
					{
						break;
					}

					case 20:
					{
						numBalanced = 0;
						numWikiTokensSeen = 0;
						currentTokType = EXTERNAL_LINK;
						Yybegin(EXTERNAL_LINK_STATE);
						break;
					}

					case 66:
					{
						break;
					}

					case 21:
					{
						Yybegin(STRING);
						return currentTokType;
					}

					case 67:
					{
						break;
					}

					case 22:
					{
						numWikiTokensSeen = 0;
						positionInc = 1;
						if (numBalanced == 0)
						{
							numBalanced++;
							Yybegin(TWO_SINGLE_QUOTES_STATE);
						}
						else
						{
							numBalanced = 0;
						}
						break;
					}

					case 68:
					{
						break;
					}

					case 23:
					{
						numWikiTokensSeen = 0;
						positionInc = 1;
						Yybegin(DOUBLE_EQUALS_STATE);
						break;
					}

					case 69:
					{
						break;
					}

					case 24:
					{
						numWikiTokensSeen = 0;
						positionInc = 1;
						currentTokType = INTERNAL_LINK;
						Yybegin(INTERNAL_LINK_STATE);
						break;
					}

					case 70:
					{
						break;
					}

					case 25:
					{
						numWikiTokensSeen = 0;
						positionInc = 1;
						currentTokType = CITATION;
						Yybegin(DOUBLE_BRACE_STATE);
						break;
					}

					case 71:
					{
						break;
					}

					case 26:
					{
						Yybegin(YYINITIAL);
						break;
					}

					case 72:
					{
						break;
					}

					case 27:
					{
						numLinkToks = 0;
						Yybegin(YYINITIAL);
						break;
					}

					case 73:
					{
						break;
					}

					case 28:
					{
						currentTokType = INTERNAL_LINK;
						numWikiTokensSeen = 0;
						Yybegin(INTERNAL_LINK_STATE);
						break;
					}

					case 74:
					{
						break;
					}

					case 29:
					{
						currentTokType = INTERNAL_LINK;
						numWikiTokensSeen = 0;
						Yybegin(INTERNAL_LINK_STATE);
						break;
					}

					case 75:
					{
						break;
					}

					case 30:
					{
						Yybegin(YYINITIAL);
						break;
					}

					case 76:
					{
						break;
					}

					case 31:
					{
						numBalanced = 0;
						currentTokType = ALPHANUM;
						Yybegin(YYINITIAL);
						break;
					}

					case 77:
					{
						break;
					}

					case 32:
					{
						numBalanced = 0;
						numWikiTokensSeen = 0;
						currentTokType = INTERNAL_LINK;
						Yybegin(INTERNAL_LINK_STATE);
						break;
					}

					case 78:
					{
						break;
					}

					case 33:
					{
						positionInc = 1;
						return APOSTROPHE;
					}

					case 79:
					{
						break;
					}

					case 34:
					{
						positionInc = 1;
						return HOST;
					}

					case 80:
					{
						break;
					}

					case 35:
					{
						positionInc = 1;
						return NUM;
					}

					case 81:
					{
						break;
					}

					case 36:
					{
						positionInc = 1;
						return COMPANY;
					}

					case 82:
					{
						break;
					}

					case 37:
					{
						currentTokType = BOLD_ITALICS;
						Yybegin(FIVE_SINGLE_QUOTES_STATE);
						break;
					}

					case 83:
					{
						break;
					}

					case 38:
					{
						numBalanced = 0;
						currentTokType = ALPHANUM;
						Yybegin(YYINITIAL);
						break;
					}

					case 84:
					{
						break;
					}

					case 39:
					{
						numBalanced = 0;
						currentTokType = ALPHANUM;
						Yybegin(YYINITIAL);
						break;
					}

					case 85:
					{
						break;
					}

					case 40:
					{
						positionInc = 1;
						return ACRONYM;
					}

					case 86:
					{
						break;
					}

					case 41:
					{
						positionInc = 1;
						return EMAIL;
					}

					case 87:
					{
						break;
					}

					case 42:
					{
						numBalanced = 0;
						currentTokType = ALPHANUM;
						Yybegin(YYINITIAL);
						break;
					}

					case 88:
					{
						break;
					}

					case 43:
					{
						positionInc = 1;
						numWikiTokensSeen++;
						Yybegin(EXTERNAL_LINK_STATE);
						return currentTokType;
					}

					case 89:
					{
						break;
					}

					case 44:
					{
						numWikiTokensSeen = 0;
						positionInc = 1;
						currentTokType = CATEGORY;
						Yybegin(CATEGORY_STATE);
						break;
					}

					case 90:
					{
						break;
					}

					case 45:
					{
						currentTokType = CATEGORY;
						numWikiTokensSeen = 0;
						Yybegin(CATEGORY_STATE);
						break;
					}

					case 91:
					{
						break;
					}

					case 46:
					{
						numBalanced = 0;
						numWikiTokensSeen = 0;
						currentTokType = CATEGORY;
						Yybegin(CATEGORY_STATE);
						break;
					}

					case 92:
					{
						break;
					}

					default:
					{
						if (zzInput == YYEOF && zzStartRead == zzCurrentPos)
						{
							zzAtEOF = true;
							return YYEOF;
						}
						else
						{
							ZzScanError(ZZ_NO_MATCH);
						}
						break;
					}
				}
			}
		}
	}
}
