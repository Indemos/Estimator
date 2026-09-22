using System;

namespace Estimator.Services
{
  /// <summary>
  /// EWMA Welford / Pebay statistics.
  /// New observations receive exponentially greater weight.
  /// Maintains exponentially weighted population moments: mean, variance, skewness and excess kurtosis.
  /// </summary>
  public class VarianceService
  {
    protected readonly double wd; // 1 - alpha
    protected readonly double w;  // alpha

    protected double mean;
    protected double m2;
    protected double m3;
    protected double m4;

    protected long count;
    protected bool setup;

    public virtual double Mean => mean;
    public virtual double Variance => count > 1 ? m2 : 0;
    public virtual double Deviation => count > 1 && m2 > 0 ? Math.Sqrt(m2) : 0;
    public virtual double Skewness => count > 2 && m2 > 0 ? m3 / Math.Pow(m2, 1.5) : 0;
    public virtual double Kurtosis => count > 3 && m2 > 0 ? m4 / (m2 * m2) - 3.0 : 0;
    public virtual double HalfLife => wd > 0 && wd < 1 ? Math.Log(0.5) / Math.Log(wd) : 0;

    public VarianceService(double alpha = 0.05)
    {
      w = alpha;
      wd = 1 - alpha;
    }

    public static VarianceService FromPeriod(int period)
    {
      return new VarianceService(2.0 / (period + 1));
    }

    public static VarianceService FromDecay(double halfLife)
    {
      return new VarianceService(1.0 - Math.Exp(Math.Log(0.5) / halfLife));
    }

    public virtual VarianceService Update(double value)
    {
      count++;

      if (setup is false)
      {
        m2 = 0;
        m3 = 0;
        m4 = 0;
        mean = value;
        setup = true;

        return this;
      }

      var delta = value - mean;
      var prevM2 = m2;
      var prevM3 = m3;
      var prevM4 = m4;

      // Update mean.
      mean += w * delta;

      var d2 = delta * delta;
      var d3 = d2 * delta;
      var d4 = d2 * d2;

      // Exponentially weighted central moments.

      m2 =
        wd * prevM2
        + w * wd * d2;

      m3 =
        wd * prevM3
        - 3.0 * w * wd * delta * prevM2
        + w * wd * (wd - w) * d3;

      m4 =
        wd * prevM4
        - 4.0 * w * wd * delta * prevM3
        + 6.0 * w * w * wd * d2 * prevM2
        + w * wd * (1.0 - 3.0 * w * wd) * d4;

      // Guard against tiny negative variance from floating-point error.

      if (m2 < 0)
      {
        m2 = 0;
      }

      return this;
    }

    /// <summary>
    /// Standardized value using the current EWMA mean and deviation.
    /// </summary>
    public virtual double Score(double value)
    {
      var deviation = Deviation;
      return deviation > 0 ? (value - mean) / deviation : 0;
    }
  }
}
