using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenTelemetry.Metric.Sdk
{
    interface IStringSequence
    {
        Span<string> AsSpan();
    }

    struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
    {
        public string Value1;

        public StringSequence1(string value1)
        {
            Value1 = value1;
        }

        public Span<string> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 1);
        }

        public override int GetHashCode() => Value1.GetHashCode();

        public bool Equals(StringSequence1 other)
        {
            return Value1 == other.Value1;
        }

        public override bool Equals(object obj)
        {
            return obj is StringSequence1 && Equals((StringSequence1)obj);
        }
    }

    struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
    {
        public string Value1;
        public string Value2;

        public StringSequence2(string value1, string value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public Span<string> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 2);
        }

        public override int GetHashCode() => HashCode.Combine(Value1.GetHashCode(), Value2.GetHashCode());

        public bool Equals(StringSequence2 other)
        {
            return Value1 == other.Value1 && Value2 == other.Value2;
        }

        public override bool Equals(object obj)
        {
            return obj is StringSequence2 && Equals((StringSequence2)obj);
        }
    }

    struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
    {
        public string Value1;
        public string Value2;
        public string Value3;

        public StringSequence3(string value1, string value2, string value3)
        {
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }

        public Span<string> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Value1, 3);
        }

        public override int GetHashCode() => HashCode.Combine(Value1.GetHashCode(), Value2.GetHashCode(), Value3.GetHashCode());

        public bool Equals(StringSequence3 other)
        {
            return Value1 == other.Value1 && Value2 == other.Value2 && Value3 == other.Value3;
        }

        public override bool Equals(object obj)
        {
            return obj is StringSequence3 && Equals((StringSequence3)obj);
        }
    }

    struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
    {
        string[] _values;

        public StringSequenceMany(string[] values)
        {
            _values = values;
        }

        public Span<string> AsSpan()
        {
            return _values.AsSpan();
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _values.Length; i++)
            {
                hash = (int)BitOperations.RotateLeft((uint)hash, 3);
                hash ^= _values[i].GetHashCode();
            }
            return hash;
        }

        public bool Equals(StringSequenceMany other)
        {
            if (_values.Length != other._values.Length)
            {
                return false;
            }
            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] != other._values[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is StringSequenceMany && Equals((StringSequenceMany)obj);
        }
    }
}
