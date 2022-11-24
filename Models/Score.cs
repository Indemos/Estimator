using ExScore.ScoreSpace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ModelSpace
{
  /// <summary>
  /// Generic position model
  /// </summary>
  public struct InputData
  {
    public int Direction { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Value { get; set; }
    public double Commission { get; set; }
    public DateTime Time { get; set; }
  }

  /// <summary>
  /// Output model
  /// </summary>
  public struct ScoreData
  {
    public double Value { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public string Description { get; set; }
  }

  /// <summary>
  /// Stats grouped by position side
  /// </summary>
  public struct DirectionData
  {
    public int GainsCount { get; set; }
    public int LossesCount { get; set; }
    public double Gains { get; set; }
    public double Losses { get; set; }
    public double GainsMax { get; set; }
    public double LossesMax { get; set; }
    public double Commissions { get; set; }
  }
  /// <summary>
  /// Common statistical characteristics
  /// </summary>
  public class Score
  {
    /// <summary>
    /// Input values
    /// </summary>
    public virtual IList<InputData> Values { get; set; } = new List<InputData>();

    /// <summary>
    /// Statistics grouped by time frames
    /// </summary>
    public virtual FrameResponse StatsByFrames { get; protected set; } = new FrameResponse();

    /// <summary>
    /// Statistics grouped by series
    /// </summary>
    public virtual SeriesResponse StatsBySeries { get; protected set; } = new SeriesResponse();

    /// <summary>
    /// Calculate
    /// </summary>
    /// <returns></returns>
    public virtual IDictionary<string, IList<ScoreData>> Calculate()
    {
      if (Values.Any() is false)
      {
        return new Dictionary<string, IList<ScoreData>>();
      }

      var balanceMax = 0.0;
      var balanceDrawdown = 0.0;
      var equityMax = 0.0;
      var equityDrawdown = 0.0;
      var count = Values.Count;
      var longs = new DirectionData();
      var shorts = new DirectionData();
      var inputBalance = Values.ElementAtOrDefault(0);
      var outputBalance = Values.ElementAtOrDefault(count - 1);

      for (var i = 1; i < count; i++)
      {
        var current = Values.ElementAtOrDefault(i);
        var previous = Values.ElementAtOrDefault(i - 1);
        var item = current.Value - previous.Value;
        var itemGain = Math.Abs(Math.Max(item, 0.0));
        var itemLoss = Math.Abs(Math.Min(item, 0.0));

        equityMax = Math.Max(equityMax, current.Max);
        balanceMax = Math.Max(balanceMax, current.Value);
        equityDrawdown = Math.Max(equityMax - current.Min, equityDrawdown);
        balanceDrawdown = Math.Max(balanceMax - current.Value, balanceDrawdown);

        switch (current.Direction)
        {
          case 1:

            longs.Gains += itemGain;
            longs.Losses += itemLoss;
            longs.GainsMax = Math.Max(itemGain, longs.GainsMax);
            longs.LossesMax = Math.Max(itemLoss, longs.LossesMax);
            longs.GainsCount += item > 0.0 ? 1 : 0;
            longs.LossesCount += item < 0.0 ? 1 : 0;
            longs.Commissions += current.Commission;
            break;

          case -1:

            shorts.Gains += itemGain;
            shorts.Losses += itemLoss;
            shorts.GainsMax = Math.Max(itemGain, shorts.GainsMax);
            shorts.LossesMax = Math.Max(itemLoss, shorts.LossesMax);
            shorts.GainsCount += item > 0.0 ? 1 : 0;
            shorts.LossesCount += item < 0.0 ? 1 : 0;
            shorts.Commissions += current.Commission;
            break;
        }
      }

      var gains = longs.Gains + shorts.Gains;
      var losses = longs.Losses + shorts.Losses;
      var gainsMax = Math.Max(longs.GainsMax, shorts.GainsMax);
      var lossesMax = Math.Max(longs.LossesMax, shorts.LossesMax);
      var gainsCount = longs.GainsCount + shorts.GainsCount;
      var lossesCount = longs.LossesCount + shorts.LossesCount;
      var gainsAverage = Validate(() => gains / gainsCount);
      var lossesAverage = Validate(() => losses / lossesCount);
      var dealsCount = gainsCount + lossesCount;

      var stats = new List<ScoreData>
      {
        new ScoreData { Group = "Balance", Name = "Initial balance $", Value = inputBalance.Value },
        new ScoreData { Group = "Balance", Name = "Final balance $", Value = outputBalance.Value },
        new ScoreData { Group = "Balance", Name = "Commissions $", Value = longs.Commissions + shorts.Commissions },
        new ScoreData { Group = "Balance", Name = "Drawdown $", Value = -balanceDrawdown },
        new ScoreData { Group = "Balance", Name = "Drawdown %", Value = -Validate(() => balanceDrawdown * 100.0 / balanceMax) },
        new ScoreData { Group = "Balance", Name = "Equity drawdown $", Value = -equityDrawdown },
        new ScoreData { Group = "Balance", Name = "Equity drawdown %", Value = -Validate(() => equityDrawdown * 100.0 / equityMax) },
        new ScoreData { Group = "Balance", Name = "ROC $", Value = new ROC { Items = Values }.Calculate(0) },
        new ScoreData { Group = "Balance", Name = "ROC %", Value = new ROC { Items = Values }.Calculate(1) },

        new ScoreData { Group = "Wins", Name = "Total gain $", Value = gains },
        new ScoreData { Group = "Wins", Name = "Max single gain $", Value = gainsMax },
        new ScoreData { Group = "Wins", Name = "Average gain $", Value = gainsAverage },
        new ScoreData { Group = "Wins", Name = "Number of wins", Value = gainsCount },
        new ScoreData { Group = "Wins", Name = "Percentage of wins %", Value = Validate(() => gainsCount * 100.0 / dealsCount) },

        new ScoreData { Group = "Losses", Name = "Total loss $", Value = -losses },
        new ScoreData { Group = "Losses", Name = "Max single loss $", Value = -lossesMax },
        new ScoreData { Group = "Losses", Name = "Average loss $", Value = -lossesAverage },
        new ScoreData { Group = "Losses", Name = "Number of losses", Value = lossesCount },
        new ScoreData { Group = "Losses", Name = "Percentage of losses %", Value = Validate(() => lossesCount * 100.0 / dealsCount) },

        new ScoreData { Group = "Longs", Name = "Total gain $", Value = longs.Gains },
        new ScoreData { Group = "Longs", Name = "Total loss $", Value = -longs.Losses },
        new ScoreData { Group = "Longs", Name = "Max gain $", Value = longs.GainsMax },
        new ScoreData { Group = "Longs", Name = "Max loss $", Value = -longs.LossesMax },
        new ScoreData { Group = "Longs", Name = "Average gain $", Value = Validate(() => longs.Gains / longs.GainsCount) },
        new ScoreData { Group = "Longs", Name = "Average loss $", Value = -Validate(() => longs.Losses / longs.LossesCount) },
        new ScoreData { Group = "Longs", Name = "Number of longs", Value = longs.GainsCount + longs.LossesCount },
        new ScoreData { Group = "Longs", Name = "Percentage of longs %", Value = Validate(() => (longs.GainsCount + longs.LossesCount) * 100.0 / dealsCount) },

        new ScoreData { Group = "Shorts", Name = "Total gain $", Value = shorts.Gains },
        new ScoreData { Group = "Shorts", Name = "Total loss $", Value = -shorts.Losses },
        new ScoreData { Group = "Shorts", Name = "Max gain $", Value = shorts.GainsMax },
        new ScoreData { Group = "Shorts", Name = "Max loss $", Value = -shorts.LossesMax },
        new ScoreData { Group = "Shorts", Name = "Average gain $", Value = Validate(() => shorts.Gains / shorts.GainsCount) },
        new ScoreData { Group = "Shorts", Name = "Average loss $", Value = -Validate(() => shorts.Losses / shorts.LossesCount) },
        new ScoreData { Group = "Shorts", Name = "Number of shorts", Value = shorts.GainsCount + shorts.LossesCount },
        new ScoreData { Group = "Shorts", Name = "Percentage of shorts %", Value = Validate(() => (shorts.GainsCount + shorts.LossesCount) * 100.0 / dealsCount) },

        new ScoreData { Group = "Ratios", Name = "Profit Factor", Value = Validate(() => gains / losses) },
        new ScoreData { Group = "Ratios", Name = "Mean", Value = Validate(() => (gains - losses) / dealsCount) },
        new ScoreData { Group = "Ratios", Name = "CAGR", Value = new CAGR { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "Sharpe Ratio", Value = new SharpeRatio { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "MAE", Value = new MAE { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "MFE", Value = new MFE { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "MAR", Value = new MAR { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "AHPR", Value = new AHPR { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "GHPR", Value = new GHPR { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "Z-Score", Value = new StandardScore { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "E-Ratio", Value = new EdgeRatio { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "Martin Ratio", Value = new MartinRatio { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "Sortino Ratio", Value = new SortinoRatio { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "Sterling Ratio", Value = new SterlingRatio { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "Kestner Ratio", Value = new KestnerRatio { Items = Values }.Calculate() },
        new ScoreData { Group = "Ratios", Name = "LR Correlation", Value = new RegressionCorrelation { Items = Values }.Calculate() }
      };

      StatsByFrames = new FrameScore { Values = Values }.Calculate();
      StatsBySeries = new SeriesScore { Values = Values }.Calculate();

      return stats.GroupBy(o => o.Group).ToDictionary(o => o.Key, o => o.ToList() as IList<ScoreData>);
    }

    /// <summary>
    /// Validate correctness of the expression, e.g. division by zero
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static double Validate(Func<double?> input)
    {
      var output = input();

      if (output is null || double.IsNaN(output.Value) || double.IsInfinity(output.Value))
      {
        return 0.0;
      }

      return output.Value;
    }
  }
}
