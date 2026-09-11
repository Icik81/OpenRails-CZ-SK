// COPYRIGHT 2021 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Orts.Simulation.Simulation.RollingStocks;
using static Orts.Simulation.Simulation.Physics.Train;

namespace Orts.Viewer3D.WebServices
{
    /// <summary>
    /// Contains information on the current player train status.
    /// </summary>
    public struct TrainInfo
    {
        /// <summary>
        /// The current control mode of the player train.
        /// </summary>
        /// <remarks>
        /// Value is the string equivalent to a <see cref="TRAIN_CONTROL"/> value.
        /// </remarks>
        public string ControlMode;

        /// <summary>
        /// Current player train speed in km/h.
        /// </summary>
        public float SpeedKmh;

        /// <summary>
        /// Projected player train speed in km/h.
        /// </summary>
        public float ProjectedSpeedKmh;

        /// <summary>
        /// Current speed limit known by OR in km/h.
        /// </summary>
        public float CurrentSpeedLimitKmh;

        /// <summary>
        /// Objects ahead of the player train, taken from OR internal Track Monitor data before rendering.
        /// Contains only signals, speedposts and stations.
        /// </summary>
        public TrainForwardItem[] ForwardItems;

        /// <summary>
        /// Nearest signal ahead of the player train.
        /// </summary>
        public TrainForwardItem NextSignal;

        /// <summary>
        /// Nearest speedpost ahead of the player train.
        /// </summary>
        public TrainForwardItem NextSpeedpost;

        /// <summary>
        /// Nearest station/platform stop ahead of the player train.
        /// </summary>
        public TrainForwardItem NextStation;

        /// <summary>
        /// Nearest voltage change marker around the player train, calculated by OR CZ/SK.
        /// Distance is an aerial/geographical distance in metres, not distance along the track.
        /// </summary>
        public TrainVoltageMarkerInfo VoltageMarker;
    }

    /// <summary>
    /// Clean API representation of nearest voltage marker for DPS.
    /// </summary>
    public class TrainVoltageMarkerInfo
    {
        /// <summary>
        /// Distance from player train to the nearest voltage marker in metres.
        /// This value is calculated by OR CZ/SK as geographical distance.
        /// </summary>
        public float DistanceM;

        /// <summary>
        /// Voltage value of the nearest marker. Example: 0, 3000, 25000.
        /// </summary>
        public int Voltage;
    }

    /// <summary>
    /// Clean API representation of one forward track object for DPS.
    /// </summary>
    public class TrainForwardItem
    {
        /// <summary>
        /// Object type from TrainObjectItem.TRAINOBJECTTYPE.
        /// Expected values for DPS: SIGNAL, SPEEDPOST, STATION.
        /// </summary>
        public string Type;

        /// <summary>
        /// Distance from player train to this object in metres.
        /// </summary>
        public float DistanceM;

        /// <summary>
        /// Speed value in km/h, when available. Otherwise -1.
        /// </summary>
        public float SpeedKmh;

        /// <summary>
        /// Signal aspect from TrackMonitorSignalAspect, when available.
        /// Example: Clear_2, Clear_1, Approach_3, Approach_2, Approach_1, Restricted, StopAndProceed, Stop.
        /// </summary>
        public string SignalState;

        /// <summary>
        /// Authority type, when available.
        /// </summary>
        public string AuthorityType;

        /// <summary>
        /// Station/platform length in metres, when available.
        /// </summary>
        public int StationPlatformLengthM;

        /// <summary>
        /// Speed object type, when available.
        /// Example: Standard, TempRestrictedStart, TempRestrictedResume.
        /// </summary>
        public string SpeedObjectType;

        /// <summary>
        /// Milepost text, when available.
        /// </summary>
        public string Milepost;
    }

    public static class TrainInfoExtensions
    {
        /// <summary>
        /// Get the player train status.
        /// </summary>
        /// <param name="viewer">The Viewer3D instance.</param>
        /// <returns></returns>
        public static TrainInfo GetWebTrainInfo(this Viewer viewer)
        {
            var trainInfo = viewer.PlayerTrain.GetTrainInfo();
            var forwardItems = new List<TrainForwardItem>();

            foreach (var item in trainInfo.ObjectInfoForward)
            {
                if (item.DistanceToTrainM < 0)
                    continue;

                if (item.ItemType != TrainObjectItem.TRAINOBJECTTYPE.SIGNAL &&
                    item.ItemType != TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST &&
                    item.ItemType != TrainObjectItem.TRAINOBJECTTYPE.STATION)
                    continue;

                forwardItems.Add(ToWebForwardItem(item));
            }

            forwardItems = forwardItems
                .OrderBy(item => item.DistanceM)
                .ToList();

            var voltageMarker = GetVoltageMarkerInfo(viewer);

            return new TrainInfo
            {
                ControlMode = Enum.GetName(typeof(TRAIN_CONTROL), trainInfo.ControlMode),
                SpeedKmh = MpS.ToKpH(Math.Abs(trainInfo.speedMpS)),
                ProjectedSpeedKmh = MpS.ToKpH(Math.Abs(trainInfo.projectedSpeedMpS)),
                CurrentSpeedLimitKmh = MpS.ToKpH(trainInfo.allowedSpeedMpS),
                ForwardItems = forwardItems.ToArray(),
                NextSignal = forwardItems.FirstOrDefault(item => item.Type == "SIGNAL"),
                NextSpeedpost = forwardItems.FirstOrDefault(item => item.Type == "SPEEDPOST"),
                NextStation = forwardItems.FirstOrDefault(item => item.Type == "STATION"),
                VoltageMarker = voltageMarker,
            };
        }

        private static TrainVoltageMarkerInfo GetVoltageMarkerInfo(Viewer viewer)
        {
            int voltage = -1;
            var distanceM = viewer.PlayerLocomotive != null && viewer.PlayerLocomotive is MSTSLocomotive ? (viewer.PlayerLocomotive as MSTSLocomotive).DistanceToVoltageMarkerM(out voltage, out var _) : -1;

            return new TrainVoltageMarkerInfo
            {
                DistanceM = distanceM,
                Voltage = voltage,
            };
        }

        private static TrainForwardItem ToWebForwardItem(TrainObjectItem item)
        {
            return new TrainForwardItem
            {
                Type = item.ItemType.ToString(),
                DistanceM = item.DistanceToTrainM,
                SpeedKmh = item.AllowedSpeedMpS > 0 ? MpS.ToKpH(item.AllowedSpeedMpS) : -1,
                SignalState = item.SignalState.ToString(),
                AuthorityType = item.AuthorityType.ToString(),
                StationPlatformLengthM = item.StationPlatformLength,
                SpeedObjectType = item.SpeedObjectType.ToString(),
                Milepost = item.ThisMile,
            };
        }
    }
}
