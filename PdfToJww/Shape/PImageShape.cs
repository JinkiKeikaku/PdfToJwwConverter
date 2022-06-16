using PdfToJww.CadMath2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww.Shape
{
    class PImageShape : PShape
    {
        public CadPoint P0 = new();     //左下
        public double Width;
        public double Height;
        public double AngleDeg;
        public Image Image;

        public PImageShape(Image image, CadPoint p0, double width, double height, double angleDeg)
        {
            Image = image;
            P0.Set(p0);
            Width = width;
            Height = height;
            AngleDeg = angleDeg;
        }

        public override void Transform(TransformMatrix m)
        {
            var m1 = new Matrix2D(m.A, m.B, m.C, m.D);
            var r = CadPointHelper.TransformedRectangle(Width, Height, AngleDeg, m1);
            Height = r.Height;
            Width = r.Width;
            AngleDeg = r.AngleDeg;
            P0 = m * P0;
        }
    }
}
