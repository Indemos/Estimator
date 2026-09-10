using System;

public class ScoreService
{
  protected double alpha; // e.g. 2.0 / (N + 1) for "N-period" convention
  protected double mean;
  protected double variance;
  protected bool setup;

  public ScoreService(double decay)
  {
    alpha = 1 - Math.Pow(0.5, 1.0 / decay);
  }

  public double Deviation => Math.Sqrt(Math.Max(0.0, variance));

  public double Score(double value)
  {
    var deviation = Deviation;
    return deviation > 1e-12 ? (value - mean) / deviation : 0.0;
  }

  public void Update(double value)
  {
    if (setup is false)
    {
      mean = value;
      variance = 0; 
      setup = true; 
      return;
    }

    var delta = value - mean;
    var increment = alpha * delta;

    mean += increment;
    variance = (1 - alpha) * (variance + delta * increment);
  }
}