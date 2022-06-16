using PdfToJww.CadMath2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww.Shape
{
    abstract class PShape
    {
        public abstract void Transform(TransformMatrix m);

        public virtual void Offset(CadPoint dp)
        {
            var m = new TransformMatrix(1, 0, 0, 1, dp.X, dp.Y);
            Transform(m);
        }

        public virtual void Rotate(CadPoint p0, double rad)
        {
            var c = Math.Cos(rad);
            var s = Math.Sin(rad);
            var m = new TransformMatrix(
                c, -s, s, c,
                p0.X - p0.X * c + p0.Y * s,
                p0.Y - p0.X * s - p0.Y * c
            );
            Transform(m);
        }

        public virtual void Magnify(CadPoint p0, double mx, double my)
        {
            var m = new TransformMatrix(mx, 0, 0, my, -mx * p0.X + p0.X, -my * p0.Y + p0.Y);
            Transform(m);
        }
    }
}
