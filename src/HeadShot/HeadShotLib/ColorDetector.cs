using System;
using System.Net;
using System.Windows;
using System.Windows.Media;

namespace HeadShotLib
{
    public struct YCbCrColor
    {
        public float Y, Cb, Cr;

        public YCbCrColor(float y, float cb, float cr)
        {
            Y = y;
            Cb = cb;
            Cr = cr;
        }

        public float DistanceSqTo(YCbCrColor otherColor)
        {
            return (this.Y - otherColor.Y) * (this.Y - otherColor.Y) + (this.Cb - otherColor.Cb) * (this.Cb - otherColor.Cb) + (this.Cr - otherColor.Cr) * (this.Cr - otherColor.Cr);
        }


        public void LoadFromString(string calib)
        {
            var tokens = calib.Split(',');
            Y = float.Parse(tokens[0]);
            Cb = float.Parse(tokens[1]);
            Cr = float.Parse(tokens[2]);
        }
        public string SaveToString()
        {
            return string.Format("{0},{1},{2}", Y, Cb, Cr);
        }
    }

    public class ColorDetector
    {
        public bool IsCalibrated { get; private set; }

        public float YAvg, CbAvg, CrAvg;

        public const float CbRange = 25;
        public const float CrRange = 25;
        public const float YRange = 70;

        public float DetectThreshold = 0.5f;

        int w, h;


        public ColorDetector(int w, int h)
        {
            this.w = w;
            this.h = h;
        }


        public YCbCrColor GetCalibrationYCbCrColor()
        {
            return new YCbCrColor(YAvg, CbAvg, CrAvg);
        }
        public void LoadFromCalibration(YCbCrColor calibColor)
        {
            YAvg = calibColor.Y;
            CbAvg = calibColor.Cb;
            CrAvg = calibColor.Cr;

            IsCalibrated = true;
        }

        public void Calibrate(Color[] data, int targ_cx, int targ_cy, int targ_halfWidth)
        {
            CbAvg = 0;
            CrAvg = 0;
            YAvg = 0;

            for (int y = targ_cy - targ_halfWidth; y <= targ_cy + targ_halfWidth; y++)
                for (int x = targ_cx - targ_halfWidth; x <= targ_cx + targ_halfWidth; x++)
                {
                    int i = y * w + x;

                    Color c = data[i];

                    byte _y, _cb, _cr;
                    ImageUtils.RGB_to_YCbCr(c.R, c.G, c.B, out _y, out _cb, out _cr);
                    CbAvg += _cb;
                    CrAvg += _cr;
                    YAvg += _y;
                }

            int numPx = (2 * targ_halfWidth + 1) * (2 * targ_halfWidth + 1);
            CbAvg /= numPx;
            CrAvg /= numPx;
            YAvg /= numPx;

            IsCalibrated = true;
        }

        public bool DetectWithTransform(Color[] data, int targ_cx, int targ_cy, int targ_halfWidth, out Color[] transformedData)
        {
            byte cbMin = (byte)(CbAvg - CbRange).Clamp(0, 255);
            byte cbMax = (byte)(CbAvg + CbRange).Clamp(0, 255);
            byte crMin = (byte)(CrAvg - CrRange).Clamp(0, 255);
            byte crMax = (byte)(CrAvg + CrRange).Clamp(0, 255);
            byte yMin = (byte)(YAvg - YRange).Clamp(0, 255);
            byte yMax = (byte)(YAvg + YRange).Clamp(0, 255);

            transformedData = new Color[data.Length];

            int matchesInTarg = 0;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;

                    Color c = data[i];

                    byte Y, Cb, Cr;
                    ImageUtils.RGB_to_YCbCr(c.R, c.G, c.B, out Y, out Cb, out Cr);

                    if (Cb > cbMin && Cb < cbMax &&
                        Cr > crMin && Cr < crMax &&
                        Y > yMin && Y < yMax)
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

            return ((float)matchesInTarg / numPxTarg >= DetectThreshold);
        }

        public bool DetectThresh(Color[] data, int targ_cx, int targ_cy, int targ_halfWidth)
        {
            byte cbMin = (byte)(CbAvg - CbRange).Clamp(0, 255);
            byte cbMax = (byte)(CbAvg + CbRange).Clamp(0, 255);
            byte crMin = (byte)(CrAvg - CrRange).Clamp(0, 255);
            byte crMax = (byte)(CrAvg + CrRange).Clamp(0, 255);
            byte yMin = (byte)(YAvg - YRange).Clamp(0, 255);
            byte yMax = (byte)(YAvg + YRange).Clamp(0, 255);


            int matchesInTarg = 0;

            for (int y = targ_cy - targ_halfWidth; y <= targ_cy + targ_halfWidth; y++)
                for (int x = targ_cx - targ_halfWidth; x <= targ_cx + targ_halfWidth; x++)
                {
                    int i = y * w + x;

                    Color c = data[i];

                    byte Y, Cb, Cr;
                    ImageUtils.RGB_to_YCbCr(c.R, c.G, c.B, out Y, out Cb, out Cr);

                    if (Cb > cbMin && Cb < cbMax &&
                        Cr > crMin && Cr < crMax &&
                        Y > yMin && Y < yMax)
                    {
                        matchesInTarg++;
                    }
                }

            int numPxTarg = (2 * targ_halfWidth + 1) * (2 * targ_halfWidth + 1);

            return ((float)matchesInTarg / numPxTarg >= DetectThreshold);
        }

        public YCbCrColor GetAvgColor(Color[] data, int targ_cx, int targ_cy, int targ_halfWidth)
        {
            float CbAvgD = 0;
            float CrAvgD = 0;
            float YAvgD = 0;

            for (int y = targ_cy - targ_halfWidth; y <= targ_cy + targ_halfWidth; y++)
                for (int x = targ_cx - targ_halfWidth; x <= targ_cx + targ_halfWidth; x++)
                {
                    int i = y * w + x;

                    Color c = data[i];

                    byte _y, _cb, _cr;
                    ImageUtils.RGB_to_YCbCr(c.R, c.G, c.B, out _y, out _cb, out _cr);
                    CbAvgD += _cb;
                    CrAvgD += _cr;
                    YAvgD += _y;
                }

            int numPx = (2 * targ_halfWidth + 1) * (2 * targ_halfWidth + 1);
            CbAvgD /= numPx;
            CrAvgD /= numPx;
            YAvgD /= numPx;


            return new YCbCrColor(YAvgD, CbAvgD, CrAvgD);

        }
    }
}
