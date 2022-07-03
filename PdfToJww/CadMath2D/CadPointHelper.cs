using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using static PdfToJww.CadMath2D.CadMath;

namespace PdfToJww.CadMath2D
{
    static class CadPointHelper
    {
        public class PerpPoint
        {
            /// <summary>
            /// 垂点
            /// </summary>
            public CadPoint P;
            /// <summary>
            /// 垂点が線上にあればtrue
            /// </summary>
            public bool IsOnLine;
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="p">垂点</param>
            /// <param name="isOnLine">線上にあればtrue</param>
            public PerpPoint(CadPoint p, bool isOnLine)
            {
                P = p;
                IsOnLine = isOnLine;
            }
        }

        /// <summary>
        /// 点[p]から線分[p0]-[p1]におろした垂線の交点を返す。交点が線分上にある場合はisOnlineがtrue。
        /// 何らかの垂線は必ずあるので返値はnullにならない。
        /// </summary>
        /// <returns>PerpPointオブジェクト。</returns>
        public static PerpPoint GetPerpPoint(CadPoint p0, CadPoint p1, CadPoint p)
        {
            var dp1 = p1 - p0;
            var dp2 = p - p0;
            var d1 = dp1.Hypot();
            if (FloatEQ(d1, 0)) return new(p0.Copy(), true);
            var t = CadPoint.Dot(dp1, dp2) / (d1 * d1);
            var f = FloatGE(t, 0) && FloatLE(t, 1.0);
            var pp = dp1 * t + p0;
            return new(pp, f);
        }

        /// <summary>
        /// 矩形のパラメータ
        /// </summary>
        public class RectParam
        {
            /// <summary>
            /// 幅
            /// </summary>
            public double Width;
            /// <summary>
            /// 高さ
            /// </summary>
            public double Height;
            /// <summary>
            /// 角度（度）
            /// </summary>
            public double AngleDeg;
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="width">幅</param>
            /// <param name="height">高さ</param>
            /// <param name="angleDeg">角度（度）</param>
            public RectParam(double width, double height, double angleDeg)
            {
                Width = width;
                Height = height;
                AngleDeg = angleDeg;
            }
        }

        /// <summary>
        /// 幅[width]、高さ[height]、角度[angle](deg)の傾いた矩形を[m]で変換した矩形を返します。
        /// 返される幅と角度は底辺を拡縮した後の幅と角度、高さは左上の頂点を拡縮し、拡縮した底辺までの距離に直角。
        /// </summary>
        public static RectParam TransformedRectangle(double width, double height, double angleDeg, Matrix2D m)
        {
            if (FloatEQ(width, 0.0)) return new(width, height, angleDeg);

            var p1 = new CadPoint(width, 0);
            var p2 = new CadPoint(0, height);
            var angle = DegToRad(angleDeg);
            p1.Rotate(angle);
            p2.Rotate(angle);
            p1 = m * p1;
            p2 = m * p2;
            var w = p1.Hypot();
            var pp = GetPerpPoint(new CadPoint(), p1, p2);
            var h = (pp.P - p2).Hypot();
            var a = p1.GetAngle360();// NormalizeAngle360(RadToDeg(p1.GetAngle()));
            return new(w, h, a);
        }

        /// <summary>
        /// 円のパラメータ
        /// </summary>
        public class CircleParam
        {
            /// <summary>
            /// 中心
            /// </summary>
            public CadPoint Center;
            /// <summary>
            /// 半径
            /// </summary>
            public double Radius;

            public CircleParam(CadPoint center, double radius)
            {
                Center = center;
                Radius = radius;
            }
        }

        public static CircleParam CreateCircleFrom3P(CadPoint p0, CadPoint p1, CadPoint p2)
        {
            var dp12 = p0 - p1;
            var dp23 = p1 - p2;
            var dp31 = p2 - p0;

            CadPoint center;
            var d = 2.0 * (p0.X * dp23.Y + p1.X * dp31.Y + p2.X * dp12.Y);
            if (FloatEQ(d, 0.0))
            {
                center = (p0 + p1) * 0.5;
            }
            else
            {
                var r1 = CadPoint.Dot(p0, p0);
                var r2 = CadPoint.Dot(p1, p1);
                var r3 = CadPoint.Dot(p2, p2);
                center = (dp23 * r1 + dp31 * r2 + dp12 * r3) / d;
                center.RotateM90();
            }
            return new CircleParam(center, (p0 - center).Hypot());
        }

