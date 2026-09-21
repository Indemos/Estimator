using MathNet.Numerics.Distributions;
using MathNet.Numerics.RootFinding;
using System;

namespace Estimator.Services
{
  public class OptionService
  {
    public enum OptionSideEnum : byte { Put = 1, Call = 2, Share = 3 }

    public const double MinTime = 1.5e-8;
    public const double MinSigma = 1e-8;
    public const double MinDispersion = 1e-8;

    /// <summary>
    /// Degeneracy check. How: T<=MinTime or sigma<=MinSigma or S/K<=0 or sigma*sqrt(T)<=MinDispersion.
    /// When: at start of every pricer/Greek to avoid divide-by-zero.
    /// Example: spot=100, strike=100, expiry=1e-9, sigma=0.2 => IsDegenerate=true, treat as expired.
    /// </summary>
    protected static bool IsCorruption(double spot, double strike, double expiry, double sigma)
    {
      return expiry <= MinTime || sigma <= MinSigma || spot <= 0 || strike <= 0 || sigma * Math.Sqrt(expiry) <= MinDispersion;
    }

    /// <summary>
    /// Discounted intrinsic. How: fwd=spot*exp(-divYield*T), pv=strike*exp(-rate*T).
    /// When: degenerate fallback and IV lower bound.
    /// Example: spot=105, strike=100, expiry=0.5, rate=0.05, divYield=0.02 => fwd=103.97, pv=97.53, Call=6.44.
    /// </summary>
    protected static double Intrinsic(OptionSideEnum side, double spot, double strike, double expiry, double rate, double divYield)
    {
      var fwd = spot * Math.Exp(-divYield * expiry);
      var pv = strike * Math.Exp(-rate * expiry);
      return side is OptionSideEnum.Call ? Math.Max(fwd - pv, 0) : Math.Max(pv - fwd, 0);
    }

    /// <summary>
    /// d1 = (ln(spot/strike)+(rate-divYield+0.5*sigma^2)*expiry)/(sigma*sqrt(expiry)).
    /// When: core for every Greek.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2, rate=0.05, divYield=0.02 => d1=0.175.
    /// </summary>
    protected static double D1(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      var sqrtT = Math.Sqrt(expiry);
      return (Math.Log(spot / strike) + (rate - divYield + 0.5 * sigma * sigma) * expiry) / (sigma * sqrtT);
    }

    /// <summary>
    /// d2 = d1 - sigma*sqrt(expiry).
    /// When: cash-or-nothing leg.
    /// Example: same inputs as D1, d1=0.175, sigma=0.2, expiry=0.25 => d2=0.075.
    /// </summary>
    protected static double D2(double expiry, double sigma, double d1)
    {
      return d1 - sigma * Math.Sqrt(expiry);
    }

    /// <summary>
    /// Theoretical fair value. How: Call = spot*exp(-qT)*N(d1) - strike*exp(-rT)*N(d2).
    /// When: mark-to-market, PnL explain, objective for IV solver.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2, rate=0.05, divYield=0.02 => Price Call=4.12.
    /// </summary>
    public static double Price(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return Intrinsic(side, spot, strike, Math.Max(expiry, 0), rate, divYield);

      var discDiv = Math.Exp(-divYield * expiry);
      var discRate = Math.Exp(-rate * expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);

      return side is OptionSideEnum.Call
        ? spot * discDiv * Normal.CDF(0, 1, d1) - strike * discRate * Normal.CDF(0, 1, d2)
        : strike * discRate * Normal.CDF(0, 1, -d2) - spot * discDiv * Normal.CDF(0, 1, -d1);
    }

    /// <summary>
    /// Delta dV/dS raw hedge ratio. How: shares to hedge = -Delta*qty.
    /// When: delta-neutral, ITM proxy, aggregation.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2, rate=0.05, divYield=0.02 => Delta Call=0.564.
    /// </summary>
    public static double Delta(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return side is OptionSideEnum.Call ? (spot > strike ? 1 : 0) : (spot < strike ? -1 : 0);

      var discDiv = Math.Exp(-divYield * expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);

      return side is OptionSideEnum.Call ? discDiv * Normal.CDF(0, 1, d1) : -discDiv * Normal.CDF(0, 1, -d1);
    }

    /// <summary>
    /// VegaAnnual raw dV/dsigma per 1.00 vol. How: spot*exp(-qT)*phi(d1)*sqrt(T).
    /// When: raw vol exposure, IV Newton derivative.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2 => VegaAnnual=19.5, so +0.01 vol => +0.195 price.
    /// </summary>
    public static double VegaAnnual(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var discDiv = Math.Exp(-divYield * expiry);
      var sqrtT = Math.Sqrt(expiry);
      var pdf = Normal.PDF(0, 1, D1(spot, strike, expiry, sigma, rate, divYield));

      return spot * discDiv * pdf * sqrtT;
    }

