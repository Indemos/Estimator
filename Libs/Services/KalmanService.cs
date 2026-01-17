using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace Estimator.Services
{
  public class KalmanRegression
  {
    protected Vector<double> betas;
    protected Matrix<double> covariance;

    protected double processNoise; // Q
    protected double observationNoise; // R

    // Safety constants
    private const double MinVariance = 1e-8;
    private const double Epsilon = 1e-12;

    public virtual IReadOnlyList<double> Betas => betas.ToArray();

    public virtual IReadOnlyList<double> BetaVariances => covariance.Diagonal().ToArray();

    public virtual double Predict(double[] xInput) => Vector<double>.Build.DenseOfArray(xInput).DotProduct(betas);

    public virtual double Spread(double y, double[] x) => y - Predict(x);

    public KalmanRegression(int dimension, double processNoise = 1e-5, double obsNoise = 1e-3)
    {
      this.processNoise = processNoise;
      this.observationNoise = obsNoise;

      // Initialize State Vector (Betas)
      betas = Vector<double>.Build.Dense(dimension);

      // Initialize Covariance Matrix (P) with Identity
      covariance = Matrix<double>.Build.DenseIdentity(dimension);
    }

    public virtual double Update(double y, double[] xInput)
    {
      // Convert input array to Math.NET Vector
      var x = Vector<double>.Build.DenseOfArray(xInput);

      // 1. Predict (Time Update)
      // P = P + Q (Add process noise to diagonal)
      for (var i = 0; i < betas.Count; i++)
      {
        covariance[i, i] += processNoise;
      }

      // 2. Innovation
      // yHat = x * beta (Dot product)
      var yHat = x.DotProduct(betas);
      var error = y - yHat;

      // 3. Innovation Covariance (S)
      // Calculate P * x (Vector)
      var px = covariance * x;

      // Calculate S = x^T * Px + R
      // Dot product of x and Px gives the scalar quadratic form
      var s = x.DotProduct(px) + observationNoise;

      // 4. Kalman Gain (K)
      if (s < Epsilon) s = Epsilon; // Safety clamp

      // K = Px / S
      var gain = px / s;

      // 5. Update State (Beta)
      // beta = beta + K * error
      betas += gain * error;

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
        if (covariance[i, i] < MinVariance) covariance[i, i] = MinVariance;

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