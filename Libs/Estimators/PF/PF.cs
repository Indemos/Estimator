using Estimator.Extensions;
using Estimator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Estimators
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
      var gains = Items.Where(o => o.Value > 0).Sum(o => o.Value);
      var losses = Items.Where(o => o.Value < 0).Sum(o => o.Value);

      if (losses == 0)
      {
        gains = 1.0;
      }

      return Math.Abs(gains / losses).Round();
    }
  }
}
