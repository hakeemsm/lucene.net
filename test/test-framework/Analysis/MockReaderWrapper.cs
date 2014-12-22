using System;
using System.IO;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Wraps a Reader, and can throw random or fixed
	/// exceptions, and spoon feed read chars.
	/// </summary>
	/// <remarks>
	/// Wraps a Reader, and can throw random or fixed
	/// exceptions, and spoon feed read chars.
	/// </remarks>
	public class MockReaderWrapper : StreamReader
	{
		private readonly StreamReader inputReader;

		private Random random;

		private int excAtChar = -1;

		private int readSoFar;

		private bool throwExcNext;
	    

	    public MockReaderWrapper(Stream stream):base(stream)
	    {
	        
	    }

	    public Random Random
	    {
	        get { return random; }
	        set { random = value; }
	    }

	    //public MockReaderWrapper(Random random, StreamReader input)
	    //{
	    //    this.inputReader = input;
	    //    this.random = random;
	    //}

	    /// <summary>Throw an exception after reading this many chars.</summary>
	    /// <remarks>Throw an exception after reading this many chars.</remarks>
	    public virtual void ThrowExcAfterChar(int charUpto)
		{
			excAtChar = charUpto;
		}

		// You should only call this on init!:
		 
		//assert readSoFar == 0;
		public virtual void ThrowExcNext()
		{
			throwExcNext = true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			inputReader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(char[] cbuf, int off, int len)
		{
			if (throwExcNext || (excAtChar != -1 && readSoFar >= excAtChar))
			{
				throw new SystemException("fake exception now!");
			}
			int read;
			int realLen;
			if (len == 1)
			{
				realLen = 1;
			}
			else
			{
				// Spoon-feed: intentionally maybe return less than
				// the consumer asked for
				realLen = TestUtil.NextInt(random, 1, len);
			}
			if (excAtChar != -1)
			{
				int left = excAtChar - readSoFar;
				 
				//assert left != 0;
				read = inputReader.Read(cbuf, off, Math.Min(realLen, left));
				 
				//assert read != -1;
				readSoFar += read;
			}
			else
			{
				read = inputReader.Read(cbuf, off, realLen);
			}
			return read;
		}

		public bool MarkSupported()
		{
			return false;
		}

		public bool Ready()
		{
			return false;
		}

		public static bool IsMyEvilException(Exception t)
		{
			return (t is SystemException) && "fake exception now!".Equals(t.Message);
		}
	}
}
