using Estimator.Extensions;
using Estimator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Estimators
{
  public struct RegressionResponse
  {
    public double DevX { get; set; }
    public double DevY { get; set; }
    public double Covariance { get; set; }
    public double Correlation { get; set; }
  }

  public class Regression
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
      var averageX = 0.0;
      var averageY = 0.0;
      var covariance = 0.0;
      var seriesX = new List<double>();
      var seriesY = new List<double>();
      var count = Items.Count;
      var slopeStep = Items.Average(o => o.Value);
      var slope = 0.0;

      for (var i = 0; i < count; i++)
      {
        slope += slopeStep;
        averageX += Items[i].Value;
        averageY += slope;
        seriesY.Add(slope);
        seriesX.Add(Items[i].Value);
      }

      averageX /= seriesX.Count;
      averageY /= seriesY.Count;

      var devX = 0.0;
      var devY = 0.0;

      for (var i = 0; i < count; i++)
      {
        var x = seriesX[i] - averageX;
        var y = seriesY[i] - averageY;

        devX += x * x;
        devY += y * y;
        covariance += x * y;
      }

      covariance /= count;
      devX = Math.Sqrt(devX / count);
      devY = Math.Sqrt(devY / count);

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
