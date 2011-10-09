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
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using System.IO;

namespace HeadShotLib
{
    public class PushNotificationHandler
    {
        public string ChannelURI { get; set; }

        public event Action<string> OnNotificationRecieved;

        int _myPID;
        public PushNotificationHandler(int myPID)
        {
            _myPID = myPID;

            /// Holds the push channel that is created or found.
            HttpNotificationChannel pushChannel;

            // The name of our push channel.
            string channelName = "RawSampleChannel";

            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(channelName);

            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null)
            {
                pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(PushChannel_HttpNotificationReceived);

                pushChannel.Open();

            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(PushChannel_HttpNotificationReceived);

                // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                MessageBox.Show(String.Format("Channel Uri is {0}",
                    pushChannel.ChannelUri.ToString()));

            }
        }

        /// <summary>
        /// Event handler for when the push channel Uri is updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {

            ChannelURI = e.ChannelUri.ToString();

            /*Dispatcher.BeginInvoke(() =>
            {
                // Display the new URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                //Need to push this to rails server
                //headshot.heroku.com/shots/1
                // make a get request to /shots/new
                System.Diagnostics.Debug.WriteLine(e.ChannelUri.ToString());
                MessageBox.Show(String.Format("Channel Uri is {0}",
                    e.ChannelUri.ToString()));

            });*/
        }

        /// <summary>
        /// Event handler for when a push notification error occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            
        }

        /// <summary>
        /// Event handler for when a raw notification arrives.  For this sample, the raw 
        /// data is simply displayed in a MessageBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PushChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            string message;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(e.Notification.Body))
            {
                message = reader.ReadToEnd();
            }

            if (OnNotificationRecieved != null)
                OnNotificationRecieved(message);
        }


        public void SendShot(string toUrl)
        {
            try
            {
                string url = @"http://headshot.heroku.com/shots?shot[user_id]={0}&shot[content]={1}&shot[push_url]={2}";
                url = string.Format(url, _myPID, "hello", toUrl);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";

                request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallbackShot), request);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Posting new player to server failed.");
            }
        }

        private void GetRequestStreamCallbackShot(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                Stream postStream = request.EndGetRequestStream(asynchronousResult);
                postStream.Close();

                request.BeginGetResponse(new AsyncCallback(GetResponseCallbackShot), request);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("GetRequestStreamCallbackShot from server failed.");
            }
        }

        public void GetResponseCallbackShot(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                System.Diagnostics.Debug.WriteLine(((HttpWebResponse)response).StatusDescription);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("GetResponseCallbackShot from server failed.");
            }
        }
    }
}
