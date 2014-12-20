///*
// * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
// * 
// * If this is an open source Java library, include the proper license and copyright attributions here!
// */

//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using Lucene.Net.Codecs;
//using Lucene.Net.Search.Similarities;
//using Lucene.Net.Util;
//using Lucene.Net.TestFramework.Index;
//using Lucene.Net.TestFramework.Search;

//namespace Lucene.Net.TestFramework.Util
//{
//    /// <summary>
//    /// Setup and restore suite-level environment (fine grained junk that
//    /// doesn't fit anywhere else).
//    /// </summary>
//    /// <remarks>
//    /// Setup and restore suite-level environment (fine grained junk that
//    /// doesn't fit anywhere else).
//    /// </remarks>
//    internal sealed class TestRuleSetupAndRestoreClassEnv : AbstractBeforeAfterRule
//    {
//        /// <summary>Restore these system property values.</summary>
//        /// <remarks>Restore these system property values.</remarks>
//        private Dictionary<string, string> restoreProperties = new Dictionary<string, string
//            >();

//        private Codec savedCodec;

//        private CultureInfo savedLocale;

//        private TimeZoneInfo savedTimeZone;

//        private InfoStream savedInfoStream;

//        internal CultureInfo locale;

//        internal TimeZoneInfo timeZone;

//        internal Similarity similarity;

//        internal Codec codec;

//        /// <seealso cref="SuppressCodecs">SuppressCodecs</seealso>
//        internal HashSet<string> avoidCodecs;

//        internal class ThreadNameFixingPrintStreamInfoStream : PrintStreamInfoStream
//        {
//            public ThreadNameFixingPrintStreamInfoStream(TextWriter @out) : base(@out)
//            {
//            }

//            public override void Message(string component, string message)
//            {
//                if ("TP".Equals(component))
//                {
//                    return;
//                }
//                // ignore test points!
//                string name;
//                if (Sharpen.Thread.CurrentThread().GetName().StartsWith("TEST-"))
//                {
//                    // The name of the main thread is way too
//                    // long when looking at IW verbose output...
//                    name = "main";
//                }
//                else
//                {
//                    name = Sharpen.Thread.CurrentThread().GetName();
//                }
//                stream.WriteLine(component + " " + messageID + " [" + new DateTime() + "; " + name
//                     + "]: " + message);
//            }
//        }

