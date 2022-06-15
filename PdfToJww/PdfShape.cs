using PdfToJww.CadMath2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToJww
{
    class PdfShape { }

    class PdfLine : PdfShape
    {
        public CadPoint P0;
        public CadPoint P1;
        public PdfLine(CadPoint p0, CadPoint p1)
        {
            P0 = p0;
            P1 = p1;
        }
    }

    class PdfBezier : PdfShape
    {
        public CadPoint P0;
        public CadPoint P1;
        public CadPoint P2;
        public CadPoint P3;

        public PdfBezier(CadPoint p0, CadPoint p1, CadPoint p2, CadPoint p3)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }
    }
}
