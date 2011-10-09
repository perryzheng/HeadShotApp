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
    public class FrameVideoSink : VideoSink
    {
        public VideoFormat VideoFormat { get; private set; }
        public bool Enabled = true;


        public Color[] CurrentFrameData { get; private set; }

        /// <summary>
        /// called when a new frame from the camera is ready. allows access to fixed camera buffer.
        /// </summary>
        public event Action<Color[]> OnFrameReady;
        void _OnFrameReady(Color[] data)
        {
            if (OnFrameReady != null)
                OnFrameReady(data);
        }

        public event Action<VideoFormat> OnFormatInit;

        protected override void OnCaptureStarted()
        {
            //
        }
        protected override void OnCaptureStopped()
        {
            //
        }
        protected override void OnFormatChange(VideoFormat videoFormat)
        {
            if (videoFormat.PixelFormat != PixelFormatType.Format32bppArgb) throw new Exception();

            VideoFormat = videoFormat;

            if (OnFormatInit != null)
                OnFormatInit(VideoFormat);
        }
        protected override void OnSample(long sampleTimeInHundredNanoseconds, long frameDurationInHundredNanoseconds, byte[] sampleData)
        {
            if (!Enabled) return;

            
            int w = VideoFormat.PixelWidth;
            int h = VideoFormat.PixelHeight;
            Color[] renderData = new Color[w * h];

            //fix WP7 pixel order: from GBRA -> ARGB
            for (int i = 0; i < w * h; i++)
            {
                byte B = sampleData[i * 4 + 0];
                byte G = sampleData[i * 4 + 1];
                byte R = sampleData[i * 4 + 2];
                byte A = sampleData[i * 4 + 3];


                renderData[i].A = A;
                renderData[i].R = R;
                renderData[i].G = G;
                renderData[i].B = B;
            }

            CurrentFrameData = renderData;

            if (VideoFormat != null)
                _OnFrameReady(renderData);
        }

    }
}
