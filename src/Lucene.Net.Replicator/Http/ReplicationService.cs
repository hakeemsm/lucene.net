/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Javax.Servlet;
using Javax.Servlet.Http;
using Org.Apache.Http;
using Lucene.Net.Replicator;
using Lucene.Net.Replicator.Http;
using Sharpen;

namespace Lucene.Net.Replicator.Http
{
	/// <summary>A server-side service for handling replication requests.</summary>
	/// <remarks>
	/// A server-side service for handling replication requests. The service assumes
	/// requests are sent in the format
	/// <code>/&lt;context&gt;/&lt;shard&gt;/&lt;action&gt;</code> where
	/// <ul>
	/// <li>
	/// <code>context</code>
	/// is the servlet context, e.g.
	/// <see cref="REPLICATION_CONTEXT">REPLICATION_CONTEXT</see>
	/// <li>
	/// <code>shard</code>
	/// is the ID of the shard, e.g. "s1"
	/// <li>
	/// <code>action</code>
	/// is one of
	/// <see cref="ReplicationAction">ReplicationAction</see>
	/// values
	/// </ul>
	/// For example, to check whether there are revision updates for shard "s1" you
	/// should send the request: <code>http://host:port/replicate/s1/update</code>.
	/// <p>
	/// This service is written like a servlet, and
	/// <see cref="Perform(Javax.Servlet.Http.HttpServletRequest, Javax.Servlet.Http.HttpServletResponse)
	/// 	">Perform(Javax.Servlet.Http.HttpServletRequest, Javax.Servlet.Http.HttpServletResponse)
	/// 	</see>
	/// takes servlet
	/// request and response accordingly, so it is quite easy to embed in your
	/// application's servlet.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class ReplicationService
	{
		/// <summary>
		/// Actions supported by the
		/// <see cref="ReplicationService">ReplicationService</see>
		/// .
		/// </summary>
		public enum ReplicationAction
		{
			OBTAIN,
			RELEASE,
			UPDATE
		}

		/// <summary>The context path for the servlet.</summary>
		/// <remarks>The context path for the servlet.</remarks>
		public static readonly string REPLICATION_CONTEXT = "/replicate";

		/// <summary>Request parameter name for providing the revision version.</summary>
		/// <remarks>Request parameter name for providing the revision version.</remarks>
		public static readonly string REPLICATE_VERSION_PARAM = "version";

		/// <summary>Request parameter name for providing a session ID.</summary>
		/// <remarks>Request parameter name for providing a session ID.</remarks>
		public static readonly string REPLICATE_SESSION_ID_PARAM = "sessionid";

		/// <summary>Request parameter name for providing the file's source.</summary>
		/// <remarks>Request parameter name for providing the file's source.</remarks>
		public static readonly string REPLICATE_SOURCE_PARAM = "source";

		/// <summary>Request parameter name for providing the file's name.</summary>
		/// <remarks>Request parameter name for providing the file's name.</remarks>
		public static readonly string REPLICATE_FILENAME_PARAM = "filename";

		private const int SHARD_IDX = 0;

		private const int ACTION_IDX = 1;

		private readonly IDictionary<string, Lucene.Net.Replicator.Replicator> replicators;

		public ReplicationService(IDictionary<string, Lucene.Net.Replicator.Replicator
			> replicators) : base()
		{
			this.replicators = replicators;
		}

		/// <summary>
		/// Returns the path elements that were given in the servlet request, excluding
		/// the servlet's action context.
		/// </summary>
		/// <remarks>
		/// Returns the path elements that were given in the servlet request, excluding
		/// the servlet's action context.
		/// </remarks>
		private string[] GetPathElements(HttpServletRequest req)
		{
			string path = req.GetServletPath();
			string pathInfo = req.GetPathInfo();
			if (pathInfo != null)
			{
				path += pathInfo;
			}
			int actionLen = REPLICATION_CONTEXT.Length;
			int startIdx = actionLen;
			if (path.Length > actionLen && path[actionLen] == '/')
			{
				++startIdx;
			}
			// split the string on '/' and remove any empty elements. This is better
			// than using String.split() since the latter may return empty elements in
			// the array
			StringTokenizer stok = new StringTokenizer(Sharpen.Runtime.Substring(path, startIdx
				), "/");
			AList<string> elements = new AList<string>();
			while (stok.HasMoreTokens())
			{
				elements.AddItem(stok.NextToken());
			}
			return Sharpen.Collections.ToArray(elements, new string[0]);
		}

		/// <exception cref="Javax.Servlet.ServletException"></exception>
		private static string ExtractRequestParam(HttpServletRequest req, string paramName
			)
		{
			string param = req.GetParameter(paramName);
			if (param == null)
			{
				throw new ServletException("Missing mandatory parameter: " + paramName);
			}
			return param;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void Copy(InputStream @in, OutputStream @out)
		{
			byte[] buf = new byte[16384];
			int numRead;
			while ((numRead = @in.Read(buf)) != -1)
			{
				@out.Write(buf, 0, numRead);
			}
		}

		/// <summary>Executes the replication task.</summary>
		/// <remarks>Executes the replication task.</remarks>
		/// <exception cref="Javax.Servlet.ServletException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Perform(HttpServletRequest req, HttpServletResponse resp)
		{
			string[] pathElements = GetPathElements(req);
			if (pathElements.Length != 2)
			{
				throw new ServletException("invalid path, must contain shard ID and action, e.g. */s1/update"
					);
			}
			ReplicationService.ReplicationAction action;
			try
			{
				action = ReplicationService.ReplicationAction.ValueOf(pathElements[ACTION_IDX].ToUpper
					(Sharpen.Extensions.GetEnglishCulture()));
			}
			catch (ArgumentException)
			{
				throw new ServletException("Unsupported action provided: " + pathElements[ACTION_IDX
					]);
			}
			Lucene.Net.Replicator.Replicator replicator = replicators.Get(pathElements
				[SHARD_IDX]);
			if (replicator == null)
			{
				throw new ServletException("unrecognized shard ID " + pathElements[SHARD_IDX]);
			}
			ServletOutputStream resOut = resp.GetOutputStream();
			try
			{
				switch (action)
				{
					case ReplicationService.ReplicationAction.OBTAIN:
					{
						string sessionID = ExtractRequestParam(req, REPLICATE_SESSION_ID_PARAM);
						string fileName = ExtractRequestParam(req, REPLICATE_FILENAME_PARAM);
						string source = ExtractRequestParam(req, REPLICATE_SOURCE_PARAM);
						InputStream @in = replicator.ObtainFile(sessionID, source, fileName);
						try
						{
							Copy(@in, resOut);
						}
						finally
						{
							@in.Close();
						}
						break;
					}

					case ReplicationService.ReplicationAction.RELEASE:
					{
						replicator.Release(ExtractRequestParam(req, REPLICATE_SESSION_ID_PARAM));
						break;
					}

					case ReplicationService.ReplicationAction.UPDATE:
					{
						string currVersion = req.GetParameter(REPLICATE_VERSION_PARAM);
						SessionToken token = replicator.CheckForUpdate(currVersion);
						if (token == null)
						{
							resOut.Write(0);
						}
						else
						{
							// marker for null token
							resOut.Write(1);
							// marker for null token
							token.Serialize(new DataOutputStream(resOut));
						}
						break;
					}
				}
			}
			catch (Exception e)
			{
				resp.SetStatus(HttpStatus.SC_INTERNAL_SERVER_ERROR);
				// propagate the failure
				try
				{
					ObjectOutputStream oos = new ObjectOutputStream(resOut);
					oos.WriteObject(e);
					oos.Flush();
				}
				catch (Exception e2)
				{
					throw new IOException("Could not serialize", e2);
				}
			}
			finally
			{
				resp.FlushBuffer();
			}
		}
	}
}
