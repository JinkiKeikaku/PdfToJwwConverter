using PdfToJww.CadMath2D;
using PdfUtility;
using System.Diagnostics;
using System.Text;
using static PdfToJww.Helper;

namespace PdfToJww
{
    class ContentsReader
    {
        public class GraphicState
        {
            public TransformMatrix Ctm = new();
            public float StrokeWidth = 0.0f;
            public System.Drawing.Color StrokeColor = System.Drawing.Color.Black;
            public System.Drawing.Color FillColor = System.Drawing.Color.Black;
            public string FontName = "";
            public float FontSize = 4.0f;
            public double Tl = 5.0;

            public GraphicState Copy()
            {
                return (GraphicState)MemberwiseClone();
            }
        }

        class TextParameter
        {
            public TransformMatrix Tm = new();
            public TransformMatrix Tlm = new();
        }

        PdfDocument mDoc;
        PdfDictionary mResource;
        List<PdfShape>? mPath = null;
        List<List<PdfShape>> mPathList = new();
        CadPoint mStartPoint = new();
        CadPoint mCurrentPoint = new();
        bool mIsTextState = false;
        List<PdfFont> mFontList;
        TextParameter mTextParameter = new();
        GraphicState mGS;
        Stack<GraphicState> mGrashicStateStack;

        public ContentsReader(PdfDocument doc, PdfDictionary resource, Stack<GraphicState> grashicStateStack)
        {
            mDoc = doc;
            mResource = resource;
            mFontList = mDoc.GetFonts(mResource);
            mGrashicStateStack = grashicStateStack;
            mGS = mGrashicStateStack.Pop();
        }

