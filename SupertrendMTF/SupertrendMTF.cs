using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AccessRights = AccessRights.None)]
    public class SupertrendMTF : Indicator
    {
        [Parameter("Time Frame", DefaultValue = "m15")]
        public TimeFrame TimeFrame { get; set; }

        [Parameter("Period", DefaultValue = 10)]
        public int Period { get; set; }

        [Parameter("Multiplier", DefaultValue = 7.0)]
        public double Multiplier { get; set; }

        [Output("Up Trend", LineColor = "Green", PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries UpTrend { get; set; }

        [Output("Down Trend", LineColor = "Red", PlotType = PlotType.Points, Thickness = 1)]
        public IndicatorDataSeries DownTrend { get; set; }

        private IndicatorDataSeries upBuffer;
        private IndicatorDataSeries downBuffer;
        private AverageTrueRange averageTrueRange;
        private int[] trend;
        private bool changeOfTrend;
        private Bars customBars;

        protected override void Initialize()
        {
            customBars = MarketData.GetBars(TimeFrame);
            trend = new int[1];
            upBuffer = CreateDataSeries();
            downBuffer = CreateDataSeries();
            averageTrueRange = Indicators.AverageTrueRange(customBars, Period, MovingAverageType.Simple);
        }

        public override void Calculate(int index)
        {
            UpTrend[index] = double.NaN;
            DownTrend[index] = double.NaN;

            if (index < 1)
            {
                trend[index] = 1;
                return;
            }

            int customIndex = customBars.OpenTimes.GetIndexByTime(Bars.OpenTimes[index]);
            double median = (customBars.HighPrices[customIndex] + customBars.LowPrices[customIndex]) / 2;
            double atr = averageTrueRange.Result[customIndex];

            upBuffer[index] = median + Multiplier * atr;
            downBuffer[index] = median - Multiplier * atr;

            Array.Resize(ref trend, index + 1);
            changeOfTrend = false;

            trend[index] = customBars.ClosePrices[customIndex] > upBuffer[index - 1] ? 1 :
                           customBars.ClosePrices[customIndex] < downBuffer[index - 1] ? -1 :
                           trend[index - 1];

            changeOfTrend = trend[index] != trend[index - 1];

            if (trend[index] == -1 && upBuffer[index] > upBuffer[index - 1])
            {
                upBuffer[index] = upBuffer[index - 1];
                
                DownTrend[index] = upBuffer[index];
                if (changeOfTrend)
                {
                    DownTrend[index - 1] = UpTrend[index - 1];
                    changeOfTrend = false;
                }               
            }

            if (trend[index] == 1 && downBuffer[index] < downBuffer[index - 1])
            {
                downBuffer[index] = downBuffer[index - 1];
                
                UpTrend[index] = downBuffer[index];
                if (changeOfTrend)
                {
                    UpTrend[index - 1] = DownTrend[index - 1];
                    changeOfTrend = false;
                }
            }
        }
    }
}