//        /// <exception cref="System.Exception"></exception>
//        protected internal override void Before()
//        {
//            // enable this by default, for IDE consistency with ant tests (as its the default from ant)
//            // TODO: really should be in solr base classes, but some extend LTC directly.
//            // we do this in beforeClass, because some tests currently disable it
//            restoreProperties.Put("solr.directoryFactory", Runtime.GetProperty("solr.directoryFactory"
//                ));
//            if (Runtime.GetProperty("solr.directoryFactory") == null)
//            {
//                Runtime.SetProperty("solr.directoryFactory", "org.apache.solr.core.MockDirectoryFactory"
//                    );
//            }
//            // Restore more Solr properties. 
//            restoreProperties.Put("solr.solr.home", Runtime.GetProperty("solr.solr.home"));
//            restoreProperties.Put("solr.data.dir", Runtime.GetProperty("solr.data.dir"));
//            // if verbose: print some debugging stuff about which codecs are loaded.
//            if (LuceneTestCase.VERBOSE)
//            {
//                ICollection<string> codecs = Codec.AvailableCodecs();
//                foreach (string codec in codecs)
//                {
//                    System.Console.Out.WriteLine("Loaded codec: '" + codec + "': " + Codec.ForName(codec
//                        ).GetType().FullName);
//                }
//                ICollection<string> postingsFormats = PostingsFormat.AvailablePostingsFormats();
//                foreach (string postingsFormat in postingsFormats)
//                {
//                    System.Console.Out.WriteLine("Loaded postingsFormat: '" + postingsFormat + "': " 
//                        + PostingsFormat.ForName(postingsFormat).GetType().FullName);
//                }
//            }
//            savedInfoStream = InfoStream.GetDefault();
//            Random random = RandomizedContext.Current().GetRandom();
//            bool v = random.NextBoolean();
//            if (LuceneTestCase.INFOSTREAM)
//            {
//                InfoStream.SetDefault(new TestRuleSetupAndRestoreClassEnv.ThreadNameFixingPrintStreamInfoStream
//                    (System.Console.Out));
//            }
//            else
//            {
//                if (v)
//                {
//                    InfoStream.SetDefault(new NullInfoStream());
//                }
//            }
//            Type targetClass = RandomizedContext.Current().GetTargetClass();
//            avoidCodecs = new HashSet<string>();
//            if (targetClass.IsAnnotationPresent(typeof(LuceneTestCase.SuppressCodecs)))
//            {
//                LuceneTestCase.SuppressCodecs a = targetClass.GetAnnotation<LuceneTestCase.SuppressCodecs
//                    >();
//            }
//            //HM:revisit below line throws an exception prolly bcoz of 150 above
//            //avoidCodecs.addAll(Arrays.asList(a.value()));
//            // set back to default
//            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
//            savedCodec = Codec.GetDefault();
//            int randomVal = random.Next(10);
//            if ("Lucene3x".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                .TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".
//                Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 3 && !ShouldAvoidCodec
//                ("Lucene3x")))
//            {
//                // preflex-only setup
//                codec = Codec.ForName("Lucene3x");
//                //HM:revisit 
//                //assert (codec instanceof PreFlexRWCodec) : "fix your classpath to have tests-framework.jar before lucene-core.jar";
//                LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
//            }
//            else
//            {
//                if ("Lucene40".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                    .TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && randomVal
//                     == 0 && !ShouldAvoidCodec("Lucene40")))
//                {
//                    // 4.0 setup
//                    codec = Codec.ForName("Lucene40");
//                    LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
//                }
//                else
//                {
//                    //HM:revisit 
//                    //assert codec instanceof Lucene40RWCodec : "fix your classpath to have tests-framework.jar before lucene-core.jar";
//                    //HM:revisit 
//                    //assert (PostingsFormat.forName("Lucene40") instanceof Lucene40RWPostingsFormat) : "fix your classpath to have tests-framework.jar before lucene-core.jar";
//                    if ("Lucene41".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                        .TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".
//                        Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 1 && !ShouldAvoidCodec
//                        ("Lucene41")))
//                    {
//                        codec = Codec.ForName("Lucene41");
//                        LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
//                    }
//                    else
//                    {
//                        //HM:revisit 
//                        //assert codec instanceof Lucene41RWCodec : "fix your classpath to have tests-framework.jar before lucene-core.jar";
//                        if ("Lucene42".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                            .TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".
//                            Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 2 && !ShouldAvoidCodec
//                            ("Lucene42")))
//                        {
//                            codec = Codec.ForName("Lucene42");
//                            LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
//                        }
//                        else
//                        {
//                            //HM:revisit 
//                            //assert codec instanceof Lucene42RWCodec : "fix your classpath to have tests-framework.jar before lucene-core.jar";
//                            if ("Lucene45".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                                .TEST_CODEC) && "random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) && "random".
//                                Equals(LuceneTestCase.TEST_DOCVALUESFORMAT) && randomVal == 5 && !ShouldAvoidCodec
//                                ("Lucene45")))
//                            {
//                                codec = Codec.ForName("Lucene45");
//                                LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
//                            }
//                            else
//                            {
//                                //HM:revisit 
//                                //assert codec instanceof Lucene45RWCodec : "fix your classpath to have tests-framework.jar before lucene-core.jar";
//                                if (("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT) == false) || ("random".Equals
//                                    (LuceneTestCase.TEST_DOCVALUESFORMAT) == false))
//                                {
//                                    // the user wired postings or DV: this is messy
//                                    // refactor into RandomCodec....
//                                    PostingsFormat format;
//                                    if ("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
//                                    {
//                                        format = PostingsFormat.ForName("Lucene41");
//                                    }
//                                    else
//                                    {
//                                        if ("MockRandom".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
//                                        {
//                                            format = new MockRandomPostingsFormat(new Random(random.NextLong()));
//                                        }
//                                        else
//                                        {
//                                            format = PostingsFormat.ForName(LuceneTestCase.TEST_POSTINGSFORMAT);
//                                        }
//                                    }
//                                    DocValuesFormat dvFormat;
//                                    if ("random".Equals(LuceneTestCase.TEST_DOCVALUESFORMAT))
//                                    {
//                                        dvFormat = DocValuesFormat.ForName("Lucene45");
//                                    }
//                                    else
//                                    {
//                                        dvFormat = DocValuesFormat.ForName(LuceneTestCase.TEST_DOCVALUESFORMAT);
//                                    }
//                                    codec = new _Lucene46Codec_235(format, dvFormat);
//                                }
//                                else
//                                {
//                                    if ("SimpleText".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                                        .TEST_CODEC) && randomVal == 9 && LuceneTestCase.Rarely(random) && !ShouldAvoidCodec
//                                        ("SimpleText")))
//                                    {
//                                        codec = new SimpleTextCodec();
//                                    }
//                                    else
//                                    {
//                                        if ("CheapBastard".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                                            .TEST_CODEC) && randomVal == 8 && !ShouldAvoidCodec("CheapBastard") && !ShouldAvoidCodec
//                                            ("Lucene41")))
//                                        {
//                                            // we also avoid this codec if Lucene41 is avoided, since thats the postings format it uses.
//                                            codec = new CheapBastardCodec();
//                                        }
//                                        else
//                                        {
//                                            if ("Asserting".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                                                .TEST_CODEC) && randomVal == 6 && !ShouldAvoidCodec("Asserting")))
//                                            {
//                                                codec = new AssertingCodec();
//                                            }
//                                            else
//                                            {
//                                                if ("Compressing".Equals(LuceneTestCase.TEST_CODEC) || ("random".Equals(LuceneTestCase
//                                                    .TEST_CODEC) && randomVal == 5 && !ShouldAvoidCodec("Compressing")))
//                                                {
//                                                    codec = CompressingCodec.RandomInstance(random);
//                                                }
//                                                else
//                                                {
//                                                    if (!"random".Equals(LuceneTestCase.TEST_CODEC))
//                                                    {
//                                                        codec = Codec.ForName(LuceneTestCase.TEST_CODEC);
//                                                    }
//                                                    else
//                                                    {
//                                                        if ("random".Equals(LuceneTestCase.TEST_POSTINGSFORMAT))
//                                                        {
//                                                            codec = new RandomCodec(random, avoidCodecs);
//                                                        }
//                                                    }
//                                                }
//                                            }
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            //HM:revisit 
//            //assert false;
//            Codec.SetDefault(codec);
//            // Initialize locale/ timezone.
//            string testLocale = Runtime.GetProperty("tests.locale", "random");
//            string testTimeZone = Runtime.GetProperty("tests.timezone", "random");
//            // Always pick a random one for consistency (whether tests.locale was specified or not).
//            savedLocale = CultureInfo.CurrentCulture;
//            CultureInfo randomLocale = LuceneTestCase.RandomLocale(random);
//            locale = testLocale.Equals("random") ? randomLocale : LuceneTestCase.LocaleForName
//                (testLocale);
//            System.Threading.Thread.CurrentThread.CurrentCulture = locale;
//            // TimeZone.getDefault will set user.timezone to the default timezone of the user's locale.
//            // So store the original property value and restore it at end.
//            restoreProperties.Put("user.timezone", Runtime.GetProperty("user.timezone"));
//            savedTimeZone = System.TimeZoneInfo.Local;
//            TimeZoneInfo randomTimeZone = LuceneTestCase.RandomTimeZone(LuceneTestCase.Random());
//            timeZone = testTimeZone.Equals("random") ? randomTimeZone : Sharpen.Extensions.GetTimeZone(testTimeZone);
//            TimeZoneInfo.SetDefault(timeZone);
//            similarity = LuceneTestCase.Random().NextBoolean() ? new DefaultSimilarity() : new RandomSimilarityProvider(LuceneTestCase.Random());
//            // Check codec restrictions once at class level.
//            try
//            {
//                CheckCodecRestrictions(codec);
//            }
//            catch (AssumptionViolatedException e)
//            {
//                System.Console.Error.WriteLine("NOTE: " + e.Message + " Suppressed codecs: " + Arrays
//                    .ToString(Sharpen.Collections.ToArray(avoidCodecs)));
//                throw;
//            }
//        }

