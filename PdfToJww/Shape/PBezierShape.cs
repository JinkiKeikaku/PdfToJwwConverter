using PdfToJww.CadMath2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww.Shape
{
    class PBezierShape : PShape
    {
        public CadPoint P0 = new();
        public CadPoint P1 = new();
        public CadPoint P2 = new();
        public CadPoint P3 = new();
        public double StrokeWidth;
        public Color StrokeColor;
        public float[] StrokePatttern = Array.Empty<float>();

        public PBezierShape()
        {
        }

        public PBezierShape(CadPoint p0, CadPoint p1, CadPoint p2, CadPoint p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public override void Transform(TransformMatrix m)
        {
            P0 = m * P0;
            P1 = m * P1;
            P2 = m * P2;
            P3 = m * P3;
        }

        public override string ToString() => $"Bezier({P0} {P1} {P2} {P3})";

    }
}
