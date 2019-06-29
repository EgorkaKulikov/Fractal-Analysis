using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Text;

namespace Multifractal_spectrum
{
  internal sealed class Program
  {
    //TODO: при переезде на Python должно появится ГУИ и решить проблему хардкода пути
    private static string imagePath = "D:\\Pictures";
    private const string spectrumFileName = "spectrum.txt";
    internal static string LayersDirectoryPath { get; private set; }

    static void Main(string[] args)
    {
      DateTime before = DateTime.Now;
      Console.WriteLine(@"Создайте папку D:\\Pictures, сохраните в ней тестовое изображение. 
В этой же папке будут сохранены изображения, соответствующие множествам уровня");
      Console.WriteLine("\nВведите имя файла, например, 1.jpg");
      Console.WriteLine(@"Если вы хотите использовать другой путь, введите его целиком в формате
C:\test\image1.jpg");
      Console.WriteLine();
      string input = "2.jpg";//Console.ReadLine();
      if (input.Contains(":"))
      {
        imagePath = Path.GetDirectoryName(input);
      }
      string path = Path.Combine(imagePath, input);

      Console.WriteLine("Введите номер желаемого алгоритма обработки изображения:");
      Console.WriteLine("1) монохромное изображение");
      Console.WriteLine("2) красная компонента RGB");
      Console.WriteLine("3) зелёная компонента RGB");
      Console.WriteLine("4) синяя компонента RGB");
      Console.WriteLine("5) компонента Hue палитры HSV");
      Console.WriteLine();

      int converterNumber;
      int.TryParse("1"/*Console.ReadLine()*/, out converterNumber);
      ConverterType converterType = (ConverterType)(converterNumber - 1);

      int directoryNumber = GetDirectoryNumber(imagePath);
      LayersDirectoryPath = Path.Combine(imagePath, "Layers ") + directoryNumber.ToString();
      Directory.CreateDirectory(LayersDirectoryPath);
      
      SpectrumBuilder spectrumBuilder = new SpectrumBuilder();
      LayersBuilder layersBuilder = new LayersBuilder();


      Bitmap image_before = (Bitmap)Image.FromFile(path);
      DirectBitmap image = ImageConverter.ConvertBitmap(image_before, converterType);

      //Вычисление показателей сингулярности

      Console.WriteLine("\nВычисляются показатели сингулярности...");
      var singularityBounds = layersBuilder.GetSingularityBounds(image, converterType);

      Console.WriteLine("Minimal singularity:   {0:0.00}", singularityBounds.Begin);
      Console.WriteLine("Maximal singularity:   {0:0.00}", singularityBounds.End);

      Console.WriteLine("Введите шаг между уровнями, например, 0,2");
      double singulatityStep = double.Parse("0,2"/*Console.ReadLine()*/);

      //Вычисление множеств уровня

      var layers = layersBuilder.SplitByLayers(singularityBounds, singulatityStep);

      //Вычисление спектра

      Console.WriteLine("\nВычисляются множества уровня...");

      var spectrum = spectrumBuilder.CalculateSpectrum(image, layers, singularityBounds, singulatityStep);

      Console.WriteLine("\nМножества уровня построены");
      Console.WriteLine("Номер папки с множествами уровня : {0}", directoryNumber);
      Console.WriteLine("Мультифрактальный спектр вычислен и находится в файле spectrum.txt");

      //Сохранение спектра в текстовый файл
      string actualSpectrumPath = Path.Combine(LayersDirectoryPath, spectrumFileName);
      using (StreamWriter sw = new StreamWriter(actualSpectrumPath, true))
      {
        sw.WriteLine("*********************");
        var outputInfo = new StringBuilder();
        foreach (var layerInfo in spectrum)
        {
          outputInfo.Append(string.Format("{0:0.00 }", layerInfo.Key));
          outputInfo.Append(string.Format("{0:0.00}\r\n", layerInfo.Value));
        }
        sw.WriteLine(outputInfo);
        sw.Close();
      }

      DateTime after = DateTime.Now;
      string s = (after - before).ToString();

      Console.WriteLine("\nЖелаем вам всего доброго!");
      Console.ReadKey();
    }

    private static int GetDirectoryNumber(string imagePath)
    {
      List<int> usedValues = new List<int>();

      DirectoryInfo imageDirectory = new DirectoryInfo(imagePath);
      foreach (DirectoryInfo layersDirectory in imageDirectory.GetDirectories())
      {
        string name = layersDirectory.Name;
        if (name.IndexOf(' ') >= 0)
        {
          int current = int.Parse(name.Split(' ')[1]);
          usedValues.Add(current);
        }
      }
      usedValues.Sort();

      if (usedValues.Count > 0)
      {
        return usedValues[usedValues.Count - 1] + 1;
      }

      return 0;
    }
  }
}
