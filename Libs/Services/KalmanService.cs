using System;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Services
{
  /// <summary>
  /// Multivariate Kalman Filter for Dynamic Regression and Adaptive Smoothing.
  /// 
  /// SCENARIOS:
  /// 1. Basket Trading (N Assets): y = b1*x1 + b2*x2 ... + bN*xN
  /// 2. Adaptive Moving Average: Set dimension = 1. Pass x = [1.0] and y = Price. 
  /// The Beta[0] will become the noise-filtered 'True Price'.
  /// </summary>
  public class KalmanRegression
  {
    protected int dimension;
    protected double[] betas;
    protected double[,] covariance;

    protected double processNoise; // Q: Higher = faster adaptation, more jitter
    protected double obsNoise;     // R: Higher = slower adaptation, smoother line

    public virtual IReadOnlyList<double> Betas => betas;

    public KalmanRegression(int dimension, double processNoise = 1e-5, double obsNoise = 1e-3)
    {
      this.dimension = dimension;
      this.processNoise = processNoise;
      this.obsNoise = obsNoise;

      betas = new double[this.dimension];
      covariance = new double[this.dimension, this.dimension];

      // Initialize uncertainty

      Enumerable.Range(0, this.dimension)
        .ToList()
        .ForEach(i => covariance[i, i] = 1.0);
    }

    /// <summary>
    /// Update model with one observation.
    /// </summary>
    /// <param name="y">Target asset price (e.g. ES)</param>
    /// <param name="x">Independent assets (e.g. NQ, RTY, or [1.0] for Moving Average)</param>
    /// <returns>The residual (Spread or Prediction Error)</returns>
    public virtual double Update(double y, double[] x)
    {
      // 1. Predict (Time Update)
      Enumerable.Range(0, dimension)
        .ToList()
        .ForEach(i => covariance[i, i] += processNoise);

      // 2. Innovation (Measurement Residual)
      var yHat = betas.Zip(x, (b, o) => b * o).Sum();
      var error = y - yHat;

      // 3. Innovation Covariance (S)
      var s = obsNoise;

      // Calculate P * x 
      var px = Enumerable.Range(0, dimension)
        .Select(i => Enumerable
          .Range(0, dimension)
          .Sum(j => covariance[i, j] * x[j]))
        .ToArray();

      // Calculate s = x * Px + R 
      s += Enumerable
        .Range(0, dimension)
        .Sum(i => x[i] * px[i]);

      // 4. Kalman Gain (K)
      var gain = Math.Abs(s) > double.Epsilon ? 
        px.Select(pxi => pxi / s).ToArray() : 
        new double[dimension];

      // 5. Update State (Beta)
      betas = betas.Zip(gain, (beta, o) => beta + o * error).ToArray();

      // 6. Update Covariance (P = P - K * Px^T)
      for (var i = 0; i < dimension; i++)
      {
        for (var ii = 0; ii < dimension; ii++)
        {
          covariance[i, ii] -= gain[i] * px[ii];
        }
      }

      return error;
    }
  }
}