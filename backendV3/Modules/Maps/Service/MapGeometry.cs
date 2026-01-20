using NetTopologySuite.Geometries;

namespace BackendV3.Modules.Maps.Service;

public static class MapGeometry
{
    public static Point MakePoint(double x, double y)
    {
        return new Point(x, y) { SRID = 0 };
    }

    public static LineString MakeLine(Point from, Point to)
    {
        return new LineString(new[] { new Coordinate(from.X, from.Y), new Coordinate(to.X, to.Y) }) { SRID = 0 };
    }

    public static double Length(LineString line)
    {
        return line.Length;
    }

    public static Point? PointAlong(LineString line, double distance)
    {
        var total = line.Length;
        if (total <= 0) return null;
        var d = Math.Clamp(distance, 0, total);

        var a = line.Coordinates.First();
        var b = line.Coordinates.Last();
        var t = d / total;
        return new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t) { SRID = 0 };
    }
}

