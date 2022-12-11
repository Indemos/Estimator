using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System.Collections.Generic;

namespace ExScore.ScoreSpace
{
  public class AHPR
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
      var mean = 0.0;
      var samples = Items.Percents((x, y) => mean += 1.0 + y.Value);

      return (mean / samples.Count).Round();
    }
  }
}
