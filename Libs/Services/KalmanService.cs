using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace Estimator.Services
{
  public class KalmanRegression
  {
    protected double score;
    protected Vector<double> betas;
    protected Matrix<double> covariance;

    protected double processNoise; // Q
    protected double observationNoise; // R

    // Constants
    private const double Epsilon = 1e-12;
    private const double MinVariance = 1e-8;

    /// <summary>
    /// Z-Score
    /// </summary>
    public virtual double Score => score;

    /// <summary>
    /// Weights
    /// </summary>
    public virtual IList<double> Betas => betas.ToArray();

    /// <summary>
    /// Range 1e-2 to 1e-7 for noise
    /// </summary>
    /// <param name="dimension"></param>
    /// <param name="processNoise">Larger values adapt betas faster.</param>
    /// <param name="obsNoise">Smaller values increase sensitivity.</param>
    public KalmanRegression(int dimension, double processNoise = 1e-5, double obsNoise = 1e-3)
    {
      this.processNoise = processNoise;
      this.observationNoise = obsNoise;

      // Initialize State Vector (Betas)
      betas = Vector<double>.Build.Dense(dimension);

      // Initialize Covariance Matrix (P) with Identity
      covariance = Matrix<double>.Build.DenseIdentity(dimension);
    }

    /// <summary>
    /// Predict regression with current betas and new observations
    /// </summary>
    /// <param name="observations"></param>
    /// <returns></returns>
    public virtual double Predict(params double[] observations) => Vector<double>
      .Build
      .DenseOfArray(observations)
      .DotProduct(betas);

    /// <summary>
    /// Update prediction
    /// </summary>
    /// <param name="y"></param>
    /// <param name="observations"></param>
    /// <returns></returns>
    public virtual double Update(double y, double[] observations)
    {
      // Convert input array to Math.NET Vector
      var x = Vector<double>.Build.DenseOfArray(observations);

      // 1. Predict (Time Update)
      // Force covariance update to trigger betas recalculation
      // P = P + Q (Add process noise to diagonal)
      for (var i = 0; i < betas.Count; i++)
      {
        covariance[i, i] += processNoise;
      }

      // 2. Innovation 
      // yHat - predicted fair value
      // error - spread between actual observation and prediction
      // yHat = x * beta (Dot product)
      var yHat = x.DotProduct(betas);
      var error = y - yHat;

      // 3. Innovation Covariance (S)
      // Calculate which asset has highest covariance and brings most uncertainty
      // Calculate P * x (Vector)
      var px = covariance * x;

      // Calculate S = x^T * Px + R
      // Dot product of x and Px gives the scalar quadratic form
      var s = x.DotProduct(px) + observationNoise;

      // 4. Kalman Gain (K)
      // gain - smaller value means confidence and less changes to betas
      // K = Px / S
      var gain = px / Math.Max(s, Epsilon);

      // 5. Update State - betas and z-score
      // beta = beta + K * error
      betas += gain * error;
      score = error / Math.Sqrt(s);

      // 6. Joseph Form Covariance Update (Numerical Stability)
      // P = (I - KH) P (I - KH)^T + KRK^T

      // Generate I - KH
      // KH is an outer product: k (column) * h (row)
      var identity = Matrix<double>.Build.DenseIdentity(betas.Count);
      var kh = gain.OuterProduct(x);
      var iKh = identity - kh;

      // Term 1: (I - KH) * P * (I - KH)^T
      var term1 = iKh * covariance * iKh.Transpose();

      // Term 2: K * R * K^T
      // Since R is a scalar (observationNoise), this is R * (K * K^T)
      var term2 = gain.OuterProduct(gain) * observationNoise;

      // Final P update
      covariance = term1 + term2;

      // 7. Housekeeping: Force strict symmetry to clear tiny rounding drifts
      for (var i = 0; i < betas.Count; i++)
      {
        covariance[i, i] = Math.Max(covariance[i, i], MinVariance);

        for (var ii = i + 1; ii < betas.Count; ii++)
        {
          var avg = (covariance[i, ii] + covariance[ii, i]) * 0.5;

          covariance[i, ii] = avg;
          covariance[ii, i] = avg;
        }
      }

      return error;
    }
  }
}