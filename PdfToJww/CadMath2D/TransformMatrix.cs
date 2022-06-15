using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww.CadMath2D
{
    /// <summary>
    /// 拡大縮小、回転、平行移動に使う行列。
    /// 
    /// <para>|A B Tx|</para>
    /// <para>|C D Ty|</para>
    /// <para>|0 0 1|</para>
    /// </summary>
    public class TransformMatrix
    {
        /// <summary>
        /// 要素
        /// <para>|A B Tx|</para>
        /// <para>|C D Ty|</para>
        /// <para>|0 0 1|</para>
        /// </summary>
        public double A = 1.0, B = 0.0, C = 0.0, D = 1.0, Tx = 0.0, Ty = 0.0;

        /// <summary>
        /// 恒等行列か？
        /// </summary>
        public bool IsIdentity => A == 1.0 && B == 0.0 && C == 0.0 && D == 1.0 && Tx == 0.0 && Ty == 0.0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TransformMatrix() { }

        /// <summary>
        /// コンストラクタ
        /// <para>|A B Tx|</para>
        /// <para>|C D Ty|</para>
        /// <para>|0 0 1|</para>
        /// </summary>
        public TransformMatrix(double a, double b, double c, double d, double tx, double ty)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            Tx = tx;
            Ty = ty;
        }

        /// <summary>
        /// コピーを返します。
        /// </summary>
        public TransformMatrix Copy() => new TransformMatrix(A, B, C, D, Tx, Ty);

        /// <summary>
        /// 単位行列にします。
        /// </summary>
        public void Reset()
        {
            A = 1.0;
            B = 0.0;
            C = 0.0;
            D = 1.0;
            Tx = 0.0;
            Ty = 0.0;
        }

        /// <summary>
        /// 引数で指定する係数のマトリックスを前に掛けます。
        /// </summary>
        public void Transform(double a, double b, double c, double d, double tx, double ty)
        {
            (A, B, C, D, Tx, Ty) = (
                a * A + b * C, a * B + b * D,
                c * A + d * C, c * B + d * D,
                a * Tx + b * Ty + tx, c * Tx + d * Ty + ty);
        }


        /// <summary>
        /// 平行移動
        /// </summary>
        public void Translate(double x, double y)
        {
            Tx += x;
            Ty += y;
        }

        /// <summary>
        /// 拡縮
        /// </summary>
        public void Scale(double sx, double sy)
        {
            A *= sx;
            B *= sx;
            Tx *= sx;
            C *= sy;
            D *= sy;
            Ty *= sy;
        }

        /// <summary>
        /// 回転。角度はradian。前に掛けます。
        /// </summary>
        public void Rotate(double rad)
        {
            var c = Math.Cos(rad);
            var s = Math.Sin(rad);
            var a11 = A * c - C * s;
            var a12 = B * c - D * s;
            var a21 = A * s + C * c;
            var a22 = B * s + D * c;
            var a31 = Tx * c - Ty * s;
            var a32 = Tx * s + Ty * c;
            A = a11;
            B = a12;
            C = a21;
            D = a22;
            Tx = a31;
            Ty = a32;
        }


        /// <summary>
        /// 加算
        /// </summary>
        public static TransformMatrix operator +(TransformMatrix m1, TransformMatrix m2) =>
            new(m1.A + m2.A, m1.B + m2.B, m1.C + m2.C, m1.D + m2.D, m1.Tx + m2.Tx, m1.Ty + m2.Ty);

        /// <summary>
        /// 減算
        /// </summary>
        public static TransformMatrix operator -(TransformMatrix m1, TransformMatrix m2) =>
            new(m1.A - m2.A, m1.B - m2.B, m1.C - m2.C, m1.D - m2.D, m1.Tx - m2.Tx, m1.Ty - m2.Ty);

        /// <summary>
        /// 行列の積
        /// </summary>
        public static TransformMatrix operator *(TransformMatrix m1, TransformMatrix m2) =>
            new(
                m1.A * m2.A + m1.B * m2.C, m1.A * m2.B + m1.B * m2.D,
                m1.C * m2.A + m1.D * m2.C, m1.C * m2.B + m1.D * m2.D,
                m1.A * m2.Tx + m1.B * m2.Ty + m1.Tx, m1.C * m2.Tx + m1.D * m2.Ty + m1.Ty
                );

        /// <summary>
        /// 座標変換
        /// </summary>
        public static CadPoint operator *(TransformMatrix m, CadPoint p) =>
            new(m.A * p.X + m.B * p.Y + m.Tx, m.C * p.X + m.D * p.Y + m.Ty);

        public override string ToString() => $"{A} {B} {C} {D} {Tx} {Ty}";
    }
}
