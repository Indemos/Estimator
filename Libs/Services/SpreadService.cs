using System;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

public class SpreadService
{
  protected const double Epsilon = 1e-10;
  protected const double MaxConditionNumber = 1e10;

  protected readonly double alpha;
  protected readonly double ridge;

  protected int count;
  protected int dimension;
  protected bool setup;
  protected bool advance = true;

  protected double meanY;
  protected Vector<double> meanX;
  protected Vector<double> m2XY;
  protected Matrix<double> m2XX;
  protected double[] cache = Array.Empty<double>();

  public bool Corruption { get; protected set; }

  public SpreadService(double period, double ridge = 0)
  {
    alpha = 1 - Math.Pow(0.5, 1.0 / period);
    this.ridge = ridge;
  }

  // no-lookahead: spread from t-1, then -> t
  public virtual double? Update(double y, params double[] x)
  {
    var spread = ComputeSpread(y, x);
    Observe(y, x);
    return spread;
  }

  public virtual double[] Betas()
  {
    if (dimension == 0) return Array.Empty<double>();
    if (advance) { cache = SolveBetas(); advance = false; }
    return (double[])cache.Clone();
  }

  public virtual double? ComputeSpread(double y, params double[] x)
  {
    if (dimension == 0) return null;
    var xv = Vector<double>.Build.DenseOfArray(x);
    return y - meanY - Vector<double>.Build.DenseOfArray(Betas()).DotProduct(xv - meanX);
  }

  protected virtual void Observe(double y, params double[] x)
  {
    if (dimension is 0) dimension = x.Length;

    var xv = Vector<double>.Build.DenseOfArray(x);

    if (setup is false)
    {
      meanX = xv; meanY = y;
      m2XX = Matrix<double>.Build.Dense(dimension, dimension);
      m2XY = Vector<double>.Build.Dense(dimension);
      setup = true;
    }
    else
    {
      var dx = xv - meanX;
      var dy = y - meanY;

      meanX += dx * alpha;
      meanY += dy * alpha;
      m2XX = (m2XX + dx.OuterProduct(dx * alpha)) * (1 - alpha);
      m2XY = (m2XY + dx * (dy * alpha)) * (1 - alpha);
    }

    count++; advance = true;
  }

  protected virtual double[] SolveBetas()
  {
    var lambda = Math.Max(ridge * (m2XX.Trace() / dimension), Epsilon);
    var A = m2XX + Matrix<double>.Build.DenseIdentity(dimension) * lambda;
    
    A = (A + A.Transpose()) * 0.5;

    try
    {
      var chol = A.Cholesky();
      var diag = chol.Factor.Diagonal();
      var cond = diag.Maximum() / diag.Minimum();
      
      cond *= cond;
      
      if (diag.Minimum() <= 0 || double.IsNaN(cond) || cond > MaxConditionNumber)
      {
        Corruption = true; 
        return new double[dimension];
      }

      var sol = chol.Solve(m2XY);

      if (sol.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
      {
        Corruption = true; return new double[dimension];
      }

      Corruption = false;
      return sol.ToArray();
    }
    catch
    {
      Corruption = true;
      return new double[dimension];
    }
  }
}