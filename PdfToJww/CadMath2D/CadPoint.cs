using System;
using static System.Math;
using static PdfToJww.CadMath2D.CadMath;
namespace PdfToJww.CadMath2D
{
    /// <summary>
    /// CADで使う点
    /// </summary>
    public class CadPoint : IComparable
    {
        /// <summary>
        /// 座標X
        /// </summary>
        public double X;
        /// <summary>
        /// 座標Y
        /// </summary>
        public double Y;
        /// <summary>
        /// コンストラクター
        /// </summary>
        public CadPoint() { }
        /// <summary>
        /// コンストラクター
        /// </summary>
        public CadPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
        /// <inheritdoc/>>
        public override string ToString()
        {
            return $"({X} {Y})";
        }
        /// <summary>
        /// x,y座標でオブジェクトを設定
        /// </summary>
        public void Set(double x, double y)
        {
            X = x;
            Y = y;
        }
        /// <summary>
        /// 座標でオブジェクトを設定
        /// </summary>
        public void Set(CadPoint p)
        {
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// この点の複製を返す
        /// </summary>
        public CadPoint Copy()
        {
            return (CadPoint)MemberwiseClone();
        }

        /// <summary>
        /// 許容差を含め座標が(0, 0)か？
        /// </summary>
        public bool IsZero()
        {
            return FloatEQ(X, 0.0) && FloatEQ(Y, 0.0);
        }

        /// <summary>
        /// 座標がFiniteでtrue（無限大などでなければTrue）。
        /// </summary>
        public bool IsFinite()
        {
            return double.IsFinite(X) && double.IsFinite(Y);
        }

        /// <summary>
        /// 座標をオフセットする（this = this + dp）
        /// </summary>
        public void Offset(CadPoint dp)
        {
            X += dp.X;
            Y += dp.Y;
        }

        /// <summary>
        /// 座標をオフセットする（this = this + (dx, dy)）
        /// </summary>
        public void Offset(double dx, double dy)
        {
            X += dx;
            Y += dy;
        }

        /// <summary>
        /// 点の座標を拡大（X=X * m, Y = Y * m）
        /// </summary>
        public void Magnify(double m)
        {
            X *= m;
            Y *= m;
        }
        /// <summary>
        /// 点の座標を拡大（X=X * mx, Y = Y * my）
        /// </summary>
        public void Magnify(double mx, double my)
        {
            X *= mx;
            Y *= my;
        }
        /// <summary>
        /// 点の座標をp0を基準に拡大
        /// </summary>
        public void Magnify(CadPoint p0, double mx, double my)
        {
            X = (X - p0.X) * mx + p0.X;
            Y = (Y - p0.Y) * my + p0.Y;
        }
        /// <summary>
        /// 座標を(0, 0)基準で回転。角度はradian。
        /// </summary>
        public void Rotate(double rad)
        {
            if (FloatEQ(rad, 0.0)) return;
            var c = Cos(rad);
            var s = Sin(rad);
            var xx = X * c - Y * s;
            var yy = X * s + Y * c;
            X = xx;
            Y = yy;
        }

        /// <summary>
        /// 座標を(0, 0)基準で回転した点を返す。角度はradian。thisは変更されない。
        /// </summary>
        public CadPoint RotatePoint(double rad)
        {
            if (FloatEQ(rad, 0.0)) return Copy();
            var c = Cos(rad);
            var s = Sin(rad);
            var xx = X * c - Y * s;
            var yy = X * s + Y * c;
            return new CadPoint(xx, yy);
        }

        /// <summary>
        /// 座標をp0基準で回転。角度はradian。
        /// </summary>
        public void Rotate(CadPoint p0, double rad)
        {
            if (FloatEQ(rad, 0.0)) return;
            var c = Cos(rad);
            var s = Sin(rad);
            var xx = X - p0.X;
            var yy = Y - p0.Y;
            X = xx * c - yy * s + p0.X;
            Y = xx * s + yy * c + p0.Y;
        }

        /// <summary>
        /// 座標をp0基準で回転した点を返す。角度はradian。thisは変更されない。
        /// </summary>
        public CadPoint RotatePoint(CadPoint p0, double rad)
        {
            if (FloatEQ(rad, 0.0)) return Copy();
            var c = Cos(rad);
            var s = Sin(rad);
            var xx = X - p0.X;
            var yy = Y - p0.Y;
            return new CadPoint(xx * c - yy * s + p0.X, xx * s + yy * c + p0.Y);
        }

        /// <summary>
        /// 座標を(0, 0)基準で左に９０度回転。
        /// </summary>
        public void Rotate90()
        {
            var xx = X;
            X = -Y;
            Y = xx;
        }
        /// <summary>
        /// 座標を(0, 0)基準で右に９０度回転。
        /// </summary>
        public void RotateM90()
        {
            var xx = X;
            X = Y;
            Y = -xx;
        }

        /// <summary>
        /// 座標を(0, 0)基準で180度回転。
        /// </summary>
        public void Negative()
        {
            X = -X;
            Y = -Y;
        }
        /// <summary>
        /// 原点からの距離を返す。
        /// </summary>
        public double Hypot()
        {
            return Sqrt(X * X + Y * Y);
        }

        /// <summary>
        /// 原点基準で座標の角度を返す。
        /// </summary>
        public double GetAngle()
        {
            return Atan2(Y, X);
        }

        /// <summary>
        /// 原点基準で座標の角度を0-360のDegで返す。
        /// </summary>
        public double GetAngle360()
        {
            return RadToDeg360(GetAngle());
        }

        /// <summary>
        /// 単位ベクトルに変換する。ゼロベクトルの時はゼロベクトルのまま。
        /// </summary>
        public void Unit()
        {
            var r = Hypot();
            if (FloatEQ(r, 0.0)) return;
            X /= r;
            Y /= r;
        }
        /// <summary>
        /// 個の座標の単位ベクトルを返す。thisは変更されない。ゼロベクトルの時はゼロベクトル。
        /// </summary>
        public CadPoint UnitPoint()
        {
            var r = Hypot();
            if (FloatEQ(r, 0.0)) return new CadPoint();
            return new CadPoint(X / r, Y / r);
        }
        /// <summary>
        /// ベクトルの長さで比較する
        /// </summary>
        public int CompareTo(object? obj)
        {
            if (obj == null) throw new ArgumentException("compare param is null");
            if (obj is CadPoint p)
            {
                var a = Hypot();
                var b = p.Hypot();
                if (FloatEQ(a, b)) return 0;
                return a < b ? -1 : 1;
            }
            throw new ArgumentException("compare param is not CadPoint");
        }

        /// <summary>
        /// 角度radで長さ1の座標
        /// </summary>
        public static CadPoint Pole(double rad)
        {
            return new CadPoint(Cos(rad), Sin(rad));
        }
        /// <summary>
        /// 長さradiusで角度radの座標
        /// </summary>
        public static CadPoint Pole(double radius, double rad)
        {
            return new CadPoint(Cos(rad) * radius, Sin(rad) * radius);
        }

        /// <summary>
        /// 位置p0から半径radius、扁平率flatnes、角度radの座標（傾いてない楕円上の座標）
        /// </summary>
        public static CadPoint Pole(CadPoint p0, double radius, double flatness, double angle)
        {
            return new CadPoint(p0.X + radius * Cos(angle), p0.Y + radius * Sin(angle) * flatness);
        }

        /// <summary>
        /// [p1]と[p2]の内積
        /// </summary>
        public static double Dot(CadPoint p1, CadPoint p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y;
        }

        /// <summary>
        /// [p1]と[p2]の外積(p1.x * p2.y - p1.y * p2.x)
        /// </summary>
        public static double Cross(CadPoint p1, CadPoint p2)
        {
            return p1.X * p2.Y - p1.Y * p2.X;
        }

        /// <summary>
        /// 座標が許容誤差を含めて等しいか
        /// </summary>
        public static bool PointEQ(CadPoint p1, CadPoint p2)
        {
            return FloatEQ(p1.X, p2.X) && FloatEQ(p1.Y, p2.Y);
        }

        /// <summary>
        /// 座標が許容誤差を含めて等しいか。２点間の距離を誤差として指定。
        /// </summary>
        public static bool PointEQ(CadPoint p1, CadPoint p2, double epsilon)
        {
            return (p1 - p2).Hypot() <= epsilon;
        }

        /// <summary>
        /// 単項＋
        /// </summary>
        public static CadPoint operator +(CadPoint p) => p;
        /// <summary>
        /// 単項ー
        /// </summary>
        public static CadPoint operator -(CadPoint p) => new(-p.X, -p.Y);
        /// <summary>
        /// 加算
        /// </summary>
        public static CadPoint operator +(CadPoint p1, CadPoint p2) => new(p1.X + p2.X, p1.Y + p2.Y);
        /// <summary>
        /// 減算
        /// </summary>
        public static CadPoint operator -(CadPoint p1, CadPoint p2) => new(p1.X - p2.X, p1.Y - p2.Y);
        /// <summary>
        /// 座標の積。p1.X*p2.Y, p1.Y*p2.Y
        /// </summary>
        public static CadPoint operator *(CadPoint p1, CadPoint p2) => new(p1.X * p2.X, p1.Y * p2.Y);
        /// <summary>
        /// 座標の割り算。p1.X/p2.Y, p1.Y/p2.Y
        /// </summary>
        public static CadPoint operator /(CadPoint p1, CadPoint p2) => new(p1.X / p2.X, p1.Y / p2.Y);
        /// <summary>
        /// 定数倍
        /// </summary>
        public static CadPoint operator *(CadPoint p, double a) => new(p.X * a, p.Y * a);
        /// <summary>
        /// 定数の割り算
        /// </summary>
        public static CadPoint operator /(CadPoint p, double a) => new(p.X / a, p.Y / a);
    }
}
