using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.PerField
{
    public abstract class PerFieldDocValuesFormat : DocValuesFormat
    {
        public const String PER_FIELD_NAME = "PerFieldDV40";

        public static readonly String PER_FIELD_FORMAT_KEY;

        public static readonly String PER_FIELD_SUFFIX_KEY;

        static PerFieldDocValuesFormat()
        {
            // .NET Port: can't we just make these const with "PerFieldDocValuesFormat.format" etc?
            PER_FIELD_FORMAT_KEY = typeof (PerFieldDocValuesFormat).Name + ".format";
            PER_FIELD_SUFFIX_KEY = typeof (PerFieldDocValuesFormat).Name + ".suffix";
        }

        public PerFieldDocValuesFormat()
            : base(PER_FIELD_NAME)
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new FieldsWriter(this, state);
        }

        internal class ConsumerAndSuffix : IDisposable
        {
            internal DocValuesConsumer consumer;
            internal int suffix;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    consumer.Dispose();
                }

                consumer = null;
            }
        }

        private class FieldsWriter : DocValuesConsumer
        {
            private readonly IDictionary<DocValuesFormat, ConsumerAndSuffix> formats =
                new HashMap<DocValuesFormat, ConsumerAndSuffix>();

            private readonly IDictionary<string, int> suffixes = new HashMap<string, int>();

            private readonly SegmentWriteState segmentWriteState;

            private readonly PerFieldDocValuesFormat parent;

            public FieldsWriter(PerFieldDocValuesFormat parent, SegmentWriteState state)
            {
                this.parent = parent;
                segmentWriteState = state;
            }

            public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
            {
                GetInstance(field).AddNumericField(field, values);
            }

            public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
            {
                GetInstance(field).AddBinaryField(field, values);
            }

            public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd)
            {
                GetInstance(field).AddSortedField(field, values, docToOrd);
            }

            public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values,
                IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
            {
                GetInstance(field).AddSortedSetField(field, values, docToOrdCount, ords);
            }

            private DocValuesConsumer GetInstance(FieldInfo field)
            {
                DocValuesFormat format = null;
                if (field.DocValuesGen != -1)
                {
                    string formatName = field.GetAttribute(PER_FIELD_FORMAT_KEY);
                    // this means the field never existed in that segment, yet is applied updates
                    if (formatName != null)
                    {
                        format = DocValuesFormat.ForName(formatName);
                    }
                }
                if (format == null)
                {
                    format = this.parent.GetDocValuesFormatForField(field.name);
                }
                if (format == null)
                {
                    throw new InvalidOperationException("invalid null DocValuesFormat for field=\"" + field.name + "\"");
                }
                String formatName2 = format.Name;

                String previousValue = field.PutAttribute(PER_FIELD_FORMAT_KEY, formatName2);
                //assert previousValue == null: "formatName=" + formatName + " prevValue=" + previousValue;

                int? suffix = null;

                ConsumerAndSuffix consumer = formats[format];
                if (consumer == null)
                {
                    // First time we are seeing this format; create a new instance
                    if (field.DocValuesGen != -1)
                    {
                        string suffixAtt = field.GetAttribute(PER_FIELD_SUFFIX_KEY);
                        // even when dvGen is != -1, it can still be a new field, that never
                        // existed in the segment, and therefore doesn't have the recorded
                        // attributes yet.
                        if (suffixAtt != null)
                        {
                            suffix = int.Parse(suffixAtt);
                        }
                    }
                    if (suffix == null)
                    {
                        // bump the suffix
                        suffix = suffixes[formatName2];
                        if (suffix == null)
                        {
                            suffix = 0;
                        }
                        else
                        {
                            suffix = suffix + 1;
                        }
                    }
                    suffixes[formatName2] = suffix.Value;

                    string segmentSuffix = GetFullSegmentSuffix(this.segmentWriteState.segmentSuffix,
                        GetSuffix(formatName2, suffix.ToString()));
                    consumer = new ConsumerAndSuffix
                    {
                        consumer = format.FieldsConsumer(new SegmentWriteState(segmentWriteState, segmentSuffix)),
                        suffix = suffix.Value
                    };
                    formats[format] = consumer;
                }
                else
                {
                    // we've already seen this format, so just grab its suffix
                    //assert suffixes.containsKey(formatName);
                    suffix = consumer.suffix;
                }

                previousValue = field.PutAttribute(PER_FIELD_SUFFIX_KEY, suffix.ToString());
                //assert previousValue == null;

                // TODO: we should only provide the "slice" of FIS
                // that this PF actually sees ...
                return consumer.consumer;
            }

            protected override void Dispose(bool disposing)
            {
                IOUtils.Close(formats.Values.ToArray());
            }
        }

        internal static String GetSuffix(String formatName, String suffix)
        {
            return formatName + "_" + suffix;
        }

        internal static string GetFullSegmentSuffix(string outerSegmentSuffix, string segmentSuffix)
        {
            if (outerSegmentSuffix.Length == 0)
            {
                return segmentSuffix;
            }
            else
            {
                // TODO: support embedding; I think it should work but
                // we need a test confirm to confirm
                return outerSegmentSuffix + "_" + segmentSuffix;
                //where if fieldName defined in the exception message?
                //throw new InvalidOperationException("cannot embed PerFieldPostingsFormat inside itself (field \"" + fieldName + "\" returned PerFieldPostingsFormat)");
            }
        }

        private class FieldsReader : DocValuesProducer
        {
            private readonly IDictionary<String, DocValuesProducer> fields = new HashMap<String, DocValuesProducer>();
            //.NET Port: what to do about TreeMap?

            private readonly IDictionary<String, DocValuesProducer> formats = new HashMap<String, DocValuesProducer>();

            public FieldsReader(SegmentReadState readState)
            {
                // Read _X.per and init each format:
                bool success = false;
                try
                {
                    // Read field name -> format name
                    foreach (FieldInfo fi in readState.fieldInfos)
                    {
                        if (fi.HasDocValues)
                        {
                            String fieldName = fi.name;
                            String formatName = fi.GetAttribute(PER_FIELD_FORMAT_KEY);
                            if (formatName != null)
                            {
                                // null formatName means the field is in fieldInfos, but has no docvalues!
                                String suffix = fi.GetAttribute(PER_FIELD_SUFFIX_KEY);
                                //assert suffix != null;
                                DocValuesFormat format = DocValuesFormat.ForName(formatName);
                                String segmentSuffix = GetSuffix(formatName, suffix);
                                if (!formats.ContainsKey(segmentSuffix))
                                {
                                    formats[segmentSuffix] =
                                        format.FieldsProducer(new SegmentReadState(readState, segmentSuffix));
                                }
                                fields[fieldName] = formats[segmentSuffix];
                            }
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(formats.Values.Cast<IDisposable>().ToArray());
                    }
                }
            }

            private FieldsReader(FieldsReader other)
            {

                IDictionary<DocValuesProducer, DocValuesProducer> oldToNew =
                    new IdentityDictionary<DocValuesProducer, DocValuesProducer>();
                // First clone all formats
                foreach (KeyValuePair<String, DocValuesProducer> ent in other.formats)
                {
                    DocValuesProducer values = ent.Value;
                    formats[ent.Key] = values;
                    oldToNew[ent.Value] = values;
                }

                // Then rebuild fields:
                foreach (KeyValuePair<String, DocValuesProducer> ent in other.fields)
                {
                    DocValuesProducer producer = oldToNew[ent.Value];
                    //assert producer != null;
                    fields[ent.Key] = producer;
                }
            }

            public override NumericDocValues GetNumeric(FieldInfo field)
            {
                DocValuesProducer producer = fields[field.name];
                return producer == null ? null : producer.GetNumeric(field);
            }

            public override BinaryDocValues GetBinary(FieldInfo field)
            {
                DocValuesProducer producer = fields[field.name];
                return producer == null ? null : producer.GetBinary(field);
            }

            public override SortedDocValues GetSorted(FieldInfo field)
            {
                DocValuesProducer producer = fields[field.name];
                return producer == null ? null : producer.GetSorted(field);
            }

            public override SortedSetDocValues GetSortedSet(FieldInfo field)
            {
                DocValuesProducer producer = fields[field.name];
                return producer == null ? null : producer.GetSortedSet(field);
            }

			public override IBits GetDocsWithField(FieldInfo field)
			{
				DocValuesProducer producer = this.fields[field.name];
				return producer == null ? null : producer.GetDocsWithField(field);
			}
            protected override void Dispose(bool disposing)
            {
                IOUtils.Close(formats.Values.ToArray());
            }

            public object Clone()
            {
                return new FieldsReader(this);
            }

            public override long RamBytesUsed
            {
                get
                {
                    return
                        this.formats.Sum(
                            entry => (entry.Key.Length*RamUsageEstimator.NUM_BYTES_CHAR) + entry.Value.RamBytesUsed);
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            public override void CheckIntegrity()
            {
                foreach (DocValuesProducer format in this.formats.Values)
                {
                    format.CheckIntegrity();
                }
            }
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
            {
                return new FieldsReader(state);
            }

            public abstract DocValuesFormat GetDocValuesFormatForField(String field);
        
    }
}
