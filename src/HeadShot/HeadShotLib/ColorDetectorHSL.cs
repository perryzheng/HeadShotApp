using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HeadShotLib
{
    public class ColorDetectorHSL
    {
        public bool IsCalibrated { get; private set; }

        public double HAvg, SAvg, LAvg;

        public float HRange = 5f;
        public float SRange = 0.5f;
        public float LRange = 0.5f;

        public float DetectThreshold = 0.5f;

        int w, h;

        public ColorDetectorHSL(int w, int h)
        {
            this.w = w;
            this.h = h;
        }

        public void Calibrate(Color[] data, int targ_cx, int targ_cy, int targ_halfWidth)
        {
            HAvg = 0;
            SAvg = 0;
            LAvg = 0;

            for (int y = targ_cy - targ_halfWidth; y <= targ_cy + targ_halfWidth; y++)
                for (int x = targ_cx - targ_halfWidth; x <= targ_cx + targ_halfWidth; x++)
                {
                    int i = y * w + x;

                    Color c = data[i];

                    var hsl = HslColor.FromColor(c);

                    HAvg += hsl.H;
                    SAvg += hsl.S;
                    LAvg += hsl.L;
                }

            int numPx = (2 * targ_halfWidth + 1) * (2 * targ_halfWidth + 1);
            HAvg /= numPx;
            SAvg /= numPx;
            LAvg /= numPx;

            IsCalibrated = true;
        }

        public bool DetectWithTransform(Color[] data, int targ_cx, int targ_cy, int targ_halfWidth, out Color[] transformedData)
        {
            double hMin = (double)((float)HAvg - HRange).Clamp(0, 360);
            double hMax = (double)((float)HAvg + HRange).Clamp(0, 360);
            double sMin = (double)((float)SAvg - SRange).Clamp(0, 1);
            double sMax = (double)((float)SAvg + SRange).Clamp(0, 1);
            double lMin = (double)((float)LAvg - LRange).Clamp(0, 1);
            double lMax = (double)((float)LAvg + LRange).Clamp(0, 1);

            transformedData = new Color[data.Length];

            int matchesInTarg = 0;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;

                    Color c = data[i];

                    var hsl = HslColor.FromColor(c);

                    if (hsl.H > hMin && hsl.H < hMax &&
                        hsl.S > sMin && hsl.S < sMax &&
                        hsl.L > lMin && hsl.L < lMax)
                    {
                        //within range -- matched pixel
                        c.A = 255;
                        c.R = 255;
                        c.G = 0;
                        c.B = 0;

                        if (x >= targ_cx - targ_halfWidth && x <= targ_cx + targ_halfWidth &&
                            y >= targ_cy - targ_halfWidth && y <= targ_cy + targ_halfWidth)
                        {
                            matchesInTarg++;
                        }
                    }
                    else
                    {
                        c.A = 0;
                        c.R = 0;
                        c.G = 255;
                        c.B = 0;
                    }

                    transformedData[i] = c;
                }

            int numPxTarg = (2 * targ_halfWidth + 1) * (2 * targ_halfWidth + 1);
            if ((float)matchesInTarg / numPxTarg >= DetectThreshold)
                return true;
            else
                return false;

        }
    }
}
