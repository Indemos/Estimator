using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace Estimator.Services
{
  public enum JohansenModel
  {
    M0 = 0, // no deterministic terms
    M1 = 1, // constant in cointegration
    M2 = 2, // constant in VAR
    M3 = 3, // constant + trend in coint
    M4 = 4  // constant + trend in VAR
  }

  public class JohansenCore
  {
    public virtual JohansenResponse Run(Matrix<double> series, int steps, JohansenModel model)
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

      (Y1, matrixLag) = Terms(Y1, matrixLag, model, T);

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

      // 7. Compute Trace Statistics
      // Trace = -T * Sum(ln(1 - lambda))
      var trace = Enumerable.Range(0, columns)
        .Select(r => -T * eigSorted.Skip(r).Sum(value => Math.Log(Math.Max(1e-15, 1.0 - value))))
        .ToArray();

      return new JohansenResponse
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
    protected virtual Matrix<double> Regression(Matrix<double> y, Matrix<double> x)
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
    protected virtual Matrix<double> Inversion(Matrix<double> A)
    {
      var evd = A.Evd(Symmetricity.Symmetric);
      var V = evd.EigenVectors;
      var D = evd.EigenValues;
      var vector = Vector<double>.Build.Dense(D.Count, o => 1.0 / Math.Sqrt(Math.Max(1e-10, D[o].Real)));
      var vectorM = Matrix<double>.Build.DiagonalOfDiagonalVector(vector);

      return V.Multiply(vectorM).Multiply(V.Transpose());
    }

    /// <summary>
    /// Choose behavior
    /// </summary>
    /// <param name="y1"></param>
    /// <param name="z"></param>
    /// <param name="model"></param>
    /// <param name="count"></param>
    protected virtual (Matrix<double> Y1, Matrix<double> Z) Terms(Matrix<double> y1, Matrix<double> z, JohansenModel model, int count)
    {
      var ones = Matrix<double>.Build.Dense(count, 1, 1.0);
      var trend = Matrix<double>.Build.DenseOfColumnArrays(Enumerable.Range(1, count)
        .Select(o => (double)o)
        .ToArray());

      switch (model)
      {
        case JohansenModel.M1: y1 = y1.Append(ones); break;
        case JohansenModel.M2: z = z is null ? ones : z.Append(ones); break;
        case JohansenModel.M3: y1 = y1.Append(ones); y1 = y1.Append(trend); break;
        case JohansenModel.M4:
          z = z is null ? ones : z.Append(ones);
          z = z.Append(trend);
          break;
      }

      return (y1, z);
    }
  }

  public class JohansenResponse
  {
    public double[] EigenValues;
    public double[] TraceStatistics;

    public Matrix<double> R0;
    public Matrix<double> R1;
    public Matrix<double> EigenVectors;

    public JohansenModel Model;
  }
}