using Estimator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Estimators
{
  public enum ModeEnum
  {
    Mar,
    Sharpe,
    Sortino,
    Sterling
  }

  public class RAR
  {
    /// <summary>
    /// Inputs
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Interest rate
    /// </summary>
    public virtual double Rate { get; set; }

    /// <summary>
    /// Number of intervals
    /// </summary>
    public virtual double Count { get; set; }

    /// <summary>
    /// Mode
    /// </summary>
    public virtual ModeEnum Mode { get; set; }

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate()
    {
      var mean = Items.Average(o => o.Value);
      var denominator = 0.0;

      foreach (var item in Items)
      {
        var gain = item.Value;

        switch (Mode)
        {
          case ModeEnum.Mar: denominator = Math.Min(denominator, gain); break;
          case ModeEnum.Sharpe: denominator += (gain - mean) * (gain - mean); break;
          case ModeEnum.Sortino: denominator += gain < 0 ? (gain - mean) * (gain - mean) : 0; break;
          case ModeEnum.Sterling: denominator += gain < 0 ? gain : 0; break;
        }
      }

      switch (Mode)
      {
        case ModeEnum.Mar: denominator = Math.Abs(denominator); break;
        case ModeEnum.Sharpe: denominator = Math.Sqrt(denominator / Items.Count); break;
        case ModeEnum.Sortino: denominator = Math.Sqrt(denominator / Items.Count); break;
        case ModeEnum.Sterling: denominator = Math.Abs(denominator / Items.Count); break;
      }

      if (denominator == 0)
      {
        denominator = 1.0;
      }

      return Math.Sqrt(Count) * (mean - Rate) / denominator;
    }
  }
}
