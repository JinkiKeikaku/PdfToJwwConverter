using System;

namespace PdfToJwwConverter
{
    public class JwwScale
    {
        public double ScaleNumber { get; set; } = 1.0;

        public JwwScale(double scaleNumber)
        {
            ScaleNumber = scaleNumber;
        }

        public override string ToString()
        {
            if (ScaleNumber < 1)
            {
                return $"{1.0 / ScaleNumber} : 1";
            }
            return $"1 : {ScaleNumber}";
        }

        public override bool Equals(object? obj)
        {
            return obj is JwwScale scale &&
                   ScaleNumber == scale.ScaleNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ScaleNumber);
        }
    }
}
