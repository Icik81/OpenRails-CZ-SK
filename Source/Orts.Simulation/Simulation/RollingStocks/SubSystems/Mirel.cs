// COPYRIGHT 2013 - 2018 by the Open Rails project.
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

using System.Data;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public class Mirel
    {
        public Mirel(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
        }
        MSTSLocomotive Locomotive;
        Simulator Simulator;

        public enum Type { Full, LS90 };
        public Type MirelType = Type.Full;
        public bool Equipped = false;
        public bool Test1, Test2, Test3, Test4, Test5, Test6, Test7 = false;
        public float MirelMaximumSpeed = 80;
        public bool RecievingRepeaterSignal = false;
        public enum RecieverState { Off, Signal50, Signal75 };
        public RecieverState recieverState = RecieverState.Signal75;
        public string Display = "";
        public bool BlueLight = false;
        public bool DisplayFlashMask = false;
        public float MirelSpeedNum1 = 0;
        public float MirelSpeedNum2 = 0;
        public float MirelSpeedNum3 = 0;
        public enum OperationalState { Off, Test, Setup, Normal, Restricting };
        public OperationalState operationalState = new OperationalState();
        public enum InitTest { Passed, Running, Off };
        public InitTest initTest = new InitTest();
        public enum MainMode { Default, DriveMode, MaxSpeed };
        public MainMode mainMode = MainMode.Default;
        public enum DriveMode { Off, Shunting, Normal, Lockout, Trailing }
        public DriveMode driveMode = DriveMode.Off;
        public bool DriveModeHideModes = false;
        public bool FullDisplay = false;
        public DriveMode selectedDriveMode = DriveMode.Shunting;
        public bool NZOK = false;
        public bool NZ1 = false;
        public bool NZ2 = false;
        public bool StartReducingSpeed = false;
        public bool ZS1B = false;
        public bool ReducedSpeed = false;
        protected float maxReducedSpeed = 0;
        protected bool ZS1BConfirmed = false;
        protected bool ZS1Bplayed = false;
        protected float NZOKtimer = 0;
        protected bool dieselEngineToState = true;
        protected float batteryRunningTime = 0;
        protected float test3RunningTime = 0;
        protected float test4RunningTime = 0;
        protected float test6RunningTime = 0;
        protected float test7RunningTime = 0;
        protected bool speedIsRestricted = false;
        protected bool engineBrakeApplied = false;
        protected bool engineBrakeReleased = false;
        protected float displayResetTime = 0;
        protected float flashTime = 0;
        protected bool flashing = false;
        protected bool driveModeSetup = false;
        public bool flashFullDisplayInProggress = false;
        protected float flashFullDisplayTimeElapsed = 0;
        protected int flashFullDisplayFlashedTimes = 0;
        public bool MaxSpeedSetup = false;
        public float MaxSelectedSpeed = 80;
        protected float distanceTravelledWithoutInitTest = 0;
        protected bool mirelBeep = false;
        protected bool mirelBeeping = false;
        public enum Confirming { None, POS, PRE, ZAV, VYL, Speed };
        public Confirming confirming = Confirming.None;
        public enum TransmittionSignalFreq { None, Freq50Hz, Freq75Hz };
        public TransmittionSignalFreq transmittionSignalFreq = TransmittionSignalFreq.None;
        protected float confirmingTime = 0;
        protected float repeaterRecievingDelayTime = 0;
        protected bool maxSpeedNeverSet = true;
        protected float selectedApproachSpeed = 40;
        protected bool modelingSpeedCurve = false;
        protected bool anyStationSet = false;
        protected bool stableRecieveCode = false;
        protected float recievingTimer = 0;
        protected int nextSignalId = 0;
        public bool EnableMirelUpdates = false;
        protected int DatabaseVersion = 0;
        protected bool DatabaseVersionUpdated = false;
        public enum LS90power { Off, Start, On };
        public LS90power Ls90power = LS90power.Off;
        public enum LS90led { Off, Red, Green };
        public LS90led Ls90led = LS90led.Off;
        public bool NoAlertOnRestrictedSignal = false;
        public List<MirelSignal> MirelSignals = new List<MirelSignal>();

        public void Initialize()
        {
            Simulator = Locomotive.Simulator;
            initTest = InitTest.Off;
            operationalState = OperationalState.Off;
            FileInfo fi = new FileInfo(Simulator.TRK.Tr_RouteFile.FullFileName);
            if (File.Exists(fi.DirectoryName + "\\MirelDbVersion.ini"))
                DatabaseVersion = int.Parse(File.ReadAllText(fi.DirectoryName + "\\MirelDbVersion.ini"));
            PopulateMirelSignalList();
        }

        public void PopulateMirelSignalList()
        {
            MirelSignals.Clear();
            if (!File.Exists(Simulator.RoutePath + "\\MirelDb.xml"))
                return;
            XmlDocument doc = new XmlDocument();
            doc.Load(Simulator.RoutePath + "\\MirelDb.xml");
            foreach (XmlNode node in doc.ChildNodes)
            {
                if (node.Name == "MirelDb")
                {
                    foreach (XmlNode nodeSignal in node.ChildNodes)
                    {
                        int nextNodeId = 0;
                        string nextNodeValue = "";

                        foreach (XmlNode nodeId in nodeSignal.ChildNodes)
                        {
                            if (nodeId.Name == "Id")
                                nextNodeId = int.Parse(nodeId.InnerText);
                            if (nodeId.Name == "Value")
                                nextNodeValue = nodeId.InnerText;
                        }
                        MirelSignal ms = new MirelSignal();
                        ms.SignalId = nextNodeId;
                        ms.Value = nextNodeValue;
                        MirelSignals.Add(ms);
                    }
                }
            }
        }

        public void SetMirelSignal(bool ToState)
        {
            
            if (!EnableMirelUpdates) return;
            if (!Locomotive.IsPlayerTrain) return;
            if (!DatabaseVersionUpdated)
            {
                cz.aspone.lkpr.WebService ws = new cz.aspone.lkpr.WebService();
                int v = int.Parse(ws.GetLastVersion(Simulator.TRK.Tr_RouteFile.FileName));
                DatabaseVersion = v + 1;
                ws.UpdateMirelVersion(DatabaseVersion, Simulator.TRK.Tr_RouteFile.FileName);
                DatabaseVersionUpdated = true;
            }
            mirelUnsetSignlEventBeeped = false;
            UpdateMirelSignal(ToState ? "b" : "a");
            Simulator.Confirmer.Information("Nové kódování bylo nastaveno a uloženo do externí databáze.");
        }


        XmlDocument MirelXml;
        protected void UpdateMirelSignal(string newFlag)
        {
            if (!EnableMirelUpdates) return;

            if (MirelXml == null)
            {
                MirelXml = new XmlDocument();
                MirelXml.Load(Simulator.RoutePath + "\\MirelDb.xml");
            }

            foreach (XmlNode node in MirelXml.ChildNodes)
            {
                if (node.Name == "MirelDb")
                {
                    foreach (XmlNode nodeSignal in node.ChildNodes)
                    {
                        bool updateNode = false;
                        foreach (XmlNode nodeId in nodeSignal.ChildNodes)
                        {
                            if (nodeId.Name == "Id" && nodeId.InnerText == nextSignalId.ToString())
                            {
                                updateNode = true;
                            }
                            if (nodeId.Name == "Value" && updateNode)
                            {
                                nodeId.InnerText = newFlag;
                                goto Save;
                            }
                        }
                    }
                    XmlNode node1 = MirelXml.CreateElement("Signal");
                    XmlNode node2 = MirelXml.CreateElement("Id");
                    node2.InnerText = nextSignalId.ToString();
                    XmlNode node3 = MirelXml.CreateElement("Value");
                    node3.InnerText = newFlag;
                    node1.AppendChild(node2);
                    node1.AppendChild(node3);
                    node.AppendChild(node1);
                }
            }
        Save:
            MirelXml.Save(Simulator.RoutePath + "\\MirelDb.xml");
            SaveMirelStateToWorld(nextSignalId, newFlag);
            FileInfo fi = new FileInfo(Simulator.TRK.Tr_RouteFile.FullFileName);
            File.WriteAllText(fi.DirectoryName + "\\MirelDbVersion.ini", DatabaseVersion.ToString());
            PopulateMirelSignalList();
        }

        protected void SaveMirelStateToWorld(int SectionID, String NewState)
        {
            cz.aspone.lkpr.WebService ws = new cz.aspone.lkpr.WebService();
            ws.SaveMirelSignal(Simulator.TRK.Tr_RouteFile.Name, SectionID, NewState, DatabaseVersion);
        }

        protected bool mirelUnsetSignlEventBeeped = false;
        protected bool ls90tested = false;
        protected float ls90testTime = 0;
        protected int prevNextSignalId = 0;
        protected RecieverState prevRecieverState = RecieverState.Signal50;
        protected int minimalSignalDistance = 0;
        protected bool noAutoblock = false;
        public void Update(float elapsedClockSeconds, float AbsSpeedMpS, float AbsWheelSpeedMpS)
        {
            UpdateDisplay();
            if (Locomotive.Battery && initTest == InitTest.Off)
            {
                defaultStateSet = false;
                initTest = InitTest.Running;
                if (MirelType == Type.Full)
                    Locomotive.SignalEvent(Common.Event.MirelTestBegin);
                Locomotive.TrainBrakeController.EmergencyBrakingPushButton = false;
            }

            if (!Locomotive.Battery)
            {
                //Locomotive.SetTrainBrakePercent(100);
                ToDefaultState();
                return;
            }

            if (MirelType == Type.LS90)
            {
                if (Ls90power == LS90power.Off)
                {
                    Ls90led = LS90led.Off;
                    ls90tested = false;
                    ls90testTime = 0;
                    BlueLight = false;
                    if (MpS.ToKpH(Locomotive.AbsWheelSpeedMpS) > 1)
                        Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
                    else
                        Locomotive.TrainBrakeController.EmergencyBrakingPushButton = false;
                    return;
                }
                if (Ls90power == LS90power.Start && !ls90tested)
                {
                    if (ls90testTime > 1.5)
                    {
                        Ls90led = LS90led.Green;
                        Locomotive.SignalEvent(Common.Event.LS90TestComplete);
                        ls90tested = true;
                        BlueLight = true;
                        if (MpS.ToKpH(Locomotive.AbsWheelSpeedMpS) > 1)
                            Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
                        else
                            Locomotive.TrainBrakeController.EmergencyBrakingPushButton = false;
                        return;
                    }
                    BlueLight = false;
                    Ls90led = LS90led.Red;
                    ls90testTime += elapsedClockSeconds;
                    return;
                }
                if (Ls90power != LS90power.On)
                {
                    return;
                }
                Test1 = Test2 = Test3 = Test4 = Test5 = Test6 = Test7 = true;
                initTest = InitTest.Passed;
                selectedDriveMode = DriveMode.Normal;
            }

            try
            {
                Physics.Train train = Locomotive.Train;
                Signalling.SignalObject[] signals = train.NextSignalObject;
                if (nextSignalId != signals[0].trItem)
                {
                    mirelUnsetSignlEventBeeped = false;
                    Random rnd = new Random();
                    int randomNum = rnd.Next(0, 3);
                    if (randomNum == 1) RecievingRepeaterSignal = false;
                }
                int nextSignalTrId = nextSignalId = signals[0].trItem;

                var validInfo = Locomotive.Train.GetTrainInfo();
                float? distance = null;
                foreach (Physics.Train.TrainObjectItem thisItem in validInfo.ObjectInfoForward)
                {
                    if (thisItem.ItemType == Physics.Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL)
                    {
                        distance = thisItem.DistanceToTrainM;
                        break;
                    }
                }
                if (distance == null)
                    distance = 2000;

                if (EnableMirelUpdates)
                {
                    bool found = false;
                    if (prevNextSignalId != nextSignalTrId)
                    {
                        prevNextSignalId = nextSignalTrId;
                        Random rnd = new Random();
                        minimalSignalDistance = rnd.Next(1250, 2000);
                        noAutoblock = false;
                        if (distance > 2500)
                            noAutoblock = true;
                    }
                    foreach (MirelSignal ms in MirelSignals)
                    {
                        if (ms.SignalId == nextSignalTrId)
                        {
                            if (ms.Value == "a")
                            {
                                prevRecieverState = recieverState = RecieverState.Off;
                                Simulator.Confirmer.MSG(nextSignalTrId.ToString() + " - kódováni Mirel na příštím návěstidle je VYPNUTO (vzdálenost: " + Math.Round((double)distance, 0).ToString() + "m) -- AutoBlock? " + (noAutoblock ? "false" : "true"));
                                found = true;
                                break;
                            }
                            if (ms.Value == "b")
                            {
                                prevRecieverState = recieverState = RecieverState.Signal50;
                                Simulator.Confirmer.MSG(nextSignalTrId.ToString() + " - kódování Mirel na příštím návěstidle je ZAPNUTO (vzdálenost: " + Math.Round((double)distance, 0).ToString() + "m) -- AutoBlock? " + (noAutoblock ? "false" : "true"));
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        if (prevNextSignalId != nextSignalTrId)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelUnwantedVigilancy);
                            prevNextSignalId = nextSignalTrId;
                        }
                        Simulator.Confirmer.MSG(nextSignalTrId.ToString() + " - kódování Mirel na příštím návěstidle je NENASTAVENO (vzdálenost: " + Math.Round((double)distance, 0).ToString() + "m) -- AutoBlock? " + (noAutoblock ? "false" : "true"));
                    }
                }
                else
                {
                    if (prevNextSignalId != nextSignalTrId)
                    {
                        noAutoblock = false;
                        if (distance > 2500)
                            noAutoblock = true;
                        Random rnd = new Random();
                        minimalSignalDistance = rnd.Next(1250, 2000);
                        foreach (MirelSignal ms in MirelSignals)
                        {
                            if (ms.SignalId == nextSignalTrId)
                            {
                                if (ms.Value == "a")
                                {
                                    prevRecieverState = recieverState = RecieverState.Off;
                                }
                                if (ms.Value == "b")
                                {
                                    prevRecieverState = recieverState = RecieverState.Signal50;
                                }
                                break;
                            }
                        }
                        prevNextSignalId = nextSignalTrId;
                    }
                }
                if (distance > minimalSignalDistance)
                {
                    recieverState = RecieverState.Off;
                }
                else
                {
                    recieverState = prevRecieverState;
                }
                if (noAutoblock)
                    recieverState = RecieverState.Off;
            }
            catch { recieverState = RecieverState.Off; }
            try
            {
                if (recieverState != RecieverState.Off)
                {
                    if (initTest == InitTest.Passed)
                    {
                        if (selectedDriveMode == DriveMode.Normal)
                            transmittionSignalFreq = recieverState == RecieverState.Signal50 ? TransmittionSignalFreq.Freq50Hz : TransmittionSignalFreq.Freq75Hz;
                        else
                            transmittionSignalFreq = TransmittionSignalFreq.None;
                    }
                    else
                        transmittionSignalFreq = TransmittionSignalFreq.None;
                }
                else
                {
                    if (selectedDriveMode == DriveMode.Normal)
                    {
                        transmittionSignalFreq = TransmittionSignalFreq.Freq75Hz;
                        RecievingRepeaterSignal = false;
                    }
                    else
                    {
                        transmittionSignalFreq = TransmittionSignalFreq.None;
                        RecievingRepeaterSignal = false;
                    }
                }
                if (Locomotive.CarID != Locomotive.Train.Cars[0].CarID) return;
                if (!Locomotive.Train.IsPlayerDriven) return;

                if (Locomotive.AbsSpeedMpS == 0 && initTest == InitTest.Passed && MirelType == Type.Full)
                {
                    StartReducingSpeed = true;
                }
                if (Locomotive.AbsSpeedMpS > 0 && !modelingSpeedCurve)
                {
                    StartReducingSpeed = false;
                }

                if (NZOK)
                {
                    VYP = ZAP = false;
                    BlueLight = true;
                    NZOKtimer += elapsedClockSeconds;
                    if (NZOKtimer > 1)
                    {
                        NZOK = false;
                        NZOKtimer = 0;
                    }
                }

                if (initTest != InitTest.Passed)
                {
                    if (AbsSpeedMpS == 0)
                    {
                        distanceTravelledWithoutInitTest = Locomotive.DistanceM;
                        mirelBeep = false;
                        EmergencyBrakes(false);
                        emergency = false;
                    }
                    if ((Locomotive.DistanceM - 5) > distanceTravelledWithoutInitTest)
                    {
                        Locomotive.SignalEvent(Common.Event.MirelOn);
                        mirelBeep = true;
                    }
                    if ((Locomotive.DistanceM - 10) > distanceTravelledWithoutInitTest)
                    {
                        EmergencyBrakes(true);
                    }
                }
                else
                {
                    distanceTravelledWithoutInitTest = Locomotive.DistanceM;
                }

                if (MirelMaximumSpeed > (MpS.ToKpH(Locomotive.MaxSpeedMpS))) MirelMaximumSpeed = MaxSelectedSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);

                if (confirming != Confirming.None)
                {
                    confirmingTime += elapsedClockSeconds;
                    if (confirmingTime > 2.5f)
                    {
                        driveMode = DriveMode.Off;
                        confirming = Confirming.None;
                        DriveModeHideModes = false;
                        return;
                    }
                    DriveModeHideModes = true;
                    switch (confirming)
                    {
                        case Confirming.POS:
                            {
                                UpdateSpeedNumbers(0, true);
                                driveMode = DriveMode.Shunting;
                                break;
                            }
                        case Confirming.PRE:
                            {
                                UpdateSpeedNumbers(0, true);
                                driveMode = DriveMode.Normal;
                                break;
                            }
                        case Confirming.ZAV:
                            {
                                UpdateSpeedNumbers(0, true);
                                driveMode = DriveMode.Trailing;
                                break;
                            }
                        case Confirming.VYL:
                            {
                                UpdateSpeedNumbers(0, true);
                                driveMode = DriveMode.Lockout;
                                break;
                            }
                        case Confirming.Speed:
                            {
                                UpdateSpeedNumbers((int)MaxSelectedSpeed, false);
                                break;
                            }
                    }

                    return;
                }
                confirmingTime = 0;

                if (flashFullDisplayInProggress)
                {
                    flashFullDisplayTimeElapsed += elapsedClockSeconds;
                    if (flashFullDisplayTimeElapsed > 0.1f)
                    {
                        FullDisplay = !FullDisplay;
                        flashFullDisplayTimeElapsed = 0;
                        flashFullDisplayFlashedTimes++;
                    }
                    if (flashFullDisplayFlashedTimes > 1)
                    {
                        flashing = false;
                        flashFullDisplayInProggress = false;
                        FullDisplay = false;
                        flashFullDisplayTimeElapsed = 0;
                        flashFullDisplayFlashedTimes = 0;
                        driveMode = DriveMode.Off;
                        mainMode = MainMode.Default;
                        if (driveModeSetup)
                        {
                            switch (selectedDriveMode)
                            {
                                case DriveMode.Shunting:
                                    {
                                        confirming = Confirming.POS;
                                        if (MirelMaximumSpeed > 40)
                                            MirelMaximumSpeed = 40;
                                        if (MirelMaximumSpeed > MaxSelectedSpeed)
                                            MirelMaximumSpeed = MaxSelectedSpeed;
                                        break;
                                    }
                                case DriveMode.Normal:
                                    {
                                        MirelMaximumSpeed = MaxSelectedSpeed;
                                        confirming = Confirming.PRE;
                                        break;
                                    }
                                case DriveMode.Trailing:
                                    {
                                        confirming = Confirming.ZAV;
                                        break;
                                    }
                                case DriveMode.Lockout:
                                    {
                                        confirming = Confirming.VYL;
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                        }
                        if (MaxSpeedSetup)
                        {
                            confirming = Confirming.Speed;
                            return;
                        }
                    }
                }
                else
                {
                    flashFullDisplayTimeElapsed = 0;
                    FullDisplay = false;
                }

                if (flashing)
                    flashTime += elapsedClockSeconds;
                else
                    flashTime = 0;

                if (mainMode != MainMode.Default)
                {
                    displayResetTime += elapsedClockSeconds;
                }

                if (displayResetTime > 5)
                {
                    ResetDisplay();
                }

                if (initTest == InitTest.Running && batteryRunningTime < 3)
                {
                    batteryRunningTime += elapsedClockSeconds;
                }

                if (batteryRunningTime >= 2)
                {
                    Test1 = true;
                    Test2 = true;
                }

                if (initTest == InitTest.Running && Locomotive.ActiveStation == MSTSLocomotive.DriverStation.Station1)
                {
                    anyStationSet = true;
                }

                if (initTest == InitTest.Running && Locomotive.Direction == Direction.Forward && Locomotive.ActiveStation != MSTSLocomotive.DriverStation.None && !Test3)
                {
                    test3RunningTime += elapsedClockSeconds;
                    if (test3RunningTime > 0.5f)
                        Test3 = true;
                }
                if (initTest == InitTest.Running && Locomotive.Direction == Direction.Reverse && Locomotive.ActiveStation != MSTSLocomotive.DriverStation.None && !Test4)
                {
                    test4RunningTime += elapsedClockSeconds;
                    if (test4RunningTime > 0.5f)
                        Test4 = true;
                }
                if (initTest == InitTest.Running && !Test5 && Locomotive.ActiveStation != MSTSLocomotive.DriverStation.None)
                {
                    if (Locomotive.EngineBrakeController.IntermediateValue == 1)
                        engineBrakeApplied = true;
                    if (Locomotive.EngineBrakeController.IntermediateValue == 0)
                        engineBrakeReleased = true;
                    if (engineBrakeApplied && engineBrakeReleased)
                        Test5 = true;
                }

                if (mainMode == MainMode.Default && MirelType == Type.Full)
                {
                    if (operationalState == OperationalState.Normal || operationalState == OperationalState.Restricting)
                    {
                        if (NZOK) VYP = ZAP = NZ1 = NZ2 = NZ3 = NZ5 = false;

                        if (!NZ1 && !NZ2 && !NZ3 && !NZ5 && !NZOK && !VYP && !ZAP)
                            UpdateSpeedNumbers((int)MirelMaximumSpeed, false);
                        else
                            UpdateSpeedNumbers(0, true);
                    }
                    else
                        UpdateSpeedNumbers(0, true);
                    driveModeSetup = false;
                    MaxSpeedSetup = false;
                }
                else if (MirelType == Type.LS90)
                {
                    CheckNextSignal();
                }

                if (Test1 && Test2 && Test3 && Test4 && Test5 && Test6 && Test7 && initTest != InitTest.Passed)
                {
                    if (Locomotive.AbsSpeedMpS == 0)
                        BlueLight = true;
                    initTest = InitTest.Passed;
                    operationalState = OperationalState.Normal;
                }

                /*            if ((mainMode == MainMode.DriveMode || mainMode == MainMode.MaxSpeed) && !driveModeSetup)
                            {
                                flashing = true;
                            }*/
                if (driveModeSetup || MaxSpeedSetup)
                {
                    flashing = true;
                }
                Flash(2.5f);
                MirelCheck(elapsedClockSeconds);
                if (showingSelectedApproachSpeed)
                {
                    showingSelectedApproachSpeedTime += elapsedClockSeconds;
                    if (showingSelectedApproachSpeedTime > 2)
                    {
                        showingSelectedApproachSpeed = false;
                        firstPress = true;
                        showingSelectedApproachSpeedTime = 0;

                    }
                }
                if (Locomotive.Battery && Locomotive.ActiveStation != MSTSLocomotive.DriverStation.None)
                {
                    TestCMBrakeChannels();
                }

                if (ReducedSpeed)
                {
                    if (MirelMaximumSpeed > maxReducedSpeed)
                        MirelMaximumSpeed = maxReducedSpeed;
                }
                if (ZAP || VYP)
                {
                    reducedSpeedTimer += elapsedClockSeconds;
                    if (reducedSpeedTimer > 5)
                    {
                        ZAP = VYP = false;
                        reducedSpeedTimer = 0;
                    }
                }

                if (initTest == InitTest.Passed) CheckNZ3();
                if (selectedDriveMode != DriveMode.Trailing && initTest == InitTest.Passed) CheckNZ5(elapsedClockSeconds);
                if (performingCMTest)
                {
                    Locomotive.BrakeSystem.BrakeLine1PressurePSI = Locomotive.BrakeSystem.BrakeLine1PressurePSI - 7;
                }
            }
            catch (Exception ec)
            {

            }
        }

        protected void UpdateDisplay()
        {
            if (initTest == InitTest.Off)
                Display = "   ";
            else if (initTest == InitTest.Running)
                Display = "=D1";
            else if (MirelMaximumSpeed > 99)
            {
                Display = Math.Round(MirelMaximumSpeed, 0).ToString();
                Display = Display.Replace("1", "l");
            }
            else
                Display = Math.Round(MirelMaximumSpeed, 0).ToString();
            if (NZ1) Display = "NZ1";
            if (NZ2) Display = "NZ2";
            if (NZ3) Display = "NZ3";
            if (NZ4) Display = "NZ4";
            if (NZ5) Display = "NZ5";
            if (NZOK) Display = "NZk";
            if (ManualModeDisplay) Display = "MAN";
            if (DriveModeHideModes)
            {
                if (driveMode == DriveMode.Shunting)
                    Display = "POS";
                if (driveMode == DriveMode.Normal)
                    Display = "PRE";
                if (driveMode == DriveMode.Lockout)
                    Display = "VYL";
                if (driveMode == DriveMode.Trailing)
                    Display = "ZAV";
            }
            if (!DriveModeHideModes)
            {
                if (mainMode == MainMode.DriveMode)
                    Display = "REZ";
                if (mainMode == MainMode.MaxSpeed)
                    Display = "MAX";
            }
            if (FullDisplay)
            {
                Display = "$$$";
            }
            if (ZAP) Display = "ZAP";
            if (VYP) Display = "VYP";
            if (initTest == InitTest.Passed)
            {
                if (Locomotive.ActiveStation == MSTSLocomotive.DriverStation.None)
                    Display = "ST-";
                if (!Locomotive.UsingRearCab && Locomotive.ActiveStation == MSTSLocomotive.DriverStation.Station2)
                    Display = "ST2";
                if (Locomotive.UsingRearCab && Locomotive.ActiveStation == MSTSLocomotive.DriverStation.Station1)
                    Display = "ST1";
            }
            if (showingSelectedApproachSpeed)
            {
                Display = selectedApproachSpeed.ToString();
            }
            if (Display == "ll0")
                Display = "l10";
            if (Display == "ll1")
                Display = "l11";
            if (Display == "ll2")
                Display = "l12";
            if (Display == "ll3")
                Display = "l13";
            if (Display == "ll4")
                Display = "l14";
            if (Display == "ll5")
                Display = "l15";
            if (Display == "ll6")
                Display = "l16";
            if (Display == "ll7")
                Display = "l17";
            if (Display == "ll8")
                Display = "l18";
            if (Display == "ll9")
                Display = "l19";

        }

        protected bool canChangeSelectedApproachSpeed = true;
        protected float showingSelectedApproachSpeedTime = 0;
        public void Save(BinaryWriter outf)
        {
            outf.Write(this.allowedSpeedInterventing);
            outf.Write(this.batteryRunningTime);
            outf.Write(this.BlueLight);
            outf.Write(this.brakesFilling);
            outf.Write(this.canChangeSelectedApproachSpeed);
            outf.Write(this.cmTestEmergency);
            outf.Write((int)this.confirming);
            outf.Write(this.currentSpeedStep);
            outf.Write(this.confirmingTime);
            outf.Write(this.defaultStateSet);
            outf.Write(this.dieselEngineToState);
            outf.Write(this.DisplayFlashMask);
            outf.Write(this.displayResetTime);
            outf.Write(this.distanceToSpeedChange);
            outf.Write(this.distanceTravelledWithoutInitTest);
            outf.Write((int)this.driveMode);
            outf.Write(this.DriveModeHideModes);
            outf.Write(this.driveModeSetup);
            outf.Write(this.emergency);
            outf.Write(this.engineBrakeApplied);
            outf.Write(this.engineBrakeReleased);
            outf.Write(this.Equipped);
            outf.Write(this.firstPress);
            outf.Write(this.flashFullDisplayFlashedTimes);
            outf.Write(this.flashFullDisplayInProggress);
            outf.Write(this.flashFullDisplayTimeElapsed);
            outf.Write(this.flashing);
            outf.Write(this.flashTime);
            outf.Write(this.FullDisplay);
            outf.Write(this.initialCheckForCMtest);
            outf.Write((int)this.initTest);
            outf.Write(this.interventionTimer);
            outf.Write((int)this.mainMode);
            outf.Write(this.ManualMode);
            outf.Write(this.ManualModeDisplay);
            outf.Write(this.manualModeTime);
            outf.Write(this.maxReducedSpeed);
            outf.Write(this.MaxSelectedSpeed);
            outf.Write(this.maxSpeedNeverSet);
            outf.Write(this.MaxSpeedSetup);
            outf.Write(this.metersWrongDirection);
            outf.Write(this.mirelBeep);
            outf.Write(this.mirelBeeping);
            outf.Write(this.MirelMaximumSpeed);
            outf.Write(this.MirelSpeedNum1);
            outf.Write(this.MirelSpeedNum2);
            outf.Write(this.MirelSpeedNum3);
            outf.Write(this.modelingSpeedCurve);
            outf.Write(this.numCMTested);
            outf.Write(this.NZ1);
            outf.Write(this.NZ2);
            outf.Write(this.NZ3);
            outf.Write(this.NZ4);
            outf.Write(this.NZ5);
            outf.Write(this.NZ5timer);
            outf.Write(this.nz5zs3);
            outf.Write(this.NZOK);
            outf.Write(this.NZOKtimer);
            outf.Write((int)this.operationalState);
            outf.Write(this.previousDistanceToSignal);
            outf.Write(this.previousOdometerM);
            outf.Write((int)this.previousTrackMonitorSignalAspect);
            outf.Write(this.randomRepeaterSignalTime);
            outf.Write((int)this.recieverState);
            outf.Write(this.RecievingRepeaterSignal);
            outf.Write(this.ReducedSpeed);
            outf.Write(this.reducedSpeedTimer);
            outf.Write(this.repeaterRecievingDelayTime);
            outf.Write(this.selectedApproachSpeed);
            outf.Write((int)this.selectedDriveMode);
            outf.Write(this.showingSelectedApproachSpeed);
            outf.Write(this.showingSelectedApproachSpeedTime);
            outf.Write(this.speedIsRestricted);
            outf.Write(this.StartReducingSpeed);
            outf.Write(this.startReducingSpeedLightFlashTime);
            outf.Write(this.stopInterventingUntilNextSignal);
            outf.Write(this.Test1);
            outf.Write(this.Test2);
            outf.Write(this.Test3);
            outf.Write(this.test3RunningTime);
            outf.Write(this.Test4);
            outf.Write(this.test4RunningTime);
            outf.Write(this.Test5);
            outf.Write(this.Test6);
            outf.Write(this.test6RunningTime);
            outf.Write(this.Test7);
            outf.Write(this.test7RunningTime);
            outf.Write(this.timeBeforeIntervetion);
            outf.Write(this.timeInterventing);
            outf.Write((int)this.transmittionSignalFreq);
            outf.Write(this.vigilanceAfterZeroSpeedConfirmed);
            outf.Write(this.VYP);
            outf.Write(this.ZAP);
            outf.Write(this.ZS1B);
            outf.Write(this.ZS1BConfirmed);
            outf.Write(this.ZS1Bplayed);
            outf.Write(this.zs3);
            outf.Write(this.recievingTimer);
            outf.Write((int)Ls90led);
            outf.Write((int)Ls90power);
            outf.Write(ls90tested);
        }

        public void Restore(BinaryReader inf)
        {
            allowedSpeedInterventing = inf.ReadBoolean();
            batteryRunningTime = inf.ReadSingle();
            BlueLight = inf.ReadBoolean();
            brakesFilling = inf.ReadBoolean();
            canChangeSelectedApproachSpeed = inf.ReadBoolean();
            cmTestEmergency = inf.ReadBoolean();
            int fConfirming = inf.ReadInt32();
            confirming = (Confirming)fConfirming;
            currentSpeedStep = inf.ReadInt32();
            confirmingTime = inf.ReadSingle();
            defaultStateSet = inf.ReadBoolean();
            dieselEngineToState = inf.ReadBoolean();
            DisplayFlashMask = inf.ReadBoolean();
            displayResetTime = inf.ReadSingle();
            distanceToSpeedChange = inf.ReadSingle();
            distanceTravelledWithoutInitTest = inf.ReadSingle();
            int fDriveMode = inf.ReadInt32();
            driveMode = (DriveMode)fDriveMode;
            DriveModeHideModes = inf.ReadBoolean();
            driveModeSetup = inf.ReadBoolean();
            emergency = inf.ReadBoolean();
            engineBrakeApplied = inf.ReadBoolean();
            engineBrakeReleased = inf.ReadBoolean();
            Equipped = inf.ReadBoolean();
            firstPress = inf.ReadBoolean();
            flashFullDisplayFlashedTimes = inf.ReadInt32();
            flashFullDisplayInProggress = inf.ReadBoolean();
            flashFullDisplayTimeElapsed = inf.ReadSingle();
            flashing = inf.ReadBoolean();
            flashTime = inf.ReadSingle();
            FullDisplay = inf.ReadBoolean();
            initialCheckForCMtest = inf.ReadBoolean();
            int fInitTest = inf.ReadInt32();
            initTest = (InitTest)fInitTest;
            interventionTimer = inf.ReadSingle();
            int fMainMode = inf.ReadInt32();
            mainMode = (MainMode)fMainMode;
            ManualMode = inf.ReadBoolean();
            ManualModeDisplay = inf.ReadBoolean();
            manualModeTime = inf.ReadSingle();
            maxReducedSpeed = inf.ReadSingle();
            MaxSelectedSpeed = inf.ReadSingle();
            maxSpeedNeverSet = inf.ReadBoolean();
            MaxSpeedSetup = inf.ReadBoolean();
            metersWrongDirection = inf.ReadSingle();
            mirelBeep = inf.ReadBoolean();
            mirelBeeping = inf.ReadBoolean();
            MirelMaximumSpeed = inf.ReadSingle();
            MirelSpeedNum1 = inf.ReadSingle();
            MirelSpeedNum2 = inf.ReadSingle();
            MirelSpeedNum3 = inf.ReadSingle();
            modelingSpeedCurve = inf.ReadBoolean();
            numCMTested = inf.ReadInt32();
            NZ1 = inf.ReadBoolean();
            NZ2 = inf.ReadBoolean();
            NZ3 = inf.ReadBoolean();
            NZ4 = inf.ReadBoolean();
            NZ5 = inf.ReadBoolean();
            NZ5timer = inf.ReadSingle();
            nz5zs3 = inf.ReadBoolean();
            NZOK = inf.ReadBoolean();
            NZOKtimer = inf.ReadSingle();
            int fOperationslState = inf.ReadInt32();
            operationalState = (OperationalState)fOperationslState;
            previousDistanceToSignal = inf.ReadSingle();
            previousOdometerM = inf.ReadSingle();
            int fPreviousTrackMonitorSignalAspect = inf.ReadInt32();
            previousTrackMonitorSignalAspect = (TrackMonitorSignalAspect)fPreviousTrackMonitorSignalAspect;
            randomRepeaterSignalTime = inf.ReadSingle();
            int fRecieverState = inf.ReadInt32();
            recieverState = (RecieverState)fRecieverState;
            RecievingRepeaterSignal = inf.ReadBoolean();
            ReducedSpeed = inf.ReadBoolean();
            reducedSpeedTimer = inf.ReadSingle();
            repeaterRecievingDelayTime = inf.ReadSingle();
            selectedApproachSpeed = inf.ReadSingle();
            int fSelectedDriveMode = inf.ReadInt32();
            selectedDriveMode = (DriveMode)fSelectedDriveMode;
            showingSelectedApproachSpeed = inf.ReadBoolean();
            showingSelectedApproachSpeedTime = inf.ReadSingle();
            speedIsRestricted = inf.ReadBoolean();
            StartReducingSpeed = inf.ReadBoolean();
            startReducingSpeedLightFlashTime = inf.ReadSingle();
            stopInterventingUntilNextSignal = inf.ReadBoolean();
            Test1 = inf.ReadBoolean();
            Test2 = inf.ReadBoolean();
            Test3 = inf.ReadBoolean();
            test3RunningTime = inf.ReadSingle();
            Test4 = inf.ReadBoolean();
            test4RunningTime = inf.ReadSingle();
            Test5 = inf.ReadBoolean();
            Test6 = inf.ReadBoolean();
            test6RunningTime = inf.ReadSingle();
            Test7 = inf.ReadBoolean();
            test7RunningTime = inf.ReadSingle();
            timeBeforeIntervetion = inf.ReadSingle();
            timeInterventing = inf.ReadSingle();
            int fTransmittionSignalFreq = inf.ReadInt32();
            transmittionSignalFreq = (TransmittionSignalFreq)fTransmittionSignalFreq;
            vigilanceAfterZeroSpeedConfirmed = inf.ReadBoolean();
            VYP = inf.ReadBoolean();
            ZAP = inf.ReadBoolean();
            ZS1B = inf.ReadBoolean();
            ZS1BConfirmed = inf.ReadBoolean();
            ZS1Bplayed = inf.ReadBoolean();
            zs3 = inf.ReadBoolean();
            recievingTimer = inf.ReadSingle();
            anyStationSet = true;
            int fLs90led = inf.ReadInt32();
            Ls90led = (LS90led)fLs90led;
            int fLs90power = inf.ReadInt32();
            Ls90power = (LS90power)fLs90power;
            ls90tested = inf.ReadBoolean();
            ls90testTime = 5;
        }

        private bool brakesFilling = false;
        private bool cmTestEmergency = false;
        private int numCMTested = 0;
        private bool initialCheckForCMtest = false;
        private bool performingCMTest = false;
        private void TestCMBrakeChannels()
        {
            if (numCMTested > 1 && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 3)
            {
                Test7 = true;
            }
            if (!initialCheckForCMtest && (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 4.9))
                return;
            else
                initialCheckForCMtest = true;
            if (numCMTested > 1 && brakesFilling)
                return;

            if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 3) // napustíme potrubí
            {
                if (initTest != InitTest.Passed)
                {
                    if (numCMTested == 1) Test6 = true;
                    if (!brakesFilling)
                    {
                        brakesFilling = true;
                        //Locomotive.SetTrainBrakePercent(0.0f);
                        //Locomotive.BrakeSystem.BrakeLine1PressurePSI = 71;
                        performingCMTest = false;
                        Locomotive.SignalEvent(Common.Event.MirelBrakeFillingPipePressureFast);
                        Locomotive.SignalEvent(Common.Event.MirekBrakeStopReleaseFastSound);
                        Locomotive.SignalEvent(Common.Event.MirekBrakeStopFillSound);
                        cmTestEmergency = false;
                    }
                }
            }
            else if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.9 || numCMTested == 0)
            {
                if (initTest != InitTest.Passed)
                {
                    if (!cmTestEmergency)
                    {
                        numCMTested++;
                        brakesFilling = false;
                        performingCMTest = true;
                        Locomotive.SignalEvent(Common.Event.MirelBrakeReleasingPipePressureFast);
                        Locomotive.SignalEvent(Common.Event.MirekBrakeStopFillFastSound);
                        Locomotive.SignalEvent(Common.Event.MirekBrakeStopReleaseSound);
                        cmTestEmergency = true;
                    }
                }
            }

        }

        private void Flash(float FlashSpeedHz)
        {
            if (flashing)
            {
                if (flashTime >= (1 / FlashSpeedHz))
                {
                    DisplayFlashMask = !DisplayFlashMask;
                    flashTime = 0;
                }
            }
            else
                DisplayFlashMask = false;
        }

        protected void FlashFullDisplay()
        {
            flashFullDisplayInProggress = true;
        }

        public bool showingSelectedApproachSpeed = false;
        protected bool firstPress = true;
        public void PlusKeyPressed()
        {
            if (MirelType == Type.LS90)
            {
                if (Ls90power < LS90power.On)
                {
                    Ls90power++;
                    Locomotive.SignalEvent(Common.Event.LightSwitchToggle);
                }
                return;
            }
            if (!Equipped) return;
            if (Locomotive.Battery)
            {
                Locomotive.SignalEvent(Common.Event.KeyboardBeep);
            }
            if (initTest != InitTest.Passed)
                return;
            if (Locomotive.AbsSpeedMpS > 0)
            {
                if (selectedDriveMode != DriveMode.Normal && selectedDriveMode != DriveMode.Trailing)
                    return;

                if (operationalState == OperationalState.Restricting && canChangeSelectedApproachSpeed)
                {
                    float newSpeed = selectedApproachSpeed;
                    showingSelectedApproachSpeed = true;
                    if (selectedApproachSpeed > 40 || MirelMaximumSpeed == 40) firstPress = false;
                    if (firstPress)
                    {
                        firstPress = false;
                        return;
                    }
                    else
                    {
                        showingSelectedApproachSpeed = true;
                        showingSelectedApproachSpeedTime = 0;
                        newSpeed = selectedApproachSpeed + 20;
                    }
                    if (newSpeed > MaxSelectedSpeed)
                        newSpeed = MaxSelectedSpeed;
                    selectedApproachSpeed = newSpeed;
                    if (MirelMaximumSpeed < newSpeed) MirelMaximumSpeed = newSpeed;
                    distanceAndSpeed = null;
                }
                return;
            }
            if (driveModeSetup)
            {
                displayResetTime = 0;
                if (driveMode < DriveMode.Trailing)
                    driveMode++;
                return;
            }
            if (MaxSpeedSetup)
            {
                displayResetTime = 0;
                MirelMaximumSpeed += 5;
                MaxSelectedSpeed = MirelMaximumSpeed;
                if (MirelMaximumSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS))
                    MirelMaximumSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                UpdateSpeedNumbers((int)MirelMaximumSpeed, false);
                return;
            }
            if (mainMode == MainMode.Default)
            {
                DriveModeHideModes = false;
                mainMode = MainMode.DriveMode;
                displayResetTime = 0;
                UpdateSpeedNumbers(0, true);
                return;
            }
            else if (mainMode == MainMode.DriveMode)
            {
                DriveModeHideModes = false;
                mainMode = MainMode.MaxSpeed;
                displayResetTime = 0;
                UpdateSpeedNumbers(0, true);
                return;
            }
        }

        protected float reducedSpeedTimer = 0;
        public bool VYP = false;
        public bool ZAP = false;
        public void MinusKeyPressed()
        {
            if (!Equipped) return;

            if (MirelType == Type.LS90)
            {
                if (Ls90power > LS90power.Off)
                {
                    Ls90power--;
                    Locomotive.SignalEvent(Common.Event.LightSwitchToggle);
                }
                return;
            }
            if (Locomotive.Battery)
            {
                Locomotive.SignalEvent(Common.Event.KeyboardBeep);
            }

            if (Locomotive.AbsSpeedMpS > 0 && selectedDriveMode != DriveMode.Trailing)
            {
                if (!ReducedSpeed)
                {
                    flashing = false;
                    float newReducedSpeed = MpS.ToKpH(Locomotive.AbsSpeedMpS);
                    newReducedSpeed = (float)Math.Round(newReducedSpeed * 0.2f, 0, MidpointRounding.AwayFromZero) / 0.2f;
                    if (newReducedSpeed < 10)
                        newReducedSpeed = 10;
                    if (newReducedSpeed > MaxSelectedSpeed)
                        newReducedSpeed = MaxSelectedSpeed;
                    ZAP = true;
                    if (VYP)
                    {
                        VYP = !VYP;
                        reducedSpeedTimer = 0;
                    }
                    maxReducedSpeed = newReducedSpeed;
                }
                else
                {
                    flashing = false;
                    MirelMaximumSpeed = MaxSelectedSpeed;
                    maxReducedSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                    VYP = true;
                    if (ZAP)
                    {
                        ZAP = !ZAP;
                        reducedSpeedTimer = 0;
                    }
                }
                ReducedSpeed = !ReducedSpeed;
                return;
            }

            if (initTest != InitTest.Passed)
                return;
            if (driveModeSetup)
            {
                displayResetTime = 0;
                if (driveMode == DriveMode.Shunting)
                    return;
                else
                    driveMode--;
                return;
            }
            if (MaxSpeedSetup)
            {
                displayResetTime = 0;
                MirelMaximumSpeed -= 5;
                MaxSelectedSpeed = MirelMaximumSpeed;
                if (MirelMaximumSpeed < 10)
                    MirelMaximumSpeed = MaxSelectedSpeed = 10;
                UpdateSpeedNumbers((int)MirelMaximumSpeed, false);
                return;
            }
            if (mainMode == MainMode.MaxSpeed)
            {
                DriveModeHideModes = false;
                mainMode = MainMode.DriveMode;
                displayResetTime = 0;
                UpdateSpeedNumbers(0, true);
                return;
            }
            
        }

        public bool ManualMode = false;
        public bool ManualModeDisplay = false;
        public bool SpeedSetupConfirmed = false;
        public void EnterKeyPressed()
        {
            SpeedSetupConfirmed = false;
            if (Locomotive.Battery)
            {
                Locomotive.SignalEvent(Common.Event.KeyboardBeep);
            }
            if (NZ1 || NZ2 || NZ3 || NZ5)
            {
                NZ1 = NZ2 = false;
                emergency = false;
                interventionTimer = 0;
                EmergencyBrakes(false);
                BlueLight = true;
                NZOK = true;
            }

            if (!Equipped) return;

            if (initTest != InitTest.Passed)
                return;
            if (Locomotive.AbsSpeedMpS == 0)
            {
                if (driveModeSetup)
                {
                    selectedDriveMode = driveMode;
                    flashFullDisplayInProggress = true;
                }

                if (MaxSpeedSetup)
                {
                    SpeedSetupConfirmed = true;
                    flashFullDisplayInProggress = true;
                    if (MaxSelectedSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS) || MirelMaximumSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS))
                    {
                        MaxSelectedSpeed = MirelMaximumSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                    }
                }

                if (mainMode == MainMode.DriveMode)
                {
                    if (!driveModeSetup)
                        driveMode = selectedDriveMode;
                    flashing = false;
                    DisplayFlashMask = false;
                    displayResetTime = 0;
                    driveModeSetup = true;
                    DriveModeHideModes = true;
                    UpdateSpeedNumbers(0, true);
                    if (driveMode == DriveMode.Off)
                        driveMode = DriveMode.Shunting;
                }
                if (mainMode == MainMode.MaxSpeed)
                {
                    flashing = false;
                    DisplayFlashMask = false;
                    displayResetTime = 0;
                    MaxSpeedSetup = true;
                    DriveModeHideModes = true;
                    MirelMaximumSpeed = MaxSelectedSpeed;
                    UpdateSpeedNumbers((int)MaxSelectedSpeed, false);
                }
            }
            if (selectedDriveMode != DriveMode.Normal) operationalState = OperationalState.Normal;
            if (operationalState == OperationalState.Restricting && !ManualMode && Locomotive.AbsSpeedMpS > 0 && !NZOK)
            {
                ManualMode = ManualModeDisplay = true;
            }
        }

        protected void ResetDisplay()
        {
            flashing = false;
            displayResetTime = 0;
            mainMode = MainMode.Default;
            driveMode = DriveMode.Off;
            UpdateSpeedNumbers((int)MirelMaximumSpeed, false);
        }

        protected bool defaultStateSet = true;
        protected void ToDefaultState()
        {
            Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
            if (defaultStateSet) return;
            defaultStateSet = true;
            Test1 = Test2 = Test3 = Test4 = Test5 = Test6 = Test7 = false;
            initTest = InitTest.Off;
            driveMode = DriveMode.Off;
            operationalState = OperationalState.Normal;
            batteryRunningTime = test3RunningTime = test4RunningTime = test6RunningTime = test6RunningTime = 0;
            engineBrakeApplied = false;
            engineBrakeReleased = false;
            UpdateSpeedNumbers(0, true);
            numCMTested = 0;
            brakesFilling = false;
            cmTestEmergency = false;
            numCMTested = 0;
            initialCheckForCMtest = false;
            mainMode = MainMode.Default;
            emergency = false;
            randomRepeaterSignalTime = 0;
            distanceAndSpeed = null;
            distanceToSpeedChange = 0;
            allowedSpeedInterventing = false;
            timeBeforeIntervetion = 0;
            timeInterventing = 0;
            currentSpeedStep = 1;
            stopInterventingUntilNextSignal = true;
            vigilanceAfterZeroSpeedConfirmed = false;
            previousDistanceToSignal = 0;
            manualModeTime = 0;
            interventionTimer = 0;
            MirelMaximumSpeed = 80;
            RecievingRepeaterSignal = false;
            BlueLight = false;
            DisplayFlashMask = false;
            MirelSpeedNum1 = 0;
            MirelSpeedNum2 = 0;
            MirelSpeedNum3 = 0;
            operationalState = new OperationalState();
            DriveModeHideModes = false;
            FullDisplay = false;
            selectedDriveMode = DriveMode.Shunting;
            NZOK = false;
            NZ1 = false;
            NZ2 = false;
            StartReducingSpeed = false;
            ZS1B = false;
            ZS1BConfirmed = false;
            ZS1Bplayed = false;
            NZOKtimer = 0;
            batteryRunningTime = 0;
            test3RunningTime = 0;
            test4RunningTime = 0;
            test6RunningTime = 0;
            test7RunningTime = 0;
            speedIsRestricted = false;
            engineBrakeApplied = false;
            engineBrakeReleased = false;
            displayResetTime = 0;
            flashTime = 0;
            flashing = false;
            driveModeSetup = false;
            flashFullDisplayInProggress = false;
            flashFullDisplayTimeElapsed = 0;
            flashFullDisplayFlashedTimes = 0;
            MaxSpeedSetup = false;
            MaxSelectedSpeed = 80;
            mirelBeep = false;
            mirelBeeping = false;
            confirming = Confirming.None;
            transmittionSignalFreq = TransmittionSignalFreq.None;
            confirmingTime = 0;
            repeaterRecievingDelayTime = 0;
            maxSpeedNeverSet = true;
            selectedApproachSpeed = 40;
            modelingSpeedCurve = false;
        }

        TrackMonitorSignalAspect previousTrackMonitorSignalAspect = TrackMonitorSignalAspect.None;
        protected void CheckNextSignal()
        {
            if (recieverState == RecieverState.Off)
            {
                MirelMaximumSpeed = MaxSelectedSpeed;
                return;
            }

            if (MaxSpeedSetup)
                return;

            if (selectedDriveMode == DriveMode.Trailing) return;

            if (ManualMode)
            {
                if (MpS.ToKpH(Locomotive.AbsSpeedMpS) <= MirelMaximumSpeed)
                {
                    flashing = false;
                    ManualMode = false;
                }
            }

            if (previousTrackMonitorSignalAspect == TrackMonitorSignalAspect.None)
                previousTrackMonitorSignalAspect = Locomotive.TrainControlSystem.CabSignalAspect;
            if (Locomotive.AbsSpeedMpS == 0 && previousDistanceToSignal > 0)
                previousDistanceToSignal = 0;
            Physics.Train train = Locomotive.Train;
            Signalling.ObjectItemInfo firstObject = null;
            if (train.SignalObjectItems.Count > 0 && Locomotive.AbsSpeedMpS > 0 && true == false)
            {
                int i = 0;
                firstObject = train.SignalObjectItems[i];
                firstObject.distance_to_train = train.GetObjectDistanceToTrain(firstObject);
                float distanceToTrain = firstObject.distance_to_train;
                if (previousDistanceToSignal < distanceToTrain)
                {
                    previousDistanceToSignal = distanceToTrain;
                    if (previousTrackMonitorSignalAspect == Locomotive.TrainControlSystem.CabSignalAspect)
                    {
                        previousTrackMonitorSignalAspect = Locomotive.TrainControlSystem.CabSignalAspect;
                        RecievingRepeaterSignal = false;
                        distanceAndSpeed = null;
                        distanceToSpeedChange = 0;
                        selectedApproachSpeed = 40;
                        allowedSpeedInterventing = false;
                        timeBeforeIntervetion = 0;
                        timeInterventing = 0;
                        currentSpeedStep = 1;
                        canChangeSelectedApproachSpeed = true;
                        stopInterventingUntilNextSignal = false;
                        ZS1B = false;
                        ZS1BConfirmed = false;
                        ZS1Bplayed = false;
                        if (ManualMode) flashing = false;
                        ManualMode = false;
                        interventionTimer = 0;
                    }
                }
            }

            if (previousTrackMonitorSignalAspect != Locomotive.TrainControlSystem.CabSignalAspect)
            {
                previousTrackMonitorSignalAspect = Locomotive.TrainControlSystem.CabSignalAspect;
                RecievingRepeaterSignal = false;
                distanceAndSpeed = null;
                distanceToSpeedChange = 0;
                selectedApproachSpeed = 40;
                allowedSpeedInterventing = false;
                timeBeforeIntervetion = 0;
                timeInterventing = 0;
                currentSpeedStep = 1;
                canChangeSelectedApproachSpeed = true;
                stopInterventingUntilNextSignal = false;
                ZS1B = false;
                ZS1BConfirmed = false;
                ZS1Bplayed = false;
                if (ManualMode) flashing = false;
                MirelMaximumSpeed = MpS.ToKpH(MaxSelectedSpeed);
                ManualMode = false;
                manualModeTime = 0;
                interventionTimer = 0;
            }
        }

        protected void UpdateSpeedNumbers(int Speed, bool NoSpeedDisplayed)
        {
            if ((initTest == InitTest.Passed && Locomotive.ActiveStation == MSTSLocomotive.DriverStation.None) ||
                (initTest == InitTest.Passed && Locomotive.ActiveStation == MSTSLocomotive.DriverStation.Station2 && !Locomotive.UsingRearCab) ||
                (initTest == InitTest.Passed && Locomotive.ActiveStation == MSTSLocomotive.DriverStation.Station1 && Locomotive.UsingRearCab))
                NoSpeedDisplayed = true;

            if (NoSpeedDisplayed || initTest != InitTest.Passed || ManualModeDisplay)
            {
                MirelSpeedNum1 = 0;
                MirelSpeedNum2 = 0;
                MirelSpeedNum3 = 0;
                return;
            }

            CheckNextSignal();
            if ((int)MirelMaximumSpeed < Speed)
                Speed = (int)MirelMaximumSpeed;
            if (MirelMaximumSpeed > MaxSelectedSpeed && selectedDriveMode != DriveMode.Trailing)
                MirelMaximumSpeed = Speed = (int)MaxSelectedSpeed;

            if (showingSelectedApproachSpeed) Speed = (int)selectedApproachSpeed;

            int num = Speed;
            List<int> listOfInts = new List<int>();
            while (num > 0)
            {
                listOfInts.Add(num % 10);
                num = num / 10;
            }
            listOfInts.Reverse();
            MirelSpeedNum1 = 0;
            MirelSpeedNum2 = 0;
            MirelSpeedNum3 = 1;
            if (listOfInts.Count == 1)
            {
                MirelSpeedNum3 = listOfInts[0] + 1;
            }
            else if (listOfInts.Count == 2)
            {
                MirelSpeedNum2 = listOfInts[0] + 1;
                MirelSpeedNum3 = listOfInts[1] + 1;
            }
            else if (listOfInts.Count == 3)
            {
                MirelSpeedNum1 = listOfInts[0] + 1;
                MirelSpeedNum2 = listOfInts[1] + 1;
                MirelSpeedNum3 = listOfInts[2] + 1;
            }
        }

        protected void EmergencyBrakes(bool Brakes)
        {
            if (Locomotive.ControllerVolts > 0)
                Locomotive.ControllerVolts = 0;
            if (Brakes)
            {
                Locomotive.EmergencyButtonPressed = true;
                Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
            }
            else
            {
                Locomotive.EmergencyButtonPressed = false;
                Locomotive.TrainBrakeController.EmergencyBrakingPushButton = false;
            }
        }

        public void AlerterPressed(bool Pressed)
        {
            if (!Pressed)
                return;
            if (initTest != InitTest.Passed)
                return;
            if (BlueLight)
            {
                Locomotive.SignalEvent(Common.Event.MirelUnwantedVigilancy);
                return;
            }
            Locomotive.SignalEvent(Common.Event.MirelOff);
            if (ZS1B && !ZS1BConfirmed)
            {
                System.Threading.Thread.Sleep(1);
                Locomotive.SignalEvent(Common.Event.MirelZS1B);
                ZS1BConfirmed = true;
            }

            interventionTimer = 0;
            mirelBeeping = false;
            BlueLight = true;
            vigilanceAfterZeroSpeedConfirmed = true;
            if (MirelType == Type.LS90)
                Locomotive.TrainBrakeController.EmergencyBrakingPushButton = false;
        }

        protected float interventionTimer = 0;
        protected bool emergency = false;
        protected float randomRepeaterSignalTime = 0;
        protected Dictionary<float, float> distanceAndSpeed = null;
        protected float distanceToSpeedChange = 0;
        protected bool allowedSpeedInterventing = false;
        protected float timeBeforeIntervetion = 0;
        protected float timeInterventing = 0;
        protected int currentSpeedStep = 1;
        protected bool stopInterventingUntilNextSignal = true;
        protected bool vigilanceAfterZeroSpeedConfirmed = false;
        protected float previousDistanceToSignal = 0;
        protected float manualModeTime = 0;
        protected bool flashingByMaxSpeed = false;
        protected void MirelCheck(float elapsedTimeSeconds)
        {
            if (MirelType == Type.LS90)
                NZ1 = NZ2 = NZ3 = NZ4 = NZ5 = false;
            if (NZ1 || NZ2|| NZ3|| NZ4|| NZ5)
            {
                EmergencyBrakes(true);
                return;
            }
            if (ManualMode)
            {
                manualModeTime += elapsedTimeSeconds;
                if (manualModeTime > 5)
                {
                    ManualModeDisplay = false;
                    flashing = true;
                }
            }

            if (Locomotive.AbsSpeedMpS == 0)
                vigilanceAfterZeroSpeedConfirmed = false;
            if (MirelType == Type.LS90)
                vigilanceAfterZeroSpeedConfirmed = true;
            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MpS.ToKpH(Locomotive.MaxSpeedMpS) && MirelType == Type.Full)
            {
                flashing = false;
                float diff = MpS.ToKpH(Locomotive.AbsSpeedMpS) - MpS.ToKpH(Locomotive.MaxSpeedMpS);
                if (diff > 3)
                {
                    if (!NZ2)
                        flashing = true;
                }
                if (diff > 5 && !mirelBeeping && !emergency)
                {
                    Locomotive.SignalEvent(Common.Event.MirelOverspeedOn);
                    mirelBeeping = true;
                }
                if (diff <= 5 && mirelBeeping)
                {
                    Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
                    mirelBeeping = false;
                }
                if (diff > 7 && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                {
                    ApplyNZ2();
                }
            }

            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MaxSelectedSpeed && MirelType == Type.Full)
            {
                flashing = false;
                float diff = MpS.ToKpH(Locomotive.AbsSpeedMpS) - MaxSelectedSpeed;
                if (diff > 3)
                {
                    if (!NZ2)
                        flashing = true;
                }
                if (diff > 5 && !mirelBeeping && !emergency)
                {
                    Locomotive.SignalEvent(Common.Event.MirelOverspeedOn);
                    mirelBeeping = true;
                    flashingByMaxSpeed = true;
                }
                if (diff <= 5 && mirelBeeping)
                {
                    Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
                    mirelBeeping = false;
                }
                if (diff > 7 && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                {
                    ApplyNZ2();
                }
            }
            else if (flashingByMaxSpeed)
            {
                flashing = false;
                mirelBeeping = false;
                flashingByMaxSpeed = false;
                Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
            }

            if (driveModeSetup || MaxSpeedSetup)
                return;
            if (selectedDriveMode != DriveMode.Normal && selectedDriveMode != DriveMode.Trailing)
            {
                RecievingRepeaterSignal = false;
            }
            if (selectedDriveMode == DriveMode.Shunting)
            {
                if (Locomotive.AbsSpeedMpS > 0)
                {
                    if (!vigilanceAfterZeroSpeedConfirmed)
                    {
                        interventionTimer += elapsedTimeSeconds;
                        if (interventionTimer > 5)
                        {
                            BlueLight = false;
                            Locomotive.SignalEvent(Common.Event.MirelOn);
                        }
                        if (interventionTimer > 8.5f)
                        {
                            ApplyNZ1();
                        }
                    }
                }

                if (MirelMaximumSpeed > 40)
                    MirelMaximumSpeed = 40;
                if (MirelMaximumSpeed > MaxSelectedSpeed)
                    MirelMaximumSpeed = MaxSelectedSpeed;

                if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MirelMaximumSpeed)
                {
                    flashing = false;
                    float diff = MpS.ToKpH(Locomotive.AbsSpeedMpS) - MirelMaximumSpeed;
                    if (diff > 3)
                    {
                        if (!NZ2)
                            flashing = true;
                    }
                    if (diff > 5 && !mirelBeeping && !emergency)
                    {
                        Locomotive.SignalEvent(Common.Event.MirelOverspeedOn);
                        mirelBeeping = true;
                    }
                    if (diff <= 5 && mirelBeeping)
                    {
                        Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
                        mirelBeeping = false;
                    }
                    if (diff > 7 && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                    {
                        ApplyNZ2();
                    }
                }

                if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < 20.5f && !emergency)
                {
                    if (vigilanceAfterZeroSpeedConfirmed)
                    {
                        BlueLight = true;
                    }
                }
                if (MpS.ToKpH(Locomotive.AbsSpeedMpS) >= 20.5f && MpS.ToKpH(Locomotive.AbsSpeedMpS) < 30 && !emergency)
                {
                    interventionTimer += elapsedTimeSeconds;
                    if (interventionTimer > 6)
                    {
                        BlueLight = false;
                    }
                    if (interventionTimer > 19.5f)
                    {
                        Locomotive.SignalEvent(Common.Event.MirelOn);
                    }
                    if (interventionTimer > 24)
                    {
                        ApplyNZ1();
                    }
                }
                else if (MpS.ToKpH(Locomotive.AbsSpeedMpS) >= 30)
                {
                    interventionTimer += elapsedTimeSeconds;
                    if (interventionTimer > 6)
                    {
                        BlueLight = false;
                    }
                    if (interventionTimer > 12.5f)
                    {
                        Locomotive.SignalEvent(Common.Event.MirelOn);
                    }
                    if (interventionTimer > 16)
                    {
                        ApplyNZ1();
                    }
                }
                if (emergency && Locomotive.AbsSpeedMpS == 0)
                {
                    emergency = false;
                    Locomotive.SignalEvent(Common.Event.MirelOff);
                    BlueLight = true;
                }
            }
            if ((selectedDriveMode == DriveMode.Normal || recieverState == RecieverState.Off) && selectedDriveMode != DriveMode.Trailing && selectedDriveMode != DriveMode.Shunting) 
            {
                // Cyklická kontrola bdělosti
                bool vigilanceActive = true;
                if (Locomotive.AbsSpeedMpS == 0)
                    vigilanceActive = false;
                if (Locomotive.AbsSpeedMpS > 0)
                {
                    if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < 15 && Locomotive.EngineBrakeController.CurrentValue > 0)
                        vigilanceActive = false;
                    if (RecievingRepeaterSignal)
                        vigilanceActive = false;
                }
                if (vigilanceActive)
                {
                    interventionTimer += elapsedTimeSeconds;
                    //Simulator.Confirmer.MSG(interventionTimer.ToString());
                    if (interventionTimer > 6)
                    {
                        BlueLight = false;
                    }
                    if (interventionTimer > 12.5f)
                    {
                        Locomotive.SignalEvent(Common.Event.MirelOn);
                    }
                    if (interventionTimer > 16)
                    {
                        if (MirelType == Type.Full)
                            ApplyNZ1();
                        else
                            Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
                    }
                }

                // Zvýšená cyklická kontrola bdělosti
                vigilanceActive = true;
                if (Locomotive.AbsSpeedMpS == 0)
                    vigilanceActive = false;
                if (Locomotive.AbsSpeedMpS > 0)
                {
                    if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < 15 && Locomotive.EngineBrakeController.CurrentValue > 0)
                        vigilanceActive = false;
                    if (Locomotive.TrainControlSystem.CabSignalAspect == TrackMonitorSignalAspect.Clear_2 || Locomotive.TrainControlSystem.CabSignalAspect == TrackMonitorSignalAspect.Restricted)
                        vigilanceActive = false;
                    if (NoAlertOnRestrictedSignal && Locomotive.TrainControlSystem.CabSignalAspect == TrackMonitorSignalAspect.Restricted)
                        vigilanceActive = false;
                    if (!RecievingRepeaterSignal)
                        vigilanceActive = false;
                }
                if (ManualMode) vigilanceActive = true;
                if (vigilanceActive)
                {
                    interventionTimer += elapsedTimeSeconds;
                    if (interventionTimer > 8.5)
                    {
                        BlueLight = false;
                        Locomotive.SignalEvent(Common.Event.MirelOn);
                    }
                    if (interventionTimer > 12)
                    {
                        if (MirelType == Type.Full)
                            ApplyNZ1();
                        else
                            Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
                    }
                }

                // Jednorázová kontrola po uvedení HDV do pohybu
                if (Locomotive.AbsSpeedMpS > 0 && initTest == InitTest.Passed)
                {
                    if (!vigilanceAfterZeroSpeedConfirmed)
                    {
                        interventionTimer += elapsedTimeSeconds;
                        if (interventionTimer > 5)
                        {
                            BlueLight = false;
                            Locomotive.SignalEvent(Common.Event.MirelOn);
                        }
                        if (interventionTimer > 8.5f)
                        {
                            ApplyNZ1();
                        }
                    }
                }

                if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MirelMaximumSpeed && MirelType == Type.Full)
                {
                    if (!ManualMode && !ManualModeDisplay)
                    {
                        flashing = false;
                        float diff = MpS.ToKpH(Locomotive.AbsSpeedMpS) - MirelMaximumSpeed;
                        if (diff > 3)
                        {
                            if (!NZ2)
                                flashing = true;
                        }
                        if (diff > 5 && !mirelBeeping && !emergency && Locomotive.EmergencyButtonPressed == false)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelOverspeedOn);
                            mirelBeeping = true;
                        }
                        if (diff <= 5 && mirelBeeping || Locomotive.EmergencyButtonPressed == true)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
                            mirelBeeping = false;
                        }
                        if (diff > 7 && Locomotive.EmergencyButtonPressed == false && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                        {
                            if (!NZ2) ApplyNZ2();
                        }
                    }
                }

                if (!RecievingRepeaterSignal && selectedDriveMode == DriveMode.Normal)
                {
                    if (recieverState == RecieverState.Off)
                    {
                        CheckSpeed(elapsedTimeSeconds);
                        return;
                    }

                    if (repeaterRecievingDelayTime == 0)
                    {
                        Random random = new Random();
                        randomRepeaterSignalTime = random.Next(3, 10);
                        randomRepeaterSignalTime /= 10;
                    }
                    repeaterRecievingDelayTime += elapsedTimeSeconds;
                    if (repeaterRecievingDelayTime > randomRepeaterSignalTime)
                    {
                        RecievingRepeaterSignal = true;
                        if (Locomotive.TrainControlSystem.CabSignalAspect != TrackMonitorSignalAspect.Stop &&
                            Locomotive.TrainControlSystem.CabSignalAspect != TrackMonitorSignalAspect.StopAndProceed &&
                            selectedDriveMode == DriveMode.Normal &&
                            MpS.ToKpH(Locomotive.AbsSpeedMpS) < 5
                            )
                            Locomotive.SignalEvent(Common.Event.MirelClearSignalAhead);
                        repeaterRecievingDelayTime = 0;
                    }
                }

                if (!RecievingRepeaterSignal && selectedDriveMode == DriveMode.Shunting)
                {
                        MirelMaximumSpeed = 40;
                }
                switch (Locomotive.TrainControlSystem.CabSignalAspect)
                {
                    case TrackMonitorSignalAspect.Clear_2:
                    case TrackMonitorSignalAspect.None:
                        {
                            if (recieverState == RecieverState.Off)
                            {
                                CheckSpeed(elapsedTimeSeconds);
                                return;
                            }
                            operationalState = OperationalState.Normal;
                            MirelMaximumSpeed = MaxSelectedSpeed;
                            if (Locomotive.AbsSpeedMpS > 0) StartReducingSpeed = false;
                            distanceAndSpeed = null;
                            break;
                        }
                    case TrackMonitorSignalAspect.Approach_1:
                    case TrackMonitorSignalAspect.Approach_2:
                    case TrackMonitorSignalAspect.Approach_3:
                    case TrackMonitorSignalAspect.Clear_1:
                        {
                            if (recieverState == RecieverState.Off)
                            {
                                CheckSpeed(elapsedTimeSeconds);
                                return;
                            }
                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < MirelMaximumSpeed && Locomotive.AbsSpeedMpS > 0 && ManualMode)
                                StartReducingSpeed = false;
                            operationalState = OperationalState.Restricting;
                            if (ManualMode)
                            {
                                if (selectedApproachSpeed > 0)
                                    MirelMaximumSpeed = selectedApproachSpeed;
                                else
                                    MirelMaximumSpeed = 40;
                                stopInterventingUntilNextSignal = true;
                            }
                            if (Locomotive.AbsSpeedMpS == 0)
                            {
                                stopInterventingUntilNextSignal = true;
                                if (selectedApproachSpeed == 40)
                                    MirelMaximumSpeed = 40;
                            }
                            if (stopInterventingUntilNextSignal)
                            {
                                if (Locomotive.AbsSpeedMpS == 0)
                                    StartReducingSpeed = true;
                                else
                                    StartReducingSpeed = false;
                                return;
                            }

                            float slowDown = 1.5f;
                            if (MpS.ToKpH(Locomotive.MaxSpeedMpS) < 101) slowDown = 0.6f;
                            if (MpS.ToKpH(Locomotive.MaxSpeedMpS) < 141) slowDown = 0.82f;

                            if (distanceAndSpeed == null)
                                distanceAndSpeed = GetRequiredDecelerationMeters(slowDown, MaxSelectedSpeed, selectedApproachSpeed);
                            float requiredSpeed = 0;
                            float fullDistance = 1000;
                            if (distanceAndSpeed.Count > 0)
                            {
                                foreach (KeyValuePair < float, float> speed in distanceAndSpeed)
                                {
                                    if (true)
                                    {
                                        fullDistance = speed.Value;
                                        break;
                                    }
                                }
                            }
                            bool fast = false;
                            float timeBeforeCountDown = 1500 - fullDistance;
                            timeBeforeCountDown = timeBeforeCountDown / Locomotive.MaxSpeedMpS;
                            if (MaxSelectedSpeed <= 120)
                            {
                                if (timeBeforeIntervetion > timeBeforeCountDown - 10 && !ZS1BConfirmed && !ZS1B)
                                {
                                    BlueLight = false;
                                    ZS1B = true;
                                    Locomotive.SignalEvent(Common.Event.MirelOn);
                                }

                                if (timeBeforeIntervetion > timeBeforeCountDown - 10)
                                {
                                    fast = true;
                                }
                            }
                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > selectedApproachSpeed)
                                StartReducingSpeedLightFlash(fast, elapsedTimeSeconds);
                            else
                                StartReducingSpeed = false;
                            timeBeforeIntervetion += elapsedTimeSeconds;
                            if (timeBeforeIntervetion > timeBeforeCountDown - 2)
                                canChangeSelectedApproachSpeed = false;

                            if (timeBeforeIntervetion > timeBeforeCountDown)
                            {
                                canChangeSelectedApproachSpeed = false;
                                timeInterventing += elapsedTimeSeconds;
                            }
                            if (timeInterventing > 1)
                            {
                                int i = 0;
                                foreach (KeyValuePair<float, float> speed in distanceAndSpeed)
                                {
                                    if (i == currentSpeedStep)
                                    {
                                        currentSpeedStep++;
                                        requiredSpeed = speed.Key;
                                        timeInterventing = 0;
                                        break;
                                    }
                                    i++;
                                }
                            }

                            if (MaxSelectedSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS)) MaxSelectedSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                            if (MirelMaximumSpeed > MaxSelectedSpeed) MirelMaximumSpeed = MaxSelectedSpeed;
                            if (requiredSpeed > 0)
                            {
                                if (MirelMaximumSpeed > requiredSpeed)
                                    MirelMaximumSpeed -= 5;
                                if (MirelMaximumSpeed < selectedApproachSpeed)
                                    MirelMaximumSpeed = selectedApproachSpeed;
                            }

                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MirelMaximumSpeed + 0.5f && !emergency && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                            {
                                ApplyNZ2();
                            }
                            else if (emergency && NZOK)
                            {
                                EmergencyBrakes(false);
                                //Locomotive.SetTrainBrakePercent(0);
                                emergency = false;
                            }
                            break;
                        }
                    case TrackMonitorSignalAspect.Restricted:
                        {
                            selectedApproachSpeed = 120;
                            if (recieverState == RecieverState.Off)
                            {
                                CheckSpeed(elapsedTimeSeconds);
                                return;
                            }
                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < MirelMaximumSpeed && Locomotive.AbsSpeedMpS > 0)
                                if (!modelingSpeedCurve)
                                    StartReducingSpeed = false;
                            operationalState = OperationalState.Restricting;
                            if (ManualMode)
                            {
                                MirelMaximumSpeed = 120;
                                stopInterventingUntilNextSignal = true;
                            }

                            if (Locomotive.AbsSpeedMpS == 0)
                            {
                                MirelMaximumSpeed = 120;
                                stopInterventingUntilNextSignal = true;
                            }
                            if (MaxSelectedSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS)) MaxSelectedSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                            if (MirelMaximumSpeed > MaxSelectedSpeed) MirelMaximumSpeed = MaxSelectedSpeed;
                            if (MirelMaximumSpeed > MaxSelectedSpeed)
                                MirelMaximumSpeed = MaxSelectedSpeed;
                            if (stopInterventingUntilNextSignal)
                            {
                                if (Locomotive.AbsSpeedMpS == 0)
                                    StartReducingSpeed = true;
                                else
                                    StartReducingSpeed = false;

                                return;
                            }

                            modelingSpeedCurve = true;
                            float slowDown = 1.5f;
                            if (MpS.ToKpH(Locomotive.MaxSpeedMpS) < 101) slowDown = 0.6f;
                            if (MpS.ToKpH(Locomotive.MaxSpeedMpS) < 141) slowDown = 0.82f;

                            if (distanceAndSpeed == null)
                                distanceAndSpeed = GetRequiredDecelerationMeters(slowDown, MaxSelectedSpeed, selectedApproachSpeed);
                            float requiredSpeed = 0;
                            float fullDistance = 1000;
                            if (distanceAndSpeed.Count > 0)
                            {
                                foreach (KeyValuePair<float, float> speed in distanceAndSpeed)
                                {
                                    if (true)
                                    {
                                        fullDistance = speed.Value;
                                        break;
                                    }
                                }
                            }
                            bool fast = false;
                            float timeBeforeCountDown = 1000 - fullDistance;
                            timeBeforeCountDown = timeBeforeCountDown / Locomotive.MaxSpeedMpS;
                            if (MaxSelectedSpeed <= 100)
                            {
                                if (timeBeforeIntervetion > timeBeforeCountDown - 10 && !ZS1BConfirmed && !ZS1B)
                                {
                                    BlueLight = false;
                                    ZS1B = true;
                                    Locomotive.SignalEvent(Common.Event.MirelOn);
                                }

                                if (timeBeforeIntervetion > timeBeforeCountDown - 10)
                                {
                                    fast = true;
                                }
                            }
                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > selectedApproachSpeed && !ManualMode)
                                StartReducingSpeedLightFlash(fast, elapsedTimeSeconds);
                            else
                                StartReducingSpeed = false;
                            timeBeforeIntervetion += elapsedTimeSeconds;
                            if (timeBeforeIntervetion > timeBeforeCountDown - 2)
                                canChangeSelectedApproachSpeed = false;

                            if (timeBeforeIntervetion > timeBeforeCountDown)
                            {
                                canChangeSelectedApproachSpeed = false;
                                timeInterventing += elapsedTimeSeconds;
                            }
                            if (timeInterventing > 1)
                            {
                                int i = 0;
                                foreach (KeyValuePair<float, float> speed in distanceAndSpeed)
                                {
                                    if (i == currentSpeedStep)
                                    {
                                        currentSpeedStep++;
                                        requiredSpeed = speed.Key;
                                        timeInterventing = 0;
                                        break;
                                    }
                                    i++;
                                }
                            }

                            if (MaxSelectedSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS)) MaxSelectedSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                            if (MirelMaximumSpeed > MaxSelectedSpeed) MirelMaximumSpeed = MaxSelectedSpeed;
                            if (requiredSpeed > 0)
                            {
                                if (MirelMaximumSpeed > requiredSpeed)
                                    MirelMaximumSpeed -= 5;
                                if (MirelMaximumSpeed < selectedApproachSpeed)
                                    MirelMaximumSpeed = selectedApproachSpeed;
                            }

                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MirelMaximumSpeed + 0.5f && !emergency)
                            {
                                ApplyNZ2();
                            }
                            else if (emergency && NZOK)
                            {
                                EmergencyBrakes(false);
                                //Locomotive.SetTrainBrakePercent(0);
                                emergency = false;
                            }
                            break;
                        }
                    case TrackMonitorSignalAspect.Stop:
                    case TrackMonitorSignalAspect.StopAndProceed:
                        {
                            if (recieverState == RecieverState.Off)
                            {
                                CheckSpeed(elapsedTimeSeconds);
                                return;
                            }
                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < MirelMaximumSpeed && Locomotive.AbsSpeedMpS > 0 && MirelType == Type.Full)
                                if (!modelingSpeedCurve)
                                    StartReducingSpeed = false;
                            operationalState = OperationalState.Restricting;
                            if (ManualMode)
                            {
                                MirelMaximumSpeed = 40;
                                stopInterventingUntilNextSignal = true;
                            }

                            if (Locomotive.AbsSpeedMpS == 0)
                            {
                                MirelMaximumSpeed = 40;
                                stopInterventingUntilNextSignal = true;
                            }
                            if (MaxSelectedSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS)) MaxSelectedSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                            if (MirelMaximumSpeed > MaxSelectedSpeed) MirelMaximumSpeed = MaxSelectedSpeed;
                            if (MirelMaximumSpeed > MaxSelectedSpeed)
                                MirelMaximumSpeed = MaxSelectedSpeed;
                            if (stopInterventingUntilNextSignal)
                            {
                                if (Locomotive.AbsSpeedMpS == 0)
                                    StartReducingSpeed = true;
                                else
                                    StartReducingSpeed = false;

                                return;
                            }

                            modelingSpeedCurve = true;
                            float slowDown = 1.5f;
                            if (MpS.ToKpH(Locomotive.MaxSpeedMpS) < 101) slowDown = 0.6f;
                            if (MpS.ToKpH(Locomotive.MaxSpeedMpS) < 141 && MpS.ToKpH(Locomotive.MaxSpeedMpS) > 100) slowDown = 0.82f;

                            if (distanceAndSpeed == null)
                                distanceAndSpeed = GetRequiredDecelerationMeters(slowDown, MaxSelectedSpeed, selectedApproachSpeed);
                            float requiredSpeed = 0;
                            float fullDistance = 1000;
                            if (distanceAndSpeed.Count > 0)
                            {
                                foreach (KeyValuePair<float, float> speed in distanceAndSpeed)
                                {
                                    if (true)
                                    {
                                        fullDistance = speed.Value;
                                        break;
                                    }
                                }
                            }
                            bool fast = false;
                            float timeBeforeCountDown = 1000 - fullDistance;
                            timeBeforeCountDown = timeBeforeCountDown / Locomotive.MaxSpeedMpS;
                            if (MaxSelectedSpeed <= 100)
                            {
                                if (timeBeforeIntervetion > timeBeforeCountDown - 10 && !ZS1BConfirmed && !ZS1B)
                                {
                                    BlueLight = false;
                                    ZS1B = true;
                                    Locomotive.SignalEvent(Common.Event.MirelOn);
                                }

                                if (timeBeforeIntervetion > timeBeforeCountDown - 10)
                                {
                                    fast = true;
                                }
                            }
                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > selectedApproachSpeed && !ManualMode)
                                StartReducingSpeedLightFlash(fast, elapsedTimeSeconds);
                            else
                                StartReducingSpeed = false;
                            timeBeforeIntervetion += elapsedTimeSeconds;
                            if (timeBeforeIntervetion > timeBeforeCountDown - 2)
                                canChangeSelectedApproachSpeed = false;

                            if (timeBeforeIntervetion > timeBeforeCountDown)
                            {
                                canChangeSelectedApproachSpeed = false;
                                timeInterventing += elapsedTimeSeconds;
                            }
                            if (timeInterventing > 1)
                            {
                                int i = 0;
                                foreach (KeyValuePair<float, float> speed in distanceAndSpeed)
                                {
                                    if (i == currentSpeedStep)
                                    {
                                        currentSpeedStep++;
                                        requiredSpeed = speed.Key;
                                        timeInterventing = 0;
                                        break;
                                    }
                                    i++;
                                }
                            }

                            if (MaxSelectedSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS)) MaxSelectedSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                            if (MirelMaximumSpeed > MaxSelectedSpeed) MirelMaximumSpeed = MaxSelectedSpeed;
                            if (requiredSpeed > 0)
                            {
                                if (MirelMaximumSpeed > requiredSpeed)
                                    MirelMaximumSpeed -= 5;
                                if (MirelMaximumSpeed < selectedApproachSpeed)
                                    MirelMaximumSpeed = selectedApproachSpeed;
                            }

                            if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MirelMaximumSpeed + 0.5f && !emergency && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                            {
                                ApplyNZ2();
                            }
                            else if (emergency && NZOK)
                            {
                                EmergencyBrakes(false);
                                //Locomotive.SetTrainBrakePercent(0);
                                emergency = false;
                            }
                            break;
                        }
                }
                CheckSpeed(elapsedTimeSeconds);
            }

            if (selectedDriveMode == DriveMode.Lockout)
            {
                // Cyklická kontrola bdělosti
                float maxSpeed = 120;
                if (maxSpeed > MaxSelectedSpeed) maxSpeed = MaxSelectedSpeed;
                if (maxSpeed > MpS.ToKpH(Locomotive.MaxSpeedMpS)) maxSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);

                MirelMaximumSpeed = maxSpeed;

                bool vigilanceActive = true;
                if (Locomotive.AbsSpeedMpS == 0)
                    vigilanceActive = false;
                if (Locomotive.AbsSpeedMpS > 0)
                {
                    if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < 15 && Locomotive.EngineBrakeController.CurrentValue > 0)
                        vigilanceActive = false;
                }
                if (vigilanceActive)
                {
                    interventionTimer += elapsedTimeSeconds;
                    if (interventionTimer > 6)
                    {
                        BlueLight = false;
                    }
                    if (interventionTimer > 12.5f)
                    {
                        Locomotive.SignalEvent(Common.Event.MirelOn);
                    }
                    if (interventionTimer > 16)
                    {
                        ApplyNZ1();
                    }
                }

                // Zvýšená cyklická kontrola bdělosti
                vigilanceActive = true;
                if (Locomotive.AbsSpeedMpS == 0)
                    vigilanceActive = false;
                if (Locomotive.AbsSpeedMpS > 0)
                {
                    if (MpS.ToKpH(Locomotive.AbsSpeedMpS) < 15 && Locomotive.EngineBrakeController.CurrentValue > 0)
                        vigilanceActive = false;
                    if (Locomotive.TrainControlSystem.CabSignalAspect != TrackMonitorSignalAspect.Stop && Locomotive.TrainControlSystem.CabSignalAspect != TrackMonitorSignalAspect.StopAndProceed)
                        vigilanceActive = false;
                    if ((Locomotive.TrainControlSystem.CabSignalAspect == TrackMonitorSignalAspect.Stop || Locomotive.TrainControlSystem.CabSignalAspect == TrackMonitorSignalAspect.StopAndProceed) & MirelMaximumSpeed > 40)
                        vigilanceActive = false;
                }
                if (ManualMode) vigilanceActive = true;
                if (vigilanceActive)
                {
                    interventionTimer += elapsedTimeSeconds;
                    if (interventionTimer > 8.5)
                    {
                        BlueLight = false;
                        Locomotive.SignalEvent(Common.Event.MirelOn);
                    }
                    if (interventionTimer > 12)
                    {
                        ApplyNZ1();
                    }
                }

                // Jednorázová kontrola po uvedení HDV do pohybu
                if (Locomotive.AbsSpeedMpS > 0)
                {
                    if (!vigilanceAfterZeroSpeedConfirmed)
                    {
                        interventionTimer += elapsedTimeSeconds;
                        if (interventionTimer > 5)
                        {
                            BlueLight = false;
                            Locomotive.SignalEvent(Common.Event.MirelOn);
                        }
                        if (interventionTimer > 8.5f)
                        {
                            ApplyNZ1();
                        }
                    }
                }

                if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MirelMaximumSpeed)
                {
                    if (!ManualMode && !ManualModeDisplay)
                    {
                        flashing = false;
                        float diff = MpS.ToKpH(Locomotive.AbsSpeedMpS) - MirelMaximumSpeed;
                        if (diff > 3)
                        {
                            if (!NZ2)
                                flashing = true;
                        }
                        if (diff > 5 && !mirelBeeping && !emergency)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelOverspeedOn);
                            mirelBeeping = true;
                        }
                        if (diff <= 5 && mirelBeeping)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
                            mirelBeeping = false;
                        }
                        if (diff > 7 && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                        {
                            if (!NZ2) ApplyNZ2();
                        }
                    }
                }
            }

            if (selectedDriveMode == DriveMode.Trailing)
            {
                RecievingRepeaterSignal = false;
                interventionTimer = 0;
                float maxSpeed = MpS.ToKpH(Locomotive.MaxSpeedMpS);
                MirelMaximumSpeed = MaxSelectedSpeed = maxSpeed;
                if (MpS.ToKpH(Locomotive.AbsSpeedMpS) > MirelMaximumSpeed)
                {
                    if (!ManualMode && !ManualModeDisplay)
                    {
                        flashing = false;
                        float diff = MpS.ToKpH(Locomotive.AbsSpeedMpS) - MirelMaximumSpeed;
                        if (diff > 3)
                        {
                            if (!NZ2)
                                flashing = true;
                        }
                        if (diff > 5 && !mirelBeeping && !emergency)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelOverspeedOn);
                            mirelBeeping = true;
                        }
                        if (diff <= 5 && mirelBeeping)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
                            mirelBeeping = false;
                        }
                        if (diff > 7 && Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) > 4.98f)
                        {
                            if (!NZ2) ApplyNZ2();
                        }
                    }
                }
            }
        }

        private void CheckSpeed(float elapsedTimeSeconds)
        {
            if (!RecievingRepeaterSignal)
            {
                if (recievingTimer < 0.1)
                {
                    if (MirelMaximumSpeed > 120)
                    {
                        MirelMaximumSpeed = 120;
                    }
                }
                else
                {
                    recievingTimer = recievingTimer - elapsedTimeSeconds;
                }
            }
            else
            {
                if (recievingTimer < 5)
                {
                    if (MirelMaximumSpeed > 120 && recievingTimer < 3)
                    {
                        MirelMaximumSpeed = 120;
                    }
                    recievingTimer = recievingTimer + elapsedTimeSeconds;
                }
            }
        }
        private Dictionary<float, float> GetRequiredDecelerationMeters(float DecelerationMpSS, float StartSpeedKpH, float EndSpeedKph)
        {
            Dictionary<float, float> speeds = new Dictionary<float, float>();
            float startSpeedMps = MpS.FromKpH(StartSpeedKpH);
            float currentSpeedMps = startSpeedMps;
            float distanceNeeded = 0;
            while (currentSpeedMps > 0)
            {
                speeds.Add(currentSpeedMps, currentSpeedMps * 3.6f);
                currentSpeedMps -= DecelerationMpSS;
            }

            Dictionary<float, float> resultSet = new Dictionary<float, float>();
            foreach (KeyValuePair<float, float> speed in speeds)
            {
                if (speed.Value < EndSpeedKph)
                    break;
                distanceNeeded += speed.Key;
            }

            foreach (KeyValuePair<float, float> speed in speeds)
            {
                resultSet.Add(speed.Value, distanceNeeded);
                distanceNeeded -= speed.Key;
                if (distanceNeeded < 0)
                    break;
            }

            return resultSet;
        }

        private void ApplyNZ1()
        {
            if (MirelType == Type.LS90)
                return;
            emergency = true;
            Locomotive.SignalEvent(Common.Event.MirelOff);
            mirelBeeping = false;
            NZ1 = true;
            EmergencyBrakes(true);
            //Locomotive.SetTrainBrakePercent(100);
            flashing = false;
        }

        private void ApplyNZ2()
        {
            if (MirelType == Type.LS90)
                return;
            emergency = true;
            Locomotive.SignalEvent(Common.Event.MirelOverspeedOff);
            mirelBeeping = false;
            NZ2 = true;
            EmergencyBrakes(true);
            //Locomotive.SetTrainBrakePercent(100);
            flashing = false;
        }

        protected float startReducingSpeedLightFlashTime = 0;
        private void StartReducingSpeedLightFlash(bool Fast, float ElapsedTimeClockSeconds)
        {
            startReducingSpeedLightFlashTime += ElapsedTimeClockSeconds;
            if (Fast)
            {
                if (startReducingSpeedLightFlashTime > 0.25f)
                {
                    StartReducingSpeed = !StartReducingSpeed;
                    startReducingSpeedLightFlashTime = 0;
                }
            }
            else
            {
                if (startReducingSpeedLightFlashTime > 0.5f)
                {
                    StartReducingSpeed = !StartReducingSpeed;
                    startReducingSpeedLightFlashTime = 0;
                }
            }
        }

        public bool NZ3 = false;
        private float metersWrongDirection = 0;
        private float previousOdometerM = 0;
        private bool zs3 = false;
        private bool previousCab = false;
        private void CheckNZ3()
        {
            return;
            if (NZOK)
            {
                previousOdometerM = Locomotive.OdometerM;
                NZ3 = false;
            }

            if (Locomotive.UsingRearCab != previousCab)
            {
                previousOdometerM = 0;
                previousCab = Locomotive.UsingRearCab;
            }

            if (Locomotive.Direction == Direction.N) previousOdometerM = Locomotive.OdometerM;

            if (Locomotive.Direction == Direction.Forward)
            {
                if (!Locomotive.UsingRearCab)
                {
                    if (previousOdometerM > Locomotive.OdometerM)
                    {
                        metersWrongDirection = previousOdometerM - Locomotive.OdometerM;
                        if (metersWrongDirection >= 3)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelZS3);
                            zs3 = true;
                        }
                        if (metersWrongDirection >= 10)
                        {
                            if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                            zs3 = false;
                            NZ3 = true;
                            EmergencyBrakes(true);
                        }
                    }
                    else
                    {
                        if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                        previousOdometerM = Locomotive.OdometerM;
                    }
                }
                else
                {
                    if (previousOdometerM > Locomotive.OdometerM)
                    {
                        metersWrongDirection = Locomotive.OdometerM - previousOdometerM;
                        if (metersWrongDirection >= 3)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelZS3);
                            zs3 = true;
                        }
                        if (metersWrongDirection >= 10)
                        {
                            if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                            zs3 = false;
                            NZ3 = true;
                            EmergencyBrakes(true);
                        }
                    }
                    else
                    {
                        if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                        previousOdometerM = Locomotive.OdometerM;
                    }
                }
            }
            if (Locomotive.Direction == Direction.Reverse)
            {
                if (Locomotive.UsingRearCab)
                {
                    if (previousOdometerM < Locomotive.OdometerM)
                    {
                        metersWrongDirection = Locomotive.OdometerM - previousOdometerM;
                        if (metersWrongDirection >= 3)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelZS3);
                            zs3 = true;
                        }
                        if (metersWrongDirection >= 10)
                        {
                            if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                            zs3 = false;
                            NZ3 = true;
                            EmergencyBrakes(true);
                        }
                    }
                    else
                    {
                        if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                        previousOdometerM = Locomotive.OdometerM;
                    }
                }
                else
                {
                    if (previousOdometerM > Locomotive.OdometerM)
                    {
                        metersWrongDirection = previousOdometerM - Locomotive.OdometerM;
                        if (metersWrongDirection >= 3)
                        {
                            Locomotive.SignalEvent(Common.Event.MirelZS3);
                            zs3 = true;
                        }
                        if (metersWrongDirection >= 10)
                        {
                            if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                            zs3 = false;
                            NZ3 = true;
                            EmergencyBrakes(true);
                        }
                    }
                    else
                    {
                        if (zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                        previousOdometerM = Locomotive.OdometerM;
                    }
                }
            }
        }

        public bool NZ4 = false;
        public bool NZ5 = false;
        public float NZ5timer = 0;
        private bool nz5zs3 = false;

        private void CheckNZ5(float ElapsedClockSeconds)
        {
            if (NZOK) NZ5timer = 0;
            if ((Bar.FromPSI(Locomotive.BrakeSystem.GetCylPressurePSI()) > 1.5f || Locomotive.AbsSpeedMpS > 0) && !NZ5)
            {
                if (!zs3 && nz5zs3) Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                nz5zs3 = false;
                NZ5timer = 0;
                return;
            }

            NZ5timer += ElapsedClockSeconds;

            if (NZ5timer > 20)
            {
                nz5zs3 = true;
                Locomotive.SignalEvent(Common.Event.MirelZS3);
            }

            if (NZ5timer > 25 && MirelType == Type.Full)
            {
                Locomotive.SignalEvent(Common.Event.MirelZS3Off);
                NZ5 = true;
                EmergencyBrakes(true);
            }
            if (NZ5) NZ1 = NZ2 = NZ3 = NZ4 = false;
        }

        public void ResetVigilance()
        {
            if (Equipped && !BlueLight && initTest == InitTest.Passed && Locomotive.SpeedMpS > 0) AlerterPressed(true);
            interventionTimer = 0;
        }
    }

    public class MirelSignal
    {
        public int SignalId { get; set; }
        public string Value { get; set; }
    }
}