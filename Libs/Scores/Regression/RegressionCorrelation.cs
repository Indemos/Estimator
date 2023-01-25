using ExScore.ExtensionSpace;
using ExScore.ModelSpace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExScore.ScoreSpace
{
  public struct RegressionResponse
  {
    public double DevX { get; set; }
    public double DevY { get; set; }
    public double Covariance { get; set; }
    public double Correlation { get; set; }
  }

  public class RegressionCorrelation
  {
    /// <summary>
    /// Inputs
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual RegressionResponse Calculate()
    {
      var slope = 0.0;
      var averageX = 0.0;
      var averageY = 0.0;
      var covariance = 0.0;
      var seriesX = new List<double>();
      var seriesY = new List<double>();
      var end = Items.Last().Value;
      var start = Items.First().Value;
      var slopeStep = (end - start) / start / Items.Count;
      var samples = Items.Percents((items, i) =>
      {
        slope = i == 0 ? items[i].Value : slope + slopeStep;

        var x = items[i].Value;
        var y = slope;

        seriesX.Add(x);
        seriesY.Add(slope);
        averageX += x;
        averageY += y;
      });

      averageX /= seriesX.Count;
      averageY /= seriesY.Count;

      var devX = 0.0;
      var devY = 0.0;

      for (var i = 0; i < samples.Count; i++)
      {
        var x = seriesX[i] - averageX;
        var y = seriesY[i] - averageY;

        devX += x * x;
        devY += y * y;
        covariance += x * y;
      }

      covariance /= samples.Count;
      devX = Math.Sqrt(devX / samples.Count);
      devY = Math.Sqrt(devY / samples.Count);

      var deviation = devX * devY;

      if (deviation == 0)
      {
        deviation = 1.0;
      }

      var response = new RegressionResponse
      {
        DevX = devX,
        DevY = devY,
        Covariance = covariance,
        Correlation = covariance / deviation
      };

      return response;
    }
  }
}
