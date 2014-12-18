/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{

    /**
     * Calls check index on close.
     */
    // do NOT make any methods in this class synchronized, volatile
    // do NOT import anything from the concurrency package.
    // no randoms, no nothing.
	public class BaseDirectoryWrapper : FilterDirectory
    {
		private bool checkIndexOnClose = true;

		private bool crossCheckTermVectorsOnClose = true;

		protected internal volatile bool isOpen = true;

		protected BaseDirectoryWrapper(Directory delegate_) : base(delegate_)
		{
		}

		// do NOT make any methods in this class synchronized, volatile
		// do NOT import anything from the concurrency package.
		// no randoms, no nothing.
		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			isOpen = false;
			if (checkIndexOnClose && DirectoryReader.IndexExists(this))
			{
				TestUtil.CheckIndex(this, crossCheckTermVectorsOnClose);
			}
			base.Close();
		}

		public virtual bool IsOpen()
		{
			return isOpen;
		}

		/// <summary>
		/// Set whether or not checkindex should be run
		/// on close
		/// </summary>
		public virtual void SetCheckIndexOnClose(bool value)
		{
			this.checkIndexOnClose = value;
		}

		public virtual bool GetCheckIndexOnClose()
		{
			return checkIndexOnClose;
		}

		public virtual void SetCrossCheckTermVectorsOnClose(bool value)
		{
			this.crossCheckTermVectorsOnClose = value;
		}

		public virtual bool GetCrossCheckTermVectorsOnClose()
		{
			return crossCheckTermVectorsOnClose;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Copy(Directory to, string src, string dest, IOContext context
			)
		{
			@in.Copy(to, src, dest, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Directory.IndexInputSlicer CreateSlicer(string name, IOContext context
			)
		{
			return @in.CreateSlicer(name, context);
		}
    }
}
