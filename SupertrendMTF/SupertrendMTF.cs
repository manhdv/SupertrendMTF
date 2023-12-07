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

        private AverageTrueRange averageTrueRange;
        private Bars customBars;
        private double prevUpBuffer, prevDownBuffer;
        private int prevTrend;

        protected override void Initialize()
        {
            customBars = MarketData.GetBars(TimeFrame);
            averageTrueRange = Indicators.AverageTrueRange(customBars, Period, MovingAverageType.Simple);
        }

        public override void Calculate(int index)
        {
            if (index < 1)
            {
                prevTrend = 1;
                return;
            }

            int customIndex = customBars.OpenTimes.GetIndexByTime(Bars.OpenTimes[index]);
            double median = (customBars.HighPrices[customIndex] + customBars.LowPrices[customIndex]) / 2;
            double atr = averageTrueRange.Result[customIndex];

            double upBuffer = median + Multiplier * atr;
            double downBuffer = median - Multiplier * atr;


            int trend = customBars.ClosePrices[customIndex] > prevUpBuffer ? 1 :
                           customBars.ClosePrices[customIndex] < prevDownBuffer ? -1 :
                           prevTrend;

            if (trend == -1)
            {
                if (upBuffer > prevUpBuffer) upBuffer = prevUpBuffer;
                DownTrend[index] = upBuffer;
                if(prevTrend == 1)  DownTrend[index - 1] = UpTrend[index - 1];              
            }

            if (trend == 1)
            {
                if (downBuffer < prevDownBuffer) downBuffer = prevDownBuffer;
                UpTrend[index] = downBuffer;
                if (prevTrend == -1)  UpTrend[index - 1] = DownTrend[index - 1];
            }

            prevDownBuffer = downBuffer;
            prevUpBuffer = upBuffer;
            prevTrend = trend;
            Print("UpTrend = {0}, DownTrend = {1}", UpTrend[index], DownTrend[index] );
        }
    }
}