        public List<JwwHelper.JwwData?> Read(byte[] script)
        {
            var shapes = new List<JwwHelper.JwwData?>();
            mDoc.ParserGraphics(script, (list, _) =>
            {
                if (list.Count == 0) return true;
                if (list[^1] is not PdfIdentifier pid) return true;
                switch (pid.Identifier)
                {
                    case "w":
                        mGS.StrokeWidth = (float)GetCadDouble(list[^2]);//幅はここでmmに変換する。
                        break;
                    case "cm":
                        {
                            var a = GetDouble(list[^7]);
                            var b = GetDouble(list[^6]);
                            var c = GetDouble(list[^5]);
                            var d = GetDouble(list[^4]);
                            var e = GetDouble(list[^3]);
                            var f = GetDouble(list[^2]);
                            mGS.Ctm *= new TransformMatrix(a, c, b, d, e, f);
                        }
                        break;
                    case "q":
                        mGrashicStateStack.Push(mGS.Copy());
                        break;
                    case "Q":
                        if (mGrashicStateStack.Count > 0) mGS = mGrashicStateStack.Pop();
                        break;
                    //case "G":
                    //    mGS.StrokeColor = ConvertColor(list[^2]);
                    //    break;
                    //case "g":
                    //    mGS.FillColor = ConvertColor(list[^2]);
                    //    break;
                    //case "RG":
                    //    mGS.StrokeColor = ConvertColor(list[^4], list[^3], list[^2]);
                    //    break;
                    //case "rg":
                    //    mGS.FillColor = ConvertColor(list[^4], list[^3], list[^2]);
                    //    break;
                    case "m":
                        {
                            var p = GetPoint(list[^3], list[^2]);
                            mStartPoint.Set(p);
                            mCurrentPoint.Set(p);
                        }
                        break;
                    case "l":
                        {
                            var p = GetPoint(list[^3], list[^2]);
                            var s = new PdfLine(mCurrentPoint.Copy(), p);
                            mPath ??= new List<PdfShape>();
                            mPath.Add(s);
                            mCurrentPoint.Set(p);
                        }
                        break;
                    case "h":
                        {
                            var s = new PdfLine(mCurrentPoint.Copy(), mStartPoint.Copy());
                            mPath ??= new List<PdfShape>();
                            mPath.Add(s);
                            mCurrentPoint.Set(mStartPoint);
                            mPathList.Add(mPath);
                            mPath = null;
                        }
                        break;
                    case "re":
                        {
                            var x = GetDouble(list[^5]);
                            var y = GetDouble(list[^4]);
                            var w = GetDouble(list[^3]);
                            var h = GetDouble(list[^2]);
                            var s = new PdfLine(new CadPoint(x, y), new CadPoint(x + w, y));
                            mPath ??= new List<PdfShape>();
                            mPath.Add(s);
                            s = new PdfLine(new CadPoint(x + w, y), new CadPoint(x + w, y + h));
                            mPath.Add(s);
                            s = new PdfLine(new CadPoint(x + w, y + h), new CadPoint(x, y + h));
                            mPath.Add(s);
                            s = new PdfLine(new CadPoint(x, y + h), new CadPoint(x, y));
                            mPath.Add(s);
                            mPathList.Add(mPath);
                            mPath = null;
                            mCurrentPoint.Set(x, y);
                            mStartPoint.Set(x, y);
                        }
                        break;
                    case "c":
                        {
                            var p1 = new CadPoint(GetDouble(list[^7]), GetDouble(list[^6]));
                            var p2 = new CadPoint(GetDouble(list[^5]), GetDouble(list[^4]));
                            var p3 = new CadPoint(GetDouble(list[^3]), GetDouble(list[^2]));
                            var s = new PdfBezier(mCurrentPoint.Copy(), p1, p2, p3);
                            mPath ??= new List<PdfShape>();
                            mPath.Add(s);
                            mCurrentPoint.Set(p3);
                        }
                        break;
                    case "v":
                        {
                            var p2 = new CadPoint(GetDouble(list[^5]), GetDouble(list[^4]));
                            var p3 = new CadPoint(GetDouble(list[^3]), GetDouble(list[^2]));
                            var s = new PdfBezier(mCurrentPoint.Copy(), mCurrentPoint.Copy(), p2, p3);
                            mPath ??= new List<PdfShape>();
                            mPath.Add(s);
                            mCurrentPoint.Set(p3);
                        }
                        break;
                    case "y":
                        {
                            var p1 = new CadPoint(GetDouble(list[^5]), GetDouble(list[^4]));
                            var p3 = new CadPoint(GetDouble(list[^3]), GetDouble(list[^2]));
                            var s = new PdfBezier(mCurrentPoint.Copy(), p1, p3, p3.Copy());
                            mPath ??= new List<PdfShape>();
                            mPath.Add(s);
                            mCurrentPoint.Set(p3);
                        }
                        break;
                    case "n":
                        {
                            mPath = null;
                            mPathList.Clear();
                        }
                        break;
                    case "s":
                    case "S":
                        {
                            if (pid.Identifier == "s")
                            {
                                var s = new PdfLine(mCurrentPoint.Copy(), mStartPoint.Copy());
                                mPath ??= new List<PdfShape>();
                                mPath.Add(s);
                                mCurrentPoint.Set(mStartPoint);
                                mPathList.Add(mPath);
                                mPath = null;
                            }
                            if (mPath != null) mPathList.Add(mPath);
                            mPath = null;
                            EnumPathList(true, ps =>
                            {
                                switch (ps)
                                {
                                    case PdfLine pl:
                                        {
                                            var line = new JwwHelper.JwwSen();
                                            var p0 = mGS.Ctm * pl.P0;
                                            var p1 = mGS.Ctm * pl.P1;
                                            line.m_start_x = p0.X;
                                            line.m_start_y = p0.Y;
                                            line.m_end_x = p1.X;
                                            line.m_end_y = p1.Y;
//                                            SetLineAttribute(line);
                                            shapes.Add(line);
                                        }
                                        break;
                                    case PdfBezier b:
                                        {
                                            var pts = Curve.CreateBezier3(b.P0, b.P1, b.P2, b.P3, 16, true);
                                            for(var i = 1; i < pts.Count; i++)
                                            {
                                                var p0 = mGS.Ctm * pts[i - 1];
                                                var p1 = mGS.Ctm * pts[i];
                                                var line = new JwwHelper.JwwSen();
                                                line.m_start_x = p0.X;
                                                line.m_start_y = p0.Y;
                                                line.m_end_x = p1.X;
                                                line.m_end_y = p1.Y;
//                                                SetLineAttribute(line);
                                                shapes.Add(line);
                                            }
                                        }
                                        break;
                                }
                            });
                        }
                        break;
                    case "b":
                    case "b*":
                    case "B":
                    case "B*":
                    case "f":
                    case "F":
                    case "f*":
                        //fもf*も同じとする（even/oddのみ）。
                        {
                            if (mPath != null) mPathList.Add(mPath);
                            mPath = null;
/*
                            var pointsList = new List<List<CadPoint>>();
                            EnumPathList(true, ps =>
                            {
                                switch (ps)
                                {
                                    case PdfLine pl:
                                        {
                                            var pts = new List<CadPoint>();
                                            pts.Add(pl.P0);
                                            pts.Add(pl.P1);
                                            pointsList.Add(pts);
                                        }
                                        break;
                                    case PdfBezier b:
                                        {
                                            var pts = Curve.CreateBezier3(b.P0, b.P1, b.P2, b.P3, 16, true);
                                            pointsList.Add(pts);
                                        }
                                        break;
                                    case null:
                                        {
                                            var polys = PolylineHelper.GetConnectedLines(pointsList, 0.0001);
                                            foreach (var poly in polys)
                                            {
                                                PolylineHelper.Degenerate(poly, true);
                                                if (poly.Count > 2)
                                                {
                                                    var s = new PolylineShape();
                                                    s.AddPoints(poly);
                                                    s.IsClosed = true;
                                                    s.Transform(mGS.Ctm);
                                                    s.LineStyle.Color = System.Drawing.Color.Transparent;
                                                    SetSolidAttribute(s);
                                                    shapes.Add(s);
                                                }
                                            }
                                            pointsList.Clear();
                                        }
                                        break;
                                }
                            });
*/
                        }
                        break;

                    case "BT":
                        {
                            mIsTextState = true;
                            mTextParameter = new();
                        };
                        break;
                    case "TL":
                        mGS.Tl = GetDouble(list[^2]);
                        break;
                    case "Tf":
                        mGS.FontName = list[^3].ToString();
                        mGS.FontSize = (float)GetDouble(list[^2]);
                        break;

                    case "Do":
                        {
                            if (list[^2] is PdfName name)
                            {
                                var xobject = mDoc.GetXObjectStream(mResource, name.Name) ??
                                    throw new Exception($"Do command, cannot find XObject {name.Name}");
                                var subtype = xobject.Dictionary.GetValue<PdfName>("/Subtype")?.Name ??
                                    throw new Exception($"Do command, cannot find Subtype of XObject.");
                                switch (subtype)
                                {
                                    //case "/Image":
                                    //    {
                                    //        var s = CreateImageShape(xobject!);
                                    //        if (s != null) shapes.Add(s);
                                    //    }
                                    //    break;
                                    case "/Form":
                                        {
                                            var buf = xobject.GetExtractedBytes();
                                            if (buf == null) throw new Exception($"Form {name.Name} cannot get data.");
                                            var xobjectResorce = mDoc.GetEntityObject<PdfDictionary>(xobject.Dictionary.GetValue("/Resources")) ?? mResource;
                                            mGrashicStateStack.Push(mGS);
                                            var formContentsReader = new ContentsReader(mDoc, xobjectResorce, mGrashicStateStack);
                                            var formShapes = formContentsReader.Read(buf);
                                            foreach (var s in formShapes)
                                            {
                                                shapes.Add(s);
                                            }
                                        }
                                        break;
                                    default:
                                        throw new Exception($"Do command not support XObject {subtype}.");
                                }
                            }
                        }
                        break;
                    case "BI":
                        throw new Exception($"Sorry, BI command (Inline image) is not supported.");
                    default:
                        if (mIsTextState)
                        {
                            ParseTextObject(shapes, list, pid.Identifier);
                            return true;
                        }
                        break;
                }
                return true;
            });
            return shapes;
        }
/*
        CadShape? CreateImageShape(PdfStream xobject)
        {
            var filter = xobject.Dictionary.GetValue<PdfName>("/Filter")?.Name;
            var width = xobject.Dictionary.GetValue<PdfNumber>("/Width")!.IntValue;
            var height = xobject.Dictionary.GetValue<PdfNumber>("/Height")!.IntValue;
            switch (filter)
            {
                case "/DCTDecode":
                    {
                        var buf = xobject.Data;
                        if (buf == null) throw new Exception($"Image /DCTDecode has no data.");
                        var s = new ImageShape();
                        s.Bytes = buf;
                        s.P0.Set(0.5, 0.5);
                        s.Width = 1.0;
                        s.Height = 1.0;
                        s.Transform(mGS.Ctm);
                        return s;
                    }
                case "/FlateDecode":
                    {
                        var buf = xobject.GetExtractedBytes();
                        if (buf == null)
                        {
                            Debug.WriteLine($"do command not support image of Filter={filter}");
                        }
                        else
                        {
                            var colorSpace = xobject.Dictionary.GetValue<PdfName>("/ColorSpace");
                            var cs = colorSpace?.Name switch
                            {
                                "/DeviceRGB" => ImageType.RGB,
                                "/DeviceCMYK" => ImageType.CMYK,
                                "/DeviceGray" => ImageType.Gray,
                                _ => ImageType.None,
                            };

                            if (cs != ImageType.None)
                            {
                                var bmp = Helper.CtreateImageFromRaw(buf, width, height, cs);
                                var s = new ImageShape();
                                var mem = new MemoryStream();
                                bmp.Save(mem, ImageFormat.Png);
                                mem.Close();
                                s.Bytes = mem.ToArray();
                                s.P0.Set(0.5, 0.5);
                                s.Width = 1.0;
                                s.Height = 1.0;
                                s.Transform(mGS.Ctm);
                                return s;
                            }
                            else
                            {
                                Debug.WriteLine($"Not supported image {colorSpace?.Name}");
                            }
                        }
                    }
                    break;
                default:
                    Debug.WriteLine($"do command not support image of Filter={filter}");
                    break;
            }
            return null;
        }
*/
        void EnumPathList(bool clearpathList, Action<PdfShape?> action)
        {
            if (mPath != null) mPathList.Add(mPath);
            mPath = null;
            if (mPathList.Count > 0)
            {
                foreach (var path in mPathList)
                {
                    foreach (var ps in path) action(ps);
                    action(null);
                }
            }
            if (clearpathList) mPathList.Clear();
        }

