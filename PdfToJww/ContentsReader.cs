using PdfToJww.CadMath2D;
using PdfToJww.Shape;
using PdfUtility;
using System.Diagnostics;
using System.Drawing;
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
            public Color StrokeColor = Color.Black;
            public Color FillColor = Color.Black;
            public string FontName = "";
            public float FontSize = 4.0f;
            public double Tl = 5.0;
            public GraphicState Copy()=> (GraphicState)MemberwiseClone();
        }

        class TextParameter
        {
            public TransformMatrix Tm = new();
            public TransformMatrix Tlm = new();
        }

        int mBezierDiv = 10;

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

        bool mUnifyKanji;

        public ContentsReader(PdfDocument doc, PdfDictionary resource, Stack<GraphicState> grashicStateStack, bool unifyKanji)
        {
            mDoc = doc;
            mResource = resource;
            mFontList = mDoc.GetFonts(mResource);
            mGrashicStateStack = grashicStateStack;
            mGS = mGrashicStateStack.Pop();
            mUnifyKanji = unifyKanji;
        }

        public List<PShape?> Read(byte[] script)
        {
            var shapes = new List<PShape?>();
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
                        CmdCurrentMatrix(list);
                        break;
                    case "q":
                        mGrashicStateStack.Push(mGS.Copy());
                        break;
                    case "Q":
                        if (mGrashicStateStack.Count > 0) mGS = mGrashicStateStack.Pop();
                        break;
                    case "G":
                        mGS.StrokeColor = ConvertColor(list[^2]);
                        break;
                    case "g":
                        mGS.FillColor = ConvertColor(list[^2]);
                        break;
                    case "RG":
                        mGS.StrokeColor = ConvertColor(list[^4], list[^3], list[^2]);
                        break;
                    case "rg":
                        mGS.FillColor = ConvertColor(list[^4], list[^3], list[^2]);
                        break;
                    case "m":
                        CmdMove(list);
                        break;
                    case "l":
                        CmdL(list);
                        break;
                    case "h":
                        CmdH();
                        break;
                    case "re":
                        CmdPlotRectangle(list);
                        break;
                    case "c":
                        CmdPlotBezier(GetPoint(list[^7], list[^6]), GetPoint(list[^5], list[^4]), GetPoint(list[^3], list[^2]));
                        break;
                    case "v":
                        CmdPlotBezier(mCurrentPoint, GetPoint(list[^5], list[^4]), GetPoint(list[^3], list[^2]));
                        break;
                    case "y":
                        CmdPlotBezier(GetPoint(list[^5], list[^4]), GetPoint(list[^3], list[^2]), GetPoint(list[^3], list[^2]));
                        break;
                    case "n":
                        mPath = null;
                        mPathList.Clear();
                        break;
                    case "s":
                    case "S":
                        CmdS(pid.Identifier, list, shapes);
                        break;
                    case "b":
                    case "b*":
                    case "B":
                    case "B*":
                    case "f":
                    case "F":
                    case "f*":
                        CmdFill(pid.Identifier, list, shapes);
                        break;
                    case "BT":
                        mIsTextState = true;
                        mTextParameter = new();
                        break;
                    case "TL":
                        mGS.Tl = GetDouble(list[^2]);
                        break;
                    case "Tf":
                        mGS.FontName = list[^3].ToString();
                        mGS.FontSize = (float)GetDouble(list[^2]);
                        break;
                    case "Do":
                        CmdDo(list, shapes);
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

        //cm command (set current transform matrix)
        void CmdCurrentMatrix(List<PdfObject> list)
        {
            var a = GetDouble(list[^7]);
            var b = GetDouble(list[^6]);
            var c = GetDouble(list[^5]);
            var d = GetDouble(list[^4]);
            var e = GetDouble(list[^3]);
            var f = GetDouble(list[^2]);
            mGS.Ctm *= new TransformMatrix(a, c, b, d, e, f);
        }

        //m command (move current point)
        void CmdMove(List<PdfObject> list)
        {
            var p = GetPoint(list[^3], list[^2]);
            mStartPoint.Set(p);
            mCurrentPoint.Set(p);
        }

        //l command (plot line)
        void CmdL(List<PdfObject> list)
        {
            var p = GetPoint(list[^3], list[^2]);
            var s = new PdfLine(mCurrentPoint.Copy(), p);
            mPath ??= new List<PdfShape>();
            mPath.Add(s);
            mCurrentPoint.Set(p);
        }

        //h command. (close path)
        void CmdH()
        {
            var s = new PdfLine(mCurrentPoint.Copy(), mStartPoint.Copy());
            mPath ??= new List<PdfShape>();
            mPath.Add(s);
            mCurrentPoint.Set(mStartPoint);
            mPathList.Add(mPath);
            mPath = null;
        }

        //re command (plot rectangle)
        void CmdPlotRectangle(List<PdfObject> list)
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

        void CmdPlotBezier(CadPoint p1, CadPoint p2, CadPoint p3)
        {
            var s = new PdfBezier(mCurrentPoint.Copy(), p1.Copy(), p2.Copy(), p3.Copy());
            mPath ??= new List<PdfShape>();
            mPath.Add(s);
            mCurrentPoint.Set(p3);
        }

        void CmdS(string cmd, List<PdfObject> list, List<PShape?> shapes)
        {
            if (cmd == "s")
            {
                //パスを閉じる
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
                            var line = new PLineShape(pl.P0, pl.P1);
                            line.Transform(mGS.Ctm);
                            SetLineAttribute(line);
                            shapes.Add(line);
                        }
                        break;
                    case PdfBezier b:
                        {
                            var pts = Curve.CreateBezier3(b.P0, b.P1, b.P2, b.P3, mBezierDiv, true);
                            for (var i = 1; i < pts.Count; i++)
                            {
                                var line = new PLineShape(pts[i - 1], pts[i]);
                                line.Transform(mGS.Ctm);
                                SetLineAttribute(line);
                                shapes.Add(line);
                            }
                        }
                        break;
                }
            });
        }

        void CmdFill(string cmd, List<PdfObject> list, List<PShape?> shapes)
        {
            if (mPath != null) mPathList.Add(mPath);
            mPath = null;
            //未実装
            mPathList.Clear();
        }

        void CmdDo(List<PdfObject> list, List<PShape?> shapes)
        {
            if (list[^2] is PdfName name)
            {
                var xobject = mDoc.GetXObjectStream(mResource, name.Name) ??
                    throw new Exception($"Do command, cannot find XObject {name.Name}");
                var subtype = xobject.Dictionary.GetValue<PdfName>("/Subtype")?.Name ??
                    throw new Exception($"Do command, cannot find Subtype of XObject.");
                switch (subtype)
                {
                    case "/Image":
                        {
                            var s = CreateImageShape(xobject!);
                            if (s != null) shapes.Add(s);
                        }
                        break;
                    case "/Form":
                        {
                            var buf = xobject.GetExtractedBytes();
                            if (buf == null) throw new Exception($"Form {name.Name} cannot get data.");
                            var xobjectResorce = mDoc.GetEntityObject<PdfDictionary>(xobject.Dictionary.GetValue("/Resources")) ?? mResource;
                            mGrashicStateStack.Push(mGS);
                            var formContentsReader = new ContentsReader(mDoc, xobjectResorce, mGrashicStateStack, mUnifyKanji);
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

        PImageShape? CreateImageShape(PdfStream xobject)
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
                        var mem = new MemoryStream(buf);
                        var image = Image.FromStream(mem);
                        var s = new PImageShape(image, new CadPoint(0, 0), 1.0, 1.0, 0);
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
                                var s = new PImageShape(bmp, new CadPoint(0, 0), 1.0, 1.0, 0);
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

        void ParseTextObject(List<PShape?> shapes, List<PdfObject> list, string cmd)
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

        void AddTextShape(List<PShape?> shapes, PdfObject obj)
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
                if (mUnifyKanji) text = UnicodeHelper.UnifiedKanjiConverter(text);
                var t = new PTextShape(text, new CadPoint(0, 0), mGS.FontSize, 0);
                t.FontName = "ＭＳ ゴシック";
                t.P0.Y += dy * t.Height / 1000.0;
                var w = t.Width;
                t.Transform(mTextParameter.Tm);
                t.Transform(mGS.Ctm);
                var x = new TransformMatrix(1, 0, 0, 1, w, 0);
                mTextParameter.Tm = mTextParameter.Tm * x;
                shapes.Add(t);
            }
        }

        Color ConvertColor(PdfObject obj)
        {
            if (obj is not PdfNumber n)
            {
                Debug.WriteLine($"Color is not number.{obj}");
                return Color.Black;
            }
            var c = (int)(n.DoubleValue * 255);
            return Color.FromArgb(c, c, c);
        }

        Color ConvertColor(PdfObject ro, PdfObject go, PdfObject bo)
        {
            if (ro is not PdfNumber rn || go is not PdfNumber gn || bo is not PdfNumber bn)
            {
                Debug.WriteLine($"Color is not number.{ro} {bo} {go}");
                return Color.Black;
            }
            var r = (int)(rn.DoubleValue * 255);
            var g = (int)(gn.DoubleValue * 255);
            var b = (int)(bn.DoubleValue * 255);
            return Color.FromArgb(r, g, b);
        }

        void SetLineAttribute(PLineShape s)
        {
            //線幅などは拡大縮小による幅が変わらなけえればいけない。その係数がd。
            //行列式の絶対値の平方根を係数とする。
            var d = Math.Sqrt(Math.Abs(mGS.Ctm.A * mGS.Ctm.D - mGS.Ctm.B * mGS.Ctm.C));
            s.StrokeColor = mGS.StrokeColor;
            s.StrokeWidth = mGS.StrokeWidth * d;
        }


        /*
                        double GetLineWidth()
                        {
                            //線幅などは拡大縮小により幅が変わらなければいけない。その係数がd。
                            //行列式の絶対値の平方根を係数とする。
                            var d = Math.Sqrt(Math.Abs(mGS.Ctm.A * mGS.Ctm.D - mGS.Ctm.B * mGS.Ctm.C));
                            return mGS.StrokeWidth * d;
                        }
        */
    }
}
