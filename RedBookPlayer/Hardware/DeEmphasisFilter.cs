using System;
using NWaves.Filters.BiQuad;

namespace RedBookPlayer.Hardware
{
    /// <summary>
    /// Filter for applying de-emphasis to audio
    /// </summary>
    public class DeEmphasisFilter : BiQuadFilter
    {
        private static readonly double B0;
        private static readonly double B1;
        private static readonly double B2;
        private static readonly double A0;
        private static readonly double A1;
        private static readonly double A2;

        static DeEmphasisFilter()
        {
            double fc    = 5277;
            double slope = 0.4850;
            double gain  = -9.465;

            double w0    = 2 * Math.PI * fc / 44100;
            double A     = Math.Exp(gain / 40 * Math.Log(10));
            double alpha = Math.Sin(w0) / 2 * Math.Sqrt(((A + (1 / A)) * ((1 / slope) - 1)) + 2);

            double cs = Math.Cos(w0);
            double v  = 2 * Math.Sqrt(A) * alpha;

            B0 = A  * (A                          + 1 + ((A - 1) * cs) + v);
            B1 = -2 * A * (A - 1                  + ((A     + 1) * cs));
            B2 = A  * (A     + 1 + ((A - 1) * cs) - v);
            A0 = A                                + 1 - ((A - 1) * cs) + v;
            A1 = 2 * (A                           - 1 - ((A + 1) * cs));
            A2 = A + 1 - ((A - 1) * cs) - v;

            B2 /= A0;
            B1 /= A0;
            B0 /= A0;
            A2 /= A0;
            A1 /= A0;
            A0 =  1;
        }

        public DeEmphasisFilter() : base(B0, B1, B2, A0, A1, A2) {}
    }
}