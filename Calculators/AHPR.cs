using Score.ModelSpace;
using System.Collections.Generic;
using System.Linq;

namespace Score.ScoreSpace
{
  /// <summary>
  /// AHPR or average holding period returns
  /// I = Balance before deal
  /// O = Balance after deal
  /// HPR = Holding period returns for one deal = O / I
  /// N = Number of deals 
  /// AHPR = (HPR[0] + HPR[1] + ... + HPR[N]) / N
  /// </summary>
  public class AHPR
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
      var sum = 0.0;
      var count = Items.Count;

      if (count < 2)
      {
        return 0;
      }

      for (var i = 1; i < count; i++)
      {
        var currentValue = Items.ElementAtOrDefault(i).Value;
        var previousValue = Items.ElementAtOrDefault(i - 1).Value;

        sum += currentValue / previousValue;
      }

      return sum / count;
    }
  }
}
