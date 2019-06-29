using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Multifractal_spectrum
{
  internal class LayersBuilder
  {
    //TODO: вся логика, завязанная на maxWindowSize, должна исчезнуть, исправление на Python
    private const int maxWindowSize = 7;

    private Dictionary<Point, double> Densities = new Dictionary<Point, double>();

    /// <summary>
    /// Определение границ сингулярности изображения
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="converterType">Тип конвертера</param>
    /// <returns>Интервал сингулярностей изображения</returns>
    internal Interval GetSingularityBounds(DirectBitmap image, ConverterType converterType)
    {
      CalculateDensity(image, converterType);

      return new Interval(Densities.Values.Min(), Densities.Values.Max());
    }

    /// <summary>
    /// Определение множеств уровня исходного изображения
    /// </summary>
    /// <param name="singularityBounds">Интервал сингулярности</param>
    /// <param name="singularityStep">Шаг сингулярности</param>
    /// <returns>Список слоёв, каждый из которых - список точек</returns>
    internal List<Layer> SplitByLayers(Interval singularityBounds, double singularityStep)
    {
      var layers = new List<Layer>();

      for (double i = singularityBounds.Begin; i <= singularityBounds.End; i += singularityStep)
      {
        var layerSingularity = new Interval(i, i + singularityStep);

        var points = new List<Point>();
        foreach (Point point in Densities.Keys)
        {
          if (Densities[point] >= i && Densities[point] < i + singularityStep)
          {
            points.Add(point);
          }
        }

        layers.Add(new Layer(points, layerSingularity));
      }

      return layers;
    }

    /// <summary>
    /// Вычисление функции плотности для всех точек изображения
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="type">Тип конвертера</param>
    private void CalculateDensity(DirectBitmap image, ConverterType type)
    {
      for (int i = maxWindowSize; i < image.Width - maxWindowSize; i++)
      {
        for (int j = maxWindowSize; j < image.Height - maxWindowSize; j++)
        {
          var point = new Point(i, j);
          double density = CalculateDensityInPoint(image, point, type);

          //TODO: очень криво вычислять по одной точке, а сохранять по другой, исправление на Python
          Densities.Add(new Point(i - maxWindowSize, j - maxWindowSize), density);
        }
      }
    }

    /// <summary>
    /// Вычисление функции плотности в окрестности данной точки
    /// </summary>  
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="point">Координаты точки</param>
    /// <returns>Значение функции плотности в окрестности данной точки</returns>
    private double CalculateDensityInPoint(DirectBitmap image, Point point, ConverterType type)
    {
      int[] windows = { 2, 3, 4, 5, 7 };
      for (int i = 0; i < windows.Length; i++)
      {
        if (windows[i] > maxWindowSize)
          windows[i] = maxWindowSize;
      }

      int n = windows.Length;
      double xsum = 0, ysum = 0, xysum = 0, xsqrsum = 0;

      foreach (int windowSize in windows)
      {
        double intens = CalculateIntensivity(image, point, windowSize, type);

        double x = Math.Log(2 * windowSize + 1);
        double y = Math.Log(intens + 1);

        xsum += x;
        ysum += y;
        xsqrsum += x * x;
        xysum += x * y;
      }

      return 1.0 * (n * xysum - xsum * ysum) / (n * xsqrsum - xsum * xsum);
    }

    /// <summary>
    /// Вычисление суммарной интенсивности пикселей в прямоугольнике
    /// </summary>
    /// <param name="image">Изучаемая область изображения</param>
    /// <param name="point">Координата левого верхнего угла</param>
    /// <param name="windowSize">Размер окна</param>
    /// <param name="type">Тип конвертера</param>
    /// <returns>Cуммарная интенсивность пикселей в области</returns>
    private double CalculateIntensivity(DirectBitmap image, Point point, int windowSize, ConverterType type)
    {
      double intensivity = 0;
      int x = point.X;
      int y = point.Y;

      for (int i = x - windowSize; i <= x + windowSize; i++)
      {
        for (int j = y - windowSize; j <= y + windowSize; j++)
        {
          var pixel = image.GetPixel(i, j);
          intensivity += GetIntensivityFromPixel(pixel, type);
        }
      }

      return intensivity;
    }

    /// <summary>
    /// Вычисление интенсивности пикселя
    /// </summary>
    /// <param name="pixel">Пиксель изображения</param>
    /// <param name="converterType">Тип конвертера</param>
    /// <returns>Интенсивность пикселя</returns>
    private double GetIntensivityFromPixel(Color pixel, ConverterType converterType)
    {
      double intensivity = 0;
      switch (converterType)
      {
        case ConverterType.Grayscale:
          intensivity = pixel.B;
          break;
        case ConverterType.RGB_B:
          intensivity = pixel.B;
          break;
        case ConverterType.RGB_G:
          intensivity = pixel.G;
          break;
        case ConverterType.RGB_R:
          intensivity = pixel.R;
          break;
        case ConverterType.HSV:
          intensivity = pixel.GetHue();
          break;
      }

      return intensivity;
    }
  }
}
