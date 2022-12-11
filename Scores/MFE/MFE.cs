using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
{
  public class MFE
  {
    /// <summary>
    /// Inputs
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate() => Items.Average(o => o.Max - o.Value).Round();
  }
}
