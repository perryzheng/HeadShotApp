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
using Microsoft.Devices;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace HeadShotMain
{
    public partial class MainPage : PhoneApplicationPage
    {

        #region cameraDefs
        const int w = 640;
        const int h = 480;
        const int cx = w / 2;
        const int cy = h / 2;

        const int targ_halfWidthINIT = 50;
        int targ_halfWidth = targ_halfWidthINIT;


        CaptureSource captureSource;
        VideoCaptureDevice capcam;
        PhotoCamera phocam;

        FrameVideoSink sink;
        int[] buffer = new int[w * h];
        #endregion cameraDefs

        int myPID = -1;
        PlayerDetector playerDetector;
        bool currentlyDetected = false;
        int detectedPlayerID = -1;

        float zoomLevel = 1.5f;
        const float ZOOM_INCR = 0.5f;
        const float MAX_ZOOM = 3.5f;

        Dictionary<int, string> pushUrls = new Dictionary<int, string>();
        Dictionary<int, string> calibData = new Dictionary<int, string>();

        DispatcherTimer restartTimer;
        bool gameEnabled = true;

        public MainPage()
        {
            InitializeComponent();

            //init the red rectangle in the center
            targetGeom.Rect = new Rect(cx - targ_halfWidth, cy - targ_halfWidth, targ_halfWidth * 2, targ_halfWidth * 2);

            CameraButtons.ShutterKeyHalfPressed += new EventHandler(CameraButtons_ShutterKeyHalfPressed);
            CameraButtons.ShutterKeyPressed += new EventHandler(CameraButtons_ShutterKeyPressed);

            (Application.Current as App).PushHandler.OnNotificationRecieved += new Action<string>(PushHandler_OnNotificationRecieved);


            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Start();


            restartTimer = new DispatcherTimer();
            restartTimer.Tick += new EventHandler(restartTimer_Tick);
            restartTimer.Interval = TimeSpan.FromSeconds(5);

            LoadUsersData();
        }

        void restartTimer_Tick(object sender, EventArgs e)
        {
            gameEnabled = true;
            deadImage.Visibility = System.Windows.Visibility.Collapsed;

            restartTimer.Stop();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            LoadUsersData();
        }

        void PushHandler_OnNotificationRecieved(string obj)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    
                    deadImage.Visibility = System.Windows.Visibility.Visible;
                    gameEnabled = false;
                    restartTimer.Start();

                    //MessageBox.Show("you have been hit!");
                });
        }

        void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            //phocam.Focus();
        }

        void LoadUsersData()
        {
            string url = "http://headshot.heroku.com/users.xml";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }
        public void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
            //System.Diagnostics.Debug.WriteLine(((HttpWebResponse)response).StatusDescription);

            var s = response.GetResponseStream();
            //StreamReader sr = new StreamReader(s);

            try
            {
                XmlReader reader = XmlReader.Create(s);
                Users users = new Users();
                XmlSerializer serializer = new XmlSerializer(typeof(Users));
                users = (Users)serializer.Deserialize(reader);

                calibData.Clear();
                foreach (User u in users.list)
                {
                    try
                    {
                        int pID = Int32.Parse(u.name);
                        var tokens = u.data.Split('$');
                        if (tokens.Length == 2)
                        {
                            calibData[pID] = tokens[0];
                            pushUrls[pID] = tokens[1];
                        }
                    }
                    catch
                    {
                    }
                }
                playerDetector.LoadPlayers(calibData);
            }
            catch
            {
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            myPID = int.Parse(NavigationContext.QueryString["myPID"]);

            //NET: LOAD up players form server like this:
            //loaded in timer tick
            //LoadUsersData();



            base.OnNavigatedTo(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            loadCamera();

            playerDetector = new PlayerDetector(myPID, w, h);
        }
        void loadCamera()
        {
            capcam = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice();
            capcam.DesiredFormat = capcam.SupportedFormats.First(f => f.PixelWidth == w);

            captureSource = new CaptureSource();
            captureSource.VideoCaptureDevice = capcam;
            if (captureSource.State != CaptureState.Started)
                captureSource.Start();

            sink = new FrameVideoSink();
            sink.CaptureSource = captureSource;
            sink.OnFrameReady += sink_OnFrameReady;
            sink.Enabled = true;


            viewfinderBrush.SetSource(captureSource);
            viewfinderBrush.Stretch = Stretch.Fill;

            phocam = new PhotoCamera();
        }

        void setTargetColor(bool detected)
        {
            Color c = detected ? Color.FromArgb(255, 255, 0, 0) : Color.FromArgb(255, 0, 255, 0);
            Dispatcher.BeginInvoke(() =>
            {
                targetPath.Stroke = new SolidColorBrush(c);

                if (detected)
                {
                    new_red_crosshair.Visibility = System.Windows.Visibility.Visible;
                    new_green_crosshair.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {

                    new_red_crosshair.Visibility = System.Windows.Visibility.Collapsed;
                    new_green_crosshair.Visibility = System.Windows.Visibility.Visible;

                }
            });
        }
        void sink_OnFrameReady(Color[] data)
        {
            currentlyDetected = playerDetector.Detect(data, cx, cy, targ_halfWidth, out detectedPlayerID);
            setTargetColor(currentlyDetected);
        }

        void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            if (gameEnabled && currentlyDetected)
            {
                //NET: fire a shot at detectedPlayerID
                var pusher = (Application.Current as App).PushHandler;

                if (pushUrls.ContainsKey(detectedPlayerID))
                    pusher.SendShot(pushUrls[detectedPlayerID]);
            }
        }


        void updateZoom()
        {
            targ_halfWidth = (int)(targ_halfWidthINIT / zoomLevel);
            viewTransform.ScaleX = zoomLevel;
            viewTransform.ScaleY = zoomLevel;
            slider.Value = (float)zoomLevel;
        }
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (zoomLevel + ZOOM_INCR <= MAX_ZOOM)
                zoomLevel += ZOOM_INCR;

            updateZoom();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (zoomLevel - ZOOM_INCR >= 1)
                zoomLevel -= ZOOM_INCR;

            updateZoom();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            zoomLevel = (float)e.NewValue;

            if (viewTransform != null)
                updateZoom();
        }
    }
}