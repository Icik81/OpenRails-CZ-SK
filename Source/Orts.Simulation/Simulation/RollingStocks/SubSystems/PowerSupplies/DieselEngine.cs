﻿// COPYRIGHT 2013, 2014 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.MultiPlayer;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    public class DieselEngines : IEnumerable
    {
        /// <summary>
        /// A list of auxiliaries
        /// </summary>
        public List<DieselEngine> DEList = new List<DieselEngine>();

        /// <summary>
        /// Number of Auxiliaries on the list
        /// </summary>
        public int Count { get { return DEList.Count; } }

        /// <summary>
        /// Reference to the locomotive carrying the auxiliaries
        /// </summary>
        public readonly MSTSDieselLocomotive Locomotive;

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        public DieselEngines(MSTSDieselLocomotive loco)
        {
            Locomotive = loco;
        }

        /// <summary>
        /// constructor from copy
        /// </summary>
        public DieselEngines(DieselEngines copy, MSTSDieselLocomotive loco)
        {
            DEList = new List<DieselEngine>();
            foreach (DieselEngine de in copy.DEList)
            {
                DEList.Add(new DieselEngine(de, loco));
            }
            Locomotive = loco;
        }

        /// <summary>
        /// Creates a set of auxiliaries connected to the locomotive, based on stf reader parameters 
        /// </summary>
        /// <param name="loco">Host locomotive</param>
        /// <param name="stf">Reference to the ENG file reader</param>
        public DieselEngines(MSTSDieselLocomotive loco, STFReader stf)
        {
            Locomotive = loco;
            Parse(stf, loco);

        }


        public DieselEngine this[int i]
        {
            get { return DEList[i]; }
            set { DEList[i] = value; }
        }

        public void Add()
        {
            DEList.Add(new DieselEngine());
        }

        public void Add(DieselEngine de)
        {
            DEList.Add(de);
        }


        /// <summary>
        /// Parses all the parameters within the ENG file
        /// </summary>
        /// <param name="stf">reference to the ENG file reader</param>
        public void Parse(STFReader stf, MSTSDieselLocomotive loco)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(0);
            for (int i = 0; i < count; i++)
            {
                string setting = stf.ReadString().ToLower();
                if (setting == "diesel")
                {
                    DEList.Add(new DieselEngine());

                    DEList[i].Parse(stf, loco);
                    DEList[i].Initialize(true);

                    // sets flag to indicate that a diesel eng prime mover code block has been defined by user, otherwise OR will define one through the next code section using "MSTS" values
                    DEList[i].DieselEngineConfigured = true;
                }

                if ((!DEList[i].IsInitialized))
                {
                    STFException.TraceWarning(stf, "Diesel engine model has some errors - loading MSTS format");
                    DEList[i].InitFromMSTS((MSTSDieselLocomotive)Locomotive);
                    DEList[i].Initialize(true);
                }
            }
        }

        public void Initialize(bool start)
        {
            foreach (DieselEngine de in DEList)
                de.Initialize(start);
        }

        /// <summary>
        /// Saves status of each auxiliary on the list
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(DEList.Count);
            foreach (DieselEngine de in DEList)
                de.Save(outf);
        }

        /// <summary>
        /// Restores status of each auxiliary on the list
        /// </summary>
        /// <param name="inf"></param>
        public void Restore(BinaryReader inf)
        {
            int count = inf.ReadInt32();
            if (DEList.Count == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    DEList.Add(new DieselEngine());
                    DEList[i].InitFromMSTS((MSTSDieselLocomotive)Locomotive);
                    DEList[i].Initialize(true);
                }

            }
            foreach (DieselEngine de in DEList)
                de.Restore(inf);
        }

        /// <summary>
        /// A summary of power of all the diesels
        /// </summary>
        public float PowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.OutputPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A power-on indicator
        /// </summary>
        public bool PowerOn
        {
            get
            {
                bool temp = false;
                foreach (DieselEngine de in DEList)
                {
                    // Icik
                    //temp |= (de.EngineStatus == DieselEngine.Status.Running) || (de.EngineStatus == DieselEngine.Status.Starting);
                    temp |= (de.EngineStatus == DieselEngine.Status.Running);
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximal power of all the diesels
        /// </summary>
        public float MaxPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.MaximumDieselPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of maximal power of all the diesels
        /// </summary>
        public float MaxOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.CurrentDieselOutputPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// Maximum rail output power for all diesl prime movers
        /// </summary>
        public float MaximumRailOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.MaximumRailOutputPowerW;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of current rail output power for all diesel prime movers
        /// </summary>
        public float CurrentRailOutputPowerW
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.CurrentRailOutputPowerW;
                }
                return temp;
            }
        }
        /// <summary>
        /// A summary of fuel flow of all the auxiliaries
        /// </summary>
        public float DieselFlowLps
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.DieselFlowLps;
                }
                return temp;
            }
        }

        /// <summary>
        /// A summary of the throttle setting of all the auxiliaries
        /// </summary>
        public float ApparentThrottleSetting
        {
            get
            {
                float temp = 0f;
                foreach (DieselEngine de in DEList)
                {
                    temp += de.ApparentThrottleSetting;
                }
                return temp / Count;
            }
        }

        public bool HasGearBox
        {
            get
            {
                bool temp = false;
                foreach (DieselEngine de in DEList)
                {
                    temp |= (de.GearBox != null);
                }
                return temp;
            }
        }

        public float TractiveForceN
        {
            get
            {
                float temp = 0;
                foreach (DieselEngine de in DEList)
                {
                    if (de.GearBox != null)
                        temp += (de.DemandedThrottlePercent * 0.01f * de.GearBox.TractiveForceN);
                }
                return temp;
            }
        }

        /// <summary>
        /// Updates each auxiliary on the list
        /// </summary>
        /// <param name="elapsedClockSeconds">Time span within the simulation cycle</param>
        public void Update(float elapsedClockSeconds)
        {
            foreach (DieselEngine de in DEList)
            {
                de.Update(elapsedClockSeconds);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public DieselEnum GetEnumerator()
        {
            return new DieselEnum(DEList.ToArray());
        }

        public string GetStatus()
        {
            var result = new StringBuilder();

            //result.AppendFormat(Simulator.Catalog.GetString("Status"));
            foreach (var eng in DEList)
            switch (GetStringAttribute.GetPrettyName(eng.EngineStatus))
                {
                    case "Starting": result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Starting")); break;
                    case "Stopping": result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Stopping")); break;
                    case "Running": result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Running")); break;
                    case "Stopped": result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Stopped")); break;
                }                

            //result.AppendFormat("\t{0}\t{1}", Simulator.Catalog.GetParticularString("HUD", "Power"), FormatStrings.FormatPower(MaxOutputPowerW, Locomotive.IsMetric, false, false));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatPower(eng.CurrentDieselInputPowerW, Locomotive.IsMetric, false, false));

            //result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Load"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F1}%", eng.LoadPercent);

            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F0} {1}", eng.RealRPM, FormatStrings.rpm);
            
            //result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Flow"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}/{1}", FormatStrings.FormatFuelVolume(pS.TopH(eng.DieselFlowLps), Locomotive.IsMetric, Locomotive.IsUK), FormatStrings.h);

            //result.Append("\t");
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", (float)(Math.Round(eng.RealDieselWaterTemperatureDeg, 2)) + " °C");

            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", (float)(Math.Round(eng.RealDieselOilTemperatureDeg, 2)) + " °C");

            //result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Oil"));
            foreach (var eng in DEList)
                result.AppendFormat("\t{0}", FormatStrings.FormatPressure(eng.DieselOilPressurePSI, PressureUnit.PSI, Locomotive.MainPressureUnit, true));

            // Icik
            if (Locomotive.PowerUnit && !Locomotive.LocoHelperOn && !Locomotive.ControlUnit)
                foreach (var eng in DEList)
                    result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Engine"));
            
            if (Locomotive.LocoHelperOn)
                foreach (var eng in DEList)
                    result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Helper"));

            if (Locomotive.ControlUnit)
                foreach (var eng in DEList)
                    result.AppendFormat("\t{0}", Simulator.Catalog.GetString("Control"));

            foreach (var eng in DEList)
                result.AppendFormat("\t{0:F0} {1}", Locomotive.Variable8, FormatStrings.rpm);

            foreach (var eng in DEList)
                result.AppendFormat("\t {0:F1} {1}", eng.TurboPressureBar, FormatStrings.bar);

            foreach (var eng in DEList)
                result.AppendFormat("\t {0:F1}%", Locomotive.Variable7);

            return result.ToString();
        }

        public int NumOfActiveEngines
        {
            get
            {
                int num = 0;
                foreach (DieselEngine eng in DEList)
                {
                    if (eng.EngineStatus == DieselEngine.Status.Running)
                        num++;
                }
                return num;
            }
        }

        // This calculates the percent of running power. If the locomotive has two prime movers, and 
        // one is shut down then power will be reduced by the size of the prime mover
        public float RunningPowerFraction
        {
            get
            {
                float totalpossiblepower = 0;
                float runningPower = 0;
                float percent = 0;
                foreach (DieselEngine eng in DEList)
                {
                    totalpossiblepower += eng.MaximumDieselPowerW;
                    if (eng.EngineStatus == DieselEngine.Status.Running)
                    {
                        runningPower += eng.MaximumDieselPowerW;
                    }
                }
                percent = runningPower / totalpossiblepower;
                return percent;
            }
        }
    }

    public class DieselEnum : IEnumerator
    {
        public DieselEngine[] deList;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public DieselEnum(DieselEngine[] list)
        {
            deList = list;
        }

        public bool MoveNext()
        {
            position++;
            return (position < deList.Length);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public DieselEngine Current
        {
            get
            {
                try
                {
                    return deList[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }

    public class DieselEngine
    {
        public enum Status
        {
            [GetParticularString("Engine", "Stopped")] Stopped = 0,
            [GetParticularString("Engine", "Starting")] Starting = 1,
            [GetParticularString("Engine", "Running")] Running = 2,
            [GetParticularString("Engine", "Stopping")] Stopping = 3
        }

        public enum Cooling
        {
            NoCooling = 0,
            Mechanical = 1,
            Hysteresis = 2,
            Proportional = 3
        }

        public enum SettingsFlags
        {
            IdleRPM = 0x0001,
            MaxRPM = 0x0002,
            StartingRPM = 0x0004,
            StartingConfirmRPM = 0x0008,
            ChangeUpRPMpS = 0x0010,
            ChangeDownRPMpS = 0x0020,
            RateOfChangeUpRPMpSS = 0x0040,
            RateOfChangeDownRPMpSS = 0x0080,
            MaximalDieselPowerW = 0x0100,
            IdleExhaust = 0x0200,
            MaxExhaust = 0x0400,
            ExhaustDynamics = 0x0800,
            ExhaustColor = 0x1000,
            ExhaustTransientColor = 0x2000,
            DieselPowerTab = 0x4000,
            DieselConsumptionTab = 0x8000,
            ThrottleRPMTab = 0x10000,
            DieselTorqueTab = 0x20000,
            MinOilPressure = 0x40000,
            MaxOilPressure = 0x80000,
            MaxTemperature = 0x100000,
            Cooling = 0x200000,
            TempTimeConstant = 0x400000,
            OptTemperature = 0x800000,
            IdleTemperature = 0x1000000
        }

        public DieselEngine()
        {
        }

        public DieselEngine(DieselEngine copy, MSTSDieselLocomotive loco)
        {
            IdleRPM = copy.IdleRPM;
            MaxRPM = copy.MaxRPM;
            StartingRPM = copy.StartingRPM;
            StartingConfirmationRPM = copy.StartingConfirmationRPM;
            ChangeUpRPMpS = copy.ChangeUpRPMpS;
            ChangeDownRPMpS = copy.ChangeDownRPMpS;
            RateOfChangeUpRPMpSS = copy.RateOfChangeUpRPMpSS;
            RateOfChangeDownRPMpSS = copy.RateOfChangeDownRPMpSS;
            MaximumDieselPowerW = copy.MaximumDieselPowerW;
            MaximumRailOutputPowerW = copy.MaximumRailOutputPowerW;
            initLevel = copy.initLevel;
            DieselPowerTab = new Interpolator(copy.DieselPowerTab);
            DieselConsumptionTab = new Interpolator(copy.DieselConsumptionTab);
            ThrottleRPMTab = new Interpolator(copy.ThrottleRPMTab);
            ReverseThrottleRPMTab = new Interpolator(copy.ReverseThrottleRPMTab);
            if (copy.DieselTorqueTab != null) DieselTorqueTab = new Interpolator(copy.DieselTorqueTab);
            DieselUsedPerHourAtMaxPowerL = copy.DieselUsedPerHourAtMaxPowerL;
            DieselUsedPerHourAtIdleL = copy.DieselUsedPerHourAtIdleL;
            InitialExhaust = copy.InitialExhaust;
            InitialMagnitude = copy.InitialMagnitude;
            MaxExhaust = copy.MaxExhaust;
            MaxMagnitude = copy.MaxMagnitude;
            ExhaustParticles = copy.ExhaustParticles;
            ExhaustColor = copy.ExhaustColor;
            ExhaustSteadyColor = copy.ExhaustSteadyColor;
            ExhaustTransientColor = copy.ExhaustTransientColor;
            ExhaustDecelColor = copy.ExhaustDecelColor;
            DieselMaxOilPressurePSI = copy.DieselMaxOilPressurePSI;
            DieselMinOilPressurePSI = copy.DieselMinOilPressurePSI;
            DieselMaxTemperatureDeg = copy.DieselMaxTemperatureDeg;
            DieselMaxWaterTemperatureDeg = copy.DieselMaxWaterTemperatureDeg;
            DieselMaxOilTemperatureDeg = copy.DieselMaxOilTemperatureDeg;
            HelperDieselMaxWaterTemperatureDeg = copy.HelperDieselMaxWaterTemperatureDeg;
            HelperDieselMaxOilTemperatureDeg = copy.HelperDieselMaxOilTemperatureDeg;
            DieselOptimalTemperatureDegC = copy.DieselOptimalTemperatureDegC;
            DieselOptimalWaterTemperatureDegC = copy.DieselOptimalWaterTemperatureDegC;
            DieselOptimalOilTemperatureDegC = copy.DieselOptimalOilTemperatureDegC;
            DieselIdleTemperatureDegC = copy.DieselIdleTemperatureDegC;
            DieselIdleWaterTemperatureDegC = copy.DieselIdleWaterTemperatureDegC;
            DieselIdleOilTemperatureDegC = copy.DieselIdleOilTemperatureDegC;
            DieselWaterTempTimeConstantSec = copy.DieselWaterTempTimeConstantSec;
            DieselOilTempTimeConstantSec = copy.DieselOilTempTimeConstantSec;
            DieselTempCoolingHyst = copy.DieselTempCoolingHyst;
            DieselTempWaterCoolingHyst = copy.DieselTempWaterCoolingHyst;
            DieselTempOilCoolingHyst = copy.DieselTempOilCoolingHyst;
            CoolingEnableRPM = copy.CoolingEnableRPM;
            WaterCoolingPower = copy.WaterCoolingPower;
            OilCoolingPower = copy.OilCoolingPower;
            ElevatedConsumptionIdleRPM = copy.ElevatedConsumptionIdleRPM;
            TurboDelayUpS = copy.TurboDelayUpS;
            TurboDelayDownS = copy.TurboDelayDownS;
            TurboChargeRPMpS = copy.TurboChargeRPMpS;
            TurboDischargeRPMpS = copy.TurboDischargeRPMpS;
            MaxTurboRPM = copy.MaxTurboRPM;
            MaxTurboPressurePSI = copy.MaxTurboPressurePSI;
            StartingChangeUpRPMpS = copy.StartingChangeUpRPMpS;
            StoppingChangeDownRPMpS = copy.StoppingChangeDownRPMpS;
            StartingRateOfChangeUpRPMpSS = copy.StartingRateOfChangeUpRPMpSS;
            StoppingRateOfChangeDownRPMpSS = copy.StoppingRateOfChangeDownRPMpSS;
            OnePushStart = copy.OnePushStart;
            OnePushStop = copy.OnePushStop;
            ElevatedConsumptionIdleRPMCompressor = copy.ElevatedConsumptionIdleRPMCompressor;
            ElevatedConsumptionIdleRPMHeatingSummer = copy.ElevatedConsumptionIdleRPMHeatingSummer;
            ElevatedConsumptionIdleRPMHeatingWinter = copy.ElevatedConsumptionIdleRPMHeatingWinter;
            WaterCoolingPlatesUpS = copy.WaterCoolingPlatesUpS; 
            WaterCoolingPlatesDownS = copy.WaterCoolingPlatesDownS;
            OilCoolingPlatesUpS = copy.OilCoolingPlatesUpS;
            OilCoolingPlatesDownS = copy.OilCoolingPlatesDownS;
            CoolingFlowBase = copy.CoolingFlowBase;
            IndependentWaterPlates = copy.IndependentWaterPlates;
            IndependentOilPlates = copy.IndependentOilPlates;

            if (copy.GearBox != null)
            {
                GearBox = new GearBox(copy.GearBox, this);
            }
            locomotive = loco;
        }

        #region Parameters and variables      
        float dRPM;
        /// <summary>
        /// Actual change rate of the engine's RPM - useful for exhaust effects
        /// </summary>
        public float EngineRPMchangeRPMpS { get { return dRPM; } }
        /// <summary>
        /// Actual RPM of the engine
        /// </summary>
        public float RealRPM;

        /// <summary>
        /// RPM treshold when the engine starts to combust fuel
        /// </summary>
        public float StartingRPM;

        /// <summary>
        /// RPM treshold when the engine is considered as succesfully started
        /// </summary>
        public float StartingConfirmationRPM;

        /// <summary>
        /// GearBox unit
        /// </summary>
        public GearBox GearBox;

        /// <summary>
        /// Parent locomotive
        /// </summary>
        public MSTSDieselLocomotive locomotive;

        SettingsFlags initLevel;          //level of initialization
        /// <summary>
        /// Initialization flag - is true when sufficient number of parameters is read succesfully
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                if (initLevel == (SettingsFlags.IdleRPM | SettingsFlags.MaxRPM | SettingsFlags.StartingRPM | SettingsFlags.StartingConfirmRPM | SettingsFlags.ChangeUpRPMpS | SettingsFlags.ChangeDownRPMpS
                    | SettingsFlags.RateOfChangeUpRPMpSS | SettingsFlags.RateOfChangeDownRPMpSS | SettingsFlags.MaximalDieselPowerW | SettingsFlags.IdleExhaust | SettingsFlags.MaxExhaust
                    | SettingsFlags.ExhaustDynamics | SettingsFlags.ExhaustColor | SettingsFlags.ExhaustTransientColor | SettingsFlags.DieselPowerTab | SettingsFlags.DieselConsumptionTab | SettingsFlags.ThrottleRPMTab
                    | SettingsFlags.DieselTorqueTab | SettingsFlags.MinOilPressure | SettingsFlags.MaxOilPressure | SettingsFlags.MaxTemperature | SettingsFlags.Cooling
                    | SettingsFlags.TempTimeConstant | SettingsFlags.OptTemperature | SettingsFlags.IdleTemperature))

                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Engine status
        /// </summary>
        public Status EngineStatus = Status.Stopped;
        /// <summary>
        /// Type of engine cooling
        /// </summary>
        public Cooling EngineCooling = Cooling.Proportional;

        /// <summary>
        /// The RPM controller tries to reach this value
        /// </summary>
        public float DemandedRPM;
        float demandedThrottlePercent;
        /// <summary>
        /// Demanded throttle percent, usually token from parent locomotive
        /// </summary>
        public float DemandedThrottlePercent { set { demandedThrottlePercent = value > 100f ? 100f : (value < 0 ? 0 : value); } get { return demandedThrottlePercent; } }
        /// <summary>
        /// Idle RPM
        /// </summary>
        public float IdleRPM;
        /// <summary>
        /// Maximal RPM
        /// </summary>
        public float MaxRPM;
        /// <summary>
        /// RPM change rate from ENG file
        /// </summary>
        public float RPMRange;
        /// <summary>
        /// Change rate when accelerating the engine
        /// </summary>
        public float ChangeUpRPMpS;
        /// <summary>
        /// Change rate when decelerating the engine
        /// </summary>
        public float ChangeDownRPMpS;
        /// <summary>
        /// "Jerk" of the RPM when accelerating the engine
        /// </summary>
        public float RateOfChangeUpRPMpSS;
        /// <summary>
        /// "Jerk" of the RPM when decelerating the engine
        /// </summary>
        public float RateOfChangeDownRPMpSS;
        /// <summary>
        /// MAximum Rated Power output of the diesel engine (prime mover)
        /// </summary>
        public float MaximumDieselPowerW;
        /// <summary>
        /// Current power available to the traction motors
        /// </summary>
        public float CurrentDieselOutputPowerW;
        /// <summary>
        /// Maximum power available to the rail
        /// </summary>
        public float MaximumRailOutputPowerW;
        /// <summary>
        /// Actual current power output to the rail
        /// </summary>
        public float CurrentRailOutputPowerW;
        /// <summary>
        /// Real power output of the engine (based upon previous cycle - ie equivalent to Previous Motive Force - to calculate difference in power
        /// </summary>
        public float OutputPowerW;
        /// <summary>
        /// Relative output power to the MaximalPowerW
        /// </summary>
        public float ThrottlePercent { get { return OutputPowerW / MaximumDieselPowerW * 100f; } }
        /// <summary>
        /// Fuel consumed at max power
        /// </summary>
        public float DieselUsedPerHourAtMaxPowerL = 1.0f;
        /// <summary>
        /// Fuel consumed at idle
        /// </summary>
        public float DieselUsedPerHourAtIdleL = 1.0f;
        /// <summary>
        /// Current fuel flow
        /// </summary>
        public float DieselFlowLps;
        /// <summary>
        /// Engine load table - Max output power vs. RPM
        /// </summary>
        public Interpolator DieselPowerTab;
        /// <summary>
        /// Engine consumption table - Consumption vs. RPM
        /// </summary>
        public Interpolator DieselConsumptionTab;
        /// <summary>
        /// Engine throttle settings table - RPM vs. throttle settings
        /// </summary>
        public Interpolator ThrottleRPMTab;
        /// <summary>
        /// Engine throttle settings table - Reverse of RPM vs. throttle settings
        /// </summary>
        public Interpolator ReverseThrottleRPMTab;
        /// <summary>
        /// Throttle setting as calculated from real RpM
        /// </summary>
        public float ApparentThrottleSetting;
        /// <summary>
        /// Engine output torque table - Torque vs. RPM
        /// </summary>
        public Interpolator DieselTorqueTab;
        /// <summary>
        /// Current exhaust number of particles
        /// </summary>
        public float ExhaustParticles = 10.0f;
        /// <summary>
        /// Current exhaust color
        /// </summary>
        public Color ExhaustColor;
        /// <summary>
        /// Exhaust color at steady state (no RPM change)
        /// </summary>
        public Color ExhaustSteadyColor = Color.Gray;
        /// <summary>
        /// Exhaust color when accelerating the engine
        /// </summary>
        public Color ExhaustTransientColor = Color.Black;
        /// <summary>
        /// Exhaust color when decelerating the engine
        /// </summary>
        public Color ExhaustDecelColor = Color.WhiteSmoke;

        public Color ExhaustCompressorBlownColor = Color.Gray;

        public float InitialMagnitude = 1.5f;
        public float MaxMagnitude = 1.5f;
        public float MagnitudeRange;
        public float ExhaustMagnitude = 1.5f;

        public float InitialExhaust = 0.7f;
        public float MaxExhaust = 2.8f;
        public float ExhaustRange;

        public float ExhaustDecelReduction = 0.75f; //Represents the percentage that exhaust will be reduced while engine is decreasing RPMs.
        public float ExhaustAccelIncrease = 2.0f; //Represents the percentage that exhaust will be increased while engine is increasing RPMs.

        public bool DieselEngineConfigured = false; // flag to indicate that the user has configured a diesel engine prime mover code block in the ENG file

        // Icik
        public float MPExhaustParticles = 10.0f;
        public float MPExhaustMagnitude = 1.5f;
        public int MPExhaustColorR = 0;
        public int MPExhaustColorG = 0;
        public int MPExhaustColorB = 0;      
        public float MPRealRPM;
        public float MPRealRPM0;
        public float MPDemandedRPM;
        public float MPIdleRPM;
        bool FirstFrame = true;
        public bool OnePushStart;
        public bool OnePushStop;
        public bool OnePushStartButton;        
        public float RealRPM0;
        public float DieselMotorWaterInitTemp;
        public float DieselMotorOilInitTemp;
        public float[] OverHeatTimer = new float[2];
        public float OverHeatTimer2 = 0;
        public float RealDieselWaterTemperatureDeg;
        public float RealDieselOilTemperatureDeg;
        bool MSGWaterOn;
        bool MSGOilOn;
        bool MSGWaterLowOn;
        bool MSGOilLowOn;
        public float CoolingEnableRPM;
        public float WaterCoolingPower = 325f;
        public float OilCoolingPower = 325f;
        float CoolingFlow;
        public float AIStartTimeToGo;
        public bool InitTriggerSetOff;
        bool ElevatedConsumptionMode = false;
        float ElevatedConsumptionIdleRPMBase = 0;
        /// <summary>
        /// Change rate when start the engine
        /// </summary>
        public float StartingChangeUpRPMpS;
        /// <summary>
        /// Change rate when stop the engine
        /// </summary>
        public float StoppingChangeDownRPMpS;
        /// <summary>
        /// "Jerk" of the RPM when start the engine
        /// </summary>
        public float StartingRateOfChangeUpRPMpSS;
        /// <summary>
        /// "Jerk" of the RPM when stop the engine
        /// </summary>
        public float StoppingRateOfChangeDownRPMpSS;

        /// <summary>
        /// Current Engine oil pressure in PSI
        /// </summary>
        public float DieselOilPressurePSI
        {
            get
            {
                // Icik
                // Tlakování mazacího čerpadla při spouštění motoru
                if (locomotive.DieselStartDelay > 0.1f)
                {
                    if (locomotive.StopButtonReleased || ((locomotive.StartButtonPressed || locomotive.StartLooseCon || OnePushStartButton) && locomotive.DieselStartTime > 0))
                    {
                        if (RealRPM0 < IdleRPM)
                            RealRPM0 += IdleRPM / locomotive.DieselStartDelay * locomotive.Simulator.OneSecondLoop;
                    }
                    else
                    if ((!locomotive.StartButtonPressed && !locomotive.StartLooseCon && !OnePushStartButton) && (EngineStatus == Status.Stopped || EngineStatus == Status.Stopping))
                    {
                        if (RealRPM0 > 0)
                            RealRPM0 -= IdleRPM / (IdleRPM / StoppingRateOfChangeDownRPMpSS) * locomotive.Simulator.OneSecondLoop / 3;
                    }
                }
                else
                {
                    // Motory bez předmazávání
                    if (EngineStatus == Status.Stopped || EngineStatus == Status.Stopping)
                    {
                        if (RealRPM0 > 0)
                            RealRPM0 -= IdleRPM / (IdleRPM / StoppingRateOfChangeDownRPMpSS) * locomotive.Simulator.OneSecondLoop / 3;
                    }
                }

                if (RealRPM0 == 0 && EngineStatus == Status.Running)
                    RealRPM0 = RealRPM;

                if (EngineStatus == Status.Starting)
                {
                    if (RealRPM0 < IdleRPM)
                        RealRPM0 += IdleRPM / (IdleRPM / StartingRateOfChangeUpRPMpSS) * locomotive.Simulator.OneSecondLoop;
                }

                if (EngineStatus == Status.Running)
                {
                    if (RealRPM0 < RealRPM)
                        RealRPM0 += RealRPM / (RealRPM / RateOfChangeUpRPMpSS) * locomotive.Simulator.OneSecondLoop * 3;
                    if (RealRPM0 > RealRPM)
                        RealRPM0 -= RealRPM / (RealRPM / RateOfChangeDownRPMpSS) * locomotive.Simulator.OneSecondLoop;
                }
                
                if (RealRPM0 < 0)
                    RealRPM0 = 0;

                float k = (DieselMaxOilPressurePSI - DieselMinOilPressurePSI) / (MaxRPM - IdleRPM);
                float q = DieselMaxOilPressurePSI - k * MaxRPM;
                float res = k * RealRPM0 + q - dieseloilfailurePSI;
                if (res < 0f)
                    res = 0f;
                return res;
            }
        }
        /// <summary>
        /// Minimal oil pressure at IdleRPM
        /// </summary>
        public float DieselMinOilPressurePSI;
        /// <summary>
        /// Maximal oil pressure at MaxRPM
        /// </summary>
        public float DieselMaxOilPressurePSI;
        /// <summary>
        /// Oil failure/leakage is substracted from the DieselOilPressurePSI
        /// </summary>
        public float dieseloilfailurePSI = 0f;              //Intended to be implemented later
        /// <summary>
        /// Actual Engine temperature
        /// </summary>
        public float FakeDieselWaterTemperatureDeg = 0f;
        public float FakeDieselOilTemperatureDeg = 0f;
        /// <summary>
        /// Maximal engine temperature
        /// </summary>
        public float DieselMaxTemperatureDeg = 90;
        public float DieselMaxWaterTemperatureDeg;
        public float DieselMaxOilTemperatureDeg;
        public float HelperDieselMaxWaterTemperatureDeg;
        public float HelperDieselMaxOilTemperatureDeg;
        /// <summary>
        /// Time constant to heat up from zero to 63% of MaxTemperature
        /// </summary>
        public float DieselWaterTempTimeConstantSec = 720f;
        public float DieselOilTempTimeConstantSec = 1420f;
        /// <summary>
        /// Optimal temperature of the diesel at rated power
        /// </summary>
        public float DieselOptimalTemperatureDegC = 70f;
        public float DieselOptimalWaterTemperatureDegC;
        public float DieselOptimalOilTemperatureDegC;
        /// <summary>
        /// Steady temperature when idling
        /// </summary>
        public float DieselIdleTemperatureDegC = 60f;
        public float DieselIdleWaterTemperatureDegC;
        public float DieselIdleOilTemperatureDegC;
        /// <summary>
        /// Hysteresis of the cooling regulator
        /// </summary>
        public float DieselTempCoolingHyst = 5f;
        public float DieselTempWaterCoolingHyst;
        public float DieselTempOilCoolingHyst;
        /// <summary>
        /// Cooling system indicator
        /// </summary>
        public bool WaterTempCoolingRunning = false;
        public bool OilTempCoolingRunning = false;
        public bool WaterTempCoolingLowRunning = false;
        public bool OilTempCoolingLowRunning = false;
        public float ElevatedConsumptionIdleRPM;
        public float ElevatedConsumptionIdleRPMCompressor;
        public float ElevatedConsumptionIdleRPMHeatingSummer;
        public float ElevatedConsumptionIdleRPMHeatingWinter;
        public float WaterCoolingPlatesUpS;
        public float WaterCoolingPlatesDownS;
        public float OilCoolingPlatesUpS;
        public float OilCoolingPlatesDownS;
        public float CoolingFlowBase;
        public bool IndependentWaterPlates;
        public bool IndependentOilPlates;

        /// <summary>
        /// Load of the engine
        /// </summary>
        public float LoadPercent
        {
            get
            {
                // Icik
                //return (CurrentDieselOutputPowerW <= 0f ? 0f : (OutputPowerW * 100f / CurrentDieselOutputPowerW)) ;
                if (EngineStatus != Status.Running) return 0;
                return MathHelper.Clamp((Math.Abs(locomotive.TractiveForceN) == 0f ? (locomotive.PowerReductionResult1 / 0.85f * MaximumDieselPowerW * 100f / MaximumDieselPowerW + (LoadSMCoef * MaximumDieselPowerW * 100f/ MaximumDieselPowerW))
                : (((OutputPowerW / 0.85f) + (locomotive.PowerReductionResult1 / 0.85f * MaximumDieselPowerW)) * 100f / MaximumDieselPowerW) + (LoadSMCoef * MaximumDieselPowerW * 100f / MaximumDieselPowerW)), 0, 100); 
            }
        }
        // Icik
        /// <summary>
        /// Load SM coeficient
        /// </summary>
        public float LoadSMCoef
        {
            get
            {                
                float LoadSM;
                float LoadEDB = 0;
                // 30kW chlazení dieselu + 20kW chlazení trakčáků + 10kW dobíjení aku + 5kW napájení elektroniky na 1000kW loko
                if (locomotive.DynamicBrake != null) LoadEDB = 10000; // Chlazení EDB 10kW na 1000kW loko
                LoadSM = ((30000f + 20000f + 10000f + 5000f + LoadEDB) / 0.85f / 1000000f * MaximumDieselPowerW) * (1000000f / MaximumDieselPowerW);                
                LoadSM /= 1000000f;
                LoadSM = MathHelper.Clamp(LoadSM, 0, 1);                
                return LoadSM;
            }
        }

        /// <summary>
        /// The engine is connected to the gearbox
        /// </summary>
        public bool HasGearBox { get { return GearBox != null; } }

        /// <summary>
        /// Current power available to the Input traction motors
        /// </summary>
        public float CurrentDieselInputPowerW;

        #endregion

        /// <summary>
        /// Parses parameters from the stf reader
        /// </summary>
        /// <param name="stf">Reference to the stf reader</param>
        /// <param name="loco">Reference to the locomotive</param>
        public virtual void Parse(STFReader stf, MSTSDieselLocomotive loco)
        {
            locomotive = loco;
            stf.MustMatch("(");
            bool end = false;
            while (!end)
            {
                string lowercasetoken = stf.ReadItem().ToLower();
                switch (lowercasetoken)
                {
                    case "idlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.IdleRPM; break;
                    case "maxrpm": MaxRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.MaxRPM; break;
                    case "startingrpm": StartingRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.StartingRPM; break;
                    case "startingconfirmrpm": StartingConfirmationRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.StartingConfirmRPM; break;
                    case "changeuprpmps": ChangeUpRPMpS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); initLevel |= SettingsFlags.ChangeUpRPMpS; break;
                    case "changedownrpmps": ChangeDownRPMpS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); initLevel |= SettingsFlags.ChangeDownRPMpS; break;
                    case "rateofchangeuprpmpss": RateOfChangeUpRPMpSS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); initLevel |= SettingsFlags.RateOfChangeUpRPMpSS; break;
                    case "rateofchangedownrpmpss": RateOfChangeDownRPMpSS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); initLevel |= SettingsFlags.RateOfChangeDownRPMpSS; break;
                    case "maximalpower": MaximumDieselPowerW = stf.ReadFloatBlock(STFReader.UNITS.Power, 0); initLevel |= SettingsFlags.MaximalDieselPowerW; break;
                    case "idleexhaust": InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.IdleExhaust; break;
                    case "maxexhaust": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.MaxExhaust; break;
                    case "exhaustdynamics": ExhaustAccelIncrease = stf.ReadFloatBlock(STFReader.UNITS.None, 0); initLevel |= SettingsFlags.ExhaustDynamics; break;
                    case "exhaustdynamicsdown": ExhaustDecelReduction = stf.ReadFloatBlock(STFReader.UNITS.None, null); initLevel |= SettingsFlags.ExhaustDynamics; break;
                    case "exhaustcolor":
                        // Color byte order changed in XNA 4 from BGRA to RGBA
                        ExhaustSteadyColor.PackedValue = stf.ReadHexBlock(Color.Gray.PackedValue);
                        var tempSR = ExhaustSteadyColor.R;
                        ExhaustSteadyColor.R = ExhaustSteadyColor.B;
                        ExhaustSteadyColor.B = tempSR;
                        initLevel |= SettingsFlags.ExhaustColor;
                        break;
                    case "exhausttransientcolor":
                        ExhaustTransientColor.PackedValue = stf.ReadHexBlock(Color.Black.PackedValue);
                        var tempTR = ExhaustTransientColor.R;
                        ExhaustTransientColor.R = ExhaustTransientColor.B;
                        ExhaustTransientColor.B = tempTR;
                        initLevel |= SettingsFlags.ExhaustTransientColor;
                        break;
                    case "dieselpowertab": DieselPowerTab = new Interpolator(stf); initLevel |= SettingsFlags.DieselPowerTab; break;
                    case "dieselconsumptiontab": DieselConsumptionTab = new Interpolator(stf); initLevel |= SettingsFlags.DieselConsumptionTab; break;
                    case "throttlerpmtab":
                        ThrottleRPMTab = new Interpolator(stf);
                        initLevel |= SettingsFlags.ThrottleRPMTab;
                        // This prevents rpm values being exactly the same for different throttle rates, as when this table is reversed, OR is unable to correctly determine a correct apparent throttle value.
                        // TO DO - would be good to be able to handle rpm values the same, and -ve if possible.
                        var size = ThrottleRPMTab.GetSize();
                        var precY = ThrottleRPMTab.Y[0];
                        for (int i = 1; i < size; i++)
                        {
                            if (ThrottleRPMTab.Y[i] <= precY) ThrottleRPMTab.Y[i] = precY + 1;
                            precY = ThrottleRPMTab.Y[i];
                        }
                        break;
                    case "dieseltorquetab": DieselTorqueTab = new Interpolator(stf); initLevel |= SettingsFlags.DieselTorqueTab; break;
                    case "minoilpressure": DieselMinOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 0); initLevel |= SettingsFlags.MinOilPressure; break;
                    case "maxoilpressure": DieselMaxOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 0); initLevel |= SettingsFlags.MaxOilPressure; break;
                    case "maxtemperature": DieselMaxTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 90); initLevel |= SettingsFlags.MaxTemperature; break;
                    case "maxtemperaturewater": DieselMaxWaterTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 90); break;
                    case "maxtemperatureoil": DieselMaxOilTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 90); break;
                    case "helpermaxtemperaturewatertopowerdown": HelperDieselMaxWaterTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 90); break;
                    case "helpermaxtemperatureoiltopowerdown": HelperDieselMaxOilTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 90); break;
                    case "cooling": EngineCooling = (Cooling)stf.ReadIntBlock((int)Cooling.Proportional); initLevel |= SettingsFlags.Cooling; break; //ReadInt changed to ReadIntBlock
                    case "temptimeconstant": DieselWaterTempTimeConstantSec = stf.ReadFloatBlock(STFReader.UNITS.Time, 720); initLevel |= SettingsFlags.TempTimeConstant; break;
                    case "tempwatertimeconstant": DieselWaterTempTimeConstantSec = stf.ReadFloatBlock(STFReader.UNITS.Time, 720); initLevel |= SettingsFlags.TempTimeConstant; break;
                    case "tempoiltimeconstant": DieselOilTempTimeConstantSec = stf.ReadFloatBlock(STFReader.UNITS.Time, 1440); initLevel |= SettingsFlags.TempTimeConstant; break;
                    case "opttemperature": DieselOptimalTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 70f); initLevel |= SettingsFlags.OptTemperature; break;
                    case "opttemperaturewater": DieselOptimalWaterTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 70f); break;
                    case "opttemperatureoil": DieselOptimalOilTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 60f); break;
                    case "idletemperature": DieselIdleTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 60f); initLevel |= SettingsFlags.IdleTemperature; break;
                    case "idlewatertemperature": DieselIdleWaterTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 60f); break;
                    case "idleoiltemperature": DieselIdleOilTemperatureDegC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 60f);  break;
                    case "tempcoolinghyst": DieselTempCoolingHyst = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 5f); break;
                    case "watertempcoolinghyst": DieselTempWaterCoolingHyst = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 5f); break;
                    case "oiltempcoolinghyst": DieselTempOilCoolingHyst = stf.ReadFloatBlock(STFReader.UNITS.Temperature, 5f); break;
                    case "coolingenablerpm": CoolingEnableRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0f); break;
                    case "watercoolingpower": WaterCoolingPower = stf.ReadFloatBlock(STFReader.UNITS.None, 75f); WaterCoolingPower = MathHelper.Clamp(WaterCoolingPower, 30, 1000); break;
                    case "oilcoolingpower": OilCoolingPower = stf.ReadFloatBlock(STFReader.UNITS.None, 75f); OilCoolingPower = MathHelper.Clamp(OilCoolingPower, 30, 1000); break;
                    case "elevatedconsumptionidlerpm": ElevatedConsumptionIdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "turbodelayup": TurboDelayUpS = stf.ReadFloatBlock(STFReader.UNITS.Time, 2f); break;
                    case "turbodelaydown": TurboDelayDownS = stf.ReadFloatBlock(STFReader.UNITS.Time, 4f); break;
                    case "turbochargerpm": TurboChargeRPMpS = stf.ReadFloatBlock(STFReader.UNITS.None, 10000f); break;
                    case "turbodischargerpm": TurboDischargeRPMpS = stf.ReadFloatBlock(STFReader.UNITS.None, 20000f); break;
                    case "maxturborpm": MaxTurboRPM = stf.ReadFloatBlock(STFReader.UNITS.None, 200000f); break;
                    case "maxturbopressure": MaxTurboPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 3f * 14.50377f); break;
                    case "startingchangeuprpmps": StartingChangeUpRPMpS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); break;
                    case "stoppingchangedownrpmps": StoppingChangeDownRPMpS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); break;
                    case "startingrateofchangeuprpmpss": StartingRateOfChangeUpRPMpSS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); break;
                    case "stoppingrateofchangedownrpmpss": StoppingRateOfChangeDownRPMpSS = MathHelper.Clamp(stf.ReadFloatBlock(STFReader.UNITS.None, 0), 0, 1000); break;
                    case "onepushstart": OnePushStart = stf.ReadBoolBlock(false); break;
                    case "onepushstop": OnePushStop = stf.ReadBoolBlock(false); break;
                    case "elevatedconsumptionidlerpmcompressor": ElevatedConsumptionIdleRPMCompressor = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "elevatedconsumptionidlerpmheatingsummer": ElevatedConsumptionIdleRPMHeatingSummer = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;
                    case "elevatedconsumptionidlerpmheatingwinter": ElevatedConsumptionIdleRPMHeatingWinter = stf.ReadFloatBlock(STFReader.UNITS.None, 0); break;                    
                    case "watercoolingplatesup": WaterCoolingPlatesUpS = stf.ReadFloatBlock(STFReader.UNITS.Time, 2f); break;
                    case "watercoolingplatesdown": WaterCoolingPlatesDownS = stf.ReadFloatBlock(STFReader.UNITS.Time, 2f); break;
                    case "oilcoolingplatesup": OilCoolingPlatesUpS = stf.ReadFloatBlock(STFReader.UNITS.Time, 2f); break;
                    case "oilcoolingplatesdown": OilCoolingPlatesDownS = stf.ReadFloatBlock(STFReader.UNITS.Time, 2f); break;
                    case "coolingflow": CoolingFlowBase = stf.ReadFloatBlock(STFReader.UNITS.None, 1f); CoolingFlowBase = MathHelper.Clamp(CoolingFlowBase, 0.0f, 5.0f); break;
                    case "independentwaterplates": IndependentWaterPlates = stf.ReadBoolBlock(false); break;
                    case "independentoilplates": IndependentOilPlates = stf.ReadBoolBlock(false); break;

                    default:
                        end = true;
                        break;
                }
            }
        }

        public void Initialize(bool start)
        {
            RPMRange = MaxRPM - IdleRPM;
            MagnitudeRange = MaxMagnitude - InitialMagnitude;
            ExhaustRange = MaxExhaust - InitialExhaust;
            ExhaustSteadyColor.A = 10;
            ExhaustDecelColor.A = 10;

            // Icik
            if (StartingChangeUpRPMpS == 0)
                StartingChangeUpRPMpS = ChangeUpRPMpS;
            if (StoppingChangeDownRPMpS == 0)
                StoppingChangeDownRPMpS = ChangeDownRPMpS;
            if (StartingRateOfChangeUpRPMpSS == 0)
                StartingRateOfChangeUpRPMpSS = RateOfChangeUpRPMpSS;
            if (StoppingRateOfChangeDownRPMpSS == 0)
                StoppingRateOfChangeDownRPMpSS = RateOfChangeDownRPMpSS;            
        }


        // Icik
        float TurboDelayUpS = 2;
        float TurboDelayDownS = 4;
        float TurboChargeRPMpS = 0;
        float TurboDischargeRPMpS = 0;
        float MaxTurboRPM = 200000;
        float MaxTurboPressurePSI = 3 * 14.50377f;
        float TurboTimerUp;
        float TurboTimerDown;                
        bool TurboCanBoostUp;
        bool TurboCanBoostDown;        
        float turboRPM;       
        public float TurboRPM;
        public float TurboLoad;
        public float TurboPressureBar;
        public void TurboRPMLoad(float elapsedClockSeconds)
        {
            if (TurboChargeRPMpS == 0) TurboChargeRPMpS = MaxTurboRPM / 20;
            if (TurboDischargeRPMpS == 0) TurboDischargeRPMpS = MaxTurboRPM / 10;

            if (FirstFrame)                
                turboRPM = RealRPM;

            if (RealRPM > 1.01f * turboRPM && !TurboCanBoostUp)
            {
                TurboTimerUp += elapsedClockSeconds;
                if (TurboTimerUp > TurboDelayUpS)
                {
                    TurboTimerUp = 0;
                    TurboCanBoostUp = true;
                }
            }
            if (RealRPM < 0.99f * turboRPM && !TurboCanBoostDown)
            {
                TurboTimerDown += elapsedClockSeconds;
                if (TurboTimerDown > TurboDelayDownS)
                {
                    TurboTimerDown = 0;
                    TurboCanBoostDown = true;
                }
            }
            if (TurboCanBoostUp)
            {
                if (turboRPM < RealRPM)
                    turboRPM += (TurboChargeRPMpS * MaxRPM / MaxTurboRPM) * elapsedClockSeconds;
                else
                    TurboCanBoostUp = false;
            }
            if (TurboCanBoostDown)
            {
                if (turboRPM > RealRPM)
                    turboRPM -= (TurboDischargeRPMpS * MaxRPM / MaxTurboRPM) * elapsedClockSeconds;
                else
                    TurboCanBoostDown = false;
            }
            turboRPM = MathHelper.Clamp(turboRPM, 0, MaxRPM);
            TurboRPM = turboRPM / MaxRPM * MaxTurboRPM;            
            TurboLoad = turboRPM / MaxRPM * 100;
            TurboPressureBar = turboRPM / MaxRPM * MaxTurboPressurePSI / 14.50377f + 1;

            //locomotive.Simulator.Confirmer.Message(ConfirmLevel.MSG, "TurboRPM = " + locomotive.Variable8 + " ot/min" + "     TurboLoad = " + locomotive.Variable7 + " %");
            //locomotive.Simulator.Confirmer.Message(ConfirmLevel.MSG, "LoadSMCoef = " + LoadSMCoef);
        }

        float RegulatorRecoveryTimer;
        float RegulatorRecoveryTimer2;
        float RegulatorRecoveryTimer3;
        float RegulatorRecoveryTime;
        int preThrottlePercentPlus;
        int preThrottlePercentMinus;
        int preThrottlePercent;
        float RegulatorDeltaRPM = 0;
        float CurrentRPM0;
        float CurrentRPM;
        public bool RPMOverkill;
        bool RPMgrowth;
        float DeltaUpRPMpS;
        bool RegulatorStandChange;
        public void Update(float elapsedClockSeconds)
        {
            locomotive.DieselOilPressurePSI = DieselOilPressurePSI;

            // Inicializace AI
            if (locomotive.BrakeSystem.StartOn && ((!locomotive.IsPlayerTrain && !locomotive.LocoIsStatic) || locomotive.CarLengthM < 1f))
            {
                RealRPM = IdleRPM;
                EngineStatus = Status.Running;
            }
            // Inicializace hráče            
            if (locomotive.IsPlayerTrain && locomotive.BrakeSystem.StartOn && !locomotive.Simulator.Settings.AirEmpty)
            {
                RealRPM = IdleRPM;
                EngineStatus = Status.Running;
            }
            if (locomotive.IsPlayerTrain && EngineStatus != Status.Running && !InitTriggerSetOff)
            {
                //locomotive.SignalEvent(Event.EnginePowerOff);
                InitTriggerSetOff = true;
            }
            // Inicializace Static
            if (locomotive.IsPlayerTrain && locomotive.BrakeSystem.StartOn && locomotive.LocoIsStatic)
            {
                RealRPM = 0;
                EngineStatus = Status.Stopped;
                locomotive.Battery = false;
                locomotive.PowerKey = false;                                
            }

            if (EngineStatus == DieselEngine.Status.Running)
                DemandedThrottlePercent = locomotive.ThrottlePercent;
            else
                DemandedThrottlePercent = 0f;

            if (locomotive.Direction == Direction.Reverse)
                locomotive.PrevMotiveForceN *= -1f;

            if ((EngineStatus == DieselEngine.Status.Running) && (locomotive.ThrottlePercent > 0))
            {
                OutputPowerW = (locomotive.PrevMotiveForceN > 0 ? locomotive.PrevMotiveForceN * locomotive.AbsSpeedMpS : 0) / locomotive.DieselEngines.NumOfActiveEngines;
            }
            else
            {
                OutputPowerW = 0.0f;
            }

            if ((ThrottleRPMTab != null) && (EngineStatus == Status.Running))
            {
                DemandedRPM = ThrottleRPMTab[demandedThrottlePercent];                
            }

            if (GearBox != null)
            {
                if (RealRPM > 0)
                    GearBox.ClutchPercent = (RealRPM - GearBox.ShaftRPM) / RealRPM * 100f;
                else
                    GearBox.ClutchPercent = 100f;

                if (GearBox.CurrentGear != null)
                {
                    if (GearBox.IsClutchOn)
                        DemandedRPM = GearBox.ShaftRPM;
                }
            }

            if (RealRPM == IdleRPM)
            {
                ExhaustParticles = InitialExhaust;
                ExhaustMagnitude = InitialMagnitude;
                ExhaustColor = ExhaustSteadyColor;
            }
            // Startování a zastavování SM má svoji rychlost změny 
            if (EngineStatus == Status.Starting || EngineStatus == Status.Stopping)
            {
                if (RealRPM < DemandedRPM)
                {
                    dRPM = (float)Math.Min(Math.Sqrt(2 * StartingRateOfChangeUpRPMpSS * (DemandedRPM - RealRPM)), StartingChangeUpRPMpS);
                    if (dRPM > 1.0f) //The forumula above generates a floating point error that we have to compensate for so we can't actually test for zero.
                    {
                        ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustAccelIncrease;
                        ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustAccelIncrease;
                        ExhaustColor = ExhaustTransientColor;
                    }
                    else
                    {
                        dRPM = 0;
                        ExhaustParticles = InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange));
                        ExhaustMagnitude = InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange));
                        ExhaustColor = ExhaustSteadyColor;
                    }
                }
                else if (RealRPM > DemandedRPM)
                {
                    dRPM = (float)Math.Max(-Math.Sqrt(2 * StoppingRateOfChangeDownRPMpSS * (RealRPM - DemandedRPM)), -StoppingChangeDownRPMpS);
                    ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                    ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                    ExhaustColor = ExhaustDecelColor;
                }
            }
            else
            {
                // Spustí volnoběh při dosažení volnoběžných otáček při přerušeném stopování motoru
                if (locomotive.StopButtonReleased && RealRPM > 0.999f * IdleRPM)
                {                    
                    locomotive.StopButtonReleased = false;
                    locomotive.SignalEvent(Event.InitMotorIdle);
                    MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "INITMOTORIDLE", 1).ToString()));
                }
                if (locomotive.StopButtonReleased && RealRPM == 0)
                {
                    locomotive.StopButtonReleased = false;
                }                
                if (RealRPM < DemandedRPM)
                {                    
                    dRPM = (float)Math.Min(Math.Sqrt(2 * RateOfChangeUpRPMpSS * (DemandedRPM - RealRPM)), ChangeUpRPMpS);
                    if (dRPM > 1.0f) //The forumula above generates a floating point error that we have to compensate for so we can't actually test for zero.
                    {
                        ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustAccelIncrease;
                        ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustAccelIncrease;
                        ExhaustColor = ExhaustTransientColor;
                    }
                    else
                    {
                        dRPM = 0;
                        ExhaustParticles = InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange));
                        ExhaustMagnitude = InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange));
                        ExhaustColor = ExhaustSteadyColor;
                    }
                }
                else if (RealRPM > DemandedRPM)
                {                    
                    dRPM = (float)Math.Max(-Math.Sqrt(2 * RateOfChangeDownRPMpSS * (RealRPM - DemandedRPM)), -ChangeDownRPMpS);
                    ExhaustParticles = (InitialExhaust + ((ExhaustRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                    ExhaustMagnitude = (InitialMagnitude + ((MagnitudeRange * (RealRPM - IdleRPM) / RPMRange))) * ExhaustDecelReduction;
                    ExhaustColor = ExhaustDecelColor;
                }
            }

            // Uncertain about the purpose of this code piece?? Does there need to be a corresponding code for RateOfChangeUpRPMpSS???
            //            if (DemandedRPM < RealRPM && (OutputPowerW > (1.1f * CurrentDieselOutputPowerW)) && (EngineStatus == Status.Running))
            //            {
            //                dRPM = (CurrentDieselOutputPowerW - OutputPowerW) / MaximumDieselPowerW * 0.01f * RateOfChangeDownRPMpSS;
            //            }
            // Deleted to see what impact it has - was holding rpm artificialy high - http://www.elvastower.com/forums/index.php?/topic/33739-throttle-bug-in-recent-or-builds/page__gopid__256086#entry256086


            // Icik
            // Zvýší otáčky motoru při větším odběru proudu                     
            if (locomotive.HeatingIsOn || ((locomotive.CompressorIsOn || locomotive.Compressor2IsOn) && locomotive.AirBrakesIsCompressorElectricOrMechanical))
            {
                //ElevatedConsumptionIdleRPM = 650;
                ElevatedConsumptionMode = true;

                // Default při nezadání
                if (ElevatedConsumptionIdleRPM == 0
                    && ElevatedConsumptionIdleRPMCompressor == 0
                    && ElevatedConsumptionIdleRPMHeatingSummer == 0
                    && ElevatedConsumptionIdleRPMHeatingWinter == 0)
                    ElevatedConsumptionIdleRPMBase = IdleRPM * 1.1f;

                // Pokud se zadá jen ElevatedConsumptionIdleRPM
                if (ElevatedConsumptionIdleRPM > 0)                
                    ElevatedConsumptionIdleRPMBase = ElevatedConsumptionIdleRPM;
                
                // Při běhu kompresoru
                if (ElevatedConsumptionIdleRPMCompressor != 0
                    && (locomotive.CompressorIsOn || locomotive.Compressor2IsOn))
                    ElevatedConsumptionIdleRPMBase = ElevatedConsumptionIdleRPMCompressor;

                // Při zapnutí topení
                if (locomotive.HeatingIsOn)
                {                    
                    switch (locomotive.SeasonSwitchPosition[locomotive.LocoStation])
                    {
                        case false:
                            if (ElevatedConsumptionIdleRPMBase < ElevatedConsumptionIdleRPMHeatingSummer)
                                ElevatedConsumptionIdleRPMBase = ElevatedConsumptionIdleRPMHeatingSummer;
                            break;
                        case true:
                            if (ElevatedConsumptionIdleRPMBase < ElevatedConsumptionIdleRPMHeatingWinter)
                                ElevatedConsumptionIdleRPMBase = ElevatedConsumptionIdleRPMHeatingWinter;
                            break;
                    }
                }
            }
            else
            {
                ElevatedConsumptionMode = false;
                ElevatedConsumptionIdleRPMBase = 0;
            }

            // Icik
            // Sníží otáčky motoru kvůli ochraně TM 
            if (!RPMgrowth && (locomotive.OverVoltage || locomotive.OverCurrent || locomotive.HeatingOverCurrent))
            {
                if (RealRPM > IdleRPM)
                    RealRPM -= ChangeDownRPMpS * elapsedClockSeconds;
            }
            else
            {
                if (ElevatedConsumptionMode)
                {
                    if (RealRPM < ElevatedConsumptionIdleRPMBase)
                    {
                        DeltaUpRPMpS = MathHelper.Clamp(ChangeUpRPMpS, 0, 100);

                        if (RealRPM > 0.99f * ElevatedConsumptionIdleRPMBase)
                            DeltaUpRPMpS = MathHelper.Clamp(ChangeUpRPMpS, -5, 5);

                        RealRPM += DeltaUpRPMpS * elapsedClockSeconds;
                    }
                    else
                    {
                        if (RealRPM < 1.01f * ElevatedConsumptionIdleRPMBase)
                            dRPM = MathHelper.Clamp(dRPM, -5, 5);

                        RealRPM = Math.Max(RealRPM + (dRPM * elapsedClockSeconds), 0);
                    }
                }
                else
                {
                    if (dRPM < 0)
                    {
                        if (RealRPM < 1.01f * DemandedRPM)
                            dRPM = MathHelper.Clamp(dRPM, -5, 5);
                    }
                    if (dRPM > 0)
                    {
                        if (RealRPM > 0.99f * DemandedRPM)
                            dRPM = MathHelper.Clamp(dRPM, -5, 5);
                    }

                    RealRPM = Math.Max(RealRPM + (dRPM * elapsedClockSeconds), 0);
                }
            }

            // Icik
            // Zvýšení otáček motoru při prudkém snížení stupňů regulátorem
            if (locomotive.OverVoltage || EngineStatus != Status.Running)
            {
                RPMgrowth = false;
            }

            if (locomotive.IsLeadLocomotive() && ((EngineStatus == Status.Running && !locomotive.OverVoltage) || RPMOverkill))
            {
                if (!RPMgrowth && locomotive.CruiseControl != null && locomotive.CruiseControl.SpeedRegMode[locomotive.LocoStation] != CruiseControl.SpeedRegulatorMode.Manual)
                { }
                else
                {
                    bool CheckRPMOverkill = false;
                    switch (locomotive.LocomotiveTypeNumber)
                    {
                        case 750: { if (locomotive.LocomotiveTypeLongNumber == 7507) CheckRPMOverkill = false; else CheckRPMOverkill = true; break; }
                        case 753: { if (locomotive.LocomotiveTypeLongNumber == 7537) CheckRPMOverkill = false; else CheckRPMOverkill = true; break; }

                        case 466:
                        case 720:
                        case 721:
                        case 725:
                        case 740:
                        case 741:
                        case 742:
                        case 743:
                        case 749:
                        case 754:
                        case 770:
                        case 771:
                            CheckRPMOverkill = true;
                            break;
                    }                    

                    if (CheckRPMOverkill || locomotive.TractionSwitchEnable)
                    {
                        int CurrentThrottlePercent = (int)locomotive.LocalThrottlePercent;

                        bool TractionSwitchOverKill = false;
                        if (locomotive.TractionSwitchEnable)
                        {                            
                            if (locomotive.TractionSwitchPosition[locomotive.LocoStation] == 0 && preThrottlePercent != 0)
                            {
                                if (preThrottlePercent > 0 && LoadPercent > 15f)
                                {
                                    CurrentThrottlePercent = 0;
                                    TractionSwitchOverKill = true;
                                }
                            }

                            if (locomotive.TractionSwitchPosition[locomotive.LocoStation] == 1)
                            {
                                preThrottlePercent = (int)locomotive.LocalThrottlePercent;
                            }
                            else
                            if (!TractionSwitchOverKill)
                            {
                                preThrottlePercent = 0;
                            }
                        }

                        if (!locomotive.TractionSwitchEnable)
                        {
                            if (CurrentThrottlePercent > preThrottlePercent)
                            {
                                preThrottlePercent = CurrentThrottlePercent;
                            }

                            // Zpoždění reakce regulátoru na změnu dávky paliva                        
                            RegulatorRecoveryTime = 1.0f;
                            if (Math.Abs(dRPM) == 0)
                            {
                                RegulatorStandChange = false;
                            }

                            if (CurrentThrottlePercent > preThrottlePercentMinus)
                            {
                                preThrottlePercentMinus = CurrentThrottlePercent;
                            }
                            else
                            if (CurrentThrottlePercent < preThrottlePercentPlus)
                            {
                                preThrottlePercentPlus = CurrentThrottlePercent;
                            }
                            if ((CurrentThrottlePercent < preThrottlePercentMinus && !RPMgrowth && !RPMOverkill)
                                || RegulatorRecoveryTimer3 > 0)
                            {
                                preThrottlePercentMinus = CurrentThrottlePercent;
                                preThrottlePercentPlus = CurrentThrottlePercent;
                                if (RegulatorRecoveryTimer3 == 0)
                                {
                                    CurrentRPM0 = RealRPM + ((Math.Abs(dRPM) + Math.Abs(DeltaUpRPMpS)) * elapsedClockSeconds);
                                }
                                RegulatorRecoveryTimer3 += elapsedClockSeconds;
                                if (RegulatorRecoveryTimer3 > RegulatorRecoveryTime)
                                {
                                    RegulatorRecoveryTimer3 = 0;
                                    RegulatorStandChange = true;
                                }
                                if (!RegulatorStandChange)
                                    RealRPM = CurrentRPM0;
                            }
                            else
                            if ((CurrentThrottlePercent > preThrottlePercentPlus && !RPMgrowth && !RPMOverkill)
                                || RegulatorRecoveryTimer3 > 0)
                            {
                                preThrottlePercentPlus = CurrentThrottlePercent;
                                preThrottlePercentMinus = CurrentThrottlePercent;
                                if (RegulatorRecoveryTimer3 == 0)
                                {
                                    CurrentRPM0 = RealRPM - ((Math.Abs(dRPM) + Math.Abs(DeltaUpRPMpS)) * elapsedClockSeconds);
                                }
                                RegulatorRecoveryTimer3 += elapsedClockSeconds;
                                if (RegulatorRecoveryTimer3 > RegulatorRecoveryTime)
                                {
                                    RegulatorRecoveryTimer3 = 0;
                                    RegulatorStandChange = true;
                                }
                                if (!RegulatorStandChange)
                                    RealRPM = CurrentRPM0;
                            }
                        }
                        // Výpočet nárůstu otáček 
                        if ((locomotive.PowerCurrent1 > 0 || TractionSwitchOverKill) && CurrentThrottlePercent < preThrottlePercent)
                        {
                            preThrottlePercent = CurrentThrottlePercent;
                            CurrentRPM = RealRPM;

                            if (ElevatedConsumptionMode)
                            {
                                float ElevatedConsumptionModeDelta = ElevatedConsumptionIdleRPMBase - ThrottleRPMTab[0];
                                if (RealRPM > 1.309f * (ThrottleRPMTab[locomotive.ThrottlePercent] + ElevatedConsumptionModeDelta) || TractionSwitchOverKill)
                                {
                                    if (TractionSwitchOverKill)
                                    {
                                        RegulatorDeltaRPM = 30.0f * RealRPM / ThrottleRPMTab[0];
                                    }
                                    else
                                    {
                                        RegulatorDeltaRPM = 30.0f * RealRPM / ThrottleRPMTab[locomotive.ThrottlePercent];
                                        //locomotive.Simulator.Confirmer.MSG("RegulatorDeltaRPM = " + RegulatorDeltaRPM);
                                    }
                                }
                            }
                            else
                            {
                                if (RealRPM > 1.309f * ThrottleRPMTab[locomotive.ThrottlePercent] || TractionSwitchOverKill)
                                {
                                    if (TractionSwitchOverKill)
                                    {
                                        RegulatorDeltaRPM = 30.0f * RealRPM / ThrottleRPMTab[0];
                                    }
                                    else
                                    {
                                        RegulatorDeltaRPM = 30.0f * RealRPM / ThrottleRPMTab[locomotive.ThrottlePercent];
                                        //locomotive.Simulator.Confirmer.MSG("RegulatorDeltaRPM = " + RegulatorDeltaRPM);
                                    }
                                }
                            }
                        }

                        // Strmost nárůstu otáček
                        if (locomotive.TractionSwitchEnable)
                        {
                            if ((TractionSwitchOverKill && RegulatorDeltaRPM > 0) || RPMgrowth)
                            {
                                if (RealRPM < CurrentRPM + RegulatorDeltaRPM)
                                {
                                    RealRPM += 200.0f * CurrentRPM / MaxRPM * elapsedClockSeconds;
                                    RPMgrowth = true;
                                }
                                if (RealRPM > 0.999f * CurrentRPM + RegulatorDeltaRPM)
                                {
                                    RegulatorRecoveryTimer += elapsedClockSeconds;
                                    RealRPM = CurrentRPM + RegulatorDeltaRPM;
                                    if (RealRPM > MaxRPM)
                                        RPMOverkill = true;
                                }
                            }
                        }
                        if (!locomotive.TractionSwitchEnable)
                        {
                            if (((locomotive.PowerCurrent1 == 0) && RegulatorDeltaRPM > 0) || RPMgrowth)
                            {
                                if (RealRPM < CurrentRPM + RegulatorDeltaRPM)
                                {
                                    RealRPM += 200.0f * CurrentRPM / MaxRPM * elapsedClockSeconds;
                                    RPMgrowth = true;
                                }
                                if (RealRPM > 0.999f * CurrentRPM + RegulatorDeltaRPM)
                                {
                                    RegulatorRecoveryTimer += elapsedClockSeconds;
                                    RealRPM = CurrentRPM + RegulatorDeltaRPM;
                                    if (RealRPM > MaxRPM)
                                        RPMOverkill = true;
                                }
                            }
                        }


                        if (RegulatorRecoveryTimer == 0 && RegulatorDeltaRPM > 0 && !RPMgrowth)
                        {
                            RegulatorRecoveryTimer2 += elapsedClockSeconds;
                            if (RegulatorRecoveryTimer2 > 0.5f)
                            {
                                RegulatorRecoveryTimer2 = 0;
                                RegulatorDeltaRPM = 0;
                            }
                        }

                        // 1.5s pro vzpamatování regulátoru
                        if (RegulatorRecoveryTimer > 1.5f)
                        {
                            RegulatorRecoveryTimer = 0;
                            RegulatorDeltaRPM = 0;
                            CurrentRPM = 0;
                            RPMgrowth = false;
                        }

                        // Vypnutí motoru při přetočení
                        if (RPMOverkill)
                        {
                            locomotive.DieselEngines[0].Stop();
                            if (RealRPM < IdleRPM)
                            {
                                locomotive.SignalEvent(Event.EnginePowerOff);
                                RPMOverkill = false;
                            }
                        }
                    }
                }
            }            

            // Icik
            // Při vyšších otáčkách fouká kompresor rychleji
            if ((locomotive.CompressorIsOn || locomotive.Compressor2IsOn) && locomotive.AirBrakesIsCompressorElectricOrMechanical) // Jen pokud je mechanický kompresor
            {
                float CompressorChangeRateByRPMChange = RealRPM / IdleRPM;
                CompressorChangeRateByRPMChange = MathHelper.Clamp(CompressorChangeRateByRPMChange, 1, 1.75f);
                locomotive.MainResChargingRatePSIpS = CompressorChangeRateByRPMChange * locomotive.MainResChargingRatePSIpS0;
                locomotive.MainResChargingRatePSIpS_2 = CompressorChangeRateByRPMChange * locomotive.MainResChargingRatePSIpS0;
            }

            // Icik
            // Vstupní výkon dieselu
            CurrentDieselInputPowerW = CurrentDieselOutputPowerW / (1 - locomotive.PowerReduction);


            // Calculate the apparent throttle setting based upon the current rpm of the diesel prime mover. This allows the Tractive effort to increase with rpm to the throttle setting selected.
            // This uses the reverse Tab of the Throttle vs rpm Tab.
            if ((ReverseThrottleRPMTab != null) && (EngineStatus == Status.Running))
            {
                ApparentThrottleSetting = ReverseThrottleRPMTab[RealRPM];
            }

            ApparentThrottleSetting = MathHelper.Clamp(ApparentThrottleSetting, 0.0f, 100.0f);  // Clamp throttle setting within bounds

            if (DieselPowerTab != null)
            {
                CurrentDieselOutputPowerW = (DieselPowerTab[RealRPM] * (1 - locomotive.PowerReduction) <= MaximumDieselPowerW * (1 - locomotive.PowerReduction) ? DieselPowerTab[RealRPM] * (1 - locomotive.PowerReduction) : MaximumDieselPowerW * (1 - locomotive.PowerReduction));
                CurrentDieselOutputPowerW = CurrentDieselOutputPowerW < 0f ? 0f : CurrentDieselOutputPowerW;
                // Rail output power will never be the same as the diesel prime mover output power it will always have some level of loss of efficiency
                CurrentRailOutputPowerW = (RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * MaximumRailOutputPowerW * (1 - locomotive.PowerReduction);
                CurrentRailOutputPowerW = CurrentRailOutputPowerW < 0f ? 0f : CurrentRailOutputPowerW;
            }
            else
            {
                CurrentDieselOutputPowerW = (RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * MaximumDieselPowerW * (1 - locomotive.PowerReduction);
            }

            if ((EngineStatus != Status.Starting) && (RealRPM == 0f))
                EngineStatus = Status.Stopped;

            if (EngineStatus == Status.Starting)
            {
                // Icik
                if ((locomotive.StartButtonPressed || locomotive.StartLooseCon || OnePushStartButton) && (locomotive.DieselDirection_Start || locomotive.StartLooseCon))
                {
                    if ((RealRPM > (0.9f * StartingRPM)) && (RealRPM < StartingRPM))
                    {
                        DemandedRPM = 1.1f * StartingConfirmationRPM;
                        ExhaustColor = ExhaustTransientColor;
                        ExhaustParticles = (MaxExhaust - InitialExhaust) / (0.5f * StartingRPM - StartingRPM) * (RealRPM - 0.5f * StartingRPM) + InitialExhaust;
                    }
                }
                if ((!locomotive.StartButtonPressed && !locomotive.StartLooseCon && !OnePushStartButton) || (!locomotive.DieselDirection_Start && !locomotive.StartLooseCon))
                {
                    locomotive.DieselEngines[0].Stop();
                    locomotive.SignalEvent(Event.StartUpMotorBreak);                    
                }

                if ((RealRPM > 0.9f * StartingConfirmationRPM))// && (RealRPM < 0.9f * IdleRPM))
                {
                    EngineStatus = Status.Running;
                    locomotive.StartLooseCon = false;
                    OnePushStartButton = false;
                    if (!locomotive.IsPlayerTrain)
                        locomotive.Variable2 = 0.01f;
                }
            }
            
            if ((EngineStatus == Status.Stopped) || (EngineStatus == Status.Stopping) || ((EngineStatus == Status.Starting) && (RealRPM < StartingRPM)))
            {
                ExhaustParticles = 0;
                DieselFlowLps = 0;
            }
            else
            {
                if (DieselConsumptionTab != null)
                {
                    DieselFlowLps = DieselConsumptionTab[RealRPM] / 3600.0f;
                }
                else
                {
                    if (ThrottlePercent == 0)
                        DieselFlowLps = DieselUsedPerHourAtIdleL / 3600.0f;
                    else
                        DieselFlowLps = ((DieselUsedPerHourAtMaxPowerL - DieselUsedPerHourAtIdleL) * ThrottlePercent / 100f + DieselUsedPerHourAtIdleL) / 3600.0f;
                }
            }

            if (ExhaustParticles > 100f)
                ExhaustParticles = 100f;

            if (locomotive.PowerReduction == 1 && EngineStatus != Status.Stopped)     // Compressor blown, you get much smoke 
            {
                ExhaustColor = Color.WhiteSmoke;
                ExhaustParticles = 40f;
                ExhaustMagnitude = InitialMagnitude * 2;
            }

            // Icik
            DieselMotorTempControl(elapsedClockSeconds); // Řízení chování teploty motoru

            switch (EngineCooling)
            {
                //case Cooling.NoCooling:
                //    RealDieselWaterTemperatureDeg += elapsedClockSeconds * (LoadPercent * 0.01f * (95f - 60f) + 60f - RealDieselWaterTemperatureDeg) / DieselWaterTempTimeConstantSec;
                //    DieselTempCoolingRunning = false;
                //    break;
                //case Cooling.Mechanical:
                //    RealDieselWaterTemperatureDeg += elapsedClockSeconds * ((RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * 95f + 60f - RealDieselWaterTemperatureDeg) / DieselWaterTempTimeConstantSec;
                //    DieselTempCoolingRunning = true;
                //    break;

                case Cooling.Hysteresis:
                    // Malý chladící okruh
                    // Chlazení vody
                    if (DieselOptimalWaterTemperatureDegC != 0)
                        DieselOptimalTemperatureDegC = DieselOptimalWaterTemperatureDegC;
                    if (RealDieselWaterTemperatureDeg > DieselOptimalTemperatureDegC)
                        WaterTempCoolingLowRunning = true;

                    if (RealDieselWaterTemperatureDeg < DieselOptimalTemperatureDegC
                        || EngineStatus != Status.Running)
                    {
                        if (WaterTempCoolingLowRunning)
                            locomotive.SignalEvent(Event.DieselMotorWaterLowCoolingOff);
                        WaterTempCoolingLowRunning = false;
                        MSGWaterLowOn = false;                        
                    }
                                       
                    if (WaterTempCoolingLowRunning)
                    {
                        RealDieselWaterTemperatureDeg -= elapsedClockSeconds * (RealDieselWaterTemperatureDeg - (2.5f * locomotive.CarOutsideTempCBase)) / DieselWaterTempTimeConstantSec * (WaterCoolingPower / 250);
                        if (!MSGWaterLowOn)
                        {
                            //locomotive.Simulator.Confirmer.Message(ConfirmLevel.MSG, Simulator.Catalog.GetString("Malý chladící okruh zapnutý!"));
                            MSGWaterLowOn = true;
                            locomotive.SignalEvent(Event.DieselMotorWaterLowCooling);
                        }
                    }
                    // Chlazení oleje
                    if (DieselOptimalOilTemperatureDegC != 0)
                        DieselOptimalTemperatureDegC = DieselOptimalOilTemperatureDegC;
                    if (RealDieselOilTemperatureDeg > DieselOptimalTemperatureDegC)
                        OilTempCoolingLowRunning = true;

                    if (RealDieselOilTemperatureDeg < DieselOptimalTemperatureDegC
                        || EngineStatus != Status.Running)
                    {
                        if (OilTempCoolingLowRunning)
                            locomotive.SignalEvent(Event.DieselMotorOilLowCoolingOff);
                        OilTempCoolingLowRunning = false;
                        MSGOilLowOn = false;                        
                    }

                    if (OilTempCoolingLowRunning)
                    {
                        RealDieselOilTemperatureDeg -= elapsedClockSeconds * (RealDieselOilTemperatureDeg - (2.5f * locomotive.CarOutsideTempCBase)) / DieselOilTempTimeConstantSec * (OilCoolingPower / 250);
                        if (!MSGOilLowOn)
                        {
                            //locomotive.Simulator.Confirmer.Message(ConfirmLevel.MSG, Simulator.Catalog.GetString("Malý chladící okruh zapnutý!"));
                            MSGOilLowOn = true;
                            locomotive.SignalEvent(Event.DieselMotorOilLowCooling);
                        }
                    }


                    // Velký chladící okruh
                    // Chlazení vody
                    if (DieselTempWaterCoolingHyst != 0)
                        DieselTempCoolingHyst = DieselTempWaterCoolingHyst;
                    if (DieselOptimalWaterTemperatureDegC != 0)
                        DieselOptimalTemperatureDegC = DieselOptimalWaterTemperatureDegC;

                    if ((CoolingEnableRPM == 0 && (RealDieselWaterTemperatureDeg > DieselOptimalTemperatureDegC + DieselTempCoolingHyst))
                        || (CoolingEnableRPM > 0 && locomotive.EngineRPM >= CoolingEnableRPM))
                        WaterTempCoolingRunning = true;

                    if ((CoolingEnableRPM == 0 && RealDieselWaterTemperatureDeg < DieselOptimalTemperatureDegC)
                        || (CoolingEnableRPM > 0 && locomotive.EngineRPM < CoolingEnableRPM)
                        || EngineStatus != Status.Running)
                    {
                        if (WaterTempCoolingRunning)
                            locomotive.SignalEvent(Event.DieselMotorWaterCoolingOff);
                        WaterTempCoolingRunning = false;
                        MSGWaterOn = false;                        
                    }

                    if (WaterTempCoolingRunning)
                    {
                        RealDieselWaterTemperatureDeg -= elapsedClockSeconds * (RealDieselWaterTemperatureDeg - (1.5f * locomotive.CarOutsideTempCBase)) / DieselWaterTempTimeConstantSec * (WaterCoolingPower / 50);
                        if (!MSGWaterOn)
                        {
                            //locomotive.Simulator.Confirmer.Message(ConfirmLevel.MSG, Simulator.Catalog.GetString("Žaluzie otevřené a ventilátor zapnutý!"));
                            MSGWaterOn = true;
                            locomotive.SignalEvent(Event.DieselMotorWaterCooling);
                        }
                    }
                    // Chlazení oleje
                    if (DieselTempOilCoolingHyst != 0)
                        DieselTempCoolingHyst = DieselTempOilCoolingHyst;
                    if (DieselOptimalOilTemperatureDegC != 0)
                        DieselOptimalTemperatureDegC = DieselOptimalOilTemperatureDegC;

                    if (RealDieselOilTemperatureDeg > DieselOptimalTemperatureDegC + DieselTempCoolingHyst)
                        OilTempCoolingRunning = true;

                    if (RealDieselOilTemperatureDeg < DieselOptimalTemperatureDegC
                        || EngineStatus != Status.Running)
                    {
                        if (OilTempCoolingRunning)
                            locomotive.SignalEvent(Event.DieselMotorOilCoolingOff);
                        OilTempCoolingRunning = false;
                        MSGOilOn = false;                        
                    }

                    if (OilTempCoolingRunning)
                    {
                        RealDieselOilTemperatureDeg -= elapsedClockSeconds * (RealDieselOilTemperatureDeg - (1.5f * locomotive.CarOutsideTempCBase)) / DieselOilTempTimeConstantSec * (OilCoolingPower / 50);
                        if (!MSGOilOn)
                        {
                            //locomotive.Simulator.Confirmer.Message(ConfirmLevel.MSG, Simulator.Catalog.GetString("Žaluzie otevřené a ventilátor zapnutý!"));
                            MSGOilOn = true;
                            locomotive.SignalEvent(Event.DieselMotorOilCooling);
                        }
                    }
                    break;

                    //default:
                    //case Cooling.Proportional:
                    //    float cooling = (95f - RealDieselWaterTemperatureDeg) * 0.01f;
                    //    cooling = cooling < 0f ? 0 : cooling;
                    //    if (RealDieselWaterTemperatureDeg >= (80f))
                    //        DieselTempCoolingRunning = true;
                    //    if(RealDieselWaterTemperatureDeg < (80f - DieselTempCoolingHyst))
                    //        DieselTempCoolingRunning = false;

                    //    if (!DieselTempCoolingRunning)
                    //        cooling = 0f;

                    //    RealDieselWaterTemperatureDeg += elapsedClockSeconds * (LoadPercent * 0.01f * 95f - RealDieselWaterTemperatureDeg) / DieselWaterTempTimeConstantSec;
                    //    if (RealDieselWaterTemperatureDeg > DieselMaxTemperatureDeg - DieselTempCoolingHyst)
                    //        RealDieselWaterTemperatureDeg = DieselMaxTemperatureDeg - DieselTempCoolingHyst;
                    //    break;
            }


            if (GearBox != null)
            {
                if ((locomotive.IsLeadLocomotive()))
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (locomotive.GearBoxController.CurrentNotch > 0)
                            GearBox.NextGear = GearBox.Gears[locomotive.GearBoxController.CurrentNotch - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }
                else
                {
                    if (GearBox.GearBoxOperation == GearBoxOperation.Manual)
                    {
                        if (locomotive.GearboxGearIndex > 0)
                            GearBox.NextGear = GearBox.Gears[locomotive.GearboxGearIndex - 1];
                        else
                            GearBox.NextGear = null;
                    }
                }
                if (GearBox.CurrentGear == null)
                    OutputPowerW = 0f;

                GearBox.Update(elapsedClockSeconds);
            }

            // Icik
            TurboRPMLoad(elapsedClockSeconds);

            FirstFrame = false;
        }

        int RunCycle;
        // Icik
        public void DieselMotorTempControl(float elapsedClockSeconds)
        {
            //if (!locomotive.IsPlayerTrain)
            //{
            //    RealDieselWaterTemperatureDeg = DieselIdleTemperatureDegC;
            //    RealDieselOilTemperatureDeg = DieselIdleTemperatureDegC - 5;
            //    return;
            //}

            EngineCooling = Cooling.Hysteresis;

            // Volitelné
            //DieselMaxTemperatureDeg = 90;
            //DieselOptimalTemperatureDegC = 70;            
            //DieselIdleTemperatureDegC = 60;
            //DieselWaterTempTimeConstantSec = 720;
            //DieselOilTempTimeConstantSec = 1440;
            //DieselTempCoolingHyst = 10;

            // Inicializační setup teplot
            if (locomotive.BrakeSystem.StartOn || (RunCycle > 0 && DieselMotorWaterInitTemp == 0))
            {
                RunCycle++;
                if (locomotive.Battery)
                {
                    if (DieselIdleWaterTemperatureDegC != 0)
                        DieselIdleTemperatureDegC = DieselIdleWaterTemperatureDegC;
                    DieselMotorWaterInitTemp = DieselIdleTemperatureDegC;

                    if (DieselIdleOilTemperatureDegC != 0)
                    {
                        DieselIdleTemperatureDegC = DieselIdleOilTemperatureDegC;
                        DieselMotorOilInitTemp = DieselIdleTemperatureDegC;
                    }
                    else
                        DieselMotorOilInitTemp = DieselIdleTemperatureDegC - 5;

                    FakeDieselWaterTemperatureDeg = DieselMotorWaterInitTemp;
                    FakeDieselOilTemperatureDeg = DieselMotorOilInitTemp;
                }
                else
                {
                    DieselMotorWaterInitTemp = locomotive.CarOutsideTempC0;
                    DieselMotorOilInitTemp = locomotive.CarOutsideTempC0;
                }

                if (!locomotive.Simulator.Settings.AirEmpty)
                {
                    if (DieselIdleWaterTemperatureDegC != 0)
                        DieselIdleTemperatureDegC = DieselIdleWaterTemperatureDegC;
                    RealDieselWaterTemperatureDeg = DieselIdleTemperatureDegC;

                    if (DieselIdleOilTemperatureDegC != 0)
                    {
                        DieselIdleTemperatureDegC = DieselIdleOilTemperatureDegC;
                        RealDieselOilTemperatureDeg = DieselIdleTemperatureDegC;
                    }
                    else
                        RealDieselOilTemperatureDeg = DieselIdleTemperatureDegC - 5;
                    
                    locomotive.DieselLocoTempReady = true;
                }
                else
                {
                    RealDieselWaterTemperatureDeg = DieselMotorWaterInitTemp;
                    RealDieselOilTemperatureDeg = DieselMotorOilInitTemp;
                }
                return;
            }
            RunCycle = 0;

            if (locomotive.Battery)
            {
                DieselMotorWaterInitTemp = RealDieselWaterTemperatureDeg;
                DieselMotorOilInitTemp = RealDieselOilTemperatureDeg;

                if (RealDieselWaterTemperatureDeg > locomotive.CarOutsideTempC0)
                    DieselMotorWaterInitTemp = RealDieselWaterTemperatureDeg;
                else
                    DieselMotorWaterInitTemp = locomotive.CarOutsideTempC0;

                if (RealDieselOilTemperatureDeg > locomotive.CarOutsideTempC0)
                    DieselMotorOilInitTemp = RealDieselOilTemperatureDeg;
                else
                    DieselMotorOilInitTemp = locomotive.CarOutsideTempC0;
            }
            else
            {
                DieselMotorWaterInitTemp = 0;
                DieselMotorOilInitTemp = 0;
            }

            if (FakeDieselWaterTemperatureDeg < DieselMotorWaterInitTemp)
                FakeDieselWaterTemperatureDeg += elapsedClockSeconds * 20;
            if (FakeDieselWaterTemperatureDeg > DieselMotorWaterInitTemp)
                FakeDieselWaterTemperatureDeg -= elapsedClockSeconds * 20;

            if (FakeDieselOilTemperatureDeg < DieselMotorOilInitTemp)
                FakeDieselOilTemperatureDeg += elapsedClockSeconds * 20;
            if (FakeDieselOilTemperatureDeg > DieselMotorOilInitTemp)
                FakeDieselOilTemperatureDeg -= elapsedClockSeconds * 20;
            

            // Fáze zahřívání motoru
            locomotive.PowerReductionResult6 = 0;
            if (EngineStatus == Status.Running && !locomotive.DieselLocoTempReady)
            {
                ExhaustColor = Color.TransparentBlack;
                //ExhaustParticles *= 2;
                ExhaustMagnitude *= 2;
                
                if (DieselIdleWaterTemperatureDegC != 0)
                    DieselIdleTemperatureDegC = DieselIdleWaterTemperatureDegC;
                locomotive.PowerReductionResult6 = MathHelper.Clamp(1 - (RealDieselWaterTemperatureDeg / (0.90f * DieselIdleTemperatureDegC)), 0, 0.5f);

                if (RealDieselWaterTemperatureDeg > 0.90f * DieselIdleTemperatureDegC)
                    locomotive.DieselLocoTempReady = true;
            }            
            if (DieselIdleWaterTemperatureDegC != 0)
                DieselIdleTemperatureDegC = DieselIdleWaterTemperatureDegC;
            if (RealDieselWaterTemperatureDeg < DieselIdleTemperatureDegC * 0.75f)
                locomotive.DieselLocoTempReady = false;
            
            //CoolingFlowBase = 2.0f;
            // Průtok čerpadla zvyšuje chlazení při vyšších otáčkách
            if (CoolingFlowBase == 0) CoolingFlowBase = 2.0f;            
            CoolingFlow = 0.1f;
            if (RealRPM > IdleRPM && RealRPM <= IdleRPM * 2.0f)
                CoolingFlow = (RealRPM / IdleRPM - 1.0f) * CoolingFlowBase * 10f;
            else
            if (RealRPM > IdleRPM)
                CoolingFlow = CoolingFlowBase * 10f;

            float CarOutsideTempDelta = MathHelper.Clamp(RealDieselWaterTemperatureDeg - locomotive.CarOutsideTempC0, -5f , 5f);

            // Voda
            // Teplotu zvyšují otáčky a zátěž motoru
            if (EngineStatus == Status.Running)
            {
                float DieselIdleTemperatureDelta = MathHelper.Clamp(DieselIdleTemperatureDegC / RealDieselWaterTemperatureDeg, 1, 10) != 1 ? MathHelper.Clamp(DieselIdleTemperatureDegC / RealDieselWaterTemperatureDeg, 1, 10) * 5.0f : 1.0f;
                if (DieselIdleWaterTemperatureDegC != 0)
                    DieselIdleTemperatureDegC = DieselIdleWaterTemperatureDegC;
                RealDieselWaterTemperatureDeg += elapsedClockSeconds * (LoadPercent * 0.02f * (120 - DieselIdleTemperatureDegC) + DieselIdleTemperatureDegC - RealDieselWaterTemperatureDeg) * 2.5f / DieselWaterTempTimeConstantSec;
                RealDieselWaterTemperatureDeg += elapsedClockSeconds * ((RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * 120 + DieselIdleTemperatureDegC - RealDieselWaterTemperatureDeg) * 1.5f * DieselIdleTemperatureDelta / DieselWaterTempTimeConstantSec;
            }
            if (float.IsNaN(RealDieselWaterTemperatureDeg))
            {
                RealDieselWaterTemperatureDeg = FakeDieselWaterTemperatureDeg;
            }
            // Teplota okolí koriguje teplotu motoru
            // Čerpadlo při vyšších otáčkách má vyšší průtok chladící kapaliny            
            float RealDieselWaterTemperatureDegDelta = MathHelper.Clamp(elapsedClockSeconds * CarOutsideTempDelta * CoolingFlow / DieselWaterTempTimeConstantSec, 0, 100);
            RealDieselWaterTemperatureDeg -= RealDieselWaterTemperatureDegDelta;

            // Olej
            // Teplotu zvyšují otáčky a zátěž motoru
            if (EngineStatus == Status.Running)
            {
                float DieselIdleTemperatureDelta = MathHelper.Clamp(DieselIdleTemperatureDegC / RealDieselOilTemperatureDeg, 1, 10) != 1 ? MathHelper.Clamp(DieselIdleTemperatureDegC / RealDieselOilTemperatureDeg, 1, 10) * 5.0f : 1.0f;
                if (DieselIdleOilTemperatureDegC != 0)
                    DieselIdleTemperatureDegC = DieselIdleOilTemperatureDegC;
                RealDieselOilTemperatureDeg += elapsedClockSeconds * (LoadPercent * 0.02f * (120 - DieselIdleTemperatureDegC) + DieselIdleTemperatureDegC - RealDieselOilTemperatureDeg) * 2.5f / DieselOilTempTimeConstantSec;
                RealDieselOilTemperatureDeg += elapsedClockSeconds * ((RealRPM - IdleRPM) / (MaxRPM - IdleRPM) * 120 + DieselIdleTemperatureDegC - RealDieselOilTemperatureDeg) * 1.5f * DieselIdleTemperatureDelta / DieselOilTempTimeConstantSec;
            }
            if (float.IsNaN(RealDieselOilTemperatureDeg))
            {
                RealDieselOilTemperatureDeg = FakeDieselOilTemperatureDeg;
            }
            // Teplota okolí koriguje teplotu motoru
            // Čerpadlo při vyšších otáčkách má vyšší průtok chladící kapaliny
            float RealDieselOilTemperatureDegDelta = MathHelper.Clamp(elapsedClockSeconds * CarOutsideTempDelta * CoolingFlow / (DieselOilTempTimeConstantSec * 2), 0, 100);
            RealDieselOilTemperatureDeg -= RealDieselOilTemperatureDegDelta;

            // Poškození a vypnutí motoru
            if (locomotive.IsPlayerTrain)
            {
                for (int i = 0; i < 2; i++)
                {
                    // Testuje teplotu vody
                    if (i == 0 && DieselMaxWaterTemperatureDeg != 0)
                        DieselMaxTemperatureDeg = DieselMaxWaterTemperatureDeg;

                    // Testuje teplotu oleje
                    if (i == 1 && DieselMaxOilTemperatureDeg != 0)
                        DieselMaxTemperatureDeg = DieselMaxOilTemperatureDeg;

                    if ((i == 0 && RealDieselWaterTemperatureDeg > DieselMaxTemperatureDeg) || (i == 1 && RealDieselOilTemperatureDeg > DieselMaxTemperatureDeg))
                        OverHeatTimer[i] += elapsedClockSeconds;
                    else
                    {
                        OverHeatTimer[i] = 0;
                        locomotive.DieselMotorTempWarning = false;
                        locomotive.SignalEvent(Event.DieselMotorTempWarningOff);
                    }

                    if (OverHeatTimer[i] > 120)
                    {
                        locomotive.DieselMotorDefected = true;
                        if (EngineStatus == Status.Running && OverHeatTimer2 == 0)
                            locomotive.SignalEvent(Event.DieselMotorTempDefected);
                        locomotive.Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("The engine's wrecked!"));
                    }
                    else
                    if (OverHeatTimer[i] > 60)
                    {
                        locomotive.DieselMotorPowerLost = true;
                        locomotive.Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("The engine's damaged!"));
                    }
                    else
                    if (OverHeatTimer[i] > 1)
                    {
                        locomotive.DieselMotorTempWarning = true;
                        locomotive.SignalEvent(Event.DieselMotorTempWarning);
                        if (i == 0 && RealDieselWaterTemperatureDeg > DieselMaxTemperatureDeg)
                            locomotive.Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("The engine is overheating! Water temperature:") + " " + Math.Round(RealDieselWaterTemperatureDeg, 2) + "°C");
                        if (i == 1 && RealDieselOilTemperatureDeg > DieselMaxTemperatureDeg)
                            locomotive.Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("The engine is overheating! Oil temperature:") + " " + Math.Round(RealDieselOilTemperatureDeg, 2) + "°C");
                    }

                    if (locomotive.DieselMotorPowerLost)
                    {
                        locomotive.PowerReductionResult9 = 0.25f;
                        ExhaustColor = Color.DarkGray;
                    }
                    if (locomotive.DieselMotorDefected && EngineStatus == Status.Running)
                    {
                        OverHeatTimer2 += elapsedClockSeconds;
                        ExhaustColor = Color.Black;
                        ExhaustParticles = 4f;
                        ExhaustMagnitude = InitialMagnitude * 10;
                        if (OverHeatTimer2 > 10)
                            locomotive.DieselEngines[0].Stop();
                    }

                    //if (i == 0)
                    //    locomotive.Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Teplota motoru: " + Math.Round(RealDieselWaterTemperatureDeg, 2)));
                    //locomotive.Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Power reduction: " + locomotive.PowerReduction));
                }
            }
        }


        public Status Start()
        {
            switch (EngineStatus)
            {
                case Status.Stopped:
                case Status.Stopping:
                    // Icik
                    if (locomotive.DieselMotorDefected || RPMOverkill) 
                    {
                        DemandedRPM = 0;
                        EngineStatus = Status.Stopped;                                                   
                    }
                    else
                    if (locomotive.StopButtonReleased) // Přerušený stop motoru
                    {                        
                        DemandedRPM = IdleRPM;
                        EngineStatus = Status.Running;
                        locomotive.SignalEvent(Event.MotorStopBreak);
                    }
                    else
                    if ((locomotive.DieselDirectionController || locomotive.DieselDirectionController2 || locomotive.DieselDirectionController3 || locomotive.DieselDirectionController4) && locomotive.DieselDirection_Start)
                    {
                        locomotive.StopButtonReleased = false;
                        DemandedRPM = StartingRPM;
                        EngineStatus = Status.Starting;
                        locomotive.SignalEvent(Event.EnginePowerOn); // power on sound hook
                    }
                    else
                    if (locomotive.CarFrameUpdateState > 9 && !locomotive.DieselDirectionController && !locomotive.DieselDirectionController2 && !locomotive.DieselDirectionController3 && !locomotive.DieselDirectionController4)
                    {
                        DemandedRPM = StartingRPM;
                        EngineStatus = Status.Starting;
                        locomotive.SignalEvent(Event.EnginePowerOn); // power on sound hook
                    }
                    break;
                default:
                    break;
            }
            return EngineStatus;
        }

        public Status Stop()
        {
            if (EngineStatus != Status.Stopped)
            {
                DemandedRPM = 0;
                EngineStatus = Status.Stopping;
                if (RealRPM <= 0)
                    EngineStatus = Status.Stopped;                
                if (!RPMOverkill)
                    locomotive.SignalEvent(Event.EnginePowerOff); // power off sound hook
            }
            return EngineStatus;
        }

        public void Restore(BinaryReader inf)
        {
            EngineStatus = (Status)inf.ReadInt32();
            RealRPM = inf.ReadSingle();
            OutputPowerW = inf.ReadSingle();
            RealDieselWaterTemperatureDeg = inf.ReadSingle();
            RealDieselOilTemperatureDeg = inf.ReadSingle();
            FakeDieselWaterTemperatureDeg = inf.ReadSingle();
            FakeDieselOilTemperatureDeg = inf.ReadSingle();

            Boolean gearSaved = inf.ReadBoolean();  // read boolean which indicates gear data was saved

            if (((MSTSDieselLocomotive)locomotive).GearBox != null)
            {
                if (!((MSTSDieselLocomotive)locomotive).GearBox.IsInitialized || !gearSaved)
                    GearBox = null;
                else
                {
                    GearBox.Restore(inf);
                }
            }

        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)EngineStatus);
            outf.Write(RealRPM);
            outf.Write(OutputPowerW);
            outf.Write(RealDieselWaterTemperatureDeg);
            outf.Write(RealDieselOilTemperatureDeg);
            outf.Write(FakeDieselWaterTemperatureDeg);
            outf.Write(FakeDieselOilTemperatureDeg);
            if (GearBox != null)
            {
                outf.Write(true);
                GearBox.Save(outf);
            }
            else
            {
                outf.Write(false);
            }
        }

        public void InitializeMoving()
        {
            EngineStatus = Status.Running;
        }

        /// <summary>
        /// Fix or define a diesel prime mover engine code block. If the user has not defned a diesel eng, then OR will use this section to create one.
        /// If the user has left a parameter out of the code, then OR uses this section to try and set the missing values to a default value.
        /// Error code has been provided that will provide the user with an indication if a parameter has been left out.
        /// </summary>
        public void InitFromMSTS(MSTSDieselLocomotive loco)
        {
            if ((initLevel & SettingsFlags.IdleRPM) == 0)
            {
                if (DieselEngineConfigured && loco.IdleRPM != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    IdleRPM = loco.IdleRPM;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("IdleRpM not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", IdleRPM);

                }
                else if (IdleRPM == 0 && loco.IdleRPM != 0) // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    IdleRPM = loco.IdleRPM;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("IdleRpM: set at default value (BASIC Config) = {0}", IdleRPM);

                }
                else if (loco.IdleRPM == 0) // No default "MSTS" value present, set to arbitary value
                {
                    IdleRPM = 300.0f;
                    loco.IdleRPM = IdleRPM;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("IdleRpM not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", IdleRPM);

                }
            }

            if ((initLevel & SettingsFlags.MaxRPM) == 0)
            {
                if (DieselEngineConfigured && loco.MaxRPM != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    MaxRPM = loco.MaxRPM;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxRpM not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", MaxRPM);

                }
                else if (MaxRPM == 0 && loco.MaxRPM != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    MaxRPM = loco.MaxRPM;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxRpM: set at default value (BASIC Config) = {0}", MaxRPM);

                }
                else if (loco.MaxRPM == 0) // No default "MSTS" value present, set to arbitary value
                {
                    MaxRPM = 600.0f;
                    loco.MaxRPM = MaxRPM;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxRpM not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", MaxRPM);

                }
            }

            // Undertake a test to ensure that MaxRPM > IdleRPM by a factor of 1.5x
            if (MaxRPM / IdleRPM < 1.5)
            {
                const float RPMFactor = 1.5f;
                MaxRPM = IdleRPM * RPMFactor;

                if (loco.Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("MaxRPM < IdleRPM x 1.5, set MaxRPM at arbitary value = {0}", MaxRPM);
                }
            }

            InitialMagnitude = loco.InitialMagnitude;
            MaxMagnitude = loco.MaxMagnitude;
            if ((initLevel & SettingsFlags.MaxExhaust) == 0)
            {
                MaxExhaust = loco.MaxExhaust;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("MaxExhaust not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", MaxExhaust);
            }

            if ((initLevel & SettingsFlags.ExhaustColor) == 0)
            {
                ExhaustSteadyColor = loco.ExhaustSteadyColor;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("ExhaustColour not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", ExhaustSteadyColor);
            }
            ExhaustDecelColor = loco.ExhaustDecelColor;

            if ((initLevel & SettingsFlags.ExhaustTransientColor) == 0)
            {

                ExhaustTransientColor = loco.ExhaustTransientColor;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("ExhaustTransientColour not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", ExhaustTransientColor);
            }

            if ((initLevel & SettingsFlags.StartingRPM) == 0)
            {
                StartingRPM = loco.IdleRPM * 2.0f / 3.0f;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("StartingRpM not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", StartingRPM);
            }

            if ((initLevel & SettingsFlags.StartingConfirmRPM) == 0)
            {
                StartingConfirmationRPM = loco.IdleRPM * 1.1f;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("StartingConfirmRpM not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", StartingConfirmationRPM);
            }

            if ((initLevel & SettingsFlags.ChangeUpRPMpS) == 0)
            {
                if (DieselEngineConfigured && loco.MaxRPMChangeRate != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    ChangeUpRPMpS = loco.MaxRPMChangeRate;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("ChangeUpRPMpS not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", ChangeUpRPMpS);
                }
                else if (ChangeUpRPMpS == 0 && loco.MaxRPMChangeRate != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    ChangeUpRPMpS = loco.MaxRPMChangeRate;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("ChangeUpRPMpS: set at default value (BASIC Config) = {0}", ChangeUpRPMpS);

                }
                else if (loco.MaxRPMChangeRate == 0) // No default "MSTS" value present, set to arbitary value
                {
                    ChangeUpRPMpS = 40.0f;
                    loco.MaxRPMChangeRate = ChangeUpRPMpS;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("ChangeUpRPMpS not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", ChangeUpRPMpS);

                }                
            }

            if ((initLevel & SettingsFlags.ChangeDownRPMpS) == 0)
            {
                if (DieselEngineConfigured && loco.MaxRPMChangeRate != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    ChangeDownRPMpS = loco.MaxRPMChangeRate;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("ChangeDownRPMpS not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", ChangeDownRPMpS);

                }
                else if (ChangeDownRPMpS == 0 && loco.MaxRPMChangeRate != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    ChangeDownRPMpS = loco.MaxRPMChangeRate;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("ChangeDownRPMpS: set at default value (BASIC Config) = {0}", ChangeDownRPMpS);

                }
                else if (loco.MaxRPMChangeRate == 0) // No default "MSTS" value present, set to arbitary value
                {
                    ChangeDownRPMpS = 40.0f;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("ChangeDownRPMpS not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", ChangeDownRPMpS);

                }
            }

            if ((initLevel & SettingsFlags.RateOfChangeUpRPMpSS) == 0)
            {
                RateOfChangeUpRPMpSS = ChangeUpRPMpS;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("RateofChangeUpRpMpS not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", RateOfChangeUpRPMpSS);
            }

            if ((initLevel & SettingsFlags.RateOfChangeDownRPMpSS) == 0)
            {
                RateOfChangeDownRPMpSS = ChangeDownRPMpS;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("RateofChangeDownRpMpS not found in Diesel Engine Prime Mover Configuration, set at default value = {0}", RateOfChangeDownRPMpSS);
            }

            if ((initLevel & SettingsFlags.MaximalDieselPowerW) == 0)
            {
                if (loco.MaximumDieselEnginePowerW != 0)
                {
                    MaximumDieselPowerW = loco.MaximumDieselEnginePowerW;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaximalPower not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value (ORTSDieselEngineMaxPower) = {0}", FormatStrings.FormatPower(MaximumDieselPowerW, loco.IsMetric, false, false));

                }
                else if (loco.MaxPowerW == 0)
                {
                    MaximumDieselPowerW = 2500000;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaximalPower not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set at arbitary value = {0}", FormatStrings.FormatPower(MaximumDieselPowerW, loco.IsMetric, false, false));

                }
                else
                {
                    MaximumDieselPowerW = loco.MaxPowerW;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaximalPower not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value (MaxPower) = {0}", FormatStrings.FormatPower(MaximumDieselPowerW, loco.IsMetric, false, false));

                }

            }

            if ((initLevel & SettingsFlags.MaxOilPressure) == 0)
            {

                if (DieselEngineConfigured && loco.DieselMaxOilPressurePSI != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    DieselMaxOilPressurePSI = loco.DieselMaxOilPressurePSI;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxOilPressure not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", DieselMaxOilPressurePSI);

                }
                else if (DieselMaxOilPressurePSI == 0 && loco.DieselMaxOilPressurePSI != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    DieselMaxOilPressurePSI = loco.DieselMaxOilPressurePSI;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxOilPressure: set at default value (BASIC Config) = {0}", DieselMaxOilPressurePSI);

                }
                else if (loco.DieselMaxOilPressurePSI == 0) // No default "MSTS" value present, set to arbitary value
                {
                    DieselMaxOilPressurePSI = 120.0f;
                    loco.DieselMaxOilPressurePSI = DieselMaxOilPressurePSI;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxOilPressure not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", DieselMaxOilPressurePSI);

                }

            }

            if ((initLevel & SettingsFlags.MinOilPressure) == 0)
            {
                if (DieselEngineConfigured && loco.DieselMinOilPressurePSI != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    DieselMinOilPressurePSI = loco.DieselMinOilPressurePSI;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MinOilPressure not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", DieselMinOilPressurePSI);

                }
                else if (DieselMinOilPressurePSI == 0 && loco.DieselMinOilPressurePSI != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    DieselMinOilPressurePSI = loco.DieselMinOilPressurePSI;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MinOilPressure: set at default value (BASIC Config) = {0}", DieselMinOilPressurePSI);

                }
                else if (loco.DieselMinOilPressurePSI == 0) // No default "MSTS" value present, set to arbitary value
                {
                    DieselMinOilPressurePSI = 40.0f;
                    loco.DieselMinOilPressurePSI = DieselMinOilPressurePSI;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MinOilPressure not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", DieselMinOilPressurePSI);

                }
            }

            if ((initLevel & SettingsFlags.MaxTemperature) == 0)
            {
                if (DieselEngineConfigured && loco.DieselMaxTemperatureDeg != 0) // Advanced conf - Prime mover Eng block defined but no IdleRPM present
                {
                    DieselMaxTemperatureDeg = loco.DieselMaxTemperatureDeg;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxTemperature not found in Diesel Engine Prime Mover Configuration (ADVANCED Config): set to default value = {0}", DieselMaxTemperatureDeg);

                }
                else if (DieselMaxTemperatureDeg == 0 && loco.DieselMaxTemperatureDeg != 0)  // Basic conf - No prime mover ENG block defined, use the default "MSTS" value
                {
                    DieselMaxTemperatureDeg = loco.DieselMaxTemperatureDeg;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxTemperature: set at default value (BASIC Config) = {0}", DieselMaxTemperatureDeg);

                }
                else if (loco.DieselMaxTemperatureDeg == 0) // No default "MSTS" value present, set to arbitary value
                {
                    DieselMaxTemperatureDeg = 100.0f;
                    loco.DieselMaxTemperatureDeg = DieselMaxTemperatureDeg;

                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("MaxTemperature not found in Diesel Engine Configuration (BASIC Config): set at arbitary value = {0}", DieselMaxTemperatureDeg);

                }

            }

            if ((initLevel & SettingsFlags.Cooling) == 0)
            {
                EngineCooling = loco.DieselEngineCooling;
            }

            // Advise user what cooling method is set
            if (loco.Simulator.Settings.VerboseConfigurationMessages)
                Trace.TraceInformation("ORTSDieselCooling, set at default value = {0}", EngineCooling);


            if ((initLevel & SettingsFlags.TempTimeConstant) == 0)
            {
                DieselWaterTempTimeConstantSec = 720f;
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("TempTimeConstant not found in Diesel Engine Config, set at arbitary value = {0}", DieselWaterTempTimeConstantSec);
            }

            if ((initLevel & SettingsFlags.DieselConsumptionTab) == 0)
            {
                DieselConsumptionTab = new Interpolator(new float[] { IdleRPM, MaxRPM }, new float[] { loco.DieselUsedPerHourAtIdleL, loco.DieselUsedPerHourAtMaxPowerL });
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("DieselConsumptionTab not found in Diesel Engine Config, set at default values");
            }

            if ((initLevel & SettingsFlags.ThrottleRPMTab) == 0)
            {
                ThrottleRPMTab = new Interpolator(new float[] { 0, 100 }, new float[] { IdleRPM, MaxRPM });
                if (DieselEngineConfigured && loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("ThrottleRpMTab not found in Diesel Engine Config, set at default values");
            }

            // If diesel power output curves not defined then set to "standard defaults" in ENG file
            // Set defaults for Torque and Power tables if both are not set.
            if (((initLevel & SettingsFlags.DieselTorqueTab) == 0) && ((initLevel & SettingsFlags.DieselPowerTab) == 0))
            {
                int count = 11;
                float[] rpm = new float[count + 1];
                float[] power = new float[] { 0.02034f, 0.09302f, 0.36628f, 0.60756f, 0.69767f, 0.81395f, 0.93023f, 0.9686f, 0.99418f, 0.99418f, 1f, 0.5f };
                float[] torque = new float[] { 0.05f, 0.2f, 0.7f, 0.95f, 1f, 1f, 0.98f, 0.95f, 0.9f, 0.86f, 0.81f, 0.3f };

                for (int i = 0; i < count; i++)
                {
                    if (i == 0)
                        rpm[i] = IdleRPM;
                    else
                        rpm[i] = rpm[i - 1] + (MaxRPM - IdleRPM) / (count - 1);
                    power[i] *= MaximumDieselPowerW;
                    torque[i] *= MaximumDieselPowerW / (MaxRPM * 2f * 3.1415f / 60f) / 0.81f;
                }
                rpm[count] = MaxRPM * 1.5f;
                DieselPowerTab = new Interpolator(rpm, power);
                DieselTorqueTab = new Interpolator(rpm, torque);
                if (DieselEngineConfigured)
                {
                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                        Trace.TraceInformation("DieselTorqueTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                    }
                }
                else
                {
                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab constructed from default values (BASIC Config)");
                        Trace.TraceInformation("DieselTorqueTab constructed from default values (BASIC Config)");
                    }
                }
            }

            // Set defaults for Torque table if it is not set.
            if (((initLevel & SettingsFlags.DieselTorqueTab) == 0) && ((initLevel & SettingsFlags.DieselPowerTab) == SettingsFlags.DieselPowerTab))
            {
                float[] rpm = new float[DieselPowerTab.GetSize()];
                float[] torque = new float[DieselPowerTab.GetSize()];
                for (int i = 0; i < DieselPowerTab.GetSize(); i++)
                {
                    rpm[i] = IdleRPM + (float)i * (MaxRPM - IdleRPM) / (float)DieselPowerTab.GetSize();
                    torque[i] = DieselPowerTab[rpm[i]] / (rpm[i] * 2f * 3.1415f / 60f);
                }
                if (DieselEngineConfigured)
                {
                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselTorqueTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                    }
                }
                else
                {
                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselTorqueTab constructed from default values (BASIC Config)");
                    }
                }
            }

            // Set defaults for Power table if it is not set.
            if (((initLevel & SettingsFlags.DieselTorqueTab) == SettingsFlags.DieselTorqueTab) && ((initLevel & SettingsFlags.DieselPowerTab) == 0))
            {
                float[] rpm = new float[DieselPowerTab.GetSize()];
                float[] power = new float[DieselPowerTab.GetSize()];
                for (int i = 0; i < DieselPowerTab.GetSize(); i++)
                {
                    rpm[i] = IdleRPM + (float)i * (MaxRPM - IdleRPM) / (float)DieselPowerTab.GetSize();
                    power[i] = DieselPowerTab[rpm[i]] * rpm[i] * 2f * 3.1415f / 60f;
                }
                if (DieselEngineConfigured)
                {
                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab not found in Diesel Engine Config (ADVANCED Config): constructed from default values");
                    }
                }
                else
                {
                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                    {
                        Trace.TraceInformation("DieselPowerTab constructed from default values (BASIC Config)");
                    }
                }
            }

            if (loco.MaximumDieselEnginePowerW == 0 && DieselPowerTab != null)
            {
                loco.MaximumDieselEnginePowerW = DieselPowerTab[MaxRPM];
                if (loco.Simulator.Settings.VerboseConfigurationMessages)
                    Trace.TraceInformation("Maximum Diesel Engine Prime Mover Power set by DieselPowerTab {0} value", FormatStrings.FormatPower(DieselPowerTab[MaxRPM], loco.IsMetric, false, false));
            }

            // Check whether this code check is really required.
            if (MaximumRailOutputPowerW == 0 && loco.MaxPowerW != 0)
            {
                MaximumRailOutputPowerW = loco.MaxPowerW; // set rail power to a default value on the basis that of the value specified in the MaxPowrW parameter
            }
            else
            {
                MaximumRailOutputPowerW = 0.8f * MaximumDieselPowerW; // set rail power to a default value on the basis that it is about 80% of the prime mover output power
            }

            InitialExhaust = loco.InitialExhaust;
            MaxExhaust = loco.MaxExhaust;
            locomotive = loco;
        }

        public void InitDieselRailPowers(MSTSDieselLocomotive loco)
        {

            // Set up the reverse ThrottleRPM table. This is used to provide an apparent throttle setting to the Tractive Force calculation, and allows the diesel engine to control the up/down time of 
            // tractive force. This table should be creeated with all locomotives, as they will either use (create) a default ThrottleRPM table, or the user will enter one. 

            if (ThrottleRPMTab != null)
            {
                var size = ThrottleRPMTab.GetSize();
                float[] rpm = new float[size];
                float[] throttle = new float[size];

                throttle[0] = 0; // Set throttle value to 0
                rpm[0] = ThrottleRPMTab[throttle[0]]; // Find rpm of this throttle value in ThrottleRPMTab 

                for (int i = 1; i < size; i++)
                {
                    throttle[i] = ThrottleRPMTab.X[i]; // read x co-ord
                    rpm[i] = ThrottleRPMTab.Y[i]; // read y co-ord value of this throttle value in ThrottleRPMTab 
                }
                ReverseThrottleRPMTab = new Interpolator(rpm, throttle); // create reverse table
            }

            // TODO - this value needs to be divided by the number of diesel engines in the locomotive

            // Set MaximumRailOutputPower if not already set
            if (MaximumRailOutputPowerW == 0)
            {
                if (loco.TractiveForceCurves != null)
                {
                    float ThrottleSetting = 1;
                    MaximumRailOutputPowerW = loco.TractiveForceCurves.Get(ThrottleSetting, loco.SpeedOfMaxContinuousForceMpS) * loco.SpeedOfMaxContinuousForceMpS;
                    if (loco.Simulator.Settings.VerboseConfigurationMessages)
                        Trace.TraceInformation("Maximum Rail Output Power set by Diesel Traction Curves {0} value", FormatStrings.FormatPower(MaximumRailOutputPowerW, loco.IsMetric, false, false));
                }
                else if (loco.MaxPowerW != 0)
                {
                    MaximumRailOutputPowerW = loco.MaxPowerW; // set rail power to a default value on the basis that of the value specified in the MaxPowerW parameter
                }
                else
                {
                    MaximumRailOutputPowerW = 0.8f * MaximumDieselPowerW; // set rail power to a default value on the basis that it is about 80% of the prime mover output power
                }
            }

            // Check MaxRpM for loco as it is needed as well
            if (loco.MaxRPM == 0)
            {
                if (MaxRPM != 0)
                {
                    loco.MaxRPM = MaxRPM;
                }
                else
                {
                    loco.MaxRPM = 600.0f;
                }


            }
        }

    }
}