        void ParseTextObject(List<JwwHelper.JwwData?> shapes, List<PdfObject> list, string cmd)
        {
            switch (cmd)
            {
                case "ET":
                    mIsTextState = false;
                    return;


                case "Td":
                    {
                        var t = new TransformMatrix(1, 0, 0, 1, GetDouble(list[^3]), GetDouble(list[^2]));
                        mTextParameter.Tlm = mTextParameter.Tlm * t;
                        mTextParameter.Tm = mTextParameter.Tlm.Copy();
                    }
                    break;
                case "TD":
                    {
                        mGS.Tl = -GetDouble(list[^2]);
                        var t = new TransformMatrix(1, 0, 0, 1, GetDouble(list[^3]), GetDouble(list[^2]));
                        mTextParameter.Tlm = mTextParameter.Tlm * t;
                        mTextParameter.Tm = mTextParameter.Tlm.Copy();
                    }
                    break;
                case "T*":
                    {
                        CommandTAsterrisk();
                    }
                    break;
                case "Tm":
                    {
                        var a = GetDouble(list[^7]);
                        var b = GetDouble(list[^6]);
                        var c = GetDouble(list[^5]);
                        var d = GetDouble(list[^4]);
                        var e = GetDouble(list[^3]);
                        var f = GetDouble(list[^2]);
                        mTextParameter.Tlm = new TransformMatrix(a, c, b, d, e, f);
                        mTextParameter.Tm = mTextParameter.Tlm.Copy();
                    }
                    break;
                case "Tj":
                    AddTextShape(shapes, list[^2]);
                    break;
                case "TJ":
                    AddTextShape(shapes, list[^2]);
                    break;
                case "'":
                    {
                        CommandTAsterrisk();
                        AddTextShape(shapes, list[^2]);
                    }
                    break;
                case "\"":
                    {
                        CommandTAsterrisk();
                        //Todo aw(list[^4]) ac(list[^3])は未実装
                        AddTextShape(shapes, list[^2]);
                    }
                    break;

                default:
                    Debug.WriteLine($"not support text command {cmd}");
                    break;
            }
        }

