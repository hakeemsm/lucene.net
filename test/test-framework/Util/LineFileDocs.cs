/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using Lucene.Net.Documents;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// Minimal port of benchmark's LneDocSource +
	/// DocMaker, so tests can enum docs from a line file created
	/// by benchmark's WriteLineDoc task
	/// </summary>
	public class LineFileDocs : IDisposable
	{
		private BufferedReader reader;

		private const int BUFFER_SIZE = 1 << 16;

		private readonly AtomicInteger id = new AtomicInteger();

		private readonly string path;

		private readonly bool useDocValues;

		/// <summary>
		/// If forever is true, we rewind the file at EOF (repeat
		/// the docs over and over)
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public LineFileDocs(Random random, string path, bool useDocValues)
		{
			// 64K
			this.path = path;
			this.useDocValues = useDocValues;
			Open(random);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public LineFileDocs(Random random) : this(random, LuceneTestCase.TEST_LINE_DOCS_FILE
			, true)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public LineFileDocs(Random random, bool useDocValues) : this(random, LuceneTestCase
			.TEST_LINE_DOCS_FILE, useDocValues)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Close()
		{
			lock (this)
			{
				if (reader != null)
				{
					reader.Close();
					reader = null;
				}
			}
		}

		private long RandomSeekPos(Random random, long size)
		{
			if (random == null || size <= 3L)
			{
				return 0L;
			}
			return (random.NextLong() & long.MaxValue) % (size / 3);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Open(Random random)
		{
			lock (this)
			{
				InputStream @is = GetType().GetResourceAsStream(path);
				bool needSkip = true;
				long size = 0L;
				long seekTo = 0L;
				if (@is == null)
				{
					// if its not in classpath, we load it as absolute filesystem path (e.g. Hudson's home dir)
					FilePath file = new FilePath(path);
					size = file.Length();
					if (path.EndsWith(".gz"))
					{
						// if it is a gzip file, we need to use InputStream and slowly skipTo:
						@is = new FileInputStream(file);
					}
					else
					{
						// optimized seek using RandomAccessFile:
						seekTo = RandomSeekPos(random, size);
						FileChannel channel = new RandomAccessFile(path, "r").GetChannel();
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: LineFileDocs: file seek to fp=" + seekTo + " on open"
								);
						}
						channel.Position(seekTo);
						@is = Channels.NewInputStream(channel);
						needSkip = false;
					}
				}
				else
				{
					// if the file comes from Classpath:
					size = @is.Available();
				}
				if (path.EndsWith(".gz"))
				{
					@is = new GZIPInputStream(@is);
					// guestimate:
					size *= 2.8;
				}
				// If we only have an InputStream, we need to seek now,
				// but this seek is a scan, so very inefficient!!!
				if (needSkip)
				{
					seekTo = RandomSeekPos(random, size);
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: LineFileDocs: stream skip to fp=" + seekTo + 
							" on open");
					}
					@is.Skip(seekTo);
				}
				// if we seeked somewhere, read until newline char
				if (seekTo > 0L)
				{
					int b;
					do
					{
						b = @is.Read();
					}
					while (b >= 0 && b != 13 && b != 10);
				}
				CharsetDecoder decoder = StandardCharsets.UTF_8.NewDecoder().OnMalformedInput(CodingErrorAction
					.REPORT).OnUnmappableCharacter(CodingErrorAction.REPORT);
				reader = new BufferedReader(new InputStreamReader(@is, decoder), BUFFER_SIZE);
				if (seekTo > 0L)
				{
					// read one more line, to make sure we are not inside a Windows linebreak (\r\n):
					reader.ReadLine();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Reset(Random random)
		{
			lock (this)
			{
				Close();
				Open(random);
				id.Set(0);
			}
		}

		private const char SEP = '\t';

		private sealed class DocState
		{
			internal readonly Lucene.Net.Documents.Document doc;

			internal readonly Field titleTokenized;

			internal readonly Field title;

			internal readonly Field titleDV;

			internal readonly Field body;

			internal readonly Field id;

			internal readonly Field date;

			public DocState(bool useDocValues)
			{
				doc = new Lucene.Net.Documents.Document();
				title = new StringField("title", string.Empty, Field.Store.NO);
				doc.Add(title);
				FieldType ft = new FieldType(TextField.TYPE_STORED);
				ft.StoreTermVectors = (true);
				ft.StoreTermVectorOffsets = (true);
				ft.StoreTermVectorPositions = (true);
				titleTokenized = new Field("titleTokenized", string.Empty, ft);
				doc.Add(titleTokenized);
				body = new Field("body", string.Empty, ft);
				doc.Add(body);
				id = new StringField("docid", string.Empty, Field.Store.YES);
				doc.Add(id);
				date = new StringField("date", string.Empty, Field.Store.YES);
				doc.Add(date);
				if (useDocValues)
				{
					titleDV = new SortedDocValuesField("titleDV", new BytesRef());
					doc.Add(titleDV);
				}
				else
				{
					titleDV = null;
				}
			}
		}

		private readonly ThreadLocal<LineFileDocs.DocState> threadDocs = new ThreadLocal<
		    LineFileDocs.DocState>();

		/// <summary>Note: Document instance is re-used per-thread</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual Document NextDoc()
		{
			string line;
			lock (this)
			{
				line = reader.ReadLine();
				if (line == null)
				{
					// Always rewind at end:
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: LineFileDocs: now rewind file...");
					}
					Close();
					Open(null);
					line = reader.ReadLine();
				}
			}
			LineFileDocs.DocState docState = threadDocs.Get();
			if (docState == null)
			{
				docState = new LineFileDocs.DocState(useDocValues);
				threadDocs.Set(docState);
			}
			int spot = line.IndexOf(SEP);
			if (spot == -1)
			{
				throw new SystemException("line: [" + line + "] is in an invalid format !");
			}
			int spot2 = line.IndexOf(SEP, 1 + spot);
			if (spot2 == -1)
			{
				throw new SystemException("line: [" + line + "] is in an invalid format !");
			}
			docState.body.SetStringValue(Sharpen.Runtime.Substring(line, 1 + spot2, line.Length
				));
			string title = Sharpen.Runtime.Substring(line, 0, spot);
			docState.title.SetStringValue(title);
			if (docState.titleDV != null)
			{
				docState.titleDV.SetBytesValue(new BytesRef(title));
			}
			docState.titleTokenized.SetStringValue(title);
			docState.date.SetStringValue(Sharpen.Runtime.Substring(line, 1 + spot, spot2));
			docState.id.SetStringValue(Sharpen.Extensions.ToString(id.GetAndIncrement()));
			return docState.doc;
		}
	}
}
