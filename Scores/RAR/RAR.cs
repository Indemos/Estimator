using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System;
using System.Collections.Generic;

namespace ExScore.ScoreSpace
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
    /// Mode
    /// </summary>
    public virtual ModeEnum Mode { get; set; }

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate()
    {
      var mean = 0.0;
      var meanSamples = Items.Percents((x, y) => mean += y.Value);

      mean /= meanSamples.Count;

      var denominator = 0.0;
      var devSamples = Items.Percents((x, y) =>
      {
        switch (Mode)
        {
          case ModeEnum.Mar: denominator = Math.Min(y.Value, denominator); break;
          case ModeEnum.Sharpe: denominator += (y.Value - mean) * (y.Value - mean); break;
          case ModeEnum.Sortino: denominator += y.Value < 0 ? (y.Value - mean) * (y.Value - mean) : 0; break;
          case ModeEnum.Sterling: denominator += y.Value < 0 ? y.Value : 0; break;
        }
      });

      switch (Mode)
      {
        case ModeEnum.Mar: denominator = Math.Abs(denominator); break;
        case ModeEnum.Sharpe: denominator = Math.Sqrt(denominator / devSamples.Count); break;
        case ModeEnum.Sortino: denominator = Math.Sqrt(denominator / devSamples.Count); break;
        case ModeEnum.Sterling: denominator = Math.Abs(denominator / devSamples.Count); break;
      }

      if (mean == 0)
      {
        mean = 1.0;
      }

      if (denominator == 0)
      {
        denominator = 1.0;
      }

      return (Math.Sqrt(meanSamples.Count) * (mean - Rate) / denominator).Round();
    }
  }
}
