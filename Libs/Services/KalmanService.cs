using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace Estimator.Services
{
  public class KalmanService
  {
    protected const double Epsilon = 1e-12;
    protected const double MinVariance = 1e-8;

    protected Vector<double> betas;
    protected Matrix<double> covariance;
    protected double processNoise;
    protected double observationNoise;
    protected double innovationScore;
    protected double maxCovariance;
    protected bool setup;

    /// <summary>
    /// Weights
    /// </summary>
    public virtual IList<double> Betas => betas.ToArray();

    /// <summary>
    /// Range 1e-2 to 1e-7 for noise
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="processNoise">Larger values adapt betas faster and ignore history.</param>
    /// <param name="obsNoise">Smaller values increase sensitivity.</param>
    public KalmanService(int dimension, double processNoise = 1e-5, double obsNoise = 1e-3, double maxCovariance = 1e2)
    {
      this.processNoise = processNoise;
      this.observationNoise = obsNoise;
      this.maxCovariance = maxCovariance;

      betas = Vector<double>.Build.Dense(dimension, 1);
      covariance = Matrix<double>.Build.DenseIdentity(dimension);
    }

    public virtual double Predict(params double[] observations) => Vector<double>
      .Build
      .DenseOfArray(observations)
      .DotProduct(betas);

    public virtual double Update(double y, params double[] observations)
    {
      var x = Vector<double>.Build.DenseOfArray(observations);

      // 1. Time update - skip Q when no observation to avoid unbounded growth
      for (var i = 0; i < betas.Count; i++)
      {
        covariance[i, i] += processNoise;
      }

      // 2. Innovation
      var yHat = x.DotProduct(betas);
      var error = y - yHat;

      // 3. Innovation covariance
      var px = covariance * x;
      var s = x.DotProduct(px) + observationNoise;

      // Safe S for both gain and score
      var sSafe = Math.Max(s, Epsilon);
      var gain = px / sSafe;

      // 4. State update
      betas += gain * error;
      innovationScore = error / Math.Sqrt(sSafe);

      // 5. Joseph form - preserves PSD in exact math
      var identity = Matrix<double>.Build.DenseIdentity(betas.Count);
      var kh = gain.OuterProduct(x);
      var iKh = identity - kh;
      var term1 = iKh * covariance * iKh.Transpose();
      var term2 = gain.OuterProduct(gain) * observationNoise;

      covariance = term1 + term2;

      // 6. Housekeeping - PSD preserving
      // 6a. Enforce symmetry to clean FP drift
      for (var i = 0; i < betas.Count; i++)
      {
        for (var ii = i + 1; ii < betas.Count; ii++)
        {
          var avg = (covariance[i, ii] + covariance[ii, i]) * 0.5;
          covariance[i, ii] = avg;
          covariance[ii, i] = avg;
        }
      }

      // 6b. Trace cap - scaling preserves PSD: c*P stays PSD if P is PSD
      var trace = covariance.Trace();
      var maxTrace = maxCovariance * betas.Count;

      if (trace > maxTrace && trace > 0)
      {
        covariance *= maxTrace / trace;
      }

      // 6c. Per-dim cap preserving correlation - D*P*D preserves PSD
      // Instead of: P[i,i] = min(P[i,i], max) which breaks PSD like [1000 990; 990 1000] -> [100 990; 990 100]
      for (var i = 0; i < betas.Count; i++)
      {
        if (covariance[i, i] > maxCovariance)
        {
          var scale = Math.Sqrt(maxCovariance / covariance[i, i]);

          for (var ii = 0; ii < betas.Count; ii++)
          {
            covariance[i, ii] *= scale;
            covariance[ii, i] *= scale;
          }

          // Floor to avoid singularity
          if (covariance[i, i] < MinVariance)
          {
            covariance[i, i] = MinVariance;
          }
        }
      }

      if (setup is false)
      {
        setup = true;
        return 0;
      }

      return error;
    }
  }
}