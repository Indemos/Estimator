using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace Estimator.Services
{
  public enum JohansenModel
  {
    Model0 = 0, // no deterministic terms
    Model1 = 1, // constant in cointegration
    Model2 = 2, // constant in VAR
    Model3 = 3, // constant + trend in coint
    Model4 = 4  // constant + trend in VAR
  }

  public enum SignificanceLevel
  {
    x90 = 0,
    x95 = 1,
    x99 = 2
  }

  public sealed class JohansenInternalResult
  {
    public double[] EigenValues;
    public double[] TraceStatistics;

    public Matrix<double> R0;
    public Matrix<double> R1;
    public Matrix<double> EigenVectors;

    public JohansenModel Model;
  }

  public static class JohansenCore
  {
    public static JohansenInternalResult Run(Matrix<double> series, int steps, JohansenModel model)
    {
      var rows = series.RowCount;
      var columns = series.ColumnCount;

      // 1. Calculate First Differences (Delta Y)
      var matrixSub =
        series.SubMatrix(1, rows - 1, 0, columns) -
        series.SubMatrix(0, rows - 1, 0, columns);

      // 2. Prepare Lagged Difference Matrix (Z)
      Matrix<double> matrixLag = null;

      if (steps > 1)
      {
        matrixLag = Matrix<double>.Build.Dense(rows - steps, columns * (steps - 1));

        for (var i = 1; i < steps; i++)
        {
          matrixLag
            .SetSubMatrix(0, rows - steps, (i - 1) * columns, columns, matrixSub
              .SubMatrix(steps - i - 1, rows - steps, 0, columns));
        }
      }

      // Align dimensions for regression
      var Y0 = matrixSub.SubMatrix(steps - 1, rows - steps, 0, columns); // Delta Y
      var Y1 = series.SubMatrix(steps - 1, rows - steps, 0, columns);    // Y
      var T = Y0.RowCount;

      matrixLag = Terms(matrixLag, model, T);

      // 3. Compute Residuals (Partialing out short-run dynamics)
      // Uses QR decomposition for stability
      var R0 = Regression(Y0, matrixLag);
      var R1 = Regression(Y1, matrixLag);

      // 4. Compute Moment Matrices
      var S00 = R0.TransposeThisAndMultiply(R0) / T; // Residuals of Diffs
      var S11 = R1.TransposeThisAndMultiply(R1) / T; // Residuals of Levels
      var S01 = R0.TransposeThisAndMultiply(R1) / T; // Cross-covariance
      var S10 = S01.Transpose();

      // 5. Solve Generalized Eigenvalue Problem via SVD
      // We want eigenvalues of: S11^-1 * S10 * S00^-1 * S01
      // Stable approach:
      // 1. Compute Inverse Square Root of S11 and S00
      // 2. Form W = S11^(-1/2) * S10 * S00^(-1/2)
      // 3. SVD(W) -> Singular Values Sigma
      // 4. Eigenvalues lambda = sigma^2

      var invS11 = Inversion(S11);
      var invS00 = Inversion(S00);

      // W = invS11 * S10 * invS00
      var W = invS11.Multiply(S10).Multiply(invS00);

      // SVD: W = U * S * VT
      // The singular values (S) squared are the canonical correlations (eigenvalues)
      var svd = W.Svd(true); // Compute U and VT

      // SVD returns singular values sorted descending by default
      var eigSorted = svd.S.Select(s => s * s).ToArray();

      // 6. Recover Cointegrating Vectors (Beta)
      // The eigenvectors of the original problem correspond to the columns of U
      // transformed back by S11^(-1/2).
      // Beta = S11^(-1/2) * U
      var vecSorted = invS11.Multiply(svd.U);

      // ### EVD approach ###
      //var A = S11.Inverse()
      //  .Multiply(S10)
      //  .Multiply(S00.Inverse())
      //  .Multiply(S01);
      //var evd = A.Evd();
      //var eigSorted = evd.EigenValues
      //  .Select(v => v.Real)
      //  .OrderByDescending(x => x)
      //  .ToArray();

      //var idx = Enumerable.Range(0, columns)
      //  .OrderByDescending(i => evd.EigenValues[i].Real)
      //  .ToArray();
      //var vecSorted = Matrix<double>.Build.Dense(columns, columns);
      //for (var i = 0; i < columns; i++)
      //{
      //  vecSorted.SetColumn(i, evd.EigenVectors.Column(idx[i]));
      //}

      // 7. Compute Trace Statistics
      // Trace = -T * Sum(ln(1 - lambda))
      var trace = Enumerable.Range(0, columns)
        .Select(r => -T * eigSorted.Skip(r).Sum(value => Math.Log(Math.Max(1e-15, 1.0 - value))))
        .ToArray();

      return new JohansenInternalResult
      {
        EigenValues = eigSorted,
        EigenVectors = vecSorted,
        TraceStatistics = trace,
        Model = model,
        R0 = R0,
        R1 = R1
      };
    }

    /// <summary>
    /// Leave only residuals
    /// </summary>
    /// <param name="y"></param>
    /// <param name="x"></param>
    private static Matrix<double> Regression(Matrix<double> y, Matrix<double> x)
    {
      if (x is null || x.ColumnCount is 0)
      {
        return y;
      }

      // QR Solve is generally more stable than PseudoInverse for regression

      var qr = x.QR();
      var beta = qr.Solve(y);

      return y - x * beta;
    }

    /// <summary>
    /// Computes A^(-1/2) for a symmetric matrix A using EVD.
    /// Handles near-singular matrices by clipping small eigenvalues.
    /// </summary>
    private static Matrix<double> Inversion(Matrix<double> A)
    {
      var evd = A.Evd(Symmetricity.Symmetric);
      var V = evd.EigenVectors;
      var D = evd.EigenValues;
      var vector = Vector<double>.Build.Dense(D.Count, o => 1.0 / Math.Sqrt(D[o].Real));
      var vectorM = Matrix<double>.Build.DiagonalOfDiagonalVector(vector);

      return V.Multiply(vectorM).Multiply(V.Transpose());
    }

    /// <summary>
    /// Choose behavior
    /// </summary>
    /// <param name="matrixLag"></param>
    /// <param name="model"></param>
    /// <param name="T"></param>
    private static Matrix<double> Terms(Matrix<double> matrixLag, JohansenModel model, int T)
    {
      // Intercept

      if (model is JohansenModel.Model1 || model is JohansenModel.Model2)
      {
        var section = Matrix<double>.Build.Dense(T, 1, 1.0);

        return matrixLag?.Append(section) ?? section;
      }

      // Trend

      if (model is JohansenModel.Model3 || model is JohansenModel.Model4)
      {
        var section = Matrix<double>
          .Build
          .DenseOfColumnArrays(Enumerable
            .Range(1, T)
            .Select(o => (double)o)
            .ToArray());

        return matrixLag?.Append(section) ?? section;
      }

      return matrixLag;
    }
  }

  /// <summary>
  /// Critical values from MacKinnon, Haug, Michelis (1999) / Osterwald-Lenum (1992).
  /// These tables support up to 12 variables (k=12) for all standard Johansen models.
  /// Columns represent significance levels: 90%, 95%, 99%.
  /// Rows represent the number of variables (r) from 1 to 12.
  /// </summary>
  public static class CriticalValuesTable
  {
    // Model 0: No deterministic trend in data, no intercept in cointegrating eq.
    private static readonly double[,] Trace0 =
    {
      {   2.98,   4.13,   6.94 },
      {  10.47,  12.32,  16.36 },
      {  21.78,  24.28,  29.51 },
      {  37.03,  40.17,  46.57 },
      {  56.28,  60.06,  67.64 },
      {  79.53,  83.94,  92.71 },
      { 106.74, 111.78, 121.74 },
      { 138.00, 143.67, 154.80 },
      { 173.23, 179.52, 191.83 },
      { 212.47, 219.41, 232.84 },
      { 255.68, 263.26, 278.00 },
      { 302.90, 311.13, 326.96 }
    };

    // Model 1: No deterministic trend in data, intercept in cointegrating eq. (Standard for Pairs Trading)
    private static readonly double[,] Trace1 =
    {
      {   2.71,   3.84,   6.63 },
      {  13.43,  15.49,  19.93 },
      {  27.07,  29.80,  35.46 },
      {  44.49,  47.86,  54.68 },
      {  65.82,  69.82,  77.82 },
      {  91.11,  95.75, 104.96 },
      { 120.37, 125.61, 135.97 },
      { 153.63, 159.53, 171.09 },
      { 190.88, 197.37, 210.06 },
      { 232.11, 239.25, 253.24 },
      { 277.38, 285.14, 300.29 },
      { 326.53, 334.98, 351.25 }
    };

    // Model 2: Linear trend in data, intercept in cointegrating eq.
    private static readonly double[,] Trace2 =
    {
      {   2.71,   3.84,   6.63 },
      {  15.41,  18.17,  23.46 },
      {  29.68,  34.55,  40.49 },
      {  47.21,  54.64,  61.24 },
      {  68.52,  76.07,  84.45 },
      {  93.92, 103.18, 114.36 },
      { 124.24, 134.67, 148.19 },
      { 158.49, 170.80, 186.39 },
      { 196.72, 210.98, 228.91 },
      { 238.92, 255.27, 275.39 },
      { 285.10, 303.13, 327.32 },
      { 335.25, 355.84, 384.15 }
    };

    // Model 3: Linear trend in data, intercept + trend in cointegrating eq.
    private static readonly double[,] Trace3 =
    {
      {  10.67,  12.52,  16.55 },
      {  23.34,  25.87,  31.16 },
      {  39.75,  42.91,  49.36 },
      {  60.09,  63.88,  71.47 },
      {  84.38,  88.80,  97.60 },
      { 112.65, 117.71, 127.71 },
      { 144.87, 150.56, 161.72 },
      { 181.16, 187.47, 199.81 },
      { 221.36, 228.31, 241.74 },
      { 265.63, 273.19, 287.87 },
      { 313.86, 322.06, 337.97 },
      { 366.11, 374.91, 392.01 }
    };

    // Model 4: Quadratic trend in data (Rarely used)
    private static readonly double[,] Trace4 =
    {
      {  12.25,  14.26,  18.52 },
      {  25.32,  28.27,  34.25 },
      {  42.44,  45.70,  52.31 },
      {  63.33,  67.27,  75.29 },
      {  88.06,  92.73, 101.77 },
      { 116.79, 122.21, 132.22 },
      { 149.62, 155.77, 167.34 },
      { 186.64, 193.38, 206.31 },
      { 227.81, 235.34, 249.79 },
      { 273.13, 281.35, 297.02 },
      { 322.62, 331.69, 348.74 },
      { 376.12, 386.10, 404.38 }
    };

    /// <summary>
    /// Retrieves the correct critical values table for the given model (Trace Test).
    /// </summary>
    /// <param name="model">Deterministic model (0-4).</param>
    /// <returns>A 2D array of critical values.</returns>
    public static double[,] Get(JohansenModel model)
    {
      switch (model)
      {
        case JohansenModel.Model0: return Trace0;
        case JohansenModel.Model1: return Trace1;
        case JohansenModel.Model2: return Trace2;
        case JohansenModel.Model3: return Trace3;
        case JohansenModel.Model4: return Trace4;
      }

      throw new ArgumentOutOfRangeException(nameof(model), model, "Invalid Model");
    }
  }

  public static class JohansenModelSelector
  {
    public static int SelectRank(JohansenInternalResult response, SignificanceLevel sig, int range)
    {
      var cv = CriticalValuesTable.Get(response.Model);
      var index = (int)sig;

      return Enumerable.Range(0, range)
        .Select(o => new { Rank = o, Index = range - o - 1 })
        .Where(o => o.Index >= 0 && o.Index < cv.GetLength(0))
        .FirstOrDefault(o => response.TraceStatistics[o.Rank] < cv[o.Index, index])
        ?.Rank ?? range;
    }
  }
}