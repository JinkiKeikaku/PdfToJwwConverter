using System;

namespace PdfToJww.CadMath2D
{
    /// <summary>
    /// Cadで使う数学関数
    /// </summary>
    public class CadMath
    {
        /// <summary>
        /// 数値比較の許容差
        /// </summary>
        public static double COMPARE_EPSILON = 0.000001;

        /// <summary>
        /// 許容差を含めた比較。許容差指定可。
        /// </summary>
        /// <param name="x">数1</param>
        /// <param name="y">数2</param>
        /// <param name="epsilon">許容差</param>
        /// <returns>等しければtrue</returns>
        public static bool FloatEQ(double x, double y, double epsilon)
        {
            return Math.Abs(x - y) < epsilon;
        }

        /// <summary>
        /// 許容差を含めた比較。許容差はCOMPARE_EPSILONを使用。
        /// x == y
        /// </summary>
        public static bool FloatEQ(double x, double y)
        {
            return Math.Abs(x - y) < COMPARE_EPSILON;
        }
        /// <summary>
        /// 許容差を含めた比較。許容差はCOMPARE_EPSILONを使用。
        /// x ＜＝ y
        /// </summary>
        public static bool FloatLE(double x, double y)
        {
            return x < y + COMPARE_EPSILON;
        }
        /// <summary>
        /// 許容差を含めた比較。許容差はCOMPARE_EPSILONを使用。
        /// x ＜ y
        /// </summary>
        public static bool FloatLT(double x, double y)
        {
            return x < y - COMPARE_EPSILON;
        }
        /// <summary>
        /// 許容差を含めた比較。許容差はCOMPARE_EPSILONを使用。
        /// x >= y
        /// </summary>
        public static bool FloatGE(double x, double y)
        {
            return x > y - COMPARE_EPSILON;
        }
        /// <summary>
        /// 許容差を含めた比較。許容差はCOMPARE_EPSILONを使用。
        /// x > y
        /// </summary>
        public static bool FloatGT(double x, double y)
        {
            return x > y + COMPARE_EPSILON;
        }

        /// <summary>
        /// 距離関数。
        /// sqrt(x^2+y^2)
        /// </summary>
        public static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);

        /// <summary>
        /// Degreeを[0, 360)の範囲に正規化
        /// </summary>
        public static double NormalizeAngle360(double deg)
        {
            var d = deg % 360.0;
            if (d < 0.0) d += 360.0;
            if (FloatEQ(d, 360.0)) d = 0.0;
            return d;
        }

        /// <summary>
        /// 角度変換。
        /// Degree->Radian
        /// </summary>
        public static double DegToRad(double deg)
        {
            return Math.PI * deg / 180.0;
        }
        /// <summary>
        /// 角度変換。
        /// Radian->Degree。正規化は行わない。
        /// </summary>
        public static double RadToDeg(double rad)
        {
            return rad * (180.0 / Math.PI);
        }

        /// <summary>
        /// 角度変換。
        /// RadianからDegreeに変換したのち、[0, 360)に正規化する。
        /// </summary>
        public static double RadToDeg360(double rad)
        {
            return NormalizeAngle360(rad * (180.0 / Math.PI));
        }
        /// <summary>
        /// 角度[angleDeg]が[minAngleDeg]と[maxAngleDeg]の間にあればtrue。 
        /// <para>例（単位は度で例示）</para>
        /// <para>minAngleDegが50でmaxAngleDegが100でangleDegが70ならtrue。</para>
        /// <para>minAngleDegが50でmaxAngleDegが100でangleDegが30ならfalse。</para>
        /// <para>minAngleDegが350でmaxAngleDegが60でangleDegが10ならtrue。</para>
        /// </summary>
        public static bool IsAngleContained(double minAngleDeg, double maxAngleDeg, double angleDeg)
        {
            var width = SubtractAngleDeg(maxAngleDeg, minAngleDeg);
            var d = SubtractAngleDeg(angleDeg, minAngleDeg);
            return FloatGE(width, d);
        }

        /// <summary>
        /// 角度[angleDeg]が[startDeg]と幅[sweepDeg]の間にあればtrue。 
        /// 幅はマイナス（左回り）も許容する。
        /// </summary>
        public static bool IsAngleContainedInArc(double start, double sweepDeg, double angle)
        {
            NormalizeArcAngle(ref start, ref sweepDeg);
            return IsAngleContained(start, AddAngleDeg(start, sweepDeg), angle);
        }

        /// <summary>
        /// 円弧の弧幅角[sweepDeg]がマイナスの場合、弧幅角を正にし、開始角[startAngle]を修正しtrueを返します。
        /// 角度は[0, 360)度に正規化されます。
        /// </summary>
        public static bool NormalizeArcAngle(ref double startDeg, ref double sweepDeg)
        {
            if (FloatLT(sweepDeg, 0.0))
            {
                startDeg = NormalizeAngle360(startDeg + sweepDeg);
                sweepDeg = NormalizeAngle360(-sweepDeg);
                return true;
            }
            //一応正規化
            sweepDeg = NormalizeAngle360(sweepDeg);
            return false;
        }

        /// <summary>
        /// <para>単位が度の角度の減算。aDeg-bDegを行い正規化する。</para>
        /// <para>60-30＝30, 30-60=330</para>
        /// </summary>
        public static double SubtractAngleDeg(double aDeg, double bDeg)
        {
            return NormalizeAngle360(aDeg - bDeg);
        }
        /// <summary>
        /// <para>単位が度の角度の加算。aDeg+bDegを行い正規化する。</para>
        /// <para>60+30＝90, 330+60=30</para>
        /// </summary>
        public static double AddAngleDeg(double aDeg, double bDeg)
        {
            return NormalizeAngle360(aDeg + bDeg);
        }

    }
}
