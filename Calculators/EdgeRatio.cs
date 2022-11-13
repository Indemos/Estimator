using Score.ModelSpace;
using System.Collections.Generic;
using System.Linq;

namespace Score.ScoreSpace
{
  /// <summary>
  /// Edge ratio or E-Ratio
  /// Ratio between MFE and MAE to identify if trades have bias towards profit
  /// N = Number of observations
  /// AvgEdge = ((MFE / MAE) + (MFE / MAE) + ... + (MFE / MAE)) / N
  /// </summary>
  public class EdgeRatio
  {
    /// <summary>
    /// Input values
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    /// <returns></returns>
    public virtual double Calculate()
    {
      var count = Items.Count;

      if (count < 2)
      {
        return 0;
      }

      var sum = Items.Sum(o =>
      {
        var maxGain = o.Max - o.Value;
        var maxLoss = o.Value - o.Min;

        if (maxLoss == 0)
        {
          return 0;
        }

        return maxGain / maxLoss;
      });

      return sum / count;
    }
  }
}
