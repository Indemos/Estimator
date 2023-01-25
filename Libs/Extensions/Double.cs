using System;

namespace ExScore.ExtensionSpace
{
  public static class DoubleExtensions
  {
    public static double Round(this double input, int points = 2, MidpointRounding mode = MidpointRounding.ToZero)
    {
      return Math.Round(input, points, mode);
    }
  }
}
