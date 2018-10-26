using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;

namespace Multifractal_spectrum
{
  internal sealed class Program
  {
    internal static string imagePath = "D:\\Pictures";
    internal const string spectrumFileName = "spectrum.txt";
    internal static string actualLayersPath { get; set; }

    static void Main(string[] args)
    {
      DateTime before = DateTime.Now;
      Console.WriteLine(@"Создайте папку D:\\Pictures, сохраните в ней тестовое изображение. 
В этой же папке будут сохранены изображения, соответствующие множествам уровня");
      Console.WriteLine("\nВведите имя файла, например, 1.jpg");
      Console.WriteLine(@"Если вы хотите использовать другой путь, введите его целиком в формате
C:\test\image1.jpg");
      Console.WriteLine();
      string input = Console.ReadLine();
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

      int converterType;
      int.TryParse(Console.ReadLine(), out converterType);

      int existedDirectories = (new DirectoryInfo(imagePath)).GetDirectories().Length;
      int directoryNumber = GetDirectoryNumber(imagePath);
      actualLayersPath = Path.Combine(imagePath, "Layers ") + directoryNumber.ToString();
      Directory.CreateDirectory(actualLayersPath);

      Console.WriteLine("\nВычисляются показатели сингулярности...");
      MainClass mainClass = new MainClass();

      //Создание изображения, вычисление спектра и множеств уровня
      Bitmap image = (Bitmap)Image.FromFile(path);
      string spectrum = mainClass.CalculateSpectrum(image, (ConverterType)(converterType - 1));

      Console.WriteLine("\nМножества уровня построены");
      Console.WriteLine("Номер папки с множествами уровня : {0}", directoryNumber);
      Console.WriteLine("Мультифрактальный спектр вычислен и находится в файле spectrum.txt");

      //Сохранение спектра в текстовый файл
      string actualSpectrumPath = Path.Combine(actualLayersPath, spectrumFileName);
      using (StreamWriter sw = new StreamWriter(actualSpectrumPath, true))
      {
        sw.WriteLine("*********************");
        sw.WriteLine(spectrum);
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
