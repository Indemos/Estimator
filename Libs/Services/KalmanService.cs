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
    public KalmanService(int dimension, double processNoise = 0.00001, double obsNoise = 0.001)
    {
      this.processNoise = processNoise;
      this.observationNoise = obsNoise;

      betas = Vector<double>.Build.Dense(dimension, 0);
      covariance = Matrix<double>.Build.DenseIdentity(dimension);
    }

    /// <summary>
    /// Betas
    /// </summary>
    /// <param name="observations"></param>
    public virtual double Predict(params double[] observations) => Vector<double>
      .Build
      .DenseOfArray(observations)
      .DotProduct(betas);

    /// <summary>
    /// Update observations
    /// </summary>
    /// <param name="y"></param>
    /// <param name="observations"></param>
    public virtual double? Update(double y, params double[] observations)
    {
      var x = Vector<double>.Build.DenseOfArray(observations);

      // 1. Time update
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

      var sSafe = Math.Max(s, Epsilon);
      var gain = px / sSafe;

      // 4. State update
      betas += gain * error;
      innovationScore = error / Math.Sqrt(sSafe);

      // 5. Joseph form
      var identity = Matrix<double>.Build.DenseIdentity(betas.Count);
      var kh = gain.OuterProduct(x);
      var iKh = identity - kh;
      var term1 = iKh * covariance * iKh.Transpose();
      var term2 = gain.OuterProduct(gain) * observationNoise;

      covariance = term1 + term2;

      if (setup is false)
      {
        setup = true;
        return null;
      }

      return error;
    }
  }
}