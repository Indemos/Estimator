using Estimator.Models;
using System.Collections.Generic;
using System.Linq;

namespace Estimator.Estimators
{
  public class MFE
  {
    /// <summary>
    /// Inputs
    /// </summary>
    public virtual IList<InputData> Items { get; set; } = new List<InputData>();

    /// <summary>
    /// Calculate
    /// </summary>
    public virtual double Calculate() => Items.Average(o => o.Max - o.Value);
  }
}
