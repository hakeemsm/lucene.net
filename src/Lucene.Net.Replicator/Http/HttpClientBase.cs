/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Org.Apache.Http;
using Org.Apache.Http.Client;
using Org.Apache.Http.Client.Methods;
using Org.Apache.Http.Conn;
using Org.Apache.Http.Impl.Client;
using Org.Apache.Http.Params;
using Org.Apache.Http.Util;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Replicator.Http
{
	/// <summary>Base class for Http clients.</summary>
	/// <remarks>Base class for Http clients.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class HttpClientBase : IDisposable
	{
		/// <summary>Default connection timeout for this client, in milliseconds.</summary>
		/// <remarks>Default connection timeout for this client, in milliseconds.</remarks>
		/// <seealso cref="SetConnectionTimeout(int)">SetConnectionTimeout(int)</seealso>
		public const int DEFAULT_CONNECTION_TIMEOUT = 1000;

		/// <summary>Default socket timeout for this client, in milliseconds.</summary>
		/// <remarks>Default socket timeout for this client, in milliseconds.</remarks>
		/// <seealso cref="SetSoTimeout(int)">SetSoTimeout(int)</seealso>
		public const int DEFAULT_SO_TIMEOUT = 60000;

		/// <summary>The URL stting to execute requests against.</summary>
		/// <remarks>The URL stting to execute requests against.</remarks>
		protected internal readonly string url;

		private volatile bool closed = false;

		private readonly HttpClient httpc;

		/// <param name="conMgr">
		/// connection manager to use for this http client.
		/// <b>NOTE:</b>The provided
		/// <see cref="Org.Apache.Http.Conn.ClientConnectionManager">Org.Apache.Http.Conn.ClientConnectionManager
		/// 	</see>
		/// will not be
		/// <see cref="Org.Apache.Http.Conn.ClientConnectionManager.Shutdown()">Org.Apache.Http.Conn.ClientConnectionManager.Shutdown()
		/// 	</see>
		/// by this class.
		/// </param>
		protected internal HttpClientBase(string host, int port, string path, ClientConnectionManager
			 conMgr)
		{
			// TODO compression?
			url = NormalizedURL(host, port, path);
			httpc = new DefaultHttpClient(conMgr);
			SetConnectionTimeout(DEFAULT_CONNECTION_TIMEOUT);
			SetSoTimeout(DEFAULT_SO_TIMEOUT);
		}

		/// <summary>Set the connection timeout for this client, in milliseconds.</summary>
		/// <remarks>
		/// Set the connection timeout for this client, in milliseconds. This setting
		/// is used to modify
		/// <see cref="Org.Apache.Http.Params.HttpConnectionParams.SetConnectionTimeout(Org.Apache.Http.Params.HttpParams, int)
		/// 	">Org.Apache.Http.Params.HttpConnectionParams.SetConnectionTimeout(Org.Apache.Http.Params.HttpParams, int)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="timeout">timeout to set, in millisecopnds</param>
		public virtual void SetConnectionTimeout(int timeout)
		{
			HttpConnectionParams.SetConnectionTimeout(httpc.GetParams(), timeout);
		}

		/// <summary>Set the socket timeout for this client, in milliseconds.</summary>
		/// <remarks>
		/// Set the socket timeout for this client, in milliseconds. This setting
		/// is used to modify
		/// <see cref="Org.Apache.Http.Params.HttpConnectionParams.SetSoTimeout(Org.Apache.Http.Params.HttpParams, int)
		/// 	">Org.Apache.Http.Params.HttpConnectionParams.SetSoTimeout(Org.Apache.Http.Params.HttpParams, int)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="timeout">timeout to set, in millisecopnds</param>
		public virtual void SetSoTimeout(int timeout)
		{
			HttpConnectionParams.SetSoTimeout(httpc.GetParams(), timeout);
		}

		/// <summary>
		/// Throws
		/// <see cref="Lucene.Net.Store.AlreadyClosedException">Lucene.Net.Store.AlreadyClosedException
		/// 	</see>
		/// if this client is already closed.
		/// </summary>
		/// <exception cref="Lucene.Net.Store.AlreadyClosedException"></exception>
		protected internal void EnsureOpen()
		{
			if (closed)
			{
				throw new AlreadyClosedException("HttpClient already closed");
			}
		}

		/// <summary>Create a URL out of the given parameters, translate an empty/null path to '/'
		/// 	</summary>
		private static string NormalizedURL(string host, int port, string path)
		{
			if (path == null || path.Length == 0)
			{
				path = "/";
			}
			return "http://" + host + ":" + port + path;
		}

		/// <summary>
		/// <b>Internal:</b> response status after invocation, and in case or error attempt to read the
		/// exception sent by the server.
		/// </summary>
		/// <remarks>
		/// <b>Internal:</b> response status after invocation, and in case or error attempt to read the
		/// exception sent by the server.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void VerifyStatus(HttpResponse response)
		{
			StatusLine statusLine = response.GetStatusLine();
			if (statusLine.GetStatusCode() != HttpStatus.SC_OK)
			{
				try
				{
					ThrowKnownError(response, statusLine);
				}
				finally
				{
					EntityUtils.ConsumeQuietly(response.GetEntity());
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void ThrowKnownError(HttpResponse response, StatusLine
			 statusLine)
		{
			ObjectInputStream @in = null;
			try
			{
				@in = new ObjectInputStream(response.GetEntity().GetContent());
			}
			catch (Exception t)
			{
				// the response stream is not an exception - could be an error in servlet.init().
				throw new RuntimeException("Unknown error: " + statusLine, t);
			}
			Exception t_1;
			try
			{
				t_1 = (Exception)@in.ReadObject();
			}
			catch (Exception th)
			{
				throw new RuntimeException("Failed to read exception object: " + statusLine, th);
			}
			finally
			{
				@in.Close();
			}
			IOUtils.ReThrow(t_1);
		}

		/// <summary>
		/// <b>internal:</b> execute a request and return its result
		/// The <code>params</code> argument is treated as: name1,value1,name2,value2,...
		/// </summary>
		/// <remarks>
		/// <b>internal:</b> execute a request and return its result
		/// The <code>params</code> argument is treated as: name1,value1,name2,value2,...
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual HttpResponse ExecutePOST(string request, HttpEntity entity
			, params string[] @params)
		{
			EnsureOpen();
			HttpPost m = new HttpPost(QueryString(request, @params));
			m.SetEntity(entity);
			HttpResponse response = httpc.Execute(m);
			VerifyStatus(response);
			return response;
		}

		/// <summary>
		/// <b>internal:</b> execute a request and return its result
		/// The <code>params</code> argument is treated as: name1,value1,name2,value2,...
		/// </summary>
		/// <remarks>
		/// <b>internal:</b> execute a request and return its result
		/// The <code>params</code> argument is treated as: name1,value1,name2,value2,...
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual HttpResponse ExecuteGET(string request, params string[]
			 @params)
		{
			EnsureOpen();
			HttpGet m = new HttpGet(QueryString(request, @params));
			HttpResponse response = httpc.Execute(m);
			VerifyStatus(response);
			return response;
		}

		/// <exception cref="System.IO.UnsupportedEncodingException"></exception>
		private string QueryString(string request, params string[] @params)
		{
			StringBuilder query = new StringBuilder(url).Append('/').Append(request).Append('?'
				);
			if (@params != null)
			{
				for (int i = 0; i < @params.Length; i += 2)
				{
					query.Append(@params[i]).Append('=').Append(URLEncoder.Encode(@params[i + 1], "UTF8"
						)).Append('&');
				}
			}
			return query.Substring(0, query.Length - 1);
		}

		/// <summary>Internal utility: input stream of the provided response</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream ResponseInputStream(HttpResponse response)
		{
			return ResponseInputStream(response, false);
		}

		// TODO: can we simplify this Consuming !?!?!?
		/// <summary>
		/// Internal utility: input stream of the provided response, which optionally
		/// consumes the response's resources when the input stream is exhausted.
		/// </summary>
		/// <remarks>
		/// Internal utility: input stream of the provided response, which optionally
		/// consumes the response's resources when the input stream is exhausted.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream ResponseInputStream(HttpResponse response, bool consume
			)
		{
			HttpEntity entity = response.GetEntity();
			InputStream @in = entity.GetContent();
			if (!consume)
			{
				return @in;
			}
			return new _InputStream_207(@in, entity);
		}

		private sealed class _InputStream_207 : InputStream
		{
			public _InputStream_207(InputStream @in, HttpEntity entity)
			{
				this.@in = @in;
				this.entity = entity;
				this.consumed = false;
			}

			private bool consumed;

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read()
			{
				int res = @in.Read();
				this.Consume(res);
				return res;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
				this.Consume(-1);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] b)
			{
				int res = @in.Read(b);
				this.Consume(res);
				return res;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] b, int off, int len)
			{
				int res = @in.Read(b, off, len);
				this.Consume(res);
				return res;
			}

			private void Consume(int minusOne)
			{
				if (!this.consumed && minusOne == -1)
				{
					try
					{
						EntityUtils.Consume(entity);
					}
					catch (Exception)
					{
					}
					// ignored on purpose
					this.consumed = true;
				}
			}

			private readonly InputStream @in;

			private readonly HttpEntity entity;
		}

		/// <summary>
		/// Returns true iff this instance was
		/// <see cref="Close()">closed</see>
		/// , otherwise
		/// returns false. Note that if you override
		/// <see cref="Close()">Close()</see>
		/// , you must call
		/// <code>super.close()</code>
		/// , in order for this instance to be properly closed.
		/// </summary>
		protected internal bool IsClosed()
		{
			return closed;
		}

		/// <summary>
		/// Same as
		/// <see cref="DoAction{T}(Org.Apache.Http.HttpResponse, bool, Sharpen.Callable{V})">DoAction&lt;T&gt;(Org.Apache.Http.HttpResponse, bool, Sharpen.Callable&lt;V&gt;)
		/// 	</see>
		/// but always do consume at the end.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual T DoAction<T>(HttpResponse response, Callable<T> call)
		{
			return DoAction(response, true, call);
		}

		/// <summary>
		/// Do a specific action and validate after the action that the status is still OK,
		/// and if not, attempt to extract the actual server side exception.
		/// </summary>
		/// <remarks>
		/// Do a specific action and validate after the action that the status is still OK,
		/// and if not, attempt to extract the actual server side exception. Optionally
		/// release the response at exit, depending on <code>consume</code> parameter.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual T DoAction<T>(HttpResponse response, bool consume, Callable
			<T> call)
		{
			Exception th = null;
			try
			{
				return call.Call();
			}
			catch (Exception t)
			{
				th = t;
			}
			finally
			{
				try
				{
					VerifyStatus(response);
				}
				finally
				{
					if (consume)
					{
						EntityUtils.ConsumeQuietly(response.GetEntity());
					}
				}
			}
			//HM:revisit
			//assert th != null; // extra safety - if we get here, it means the callable failed
			IOUtils.ReThrow(th);
			return null;
		}

		// silly, if we're here, IOUtils.reThrow always throws an exception 
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Close()
		{
			closed = true;
		}
	}
}
