using Estimator.Extensions;
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
      var gains = 0.0;
      var losses = 0.0;
      var seriesGains = 0.0;
      var seriesLosses = 0.0;

      for (var i = 0; i < Items.Count; i++)
      {
        var current = Items.ElementAtOrDefault(i).Value;
        var previous = Items.ElementAtOrDefault(i - 1).Value;

        switch (true)
        {
          case true when current > 0 && previous <= 0: gains++; seriesGains++; break;
          case true when current < 0 && previous >= 0: losses++; seriesLosses++; break;
          case true when current > 0: gains++; break;
          case true when current < 0: losses++; break;
        }
      }

      var sum = gains + losses;
      var product = 2 * gains * losses;
      var seriesCount = seriesGains + seriesLosses;
      var mean = product / sum + 1;
      var deviation = Math.Sqrt(product * (product - sum) / ((sum - 1.0) * sum * sum));

      if (deviation == 0)
      {
        deviation = 1.0;
      }

      return ((seriesCount - mean - 0.5) / deviation).Round();
    }
  }
}
