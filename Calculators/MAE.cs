using Stats.ModelSpace;
using System.Collections.Generic;
using System.Linq;

namespace Stats.ScoreSpace
{
  /// <summary>
  /// Maximum adverse excursion
  /// Maximum unrealized loss
  /// </summary>
  public class MAE
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

      return Items.Average(o => o.Value - o.Min);
    }
  }
}