    /// <summary>
    /// Vega per 1% IV. How: VegaAnnual/100.
    /// When: risk limits, IV shock.
    /// Example: same inputs => Vega=0.195 per 1% IV, so IV 20%->21% lifts Call 4.12->4.31.
    /// </summary>
    public static double Vega(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      return VegaAnnual(spot, strike, expiry, sigma, rate, divYield) / 100;
    }

    /// <summary>
    /// ThetaAnnual dV/dt time passing. How: -S*phi*sigma/(2*sqrtT)+rate/q terms.
    /// When: carry, theta harvesting.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2 => ThetaAnnual Call=-9.2, Put=-4.1 per year.
    /// </summary>
    public static double ThetaAnnual(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var sqrtT = Math.Sqrt(expiry);
      var discDiv = Math.Exp(-divYield * expiry);
      var discRate = Math.Exp(-rate * expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);
      var pdf = Normal.PDF(0, 1, d1);
      var decay = -discDiv * spot * pdf * sigma / (2 * sqrtT);

      return side is OptionSideEnum.Call
        ? decay - rate * strike * discRate * Normal.CDF(0, 1, d2) + divYield * spot * discDiv * Normal.CDF(0, 1, d1)
        : decay + rate * strike * discRate * Normal.CDF(0, 1, -d2) - divYield * spot * discDiv * Normal.CDF(0, 1, -d1);
    }

    /// <summary>
    /// Theta per calendar day. How: ThetaAnnual/365.
    /// When: daily PnL explain.
    /// Example: ThetaAnnual=-9.2 => Theta=-0.0252 per day, so 1 day bleed 4.12->4.095.
    /// </summary>
    public static double Theta(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield) => ThetaAnnual(side, spot, strike, expiry, sigma, rate, divYield) / 365;

    /// <summary>
    /// Rho dV/drate per 1%. How: K*T*exp(-rT)*N(d2).
    /// When: long-dated or rates volatile.
    /// Example: spot=100, strike=100, expiry=2.0, sigma=0.2, rate=0.05 => Rho Call=0.72 per 1% rates.
    /// </summary>
    public static double Rho(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var discRate = Math.Exp(-rate * expiry);
      var d2 = D2(expiry, sigma, D1(spot, strike, expiry, sigma, rate, divYield));
      var rho = side is OptionSideEnum.Call ? strike * expiry * discRate * Normal.CDF(0, 1, d2) : -strike * expiry * discRate * Normal.CDF(0, 1, -d2);

      return rho / 100;
    }

    /// <summary>
    /// Epsilon dV/dq per 1% div yield. How: -S*T*exp(-qT)*N(d1) for Call.
    /// When: index div season.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2 => Epsilon Call=-0.141 per 1% yield.
    /// </summary>
    public static double Epsilon(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var discDiv = Math.Exp(-divYield * expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var eps = side is OptionSideEnum.Call ? -spot * expiry * discDiv * Normal.CDF(0, 1, d1) : spot * expiry * discDiv * Normal.CDF(0, 1, -d1);

      return eps / 100;
    }

    /// <summary>
    /// Lambda Delta*spot/Price leverage.
    /// When: capital efficiency.
    /// Example: spot=100, Call price=4.12, Delta=0.564 => Lambda=13.7 => 1% spot up => ~13.7% option up.
    /// </summary>
    public static double Lambda(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      var price = Price(side, spot, strike, expiry, sigma, rate, divYield);
      return Math.Abs(price) < 1e-12 ? 0 : Delta(side, spot, strike, expiry, sigma, rate, divYield) * spot / price;
    }

    /// <summary>
    /// Gamma d2V/dS2 raw. How: exp(-qT)*phi/(S*sigma*sqrtT).
    /// When: gamma scalping, hedge frequency, pin risk.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2 => Gamma=0.0195, so +1 spot => Delta +0.0195.
    /// </summary>
    public static double Gamma(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var sqrtT = Math.Sqrt(expiry);
      var discDiv = Math.Exp(-divYield * expiry);
      var pdf = Normal.PDF(0, 1, D1(spot, strike, expiry, sigma, rate, divYield));

      return discDiv * pdf / (spot * sigma * sqrtT);
    }

    /// <summary>
    /// Vanna d2V/dSdsigma per 1%. How: -exp(-qT)*phi*d2/sigma.
    /// When: spot-vol correlation, delta drifts with IV.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2, d2=0.075 => Vanna Call=-0.0073 per 1% vol.
    /// </summary>
    public static double Vanna(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);

      return -Math.Exp(-divYield * expiry) * Normal.PDF(0, 1, d1) * d2 / sigma / 100;
    }

