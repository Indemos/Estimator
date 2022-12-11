using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System;
using System.Collections.Generic;

namespace ExScore.ScoreSpace
{
  public class PF
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
      var count = Items.Count;

      for (var i = 1; i < count; i++)
      {
        var delta = Items[i].Value - Items[i - 1].Value;

        gains += Math.Abs(Math.Max(delta, 0.0));
        losses += Math.Abs(Math.Min(delta, 0.0));
      }

      if (losses == 0)
      {
        gains = 1.0;
      }

      return (gains / losses).Round();
    }
  }
}
