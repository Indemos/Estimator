using Estimator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Estimators
{
  public class CAGR
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
    public virtual double Calculate() => Math.Pow(Items.Sum(o => o.Value), 1.0 / Count) - 1.0;
  }
}
