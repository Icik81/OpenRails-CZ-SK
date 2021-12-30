﻿// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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

using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using ORTS.Scripting.Api;
using System;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{

    public class ScriptedCircuitBreaker
    {
        public readonly MSTSElectricLocomotive Locomotive;
        readonly Simulator Simulator;

        public bool Activated = false;
        
        // Icik
        //string ScriptName = "Automatic";
        string ScriptName = "Manual";

        CircuitBreaker Script;

        private float DelayS = 0.5f;

        public CircuitBreakerState State { get; private set; }
        public bool DriverClosingOrder { get; private set; }
        public bool DriverOpeningOrder { get; private set; }
        public bool DriverClosingAuthorization { get; private set; }
        
        public bool TCSClosingOrder
        {
            get
            {
                MSTSLocomotive locomotive = Locomotive.Train.LeadLocomotive as MSTSLocomotive;
                if (locomotive != null)
                    return locomotive.TrainControlSystem.CircuitBreakerClosingOrder;
                else
                    return false;
            }
        }
        public bool TCSOpeningOrder
        {
            get
            {
                MSTSLocomotive locomotive = Locomotive.Train.LeadLocomotive as MSTSLocomotive;
                if (locomotive != null)
                    return locomotive.TrainControlSystem.CircuitBreakerOpeningOrder;
                else
                    return false;
            }
        }
        public bool TCSClosingAuthorization
        {
            get
            {
                MSTSLocomotive locomotive = Locomotive.Train.LeadLocomotive as MSTSLocomotive;
                if (locomotive != null)
                    return locomotive.TrainControlSystem.PowerAuthorization;
                else
                    return false;
            }
        }
        public bool ClosingAuthorization { get;  set; }

        public ScriptedCircuitBreaker(MSTSElectricLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;
        }

        public void Copy(ScriptedCircuitBreaker other)
        {
            ScriptName = other.ScriptName;
            State = other.State;
            DelayS = other.DelayS;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortscircuitbreaker":
                    if (Locomotive.Train as AITrain == null)
                    {
                        ScriptName = stf.ReadStringBlock("Automatic");
                    }
                    break;

                case "engine(ortscircuitbreakerclosingdelay":
                    DelayS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0.5f);
                    break;
            }
        }

        public void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null)
                {
                    switch(ScriptName)
                    {
                        case "Automatic":
                            Script = new AutomaticCircuitBreaker() as CircuitBreaker;
                            break;

                        case "Manual":
                            Script = new ManualCircuitBreaker() as CircuitBreaker;
                            break;

                        default:
                            var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                            Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as CircuitBreaker;
                            break;
                    }
                }
                // Fallback to automatic circuit breaker if the above failed.
                if (Script == null)
                {
                    Script = new AutomaticCircuitBreaker() as CircuitBreaker;
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

                // CircuitBreaker getters
                Script.CurrentState = () => State;
                Script.CurrentPantographState = () => Locomotive.Pantographs.State;
                Script.CurrentPowerSupplyState = () => Locomotive.PowerSupply.State;
                Script.DriverClosingOrder = () => DriverClosingOrder;
                Script.DriverOpeningOrder = () => DriverOpeningOrder;
                Script.DriverClosingAuthorization = () => DriverClosingAuthorization;
                Script.TCSClosingOrder = () => TCSClosingOrder;
                Script.TCSOpeningOrder = () => TCSOpeningOrder;
                Script.TCSClosingAuthorization = () => TCSClosingAuthorization;
                Script.ClosingAuthorization = () => ClosingAuthorization;
                Script.ClosingDelayS = () => DelayS;

                // CircuitBreaker setters
                Script.SetCurrentState = (value) =>
                {
                    State = value;
                    TCSEvent CircuitBreakerEvent = State == CircuitBreakerState.Closed ? TCSEvent.CircuitBreakerClosed : TCSEvent.CircuitBreakerOpen;
                    Locomotive.TrainControlSystem.HandleEvent(CircuitBreakerEvent);
                };
                Script.SetDriverClosingOrder = (value) => DriverClosingOrder = value;
                Script.SetDriverOpeningOrder = (value) => DriverOpeningOrder = value;
                Script.SetDriverClosingAuthorization = (value) => DriverClosingAuthorization = value;
                Script.SetClosingAuthorization = (value) => ClosingAuthorization = value;

                Script.Initialize();
                Activated = true;
            }
        }

        public void InitializeMoving()
        {
            State = CircuitBreakerState.Closed;
        }

        public void Update(float elapsedSeconds)
        {
            // Icik
            if (Locomotive.IsPlayerTrain && Locomotive.LocoSwitchACDC && Locomotive.SwitchingVoltageMode == 1 && State == CircuitBreakerState.Open)
                HandleEvent(PowerSupplyEvent.OpenCircuitBreaker);

            if (Locomotive.Train.TrainType == Train.TRAINTYPE.AI || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_AUTOGENERATE
                || Locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
            {
                State = CircuitBreakerState.Closed;
            }
            else
            {
                if (Script != null)
                {
                    Script.Update(elapsedSeconds, Locomotive.Battery);
                }
            }
            if (!Locomotive.IsPlayerTrain)
                Locomotive.Battery = true;
            if (!Locomotive.Battery && State != CircuitBreakerState.Open)
            {
                HandleEvent(PowerSupplyEvent.OpenCircuitBreaker);
            }
        }

        public void HandleEvent(PowerSupplyEvent evt)
        {
            if (Script != null)
            {
                Script.HandleEvent(evt);
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(ScriptName);
            outf.Write(DelayS);
            outf.Write(State.ToString());
            outf.Write(DriverClosingOrder);
            outf.Write(DriverOpeningOrder);
            outf.Write(DriverClosingAuthorization);
            outf.Write(ClosingAuthorization);
        }

        public void Restore(BinaryReader inf)
        {
            ScriptName = inf.ReadString();
            DelayS = inf.ReadSingle();
            State = (CircuitBreakerState)Enum.Parse(typeof(CircuitBreakerState), inf.ReadString());
            DriverClosingOrder = inf.ReadBoolean();
            DriverOpeningOrder = inf.ReadBoolean();
            DriverClosingAuthorization = inf.ReadBoolean();
            ClosingAuthorization = inf.ReadBoolean();
        }
    }

    class AutomaticCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingOrder(false);
            SetDriverOpeningOrder(false);
            SetDriverClosingAuthorization(true);
        }

        public override void Update(float elapsedSeconds, bool battery)
        {
            SetClosingAuthorization(TCSClosingAuthorization() && CurrentPantographState() == PantographState.Up);

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization())
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState() && battery)
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        SignalEvent(Event.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(Event.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
                        SignalEvent(Event.CircuitBreakerClosed);
                        break;
                }
            }

            PreviousState = CurrentState();
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            // Nothing to do since it is automatic
        }
    }

    public class ManualCircuitBreaker : CircuitBreaker
    {
        private Timer ClosingTimer;
        private CircuitBreakerState PreviousState;

        public override void Initialize()
        {
            ClosingTimer = new Timer(this);
            ClosingTimer.Setup(ClosingDelayS());

            SetDriverClosingAuthorization(true);
        }

        public override void Update(float elapsedSeconds, bool battery)
        {
            //SetClosingAuthorization(TCSClosingAuthorization() && CurrentPantographState() == PantographState.Up);
            // Icik
            SetClosingAuthorization(TCSClosingAuthorization());

            switch (CurrentState())
            {
                case CircuitBreakerState.Closed:
                    if (!ClosingAuthorization() || DriverOpeningOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Closing:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        if (!ClosingTimer.Started)
                        {
                            ClosingTimer.Start();
                        }

                        if (ClosingTimer.Triggered)
                        {
                            ClosingTimer.Stop();
                            SetCurrentState(CircuitBreakerState.Closed);
                        }
                    }
                    else
                    {
                        ClosingTimer.Stop();
                        SetCurrentState(CircuitBreakerState.Open);
                    }
                    break;

                case CircuitBreakerState.Open:
                    if (ClosingAuthorization() && DriverClosingOrder())
                    {
                        SetCurrentState(CircuitBreakerState.Closing);
                    }
                    break;
            }

            if (PreviousState != CurrentState())
            {
                switch (CurrentState())
                {
                    case CircuitBreakerState.Open:
                        if (battery) SignalEvent(Event.CircuitBreakerOpen);
                        break;

                    case CircuitBreakerState.Closing:
                        SignalEvent(Event.CircuitBreakerClosing);
                        break;

                    case CircuitBreakerState.Closed:
                        SignalEvent(Event.CircuitBreakerClosed);
                        break;
                }
            }

            PreviousState = CurrentState();
        }

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            switch (evt)
            {
                case PowerSupplyEvent.CloseCircuitBreaker:
                    SetDriverClosingOrder(true);
                    SetDriverOpeningOrder(false);
                    SignalEvent(Event.CircuitBreakerClosingOrderOn);

                    Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.On);
                    if (!ClosingAuthorization())
                    {
                        Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("Circuit breaker closing not authorized"));
                    }
                    break;

                case PowerSupplyEvent.OpenCircuitBreaker:
                    SetDriverClosingOrder(false);
                    SetDriverOpeningOrder(true);
                    SignalEvent(Event.CircuitBreakerClosingOrderOff);

                    //Confirm(CabControl.CircuitBreakerClosingOrder, CabSetting.Off);
                    break;
            }
        }
    }
}
