using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace Estimator.Services
{
  /// <summary>
  /// OLS regressino with optional ridge regularization.
  /// </summary>
  public class RatioService
  {
    private const double Epsilon = 1e-10;

    private readonly int capacity;
    private readonly double ridge;

    private readonly Queue<(double y, Vector<double> x)> queue;

    private int count;
    private int dimension;

    private double sumY;
    private Vector<double> sumX;
    private Matrix<double> sumXX;
    private Vector<double> sumXY;


    /// <summary>
    /// Constructor with optional capacity and ridge regularization parameter.
    /// </summary>
    /// <param name="capacity"></param>
    /// <param name="ridge"></param>
    /// 0.0 Nearly identical to OLS(only Epsilon is added)
    /// 0.001	Very light regularization
    /// 0.01	Good default for financial time series
    /// 0.05	Stable hedge ratio
    /// 0.10	Strong regularization
    /// 0.50 + Betas shrink noticeably toward zero
    public RatioService(int capacity = int.MaxValue, double ridge = 0.0)
    {
      this.capacity = capacity;
      this.ridge = ridge;

      queue = new Queue<(double y, Vector<double> x)>();
    }

    public void Update(double y, params double[] x)
    {
      var xv = Vector<double>.Build.DenseOfArray(x);

      if (dimension is 0)
      {
        dimension = x.Length;

        sumX = Vector<double>.Build.Dense(dimension);
        sumXX = Matrix<double>.Build.Dense(dimension, dimension);
        sumXY = Vector<double>.Build.Dense(dimension);
      }

      if (count == capacity)
      {
        var item = queue.Dequeue();

        sumY -= item.y;
        sumX -= item.x;
        sumXY -= item.x * item.y;
        sumXX -= item.x.OuterProduct(item.x);

        count--;
      }

      queue.Enqueue((y, xv));

      sumY += y;
      sumX += xv;
      sumXY += xv * y;
      sumXX += xv.OuterProduct(xv);

      count++;
    }

    public double Spread(double y, params double[] x)
    {
      var beta = Betas();

      if (count == 0) return 0;

      var meanY = sumY / count;
      var alpha = meanY;
      var prediction = 0.0;

      for (var i = 0; i < dimension; i++)
      {
        alpha -= beta[i] * (sumX[i] / count);
        prediction += beta[i] * x[i];
      }

      prediction += alpha;

      return y - prediction;
    }

    public double[] Betas()
    {
      if (count < 2 || dimension is 0) return new double[dimension];

      var sxx = sumXX - sumX.OuterProduct(sumX) / count;
      var sxy = sumXY - sumX * (sumY / count);

      // Adaptive Ridge regularization

      var lambda = ridge * sxx.Trace() / dimension;

      // Always keep matrix strictly positive definite
      lambda = System.Math.Max(lambda, Epsilon);

      // O(N) allocation-free Ridge addition to the diagonal.
      // This directly modifies sxx, correctly applying the regularization 
      // without needing to instantiate a dense identity matrix.
      for (int i = 0; i < dimension; i++)
      {
        sxx[i, i] += lambda;
      }

      try
      {
        // Solve the linear system Sxx * Beta = Sxy to find the Beta coefficients.
        // MathNet's Solve() uses robust decomposition (like LU or QR) under the hood.
        return sxx.Solve(sxy).ToArray();
      }
      catch
      {
        // If the matrix is singular (e.g., due to collinearity, flat markets, or catastrophic cancellation), 
        // the solver will throw. We catch it and return zeros as a safe fallback to prevent crashes.
        return new double[dimension];
      }
    }
  }
}