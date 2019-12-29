using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Multifractal_spectrum
{
  internal class LayersBuilder
  {
    private double[,] Densities;
    private double[,] Intensivities;

    /// <summary>
    /// Определение границ сингулярности изображения
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="converterType">Тип конвертера</param>
    /// <returns>Интервал сингулярностей изображения</returns>
    internal Interval GetSingularityBounds(DirectBitmap image, ConverterType converterType)
    {
      Intensivities = new double[image.Width, image.Height];
      CalculateIntensivities(image, converterType);

      CalculateDensity(image);

      var densityValues = Densities.Cast<double>();
      return new Interval(densityValues.Min(), densityValues.Max());
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

      for (double sin = singularityBounds.Begin; sin <= singularityBounds.End; sin += singularityStep)
      {
        var layerSingularity = new Interval(sin, sin + singularityStep);

        var points = new List<Point>();
        int width = Densities.GetLength(0);
        int height = Densities.GetLength(1);

        for (int i = 0; i < width; i++)
        {
          for (int j = 0; j < height; j++)
          {
            if (Densities[i, j] >= sin && Densities[i, j] < sin + singularityStep)
            {
              points.Add(new Point(i,j));
            }
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
    private void CalculateDensity(DirectBitmap image)
    {
      int width = Intensivities.GetLength(0);
      int height = Intensivities.GetLength(1);

      Densities = new double[width, height];
      for (int i = 0; i < width; i++)
      {
        for (int j = 0; j < height; j++)
        {
          var point = new Point(i, j);
          double density = CalculateDensityInPoint(image, point);

          Densities[i, j] = density;
        }
      }
    }

    /// <summary>
    /// Вычисление функции плотности в окрестности данной точки
    /// </summary>  
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="point">Координаты точки</param>
    /// <returns>Значение функции плотности в окрестности данной точки</returns>
    private double CalculateDensityInPoint(DirectBitmap image, Point point)
    {
      int[] windows = { 2, 3, 4, 5, 7 }; //Нельзя использовать значения больше maxWindowSize

      var points = windows
                      .Select(windowSize =>
                              {
                                double intens = CalculateIntensivity(image, point, windowSize);

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
          var pixel = image.GetPixel(i, j);
          var intensivity = GetIntensivityFromPixel(pixel, converterType);

          Intensivities[i, j] = intensivity;
        }
      }
    }

    /// <summary>
    /// Вычисление суммарной интенсивности пикселей в прямоугольнике
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="point">Цетральная точка области</param>
    /// <param name="windowSize">Размер окна</param>
    /// <returns>Cуммарная интенсивность пикселей в области</returns>
    private double CalculateIntensivity(DirectBitmap image, Point point, int windowSize)
    {
      double intensivity = 0;

      for (int i = Math.Max(point.X - windowSize, 0); i < Math.Min(point.X + windowSize, image.Width); i++)
      {
        for (int j = Math.Max(point.Y - windowSize, 0); j < Math.Min(point.Y + windowSize, image.Height); j++)
        {
          intensivity += Intensivities[i,j];
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
