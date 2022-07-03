using PdfToJww.CadMath2D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww.Shape
{
    class PArcShape : PShape
    {
        public CadPoint P0 = new();
        public double Radius;
        public double Flatness = 1.0;
        public double Angle;
        public double StartAngle;
        public double SweepAngle;
        public double StrokeWidth;
        public Color StrokeColor;
        public float[] StrokePatttern = Array.Empty<float>();

        public PArcShape() { }

        public override void Transform(TransformMatrix m)
        {
            var m1 = new Matrix2D(m.A, m.B, m.C, m.D);
            var angle0 = CadMath.DegToRad(Angle);
            var start = CadMath.DegToRad(StartAngle);
            var end = CadMath.DegToRad(StartAngle + SweepAngle / 4.0);
            var pa = new CadPoint[] { 
                new CadPoint(Math.Cos(start), Flatness * Math.Sin(start)), 
                new CadPoint(Math.Cos(end), Flatness * Math.Sin(end)) };
            foreach (var p in pa)
            {
                p.Rotate(angle0);
                p.Set(m1 * p);
            }
            P0 = m * P0;
            if (!m1.IsInvertible)
            {
                Debug.WriteLine("`PArcShape::Transform: Matrix2D can not invert");
                return;
            }
            var oval = CadPointHelper.TransformOval(Radius, Flatness, Angle, m1);
            Radius = oval.Radius;
            Flatness = oval.Flatness;
            Angle = oval.AngleDeg;
            var angle1 = CadMath.DegToRad(Angle);
            //端点を新しい角度と扁平率で円に戻す
            foreach (var p in pa)
            {
                p.Rotate(-angle1);
                p.Y /= Flatness;
            }

            var pe = pa[1].RotatePoint(-pa[0].GetAngle());
            StartAngle = pa[0].GetAngle360();
            SweepAngle = CadMath.RadToDeg(pe.GetAngle()) * 4;
        }

        public override string ToString() => $"Arc({P0} {Radius} {StartAngle} {SweepAngle})";
    }
}
