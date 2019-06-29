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
    private Dictionary<Point, double> Intensivities = new Dictionary<Point, double>();

    /// <summary>
    /// Определение границ сингулярности изображения
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="converterType">Тип конвертера</param>
    /// <returns>Интервал сингулярностей изображения</returns>
    internal Interval GetSingularityBounds(DirectBitmap image, ConverterType converterType)
    {
      CalculateIntensivities(image, converterType);
      CalculateDensity();

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
    private void CalculateDensity()
    {
      var width = Intensivities.Keys.Max(point => point.X);
      var height = Intensivities.Keys.Max(point => point.Y);

      for (int i = maxWindowSize; i < width - maxWindowSize; i++)
      {
        for (int j = maxWindowSize; j < height - maxWindowSize; j++)
        {
          var point = new Point(i, j);
          double density = CalculateDensityInPoint(point);
          //TODO: очень криво вычислять по одной точке, а сохранять по другой, исправление на Python
          Densities.Add(new Point(point.X - maxWindowSize, point.Y - maxWindowSize), density);
        }
      }
    }

    /// <summary>
    /// Вычисление функции плотности в окрестности данной точки
    /// </summary>  
    /// <param name="point">Координаты точки</param>
    /// <returns>Значение функции плотности в окрестности данной точки</returns>
    private double CalculateDensityInPoint(Point point)
    {
      int[] windows = { 2, 3, 4, 5, 7 }; //Нельзя использовать значения больше maxWindowSize

      var points = windows
                      .Select(windowSize =>
                              {
                                double intens = CalculateIntensivity(point, windowSize);

                                //CRITICAL: НБ, почему здесь есть двойка, а в вычислении спектра нет? 
                                double x = Math.Log(2 * windowSize + 1);
                                double y = Math.Log(intens + 1);

                                return (x,y);
                              })
                      .ToList();

      return LeastSquares.ApplyMethod(points);
    }

    /// <summary>
    /// Вычисление интенсивности пикселей анализируемого изображения
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="converterType">Тип конвертера</param>
    private void CalculateIntensivities(DirectBitmap image, ConverterType converterType)
    {
      for (int i = 0; i < image.Width; i++)
      {
        for (int j = 0; j < image.Height; j++)
        {
          var point = new Point(i, j);
          var pixel = image.GetPixel(i, j);
          var intensivity = GetIntensivityFromPixel(pixel, converterType);

          Intensivities.Add(point, intensivity);
        }
      }
    }

    /// <summary>
    /// Вычисление суммарной интенсивности пикселей в прямоугольнике
    /// </summary>
    /// <param name="point">Цетральная точка области</param>
    /// <param name="windowSize">Размер окна</param>
    /// <returns>Cуммарная интенсивность пикселей в области</returns>
    private double CalculateIntensivity(Point point, int windowSize)
    {
      double intensivity = 0;

      DateTime before = DateTime.Now;
      for (int i = point.X - windowSize; i <= point.X + windowSize; i++)
      {
        for (int j = point.Y - windowSize; j <= point.Y + windowSize; j++)
        {
          var currentPoint = new Point(i, j);
          intensivity += Intensivities[currentPoint];
        }
      }
      DateTime after = DateTime.Now;
      string s = (after - before).ToString();

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
