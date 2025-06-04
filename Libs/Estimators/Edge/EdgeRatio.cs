using Estimator.Models;
using System.Collections.Generic;

namespace Estimator.Estimators
{
  public class EdgeRatio
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
      var averageGain = new MFE { Items = Items }.Calculate();
      var averageLoss = new MAE { Items = Items }.Calculate();

      if (averageLoss is 0)
      {
        return averageGain;
      }

      return averageGain / averageLoss;
    }
  }
}
