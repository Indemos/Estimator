using ExScore.ModelSpace;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
{
  /// <summary>
  /// Kestner ratio or K-Ratio
  /// Deviation from the expected returns curve
  /// KR = Kestner ratio
  /// N = Number of observations
  /// Slope = Coefficient that defines how steep the optimal regression line should be
  /// Error = Standard error explaining deviation from regression
  /// KR = Slope / Error / N
  /// </summary>
  public class KestnerRatio
  {
    /// <summary>
    /// Input values
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

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

      var deviation = 0.0;
      var regression = new double[count];
      var slope = Math.Log(Items.Last().Value) / count;

      for (var i = 0; i < count; i++)
      {
        var value = Math.Log(Items.ElementAtOrDefault(i).Value);

        if (double.IsNaN(value) is false)
        {
          regression[i] = regression.ElementAtOrDefault(i - 1) + slope;
          deviation += Math.Pow(regression[i] - value, 2);
        }
      }

      var error = Math.Sqrt(deviation / count) / Math.Sqrt(count);

      return slope / (error * count);
    }
  }
}
