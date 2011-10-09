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
    public static class ImageUtils
    {

        //conversion formulas from http://www.intersil.com/data/an/an9717.pdf

        public static void RGB_to_YCbCr(byte R, byte G, byte B, out byte Y, out byte Cb, out byte Cr)
        {
            Y = (byte)(0.257f * R + 0.504f * G + 0.098f * B + 16f).Clamp(0f, 255f);
            Cb = (byte)(-0.148f * R - 0.291f * G + 0.439f * B + 128f).Clamp(0f, 255f);
            Cr = (byte)(0.439f * R - 0.368f * G - 0.071f * B + 128f).Clamp(0f, 255f);
        }

        public static void YCbCr_to_RGB(byte Y, byte Cb, byte Cr, out byte R, out byte G, out byte B)
        {
            R = (byte)(1.164f * (Y - 16f) + 1.596f * (Cr - 128f)).Clamp(0f, 255f);
            G = (byte)(1.164f * (Y - 16f) - 0.813f * (Cr - 128f) - 0.392f * (Cb - 128f)).Clamp(0f, 255f);
            B = (byte)(1.164f * (Y - 16f) + 2.017f * (Cb - 128f)).Clamp(0f, 255f);
        }




        public static float Clamp(this float x, float min, float max)
        {
            return Math.Max(Math.Min(x, max), min);
        }


        public static void Pack(Color[] cdata, ref int[] pack)
        {
            //convert the color data to packed int
            for (int i = 0; i < pack.Length; i++)
            {
                Color c = cdata[i];
                int packed = c.A << 24 | c.R << 16 | c.G << 8 | c.B; //pack the ARGB bytes into an Int32
                pack[i] = packed;
            }
        }


    }
}
