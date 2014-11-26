using System;
using System.Threading;

namespace Lucene.Net.Util
{
	/// <summary>Manages reference counting for a given object.</summary>
	/// <remarks>
	/// Manages reference counting for a given object. Extensions can override
	/// <see cref="RefCount{T}.Release()">RefCount&lt;T&gt;.Release()</see>
	/// to do custom logic when reference counting hits 0.
	/// </remarks>
	public class RefCount<T>
	{
		private int refCount = 1;

		protected internal readonly T genericObject;

		public RefCount(T t)
		{
			this.genericObject = t;
		}

		/// <summary>Called when reference counting hits 0.</summary>
		/// <remarks>
		/// Called when reference counting hits 0. By default this method does nothing,
		/// but extensions can override to e.g. release resources attached to object
		/// that is managed by this class.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void Release()
		{
		}

		/// <summary>Decrements the reference counting of this object.</summary>
		/// <remarks>
		/// Decrements the reference counting of this object. When reference counting
		/// hits 0, calls
		/// <see cref="RefCount{T}.Release()">RefCount&lt;T&gt;.Release()</see>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public void DecRef()
		{
		    int rc = Interlocked.Decrement(ref refCount);
			if (rc == 0)
			{
				bool success = false;
				try
				{
					Release();
					success = true;
				}
				finally
				{
					if (!success)
					{
						// Put reference back on failure
						Interlocked.Increment(ref refCount);
					}
				}
			}
			else
			{
				if (rc < 0)
				{
					throw new InvalidOperationException("too many decRef calls: refCount is " + rc + 
						" after decrement");
				}
			}
		}

		public T Get()
		{
			return genericObject;
		}

		
		public int RefCountValue
		{
		    get { return refCount; }
		}

		/// <summary>Increments the reference count.</summary>
		/// <remarks>
		/// Increments the reference count. Calls to this method must be matched with
		/// calls to
		/// <see cref="RefCount{T}.DecRef()">RefCount&lt;T&gt;.DecRef()</see>
		/// .
		/// </remarks>
		public void IncRef()
		{
			Interlocked.Increment(ref refCount);
		}
	}
}
