// COPYRIGHT 2009 - 2021 by the Open Rails project.
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

/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 *  The viewer is a separate class object since there could be multiple 
 *  viewers potentially on different devices for a single car. 
 *  
 */


using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks
{

    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Extended physical motion and behaviour of the car.
    /// </summary>

    public class ExtendedPhysics
    {
        public List<Undercarriage> Undercarriages = new List<Undercarriage>();
        public MSTSLocomotive Locomotive = null;
        public ExtendedDynamicBrake extendedDynamicBrake = null;
        public int NumMotors = 0;
        public int NumAxles = 0;
        public float StarorsCurrent = 0;
        public float RotorsCurrent = 0;
        public float TotalCurrent = 0;
        public float TotalMaxCurrent = 0;
        public float UndercarriageDistance = 0;
        public float CouplerDistanceFromTrack = 0;
        public float CenterOfGravityDistanceFromTrack = 0;
        public float AverageAxleSpeedMpS = 0;
        public float FastestAxleSpeedMpS = 0;
        public float OverridenControllerVolts = 0;
        public bool IndependetMotorPower = false;
        public bool UseControllerVolts = false;
        public float TotalForceN = 0;
        public float TotalMaxForceN = 0;
        public ExtendedPhysics(MSTSLocomotive loco)
        {
            Locomotive = loco;
        }

        public void Parse(string path)
        {
            string delimiter = "";
            try
            {
                float test = float.Parse("0.0");
            }
            catch
            {
                delimiter = ",";
            }
            string innerText = "";
            TotalMaxCurrent = Locomotive.MaxCurrentA;
            XmlDocument document = new XmlDocument();
            document.Load(path);
            foreach (XmlNode node in document.ChildNodes)
            {
                if (node.Name.ToLower() == "extendedphysics")
                {
                    foreach (XmlNode main in node.ChildNodes)
                    {
                        innerText = main.InnerText;
                        if (!string.IsNullOrEmpty(delimiter))
                            innerText = innerText.Replace(".", delimiter);
                        if (main.Name.ToLower() == "couplerdistancefromtrack")
                            CouplerDistanceFromTrack = float.Parse(innerText);
                        if (main.Name.ToLower() == "centerofgravitydistancefromtrack")
                            CenterOfGravityDistanceFromTrack = float.Parse(innerText);
                        if (main.Name.ToLower() == "independentmotorpower")
                            IndependetMotorPower = true;
                        if (main.Name.ToLower() == "usecontrollervolts")
                            UseControllerVolts = true;
                        if (main.Name.ToLower() == "undercarriage")
                        {
                            Undercarriage undercarriage = new Undercarriage();
                            foreach (XmlNode undercarriageNode in main.ChildNodes)
                            {
                                innerText = undercarriageNode.InnerText;
                                if (!string.IsNullOrEmpty(delimiter))
                                    innerText = innerText.Replace(".", delimiter);
                                if (undercarriageNode.Name.ToLower() == "id")
                                    undercarriage.Id = int.Parse(innerText);
                                if (undercarriageNode.Name.ToLower() == "pivoty")
                                    undercarriage.PivotY = int.Parse(innerText);
                                if (undercarriageNode.Name.ToLower() == "pivotz")
                                    undercarriage.PivotZ = int.Parse(innerText);
                                if (undercarriageNode.Name.ToLower() == "axle")
                                {
                                    ExtendedAxle extendedAxle = new ExtendedAxle(Locomotive);
                                    foreach (XmlNode axleNode in undercarriageNode.ChildNodes)
                                    {
                                        innerText = axleNode.InnerText;
                                        if (!string.IsNullOrEmpty(delimiter))
                                            innerText = innerText.Replace(".", delimiter);
                                        if (axleNode.Name.ToLower() == "id")
                                            extendedAxle.Id = int.Parse(innerText);
                                        if (axleNode.Name.ToLower() == "pivoty")
                                            extendedAxle.PivotY = int.Parse(innerText);
                                        if (axleNode.Name.ToLower() == "wheeldiameter")
                                            extendedAxle.WheelDiameter = int.Parse(innerText);
                                        if (axleNode.Name.ToLower() == "havespeedometersensor")
                                            extendedAxle.HaveSpeedometerSensor = true;
                                        if (axleNode.Name.ToLower() == "havetcssensor")
                                            extendedAxle.HaveTcsSensor = true;
                                        if (axleNode.Name.ToLower() == "electricmotor")
                                        {
                                            ElectricMotor electricMotor = new ElectricMotor(Locomotive);
                                            electricMotor.MaxControllerVolts = Locomotive.MaxControllerVolts;
                                            NumMotors++;
                                            extendedAxle.NumMotors++;
                                            foreach (XmlNode motorNode in axleNode.ChildNodes)
                                            {
                                                innerText = motorNode.InnerText;
                                                if (!string.IsNullOrEmpty(delimiter))
                                                    innerText = innerText.Replace(".", delimiter);
                                                if (motorNode.Name.ToLower() == "id")
                                                    electricMotor.Id = int.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "statorinserieswith")
                                                    electricMotor.InSeriesWith = int.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "maxstatorcurrenta")
                                                    electricMotor.MaxStatorCurrent = float.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "maxrotorcurrenta")
                                                    electricMotor.MaxRotorCurrent = int.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "maxnegativestatorcurrenta")
                                                    electricMotor.MaxNegativeStatorCurrent = float.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "maxnegativerotorcurrenta")
                                                    electricMotor.MaxNegativeRotorCurrent = int.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "minrotorcurrenta")
                                                    electricMotor.MinRotorCurrent = int.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "errorcoefficient")
                                                    electricMotor.ErrorCoefficient = float.Parse(innerText);
                                                if (motorNode.Name.ToLower() == "gearratio")
                                                    electricMotor.GearRatio = float.Parse(innerText);
                                            }
                                            extendedAxle.ElectricMotors.Add(electricMotor);
                                        }
                                    }
                                    NumAxles++;
                                    undercarriage.FullAxleDistance += extendedAxle.PivotY < 0 ? -extendedAxle.PivotY : extendedAxle.PivotY;
                                    undercarriage.Axles.Add(extendedAxle);
                                }
                            }
                            Undercarriages.Add(undercarriage);
                            UndercarriageDistance += undercarriage.PivotY < 0 ? -undercarriage.PivotY : undercarriage.PivotY;
                        }
                    }
                }
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(StarorsCurrent);
            outf.Write(RotorsCurrent);
            outf.Write(AverageAxleSpeedMpS);
            outf.Write(FastestAxleSpeedMpS);
            foreach (Undercarriage uc in Undercarriages)
            {
                outf.Write(uc.Mass);
                outf.Write(uc.RotorsCurrent);
                outf.Write(uc.StatorsCurrent);
                foreach (ExtendedAxle ea in uc.Axles)
                {
                    ea.LocomotiveAxle.Save(outf);
                    foreach (ElectricMotor em in ea.ElectricMotors)
                    {
                        outf.Write(em.RotorCurrent);
                        outf.Write(em.StatorCurrent);
                    }
                    outf.Write(ea.ForceN);
                    outf.Write(ea.Mass);
                    outf.Write(ea.WheelSpeedMpS);
                }
            }
            outf.Write(OverridenControllerVolts);
        }

        protected bool wasRestored = false;
        public void Restore(BinaryReader inf)
        {
            wasRestored = true;
            if (!Locomotive.IsPlayerTrain)
                return;
            StarorsCurrent = inf.ReadSingle();
            RotorsCurrent = inf.ReadSingle();
            AverageAxleSpeedMpS = inf.ReadSingle();
            FastestAxleSpeedMpS = inf.ReadSingle();
            foreach (Undercarriage uc in Undercarriages)
            {
                uc.Mass = inf.ReadSingle();
                uc.RotorsCurrent = inf.ReadSingle();
                uc.StatorsCurrent = inf.ReadSingle();
                foreach (ExtendedAxle ea in uc.Axles)
                {
                    ea.LocomotiveAxle = new SubSystems.PowerTransmissions.Axle(inf);
                    foreach (ElectricMotor em in ea.ElectricMotors)
                    {
                        em.RotorCurrent = inf.ReadSingle();
                        em.StatorCurrent = inf.ReadSingle();
                    }
                    ea.ForceN = inf.ReadSingle();
                    ea.Mass = inf.ReadSingle();
                    ea.WheelSpeedMpS = inf.ReadSingle();
                }
            }
            OverridenControllerVolts = inf.ReadSingle();
        }

        public void Update(float elapsedClockSeconds)
        {
            if (extendedDynamicBrake == null)
                extendedDynamicBrake = new ExtendedDynamicBrake(Locomotive);
            extendedDynamicBrake.Update(elapsedClockSeconds);
            if (!Locomotive.IsLeadLocomotive())
            {
                if (Locomotive.CruiseControl != null)
                {
                    Locomotive.CruiseControl.SpeedRegMode = CruiseControl.SpeedRegulatorMode.Manual;
                }
            }
            if (!Locomotive.IsLeadLocomotive() && Locomotive.PowerOn && Locomotive.AcceptMUSignals)
                Locomotive.ControllerVolts = Locomotive.Train.ControllerVolts;
            if (!Locomotive.IsLeadLocomotive() && (!Locomotive.PowerOn || !Locomotive.AcceptMUSignals))
                Locomotive.ControllerVolts = OverridenControllerVolts = 0;
            if (!Locomotive.IsPlayerTrain)
                return;
            if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 4.98 && Locomotive.DynamicBrakePercent < 0.1f)
                Locomotive.ControllerVolts = 0;

            if (Locomotive.ControllerVolts > 0)
            {
                Locomotive.SetThrottlePercent(Locomotive.ControllerVolts * 10);
            }
            else if (Locomotive.ControllerVolts < 0)
            {
                Locomotive.SetThrottlePercent(0);
            }
            if (Locomotive.ControllerVolts == 0)
            {
                if (Locomotive.DynamicBrakePercent > 0)
                    Locomotive.ControllerVolts = -Locomotive.DynamicBrakePercent / 10;
                Locomotive.SetThrottlePercent(0);
                foreach (Undercarriage uc in Undercarriages)
                {
                    foreach (ExtendedAxle ea in uc.Axles)
                    {
                        ea.ForceN = 0;
                    }
                }
            }
            if (Locomotive.DynamicBrakePercent < 0.7 && Locomotive.ControllerVolts < 0)
            {
                Locomotive.DynamicBrakePercent = 0;
                Locomotive.ControllerVolts = 0;
            }
            if (UseControllerVolts && Locomotive.ControllerVolts < 0)
            {
                Locomotive.SetDynamicBrakePercent(-Locomotive.ControllerVolts * 10);
            }
            TotalCurrent = 0;
            StarorsCurrent = 0;
            RotorsCurrent = 0;
            FastestAxleSpeedMpS = 0;
            AverageAxleSpeedMpS = 0;
            TotalForceN = 0;
            TotalMaxForceN = 0;
            foreach (Undercarriage uc in Undercarriages)
            {
                uc.StatorsCurrent = 0;
                uc.RotorsCurrent = 0;
                uc.Mass = Locomotive.MassKG / Undercarriages.Count;
                foreach (ExtendedAxle ea in uc.Axles)
                {                    
                    if (ea.WheelSpeedMpS == 0)
                        ea.WheelSpeedMpS = Locomotive.WheelSpeedMpS;

                    ea.WheelSpeedMpS = Math.Abs(ea.WheelSpeedMpS);

                    AverageAxleSpeedMpS += ea.WheelSpeedMpS;

                    if (FastestAxleSpeedMpS < ea.WheelSpeedMpS)
                        FastestAxleSpeedMpS = ea.WheelSpeedMpS;

                    foreach (ElectricMotor em in ea.ElectricMotors)
                    {
                        ea.GetCorrectedMass(this);
                        ea.Update(NumMotors, elapsedClockSeconds, OverridenControllerVolts, UseControllerVolts);
                        TotalCurrent += em.RotorCurrent;
                        StarorsCurrent += em.StatorCurrent;
                        RotorsCurrent += em.RotorCurrent;
                        uc.StatorsCurrent += em.StatorCurrent;
                        uc.RotorsCurrent += em.RotorCurrent;
                    }
                    TotalForceN += ea.ForceN;
                    TotalMaxForceN += ea.maxForceN;
                }
            }
            wasRestored = false;
            AverageAxleSpeedMpS /= NumAxles;
            if (UseControllerVolts)
                Locomotive.MotiveForceN = Locomotive.TractiveForceN = TotalForceN;

            if (Locomotive.IsLeadLocomotive())
                Locomotive.Train.ControllerVolts = Locomotive.ControllerVolts;
            //Locomotive.Simulator.Confirmer.MSG(TotalForceN.ToString() + " " + Locomotive.TractiveForceN.ToString());
            //Locomotive.Simulator.Confirmer.MSG(Undercarriages[0].Axles[0].WheelSpeedMpS.ToString() + " " + Undercarriages[0].Axles[1].WheelSpeedMpS.ToString() + " " + Undercarriages[1].Axles[0].WheelSpeedMpS.ToString() + " " + Undercarriages[1].Axles[1].WheelSpeedMpS.ToString());
        }
    }

    public class Undercarriage
    {
        public int Id = -1;
        public int PivotY = 0;
        public int PivotZ = 0;
        public float FullAxleDistance = 0;
        public List<ExtendedAxle> Axles = new List<ExtendedAxle>();
        public float Mass = 0;
        public float RotorsCurrent = 0;
        public float StatorsCurrent = 0;
        public Undercarriage()
        {
        }
    }

    public class ExtendedAxle
    {
        public List<ElectricMotor> ElectricMotors = new List<ElectricMotor>();
        public int Id = -1;
        public int PivotY = 0;
        public int WheelDiameter = 0;
        public float Mass = 0;
        public float ForceN = 0;
        public float maxForceN = 0;
        public int NumMotors = 0;
        public float WheelSpeedMpS = 0;
        protected float reducedForceN = 0;
        MSTSLocomotive Locomotive = null;
        public SubSystems.PowerTransmissions.Axle LocomotiveAxle;
        public bool HaveSpeedometerSensor = false;
        public bool HaveTcsSensor = false;
        public float ForceNFiltered = 0;
        public List<float> ForceFilter = new List<float>();
        public float ForceNFilteredMotor = 0;
        public List<float> ForceFilterMotor = new List<float>();

        public ExtendedAxle(MSTSLocomotive loco)
        {
            Locomotive = loco;
            LocomotiveAxle = new SubSystems.PowerTransmissions.Axle();
            LocomotiveAxle.DriveType = SubSystems.PowerTransmissions.AxleDriveType.ForceDriven;
            LocomotiveAxle.StabilityCorrection = true;
            LocomotiveAxle.FilterMovingAverage.Size = Locomotive.Simulator.Settings.AdhesionMovingAverageFilterSize;
        }

        public void Update(int totalMotors, float elapsedClockSeconds, float overridenControllerVolts, bool usingControllerVolts)
        {
            LocomotiveAxle.DampingNs = Mass;
            LocomotiveAxle.FrictionN = Mass * 10;
            LocomotiveAxle.SlipWarningTresholdPercent = Locomotive.SlipWarningThresholdPercent;
            LocomotiveAxle.AdhesionK = Locomotive.AdhesionK;
            LocomotiveAxle.CurtiusKnifflerA = Locomotive.Curtius_KnifflerA;
            LocomotiveAxle.CurtiusKnifflerB = Locomotive.Curtius_KnifflerB;
            LocomotiveAxle.CurtiusKnifflerC = Locomotive.Curtius_KnifflerC;

            float axleCurrent = 0;
            float maxCurrent = 0;
            float motorMultiplier = totalMotors / ElectricMotors.Count;
            if (WheelSpeedMpS == 0 && Locomotive.WheelSpeedMpS != 0)
                WheelSpeedMpS = Locomotive.WheelSpeedMpS;

            WheelSpeedMpS = Math.Abs(WheelSpeedMpS);

            foreach (ElectricMotor em in ElectricMotors)
            {
                em.Update(em, WheelSpeedMpS, overridenControllerVolts);
                axleCurrent = axleCurrent + em.RotorCurrent;
                maxCurrent = maxCurrent + em.MaxRotorCurrent;
            }
            if ((Locomotive.TractiveForceCurves != null || Locomotive.TractiveForceCurvesAC != null || Locomotive.TractiveForceCurvesDC != null) && overridenControllerVolts > 0 && Locomotive.ControllerVolts > 0)
            {
                // Icik
                switch (Locomotive.SwitchingVoltageMode)
                {
                    case 0:
                        if (Locomotive.TractiveForceCurvesDC != null)
                        {
                            if (overridenControllerVolts != Locomotive.ControllerVolts)
                                maxForceN = Locomotive.TractiveForceCurvesDC.Get(Locomotive.CruiseControl.controllerVolts / 100 < overridenControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto? overridenControllerVolts / 10 : overridenControllerVolts / 10) : (usingControllerVolts ? 1 : overridenControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                            else
                                maxForceN = Locomotive.TractiveForceCurvesDC.Get(Locomotive.CruiseControl.controllerVolts / 100 < Locomotive.ControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.CruiseControl.controllerVolts / 10) : (usingControllerVolts ? 1 : Locomotive.ControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                        }
                        break;
                    case 1:
                        if (Locomotive.TractiveForceCurves != null)
                        {
                            if (overridenControllerVolts != Locomotive.ControllerVolts)
                                maxForceN = Locomotive.TractiveForceCurves.Get(Locomotive.CruiseControl.controllerVolts / 100 < overridenControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.CruiseControl.controllerVolts / 10) : (usingControllerVolts ? 1 : overridenControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                            else
                                maxForceN = Locomotive.TractiveForceCurves.Get(Locomotive.CruiseControl.controllerVolts / 100 < Locomotive.ControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.CruiseControl.controllerVolts / 10) : (usingControllerVolts ? 1 : Locomotive.ControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                        }
                        break;
                    case 2:
                        if (Locomotive.TractiveForceCurvesAC != null)
                        {
                            if (overridenControllerVolts != Locomotive.ControllerVolts)
                                maxForceN = Locomotive.TractiveForceCurvesAC.Get(Locomotive.CruiseControl.controllerVolts / 100 < overridenControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.CruiseControl.controllerVolts / 10) : (usingControllerVolts ? 1 : overridenControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                            else
                                maxForceN = Locomotive.TractiveForceCurvesAC.Get(Locomotive.CruiseControl.controllerVolts / 100 < Locomotive.ControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.CruiseControl.controllerVolts / 10) : (usingControllerVolts ? 1 : Locomotive.ControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                        }
                        break;
                }
                if (Locomotive.ControllerVolts > 0 && Locomotive.TractiveForceCurvesAC == null && Locomotive.TractiveForceCurvesDC == null && Locomotive.TractiveForceCurves != null)
                {
                    if (overridenControllerVolts != Locomotive.ControllerVolts)
                        maxForceN = Locomotive.TractiveForceCurves.Get(Locomotive.CruiseControl.controllerVolts / 100 < overridenControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.CruiseControl.controllerVolts / 10) : (usingControllerVolts ? 1 : overridenControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                    else
                        maxForceN = Locomotive.TractiveForceCurves.Get(Locomotive.CruiseControl.controllerVolts / 100 < Locomotive.ControllerVolts ? (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.CruiseControl.controllerVolts / 10) : (usingControllerVolts ? 1 : Locomotive.ControllerVolts / Locomotive.MaxControllerVolts), WheelSpeedMpS) / totalMotors;
                }
                maxForceN = maxForceN * Locomotive.UiPowerLose;
            }
            else if (Locomotive.ControllerVolts > 0)
            {
                if (overridenControllerVolts != Locomotive.ControllerVolts)
                    maxForceN = Locomotive.MaxForceN * (overridenControllerVolts / Locomotive.MaxControllerVolts) / totalMotors;
                else
                    maxForceN = Locomotive.MaxForceN * (Locomotive.ControllerVolts / Locomotive.MaxControllerVolts) / totalMotors;
                maxForceN = maxForceN * Locomotive.UiPowerLose;
            }
            else if ((Locomotive.DynamicBrakeForceCurves != null || Locomotive.DynamicBrakeForceCurvesAC != null || Locomotive.DynamicBrakeForceCurvesDC != null) && Locomotive.ControllerVolts < 0)
            {                
                // Icik
                switch (Locomotive.SwitchingVoltageMode)
                {
                    case 0:
                        if (Locomotive.DynamicBrakeForceCurvesDC != null)
                        {
                            maxForceN = -Locomotive.DynamicBrakeForceCurvesDC.Get(-Locomotive.ControllerVolts / Locomotive.MaxControllerVolts, WheelSpeedMpS) / totalMotors;
                        }
                        break;
                    case 1:
                        if (Locomotive.DynamicBrakeForceCurves != null)
                        {
                            maxForceN = -Locomotive.DynamicBrakeForceCurves.Get(-Locomotive.ControllerVolts / Locomotive.MaxControllerVolts, WheelSpeedMpS) / totalMotors;
                        }
                        break;
                    case 2:
                        if (Locomotive.DynamicBrakeForceCurvesAC != null)
                        {
                            maxForceN = -Locomotive.DynamicBrakeForceCurvesAC.Get(-Locomotive.ControllerVolts / Locomotive.MaxControllerVolts, WheelSpeedMpS) / totalMotors;
                        }
                        break;
                }
                maxForceN = maxForceN * Locomotive.UiPowerLose;
            }
            else if (Locomotive.ControllerVolts < 0)
            {
                maxForceN = -Locomotive.MaxForceN * (-Locomotive.ControllerVolts / Locomotive.MaxControllerVolts) / 4;
                maxForceN = maxForceN * Locomotive.UiPowerLose;
            }
            /*            if (Locomotive.CruiseControl != null)
                        {
                          if (((Locomotive.CruiseControl.SelectedMaxAccelerationPercent == 0 && Locomotive.SelectedMaxAccelerationStep == 0) || Locomotive.CruiseControl.SpeedSelMode == CruiseControl.SpeedSelectorMode.Neutral) && maxForceN > 0 && Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto)
                            {
                                maxForceN = 0;
                            }
                        }*/
            if (Locomotive.ControllerVolts == 0 && maxForceN > 0)
                maxForceN = 0;
            if (!Locomotive.PowerOn)
                maxForceN = 0;
            maxForceN += (reducedForceN * 2);
            if (usingControllerVolts)
            {
                ForceFilter.Add(ForceN);
                if (ForceFilter.Count >= 60)
                {
                    ForceFilter.RemoveAt(0);
                    ForceNFiltered = ForceFilter.Average();
                }
                ForceFilterMotor.Add(ForceN);
                if (ForceFilterMotor.Count >= 20)
                {
                    ForceFilterMotor.RemoveAt(0);
                    ForceNFilteredMotor = ForceFilterMotor.Average();
                }
            }
            else
            {
                ForceNFiltered = maxForceN;
                ForceNFilteredMotor = maxForceN;
            }
            LocomotiveAxle.InertiaKgm2 = 10000;
            LocomotiveAxle.AxleRevolutionsInt.MinStep = LocomotiveAxle.InertiaKgm2 / (Locomotive.MaxPowerW / totalMotors) / 5.0f;

            if (Locomotive.AdhesionEfficiencyKoef == 0) Locomotive.AdhesionEfficiencyKoef = 1.00f;
            LocomotiveAxle.AdhesionEfficiencyKoef = Locomotive.AdhesionEfficiencyKoef;

            LocomotiveAxle.BrakeRetardForceN = Locomotive.BrakeRetardForceN;

            if (!usingControllerVolts)
                ForceN = maxForceN;
            else if (Locomotive.CruiseControl.controllerVolts > 0)
            {
                ForceN = (Locomotive.MaxForceN / 4) * (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto ? Locomotive.CruiseControl.controllerVolts / 100 : Locomotive.ControllerVolts / 10);
                ForceN += (reducedForceN * 2);
                if (ForceN > maxForceN)
                    ForceN = maxForceN;
            }
            else if (Locomotive.CruiseControl.controllerVolts < 0)
            {
                ForceN = -Locomotive.DynamicBrakeForceN / 4;
                ForceN += (reducedForceN * 2);
            }
            else if (Locomotive.DynamicBrakeForceN  > 0)
            {
                ForceN = -Locomotive.DynamicBrakeForceN / 4;
                ForceN += (reducedForceN * 2);
            }

            if (Locomotive.AbsSpeedMpS == 0 && ForceN < 0)
                ForceN = 0;

            LocomotiveAxle.AxleWeightN = 9.81f * Mass * 1000;   //will be computed each time considering the tilting
            LocomotiveAxle.DriveForceN = ForceN;  //Total force applied to wheels
            LocomotiveAxle.TrainSpeedMpS = Locomotive.SpeedMpS < 0 ? -Locomotive.SpeedMpS : Locomotive.SpeedMpS;
            LocomotiveAxle.AdhesionConditions = Locomotive.LocomotiveAxle.AdhesionConditions;//Set the train speed of the axle model
            LocomotiveAxle.Update(elapsedClockSeconds);         //Main updater of the axle model
            WheelSpeedMpS = LocomotiveAxle.AxleSpeedMpS;
            if (Locomotive.CruiseControl != null)
            {
                if ((Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto && Locomotive.ThrottlePercent > 0 && ((Locomotive.CruiseControl.SelectedSpeedMpS - Locomotive.AbsSpeedMpS) > 0.05f) && Locomotive.SelectedMaxAccelerationStep > 0 && Locomotive.CruiseControl.PreciseSpeedControl) || (ForceN < 0 && Locomotive.CruiseControl.PreciseSpeedControl))
                {
                    float mMass = Mass;
                    Mass *= 1000;
                    float addMass = (Locomotive.MassKG / totalMotors) - Mass;
                    Mass += addMass * 2;
                    reducedForceN = -((WheelSpeedMpS - (Locomotive.AbsSpeedMpS + 0.1f)) * (Mass / 1000)) * 1000;
                    Mass = mMass;
                }
                else
                    reducedForceN = 0;
            }
            //if (WheelSpeedMpS == 0 && Locomotive.WheelSpeedMpS > 0)
            //    WheelSpeedMpS = Locomotive.WheelSpeedMpS;
        }

        public void GetCorrectedMass(ExtendedPhysics extendedPhysics)
        {
            float bp = extendedPhysics.Undercarriages[0].FullAxleDistance / 1000f;
            float b = extendedPhysics.UndercarriageDistance / 1000f;
            float hp = extendedPhysics.Undercarriages[0].PivotZ / 1000f;
            float h = extendedPhysics.CouplerDistanceFromTrack / 1000f;
            float hg = extendedPhysics.CenterOfGravityDistanceFromTrack / 1000f;
            float M = Locomotive.MassKG / 1000;
            float Gs = M * 9.81f;
            float Gp = Gs / extendedPhysics.Undercarriages.Count;
            float Gn = Gs / extendedPhysics.NumAxles;
            float Fh = 0;
            foreach (TrainCar tc in Locomotive.Train.Cars)
            {
                if (tc == Locomotive)
                {
                    Fh = -tc.CouplerForceU / 1000;
                    break;
                }
            }
            float Fo = Locomotive.FrictionForceN / 1000;
            if (WheelSpeedMpS == 0)
                Fo = 0;
            float Fok = Fo + Fh;
            float T1, T2, T3, T4;
            T1 = T2 = T3 = T4 = Fok / 4;
            float T12 = T1 + T2;
            float T34 = T3 + T4;

            float N1 = -(T1 + T2) * hp / bp;
            float N2 = (T1 + T2) * hp / bp;
            float N3 = -(T3 + T4) * hp / bp;
            float N4 = (T3 + T4) * hp / bp;

            float N12_0 = -Fh * (hp - h) / b;
            float N12_1 = -(T12 + T34) * hg / b;
            float N12 = N12_0 + N12_1;
            float N34_0 = Fh * (hp - h) / b;
            float N34_1 = (T12 + T34) * hg / b;
            float N34 = N34_0 + N34_1;

            float Mass1 = ((Gp + N12) / 2 + N1) / 9.81f;
            float Mass2 = ((Gp + N12) / 2 + N2) / 9.81f;
            float Mass3 = ((Gp + N34) / 2 + N3) / 9.81f;
            float Mass4 = ((Gp + N34) / 2 + N4) / 9.81f;

            //            Locomotive.Simulator.Confirmer.MSG(Mass1.ToString() + " " + Mass2.ToString() + " " + Mass3.ToString() + " " + Mass4.ToString());
//            Locomotive.Simulator.Confirmer.MSG(MpS.ToKpH(extendedPhysics.Undercarriages[0].Axles[0].WheelSpeedMpS).ToString() + " " + MpS.ToKpH(extendedPhysics.Undercarriages[0].Axles[1].WheelSpeedMpS).ToString() + " " + MpS.ToKpH(extendedPhysics.Undercarriages[1].Axles[0].WheelSpeedMpS).ToString() + " " + MpS.ToKpH(extendedPhysics.Undercarriages[1].Axles[1].WheelSpeedMpS).ToString());

            foreach (Undercarriage uc in extendedPhysics.Undercarriages)
            {
                foreach(ExtendedAxle ea in uc.Axles)
                {
                    if (ea.Id == 0)
                        ea.Mass = Mass1;
                    if (ea.Id == 1)
                        ea.Mass = Mass2;
                    if (ea.Id == 2)
                        ea.Mass = Mass3;
                    if (ea.Id == 3)
                        ea.Mass = Mass4;

                }
            }
        }
    }

    public class ElectricMotor
    {
        public int Id = -1;
        public float StatorCurrent = 0;
        public float RotorCurrent = 0;
        public float MinRotorCurrent = 0;
        public float MaxRotorCurrent = 0;
        public float MaxStatorCurrent = 0;
        public float MaxNegativeRotorCurrent = 0;
        public float MaxNegativeStatorCurrent = 0;
        public int InSeriesWith = 1;
        public float MaxRpm = 0;
        public float GearRatio = 1;
        public float ErrorCoefficient = 1;
        public float RPM = 0;
        public float MaxControllerVolts = 0;
        MSTSLocomotive Locomotive = null;
        public ElectricMotor(MSTSLocomotive loco)
        {
            Locomotive = loco;
        }
        public void Update(ElectricMotor Motor, float axleSpeed, float overridenControllerVolts)
        {
            if (axleSpeed < 0)
                axleSpeed = -axleSpeed;
            if (!Locomotive.PowerOn && Locomotive.ControllerVolts > 0)
                Locomotive.ControllerVolts = 0;
            if (Locomotive.ControllerVolts > 0)
            {
                float currentRotor = Motor.MaxRotorCurrent;
                if (Locomotive.ExtendedArmCurrent != null)
                {
                    if (overridenControllerVolts != Locomotive.ControllerVolts)
                        Motor.RotorCurrent = Locomotive.ExtendedArmCurrent.Get(overridenControllerVolts / MaxControllerVolts, axleSpeed) / 2 * Motor.ErrorCoefficient;
                    else
                        Motor.RotorCurrent = Locomotive.ExtendedArmCurrent.Get(Locomotive.ControllerVolts / MaxControllerVolts, axleSpeed) / 2 * Motor.ErrorCoefficient;
                }
                else
                {
                    if (overridenControllerVolts != Locomotive.ControllerVolts)
                        Motor.RotorCurrent = (((overridenControllerVolts / MaxControllerVolts) * currentRotor) + Motor.MinRotorCurrent) * Motor.ErrorCoefficient;
                    else
                        Motor.RotorCurrent = (((Locomotive.ControllerVolts / MaxControllerVolts) * currentRotor) + Motor.MinRotorCurrent) * Motor.ErrorCoefficient;
                    Motor.RotorCurrent = Motor.RotorCurrent - (axleSpeed / Locomotive.MaxSpeedMpS);
                }
                if (Locomotive.ControllerVolts == 0)
                    Motor.RotorCurrent = 0;
                if (Locomotive.ExtendedExcitationCurrent != null)
                {
                    if (overridenControllerVolts != Locomotive.ControllerVolts)
                        Motor.StatorCurrent = Locomotive.ExtendedExcitationCurrent.Get(overridenControllerVolts / MaxControllerVolts, axleSpeed) / 4;
                    else
                        Motor.StatorCurrent = Locomotive.ExtendedExcitationCurrent.Get(Locomotive.ControllerVolts / MaxControllerVolts, axleSpeed) / 4;
                }
                else
                    Motor.StatorCurrent = Motor.RotorCurrent / Motor.MaxStatorCurrent;
            }
            else if (Locomotive.ControllerVolts < 0)
            {
                float currentRotor = Motor.MaxNegativeRotorCurrent;
                if (Locomotive.ExtendedArmEDBCurrent != null)
                {
                    Motor.RotorCurrent = Locomotive.ExtendedArmEDBCurrent.Get(-Locomotive.ControllerVolts / MaxControllerVolts, axleSpeed) / 2;
                }
                else
                {
                    Motor.RotorCurrent = (((Locomotive.ControllerVolts / MaxControllerVolts) * currentRotor) + Motor.MinRotorCurrent) * Motor.ErrorCoefficient;
                    Motor.RotorCurrent = Motor.RotorCurrent - (axleSpeed / Locomotive.MaxSpeedMpS);
                }
                if (Locomotive.ControllerVolts == 0)
                    Motor.RotorCurrent = 0;
                if (Locomotive.ExtendedExcitationEDBCurrent != null)
                    Motor.StatorCurrent = Locomotive.ExtendedExcitationEDBCurrent.Get(-Locomotive.ControllerVolts / MaxControllerVolts, axleSpeed) / 4;
                else
                    Motor.StatorCurrent = Motor.RotorCurrent / Motor.MaxNegativeStatorCurrent;
            }
            else if (Locomotive.ControllerVolts == 0)
            {
                Motor.RotorCurrent = 0;
                Motor.StatorCurrent = 0;
            }
            //Locomotive.Simulator.Confirmer.MSG("R: " + Motor.RotorCurrent.ToString() + " S: " + Motor.StatorCurrent.ToString());
        }
    }

    public class RotorCurrentTable
    {
        public RotorCurrentTable()
        {

        }
    }


    public class ExtendedDynamicBrake
    {
        public MSTSLocomotive Locomotive = null;
        public float TimeToEngage = 0;
        public float MotiveForce = 0;
        public float Percent = 0;

        public ExtendedDynamicBrake(MSTSLocomotive loco)
        {
            Locomotive = loco;
        }

        public bool Increasing = false;
        public bool Decreasing = false;
        protected float timeToActivate = 0;
        public void Update(float elapsedClockSeconds)
        {
            return;
            if (Increasing)
            {
                if (Percent == 0 && TimeToEngage > 0)
                {
                    timeToActivate += elapsedClockSeconds;
                    if (timeToActivate < TimeToEngage)
                        return;
                }
                float step = 100 / Locomotive.DynamicBrakeFullRangeIncreaseTimeSeconds;
                step *= elapsedClockSeconds;
                Percent += step;
                if (Percent > 100)
                    Percent = 100;
                Locomotive.Simulator.Confirmer.Information("Dynamická brzda " + Math.Round(Percent, 0).ToString());
            }
            if (Decreasing)
            {
                float step = 100 / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                step *= elapsedClockSeconds;
                Percent -= step;
                if (Percent < 0)
                    Percent = 0;
                Locomotive.Simulator.Confirmer.Information("Dynamická brzda " + Math.Round(Percent, 0).ToString());
            }

            // compute EDB force
            if (Locomotive.RouteVoltageV == 3000)
            {
                MotiveForce = Locomotive.DynamicBrakeForceCurvesDC.Get(Percent / 100, Locomotive.extendedPhysics.AverageAxleSpeedMpS);
            }
            if (Locomotive.RouteVoltageV == 25000)
            {
                MotiveForce = Locomotive.DynamicBrakeForceCurvesAC.Get(Percent / 100, Locomotive.extendedPhysics.AverageAxleSpeedMpS);
            }

            if (Locomotive.ThrottlePercent == 0)
            {
                Locomotive.ControllerVolts = -(Percent / 10);
            }
        }

        public void StartIncrease()
        {
            Increasing = true;
        }

        public void StopIncrease()
        {
            Increasing = false;
        }

        public void StartDecrease()
        {
            Decreasing = true;
        }

        public void StopDecrease()
        {
            Decreasing = false;
        }
    }
}
