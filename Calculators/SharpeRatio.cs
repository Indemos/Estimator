using Score.ModelSpace;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Score.ScoreSpace
{
  /// <summary>
  /// Reward to risk ratio for a selected period
  /// Ra = Asset returns
  /// Rb = Risk-free returns
  /// IR = Interest rate
  /// Dev = Series deviation
  /// AnnDev = Dev * Sqrt(Days)
  /// Sharpe = Mean([Ra - Rb]) / Dev([Ra - Rb])
  /// Using CAGR
  /// Sharpe = (CAGR - IR) / AnnDev
  /// Using AHPR
  /// Sharpe = (AHPR - (1 + IR)) / Dev
  /// </summary>
  public class SharpeRatio
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
    /// <param name="option"></param>
    /// <returns></returns>
    public virtual double Calculate(int? option = 0)
    {
      var count = Items.Count;

      if (count < 2)
      {
        return 0;
      }

      switch (option)
      {
        case 0: return CalculateDealsRatio();
        case 1: return CalculateAverageRatio();
        case 2: return CalculateCompoundRatio();
      }

      return 0.0;
    }

    /// <summary>
    /// Calculate SR based on mean
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public virtual double CalculateDealsRatio()
    {
      var input = Items.First();
      var output = Items.Last();
      var excessGain = Items.Select((o, i) => o.Value - Items.ElementAtOrDefault(i - 1).Value).Mean();
      var deviation = Items.Select(o => o.Value).StandardDeviation();

      if (deviation == 0)
      {
        return 0;
      }

      return excessGain / deviation;
    }

    /// <summary>
    /// Calculate SR based on AHPR
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public virtual double CalculateAverageRatio()
    {
      var input = Items.First();
      var output = Items.Last();
      var score = new AHPR
      {
        Items = Items
      };

      var excessGain = score.Calculate() - InterestRate;
      var deviation = Items.Select(o => o.Value).StandardDeviation();

      if (deviation == 0)
      {
        return 0;
      }

      return excessGain / deviation;
    }

    /// <summary>
    /// Calculate
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public virtual double CalculateCompoundRatio(int? option = 0)
    {
      var input = Items.First();
      var output = Items.Last();
      var score = new CAGR
      {
        Items = Items
      };

      var excessGain = score.Calculate() - InterestRate;
      var days = output.Time.Subtract(input.Time).Duration().Days + 1;
      var deviation = Items.Select(o => o.Value).StandardDeviation() * Math.Sqrt(days);

      if (deviation == 0)
      {
        return 0;
      }

      return excessGain / deviation;
    }
  }
}
