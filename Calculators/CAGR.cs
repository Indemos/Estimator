using Score.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Score.ScoreSpace
{
  /// <summary>
  /// Compound annual growth rate
  /// F = Final value or last in the series
  /// I = Initial value or last in the series
  /// GR = Growth Rate = (F - I) / I
  /// CAGR = ((F / I) ^ (1 / Years)) - 1
  /// For a finite series with known growth rate
  /// Days = Number of days in the selected period
  /// CAGR = (GR ^ (365 / Days)) - 1
  /// </summary>
  public class CAGR
  {
    /// <summary>
    /// Inputs values
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

      var input = Items.First();
      var output = Items.Last();
      var days = 365.0 / (output.Time.Subtract(input.Time).Duration().Days + 1.0);
      var change = output.Value / Math.Max(input.Value, 1.0);

      if (change == 0)
      {
        return 0;
      }

      return Math.Pow(change, days) - 1.0;
    }
  }
}
