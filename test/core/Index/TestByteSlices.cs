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

using System;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	
	[TestFixture]
	public class TestByteSlices:LuceneTestCase
	{

		
		[Test]
		public virtual void  TestBasic()
		{
			ByteBlockPool pool = new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool
				.BYTE_BLOCK_SIZE, Random().Next(100)));
			int NUM_STREAM = AtLeast(100);
			
			ByteSliceWriter writer = new ByteSliceWriter(pool);
			
			int[] starts = new int[NUM_STREAM];
			int[] uptos = new int[NUM_STREAM];
			int[] counters = new int[NUM_STREAM];
			
			
			ByteSliceReader reader = new ByteSliceReader();
			
			for (int ti = 0; ti < 100; ti++)
			{
				
				for (int stream = 0; stream < NUM_STREAM; stream++)
				{
					starts[stream] = - 1;
					counters[stream] = 0;
				}
				int num = AtLeast(3000);
				
				for (int iter = 0; iter < num; iter++)
				{
					int stream_1;
					if (Random().NextBoolean())
					{
						stream_1 = Random().Next(3);
					}
					else
					{
						stream_1 = Random().Next(NUM_STREAM);
					}
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("write stream=" + stream_1);
					}
					if (starts[stream_1] == -1)
					{
						int spot = pool.NewSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
						starts[stream_1] = uptos[stream_1] = spot + pool.byteOffset;
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  init to " + starts[stream_1]);
						}
					}
					writer.Init(uptos[stream_1]);
					int numValue;
					if (Random().Next(10) == 3)
					{
						numValue = Random().Next(100);
					}
					else
					{
						if (Random().Next(5) == 3)
						{
							numValue = Random().Next(3);
						}
						else
						{
							numValue = Random().Next(20);
						}
					}
					for (int j = 0; j < numValue; j++)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("    write " + (counters[stream_1] + j));
						}
						// write some large (incl. negative) ints:
						writer.WriteVInt(Random().Next());
						writer.WriteVInt(counters[stream_1] + j);
					}
					counters[stream_1] += numValue;
					uptos[stream_1] = writer.Address;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("    addr now " + uptos[stream_1]);
					}
				}
				for (int stream_2 = 0; stream_2 < NUM_STREAM; stream_2++)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  stream=" + stream_2 + " count=" + counters[stream_2
							]);
					}
					if (starts[stream_2] != -1 && starts[stream_2] != uptos[stream_2])
					{
						reader.Init(pool, starts[stream_2], uptos[stream_2]);
						for (int j = 0; j < counters[stream_2]; j++)
						{
							reader.ReadVInt();
							NUnit.Framework.Assert.AreEqual(j, reader.ReadVInt());
						}
					}
				}
				pool.Reset();
			}
		}
	}
}