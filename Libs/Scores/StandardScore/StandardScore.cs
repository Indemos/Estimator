using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
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
      var count = Items.Count;
      var samples = Items.Percents((items, i) =>
      {
        var current = items.ElementAtOrDefault(i).Value;
        var previous = items.ElementAtOrDefault(i - 1).Value;

        switch (true)
        {
          case true when current > 0 && previous <= 0: gains++; seriesGains++; break;
          case true when current < 0 && previous >= 0: losses++; seriesLosses++; break;
          case true when current > 0: gains++; break;
          case true when current < 0: losses++; break;
        }
      });

      var product = 2 * seriesGains * seriesLosses;
      var seriesCount = seriesGains + seriesLosses;
      var denominator = product * (product - count) / (count - 1.0);

      if (denominator == 0)
      {
        denominator = 1.0;
      }

      return (Math.Sqrt(count * (seriesCount - 0.5) - product) / denominator).Round();
    }
  }
}
