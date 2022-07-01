namespace PdfToJww
{
    public class JwwPaper
    {
        public JwwPaper(string paperName, double width, double height, int code)
        {
            PaperName = paperName;
            Width = width;
            Height = height;
            Code = code;
        }

        public string PaperName { get; }
        public double Width { get; }
        public double Height { get; }
        public int Code { get; }

        public override bool Equals(object? obj)
        {
            return obj is JwwPaper paper &&
                   Width == paper.Width &&
                   Height == paper.Height &&
                   PaperName == paper.PaperName &&
                   Code == paper.Code;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, PaperName, Code);
        }

        public override string ToString() => $"{PaperName}  {Width} X {Height}";


        /// <summary>
        /// Jwwの図面コードを用紙サイズに変換
        /// </summary>
        static public JwwPaper GetJwwPaperSize(int code)
        {
            //わからなかったらひとまずA3
            return JwwPaperSizeArray.FirstOrDefault(x => x.Code == code, JwwPaperSizeArray[3]);
        }

        public static readonly JwwPaper[] JwwPaperSizeArray = new JwwPaper[]{
            new JwwPaper("A0", 1189.0, 841.0, 0), //A0
            new JwwPaper("A1",841.0, 594.0, 1),  //A1
            new JwwPaper("A2",594.0, 420.0, 2),  //A2
            new JwwPaper("A3",420.0, 297.0, 3),  //A3
            new JwwPaper("A4",297.0, 210.0, 4),  //A4
            //new JwwPaper(210.0, 148.0),  //A5???使わない
            //new JwwPaper(210.0, 148.0),  //A6???使わない
            //new JwwPaper(148.0, 105.0),  //A7???使わない
            new JwwPaper("2A", 1682.0, 1189.0, 8),  //8:2A
            new JwwPaper("3A", 2378.0, 1682.0, 9),  //9:3A
            new JwwPaper("4A", 3364.0, 2378.0, 10),  //10:4A
            new JwwPaper("5A", 4756.0, 3364.0, 11),  //11:5A
            new JwwPaper("10m", 10000.0, 7071.0, 12),  //12:10m
            new JwwPaper("50m", 50000.0, 35355.0, 13),  //13:50m
            new JwwPaper("100m", 100000.0, 70711.0, 14)  //14:100m
        };

    }
}
