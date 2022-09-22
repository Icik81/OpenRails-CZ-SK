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

using Orts.Parsers.Msts;
using ORTS.Common;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes
{
    public enum BrakeSystemComponent
    {
        MainReservoir,
        EqualizingReservoir,
        AuxiliaryReservoir,
        EmergencyReservoir,
        MainPipe,
        BrakePipe,
        BrakeCylinder
    }

    public abstract class BrakeSystem
    {
        public float BrakeLine1PressurePSI = 72;    // main trainline pressure at this car
        public float BrakeLine2PressurePSI;         // main reservoir equalization pipe pressure
        public float BrakeLine3PressurePSI;         // engine brake cylinder equalization pipe pressure
        public float BrakePipeVolumeM3Base = 0f;      // volume of a single brake line
        public float BrakePipeVolumeM3 = 0f;
        public bool ControllerRunningLock = false;  // Stops Running controller from becoming active until BP = EQ Res, used in EQ vacuum brakes
        public float BrakeCylFraction;
        public float BrakeSensitivityPSIpS = 0;
        public float OverchargeEliminationRatePSIpS = 0;
        public float BrakeCylinderMaxSystemPressurePSI = 0;
        public float TrainBrakesControllerMaxOverchargePressurePSI = 0;
        public float AutoCylPressurePSI2;
        public float AutoCylPressurePSI1;
        public float AutoCylPressurePSI0;
        public float LocoAuxCylVolumeRatio;
        public float maxPressurePSI0 = 72;
        public float TotalCapacityMainResBrakePipe;
        public float EB; // Hodnota EngineBrake
        public float MCP;  // Hodnota MaxCylPressure
        public float MaxReleaseRatePSIpS0;
        public float MaxApplicationRatePSIpS0;
        public float BrakeMassG;
        public float BrakeMassP;
        public float BrakeMassR;
        public float BrakeMassRMg;
        public float BrakeMassEmpty;
        public float BrakeMassLoaded;
        public bool ForceWagonLoaded;
        public string ForceBrakeMode;
        public float BrakeMassKG;
        public float BrakeMassKGRMg;
        public float CoefMode;
        public float DebugKoef;
        public float DebugKoef1;
        public float DebugKoef2;
        public float MaxReleaseRatePSIpSG;
        public float MaxApplicationRatePSIpSG;
        public float MaxReleaseRatePSIpSP;
        public float MaxApplicationRatePSIpSP;
        public float MaxReleaseRatePSIpSR;
        public float MaxApplicationRatePSIpSR;
        public bool AutoLoadRegulatorEquipped;
        public float AutoLoadRegulatorMaxBrakeMass;
        public bool BrakeCylRelease;
        public bool BrakeCylReleaseFlow;
        public bool BrakeCylApply;
        public float MainResMinimumPressureForMGbrakeActivationPSI;
        public float BrakePipePressureForMGbrakeActivationPSI;
        public bool BrakeModeRMgActive;
        public bool PowerForWagon;
        public bool EmergencyBrakeForWagon;
        public bool AirForWagon;
        public bool AntiSkidSystemEquipped;
        public bool BailOffOnAntiSkid;
        public bool IsAirEmpty;
        public bool IsAirFull;
        public bool HandBrakeActive;
        public bool HandBrakeDeactive;
        public bool StartOn = true;
        public string NextLocoBrakeState;
        public bool NextLocoRelease;
        public bool NextLocoApply;
        public bool NextLocoQuickRelease;
        public bool NextLocoEmergency;
        public bool NextLocoOvercharge;
        public bool NextLocoLap;
        public bool NextLocoRunning;
        public bool NextLocoNeutral;
        public bool NextLocoSuppression;
        public bool NextLocoGSelfLapH;
        public bool NextLocoSlowApplyStart;
        public bool NextLocoMatrosovRelease;
        public bool NextLocoWestingHouseRelease;
        public bool NextLocoEPApply;
        public bool TripleValveRelease;
        public bool TripleValveApply;
        public bool TripleValveLap;
        public bool TripleValveEmergency;
        public bool CompressorOnDelay;
        public bool Compressor2OnDelay;
        public bool AuxCompressorOnDelay;
        public float CompressorT0 = 0;
        public float Compressor2T0 = 0;
        public float AuxCompressorT0 = 0;
        public float AutoBailOffOnRatePSIpS;
        public float BrakeDelayToEngage;
        public bool AutoOverchargePressure;
        public float BrakePipeMinPressureDropToEngage;
        public float EngineBrakeControllerApplyDeadZone;
        public float EngineBrakeControllerReleaseDeadZone;
        public float BP1_EngineBrakeControllerRatePSIpS;
        public float BP2_EngineBrakeControllerRatePSIpS;
        public float LEKOV_EngineBrakeControllerRatePSIpS;
        public bool BP1_EngineBrakeController;
        public bool BP2_EngineBrakeController;
        public bool LEKOV_EngineBrakeController;
        public float PrevEngineBrakeControllerRateApply = 0;
        public float PrevEngineBrakeControllerRateRelease = 0;
        public int T4_ParkingkBrake = 0;
        public float EngineBrakeDelay = 0;
        public float AutoEngineBrakeDelay = 0;
        public int ReleaseTr = 0;
        public bool BrakeCylApplyMainResPressureOK = false;
        public bool BrakeCylReleaseEDBOn = false;
        public bool OverChargeRunning = false;
        public bool BrakePipeFlow;
        public float BrakePipeChangeRate;
        public float CylinderChangeRate;
        public float BrakePipeChangeRateBar;
        public float CylinderChangeRateBar;
        public string OLBailOffType;
        public bool OLBailOff;
        public float OLBailOffLimitPressurePSI;
        public bool OL3active;
        public bool EmerBrakeTriggerActive = false;
        public float MainResChangeRate;
        public float prevTotalCapacityMainResBrakePipe;
        public float PressureConverterBase;
        public float PressureConverterBaseTrainBrake;
        public float PressureConverterBaseEDB;
        public float PressureConverter;        
        public float MCP_TrainBrake = 4.0f * 14.50377f;        
        public bool ARRTrainBrakeCanEngage;
        public float ARRTrainBrakeCycle1 = 0;
        public float ARRTrainBrakeCycle2 = 0;

        public bool BrakePipeDischargeRate = false;
        public bool BrakePipeChargeRate = false;
        public Interpolator PressureRateFactorDischarge;
        public Interpolator PressureRateFactorCharge;
        public Interpolator DebugKoef2Factor;

        public float BrakeCylinderMaxPressureForLowState;
        public float LowStateOnSpeedEngageLevel;
        public bool TwoStateBrake = false;
        public bool HighPressure = false;
        public bool LowPressure = true;
        public float T_HighPressure;
        public float FromHighToLowPressureRate;
        public float BrakeMassKG_TwoStateBrake;

        public float T1AirLoss = 0;
        public float T2AirLoss = 0;
        public bool AirOK_DoorCanManipulate;

        public bool BrakeCarHasStatus;
        public bool CarHasAirStuckBrake_1;
        public bool CarHasAirStuckBrake_2;
        public bool CarHasAirStuckBrake_3;
        public bool CarHasMechanicStuckBrake_1;
        public bool CarHasMechanicStuckBrake_2;
        public bool CarHasProblemWithBrake;


        public float AuxPowerOnDelayS { get; set; }

        public float GetDebugKoef2()
        {
            if (DebugKoef2Factor == null || DebugKoef2Factor.GetSize() == 0)
            {
                DebugKoef2 = 1.0f;
            }
            else
                DebugKoef2 = DebugKoef2Factor[AutoCylPressurePSI0 + AutoCylPressurePSI1 + AutoCylPressurePSI2];
            return DebugKoef2;
        }

        public float GetBrakePipeDischargeRate()
        {
            var PressureRate = 0.0f;
            if (PressureRateFactorDischarge == null)
            {
                PressureRate = 0.0f;
            }
            else
            {
                PressureRate = PressureRateFactorDischarge[BrakeLine1PressurePSI];
            }
            return PressureRate;
        }

        public float GetBrakePipeChargeRate()
        {
            var PressureRate = 0.0f;
            if (PressureRateFactorCharge == null)
            {
                PressureRate = 0.0f;
            }
            else
            {
                PressureRate = PressureRateFactorCharge[BrakeLine1PressurePSI];
            }
            return PressureRate;
        }

        /// <summary>
        /// Front brake hoses connection status
        /// </summary>
        public bool FrontBrakeHoseConnected;
        /// <summary>
        /// Front angle cock opened/closed status
        /// </summary>
        public bool AngleCockAOpen = true;
        /// <summary>
        /// Rear angle cock opened/closed status
        /// </summary>
        public bool AngleCockBOpen = true;
        /// <summary>
        /// Auxiliary brake reservoir vent valve open/closed status
        /// </summary>
        public bool BleedOffValveOpen;
        /// <summary>
        /// Indicates whether the main reservoir pipe is available
        /// </summary>
        public bool TwoPipesConnection;

        /// <summary>
        /// Volba režimu vozu G, P, R, MG+R
        /// </summary>
        public float BrakeCarMode = 1;  // Default režim P
        public string BrakeCarModeText = "P";
        public float NumberBrakeCarMode = 4;  // Celkový počet režimů vozu

        public float BrakeCarModePL = 0;
        public string BrakeCarModeTextPL = Simulator.Catalog.GetString("Empty");

        public float TwoPipesConnectionMenu = 0;
        public string TwoPipesConnectionText = Simulator.Catalog.GetString("disconnect");
        public bool ForceTwoPipesConnection = false;

        public float BrakeCarDeactivateMenu = 0;
        public string BrakeCarDeactivateText = Simulator.Catalog.GetString("disconnect");
        public bool BrakeCarDeactivate;

        public float LeftDoorMenu = 0;
        public string LeftDoorText = Simulator.Catalog.GetString("closed");
        public float RightDoorMenu = 0;
        public string RightDoorText = Simulator.Catalog.GetString("closed");
        public int LeftDoorCycle = 1;
        public int RightDoorCycle = 1;
        public bool LeftDoorIsOpened;
        public bool RightDoorIsOpened;
        public bool DoorsOpen;

        public float HeatingMenu = 1;
        public string HeatingText = Simulator.Catalog.GetString("on");
        public bool HeatingIsOn;

        public int WagonType;

        public abstract void AISetPercent(float percent);

        public abstract string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract string GetSimpleStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract float GetCylPressurePSI();
        public abstract float GetCylVolumeM3();
        public abstract float GetVacResPressurePSI();
        public abstract float GetVacResVolume();
        public abstract float GetVacBrakeCylNumber();
        public bool CarBPIntact;

        public abstract void Save(BinaryWriter outf);

        public abstract void Restore(BinaryReader inf);

        public abstract void PropagateBrakePressure(float elapsedClockSeconds);

        /// <summary>
        /// Convert real pressure to a system specific internal pressure.
        /// For pressured brakes it is a straight 1:1 noop conversion,
        /// but for vacuum brakes it is a conversion to an internally used equivalent pressure.
        /// </summary>
        public abstract float InternalPressure(float realPressure);

        public abstract void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease);
        public abstract void SetHandbrakePercent(float percent);
        public abstract bool GetHandbrakeStatus();
        public abstract void SetRetainer(RetainerSetting setting);
        public abstract void InitializeMoving(); // starting conditions when starting speed > 0
        public abstract void LocoInitializeMoving(); // starting conditions when starting speed > 0
        public abstract bool IsBraking(); // return true if the wagon is braking above a certain threshold
        public abstract void CorrectMaxCylPressurePSI(MSTSLocomotive loco); // corrects max cyl pressure when too high
    }

    public enum RetainerSetting
    {
        [GetString("Exhaust")] Exhaust,
        [GetString("High Pressure")] HighPressure,
        [GetString("Low Pressure")] LowPressure,
        [GetString("Slow Direct")] SlowDirect
    };
}
