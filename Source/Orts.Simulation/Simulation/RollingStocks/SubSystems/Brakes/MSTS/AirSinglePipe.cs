// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// Debug for Airbrake operation - Train Pipe Leak
//#define DEBUG_TRAIN_PIPE_LEAK

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class AirSinglePipe : MSTSBrakeSystem
    {
        protected TrainCar Car;
        protected float HandbrakePercent;
        public float CylPressurePSI = 55;
        protected float AutoCylPressurePSI = 55;
        protected float AuxResPressurePSI = 72;
        protected float EmergResPressurePSI = 72;
        protected float FullServPressurePSI = 50;
        protected float MaxCylPressurePSI = 55;
        protected float AuxCylVolumeRatio = 2.5f;
        protected float AuxBrakeLineVolumeRatio;
        protected float EmergResVolumeM3 = 0.07f;
        protected float RetainerPressureThresholdPSI;
        protected float ReleaseRatePSIpS = 1.86f;
        protected float MaxReleaseRatePSIpS = 1.86f;
        protected float MaxApplicationRatePSIpS = .9f;
        protected float MaxAuxilaryChargingRatePSIpS = 1.684f;
        protected float BrakeInsensitivityPSIpS = 0;
        protected float EmergResChargingRatePSIpS = 1.684f;
        protected float EmergAuxVolumeRatio = 1.4f;
        protected string DebugType = string.Empty;
        protected string RetainerDebugState = string.Empty;
        protected bool NoMRPAuxResCharging;
        protected float CylVolumeM3;

        protected bool TrainBrakePressureChanging = false;
        protected bool EngineBrakePressureChanging = false;
        protected bool BrakePipePressureChanging = false;
        protected float SoundTriggerCounter = 0;
        protected float prevCylPressurePSI = 0;
        protected float prevBrakePipePressurePSI = 0;
        protected bool BailOffOn;

        protected float T0 = 0;
        protected float T1 = 0;
        protected float TrainBrakeDelay = 0;
        protected bool BrakeReadyToApply = false;
        protected float EDBEngineBrakeDelay = 0;
        protected int T00 = 0;
        protected float TRMg = 0;
        protected float PrevAuxResPressurePSI = 0;
        protected float threshold = 0;
        protected float prevBrakeLine1PressurePSI = 0;
        protected float prevAutoCylPressurePSI = 0;
        protected bool NotConnected = false;
        protected float ThresholdBailOffOn = 0;
        protected ValveState PrevTripleValveStateState;
        protected float AutomaticDoorsCycle = 0;

        protected bool AICompressorOn;
        protected bool AICompressorOff;
        protected bool AICompressorRun;
        protected float AITrainLeakage;
        protected float AITrainBrakePipeVolumeM3;
        

        /// <summary>
        /// EP brake holding valve. Needs to be closed (Lap) in case of brake application or holding.
        /// For non-EP brake types must default to and remain in Release.
        /// </summary>
        protected ValveState HoldingValve = ValveState.Release;

        public enum ValveState
        {
            [GetString("Lap")] Lap,
            [GetString("Apply")] Apply,
            [GetString("Release")] Release,
            [GetString("Emergency")] Emergency
        };
        protected ValveState TripleValveState = ValveState.Lap;

        public AirSinglePipe(TrainCar car)
        {
            Car = car;
            // taking into account very short (fake) cars to prevent NaNs in brake line pressures
            BrakePipeVolumeM3Base = ((0.032f / 2f) * (0.032f / 2f) * (float)Math.PI) * (2f + car.CarLengthM);
            DebugType = "1P";
            // Force graduated releasable brakes. Workaround for MSTS with bugs preventing to set eng/wag files correctly for this.
            (Car as MSTSWagon).DistributorPresent |= Car.Simulator.Settings.GraduatedRelease;

            if (Car.Simulator.Settings.RetainersOnAllCars && !(Car is MSTSLocomotive))
                (Car as MSTSWagon).RetainerPositions = 4;
        }

        public override bool GetHandbrakeStatus()
        {
            return HandbrakePercent > 0;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            AirSinglePipe thiscopy = (AirSinglePipe)copy;
            MaxCylPressurePSI = thiscopy.MaxCylPressurePSI;
            AuxCylVolumeRatio = thiscopy.AuxCylVolumeRatio;
            AuxBrakeLineVolumeRatio = thiscopy.AuxBrakeLineVolumeRatio;
            EmergResVolumeM3 = thiscopy.EmergResVolumeM3;
            BrakePipeVolumeM3Base = thiscopy.BrakePipeVolumeM3Base;
            RetainerPressureThresholdPSI = thiscopy.RetainerPressureThresholdPSI;
            ReleaseRatePSIpS = thiscopy.ReleaseRatePSIpS;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            MaxAuxilaryChargingRatePSIpS = thiscopy.MaxAuxilaryChargingRatePSIpS;
            BrakeInsensitivityPSIpS = thiscopy.BrakeInsensitivityPSIpS;
            EmergResChargingRatePSIpS = thiscopy.EmergResChargingRatePSIpS;
            EmergAuxVolumeRatio = thiscopy.EmergAuxVolumeRatio;
            TwoPipesConnection = thiscopy.TwoPipesConnection;
            NoMRPAuxResCharging = thiscopy.NoMRPAuxResCharging;
            HoldingValve = thiscopy.HoldingValve;
            TripleValveState = thiscopy.TripleValveState;
            BrakeSensitivityPSIpS = thiscopy.BrakeSensitivityPSIpS;
            OverchargeEliminationRatePSIpS = thiscopy.OverchargeEliminationRatePSIpS;
            BrakeCylinderMaxSystemPressurePSI = thiscopy.BrakeCylinderMaxSystemPressurePSI;
            TrainBrakesControllerMaxOverchargePressurePSI = thiscopy.TrainBrakesControllerMaxOverchargePressurePSI;
            BrakeMassG = thiscopy.BrakeMassG;
            BrakeMassP = thiscopy.BrakeMassP;
            BrakeMassR = thiscopy.BrakeMassR;
            BrakeMassRMg = thiscopy.BrakeMassRMg;
            BrakeMassEmpty = thiscopy.BrakeMassEmpty;
            BrakeMassLoaded = thiscopy.BrakeMassLoaded;
            ForceWagonLoaded = thiscopy.ForceWagonLoaded;
            ForceBrakeMode = thiscopy.ForceBrakeMode;
            DebugKoef1 = thiscopy.DebugKoef1;
            DebugKoef2Factor = thiscopy.DebugKoef2Factor;
            DebugKoef = thiscopy.DebugKoef;
            MaxReleaseRatePSIpSG = thiscopy.MaxReleaseRatePSIpSG;
            MaxApplicationRatePSIpSG = thiscopy.MaxApplicationRatePSIpSG;
            MaxReleaseRatePSIpSP = thiscopy.MaxReleaseRatePSIpSP;
            MaxApplicationRatePSIpSP = thiscopy.MaxApplicationRatePSIpSP;
            MaxReleaseRatePSIpSR = thiscopy.MaxReleaseRatePSIpSR;
            MaxApplicationRatePSIpSR = thiscopy.MaxApplicationRatePSIpSR;
            maxPressurePSI0 = thiscopy.maxPressurePSI0;
            AutoLoadRegulatorEquipped = thiscopy.AutoLoadRegulatorEquipped;
            AutoLoadRegulatorMaxBrakeMass = thiscopy.AutoLoadRegulatorMaxBrakeMass;
            MainResMinimumPressureForMGbrakeActivationPSI = thiscopy.MainResMinimumPressureForMGbrakeActivationPSI;
            BrakePipePressureForMGbrakeActivationPSI = thiscopy.BrakePipePressureForMGbrakeActivationPSI;
            AntiSkidSystemEquipped = thiscopy.AntiSkidSystemEquipped;
            AutoBailOffOnRatePSIpS = thiscopy.AutoBailOffOnRatePSIpS;
            BrakeDelayToEngage = thiscopy.BrakeDelayToEngage;
            AutoOverchargePressure = thiscopy.AutoOverchargePressure;
            BrakePipeMinPressureDropToEngage = thiscopy.BrakePipeMinPressureDropToEngage;
            EngineBrakeControllerApplyDeadZone = thiscopy.EngineBrakeControllerApplyDeadZone;
            EngineBrakeControllerReleaseDeadZone = thiscopy.EngineBrakeControllerReleaseDeadZone;
            BP1_EngineBrakeControllerRatePSIpS = thiscopy.BP1_EngineBrakeControllerRatePSIpS;
            BP2_EngineBrakeControllerRatePSIpS = thiscopy.BP2_EngineBrakeControllerRatePSIpS;
            LEKOV_EngineBrakeControllerRatePSIpS = thiscopy.LEKOV_EngineBrakeControllerRatePSIpS;
            PressureRateFactorDischarge = thiscopy.PressureRateFactorDischarge;
            PressureRateFactorCharge = thiscopy.PressureRateFactorCharge;
            BrakeCylinderMaxPressureForLowState = thiscopy.BrakeCylinderMaxPressureForLowState;
            LowStateOnSpeedEngageLevel = thiscopy.LowStateOnSpeedEngageLevel;
            TwoStateBrake = thiscopy.TwoStateBrake;
            AuxPowerOnDelayS = thiscopy.AuxPowerOnDelayS;
            OLBailOffLimitPressurePSI = thiscopy.OLBailOffLimitPressurePSI;
        }

        // Get the brake BC & BP for EOT conditions
        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)}";
        }

        // Get Brake information for train
        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            var s = $" {Simulator.Catalog.GetString("EQ")} {FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.PSI, units[BrakeSystemComponent.EqualizingReservoir], true)}"
                //+ $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(Car.Train.HUDWagonBrakeCylinderPSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(AutoCylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)}";
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += $" {Simulator.Catalog.GetString("EOT")} {lastCarBrakeSystem.GetStatus(units)}";
            if (HandbrakePercent > 0)
                s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";

            s += string.Format("  " + Simulator.Catalog.GetString("Pipe pressure change") + " {0:F5} bar/s", BrakePipeChangeRate / 14.50377f);
            s += string.Format("  " + Simulator.Catalog.GetString("Leakage") + " {0:F5} bar/s", Car.Train.TotalTrainTrainPipeLeakRate / 14.50377f);
            //s += string.Format("  Objem potrubí {0:F0} L", Car.Train.TotalTrainBrakePipeVolumeM3 * 1000);
            s += string.Format("  " + Simulator.Catalog.GetString("Volume mainres and pipes") + " {0:F0} L", Car.Train.TotalCapacityMainResBrakePipe * 1000 / 14.50377f);

            return s;
        }
        public override string GetSimpleStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            var s = $" {Simulator.Catalog.GetString("EQ")} {FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.PSI, units[BrakeSystemComponent.EqualizingReservoir], true)}"
                //+ $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(Car.Train.HUDWagonBrakeCylinderPSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(AutoCylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)}";
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += $" {Simulator.Catalog.GetString("EOT")} {lastCarBrakeSystem.GetStatus(units)}";
            if (HandbrakePercent > 0)
                s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";            
            return s;
        }


        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return new string[] {
                DebugType,
                FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true),
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true),
                FormatStrings.FormatPressure(AuxResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.AuxiliaryReservoir], true),
                //(Car as MSTSWagon).EmergencyReservoirPresent ? FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.EmergencyReservoir], true) : string.Empty,
                //TwoPipes ? FormatStrings.FormatPressure(BrakeLine2PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.MainPipe], true) : string.Empty,
                //(Car as MSTSWagon).RetainerPositions == 0 ? string.Empty : RetainerDebugState,
                Simulator.Catalog.GetString(GetStringAttribute.GetPrettyName(TripleValveState)),
                //string.Empty, // Spacer because the state above needs 2 columns.
                (Car as MSTSWagon).HandBrakePresent ? string.Format("{0:F0} %", HandbrakePercent) : string.Empty,
                string.Format("{0} {1}", FrontBrakeHoseConnected ? "I" : "T", TwoPipesConnection ? "I" : ""),
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : " ",//HudScroll feature requires for the last value, at least one space instead of string.Empty,                                                
                BailOffOnAntiSkid ? Simulator.Catalog.GetString("Active") : "",
                string.Format("{0:F5} bar/s", (Car as MSTSWagon).TrainPipeLeakRatePSIpSBase / 14.50377f),
                string.Empty, // Spacer because the state above needs 2 columns.                                     
                string.Format("{0:F0} L", BrakePipeVolumeM3Base * 1000),
                string.Format("{0:F0} L", CylVolumeM3 * 1000),
                string.Format("{0:F0} L", TotalCapacityMainResBrakePipe * 1000 / 14.50377f),
                CarHasProblemWithBrake ?  BrakeCarDeactivate ? Simulator.Catalog.GetString("Off") : Simulator.Catalog.GetString("Failure!") : BrakeCarModeText,
                string.Format("{0}{1:F0} t", AutoLoadRegulatorEquipped ? "Auto " : "", (BrakeMassKG + BrakeMassKGRMg) / 1000),
                string.Format("DebKoef {0:F1}", DebugKoef),
                string.Empty, // Spacer because the state above needs 2 columns.                                                     
                string.Format("{0}", NextLocoBrakeState),

                //string.Empty, // Spacer because the state above needs 2 columns.                                                     
                //(Car as MSTSLocomotive) != null ? string.Format("AuxPowerOff {0}", (Car as MSTSLocomotive).AuxPowerOff): string.Empty,
                
                //string.Empty, // Spacer because the state above needs 2 columns.                                                     
                //(Car as MSTSLocomotive) != null ? ((Car as MSTSLocomotive).PowerUnit) ? string.Format("Hnací vůz"): string.Format("Control"): string.Empty,

                //string.Empty, // Spacer because the state above needs 2 columns.                                                     
                //(Car as MSTSLocomotive) != null ? string.Format("RDST {0}", (Car as MSTSLocomotive).RDSTBreaker): string.Empty,
                
                //string.Empty, // Spacer because the state above needs 2 columns.                                                     
                //(Car as MSTSLocomotive) != null ? string.Format("MUCable {0}", (Car as MSTSLocomotive).MUCable): string.Empty,                

                //string.Empty, // Spacer because the state above needs 2 columns.                                     
                //string.Format("AirOK_DoorCanManipulate {0:F0}", AirOK_DoorCanManipulate),
                //string.Empty, // Spacer because the state above needs 2 columns.                                     
                //string.Format("Parking {0:F0}", ParkingBrakeAutoCylPressurePSI1),
                //string.Empty, // Spacer because the state above needs 2 columns.                                     
                //string.Format("PrevAux {0:F0}", PrevAuxResPressurePSI),
            };
        }

        public override float GetCylPressurePSI()
        {
            return CylPressurePSI;
        }

        public override float GetCylVolumeM3()
        {
            return CylVolumeM3;
        }

        public float GetFullServPressurePSI()
        {
            return FullServPressurePSI;
        }

        public float GetMaxCylPressurePSI()
        {
            return MaxCylPressurePSI;
        }

        public float GetAuxCylVolumeRatio()
        {
            return AuxCylVolumeRatio;
        }

        public float GetMaxReleaseRatePSIpS()
        {
            return MaxReleaseRatePSIpS;
        }

        public float GetMaxApplicationRatePSIpS()
        {
            return MaxApplicationRatePSIpS;
        }

        public override float GetVacResPressurePSI()
        {
            return 0;
        }

        public override float GetVacResVolume()
        {
            return 0;
        }
        public override float GetVacBrakeCylNumber()
        {
            return 0;
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxCylPressurePSI = AutoCylPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(triplevalveratio": AuxCylVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(brakedistributorreleaserate":
                case "wagon(maxreleaserate": MaxReleaseRatePSIpS = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(brakedistributorapplicationrate":
                case "wagon(maxapplicationrate": MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxauxilarychargingrate": MaxAuxilaryChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyreschargingrate": EmergResChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyresvolumemultiplier": EmergAuxVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(emergencyrescapacity": EmergResVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;

                // OpenRails specific parameters
                case "wagon(brakepipevolume": BrakePipeVolumeM3Base = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                //case "wagon(ortsbrakeinsensitivity": BrakeInsensitivityPSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Načte hodnotu citivosti brzdy lokomotivy i vozů
                case "wagon(brakesensitivity": BrakeSensitivityPSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Načte brzdící váhu lokomotivy i vozů v režimech G, P, R, Prázdný, Ložený
                case "wagon(brakemassg": BrakeMassG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassp": BrakeMassP = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassr": BrakeMassR = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassrmg": BrakeMassRMg = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassempty": BrakeMassEmpty = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(brakemassloaded": BrakeMassLoaded = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(forcewagonloaded": ForceWagonLoaded = stf.ReadBoolBlock(false); break;
                case "wagon(forcebrakemode": ForceBrakeMode = stf.ReadStringBlock("P"); break;

                // Načte hodnoty napouštění a vypouštění brzdových válců lokomotivy i vozů v režimech G, P, R
                case "wagon(maxapplicationrateg": MaxApplicationRatePSIpSG = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxreleaserateg": MaxReleaseRatePSIpSG = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxapplicationratep": MaxApplicationRatePSIpSP = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxreleaseratep": MaxReleaseRatePSIpSP = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxapplicationrater": MaxApplicationRatePSIpSR = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxreleaserater": MaxReleaseRatePSIpSR = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Automatický zátěžový regulátor pro vozy
                case "wagon(autoloadregulatorequipped": AutoLoadRegulatorEquipped = stf.ReadBoolBlock(false); break;
                case "wagon(autoloadregulatormaxbrakemass": AutoLoadRegulatorMaxBrakeMass = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;

                // Ladící koeficient pro ladiče brzd                
                case "wagon(debugkoef": DebugKoef1 = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(debugkoef2": DebugKoef2Factor = new Interpolator(stf); break;

                // Minimální tlak v hlavní jímce a brzdovém potrubí pro brzdu R+Mg
                case "wagon(mainresminimumpressureformgbrakeactivation": MainResMinimumPressureForMGbrakeActivationPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(brakepipepressureformgbrakeactivation": BrakePipePressureForMGbrakeActivationPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;

                // Antismykový systém
                case "wagon(antiskidsystemequipped": AntiSkidSystemEquipped = stf.ReadBoolBlock(false); break;

                // Načte hodnotu zpoždění náběhu brzdy                              
                case "wagon(brakedelaytoengage": BrakeDelayToEngage = stf.ReadFloatBlock(STFReader.UNITS.Time, null); break;

                // Načte hodnotu úbytku tlaku pro pohyb ústrojí                              
                case "wagon(brakepipeminpressuredroptoengage": BrakePipeMinPressureDropToEngage = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;

                // Omezení tlaku do válce po překročení rychlosti                              
                case "wagon(twostatebrake(brakecylindermaxpressureforlowstate":
                    stf.MustMatch("(");
                    BrakeCylinderMaxPressureForLowState = stf.ReadFloat(STFReader.UNITS.PressureDefaultPSI, null);
                    //TwoStateBrake = true;
                    break;

                case "wagon(twostatebrake(lowstateonspeedengagelevel":
                    stf.MustMatch("(");
                    LowStateOnSpeedEngageLevel = stf.ReadFloat(STFReader.UNITS.Speed, null);
                    TwoStateBrake = true;
                    break;

                // Načte hodnotu rychlosti eliminace níkotlakého přebití                              
                case "engine(overchargeeliminationrate": OverchargeEliminationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Načte hodnotu maximálního tlaku v brzdovém válci
                case "engine(brakecylindermaxsystempressure": BrakeCylinderMaxSystemPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;

                // Načte hodnotu tlaku při nízkotlakém přebití
                case "engine(trainbrakescontrollermaxoverchargepressure": TrainBrakesControllerMaxOverchargePressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;

                // Načte hodnotu rychlosti AutoBailOffOn                              
                case "engine(autobailoffonrate": AutoBailOffOnRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;

                // Automatické nízkotlaké přebití                              
                case "engine(autooverchargepressure": AutoOverchargePressure = stf.ReadBoolBlock(false); break;

                // Definice mrtvých zón pro přímočinnou brzdu                             
                case "engine(enginebrakecontrollerapplydeadzone": EngineBrakeControllerApplyDeadZone = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(enginebrakecontrollerreleasedeadzone": EngineBrakeControllerReleaseDeadZone = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;

                // Definování brzdičů BP1, BP2 a LEKOV                               
                case "engine(enginebrakecontroller_bp1":
                    stf.MustMatch("(");
                    EngineBrakeControllerApplyDeadZone = stf.ReadFloat(STFReader.UNITS.None, null);
                    EngineBrakeControllerReleaseDeadZone = stf.ReadFloat(STFReader.UNITS.None, null);
                    BP1_EngineBrakeControllerRatePSIpS = stf.ReadFloat(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    stf.SkipRestOfBlock();
                    BP1_EngineBrakeController = true;
                    break;
                case "engine(enginebrakecontroller_bp2":
                    stf.MustMatch("(");
                    BP2_EngineBrakeControllerRatePSIpS = stf.ReadFloat(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    stf.SkipRestOfBlock();
                    BP2_EngineBrakeController = true;
                    break;
                case "engine(enginebrakecontroller_lekov":
                    stf.MustMatch("(");
                    LEKOV_EngineBrakeControllerRatePSIpS = stf.ReadFloat(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    stf.SkipRestOfBlock();
                    LEKOV_EngineBrakeController = true;
                    break;

                case "engine(brakepipedischargerate":
                    BrakePipeDischargeRate = true;
                    PressureRateFactorDischarge = new Interpolator(stf);
                    break;
                case "engine(brakepipechargerate":
                    BrakePipeChargeRate = true;
                    PressureRateFactorCharge = new Interpolator(stf);
                    break;
                case "engine(ortsauxpowerondelay": AuxPowerOnDelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, 10); break;
                case "engine(olbailofftype": OLBailOffType = stf.ReadStringBlock("OL3"); break;
                case "engine(ol2bailofflimitpressure": OLBailOffLimitPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(HandbrakePercent);
            outf.Write(ReleaseRatePSIpS);
            outf.Write(RetainerPressureThresholdPSI);
            outf.Write(AutoCylPressurePSI);
            outf.Write(AuxResPressurePSI);
            outf.Write(EmergResPressurePSI);
            outf.Write(FullServPressurePSI);
            outf.Write((int)TripleValveState);
            outf.Write(FrontBrakeHoseConnected);
            outf.Write(AngleCockAOpen);
            outf.Write(AngleCockBOpen);
            outf.Write(BleedOffValveOpen);
            outf.Write((int)HoldingValve);
            outf.Write(CylVolumeM3);
            outf.Write(BailOffOn);
            outf.Write(StartOn);
            outf.Write(PrevAuxResPressurePSI);
            outf.Write(AutoCylPressurePSI2);
            outf.Write(AutoCylPressurePSI1);
            outf.Write(AutoCylPressurePSI0);
            outf.Write(BrakeCarMode);
            outf.Write(BrakeCarModeText);
            outf.Write(BrakeCarModePL);
            outf.Write(BrakeCarModeTextPL);
            outf.Write(MaxApplicationRatePSIpS0);
            outf.Write(MaxReleaseRatePSIpS0);
            outf.Write(maxPressurePSI0);
            outf.Write(BailOffOnAntiSkid);
            outf.Write(PrevEngineBrakeControllerRateApply);
            outf.Write(PrevEngineBrakeControllerRateRelease);
            outf.Write(EngineBrakeDelay);
            outf.Write(TrainBrakeDelay);
            outf.Write(T4_ParkingkBrake);
            outf.Write(BrakeCylReleaseEDBOn);
            outf.Write(HighPressure);
            outf.Write(LowPressure);
            outf.Write(T_HighPressure);
            outf.Write(TwoPipesConnectionMenu);
            outf.Write(TwoPipesConnectionText);
            outf.Write(AutomaticDoorsCycle);
            outf.Write(LeftDoorIsOpened);
            outf.Write(RightDoorIsOpened);
            outf.Write(BrakeCarDeactivateMenu);
            outf.Write(BrakeCarDeactivateText);
            outf.Write(BrakeCarDeactivate);
            outf.Write(BrakeCarHasStatus);
            outf.Write(CarHasAirStuckBrake_1);
            outf.Write(CarHasAirStuckBrake_2);
            outf.Write(CarHasAirStuckBrake_3);
            outf.Write(CarHasMechanicStuckBrake_1);
            outf.Write(CarHasMechanicStuckBrake_2);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            HandbrakePercent = inf.ReadSingle();
            ReleaseRatePSIpS = inf.ReadSingle();
            RetainerPressureThresholdPSI = inf.ReadSingle();
            AutoCylPressurePSI = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            EmergResPressurePSI = inf.ReadSingle();
            FullServPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
            FrontBrakeHoseConnected = inf.ReadBoolean();
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockBOpen = inf.ReadBoolean();
            BleedOffValveOpen = inf.ReadBoolean();
            HoldingValve = (ValveState)inf.ReadInt32();
            CylVolumeM3 = inf.ReadSingle();
            BailOffOn = inf.ReadBoolean();
            StartOn = inf.ReadBoolean();
            PrevAuxResPressurePSI = inf.ReadSingle();
            AutoCylPressurePSI2 = inf.ReadSingle();
            AutoCylPressurePSI1 = inf.ReadSingle();
            AutoCylPressurePSI0 = inf.ReadSingle();
            BrakeCarMode = inf.ReadSingle();
            BrakeCarModeText = inf.ReadString();
            BrakeCarModePL = inf.ReadSingle();
            BrakeCarModeTextPL = inf.ReadString();
            MaxApplicationRatePSIpS0 = inf.ReadSingle();
            MaxReleaseRatePSIpS0 = inf.ReadSingle();
            maxPressurePSI0 = inf.ReadSingle();
            BailOffOnAntiSkid = inf.ReadBoolean();
            PrevEngineBrakeControllerRateApply = inf.ReadSingle();
            PrevEngineBrakeControllerRateRelease = inf.ReadSingle();
            EngineBrakeDelay = inf.ReadSingle();
            TrainBrakeDelay = inf.ReadSingle();
            T4_ParkingkBrake = inf.ReadInt32();
            BrakeCylReleaseEDBOn = inf.ReadBoolean();
            HighPressure = inf.ReadBoolean();
            LowPressure = inf.ReadBoolean();
            T_HighPressure = inf.ReadSingle();
            TwoPipesConnectionMenu = inf.ReadSingle();
            TwoPipesConnectionText = inf.ReadString();
            AutomaticDoorsCycle = inf.ReadSingle();
            LeftDoorIsOpened = inf.ReadBoolean();
            RightDoorIsOpened = inf.ReadBoolean();
            BrakeCarDeactivateMenu = inf.ReadSingle();
            BrakeCarDeactivateText = inf.ReadString();
            BrakeCarDeactivate = inf.ReadBoolean();
            BrakeCarHasStatus = inf.ReadBoolean();
            CarHasAirStuckBrake_1 = inf.ReadBoolean();
            CarHasAirStuckBrake_2 = inf.ReadBoolean();
            CarHasAirStuckBrake_3 = inf.ReadBoolean();
            CarHasMechanicStuckBrake_1 = inf.ReadBoolean();
            CarHasMechanicStuckBrake_2 = inf.ReadBoolean();
        }


        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            // Zjistí maximální pracovní tlak v systému
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams)
            {
                Car.Train.EqualReservoirPressurePSIorInHg = maxPressurePSI = maxPressurePSI0 = 5.0f * 14.50377f;
            }
            else
                Car.Train.EqualReservoirPressurePSIorInHg = maxPressurePSI0 = maxPressurePSI;

            if (StartOn) maxPressurePSI0 = Car.Train.EqualReservoirPressurePSIorInHg;

            BrakeLine1PressurePSI = maxPressurePSI0;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            PrevAuxResPressurePSI = 0;
            prevBrakeLine1PressurePSI = 0;
            BrakeReadyToApply = false;
            EDBEngineBrakeDelay = 0;
            TrainBrakeDelay = 0;
            EngineBrakeDelay = 0;
            AutoEngineBrakeDelay = 0;
            threshold = 0;
            ThresholdBailOffOn = 0;
            BrakeCylReleaseEDBOn = false;
            T_HighPressure = 0;

            if ((Car as MSTSWagon).EmergencyReservoirPresent || maxPressurePSI > 0)
                EmergResPressurePSI = maxPressurePSI;
            FullServPressurePSI = fullServPressurePSI;
            AutoCylPressurePSI0 = immediateRelease ? 0 : Math.Min((maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, MaxCylPressurePSI);
            AuxResPressurePSI = AutoCylPressurePSI == 0 ? (maxPressurePSI > BrakeLine1PressurePSI ? maxPressurePSI : BrakeLine1PressurePSI)
                : Math.Max(maxPressurePSI - AutoCylPressurePSI / AuxCylVolumeRatio, BrakeLine1PressurePSI);
            TripleValveState = ValveState.Lap;
            HoldingValve = ValveState.Release;
            if ((Car as MSTSWagon).HandBrakePresent)
                HandbrakePercent = 0;
            SetRetainer(RetainerSetting.Exhaust);
            TrainBrakePositionSet();
            MSTSLocomotive loco = Car as MSTSLocomotive;
            if (loco != null)
            {
                loco.MainResPressurePSI = loco.MaxMainResPressurePSI;
                if (loco.AuxCompressor)
                    loco.AuxResPressurePSI = loco.MaxAuxResPressurePSI;
                if (loco.HandBrakePresent)
                    HandbrakePercent = 0;                
            }
        }

        /// <summary>
        /// Used when initial speed > 0
        /// </summary>
        public override void InitializeMoving()
        {
            var emergResPressurePSI = EmergResPressurePSI;
            Initialize(false, 0, FullServPressurePSI, true);
            EmergResPressurePSI = emergResPressurePSI;
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
        }

        public virtual void UpdateTripleValveState(float controlPressurePSI)
        {
            // Funkční 3-cestný ventil          
            if (!BailOffOn && BrakeLine1PressurePSI < AuxResPressurePSI - 0.5f) TripleValveState = ValveState.Apply;
            else
                TripleValveState = ValveState.Lap;

            if (!BailOffOn && BrakeLine1PressurePSI > AuxResPressurePSI + 0.5f) TripleValveState = ValveState.Release;
        }

        public void TrainBrakePositionSet()
        {
            MSTSLocomotive loco = Car as MSTSLocomotive;
            if (loco != null)
            {
                if (loco.Battery && loco.PowerKey && loco.IsLeadLocomotive())
                {
                    if (loco.TrainBrakeController.DefaultNeutralBrakeValue > 0)
                        loco.TrainBrakeController.SetPercent(loco.TrainBrakeController.DefaultNeutralBrakeValue * 100);
                    else
                    if (loco.TrainBrakeController.DefaultLapBrakeValue > 0)
                        loco.TrainBrakeController.SetPercent(loco.TrainBrakeController.DefaultLapBrakeValue * 100);
                    else
                    if (loco.TrainBrakeController.DefaultBrakeValue > 0)
                        loco.TrainBrakeController.SetPercent(loco.TrainBrakeController.DefaultBrakeValue * 100);
                }
                else
                {
                    if (loco.TrainBrakeController.DefaultLapBrakeValue > 0)
                        loco.TrainBrakeController.SetPercent(loco.TrainBrakeController.DefaultLapBrakeValue * 100);
                    else
                    if (loco.TrainBrakeController.DefaultNeutralBrakeValue > 0)
                        loco.TrainBrakeController.SetPercent(loco.TrainBrakeController.DefaultNeutralBrakeValue * 100);
                    else
                    if (loco.TrainBrakeController.DefaultBrakeValue > 0)
                        loco.TrainBrakeController.SetPercent(loco.TrainBrakeController.DefaultBrakeValue * 100);
                }
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            // Ochrana proti NaN
            if (float.IsNaN(BrakeLine1PressurePSI))
            {
                BrakeLine1PressurePSI = 0;
                AutoCylPressurePSI0 = GetCylPressurePSI();
                AuxResPressurePSI = 0;
            }

            // Výpočet cílového tlaku v brzdovém válci
            threshold = (PrevAuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
            threshold = MathHelper.Clamp(threshold, 0, MCP);

            MSTSLocomotive loco = Car as MSTSLocomotive;
            if (StartOn)
            {
                TrainBrakePositionSet();

                // Vyfouká všechny vozy
                if (!(Car as MSTSWagon).IsDriveable)
                {
                    FullServPressurePSI = 0;
                    AutoCylPressurePSI = 0;
                    AutoCylPressurePSI0 = 0;
                    AuxResPressurePSI = 0;
                    PrevAuxResPressurePSI = 0;
                    BrakeLine1PressurePSI = 0;
                    BrakeLine2PressurePSI = 0;
                    BrakeLine3PressurePSI = 0;
                    prevBrakeLine1PressurePSI = 0;
                }
                // Vyfouká lokomotivu při AirEmpty a nastaví ruční brzdy
                if (IsAirEmpty || !IsAirFull)
                {
                    if (loco != null)
                    {
                        loco.MainResPressurePSI = 0;
                        if (loco.HandBrakePresent)
                        {
                            HandbrakePercent = Simulator.Random.Next(80, 101);
                        }
                        FullServPressurePSI = 0;
                        AutoCylPressurePSI = 0;
                        AutoCylPressurePSI0 = 0;
                        AuxResPressurePSI = 0;
                        PrevAuxResPressurePSI = 0;
                        BrakeLine1PressurePSI = 0;
                        BrakeLine2PressurePSI = 0;
                        BrakeLine3PressurePSI = 0;
                        prevBrakeLine1PressurePSI = 0;
                        TotalCapacityMainResBrakePipe = 0;
                        loco.MainResPressurePSI = 0;
                        loco.AuxResPressurePSI = 0;
                    }
                    if ((Car as MSTSWagon).HandBrakePresent)
                    {
                        if (!(Car as MSTSWagon).IsDriveable)
                            HandbrakePercent = Simulator.Random.Next(80, 101);
                        if (HandBrakeDeactive)
                            HandbrakePercent = 0;
                        if (HandBrakeActive)
                            HandbrakePercent = Simulator.Random.Next(80, 101);
                    }
                }
                //Start vlaku se vzduchem
                else
                if (IsAirFull)
                {
                    if (loco != null)
                    {
                        loco.AuxResPressurePSI = loco.MaxAuxResPressurePSI;
                        HandbrakePercent = loco.HandBrakePresent ? 0 : 0;
                        if (loco.IsLeadLocomotive())
                        {
                            loco.SetEngineBrakePercent(100);
                            AutoCylPressurePSI0 = MaxCylPressurePSI;
                        }
                        loco.LocoReadyToGo = true;
                    }
                    HandbrakePercent = (Car as MSTSWagon).HandBrakePresent ? 0 : 0;
                    BrakeLine1PressurePSI = maxPressurePSI0;
                    BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
                    AuxResPressurePSI = maxPressurePSI0;
                }

                // Definice limitů proměnných pro chod nenaladěných vozidel
                if ((Car as MSTSWagon).Simulator.Settings.CorrectQuestionableBrakingParams)
                {
                    if (loco != null) // Lokomotiva
                    {
                        MaxReleaseRatePSIpS = ReleaseRatePSIpS = MathHelper.Clamp(MaxReleaseRatePSIpS, 0.15f * 14.50377f, 0.5f * 14.50377f);
                        MaxApplicationRatePSIpS = MathHelper.Clamp(MaxApplicationRatePSIpS, 0.5f * 14.50377f, 1.0f * 14.50377f);
                    }
                    else // Vagón
                    {
                        MaxReleaseRatePSIpS = ReleaseRatePSIpS = MathHelper.Clamp(MaxReleaseRatePSIpS, 0.15f * 14.50377f, 0.5f * 14.50377f);
                        MaxApplicationRatePSIpS = MathHelper.Clamp(MaxApplicationRatePSIpS, 0.5f * 14.50377f, 1.0f * 14.50377f);
                    }
                }
                MaxReleaseRatePSIpS0 = MaxReleaseRatePSIpS;
                MaxApplicationRatePSIpS0 = MaxApplicationRatePSIpS;

                StartOn = false;

                if (ForceWagonLoaded)
                {
                    BrakeCarModePL = 1;
                    BrakeCarModeTextPL = Simulator.Catalog.GetString("Loaded");
                }
                else
                {
                    BrakeCarModePL = 0; // Default režim 
                    BrakeCarModeTextPL = Simulator.Catalog.GetString("Empty");
                }

                if (ForceTwoPipesConnection)
                {
                    TwoPipesConnectionMenu = 1;
                    TwoPipesConnectionText = Simulator.Catalog.GetString("connect");
                }
                else
                {
                    TwoPipesConnectionMenu = 0; // Default režim 
                    TwoPipesConnectionText = Simulator.Catalog.GetString("disconnect");
                }

                if (ForceBrakeMode != null)
                {
                    switch (ForceBrakeMode)
                    {
                        case "G":
                            if ((Car as MSTSWagon).WagonType == TrainCar.WagonTypes.Engine || (Car as MSTSWagon).WagonType == TrainCar.WagonTypes.Freight)
                            {
                                BrakeCarMode = 0;
                                BrakeCarModeText = ForceBrakeMode;
                            }
                            break;
                        case "P":
                            BrakeCarMode = 1;
                            BrakeCarModeText = ForceBrakeMode;
                            break;
                        case "R":
                            if ((Car as MSTSWagon).WagonType == TrainCar.WagonTypes.Engine || (Car as MSTSWagon).WagonType == TrainCar.WagonTypes.Passenger)
                            {
                                BrakeCarMode = 2;
                                BrakeCarModeText = ForceBrakeMode;
                            }
                            break;
                        case "R+MG":
                            if ((Car as MSTSWagon).WagonType == TrainCar.WagonTypes.Engine || (Car as MSTSWagon).WagonType == TrainCar.WagonTypes.Passenger)
                            {
                                BrakeCarMode = 3;
                                BrakeCarModeText = ForceBrakeMode;
                            }
                            break;
                    }
                }
            }

            // Definice limitů proměnných pro chod nenaladěných vozidel
            if ((Car as MSTSWagon).Simulator.Settings.CorrectQuestionableBrakingParams)
            {
                if (loco != null) // Lokomotiva
                {
                    MaxCylPressurePSI = AutoCylPressurePSI = MathHelper.Clamp(MaxCylPressurePSI, 0.0f * 14.50377f, 10.0f * 14.50377f);
                    AuxCylVolumeRatio = MathHelper.Clamp(AuxCylVolumeRatio, 1.5f, 4.0f);
                    loco.BrakeSystem.LocoAuxCylVolumeRatio = AuxCylVolumeRatio;
                    MaxAuxilaryChargingRatePSIpS = MathHelper.Clamp(MaxAuxilaryChargingRatePSIpS, 0.1f * 14.50377f, 0.5f * 14.50377f);
                    EmergResChargingRatePSIpS = MathHelper.Clamp(EmergResChargingRatePSIpS, 0.1f * 14.50377f, 0.5f * 14.50377f);
                    EmergAuxVolumeRatio = MathHelper.Clamp(EmergAuxVolumeRatio, 5.0f, 7.0f);
                    EmergResVolumeM3 = MathHelper.Clamp(EmergResVolumeM3, 0.20f, 0.30f);
                    BrakePipeVolumeM3 = MathHelper.Clamp(BrakePipeVolumeM3, 0.0f, 0.020f);
                }
                else // Vagón
                {
                    MaxCylPressurePSI = AutoCylPressurePSI = MathHelper.Clamp(MaxCylPressurePSI, 0.0f * 14.50377f, 10.0f * 14.50377f);
                    AuxCylVolumeRatio = MathHelper.Clamp(AuxCylVolumeRatio, 1.5f, 4.0f);
                    MaxAuxilaryChargingRatePSIpS = MathHelper.Clamp(MaxAuxilaryChargingRatePSIpS, 0.1f * 14.50377f, 0.5f * 14.50377f);
                    EmergResChargingRatePSIpS = MathHelper.Clamp(EmergResChargingRatePSIpS, 0.1f * 14.50377f, 0.5f * 14.50377f);
                    EmergAuxVolumeRatio = MathHelper.Clamp(EmergAuxVolumeRatio, 0.5f, 1.5f);
                    EmergResVolumeM3 = MathHelper.Clamp(EmergResVolumeM3, 0.050f, 0.100f);
                    BrakePipeVolumeM3 = MathHelper.Clamp(BrakePipeVolumeM3, 0.0f, 0.030f);
                }
            }

            // DebugKoef pro doladění MaxBrakeForce
            if (DebugKoef1 == 0) DebugKoef1 = 1.0f;
            DebugKoef = DebugKoef1 * GetDebugKoef2();

            // Časy pro napouštění a vypouštění brzdového válce v sekundách režimy G, P, R
            float TimeApplyG = 22.0f;
            float TimeReleaseG = 50.0f;

            float TimeApplyP = 5.3f;
            float TimeReleaseP = 22.4f;

            float TimeApplyR = 3.5f;
            float TimeReleaseR = 22.4f;

            // Vypočítá rychlost plnění/vyprazdňování brzdových válců s ohledem na režim
            switch (BrakeCarMode)
            {
                case 0: // Režim G                     
                    if (MaxApplicationRatePSIpSG == 0) MaxApplicationRatePSIpS = MaxApplicationRatePSIpS0 / (TimeApplyG / TimeApplyP);
                    else MaxApplicationRatePSIpS = MaxApplicationRatePSIpSG;

                    if (MaxReleaseRatePSIpSG == 0) MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpS0 / (TimeReleaseG / TimeReleaseP);
                    else MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpSG;
                    break;
                case 1: // Režim P                    
                    if (MaxApplicationRatePSIpSP == 0) MaxApplicationRatePSIpS = MaxApplicationRatePSIpS0;
                    else MaxApplicationRatePSIpS = MaxApplicationRatePSIpSP;

                    if (MaxReleaseRatePSIpSP == 0) MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpS0;
                    else MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpSP;
                    break;
                case 2: // Režim R
                    if (MaxApplicationRatePSIpSR == 0) MaxApplicationRatePSIpS = MaxApplicationRatePSIpS0 / (TimeApplyR / TimeApplyP);
                    else MaxApplicationRatePSIpS = MaxApplicationRatePSIpSR;

                    if (MaxReleaseRatePSIpSR == 0) MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpS0 / (TimeReleaseR / TimeReleaseP);
                    else MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpSR;
                    break;
                case 3: // Režim R+Mg
                    if (MaxApplicationRatePSIpSR == 0) MaxApplicationRatePSIpS = MaxApplicationRatePSIpS0 / (TimeApplyR / TimeApplyP);
                    else MaxApplicationRatePSIpS = MaxApplicationRatePSIpSR;

                    if (MaxReleaseRatePSIpSR == 0) MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpS0 / (TimeReleaseR / TimeReleaseP);
                    else MaxReleaseRatePSIpS = ReleaseRatePSIpS = MaxReleaseRatePSIpSR;
                    break;
            }

            // Zjednodušený model pro AI
            if (!Car.IsPlayerTrain)
            {
                UpdateTripleValveState(threshold);                
                // triple valve is set to charge the brake cylinder
                if ((TripleValveState == ValveState.Apply || TripleValveState == ValveState.Emergency))
                {
                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI0 + dp)
                        dp = (AuxResPressurePSI - AutoCylPressurePSI0) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                    if (AutoCylPressurePSI0 + dp > MaxCylPressurePSI)
                        dp = MaxCylPressurePSI - AutoCylPressurePSI0;
                    if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                        dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                    if (dp < 0)
                        dp = 0;

                    AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                    AutoCylPressurePSI0 += dp;

                    if (TripleValveState == ValveState.Emergency && (Car as MSTSWagon).EmergencyReservoirPresent)
                    {
                        dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                        if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                            dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                        EmergResPressurePSI -= dp;
                        AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                    }
                }
                // triple valve set to release pressure in brake cylinder and EP valve set
                if (TripleValveState == ValveState.Release && HoldingValve == ValveState.Release)
                {
                    if (AutoCylPressurePSI0 > threshold)
                    {
                        AutoCylPressurePSI0 -= elapsedClockSeconds * ReleaseRatePSIpS;
                        if (AutoCylPressurePSI0 < threshold)
                            AutoCylPressurePSI0 = threshold;
                    }

                    if ((Car as MSTSWagon).EmergencyReservoirPresent)
                    {
                        if (!(Car as MSTSWagon).DistributorPresent && AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
                        {
                            float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
                            if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                                dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                            if (BrakeLine1PressurePSI < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                                dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / EmergAuxVolumeRatio;
                            EmergResPressurePSI -= dp;
                            AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                        }
                        if (AuxResPressurePSI > EmergResPressurePSI)
                        {
                            float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
                            if (EmergResPressurePSI + dp > AuxResPressurePSI - dp * EmergAuxVolumeRatio)
                                dp = (AuxResPressurePSI - EmergResPressurePSI) / (1 + EmergAuxVolumeRatio);
                            EmergResPressurePSI += dp;
                            AuxResPressurePSI -= dp * EmergAuxVolumeRatio;
                        }
                    }
                    if (AuxResPressurePSI < BrakeLine1PressurePSI && (NoMRPAuxResCharging || BrakeLine2PressurePSI < BrakeLine1PressurePSI) && !BleedOffValveOpen)
                    {
                        float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS; // Change in pressure for train brake pipe.
                        if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                            dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                        AuxResPressurePSI += dp;
                        BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;  // Adjust the train brake pipe pressure
                    }
                    if (AuxResPressurePSI > BrakeLine1PressurePSI) // Allow small flow from auxiliary reservoir to brake pipe so the triple valve is not sensible to small pressure variations when in release position
                    {
                        float dp = elapsedClockSeconds * BrakeInsensitivityPSIpS;
                        if (AuxResPressurePSI - dp < BrakeLine1PressurePSI + dp * AuxBrakeLineVolumeRatio)
                            dp = (AuxResPressurePSI - BrakeLine1PressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                        AuxResPressurePSI -= dp;
                        BrakeLine1PressurePSI += dp * AuxBrakeLineVolumeRatio;
                    }
                    // AI vyčerpává hlavní jímku při odbrzďování
                    if (loco != null) 
                        loco.MainResPressurePSI -= loco.TrainBrakeController.ApplyRatePSIpS * elapsedClockSeconds * AITrainBrakePipeVolumeM3 / loco.MainResVolumeM3;
                }
                // AI vyčerpává hlavní jímku netěstnostmi v potrubí
                if (loco != null)
                {
                    AITrainLeakage = 0.001f;
                    AITrainBrakePipeVolumeM3 = ((0.032f / 2f) * (0.032f / 2f) * (float)Math.PI) * (2f * Car.Train.Cars.Count + Car.Train.Length);
                    loco.MainResPressurePSI -= AITrainLeakage * elapsedClockSeconds * AITrainBrakePipeVolumeM3 / loco.MainResVolumeM3;
                }
                // AI spouští kompresor
                if (loco != null && loco.PowerOn && loco.Battery)
                {
                    if (loco.MainResPressurePSI < 7.0f * 14.50377f)
                    {
                        AICompressorOn = true;
                        AICompressorOff = false;                        
                    }
                    if (loco.MainResPressurePSI > 9.0f * 14.50377f)
                    {
                        AICompressorOn = false;
                        AICompressorOff = true;
                    }
                    if (AICompressorOn)
                    {
                        loco.AICompressorStartDelay += elapsedClockSeconds;
                        if (loco.AICompressorStartDelay > 10)
                        {
                            loco.MainResPressurePSI += (loco.MainResChargingRatePSIpS + loco.MainResChargingRatePSIpS_2) * elapsedClockSeconds;
                            if (!AICompressorRun)
                            {
                                loco.SignalEvent(Event.CompressorOn);
                                loco.SignalEvent(Event.Compressor2On);
                            }
                            AICompressorRun = true;
                        }
                    }
                    if (AICompressorOff && AICompressorRun)
                    {
                        loco.AICompressorStartDelay = 10;
                        loco.SignalEvent(Event.CompressorOff);
                        loco.SignalEvent(Event.Compressor2Off);                        
                        AICompressorRun = false;
                    }
                }
                if (loco != null && !loco.Battery)
                {
                    AICompressorOn = false;
                    AICompressorOff = true;
                    loco.AICompressorStartDelay = 0;
                    if (AICompressorRun)
                    {                        
                        loco.SignalEvent(Event.CompressorOff);
                        loco.SignalEvent(Event.Compressor2Off);
                        AICompressorRun = false;
                    }
                }
            }
            // Ostatní
            else
            {
                // Pokud bude v sekci Wagon "AutomaticDoors" nebo v sekci Engine "CentralHandlingDoors", nastaví napájecí hadice jako aktivní
                if (AutomaticDoorsCycle < 100
                    && ((Car as MSTSWagon) != null && (Car as MSTSWagon).AutomaticDoors || (Car as MSTSLocomotive) != null && (Car as MSTSLocomotive).CentralHandlingDoors))
                {
                    TwoPipesConnectionMenu = 1;
                    TwoPipesConnectionText = Simulator.Catalog.GetString("connect");
                    AutomaticDoorsCycle++;
                }

                switch (TwoPipesConnectionMenu)
                {
                    case 0:
                        TwoPipesConnection = false;
                        DebugType = "1P";
                        break;
                    case 1:
                        TwoPipesConnection = true;
                        DebugType = "2P";
                        break;
                }

                // Načte hodnotu maximálního tlaku v BV
                if (TwoStateBrake && BrakeCarMode > 1) // Vozy v R, Mg mají nad určitou rychlost plný tlak do válců
                {
                    // Nad zadanou rychlost aktivuje vyšší stupeň brzdění
                    if ((Car as MSTSWagon) != null && (Car as MSTSWagon).AbsSpeedMpS > LowStateOnSpeedEngageLevel
                        || (Car as MSTSLocomotive) != null && (Car as MSTSLocomotive).AbsSpeedMpS > LowStateOnSpeedEngageLevel)
                    {
                        HighPressure = true;
                        LowPressure = false;
                        MCP = MaxCylPressurePSI;
                        T_HighPressure = 0;
                    }
                    // Po dobu 12s se brzdící válce odvětrávají na nižší stupeň brzdění 
                    if ((Car as MSTSWagon) != null && (Car as MSTSWagon).AbsSpeedMpS < LowStateOnSpeedEngageLevel && HighPressure
                        || (Car as MSTSLocomotive) != null && (Car as MSTSLocomotive).AbsSpeedMpS < LowStateOnSpeedEngageLevel && HighPressure)
                    {
                        MCP = BrakeCylinderMaxPressureForLowState;
                        if (T_HighPressure == 0)
                            FromHighToLowPressureRate = (AutoCylPressurePSI0 - MCP) / 12;

                        T_HighPressure += elapsedClockSeconds;
                        if (T_HighPressure < 12)
                        {
                            if (AutoCylPressurePSI0 > MCP)
                                AutoCylPressurePSI0 -= elapsedClockSeconds * FromHighToLowPressureRate; // Rychlost odvětrání po dobu 12s na 1.9bar                                                
                        }
                        else
                        {
                            HighPressure = false;
                            LowPressure = true;
                        }
                    }
                    // Pod 50km/h přepne na nižší stupeň brzdění
                    if ((Car as MSTSWagon) != null && (Car as MSTSWagon).AbsSpeedMpS < 50 / 3.6f && HighPressure
                        || (Car as MSTSLocomotive) != null && (Car as MSTSLocomotive).AbsSpeedMpS < 50 / 3.6f && HighPressure)
                        LowPressure = true;

                    if (LowPressure)
                    {
                        if (AutoCylPressurePSI0 > MCP)
                            AutoCylPressurePSI0 -= elapsedClockSeconds * (1.0f * 14.50377f); // Rychlost odvětrání 1 bar/s
                        MCP = BrakeCylinderMaxPressureForLowState;
                    }
                }
                else
                if (TwoStateBrake && BrakeCarMode < 2) // Vozy v G, P mají omezený tlak do válců
                    MCP = BrakeCylinderMaxPressureForLowState;
                else
                    MCP = MaxCylPressurePSI;

                // Definice default zpoždění náskoku brzdy pro nenaladěné vozy
                switch ((Car as MSTSWagon).WagonType)
                {
                    case MSTSWagon.WagonTypes.Freight:
                        if (BrakeDelayToEngage == 0) BrakeDelayToEngage = 1.0f; // 1s u nákladních vozů
                        break;
                    case MSTSWagon.WagonTypes.Passenger:
                        if (BrakeDelayToEngage == 0) BrakeDelayToEngage = 0.5f; // 0.5s u osobních vozů 
                        break;
                }
                // Defaultní zpoždění náběhu brzdiče pro lokomotivy
                if (BrakeDelayToEngage == 0) BrakeDelayToEngage = 1.0f; // sekundy


                // Nastavení výchozího brzdiče přímočinné brzdy pro zpětnou kompatibilitu
                if (!BP1_EngineBrakeController && !BP2_EngineBrakeController && !LEKOV_EngineBrakeController)
                    BP2_EngineBrakeController = true;

                // Defaultní úbytek tlaku, při kterém dojde k pohnutí brzdícího ústrojí
                if (BrakePipeMinPressureDropToEngage == 0) BrakePipeMinPressureDropToEngage = 0.3f * 14.50377f;

                // Defaultní hodnota pro AutoBailOffOnRatePSIpS
                if (AutoBailOffOnRatePSIpS == 0) AutoBailOffOnRatePSIpS = 1.0f * 14.50377f; // 1bar/s

                // Defaultní citlivost brzd
                if (BrakeSensitivityPSIpS == 0) BrakeSensitivityPSIpS = 0.07252f; // Výchozí nastavení 0.07252PSI/s ( 0.005bar/s)

                // Defaultní minimální tlaky pro brzdu R+Mg
                if (MainResMinimumPressureForMGbrakeActivationPSI == 0) MainResMinimumPressureForMGbrakeActivationPSI = 3.5f * 14.50377f;
                if (BrakePipePressureForMGbrakeActivationPSI == 0) BrakePipePressureForMGbrakeActivationPSI = 3.0f * 14.50377f;

                // Příznak pro dostatek vzduchu v hlavní jímce (virtuální napájecím potrubí)
                if (TotalCapacityMainResBrakePipe > MainResMinimumPressureForMGbrakeActivationPSI)
                    AirForWagon = true;
                else
                    AirForWagon = false;

                

                // Tlak v BV nepřekročí maximální tlak pro BV nadefinovaný v eng lokomotivy
                if (BrakeCylinderMaxSystemPressurePSI == 0) BrakeCylinderMaxSystemPressurePSI = MaxCylPressurePSI * 1.0f; // Výchozí hodnota pro maximální tlak přímočinné brzdy v BV 
                if (AutoCylPressurePSI > BrakeCylinderMaxSystemPressurePSI) AutoCylPressurePSI = BrakeCylinderMaxSystemPressurePSI;

                // Snižuje tlak v potrubí kvůli netěsnosti
                if (BrakeLine1PressurePSI - Car.Train.TotalTrainTrainPipeLeakRate > 0)
                    BrakeLine1PressurePSI -= Car.Train.TotalTrainTrainPipeLeakRate * elapsedClockSeconds;

                // Odvětrání pomocné jímky při přebití
                if (AuxResPressurePSI > maxPressurePSI0 && BrakeLine1PressurePSI < AuxResPressurePSI - 0.1f) AuxResPressurePSI -= elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;

                // Výpočet objemu vzduchu brzdových válců a násobiče pro objem pomocné jímky
                CylVolumeM3 = EmergResVolumeM3 / EmergAuxVolumeRatio / AuxCylVolumeRatio;
                AuxBrakeLineVolumeRatio = EmergResVolumeM3 / EmergAuxVolumeRatio / BrakePipeVolumeM3;

                if (BleedOffValveOpen)
                {
                    if (AuxResPressurePSI < 0.01f && AutoCylPressurePSI < 0.01f && BrakeLine1PressurePSI < 0.01f && (EmergResPressurePSI < 0.01f || !(Car as MSTSWagon).EmergencyReservoirPresent))
                    {
                        BleedOffValveOpen = false;
                    }
                    else
                    {
                        AuxResPressurePSI -= elapsedClockSeconds * MaxApplicationRatePSIpS;
                        if (AuxResPressurePSI < 0)
                            AuxResPressurePSI = 0;

                        AutoCylPressurePSI0 -= elapsedClockSeconds * (1.0f * 14.50377f); // Rychlost odvětrání 1 bar/s                 
                        if (AutoCylPressurePSI0 < 0)
                            AutoCylPressurePSI0 = 0;

                        if ((Car as MSTSWagon).EmergencyReservoirPresent)
                        {
                            EmergResPressurePSI -= elapsedClockSeconds * EmergResChargingRatePSIpS;
                            if (EmergResPressurePSI < 0)
                                EmergResPressurePSI = 0;
                        }
                        TripleValveState = ValveState.Release;
                    }
                }
                else
                    UpdateTripleValveState(threshold);

                // Vypouštění brzdového válce při aktivaci protismykového systému
                if (BailOffOnAntiSkid)
                {
                    TripleValveState = ValveState.Lap;
                    BrakeCylApply = false;
                    TRMg += elapsedClockSeconds;
                    if (TRMg < 1.0f)
                    {
                        if (AutoCylPressurePSI0 > 0.5f * 14.50377f)
                        {                            
                            AutoCylPressurePSI0 -= elapsedClockSeconds * (1.0f * 14.50377f); // Rychlost odvětrání 1 bar/s                            
                        }
                    }
                    if (TRMg > 0.99f) TRMg = 0;
                }
                else
                    UpdateTripleValveState(threshold);

                // Zjistí rychlost změny tlaku v potrubí a v brzdovém válci
                if (T0 > 1) T0 = 0;
                if (T0 == 0.0f)
                {
                    prevBrakeLine1PressurePSI = BrakeLine1PressurePSI;
                    prevAutoCylPressurePSI = AutoCylPressurePSI;
                    prevTotalCapacityMainResBrakePipe = TotalCapacityMainResBrakePipe;
                }
                T0 += elapsedClockSeconds;
                if (T0 > 0.33f && T0 < 0.43f)
                {
                    T0 = 0;
                    MainResChangeRate = (prevTotalCapacityMainResBrakePipe - TotalCapacityMainResBrakePipe) * 3.33f;
                    if (MainResChangeRate < 0)
                        MainResChangeRate = 0;

                    BrakePipeChangeRate = Math.Abs(prevBrakeLine1PressurePSI - BrakeLine1PressurePSI) * 3.33f;

                    if (BrakePipeChangeRate > 1)
                        BrakePipeChangeRateBar = Math.Max(BrakePipeChangeRateBar, BrakePipeChangeRate / 14.50377f);
                    else
                        BrakePipeChangeRateBar = 0;

                    CylinderChangeRate = Math.Abs(prevAutoCylPressurePSI - AutoCylPressurePSI) * 3.33f;
                    if (AutoCylPressurePSI > prevAutoCylPressurePSI)
                        CylinderChangeRateBar = GetCylPressurePSI() / GetMaxCylPressurePSI() * GetMaxApplicationRatePSIpS() / 14.50377f;

                    if (AutoCylPressurePSI < prevAutoCylPressurePSI)
                        CylinderChangeRateBar = GetCylPressurePSI() / GetMaxCylPressurePSI() * GetMaxReleaseRatePSIpS() / 14.50377f;
                }
                //if (loco != null)
                //    loco.Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("CylinderChangeRateBar " + CylinderChangeRateBar));


                // Zaznamená poslední stav pomocné jímky pro určení pracovního bodu pomocné jímky
                if (AutoCylPressurePSI0 < 1 && !BrakeReadyToApply)
                    PrevAuxResPressurePSI = AuxResPressurePSI;

                // triple valve is set to charge the brake cylinder
                if (TripleValveState == ValveState.Apply || TripleValveState == ValveState.Emergency && !CarHasAirStuckBrake_2)
                {
                    BrakeCylRelease = false;

                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;

                    if (dp < 0) dp = 0;
                    if (AutoCylPressurePSI0 + dp > MCP)
                        dp = MCP - AutoCylPressurePSI0;

                    if (dp < 0) dp = 0;
                    if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                        dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;

                    // Otestuje citlivost brzdy, nastartuje časovač zpoždění náběhu brzdy a nastaví příznak pro neukládání threshold                
                    if (BrakePipeChangeRate >= BrakeSensitivityPSIpS)
                    {
                        BrakeCylApply = true;
                        TrainBrakeDelay += elapsedClockSeconds;
                        BrakeReadyToApply = true;
                        BrakeCylReleaseEDBOn = false;
                    }

                    // Plní pomocnou jímku stále stejnou rychlostí 0.1bar/s
                    if (AuxResPressurePSI > maxPressurePSI0 && BrakeLine1PressurePSI > AuxResPressurePSI)
                    {
                        dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                        AuxResPressurePSI += dp;
                    }

                    if (dp < 0) dp = 0;
                    AuxResPressurePSI -= dp / AuxCylVolumeRatio;

                    if (TripleValveState == ValveState.Emergency && (Car as MSTSWagon).EmergencyReservoirPresent)
                    {
                        dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                        if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                            dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                        EmergResPressurePSI -= dp;
                        AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                    }
                }

                // Pokračování časovače pro zpoždění náběhu brzdy
                if (TrainBrakeDelay > 0)
                    TrainBrakeDelay += elapsedClockSeconds;

                // Vynulování časovače při brzdění a tlaku v potrubí menším než je drop brzdícího ústrojí
                if (BrakeCylApply && BrakeLine1PressurePSI > PrevAuxResPressurePSI - BrakePipeMinPressureDropToEngage)
                    TrainBrakeDelay = 0;

                // Napouští brzdový válec            
                if (BrakeCylApply
                    && BrakeLine1PressurePSI < PrevAuxResPressurePSI - BrakePipeMinPressureDropToEngage
                    && ThresholdBailOffOn == 0
                    && BrakeCylApplyMainResPressureOK
                    && !CarHasAirStuckBrake_2
                    && !OLBailOff)
                {
                    if (TrainBrakeDelay > BrakeDelayToEngage - 0.05f && TrainBrakeDelay < BrakeDelayToEngage && AutoCylPressurePSI < 1)
                        AutoCylPressurePSI0 = 0.1f * 14.50377f;
                    if (TrainBrakeDelay > BrakeDelayToEngage + 0.25f)
                    {
                        if (AutoCylPressurePSI0 < threshold)
                        {
                            AutoCylPressurePSI0 += elapsedClockSeconds * MaxApplicationRatePSIpS;
                            if (AutoCylPressurePSI0 > threshold)
                                AutoCylPressurePSI0 = threshold;
                        }
                        else BrakeCylApply = false;
                    }
                }

                // Vypouští brzdový válec
                if (BrakeCylRelease && !CarHasAirStuckBrake_1)
                {
                    if (AutoCylPressurePSI0 > threshold)
                    {
                        AutoCylPressurePSI0 -= elapsedClockSeconds * ReleaseRatePSIpS;
                        if (AutoCylPressurePSI0 < threshold)
                            AutoCylPressurePSI0 = threshold;
                    }
                    else BrakeCylRelease = false;
                }
                // Vypouští brzdový válec, pokud je tlak v potrubí vyšší než 4.84bar            
                if (BrakeLine1PressurePSI > 4.84f * 14.50377f && threshold < (maxPressurePSI0 - (4.84f * 14.50377f)) * AuxCylVolumeRatio && !CarHasAirStuckBrake_1)
                {
                    if (AutoCylPressurePSI0 > 0)
                        AutoCylPressurePSI0 -= elapsedClockSeconds * ReleaseRatePSIpS;
                }

                // triple valve set to release pressure in brake cylinder and EP valve set
                if (TripleValveState == ValveState.Release && HoldingValve == ValveState.Release && !CarHasAirStuckBrake_1)
                {
                    BrakeCylRelease = true;
                    BrakeCylApply = false;
                    BrakeReadyToApply = false;
                    //ThresholdBailOffOn = 0;
                    BrakeCylReleaseEDBOn = false;

                    if ((Car as MSTSWagon).EmergencyReservoirPresent)
                    {
                        if (!(Car as MSTSWagon).DistributorPresent && AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
                        {
                            float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
                            if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                                dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                            if (BrakeLine1PressurePSI < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                                dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / EmergAuxVolumeRatio;
                            EmergResPressurePSI -= dp;
                            AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                        }
                        if (AuxResPressurePSI > EmergResPressurePSI)
                        {
                            float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
                            if (EmergResPressurePSI + dp > AuxResPressurePSI - dp * EmergAuxVolumeRatio)
                                dp = (AuxResPressurePSI - EmergResPressurePSI) / (1 + EmergAuxVolumeRatio);
                            EmergResPressurePSI += dp;
                            AuxResPressurePSI -= dp * EmergAuxVolumeRatio;
                        }
                    }
                    if (AuxResPressurePSI < BrakeLine1PressurePSI && !BleedOffValveOpen)
                    {
                        float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS; // Change in pressure for train brake pipe.
                        if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                            dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                        AuxResPressurePSI += dp;
                        BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;  // Adjust the train brake pipe pressure
                    }
                }

                // Odbržďovač OL2 a OL3
                if (OLBailOffLimitPressurePSI == 0) OLBailOffLimitPressurePSI = 3.2f * 14.50377f; // V15 definuje 3.2bar
                if (OLBailOffType == null) OLBailOffType = "OL2";
                if (Car is MSTSLocomotive
                    && loco.Train.LeadLocomotiveIndex >= 0 && ((MSTSLocomotive)loco.Train.Cars[loco.Train.LeadLocomotiveIndex]).BailOff
                    && loco.Direction != Direction.N
                )
                    OLBailOff = true;
                else
                    OLBailOff = false;

                if (OLBailOff || OL3active)
                {
                    if (BrakeLine1PressurePSI > OLBailOffLimitPressurePSI)
                    {
                        loco.ARRTrainBrakeEngage = false;
                        if (AutoCylPressurePSI0 > 0)
                            AutoCylPressurePSI0 -= elapsedClockSeconds * AutoBailOffOnRatePSIpS;
                        if (AutoCylPressurePSI1 > 0)
                            AutoCylPressurePSI1 -= elapsedClockSeconds * AutoBailOffOnRatePSIpS;
                        BrakeCylApply = false;
                        switch (OLBailOffType)
                        {
                            case "OL2":
                                break;
                            case "OL3":
                                OL3active = true;
                                break;
                        }
                    }
                    else
                        OL3active = false;
                }
                if (BrakeLine1PressurePSI > 4.9f * 14.50377f && BrakeCylRelease || (Car is MSTSLocomotive && (Car as MSTSLocomotive).BrakeSystem.EmergencyBrakeForWagon))
                    OL3active = false;
            }

            if (Car is MSTSLocomotive && !(Car as MSTSLocomotive).PowerOn) PowerForWagon = false;

            if (Car is MSTSLocomotive && (Car as MSTSLocomotive).PowerOn
                || Car is MSTSLocomotive && (Car as MSTSLocomotive).EDBIndependent && (Car as MSTSLocomotive).PowerOnFilter > 0)
            {
                PowerForWagon = true;

                if ((Car as MSTSLocomotive).EmergencyButtonPressed) EmergencyBrakeForWagon = true;
                else EmergencyBrakeForWagon = false;

                BailOffOn = false;
                if (loco.DynamicBrakeAutoBailOff && loco.Train.MUDynamicBrakePercent > 0 && loco.DynamicBrakeForceCurves == null)
                {
                    BailOffOn = true;
                }
                else if (loco.DynamicBrakeAutoBailOff && loco.Train.MUDynamicBrakePercent > 0 && loco.DynamicBrakeForceCurves != null)
                {
                    var dynforce = loco.DynamicBrakeForceCurves.Get(1.0f, loco.AbsSpeedMpS);  // max dynforce at that speed
                    if ((loco.MaxDynamicBrakeForceN == 0 && dynforce > 0) || dynforce > loco.MaxDynamicBrakeForceN * 0.6)
                        BailOffOn = true;
                }
                if (BailOffOnAntiSkid)
                {
                    AutoCylPressurePSI0 -= MaxReleaseRatePSIpS * elapsedClockSeconds;
                }

                if (BailOffOn && AutoCylPressurePSI0 > 0 && BrakeCylReleaseEDBOn)
                {
                    ThresholdBailOffOn = (maxPressurePSI0 - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                    ThresholdBailOffOn = MathHelper.Clamp(ThresholdBailOffOn, 0, MCP);
                    AutoCylPressurePSI0 -= elapsedClockSeconds * AutoBailOffOnRatePSIpS; // Rychlost odvětrání při EDB                    
                }

                if (AutoCylPressurePSI0 < 1)
                    BailOffOn = false;

                if (AutoCylPressurePSI0 > threshold - 1)
                    BrakeCylReleaseEDBOn = true;

                // Automatické napuštění brzdového válce po uvadnutí EDB
                float AirWithEDBMotiveForceN = loco.MaxDynamicBrakeForceN * 0.20f;
                if (ThresholdBailOffOn > 0 && Math.Abs(loco.DynamicBrakeForceN) <= AirWithEDBMotiveForceN) // Napustí brzdový válec pod limit síly k EDB
                {
                    ThresholdBailOffOn = (maxPressurePSI0 - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                    if (AutoCylPressurePSI0 < ThresholdBailOffOn
                        && loco.MainResPressurePSI > 0
                        && AutoCylPressurePSI < loco.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                        && AutoCylPressurePSI < loco.MainResPressurePSI)
                    {
                        EDBEngineBrakeDelay += elapsedClockSeconds;
                        if (EDBEngineBrakeDelay > BrakeDelayToEngage - 0.05f && EDBEngineBrakeDelay < BrakeDelayToEngage && AutoCylPressurePSI < 1)
                            AutoCylPressurePSI0 = 0.1f * 14.50377f;
                        if (EDBEngineBrakeDelay > BrakeDelayToEngage + 0.25f && !OL3active)
                        {
                            AutoCylPressurePSI0 += elapsedClockSeconds * AutoBailOffOnRatePSIpS; // Rychlost napouštění po uvadnutí EDB
                        }
                        loco.MainResPressurePSI -= elapsedClockSeconds * AutoBailOffOnRatePSIpS * loco.BrakeSystem.BrakePipeVolumeM3 / loco.MainResVolumeM3;
                    }
                    else
                    if (AutoCylPressurePSI0 >= ThresholdBailOffOn)
                    {
                        threshold = ThresholdBailOffOn;
                        ThresholdBailOffOn = 0;
                        EDBEngineBrakeDelay = 0;
                    }
                }
            }
            else
            {
                if (ThresholdBailOffOn > 0)
                    threshold = ThresholdBailOffOn;
                BailOffOn = false;
                ThresholdBailOffOn = 0;
            }

            if (AutoCylPressurePSI0 < 0)
                AutoCylPressurePSI0 = 0;

            // Výsledný tlak v brzdovém válci            
            AutoCylPressurePSI = 0;
            if (AutoCylPressurePSI < AutoCylPressurePSI0)
                AutoCylPressurePSI = AutoCylPressurePSI0;
            if (AutoCylPressurePSI < AutoCylPressurePSI1)
                AutoCylPressurePSI = AutoCylPressurePSI1;
            if (AutoCylPressurePSI < AutoCylPressurePSI2)
                AutoCylPressurePSI = AutoCylPressurePSI2;

            if (AutoCylPressurePSI < BrakeLine3PressurePSI) // Brake Cylinder pressure will be the greater of engine brake pressure or train brake pressure
                CylPressurePSI = BrakeLine3PressurePSI;
            else
                CylPressurePSI = AutoCylPressurePSI;

            // Record HUD display values for brake cylinders depending upon whether they are wagons or locomotives/tenders (which are subject to their own engine brakes)   
            if (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender)
            {
                Car.Train.HUDLocomotiveBrakeCylinderPSI = CylPressurePSI;
                Car.Train.HUDWagonBrakeCylinderPSI = Car.Train.HUDLocomotiveBrakeCylinderPSI;  // Initially set Wagon value same as locomotive, will be overwritten if a wagon is attached                
            }
            else
            {
                // Record the Brake Cylinder pressure in first wagon, as EOT is also captured elsewhere, and this will provide the two extremeties of the train
                // Identifies the first wagon based upon the previously identified UiD 
                if (Car.UiD == Car.Train.FirstCarUiD)
                {
                    Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSI;
                }
            }

            // If wagons are not attached to the locomotive, then set wagon BC pressure to same as locomotive in the Train brake line
            if (!Car.Train.WagonsAttached && (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender))
            {
                Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSI;
            }

            float f;
            float fRMg;
            // Konstantní síla magnetických bačkor fRMg
            fRMg = Car.MaxBrakeForceNRMg;
            if (!Car.BrakesStuck)
            {
                f = Car.MaxBrakeForceN * Math.Min(CylPressurePSI / MaxCylPressurePSI, 1);
                if (f < Car.MaxHandbrakeForceN * HandbrakePercent / 100)
                    f = Car.MaxHandbrakeForceN * HandbrakePercent / 100;
            }
            else
            f = Math.Max(Car.MaxBrakeForceN, Car.MaxHandbrakeForceN / 2);

            // Síla zaseklých zdrží nebo kotoučů
            if (CarHasMechanicStuckBrake_2)
                 f = Car.MaxBrakeForceN * 0.75f;

            // fRMg není zohledněna v síle na brzdící nápravy
            Car.BrakeRetardForceN = f * Car.BrakeShoeRetardCoefficientFrictionAdjFactor; // calculates value of force applied to wheel, independent of wheel skid

            if (Car.BrakeSkid) // Test to see if wheels are skiding to excessive brake force
            {
                Car.BrakeForceN = (fRMg * Car.RMgShoeCoefficientFrictionAdjFactor) + (f * Car.SkidFriction);   // if excessive brakeforce, wheel skids, and loses adhesion
            }
            else
            {
                Car.BrakeForceN = (fRMg * Car.RMgShoeCoefficientFrictionAdjFactor) + (f * Car.BrakeShoeCoefficientFrictionAdjFactor); // In advanced adhesion model brake shoe coefficient varies with speed, in simple model constant force applied as per value in WAG file, will vary with wheel skid.
            }
                        

            // sound trigger checking runs every half second, to avoid the problems caused by the jumping BrakeLine1PressurePSI value, and also saves cpu time :)
            if (SoundTriggerCounter >= 0.5f)
            {
                SoundTriggerCounter = 0f;
                // Událost pro hodnotu tlaku v brzdovém válci
                if (Math.Abs(AutoCylPressurePSI0 - prevCylPressurePSI) > 0.5f) //(AutoCylPressurePSI != prevCylPressurePSI)
                {
                    if (!TrainBrakePressureChanging)
                    {
                        if (AutoCylPressurePSI0 > prevCylPressurePSI)
                            Car.SignalEvent(Event.TrainBrakePressureIncrease);
                        else
                            Car.SignalEvent(Event.TrainBrakePressureDecrease);
                        TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    }
                }
                else if (TrainBrakePressureChanging)
                {
                    TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    Car.SignalEvent(Event.TrainBrakePressureStoppedChanging);
                }

                // Událost pro hodnotu tlaku v brzdovém potrubí
                if (Math.Abs(BrakeLine1PressurePSI - prevBrakePipePressurePSI) > 0.5f) //BrakeLine1PressurePSI != prevBrakePipePressurePSI
                {
                    if (!BrakePipePressureChanging)
                    {
                        if (BrakeLine1PressurePSI > prevBrakePipePressurePSI)
                            Car.SignalEvent(Event.BrakePipePressureIncrease);
                        else
                            Car.SignalEvent(Event.BrakePipePressureDecrease);
                        BrakePipePressureChanging = !BrakePipePressureChanging;
                    }
                }
                else if (BrakePipePressureChanging)
                {
                    BrakePipePressureChanging = !BrakePipePressureChanging;
                    Car.SignalEvent(Event.BrakePipePressureStoppedChanging);                                        
                }
                prevCylPressurePSI = AutoCylPressurePSI0;
                prevBrakePipePressurePSI = BrakeLine1PressurePSI;                
            }
            SoundTriggerCounter = SoundTriggerCounter + elapsedClockSeconds;
        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            PropagateBrakeLinePressures(elapsedClockSeconds, Car, TwoPipesConnection);
        }

        protected static void PropagateBrakeLinePressures(float elapsedClockSeconds, TrainCar trainCar, bool TwoPipesConnection)
        {
            // Brake pressures are calculated on the lead locomotive first, and then propogated along each wagon in the consist.
            var train = trainCar.Train;
            var lead = trainCar as MSTSLocomotive;
            //var brakePipeTimeFactorS = lead == null ? 0.003f : lead.BrakePipeTimeFactorS; // Průrazná rychlost tlakové vlny 250m/s 0.003f
            var brakePipeTimeFactorS = 0.003f; // Průrazná rychlost tlakové vlny 250m/s 0.003f
            var BrakePipeChargingRatePSIorInHgpS0 = lead == null ? 29 : lead.BrakePipeChargingRatePSIorInHgpS;

            if (train.Simulator.Settings.CorrectQuestionableBrakingParams)
            {
                brakePipeTimeFactorS = MathHelper.Clamp(brakePipeTimeFactorS, 0.001f, 0.01f);
                BrakePipeChargingRatePSIorInHgpS0 = MathHelper.Clamp(BrakePipeChargingRatePSIorInHgpS0, 21, 50);
            }

            // Výpočet z údaje vlaku dlouhého 330m (25 vozů) sníží tlak v hp z 5 na 3.4bar za 22s
            float brakePipeTimeFactorSToTrainLength = train.Length / (330 / (brakePipeTimeFactorS * 7.5f * 25) * train.Cars.Count);
            float brakePipeTimeFactorS_Release = brakePipeTimeFactorSToTrainLength / 3;  // Vytvoří zpoždění tlakové vlny při odbržďování
            float brakePipeTimeFactorS_Apply = brakePipeTimeFactorSToTrainLength; // Vytvoří zpoždění náběhu brzdy vlaku kvůli průrazné tlakové vlně            

            // Výchozí zpoždění tlakové vlny v potrubí 
            float brakePipeTimeFactorSBase = brakePipeTimeFactorS_Release;

            float brakePipeChargingNormalPSIpS = BrakePipeChargingRatePSIorInHgpS0; // Rychlost plnění průběžného potrubí při normálním plnění 29 PSI/s
            float brakePipeChargingQuickPSIpS = 1000; // Rychlost plnění průběžného potrubí při švihu 1000 PSI/s

            int nSteps = (int)(elapsedClockSeconds / brakePipeTimeFactorS + 1);
            float TrainPipeTimeVariationS = elapsedClockSeconds / nSteps;
            bool NotConnected = false;

            float AutoCylPressurePSI = 0;
            if (lead != null)
            {
                if (AutoCylPressurePSI < lead.BrakeSystem.AutoCylPressurePSI0)
                    AutoCylPressurePSI = lead.BrakeSystem.AutoCylPressurePSI0;
                if (AutoCylPressurePSI < lead.BrakeSystem.AutoCylPressurePSI1)
                    AutoCylPressurePSI = lead.BrakeSystem.AutoCylPressurePSI1;
                if (AutoCylPressurePSI < lead.BrakeSystem.AutoCylPressurePSI2)
                    AutoCylPressurePSI = lead.BrakeSystem.AutoCylPressurePSI2;

                // Ohlídá tlak ve válci, aby nebyl vyšší než tlak hlavní jímky
                if (AutoCylPressurePSI < lead.MainResPressurePSI)
                    foreach (TrainCar car in train.Cars)
                        car.BrakeSystem.BrakeCylApplyMainResPressureOK = true;
                else
                    foreach (TrainCar car in train.Cars)
                        car.BrakeSystem.BrakeCylApplyMainResPressureOK = false;

                // Aktivace příznaku rychlobrzdy pro vozy 
                if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Emergency
                    || lead.TrainBrakeController.TCSEmergencyBraking
                    || lead.TrainBrakeController.EmergencyBrakingPushButton)
                    foreach (TrainCar car in train.Cars)
                        car.BrakeSystem.EmergencyBrakeForWagon = true;
                else
                    foreach (TrainCar car in train.Cars)
                        car.BrakeSystem.EmergencyBrakeForWagon = false;

                // Spustí trigger při nouzovém brždění
                if (lead.BrakeSystem.EmergencyBrakeForWagon)
                {
                    lead.ARRTrainBrakeEngage = false;
                    if (lead.BrakeSystem.BrakeLine1PressurePSI > 0 && !lead.BrakeSystem.EmerBrakeTriggerActive)
                    {
                        lead.SignalEvent(Event.TrainBrakeEmergencyActivated);
                        lead.BrakeSystem.EmerBrakeTriggerActive = true;
                    }
                }
                else
                    lead.BrakeSystem.EmerBrakeTriggerActive = false;

                // Aktivace napájení pro vozy, trigger 23 a 24 
                if (lead.BrakeSystem.PowerForWagon)
                    foreach (TrainCar car in train.Cars)
                    {
                        if (car as MSTSLocomotive == null)
                        {
                            car.BrakeSystem.PowerForWagon = true;
                            if (lead.Heating_OffOn)
                                car.SignalEvent(Event.EnginePowerOn);
                            if (!lead.Heating_OffOn)
                                car.SignalEvent(Event.EnginePowerOff);
                        }
                    }
                else
                    foreach (TrainCar car in train.Cars)
                    {
                        if (car as MSTSLocomotive == null)
                        {
                            car.BrakeSystem.PowerForWagon = false;
                            car.SignalEvent(Event.EnginePowerOff);
                        }
                    }

                // Aktivace napájení vzduchem pro vozy 
                if (lead.BrakeSystem.AirForWagon)
                    foreach (TrainCar car in train.Cars)
                        car.BrakeSystem.AirForWagon = true;
                else
                    foreach (TrainCar car in train.Cars)
                        car.BrakeSystem.AirForWagon = false;
            }

            // Výpočet netěsnosti vzduchu v potrubí pro každý vůz
            train.TotalTrainTrainPipeLeakRate = 0f;
            foreach (TrainCar car in train.Cars)
            {
                //  Pokud není netěstnost vozu definována
                if ((car as MSTSWagon).TrainPipeLeakRatePSIpSBase == 0 && !(car as MSTSWagon).BrakeSystem.BrakeCarDeactivate)
                    (car as MSTSWagon).TrainPipeLeakRatePSIpSBase = 0.0010f * 14.50377f; // Výchozí netěsnost 0.0010bar/s                

                if ((car as MSTSWagon).BrakeSystem.CarHasAirStuckBrake_3)
                {
                    (car as MSTSWagon).TrainPipeLeakRatePSIpSBase = 0.0500f * 14.50377f;
                    if ((car as MSTSWagon).BrakeSystem.BrakeCarDeactivate)
                    {
                        (car as MSTSWagon).TrainPipeLeakRatePSIpS = 0;
                        (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3 = 0.00001f;
                    }
                }

                (car as MSTSWagon).TrainPipeLeakRatePSIpS = (car as MSTSWagon).TrainPipeLeakRatePSIpSBase * (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3 / train.TotalTrainBrakePipeVolumeM3;
                (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3 = (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3Base;

                //  První vůz
                if (car == train.Cars[0] && !car.BrakeSystem.AngleCockBOpen) NotConnected = true;

                //  Ostatní kromě prvního a posledního vozu
                if (car != train.Cars[0] && car != train.Cars[train.Cars.Count - 1])
                {
                    if (NotConnected)
                    {
                        (car as MSTSWagon).TrainPipeLeakRatePSIpS = 0;
                        (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3 = 0.00001f;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                    if (!car.BrakeSystem.FrontBrakeHoseConnected || !car.BrakeSystem.AngleCockAOpen)
                    {
                        NotConnected = true;
                        (car as MSTSWagon).TrainPipeLeakRatePSIpS = 0;
                        (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3 = 0.00001f;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                    if (!car.BrakeSystem.AngleCockBOpen) NotConnected = true;
                }

                //  Poslední vůz
                if (car != train.Cars[0] && car == train.Cars[train.Cars.Count - 1])
                {
                    if (NotConnected)
                    {
                        (car as MSTSWagon).TrainPipeLeakRatePSIpS = 0;
                        (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3 = 0.00001f;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                    if (!car.BrakeSystem.FrontBrakeHoseConnected || !car.BrakeSystem.AngleCockAOpen)
                    {
                        (car as MSTSWagon).TrainPipeLeakRatePSIpS = 0;
                        (car as MSTSWagon).BrakeSystem.BrakePipeVolumeM3 = 0.00001f;
                        //car.BrakeSystem.KapacitaHlJimkyAPotrubi = 0;
                    }
                }

                // Spočítá celkovou netěsnost vlaku 
                train.TotalTrainTrainPipeLeakRate += (car as MSTSWagon).TrainPipeLeakRatePSIpS;
            }

            // Propagate brake line (1) data if pressure gradient disabled            
            // approximate pressure gradient in train pipe line1
            float serviceTimeFactor = lead != null ? lead.TrainBrakeController != null && lead.TrainBrakeController.EmergencyBraking ? lead.BrakeEmergencyTimeFactorS : lead.BrakeServiceTimeFactorS : 0;

            // Definice limitů pro základní proměnné vzduchařiny
            serviceTimeFactor = MathHelper.Clamp(serviceTimeFactor, 1.009f, 2.0f);
            if (train.Simulator.Settings.CorrectQuestionableBrakingParams)
            {                
                if (lead != null)
                {
                    lead.TrainBrakeController.QuickReleaseRatePSIpS = MathHelper.Clamp(lead.TrainBrakeController.QuickReleaseRatePSIpS, 0.0f, 7.0f * 14.50377f);
                    lead.TrainBrakeController.ReleaseRatePSIpS = MathHelper.Clamp(lead.TrainBrakeController.ReleaseRatePSIpS, 0.25f * 14.50377f, 0.5f * 14.50377f);
                    lead.TrainBrakeController.ApplyRatePSIpS = MathHelper.Clamp(lead.TrainBrakeController.ApplyRatePSIpS, 0.25f * 14.50377f, 0.5f * 14.50377f);
                    lead.TrainBrakeController.EmergencyRatePSIpS = MathHelper.Clamp(lead.TrainBrakeController.EmergencyRatePSIpS, 3.0f * 14.50377f, 4.0f * 14.50377f);
                    lead.EngineBrakeController.ReleaseRatePSIpS = MathHelper.Clamp(lead.EngineBrakeController.ReleaseRatePSIpS, 1.0f * 14.50377f, 2.5f * 14.50377f);
                    lead.EngineBrakeController.ApplyRatePSIpS = MathHelper.Clamp(lead.EngineBrakeController.ApplyRatePSIpS, 1.0f * 14.50377f, 2.5f * 14.50377f);
                }
            }
            
            for (int i = 0; i < nSteps; i++)
            {
                if (lead != null)
                {
                    // Zajistí funkci zkrokování nebo znulování výkonu při použití brzdy
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Apply
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.EPApply
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.EPFullServ
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullServ
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Emergency)
                    {
                        if (lead.ThrottlePercent > 0 && lead.DoesBrakeCutPower && (lead.DoesPowerLossResetControls || lead.DoesPowerLossResetControls2))
                        {
                            if (lead.DoesPowerLossResetControls)
                                lead.StartThrottleToZero(0.0f);
                            if (lead.DoesPowerLossResetControls2)
                                lead.ThrottleController.SetPercent(0);
                        }
                    }

                    // Ohlídá hodnotu v hlavní jímce, aby nepřekročila limity
                    lead.MainResPressurePSI = MathHelper.Clamp(lead.MainResPressurePSI, 0, lead.MaxMainResPressurePSI + 1.0f * 14.50377f);

                    // Výchozí hodnota pro nízkotlaké přebití je 5.4 barů, pokud není definována v sekci engine
                    if (lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI == 0) lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI = 5.4f * 14.50377f;

                    // Výchozí hodnota pro odvětrávání 3 minuty 0.00222bar/s, pokud není definována v sekci engine
                    if (lead.BrakeSystem.OverchargeEliminationRatePSIpS == 0) lead.BrakeSystem.OverchargeEliminationRatePSIpS = 0.00222f * 14.50377f;

                    // Pohlídá tlak v equalizéru, aby nebyl větší než tlak hlavní jímky
                    if (train.EqualReservoirPressurePSIorInHg > lead.MainResPressurePSI) train.EqualReservoirPressurePSIorInHg = lead.MainResPressurePSI;

                    // Rozsvítí kontrolku průtoku vzduchu, pokud je změna tlaku v potrubí vyšší než 0.01bar/s
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Release)
                    {
                        lead.BrakeSystem.BrakeCylReleaseFlow = true;
                        lead.ARRTrainBrakeEngage = false;
                    }
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Apply)
                    {
                        lead.BrakeSystem.BrakeCylReleaseFlow = false;
                        lead.ARRTrainBrakeEngage = false;
                    }
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Neutral && !lead.ARRTrainBrakeEngage)
                        lead.BrakeSystem.BrakeCylReleaseFlow = true;

                    if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap
                        && !lead.LapActive
                        && lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Emergency
                        && !lead.TrainBrakeController.TCSEmergencyBraking
                        && !lead.EmergencyButtonPressed
                        && lead.BrakeSystem.BrakeCylReleaseFlow)
                    {
                        if ((lead.BrakeSystem.BrakePipeChangeRate > 0.01f * 14.50377f 
                            /*&& lead.BrakeSystem.MainResChangeRate > 0.01f * 14.50377f*/)
                            && !lead.ARRTrainBrakeEngage)
                            lead.BrakeSystem.BrakePipeFlow = true;
                        else lead.BrakeSystem.BrakePipeFlow = false;
                    }
                    else lead.BrakeSystem.BrakePipeFlow = false;

                    // Vyrovnává maximální tlak s tlakem v potrubí    
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Lap || lead.LapActive) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI;

                    // Změna rychlosti plnění vzduchojemu při švihu
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease
                        || lead.QuickReleaseButton && lead.QuickReleaseButtonEnable
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.MatrosovRelease
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.WestingHouseRelease)
                    {
                        lead.ARRTrainBrakeEngage = false;
                        BrakePipeChargingRatePSIorInHgpS0 = brakePipeChargingQuickPSIpS;  // Rychlost plnění ve vysokotlakém švihu 
                        if (lead.TrainBrakeController.MaxPressurePSI < lead.MainResPressurePSI) lead.TrainBrakeController.MaxPressurePSI = lead.MainResPressurePSI;
                    }

                    else
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.OverchargeStart
                        || lead.LowPressureReleaseButton && lead.LowPressureReleaseButtonEnable)
                    {
                        lead.ARRTrainBrakeEngage = false;
                        BrakePipeChargingRatePSIorInHgpS0 = brakePipeChargingQuickPSIpS;  // Rychlost plnění ve vysokotlakém švihu 
                        if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI * 1.11f) lead.TrainBrakeController.MaxPressurePSI -= lead.TrainBrakeController.QuickReleaseRatePSIpS * elapsedClockSeconds / 12;
                        else if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI) lead.TrainBrakeController.MaxPressurePSI -= lead.TrainBrakeController.QuickReleaseRatePSIpS / 20 * elapsedClockSeconds / 12; // Zpomalí 

                        if (lead.TrainBrakeController.MaxPressurePSI < lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI;
                        lead.BrakeSystem.OverChargeRunning = true;
                    }

                    else if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap && !lead.LapActive)
                    {
                        BrakePipeChargingRatePSIorInHgpS0 = brakePipeChargingNormalPSIpS;  // Standardní rychlost plnění 

                        // Zavádí automatické nízkotlaké přebití pokud je povoleno
                        if (lead.BrakeSystem.AutoOverchargePressure || lead.BrakeSystem.OverChargeRunning)
                        {
                            if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI * 1.11f) lead.TrainBrakeController.MaxPressurePSI -= lead.TrainBrakeController.QuickReleaseRatePSIpS * elapsedClockSeconds / 12;
                            else if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI) lead.TrainBrakeController.MaxPressurePSI -= lead.TrainBrakeController.QuickReleaseRatePSIpS / 20 * elapsedClockSeconds / 12; // Zpomalí 
                            else if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.maxPressurePSI0) lead.TrainBrakeController.MaxPressurePSI -= lead.BrakeSystem.OverchargeEliminationRatePSIpS * elapsedClockSeconds / 12;
                        }
                        else
                        {
                            if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.maxPressurePSI0 * 1.08f) lead.TrainBrakeController.MaxPressurePSI -= lead.TrainBrakeController.QuickReleaseRatePSIpS * elapsedClockSeconds / 12;
                            else if (lead.TrainBrakeController.MaxPressurePSI > lead.BrakeSystem.maxPressurePSI0) lead.TrainBrakeController.MaxPressurePSI -= lead.TrainBrakeController.QuickReleaseRatePSIpS / 20 * elapsedClockSeconds / 12; // Zpomalí 
                        }

                        if (lead.BrakeSystem.BrakeLine1PressurePSI < lead.BrakeSystem.maxPressurePSI0) lead.TrainBrakeController.MaxPressurePSI = lead.BrakeSystem.maxPressurePSI0;
                        if (lead.TrainBrakeController.MaxPressurePSI == lead.BrakeSystem.maxPressurePSI0)
                            lead.BrakeSystem.OverChargeRunning = false;
                    }

                    // Charge train brake pipe - adjust main reservoir pressure, and lead brake pressure line to maintain brake pipe equal to equalising resevoir pressure - release brakes
                    if (lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg && lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap
                        || lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg && lead.TrainBrakeController.TCSEmergencyBraking
                        || lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg && lead.EmergencyButtonPressed
                        || lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg && lead.TrainBrakeController.EmergencyBrakingPushButton)
                    {
                        // Calculate change in brake pipe pressure between equalising reservoir and lead brake pipe
                        float PressureDiffEqualToPipePSI = TrainPipeTimeVariationS * BrakePipeChargingRatePSIorInHgpS0; // default condition - if EQ Res is higher then Brake Pipe Pressure

                        if (lead.BrakeSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > train.EqualReservoirPressurePSIorInHg)
                            PressureDiffEqualToPipePSI = train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;

                        if (lead.BrakeSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > lead.MainResPressurePSI)
                            PressureDiffEqualToPipePSI = lead.MainResPressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;

                        if (PressureDiffEqualToPipePSI < 0)
                            PressureDiffEqualToPipePSI = 0;

                        // U těchto funkcí se kompenzují ztráty vzduchu o netěsnosti
                        if ((lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Release
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.MatrosovRelease
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.WestingHouseRelease
                        || (lead.QuickReleaseButton && lead.QuickReleaseButtonEnable)
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.OverchargeStart
                        || (lead.LowPressureReleaseButton && lead.LowPressureReleaseButtonEnable)
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Running
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Neutral       // Vyrovná ztráty vzduchu pro neutrální pozici kontroléru
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Suppression   // Klesne na tlak v potrubí snížený o FullServicePressureDrop 
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.GSelfLapH    // Postupné odbržďování pro BS2
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.GSelfLap     // Bez postupného odbržďování 
                        || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.EPApply)    // Stupňovité odbržďování pro EP
                        && !lead.ARRTrainBrakeEngage_Apply)
                        {
                            lead.BrakeSystem.BrakeLine1PressurePSI += PressureDiffEqualToPipePSI;  // Increase brake pipe pressure to cover loss
                            lead.MainResPressurePSI = lead.MainResPressurePSI - (PressureDiffEqualToPipePSI * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3);   // Decrease main reservoir pressure
                        }
                    }
                    // reduce pressure in lead brake line if brake pipe pressure is above equalising pressure - apply brakes
                    else
                    if (lead.BrakeSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg && lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap
                        || lead.BrakeSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg && lead.TrainBrakeController.TCSEmergencyBraking
                        || lead.BrakeSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg && lead.EmergencyButtonPressed
                        || lead.BrakeSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg && lead.TrainBrakeController.EmergencyBrakingPushButton)
                    {
                        if (lead.BrakeSystem.BrakeLine1PressurePSI < lead.MainResPressurePSI)
                        {
                            float ServiceVariationFactor = (1 - TrainPipeTimeVariationS / serviceTimeFactor);
                            ServiceVariationFactor = MathHelper.Clamp(ServiceVariationFactor, 0.05f, 1.0f); // Keep factor within acceptable limits - prevent value from going negative

                            if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Emergency
                                || lead.TrainBrakeController.TCSEmergencyBraking
                                || lead.EmergencyButtonPressed
                                || lead.TrainBrakeController.EmergencyBrakingPushButton)
                                lead.BrakeSystem.BrakeLine1PressurePSI *= ServiceVariationFactor / serviceTimeFactor;
                            else lead.BrakeSystem.BrakeLine1PressurePSI *= ServiceVariationFactor;

                            if (lead.TrainBrakeController.MaxPressurePSI <= lead.BrakeSystem.maxPressurePSI0) brakePipeTimeFactorSBase = brakePipeTimeFactorS_Apply;
                        }
                    }

                    train.LeadPipePressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI;  // Keep a record of current train pipe pressure in lead locomotive
                }

                // Propogate lead brake line pressure from lead locomotive along the train to each car
                TrainCar car0 = train.Cars[0];
                float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                float brakePipeVolumeM30 = car0.BrakeSystem.BrakePipeVolumeM3;
                train.TotalTrainBrakePipeVolumeM3 = 0.0f; // initialise train brake pipe volume
                train.TotalCapacityMainResBrakePipe = 0.0f;

                foreach (TrainCar car in train.Cars)
                {
                    // Výpočet objemu potrubí pro každý vůz
                    if (car.BrakeSystem.BrakePipeVolumeM3Base == 0) car.BrakeSystem.BrakePipeVolumeM3Base = ((0.032f / 2) * (0.032f / 2) * (float)Math.PI) * (2 + car.CarLengthM);

                    // Výpočet celkového objemu potrubí
                    train.TotalTrainBrakePipeVolumeM3 += car.BrakeSystem.BrakePipeVolumeM3;

                    // Výpočet celkové kapacity hlavních jímek
                    train.TotalCapacityMainResBrakePipe += car.BrakeSystem.TotalCapacityMainResBrakePipe;

                    float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                    if (car != train.Cars[0] && car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen && car0.BrakeSystem.AngleCockBOpen)
                    {
                        // Based on the principle of pressure equualization between adjacent cars
                        // First, we define a variable storing the pressure diff between cars, but limited to a maximum flow rate depending on pipe characteristics
                        // The sign in the equation determines the direction of air flow.
                        float TrainPipePressureDiffPropogationPSI = (p0 > p1 ? -1 : 1) * Math.Min(TrainPipeTimeVariationS * Math.Abs(p1 - p0) / brakePipeTimeFactorSBase, Math.Abs(p1 - p0));

                        // Air flows from high pressure to low pressure, until pressure is equal in both cars.
                        // Brake pipe volumes of both cars are taken into account, so pressure increase/decrease is proportional to relative volumes.
                        // If TrainPipePressureDiffPropagationPSI equals to p1-p0 the equalization is achieved in one step.
                        car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipePressureDiffPropogationPSI * brakePipeVolumeM30 / (brakePipeVolumeM30 + car.BrakeSystem.BrakePipeVolumeM3);
                        car0.BrakeSystem.BrakeLine1PressurePSI += TrainPipePressureDiffPropogationPSI * car.BrakeSystem.BrakePipeVolumeM3 / (brakePipeVolumeM30 + car.BrakeSystem.BrakePipeVolumeM3);
                    }

                    if (!car.BrakeSystem.FrontBrakeHoseConnected)  // Car front brake hose not connected
                    {
                        if (car.BrakeSystem.AngleCockAOpen) //  AND Front brake cock opened
                        {
                            car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * p1 / (brakePipeTimeFactorSBase * 100);
                            if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                                car.BrakeSystem.BrakeLine1PressurePSI = 0;
                        }

                        if (car0.BrakeSystem.AngleCockBOpen && car != car0) //  AND Rear cock of wagon opened, and car is not the first wagon
                        {
                            car0.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * p0 / (brakePipeTimeFactorSBase * 100);
                            if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                                car.BrakeSystem.BrakeLine1PressurePSI = 0;
                        }
                    }
                    if (car == train.Cars[train.Cars.Count - 1] && car.BrakeSystem.AngleCockBOpen) // Last car in train and rear cock of wagon open
                    {
                        car.BrakeSystem.BrakeLine1PressurePSI -= TrainPipeTimeVariationS * p1 / (brakePipeTimeFactorSBase * 100);
                        if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                            car.BrakeSystem.BrakeLine1PressurePSI = 0;
                    }
                    p0 = car.BrakeSystem.BrakeLine1PressurePSI;
                    car0 = car;
                    brakePipeVolumeM30 = car0.BrakeSystem.BrakePipeVolumeM3;
                }
            }


            // Propagate main reservoir pipe (2) and engine brake pipe (3) data
            int first = -1;
            int last = -1;
            train.FindLeadLocomotives(ref first, ref last);
            float sumpv = 0;
            float sumv = 0;
            int continuousFromInclusive = 0;
            int continuousToExclusive = train.Cars.Count;
            bool TwoPipesConnectionBreak = false;
            int MUCableLocoCount = 0;

            for (int i = 0; i < train.Cars.Count; i++)
            {
                BrakeSystem brakeSystem = train.Cars[i].BrakeSystem;
                if (i < first && (!train.Cars[i + 1].BrakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockBOpen || !train.Cars[i + 1].BrakeSystem.AngleCockAOpen || !train.Cars[i].BrakeSystem.TwoPipesConnection))
                {
                    if (continuousFromInclusive < i + 1)
                    {
                        sumv = sumpv = 0;
                        continuousFromInclusive = i + 1;
                    }
                    continue;
                }
                if (i > last && i > 0 && (!brakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockAOpen || !train.Cars[i - 1].BrakeSystem.AngleCockBOpen || !train.Cars[i].BrakeSystem.TwoPipesConnection))
                {
                    if (continuousToExclusive > i)
                        continuousToExclusive = i;
                    continue;
                }
                // Sčítá hlavní jímky pro napojení na napájecí potrubí                
                if (i >= first && i <= last || TwoPipesConnection && continuousFromInclusive <= i && i < continuousToExclusive)
                {
                    sumv += brakeSystem.BrakePipeVolumeM3;
                    sumpv += brakeSystem.BrakePipeVolumeM3 * brakeSystem.BrakeLine2PressurePSI;
                    var eng = train.Cars[i] as MSTSLocomotive;
                    if (eng != null)
                    {
                        sumv += eng.MainResVolumeM3;
                        sumpv += eng.MainResVolumeM3 * eng.MainResPressurePSI;
                    }
                }
                // Testuje propojení napájecích hadic mezi vozy s tlakovými jímkami
                if (i >= first && i <= last)
                {
                    if (!brakeSystem.TwoPipesConnection)
                        TwoPipesConnectionBreak = true;
                }

                // Testuje propojení MU kabelu
                if (train.Cars[i] is MSTSLocomotive)
                {
                    (train.Cars[i] as MSTSLocomotive).MUCable = false;
                    if ((train.Cars[i] as MSTSLocomotive).MUCableEquipment && i >= first && i <= last && continuousFromInclusive <= i && i < continuousToExclusive)
                    {
                        var eng = train.Cars[i] as MSTSLocomotive;
                        if (eng != null)
                        {
                            eng.MUCable = true;
                            MUCableLocoCount++;
                        }
                    }
                }
            }
            if (sumv > 0)
                sumpv /= sumv;


            // Testuje propojení napájecích hadic v celém vlaku
            int T = 0;
            if (lead != null)
            {
                for (int i = 0; i < train.Cars.Count; i++)
                {
                    BrakeSystem MSTSWagon = train.Cars[i].BrakeSystem;
                    MSTSWagon.AirOK_DoorCanManipulate = true;

                    if (!MSTSWagon.TwoPipesConnection)
                    {
                        MSTSWagon.AirOK_DoorCanManipulate = false;
                        T = 1;
                    }
                    else
                    if (lead.MainResPressurePSI > 5 * 14.50377f && T == 0) // 5bar default
                        MSTSWagon.AirOK_DoorCanManipulate = true;
                    else
                        MSTSWagon.AirOK_DoorCanManipulate = false;

                    if (lead.MainResPressurePSI > 5 * 14.50377f && lead.AutomaticDoors)
                        lead.BrakeSystem.AirOK_DoorCanManipulate = true;
                }
            }



            // Počítání hlavních jímek
            // Úbytky vzduchu při manipulaci s dveřmi
            // Spouštění kompresoru na obsazených nebo propojených lokomotivách
            train.BrakeLine2PressurePSI = sumpv;
            for (int i = 0; i < train.Cars.Count; i++)
            {
                var loco = (train.Cars[i] as MSTSLocomotive);
                train.Cars[i].BrakeSystem.TotalCapacityMainResBrakePipe = 0;

                if (i >= first && i <= last || TwoPipesConnection && continuousFromInclusive <= i && i < continuousToExclusive)
                {
                    if (!TwoPipesConnectionBreak)
                        train.Cars[i].BrakeSystem.BrakeLine2PressurePSI = sumpv;

                    if (loco != null && lead != null)
                    {
                        if (!TwoPipesConnectionBreak)
                        {
                            // Použití všech hlavních jímek při propojení napájecího potrubí                             
                            (train.Cars[i] as MSTSLocomotive).MainResPressurePSI = sumpv;
                            train.Cars[i].BrakeSystem.TotalCapacityMainResBrakePipe = (train.Cars[i].BrakeSystem.BrakePipeVolumeM3 * train.Cars[i].BrakeSystem.BrakeLine1PressurePSI) + (loco.MainResVolumeM3 * loco.MainResPressurePSI);
                        }
                        else
                        {
                            // Výpočet kapacity hlavní jímky a přilehlého potrubí                            
                            lead.BrakeSystem.TotalCapacityMainResBrakePipe = (lead.BrakeSystem.BrakePipeVolumeM3 * lead.BrakeSystem.BrakeLine1PressurePSI) + (lead.MainResVolumeM3 * lead.MainResPressurePSI);
                        }

                        // *** Manipulace s dveřmi ***
                        if (loco.CentralHandlingDoors)
                        {
                            BrakeSystem MSTSWagon = train.Cars[i].BrakeSystem;
                            // Snižuje tlak v hlavní jímce kvůli spotřebě vzduchu při otevírání/zavírání dveří
                            if (MSTSWagon.AirOK_DoorCanManipulate)
                            {
                                if (sumv > 0)
                                    lead.MainResPressurePSI -= train.TotalAirLoss / sumv * elapsedClockSeconds;
                                else
                                    lead.MainResPressurePSI -= train.TotalAirLoss * elapsedClockSeconds;
                            }
                        }


                        // *** Kompresory ***

                        // Propojení hlavní jímky s pomocnou jímkou pomocného kompresoru
                        if (loco.AuxCompressor && loco.MainResPressurePSI > loco.AuxResPressurePSI && loco.AuxResPressurePSI < loco.MaxAuxResPressurePSI)
                        {
                            loco.MainResPressurePSI -= 2 * (loco.AuxResVolumeM3 * loco.MaxAuxResPressurePSI / (loco.MainResVolumeM3 * loco.MaxMainResPressurePSI)) * 5 * elapsedClockSeconds;
                            loco.AuxResPressurePSI += 5 * elapsedClockSeconds;
                        }

                        // Netěsnosti jímky pomocného kompresoru
                        if (loco.AuxResPressurePSI > 0)
                            loco.AuxResPressurePSI -= loco.AuxResPipeLeak * elapsedClockSeconds * 1;

                        // Minimální mez pro dostatek vzduchu pro pantografy
                        if (loco.AuxResPressurePSI >= loco.MinAuxPressurePantoPSI || !loco.AuxCompressor)
                            loco.AirForPantograph = true;
                        else loco.AirForPantograph = false;

                        // Minimální mez pro dostatek vzduchu pro HV
                        if (loco.AuxResPressurePSI >= loco.MinAuxPressureHVPSI || !loco.AuxCompressor)
                            loco.AirForHV = true;
                        else loco.AirForHV = false;

                        // Automatický náběh kompresoru u dieselelektrické trakce
                        if (loco is MSTSDieselLocomotive || loco is MSTSSteamLocomotive)
                        {
                            if (loco.BrakeSystem.AuxPowerOnDelayS == 0) loco.BrakeSystem.AuxPowerOnDelayS = 10; // Default 10s
                            if (loco.Compressor_I || !loco.Compressor_II)
                            {
                                loco.CompressorMode_OffAuto = true;
                                if (!loco.PowerOn)
                                    loco.BrakeSystem.CompressorT0 = 0;
                                // Zpoždění náběhu kompresoru
                                if (loco.CompressorMode_OffAuto && !loco.CompressorIsOn)
                                {
                                    loco.BrakeSystem.CompressorT0 += elapsedClockSeconds;
                                    if (loco.BrakeSystem.CompressorT0 > loco.BrakeSystem.AuxPowerOnDelayS)
                                    {
                                        loco.BrakeSystem.CompressorOnDelay = true;
                                    }
                                    else loco.BrakeSystem.CompressorOnDelay = false;
                                }
                            }
                            if (loco.Compressor_II)
                            {
                                loco.CompressorMode2_OffAuto = true;
                                if (!loco.PowerOn)
                                    loco.BrakeSystem.Compressor2T0 = 0;
                                // Zpoždění náběhu kompresoru
                                if (loco.CompressorMode2_OffAuto && !loco.Compressor2IsOn)
                                {
                                    loco.BrakeSystem.Compressor2T0 += elapsedClockSeconds;
                                    if (loco.BrakeSystem.Compressor2T0 > loco.BrakeSystem.AuxPowerOnDelayS)
                                    {
                                        loco.BrakeSystem.Compressor2OnDelay = true;
                                    }
                                    else loco.BrakeSystem.Compressor2OnDelay = false;
                                }
                            }
                            if (loco.AuxCompressor)
                            {
                                //loco.AuxCompressorMode_OffOn = true;  // Vždy ruční spuštění
                                // Zpoždění náběhu kompresoru
                                if (loco.AuxCompressorMode_OffOn && !loco.AuxCompressorIsOn)
                                {
                                    loco.BrakeSystem.AuxCompressorT0 += elapsedClockSeconds;
                                    if (loco.BrakeSystem.AuxCompressorT0 > 1)
                                    {
                                        loco.BrakeSystem.AuxCompressorOnDelay = true;
                                        loco.BrakeSystem.AuxCompressorT0 = 0;
                                    }
                                    else loco.BrakeSystem.AuxCompressorOnDelay = false;
                                }
                            }
                        }

                        if (loco is MSTSElectricLocomotive)
                        {
                            // Zpoždění náběhu kompresoru
                            if (loco.Compressor_I || !loco.Compressor_II)
                            {
                                if ((loco.CompressorMode_OffAuto || loco.Compressor_I_HandMode) && !loco.CompressorIsOn)
                                {
                                    loco.BrakeSystem.CompressorT0 += elapsedClockSeconds;
                                    if (loco.BrakeSystem.CompressorT0 > 1) // 1s
                                    {
                                        loco.BrakeSystem.CompressorOnDelay = true;
                                        loco.BrakeSystem.CompressorT0 = 0;
                                    }
                                    else loco.BrakeSystem.CompressorOnDelay = false;
                                }
                            }
                            if (loco.Compressor_II)
                            {
                                if ((loco.CompressorMode2_OffAuto || loco.Compressor_II_HandMode) && !loco.Compressor2IsOn)
                                {
                                    loco.BrakeSystem.Compressor2T0 += elapsedClockSeconds;
                                    if (loco.BrakeSystem.Compressor2T0 > 1) // 1s
                                    {
                                        loco.BrakeSystem.Compressor2OnDelay = true;
                                        loco.BrakeSystem.Compressor2T0 = 0;
                                    }
                                    else loco.BrakeSystem.Compressor2OnDelay = false;
                                }
                            }
                            if (loco.AuxCompressor)
                            {
                                if (loco.AuxCompressorMode_OffOn && !loco.AuxCompressorIsOn)
                                {
                                    loco.BrakeSystem.AuxCompressorT0 += elapsedClockSeconds;
                                    if (loco.BrakeSystem.AuxCompressorT0 > 1) // 1s
                                    {
                                        loco.BrakeSystem.AuxCompressorOnDelay = true;
                                        loco.BrakeSystem.AuxCompressorT0 = 0;
                                    }
                                    else loco.BrakeSystem.AuxCompressorOnDelay = false;
                                }
                            }
                        }

                        // Tlakový ventil při ručním provozu kompresorů
                        if (loco.MaxMainResOverPressurePSI == 0)
                            loco.MaxMainResOverPressurePSI = loco.MaxMainResPressurePSI + (0.60f * 14.50377f);

                        if (loco.MainResPressurePSI > loco.MaxMainResOverPressurePSI)
                            loco.MainResOverPressure = true;

                        if (loco.MainResPressurePSI > loco.MaxMainResPressurePSI && loco.MainResOverPressure)
                        {
                            loco.MainResPressurePSI -= 0.25f * 14.50377f * elapsedClockSeconds;
                            loco.SignalEvent(Event.MaxMainResOverPressureValveOpen);
                        }
                        else
                        {
                            loco.MainResOverPressure = false;
                            loco.SignalEvent(Event.MaxMainResOverPressureValveClosed);
                        }

                        // Tlakový ventil při provozu pomocného kompresoru  
                        if (loco.MaxAuxResOverPressurePSI == 0)
                            loco.MaxAuxResOverPressurePSI = loco.MaxAuxResPressurePSI + (0.50f * 14.50377f);

                        if (loco.AuxResPressurePSI > loco.MaxAuxResOverPressurePSI)
                            loco.AuxResOverPressure = true;

                        if (loco.AuxResPressurePSI > loco.MaxAuxResPressurePSI && loco.AuxResOverPressure)
                        {
                            loco.AuxResPressurePSI -= 0.25f * 14.50377f * elapsedClockSeconds;
                            loco.SignalEvent(Event.MaxAuxResOverPressureValveOpen);
                        }
                        else
                        {
                            loco.AuxResOverPressure = false;
                            loco.SignalEvent(Event.MaxAuxResOverPressureValveClosed);
                        }

                        // Automatický restart pomocného kompresoru, pokud je zadáno
                        bool AuxResRestart = false;
                        if (loco.AuxCompressorRestartPressurePSI != 0) AuxResRestart = true;

                        if ((loco.AuxResPressurePSI <= loco.AuxCompressorRestartPressurePSI && AuxResRestart || !AuxResRestart)
                            && loco.Battery && loco.PowerKey
                            && loco.AuxCompressorMode_OffOn
                            && loco.BrakeSystem.AuxCompressorOnDelay
                            && !loco.AuxCompressorIsOn)
                            loco.SignalEvent(Event.AuxCompressorOn);

                        bool genModeActive = false;
                        if (loco.extendedPhysics != null)
                        {
                            genModeActive = loco.extendedPhysics.GeneratoricModeActive;
                        }

                        if ((loco.MainResPressurePSI <= loco.CompressorRestartPressurePSI || loco.Compressor_I_HandMode)
                            && (loco.AuxPowerOn || genModeActive)
                            && (loco.CompressorMode_OffAuto || loco.Compressor_I_HandMode)
                            && loco.BrakeSystem.CompressorOnDelay
                            && !loco.CompressorIsOn)
                            loco.SignalEvent(Event.CompressorOn);

                        if ((loco.MainResPressurePSI <= loco.CompressorRestartPressurePSI || loco.Compressor_II_HandMode)
                            && (loco.AuxPowerOn || genModeActive)
                            && (loco.CompressorMode2_OffAuto || loco.Compressor_II_HandMode)
                            && (loco.BrakeSystem.Compressor2OnDelay || genModeActive)
                            && !loco.Compressor2IsOn)
                            loco.SignalEvent(Event.Compressor2On);


                        if ((loco.AuxResPressurePSI >= loco.MaxAuxResPressurePSI && AuxResRestart
                            || (!loco.Battery || !loco.PowerKey)
                            || !loco.AuxCompressorMode_OffOn)
                            && loco.AuxCompressorIsOn)
                            loco.SignalEvent(Event.AuxCompressorOff);

                        if (((loco.MainResPressurePSI >= loco.MaxMainResPressurePSI && !loco.Compressor_I_HandMode)
                            //|| (loco.MainResPressurePSI >= loco.MaxMainResOverPressurePSI && loco.Compressor_I_HandMode)
                            || !loco.AuxPowerOn
                            || (!loco.CompressorMode_OffAuto && !loco.Compressor_I_HandMode))
                            && !genModeActive
                            && loco.CompressorIsOn)
                            loco.SignalEvent(Event.CompressorOff);


                        if (((loco.MainResPressurePSI >= loco.MaxMainResPressurePSI && !loco.Compressor_II_HandMode)
                            //|| (loco.MainResPressurePSI >= loco.MaxMainResOverPressurePSI && loco.Compressor_II_HandMode)
                            || !loco.AuxPowerOn
                            || (!loco.CompressorMode2_OffAuto && !loco.Compressor_II_HandMode))
                            && !genModeActive 
                            && loco.Compressor2IsOn)
                            loco.SignalEvent(Event.Compressor2Off);
                    }
                }
            }

            bool Apply = false;
            bool ApplyGA = false;
            bool Release = false;
            bool Lap = false;
            bool Overcharge = false;
            bool QuickRelease = false;
            bool Running = false;
            bool SlowApplyStart = false;
            bool Emergency = false;
            bool Neutral = false;
            bool MatrosovRelease = false;
            bool WestingHouseRelease = false;
            int SumSA = 0;
            int SumA = 0;
            int SumGA = 0;
            int SumRu = 0;
            int SumRe = 0;
            int SumQRe = 0;
            int SumO = 0;
            int SumMaRe = 0;
            int SumWHRe = 0;

            // Nastavení příznaků pro vozy
            for (int i = 0; i < train.Cars.Count; i++)
            {
                var engine = train.Cars[i] as MSTSLocomotive;

                // Detekce nastavení polohy brzdiče průběžné brzdy                
                if (engine != null && lead != null)
                {
                    if (!engine.LapActive || engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Emergency)
                    {
                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Release)
                        {
                            engine.BrakeSystem.NextLocoRelease = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Release position");
                            Release = true;
                            SumRe++;
                            lead.BrakeSystem.ReleaseTr = 1;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.Release)
                            engine.BrakeSystem.NextLocoRelease = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Apply)
                        {
                            engine.BrakeSystem.NextLocoApply = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Apply position");
                            Apply = true;
                            SumA++;
                            lead.BrakeSystem.ReleaseTr = 0;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.Apply)
                            engine.BrakeSystem.NextLocoApply = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease || engine.QuickReleaseButton && engine.QuickReleaseButtonEnable)
                        {
                            engine.BrakeSystem.NextLocoQuickRelease = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Highpressure release");
                            QuickRelease = true;
                            SumQRe++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.FullQuickRelease && !engine.QuickReleaseButton)
                            engine.BrakeSystem.NextLocoQuickRelease = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Emergency)
                        {
                            engine.BrakeSystem.NextLocoEmergency = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Emergency");
                            Emergency = true;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.Emergency)
                            engine.BrakeSystem.NextLocoEmergency = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.OverchargeStart || engine.LowPressureReleaseButton && engine.LowPressureReleaseButtonEnable)
                        {
                            engine.BrakeSystem.NextLocoOvercharge = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Lowpressure release");
                            Overcharge = true;
                            SumO++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.OverchargeStart && !engine.LowPressureReleaseButton)
                            engine.BrakeSystem.NextLocoOvercharge = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Lap)
                        {
                            engine.BrakeSystem.NextLocoLap = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Lap");
                            Lap = true;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap)
                            engine.BrakeSystem.NextLocoLap = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Running)
                        {
                            engine.BrakeSystem.NextLocoRunning = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Driving position");
                            Running = true;
                            SumRu++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.Running)
                            engine.BrakeSystem.NextLocoRunning = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Neutral)
                        {
                            engine.BrakeSystem.NextLocoNeutral = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Driving position");
                            Neutral = true;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.Neutral)
                            engine.BrakeSystem.NextLocoNeutral = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.Suppression)
                        {
                            engine.BrakeSystem.NextLocoSuppression = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Full braking");
                            ApplyGA = true;
                            SumGA++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.Suppression)
                            engine.BrakeSystem.NextLocoSuppression = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.GSelfLapH)
                        {
                            engine.BrakeSystem.NextLocoGSelfLapH = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Braking");
                            ApplyGA = true;
                            SumGA++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.GSelfLapH)
                            engine.BrakeSystem.NextLocoGSelfLapH = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.SlowApplyStart)
                        {
                            engine.BrakeSystem.NextLocoSlowApplyStart = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Slow braking");
                            SlowApplyStart = true;
                            SumSA++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.SlowApplyStart)
                            engine.BrakeSystem.NextLocoSlowApplyStart = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.MatrosovRelease)
                        {
                            engine.BrakeSystem.NextLocoMatrosovRelease = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Release position");
                            MatrosovRelease = true;
                            SumMaRe++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.MatrosovRelease)
                            engine.BrakeSystem.NextLocoMatrosovRelease = false;

                        if (engine.TrainBrakeController.TrainBrakeControllerState == ControllerState.WestingHouseRelease)
                        {
                            engine.BrakeSystem.NextLocoWestingHouseRelease = true;
                            engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Release position");
                            WestingHouseRelease = true;
                            SumWHRe++;
                        }
                        else
                        if (engine.TrainBrakeController.TrainBrakeControllerState != ControllerState.WestingHouseRelease)
                            engine.BrakeSystem.NextLocoWestingHouseRelease = false;
                    }

                    if (engine.LapActive && !Emergency)
                    {
                        engine.BrakeSystem.NextLocoLap = true;
                        engine.BrakeSystem.NextLocoBrakeState = Simulator.Catalog.GetString("Lap");
                        Lap = true;
                    }
                    else
                    if (!engine.LapActive)
                        engine.BrakeSystem.NextLocoLap = false;
                }

            }

            // Upravuje chování řídící jímky 
            if (lead != null)
            {
                // Nastavení volitelných rychlostí vypouštění potrubí 
                if (lead.BrakeSystem.BrakePipeDischargeRate) // Vypouštění
                {
                    lead.TrainBrakeController.ApplyRatePSIpS = lead.BrakeSystem.GetBrakePipeDischargeRate();
                }
                if (lead.BrakeSystem.BrakePipeChargeRate) // Napouštění
                {
                    lead.TrainBrakeController.ReleaseRatePSIpS = lead.BrakeSystem.GetBrakePipeChargeRate();
                }

                // Automatické napouštění při tlaku větším než 4.84bar
                if (train.EqualReservoirPressurePSIorInHg > 4.84f * 14.50377f) lead.BrakeSystem.ReleaseTr = 0;

                // Zpětné automatické dofouknutí při nechtěné manipulace s brzdičem
                if (Neutral && lead.BrakeSystem.ReleaseTr != 1 && !Apply && !ApplyGA && !SlowApplyStart)
                {
                    if (lead.TrainBrakeController.MaxPressurePSI - train.EqualReservoirPressurePSIorInHg < lead.BrakeSystem.BrakePipeMinPressureDropToEngage)
                        train.EqualReservoirPressurePSIorInHg += lead.TrainBrakeController.ReleaseRatePSIpS * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg > lead.TrainBrakeController.MaxPressurePSI)
                        train.EqualReservoirPressurePSIorInHg = lead.TrainBrakeController.MaxPressurePSI;
                }

                if (SlowApplyStart)
                {
                    if (train.EqualReservoirPressurePSIorInHg > 0)
                        train.EqualReservoirPressurePSIorInHg -= lead.TrainBrakeController.ApplyRatePSIpS / 3 * SumSA * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg < 0)
                        train.EqualReservoirPressurePSIorInHg = 0;
                }
                if (ApplyGA)
                {

                }
                if (Apply)
                {
                    if (train.EqualReservoirPressurePSIorInHg > 0)
                        train.EqualReservoirPressurePSIorInHg -= lead.TrainBrakeController.ApplyRatePSIpS * SumA * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg < 0)
                        train.EqualReservoirPressurePSIorInHg = 0;
                }
                if (Running)
                {
                    if (train.EqualReservoirPressurePSIorInHg < lead.TrainBrakeController.MaxPressurePSI)
                        train.EqualReservoirPressurePSIorInHg += lead.TrainBrakeController.RunningReleaseRatePSIpS * SumRu * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg > lead.TrainBrakeController.MaxPressurePSI)
                        train.EqualReservoirPressurePSIorInHg = lead.TrainBrakeController.MaxPressurePSI;
                }
                if (Release)
                {
                    if (train.EqualReservoirPressurePSIorInHg < lead.TrainBrakeController.MaxPressurePSI)
                        train.EqualReservoirPressurePSIorInHg += lead.TrainBrakeController.ReleaseRatePSIpS * SumRe * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg > lead.TrainBrakeController.MaxPressurePSI)
                        train.EqualReservoirPressurePSIorInHg = lead.TrainBrakeController.MaxPressurePSI;
                }
                if (Overcharge)
                {
                    if (train.EqualReservoirPressurePSIorInHg < lead.BrakeSystem.TrainBrakesControllerMaxOverchargePressurePSI)
                        train.EqualReservoirPressurePSIorInHg += lead.TrainBrakeController.ReleaseRatePSIpS * SumO * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg > lead.TrainBrakeController.MaxPressurePSI)
                        train.EqualReservoirPressurePSIorInHg = lead.TrainBrakeController.MaxPressurePSI;
                }
                if (QuickRelease)
                {
                    if (train.EqualReservoirPressurePSIorInHg < lead.MainResPressurePSI)
                        train.EqualReservoirPressurePSIorInHg += lead.TrainBrakeController.QuickReleaseRatePSIpS * SumQRe * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg > lead.MainResPressurePSI)
                        train.EqualReservoirPressurePSIorInHg = lead.MainResPressurePSI;
                }
                if (Emergency)
                {
                    if (train.EqualReservoirPressurePSIorInHg > 0)
                        train.EqualReservoirPressurePSIorInHg -= lead.TrainBrakeController.EmergencyRatePSIpS * elapsedClockSeconds;
                    if (train.EqualReservoirPressurePSIorInHg < 0)
                        train.EqualReservoirPressurePSIorInHg = 0;
                }
                if (MatrosovRelease)
                {
                    // Matrosov odbrzdění
                }
                if (WestingHouseRelease)
                {
                    // WestingHouse odbrzdění
                }
            }

            // Kompenzuje ztráty z hlavní jímky            
            if (Running && SlowApplyStart
                || Release && SlowApplyStart
                || Overcharge && SlowApplyStart
                || QuickRelease && SlowApplyStart)
            {
                lead.MainResPressurePSI -= SumSA * (lead.TrainBrakeController.ApplyRatePSIpS / 3 * elapsedClockSeconds * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3);
                foreach (TrainCar car in train.Cars)
                {
                    if (!car.BrakeSystem.NextLocoLap)
                    {
                        if (car.BrakeSystem.NextLocoSlowApplyStart)
                        {
                            car.SignalEvent(Event.TrainBrakePressureDecrease);
                            car.SignalEvent(Event.BrakePipePressureDecrease);
                        }
                        if (car.BrakeSystem.NextLocoRunning
                            || car.BrakeSystem.NextLocoRelease
                            || car.BrakeSystem.NextLocoOvercharge
                            || car.BrakeSystem.NextLocoQuickRelease)
                        {
                            car.SignalEvent(Event.TrainBrakePressureIncrease);
                            car.SignalEvent(Event.BrakePipePressureIncrease);
                        }
                    }
                    if (car.BrakeSystem.NextLocoLap)
                    {
                        car.SignalEvent(Event.TrainBrakePressureStoppedChanging);
                        car.SignalEvent(Event.BrakePipePressureStoppedChanging);
                    }
                }
            }
            if (Running && ApplyGA
                || Release && ApplyGA
                || Overcharge && ApplyGA
                || QuickRelease && ApplyGA)
            {
                lead.MainResPressurePSI -= SumGA * (lead.TrainBrakeController.ApplyRatePSIpS * elapsedClockSeconds * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3);
                foreach (TrainCar car in train.Cars)
                {
                    if (!car.BrakeSystem.NextLocoLap)
                    {
                        if (car.BrakeSystem.NextLocoGSelfLapH || car.BrakeSystem.NextLocoSuppression)
                        {
                            car.SignalEvent(Event.TrainBrakePressureDecrease);
                            car.SignalEvent(Event.BrakePipePressureDecrease);
                        }
                        if (car.BrakeSystem.NextLocoRunning
                            || car.BrakeSystem.NextLocoRelease
                            || car.BrakeSystem.NextLocoOvercharge
                            || car.BrakeSystem.NextLocoQuickRelease)
                        {
                            car.SignalEvent(Event.TrainBrakePressureIncrease);
                            car.SignalEvent(Event.BrakePipePressureIncrease);
                        }
                    }
                    if (car.BrakeSystem.NextLocoLap)
                    {
                        car.SignalEvent(Event.TrainBrakePressureStoppedChanging);
                        car.SignalEvent(Event.BrakePipePressureStoppedChanging);
                    }
                }
            }
            if (Running && Apply
               || Release && Apply
               || Overcharge && Apply
               || QuickRelease && Apply)
            {
                lead.MainResPressurePSI -= SumA * (lead.TrainBrakeController.ApplyRatePSIpS * elapsedClockSeconds * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3);
                foreach (TrainCar car in train.Cars)
                {
                    if (!car.BrakeSystem.NextLocoLap)
                    {
                        if (car.BrakeSystem.NextLocoApply)
                        {
                            car.SignalEvent(Event.TrainBrakePressureDecrease);
                            car.SignalEvent(Event.BrakePipePressureDecrease);
                        }
                        if (car.BrakeSystem.NextLocoRunning
                            || car.BrakeSystem.NextLocoRelease
                            || car.BrakeSystem.NextLocoOvercharge
                            || car.BrakeSystem.NextLocoQuickRelease)
                        {
                            car.SignalEvent(Event.TrainBrakePressureIncrease);
                            car.SignalEvent(Event.BrakePipePressureIncrease);
                        }
                    }
                    if (car.BrakeSystem.NextLocoLap)
                    {
                        car.SignalEvent(Event.TrainBrakePressureStoppedChanging);
                        car.SignalEvent(Event.BrakePipePressureStoppedChanging);
                    }
                }
            }


            // Úbytky vzduchu při spotřebě vzduchu otevírání a zavírání dvěří            
            train.TotalAirLoss = 0;
            float TotalAirLoss0 = 0;
            train.TrainDoorsOpen = false;
            foreach (TrainCar car in train.Cars)
            {
                float AirLossDoorL;
                float AirLossDoorR;
                var wagon = car as MSTSWagon;

                if (wagon.DoorLeftOpen || wagon.DoorRightOpen)
                {
                    wagon.BrakeSystem.DoorsOpen = true;
                    if (!wagon.LeftDoorOpenOverride && !wagon.RightDoorOpenOverride)
                        train.TrainDoorsOpen = true;
                }
                else
                    wagon.BrakeSystem.DoorsOpen = false;

                if (lead != null && lead.CentralHandlingDoors && wagon.AutomaticDoors && wagon.BrakeSystem.AirOK_DoorCanManipulate)
                {
                    if (wagon.AirlossByHandlingDoorsPSIpS == 0)
                        wagon.AirlossByHandlingDoorsPSIpS = 0.01f * 14.50377f; // Default 0.01bar/s    

                    if (wagon.DoorLeftOpen)
                        AirLossDoorL = wagon.AirlossByHandlingDoorsPSIpS; // otevření
                    else AirLossDoorL = wagon.AirlossByHandlingDoorsPSIpS; // zavření

                    if (wagon.DoorRightOpen)
                        AirLossDoorR = wagon.AirlossByHandlingDoorsPSIpS; // otevření
                    else AirLossDoorR = wagon.AirlossByHandlingDoorsPSIpS; // zavření

                    //if (wagon.AutomaticDoors)
                    TotalAirLoss0 += AirLossDoorL + AirLossDoorR;

                    // Automaticky zavře dveře při vyšší rychlosti než 15km/h
                    if (lead.SpeedMpS > 15.0f / 3.6f && (wagon.DoorLeftOpen || wagon.DoorRightOpen))
                    {
                        wagon.DoorRightOpen = false;
                        wagon.DoorLeftOpen = false;
                        wagon.SignalEvent(Event.DoorClose);
                        car.BrakeSystem.LeftDoorIsOpened = false;
                        car.BrakeSystem.RightDoorIsOpened = false;
                    }
                }
            }

            // Levé dveře
            var loco0 = (train.Cars[0] as MSTSLocomotive);
            if (loco0 != null)
            {
                if (loco0.DoorLeftOpen && !loco0.OpenedLeftDoor)
                {
                    BrakeSystem brakeSystem = loco0.BrakeSystem;
                    if (brakeSystem.T1AirLoss < 0.5f)
                    {
                        train.TotalAirLoss = TotalAirLoss0 / 2;
                        brakeSystem.T1AirLoss += elapsedClockSeconds;
                    }
                    else
                    {
                        loco0.OpenedLeftDoor = true;
                        brakeSystem.T1AirLoss = 0;
                    }
                }
                if (!loco0.DoorLeftOpen && loco0.OpenedLeftDoor)
                {
                    BrakeSystem brakeSystem = loco0.BrakeSystem;
                    if (brakeSystem.T1AirLoss < 0.5f)
                    {
                        train.TotalAirLoss = TotalAirLoss0 / 2;
                        brakeSystem.T1AirLoss += elapsedClockSeconds;
                    }
                    else
                    {
                        loco0.OpenedLeftDoor = false;
                        brakeSystem.T1AirLoss = 0;
                    }
                }

                // Pravé dveře
                if (loco0.DoorRightOpen && !loco0.OpenedRightDoor)
                {
                    BrakeSystem brakeSystem = loco0.BrakeSystem;
                    if (brakeSystem.T2AirLoss < 0.5f)
                    {
                        train.TotalAirLoss = TotalAirLoss0 / 2;
                        brakeSystem.T2AirLoss += elapsedClockSeconds;
                    }
                    else
                    {
                        loco0.OpenedRightDoor = true;
                        brakeSystem.T2AirLoss = 0;
                    }
                }
                if (!loco0.DoorRightOpen && loco0.OpenedRightDoor)
                {
                    BrakeSystem brakeSystem = loco0.BrakeSystem;
                    if (brakeSystem.T2AirLoss < 0.5f)
                    {
                        train.TotalAirLoss = TotalAirLoss0 / 2;
                        brakeSystem.T2AirLoss += elapsedClockSeconds;
                    }
                    else
                    {
                        loco0.OpenedRightDoor = false;
                        brakeSystem.T2AirLoss = 0;
                    }
                }
            }

            if (lead != null)
            {
                // Samostatná přímočinná brzda pro každou lokomotivu
                BrakeSystem brakeSystem = lead.BrakeSystem;
                var prevState = lead.EngineBrakeState;
                train.BrakeLine3PressurePSI = MathHelper.Clamp(train.BrakeLine3PressurePSI, 0, lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI);

                // Při aktivní EDB a použití přímočinné brzdy zruší účinek EDB
                if (lead.BrakeSystem.AutoCylPressurePSI1 > 0 && lead.DynamicBrakePercent > 0)
                    lead.EngineBrakeEngageEDB = true;

                // Propojí přímočinné brzdy, pokud jsou lokomotivy propojené kabelem
                foreach (TrainCar car in train.Cars)
                {
                    if ((car is MSTSLocomotive) && (car as MSTSLocomotive).MUCable && (car as MSTSLocomotive).MUCableEquipment)
                    {
                        car.BrakeSystem.AutoCylPressurePSI1 = lead.BrakeSystem.AutoCylPressurePSI1;
                    }
                }

                // Definice pro brzdič BP1
                if (brakeSystem.BP1_EngineBrakeController)
                {
                    // Určení pozice kontroléru 0.0 - 1.0
                    float EngineBrakeControllerRate = train.BrakeLine3PressurePSI / lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI;
                    EngineBrakeControllerRate = MathHelper.Clamp(EngineBrakeControllerRate, 0, 1);

                    // Definování mrtvé zóny brzdiče                    
                    float EngineBrakeControllerApply = (1 - brakeSystem.EngineBrakeControllerApplyDeadZone) * EngineBrakeControllerRate;
                    float EngineBrakeControllerRelease = brakeSystem.PrevEngineBrakeControllerRateRelease - (brakeSystem.PrevEngineBrakeControllerRateRelease * brakeSystem.EngineBrakeControllerReleaseDeadZone);

                    if (brakeSystem.PrevEngineBrakeControllerRateApply < brakeSystem.EngineBrakeControllerApplyDeadZone)
                        EngineBrakeControllerApply = brakeSystem.EngineBrakeControllerApplyDeadZone;

                    float EngineBrakeCylOffset = train.BrakeLine3PressurePSI - (brakeSystem.EngineBrakeControllerApplyDeadZone * lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI - (EngineBrakeControllerRate * brakeSystem.EngineBrakeControllerApplyDeadZone * lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI));

                    // Apply
                    if (train.BrakeLine3PressurePSI > EngineBrakeControllerApply * lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                        && lead.BrakeSystem.AutoCylPressurePSI1 < EngineBrakeCylOffset
                        && lead.MainResPressurePSI > 0
                        && AutoCylPressurePSI < lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                        && AutoCylPressurePSI < lead.MainResPressurePSI)
                    {
                        if (lead.BrakeSystem.BP1_EngineBrakeControllerRatePSIpS == 0) lead.BrakeSystem.BP1_EngineBrakeControllerRatePSIpS = lead.EngineBrakeApplyRatePSIpS;
                        float dp = elapsedClockSeconds * lead.BrakeSystem.BP1_EngineBrakeControllerRatePSIpS;

                        brakeSystem.EngineBrakeDelay += elapsedClockSeconds;
                        if (brakeSystem.EngineBrakeDelay > brakeSystem.BrakeDelayToEngage - 0.05f && brakeSystem.EngineBrakeDelay < brakeSystem.BrakeDelayToEngage && AutoCylPressurePSI < 1)
                            lead.BrakeSystem.AutoCylPressurePSI1 = 0.1f * 14.50377f;

                        if (brakeSystem.EngineBrakeDelay > brakeSystem.BrakeDelayToEngage + 0.25f)
                            lead.BrakeSystem.AutoCylPressurePSI1 += dp * ((EngineBrakeControllerRate - EngineBrakeControllerApply) / EngineBrakeControllerApply);

                        lead.BrakeSystem.AutoCylPressurePSI1 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI1, 0, lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI);
                        lead.MainResPressurePSI -= dp * ((EngineBrakeControllerRate - EngineBrakeControllerApply) / EngineBrakeControllerApply) * brakeSystem.GetCylVolumeM3() / lead.MainResVolumeM3;
                        if (EngineBrakeCylOffset < lead.BrakeSystem.AutoCylPressurePSI1)
                        {
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                            lead.EngineBrakeState = ValveState.Lap;
                        }
                        else lead.EngineBrakeState = ValveState.Apply;
                        brakeSystem.PrevEngineBrakeControllerRateRelease = EngineBrakeControllerRate;
                        if (lead.BrakeSystem.AutoCylPressurePSI1 > train.BrakeLine3PressurePSI * 0.95f)
                        {
                            lead.EngineBrakeState = ValveState.Lap;
                        }
                    }
                    // Release
                    else
                    if (lead.BrakeSystem.AutoCylPressurePSI1 > 0
                        && train.BrakeLine3PressurePSI < EngineBrakeControllerRelease * lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                        && lead.BrakeSystem.AutoCylPressurePSI1 > train.BrakeLine3PressurePSI - 0)
                    {
                        if (lead.BrakeSystem.BP1_EngineBrakeControllerRatePSIpS == 0) lead.BrakeSystem.BP1_EngineBrakeControllerRatePSIpS = lead.EngineBrakeApplyRatePSIpS;
                        float dp = elapsedClockSeconds * lead.BrakeSystem.BP1_EngineBrakeControllerRatePSIpS;

                        lead.BrakeSystem.AutoCylPressurePSI1 -= dp * (1 - (1 + ((EngineBrakeControllerRate - EngineBrakeControllerRelease) / EngineBrakeControllerRelease)));
                        lead.BrakeSystem.AutoCylPressurePSI1 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI1, 0, lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI);
                        if (train.BrakeLine3PressurePSI > lead.BrakeSystem.AutoCylPressurePSI1)
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                        else lead.EngineBrakeState = ValveState.Release;
                        if (lead.BrakeSystem.AutoCylPressurePSI1 < 1) brakeSystem.EngineBrakeDelay = 0;
                        brakeSystem.PrevEngineBrakeControllerRateApply = EngineBrakeControllerRate;
                    }
                    if (Math.Round(train.BrakeLine3PressurePSI) == Math.Round(lead.BrakeSystem.AutoCylPressurePSI1))
                    {
                        lead.EngineBrakeState = ValveState.Lap;
                    }
                }

                // Definice pro brzdič BP2
                if (brakeSystem.BP2_EngineBrakeController)
                {
                    // Apply
                    if (lead.BrakeSystem.AutoCylPressurePSI1 < train.BrakeLine3PressurePSI
                        && lead.MainResPressurePSI > 0
                        && AutoCylPressurePSI < lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                        && AutoCylPressurePSI < lead.MainResPressurePSI)
                    {
                        if (lead.BrakeSystem.BP2_EngineBrakeControllerRatePSIpS == 0) lead.BrakeSystem.BP2_EngineBrakeControllerRatePSIpS = lead.EngineBrakeApplyRatePSIpS;
                        float dp = elapsedClockSeconds * lead.BrakeSystem.BP2_EngineBrakeControllerRatePSIpS;

                        if (lead.BrakeSystem.AutoCylPressurePSI1 + dp > train.BrakeLine3PressurePSI)
                            dp = train.BrakeLine3PressurePSI - lead.BrakeSystem.AutoCylPressurePSI1;

                        if (dp * brakeSystem.GetCylVolumeM3() > lead.MainResPressurePSI * lead.MainResVolumeM3)
                            dp = (lead.MainResPressurePSI * lead.MainResVolumeM3) / brakeSystem.GetCylVolumeM3();

                        if (lead.BrakeSystem.AutoCylPressurePSI0 + lead.BrakeSystem.AutoCylPressurePSI1 + dp > lead.MainResPressurePSI)
                            dp = lead.MainResPressurePSI - lead.BrakeSystem.AutoCylPressurePSI1 - lead.BrakeSystem.AutoCylPressurePSI0;

                        brakeSystem.EngineBrakeDelay += elapsedClockSeconds;
                        if (brakeSystem.EngineBrakeDelay > brakeSystem.BrakeDelayToEngage - 0.05f && brakeSystem.EngineBrakeDelay < brakeSystem.BrakeDelayToEngage && AutoCylPressurePSI < 1)
                            lead.BrakeSystem.AutoCylPressurePSI1 = 0.1f * 14.50377f;

                        if (brakeSystem.EngineBrakeDelay > brakeSystem.BrakeDelayToEngage + 0.25f)
                            lead.BrakeSystem.AutoCylPressurePSI1 += dp;

                        lead.BrakeSystem.AutoCylPressurePSI1 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI1, 0, lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI);
                        lead.MainResPressurePSI -= dp * brakeSystem.GetCylVolumeM3() / lead.MainResVolumeM3;
                        if (train.BrakeLine3PressurePSI < lead.BrakeSystem.AutoCylPressurePSI1)
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                        else lead.EngineBrakeState = ValveState.Apply;
                        if (lead.BrakeSystem.AutoCylPressurePSI1 > train.BrakeLine3PressurePSI * 0.95f)
                        {
                            lead.EngineBrakeState = ValveState.Lap;
                        }
                    }
                    // Release
                    else
                    if (lead.BrakeSystem.AutoCylPressurePSI1 > 0
                        && lead.BrakeSystem.AutoCylPressurePSI1 > train.BrakeLine3PressurePSI)
                    {
                        if (lead.BrakeSystem.BP2_EngineBrakeControllerRatePSIpS == 0) lead.BrakeSystem.BP2_EngineBrakeControllerRatePSIpS = lead.EngineBrakeApplyRatePSIpS;
                        float dp = elapsedClockSeconds * lead.BrakeSystem.BP2_EngineBrakeControllerRatePSIpS;

                        if (lead.BrakeSystem.AutoCylPressurePSI1 - dp < train.BrakeLine3PressurePSI)
                            dp = lead.BrakeSystem.AutoCylPressurePSI1 - train.BrakeLine3PressurePSI;

                        lead.BrakeSystem.AutoCylPressurePSI1 -= dp;
                        lead.BrakeSystem.AutoCylPressurePSI1 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI1, 0, lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI);
                        if (train.BrakeLine3PressurePSI > lead.BrakeSystem.AutoCylPressurePSI1)
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                        else lead.EngineBrakeState = ValveState.Release;
                        if (lead.BrakeSystem.AutoCylPressurePSI1 == train.BrakeLine3PressurePSI)
                        {
                            lead.EngineBrakeState = ValveState.Lap;
                        }
                        if (lead.BrakeSystem.AutoCylPressurePSI1 < 1) brakeSystem.EngineBrakeDelay = 0;
                    }
                }

                // Definice pro brzdič LEKOV
                if (brakeSystem.LEKOV_EngineBrakeController)
                {
                    // Apply
                    if (lead.BrakeSystem.AutoCylPressurePSI1 < train.BrakeLine3PressurePSI
                        && lead.MainResPressurePSI > 0
                        && AutoCylPressurePSI < lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                        && AutoCylPressurePSI < lead.MainResPressurePSI)
                    {
                        if (lead.BrakeSystem.LEKOV_EngineBrakeControllerRatePSIpS == 0) lead.BrakeSystem.LEKOV_EngineBrakeControllerRatePSIpS = lead.EngineBrakeApplyRatePSIpS;
                        float dp = elapsedClockSeconds * lead.BrakeSystem.LEKOV_EngineBrakeControllerRatePSIpS;

                        if (lead.BrakeSystem.AutoCylPressurePSI1 + dp > train.BrakeLine3PressurePSI)
                            dp = train.BrakeLine3PressurePSI - lead.BrakeSystem.AutoCylPressurePSI1;

                        if (dp * brakeSystem.GetCylVolumeM3() > lead.MainResPressurePSI * lead.MainResVolumeM3)
                            dp = (lead.MainResPressurePSI * lead.MainResVolumeM3) / brakeSystem.GetCylVolumeM3();

                        if (lead.BrakeSystem.AutoCylPressurePSI0 + lead.BrakeSystem.AutoCylPressurePSI1 + dp > lead.MainResPressurePSI)
                            dp = lead.MainResPressurePSI - lead.BrakeSystem.AutoCylPressurePSI1 - lead.BrakeSystem.AutoCylPressurePSI0;

                        brakeSystem.EngineBrakeDelay += elapsedClockSeconds;
                        if (brakeSystem.EngineBrakeDelay > brakeSystem.BrakeDelayToEngage - 0.05f && brakeSystem.EngineBrakeDelay < brakeSystem.BrakeDelayToEngage && AutoCylPressurePSI < 1)
                            lead.BrakeSystem.AutoCylPressurePSI1 = 0.1f * 14.50377f;

                        if (brakeSystem.EngineBrakeDelay > brakeSystem.BrakeDelayToEngage + 0.25f)
                            lead.BrakeSystem.AutoCylPressurePSI1 += dp;

                        lead.BrakeSystem.AutoCylPressurePSI1 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI1, 0, lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI);
                        lead.MainResPressurePSI -= dp * brakeSystem.GetCylVolumeM3() / lead.MainResVolumeM3;
                        if (train.BrakeLine3PressurePSI < lead.BrakeSystem.AutoCylPressurePSI1)
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                        else lead.EngineBrakeState = ValveState.Apply;
                        if (lead.BrakeSystem.AutoCylPressurePSI1 > train.BrakeLine3PressurePSI * 0.95f)
                        {
                            lead.EngineBrakeState = ValveState.Lap;
                        }
                    }
                    // Release
                    else
                    if (lead.BrakeSystem.AutoCylPressurePSI1 > 0
                        && lead.BrakeSystem.AutoCylPressurePSI1 > train.BrakeLine3PressurePSI)
                    {
                        if (lead.BrakeSystem.LEKOV_EngineBrakeControllerRatePSIpS == 0) lead.BrakeSystem.LEKOV_EngineBrakeControllerRatePSIpS = lead.EngineBrakeApplyRatePSIpS;
                        float dp = elapsedClockSeconds * lead.BrakeSystem.LEKOV_EngineBrakeControllerRatePSIpS;

                        if (lead.BrakeSystem.AutoCylPressurePSI1 - dp < train.BrakeLine3PressurePSI)
                            dp = lead.BrakeSystem.AutoCylPressurePSI1 - train.BrakeLine3PressurePSI;

                        lead.BrakeSystem.AutoCylPressurePSI1 -= dp;
                        lead.BrakeSystem.AutoCylPressurePSI1 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI1, 0, lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI);
                        if (train.BrakeLine3PressurePSI > lead.BrakeSystem.AutoCylPressurePSI1)
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                        else lead.EngineBrakeState = ValveState.Release;
                        if (lead.BrakeSystem.AutoCylPressurePSI1 == train.BrakeLine3PressurePSI)
                        {
                            lead.EngineBrakeState = ValveState.Lap;
                        }
                        if (lead.BrakeSystem.AutoCylPressurePSI1 < 1) brakeSystem.EngineBrakeDelay = 0;
                    }
                }

                //lead.ParkingBrakeTargetPressurePSI = 2 * 14.50377f;

                // Automatická parkovací brzda
                if (lead.AutomaticParkingBrakeEngaged
                    && lead.MainResPressurePSI > 0
                    && AutoCylPressurePSI < lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                    && AutoCylPressurePSI < lead.MainResPressurePSI)
                {
                    if (lead.BrakeSystem.AutoCylPressurePSI2 < lead.ParkingBrakeTargetPressurePSI)
                    {
                        float dp = elapsedClockSeconds * lead.EngineBrakeApplyRatePSIpS;

                        brakeSystem.AutoEngineBrakeDelay += elapsedClockSeconds;
                        if (brakeSystem.AutoEngineBrakeDelay > brakeSystem.BrakeDelayToEngage - 0.05f && brakeSystem.AutoEngineBrakeDelay < brakeSystem.BrakeDelayToEngage && AutoCylPressurePSI < 1)
                            lead.BrakeSystem.AutoCylPressurePSI2 = 0.1f * 14.50377f;

                        if (brakeSystem.AutoEngineBrakeDelay > brakeSystem.BrakeDelayToEngage + 0.25f)
                            lead.BrakeSystem.AutoCylPressurePSI2 += dp;

                        lead.MainResPressurePSI -= dp * brakeSystem.GetCylVolumeM3() / lead.MainResVolumeM3;
                        if (lead.BrakeSystem.AutoCylPressurePSI2 >= lead.ParkingBrakeTargetPressurePSI)
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                        else lead.EngineBrakeState = ValveState.Apply;
                        lead.BrakeSystem.T4_ParkingkBrake = 1;
                        lead.BrakeSystem.AutoCylPressurePSI2 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI2, 0, lead.ParkingBrakeTargetPressurePSI);
                    }
                }
                else if (!lead.AutomaticParkingBrakeEngaged && lead.BrakeSystem.T4_ParkingkBrake == 1)
                {
                    if (lead.BrakeSystem.AutoCylPressurePSI2 > 0)
                    {
                        float dp = elapsedClockSeconds * lead.EngineBrakeReleaseRatePSIpS;
                        lead.BrakeSystem.AutoCylPressurePSI2 -= dp;
                        if (lead.BrakeSystem.AutoCylPressurePSI2 <= 0)
                            lead.SignalEvent(Event.EngineBrakePressureStoppedChanging);
                        else lead.EngineBrakeState = ValveState.Release;
                        lead.BrakeSystem.AutoCylPressurePSI2 = MathHelper.Clamp(lead.BrakeSystem.AutoCylPressurePSI2, 0, lead.ParkingBrakeTargetPressurePSI);
                        if (lead.BrakeSystem.AutoCylPressurePSI2 < 1) brakeSystem.AutoEngineBrakeDelay = 0;
                    }
                    else
                        lead.BrakeSystem.T4_ParkingkBrake = 0;
                }


                if (lead.EngineBrakeState != prevState)
                    switch (lead.EngineBrakeState)
                    {
                        case ValveState.Release: lead.SignalEvent(Event.EngineBrakePressureDecrease); break;
                        case ValveState.Apply: lead.SignalEvent(Event.EngineBrakePressureIncrease); break;
                        case ValveState.Lap: lead.SignalEvent(Event.EngineBrakePressureStoppedChanging); break;
                    }

                // Použití průběžné brzdy v režimu automatiky ARR
                //lead.ARRTrainBrakeEngage = true;
                //lead.ARRAutoCylPressurePSI = 0f * 14.50377f;

                if (lead.ARRTrainBrakeEngage
                    && lead.MainResPressurePSI > 0
                    && AutoCylPressurePSI < lead.BrakeSystem.BrakeCylinderMaxSystemPressurePSI
                    && AutoCylPressurePSI < lead.MainResPressurePSI
                    && (AutoCylPressurePSI <= lead.ARRAutoCylPressurePSI * 0.95f || AutoCylPressurePSI >= lead.ARRAutoCylPressurePSI * 1.05f))
                {
                    if (lead.ARRAutoCylPressurePSI > (lead.TrainBrakeController.MaxPressurePSI - train.EqualReservoirPressurePSIorInHg) * lead.BrakeSystem.LocoAuxCylVolumeRatio)
                    {
                        lead.ARRTrainBrakeEngage_Apply = true;
                        lead.ARRTrainBrakeEngage_Release = false;
                        if (train.EqualReservoirPressurePSIorInHg > 0)
                            train.EqualReservoirPressurePSIorInHg -= lead.TrainBrakeController.ApplyRatePSIpS * elapsedClockSeconds;
                        if (train.EqualReservoirPressurePSIorInHg < 0)
                            train.EqualReservoirPressurePSIorInHg = 0;
                    }
                    if (lead.ARRAutoCylPressurePSI < (lead.TrainBrakeController.MaxPressurePSI - train.EqualReservoirPressurePSIorInHg) * lead.BrakeSystem.LocoAuxCylVolumeRatio)
                    {
                        lead.ARRTrainBrakeEngage_Apply = false;
                        lead.ARRTrainBrakeEngage_Release = true;
                        if (train.EqualReservoirPressurePSIorInHg < lead.TrainBrakeController.MaxPressurePSI)
                            train.EqualReservoirPressurePSIorInHg += lead.TrainBrakeController.ReleaseRatePSIpS * elapsedClockSeconds;
                        if (train.EqualReservoirPressurePSIorInHg > lead.TrainBrakeController.MaxPressurePSI)
                            train.EqualReservoirPressurePSIorInHg = lead.TrainBrakeController.MaxPressurePSI;
                    }
                }
            }

            // Start se vzduchem nebo bez vzduchu podle klíčového slova v názvu consistu nebo volby v menu OR
            if (lead != null && lead.BrakeSystem.StartOn)
            {
                if (train.LocoIsAirEmpty || trainCar.Simulator.Settings.AirEmpty)
                {
                    lead.BrakeSystem.IsAirEmpty = true;
                    foreach (TrainCar car in train.Cars)
                    {
                        car.BrakeSystem.IsAirEmpty = true;
                        int x = 0;
                        int y = train.Cars.Count - 1;
                        if (y > 1 && y <= 10)
                            x = 2;
                        if (y > 10 && y <= 15)
                            x = 3;
                        if (y > 15 && y <= 20)
                            x = 4;
                        if (y > 20 && y <= 25)
                            x = 5;
                        if (y > 25 && y <= 30)
                            x = 6;
                        if (y > 30 && y <= 35)
                            x = 7;
                        if (y > 35 && y <= 40)
                            x = 8;
                        if (y > 40 && y <= 45)
                            x = 9;
                        if (y > 45 && y <= 50)
                            x = 10;
                        if (y > 50 && y <= 55)
                            x = 11;
                        if (y > 55 && y <= 60)
                            x = 12;
                        if (y > 60 && y <= 65)
                            x = 13;

                        for (int i = 1; i < x + 1; i++)
                        {
                            if (car == train.Cars[i])
                                car.BrakeSystem.HandBrakeActive = true;
                        }
                        for (int i = x + 1; i < train.Cars.Count; i++)
                        {
                            if (car == train.Cars[i])
                                car.BrakeSystem.HandBrakeDeactive = true;
                        }
                    }
                }
                else
                if (!train.LocoIsAirEmpty && !trainCar.Simulator.Settings.AirEmpty)
                {
                    lead.BrakeSystem.IsAirEmpty = false;
                    lead.BrakeSystem.IsAirFull = true;
                    foreach (TrainCar car in train.Cars)
                    {
                        for (int i = 1; i < train.Cars.Count; i++)
                        {
                            if (car == train.Cars[i])
                            {
                                car.BrakeSystem.IsAirEmpty = false;
                                car.BrakeSystem.IsAirFull = true;
                            }
                        }
                    }
                }
            }
        }

        public override float InternalPressure(float realPressure)
        {
            return realPressure;
        }

        public override void SetRetainer(RetainerSetting setting)
        {
            switch (setting)
            {
                case RetainerSetting.Exhaust:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = MaxReleaseRatePSIpS;
                    RetainerDebugState = "EX";
                    break;
                case RetainerSetting.HighPressure:
                    if ((Car as MSTSWagon).RetainerPositions > 0)
                    {
                        RetainerPressureThresholdPSI = 20;
                        ReleaseRatePSIpS = (50 - 20) / 90f;
                        RetainerDebugState = "HP";
                    }
                    break;
                case RetainerSetting.LowPressure:
                    if ((Car as MSTSWagon).RetainerPositions > 3)
                    {
                        RetainerPressureThresholdPSI = 10;
                        ReleaseRatePSIpS = (50 - 10) / 60f;
                        RetainerDebugState = "LP";
                    }
                    else if ((Car as MSTSWagon).RetainerPositions > 0)
                    {
                        RetainerPressureThresholdPSI = 20;
                        ReleaseRatePSIpS = (50 - 20) / 90f;
                        RetainerDebugState = "HP";
                    }
                    break;
                case RetainerSetting.SlowDirect:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = (50 - 10) / 86f;
                    RetainerDebugState = "SD";
                    break;
            }
        }

        public override void SetHandbrakePercent(float percent)
        {
            if (!(Car as MSTSWagon).HandBrakePresent)
            {
                HandbrakePercent = 0;
                return;
            }
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            HandbrakePercent = percent;
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            Car.Train.EqualReservoirPressurePSIorInHg = 90 - (90 - FullServPressurePSI) * percent / 100;
        }

        // used when switching from autopilot to player driven mode, to move from default values to values specific for the trainset
        public void NormalizePressures(float maxPressurePSI)
        {
            if (AuxResPressurePSI > maxPressurePSI) AuxResPressurePSI = maxPressurePSI;
            if (BrakeLine1PressurePSI > maxPressurePSI) BrakeLine1PressurePSI = maxPressurePSI;
            if (EmergResPressurePSI > maxPressurePSI) EmergResPressurePSI = maxPressurePSI;
        }

        public override bool IsBraking()
        {
            if (AutoCylPressurePSI > MaxCylPressurePSI * 0.3)
                return true;
            return false;
        }

        //Corrects MaxCylPressure (e.g 380.eng) when too high
        public override void CorrectMaxCylPressurePSI(MSTSLocomotive loco)
        {
            //if (MaxCylPressurePSI > loco.TrainBrakeController.MaxPressurePSI - MaxCylPressurePSI / AuxCylVolumeRatio)
            //{
            //    MaxCylPressurePSI = loco.TrainBrakeController.MaxPressurePSI * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
            //}
        }
    }
}
