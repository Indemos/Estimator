using MathNet.Numerics.Distributions;
using MathNet.Numerics.RootFinding;
using System;

namespace Estimator.Services
{
  public class OptionService
  {
    protected enum OptionSideEnum : byte
    {
      Put = 1,
      Call = 2,
      Share = 3
    }

    /// <summary>
    /// Asset or nothing
    /// </summary>
    /// <param name="S"></param>
    /// <param name="K"></param>
    /// <param name="T"></param>
    /// <param name="sigma"></param>
    /// <param name="r"></param>
    /// <param name="q"></param>
    /// <returns></returns>
    private static double D1(double S, double K, double T, double sigma, double r, double q)
    {
      return (Math.Log(S / K) + (r - q + (sigma * sigma) / 2.0) * T) / (sigma * Math.Sqrt(T));
    }

    /// <summary>
    /// Cash or nothing
    /// </summary>
    /// <param name="T"></param>
    /// <param name="sigma"></param>
    /// <param name="d1"></param>
    /// <returns></returns>
    private static double D2(double T, double sigma, double d1)
    {
      return d1 - sigma * Math.Sqrt(T);
    }

    /// <summary>
    /// Computes theoretical price.
    /// </summary>
    /// <param name="optionType">call or put</param>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Price(string optionType, double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);
      var d2 = D2(T, sigma, d1);

      switch (optionType)
      {
        case nameof(OptionSideEnum.Call):
          return S * Math.Exp(-q * T) * Normal.CDF(0, 1, d1) - K * Math.Exp(-r * T) * Normal.CDF(0, 1, d2);

        case nameof(OptionSideEnum.Put):
          return K * Math.Exp(-r * T) * Normal.CDF(0, 1, -d2) - S * Math.Exp(-q * T) * Normal.CDF(0, 1, -d1);
      }

      return 0;
    }

    /// <summary>
    /// Computes Vega. The amount of option price change for each 1% change in vol (sigma)
    /// </summary>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Vega(double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);
      var vega = S * Math.Exp(-q * T) * Normal.PDF(0, 1, d1) * Math.Sqrt(T);

      return vega / 100.0;
    }

    /// <summary>
    /// Volatility
    /// </summary>
    /// <param name="optionType"></param>
    /// <param name="S"></param>
    /// <param name="K"></param>
    /// <param name="T"></param>
    /// <param name="r"></param>
    /// <param name="q"></param>
    /// <param name="optionMarketPrice"></param>
    /// <returns></returns>
    public static double IV(string optionType, double S, double K, double T, double r, double q, double optionMarketPrice)
    {
      Func<double, double> f = sigma => Price(optionType, S, K, T, sigma, r, q) - optionMarketPrice;
      Func<double, double> df = sigma => Vega(S, K, T, sigma, r, q);

      return RobustNewtonRaphson.FindRoot(f, df, lowerBound: 0, upperBound: 100, accuracy: 0.001);
    }

    /// <summary>
    /// Computes theta.
    /// </summary>
    /// <param name="optionType">call or put</param>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Theta(string optionType, double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);
      var d2 = D2(T, sigma, d1);

      switch (optionType)
      {
        case nameof(OptionSideEnum.Call):
          {
            var theta = -Math.Exp(-q * T) * (S * Normal.PDF(0, 1, d1) * sigma) / (2.0 * Math.Sqrt(T))
                    - (r * K * Math.Exp(-r * T) * Normal.CDF(0, 1, d2))
                    + q * S * Math.Exp(-q * T) * Normal.CDF(0, 1, d1);

            return theta / 365.0;
          }

        case nameof(OptionSideEnum.Put):
          {
            var theta = -Math.Exp(-q * T) * (S * Normal.PDF(0, 1, d1) * sigma) / (2.0 * Math.Sqrt(T))
                + (r * K * Math.Exp(-r * T) * Normal.PDF(0, 1, -d2))
                - q * S * Math.Exp(-q * T) * Normal.CDF(0, 1, -d1);

            return theta / 365.0;
          }
      }

      return 0;
    }

    /// <summary>
    /// Computes delta.
    /// </summary>
    /// <param name="optionType">call or put</param>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Delta(string optionType, double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);

      switch (optionType)
      {
        case nameof(OptionSideEnum.Call): return Math.Exp(-r * T) * Normal.CDF(0, 1, d1);
        case nameof(OptionSideEnum.Put): return -Math.Exp(-r * T) * Normal.CDF(0, 1, -d1);
      }

      return 0;
    }

    /// <summary>
    /// Computes gamma.
    /// </summary>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Gamma(double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);
      return Math.Exp(-q * T) * (Normal.PDF(0, 1, d1) / (S * sigma * Math.Sqrt(T)));
    }

    /// <summary>
    /// Computes delta.
    /// </summary>
    /// <param name="optionType">call or put</param>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Rho(string optionType, double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);
      var d2 = D2(T, sigma, d1);

      switch (optionType)
      {
        case nameof(OptionSideEnum.Call): return K * T * Math.Exp(-r * T) * Normal.CDF(0, 1, d2);
        case nameof(OptionSideEnum.Put): return -K * T * Math.Exp(-r * T) * Normal.CDF(0, 1, -d2);
      }

      return 0;
    }

    /// <summary>
    /// Computes Vanna. The amount of option price change for each 1% change in vol (sigma)
    /// </summary>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Vanna(double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);
      var vega = Vega(S, K, T, sigma, r, q);
      
      return (d1 / sigma) * vega;
    }

    /// <summary>
    /// Computes Volga. The amount of option price change for each 1% change in vol (sigma)
    /// </summary>
    /// <param name="S">Underlying price</param>
    /// <param name="K">Strike price</param>
    /// <param name="T">Time to expiration in % of year</param>
    /// <param name="sigma">Volatility</param>
    /// <param name="r">continuously compounded risk-free interest rate</param>
    /// <param name="q">continuously compounded dividend yield</param>
    /// <returns></returns>
    public static double Volga(double S, double K, double T, double sigma, double r, double q)
    {
      var d1 = D1(S, K, T, sigma, r, q);
      var d2 = D2(T, sigma, d1);
      var vega = Vega(S, K, T, sigma, r, q);
      
      return vega * d1 * d2 / sigma;
    }
  }
}
