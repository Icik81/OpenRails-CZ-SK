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
        public float BrakeLine1PressurePSI = 90;    // main trainline pressure at this car
        public float BrakeLine2PressurePSI;         // main reservoir equalization pipe pressure
        public float BrakeLine3PressurePSI;         // engine brake cylinder equalization pipe pressure
        public float BrakePipeVolumeM3 = 0f;      // volume of a single brake line
        public bool ControllerRunningLock = false;  // Stops Running controller from becoming active until BP = EQ Res, used in EQ vacuum brakes
        public float BrakeCylFraction;
        public float TrainPipeLeakRatePSIpS = 0;
        public float BrakeSensitivityPSIpS = 0;
        public float OverchargeEliminationRatePSIpS = 0;
        public float BrakeCylinderMaxSystemPressurePSI = 0;
        public float TrainBrakesControllerMaxOverchargePressurePSI = 0;
        public float AutoCylPressurePSI1;
        public float AutoCylPressurePSI0;
        public float maxPressurePSI0;
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
        public float BrakeMassKG;
        public float BrakeMassKGRMg;
        public float CoefMode;
        public float DebugKoef;
        public float MaxReleaseRatePSIpSG;
        public float MaxApplicationRatePSIpSG;
        public float MaxReleaseRatePSIpSP;
        public float MaxApplicationRatePSIpSP;
        public float MaxReleaseRatePSIpSR;
        public float MaxApplicationRatePSIpSR;
        public bool AutoLoadRegulatorEquipped;
        public float AutoLoadRegulatorMaxBrakeMass;
        public bool BrakeCylRelease;
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
        public bool HandBrakeActive;

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
        public bool TwoPipes { get; protected set; }

        /// <summary>
        /// Volba režimu vozu G, P, R, MG+R
        /// </summary>
        public float BrakeCarMode = 1;  // Default režim P
        public string BrakeCarModeText = "P";
        public float NumberBrakeCarMode = 4;  // Celkový počet režimů vozu

        public float BrakeCarModePL = 0;
        public string BrakeCarModeTextPL = "Prázdný";

        public int WagonType;

        public abstract void AISetPercent(float percent);

        public abstract string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units);
        public abstract string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units);
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
