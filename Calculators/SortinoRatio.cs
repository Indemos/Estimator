using Stats.ModelSpace;
using MathNet.Numerics.Financial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stats.ScoreSpace
{
  /// <summary>
  /// Reward to risk ratio for a selected period measuring downside
  /// Ra = Asset returns
  /// Rb = Risk-free returns
  /// IR = Interest rate
  /// DownDev = Series deviation below 0 level
  /// AnnDev = DownDev * (Days ^ (1 / 2))
  /// Sortino = (CAGR - IR) / AnnDev
  /// </summary>
  public class SortinoRatio
  {
    /// <summary>
    /// Input values
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Interest rate
    /// </summary>
    public virtual double InterestRate { get; set; } = 0;

    /// <summary>
    /// Calculate
    /// </summary>
    /// <returns></returns>
    public virtual double Calculate()
    {
      var count = Items.Count;

      if (count < 2)
      {
        return 0;
      }

      var input = Items.First();
      var output = Items.Last();
      var cagr = new CAGR
      {
        Items = Items
      };

      var values = Items.Select((o, i) => o.Value - Items.ElementAtOrDefault(i - 1).Value);
      var excessGain = cagr.Calculate() - InterestRate;
      var days = output.Time.Subtract(input.Time).Duration().Days + 1.0;
      var downsideDeviation = values.DownsideDeviation(0);
      var annualDeviation = (double.IsNaN(downsideDeviation) ? 0.0 : downsideDeviation) * Math.Sqrt(days);

      if (annualDeviation == 0)
      {
        return 0.0;
      }

      return excessGain / annualDeviation;
    }
  }
}
