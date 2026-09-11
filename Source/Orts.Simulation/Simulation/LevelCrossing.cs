// COPYRIGHT 2012, 2013 by the Open Rails project.
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
//
// This file is the responsibility of the 3D & Environment Team.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ORTS.Common;
using Orts.Formats.Msts;
using Orts.Simulation.Simulation.AIs;
using Orts.Simulation.Simulation.Physics;
using Orts.Simulation.Simulation.RollingStocks;

namespace Orts.Simulation.Simulation
{
    public class LevelCrossings
    {
        const float MaximumActivationDistance = 2000;

        readonly Simulator Simulator;
        public readonly Dictionary<int, LevelCrossingItem> TrackCrossingItems;
        public readonly Dictionary<int, LevelCrossingItem> RoadCrossingItems;
        public readonly Dictionary<LevelCrossingItem, LevelCrossingItem> RoadToTrackCrossingItems = new Dictionary<LevelCrossingItem, LevelCrossingItem>();

        public object Program { get; private set; }

        public LevelCrossings(Simulator simulator)
        {
            Simulator = simulator;
            TrackCrossingItems = simulator.TDB != null && simulator.TDB.TrackDB != null && simulator.TDB.TrackDB.TrackNodes != null && simulator.TDB.TrackDB.TrItemTable != null
                ? GetLevelCrossingsFromDB(simulator.TDB.TrackDB.TrackNodes, simulator.TDB.TrackDB.TrItemTable) : new Dictionary<int, LevelCrossingItem>();
            RoadCrossingItems = simulator.RDB != null && simulator.RDB.RoadTrackDB != null && simulator.RDB.RoadTrackDB.TrackNodes != null && simulator.RDB.RoadTrackDB.TrItemTable != null
                ? GetLevelCrossingsFromDB(simulator.RDB.RoadTrackDB.TrackNodes, simulator.RDB.RoadTrackDB.TrItemTable) : new Dictionary<int, LevelCrossingItem>();
        }

        static Dictionary<int, LevelCrossingItem> GetLevelCrossingsFromDB(TrackNode[] trackNodes, TrItem[] trItemTable)
        {
            return (from trackNode in trackNodes
                    where trackNode != null && trackNode.TrVectorNode != null && trackNode.TrVectorNode.NoItemRefs > 0
                    from itemRef in trackNode.TrVectorNode.TrItemRefs.Distinct()
                    where trItemTable[itemRef] != null && trItemTable[itemRef].ItemType == TrItem.trItemType.trXING
                    select new KeyValuePair<int, LevelCrossingItem>(itemRef, new LevelCrossingItem(trackNode, trItemTable[itemRef])))
                    .ToDictionary(_ => _.Key, _ => _.Value);
        }

        /// <summary>
        /// Creates a level crossing from its track and road component IDs.
        /// </summary>
        /// <param name="position">Position of the level crossing object for error reporting.</param>
        /// <param name="trackIDs">List of TrItem IDs (from the track database) for the track crossing items.</param>
        /// <param name="roadIDs">List of TrItem IDs (from the road database) for the road crossing items.</param>
        /// <param name="warningTime">Time that gates should be closed prior to a train arriving (seconds).</param>
        /// <param name="minimumDistance">Minimum distance from the gates that a train is allowed to stop and have the gates open (meters).</param>
        /// <returns>The level crossing object comprising of the specified track and road items plus warning and distance configuration.</returns>
        public LevelCrossing CreateLevelCrossing(WorldPosition position, IEnumerable<int> trackIDs, IEnumerable<int> roadIDs, float warningTime, float minimumDistance, int crashProbability)
        {
            var trackItems = trackIDs.Select(id => TrackCrossingItems[id]).ToArray();
            var roadItems = roadIDs.Select(id => RoadCrossingItems[id]).ToArray();
            if (trackItems.Length != roadItems.Length)
                Trace.TraceWarning("{0} level crossing contains {1} rail and {2} road items; expected them to match.", position, trackItems.Length, roadItems.Length);
            if (trackItems.Length >= roadItems.Length)
                for (var i = 0; i < roadItems.Length; i++)
                    if (!RoadToTrackCrossingItems.ContainsKey(roadItems[i]))
                        RoadToTrackCrossingItems.Add(roadItems[i], trackItems[i]);

            return new LevelCrossing(trackItems.Union(roadItems), warningTime, minimumDistance, crashProbability);
        }

