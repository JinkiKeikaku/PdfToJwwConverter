using PdfToJww.CadMath2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww.Shape
{
    class PTextShape : PShape
    {
        public CadPoint P0=new();     //左下
        public double Width;    //外形の幅
        public double Height;   //外形の高さ
        public double AngleDeg; //角度（度）    
        public string Text;
        public Color Color;
        public string FontName = "ＭＳ ゴシック";

        public PTextShape(string text, CadPoint p0, double width, double height, double angleDeg)
        {
            Text = text;
            P0.Set(p0);
            Width = width;
            Height = height;
            AngleDeg = angleDeg;
        }

        public PTextShape(string text, CadPoint p0, double height, double angleDeg)
        {
            Text = text;
            P0.Set(p0);
            Height = height;
            Width = GetTextWidth(Text, Height);
            AngleDeg = angleDeg;
        }


        public override void Transform(TransformMatrix m)
        {
            //文字の幅と高さを変換。ただし、ここまでしなくてもたいていは縦横同比なのでもっと単純化してもいいかも。
            //たとえば行列式det(m1)の平方根を幅と高さにかけて角度はそのままなど。
            var m1 = new Matrix2D(m.A, m.B, m.C, m.D);
            var r = CadPointHelper.TransformedRectangle(Width, Height, AngleDeg, m1);
            Height = r.Height;
            Width = r.Width;
            AngleDeg = r.AngleDeg;
            P0 = m * P0;
        }

        public override string ToString()=>$"Text(\"{Text}\" {P0} {Width} {Height} {AngleDeg}";

        public static double GetTextWidth(string text, double height)
        {
            var w = 0.0;
            var enc = Encoding.GetEncoding("shift_jis");
            foreach (var c in text)
            {
                var k = enc.GetByteCount(c.ToString());
                w += height * k * 0.5;
            }
            return w;
        }
    }
}
