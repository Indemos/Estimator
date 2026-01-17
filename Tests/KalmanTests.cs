using Estimator.Services;
using Xunit;

namespace Tests
{
  public class KalmanTests
  {
    KalmanRegression Regression { get; set; } = new(dimension: 2);

    [Fact]
    public void Calculate()
    {
      List<double[]> assets =
      [
        [100.50, 150.25, 75.80],
        [101.20, 151.80, 76.40],
        [102.10, 149.60, 77.10],
        [101.80, 152.40, 76.80],
        [103.50, 153.20, 78.20],
        [104.20, 154.60, 79.10],
        [103.90, 153.80, 78.60],
        [105.10, 155.40, 80.20],
        [106.30, 156.80, 81.50],
        [105.80, 157.20, 82.10],
        [107.20, 158.60, 83.40],
        [108.40, 159.80, 84.70],
        [107.90, 160.40, 85.30],
        [109.10, 161.60, 86.60],
        [110.30, 162.80, 87.90],
        [109.80, 163.40, 88.50],
        [111.20, 164.60, 89.80],
        [112.40, 165.80, 91.10],
        [111.90, 166.40, 91.70],
        [113.10, 167.60, 93.00]
      ];

      foreach (var asset in assets)
      {
        var error = Regression.Update(Math.Log(asset[0]), [Math.Log(asset[1]), Math.Log(asset[2])]);
        var spread =
          Math.Log(asset[0]) -
          Math.Log(asset[1]) * Regression.Betas[0] -
          Math.Log(asset[2]) * Regression.Betas[1];

        Console.WriteLine(spread);
      }
    }
  }
}