//        private sealed class _Lucene46Codec_235 : Lucene46Codec
//        {
//            public _Lucene46Codec_235(PostingsFormat format, DocValuesFormat dvFormat)
//            {
//                this.format = format;
//                this.dvFormat = dvFormat;
//            }

//            public override PostingsFormat GetPostingsFormatForField(string field)
//            {
//                return format;
//            }

//            public override DocValuesFormat GetDocValuesFormatForField(string field)
//            {
//                return dvFormat;
//            }

//            public override string ToString()
//            {
//                return base.ToString() + ": " + format.ToString() + ", " + dvFormat.ToString();
//            }

//            private readonly PostingsFormat format;

//            private readonly DocValuesFormat dvFormat;
//        }

//        /// <summary>Check codec restrictions.</summary>
//        /// <remarks>Check codec restrictions.</remarks>
//        /// <exception cref="NUnit.Framework.Internal.AssumptionViolatedException">if the class does not work with a given codec.
//        /// 	</exception>
//        private void CheckCodecRestrictions(Codec codec)
//        {
//            LuceneTestCase.AssumeFalse("Class not allowed to use codec: " + codec.GetName() +
//                 ".", ShouldAvoidCodec(codec.GetName()));
//            if (codec is RandomCodec && !avoidCodecs.IsEmpty())
//            {
//                foreach (string name in ((RandomCodec)codec).formatNames)
//                {
//                    LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + name + 
//                        ".", ShouldAvoidCodec(name));
//                }
//            }
//            PostingsFormat pf = codec.PostingsFormat();
//            LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + pf.GetName
//                () + ".", ShouldAvoidCodec(pf.GetName()));
//            LuceneTestCase.AssumeFalse("Class not allowed to use postings format: " + LuceneTestCase
//                .TEST_POSTINGSFORMAT + ".", ShouldAvoidCodec(LuceneTestCase.TEST_POSTINGSFORMAT)
//                );
//        }

//        /// <summary>After suite cleanup (always invoked).</summary>
//        /// <remarks>After suite cleanup (always invoked).</remarks>
//        /// <exception cref="System.Exception"></exception>
//        protected internal override void After()
//        {
//            foreach (KeyValuePair<string, string> e in restoreProperties.EntrySet())
//            {
//                if (e.Value == null)
//                {
//                    Runtime.ClearProperty(e.Key);
//                }
//                else
//                {
//                    Runtime.SetProperty(e.Key, e.Value);
//                }
//            }
//            restoreProperties.Clear();
//            Codec.SetDefault(savedCodec);
//            InfoStream.SetDefault(savedInfoStream);
//            if (savedLocale != null)
//            {
//                System.Threading.Thread.CurrentThread.CurrentCulture = savedLocale;
//            }
//            if (savedTimeZone != null)
//            {
//                TimeZoneInfo.SetDefault(savedTimeZone);
//            }
//        }

//        /// <summary>Should a given codec be avoided for the currently executing suite?</summary>
//        private bool ShouldAvoidCodec(string codec)
//        {
//            return !avoidCodecs.IsEmpty() && avoidCodecs.Contains(codec);
//        }
//    }
//}
