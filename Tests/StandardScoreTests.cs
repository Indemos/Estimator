using Estimator.Estimators;
using Estimator.Models;

namespace Tests
{
  public class StandardScoreTests
  {
    [Fact]
    public void Calculate()
    {
      var score = new StandardScore
      {
        Items = new[] { 104, -184, -196, -197, -248, 253, -260, -279, 300, 308, 315, 331, -355, -386, -393, 394, 414, -432, -450, 452 }
          .Select(o => new InputData { Value = o })
          .ToList()
      };

      Assert.Equal(-1.11, score.Calculate(), 2);
    }
  }
}