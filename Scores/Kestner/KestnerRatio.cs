using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
{
  /// <summary>
  /// Kestner ratio or K-Ratio
  /// Deviation from the expected returns curve and volatility
  /// KR = Kestner ratio
  /// N = Number of observations
  /// Slope = Coefficient that defines how steep the optimal regression line should be
  /// Error = Standard error explaining deviation from regression
  /// KR = Slope / Error / N
  /// </summary>
  public class KestnerRatio
  {
    /// <summary>
    /// Input values
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate()
    {
      var count = Items.Count;
      var items = new InputData[count];
      var regression = new InputData[count];
      var step = (Items.Last().Value - Items.First().Value) / count;

      for (var i = 1; i < count; i++)
      {
        items[i] = new InputData { Value = Items[i].Value - Items[i - 1].Value };
        regression[i] = new InputData { Value = regression[i - 1].Value + step };
      }

      var slope = 1.0; // new Covariance { ItemsX = items, ItemsY = regression }.Calculate();
      var error = 1.0; // new StandardError { Items = Items }.Calculate();

      return (slope / (error * count)).Round();
    }
  }
}
