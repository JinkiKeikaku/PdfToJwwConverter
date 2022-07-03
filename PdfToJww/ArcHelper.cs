using PdfToJww.CadMath2D;
using PdfToJww.Shape;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww
{
    static class ArcHelper
    {
        static CadPointHelper.CircleParam? ChecksArc(PBezierShape s)
        {
            double mArcEpsilon = 0.1;
            var p0 = s.P0;
            var p1 = s.P1;
            var p2 = s.P2;
            var p3 = s.P3;
            var pts = Curve.CreateBezier3(p0, p1, p2, p3, 4, true);
            var cp = CadPointHelper.CreateCircleFrom3P(pts[0], pts[2], pts[^1]);
            if (!double.IsFinite(cp.Radius) || !cp.Center.IsFinite()) return null;
            var dr1 = (pts[1] - cp.Center).Hypot() - cp.Radius;
            var dr2 = (pts[^2] - cp.Center).Hypot() - cp.Radius;
            if (CadMath.FloatEQ(dr1, 0.0, mArcEpsilon) && CadMath.FloatEQ(dr2, 0.0, mArcEpsilon)) return cp;
            return null;
        }
        static double mArcEpsilon = 0.1;
        static double mRadiusEpsilon = 0.5;

        static public void ConvertArc(List<PShape?> shapes)
        {
            double radius = 0;
            CadPoint center = new();
            CadPoint startPoint = new();
            CadPoint endPoint = new();

            var buffer = new List<(int, CadPoint, CadPoint)>();
            for (var i = 0; i < shapes.Count; i++)
            {
                var s = shapes[i] as PBezierShape;
                if (s == null) continue;
                var (cp, pts) = CheckArc(s);
                if (CadPoint.PointEQ(pts[0], pts[4], mArcEpsilon))
                {
                    //時々無意味な小さい曲線が存在することがある。出来れば残したいが、削除する。
                    shapes[i] = null;
                }
                else if (cp != null)
                {
                    var a1 = (pts[0] - cp.Center).GetAngle360();
                    var a2 = (pts[pts.Count / 2] - cp.Center).GetAngle360();
                    var (ps, pe) = CadMath.FloatLT(CadMath.SubtractAngleDeg(a2, a1), 180) ?
                        (s.P0, s.P3) : (s.P3, s.P0);
                    if (buffer.Count == 0)
                    {
                        buffer.Add((i, ps, pe));
                        radius = cp.Radius;
                        center = cp.Center;
                        startPoint = ps;
                        endPoint = pe;
                        continue;
                    }
                    if (CadMath.FloatEQ(radius - cp.Radius, 0.0, mRadiusEpsilon) && CadPoint.PointEQ(center, cp.Center, mRadiusEpsilon))
                    {
                        var f = false;
                        if (CadPoint.PointEQ(endPoint, ps, mArcEpsilon))
                        {
                            endPoint = pe;
                            buffer.Add((i, ps, pe));
                            f = true;
                        }
                        else if (CadPoint.PointEQ(startPoint, pe, mArcEpsilon))
                        {
                            startPoint = ps;
                            buffer.Insert(0, (i, ps, pe));
                            f = true;
                        }
                        if (f && CadPoint.PointEQ(startPoint, endPoint, mArcEpsilon))
                        {
                            var circle = new PArcShape();
                            cp = CadPointHelper.CreateCircleFrom3P(
                                buffer[0].Item2, buffer[buffer.Count / 2].Item2, buffer[^1].Item2);
                            circle.Radius = cp.Radius;
                            circle.P0 = cp.Center;
                            circle.StartAngle = 0;
                            circle.SweepAngle = 360;
                            circle.StrokeColor = s.StrokeColor;
                            circle.StrokeWidth=s.StrokeWidth;
                            circle.StrokePatttern = s.StrokePatttern;
                            shapes[buffer[0].Item1] = circle;
                            for (int j = 1; j < buffer.Count; j++)
                            {
                                shapes[buffer[j].Item1] = null;
                            }
                            buffer.Clear();
                        }
                        if (f) continue;
                    }
                    MakeArc(shapes, buffer, center, radius, startPoint, endPoint);
                    buffer.Clear();
                    buffer.Add((i, ps, pe));
                    radius = cp.Radius;
                    center = cp.Center;
                    startPoint = ps;
                    endPoint = pe;
                }
            }
            if (buffer.Count == 0) return;
            MakeArc(shapes, buffer, center, radius, startPoint, endPoint);
        }

        static (CadPointHelper.CircleParam?, List<CadPoint>) CheckArc(PBezierShape s)
        {
            var p0 = s.P0;
            var p1 = s.P1;
            var p2 = s.P2;
            var p3 = s.P3;
            var pts = Curve.CreateBezier3(p0, p1, p2, p3, 4, true);
            var cp = CadPointHelper.CreateCircleFrom3P(pts[0], pts[2], pts[4]);
            if (double.IsFinite(cp.Radius) && cp.Center.IsFinite() && !CadPoint.PointEQ(p0, p3))
            {
                var dr1 = (pts[1] - cp.Center).Hypot() - cp.Radius;
                var dr2 = (pts[3] - cp.Center).Hypot() - cp.Radius;
                if (CadMath.FloatEQ(dr1, 0.0, mArcEpsilon) && CadMath.FloatEQ(dr2, 0.0, mArcEpsilon))
                {
                    return (cp, pts);
                }
            }
            return (null, pts);
        }

        static void MakeArc(
            List<PShape?> shapes, List<(int, CadPoint, CadPoint)> buffer,
            CadPoint center, double radius, CadPoint startPoint, CadPoint endPoint)
        {
            var arc = new PArcShape();
            if (buffer.Count == 1)
            {
                arc.Radius = radius;
                arc.P0 = center;
            }
            else
            {
                var pp = CadPointHelper.CreateCircleFrom3P(
                    startPoint, buffer[buffer.Count / 2].Item2, endPoint);
                arc.Radius = pp.Radius;
                arc.P0 = pp.Center;
            }
            var sa = (startPoint - arc.P0).GetAngle360();
            var ea = (endPoint - arc.P0).GetAngle360();
            arc.StartAngle = sa;
            arc.SweepAngle = CadMath.SubtractAngleDeg(ea, sa);
            var s = shapes[buffer[0].Item1] as PBezierShape;
            if (s != null)
            {
                arc.StrokeWidth = s.StrokeWidth;
                arc.StrokeColor = s.StrokeColor;
                arc.StrokePatttern = s.StrokePatttern;
            }
            shapes[buffer[0].Item1] = arc;
            for (int j = 1; j < buffer.Count; j++)
            {
                shapes[buffer[j].Item1] = null;
            }
            buffer.Clear();
        }

    }
}
