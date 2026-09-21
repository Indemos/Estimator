using System;
using System.Collections.Generic;

namespace Estimator.Services
{
  /// <summary>
  /// Rolling Hayashi-Yoshida covariance/correlation estimator for two asynchronous return processes.
  /// Each observation represents a return measured over a physical time interval [Min, Max]:
  /// r_i = log(P_i / P_{i-1})
  /// For asynchronous observations, ordinary Pearson correlation is not appropriate because the observations do not occur at identical times.
  /// The Hayashi-Yoshida estimator estimates covariance by summing products of returns whose observation intervals overlap:
  /// HY(X,Y) = Σ rX_i * rY_j * I(interval X_i overlaps interval Y_j)
  /// For a time shift τ, X intervals are shifted by τ:
  /// [X.Min + τ, X.Max + τ]
  /// and the same overlap rule is applied.
  /// The resulting correlation is:
  /// ρ(τ) = HY(X,Y;τ) / sqrt(RV_X * RV_Y)
  /// where:
  /// RV_X = Σ rX_i²
  /// RV_Y = Σ rY_j²
  /// The realized variances do not depend on τ, so they are calculated once for the current rolling window.
  /// </summary>
  public class LeadService
  {
    protected struct Interval
    {
      public long Min;
      public long Max;
      public double Value;
    }

    protected readonly Queue<Interval> groupX;
    protected readonly Queue<Interval> groupY;

    public long Frame { get; }

    /// <summary>
    /// True when both series contain at least one return interval.
    /// </summary>
    public bool IsReady => groupX.Count > 0 && groupY.Count > 0;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="capacity"></param>
    public LeadService(long frame, int capacity = 1000)
    {
      Frame = frame;

      groupX = new Queue<Interval>(capacity);
      groupY = new Queue<Interval>(capacity);
    }

    /// <summary>
    /// Adds one completed X return interval.
    /// The caller supplies the physical interval over which the return was measured and the return itself.
    /// </summary>
    /// <param name="min">Start time</param>
    /// <param name="max">End time</param>
    /// <param name="value">Return within specified time</param>
    public virtual void UpdateX(long min, long max, double value)
    {
      if (max > min)
      {
        groupX.Enqueue(new Interval { Min = min, Max = max, Value = value });
      }
    }

    /// <summary>
    /// Adds one completed Y return interval.
    /// </summary>
    /// <param name="min">Start time</param>
    /// <param name="max">End time</param>
    /// <param name="value">Return within specified time</param>
    public virtual void UpdateY(long min, long max, double value)
    {
      if (max > min)
      {
        groupY.Enqueue(new Interval { Min = min, Max = max, Value = value });
      }
    }

    /// <summary>
    /// Removes intervals that are completely outside the rolling window.
    /// The window is defined relative to the supplied physical timestamp:
    /// [timestamp - Frame, timestamp]
    /// An interval is removed when its end is at or before the window start.
    /// </summary>
    public virtual void Trim(long stamp)
    {
      var mark = stamp - Frame;

      while (groupX.Count > 0 && groupX.Peek().Max <= mark)
      {
        groupX.Dequeue();
      }

      while (groupY.Count > 0 && groupY.Peek().Max <= mark)
      {
        groupY.Dequeue();
      }
    }

    /// <summary>
    /// Calculates the Hayashi-Yoshida covariance for a specified physical time shift.
    /// Positive shift moves X forward in time:
    /// X'[t] = X[t - shift]
    /// Therefore, a positive shift means that X returns occurred earlier than the corresponding Y returns — i.e. X leads Y.
    /// HY covariance:
    /// Cov_HY(τ) = Σ X_i * Y_j
    /// for every pair whose shifted physical intervals overlap.
    /// Returns null when there is insufficient data.
    /// </summary>
    public virtual double? Covariance(long range)
    {
      if (IsReady is false)
      {
        return null;
      }

      // Queue<T> has no indexer, so the two-pointer merge walks each
      // queue's own enumerator instead of an index - same O(N+M) sweep,
      // just advanced with MoveNext() instead of i++/modulo. Both
      // enumerators are structs (no heap allocation), and neither queue is
      // mutated during the sweep, so this is safe.
      var ex = groupX.GetEnumerator();
      var ey = groupY.GetEnumerator();
      var hasX = ex.MoveNext();
      var hasY = ey.MoveNext();
      var covariance = 0.0;

      while (hasX && hasY)
      {
        var x = ex.Current;
        var y = ey.Current;

        // Shift the X observation interval by τ.
        var xMin = x.Min + range;
        var xMax = x.Max + range;

        // Hayashi-Yoshida covariance includes the product of two returns
        // whenever their observation intervals overlap:
        // xMin < yMax && yMin < xMax
        if (xMin < y.Max && y.Min < xMax)
        {
          covariance += x.Value * y.Value;
        }

        // Move past whichever interval ends first.
        // If both end at exactly the same time, both can be advanced because
        // neither can overlap another interval after its endpoint.
        var cmp = xMax.CompareTo(y.Max);

        if (cmp <= 0) hasX = ex.MoveNext();
        if (cmp >= 0) hasY = ey.MoveNext();
      }

      ex.Dispose();
      ey.Dispose();

      return covariance;
    }

    /// <summary>
    /// Calculates realized variance of X:
    /// RV_X = Σ X_i²
    /// This quantity is invariant to the time shift applied to X.
    /// </summary>
    public virtual double VarianceX()
    {
      var variance = 0.0;

      foreach (var interval in groupX)
      {
        variance += interval.Value * interval.Value;
      }

      return variance;
    }

    /// <summary>
    /// Calculates realized variance of Y:
    /// RV_Y = Σ Y_i²
    /// </summary>
    public virtual double VarianceY()
    {
      var variance = 0.0;

      foreach (var interval in groupY)
      {
        variance += interval.Value * interval.Value;
      }

      return variance;
    }

    /// <summary>
    /// Calculates normalized Hayashi-Yoshida correlation:
    /// ρ(τ) = HY(X,Y;τ) / sqrt(RV_X * RV_Y)
    /// Returns null when there is insufficient data or either realized
    /// variance is zero.
    /// </summary>
    public virtual double? Correlation(long range)
    {
      if (IsReady is false)
      {
        return null;
      }

      var varianceX = VarianceX();
      var varianceY = VarianceY();
      var denominator = Math.Sqrt(varianceX * varianceY);

      if (denominator <= 0.0)
      {
        return null;
      }

      return Covariance(range).Value / denominator;
    }
  }
}