using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
