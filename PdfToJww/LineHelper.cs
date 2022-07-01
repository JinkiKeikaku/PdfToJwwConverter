using PdfToJww.CadMath2D;
using PdfToJww.Shape;

namespace PdfToJww
{
    static class LineHelper
    {
        static double mDashMax = 8.0;//線とギャップの長さ（mm）。
        static double mGapMax = 4.0;//線とギャップの長さ（mm）。
        static double mPdfUnitScale = 25.4 / 72.0;
        static double mEpsilon = 0.0025;//許容差。大きめにしないとうまくいかないことがあった。

        public static void CombineLine(List<PShape?> shapes)
        {
            PLineShape? shape0 = null!;
            CadPoint p0 = new();
            CadPoint p1 = new();
            var buffer = new LinkedList<(int, double)>();
            for (var i = shapes.Count - 1; i >= 0; i--)
            {
                var s2 = shapes[i] as PLineShape;
                if (s2 == null) continue;
                if (buffer.Count == 0)
                {
                    if (CombineLineLengthCheck(s2))
                    {
                        buffer.AddLast((i, (s2.P1 - s2.P0).Hypot()));
                        shape0 = s2;
                        p0 = s2.P0;
                        p1 = s2.P1;
                    }
                    continue;
                }
                var w = shape0.StrokeWidth;
                var gapMax = mGapMax / mPdfUnitScale;
                if (CombineLineStyleCheck(shape0, s2) && CombineLineLengthCheck(s2))
                {
                    //終わりとつながるか？
                    var dp10 = p1 - p0;
                    var dp200 = s2.P0 - p0;
                    var gap1 = dp200.Hypot() - dp10.Hypot();
                    var e1 = CadPoint.Dot(dp10.UnitPoint(), dp200.UnitPoint());
                    if (CadMath.FloatEQ(e1, 1.0, mEpsilon) && gap1 >= w && gap1 <= gapMax)
                    {
                        buffer.AddLast((-1, gap1));
                        buffer.AddLast((i, (s2.P1 - s2.P0).Hypot()));
                        p1 = s2.P1;
                        continue;
                    }
                    //先頭とつながるか？
                    var dp01 = p0 - p1;
                    var dp211 = s2.P1 - p1;
                    var gap2 = dp211.Hypot() - dp01.Hypot();
                    var e3 = CadPoint.Dot(dp01.UnitPoint(), dp211.UnitPoint());
                    if (CadMath.FloatEQ(e3, 1.0, mEpsilon) && gap2 >= w && gap2 <= gapMax)
                    {
                        buffer.AddFirst((-1, gap2));
                        buffer.AddFirst((i, (s2.P1 - s2.P0).Hypot()));
                        p0 = s2.P0;
                        continue;
                    }
                }
                CombineLineMake(buffer, shapes, shape0, p0, p1);
                i++;
            }
            CombineLineMake(buffer, shapes, shape0, p0, p1);
        }

        static bool CombineLineLengthCheck(PLineShape s1)
        {
            var dp1 = s1.P1 - s1.P0;
            var len1 = dp1.Hypot();
            return CadMath.FloatGT(len1, 0.0) && len1 <= mDashMax / mPdfUnitScale;

        }
        static bool CombineLineStyleCheck(PLineShape s1, PLineShape s2)
        {
            if (!CadMath.FloatEQ(s1.StrokeWidth, s2.StrokeWidth, mEpsilon)) return false;
            if (s1.StrokeColor != s2.StrokeColor) return false;
            var dp1 = (s1.P1 - s1.P0).UnitPoint();
            var dp2 = (s2.P1 - s2.P0).UnitPoint();
            return CadMath.FloatEQ(CadPoint.Dot(dp1, dp2), 1.0, mEpsilon);
        }

        static void CombineLineMake(LinkedList<(int, double)> buffer, List<PShape?> shapes, PLineShape shape0, CadPoint p0, CadPoint p1)
        {
            if (buffer.Count > 3)
            {
                var la = new List<float>();
                foreach (var a in buffer)
                {
                    if (a.Item1 >= 0) shapes[a.Item1] = null;
                    if (la.Count < 8) la.Add((float)a.Item2);
                }
                var ca = new float[(la.Count + 1) / 2];
                for (int i = 0; i < ca.Length; i++)
                {
                    ca[i] = 0;
                    for (var j = 0; j < la.Count; j++)
                    {
                        ca[i] += la[j] * la[(j + i * 2) % la.Count];
                    }
                }
                var iMax = 0;
                var aMax = 0.0f;
                for (int i = 1; i < ca.Length; i++)
                {
                    if (aMax < ca[i])
                    {
                        iMax = i;
                        aMax = ca[i];
                    }
                }
                la.RemoveRange(iMax * 2, la.Count - iMax * 2);
                //                if ((la.Count & 1) == 1) la.RemoveAt(la.Count - 1);
                shape0.P0 = p0;
                shape0.P1 = p1;
                shape0.StrokePatttern = la.ToArray();
                shapes[buffer.First().Item1] = shape0;
            }
            buffer.Clear();
        }

        static float[][] mJwwLinePattern = new float[][] {
            new float[] {8,4 },
            new float[] {24,4,8,4 },
            new float[] {24,4,8,4,8,4 },
            };

        public static int GetNearLineType(IReadOnlyList<float> pat)
        {
            var PATSIZE = 12;
            var ret = 0;
            var size = pat.Count;
            if (size >= 2)
            {
                var pSrc = new float[PATSIZE];
                var pCmp = new float[PATSIZE];
                FillPat(pSrc, pat, PATSIZE);
                NormalizePattern(pSrc);
                var maxC = double.MinValue;
                for (var k = 0; k < mJwwLinePattern.Length; k++)
                {
                    var t = mJwwLinePattern[k];
                    FillPat(pCmp, t, PATSIZE);
                    NormalizePattern(pCmp);
                    var c = Correlation2(pSrc, pCmp, PATSIZE);
                    if (maxC < c)
                    {
                        maxC = c;
                        ret = k+1;
                    }
                }
            }
            //1=実線、3＝点線、6＝一点鎖線、8＝２点鎖線
            return ret switch
            {
                0=>1,
                1 => 3,
                2 => 6,
                3 => 8,
                _=>1,
            };
        }
        static void NormalizePattern(float[] pat)
        {
            var s = pat.Sum();
            for (var i = 0; i < pat.Length; i++)
            {
                pat[i] /= s;
            }
        }

        static void FillPat(float[] dst, IReadOnlyList<float> src, int size)
        {
            var n = src.Count;
            for (var i = 0; i < size; i++)
            {
                dst[i] = src[i % n];
            }
        }

        static double Correlation2(float[] p1, float[] p2, int size)
        {
            var cMax = float.MinValue;
            var m = size * 8;
            float[] p0 = new float[size];
            for (var j = 0; j < size; j += 2)
            {
                for (var i = 0; i < size; i++)
                {
                    p0[i] = p2[(i + j) % size];
                }
                var s = 0.0f;
                for (var i = 0; i < m; i++)
                {
                    var x = i * 1.0f / m;
                    s += GetWave(p0, x) * GetWave(p1, x);
                }
                if (cMax < s) cMax = s;
            }
            return cMax;

            static int GetWave(float[] p, float x)
            {
                var x0 = 0.0f;
                for (var i = 0; i < p.Length - 1; i++)
                {
                    x0 += p[i];
                    if (x < x0)
                    {
                        return 1 - 2 * (i & 1);
                    }
                }
                return 0;
            }
        }

    }
}
