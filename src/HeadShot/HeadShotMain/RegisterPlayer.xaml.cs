using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using HeadShotLib;
using System.Windows.Media.Imaging;
using System.IO;

namespace HeadShotMain
{
    public partial class RegisterPlayer : PhoneApplicationPage
    {
        const int w = 640;
        const int h = 480;
        const int cx = w / 2;
        const int cy = h / 2;
        const int targ_halfWidth = 50;

        CaptureSource captureSource;
        VideoCaptureDevice capcam;

        FrameVideoSink sink;
        WriteableBitmap renderBmp;
        int[] buffer = new int[w * h];


        ColorDetector detector;


        public RegisterPlayer()
        {
            InitializeComponent();

            targetGeom.Rect = new Rect(cx - targ_halfWidth, cy - targ_halfWidth, targ_halfWidth * 2, targ_halfWidth * 2);
            this.doneButton.IsEnabled = false;
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.textDescription.Text = "Here is where you calibrate the Color of your T-shirt, \n";


            capcam = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();
            capcam.DesiredFormat = capcam.SupportedFormats.First(f => f.PixelWidth == w);

            captureSource = new CaptureSource();
            captureSource.VideoCaptureDevice = capcam;
            captureSource.Start();

            sink = new FrameVideoSink();
            sink.CaptureSource = captureSource;
            sink.OnFrameReady += sink_OnFrameReady;
            sink.Enabled = true;


            viewfinderBrush.SetSource(captureSource);
            viewfinderBrush.Stretch = Stretch.Fill;

            renderBmp = new WriteableBitmap(w, h);
            renderBrush.ImageSource = renderBmp;

            detector = new ColorDetector(w, h);
        }

        protected override void  OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            captureSource.Stop();

            base.OnNavigatedTo(e);
        }

        void setTargetColor(bool detected)
        {
            Color c = detected ? Color.FromArgb(255, 255, 0, 0) : Color.FromArgb(255, 0, 255, 0);

            Dispatcher.BeginInvoke(() => targetPath.Stroke = new SolidColorBrush(c));
        }

        void sink_OnFrameReady(Color[] data)
        {
            if (detector.IsCalibrated)
            {
                Color[] renderData;
                bool detected = detector.DetectWithTransform(data, cx, cy, targ_halfWidth, out renderData);

                setTargetColor(detected);

                ImageUtils.Pack(renderData, ref buffer);

                Dispatcher.BeginInvoke(() =>
                {
                    buffer.CopyTo(renderBmp.Pixels, 0);
                    renderBmp.Invalidate();
                });

                /*
                bool detected = detector.DetectThresh(data, cx, cy, targ_halfWidth);
                setTargetColor(detected);
                */
            }
        }

        private void calibButton_Click(object sender, RoutedEventArgs e)
        {
            if (sink.CurrentFrameData == null)
                return;

            detector.Calibrate(sink.CurrentFrameData, cx, cy, targ_halfWidth);

            Color c = new Color();
            byte R, G, B;
            ImageUtils.YCbCr_to_RGB((byte)detector.YAvg, (byte)detector.CbAvg, (byte)detector.CrAvg, out R, out G, out B);
            c.A = 255;
            c.R = R;
            c.G = G;
            c.B = B;

            /* HslColor hsl = new HslColor();
             hsl.A = 1;
             hsl.H = detector.HAvg;
             hsl.S = detector.SAvg;
             hsl.L = detector.LAvg;
             Color c = hsl.ToColor();*/

            calibColor.Fill = new SolidColorBrush(c);

            renderDisplay.Visibility = System.Windows.Visibility.Visible;

            this.doneButton.IsEnabled = true;
        }

        private void doneButton_Click(object sender, RoutedEventArgs e)
        {
            int myPID = (App.Current as App).DeviceHash;
            string calibString = detector.GetCalibrationYCbCrColor().SaveToString();
            RegisterPlayerOnServer(myPID, calibString);

            NavigationService.Navigate(new Uri("/MainPage.xaml?myPID=" + myPID.ToString(), UriKind.Relative));
        }

        public void RegisterPlayerOnServer(int myPID, string calibString)
        {
            try
            {
                string pushUrl = (Application.Current as App).PushHandler.ChannelURI;

                string data = calibString + "$" + pushUrl;
                string url = string.Format("http://headshot.heroku.com/users?user[name]={0}&user[data]={1}", myPID, data);
                //Register user 
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";

                request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Posting new player to server failed.");
            }
        }

        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                Stream postStream = request.EndGetRequestStream(asynchronousResult);
                postStream.Close();

                request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("GetRequestStreamCallback from server failed.");
            }
        }

        public void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                System.Diagnostics.Debug.WriteLine(((HttpWebResponse)response).StatusDescription);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("GetResponseCallback from server failed.");
            }
        }
    }
}