        void CommandTAsterrisk()
        {
            var t = new TransformMatrix(1, 0, 0, 1, 0, -mGS.Tl);
            mTextParameter.Tlm *= t;
            mTextParameter.Tm = mTextParameter.Tlm.Copy();
        }

        void AddTextShape(List<JwwHelper.JwwData?> shapes, PdfObject obj)
        {
            var fontSizeBase = 1000.0;
            var font = mFontList.Find(x => x.Name == mGS.FontName);
            var dy = 0.0;
            if (font != null)
            {
                fontSizeBase = font.FontBBox.Height;
                dy = font.Descent;
            }

            string text = "";
            Byte[]? buf = null;
            switch (obj)
            {
                case PdfString ps:
                    {
                        buf = ps.Text.Buffer;
                        text = font != null ? font.ConvertString(buf) : obj.ToString();
                    }
                    break;
                case PdfHexString hs1:
                    {
                        buf = hs1.Bytes;
                        text = font != null ? font.ConvertString(buf) : obj.ToString();
                    }
                    break;
                case PdfArray pa:
                    {
                        var sb = new StringBuilder();
                        foreach (var b in pa)
                        {
                            switch (b)
                            {
                                case PdfHexString hs:
                                    sb.Append(font != null ? font.ConvertString(hs.Bytes) : obj.ToString());
                                    break;
                                case PdfString ps2:
                                    sb.Append(font != null ? font.ConvertString(ps2.Text.Buffer) : obj.ToString());
                                    break;
                                case PdfNumber num:
                                    if (num.DoubleValue < -225) sb.Append(' ');
                                    break;
                            }
                        }
                        text = sb.ToString();
                    }
                    break;
            }
            if (text != "")
            {
                text = PdfUtility.UnicodeHelper.UnifiedKanjiConverter(text);

                var t = new JwwHelper.JwwMoji();
                t.m_string = text;
                t.m_strFontName = "ＭＳ ゴシック";
                var w = GetTextWidth(text, mGS.FontSize);
                Debug.WriteLine($"{this.GetType().Name}::Text({text}), {mGS.FontSize}, ({mTextParameter.Tm}), ({mGS.Ctm})");

                //文字の幅と高さを変換。ただし、ここまでしなくてもたいていは縦横同比なのでもっと単純化してもいいかも。
                //たとえば行列式det(m1)の平方根を幅と高さにかけて角度はそのままなど。
                var m1 = new Matrix2D(mTextParameter.Tm.A, mTextParameter.Tm.B, mTextParameter.Tm.C, mTextParameter.Tm.D);
                var m2 = new Matrix2D(mGS.Ctm.A, mGS.Ctm.B, mGS.Ctm.C, mGS.Ctm.D);
                var r = CadPointHelper.TransformedRectangle(w, mGS.FontSize, 0.0, m1);
                r = CadPointHelper.TransformedRectangle(r.Width, r.Height, r.AngleDeg, m2);
                t.m_dSizeY = r.Height;
                t.m_dSizeX = mGS.FontSize * r.Width / w;
                t.m_degKakudo = r.AngleDeg;

                var p0 = new CadPoint(0, dy * mGS.FontSize / 1000.0);
                var p1 = new CadPoint(w, dy * mGS.FontSize / 1000.0);
//                var p1 = new CadPoint(r.Width, dy * mGS.FontSize / 1000.0);
                p0 = mTextParameter.Tm * p0;
                p0 = mGS.Ctm * p0;
                p1 = mTextParameter.Tm * p1;
                p1 = mGS.Ctm * p1;
                t.m_start_x = p0.X;
                t.m_start_y = p0.Y;
                t.m_end_x = p1.X;
                t.m_end_y = p1.Y;
                shapes.Add(t);
                var x = new TransformMatrix(1, 0, 0, 1, w, 0);
                mTextParameter.Tm = mTextParameter.Tm * x;
            }
        }
        public static double GetTextWidth(string text, double height)
        {
            var w = 0.0;
            var enc = Encoding.GetEncoding("shift_jis");
            foreach (var c in text)
            {
                w += height * enc.GetByteCount(c.ToString());
            }
            return w;
        }

/*
        System.Drawing.Color ConvertColor(PdfObject obj)
        {
            if (obj is not PdfNumber n)
            {
                Debug.WriteLine($"Color is not number.{obj}");
                return System.Drawing.Color.Black;
            }
            var c = (int)(n.DoubleValue * 255);
            return System.Drawing.Color.FromArgb(c, c, c);
        }

        System.Drawing.Color ConvertColor(PdfObject ro, PdfObject go, PdfObject bo)
        {
            if (ro is not PdfNumber rn || go is not PdfNumber gn || bo is not PdfNumber bn)
            {
                Debug.WriteLine($"Color is not number.{ro} {bo} {go}");
                return System.Drawing.Color.Black;
            }
            var r = (int)(rn.DoubleValue * 255);
            var g = (int)(gn.DoubleValue * 255);
            var b = (int)(bn.DoubleValue * 255);
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        void SetLineAttribute(CadShape s)
        {
            //線幅などは拡大縮小による幅が変わらなけえればいけない。その係数がd。
            //行列式の絶対値の平方根を係数とする。
            var d = Math.Sqrt(Math.Abs(mGS.Ctm.A * mGS.Ctm.D - mGS.Ctm.B * mGS.Ctm.C));
            s.SetAttribute(ShapeAttribute.ATTR_LINE_STYLE_WIDTH_FLOAT, (float)(mGS.StrokeWidth * d));
            s.SetAttribute(ShapeAttribute.ATTR_LINE_STYLE_COLOR, mGS.StrokeColor);
        }

        void SetSolidAttribute(CadShape s)
        {
            s.SetAttribute(ShapeAttribute.ATTR_SOLID_STYLE_COLOR, mGS.FillColor);
        }
*/



    }
}
