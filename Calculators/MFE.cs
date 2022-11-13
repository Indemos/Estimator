using ExScore.ModelSpace;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
{
  /// <summary>
  /// Maximum favorable excursion
  /// Maximum unrealized profit
  /// </summary>
  public class MFE
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

      return Items.Average(o => o.Max - o.Value);
    }
  }
}
