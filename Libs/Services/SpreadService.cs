using MathNet.Numerics.LinearAlgebra;
using System;

namespace Estimator.Services
{
  public class SpreadService
  {
    protected double decay;
    protected double ridge;

    protected int dimension;

    // EWMA means.
    protected double meanY;
    protected Vector<double> meanX;

    // EWMA centered second moments.
    // m2XX = E[(X - meanX)(X - meanX)^T]
    // m2XY = E[(X - meanX)(Y - meanY)]
    protected Matrix<double> m2XX;
    protected Vector<double> m2XY;

    protected double[] cache;

    public SpreadService(double period, double ridge = 0)
    {
      this.decay = 1.0 - Math.Pow(0.5, 1.0 / period);
      this.ridge = ridge;
    }

    /// <summary>
    /// Updates the regression and returns the spread/residual for the
    /// current observation using parameters from before this observation.
    /// </summary>
    public virtual double? Update(double y, params double[] x)
    {
      var xv = Vector<double>.Build.DenseOfArray(x);

      if (dimension is 0)
      {
        dimension = x.Length;

        meanY = y;
        meanX = xv;

        m2XX = Matrix<double>.Build.Dense(dimension, dimension);
        m2XY = Vector<double>.Build.Dense(dimension);
      }

      var spread = ComputeSpread(y, x);

      Observe(y, x);

      return spread;
    }

    /// <summary>
    /// Incorporates one observation into the EWMA statistics.
    /// Mean: mean = (1-alpha) * oldMean + alpha * observation
    /// Covariance: C = (1-alpha) * oldC + alpha * centeredOuterProduct
    /// </summary>
    protected virtual void Observe(double y, params double[] x)
    {
      var xv = Vector<double>.Build.DenseOfArray(x);
      var dx = xv - meanX;
      var dy = y - meanY;

      // EWMA means.
      meanX += dx * decay;
      meanY += dy * decay;

      // Standard EWMA covariance updates.
      m2XX = m2XX * (1.0 - decay) + dx.OuterProduct(dx) * decay;
      m2XY = m2XY * (1.0 - decay) + dx * dy * decay;
    }

    /// <summary>
    /// Computes the centered regression residual using the current regression parameters.
    /// spread = (y - meanY) - beta' * (x - meanX)
    /// </summary>
    public virtual double? ComputeSpread(double y, params double[] x)
    {
      var beta = Betas(true);

      if (beta is null)
      {
        return null;
      }

      var spread = y - meanY;

      for (var i = 0; i < dimension; i++)
      {
        spread -= beta[i] * (x[i] - meanX[i]);
      }

      return spread;
    }

    /// <summary>
    /// Returns the current regression coefficients.
    /// The intercept is implicit through mean-centering.
    /// </summary>
    public virtual double[] Betas(bool update = false)
    {
      if (update)
      {
        cache = Solve();
      }

      return cache;
    }

    /// <summary>
    /// Solves: (m2XX + ridge * I) * beta = m2XY
    /// Cholesky is appropriate because the matrix is intended to be symmetric positive definite.
    /// </summary>
    protected virtual double[] Solve()
    {
      // With ridge = 0, use only a tiny numerical floor.
      // With ridge > 0, scale the regularization by the average variance.
      var scale = m2XX.Trace() / dimension;
      var identity = Matrix<double>.Build.DenseIdentity(dimension);
      var matrix = m2XX + identity * ridge * scale;

      // Remove numerical asymmetry accumulated through floating-point arithmetic.
      matrix = (matrix + matrix.Transpose()) * 0.5;

      try
      {
        return matrix.Cholesky().Solve(m2XY).ToArray();
      }
      catch
      {
        return null;
      }
    }
  }
}