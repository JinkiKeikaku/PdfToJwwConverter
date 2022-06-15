using System.Collections.Generic;

namespace PdfToJww.CadMath2D
{
    /// <summary>
    /// 曲線
    /// </summary>
    public static class Curve
    {
        /// <summary>
        /// ２次のベジェ曲線
        /// </summary>
        /// <param name="p0">始点</param>
        /// <param name="p1">制御点</param>
        /// <param name="p2">終点</param>
        /// <param name="div">分割数</param>
        /// <param name="includeLastPoint">終点を含むか？</param>
        /// <returns>点のリスト</returns>
        public static List<CadPoint> CreateBezier2(CadPoint p0, CadPoint p1, CadPoint p2, int div, bool includeLastPoint)
        {
            var points = new List<CadPoint>();
            var dt = 1.0 / div;
            for (var i = 0; i < div; i++)
            {
                var t = i * dt;
                var tt = 1 - t;
                var x = tt * tt * p0.X + 2 * tt * t * p1.X + t * t * p2.X;
                var y = tt * tt * p0.Y + 2 * tt * t * p1.Y + t * t * p2.Y;
                points.Add(new CadPoint(x, y));
            }
            if (includeLastPoint)
            {
                points.Add(p2.Copy());
            }
            return points;
        }

        /// <summary>
        /// 3次のベジェ曲線
        /// </summary>
        /// <param name="p0">始点</param>
        /// <param name="p1">制御点1</param>
        /// <param name="p2">制御点2</param>
        /// <param name="p3">終点</param>
        /// <param name="div">分割数</param>
        /// <param name="includeLastPoint">終点を含むか？</param>
        /// <returns>点のリスト</returns>
        public static List<CadPoint> CreateBezier3(CadPoint p0, CadPoint p1, CadPoint p2, CadPoint p3, int div, bool includeLastPoint)
        {
            var points = new List<CadPoint>();
            var dt = 1.0 / div;
            for (var i = 0; i < div; i++)
            {
                var t = i * dt;
                var tt = 1 - t;
                var x = tt * tt * tt * p0.X + 3 * tt * tt * t * p1.X + 3 * tt * t * t * p2.X + t * t * t * p3.X;
                var y = tt * tt * tt * p0.Y + 3 * tt * tt * t * p1.Y + 3 * tt * t * t * p2.Y + t * t * t * p3.Y;
                points.Add(new CadPoint(x, y));
            }
            if (includeLastPoint)
            {
                points.Add(p3.Copy());
            }
            return points;
        }

        /// <summary>
        /// カーディナルスプライン
        /// </summary>
        /// <param name="points">頂点のリスト</param>
        /// <param name="isClosed">閉じているか</param>
        /// <param name="tension">テンション</param>
        /// <param name="div">分割数</param>
        /// <returns>スプラインの点列</returns>
        public static List<CadPoint> CreateCardinalSpline(
            IReadOnlyList<CadPoint> points, bool isClosed, double tension, int div)
        {
            var tmp = new List<CadPoint>();
            tmp.AddRange(points);
            return isClosed ? CreateSegmentPointsClose(tmp, tension, div) : CreateSegmentPointsOpen(tmp, tension, div);
        }

        private static List<CadPoint> CreateSegmentPointsOpen(
            IReadOnlyList<CadPoint> points, double tension, int div)
        {
            var pts = new List<CadPoint>();
            int n = points.Count;
            if (n < 2) return pts;
            var dt = 1.0 / div;
            var p0 = points[0];
            for (var i = 0; i < n - 2; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[i + 2];
                var t = 0.0;
                for (var k = 0; k < div; k++)
                {
                    pts.Add(Cardinal(p0, p1, p2, p3, t, tension));
                    t += dt;
                }
                p0 = p1;
            }
            {
                var t = 0.0;
                for (var k = 0; k < div + 1; k++)
                {
                    pts.Add(Cardinal(p0, points[n - 2], points[n - 1], points[n - 1], t, tension));
                    t += dt;
                }
            }
            return pts;
        }

        private static List<CadPoint> CreateSegmentPointsClose(
            IReadOnlyList<CadPoint> points, double tension, int div)
        {
            var pts = new List<CadPoint>();
            int n = points.Count;
            if (n < 2) return pts;
            var dt = 1.0 / div;
            var p0 = points[n - 1];
            for (var i = 0; i < n - 2; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[i + 2];
                var t = 0.0;
                for (var k = 0; k < div; k++)
                {
                    pts.Add(Cardinal(p0, p1, p2, p3, t, tension));
                    t += dt;
                }
                p0 = p1;
            }
            {
                var t = 0.0;
                for (var k = 0; k < div; k++)
                {
                    pts.Add(Cardinal(p0, points[n - 2], points[n - 1], points[0], t, tension));
                    t += dt;
                }
                t = 0.0;
                for (var k = 0; k < div + 1; k++)
                {
                    pts.Add(Cardinal(points[n - 2], points[n - 1], points[0], points[1], t, tension));
                    t += dt;
                }
            }
            return pts;
        }

        private static CadPoint Cardinal(CadPoint p0, CadPoint p1, CadPoint p2, CadPoint p3, double t, double tension)
        {
            var x = Cardinal(p0.X, p1.X, p2.X, p3.X, t, tension);
            var y = Cardinal(p0.Y, p1.Y, p2.Y, p3.Y, t, tension);
            return new CadPoint(x, y);
        }

        private static double Cardinal(double x0, double x1, double x2, double x3, double t, double tension)
        {
            var m0 = (x2 - x0) * tension;
            var m1 = (x3 - x1) * tension;
            var d = x1 - x2;
            var a = 2.0 * d + m0 + m1;
            var b = -3.0 * d - 2.0 * m0 - m1;
            return ((a * t + b) * t + m0) * t + x1;
        }
    }
}
