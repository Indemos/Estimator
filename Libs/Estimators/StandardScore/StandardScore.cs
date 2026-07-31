using Estimator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Estimators
{
  public class StandardScore
  {
    /// <summary>
    /// Inputs
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate()
    {
      if (Items?.Count < 2) return 0;

      var runs = 0;
      var wins = 0;
      var losses = 0;

      int? curSign = null;

      foreach (var item in Items)
      {
        var sign = item.Value > 0 ? 1 : item.Value < 0 ? -1 : 0;

        if (sign == 0) continue;
        if (sign > 0) wins++; else losses++;
        if (curSign is null || sign != curSign) runs++;

        curSign = sign;
      }

      var N = wins + losses;

      if (N <= 1 || wins is 0 || losses is 0) return 0;

      var P = 2.0 * wins * losses;
      var numerator = N * (runs - 0.5) - P;
      var denominator = P * (P - N) / (N - 1.0);
      
      if (denominator <= 0) return 0;

      return numerator / Math.Sqrt(denominator);
    }
  }
}
