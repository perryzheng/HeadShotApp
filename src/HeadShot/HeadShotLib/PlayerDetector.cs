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
using System.Collections.Generic;
using System.Linq;

namespace HeadShotLib
{
    public class PlayerDetector
    {
        int w, h;
        int _pID;

        Dictionary<int, YCbCrColor> playerCalibs;

        public PlayerDetector(int myPID, int w, int h)
        {
            _pID = myPID;

            this.w = w;
            this.h = h;

            playerCalibs = new Dictionary<int, YCbCrColor>();
        }

        public void LoadPlayers(Dictionary<int, string> usersToDataDict)
        {
            lock (playerCalibs)
            {
                playerCalibs.Clear();

                foreach (int playerID in usersToDataDict.Keys)
                {
                    YCbCrColor calibColor = new YCbCrColor();
                    calibColor.LoadFromString(usersToDataDict[playerID]);
                    LoadPlayer(playerID, calibColor);
                }
            }
        }

        public void LoadPlayer(int pID, YCbCrColor calibColor)
        {
            playerCalibs[pID] = calibColor;
        }

        public bool Detect(Color[] data, int targ_cx, int targ_cy, int targ_halfWidth, out int pID)
        {
            lock (playerCalibs)
            {
                pID = -1;
                ColorDetector detector = new ColorDetector(w, h);
                YCbCrColor avgColor = detector.GetAvgColor(data, targ_cx, targ_cy, targ_halfWidth);

                var canidates = playerCalibs.Where(calib =>
                    {
                        if (calib.Key == _pID) return false;

                        detector.LoadFromCalibration(calib.Value);
                        return detector.DetectThresh(data, targ_cx, targ_cy, targ_halfWidth);
                    }).OrderBy(calib => calib.Value.DistanceSqTo(avgColor));

                if (canidates.Any())
                {
                    pID = canidates.First().Key;
                    return true;
                }
                else
                    return false;
            }
        }
    }
}
