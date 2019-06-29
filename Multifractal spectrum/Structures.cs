namespace Multifractal_spectrum
{
  internal struct Point
  {
    internal int X { get; private set; }
    internal int Y { get; private set; }

    internal Point(int x, int y)
    {
      X = x;
      Y = y;
    }
  }

  internal struct Interval
  {
    internal double Begin { get; private set; }
    internal double End { get; private set; }

    internal Interval(double begin, double end)
    {
      Begin = begin;
      End = end;
    }
  }
}
