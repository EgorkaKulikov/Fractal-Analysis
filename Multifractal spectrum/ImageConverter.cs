﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multifractal_spectrum
{
  public enum ConverterType
  {
    Grayscale,
    RGB_R,
    RGB_G,
    RGB_B,
    HSV
  }

  public static class ImageConverter
  {
    public static DirectBitmap ConvertBitmap(Bitmap bitmap, ConverterType type)
    {
      DirectBitmap directBitmap = new DirectBitmap(bitmap.Width, bitmap.Height);

      for (int x = 0; x < bitmap.Width; x++)
      {
        for (int y = 0; y < bitmap.Height; y++)
        {
          var pixel = bitmap.GetPixel(x, y);
          Tuple<byte, byte, byte> color = ConvertPixel(pixel, type);

          directBitmap.SetPixel(x, y, Color.FromArgb(pixel.A, color.Item1, color.Item2, color.Item3));
        }
      }

      return directBitmap;
    }

    private static Tuple<byte, byte, byte> ConvertPixel(Color pixel, ConverterType type)
    {
      Tuple<byte, byte, byte> newColor = Tuple.Create((byte)0, (byte)0, (byte)0);
      byte r = 0, g = 0, b = 0;

      switch (type)
      {
        case ConverterType.Grayscale:
          byte middle = (byte)((pixel.R + pixel.G + pixel.B) / 3);
          r = middle;
          g = middle;
          b = middle;
          break;
        case ConverterType.RGB_R:
          r = pixel.R;
          break;
        case ConverterType.RGB_G:
          g = pixel.G;
          break;
        case ConverterType.RGB_B:
          b = pixel.B;
          break;
        case ConverterType.HSV:
          r = pixel.R;
          g = pixel.G;
          b = pixel.B;
          break;
      }

      return Tuple.Create(r,g,b);
    }
  }
}