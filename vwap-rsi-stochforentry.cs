// NinjaTrader 8 Strategy Skeleton with RSI prereq and Stoch cross entry
// NOTE: This script is a template and will require your own validation and tuning.
// The logic follows your requirements:
// - RSI must have been below 20 (long) or above 80 (short) within last X bars
// - Entry happens when Stochastics K crosses above 20 (long) or below 80 (short)
// - 34 EMA location filter relative to VWAP and VWAP ± 1 SD
// - Multi‑target scaling supported (placeholders added)

#region Using declarations
using System;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class RSIStochVWAPStrategy : Strategy
    {
        // ---- Input Parameters ----
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "BarsLookbackRSI", Order = 1)]
        public int BarsLookbackRSI { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Contracts", Order = 2)]
        public int Contracts { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Target1_ATR", Order = 3)]
        public double Target1_ATR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Target2_ATR", Order = 4)]
        public double Target2_ATR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop_ATR", Order = 5)]
        public double Stop_ATR { get; set; }

        // ---- Indicators ----
        private EMA ema34;
        private RSI rsi;
        private Stochastics stoch;
        private ATR atr;
        private VWAP vwap;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "RSI + Stoch + VWAP_SD Strategy";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                BarsLookbackRSI = 20;
                Contracts = 2;
                Target1_ATR = 3;
                Target2_ATR = 6;
                Stop_ATR = 2.5;
            }
            else if (State == State.Configure)
            {
                // Add your data series if needed
            }
            else if (State == State.DataLoaded)
            {
                ema34 = EMA(34);
                rsi = RSI(14, 3);
                stoch = Stochastics(14, 3, 3);
                atr = ATR(14);
                vwap = VWAP();

                AddChartIndicator(ema34);
                AddChartIndicator(rsi);
                AddChartIndicator(stoch);
                AddChartIndicator(atr);
                AddChartIndicator(vwap);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 50) return;

            // ---- TIME FILTER 6:30–10:30 PT (9:30–13:30 ET) ----
            if (ToTime(Time[0]) < 093000 || ToTime(Time[0]) > 133000)
                return;

            // ---- VWAP SD Check ----
            double vwapValue = vwap[0];
            double sd = vwap.StdDev[0]; // Confirm your VWAP has StdDev; if not, custom calc needed

            // ---- ATR for stops/targets ----
            double currATR = atr[0] * Instrument.MasterInstrument.PointValue;

            // ---- RSI prereq logic ----
            bool rsiWasBelow20 = false;
            bool rsiWasAbove80 = false;

            for (int i = 0; i < BarsLookbackRSI; i++)
            {
                if (rsi[i] < 20) rsiWasBelow20 = true;
                if (rsi[i] > 80) rsiWasAbove80 = true;
            }

            // ---- EMA/VWAP Filters ----
            bool longFilter = ema34[0] > vwapValue && ema34[0] < (vwapValue + sd);
            bool shortFilter = ema34[0] < vwapValue && ema34[0] > (vwapValue - sd);

            // ---- Stoch Cross Logic ----
            bool stochCrossUp = CrossAbove(stoch.K, 20, 1);
            bool stochCrossDown = CrossBelow(stoch.K, 80, 1);

            // ----- LONG ENTRY -----
            if (longFilter && rsiWasBelow20 && stochCrossUp && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterLong(Contracts, "LongEntry");

                // Scale targets
                double target1 = Instrument.MasterInstrument.RoundToTickSize(Target1_ATR * atr[0]);
                double target2 = Instrument.MasterInstrument.RoundToTickSize(Target2_ATR * atr[0]);
                double stopdist = Instrument.MasterInstrument.RoundToTickSize(Stop_ATR * atr[0]);

                SetStopLoss("LongEntry", CalculationMode.Price, Close[0] - stopdist, false);
                SetProfitTarget("LongEntry", CalculationMode.Price, Close[0] + target2);
                // For real scaling: use separate entries: EnterLong(1, "L1"); EnterLong(1, "L2"); etc.
            }

            // ----- SHORT ENTRY -----
            if (shortFilter && rsiWasAbove80 && stochCrossDown && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterShort(Contracts, "ShortEntry");

                double target1 = Instrument.MasterInstrument.RoundToTickSize(Target1_ATR * atr[0]);
                double target2 = Instrument.MasterInstrument.RoundToTickSize(Target2_ATR * atr[0]);
                double stopdist = Instrument.MasterInstrument.RoundToTickSize(Stop_ATR * atr[0]);

                SetStopLoss("ShortEntry", CalculationMode.Price, Close[0] + stopdist, false);
                SetProfitTarget("ShortEntry", CalculationMode.Price, Close[0] - target2);
            }
        }
    }
}
