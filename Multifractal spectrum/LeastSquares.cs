using System;
using System.Collections.Generic;

namespace Multifractal_spectrum
{
  internal static class LeastSquares
  {
    /// <summary>
    /// Уточнение результата вычислений с помощью МНК
    /// </summary>
    /// <param name="points">множество точек и значений в них</param>
    /// <returns></returns>
    internal static double ApplyMethod(List<(double X, double Y)> points)
    {
      int n = points.Count;
      double xsum = 0, ysum = 0, xysum = 0, xsqrsum = 0;

      foreach (var point in points)
      {
        xsum += point.X;
        ysum += point.Y;
        xsqrsum += point.X * point.X;
        xysum += point.X * point.Y;
      }

      return 1.0 * (n * xysum - xsum * ysum) / (n * xsqrsum - xsum * xsum);
    }
  }
}