    /// <summary>
    /// Volga/vomma d2V/dsigma2 per 1%. How: VegaAnnual*d1*d2/sigma/100.
    /// When: vol-of-vol, large IV moves.
    /// Example: spot=100, strike=110, expiry=0.25, sigma=0.2 => d1=-0.32, d2=-0.42 => Volga=0.013 per 1% vol.
    /// </summary>
    public static double Volga(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);

      return VegaAnnual(spot, strike, expiry, sigma, rate, divYield) * d1 * d2 / sigma / 100;
    }

    /// <summary>
    /// CharmAnnual dDelta/dt annualized. How: -exp(-qT)*phi*(2*(r-q)T - d2*sigma*sqrtT)/(2*T*sigma*sqrtT).
    /// When: overnight delta bleed.
    /// Example: spot=100, strike=100, expiry=0.05 (18d), sigma=0.2 => CharmAnnual Call=-0.82 => tomorrow delta 0.56->0.558.
    /// </summary>
    public static double CharmAnnual(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var sqrtT = Math.Sqrt(expiry);
      var discDiv = Math.Exp(-divYield * expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);
      var pdf = Normal.PDF(0, 1, d1);
      var common = -discDiv * pdf * (2 * (rate - divYield) * expiry - d2 * sigma * sqrtT) / (2 * expiry * sigma * sqrtT);

      return side is OptionSideEnum.Call ? common + divYield * discDiv * Normal.CDF(0, 1, d1) : common - divYield * discDiv * Normal.CDF(0, 1, -d1);
    }

    /// <summary>
    /// Charm per day. How: CharmAnnual/365.
    /// When: daily delta forecast.
    /// Example: CharmAnnual=-0.82 => Charm=-0.00225 per day.
    /// </summary>
    public static double Charm(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      return CharmAnnual(side, spot, strike, expiry, sigma, rate, divYield) / 365;
    }

    /// <summary>
    /// VetaAnnual dVega/dt raw. How: S*exp(-qT)*phi*sqrtT*(q + (r-q)*d1/(sigma*sqrtT) - (1+d1*d2)/(2T)).
    /// When: vega decay, roll timing.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2 => VetaAnnual=-25.3, so next week vega 19.5->18.0.
    /// </summary>
    public static double VetaAnnual(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var sqrtT = Math.Sqrt(expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);
      var pdf = Normal.PDF(0, 1, d1);
      var term = divYield + (rate - divYield) * d1 / (sigma * sqrtT) - (1 + d1 * d2) / (2 * expiry);

      return spot * Math.Exp(-divYield * expiry) * pdf * sqrtT * term;
    }

    /// <summary>
    /// Veta per day per 1%. How: VetaAnnual/365/100.
    /// When: daily vega bleed.
    /// Example: VetaAnnual=-25.3 => Veta=-0.00069 per day per 1% vol.
    /// </summary>
    public static double Veta(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      return VetaAnnual(spot, strike, expiry, sigma, rate, divYield) / 365 / 100;
    }

    /// <summary>
    /// DualDelta dV/dK discounted ITM prob. How: -exp(-rT)*N(d2) Call.
    /// When: market-implied odds, digital pricing.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2, rate=0.05 => DualDelta Call=-0.52 => 52% prob ITM.
    /// </summary>
    public static double DualDelta(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var discRate = Math.Exp(-rate * expiry);
      var d2 = D2(expiry, sigma, D1(spot, strike, expiry, sigma, rate, divYield));

      return side is OptionSideEnum.Call ? -discRate * Normal.CDF(0, 1, d2) : discRate * Normal.CDF(0, 1, -d2);
    }

    /// <summary>
    /// DualGamma d2V/dK2 risk-neutral density. How: exp(-rT)*phi(d2)/(K*sigma*sqrtT).
    /// When: implied distribution.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2 => DualGamma=0.0197 => density peak at ATM.
    /// </summary>
    public static double DualGamma(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var sqrtT = Math.Sqrt(expiry);
      var d2 = D2(expiry, sigma, D1(spot, strike, expiry, sigma, rate, divYield));

      return Math.Exp(-rate * expiry) * Normal.PDF(0, 1, d2) / (strike * sigma * sqrtT);
    }

