using System;
using System.Collections.Generic;

namespace Estimator.Services
{
  public class ScoreService
  {
    protected const double Epsilon = 1e-12;

    private readonly Queue<double> queue;
    private readonly int capacity;
    private double meanSquare;
    private double mean;
    private int count;

    public ScoreService(int capacity)
    {
      this.capacity = capacity;
      this.queue = new Queue<double>(capacity);
    }

    public double Deviation => Math.Sqrt(count > 1 ? Math.Max(0.0, meanSquare) / (count - 1) : 0.0);

    public double Score(double value)
    {
      var deviation = Deviation;
      return deviation > Epsilon ? (value - mean) / deviation : 0.0;
    }

    public void Update(double value)
    {
      if (count == capacity)
      {
        var previous = queue.Dequeue();
        var previousMean = mean;

        mean = (mean * count - previous) / (count - 1);
        meanSquare -= (previous - previousMean) * (previous - mean);
        count--;
      }

      var delta = value - mean;

      count++;
      mean += delta / count;
      meanSquare += delta * (value - mean);
      queue.Enqueue(value);
    }
  }
}