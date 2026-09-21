using System;

namespace Estimator.Services
{
  public class CorrelationService
  {
    protected int count;
    protected int pointer;
    protected int distance;

    protected double[] x;
    protected double[] y;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="frame"></param>
    public CorrelationService(int frame = 60)
    {
      distance = frame;

      x = new double[distance];
      y = new double[distance];
    }

    /// <summary>
    /// Update
    /// </summary>
    /// <param name="valueX"></param>
    /// <param name="valueY"></param>
    public void Update(double valueX, double valueY)
    {
      pointer = (pointer + 1) % distance;

      x[pointer] = valueX;
      y[pointer] = valueY;

      if (count < distance)
      {
        count++;
      }
    }
    
    /// <summary>
    /// Calculate correlation at a specific position
    /// </summary>
    /// <param name="position"></param>
    public double Correlation(int position)
    {
      if (count < distance)
      {
        return 0.0;
      }

      var sumX = 0.0;
      var sumY = 0.0;
      var sumXY = 0.0;
      var sumX2 = 0.0;
      var sumY2 = 0.0;
      var n = distance - Math.Abs(position);

      for (var i = 0; i < n; i++)
      {
        var xLoop = position > 0 ? i + position : i;
        var yLoop = position > 0 ? i : i - position;
        var xv = x[(pointer - xLoop + distance) % distance];
        var yv = y[(pointer - yLoop + distance) % distance];

        sumX += xv;
        sumY += yv;
        sumXY += xv * yv;
        sumX2 += xv * xv;
        sumY2 += yv * yv;
      }

      var denominator =
        (n * sumX2 - sumX * sumX) *
        (n * sumY2 - sumY * sumY);

      return denominator is 0.0 ? 0.0 : (n * sumXY - sumX * sumY) / Math.Sqrt(denominator);
    }

    /// <summary>
    /// Find lag with max correlation in range [-range, range]
    /// </summary>
    /// <param name="range"></param>
    public ((int Index, double Value) Min, (int Index, double Value) Max) Max(int range)
    {
      var max = 0.0;
      var min = 0.0;
      var maxIndex = 0;
      var minIndex = 0;
      var spread = Math.Min(range, distance / 2);

      for (var i = -spread; i <= spread; i++)
      {
        var correlation = Correlation(i);

        if (correlation > 0 && correlation > max)
        {
          maxIndex = i;
          max = correlation;
        }

        if (correlation < 0 && correlation < min)
        {
          minIndex = i;
          min = correlation;
        }
      }

      return ((maxIndex, max), (minIndex, min));
    }
  }
}