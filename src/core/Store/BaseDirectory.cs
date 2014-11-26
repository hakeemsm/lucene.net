namespace Lucene.Net.Store
{
	/// <summary>
	/// Base implementation for a concrete
	/// <see cref="Directory">Directory</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class BaseDirectory : Directory
	{
		protected internal volatile bool isOpen = true;

		/// <summary>
		/// Holds the LockFactory instance (implements locking for
		/// this Directory instance).
		/// </summary>
		/// <remarks>
		/// Holds the LockFactory instance (implements locking for
		/// this Directory instance).
		/// </remarks>
		protected internal LockFactory lockFactory;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public BaseDirectory() : base()
		{
		}

		public override Lock MakeLock(string name)
		{
			return lockFactory.MakeLock(name);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void ClearLock(string name)
		{
			if (lockFactory != null)
			{
				lockFactory.ClearLock(name);
			}
		}

		public override LockFactory LockFactory
		{
		    get { return this.lockFactory; }
		    set
		    {
                this.lockFactory = value;
                lockFactory.LockPrefix = this.LockId;
		    }
		}

		
	}
}
