using PdfUtility;
using System.Reflection;
using System.Text;
using static PdfToJww.CadMath2D.CadMath;
using PdfToJww.CadMath2D;

namespace PdfToJww
{
    public class PdfConverter
    {
        public double CombineRate = 0.8;
        public double SpaceRate = 0.7;



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
                        //string.Format("ExchangeJww\\JwwHelper_{0}.dll", (IntPtr.Size == 4) ? "x86" : "x64"));
                        return Assembly.LoadFile(fileName);
                    }
                    return null;
                };
            }
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

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
        public void Convert(string pdfPath, int pageNumber, string jwwPath, bool combineText)
        {
            using var pdfDoc = new PdfUtility.PdfDocument();
            pdfDoc.Open(new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (pdfDoc.IsEncrypt()) throw new Exception("This PDF file is encrypted. So cannot open.");
            var datas = new List<JwwHelper.JwwData>();
            var page = ReadPage(pdfDoc, pageNumber, datas, combineText);
            var jw = new JwwHelper.JwwWriter();
            var tmp = Path.GetTempFileName();
            var buf = Properties.Resources.template;
            File.WriteAllBytes(tmp, buf);
            jw.InitHeader(tmp);
            File.Delete(tmp);

            FitToPaper(page, jw, datas);
            foreach (var s in datas)
            {
                jw.AddData(s);
            }
            jw.Write(jwwPath);
        }
        PdfPage ReadPage(PdfDocument doc, int pageNumber, List<JwwHelper.JwwData> datas, bool combineText)
        {
            //PDFは左下が原点
            var page = doc.GetPage(pageNumber - 1);
            var graphicStack = new Stack<ContentsReader.GraphicState>();
            graphicStack.Push(new ContentsReader.GraphicState());
            var resource = page.ResourcesDictionary;
            var contentsReader = new ContentsReader(doc, resource, graphicStack);
            foreach (var c in page.ContentsList)
            {
                if (c != null)
                {
                    var shapes = contentsReader.Read(c);
                    if(combineText)   CombineText(shapes);
                    foreach (var shape in shapes)
                    {
                        if (shape != null)
                        {
                            datas.Add(shape);
                        }
                    }
                }
            }
            return page;
        }

        void FitToPaper(PdfPage page, JwwHelper.JwwWriter jw, List<JwwHelper.JwwData> datas)
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
            var jwwPaperSize = Helper.GetJwwPaperSize(jw.Header.m_nZumen);
            var dp = new CadPoint(jwwPaperSize.Width / 2.0, jwwPaperSize.Height / 2.0);
            foreach (var s in datas)
            {
                if (rotate != 0.0)
                {
                    Helper.RotateJwwShape(s, p0, rotate);
                    Helper.OffsetJwwShape(s, dpr);
                }
                Helper.ScaleJwwShape(s, p0, scale);
                Helper.OffsetJwwShape(s, -dp);
            }
        }



        void CombineText(List<JwwHelper.JwwData?> shapes)
        {
            var j = -1;
            JwwHelper.JwwMoji s2 = null!;
            for (var i = shapes.Count - 1; i >= 0; i--)
            {
                var s1 = shapes[i] as JwwHelper.JwwMoji;
                if (s1 != null && !s1.m_string.StartsWith("^@BM"))
                {
                    if (j < 0)
                    {
                        j = i;
                        s2 = s1;
                        continue;
                    }
                    if (FloatEQ(s1.m_degKakudo, s2.m_degKakudo) &&
                        FloatEQ(s1.m_dSizeY, s2.m_dSizeY) &&
                        s1.m_nPenColor == s2.m_nPenColor)
                    {
                        var h = s1.m_dSizeY;
                        var a = DegToRad(s1.m_degKakudo);
                        var p1 = new CadPoint(s1.m_end_x, s1.m_end_y);
                        var p2 = new CadPoint(s2.m_start_x, s2.m_start_y);
                        var dp = p2 - p1;
                        var dr = dp.Hypot();
                        if (dr < CombineRate * h)
                        {
                            var t = s1.m_string;
                            if (dr > SpaceRate * h) t += " ";
                            s1.m_string = t + s2.m_string;
                            var w = ContentsReader.GetTextWidth(s1.m_string, s1.m_dSizeY);
                            var p02 = p1 + CadPoint.Pole(w, DegToRad(s1.m_degKakudo));
                            s1.m_end_x = p02.X;
                            s1.m_end_y = p02.Y;
                            shapes[i] = s1;
                            shapes[j] = null;
                        }
                    }
                    j = i;
                    s2 = s1;
                }
            }
        }
    }
}