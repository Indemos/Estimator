using System;

namespace Estimator.Services
{
  public class EntropyIndicator
  {
    protected bool setup;
    protected double alpha;
    protected double[] probs = new double[3]; // EWMA prob [down,even,up]
    protected double? currentPrice;
    protected int period = 15;

    public EntropyIndicator(int period = 15)
    {
      this.period = period;
      this.alpha = 1 - Math.Pow(0.5, 1.0 / period);
    }

    public virtual double Update(double? price)
    {
      // no price -> return current state

      if (price is null)
      {
        return Entropy();
      }

      // first price: no direction yet, just seed currentPrice

      if (currentPrice is null)
      {
        currentPrice = price;
        return 0;
      }

      // dir: -1,0,1 -> 0,1,2 index
      
      var dir = Math.Sign(price.Value - currentPrice.Value) + 1;

      currentPrice = price;

      if (setup is false)
      {
        // init as one-hot: first direction is 100% certain -> H=0
        // avoids permanent zero-bias from zero-init

        probs[0] = probs[1] = probs[2] = 0;
        probs[dir] = 1;
        setup = true;

        return 0;
      }

      // EWMA fading: decay old probs, blend new one-hot with weight alpha
      // probs[t] = (1 - a) * probs[t - 1] + a * I(dir)

      for (var i = 0; i < probs.Length; i++)
      {
        probs[i] *= (1 - alpha);
      }

      probs[dir] += alpha; // sum stays 1: (1 - a) * 1 + a = 1

      return Entropy();
    }

    /// <summary>
    /// Entropy
    /// </summary>
    public virtual double Entropy()
    {
      if (setup is false)
      {
        return 0;
      }

      // H = - sum p*log2(p)

      var H = 0.0;
      
      for (var i = 0; i < probs.Length; i++)
      {
        if (probs[i] > 0) H -= probs[i] * Math.Log(probs[i], 2);
      }

      // normalize by Hmax = log2(probs.Length) for uniform distribution

      return H / Math.Log(probs.Length, 2);
    }
  }
}