namespace PdfToJww.CadMath2D
{
    /// <summary>
    /// 領域の大きさなど幅と高さを持つ対象のためのクラス
    /// </summary>
    public class CadSize
    {
        /// <summary>
        /// 幅
        /// </summary>
        public double Width;
        /// <summary>
        /// 高さ
        /// </summary>
        public double Height;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadSize() { }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CadSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Width} {Height}";
        }

        /// <summary>
        /// 定数倍
        /// </summary>
        public static CadSize operator *(CadSize a, double b) => new CadSize(a.Width * b, a.Height * b);

        /// <summary>
        /// 定数割り算
        /// </summary>
        public static CadSize operator /(CadSize a, double b) => new CadSize(a.Width / b, a.Height / b);


    }
}
