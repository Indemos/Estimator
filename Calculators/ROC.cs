using ExScore.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
{
  /// <summary>
  /// ROC or rate of change
  /// </summary>
  public class ROC
  {
    /// <summary>
    /// Input values
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public virtual double Calculate(int? option = 0)
    {
      var count = Items.Count;

      if (count < 2)
      {
        return 0;
      }

      var input = Items.First();
      var output = Items.Last();

      if (input.Value == 0)
      {
        return 0;
      }

      switch (option)
      {
        case 0: return output.Value - input.Value;
        case 1: return (output.Value - input.Value) / Math.Abs(input.Value) * 100;
      }

      return 0.0;
    }
  }
}
