using System;

namespace Estimator.Extensions
{
  public static class DoubleExtensions
  {
    public static double Round(this double input, int points = 2, MidpointRounding mode = MidpointRounding.ToEven)
    {
      return Math.Round(input, points, mode);
    }
  }
}
