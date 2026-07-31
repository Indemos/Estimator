using System;
using System.Collections.Generic;

namespace Estimator.Services
{
  /// <summary>
  /// Multi-asset Ornstein-Uhlenbeck Spread with fully iterative parameter estimation.
  /// Tracks cointegration dynamically using a Kalman Filter and maps discrete AR(1) 
  /// properties to continuous OU diffusion parameters.
  /// </summary>
  public class MeanService
  {
    // Continuous-time OU Parameters
    public double Mu { get; private set; }    // Equilibrium mean (intercept)
    public double Theta { get; private set; } // Speed of mean reversion
    public double Sigma { get; private set; } // Continuous diffusion volatility

    private readonly int assets;
    private readonly bool interception;
    private readonly KalmanService regression;

    // Exponential forgetting factors for non-stationary market adaptation
    private readonly double weight;           // Weights historical covariance
    private readonly double sigmaWeight;      // Weights historical residuals

    private double currentSpread = double.NaN;
    private double step = 1.0;                // Time step increment (e.g., 1/390 for minute bars)

    // Online AR(1) statistics variables (Welford's algorithm states)
    private double meanX = 0.0;
    private double meanY = 0.0;
    private double sumXX = 0.0;
    private double sumXY = 0.0;
    private double memorySize = 0.0;
    private double emaVariance = 0.0;
    private double barrier = 1e-8;

    public MeanService(
      int assets,
      bool interception = true,
      double processNoise = 1e-5,
      double observationNoise = 1e-3,
      double weight = 0.99,
      double sigmaWeight = 0.95,
      double step = 1.0)
    {
      this.assets = assets;
      this.interception = interception;
      this.weight = weight;
      this.sigmaWeight = sigmaWeight;
      this.step = step;

      var dimension = interception ? assets + 1 : assets;
      regression = new KalmanService(dimension, processNoise, observationNoise);

      // Safe defaults before statistical significance is reached
      Mu = 0.0;
      Sigma = 0.1;
      Theta = 0.05;
      emaVariance = 0.0001;
    }

    public double Update(double priceTarget, double[] prices)
    {
      // Step 1: Construct the observation array for the Kalman Filter
      var observations = new List<double>();

      if (interception)
      {
        observations.Add(1.0); // Dynamic intercept beta
      }

      observations.AddRange(prices);

      // Step 2: Update the Kalman state with the new target and hedge prices
      regression.Update(priceTarget, observations.ToArray());

      // Step 3: Calculate the current spread (Error between actual and predicted target)
      var prediction = regression.Predict(observations.ToArray());
      var spread = priceTarget - prediction;

      // Step 4: Feed the new spread into the AR(1) / OU statistical engine
      UpdateOnlineStatistics(spread);

      return spread;
    }

    private void UpdateOnlineStatistics(double spread)
    {
      // Initialization condition: Need at least one prior spread to form an AR(1) pair
      if (double.IsNaN(currentSpread))
      {
        currentSpread = spread;
        return;
      }

      // x = Spread(t-1), y = Spread(t)
      var x = currentSpread;
      var y = spread;

      // Step 1: Update the effective sample size using exponential decay
      // This bounds the memory of the model so it adapts to regime changes.
      memorySize = weight * memorySize + 1.0;

      // Step 2: Calculate the pre-update innovations (deltas)
      // It is critical to calculate these BEFORE updating _meanX and _meanY 
      // for mathematically correct Welford updates.
      var deltaX = x - meanX;
      var deltaY = y - meanY;

      // Step 3: Update the running means
      meanX += deltaX / memorySize;
      meanY += deltaY / memorySize;

      // Step 4: Update the cross-covariance and variance sums (EWMA Welford)
      // We scale the delta product by the Welford weight (1 - 1/N) to prevent variance drift.
      var welfordWeight = 1.0 - (1.0 / memorySize);
      
      sumXX = weight * sumXX + welfordWeight * deltaX * deltaX;
      sumXY = weight * sumXY + welfordWeight * deltaX * deltaY;

      // Step 5: Calculate the AR(1) regression coefficient (Phi)
      // Phi represents the slope of the regression of Spread(t) against Spread(t-1).
      var phi = sumXX > 1e-12 ? sumXY / sumXX : 0.0;

      // Step 6: Calculate the AR(1) intercept
      // Using the post-update means ensures structural alignment with the current state.
      var intercept = meanY - phi * meanX;

      // Step 7: Form the AR(1) prediction and extract the pure residual (noise)
      var predicted = intercept + phi * x;
      var residual = y - predicted;

      // Step 8: Smooth the residual variance using its own EWMA factor
      // This represents the discrete variance of the AR(1) process errors.
      emaVariance = sigmaWeight * emaVariance + (1.0 - sigmaWeight) * residual * residual;

      // Shift the state forward
      currentSpread = spread;
    }

    public void UpdateOUParameters()
    {
      // Gatekeeper: Require a minimum effective sample size to prevent erratic early parameters
      if (sumXX < barrier)
      {
        return;
      }

      // Extract the AR(1) regression coefficient
      var phi = sumXY / sumXX;

      // Validate that the process is currently mean-reverting
      // Phi must be between 0 (white noise) and 1 (random walk).
      if (phi > 0 && phi < 1)
      {
        // Step 1: Calculate continuous speed of reversion (Theta)
        // -Math.Log(phi) maps the discrete slope to a continuous rate.
        // Dividing by _dt scales it to the chosen timeframe (e.g., annualized).
        Theta = -Math.Log(phi) / step;

        // Step 2: Calculate the equilibrium mean (Mu)
        // Maps the discrete AR(1) intercept to the continuous structural mean.
        Mu = (meanY - phi * meanX) / (1.0 - phi);

        // Step 3: Calculate the continuous diffusion volatility (Sigma)
        // We must transform the discrete AR(1) error variance into continuous OU variance.
        var varianceScaleFactor = (2.0 * Theta) / (1.0 - (phi * phi));
        Sigma = Math.Sqrt(Math.Max(emaVariance * varianceScaleFactor, barrier));
      }
      else
      {
        // Fallback: The spread is behaving like an unconstrained Random Walk
        // Scale variance back up by step to keep continuous-time parameter matching invariant
        Mu = meanY;
        Theta = 0.0;
        Sigma = Math.Sqrt(Math.Max(emaVariance / step, barrier));
      }
    }

    /// <summary>
    /// Calculates the equilibrium standard deviation of the spread itself.
    /// This is the structural width of the trade envelope, dictated by the balance 
    /// between the diffusion noise (Sigma) and the reversion pull (Theta).
    /// </summary>
    public double SpreadStdDev => Math.Sqrt((Sigma * Sigma) / (2.0 * Math.Max(Theta, double.Epsilon)));

    public double GetSpeed() => Math.Log(2.0) / Math.Max(Theta, double.Epsilon);
  }
}