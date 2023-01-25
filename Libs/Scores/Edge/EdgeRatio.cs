using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System.Collections.Generic;

namespace ExScore.ScoreSpace
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

      return (averageGain / averageLoss).Round();
    }
  }
}
