using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multifractal_spectrum
{
  /// <summary>
  /// Множество уровня
  /// </summary>
  internal class Layer
  {
    /// <summary>
    /// Точки данного уровня
    /// </summary>
    internal List<Point> Points { get; private set; }

    /// <summary>
    /// Интервал сингулярности
    /// </summary>
    internal Interval SingularityBounds { get; private set; }

    internal Layer(List<Point> points, Interval singularityBounds)
    {
      Points = points;
      SingularityBounds = singularityBounds;
    }
  }
}
