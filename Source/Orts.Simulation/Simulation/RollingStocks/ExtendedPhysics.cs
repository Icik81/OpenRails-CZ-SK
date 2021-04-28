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
        public float MaxControllerVolts = 10;
        public MSTSLocomotive Locomotive = null;
        public int NumMotors = 0;
        public int NumAxles = 0;
        public float StarorsCurrent = 0;
        public float RotorsCurrent = 0;
        public float TotalCurrent = 0;
        public float TotalMaxCurrent = 0;
        public float UndercarriageDistance = 0;
        public float CouplerDistanceFromTrack = 0;
        public float CenterOfGravityDistanceFromTrack = 0;
        public ExtendedPhysics(MSTSLocomotive loco)
        {
            Locomotive = loco;
        }

        public void Parse(string path)
        {
            XmlDocument document = new XmlDocument();
            document.Load(path);
            foreach (XmlNode node in document.ChildNodes)
            {
                if (node.Name.ToLower() == "extendedphysics")
                {
                    foreach (XmlNode main in node.ChildNodes)
                    {
                        if (main.Name.ToLower() == "maxcontrollervolts")
                            MaxControllerVolts = float.Parse(main.InnerText);
                        if (main.Name.ToLower() == "couplerdistancefromtrack")
                            CouplerDistanceFromTrack = float.Parse(main.InnerText);
                        if (main.Name.ToLower() == "centerofgravitydistancefromtrack")
                            CenterOfGravityDistanceFromTrack = float.Parse(main.InnerText);
                        if (main.Name.ToLower() == "undercarriage")
                        {
                            Undercarriage undercarriage = new Undercarriage();
                            foreach (XmlNode undercarriageNode in main.ChildNodes)
                            {
                                if (undercarriageNode.Name.ToLower() == "id")
                                    undercarriage.Id = int.Parse(undercarriageNode.InnerText);
                                if (undercarriageNode.Name.ToLower() == "pivoty")
                                    undercarriage.PivotY = int.Parse(undercarriageNode.InnerText);
                                if (undercarriageNode.Name.ToLower() == "pivotz")
                                    undercarriage.PivotZ = int.Parse(undercarriageNode.InnerText);
                                if (undercarriageNode.Name.ToLower() == "axle")
                                {
                                    ExtendedAxle extendedAxle = new ExtendedAxle(Locomotive);
                                    foreach (XmlNode axleNode in undercarriageNode.ChildNodes)
                                    {
                                        if (axleNode.Name.ToLower() == "id")
                                            extendedAxle.Id = int.Parse(axleNode.InnerText);
                                        if (axleNode.Name.ToLower() == "pivoty")
                                            extendedAxle.PivotY = int.Parse(axleNode.InnerText);
                                        if (axleNode.Name.ToLower() == "wheeldiameter")
                                            extendedAxle.WheelDiameter = int.Parse(axleNode.InnerText);
                                        if (axleNode.Name.ToLower() == "electricmotor")
                                        {
                                            ElectricMotor electricMotor = new ElectricMotor(Locomotive);
                                            electricMotor.MaxControllerVolts = MaxControllerVolts;
                                            NumMotors++;
                                            extendedAxle.NumMotors++;
                                            foreach (XmlNode motorNode in axleNode.ChildNodes)
                                            {
                                                if (motorNode.Name.ToLower() == "id")
                                                    electricMotor.Id = int.Parse(motorNode.InnerText);
                                                if (motorNode.Name.ToLower() == "statorinserieswith")
                                                    electricMotor.InSeriesWith = int.Parse(motorNode.InnerText);
                                                if (motorNode.Name.ToLower() == "maxstatorcurrenta")
                                                    electricMotor.MaxStatorCurrent = float.Parse(motorNode.InnerText);
                                                if (motorNode.Name.ToLower() == "maxrotorcurrenta")
                                                {
                                                    electricMotor.MaxRotorCurrent = int.Parse(motorNode.InnerText);
                                                    TotalMaxCurrent += electricMotor.MaxRotorCurrent;
                                                }
                                                if (motorNode.Name.ToLower() == "minrotorcurrenta")
                                                    electricMotor.MinRotorCurrent = int.Parse(motorNode.InnerText);
                                                if (motorNode.Name.ToLower() == "errorcoefficient")
                                                    electricMotor.ErrorCoefficient = float.Parse(motorNode.InnerText);
                                                if (motorNode.Name.ToLower() == "gearratio")
                                                    electricMotor.GearRatio = float.Parse(motorNode.InnerText);
                                                if (motorNode.Name.ToLower() == "fullspeedrangecurrentdrop")
                                                    electricMotor.FullSpeedRangeCurrentDrop = int.Parse(motorNode.InnerText);
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

        public void Update(float elapsedClockSeconds)
        {
            if (Locomotive.ControllerVolts >= 0)
            {
                Locomotive.SetThrottlePercent((TotalCurrent / TotalMaxCurrent) * 100);
            }
            else if (Locomotive.ControllerVolts < 0)
            {
                Locomotive.SetThrottlePercent(0);
                Locomotive.ControllerVolts = 0;
            }
            TotalCurrent = 0;
            StarorsCurrent = 0;
            RotorsCurrent = 0;
            foreach (Undercarriage uc in Undercarriages)
            {
                uc.StatorsCurrent = 0;
                uc.RotorsCurrent = 0;
                uc.Mass = Locomotive.MassKG / Undercarriages.Count;
                foreach (ExtendedAxle ea in uc.Axles)
                {
                    ea.Mass = uc.Mass / uc.Axles.Count;
                    foreach (ElectricMotor em in ea.ElectricMotors)
                    {
                        ea.GetCorrectedMass(this);
                        ea.Update(NumMotors, elapsedClockSeconds);
                        TotalCurrent += em.RotorCurrent;
                        StarorsCurrent += em.StatorCurrent;
                        RotorsCurrent += em.RotorCurrent;
                        uc.StatorsCurrent += em.StatorCurrent;
                        uc.RotorsCurrent += em.RotorCurrent;
                    }
                }
            }
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
        public int NumMotors = 0;
        public float WheelSpeedMpS = 0;
        MSTSLocomotive Locomotive = null;
        public SubSystems.PowerTransmissions.Axle LocomotiveAxle;
        public ExtendedAxle(MSTSLocomotive loco)
        {
            Locomotive = loco;
            LocomotiveAxle = new SubSystems.PowerTransmissions.Axle();
            LocomotiveAxle.DriveType = SubSystems.PowerTransmissions.AxleDriveType.ForceDriven;
            LocomotiveAxle.StabilityCorrection = true;
            LocomotiveAxle.FilterMovingAverage.Size = Locomotive.Simulator.Settings.AdhesionMovingAverageFilterSize;
        }

        public void Update(int totalMotors, float elapsedClockSeconds)
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
            foreach (ElectricMotor em in ElectricMotors)
            {
                em.Update(em, Locomotive.WheelSpeedMpS);
                axleCurrent = axleCurrent + em.RotorCurrent;
                maxCurrent = maxCurrent + em.MaxRotorCurrent;
            }
            if (Locomotive.TractiveForceCurves != null)
            {
                float t = (axleCurrent / maxCurrent) / totalMotors;
                ForceN = Locomotive.TractiveForceCurves.Get(t, Locomotive.LocomotiveAxle.AxleSpeedMpS);
            }
            else // TODO bez tabulek!
            {

            }

            LocomotiveAxle.InertiaKgm2 = 10000;
            LocomotiveAxle.AxleRevolutionsInt.MinStep = LocomotiveAxle.InertiaKgm2 / (Locomotive.MaxPowerW / totalMotors) / 5.0f;

            if (Locomotive.AdhesionEfficiencyKoef == 0) Locomotive.AdhesionEfficiencyKoef = 1.00f;
            LocomotiveAxle.AdhesionEfficiencyKoef = Locomotive.AdhesionEfficiencyKoef;

            LocomotiveAxle.BrakeRetardForceN = Locomotive.BrakeRetardForceN;

            LocomotiveAxle.AxleWeightN = 9.81f * Mass * 1000;   //will be computed each time considering the tilting
            LocomotiveAxle.DriveForceN = ForceN;  //Total force applied to wheels
            LocomotiveAxle.TrainSpeedMpS = Locomotive.SpeedMpS;
            LocomotiveAxle.AdhesionConditions = Locomotive.LocomotiveAxle.AdhesionConditions;//Set the train speed of the axle model
            LocomotiveAxle.Update(elapsedClockSeconds);         //Main updater of the axle model
            WheelSpeedMpS = LocomotiveAxle.AxleSpeedMpS;
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
        public float FullSpeedRangeCurrentDrop = 0;
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
        public void Update(ElectricMotor Motor, float axleSpeed)
        {
            float currentRotor = Motor.MaxRotorCurrent - Motor.MinRotorCurrent;
            Motor.RotorCurrent = (((Locomotive.ControllerVolts / MaxControllerVolts) * currentRotor) + Motor.MinRotorCurrent) * Motor.ErrorCoefficient;
            Motor.RotorCurrent = Motor.RotorCurrent - ((axleSpeed / Locomotive.MaxSpeedMpS) * Motor.FullSpeedRangeCurrentDrop);
            if (Locomotive.ControllerVolts == 0)
                Motor.RotorCurrent = 0;
            Motor.StatorCurrent = Motor.RotorCurrent / Motor.MaxStatorCurrent;
            //Locomotive.Simulator.Confirmer.MSG("R: " + Motor.RotorCurrent.ToString() + " S: " + Motor.StatorCurrent.ToString());
        }

    }
}
