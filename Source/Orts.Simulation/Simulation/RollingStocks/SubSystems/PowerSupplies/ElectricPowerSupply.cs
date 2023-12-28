﻿// COPYRIGHT 2013, 2014, 2015 by the Open Rails project.
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
using ORTS.Scripting.Api;
using System;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedElectricPowerSupply : AbstractPowerSupply
    {
        private readonly MSTSElectricLocomotive Locomotive;
        private readonly Simulator Simulator;
        private readonly Pantographs Pantographs;
        public ScriptedCircuitBreaker CircuitBreaker;

        public bool Activated = false;
        private string ScriptName = "Default";
        private ElectricPowerSupply Script;

        public bool RouteElectrified
        {
            get
            {
                return Simulator.TRK.Tr_RouteFile.Electrified || Simulator.Settings.OverrideNonElectrifiedRoutes;
            }
        }

        public float LineVoltageV
        {
            get
            {
                // TODO change to actual voltage calculated by power station location
                return (float)Simulator.TRK.Tr_RouteFile.MaxLineVoltage;
            }
        }
        public float PantographVoltageV { get; set; }
        public float FilterVoltageV { get; set; }

        public ScriptedElectricPowerSupply(MSTSElectricLocomotive locomotive) :
            base(locomotive)
        {
            Locomotive = locomotive;
            Simulator = locomotive.Simulator;
            Pantographs = locomotive.Pantographs;

            State = PowerSupplyState.PowerOff;
            AuxiliaryState = PowerSupplyState.PowerOff;
            FilterVoltageV = 0;

            CircuitBreaker = new ScriptedCircuitBreaker(Locomotive);
        }

        public ScriptedElectricPowerSupply(MSTSLocomotive locomotive) : base(locomotive)
        {
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortspowersupply":
                    ScriptName = stf.ReadStringBlock(null);
                    break;

                case "engine(ortscircuitbreaker":
                case "engine(ortscircuitbreakerclosingdelay":
                    CircuitBreaker.Parse(lowercasetoken, stf);
                    break;

                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        public void Copy(ScriptedElectricPowerSupply other)
        {
            ScriptName = other.ScriptName;

            base.Copy(other);
            CircuitBreaker.Copy(other.CircuitBreaker);
        }

        public override void Restore(BinaryReader inf)
        {
            ScriptName = inf.ReadString();

            base.Restore(inf);
            CircuitBreaker.Restore(inf);
        }

        public void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "Default")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as ElectricPowerSupply;
                }
                if (Script == null)
                {
                    Script = new DefaultElectricPowerSupply(Locomotive) as ElectricPowerSupply;
                }

                // AbstractScriptClass
                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.GameTime = () => (float)Simulator.GameTime;
                Script.DistanceM = () => Locomotive.DistanceM;
                Script.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
                Script.Confirm = Locomotive.Simulator.Confirmer.Confirm;
                Script.Message = Locomotive.Simulator.Confirmer.Message;
                Script.SignalEvent = Locomotive.SignalEvent;
                Script.SignalEventToTrain = (evt) =>
                {
                    if (Locomotive.Train != null)
                    {
                        Locomotive.Train.SignalEvent(evt);
                    }
                };

                // ElectricPowerSupply getters
                Script.CurrentState = () => State;
                Script.CurrentAuxiliaryState = () => AuxiliaryState;
                Script.CurrentPantographState = () => Pantographs.State;
                Script.CurrentCircuitBreakerState = () => CircuitBreaker.State;
                Script.PantographVoltageV = () => PantographVoltageV;
                Script.FilterVoltageV = () => FilterVoltageV;
                Script.LineVoltageV = () => LineVoltageV;
                Script.PowerOnDelayS = () => PowerOnDelayS;
                Script.AuxPowerOnDelayS = () => AuxPowerOnDelayS;

                // ElectricPowerSupply setters
                Script.SetCurrentState = (value) => State = value;
                Script.SetCurrentAuxiliaryState = (value) => AuxiliaryState = value;
                Script.SetPantographVoltageV = (value) => PantographVoltageV = value;
                Script.SetFilterVoltageV = (value) => FilterVoltageV = value;

                Script.Initialize();
                Activated = true;
            }

            Pantographs.Initialize();
            CircuitBreaker.Initialize();
        }


        //================================================================================================//
        /// <summary>
        /// Initialization when simulation starts with moving train
        /// <\summary>
        public override void InitializeMoving()
        {
            base.InitializeMoving();
            CircuitBreaker.InitializeMoving();
        }

        public void Update(float elapsedClockSeconds)
        {
            CircuitBreaker.Update(elapsedClockSeconds);

            if (Script != null)
                Script.Update(elapsedClockSeconds);
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            CircuitBreaker.HandleEvent(evt);
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(ScriptName);

            base.Save(outf);
            CircuitBreaker.Save(outf);
        }
    }

    public class DefaultElectricPowerSupply : ElectricPowerSupply
    {
        private IIRFilter PantographFilter;
        private IIRFilter VoltageFilter;
        private Timer PowerOnTimer;
        private Timer AuxPowerOnTimer;

        // Icik
        private readonly MSTSElectricLocomotive Locomotive;
        public DefaultElectricPowerSupply(MSTSElectricLocomotive locomotive) :
            base(locomotive)
        {
            Locomotive = locomotive;
        }
        float TDC = 0;

        public override void Initialize()
        {
            PantographFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);
            VoltageFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, IIRFilter.HzToRad(0.7f), 0.001f);

            PowerOnTimer = new Timer(this);
            PowerOnTimer.Setup(PowerOnDelayS());

            AuxPowerOnTimer = new Timer(this);
            AuxPowerOnTimer.Setup(AuxPowerOnDelayS());
        }

        public override void Update(float elapsedClockSeconds)
        {
            if (elapsedClockSeconds == 0 || Locomotive.Simulator.GameSpeed > 1)
                return;

            if (Locomotive.SwitchingVoltageMode == 1 && Locomotive.LocoSwitchACDC)
                TDC = 0;

            switch (CurrentPantographState())
            {
                case PantographState.Down:
                case PantographState.Lowering:
                case PantographState.Raising:
                    if (PowerOnTimer.Started)
                        PowerOnTimer.Stop();
                    if (AuxPowerOnTimer.Started)
                        AuxPowerOnTimer.Stop();

                    SetCurrentState(PowerSupplyState.PowerOff);
                    SetCurrentAuxiliaryState(PowerSupplyState.PowerOff);
                    SetPantographVoltageV(PantographFilter.Filter(0.0f, elapsedClockSeconds));
                    SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));

                    // Napětí po složení pantografu na filtru DC 3kV
                    if (Locomotive.preVoltageDC > 1)
                        Locomotive.preVoltageDC -= 100;
                    TDC = 0;

                    break;

                case PantographState.Up:

                    // Icik
                    if (Locomotive.IsPlayerTrain && Locomotive.RouteVoltageV != 600)
                    {                        
                        // Trakce na 15kV nebo 25kV naměří napětí hned                                           
                        if (Locomotive.RouteVoltageV > 3000)
                            SetPantographVoltageV(PantographFilter.Filter((int)Locomotive.ActualLocoVoltage, elapsedClockSeconds));

                        // Trakce na 3kV po přepnutí na DC naběhne napětí, jednosystémová lokomotiva naměří napětí hned
                        if (Locomotive.RouteVoltageV == 3000 && (Locomotive.SwitchingVoltageMode_OffDC || !Locomotive.MultiSystemEngine))
                        {
                            TDC += elapsedClockSeconds;
                            if (TDC > 0.5f)
                                SetPantographVoltageV(PantographFilter.Filter((int)Locomotive.ActualLocoVoltage, elapsedClockSeconds));
                        }

                        // Napětí po zdvihu pantografu na filtru DC 3kV
                        if (Locomotive.RouteVoltageV == 3000 && Locomotive.preVoltageDC < Locomotive.RouteVoltageV)
                            Locomotive.preVoltageDC += 100;
                        else
                        if (Locomotive.preVoltageDC > 1)
                            Locomotive.preVoltageDC -= 100;

                        // Trakce na 3kV spadne napětí po odpadu HV
                        if (!Locomotive.MultiSystemEngine && Locomotive.RouteVoltageV == 3000 && Locomotive.PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open && Locomotive.PantographVoltageV == 1
                            || Locomotive.MultiSystemEngine && Locomotive.RouteVoltageV == 3000 && !Locomotive.SwitchingVoltageMode_OffDC)
                        {
                            SetCurrentState(PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryState(PowerSupplyState.PowerOff);
                            SetPantographVoltageV(PantographFilter.Filter(0.0f, elapsedClockSeconds));
                            SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                        }
                    }

                    // Trakce na 600V naměří napětí hned                                           
                    if (Locomotive.IsPlayerTrain && Locomotive.RouteVoltageV == 600)
                    {
                        SetPantographVoltageV(PantographFilter.Filter((int)Locomotive.ActualLocoVoltage, elapsedClockSeconds));
                    }

                    switch (CurrentCircuitBreakerState())
                    {
                        case CircuitBreakerState.Open:
                            if (PowerOnTimer.Started)
                                PowerOnTimer.Stop();
                            if (AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Stop();

                            SetCurrentState(PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryState(PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(0.0f, elapsedClockSeconds));
                            break;

                        case CircuitBreakerState.Closed:
                            if (!PowerOnTimer.Started)
                                PowerOnTimer.Start();
                            if (!AuxPowerOnTimer.Started)
                                AuxPowerOnTimer.Start();

                            SetCurrentState(PowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetCurrentAuxiliaryState(AuxPowerOnTimer.Triggered ? PowerSupplyState.PowerOn : PowerSupplyState.PowerOff);
                            SetFilterVoltageV(VoltageFilter.Filter(PantographVoltageV(), elapsedClockSeconds));
                            break;
                    }
                    break;
            }
        }
    }
}
