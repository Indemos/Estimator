using Estimator.Estimators;
using Estimator.Models;

namespace Tests
{
  public class RarTests
  {
    [Fact]
    public void Calculate()
    {
      var items = new[] { 104, -184, -196, -197, -248, 253, -260, -279, 300, 308, 315, 331, -355, -386, -393, 394, 414, -432, -450, 452 };
      var score = new RAR
      {
        Mode = ModeEnum.Mar,
        Count = items.Length,
        Items = items
          .Select(o => new InputData { Value = o })
          .ToList()
      };

      Assert.Equal(-0.25, score.Calculate(), 2);
    }
  }
}