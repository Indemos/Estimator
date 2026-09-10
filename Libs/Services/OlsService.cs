using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace Estimator.Services
{
  /// <summary>
  /// EWMA (exponentially-weighted), ridge-regularized OLS regression (with an
  /// implicit intercept via mean-centering) used to estimate a stationary
  /// spread between two or more series.
  ///
  /// Design notes:
  /// - First and second moments are tracked with an exponentially-weighted
  ///   generalization of Welford's online update: instead of removing the
  ///   oldest observation's exact contribution when a fixed-size window
  ///   overflows, every existing moment is decayed by a constant factor
  ///   (1 - alpha) each step and the new observation is blended in with
  ///   weight alpha. There is no hard edge, so no single stale observation
  ///   can drop out and produce a step-jump in beta/spread the way it could
  ///   with a rolling window.
  /// - alpha is derived from a half-life in observations: the number of
  ///   updates it takes for a past observation's influence to decay to 50%.
  /// - Ridge regularization shrinks beta toward a caller-supplied economic
  ///   prior. With no data, or when the regression is too ill-conditioned to
  ///   solve reliably, beta collapses exactly to that prior. This logic is
  ///   UNCHANGED from the windowed version: it operates generically on the
  ///   accumulated moments regardless of how they were computed.
  /// - Cholesky solve + explicit conditioning check, also unchanged.
  ///
  /// Caveat: for the first few half-lives after construction, the moments
  /// carry a small "cold start" bias relative to the idealized
  /// infinite-history exponential weighting - the same distinction pandas
  /// draws between ewm(adjust=False) (used here) and ewm(adjust=True). It
  /// fades below significance within roughly 5-10 half-lives and does not
  /// recur later, unlike the windowed version's per-dropout jumps.
  /// </summary>
  public class OlsService
  {
    protected const double Epsilon = 1e-10;
    protected const double MaxConditionNumber = 1e10;

    protected readonly double alpha;
    protected readonly double ridge;

    protected int count;
    protected int dimension;
    protected bool initialized;

    // Running exponentially-weighted moments - already on a covariance
    // scale, so they don't need dividing by count/window size anywhere.
    protected double meanY;
    protected Vector<double> meanX;
    protected Matrix<double> m2XX; // EW Cov(X, X)
    protected Vector<double> m2XY; // EW Cov(X, Y)

    protected double[] userPrior;
    protected Vector<double> priorBeta;

    protected bool setup = true;
    protected double[] cachedBeta;

    public int Count => count;
    public int Dimension => dimension;
    public bool Corruption { get; protected set; }

    // Suggested (not enforced) threshold for trusting the fit -- this is an
    // identifiability floor, not a cold-start guard. Consider also gating on
    // Count > ~3 * halfLifeObservations if you want to wait out the warm-up
    // transient described above.
    public bool IsWarm => count > dimension;

    /// <param name="decay">
    /// Updates for a past observation's weight to decay to 50%. Tie this to
    /// the spread's own empirical mean-reversion half-life (AR(1)/OU fit),
    /// not to an old window length -- an EWMA half-life retains meaningful
    /// weight well past that many observations, unlike a boxcar of the
    /// same size.
    /// </param>
    public OlsService(double decay, double ridge = 0.0, double[] prior = null)
    {
      alpha = 1.0 - Math.Pow(0.5, 1.0 / decay);

      this.ridge = ridge;
      this.userPrior = prior;
    }

    public virtual double? Update(double y, params double[] x)
    {
      var spread = ComputeSpread(y, x);

      Observe(y, x);

      return spread;
    }

    public virtual double[] Betas()
    {
      if (dimension is 0)
      {
        return Array.Empty<double>();
      }

      if (setup)
      {
        cachedBeta = SolveBetas();
        setup = false;
      }

      return (double[])cachedBeta.Clone();
    }

    public virtual double? ComputeSpread(double y, params double[] x)
    {
      if (dimension is 0)
      {
        return null;
      }

      var beta = Betas();
      var spread = y - meanY;

      for (var i = 0; i < dimension; i++)
      {
        spread -= beta[i] * (x[i] - meanX[i]);
      }

      return spread;
    }

    protected virtual void Observe(double y, params double[] x)
    {
      if (dimension is 0)
      {
        dimension = x.Length;
        priorBeta = userPrior is null ?
          Vector<double>.Build.Dense(dimension) :
          Vector<double>.Build.DenseOfArray(userPrior);
      }

      var xv = Vector<double>.Build.DenseOfArray(x);

      if (initialized is false)
      {
        // Initialize at the first observation, not zero. Unlike plain
        // count-based Welford, a fixed-alpha EWMA does NOT self-correct a
        // zero-initialized mean -- it would permanently behave as if there
        // were infinite prior history sitting at 0.
        meanX = xv;
        meanY = y;
        m2XX = Matrix<double>.Build.Dense(dimension, dimension);
        m2XY = Vector<double>.Build.Dense(dimension);
        initialized = true;
      }
      else
      {
        Add(y, xv);
      }

      count++;
      setup = true;
    }

    public virtual void SetPrior(params double[] prior)
    {
      if (dimension is 0)
      {
        userPrior = prior;
        return;
      }

      priorBeta = Vector<double>.Build.DenseOfArray(prior);
      setup = true;
    }

    // Exponentially-weighted generalization of Welford: decay existing
    // moments by (1 - alpha), blend in the new point with weight alpha.
    // Uses dx (old delta) times incrX (alpha * dx) rather than dx squared
    // directly -- the same numerical-stability reason plain Welford uses
    // old-delta * new-delta instead of accumulating a separately-rounded
    // square.
    protected virtual void Add(double y, Vector<double> x)
    {
      var dx = x - meanX;
      var incrX = dx * alpha;
      meanX += incrX;

      var dy = y - meanY;
      var incrY = dy * alpha;
      meanY += incrY;

      // dx.OuterProduct(incrX) = alpha * (dx ⊗ dx): symmetric and PSD for
      // any dx, so m2XX is a sum of scaled PSD terms at every step and
      // stays PSD by construction -- no Joseph-form-style safeguard needed.
      m2XX = (m2XX + dx.OuterProduct(incrX)) * (1 - alpha);
      m2XY = (m2XY + dx * incrY) * (1 - alpha);
    }

    // UNCHANGED from the windowed version. Because m2XX/m2XY and lambda all
    // scale together, the ridge fraction and resulting beta are insensitive
    // to whether the moments are on a "raw sum" scale (windowed) or an
    // already-normalized covariance scale (EWMA).
    protected virtual double[] SolveBetas()
    {
      var lambda = Math.Max(ridge * (m2XX.Trace() / dimension), Epsilon);
      var a = m2XX.Clone();

      for (var i = 0; i < dimension; i++)
      {
        a[i, i] += lambda;
      }

      a = (a + a.Transpose()) * 0.5;

      var rhs = m2XY + priorBeta * lambda;

      Cholesky<double> cholesky;

      try
      {
        cholesky = a.Cholesky();
      }
      catch (Exception)
      {
        Corruption = true;
        return priorBeta.ToArray();
      }

      var diag = cholesky.Factor.Diagonal();
      var maxDiag = diag.Maximum();
      var minDiag = diag.Minimum();
      var conditionEstimate = (maxDiag / minDiag) * (maxDiag / minDiag);

      if (minDiag <= 0 || double.IsNaN(conditionEstimate) || conditionEstimate > MaxConditionNumber)
      {
        Corruption = true;
        return priorBeta.ToArray();
      }

      var solved = cholesky.Solve(rhs);

      for (var i = 0; i < dimension; i++)
      {
        if (double.IsNaN(solved[i]) || double.IsInfinity(solved[i]))
        {
          Corruption = true;
          return priorBeta.ToArray();
        }
      }

      Corruption = false;
      return solved.ToArray();
    }
  }
}