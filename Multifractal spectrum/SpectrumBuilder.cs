﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Multifractal_spectrum
{
  internal class SpectrumBuilder
  {
    /// <summary>
    /// Вычисление мультифрактального спектра: создание уровней и измерение их размерности
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="layers">Множества уровня</param>
    /// <param name="singularityBounds">Интервал сингулярности</param>
    /// <param name="singularityStep">Шаг сингулярности</param>
    /// <returns>Мультифракальный спектр изображения</returns>
    internal Dictionary<double, double> CalculateSpectrum(
      DirectBitmap image,
      List<Layer> layers,
      Interval singularityBounds,
      double singularityStep)
    {
      double currentLayerSingularity = singularityBounds.Begin;
      var spectrum = new Dictionary<double, double>();

      foreach (var layer in layers)
      {
        double measure = CreateAndMeasureLayer(image, layer);
        spectrum.Add(currentLayerSingularity, measure);

        currentLayerSingularity += singularityStep;
      }

      return spectrum;
    }

    /// <summary>
    /// Создание изображения, соответствующего данному уровню, и его измерение
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <param name="layer">Множество уровня</param>
    /// <returns>Изображение слоя и его фрактальная размерность</returns>
    private double CreateAndMeasureLayer(DirectBitmap image, Layer layer)
    {
      DirectBitmap layerImage = new DirectBitmap(image.Width, image.Height);

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

      SaveLayerImage(layer, layerImage);
      return CalculateMeasure(layerImage);
    }

    /// <summary>
    /// Сохранение изображения множества уровня
    /// </summary>
    /// <param name="layer">Множество уровня</param>
    /// <param name="layerImage">Изображение множества уровня</param>
    private void SaveLayerImage(Layer layer, DirectBitmap layerImage)
    {
      var minSingularity = Math.Round(layer.SingularityBounds.Begin, 2).ToString();
      var maxSingularity = Math.Round(layer.SingularityBounds.End, 2).ToString();

      string layerName = string.Join(" ", new[] { "layer", minSingularity, maxSingularity, ".jpg" });
      string pathToImage = Path.Combine(Program.LayersDirectoryPath, layerName);

      layerImage.Bitmap.Save(pathToImage);
    }

    /// <summary>
    /// Вычисление фрактальной размерности изображения
    /// </summary>
    /// <param name="image">Анализируемое изображение</param>
    /// <returns>Фрактальная размерность изображения</returns>
    private double CalculateMeasure(DirectBitmap image)
    {
      int[] windows = { 3, 4, 5, 6, 7, 8 };

      var points = windows
                      .Select(windowSize =>
                              {
                                double intens = CalculateBlackWindows(image, windowSize);

                                double x = Math.Log(windowSize);
                                double y = Math.Log(intens + 1);

                                return (x, y);
                              })
                      .ToList();

      return -LeastSquares.ApplyMethod(points);
    }

    /// <summary>
    /// Подсчёт числа квадратиков, имеющих внутри себя хотя бы один чёрный пиксель
    /// </summary>
    /// <param name="image">Исследуемая область изображения</param>
    /// <param name="window">Размер окна</param>
    /// <returns>Число квадратиков, имеющих внутри себя хотя бы один чёрный пиксель </returns>
    private double CalculateBlackWindows(DirectBitmap image, int window)
    {
      double blackWindows = 0;

      for (int i = 0; i < image.Width; i += window)
      {
        for (int j = 0; j < image.Height; j += window)
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
    /// <returns>Результат проверки</returns>
    private bool HasBlackPixel(DirectBitmap image, int start_x, int start_y, int window)
    {
      for (int i = start_x; i < Math.Min(start_x + window, image.Width); i++)
      {
        for (int j = start_y; j < Math.Min(start_y + window, image.Height); j++)
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
  }
}