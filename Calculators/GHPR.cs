using Score.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Score.ScoreSpace
{
  /// <summary>
  /// GHPR or geometric holding period returns
  /// I = Initial balance
  /// O = Final balance
  /// HPR = Holding period returns for one deal = O / I
  /// N = Number of deals 
  /// GHPR = (HPR[0] + HPR[1] + ... + HPR[N]) ^ (1 / N)
  /// </summary>
  public class GHPR
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

      var sum = 1.0;
      var input = Items.First();

      if (input.Value == 0)
      {
        return 0.0;
      }

      for (var i = 1; i < count; i++)
      {
        var currentValue = Items.ElementAtOrDefault(i).Value;
        var previousValue = Items.ElementAtOrDefault(i - 1).Value;

        sum *= currentValue / previousValue;
      }

      return Math.Pow(sum, 1.0 / count);
    }
  }
}
