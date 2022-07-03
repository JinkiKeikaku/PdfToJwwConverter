using PdfUtility;
using System.Reflection;
using System.Text;
using static PdfToJww.CadMath2D.CadMath;
using PdfToJww.CadMath2D;
using PdfToJww.Shape;
using System.IO.Compression;
using JwwHelper;
using System.Diagnostics;

namespace PdfToJww
{
    public class PdfConverter
    {
        public class ConvertOptions
        {
            public bool CombineDashedLine;
            public bool CreateArc;
            public bool CombineText;
            public bool UnifyKanji;

            public ConvertOptions(bool combineDashedLine, bool createArc, bool combineText, bool unifyKanji)
            {
                CombineDashedLine = combineDashedLine;
                CreateArc = createArc;
                CombineText = combineText;
                UnifyKanji = unifyKanji;
            }
        }

        public double CombineRate = 1.0;
        public double SpaceRate = 0.6;

        public PdfConverter()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (assemblyDir != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
                {
                    if (e.Name.StartsWith("JwwHelper,", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = Path.Combine(assemblyDir,
                        string.Format("JwwHelper_{0}.dll", (IntPtr.Size == 4) ? "x86" : "x64"));
                        return Assembly.LoadFile(fileName);
                    }
                    return null;
                };
            }
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Jwwファイルの縮尺表現。縮尺は(1/JwwScaleNumber)です。
        /// </summary>
        public double JwwScaleNumber { get; set; } = 1;


        /// <summary>
        /// PDFの総ページ数を取得します。
        /// </summary>
        /// <param name="pdfPath"></param>
        /// <returns>総ページ数</returns>
        /// <exception cref="Exception">
        /// 暗号化されている場合、またページ数が取得できない場合は例外が発生します。
        /// </exception>
        public int GetPageSize(string pdfPath)
        {
            using var pdfDoc = new PdfUtility.PdfDocument();
            pdfDoc.Open(new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (pdfDoc.IsEncrypt()) throw new Exception("This PDF file is encrypted. So cannot open.");
            var pageSize = pdfDoc.GetPageSize();
            if (pageSize < 1) throw new Exception("There is no pages.");
            return pageSize;
        }

        /// <summary>
        /// PDFをJWWに変換します。
        /// </summary>
        /// <param name="pdfPath">PDFファイルのパス</param>
        /// <param name="pageNumber">ページ番号。１以上、総ページ数以下</param>
        /// <param name="jwwPath">Jwwファイルのパス</param>
        /// <exception cref="Exception">
        /// 暗号化されている場合、変換失敗時に例外が発生します。
        /// </exception>
        public void Convert(
            string pdfPath, int pageNumber, string jwwPath, int paperCode, ConvertOptions options)
        {
            using var pdfDoc = new PdfUtility.PdfDocument();
            pdfDoc.Open(new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (pdfDoc.IsEncrypt()) throw new Exception("This PDF file is encrypted. So cannot open.");

            var shapes = new List<PShape>();
            var page = ReadPage(pdfDoc, pageNumber, shapes, options);
            var writer = new JwwHelper.JwwWriter();
            var tmp = Path.GetTempFileName();
            var buf = Properties.Resources.template;
            File.WriteAllBytes(tmp, buf);
            writer.InitHeader(tmp);
            File.Delete(tmp);

            writer.Header.m_nZumen = paperCode;
            var jwwPaperSize = JwwPaper.GetJwwPaperSize(writer.Header.m_nZumen);
            writer.Header.m_adScale[0] = JwwScaleNumber;

            FitToPaper(page, jwwPaperSize.Width, jwwPaperSize.Height, shapes);
            foreach (var shape in shapes)
            {
                AddConvertedJwwData(writer, shape);
                //                if (s != null) writer.AddData(s);
            }
            writer.Write(jwwPath);
        }

        PdfPage ReadPage(PdfDocument doc, int pageNumber, List<PShape> shapes, ConvertOptions options)
        {
            //PDFは左下が原点
            var page = doc.GetPage(pageNumber);
            var graphicStack = new Stack<ContentsReader.GraphicState>();
            graphicStack.Push(new ContentsReader.GraphicState());
            var resource = page.ResourcesDictionary;
            var contentsReader = new ContentsReader(doc, resource, graphicStack, options.UnifyKanji);
            foreach (var c in page.ContentsList)
            {
                if (c != null)
                {
                    var ss = contentsReader.Read(c);
                    if(options.CreateArc)   ArcHelper.ConvertArc(ss);
                    if (options.CombineText) CombineText(ss);
                    if (options.CombineDashedLine) CombineLine(ss);

                    foreach (var shape in ss)
                    {
                        if (shape != null) shapes.Add(shape);
                    }
                }
            }
            return page;
        }

        void FitToPaper(PdfPage page, double paperWidth, double paperHeight, List<PShape> shapes)
        {
            var dpr = new CadPoint();
            var rotate = -page.Attribute.Rotate / 180.0 * Math.PI;
            if (rotate != 0.0)
            {
                var p = new CadPoint(page.Attribute.MediaBox.Width, page.Attribute.MediaBox.Height);
                p.Rotate(rotate);
                if (p.X < 0.0) dpr.X = -p.X;
                if (p.Y < 0.0) dpr.Y = -p.Y;
            }
            var scale = 25.4 / 72.0;
            var p0 = new CadPoint(0, 0);
            var dp = new CadPoint(paperWidth / 2.0, paperHeight / 2.0);
            foreach (var s in shapes)
            {
                if (rotate != 0.0)
                {
                    s.Rotate(p0, rotate);
                    s.Offset(dpr);
                }
                s.Magnify(p0, scale, scale);
                s.Offset(-dp);
            }
        }

        void AddConvertedJwwData(JwwWriter writer, PShape shape)
        {
            switch (shape)
            {
                case PLineShape line:
                    {
                        var jw = new JwwHelper.JwwSen();
                        jw.m_start_x = line.P0.X;
                        jw.m_start_y = line.P0.Y;
                        jw.m_end_x = line.P1.X;
                        jw.m_end_y = line.P1.Y;
                        jw.m_nLayer = 0;
                        jw.m_nGLayer = 0;
                        jw.m_nPenStyle = (byte)LineHelper.GetNearLineType(line.StrokePatttern);
                        Debug.WriteLine(jw.m_nPenStyle.ToString());

                        writer.AddData(jw);
                    }
                    break;

                case PArcShape arc:
                    {
                        var jw = new JwwHelper.JwwEnko();
                        jw.m_start_x = arc.P0.X;
                        jw.m_start_y = arc.P0.Y;
                        jw.m_bZenEnFlg = CadMath.FloatEQ(arc.SweepAngle, 360) ? 1 : 0;
                        jw.m_dHankei = arc.Radius;
                        jw.m_dHenpeiRitsu = arc.Flatness;
                        jw.m_radKatamukiKaku = DegToRad(arc.Angle);
                        jw.m_radKaishiKaku = DegToRad(arc.StartAngle);
                        jw.m_radEnkoKaku = DegToRad(arc.SweepAngle);
                        jw.m_nLayer = 0;
                        jw.m_nGLayer = 0;
                        jw.m_nPenStyle = (byte)LineHelper.GetNearLineType(arc.StrokePatttern);
                        writer.AddData(jw);
                    }
                    break;

                case PBezierShape bezier:
                    {
                        var pt = Curve.CreateBezier3(bezier.P0, bezier.P1, bezier.P2, bezier.P3, 16, true);
                        for (var i = 0; i < pt.Count - 1; i++)
                        {
                            var p0 = pt[i];
                            var p1 = pt[i + 1];
                            var jw = new JwwHelper.JwwSen();
                            jw.m_start_x = p0.X;
                            jw.m_start_y = p0.Y;
                            jw.m_end_x = p1.X;
                            jw.m_end_y = p1.Y;
                            jw.m_nLayer = 0;
                            jw.m_nGLayer = 0;
                            jw.m_nPenStyle = (byte)LineHelper.GetNearLineType(bezier.StrokePatttern);
                            writer.AddData(jw);
                        }
                    }
                    break;
                case PTextShape text:
                    {
                        var jw = new JwwHelper.JwwMoji();
                        jw.m_string = text.Text;
                        jw.m_strFontName = text.FontName;
                        jw.m_dSizeY = text.Height;//フォント高さ
                        jw.m_dSizeX = text.Height;//フォント幅
                        jw.m_degKakudo = text.AngleDeg;
                        jw.m_start_x = text.P0.X;
                        jw.m_start_y = text.P0.Y;
                        var p2 = text.P0 + CadPoint.Pole(text.Width, DegToRad(text.AngleDeg));
                        jw.m_end_x = p2.X;
                        jw.m_end_y = p2.Y;
                        jw.m_nLayer = 0;
                        jw.m_nGLayer = 0;
                        writer.AddData(jw);
                    }
                    break;
                case PImageShape image:
                    {
                        var (name, gzName, buffer) = CreateJwwImageInfo(image);
                        if (buffer != null)
                        {
                            var img = JwwImage.Create(gzName, buffer);
                            writer.AddImage(img);
                            var moji = new JwwMoji();
                            //JwwMojiのほうはそのままの名前
                            moji.m_string = name;
                            moji.m_degKakudo = image.AngleDeg;
                            moji.m_dSizeX = 2;
                            moji.m_dSizeY = 2;
                            var w = PTextShape.GetTextWidth(name, moji.m_dSizeY);
                            var pe = new CadPoint(w, 0);
                            pe.Rotate(DegToRad(image.AngleDeg));
                            moji.m_start_x = image.P0.X;
                            moji.m_start_y = image.P0.Y;
                            moji.m_end_x = moji.m_start_x + pe.X;
                            moji.m_end_y = moji.m_start_y + pe.Y;
                            moji.m_strFontName = "ＭＳ ゴシック";//決め打ち
                            moji.m_nLayer = 0;
                            moji.m_nGLayer = 0;
                            writer.AddData(moji);
                        }
                    }
                    break;
            }
        }

        static int mImageNameIndex = 0;
        const string mImageNamePrefix = "image_";
        //"^@BM%temp%V__Picture_Towada20160803_IMG_0193_JPG.bmp,100,75,0,0,1,0"
        //100,75,0,0,1,0 => width,height, ?, ?, ?, amgle
        (string name, string gzName, byte[] buffer) CreateJwwImageInfo(PImageShape imageShape)
        {
            var bmName = $"C__Picture__{mImageNamePrefix + mImageNameIndex}.bmp";

            var name = $"^@BM%temp%{bmName},{imageShape.Width},{imageShape.Height},0,0,1,{imageShape.AngleDeg}";
            var gzName = bmName + ".gz";
            mImageNameIndex++;
            using var ws = new MemoryStream();
#pragma warning disable CA1416 // プラットフォームの互換性を検証
            imageShape.Image.Save(ws, System.Drawing.Imaging.ImageFormat.Bmp);
#pragma warning restore CA1416 // プラットフォームの互換性を検証
            var buffer = ws.GetBuffer();
            using var dst = new MemoryStream();
            using var gz = new GZipStream(dst, CompressionLevel.Optimal);
            gz.Write(buffer, 0, buffer.Length);
            gz.Close();
            return (name, gzName, dst.GetBuffer());
        }

        void CombineLine(List<PShape?> shapes)
        {
            LineHelper.CombineLine(shapes);

        }

        void CombineText(List<PShape?> shapes)
        {
            var j = -1;
            PTextShape s2 = null!;
            for (var i = shapes.Count - 1; i >= 0; i--)
            {
                var s1 = shapes[i] as PTextShape;
                if (s1 == null) continue;
                if (j >= 0)
                {
                    if (FloatEQ(s1.AngleDeg, s2.AngleDeg) &&
                        FloatEQ(s1.Height, s2.Height) &&
                        s1.Color == s2.Color)
                    {
                        var h = s1.Height;
                        var w = s1.Width;
                        var a = DegToRad(s1.AngleDeg);
                        var p0 = s1.P0.Copy();
                        var p1 = (p0 + new CadPoint(w, 0)).RotatePoint(p0, -a);
                        var p2 = s2.P0.RotatePoint(p0, -a);
                        var dp = p2 - p1;
                        var dr = dp.Hypot();
                        if (dr < CombineRate * h)
                        //                            if (FloatEQ(dp.Y, 0.0) && dr < CombineRate * h)
                        {
                            var t = s1.Text;
                            if (dr > SpaceRate * h) t += " ";
                            s1.Text = t + s2.Text;
                            shapes[i] = s1;
                            shapes[j] = null;
                        }
                    }
                }
                j = i;
                s2 = s1;
            }
        }
    }
}