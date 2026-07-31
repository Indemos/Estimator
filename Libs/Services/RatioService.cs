using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;

namespace Estimator.Services
{
  /// <summary>
  /// OLS for beta weights
  /// </summary>
  public class RatioService
  {
    // Maximum number of historical observations to retain in the rolling window.
    private readonly int capacity;

    // Queue storing the historical data points: dependent variable (y) and independent variables (x).
    private readonly Queue<(double y, Vector<double> x)> queue;

    // Current number of observations in the window.
    private int count;

    // Number of independent variables (factors) in the model.
    private int dimension;

    // Running sums used for the "one-pass" Ordinary Least Squares (OLS) calculation.
    private double sumY;                      // Sum of all y values: Σy
    private Vector<double> sumX;              // Sum of all x vectors: Σx
    private Matrix<double> sumXX;             // Sum of outer products of x vectors: Σ(x * x^T)
    private Vector<double> sumXY;             // Sum of x vectors multiplied by y: Σ(x * y)

    /// <summary>
    /// Constructor with capacity of the rollign window
    /// </summary>
    /// <param name="capacity"></param>
    public RatioService(int capacity = int.MaxValue)
    {
      this.capacity = capacity;
      this.queue = new Queue<(double y, Vector<double> x)>();
    }

    public void Update(double y, params double[] x)
    {
      // Convert the incoming array to a Math.NET Vector to enable matrix/vector operations.
      var xv = Vector<double>.Build.DenseOfArray(x);

      // On the very first data point, initialize the dimension and allocate the running sum structures.
      if (dimension == 0)
      {
        dimension = x.Length;
        sumX = Vector<double>.Build.Dense(dimension);
        sumXX = Matrix<double>.Build.Dense(dimension, dimension);
        sumXY = Vector<double>.Build.Dense(dimension);
      }

      // If the rolling window has reached its maximum capacity, evict the oldest observation.
      if (count == capacity)
      {
        var current = queue.Dequeue();

        // Subtract the evicted observation's contribution from the running sums to maintain accuracy.
        sumY -= current.y;
        sumX -= current.x;
        sumXY -= current.x * current.y;
        sumXX -= current.x.OuterProduct(current.x);
        count--;
      }

      // Add the new observation to the back of the queue.
      queue.Enqueue((y, xv));

      // Add the new observation's contribution to the running sums.
      sumY += y;
      sumX += xv;
      sumXY += xv * y;
      sumXX += xv.OuterProduct(xv);
      count++;
    }

    // Calculates the spread (residual) for a new observation using the current rolling model.
    public double Spread(double y, params double[] x)
    {
      var beta = Betas();
      var meanY = count > 0 ? sumY / count : 0;
      var dotMeanX = 0.0;
      var dotCurrentX = 0.0;

      // Calculate two dot products:
      // 1. mean(X) · Beta (used to find Alpha)
      // 2. current(X) · Beta (used to predict current Y)
      for (var i = 0; i < dimension; i++)
      {
        dotMeanX += (sumX[i] / count) * beta[i];
        dotCurrentX += beta[i] * x[i];
      }

      // Calculate Alpha based on historical means.
      double alpha = meanY - dotMeanX;

      // Calculate the predicted Y for the current X: Predicted Y = Alpha + (current(X) · Beta)
      double pred = alpha + dotCurrentX;

      // The spread (or error/residual) is the actual observed Y minus the predicted Y.
      return y - pred;
    }

    // Internal method to solve for the Beta coefficients using the Normal Equation.
    public double[] Betas()
    {
      // A linear regression requires at least 2 data points, and the dimension must be defined.
      if (count < 2 || dimension == 0) return new double[dimension];

      // Calculate the centered covariance matrix Sxx = Σ(xx^T) - (Σx * Σx^T) / N
      // This "one-pass" formula is computationally efficient but highly susceptible to 
      // catastrophic cancellation (loss of precision) when dealing with large price values.
      var sxx = sumXX - sumX.OuterProduct(sumX) / count;

      // Calculate the centered covariance vector Sxy = Σ(xy) - (Σx * Σy) / N
      var sxy = sumXY - sumX * (sumY / count);

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