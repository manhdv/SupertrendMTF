using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class SupertrendMTF : Indicator
    {
        [Parameter("Time Frame", DefaultValue = "Daily")]
        public TimeFrame TimeFrame { get; set; }

        [Parameter("Period", DefaultValue = 10)]
        public int Period { get; set; }

        [Parameter("Multiplier", DefaultValue = 3.0)]
        public double Multiplier { get; set; }

        [Output("Up Trend", LineColor = "Green", PlotType = PlotType.Points, Thickness = 3)]
        public IndicatorDataSeries UpTrend { get; set; }

        [Output("Down Trend", LineColor = "Red", PlotType = PlotType.Points, Thickness = 3)]
        public IndicatorDataSeries DownTrend { get; set; }

        private IndicatorDataSeries _upBuffer;
        private IndicatorDataSeries _downBuffer;
        private AverageTrueRange _averageTrueRange;
        private int[] _trend;
        private bool _changeOfTrend;
        private Bars _customBars;

        protected override void Initialize()
        {
            _customBars = MarketData.GetBars(TimeFrame);
            _trend = new int[1];
            _upBuffer = CreateDataSeries();
            _downBuffer = CreateDataSeries();
            _averageTrueRange = Indicators.AverageTrueRange(_customBars, Period, MovingAverageType.Simple);
        }

        public override void Calculate(int index)
        {
            if (index < 1)
            {
                InitializeTrend(index);
                return;
            }

            UpdateTrend(index);

            if (_trend[index] == 1)
            {
                UpTrend[index] = _downBuffer[index];
                HandleChangeOfTrend(index, isUpTrend: true);
            }
            else if (_trend[index] == -1)
            {
                DownTrend[index] = _upBuffer[index];
                HandleChangeOfTrend(index, isUpTrend: false);
            }
        }

        private void InitializeTrend(int index)
        {
            UpTrend[index] = double.NaN;
            DownTrend[index] = double.NaN;
            _trend[index] = 1;
        }

        private void UpdateTrend(int index)
        {
            int customIndex = _customBars.OpenTimes.GetIndexByTime(Bars.OpenTimes[index]);
            double median = (_customBars.HighPrices[customIndex] + _customBars.LowPrices[customIndex]) / 2;
            double atr = _averageTrueRange.Result[customIndex];

            _upBuffer[index] = median + Multiplier * atr;
            _downBuffer[index] = median - Multiplier * atr;
            _changeOfTrend = false;

            UpdateTrendDirection(index, customIndex);

            AdjustBuffersForTrendContinuation(index);
        }

        private void UpdateTrendDirection(int index, int customIndex)
        {
            Array.Resize(ref _trend, index + 1);

            if (_customBars.ClosePrices[customIndex] > _upBuffer[index - 1])
            {
                _trend[index] = 1;
            }
            else if (_customBars.ClosePrices[customIndex] < _downBuffer[index - 1])
            {
                _trend[index] = -1;
            }
            else
            {
                _trend[index] = _trend[index - 1];
            }

            _changeOfTrend = _trend[index] != _trend[index - 1];
        }

        private void AdjustBuffersForTrendContinuation(int index)
        {
            if (_trend[index] < 0 && _upBuffer[index] > _upBuffer[index - 1])
            {
                _upBuffer[index] = _upBuffer[index - 1];
            }

            if (_trend[index] > 0 && _downBuffer[index] < _downBuffer[index - 1])
            {
                _downBuffer[index] = _downBuffer[index - 1];
            }
        }

        private void HandleChangeOfTrend(int index, bool isUpTrend)
        {
            if (!_changeOfTrend) return;

            if (isUpTrend)
            {
                UpTrend[index - 1] = DownTrend[index - 1];
            }
            else
            {
                DownTrend[index - 1] = UpTrend[index - 1];
            }

            _changeOfTrend = false;
        }
    }
}
