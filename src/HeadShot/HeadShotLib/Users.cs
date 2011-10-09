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
using System.Xml.Serialization;
using System.Collections.Generic;

namespace HeadShotLib
{
    [XmlRoot("users")]
    public class Users
    {
        [XmlElement("user")]
        public List<User> list;
    }

    [XmlRoot("user")]
    public class User
    {
        [XmlElement("data")]
        public string data;

        [XmlElement("name")]
        public string name;

        [XmlElement("id")]
        public int id;

        [XmlElement("created_at")]
        public DateTime created_at;

        [XmlElement("updated_at")]
        public DateTime updated_at;
    }
}