        [CallOnThread("Updater")]
        public void Update(float elapsedClockSeconds)
        {
            foreach (var train in Simulator.Trains)
                UpdateCrossings(train, elapsedClockSeconds);

            // NEW: after HasTrain lists were updated, update any per-crossing state machines (e.g. CZ logic).
            foreach (var crossing in TrackCrossingItems.Values
                         .Where(ci => ci.CrossingGroup != null)
                         .Select(ci => ci.CrossingGroup)
                         .Distinct())
            {
                crossing.Update(elapsedClockSeconds);
            }
        }

        [CallOnThread("Updater")]
        void UpdateCrossings(Train train, float elapsedTime)
        {
            var speedMpS = train.SpeedMpS;
            var absSpeedMpS = Math.Abs(speedMpS);
            var maxSpeedMpS = train.AllowedMaxSpeedMpS;
            var minCrossingActivationSpeed = 5.0f;  //5.0MpS is equalivalent to 11.1mph.  This is the estimated min speed that MSTS uses to activate the gates when in range.

            bool validTrain = false;
            bool validStaticConsist = false;

            // We only care about crossing items which are:
            //   a) Grouped properly.
            //   b) Within the maximum activation distance of front/rear of the train.
            // Separate tests are performed for present speed and for possible maximum speed to avoid anomolies if train accelerates.
            // Special test is also done to check on section availability to avoid closure beyond signal at danger.
            int crossingNr = 0;
            foreach (var crossing in TrackCrossingItems.Values.Where(ci => ci.CrossingGroup != null))
            {
                crossingNr++;
                bool UnprotectedLevelCross = crossing.CrossingGroup.CrashProbability > 0f ? true : false;
                bool UnprotectedLevelCross1 = crossing.CrossingGroup.CrashProbability == 1f ? true : false;
                bool UnprotectedLevelCross2 = crossing.CrossingGroup.CrashProbability >= 2f ? true : false;

                var predictedDist = crossing.CrossingGroup.WarningTime * absSpeedMpS;
                var maxPredictedDist = crossing.CrossingGroup.WarningTime * (maxSpeedMpS - absSpeedMpS) / 2; // added distance if train accelerates to maxspeed
                var minimumDist = crossing.CrossingGroup.MinimumDistance;
                var totalDist = predictedDist + minimumDist + 1;
                var totalMaxDist = predictedDist + maxPredictedDist + minimumDist + 1;

                var reqDist = 0f; // actual used distance
                var adjustDist = 0f;

                // Validate STATIC consists near the crossing.
                if (train.TrainType == Train.TRAINTYPE.STATIC)
                {
                    if (!WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, (minimumDist + (train.Length / 2))) && !WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, (minimumDist + (train.Length / 2))))
                        continue;
                    if (WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, (minimumDist + (train.Length / 2))) || WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, (minimumDist + (train.Length / 2))))
                    {
                        foreach (var scar in train.Cars)
                        {
                            if (WorldLocation.Within(crossing.Location, scar.WorldPosition.WorldLocation, minimumDist))
                                validStaticConsist = true;
                        }
                    }
                }

                if ((train.TrainType != Train.TRAINTYPE.STATIC) && WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, totalDist) || WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, totalDist))
                {
                    validTrain = true;
                    reqDist = totalDist;

                    foreach (TrainCar Car in train.Cars)
                    {
                        if (WorldLocation.Within(crossing.Location, Car.WorldPosition.WorldLocation, 5))
                            Car.CarIsOnLvlCrossover = true; // Vůz je na přejezdu
                        else
                            Car.CarIsOnLvlCrossover = false;
                    }

                    // Hráč
                    if (train.IsActualPlayerTrain && !train.Simulator.PlayerTrainInAutopilotMode)
                    {
                        if (UnprotectedLevelCross)
                        {
                            train.TrainIsNearToLvlCross = true;
                            train.UnprotectedLevelCross1[crossingNr] = UnprotectedLevelCross1;
                            train.UnprotectedLevelCross2[crossingNr] = UnprotectedLevelCross2;
                            if (train.UnprotectedLevelCrossWarningDistance[crossingNr] == 0) train.UnprotectedLevelCrossWarningDistance[crossingNr] = totalDist;

                            float frontDistance = crossing.DistanceTo(train.FrontTDBTraveller, train.UnprotectedLevelCrossWarningDistance[crossingNr]);
                            if (!train.AITrainDirectionForward)
                            {
                                frontDistance = -crossing.DistanceTo(new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward), train.UnprotectedLevelCrossWarningDistance[crossingNr] + train.Length);
                                var rearDistance = -frontDistance - train.Length;
                                if (rearDistance > 0 && Math.Abs(train.SpeedMpS) > 0)
                                    train.UnprotectedLevelCrossWarningCanEnable[crossingNr] = true;
                                else
                                {
                                    train.UnprotectedLevelCrossWarningDistance[crossingNr] = 0;
                                    train.UnprotectedLevelCrossWarningCanEnable[crossingNr] = false;
                                }
                            }
                            else
                            {
                                if (frontDistance > 0 && Math.Abs(train.SpeedMpS) > 0)
                                    train.UnprotectedLevelCrossWarningCanEnable[crossingNr] = true;
                                else
                                {
                                    train.UnprotectedLevelCrossWarningDistance[crossingNr] = 0;
                                    train.UnprotectedLevelCrossWarningCanEnable[crossingNr] = false;
                                }
                            }
                        }
                    }

                    // AI
                    if (train is AITrain && (!(train as AITrain).IsActualPlayerTrain || ((train as AITrain).IsActualPlayerTrain && train.Simulator.PlayerTrainInAutopilotMode)))
                    {
                        var AItrain = train as AITrain;

                        // AI LvlCr Setup
                        if (!AItrain.AIUnprotectedLevelCrossSetup[crossingNr] && !AItrain.AIUnprotectedLevelCrossWarningRunning)
                        {
                            if (UnprotectedLevelCross1)
                            {
                                AItrain.AIUnprotectedLevelCrossWarningDistance[crossingNr] = 150;
                                AItrain.AIUnprotectedLevelCrossTime[crossingNr] = Simulator.Random.Next(1, 2);
                                AItrain.AIUnprotectedLevelCrossWarningCount[crossingNr] = Simulator.Random.Next(1, 2);
                                AItrain.AIUnprotectedLevelCrossWarningType[crossingNr] = (int)Math.Round(Simulator.Random.Next(14, 20) / 10f, 0); // 1 - Horn, 2 - Bell
                                AItrain.AIUnprotectedLevelCrossSetup[crossingNr] = true;
                            }
                            if (UnprotectedLevelCross2)
                            {
                                AItrain.AIUnprotectedLevelCrossWarningDistance[crossingNr] = 350;
                                AItrain.AIUnprotectedLevelCrossTime[crossingNr] = Simulator.Random.Next(1, 3);
                                AItrain.AIUnprotectedLevelCrossWarningCount[crossingNr] = Simulator.Random.Next(2, 4);
                                AItrain.AIUnprotectedLevelCrossWarningType[crossingNr] = Simulator.Random.Next(1, 3); // 1 - Horn, 2 - Bell
                                AItrain.AIUnprotectedLevelCrossSetup[crossingNr] = true;
                            }
                        }

                        // Startovní pravidlo
                        if (UnprotectedLevelCross)
                        {
                            float frontDistance = crossing.DistanceTo(AItrain.FrontTDBTraveller, AItrain.AIUnprotectedLevelCrossWarningDistance[crossingNr]);
                            if (!AItrain.AITrainDirectionForward)
                            {
                                frontDistance = -crossing.DistanceTo(new Traveller(AItrain.FrontTDBTraveller, Traveller.TravellerDirection.Backward), AItrain.AIUnprotectedLevelCrossWarningDistance[crossingNr] + AItrain.Length);
                                var rearDistance = -frontDistance - AItrain.Length;
                                if (rearDistance > 0 && Math.Abs(AItrain.SpeedMpS) > 0 && rearDistance < AItrain.AIUnprotectedLevelCrossWarningDistance[crossingNr])
                                    AItrain.AIUnprotectedLevelCrossWarningCanEnable[crossingNr] = true;
                                else
                                {
                                    AItrain.AIUnprotectedLevelCrossWarningCanEnable[crossingNr] = false;
                                    if (frontDistance > 0)
                                    {
                                        AItrain.AIUnprotectedLevelCrossSetup[crossingNr] = false;
                                        AItrain.AIUnprotectedLevelCrossWarningRunning = false;
                                    }
                                }
                            }
                            else
                            {
                                float rearDistance = crossing.DistanceTo(AItrain.RearTDBTraveller, AItrain.AIUnprotectedLevelCrossWarningDistance[crossingNr]);
                                if (frontDistance > 0 && Math.Abs(AItrain.SpeedMpS) > 0 && frontDistance < AItrain.AIUnprotectedLevelCrossWarningDistance[crossingNr])
                                    AItrain.AIUnprotectedLevelCrossWarningCanEnable[crossingNr] = true;
                                else
                                {
                                    AItrain.AIUnprotectedLevelCrossWarningCanEnable[crossingNr] = false;
                                    if (rearDistance < 0)
                                    {
                                        AItrain.AIUnprotectedLevelCrossSetup[crossingNr] = false;
                                        AItrain.AIUnprotectedLevelCrossWarningRunning = false;
                                    }
                                }
                            }
                        }

                        if (AItrain.AIUnprotectedLevelCrossWarningRunning || AItrain.AIUnprotectedLevelCrossWarningCanEnable[crossingNr])
                        {
                            AItrain.AIUnprotectedLevelCrossWarningRunning = true;
                            if (AItrain.AIUnprotectedLevelCrossWarningCount[crossingNr] > 1 && WorldLocation.Within(crossing.Location, AItrain.FrontTDBTraveller.WorldLocation, 100))
                            {
                                if (AItrain.AIUnprotectedLevelCrossHornOn[crossingNr])
                                    AItrain.AIUnprotectedLevelCrossWarningCount[crossingNr] = 1;
                                else
                                    AItrain.AIUnprotectedLevelCrossWarningCount[crossingNr] = 0;
                            }

                            if (AItrain.AIUnprotectedLevelCrossWarningCount[crossingNr] > 0)
                            {
                                AItrain.AIUnprotectedLevelCrossTimer1[crossingNr] += elapsedTime;
                                if (AItrain.AIUnprotectedLevelCrossTimer1[crossingNr] < AItrain.AIUnprotectedLevelCrossTime[crossingNr])
                                {
                                    if (!AItrain.AIUnprotectedLevelCrossHornOn[crossingNr])
                                    {
                                        foreach (var car in AItrain.Cars)
                                        {
                                            if (car is MSTSLocomotive)
                                            {
                                                if (AItrain.AIUnprotectedLevelCrossWarningType[crossingNr] == 1)
                                                {
                                                    if (car.TriggerHornNumber == 1)
                                                        car.SignalEvent(Common.Event.HornOn);
                                                    if (car.TriggerHornNumber == 2)
                                                        car.SignalEvent(Common.Event.Horn2On);
                                                    if (car.TriggerHornNumber == 3)
                                                        car.SignalEvent(Common.Event.HornOn);
                                                }
                                                if (AItrain.AIUnprotectedLevelCrossWarningType[crossingNr] == 2)
                                                    car.SignalEvent(Common.Event.BellOn);
                                                AItrain.AIUnprotectedLevelCrossHornOn[crossingNr] = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (AItrain.AIUnprotectedLevelCrossHornOn[crossingNr])
                                    {
                                        foreach (var car in AItrain.Cars)
                                        {
                                            if (car is MSTSLocomotive)
                                            {
                                                car.SignalEvent(Common.Event.HornOff);
                                                car.SignalEvent(Common.Event.Horn2Off);
                                                car.SignalEvent(Common.Event.BellOff);
                                                AItrain.AIUnprotectedLevelCrossHornOn[crossingNr] = false;
                                            }
                                        }
                                    }
                                    AItrain.AIUnprotectedLevelCrossTimer2[crossingNr] += elapsedTime;
                                    if (AItrain.AIUnprotectedLevelCrossTimer2[crossingNr] > AItrain.AIUnprotectedLevelCrossTime[crossingNr] + 5)
                                    {
                                        AItrain.AIUnprotectedLevelCrossWarningCount[crossingNr]--;
                                        AItrain.AIUnprotectedLevelCrossTimer1[crossingNr] = 0;
                                        AItrain.AIUnprotectedLevelCrossTimer2[crossingNr] = 0;
                                        if (UnprotectedLevelCross1)
                                        {
                                            AItrain.AIUnprotectedLevelCrossTime[crossingNr] = Simulator.Random.Next(1, 2);
                                            AItrain.AIUnprotectedLevelCrossWarningType[crossingNr] = (int)Math.Round(Simulator.Random.Next(14, 20) / 10f, 0); // 1 - Horn, 2 - Bell
                                        }
                                        if (UnprotectedLevelCross2)
                                        {
                                            AItrain.AIUnprotectedLevelCrossTime[crossingNr] = Simulator.Random.Next(1, 3);
                                            AItrain.AIUnprotectedLevelCrossWarningType[crossingNr] = Simulator.Random.Next(1, 3); // 1 - Horn, 2 - Bell
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if ((train.TrainType != Train.TRAINTYPE.STATIC) && WorldLocation.Within(crossing.Location, train.FrontTDBTraveller.WorldLocation, totalMaxDist) || WorldLocation.Within(crossing.Location, train.RearTDBTraveller.WorldLocation, totalMaxDist))
                {
                    validTrain = true;
                    reqDist = totalMaxDist;
                }

                if ((train.TrainType == Train.TRAINTYPE.STATIC) && !validStaticConsist && !crossing.StaticConsists.Contains(train))
                {
                    continue;
                }

                if ((train.TrainType != Train.TRAINTYPE.STATIC) && !validTrain && !crossing.Trains.Contains(train))
                {
                    continue;
                }

                // Distances forward from the front and rearwards from the rear.
                var frontDist = crossing.DistanceTo(train.FrontTDBTraveller, reqDist);
                if (frontDist < 0 && train.TrainType != Train.TRAINTYPE.STATIC)
                {
                    frontDist = -crossing.DistanceTo(new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward), reqDist + train.Length);
                    if (frontDist > 0)
                    {
                        // Train cannot find crossing.
                        crossing.RemoveTrain(train);
                        continue;
                    }
                }

                var rearDist = -frontDist - train.Length;

                if (train is AITrain aiTrain && (aiTrain.LevelCrossingHornPattern?.ShouldActivate(crossing.CrossingGroup, absSpeedMpS, Math.Min(frontDist, Math.Abs(rearDist))) ?? false))
                {
                    //  Add generic actions if needed
                    aiTrain.AuxActionsContain.CheckGenActions(this.GetType(), crossing.Location, rearDist, frontDist, crossing.TrackIndex, aiTrain.LevelCrossingHornPattern);
                }

                if (absSpeedMpS > 5f / 3.6f) train.TrainStopAtLevelCrossTimer = 0; // Reset časovače při rychlosti vlaku vyšší než 5 km/h

                // Recognizing static consists at crossings.
                if ((train.TrainType == Train.TRAINTYPE.STATIC) && validStaticConsist)
                {
                    // This process is to raise the crossing gates if a loose consist rolls through the crossing.
                    if (speedMpS > 0)
                    {
                        frontDist = crossing.DistanceTo(train.FrontTDBTraveller, minimumDist);
                        rearDist = crossing.DistanceTo(train.RearTDBTraveller, minimumDist);

                        if (frontDist < 0 && rearDist < 0)
                            crossing.RemoveTrain(train);
                    }
                    //adjustDist is used to allow static cars to be placed closer to the crossing without activation.
                    if (minimumDist >= 20)
                        adjustDist = minimumDist - 13.5f;
                    else if (minimumDist < 20)
                        adjustDist = minimumDist - 6.5f;
                    frontDist = crossing.DistanceTo(train.FrontTDBTraveller, adjustDist);
                    rearDist = crossing.DistanceTo(train.RearTDBTraveller, adjustDist);
                    // Static consist passed the crossing.
                    if (frontDist < 0 && rearDist < 0)
                        rearDist = crossing.DistanceTo(new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward), adjustDist);

                    // Testing distance before crossing
                    if (frontDist > 0 && frontDist <= adjustDist)
                        crossing.AddTrain(train);

                    // Testing to check if consist is straddling the crossing.
                    else if (frontDist < 0 && rearDist > 0)
                        crossing.AddTrain(train);

                    // Odd test
                    else if (frontDist < 0 && rearDist < 0)
                        crossing.AddTrain(train);

                    // Testing distance when past crossing.
                    else if (rearDist <= adjustDist && rearDist > 0)
                        crossing.AddTrain(train);

                    else
                        crossing.RemoveTrain(train);
                }

                // Train is stopped.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.REMOTE) && Math.Abs(speedMpS) <= Simulator.MaxStoppedMpS && frontDist <= reqDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    float DistanceToOpenCrossing = 250f; // Vzdálenost pro otevření přejezdu po zastavení vlaku před přejezdem
                    if (frontDist > DistanceToOpenCrossing && Simulator.Trains.Contains(train))
                    {
                        crossing.RemoveTrain(train);
                    }

                    if (frontDist <= DistanceToOpenCrossing && Simulator.Trains.Contains(train))
                    {
                        crossing.AddTrain(train);
                        if (frontDist > 30f && train.TrainStopAtLevelCrossTimer == 0) train.TrainStopAtLevelCrossTimer += elapsedTime;
                    }

                    float TrainStopAtLevelCrossWaitTime = 3f * 60f;
                    if (frontDist > 30f && train.TrainStopAtLevelCrossTimer > TrainStopAtLevelCrossWaitTime && Simulator.Trains.Contains(train))
                    {
                        crossing.RemoveTrain(train);
                    }
                }

                // Train is travelling toward crossing below 11.1mph.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.STATIC || train.TrainType == Train.TRAINTYPE.REMOTE) && speedMpS > 0 && speedMpS <= minCrossingActivationSpeed && frontDist <= reqDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    if (frontDist <= minimumDist + 65f)
                        crossing.AddTrain(train);
                }

                // Reverse movement under 11.1mph.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER) && speedMpS < 0 && absSpeedMpS <= minCrossingActivationSpeed && rearDist <= reqDist && (train.ReservedTrackLengthM <= 0 || rearDist < train.ReservedTrackLengthM) && frontDist <= minimumDist)
                {
                    if (frontDist > 9.5)
                        crossing.RemoveTrain(train);
                    else if (rearDist <= minimumDist + 65f)
                        crossing.AddTrain(train);
                }

                // Reverse movement above 11.1mph.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.REMOTE) && speedMpS < 0 && absSpeedMpS > minCrossingActivationSpeed && rearDist <= reqDist && (train.ReservedTrackLengthM <= 0 || rearDist < train.ReservedTrackLengthM) && frontDist <= minimumDist)
                {
                    crossing.AddTrain(train);
                }

                // Forward above 11.1mph.
                else if ((train is AITrain || train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.REMOTE) && speedMpS > 0 && speedMpS > minCrossingActivationSpeed && frontDist <= reqDist && (train.ReservedTrackLengthM <= 0 || frontDist < train.ReservedTrackLengthM) && rearDist <= minimumDist)
                {
                    crossing.AddTrain(train);
                }
                else
                {
                    crossing.RemoveTrain(train);
                }
            }

            train.UnprotectedLevelCrossCount = crossingNr;
        }

        public LevelCrossingItem SearchNearLevelCrossing(Train train, float reqDist, bool trainForwards, out float frontDist)
        {
            LevelCrossingItem roadItem = LevelCrossingItem.None;
            frontDist = -1;
            Traveller traveller = trainForwards ? train.FrontTDBTraveller :
                new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
            foreach (var crossing in TrackCrossingItems.Values.Where(ci => ci.CrossingGroup != null))
            {
                if (crossing.Trains.Contains(train))
                {
                    frontDist = crossing.DistanceTo(traveller, reqDist);
                    if (frontDist > 0 && frontDist <= reqDist)
                    {
                        if (RoadToTrackCrossingItems.ContainsValue(crossing))
                        {
                            roadItem = RoadToTrackCrossingItems.FirstOrDefault(x => x.Value == crossing).Key;
                            return roadItem;
                        }
                    }
                }
            }
            return roadItem;
        }
    }

    public class LevelCrossingItem
    {
        readonly TrackNode TrackNode;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        internal List<Train> Trains = new List<Train>();
        internal List<Train> StaticConsists = new List<Train>();
        public readonly WorldLocation Location;
        public LevelCrossing CrossingGroup { get; internal set; }
        public uint TrackIndex { get { return TrackNode.Index; } }

        public LevelCrossing Crossing { get { return CrossingGroup; } }

        public static LevelCrossingItem None = new LevelCrossingItem();

        public LevelCrossingItem(TrackNode trackNode, TrItem trItem)
        {
            TrackNode = trackNode;
            Location = new WorldLocation(trItem.TileX, trItem.TileZ, trItem.X, trItem.Y, trItem.Z);
        }

        public LevelCrossingItem()
        {
        }

        [CallOnThread("Updater")]
        public void AddTrain(Train train)
        {
            if (train.TrainType == Train.TRAINTYPE.STATIC)
            {
                var staticConsists = StaticConsists;
                if (!staticConsists.Contains(train))
                {
                    var newStaticConsists = new List<Train>(staticConsists);
                    newStaticConsists.Add(train);
                    StaticConsists = newStaticConsists;
                }
            }
            else
            {
                var trains = Trains;
                if (!trains.Contains(train))
                {
                    var newTrains = new List<Train>(trains);
                    newTrains.Add(train);
                    Trains = newTrains;
                }
            }
        }

        [CallOnThread("Updater")]
        public void RemoveTrain(Train train)
        {
            var trains = Trains;
            var staticConsists = StaticConsists;
            if (staticConsists.Count > 0)
            {
                if (staticConsists.Contains(train))
                {
                    var newStaticConsists = new List<Train>(staticConsists);
                    newStaticConsists.Remove(train);
                    StaticConsists = newStaticConsists;
                }
                else
                {
                    var newStaticConsists = new List<Train>(staticConsists);
                    for (int i = 0; i < newStaticConsists.Count; i++)
                    {
                        if (newStaticConsists[i].TrainType == Train.TRAINTYPE.STATIC)
                        {
                            newStaticConsists.RemoveAt(i);
                        }
                    }
                    StaticConsists = newStaticConsists;
                }
            }
            else if (trains.Count > 0)
            {
                if (trains.Contains(train))
                {
                    var newTrains = new List<Train>(trains);
                    newTrains.Remove(train);
                    Trains = newTrains;
                }
            }
        }

        public float DistanceTo(Traveller traveller)
        {
            return DistanceTo(traveller, float.MaxValue);
        }

        public float DistanceTo(Traveller traveller, float maxDistance)
        {
            return traveller.DistanceTo(TrackNode, Location.TileX, Location.TileZ, Location.Location.X, Location.Location.Y, Location.Location.Z, maxDistance);
        }
    }

    /// <summary>
    /// LevelCrossing now contains both:
    ///  - original (default) MSTS logic driven by HasTrain
    ///  - optional "CZ" state machine (barrier+lights), deterministic in simulation.
    /// Viewer may ignore CZ fields for non-CZ shapes.
    /// </summary>
    public class LevelCrossing
    {
        internal readonly List<LevelCrossingItem> Items;
        internal readonly float WarningTime;
        internal readonly float MinimumDistance;
        internal readonly int CrashProbability;

        // ===== New optional CZ logic =====================================================

        public enum VisualLogic
        {
            DefaultMsts = 0,
            Czech = 1,
        }

        public enum CzechPhase
        {
            Open = 0,
            Warning = 1,
            Closing = 2,
            Closed = 3,
            Opening = 4,
        }

        public VisualLogic LogicMode { get; private set; } = VisualLogic.DefaultMsts;

        public float CzechWarningDelayS { get; private set; } = 10f;
        public float CzechBarrierSpeed01PerS { get; private set; } = 0.2f;
        public float CzechRedFlashesPerMinute { get; private set; } = 60f;
        public float CzechWhiteFlashesPerMinute { get; private set; } = 40f;

        public CzechPhase CzPhase { get; private set; } = CzechPhase.Open;

        /// <summary>0..1 poloha závory (0=nahoře, 1=dole)</summary>
        public float CzBarrierPos01 { get; private set; } = 0f;

        public bool CzRed1On { get; private set; } = false;
        public bool CzRed2On { get; private set; } = false;
        public bool CzWhiteOn { get; private set; } = true;

        float CzStateTimerS = 0f;
        float CzLightTimerS = 0f;

        public void EnableCzechLogic(
            float warningDelayS = 10f,
            float barrierSpeed01PerS = 0.2f,
            float redFlashesPerMinute = 60f,
            float whiteFlashesPerMinute = 40f)
        {
            LogicMode = VisualLogic.Czech;

            CzechWarningDelayS = Math.Max(0f, warningDelayS);
            CzechBarrierSpeed01PerS = Math.Max(0.001f, barrierSpeed01PerS);
            CzechRedFlashesPerMinute = Math.Max(1f, redFlashesPerMinute);
            CzechWhiteFlashesPerMinute = Math.Max(1f, whiteFlashesPerMinute);

            CzPhase = CzechPhase.Open;
            CzBarrierPos01 = 0f;
            CzStateTimerS = 0f;
            CzLightTimerS = 0f;

            CzRed1On = false;
            CzRed2On = false;
            CzWhiteOn = true;
        }

        [CallOnThread("Updater")]
        public void Update(float elapsedClockSeconds)
        {
            if (LogicMode != VisualLogic.Czech)
                return;

            float dt = elapsedClockSeconds;
            if (dt <= 0) return;

            bool hasTrain = HasTrain;

            switch (CzPhase)
            {
                case CzechPhase.Open:
                    if (hasTrain)
                    {
                        CzPhase = CzechPhase.Warning;
                        CzStateTimerS = 0f;
                    }
                    break;

                case CzechPhase.Warning:
                    if (!hasTrain)
                    {
                        CzPhase = CzechPhase.Open;
                        CzStateTimerS = 0f;
                    }
                    else
                    {
                        CzStateTimerS += dt;
                        if (CzStateTimerS >= CzechWarningDelayS)
                        {
                            CzPhase = CzechPhase.Closing;
                            CzStateTimerS = 0f;
                        }
                    }
                    break;

                case CzechPhase.Closing:
                    CzBarrierPos01 += dt * CzechBarrierSpeed01PerS;
                    if (CzBarrierPos01 >= 1f)
                    {
                        CzBarrierPos01 = 1f;
                        CzPhase = CzechPhase.Closed;
                        CzStateTimerS = 0f;
                    }
                    break;

                case CzechPhase.Closed:
                    if (!hasTrain)
                    {
                        CzPhase = CzechPhase.Opening;
                        CzStateTimerS = 0f;
                    }
                    break;

                case CzechPhase.Opening:
                    CzBarrierPos01 -= dt * CzechBarrierSpeed01PerS;
                    if (CzBarrierPos01 <= 0f)
                    {
                        CzBarrierPos01 = 0f;
                        CzPhase = CzechPhase.Open;
                        CzStateTimerS = 0f;
                    }
                    break;
            }

            CzLightTimerS += dt;

            float redPeriod = 60f / CzechRedFlashesPerMinute;
            float whitePeriod = 60f / CzechWhiteFlashesPerMinute;

            if (CzPhase != CzechPhase.Open)
            {
                float t = CzLightTimerS % redPeriod;
                bool red1 = t < redPeriod * 0.5f;

                CzRed1On = red1;
                CzRed2On = !red1;
                CzWhiteOn = false;
            }
            else
            {
                float t = CzLightTimerS % whitePeriod;
                bool whiteOn = t < whitePeriod * 0.5f;

                CzWhiteOn = whiteOn;
                CzRed1On = false;
                CzRed2On = false;
            }
        }

        // ===============================================================================

        public LevelCrossing(IEnumerable<LevelCrossingItem> items, float warningTime, float minimumDistance, int crashProbability)
        {
            Items = new List<LevelCrossingItem>(items);
            WarningTime = warningTime;
            MinimumDistance = minimumDistance;
            CrashProbability = crashProbability;
            foreach (var item in items)
                item.CrossingGroup = this;
        }

        public bool HasTrain
        {
            get
            {
                bool trains = Items.Any(i => i.Trains.Count > 0);
                bool staticconsists = Items.Any(i => i.StaticConsists.Count > 0);
                return trains || staticconsists;
            }
        }
    }
}
