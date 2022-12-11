using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
{
  public class GHPR
  {
    /// <summary>
    /// Combined with daily returns, custom count can calculate CAGR
    /// </summary>
    public virtual int Count { get; set; }

    /// <summary>
    /// Inputs
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate()
    {
      var count = Count;

      if (count == 0)
      {
        count = Items.Count;
      }

      var input = Items.First().Value;
      var output = Items.Last().Value;

      if (input == 0)
      {
        input = 1.0;
      }

      return Math.Pow(output / input, 1.0 / count).Round();
    }
  }
}
