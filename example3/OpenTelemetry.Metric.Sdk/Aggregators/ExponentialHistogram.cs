using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTelemetry.Metric.Sdk
{
    class ExponentialHistogram : Aggregator
    {
        const int ExponentArraySize = 2048;
        const int ExponentShift = 52;
        const double MinRelativeError = 0.000001;

        PercentileAggregation _config;
        object[] _counters;
        int _count;
        int _mantissaMax;
        int _mantissaMask;
        int _mantissaShift;


        public ExponentialHistogram(PercentileAggregation config)
        {
            _config = config;
            _counters = new object[ExponentArraySize];
            if (_config.MaxRelativeError < MinRelativeError)
            {
                throw new ArgumentException();
            }
            int mantissaBits = (int)Math.Ceiling(Math.Log2(1 / _config.MaxRelativeError)) - 1;
            _mantissaShift = 52 - mantissaBits;
            _mantissaMax = 1 << mantissaBits;
            _mantissaMask = _mantissaMax - 1;
        }

        public override MeasurementAggregation MeasurementAggregation => _config;

        public override AggregationStatistics Collect()
        {
            object[] counters;
            int count;
            lock (this)
            {
                counters = _counters;
                count = _count;
                _counters = new object[ExponentArraySize];
                _count = 0;
            }
            QuantileValue[] quantiles = new QuantileValue[_config.Percentiles.Length];
            int nextPercentileIdx = 0;
            int cur = 0;
            int target = Math.Max(1, (int)(_config.Percentiles[nextPercentileIdx] * count / 100.0));
            for (int exponent = 0; exponent < ExponentArraySize; exponent++)
            {
                int[] mantissaCounts = Unsafe.As<object, int[]>(ref counters[exponent]);
                if (mantissaCounts == null)
                {
                    continue;
                }
                for (int mantissa = 0; mantissa < _mantissaMax; mantissa++)
                {
                    cur += mantissaCounts[mantissa];
                    while (cur >= target)
                    {
                        quantiles[nextPercentileIdx] = new QuantileValue(
                            _config.Percentiles[nextPercentileIdx] / 100.0,
                            GetBucketCanonicalValue(exponent, mantissa));
                        nextPercentileIdx++;
                        if (nextPercentileIdx == _config.Percentiles.Length)
                        {
                            return new DistributionStatistics(MeasurementAggregation, quantiles);
                        }
                        target = Math.Max(1, (int)(_config.Percentiles[nextPercentileIdx] * count / 100.0));
                    }
                }
            }

            Debug.Assert(_count == 0);
            return new DistributionStatistics(MeasurementAggregation, new QuantileValue[0]);
        }

        public override void Update<T>(T measurement)
        {
            double dval = ToDouble(measurement);
            lock (this)
            {
                ref long bits = ref Unsafe.As<double, long>(ref dval);
                int exponent = (int)(bits >> ExponentShift);
                int mantissa = (int)(bits >> _mantissaShift) & _mantissaMask;
                ref int[] mantissaCounts = ref Unsafe.As<object, int[]>(ref _counters[exponent]);
                mantissaCounts ??= new int[_mantissaMax];
                mantissaCounts[mantissa]++;
                _count++;
            }
        }

        // This is the upper bound for negative valued buckets and the
        // lower bound for positive valued buckets
        private double GetBucketCanonicalValue(int exponent, int mantissa)
        {
            double result = 0;
            ref long bits = ref Unsafe.As<double, long>(ref result);
            bits = ((long)exponent << ExponentShift) | ((long)mantissa << _mantissaShift);
            return result;
        }
    }
}
