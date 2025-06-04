using Estimator.Models;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Estimators
{
  public class MAE
  {
    /// <summary>
    /// Inputs
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate() => Items.Average(o => o.Value - o.Min);
  }
}
