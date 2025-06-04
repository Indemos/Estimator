using Estimator.Models;
using System;
using System.Collections.Generic;

namespace Estimator.Estimators
{
  public class KestnerRatio
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
      var regression = new Regression { Items = Items }.Calculate();
      var slope = regression.Covariance / regression.DevY;
      var error = Math.Sqrt(regression.DevY / (Items.Count - 1));

      if (error is 0)
      {
        return slope;
      }

      return slope / error;
    }
  }
}
