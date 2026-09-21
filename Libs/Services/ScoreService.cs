using System;

public class ScoreService
{
  protected bool setup;
  protected double alpha;
  protected double mean, variance;

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

    // EWMA mean: mean[t] = mean[t - 1] + alpha * (value - mean[t - 1]) = (1 - alpha) * mean[t - 1] + alpha * value
    // EWMA variance: Welford, always PSD - positive semi-definite
    // increment = alpha*range, so range*increment = alpha*range^2
    // var[t] = (1 - alpha) * (var[t - 1] + alpha * range^2)
    // (1 - alpha) decays old moments, alpha*range^2 blends new info

    mean += alpha * range;
    variance = (1 - alpha) * (variance + alpha * range * range);

    return score;
  }
}