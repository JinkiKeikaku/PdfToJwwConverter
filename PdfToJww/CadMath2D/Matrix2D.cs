using System;
using static PdfToJww.CadMath2D.CadMath;

namespace PdfToJww.CadMath2D
{
    /// <summary>
    /// 拡大縮小、回転使う行列。
    /// 
    /// <para>|A11 A12|</para>
    /// <para>|A21 A22|</para>
    /// </summary>
    public class Matrix2D
    {
        /// <summary>
        /// 要素
        /// <para>|A11 A12|</para>
        /// <para>|A21 A22|</para>
        /// </summary>
        public double A11, A12, A21, A22;

        /// <summary>
        /// 逆行列が存在するか？
        /// </summary>
        public bool IsInvertible => !FloatEQ(A11 * A22 - A12 * A21, 0);

        /// <summary>
        /// 恒等行列か？
        /// </summary>
        public bool IsIdentity => A11 == 1.0 && A12 == 0.0 && A21 == 0.0 && A22 == 1.0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Matrix2D() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Matrix2D(double a11, double a12, double a21, double a22)
        {
            A11 = a11;
            A12 = a12;
            A21 = a21;
            A22 = a22;
        }

        /// <summary>
        /// この行列に回転操作を行います。
        /// </summary>
        public void Rotate(double rad)
        {
            var c = Math.Cos(rad);
            var s = Math.Sin(rad);
            var a11 = A11 * c - A21 * s;
            var a12 = A12 * c - A22 * s;
            var a21 = A11 * s + A21 * c;
            var a22 = A12 * s + A22 * c;
            A11 = a11;
            A12 = a12;
            A21 = a21;
            A22 = a22;
        }

        /// <summary>
        /// 行列を転置します。
        /// </summary>
        public void Transpose()
        {
            (A12, A21) = (A21, A12);
        }

        /// <summary>
        /// 転置行列を返します。このオブジェクトの値は変わりません。。
        /// </summary>
        public Matrix2D TransposedMatrix() => new Matrix2D(A11, A21, A12, A22);

        /// <summary>
        /// 行列を逆行列にします。
        /// </summary>
        public void Invert()
        {
            var d = A11 * A22 - A12 * A21;
            if (FloatEQ(d, 0.0))
            {
                throw new Exception("Matrix2D:Matrix not Invertible.");
            }
            (A11, A12, A21, A22) = (A22 / d, -A12 / d, -A21 / d, A11 / d);
        }

        /// <summary>
        /// 逆行列を返します。このオブジェクトは変更されません。逆行列が存在しない場合は例外が発生します。
        /// /// </summary>
        public Matrix2D InvertedMatrix()
        {
            var d = A11 * A22 - A12 * A21;
            if (FloatEQ(d, 0.0))
            {
                throw new Exception("Matrix2D:Matrix not Invertible.");
            }
            return new Matrix2D(A22 / d, -A12 / d, -A21 / d, A11 / d);
        }

        /// <summary>
        /// コピーを返します。
        /// </summary>
        /// <returns></returns>
        public Matrix2D Copy() => new Matrix2D(A11, A12, A21, A22);

        /// <summary>
        /// 加算
        /// </summary>
        public static Matrix2D operator +(Matrix2D m1, Matrix2D m2) =>
            new(m1.A11 + m2.A11, m1.A12 + m2.A12, m1.A21 + m2.A21, m1.A22 + m2.A22);
        /// <summary>
        /// 減算
        /// </summary>
        public static Matrix2D operator -(Matrix2D m1, Matrix2D m2) =>
            new(m1.A11 - m2.A11, m1.A12 - m2.A12, m1.A21 - m2.A21, m1.A22 - m2.A22);

        /// <summary>
        /// マイナス
        /// </summary>
        public static Matrix2D operator -(Matrix2D m1) =>
            new(-m1.A11, -m1.A12, -m1.A21, -m1.A22);

        /// <summary>
        /// マトリックスの乗算。
        /// </summary>
        public static Matrix2D operator *(Matrix2D m1, Matrix2D m2) =>
            new(
                m1.A11 * m2.A11 + m1.A12 * m2.A21,
                m1.A11 * m2.A12 + m1.A12 * m2.A22,
                m1.A21 * m2.A11 + m1.A22 * m2.A21,
                m1.A21 * m2.A12 + m1.A22 * m2.A22);

        /// <summary>
        /// 点をマトリックスで座標変換。
        /// </summary>
        public static CadPoint operator *(Matrix2D m, CadPoint p) =>
            new(m.A11 * p.X + m.A12 * p.Y, m.A21 * p.X + m.A22 * p.Y);
    }
}
