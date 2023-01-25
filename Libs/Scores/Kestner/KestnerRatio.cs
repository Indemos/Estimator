using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System;
using System.Collections.Generic;

namespace ExScore.ScoreSpace
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
      var count = Items.Count;
      var regression = new RegressionCorrelation { Items = Items }.Calculate();
      var slope = regression.Covariance / regression.DevY;
      var error = Math.Sqrt(regression.DevY / (count - 1));

      if (error == 0)
      {
        error = 1.0;
      }

      return (slope / error).Round();
    }
  }
}
