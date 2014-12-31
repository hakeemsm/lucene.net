/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Security;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// A
	/// <see cref="Sharpen.SecurityManager">Sharpen.SecurityManager</see>
	/// that prevents tests calling
	/// <see cref="System.Environment.Exit(int)">System.Environment.Exit(int)</see>
	/// .
	/// Only the test runner itself is allowed to exit the JVM.
	/// All other security checks are handled by the default security policy.
	/// <p>
	/// Use this with
	/// <code>-Djava.security.manager=Lucene.Net.TestFramework.Util.TestSecurityManager</code>
	/// .
	/// </summary>
	public sealed class TestSecurityManager : SecurityManager
	{
		internal static readonly string TEST_RUNNER_PACKAGE = "com.carrotsearch.ant.tasks.junit4.";

		/// <summary>Creates a new TestSecurityManager.</summary>
		/// <remarks>
		/// Creates a new TestSecurityManager. This ctor is called on JVM startup,
		/// when
		/// <code>-Djava.security.manager=Lucene.Net.TestFramework.Util.TestSecurityManager</code>
		/// is passed to JVM.
		/// </remarks>
		public TestSecurityManager() : base()
		{
		}

		/// <summary>
		/// <inheritDoc></inheritDoc>
		/// <p>This method inspects the stack trace and checks who is calling
		/// <see cref="System.Environment.Exit(int)">System.Environment.Exit(int)</see>
		/// and similar methods
		/// </summary>
		/// <exception cref="System.Security.SecurityException">if the caller of this method is not the test runner itself.
		/// 	</exception>
		public override void CheckExit(int status)
		{
			AccessController.DoPrivileged(new _PrivilegedAction_51(status));
			// this exit point is allowed, we return normally from closure:
			// anything else in stack trace is not allowed, break and throw SecurityException below:
			// should never happen, only if JVM hides stack trace - replace by generic:
			// we passed the stack check, delegate to super, so default policy can still deny permission:
			base.CheckExit(status);
		}

		private sealed class _PrivilegedAction_51 : PrivilegedAction<Void>
		{
			public _PrivilegedAction_51(int status)
			{
				this.status = status;
			}

			public Void Run()
			{
				string systemClassName = typeof(Runtime).FullName;
				string runtimeClassName = typeof(Runtime).FullName;
				string exitMethodHit = null;
				foreach (StackTraceElement se in Thread.CurrentThread().GetStackTrace())
				{
					string className = se.GetClassName();
					string methodName = se.GetMethodName();
					if (("exit".Equals(methodName) || "halt".Equals(methodName)) && (systemClassName.
						Equals(className) || runtimeClassName.Equals(className)))
					{
						exitMethodHit = className + '#' + methodName + '(' + status + ')';
						continue;
					}
					if (exitMethodHit != null)
					{
						if (className.StartsWith(Lucene.Net.TestFramework.Util.TestSecurityManager.TEST_RUNNER_PACKAGE
							))
						{
							return null;
						}
						else
						{
							break;
						}
					}
				}
				if (exitMethodHit == null)
				{
					exitMethodHit = "JVM exit method";
				}
				throw new SecurityException(exitMethodHit + " calls are not allowed because they terminate the test runner's JVM."
					);
			}

			private readonly int status;
		}
	}
}
