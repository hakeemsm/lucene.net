/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
usingLucene.Net.TestFramework.Search.Similarities;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search.Similarities;
using Sharpen;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>
	/// Similarity implementation that randomizes Similarity implementations
	/// per-field.
	/// </summary>
	/// <remarks>
	/// Similarity implementation that randomizes Similarity implementations
	/// per-field.
	/// <p>
	/// The choices are 'sticky', so the selected algorithm is always used
	/// for the same field.
	/// </remarks>
	public class RandomSimilarityProvider : PerFieldSimilarityWrapper
	{
		internal readonly DefaultSimilarity defaultSim = new DefaultSimilarity();

		internal readonly IList<Similarity> knownSims;

		internal IDictionary<string, Similarity> previousMappings = new Dictionary<string
			, Similarity>();

		internal readonly int perFieldSeed;

		internal readonly int coordType;

		internal readonly bool shouldQueryNorm;

		public RandomSimilarityProvider(Random random)
		{
			// 0 = no coord, 1 = coord, 2 = crazy coord
			perFieldSeed = random.Next();
			coordType = random.Next(3);
			shouldQueryNorm = random.NextBoolean();
			knownSims = new AList<Similarity>(allSims);
			Sharpen.Collections.Shuffle(knownSims, random);
		}

		public override float Coord(int overlap, int maxOverlap)
		{
			if (coordType == 0)
			{
				return 1.0f;
			}
			else
			{
				if (coordType == 1)
				{
					return defaultSim.Coord(overlap, maxOverlap);
				}
				else
				{
					return overlap / ((float)maxOverlap + 1);
				}
			}
		}

		public override float QueryNorm(float sumOfSquaredWeights)
		{
			if (shouldQueryNorm)
			{
				return defaultSim.QueryNorm(sumOfSquaredWeights);
			}
			else
			{
				return 1.0f;
			}
		}

		public override Similarity Get(string field)
		{
			lock (this)
			{
				//HM:revisit 
				//assert field != null;
				Similarity sim = previousMappings.Get(field);
				if (sim == null)
				{
					sim = knownSims[Math.Max(0, Math.Abs(perFieldSeed ^ field.GetHashCode())) % knownSims
						.Count];
					previousMappings.Put(field, sim);
				}
				return sim;
			}
		}

		/// <summary>The DFR basic models to test.</summary>
		/// <remarks>The DFR basic models to test.</remarks>
		internal static BasicModel[] BASIC_MODELS = new BasicModel[] { new BasicModelG(), 
			new BasicModelIF(), new BasicModelIn(), new BasicModelIne() };

		/// <summary>The DFR aftereffects to test.</summary>
		/// <remarks>The DFR aftereffects to test.</remarks>
		internal static AfterEffect[] AFTER_EFFECTS = new AfterEffect[] { new AfterEffectB
			(), new AfterEffectL(), new AfterEffect.NoAfterEffect() };

		/// <summary>The DFR normalizations to test.</summary>
		/// <remarks>The DFR normalizations to test.</remarks>
		internal static Normalization[] NORMALIZATIONS = new Normalization[] { new NormalizationH1
			(), new NormalizationH2(), new NormalizationH3(), new NormalizationZ() };

		/// <summary>The distributions for IB.</summary>
		/// <remarks>The distributions for IB.</remarks>
		internal static Distribution[] DISTRIBUTIONS = new Distribution[] { new DistributionLL
			(), new DistributionSPL() };

		/// <summary>Lambdas for IB.</summary>
		/// <remarks>Lambdas for IB.</remarks>
		internal static Lambda[] LAMBDAS = new Lambda[] { new LambdaDF(), new LambdaTTF()
			 };

		internal static IList<Similarity> allSims;

		static RandomSimilarityProvider()
		{
			// all the similarities that we rotate through
			// TODO: if we enable NoNormalization, we have to deal with
			// a couple tests (e.g. TestDocBoost, TestSort) that expect length normalization
			// new Normalization.NoNormalization()
			allSims = new AList<Similarity>();
			allSims.AddItem(new DefaultSimilarity());
			allSims.AddItem(new BM25Similarity());
			foreach (BasicModel basicModel in BASIC_MODELS)
			{
				foreach (AfterEffect afterEffect in AFTER_EFFECTS)
				{
					foreach (Normalization normalization in NORMALIZATIONS)
					{
						allSims.AddItem(new DFRSimilarity(basicModel, afterEffect, normalization));
					}
				}
			}
			foreach (Distribution distribution in DISTRIBUTIONS)
			{
				foreach (Lambda lambda in LAMBDAS)
				{
					foreach (Normalization normalization in NORMALIZATIONS)
					{
						allSims.AddItem(new IBSimilarity(distribution, lambda, normalization));
					}
				}
			}
			allSims.AddItem(new LMJelinekMercerSimilarity(0.1f));
			allSims.AddItem(new LMJelinekMercerSimilarity(0.7f));
		}

		public override string ToString()
		{
			lock (this)
			{
				string coordMethod;
				if (coordType == 0)
				{
					coordMethod = "no";
				}
				else
				{
					if (coordType == 1)
					{
						coordMethod = "yes";
					}
					else
					{
						coordMethod = "crazy";
					}
				}
				return "RandomSimilarityProvider(queryNorm=" + shouldQueryNorm + ",coord=" + coordMethod
					 + "): " + previousMappings.ToString();
			}
		}
	}
}
