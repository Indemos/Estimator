using ExScore.ModelSpace;
using System;
using System.Collections.Generic;

namespace ExScore.ExtensionSpace
{
  public static class ListExtensions
  {
    /// <summary>
    /// Samples
    /// </summary>
    /// <param name="items"></param>
    /// <param name="action"></param>
    public static IList<InputData> Samples(this IList<InputData> items, Action<IList<InputData>, int> action)
    {
      return Items(items, (x, y) => y.Value - x.Value, action);
    }

    /// <summary>
    /// Logs
    /// </summary>
    /// <param name="items"></param>
    /// <param name="action"></param>
    public static IList<InputData> Logs(this IList<InputData> items, Action<IList<InputData>, int> action)
    {
      return Items(items, (x, y) => Math.Log(y.Value / x.Value), action);
    }

    /// <summary>
    /// Percents
    /// </summary>
    /// <param name="items"></param>
    /// <param name="action"></param>
    public static IList<InputData> Percents(this IList<InputData> items, Action<IList<InputData>, int> action)
    {
      return Items(items, (x, y) => (y.Value - x.Value) / x.Value, action);
    }

    /// <summary>
    /// Items
    /// </summary>
    /// <param name="items"></param>
    /// <param name="process"></param>
    /// <param name="response"></param>
    private static IList<InputData> Items(
      IList<InputData> items,
      Func<InputData, InputData, double> process,
      Action<IList<InputData>, int> response)
    {
      var samples = new InputData[items.Count - 1];

      for (var i = 1; i < items.Count; i++)
      {
        samples[i - 1] = items[i];
        samples[i - 1].Value = process(items[i - 1], items[i]);
        response(samples, i - 1);
      }

      return samples;
    }
  }
}
