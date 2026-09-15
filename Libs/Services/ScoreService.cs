using System;

public class ScoreService
{
  protected double alpha;
  protected double mean, variance;
  protected bool setup;

  public ScoreService(double period)
  {
    alpha = 1 - Math.Pow(0.5, 1.0 / period);
  }

  public virtual double Update(double value)
  {
    if (setup is false)
    {
      mean = value; 
      setup = true; 
      return 0;
    }

    var range = value - mean;
    var deviation = variance > 0 ? Math.Sqrt(variance) : 0;
    var score = deviation > 1e-12 ? range / deviation : 0;

    mean += alpha * range;
    variance = (1 - alpha) * (variance + alpha * range * range);

    return score;
  }
}