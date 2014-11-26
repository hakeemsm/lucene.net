/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Org.Apache.Http;
using Org.Apache.Http.Conn;
using Lucene.Net.Replicator;
using Lucene.Net.Replicator.Http;
using Sharpen;

namespace Lucene.Net.Replicator.Http
{
	/// <summary>
	/// An HTTP implementation of
	/// <see cref="Lucene.Net.Replicator.Replicator">Lucene.Net.Replicator.Replicator
	/// 	</see>
	/// . Assumes the API supported by
	/// <see cref="ReplicationService">ReplicationService</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class HttpReplicator : HttpClientBase, Lucene.Net.Replicator.Replicator
	{
		/// <summary>Construct with specified connection manager.</summary>
		/// <remarks>Construct with specified connection manager.</remarks>
		protected internal HttpReplicator(string host, int port, string path, ClientConnectionManager
			 conMgr) : base(host, port, path, conMgr)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual SessionToken CheckForUpdate(string currVersion)
		{
			string[] @params = null;
			if (currVersion != null)
			{
				@params = new string[] { ReplicationService.REPLICATE_VERSION_PARAM, currVersion };
			}
			HttpResponse response = ExecuteGET(ReplicationService.ReplicationAction.UPDATE.ToString
				(), @params);
			return DoAction(response, new _Callable_52(this, response));
		}

		private sealed class _Callable_52 : Callable<SessionToken>
		{
			public _Callable_52(HttpReplicator _enclosing, HttpResponse response)
			{
				this._enclosing = _enclosing;
				this.response = response;
			}

			/// <exception cref="System.Exception"></exception>
			public SessionToken Call()
			{
				DataInputStream dis = new DataInputStream(this._enclosing.ResponseInputStream(response
					));
				try
				{
					if (dis.ReadByte() == 0)
					{
						return null;
					}
					else
					{
						return new SessionToken(dis);
					}
				}
				finally
				{
					dis.Close();
				}
			}

			private readonly HttpReplicator _enclosing;

			private readonly HttpResponse response;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream ObtainFile(string sessionID, string source, string fileName
			)
		{
			string[] @params = new string[] { ReplicationService.REPLICATE_SESSION_ID_PARAM, 
				sessionID, ReplicationService.REPLICATE_SOURCE_PARAM, source, ReplicationService
				.REPLICATE_FILENAME_PARAM, fileName };
			HttpResponse response = ExecuteGET(ReplicationService.ReplicationAction.OBTAIN.ToString
				(), @params);
			return DoAction(response, false, new _Callable_77(this, response));
		}

		private sealed class _Callable_77 : Callable<InputStream>
		{
			public _Callable_77(HttpReplicator _enclosing, HttpResponse response)
			{
				this._enclosing = _enclosing;
				this.response = response;
			}

			/// <exception cref="System.Exception"></exception>
			public InputStream Call()
			{
				return this._enclosing.ResponseInputStream(response, true);
			}

			private readonly HttpReplicator _enclosing;

			private readonly HttpResponse response;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Publish(Revision revision)
		{
			throw new NotSupportedException("this replicator implementation does not support remote publishing of revisions"
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Release(string sessionID)
		{
			string[] @params = new string[] { ReplicationService.REPLICATE_SESSION_ID_PARAM, 
				sessionID };
			HttpResponse response = ExecuteGET(ReplicationService.ReplicationAction.RELEASE.ToString
				(), @params);
			DoAction(response, new _Callable_97());
		}

		private sealed class _Callable_97 : Callable<object>
		{
			public _Callable_97()
			{
			}

			/// <exception cref="System.Exception"></exception>
			public object Call()
			{
				return null;
			}
		}
		// do not remove this call: as it is still validating for us!
	}
}
