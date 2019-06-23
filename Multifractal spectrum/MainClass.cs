using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Multifractal_spectrum
{
  internal class MainClass
  {
    //TODO: при встроенной обработке окон вся логика, завязанная на maxWindowSize, должна исчезнуть
    private const int maxWindowSize = 7;

    private List<Interval> layersSingularities = new List<Interval>();

    /// <summary>
    /// Создание множеств уровня по исходному изображению
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="converterType">Тип конвертера</param>
    /// <param name="currentLayerSingularity">Минимальная сингулярность изображения</param>
    /// <param name="singularityStep">Шаг сингулярности</param>
    /// <returns>Список слоёв, каждый из которых - список точек</returns>
    private List<List<Point>> CreateLayers(
      DirectBitmap image,
      ConverterType converterType,
      ref double currentLayerSingularity,
      ref double singularityStep)
    {
      //TODO: разбить метод на три части: 
      // 1) Вычисление границ и шага сингулярности
      // 2) Вывод на печать (в другой класс)
      // 3) Вычисление слоёв
      Dictionary<Point, double> densities = CalculateDensity(image, converterType);
      var layers = new List<List<Point>>();

      var sortedValues = densities.Values.ToList();
      sortedValues.Sort();
      double min = sortedValues[0];
      double max = sortedValues[sortedValues.Count - 1];

      Console.WriteLine("Minimal singularity:   {0:0.00}", min);
      Console.WriteLine("Maximal singularity:   {0:0.00}", max);

      Console.WriteLine("Введите шаг между уровнями, например, 0,2");
      double step = double.Parse("0,2"/*Console.ReadLine()*/);

      Console.WriteLine("\nВычисляются множества уровня...");

      double layerStep = step;
      singularityStep = step;
      currentLayerSingularity = min;


      for (double i = min; i <= max; i += layerStep)
      {
        layersSingularities.Add(new Interval(i, i + layerStep));

        var layer = new List<Point>();
        foreach (Point point in densities.Keys)
        {
          if (densities[point] >= i && densities[point] < i + layerStep)
          {
            layer.Add(point);
          }
        }

        layers.Add(layer);
      }

      return layers;
    }

    /// <summary>
    /// Создание изображения, соответствующего данному уровню, и его измерение
    /// </summary>
    /// <param name="image">исходное изображение</param>
    /// <param name="layerPoints">координаты точек, соответствующих данному уровню</param>
    /// <returns></returns>
    private double CreateAndMeasureLayer(DirectBitmap image, List<Point> layerPoints, int layerNumber)
    {
      int newWidth = image.Width - maxWindowSize * 2;
      int newHeight = image.Height - maxWindowSize * 2;
      DirectBitmap layerImage = new DirectBitmap(newWidth, newHeight);

      for (int i = 0; i < layerImage.Width; i++)
      {
        for (int j = 0; j < layerImage.Height; j++)
        {
          layerImage.SetPixel(i, j, Color.FromArgb(255, 255, 255, 255));
        }
      }

      foreach (Point point in layerPoints)
      {
        layerImage.SetPixel(point.X, point.Y, Color.FromArgb(255, 0, 0, 0));
      }

      string layerName = "layer " + Math.Round(layersSingularities[layerNumber].Begin, 2).ToString()
         + "  " + Math.Round(layersSingularities[layerNumber].End, 2).ToString();
      string pathToImage = Path.Combine(Program.actualLayersPath, layerName + ".jpg");

      layerImage.Bitmap.Save(pathToImage);

      return CalculateMeasure(layerImage);
    }

    /// <summary>
    /// Вычисление функции плотности для всех точек изображения
    /// </summary>
    /// <param name="image">исходное изображение</param>
    /// <returns></returns>
    private Dictionary<Point, double> CalculateDensity(DirectBitmap image, ConverterType type)
    {
      var densities = new Dictionary<Point, double>();

      for (int i = maxWindowSize; i < image.Width - maxWindowSize; i++)
      {
        for (int j = maxWindowSize; j < image.Height - maxWindowSize; j++)
        {
          var point = new Point(i, j);
          double density = CalculateDensityInPoint(image, point, type);

          //TODO: очень криво вычислять по одной точке, а сохранять по другой
          densities.Add(new Point(i - maxWindowSize, j - maxWindowSize), density);
        }
      }

      return densities;
    }

    /// <summary>
    /// Вычисление функции плотности в окрестности данной точки
    /// </summary>
    /// <param name="point">координаты точки</param>
    /// <param name="image">исходное изображение</param>
    /// <returns></returns>
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
    /// <param name="image">прямоугольник, который изучаем</param>
    /// <param name="point">координата левой верхней вершины</param>
    /// <param name="windowSize">размер окна</param>
    /// <returns></returns>
    private double CalculateIntensivity(DirectBitmap image, Point point, int windowSize, ConverterType type)
    {
      double intensivity = 0;
      int x = point.X;
      int y = point.Y;

      for (int i = x - windowSize; i <= x + windowSize; i++)
      {
        for (int j = y - windowSize; j <= y + windowSize; j++)
        {
          Color pixel = image.GetPixel(i, j);
          intensivity += GetIntensivityFromPixel(pixel, type);
        }
      }

      return intensivity;
    }

    private double GetIntensivityFromPixel(Color pixel, ConverterType type)
    {
      double intensivity = 0;
      switch (type)
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

    /// <summary>
    /// Вычисление фрактальной размерности изображения
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    private double CalculateMeasure(DirectBitmap image)
    {
      int[] windows = { 3, 4, 5, 6, 7, 8 };
      int n = windows.Length;
      double xsum = 0, ysum = 0, xysum = 0, xsqrsum = 0;

      foreach (int windowSize in windows)
      {
        double intens = CalculateBlackWindows(image, windowSize);

        double x = Math.Log(1.0 / windowSize);
        double y = Math.Log(intens + 1);

        //TODO: этих проверок здесь быть не должно
        if (!double.IsNaN(x) && !double.IsNaN(y)
            && !double.IsInfinity(x) && !double.IsInfinity(y))
        {
          xsum += x;
          ysum += y;
          xsqrsum += x * x;
          xysum += x * y;
        }
      }

      //TODO: фрактальная размерность изображения не может быть отрицательной!
      return Math.Max(0.0, 1.0 * (n * xysum - xsum * ysum) / (n * xsqrsum - xsum * xsum));
    }

    /// <summary>
    /// Подсчёт числа квадратиков, имеющих внутри себя хотя бы один чёрный пиксель
    /// </summary>
    /// <param name="image">прямоугольник, который изучаем</param>
    /// <param name="window">размер окна</param>
    /// <returns></returns>
    private double CalculateBlackWindows(DirectBitmap image, int window)
    {
      double blackWindows = 0;

      for (int i = 0; i < image.Width - window; i += window)
      {
        for (int j = 0; j < image.Height - window; j += window)
        {
          if (HasBlackPixel(image, i, j, window))
          {
            blackWindows++;
          }
        }
      }

      return blackWindows;
    }

    /// <summary>
    /// Проверка, содержит ли прямоугольник хотя бы один чёрный пиксель
    /// </summary>
    /// <param name="image">прямоугольник, который изучаем</param>
    /// <param name="start_x">левая верхняя координата по оси x</param>
    /// <param name="start_y">левая верхняя координата по оси y</param>
    /// <param name="window">размер окна</param>
    /// <returns></returns>
    private bool HasBlackPixel(DirectBitmap image, int start_x, int start_y, int window)
    {
      for (int i = start_x; i < start_x + window; i++)
      {
        for (int j = start_y; j < start_y + window; j++)
        {
          var color = image.GetPixel(i, j);
          if (color.B == 0 && color.R == 0 && color.G == 0)
          {
            return true;
          }
        }
      }

      return false;
    }

    /// <summary>
    /// Вычисление мультифрактального спектра: создание уровней и измерение их размерности
    /// </summary>
    /// <param name="image">изображение для анализа</param>
    /// <returns></returns>
    internal Dictionary<double, double> CalculateSpectrum(Bitmap image_before, ConverterType type)
    {
      double singularityStep = 0;
      double currentLayerSingularity = 0;
      int layersCounter = 0;
      var spectrum = new Dictionary<double, double>();

      DirectBitmap image = ImageConverter.ConvertBitmap(image_before, type);
      var layers = CreateLayers(image, type, ref currentLayerSingularity, ref singularityStep);

      foreach (var layer in layers)
      {
        double measure = CreateAndMeasureLayer(image, layer, layersCounter);
        //TODO: избавиться от этой проверки
        if (!double.IsNaN(measure))
        {
          spectrum.Add(currentLayerSingularity, measure);
        }

        layersCounter++;
        currentLayerSingularity += singularityStep;
      }

      return spectrum;
    }
  }
}