using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;

namespace Multifractal_spectrum
{
  internal class SpectrumBuilder
  {
    //TODO: вся логика, завязанная на maxWindowSize, должна исчезнуть, исправление на Python
    private const int maxWindowSize = 7;

    /// <summary>
    /// Создание изображения, соответствующего данному уровню, и его измерение
    /// </summary>
    /// <param name="image">исходное изображение</param>
    /// <param name="layer">множество уровня</param>
    /// <returns></returns>
    private double CreateAndMeasureLayer(DirectBitmap image, Layer layer, int layerNumber)
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

      foreach (Point point in layer.Points)
      {
        layerImage.SetPixel(point.X, point.Y, Color.FromArgb(255, 0, 0, 0));
      }

      string layerName = "layer " + Math.Round(layer.SingularityBounds.Begin, 2).ToString()
         + "  " + Math.Round(layer.SingularityBounds.End, 2).ToString();
      string pathToImage = Path.Combine(Program.actualLayersPath, layerName + ".jpg");

      layerImage.Bitmap.Save(pathToImage);

      return CalculateMeasure(layerImage);
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
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="type">Тип конвертера</param>
    /// <param name="singularityBounds">Интервал сингулярности</param>
    /// <param name="singularityStep">Шаг сингулярности</param>
    /// <returns></returns>
    internal Dictionary<double, double> CalculateSpectrum(
      DirectBitmap image,  
      List<Layer> layers,
      Interval singularityBounds,
      double singularityStep)
    {    
      double currentLayerSingularity = singularityBounds.Begin;
      int layersCounter = 0;
      var spectrum = new Dictionary<double, double>();

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