using PdfToJww.CadMath2D;
using PdfUtility;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PdfToJww
{
#pragma warning disable CA1416 // プラットフォームの互換性を検証
    static class Helper
    {
        /// <summary>
        /// PDFの単位系からｍｍへ変換
        /// </summary>
        public static double PdfToCad(double x) => x * 25.4 / 72;

        /// <summary>
        /// ２つのPdfObject（実際はPdfNumber）を受けて座標を返す。単位はmm。
        /// </summary>
        public static CadPoint GetPoint(PdfObject x, PdfObject y)
        {
            return new CadPoint(GetDouble(x), GetDouble(y));
        }

        /// <summary>
        /// 1つのPdfObject（実際はPdfNumber）をｍｍの長さへ変換。
        /// </summary>
        public static double GetCadDouble(PdfObject obj) => PdfToCad(GetDouble(obj));

        /// <summary>
        /// 1つのPdfObject（実際はPdfNumber）をそのままの数値に変換。
        /// </summary>
        public static double GetDouble(PdfObject obj)
        {
            if (obj is not PdfNumber num)
            {
                Debug.WriteLine($"PDF reader:obj is not number.{obj}");
                return 0.0;
            }
            return num.DoubleValue;
        }

        ///// <summary>
        ///// Jwwの図面コードを用紙サイズに変換
        ///// </summary>
        //static public CadSize GetJwwPaperSize(int code)
        //{
        //    if (code < 15)
        //    {
        //        return mJwwPaperSizeArray[code];
        //    }
        //    return mJwwPaperSizeArray[3];    //わからなかったらひとまずA3
        //}

        /// <summary>
        /// 画像のタイプ
        /// </summary>
        public enum ImageType
        {
            None,
            BGR,    //Windows
            RGB,    //PNG
            CMYK,    //CMYK
            Gray,   //8bit
        }

        /// <summary>
        /// PDFのストリームのバイト列(raw image)をBitmapに変換。
        /// </summary>
        public static Bitmap CtreateImageFromRaw(this byte[] src, int width, int height, ImageType imageType)
        {
            switch (imageType)
            {
                case ImageType.BGR:
                    return CtreateImageFromBGR(src, width, height);
                case ImageType.RGB:
                    return CtreateImageFromRGB(src, width, height);
                case ImageType.CMYK:
                    return CtreateImageFromCMYK(src, width, height);
                case ImageType.Gray:
                    return CtreateImageFromGray(src, width, height);
            }
            throw new Exception($"CtreateImageFromRaw Unknown image type {imageType}");

        }

        //private static readonly CadSize[] mJwwPaperSizeArray = new CadSize[]{
        //    new CadSize(1189.0, 841.0), //A0
        //    new CadSize(841.0, 594.0),  //A1
        //    new CadSize(594.0, 420.0),  //A2
        //    new CadSize(420.0, 297.0),  //A3
        //    new CadSize(297.0, 210.0),  //A4
        //    new CadSize(210.0, 148.0),  //A5???使わない
        //    new CadSize(210.0, 148.0),  //A6???使わない
        //    new CadSize(148.0, 105.0),  //A7???使わない
        //    new CadSize(1682.0, 1189.0),  //8:2A
        //    new CadSize(2378.0, 1682.0),  //9:3A
        //    new CadSize(3364.0, 2378.0),  //10:4A
        //    new CadSize(4756.0, 3364.0),  //11:5A
        //    new CadSize(10000.0, 7071.0),  //12:10m
        //    new CadSize(50000.0, 35355.0),  //13:50m
        //    new CadSize(100000.0, 70711.0)  //14:100m
        //};

        private static Bitmap CtreateImageFromRGB(byte[] src, int width, int height)
        {
            var output = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = output.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, output.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            var w3 = width * 3;
            for (var y = 0; y < height; y++)
            {
                var srcTop = y * w3;
                for (var x = 0; x < w3; x += 3)
                {
                    buf[x] = src[srcTop + x + 2];
                    buf[x + 1] = src[srcTop + x + 1];
                    buf[x + 2] = src[srcTop + x];
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            output.UnlockBits(bmpData);
            return output;
        }
        private static Bitmap CtreateImageFromBGR(byte[] src, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            var w3 = width * 3;
            for (var y = 0; y < height; y++)
            {
                var srcTop = y * w3;
                for (var x = 0; x < w3; x += 3)
                {
                    buf[x] = src[srcTop + x];
                    buf[x + 1] = src[srcTop + x + 1];
                    buf[x + 2] = src[srcTop + x + 2];
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private static Bitmap CtreateImageFromCMYK(byte[] src, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            for (var y = 0; y < height; y++)
            {
                var x3 = 0;
                var x4 = 0;
                var srcTop = y * width * 4;
                for (var x = 0; x < width; x++)
                {
                    (buf[x3 + 2], buf[x3 + 1], buf[x3]) = CmykToRgb(src[srcTop + x4], src[srcTop + x4 + 1], src[srcTop + x4 + 2], src[srcTop + x4 + 3]);
                    x3 += 3;
                    x4 += 4;
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private static Bitmap CtreateImageFromGray(byte[] src, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            for (var y = 0; y < height; y++)
            {
                var srcTop = y * width;
                for (var x = 0; x < width; x++)
                {
                    var c = src[srcTop + x];
                    var x3 = x * 3;
                    buf[x3] = c;
                    buf[x3 + 1] = c;
                    buf[x3 + 2] = c;
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        private static (byte r, byte g, byte b) CmykToRgb(byte c, byte m, byte y, byte k)
        {
            var r = (byte)((255 - c) * (255 - k) / 255);
            var g = (byte)((255 - m) * (255 - k) / 255);
            var b = (byte)((255 - y) * (255 - k) / 255);
            return (r, g, b);
        }



    }
}
