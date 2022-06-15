using PdfToJww.CadMath2D;
using PdfUtility;
using System.Diagnostics;
using System.Text;

namespace PdfToJww
{
    static class Helper
    {


        public static double PdfToCad(double x) => x * 25.4 / 72;

        public static CadPoint GetPoint(PdfObject x, PdfObject y)
        {
            return new CadPoint(GetDouble(x), GetDouble(y));
        }

        public static double GetCadDouble(PdfObject obj) => PdfToCad(GetDouble(obj));

        public static double GetDouble(PdfObject obj)
        {
            if (obj is not PdfNumber num)
            {
                Debug.WriteLine($"PDF reader:obj is not number.{obj}");
                return 0.0;
            }
            return num.DoubleValue;
        }


        public static void OffsetJwwShape(JwwHelper.JwwData data, CadPoint dp)
        {
            switch (data)
            {
                case JwwHelper.JwwSen s:
                    {
                        s.m_start_x += dp.X;
                        s.m_start_y += dp.Y;
                        s.m_end_x += dp.X;
                        s.m_end_y += dp.Y;
                    }
                    break;
                case JwwHelper.JwwMoji s:
                    {
                        s.m_start_x += dp.X;
                        s.m_start_y += dp.Y;
                        s.m_end_x += dp.X;
                        s.m_end_y += dp.Y;
                    }
                    break;
            }
        }

        public static void ScaleJwwShape(JwwHelper.JwwData data, CadPoint p0, double scale)
        {
            switch (data)
            {
                case JwwHelper.JwwSen s:
                    {
                        var p1 = new CadPoint(s.m_start_x, s.m_start_y);
                        var p2 = new CadPoint(s.m_end_x, s.m_end_y);
                        p1.Magnify(p0, scale, scale);
                        p2.Magnify(p0, scale, scale);
                        s.m_start_x = p1.X;
                        s.m_start_y = p1.Y;
                        s.m_end_x = p2.X;
                        s.m_end_y = p2.Y;
                    }
                    break;
                case JwwHelper.JwwMoji s:
                    {
                        var p1 = new CadPoint(s.m_start_x, s.m_start_y);
                        var p2 = new CadPoint(s.m_end_x, s.m_end_y);
                        p1.Magnify(p0, scale, scale);
                        p2.Magnify(p0, scale, scale);
                        s.m_start_x = p1.X;
                        s.m_start_y = p1.Y;
                        s.m_end_x = p2.X;
                        s.m_end_y = p2.Y;
                        s.m_dSizeX *= scale;
                        s.m_dSizeY *= scale;
                    }
                    break;
            }
        }

        public static void RotateJwwShape(JwwHelper.JwwData data, CadPoint p0, double rad)
        {
            switch (data)
            {
                case JwwHelper.JwwSen s:
                    {
                        var p1 = new CadPoint(s.m_start_x, s.m_start_y);
                        var p2 = new CadPoint(s.m_end_x, s.m_end_y);
                        p1.Rotate(p0, rad);
                        p2.Rotate(p0, rad);
                        s.m_start_x = p1.X;
                        s.m_start_y = p1.Y;
                        s.m_end_x = p2.X;
                        s.m_end_y = p2.Y;
                    }
                    break;
                case JwwHelper.JwwMoji s:
                    {
                        var p1 = new CadPoint(s.m_start_x, s.m_start_y);
                        var p2 = new CadPoint(s.m_end_x, s.m_end_y);
                        p1.Rotate(p0, rad);
                        p2.Rotate(p0, rad);
                        s.m_start_x = p1.X;
                        s.m_start_y = p1.Y;
                        s.m_end_x = p2.X;
                        s.m_end_y = p2.Y;
                    }
                    break;
            }
        }


        static public CadSize GetJwwPaperSize(int code)
        {
            if (code < 15)
            {
                return mJwwPaperSizeArray[code];
            }
            return mJwwPaperSizeArray[3];    //わからなかったらひとまずA3
        }

        static readonly CadSize[] mJwwPaperSizeArray = new CadSize[]{
            new CadSize(1189.0, 841.0), //A0
            new CadSize(841.0, 594.0),  //A1
            new CadSize(594.0, 420.0),  //A2
            new CadSize(420.0, 297.0),  //A3
            new CadSize(297.0, 210.0),  //A4
            new CadSize(210.0, 148.0),  //A5???使わない
            new CadSize(210.0, 148.0),  //A6???使わない
            new CadSize(148.0, 105.0),  //A7???使わない
            new CadSize(1682.0, 1189.0),  //8:2A
            new CadSize(2378.0, 1682.0),  //9:3A
            new CadSize(3364.0, 2378.0),  //10:4A
            new CadSize(4756.0, 3364.0),  //11:5A
            new CadSize(10000.0, 7071.0),  //12:10m
            new CadSize(50000.0, 35355.0),  //13:50m
            new CadSize(100000.0, 70711.0)  //14:100m
        };


    }
}
