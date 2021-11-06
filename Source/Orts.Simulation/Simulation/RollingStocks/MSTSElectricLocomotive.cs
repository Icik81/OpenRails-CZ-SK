// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

/* ELECTRIC LOCOMOTIVE CLASSES
 * 
 * The locomotive is represented by two classes:
 *  ...Simulator - defines the behaviour, ie physics, motion, power generated etc
 *  ...Viewer - defines the appearance in a 3D viewer
 * 
 * The ElectricLocomotive classes add to the basic behaviour provided by:
 *  LocomotiveSimulator - provides for movement, throttle controls, direction controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
 * 
 */

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Event = Orts.Common.Event;
using System;

namespace Orts.Simulation.RollingStocks
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Adds pantograph control to the basic LocomotiveSimulator functionality
    /// </summary>
    public class MSTSElectricLocomotive : MSTSLocomotive
    {
        public ScriptedElectricPowerSupply PowerSupply;

        // Icik        
        public bool PantographDown = true;
        public double PantographCriticalVoltage;       
        public float VoltageSprung = 1.0f;
        public float TimeCriticalVoltage = 0;
        public float TimeCriticalVoltage0 = 0;
        public float Delta0 = 0;
        public float Delta1 = 0;
        public float Delta2 = 0;
        public float Step0;
        public float Step1;
        public double MaxLineVoltage0;
        public double MaxLineVoltage1 = 1;
        public double MaxLineVoltage2;
        public float PantographVoltageV;
        public float VoltageAC;
        public float VoltageDC;
        public float preVoltageAC;
        public float preVoltageDC;
        public bool LocoSwitchACDC;
        int T = 0;
        int T_CB = 0;
        float Induktion = 0;
        float TInduktion = 0;
        float TRouteVoltageV_1 = 0;

        float TPowerOnAC = 0;
        float TPowerOnDC = 0;
        float TCircuitBreakerAC = 0;
        float TCircuitBreakerDC = 0;
        float TPanto1AC = 0;
        float TPanto2AC = 0;
        float TPanto1DC = 0;
        float TPanto2DC = 0;
        float TCompressorAC = 0;
        float TCompressorDC = 0;

        bool HVClosed = false;
        float T_HVOpen = 0;
        float T_HVClosed = 0;
        float[] T_PantoUp = new float[4];

        float PreDataVoltageAC;
        float PreDataVoltageDC;
        float PreDataVoltage;
        bool UpdateTimeEnable;

        public bool PantographFaultByVoltageChange;
        public bool PantographFaultByNotLowering;
        float FaultByPlayerPenaltyTime;
        float AIPantoUPTime;
        float AIPantoDownGenerate;
        bool AIPantoDown;
        bool AIPantoDownStop;

        public MSTSElectricLocomotive(Simulator simulator, string wagFile) :
            base(simulator, wagFile)
        {
            PowerSupply = new ScriptedElectricPowerSupply(this);
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowerondelay":
                case "engine(ortsauxpowerondelay":
                case "engine(ortspowersupply":
                case "engine(ortscircuitbreaker":
                case "engine(ortscircuitbreakerclosingdelay":
                    PowerSupply.Parse(lowercasetoken, stf);
                    break;

                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void Copy(MSTSWagon copy)
        {
            base.Copy(copy);  // each derived level initializes its own variables

            // for example
            //CabSoundFileName = locoCopy.CabSoundFileName;
            //CVFFileName = locoCopy.CVFFileName;
            MSTSElectricLocomotive locoCopy = (MSTSElectricLocomotive)copy;

            PowerSupply.Copy(locoCopy.PowerSupply);
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            PowerSupply.Save(outf);
            outf.Write(CurrentLocomotiveSteamHeatBoilerWaterCapacityL);
            outf.Write(PantographDown);
            outf.Write(CircuitBreakerOn);
            outf.Write(PantographCriticalVoltage);
            outf.Write(PowerOnFilter);
            outf.Write(RouteVoltageV);
            outf.Write(TPowerOnAC);
            outf.Write(TPowerOnDC);
            outf.Write(TCircuitBreakerAC);
            outf.Write(TCircuitBreakerDC);
            outf.Write(TPanto1AC);
            outf.Write(TPanto2AC);
            outf.Write(TPanto1DC);
            outf.Write(TPanto2DC);
            outf.Write(TCompressorAC);
            outf.Write(TCompressorDC);
            outf.Write(HVClosed);
            outf.Write(PantographFaultByVoltageChange);
            outf.Write(PantographFaultByNotLowering);
            outf.Write(AIPantoUPTime);
            outf.Write(AIPantoDown);
            outf.Write(AIPantoDownGenerate);
            outf.Write(FaultByPlayerPenaltyTime);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            PowerSupply.Restore(inf);
            CurrentLocomotiveSteamHeatBoilerWaterCapacityL = inf.ReadSingle();
            PantographDown = inf.ReadBoolean();
            CircuitBreakerOn = inf.ReadBoolean();
            PantographCriticalVoltage = inf.ReadDouble();
            PowerOnFilter = inf.ReadSingle();
            RouteVoltageV = inf.ReadSingle();
            TPowerOnAC = inf.ReadSingle();
            TPowerOnDC = inf.ReadSingle();
            TCircuitBreakerAC = inf.ReadSingle();
            TCircuitBreakerDC = inf.ReadSingle();
            TPanto1AC = inf.ReadSingle();
            TPanto2AC = inf.ReadSingle();
            TPanto1DC = inf.ReadSingle();
            TPanto2DC = inf.ReadSingle();
            TCompressorAC = inf.ReadSingle();
            TCompressorDC = inf.ReadSingle();
            HVClosed = inf.ReadBoolean();
            PantographFaultByVoltageChange = inf.ReadBoolean();
            PantographFaultByNotLowering = inf.ReadBoolean();
            AIPantoUPTime = inf.ReadSingle();
            AIPantoDown = inf.ReadBoolean();
            AIPantoDownGenerate = inf.ReadSingle();
            FaultByPlayerPenaltyTime = inf.ReadSingle();
            base.Restore(inf);
        }

        public override void Initialize()
        {
            if (!PowerSupply.RouteElectrified)
                Trace.WriteLine("Warning: The route is not electrified. Electric driven trains will not run!");

            PowerSupply.Initialize();

            base.Initialize();

            // If DrvWheelWeight is not in ENG file, then calculate drivewheel weight freom FoA

            if (DrvWheelWeightKg == 0) // if DrvWheelWeightKg not in ENG file.
            {
                DrvWheelWeightKg = MassKG; // set Drive wheel weight to total wagon mass if not in ENG file
            }

            // Initialise water level in steam heat boiler
            if (CurrentLocomotiveSteamHeatBoilerWaterCapacityL == 0 && IsSteamHeatFitted)
            {
                if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
                {
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL = MaximumSteamHeatBoilerWaterTankCapacityL;
                }
                else
                {
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL = L.FromGUK(800.0f);
                }
            }

            // Icik
            if (RouteVoltageV == 0)
                RouteVoltageV = (float)Simulator.TRK.Tr_RouteFile.MaxLineVoltage;

        }

        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        /// 
        public override void InitializeMoving()
        {
            base.InitializeMoving();
            WheelSpeedMpS = SpeedMpS;
            DynamicBrakePercent = -1;
            ThrottleController.SetValue(Train.MUThrottlePercent / 100);

            Pantographs.InitializeMoving();
            PowerSupply.InitializeMoving();
        }

        // Jindrich
        // výpočet odběru pro AI
        protected float AICalculatedSteps = 0;
        protected void AIConsumption()
        {
            if (AICalculatedSteps < 10)
            {
                AICalculatedSteps++;
                return;
            }
            AICalculatedSteps = 0;
            MultiSystemEngine = true;
            if (Simulator.powerSupplyStations.Count == 0)
            {
                PowerSupplyStation pss = new PowerSupplyStation();
                pss.PowerSystem = RouteVoltageV == 25000 ? 1 : 0;
                Simulator.powerSupplyStations.Add(pss);
            }    
            float watts = MaxForceN * (ThrottlePercent / 100)  * 1f + (MaxForceN * (ThrottlePercent / 100)) * AbsSpeedMpS;
            watts += PowerReductionByHeating0 + PowerReductionByAuxEquipment0;
            if ((Flipped || Direction == Direction.Reverse) && watts < 0)
                watts = -watts;
            if (PantographVoltageV < 10)
                PantographVoltageV = RouteVoltageV;
            if (watts < 0 && !RecuperationAvailable)
                watts = 0;
            if (PantographVoltageV > 1)
                Amps = watts / PantographVoltageV;
            else
                Amps = 0;

            if (Amps > 1500)
                Amps = 1500;

            if (float.IsNaN(Amps))
                Amps = 0;
            if (RouteVoltageV == 25000)
                Amps *= 1.3f; //přidáme malinko jalovinu
            if (float.IsInfinity(Amps))
                Amps = 0;
            if (Amps < 0)
                Amps = 0;
            int powerSys = -1;
            int markerVoltage = 0;
            VoltageChangeMarker marker;

            float dist = DistanceToPowerSupplyStationM(RouteVoltageV == 3000 ? 0 : 1, out myStation);
            float distToMarker = DistanceToVoltageMarkerM(out markerVoltage, out marker);

            if (myStation == null && prevPss != null)
            {
                myStation = prevPss;
                powerSys = myStation.PowerSystem;
            }
            else
                powerSys = myStation.PowerSystem;
            if (powerSys == 0)
            {
                RouteVoltageV = 3000;
            }
            else if (powerSys == 1)
            {
                RouteVoltageV = 25000;
            }
            if (powerSys == -1)
            {
                RouteVoltageV = 0;
            }
            if (distToMarker < dist)
            {
                RouteVoltageV = markerVoltage;
            }

            if (RouteVoltageV == 0)
            {
                RouteVoltageV = 1;
                Amps = 0;
            }

            foreach (PowerSupplyStation pss in Simulator.powerSupplyStations)
            {
                if (pss.Longitude == myStation.Longitude)
                    myStation = pss;
            }
            if (prevPss == null)
            {
                if (myStation == null)
                {
                    myStation = new PowerSupplyStation();
                    myStation.PowerSystem = 1;
                }

                prevPss = myStation;
                myStation.Consuptors.Add(this);
                myStation.Update();
            }
            if (prevPss != myStation)
            {
                prevPss.Consuptors.Remove(this);
                prevPss.Update();
                myStation.Consuptors.Add(this);
                myStation.Update();
                prevPss = myStation;
            }
            else
                myStation.Update();
            float wireResistance = RouteVoltageV == 3000 ? dist / 50000 : dist / 5000;

            float newVoltage = wireResistance * myStation.TotalAmps;
            float distDrop = RouteVoltageV == 3000 ? dist / 50 : dist / 20;
            float volts = -newVoltage - distDrop;

            if (RouteVoltageV == 3000)
            {
                volts += 400; // max 3.4kV poblíž měničky
                Induktion = 0;
            }
            if (RouteVoltageV == 25000)
                volts += 2000; // max 27kV poblíž napaječky
        }

        // Icik
        // Podpěťová ochrana a blokace pantografů
        protected PowerSupplyStation prevPss = null;
        public float Amps;
        public PowerSupplyStation myStation = null;
        protected int markerVoltage = 0;
        protected void UnderVoltageProtection(float elapsedClockSeconds)
        {
            if (Simulator.Paused)
                return;

            if (RouteVoltageV == 0)
                RouteVoltageV = 25000;

            bool my = IsPlayerTrain;
            if (!my)
            {
                AIConsumption();
                return;
            }

            // výpočet napětí dle proudu a odporu k napaječce
            float watts = TractiveForceN > 0 ? (TractiveForceN * 1f + TractiveForceN * AbsSpeedMpS) : 0;
            if ((Flipped || Direction == Direction.Reverse) && watts < 0)
                watts = -watts;

            watts += PowerReductionByHeating0 + PowerReductionByAuxEquipment0;

            if (watts < 0 && !RecuperationAvailable)
                watts = 0;
            if (!my && PantographVoltageV < 2)
                PantographVoltageV = RouteVoltageV;
            if (PantographVoltageV > 1)
                Amps = watts / PantographVoltageV;
            else
                Amps = 0;

            if (float.IsNaN(Amps))
                Amps = 0;
            if (RouteVoltageV == 25000)
                Amps *= 1.3f; //přidáme malinko jalovinu
            if (float.IsInfinity(Amps))
                Amps = 0;
            if (Amps < 0)
                Amps = 0;
            int powerSys = -1;
            VoltageChangeMarker marker;

            float dist = DistanceToPowerSupplyStationM(RouteVoltageV == 3000 ? 0 : 1, out myStation);
            float distToMarker = DistanceToVoltageMarkerM(out markerVoltage, out marker);

            if (dist < 200)
                dist = 200;

            if (Simulator.powerSupplyStations.Count == 0)
            {
                PowerSupplyStation pss = new PowerSupplyStation();
                pss.IsDefault = true;

                Simulator.powerSupplyStations.Add(pss);
                if (Simulator.powerSupplyStations[0].IsDefault)
                {
                    Simulator.powerSupplyStations[0].PowerSystem = RouteVoltageV == 25000 ? 1 : 0;
                    myStation = new PowerSupplyStation();
                    myStation.PowerSystem = RouteVoltageV == 25000 ? 1 : 0;
                }
            }
            else if (Simulator.powerSupplyStations.Count == 1)
            {
                if (Simulator.powerSupplyStations[0].IsDefault)
                {
                    Simulator.powerSupplyStations[0].PowerSystem = RouteVoltageV == 25000 ? 1 : 0;
                    myStation = new PowerSupplyStation();
                    myStation.PowerSystem = RouteVoltageV == 25000 ? 1 : 0;
                }
            }

            if (myStation == null && prevPss != null)
            {
                myStation = prevPss;
                powerSys = myStation.PowerSystem;
            }
            else if (myStation != null)
                powerSys = myStation.PowerSystem;
            else
            {
                myStation = Simulator.powerSupplyStations[0];
                powerSys = RouteVoltageV == 3000 ? 0 : 1;
            }
            if (powerSys == 0)
            {
                RouteVoltageV = 3000;
            }
            else if (powerSys == 1)
            {
                RouteVoltageV = 25000;
            }
            if (powerSys == -1)
            {
                RouteVoltageV = 0;
            }
            if (distToMarker < dist)
            {
                RouteVoltageV = markerVoltage;
            }

            if (RouteVoltageV == 0)
            {
                RouteVoltageV = 1;
                Amps = 0;
            }

            foreach (PowerSupplyStation pss in Simulator.powerSupplyStations)
            {
                if (pss.Longitude == myStation.Longitude)
                    myStation = pss;
            }
            if (prevPss == null)
            {
                if (myStation == null)
                {
                    myStation = new PowerSupplyStation();
                    myStation.PowerSystem = 1;
                }

                prevPss = myStation;
                myStation.Consuptors.Add(this);
                myStation.Update();
            }
            if (prevPss != myStation)
            {
                prevPss.Consuptors.Remove(this);
                prevPss.Update();
                myStation.Consuptors.Add(this);
                myStation.Update();
                prevPss = myStation;
            }
            else
                myStation.Update();
            float wireResistance = RouteVoltageV == 3000 ? dist / 50000 : dist / 5000;

            float newVoltage = wireResistance * myStation.TotalAmps;
            float distDrop = RouteVoltageV == 3000 ? dist / 50 : dist / 20;
            float volts = -newVoltage - distDrop;

            if (RouteVoltageV == 3000)
            {
                volts += 400; // max 3.4kV poblíž měničky
                Induktion = 0;
            }
            if (RouteVoltageV == 25000)
                volts += 2000; // max 27kV poblíž napaječky

            //VoltageFilter = false;
            //RouteVoltageV = 1;
            //Induktion = 0;
            //LocomotivePowerVoltage = 25000;

            // Penalizace hráče při chybě přes přechody a změně voltáže na trati
            FaultByPlayer_RouteVoltage(elapsedClockSeconds);
            
            // Výpočet napětí v drátech
            if (IsPlayerTrain)
            {
                if (RouteVoltageV > 1)
                {
                    Simulator.TRK.Tr_RouteFile.MaxLineVoltage = RouteVoltageV * VoltageSprung + volts;
                    MaxLineVoltage0 = RouteVoltageV + volts;
                }
                if (RouteVoltageV == 1)
                    PowerSupply.PantographVoltageV -= (float)MaxLineVoltage0 * 1.5f * elapsedClockSeconds;

                PantographVoltageV = (float)Math.Round(PantographVoltageV);
                PowerSupply.PantographVoltageV = (float)Math.Round(PowerSupply.PantographVoltageV);
                MaxLineVoltage0 = (float)Math.Round(MaxLineVoltage0);
            }
            if (PantographVoltageV < 1)
                PantographVoltageV = 1;
            if (PowerSupply.PantographVoltageV < 1)
                PowerSupply.PantographVoltageV = 1;

            decimal kW = Math.Round((decimal)((RouteVoltageV * myStation.TotalAmps) / 1000), 2);
            if (IsPlayerTrain && Simulator.SuperUser)
                Simulator.Confirmer.MSG("Mark U: " + markerVoltage.ToString() + "; Mark dist: " + Math.Round(distToMarker, 0).ToString() + "; Supl dist: " + Math.Round(dist, 0).ToString() + "; My panto U: " + Math.Round(PantographVoltageV, 0).ToString() + "; Supl I: " + Math.Round(myStation.TotalAmps, 0).ToString() + "; Supl #locos: " + myStation.Consuptors.Count.ToString() + "; Supl kW: " + kW.ToString());

            if (IsPlayerTrain)
            {
                // Pokud je loko AC tak napětí pantografu sleduje napětí v drátech
                if ((SwitchingVoltageMode_OffAC || LocomotivePowerVoltage == 25000) && RouteVoltageV == 25000) // zohlednění indukce
                {
                    if (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up)
                        PantographVoltageV = PowerSupply.PantographVoltageV;
                }
                if ((SwitchingVoltageMode_OffAC || LocomotivePowerVoltage == 25000) && RouteVoltageV == 1) // bez indukce
                {
                    if (PantographVoltageV > PowerSupply.PantographVoltageV && Induktion > 0) // pokud je indukce, klesne pozvolna než se dorovná
                        PantographVoltageV -= 100;
                    else
                    {
                        PantographVoltageV = PowerSupply.PantographVoltageV;
                        Induktion = 0;
                    }
                }

                if ((SwitchingVoltageMode_OffDC || LocomotivePowerVoltage == 3000) && VoltageFilter && RouteVoltageV == 25000)
                {
                    if (CircuitBreakerOn && PowerSupply.PantographVoltageV < PantographVoltageV && PowerSupply.PantographVoltageV < MaxLineVoltage0 && MaxLineVoltage1 > 1)
                    {
                        if (T == 0)
                            MaxLineVoltage2 = PantographVoltageV;
                        T = 1;
                        PantographVoltageV = (float)MaxLineVoltage2;
                        MaxLineVoltage2 -= (0.02f * MaxLineVoltage0) * elapsedClockSeconds; // 2% z napětí v troleji za 1s
                        if (MaxLineVoltage2 < (0.1f * MaxLineVoltage0)) MaxLineVoltage2 = (0.1f * MaxLineVoltage0);
                    }
                }

                // Pokud má lokomotiva napěťový filtr a napětí tratě je stejnosměrné 3kV
                if (VoltageFilter && (RouteVoltageV == 3000 || RouteVoltageV == 1))
                {
                    // Zákmit ručky do plna
                    if (CircuitBreakerOn && PowerSupply.PantographVoltageV > PantographVoltageV && MaxLineVoltage1 > 1)
                    {
                        MaxLineVoltage1 = PowerSupply.PantographVoltageV;
                        PantographVoltageV = PowerSupply.PantographVoltageV;
                        T = 0;
                    }
                    // Rychlost padání napětí při vypnutém HV
                    else
                    if ((RouteVoltageV == 3000 || RouteVoltageV == 1) && !CircuitBreakerOn && PantographVoltageV > PowerSupply.PantographVoltageV && PowerSupply.PantographVoltageV < MaxLineVoltage0 && MaxLineVoltage1 > 1)
                    {
                        if (T == 0)
                            MaxLineVoltage2 = PantographVoltageV;
                        T = 1;
                        PantographVoltageV = (float)MaxLineVoltage2;
                        MaxLineVoltage2 -= MaxLineVoltage0 * 1.5f * elapsedClockSeconds;
                        if (MaxLineVoltage2 < 1)
                        {
                            MaxLineVoltage1 = 1;
                            MaxLineVoltage2 = 1;
                            PantographVoltageV = 1;
                            T = 0;
                        }
                    }
                    // Rychlost padání napětí při vypnutém sběrači
                    else
                    if ((RouteVoltageV == 3000 || RouteVoltageV == 1) && CircuitBreakerOn && PowerSupply.PantographVoltageV < PantographVoltageV && PowerSupply.PantographVoltageV < MaxLineVoltage0 && MaxLineVoltage1 > 1)
                    {
                        if (T == 0)
                            MaxLineVoltage2 = PantographVoltageV;
                        T = 1;
                        PantographVoltageV = (float)MaxLineVoltage2;
                        MaxLineVoltage2 -= (0.02f * MaxLineVoltage0) * elapsedClockSeconds; // 2% z napětí v troleji za 1s
                        if (MaxLineVoltage2 < (0.1f * MaxLineVoltage0)) MaxLineVoltage2 = (0.1f * MaxLineVoltage0);
                    }
                    else if (RouteVoltageV > 1)
                        PantographVoltageV = PowerSupply.PantographVoltageV;

                    // Uchová informaci o napětí pro filtr
                    if (RouteVoltageV > 1 && MaxLineVoltage1 == 1)
                        MaxLineVoltage1 = PowerSupply.PantographVoltageV;
                }
                else
                {
                    if (PowerSupply.PantographVoltageV > Induktion * 1000)
                        PantographVoltageV = PowerSupply.PantographVoltageV;
                }


                // Indukce z trolejového vedení pro střídavé napájení 25kV
                if (RouteVoltageV == 25000 && LocomotivePowerVoltage != 3000 
                    && (Pantographs[1].State == PantographState.Down || Pantographs[1].State == PantographState.Raising || Pantographs[1].State == PantographState.Lowering)
                    && (Pantographs[2].State == PantographState.Down || Pantographs[2].State == PantographState.Raising || Pantographs[2].State == PantographState.Lowering))
                {
                    if (TInduktion == 0)
                        Induktion = Simulator.Random.Next(1, 3);
                    TInduktion = 1;
                    if (PantographVoltageV < Induktion * 1000)
                        PantographVoltageV += 10;
                }                
                if (RouteVoltageV != 25000)
                    TInduktion = 0;


                if (!UpdateTimeEnable)
                {
                    // Zákmit na voltmetru            
                    if (PowerSupply.PantographVoltageV < 2)
                    {
                        VoltageSprung = 1.5f;
                        Step1 = 0.40f;
                        TimeCriticalVoltage = 0;
                    }
                    Step1 = Step1 - elapsedClockSeconds;
                    if (Step1 < 0) Step1 = 0;
                    if ((VoltageSprung > 1.0f && Step1 == 0 && PowerSupply.PantographVoltageV > MaxLineVoltage0)) VoltageSprung = 1.0f;
                }

                // Kritická mez napětí pro podnapěťovku
                if (RouteVoltageV == 25000)
                    PantographCriticalVoltage = 19000;
                if (RouteVoltageV == 3000)
                    PantographCriticalVoltage = 1900;
                      
                if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed) 
                    CircuitBreakerOn = true;
                else CircuitBreakerOn = false;

                // Plynulé klesání ručičky ampermetru při vynulování Throttle
                if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open && DoesPowerLossResetControls && LocalThrottlePercent > 0)
                    StartThrottleToZero(0.0f);

                // Úbytek výkonu v závislosti na napětí
                UiPowerLose = MathHelper.Clamp(PantographVoltageV / RouteVoltageV, 0, 2);
                if (!PowerOn && EDBIndependent && PowerOnFilter > 1)
                    UiPowerLose = 1;

                PantographCriticalVoltage = (int)PantographCriticalVoltage;
                PowerSupply.PantographVoltageV = (int)PowerSupply.PantographVoltageV;
                LocalThrottlePercent = (int)ThrottlePercent;
                LocalDynamicBrakePercent = (int)DynamicBrakePercent;
                if (PowerSupply.PantographVoltageV < 1) PowerSupply.PantographVoltageV = 1;
                if (LocalThrottlePercent < 0) LocalThrottlePercent = 0;
                if (LocalDynamicBrakePercent < 0) LocalDynamicBrakePercent = 0;

                // Vyvolání penalizace hráče při nestažení pantografu na úseku s 0V
                if (RouteVoltageV == 1 && (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up))
                {
                    PantographFaultByNotLowering = true;
                }

                // Shodí HV při stažení sběračů při navoleném výkonu
                if (LocalThrottlePercent != 0 && Pantograph4Switch == 0)                    
                    HVOff = true;

                // Použije se pro kontrolku při ztrátě napětí v pantografech
                if (RouteVoltageV == 25000 && PowerSupply.PantographVoltageV < 19000
                    || RouteVoltageV == 3000 && PowerSupply.PantographVoltageV < 1900
                    || RouteVoltageV == 1 && PowerSupply.PantographVoltageV < 1900)
                    CheckPowerLoss = true;
                else CheckPowerLoss = false;

                // Použije se pro text na displeji "NEDVIHAJ ZBERAČ"
                if (Pantograph4Switch == 0 && PantographVoltageV >= 2000)                
                    DontRaisePanto = true;                
                else 
                if (Pantograph4Switch != 0 || PantographVoltageV < 2000)                
                    DontRaisePanto = false;                

                // Blokování pantografu u jednosystémových lokomotiv při vypnutém HV
                if (!MultiSystemEngine)
                {
                    // Definice default provozního napájení lokomotivy 25kV
                    if (LocomotivePowerVoltage == 0) LocomotivePowerVoltage = 25000; //Default pro lokomotivy bez udání napětí

                    // Stisknutí hříbku pro přerušení napájení, vypne HV a shodí sběrače
                    if (BreakPowerButton)
                    {
                        HVOff = true;
                        if (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up)
                            SignalEvent(PowerSupplyEvent.LowerPantograph);
                    }

                    // Test napětí v troleji pro jednosystémové lokomotivy
                    if (PantographVoltageV == MaxLineVoltage0)
                    {
                        if (PantographVoltageV > 1.5f * LocomotivePowerVoltage)
                        {
                            HVOff = true;
                            PantographFaultByVoltageChange = true;
                        }
                        else
                        if (PantographVoltageV < 0.5f * LocomotivePowerVoltage)
                        {
                            HVOff = true;
                            PantographFaultByVoltageChange = true;
                        }
                    }

                    if (!CircuitBreakerOn && PantographDown)
                    {
                        Pantographs[1].PantographsBlocked = true;
                        Pantographs[2].PantographsBlocked = true;
                    }
                    if (!CircuitBreakerOn && Pantographs[1].PantographsBlocked == false && Pantographs[2].PantographsBlocked == false)
                    {
                        if (Pantograph4Switch != 0)
                            SignalEvent(PowerSupplyEvent.LowerPantograph);
                        PantographDown = true;
                    }
                    if (CircuitBreakerOn)
                    {
                        Pantographs[1].PantographsBlocked = false;
                        Pantographs[2].PantographsBlocked = false;
                        PantographDown = false;

                        if (!EDBIndependent)
                        {
                            // Shodí HV při nulovém napětí a manipulaci s kontrolérem a EDB
                            if ((PowerSupply.PantographVoltageV == 1 && LocalThrottlePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                            || (PowerSupply.PantographVoltageV == 1 && LocalDynamicBrakePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed))
                            {

                                // Shodí HV při poklesu napětí v troleji a nastaveném výkonu (podpěťová ochrana)
                                if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && LocalThrottlePercent > 0.1)
                                    HVOff = true;
                            }

                            if (CruiseControl != null)
                                if (PowerSupply.PantographVoltageV == 1
                                    && CruiseControl.ForceThrottleAndDynamicBrake != 0
                                    && CruiseControl.ForceThrottleAndDynamicBrake != 1
                                    && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                                {
                                    HVOff = true;
                                }

                            // Shodí HV při poklesu napětí v troleji a nastaveném výkonu (podpěťová ochrana)
                            if (PowerSupply.PantographVoltageV > 1)
                            {
                                if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && LocalThrottlePercent != 0)
                                {
                                    HVOff = true;
                                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                                }
                                if (CruiseControl != null)
                                    if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && CruiseControl.ForceThrottleAndDynamicBrake > 0)
                                    {
                                        CruiseControl.ForceThrottleAndDynamicBrake = 0;
                                        CruiseControl.controllerVolts = 0;
                                        SubSystems.CruiseControl.SpeedSelectorMode prevMode = CruiseControl.SpeedSelMode;
                                        CruiseControl.SpeedSelMode = SubSystems.CruiseControl.SpeedSelectorMode.Neutral;
                                        CruiseControl.Update(elapsedClockSeconds, AbsWheelSpeedMpS);
                                        CruiseControl.SpeedSelMode = prevMode;
                                        CruiseControl.DynamicBrakePriority = false;
                                        TractiveForceN = 0;

                                        HVOff = true;
                                        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                                    }
                            }
                        }

                        if (EDBIndependent)
                        {
                            // Shodí HV při nulovém napětí a manipulaci s kontrolérem
                            if (PowerSupply.PantographVoltageV == 1 && LocalThrottlePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                            {
                                // Shodí HV při poklesu napětí v troleji a nastaveném výkonu (podpěťová ochrana)
                                if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && LocalThrottlePercent > 0.1)
                                    HVOff = true;
                            }

                            if (CruiseControl != null)
                                if (PowerSupply.PantographVoltageV == 1
                                    && CruiseControl.ForceThrottleAndDynamicBrake != 0
                                    && CruiseControl.ForceThrottleAndDynamicBrake != 1
                                    && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                                {
                                    HVOff = true;
                                }

                            // Shodí HV při poklesu napětí v troleji a nastaveném výkonu (podpěťová ochrana)
                            if (PowerSupply.PantographVoltageV > 1)
                            {
                                if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && LocalThrottlePercent != 0)
                                {
                                    HVOff = true;
                                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                                }
                                if (CruiseControl != null)
                                    if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && CruiseControl.ForceThrottleAndDynamicBrake > 0)
                                    {
                                        CruiseControl.ForceThrottleAndDynamicBrake = 0;
                                        CruiseControl.controllerVolts = 0;
                                        SubSystems.CruiseControl.SpeedSelectorMode prevMode = CruiseControl.SpeedSelMode;
                                        CruiseControl.SpeedSelMode = SubSystems.CruiseControl.SpeedSelectorMode.Neutral;
                                        CruiseControl.Update(elapsedClockSeconds, AbsWheelSpeedMpS);
                                        CruiseControl.SpeedSelMode = prevMode;
                                        CruiseControl.DynamicBrakePriority = false;
                                        TractiveForceN = 0;
                                        HVOff = true;
                                        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                                    }
                            }
                        }
                    }
                }

                // Blokování HV u vícesystémových lokomotiv při malém napětí                                                
                if (MultiSystemEngine)
                {
                    // Stisknutí hříbku pro přerušení napájení, vypne HV a shodí sběrače
                    if (BreakPowerButton)
                    {
                        HVOff = true;
                        if (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up)
                            SignalEvent(PowerSupplyEvent.LowerPantograph);
                    }

                    // Nedovolí zapnout HV, pokud není napětí v drátech 
                    if (RouteVoltageV == 1 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                        HVOff = true;

                    // Nastavení AC při zapnutém HV  a pantografy nahoře přejede do úseku 3kV - shodí HV
                    if (CircuitBreakerOn && SwitchingVoltageMode_OffAC && RouteVoltageV == 3000
                        && (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up))
                    {
                        HVOff = true;
                        PantographFaultByVoltageChange = true;
                    }

                    // Při zapnutém HV přejede do beznapěťového úseku - shodí HV po pár sekundách
                    if (CircuitBreakerOn && (RouteVoltageV == 1 || (Pantographs[1].State == PantographState.Down && Pantographs[2].State == PantographState.Down)))
                    {
                        TRouteVoltageV_1 += elapsedClockSeconds;
                        if (!VoltageFilter && TRouteVoltageV_1 > Simulator.Random.Next(2, 4))
                        {
                            if (PowerReductionByHeating0 + PowerReductionByAuxEquipmentEng > 0)
                                HVOff = true;
                            TRouteVoltageV_1 = 0;
                        }
                        if (VoltageFilter && TRouteVoltageV_1 > Simulator.Random.Next(2, 4))
                        {
                            if (PowerReductionByHeating0 + PowerReductionByAuxEquipmentEng > 0)
                                HVOff = true;
                            TRouteVoltageV_1 = 0;
                        }
                    }

                    // Nastavení DC při zapnutém HV a pantografy nahoře přejede do úseku 25kV - shodí HV
                    if (CircuitBreakerOn && SwitchingVoltageMode_OffDC && RouteVoltageV == 25000
                        && (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up))
                    {
                        HVOff = true;
                        PantographFaultByVoltageChange = true;
                    }


                    if (LocoSwitchACDC
                        && (SwitchingVoltageMode == 1)
                        && (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed || PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing))
                        HVOff = true;

                    if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed && PowerOn)
                        T_CB = 1;

                    //if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open && T_CB == 1)
                    //    SwitchingVoltageMode = 1;

                    if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open)
                        T_CB = 0;

                    if (RouteVoltageV == 3000 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing && VoltageDC < 1500)
                        HVOff = true;

                    if (RouteVoltageV == 25000 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing && VoltageAC < 15000)
                        HVOff = true;

                    if (SwitchingVoltageMode_OffAC && RouteVoltageV == 3000
                        && (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing || PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                        && (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up))
                        HVOff = true;

                    if (SwitchingVoltageMode_OffDC && RouteVoltageV == 25000
                        && (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing || PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                        && (Pantographs[1].State == PantographState.Up || Pantographs[2].State == PantographState.Up))
                        HVOff = true;


                    // Test napětí v troleji stanoví napěťovou soustavu
                    if (MaxLineVoltage0 > 3500)
                    {
                        VoltageAC = PantographVoltageV; // Střídavá napěťová soustava 25kV
                        if (VoltageDC > 0)
                            VoltageDC -= VoltageDC * 1.5f * elapsedClockSeconds;
                        if (VoltageDC < 0) VoltageDC = 0;
                    }
                    else
                    {
                        VoltageDC = PantographVoltageV; // Stejnosměrná napěťová soustava 3kV
                        if (VoltageAC > 0)
                            VoltageAC -= VoltageAC * 1.5f * elapsedClockSeconds;
                        if (VoltageAC < 0) VoltageAC = 0;
                    }


                    Pantographs[1].PantographsBlocked = false;
                    Pantographs[2].PantographsBlocked = false;

                    if (!EDBIndependent && GameTimeFlow > 1)
                    {
                        // Blokuje zapnutí HV při staženém sběrači a nebo navoleném výkonu a EDB
                        if ((PowerSupply.PantographVoltageV == 1 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                        || (LocalThrottlePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                        || (LocalDynamicBrakePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                        || (PowerSupply.PantographVoltageV == 1 && LocalThrottlePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                        || (PowerSupply.PantographVoltageV == 1 && LocalDynamicBrakePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed))
                        {
                            if (DynamicBrakePercent > 0)
                            {
                                LocalDynamicBrakePercent = 0;
                                SetDynamicBrakePercent(0);
                                DynamicBrakeChangeActiveState(false);
                            }
                            if (RouteVoltageV != 3000 && !SwitchingVoltageMode_OffDC)
                                HVOff = true;
                        }

                        if (CruiseControl != null)
                            if ((PowerSupply.PantographVoltageV == 1 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                            || (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing && CruiseControl.ForceThrottleAndDynamicBrake != 0 && CruiseControl.ForceThrottleAndDynamicBrake != 1)
                            || (PowerSupply.PantographVoltageV == 1 && CruiseControl.ForceThrottleAndDynamicBrake != 0 && CruiseControl.ForceThrottleAndDynamicBrake != 1 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed))
                            {
                                CruiseControl.ForceThrottleAndDynamicBrake = 0;
                                CruiseControl.controllerVolts = 0;
                                SubSystems.CruiseControl.SpeedSelectorMode prevMode = CruiseControl.SpeedSelMode;
                                CruiseControl.SpeedSelMode = SubSystems.CruiseControl.SpeedSelectorMode.Neutral;
                                CruiseControl.Update(elapsedClockSeconds, AbsWheelSpeedMpS);
                                CruiseControl.SpeedSelMode = prevMode;
                                CruiseControl.DynamicBrakePriority = false;
                                TractiveForceN = 0;
                                if (RouteVoltageV != 3000 && !SwitchingVoltageMode_OffDC)
                                    HVOff = true;
                            }

                        //Shodí HV při poklesu napětí v troleji a nastaveném výkonu a EDB(podpěťová ochrana)
                        if (PowerSupply.PantographVoltageV > 1)
                        {
                            if ((PowerSupply.PantographVoltageV < PantographCriticalVoltage && LocalThrottlePercent != 0)
                                || (PowerSupply.PantographVoltageV < PantographCriticalVoltage && LocalDynamicBrakePercent != 0))
                            {
                                if (DynamicBrakePercent > 0)
                                {
                                    LocalDynamicBrakePercent = 0;
                                    SetDynamicBrakePercent(0);
                                    DynamicBrakeChangeActiveState(false);
                                }
                                HVOff = true;
                                Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                            }

                            if (CruiseControl != null)
                                if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && CruiseControl.ForceThrottleAndDynamicBrake != 0 && CruiseControl.ForceThrottleAndDynamicBrake != 1)
                                {
                                    CruiseControl.ForceThrottleAndDynamicBrake = 0;
                                    CruiseControl.controllerVolts = 0;
                                    SubSystems.CruiseControl.SpeedSelectorMode prevMode = CruiseControl.SpeedSelMode;
                                    CruiseControl.SpeedSelMode = SubSystems.CruiseControl.SpeedSelectorMode.Neutral;
                                    CruiseControl.Update(elapsedClockSeconds, AbsWheelSpeedMpS);
                                    CruiseControl.SpeedSelMode = prevMode;
                                    CruiseControl.DynamicBrakePriority = false;
                                    TractiveForceN = 0;
                                    HVOff = true;
                                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                                }
                        }
                    }

                    if (EDBIndependent && GameTimeFlow > 1)
                    {
                        // Blokuje zapnutí HV při staženém sběrači a nebo navoleném výkonu
                        if ((PowerSupply.PantographVoltageV == 1 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                        || (LocalThrottlePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                        || (PowerSupply.PantographVoltageV == 1 && LocalThrottlePercent != 0 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed))
                        {
                            if (RouteVoltageV != 3000 && !SwitchingVoltageMode_OffDC)
                                HVOff = true;
                        }

                        if (CruiseControl != null)
                            if ((PowerSupply.PantographVoltageV == 1 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                            || (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing && CruiseControl.ForceThrottleAndDynamicBrake != 0 && CruiseControl.ForceThrottleAndDynamicBrake != 1)
                            || (PowerSupply.PantographVoltageV == 1 && CruiseControl.ForceThrottleAndDynamicBrake != 0 && CruiseControl.ForceThrottleAndDynamicBrake != 1 && PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed))
                            {
                                if (DynamicBrakePercent > 0)
                                {
                                    LocalDynamicBrakePercent = 0;
                                    SetDynamicBrakePercent(0);
                                    DynamicBrakeChangeActiveState(false);
                                }
                                if (RouteVoltageV != 3000 && !SwitchingVoltageMode_OffDC)
                                    HVOff = true;
                            }

                        if (PowerSupply.PantographVoltageV > 1)
                        {
                            // Shodí HV při poklesu napětí v troleji a nastaveném výkonu (podpěťová ochrana)
                            if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && LocalThrottlePercent != 0)
                            {
                                if (DynamicBrakePercent > 0)
                                {
                                    LocalDynamicBrakePercent = 0;
                                    SetDynamicBrakePercent(0);
                                    DynamicBrakeChangeActiveState(false);
                                }
                                HVOff = true;
                                Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                            }

                            if (CruiseControl != null)
                                if (PowerSupply.PantographVoltageV < PantographCriticalVoltage && CruiseControl.ForceThrottleAndDynamicBrake != 0 && CruiseControl.ForceThrottleAndDynamicBrake != 1)
                                {
                                    CruiseControl.ForceThrottleAndDynamicBrake = 0;
                                    CruiseControl.controllerVolts = 0;
                                    SubSystems.CruiseControl.SpeedSelectorMode prevMode = CruiseControl.SpeedSelMode;
                                    CruiseControl.SpeedSelMode = SubSystems.CruiseControl.SpeedSelectorMode.Neutral;
                                    CruiseControl.Update(elapsedClockSeconds, AbsWheelSpeedMpS);
                                    CruiseControl.SpeedSelMode = prevMode;
                                    CruiseControl.DynamicBrakePriority = false;
                                    TractiveForceN = 0;
                                    HVOff = true;
                                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Zásah podpěťové ochrany!"));
                                }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This function updates periodically the states and physical variables of the locomotive's power supply.
        /// </summary>
        protected override void UpdatePowerSupply(float elapsedClockSeconds)
        {
            // Icik                      
            if (HVOff)
            {
                HVOff = false;
                SignalEvent(PowerSupplyEvent.OpenCircuitBreaker);
            }
            if (HVOn)
            {
                HVOn = false;
                SignalEvent(PowerSupplyEvent.CloseCircuitBreaker);
            }
          
            PowerSupply.Update(elapsedClockSeconds);
            
            if (PowerSupply.CircuitBreaker != null && IsPlayerTrain)
            {
                if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open && DoesPowerLossResetControls)
                {                    
                    ControllerVolts = 0;
                    ThrottleController.SetPercent(0);
                    if (SpeedMpS > 0)
                    {
                        DynamicBrakePercent = 0;
                        DynamicBrakeChangeActiveState(false);
                    }
                }
            }

            // Icik            
            SetAIPantoDown(elapsedClockSeconds);

            UnderVoltageProtection(elapsedClockSeconds);
            
            if (IsPlayerTrain)
            {                
                HVPressedTesting(elapsedClockSeconds);
                AuxAirConsumption(elapsedClockSeconds);
                
                // Nastavení pro plně oživenou lokomotivu
                if (LocoReadyToGo && BrakeSystem.IsAirFull)
                {                    
                    Battery = true;
                    PowerKey = true;
                    ActiveStation = UsingRearCab ? DriverStation.Station2 : DriverStation.Station1;
                    CompressorSwitch = 2;
                    CompressorSwitch2 = 1;
                    CompressorMode_OffAuto = true;
                    CompressorMode2_OffAuto = true;

                    if (MultiSystemEngine && RouteVoltageV != 1)
                    {
                        Pantograph4Switch = 1;
                        if (RouteVoltageV == 3000)
                            HV5Switch = 2;
                        if (RouteVoltageV == 25000)
                            HV5Switch = 4;
                        if (GameTimeFlow > 5)
                            HVOn = true;
                        if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                            LocoReadyToGo = false;
                    }
                    if (!MultiSystemEngine)
                    {
                        HVOn = true;                                              
                        if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                        {
                            Pantograph4Switch = 1;
                            LocoReadyToGo = false;
                        }
                    }
                    // Mirel
                    Mirel.Test1 = true;
                    Mirel.Test2 = true;
                    Mirel.Test3 = true;
                    Mirel.Test4 = true;
                    Mirel.Test5 = true;
                    Mirel.Test6 = true;
                    Mirel.Test7 = true;
                    Mirel.initTest = SubSystems.Mirel.InitTest.Passed;
                    Mirel.BlueLight = true;
                    Mirel.driveMode = Mirel.selectedDriveMode = SubSystems.Mirel.DriveMode.Normal;
                    Mirel.MaxSelectedSpeed = Mirel.MirelMaximumSpeed = MpS.ToKpH(MaxSpeedMpS);
                    // LS90
                    Mirel.ls90tested = true;
                    Mirel.Ls90power = SubSystems.Mirel.LS90power.On;                    

                    // Směr + ARR
                    Direction = Direction.Forward;
                    if (CruiseControl != null && CruiseControl.Equipped)
                    {
                        CruiseControl.SpeedRegMode = SubSystems.CruiseControl.SpeedRegulatorMode.Auto;
                        CruiseControl.SelectedSpeedMpS = MpS.FromKpH(40);
                        CruiseControl.SpeedSelMode = SubSystems.CruiseControl.SpeedSelectorMode.Parking;
                    }
                }                   
            }
        }

        // Icik
        // AI stahuje pantografy na úseku bez napětí a když nemá akci
        protected void SetAIPantoDown(float elapsedClockSeconds)
        {
            // Po zastavení AI vlaku složí pantograf
            if (!IsPlayerTrain && GameTimeCyklus == 10)
            {
                if ((Train as AITrain).MovementState == AITrain.AI_MOVEMENT_STATE.AI_STATIC
                || (Train as AITrain).MovementState == AITrain.AI_MOVEMENT_STATE.STOPPED
                || (Train as AITrain).MovementState == AITrain.AI_MOVEMENT_STATE.SUSPENDED
                || (Train as AITrain).MovementState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION)
                    AIPantoDownStop = true;
                else
                    AIPantoDownStop = false;
            }

            // AI stahuje pantografy a nechá je dole minimálně 20s
            if (AIPantoDown)
            {
                if (AIPantoDownGenerate == 0)
                    AIPantoDownGenerate = Simulator.Random.Next(20, 30);
                AIPantoUPTime += elapsedClockSeconds;
                if (AIPantoUPTime > AIPantoDownGenerate)
                {
                    AIPantoDown = false;
                    AIPantoUPTime = 0;
                    AIPantoDownGenerate = 0;
                }
            }

            if (!IsPlayerTrain && GameTimeCyklus == 10)
            {
                for (int i = 1; i <= Pantographs.Count; i++)
                {
                    switch (Pantographs[i].State)
                    {
                        case PantographState.Raising:
                        case PantographState.Lowering:
                        case PantographState.Down:
                            if (RouteVoltageV != 1 && !AIPantoDown && !AIPantoDownStop)
                            {
                                Pantographs[i].PantographsUpBlocked = false;
                            }
                            break;
                        case PantographState.Up:
                            if (RouteVoltageV == 1 || AIPantoDownStop)
                            {
                                Pantographs[i].PantographsUpBlocked = true;
                                SignalEvent(PowerSupplyEvent.LowerPantograph);
                                if (!AIPantoDownStop)
                                    AIPantoDown = true;
                            }
                            break;
                    }
                }
            }
        }

        // Penalizace hráče
        protected void FaultByPlayer_RouteVoltage(float elapsedClockSeconds)
        {
            if (PantographFaultByNotLowering)
            {
                RouteVoltageV = 1;
                FaultByPlayerPenaltyTime += elapsedClockSeconds;
                Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Urval si zdvihnutým pantografem trolej!"));
                if (FaultByPlayerPenaltyTime > 30) // Potrestání hráče čekáním 30s
                {
                    PantographFaultByNotLowering = false;
                    FaultByPlayerPenaltyTime = 0;
                }
            }

            if (PantographFaultByVoltageChange)
            {
                RouteVoltageV = 1;
                FaultByPlayerPenaltyTime += elapsedClockSeconds;
                Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Poškodil si zdvihnutým pantografem lokomotivu!"));
                if (FaultByPlayerPenaltyTime > 30) // Potrestání hráče čekáním 30s
                {
                    PantographFaultByNotLowering = false;
                    FaultByPlayerPenaltyTime = 0;
                }
            }
        }


        // Testování času stiknutého HV
        protected void HVPressedTesting(float elapsedClockSeconds)
        {
            HVCanOn = false;            
            if (HV2Enable && !HVPressedTest)
                HVPressedTime = 0;

            if (HV5Enable && (!HVPressedTestDC && !HVPressedTestAC || HV5Switch != 1 && HV5Switch != 5))
                HVPressedTime = 0;

            if (HVPressedTest)
                HVPressedTime += elapsedClockSeconds;
            if (HVPressedTestDC)
                HVPressedTime += elapsedClockSeconds;                        
            if (HVPressedTestAC)
                HVPressedTime += elapsedClockSeconds;            

            if (HVPressedTime > 0.9f && HVPressedTime < 1.1f) // 1s na podržení polohy pro zapnutí HV
                HVCanOn = true;            
        }

        // Výpočet spotřeby vzduchu, jímka pomocného kompresoru
        protected void AuxAirConsumption(float elapsedClockSeconds)
        {
            // Spotřeba pantografu
            if (PantoConsumptionVolumeM3 == 0)
                PantoConsumptionVolumeM3 = 25.0f / 1000f; // 25 L 
            // AC    
            if (SwitchingVoltageMode_OffAC || LocomotivePowerVoltage == 25000)
            {
                // Spotřeba HV při AC
                if (HVConsumptionVolumeM3_On == 0)
                    HVConsumptionVolumeM3_On = 30.0f / 1000f; // 30 L

                if (HVConsumptionVolumeM3_Off == 0)
                    HVConsumptionVolumeM3_Off = 55.0f / 1000f; // 55 L
            }
            //DC
            if (SwitchingVoltageMode_OffDC || LocomotivePowerVoltage == 3000)
            {
                // Spotřeba HV při DC
                if (HVConsumptionVolumeM3_On == 0)
                    HVConsumptionVolumeM3_On = 20.0f / 1000f; // 20 L

                if (HVConsumptionVolumeM3_Off == 0)
                    HVConsumptionVolumeM3_Off = 20.0f / 1000f; // 20 L
            }

            if (AuxCompressor)
            {
                for (int i = 1; i <= Pantographs.Count; i++)
                {
                    switch (Pantographs[i].State)
                    {
                        case PantographState.Raising:
                            {
                                T_PantoUp[i] += elapsedClockSeconds;
                                if (AuxResPressurePSI > 0 && T_PantoUp[i] < 1)
                                    AuxResPressurePSI -= 14.50377f * (MaxAuxResPressurePSI / (AuxResVolumeM3 * MaxAuxResPressurePSI / PantoConsumptionVolumeM3)) * elapsedClockSeconds;
                                break;
                            }
                        case PantographState.Lowering:
                        case PantographState.Down:
                            {
                                if (!AirForPantograph)
                                    Pantographs[i].PantographsUpBlocked = true;
                                else Pantographs[i].PantographsUpBlocked = false;
                                break;
                            }
                        case PantographState.Up:
                            {
                                T_PantoUp[i] = 0;
                                if (!AirForPantograph)
                                    SignalEvent(PowerSupplyEvent.LowerPantograph);
                                break;
                            }
                    }
                }
                
                if (!HVElectric)
                {
                    switch (PowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Closing:
                            {
                                if (!AirForHV)
                                    HVOff = true;
                                break;
                            }

                        case CircuitBreakerState.Closed:
                            {
                                HVClosed = true;
                                T_HVOpen = 0;
                                T_HVClosed += elapsedClockSeconds;
                                if (AuxResPressurePSI > 0 && T_HVClosed < 1)
                                    AuxResPressurePSI -= 14.50377f * (MaxAuxResPressurePSI / (AuxResVolumeM3 * MaxAuxResPressurePSI / HVConsumptionVolumeM3_On)) * elapsedClockSeconds;

                                if (!AirForHV)
                                    HVOff = true;

                                break;
                            }

                        case CircuitBreakerState.Open:
                            {
                                if (HVClosed)
                                {
                                    T_HVClosed = 0;
                                    T_HVOpen += elapsedClockSeconds;
                                    if (AuxResPressurePSI > 0 && T_HVOpen < 1)
                                        AuxResPressurePSI -= 14.50377f * (MaxAuxResPressurePSI / (AuxResVolumeM3 * MaxAuxResPressurePSI / HVConsumptionVolumeM3_Off)) * elapsedClockSeconds;
                                    else
                                        HVClosed = false;
                                }
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// This function updates periodically the wagon heating.
        /// </summary>
        protected override void UpdateCarSteamHeat(float elapsedClockSeconds)
        {
            // Update Steam Heating System

            // TO DO - Add test to see if cars are coupled, if Light Engine, disable steam heating.


            if (IsSteamHeatFitted && this.IsLeadLocomotive())  // Only Update steam heating if train and locomotive fitted with steam heating
            {

                // Update water controller for steam boiler heating tank
                    WaterController.Update(elapsedClockSeconds);
                    if (WaterController.UpdateValue > 0.0)
                        Simulator.Confirmer.UpdateWithPerCent(CabControl.SteamHeatBoilerWater, CabSetting.Increase, WaterController.CurrentValue * 100);


                CurrentSteamHeatPressurePSI = SteamHeatController.CurrentValue * MaxSteamHeatPressurePSI;

                // Calculate steam boiler usage values
                // Don't turn steam heat on until pressure valve has been opened, water and fuel capacity also needs to be present, and steam boiler is not locked out
                if (CurrentSteamHeatPressurePSI > 0.1 && CurrentLocomotiveSteamHeatBoilerWaterCapacityL > 0 && CurrentSteamHeatBoilerFuelCapacityL > 0 && !IsSteamHeatBoilerLockedOut)
                {
                    // Set values for visible exhaust based upon setting of steam controller
                    HeatingSteamBoilerVolumeM3pS = 1.5f * SteamHeatController.CurrentValue;
                    HeatingSteamBoilerDurationS = 1.0f * SteamHeatController.CurrentValue;
                    Train.CarSteamHeatOn = true; // turn on steam effects on wagons

                    // Calculate fuel usage for steam heat boiler
                    float FuelUsageLpS = L.FromGUK(pS.FrompH(TrainHeatBoilerFuelUsageGalukpH[pS.TopH(CalculatedCarHeaterSteamUsageLBpS)]));
                    CurrentSteamHeatBoilerFuelCapacityL -= FuelUsageLpS * elapsedClockSeconds; // Reduce Tank capacity as fuel used.

                    // Calculate water usage for steam heat boiler
                    float WaterUsageLpS = L.FromGUK(pS.FrompH(TrainHeatBoilerWaterUsageGalukpH[pS.TopH(CalculatedCarHeaterSteamUsageLBpS)]));
                    CurrentLocomotiveSteamHeatBoilerWaterCapacityL -= WaterUsageLpS * elapsedClockSeconds; // Reduce Tank capacity as water used. Weight of locomotive is reduced in Wagon.cs
                }
                else
                {
                    Train.CarSteamHeatOn = false; // turn on steam effects on wagons
                }


            }
        }


        /// <summary>
        /// This function updates periodically the locomotive's sound variables.
        /// </summary>
        protected override void UpdateSoundVariables(float elapsedClockSeconds)
        {
            Variable1 = ThrottlePercent;
            if (ThrottlePercent == 0f) Variable2 = 0;
            else
            {
                float dV2;
                dV2 = Math.Abs(TractiveForceN) / MaxForceN * 100f - Variable2;
                float max = 2f;
                if (dV2 > max) dV2 = max;
                else if (dV2 < -max) dV2 = -max;
                Variable2 += dV2;
            }
            if (DynamicBrakePercent > 0)
                Variable3 = MaxDynamicBrakeForceN == 0 ? DynamicBrakePercent / 100f : DynamicBrakeForceN / MaxDynamicBrakeForceN;
            else
                Variable3 = 0;

            // Multisystémová lokomotiva
            if (MultiSystemEngine)
            {
                // **** AC ****
                if (VoltageAC > 5000 && SwitchingVoltageMode_OffAC || (TPowerOnAC == 1 || TCircuitBreakerAC == 2 || TCompressorAC == 1))
                {
                    AC_Triggers();                   
                }
                else

                // **** DC ****
                if (VoltageDC > 500 && SwitchingVoltageMode_OffDC || (TPowerOnDC == 1 || TCircuitBreakerDC == 2 || TCompressorDC == 1))
                {
                    DC_Triggers();                    
                }

                // Pantografy
                if (MaxLineVoltage0 > 15000)
                {                    
                    if (Pantographs[1].State == PantographState.Raising && TPanto1AC == 0) // Zadní panto
                    {
                        SignalEvent(Event.Pantograph1UpAC); 
                         TPanto1AC = 1;
                    }
                    if (Pantographs[1].State == PantographState.Lowering && TPanto1AC == 1) // Zadní panto
                    {
                        SignalEvent(Event.Pantograph1DownAC);
                        TPanto1AC = 0;
                    }

                    if (Pantographs[2].State == PantographState.Raising && TPanto2AC == 0) // Přední panto
                    {
                        SignalEvent(Event.Pantograph2UpAC);
                        TPanto2AC = 1;
                    }
                    if (Pantographs[2].State == PantographState.Lowering && TPanto2AC == 1) // Přední panto
                    {
                        SignalEvent(Event.Pantograph2DownAC); 
                         TPanto2AC = 0;
                    }
                }
                else
                {
                    if (Pantographs[1].State == PantographState.Raising && TPanto1DC == 0) // Zadní panto
                    {
                        SignalEvent(Event.Pantograph1UpDC);
                        TPanto1DC = 1;
                    }
                    if (Pantographs[1].State == PantographState.Lowering && TPanto1DC == 1) // Zadní panto
                    {
                        SignalEvent(Event.Pantograph1DownDC);
                        TPanto1DC = 0;
                    }

                    if (Pantographs[2].State == PantographState.Raising && TPanto2DC == 0) // Přední panto
                    {
                        SignalEvent(Event.Pantograph2UpDC);
                        TPanto2DC = 1;
                    }
                    if (Pantographs[2].State == PantographState.Lowering && TPanto2DC == 1) // Přední panto
                    {
                        SignalEvent(Event.Pantograph2DownDC);
                        TPanto2DC = 0;
                    }
                }
            }
        }

        public void AC_Triggers()
        {
            // **** Discrette Triggers ****
            // Power
            if (PowerOn && TPowerOnAC == 0)
            {
                SignalEvent(Event.PowerOnAC);
                TPowerOnAC = 1;
            }
            if (!PowerOn && TPowerOnAC == 1)
            {
                SignalEvent(Event.PowerOffAC);
                TPowerOnAC = 0;
            }

            // CircuitBreaker
            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open && TCircuitBreakerAC == 2)
            {
                SignalEvent(Event.CircuitBreakerOpenAC);
                TCircuitBreakerAC = 0;
            }
            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing && TCircuitBreakerAC == 0)
            {
                SignalEvent(Event.CircuitBreakerClosingAC);
                TCircuitBreakerAC = 1;
            }
            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed && TCircuitBreakerAC == 1)
            {
                SignalEvent(Event.CircuitBreakerClosedAC);
                TCircuitBreakerAC = 2;
            }

            // Compressor
            if (CompressorIsOn && TCompressorAC == 0)
            {
                SignalEvent(Event.CompressorOnAC);
                TCompressorAC = 1;
            }
            if (!CompressorIsOn && TCompressorAC == 1)
            {
                SignalEvent(Event.CompressorOffAC);
                TCompressorAC = 0;
            }

            // **** Variable Triggers ****
            Variable1AC = ThrottlePercent;
            Variable1DC = 0;

            if (ThrottlePercent == 0f) Variable2AC = 0;
            else
            {
                float dV2;
                dV2 = Math.Abs(TractiveForceN) / MaxForceN * 100f - Variable2AC;
                float max = 2f;
                if (dV2 > max) dV2 = max;
                else if (dV2 < -max) dV2 = -max;
                Variable2AC += dV2;
            }
            Variable2DC = 0;

            if (DynamicBrakePercent > 0)
                Variable3AC = MaxDynamicBrakeForceN == 0 ? DynamicBrakePercent / 100f : DynamicBrakeForceN / MaxDynamicBrakeForceN;
            else
                Variable3AC = 0;
            Variable3DC = 0;
        }

        public void DC_Triggers()
        {
            // **** Discrette Triggers ****
            // Power
            if (PowerOn && TPowerOnDC == 0)
            {
                SignalEvent(Event.PowerOnDC);
                TPowerOnDC = 1;
            }
            if (!PowerOn && TPowerOnDC == 1)
            {
                SignalEvent(Event.PowerOffDC);
                TPowerOnDC = 0;
            }

            // CircuitBreaker
            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open && TCircuitBreakerDC == 2)
            {
                SignalEvent(Event.CircuitBreakerOpenDC);
                TCircuitBreakerDC = 0;
            }
            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing && TCircuitBreakerDC == 0)
            {
                SignalEvent(Event.CircuitBreakerClosingDC);
                TCircuitBreakerDC = 1;
            }
            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed && TCircuitBreakerDC == 1)
            {
                SignalEvent(Event.CircuitBreakerClosedDC);
                TCircuitBreakerDC = 2;
            }

            // Compressor
            if (CompressorIsOn && TCompressorDC == 0)
            {
                SignalEvent(Event.CompressorOnDC);
                TCompressorDC = 1;
            }
            if (!CompressorIsOn && TCompressorDC == 1)
            {
                SignalEvent(Event.CompressorOffDC);
                TCompressorDC = 0;
            }

            // **** Variable Triggers ****
            Variable1DC = ThrottlePercent;
            Variable1AC = 0;

            if (ThrottlePercent == 0f) Variable2DC = 0;
            else
            {
                float dV2;
                dV2 = Math.Abs(TractiveForceN) / MaxForceN * 100f - Variable2DC;
                float max = 2f;
                if (dV2 > max) dV2 = max;
                else if (dV2 < -max) dV2 = -max;
                Variable2DC += dV2;
            }
            Variable2AC = 0;

            if (DynamicBrakePercent > 0)
                Variable3DC = MaxDynamicBrakeForceN == 0 ? DynamicBrakePercent / 100f : DynamicBrakeForceN / MaxDynamicBrakeForceN;
            else
                Variable3DC = 0;
            Variable3AC = 0;
        }

        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent(Event evt)
        {
            base.SignalEvent(evt);
        }

        public override void SignalEvent(PowerSupplyEvent evt)
        {
            if (Simulator.Confirmer != null && Simulator.PlayerLocomotive == this)
            {
                switch (evt)
                {
                    case PowerSupplyEvent.RaisePantograph:
                        Simulator.Confirmer.Confirm(CabControl.Pantograph1, CabSetting.On);
                        Simulator.Confirmer.Confirm(CabControl.Pantograph2, CabSetting.On);
                        Simulator.Confirmer.Confirm(CabControl.Pantograph3, CabSetting.On);
                        Simulator.Confirmer.Confirm(CabControl.Pantograph4, CabSetting.On);
                        break;

                    case PowerSupplyEvent.LowerPantograph:
                        Simulator.Confirmer.Confirm(CabControl.Pantograph1, CabSetting.Off);
                        Simulator.Confirmer.Confirm(CabControl.Pantograph2, CabSetting.Off);
                        Simulator.Confirmer.Confirm(CabControl.Pantograph3, CabSetting.Off);
                        Simulator.Confirmer.Confirm(CabControl.Pantograph4, CabSetting.Off);
                        break;
                }
            }

            switch (evt)
            {
                case PowerSupplyEvent.CloseCircuitBreaker:
                case PowerSupplyEvent.OpenCircuitBreaker:
                case PowerSupplyEvent.CloseCircuitBreakerButtonPressed:
                case PowerSupplyEvent.CloseCircuitBreakerButtonReleased:
                case PowerSupplyEvent.OpenCircuitBreakerButtonPressed:
                case PowerSupplyEvent.OpenCircuitBreakerButtonReleased:
                case PowerSupplyEvent.GiveCircuitBreakerClosingAuthorization:
                case PowerSupplyEvent.RemoveCircuitBreakerClosingAuthorization:
                    PowerSupply.HandleEvent(evt);
                    break;
            }

            base.SignalEvent(evt);
        }

        public override void SignalEvent(PowerSupplyEvent evt, int id)
        {
            if (Simulator.Confirmer != null && Simulator.PlayerLocomotive == this)
            {
                switch (evt)
                {
                    case PowerSupplyEvent.RaisePantograph:
                        if (id == 1) Simulator.Confirmer.Confirm(CabControl.Pantograph1, CabSetting.On);
                        if (id == 2) Simulator.Confirmer.Confirm(CabControl.Pantograph2, CabSetting.On);
                        if (id == 3) Simulator.Confirmer.Confirm(CabControl.Pantograph3, CabSetting.On);
                        if (id == 4) Simulator.Confirmer.Confirm(CabControl.Pantograph4, CabSetting.On);

                        if (!Simulator.TRK.Tr_RouteFile.Electrified)
                            Simulator.Confirmer.Warning(Simulator.Catalog.GetString("No power line!"));
                        if (Simulator.Settings.OverrideNonElectrifiedRoutes)
                            Simulator.Confirmer.Information(Simulator.Catalog.GetString("Power line condition overridden."));
                        break;

                    case PowerSupplyEvent.LowerPantograph:
                        if (id == 1) Simulator.Confirmer.Confirm(CabControl.Pantograph1, CabSetting.Off);
                        if (id == 2) Simulator.Confirmer.Confirm(CabControl.Pantograph2, CabSetting.Off);
                        if (id == 3) Simulator.Confirmer.Confirm(CabControl.Pantograph3, CabSetting.Off);
                        if (id == 4) Simulator.Confirmer.Confirm(CabControl.Pantograph4, CabSetting.Off);
                        break;
                }
            }

            base.SignalEvent(evt, id);
        }

        public override void SetPower(bool ToState)
        {
            if (Train != null)
            {
                if (!ToState)
                    SignalEvent(PowerSupplyEvent.LowerPantograph);
                else
                    SignalEvent(PowerSupplyEvent.RaisePantograph, 1);
            }

            base.SetPower(ToState);
        }

        public override float GetDataOf(CabViewControl cvc)
        {
            float data = 0;

            switch (cvc.ControlType)
            {
                case CABViewControlTypes.LINE_VOLTAGE:
                    if (cvc.UpdateTime != 0)
                        UpdateTimeEnable = true;
                    cvc.ElapsedTime += elapsedTime;
                    if (cvc.ElapsedTime > cvc.UpdateTime)
                    {
                        data = PantographVoltageV;
                        cvc.ElapsedTime = 0;
                        PreDataVoltage = data;
                    }
                    else
                        data = PreDataVoltage;                                     
                    if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                        data /= 1000;
                    break;

                case CABViewControlTypes.PANTO_DISPLAY:
                    data = Pantographs.State == PantographState.Up ? 1 : 0;
                    break;

                case CABViewControlTypes.PANTOGRAPH:
                    data = Pantographs[UsingRearCab && Pantographs.List.Count > 1 ? 2 : 1].CommandUp ? 1 : 0;
                    break;

                case CABViewControlTypes.PANTOGRAPH2:
                    data = Pantographs[UsingRearCab ? 1 : 2].CommandUp ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_PANTOGRAPH3:
                    data = Pantographs.List.Count > 2 && Pantographs[UsingRearCab && Pantographs.List.Count > 3 ? 4 : 3].CommandUp ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_PANTOGRAPH4:
                    data = Pantographs.List.Count > 3 && Pantographs[UsingRearCab ? 3 : 4].CommandUp ? 1 : 0;
                    break;

                // Icik
                // Nahrazeno novější variantou
                //case CABViewControlTypes.PANTOGRAPHS_4:
                //case CABViewControlTypes.PANTOGRAPHS_4C:
                //    if (Pantographs[1].CommandUp && Pantographs[2].CommandUp)
                //        data = 2;
                //    else if (Pantographs[UsingRearCab ? 2 : 1].CommandUp)
                //        data = 1;
                //    else if (Pantographs[UsingRearCab ? 1 : 2].CommandUp)
                //        data = 3;
                //    else
                //        data = 0;
                //    break;

                case CABViewControlTypes.PANTOGRAPHS_5:
                    if (Pantographs[1].CommandUp && Pantographs[2].CommandUp)
                        data = 0; // TODO: Should be 0 if the previous state was Pan2Up, and 4 if that was Pan1Up
                    else if (Pantographs[2].CommandUp)
                        data = 1;
                    else if (Pantographs[1].CommandUp)
                        data = 3;
                    else
                        data = 2;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                    data = PowerSupply.CircuitBreaker.DriverClosingOrder ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER:
                    data = PowerSupply.CircuitBreaker.DriverOpeningOrder ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION:
                    data = PowerSupply.CircuitBreaker.DriverClosingAuthorization ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE:
                    switch (PowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Open:
                            data = 0;
                            break;
                        case CircuitBreakerState.Closing:
                            data = 1;
                            break;
                        case CircuitBreakerState.Closed:
                            data = 2;
                            break;
                            LocoSwitchACDC = false;
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED:
                    switch (PowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Open:
                        case CircuitBreakerState.Closing:
                            data = 0;
                            break;
                        case CircuitBreakerState.Closed:
                            data = 1;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN:
                    switch (PowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Open:
                        case CircuitBreakerState.Closing:
                            data = 1;
                            break;
                        case CircuitBreakerState.Closed:
                            data = 0;
                            break;
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_AUTHORIZED:
                    data = PowerSupply.CircuitBreaker.ClosingAuthorization ? 1 : 0;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AND_AUTHORIZED:
                    data = (PowerSupply.CircuitBreaker.State < CircuitBreakerState.Closed && PowerSupply.CircuitBreaker.ClosingAuthorization) ? 1 : 0;
                    break;

                // Icik
                case CABViewControlTypes.SWITCHINGVOLTAGEMODE_OFF_DC:
                    {
                        SwitchingVoltageMode = MathHelper.Clamp(SwitchingVoltageMode, 0, 1);
                        data = SwitchingVoltageMode;
                        break;
                    }

                case CABViewControlTypes.SWITCHINGVOLTAGEMODE_OFF_AC:
                    {
                        SwitchingVoltageMode = MathHelper.Clamp(SwitchingVoltageMode, 1, 2);
                        data = SwitchingVoltageMode;
                        break;
                    }

                case CABViewControlTypes.SWITCHINGVOLTAGEMODE_DC_OFF_AC:
                    {
                        if (preVoltageDC > 500 && preVoltageDC < 4000)
                            data = 0;
                        else 
                        if (VoltageAC > 5000)
                            data = 2;
                        else
                            data = 1;

                        if (PantographVoltageV == 1 && preVoltageDC == 1)
                            data = 1;
                        break;
                    }
                
                case CABViewControlTypes.HV2:
                    {
                        HV2Enable = true;
                        switch (HV2Switch)
                        {
                            case 0:
                                data = 0;
                                break;
                            case 1: 
                                data = 1;
                                break;                            
                        }                        
                        break;
                    }

                case CABViewControlTypes.HV5:
                    {
                        HV5Enable = true;
                        switch (HV5Switch)
                        {
                            case 1:
                                data = 0;
                                break;
                            case 2: // DC
                                data = 1;
                                break;
                            case 3: // střed
                                data = 2;
                                break;
                            case 4: // AC
                                data = 3;
                                break;
                            case 5:
                                data = 4;
                                break;
                        }
                        LocoSwitchACDC = true;
                        break;
                    }

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE_MULTISYSTEM:
                    switch (PowerSupply.CircuitBreaker.State)
                    {
                        case CircuitBreakerState.Open:
                            if (SwitchingVoltageMode == 1) // Střed                          
                                data = 0;
                            if (SwitchingVoltageMode == 0) // levá strana - DC
                                data = 6;
                            if (SwitchingVoltageMode == 2) // pravá strana - AC
                                data = 2;
                            break;

                        case CircuitBreakerState.Closing:
                            if (SwitchingVoltageMode_OffAC)
                                data = 1;
                            if (SwitchingVoltageMode_OffDC)
                                data = 5;
                            break;

                        case CircuitBreakerState.Closed:
                            if (SwitchingVoltageMode_OffAC)
                                data = 2;
                            if (SwitchingVoltageMode_OffDC)
                                data = 6;
                            break;
                    }
                    LocoSwitchACDC = true;
                    break;
              
                case CABViewControlTypes.LINE_VOLTAGE_AC:
                    if (cvc.UpdateTime != 0)
                        UpdateTimeEnable = true;
                    cvc.ElapsedTime += elapsedTime;
                    if (cvc.ElapsedTime > cvc.UpdateTime)
                    {
                        data = VoltageAC;
                        cvc.ElapsedTime = 0;
                        PreDataVoltageAC = data;
                    }
                    else
                        data = PreDataVoltageAC;
                    if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                        data /= 1000;
                    break;

                case CABViewControlTypes.LINE_VOLTAGE_DC:
                    if (cvc.UpdateTime != 0)
                        UpdateTimeEnable = true;
                    cvc.ElapsedTime += elapsedTime;
                    if (cvc.ElapsedTime > cvc.UpdateTime)
                    {
                        data = VoltageDC;
                        cvc.ElapsedTime = 0;
                        PreDataVoltageDC = data;
                    }
                    else
                        data = PreDataVoltageDC;
                    if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                        data /= 1000;
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED_AC:
                    if (SwitchingVoltageMode_OffAC)
                    {
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                            case CircuitBreakerState.Closing:
                                data = 0;
                                break;
                            case CircuitBreakerState.Closed:
                                data = 1;
                                break;
                        }
                    }
                    break;
                
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED_DC:
                    if (SwitchingVoltageMode_OffDC)
                    {
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                            case CircuitBreakerState.Closing:
                                data = 0;
                                break;
                            case CircuitBreakerState.Closed:
                                data = 1;
                                break;
                        }
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AC:
                    if (SwitchingVoltageMode_OffAC)
                    {
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                            case CircuitBreakerState.Closing:
                                data = 1;
                                break;
                            case CircuitBreakerState.Closed:
                                data = 0;
                                break;
                        }
                    }
                    break;

                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_DC:
                    if (SwitchingVoltageMode_OffDC)
                    {
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                            case CircuitBreakerState.Closing:
                                data = 1;
                                break;
                            case CircuitBreakerState.Closed:
                                data = 0;
                                break;
                        }
                    }
                    break;

                default:
                    data = base.GetDataOf(cvc);
                    break;
            }


            return data;
        }

        public override void SwitchToAutopilotControl()
        {
            SetDirection(Direction.Forward);
            base.SwitchToAutopilotControl();
        }

        public override string GetStatus()
        {
            var status = new StringBuilder();
            status.AppendFormat("{0} = ", Simulator.Catalog.GetString("Pantographs"));
            foreach (var pantograph in Pantographs.List)
                status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(pantograph.State)));
            status.AppendLine();
            status.AppendFormat("{0} = {1}",
                Simulator.Catalog.GetString("Circuit breaker"),
                Simulator.Catalog.GetParticularString("CircuitBreaker", GetStringAttribute.GetPrettyName(PowerSupply.CircuitBreaker.State)));
            status.AppendLine();
            status.AppendFormat("{0} = {1}",
                Simulator.Catalog.GetParticularString("PowerSupply", "Power"),
                Simulator.Catalog.GetParticularString("PowerSupply", GetStringAttribute.GetPrettyName(PowerSupply.State)));
            return status.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder(base.GetDebugStatus());
            //Simulator.Catalog.GetString("Circuit breaker"),
            status.AppendFormat("\t{0}\t\t", Simulator.Catalog.GetParticularString("CircuitBreaker", GetStringAttribute.GetPrettyName(PowerSupply.CircuitBreaker.State)));
            //Simulator.Catalog.GetString("TCS"),
            status.AppendFormat("{0}\t", PowerSupply.CircuitBreaker.TCSClosingAuthorization ? Simulator.Catalog.GetString("OK") : Simulator.Catalog.GetString("NOT OK"));
            //Simulator.Catalog.GetString("Driver"),
            status.AppendFormat("{0}\t", PowerSupply.CircuitBreaker.DriverClosingAuthorization ? Simulator.Catalog.GetString("OK") : Simulator.Catalog.GetString("NOT OK"));
            //Simulator.Catalog.GetString("Auxiliary power"),
            status.AppendFormat("{0}", Simulator.Catalog.GetParticularString("PowerSupply", GetStringAttribute.GetPrettyName(PowerSupply.AuxiliaryState)));

            if (IsSteamHeatFitted && Train.PassengerCarsNumber > 0 && this.IsLeadLocomotive() && Train.CarSteamHeatOn)
            {
                // Only show steam heating HUD if fitted to locomotive and the train, has passenger cars attached, and is the lead locomotive
                // Display Steam Heat info
                status.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}/{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18:N0}\n",
                   Simulator.Catalog.GetString("StHeat:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(CurrentSteamHeatPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("StTemp"),
                   FormatStrings.FormatTemperature(C.FromF(SteamHeatPressureToTemperaturePSItoF[CurrentSteamHeatPressurePSI]), IsMetric, false),
                   Simulator.Catalog.GetString("StUse"),
                   FormatStrings.FormatMass(pS.TopH(Kg.FromLb(CalculatedCarHeaterSteamUsageLBpS)), IsMetric),
                   FormatStrings.h,
                   Simulator.Catalog.GetString("WaterLvl"),
                   FormatStrings.FormatFuelVolume(CurrentLocomotiveSteamHeatBoilerWaterCapacityL, IsMetric, IsUK),
                   Simulator.Catalog.GetString("Last:"),
                   Simulator.Catalog.GetString("Press"),
                   FormatStrings.FormatPressure(Train.LastCar.CarSteamHeatMainPipeSteamPressurePSI, PressureUnit.PSI, MainPressureUnit, true),
                   Simulator.Catalog.GetString("Temp"),
                   FormatStrings.FormatTemperature(Train.LastCar.CarCurrentCarriageHeatTempC, IsMetric, false),
                   Simulator.Catalog.GetString("OutTemp"),
                   FormatStrings.FormatTemperature(Train.TrainOutsideTempC, IsMetric, false),
                   Simulator.Catalog.GetString("NetHt"),
                   Train.LastCar.DisplayTrainNetSteamHeatLossWpTime);
            }

            return status.ToString();
        }

        /// <summary>
        /// Returns the controller which refills from the matching pickup point.
        /// </summary>
        /// <param name="type">Pickup type</param>
        /// <returns>Matching controller or null</returns>
        public override MSTSNotchController GetRefillController(uint type)
        {
            MSTSNotchController controller = null;
            if (type == (uint)PickupType.FuelWater) return WaterController;
            return controller;
        }

        /// <summary>
        /// Sets step size for the fuel controller basing on pickup feed rate and engine fuel capacity
        /// </summary>
        /// <param name="type">Pickup</param>

        public override void SetStepSize(PickupObj matchPickup)
        {
            if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
                WaterController.SetStepSize(matchPickup.PickupCapacity.FeedRateKGpS / MSTSNotchController.StandardBoost / MaximumSteamHeatBoilerWaterTankCapacityL);
        }

        /// <summary>
        /// Sets coal and water supplies to full immediately.
        /// Provided in case route lacks pickup points for diesel oil.
        /// </summary>
        public override void RefillImmediately()
        {
            WaterController.CurrentValue = 1.0f;
        }

        /// <summary>
        /// Returns the fraction of diesel oil already in tank.
        /// </summary>
        /// <param name="pickupType">Pickup type</param>
        /// <returns>0.0 to 1.0. If type is unknown, returns 0.0</returns>
        public override float GetFilledFraction(uint pickupType)
        {
            if (pickupType == (uint)PickupType.FuelWater)
            {
                return WaterController.CurrentValue;
            }
            return 0f;
        }

    } // class ElectricLocomotive
}
