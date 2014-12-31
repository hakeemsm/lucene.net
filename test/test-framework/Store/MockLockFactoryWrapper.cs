/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.TestFramework.Store;
using Sharpen;

namespace Lucene.Net.TestFramework.Store
{
	/// <summary>
	/// Used by MockDirectoryWrapper to wrap another factory
	/// and track open locks.
	/// </summary>
	/// <remarks>
	/// Used by MockDirectoryWrapper to wrap another factory
	/// and track open locks.
	/// </remarks>
	public class MockLockFactoryWrapper : LockFactory
	{
		internal MockDirectoryWrapper dir;

		internal LockFactory delegate_;

		public MockLockFactoryWrapper(MockDirectoryWrapper dir, LockFactory delegate_)
		{
			this.dir = dir;
			this.delegate_ = delegate_;
		}

		public override void SetLockPrefix(string lockPrefix)
		{
			delegate_.SetLockPrefix(lockPrefix);
		}

		public override string GetLockPrefix()
		{
			return delegate_.GetLockPrefix();
		}

		public override Lock MakeLock(string lockName)
		{
			return new MockLockFactoryWrapper.MockLock(this, delegate_.MakeLock(lockName), lockName
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void ClearLock(string lockName)
		{
			delegate_.ClearLock(lockName);
			dir.openLocks.Remove(lockName);
		}

		public override string ToString()
		{
			return "MockLockFactoryWrapper(" + delegate_.ToString() + ")";
		}

		private class MockLock : Lock
		{
			private Lock delegateLock;

			private string name;

			internal MockLock(MockLockFactoryWrapper _enclosing, Lock delegate_, string name)
			{
				this._enclosing = _enclosing;
				this.delegateLock = delegate_;
				this.name = name;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool Obtain()
			{
				if (this.delegateLock.Obtain())
				{
					this._enclosing.dir.openLocks.Add(this.name);
					return true;
				}
				else
				{
					return false;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				this.delegateLock.Close();
				this._enclosing.dir.openLocks.Remove(this.name);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IsLocked()
			{
				return this.delegateLock.IsLocked();
			}

			private readonly MockLockFactoryWrapper _enclosing;
		}
	}
}