    /// <summary>
    /// Speed dGamma/dS. How: -Gamma/S*(d1/(sigma*sqrtT)+1).
    /// When: gamma unstable 0DTE pin.
    /// Example: spot=100, strike=100, expiry=0.02 (7d), sigma=0.3 => Gamma=0.045, Speed=-0.0021 => +1 spot => Gamma 0.045->0.0429.
    /// </summary>
    public static double Speed(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var sqrtT = Math.Sqrt(expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);

      return -Gamma(spot, strike, expiry, sigma, rate, divYield) / spot * (d1 / (sigma * sqrtT) + 1);
    }

    /// <summary>
    /// Zomma dGamma/dsigma per 1%. How: Gamma*(d1*d2-1)/sigma/100.
    /// When: gamma across vol regime.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2, Gamma=0.0195 => Zomma=-0.095 per 1% vol.
    /// </summary>
    public static double Zomma(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);

      return Gamma(spot, strike, expiry, sigma, rate, divYield) * (d1 * d2 - 1) / sigma / 100;
    }

    /// <summary>
    /// ColorAnnual dGamma/dt annualized. How: numer/denom*bracket.
    /// When: gamma decay, hedge frequency.
    /// Example: spot=100, strike=100, expiry=0.05, sigma=0.2 => ColorAnnual=-0.65 => tomorrow Gamma 0.038->0.036.
    /// </summary>
    public static double ColorAnnual(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var sqrtT = Math.Sqrt(expiry);
      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);
      var numer = Math.Exp(-divYield * expiry) * Normal.PDF(0, 1, d1);
      var denom = 2 * spot * expiry * sigma * sqrtT;
      var bracket = 2 * divYield * expiry + 1 + (2 * (rate - divYield) * expiry - d2 * sigma * sqrtT) / (sigma * sqrtT) * d1;

      return numer / denom * bracket;
    }

    /// <summary>
    /// Color per day. How: ColorAnnual/365.
    /// When: daily gamma decay forecast.
    /// Example: ColorAnnual=-0.65 => Color=-0.00178 per day.
    /// </summary>
    public static double Color(OptionSideEnum side, double spot, double strike, double expiry, double sigma, double rate, double divYield) => ColorAnnual(spot, strike, expiry, sigma, rate, divYield) / 365;

    /// <summary>
    /// Ultima dVolga/dsigma per 1%. How: -VegaAnnual/(sigma^2)*(d1*d2*(1-d1*d2)+d1^2+d2^2)/100.
    /// When: large vol moves, vol-convex.
    /// Example: spot=100, strike=100, expiry=0.25, sigma=0.2 => Ultima=-0.12 per 1% vol.
    /// </summary>
    public static double Ultima(double spot, double strike, double expiry, double sigma, double rate, double divYield)
    {
      if (IsCorruption(spot, strike, expiry, sigma)) return 0;

      var d1 = D1(spot, strike, expiry, sigma, rate, divYield);
      var d2 = D2(expiry, sigma, d1);
      var inner = d1 * d2 * (1 - d1 * d2) + d1 * d1 + d2 * d2;

      return -VegaAnnual(spot, strike, expiry, sigma, rate, divYield) / (sigma * sigma) * inner / 100;
    }

    /// <summary>
    /// IV via Newton-Raphson. How: solve Price(sigma)-marketPrice=0 with VegaAnnual derivative.
    /// When: invert quote to IV for surface, marking.
    /// Example: spot=100, strike=100, expiry=0.25, rate=0.05, divYield=0.02, marketPrice=4.12 => IV=0.20.
    /// </summary>
    public static double? IV(OptionSideEnum side, double spot, double strike, double expiry, double rate, double divYield, double marketPrice, double accuracy = 1e-6)
    {
      if (expiry <= MinTime || spot <= 0 || strike <= 0 || marketPrice <= 0) return null;

      var floor = Intrinsic(side, spot, strike, expiry, rate, divYield);
      var ceil = side is OptionSideEnum.Call ? spot * Math.Exp(-divYield * expiry) : strike * Math.Exp(-rate * expiry);

      if (marketPrice < floor - 1e-10 || marketPrice > ceil + 1e-10) return null;

      double f(double v) => Price(side, spot, strike, expiry, v, rate, divYield) - marketPrice;
      double df(double v) => VegaAnnual(spot, strike, expiry, v, rate, divYield);

      try
      {
        var root = RobustNewtonRaphson.FindRoot(f, df, 1e-6, 10, accuracy);
        if (double.IsNaN(root) || double.IsInfinity(root)) return null;
        return root;
      }
      catch { return null; }
    }
  }
}