        /// <summary>
        /// 楕円のパラメータ
        /// </summary>
        public class OvalParam
        {
            /// <summary>
            /// 半径
            /// </summary>
            public double Radius;
            /// <summary>
            /// 扁平率
            /// </summary>
            public double Flatness;
            /// <summary>
            /// 角度（度）
            /// </summary>
            public double AngleDeg;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="radius">半径</param>
            /// <param name="flatness">扁平率</param>
            /// <param name="angleDeg">角度（度）</param>
            public OvalParam(double radius, double flatness, double angleDeg)
            {
                Radius = radius;
                Flatness = flatness;
                AngleDeg = angleDeg;
            }
        }

        /// <summary>
        /// 回転した楕円をMatrix2Dで変形した楕円を返します。
        /// 変換マトリクスは逆行列が存在する必要があります。
        /// 逆用列が存在しないなどで変換が行われない場合、例外が発生します。
        /// </summary>
        public static OvalParam TransformOval(
            double radius, double flatness, double angleDeg, Matrix2D m
        )
        {
            if (!m.IsInvertible)
            {
                throw new Exception("TransformOval:Matrix not Invertible, so not transform.");
            }
            var m1 = m.InvertedMatrix();
            //radiusをそのまま使うと結果が小さくなり誤差判断でバグるため1で計算し、最後にradiusをかける。
            var rx = 1;// radius;
            var ry = flatness;// * radius;
            var angle = -DegToRad(angleDeg);
            var c = Cos(angle);
            var s = Sin(angle);
            var f11 = c * c / (rx * rx) + s * s / (ry * ry);
            var f22 = s * s / (rx * rx) + c * c / (ry * ry);
            var f12 = c * s * (-1 / (rx * rx) + 1 / (ry * ry));
            var m2 = m1.TransposedMatrix() * (new Matrix2D(f11, f12, f12, f22)) * m1;
            var lams = QuadEq(1, -(m2.A11 + m2.A22), m2.A11 * m2.A22 - m2.A12 * m2.A12);
            if (lams.Length == 0)
            {
                throw new Exception("TransformOval: No eigenvalues, so not transform.");
            }

            double r, f;
            if (lams.Length == 1)
            {
                r = Sqrt(1.0 / m2.A11);
                f = Sqrt(1.0 / m2.A22) / r;
            }
            else
            {
                r = Sqrt(1.0 / lams[0]);
                f = Sqrt(1.0 / lams[1]) / r;
            }
            var vy = m2.A11 - 1 / (r * r);// lams[0];
            var vx = -m2.A12;
            var a = FloatEQ(vy, 0.0) ? 0.0 : FloatEQ(vx, 0.0) ? 90.0 : RadToDeg360(Atan2(vy, vx));
            return new(r * radius, f, a);
        }
        static double[] QuadEq(double a, double b, double c)
        {
            if (FloatEQ(a, 0.0))
            {
                if (FloatEQ(b, 0.0))
                {
                    return Array.Empty<double>();// new double[] { };
                }
                return new double[] { c / b };
            }
            var bb = b / a;
            var cc = c / a;
            double disc = bb * bb - 4 * cc;
            if (FloatEQ(disc, 0))
            {
                return new double[] { -0.5 * bb };
            }
            if (disc < 0)
                return Array.Empty<double>();// new double[] { };
            disc = Math.Sqrt(disc);
            double q = ((bb < 0) ? -0.5 * (bb - disc) : -0.5 * (bb + disc));
            double t0 = q;
            double t1 = cc / q;
            return (t0 > t1) ? new double[] { t1, t0 } : new double[] { t0, t1 };
        }

    }
}
