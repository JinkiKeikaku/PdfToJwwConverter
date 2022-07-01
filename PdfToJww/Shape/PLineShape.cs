using PdfToJww.CadMath2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww.Shape
{
    class PLineShape : PShape
    {
        public CadPoint P0 = new();
        public CadPoint P1 = new();
        public double StrokeWidth;
        public Color StrokeColor;
        public float[] StrokePatttern = Array.Empty<float>();

        public PLineShape(CadPoint p0, CadPoint p1)
        {
            P0.Set(p0);
            P1.Set(p1);
        }

        public override void Transform(TransformMatrix m)
        {
            P0 = m * P0;
            P1 = m * P1;
        }

        public override string ToString() => $"Line({P0} {P1})";
    }
}
