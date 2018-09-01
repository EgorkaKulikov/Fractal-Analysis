using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Multifractal_spectrum
{
  class MainClass
  {
    private double LayerStep = 0;
    private double CurrentLayerValue = 0;

    internal List<Tuple<double, double>> LayersSingularities = new List<Tuple<double, double>>();

    /// <summary>
    /// Создание множеств уровня по исходному изображению
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    private List<List<Tuple<int, int>>> CreateLayerPoints(DirectBitmap image, ConverterType type)
    {
      Dictionary<Tuple<int, int>, double> densities = CalculateDensity(image, type);
      List<List<Tuple<int, int>>> layers = new List<List<Tuple<int, int>>>();

      var sortedValues = densities.Values.ToList();
      sortedValues.Sort();
      double min = sortedValues[0];
      double max = sortedValues[sortedValues.Count - 1];

      Console.WriteLine("Minimal singularity:   {0:0.00}", min);
      Console.WriteLine("Maximal singularity:   {0:0.00}", max);

      Console.WriteLine("Введите шаг между уровнями, например, 0,2");
      double step = double.Parse(Console.ReadLine());

      Console.WriteLine("\nВычисляются множества уровня...");

      double layerStep = step;
      LayerStep = step;
      CurrentLayerValue = min;


      for (double i = min; i <= max; i += layerStep)
      {
        LayersSingularities.Add(Tuple.Create(i, i + layerStep));

        List<Tuple<int, int>> layer = new List<Tuple<int, int>>();
        foreach (Tuple<int, int> point in densities.Keys)
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
    private double CreateAndMeasureLayer(DirectBitmap image, List<Tuple<int, int>> layerPoints, int layerNumber)
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

      foreach (Tuple<int, int> point in layerPoints)
      {
        layerImage.SetPixel(point.Item1, point.Item2, Color.FromArgb(255, 0, 0, 0));
      }

      string layerName = "layer " + Math.Round(LayersSingularities[layerNumber].Item1, 2).ToString()
         + "  " + Math.Round(LayersSingularities[layerNumber].Item2, 2).ToString();
      string pathToImage = Path.Combine(Program.actualLayersPath, layerName + ".jpg");

      layerImage.Bitmap.Save(pathToImage);

      return CalculateMeasure(layerImage);
    }

    private const int maxWindowSize = 7;

    /// <summary>
    /// Вычисление функции плотности для всех точек изображения
    /// </summary>
    /// <param name="image">исходное изображение</param>
    /// <returns></returns>
    private Dictionary<Tuple<int, int>, double> CalculateDensity(DirectBitmap image, ConverterType type)
    {
      Dictionary<Tuple<int, int>, double> densities = new Dictionary<Tuple<int, int>, double>();

      for (int i = maxWindowSize; i < image.Width - maxWindowSize; i++)
      {
        for (int j = maxWindowSize; j < image.Height - maxWindowSize; j++)
        {
          var coord = Tuple.Create(i, j);
          double density = CalculateDensityInPoint(coord, image, type);

          densities.Add(Tuple.Create(i - maxWindowSize, j - maxWindowSize), density);
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
    private double CalculateDensityInPoint(Tuple<int, int> point, DirectBitmap image, ConverterType type)
    {
      int[] windows = { 2, 3, 4, 5, 7 };
      for (int i = 0; i < windows.Length; i++)
      {
        if (windows[i] > maxWindowSize)
          windows[i] = maxWindowSize;
      }
      int n = windows.Length;
      double xsum = 0, ysum = 0, xsqrsum = 0;
      double xysum = 0;

      foreach (int eps in windows)
      {
        double intens = CalculateIntensivity(image, point, eps, type);

        double x = Math.Log(2 * eps + 1);
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
    /// <param name="window">размер окна</param>
    /// <returns></returns>
    private double CalculateIntensivity(DirectBitmap image, Tuple<int, int> point, int window, ConverterType type)
    {
      double intensivity = 0;
      int x = point.Item1;
      int y = point.Item2;

      for (int i = x - window; i <= x + window; i++)
      {
        for (int j = y - window; j <= y + window; j++)
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
      double xsum = 0, ysum = 0, xsqrsum = 0;
      double xysum = 0;

      foreach (int eps in windows)
      {
        double intens = CalculateBlackWindows(image, eps);

        double x = Math.Log(1.0 / eps);
        double y = Math.Log(intens + 1);

        if (!double.IsNaN(x) && !double.IsNaN(y)
            && !double.IsInfinity(x) && !double.IsInfinity(y))
        {
          xsum += x;
          ysum += y;
          xsqrsum += x * x;
          xysum += x * y;
        }
      }

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
          blackWindows += ProduceWindow(image, i, j, window);
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
    private double ProduceWindow(DirectBitmap image, int start_x, int start_y, int window)
    {
      int black = 0, white = 0;

      for (int i = start_x; i < start_x + window; i++)
      {
        for (int j = start_y; j < start_y + window; j++)
        {
          Color color = image.GetPixel(i, j);
          if (color.B == 0 && color.R == 0 && color.G == 0)
          {
            black++;
          }
          else
          {
            white++;
          }
        }
      }

      if (black > 0) return 1; else return 0;
    }

    /// <summary>
    /// Вычисление мультифрактального спектра: создание уровней и измерение их размерности
    /// </summary>
    /// <param name="image">изображение для анализа</param>
    /// <returns></returns>
    internal string CalculateSpectrum(Bitmap image_before, ConverterType type)
    {
      DirectBitmap image = ImageConverter.ConvertBitmap(image_before, type);

      var layers = CreateLayerPoints(image, type);

      int totalPoints = layers.Sum(layer => layer.Count);

      StringBuilder sb = new StringBuilder();
      int layersCounter = 0;
      foreach (List<Tuple<int,int>> layer in layers)
      {
        double measure = CreateAndMeasureLayer(image, layer, layersCounter);
        if (!double.IsNaN(measure))
        {
          sb.Append(string.Format("{0:0.00 }", CurrentLayerValue));
          sb.Append(string.Format("{0:0.00}\r\n", measure));
        }

        layersCounter++;
        CurrentLayerValue += LayerStep;
      }

      return sb.ToString();
    }
  }
}