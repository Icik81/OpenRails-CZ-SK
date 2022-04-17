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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    public class MultiPositionController
    {
        public MultiPositionController(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = Locomotive.Simulator;
            controllerPosition = ControllerPosition.Neutral;
        }
        MSTSLocomotive Locomotive;
        Simulator Simulator;

        public List<Position> PositionsList = new List<Position>();

        public bool Equipped = false;
        public bool StateChanged = false;

        public ControllerPosition controllerPosition = new ControllerPosition();
        public ControllerBinding controllerBinding = new ControllerBinding();
        protected float elapsedSecondsFromLastChange = 0;
        protected bool checkNeutral = false;
        protected bool noKeyPressed = true;
        protected string currentPosition = "";
        protected bool emergencyBrake = false;
        protected bool previousDriveModeWasAddPower = false;
        protected bool isBraking = false;
        protected bool needPowerUpAfterBrake = false;
        public bool CanControlTrainBrake = false;
        protected bool initialized = false;
        protected bool movedForward = false;
        protected bool movedAft = false;
        protected bool haveCruiseControl = false;
        public int ControllerId = 0;
        public bool MouseInputActive = false;

        public void Save(BinaryWriter outf)
        {
            outf.Write(this.checkNeutral);
            outf.Write((int)this.controllerPosition);
            outf.Write(this.currentPosition);
            outf.Write(this.elapsedSecondsFromLastChange);
            outf.Write(this.emergencyBrake);
            outf.Write(this.Equipped);
            outf.Write(this.isBraking);
            outf.Write(this.noKeyPressed);
            outf.Write(this.previousDriveModeWasAddPower);
            outf.Write(this.StateChanged);
            outf.Write(haveCruiseControl);
        }

        public void Restore(BinaryReader inf)
        {
            initialized = true;
            checkNeutral = inf.ReadBoolean();
            int fControllerPosition = inf.ReadInt32();
            controllerPosition = (ControllerPosition)fControllerPosition;
            currentPosition = inf.ReadString();
            elapsedSecondsFromLastChange = inf.ReadSingle();
            emergencyBrake = inf.ReadBoolean();
            Equipped = inf.ReadBoolean();
            isBraking = inf.ReadBoolean();
            noKeyPressed = inf.ReadBoolean();
            previousDriveModeWasAddPower = inf.ReadBoolean();
            StateChanged = inf.ReadBoolean();
            haveCruiseControl = inf.ReadBoolean();
        }
        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(ortsmultipositioncontroller(positions":
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                    {
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("position", ()=>{
                            stf.MustMatch("(");
                            string positionType = stf.ReadString();
                            string positionFlag = stf.ReadString();
                            string positionName = stf.ReadString();
                            PositionsList.Add(new Position(positionType, positionFlag, positionName));
                        }),
                    });
                    }
                    break;
                case "engine(ortsmultipositioncontroller(controllerbinding":
                    String binding = stf.ReadStringBlock("null").ToLower();
                    switch (binding)
                    {
                        case "throttle":
                            controllerBinding = ControllerBinding.Throttle;
                            break;
                        case "selectedspeed":
                            controllerBinding = ControllerBinding.SelectedSpeed;
                            break;
                        case "dynamicbrake":
                            controllerBinding = ControllerBinding.DynamicBrake;
                            break;
                        case "trainbrake":
                            controllerBinding = ControllerBinding.TrainBrake;
                            break;
                    }
                    break;
                case "engine(ortsmultipositioncontroller(controllerid": ControllerId = stf.ReadIntBlock(0); break;
                case "engine(ortsmultipositioncontrollercancontroltrainbrake": CanControlTrainBrake = stf.ReadBoolBlock(false); break;
            }
        }

        protected bool dynBrakeEngaged = false;
        protected bool dynBrakeSetup = false;

        public void Update(float elapsedClockSeconds)
        {
            if (!initialized)
            {
                if (Locomotive.CruiseControl != null)
                    haveCruiseControl = true;
                foreach (Position pair in PositionsList)
                {
                    if (pair.Flag.ToLower() == "default")
                    {
                        currentPosition = pair.Type;
                        break;
                    }
                }
                initialized = true;
            }
            if (!Locomotive.IsPlayerTrain) return;

            if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseWithPriority)
            {
                dynBrakeEngaged = true;
                bool skipBraking = false; 
                if (haveCruiseControl)
                {
                    if (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Manual)
                    {
                        if (Locomotive.ThrottlePercent > 0)
                        {
                            Random rnd = new Random();
                            int test = rnd.Next(0, 2);
                            if (test == 0)
                                Locomotive.HVOff = true;
                            float pct = Locomotive.ThrottlePercent - 0.3f;
                            if (pct < 0)
                                pct = 0;
                            Locomotive.ThrottlePercent = Locomotive.ControllerVolts = Locomotive.CruiseControl.controllerVolts = 0;
                            skipBraking = true;
                        }
                    }
                    Locomotive.CruiseControl.DynamicBrakePriority = true;
                    if (Locomotive.TractiveForceN > 0)
                        skipBraking = true;
                }
                if (!skipBraking)
                {
                    if (Locomotive.CanUseDynamicBrake())
                    {
                        if (Locomotive.DynamicBrakePercent < 0)
                        {
                            Locomotive.DynamicBrakeChangeActiveState(true);
                        }
                        else if (Locomotive.DynamicBrake)
                        {
                            Locomotive.SignalEvent(Common.Event.DynamicBrakeChange);

                            float step = 100 / Locomotive.DynamicBrakeFullRangeIncreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + step);
                        }
                    }
                }
            }
            else if (haveCruiseControl)
            {
                if (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Manual && dynBrakeEngaged && Locomotive.ThrottlePercent > 0)
                {
                    float pct = Locomotive.ThrottlePercent - 0.3f;
                    if (pct < 0)
                    {
                        pct = 0;
                        dynBrakeEngaged = false;
                    }
                    Locomotive.ThrottleController.SetPercent(pct);
                }
            }
            if (controllerPosition == ControllerPosition.Neutral)
            {
                if (Locomotive.DynamicBrakeController != null)
                {
                    Locomotive.DynamicBrakeController.StopIncrease();
                    Locomotive.DynamicBrakeController.StopDecrease();
                }
            }
            if (controllerPosition == ControllerPosition.DynamicBrakeDecrease)
            {
                Locomotive.DynamicBrakeController.StartDecrease();
                if (Locomotive.DynamicBrakePercent == 0)
                {
                    if (haveCruiseControl)
                        Locomotive.CruiseControl.DynamicBrakePriority = false;
                    Locomotive.DynamicBrakeChangeActiveState(false);
                }
            }

            ReloadPositions();

            if (haveCruiseControl)
                if (Locomotive.CruiseControl.DynamicBrakePriority && Locomotive.DynamicBrakePercent > -1 && controllerBinding == ControllerBinding.DynamicBrake) return;

            if (Locomotive.AbsSpeedMpS > 0)
            {
                if (emergencyBrake)
                {
                    Locomotive.TrainBrakeController.TCSEmergencyBraking = true;
                    return;
                }
            }
            else
            {
                emergencyBrake = false;
            }
/*            if (Locomotive.TrainBrakeController.TCSEmergencyBraking)
                Locomotive.TrainBrakeController.TCSEmergencyBraking = false; */
            elapsedSecondsFromLastChange += elapsedClockSeconds;
            // Simulator.Confirmer.MSG(currentPosition.ToString());
            if (checkNeutral)
            {
                if (elapsedSecondsFromLastChange > 0.2f)
                {
                    CheckNeutralPosition();
                    checkNeutral = false;
                }
            }
            bool ccAutoMode = false;
            if (haveCruiseControl)
            {
                if (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto)
                {
                    ccAutoMode = true;
                }

            }

            if (controllerBinding == ControllerBinding.TrainBrake)
            {
                foreach (MSTSNotch notch in Locomotive.TrainBrakeController.Notches)
                {
                    if (controllerPosition == ControllerPosition.TrainBrakesControllerFullQuickReleaseStart)
                    {
                        if (notch.Type == ORTS.Scripting.Api.ControllerState.FullQuickRelease && Locomotive.TrainBrakeController.CurrentValue != notch.Value)
                        {
                            Locomotive.SetTrainBrakePercent(notch.Value * 100);
                        }
                    }
                    if (controllerPosition == ControllerPosition.TrainBrakesControllerOverchargeStart)
                    {
                        if (notch.Type == ORTS.Scripting.Api.ControllerState.OverchargeStart && Locomotive.TrainBrakeController.CurrentValue != notch.Value)
                        {
                            Locomotive.SetTrainBrakePercent(notch.Value * 100);
                        }
                    }
                    if (controllerPosition == ControllerPosition.TrainBrakesControllerHoldLappedStart)
                    {
                        if (notch.Type == ORTS.Scripting.Api.ControllerState.Lap && Locomotive.TrainBrakeController.CurrentValue != notch.Value)
                        {
                            Locomotive.SetTrainBrakePercent(notch.Value * 100);
                        }
                    }
                    if (controllerPosition == ControllerPosition.TrainBrakesControllerReleaseStart)
                    {
                        if (notch.Type == ORTS.Scripting.Api.ControllerState.Release && Locomotive.TrainBrakeController.CurrentValue != notch.Value)
                        {
                            Locomotive.SetTrainBrakePercent(notch.Value * 100);
                        }
                    }
                    if (controllerPosition == ControllerPosition.TrainBrakesControllerNeutralhandleOffStart)
                    {
                        if (notch.Type == ORTS.Scripting.Api.ControllerState.Neutral && Locomotive.TrainBrakeController.CurrentValue != notch.Value)
                        {
                            Locomotive.SetTrainBrakePercent(notch.Value * 100);
                        }
                    }
                    if (controllerPosition == ControllerPosition.TrainBrakesControllerApplyStart)
                    {
                        if (notch.Type == ORTS.Scripting.Api.ControllerState.Apply && Locomotive.TrainBrakeController.CurrentValue != notch.Value)
                        {
                            Locomotive.SetTrainBrakePercent(notch.Value * 100);
                        }
                        if (Locomotive.CruiseControl != null)
                            Locomotive.CruiseControl.TrainBrakePriority = true;
                    }
                    if (controllerPosition == ControllerPosition.TrainBrakesControllerEmergencyStart)
                    {
                        if (notch.Type == ORTS.Scripting.Api.ControllerState.Emergency && Locomotive.TrainBrakeController.CurrentValue != notch.Value)
                        {
                            Locomotive.SetTrainBrakePercent(notch.Value * 100);
                        }
                        else
                        {
                            Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
                        }
                    }
                }
                return;
            }

            if (!haveCruiseControl || !ccAutoMode)
            {
                if (controllerPosition == ControllerPosition.ThrottleIncrease && Locomotive.Direction != ORTS.Common.Direction.N)
                {
                    if (Locomotive.extendedPhysics != null)
                    {
                        float step = Locomotive.MaxControllerVolts / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                        step *= elapsedClockSeconds;
                        if (Locomotive.ControllerVolts >= 0)
                            Locomotive.ControllerVolts += step;
                        if (Locomotive.ControllerVolts > Locomotive.MaxControllerVolts)
                            Locomotive.ControllerVolts = Locomotive.MaxControllerVolts;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 1)
                        {
                            if (Locomotive.ThrottlePercent < 100)
                            {
                                float step = 100 / Locomotive.ThrottleFullRangeIncreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + step);
                            }
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseFast && Locomotive.Direction != ORTS.Common.Direction.N)
                {
                    if (Locomotive.extendedPhysics != null)
                    {
                        float step = Locomotive.MaxControllerVolts / (Locomotive.ThrottleFullRangeIncreaseTimeSeconds / 2);
                        step *= elapsedClockSeconds;
                        if (Locomotive.ControllerVolts >= 0)
                            Locomotive.ControllerVolts += step;
                        if (Locomotive.ControllerVolts > Locomotive.MaxControllerVolts)
                            Locomotive.ControllerVolts = Locomotive.MaxControllerVolts;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 1)
                        {
                            if (Locomotive.ThrottlePercent < 100)
                            {
                                float step = 100 / (Locomotive.ThrottleFullRangeIncreaseTimeSeconds / 2);
                                step *= elapsedClockSeconds;
                                Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + step);
                            }
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleDecrease)
                {
                    if (Locomotive.extendedPhysics != null && Locomotive.ControllerVolts >= 0)
                    {
                        float step = Locomotive.MaxControllerVolts / Locomotive.ThrottleFullRangeDecreaseTimeSeconds;
                        step *= elapsedClockSeconds;
                        Locomotive.ControllerVolts -= step;
                        if (Locomotive.ControllerVolts < 0)
                            Locomotive.ControllerVolts = 0;
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent > 0)
                        {
                            float step = 100 / Locomotive.ThrottleFullRangeDecreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - step);
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleDecreaseFast && Locomotive.ControllerVolts >= 0)
                {
                    if (Locomotive.extendedPhysics != null)
                    {
                        float step = Locomotive.MaxControllerVolts / Locomotive.ThrottleFullRangeDecreaseTimeSeconds * 2;
                        step *= elapsedClockSeconds;
                        Locomotive.ControllerVolts -= step;
                        if (Locomotive.ControllerVolts < 0)
                            Locomotive.ControllerVolts = 0;
                    }
                    else if (Locomotive.ThrottlePercent > 0)
                    {
                        float step = 100 / Locomotive.ThrottleFullRangeDecreaseTimeSeconds * 2;
                        step *= elapsedClockSeconds;
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - step);
                    }
                }
                if (controllerPosition == ControllerPosition.Neutral || controllerPosition == ControllerPosition.DynamicBrakeHold)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease(0);
                        }
                    }
                    if (Locomotive.ThrottlePercent < 2 && controllerBinding == ControllerBinding.Throttle)
                    {
                        if (Locomotive.ThrottlePercent != 0)
                            Locomotive.SetThrottlePercent(0);
                        if (Locomotive.ControllerVolts > 0)
                            Locomotive.ControllerVolts = 0;
                    }
                    if (Locomotive.ThrottlePercent > 1 && controllerBinding == ControllerBinding.Throttle)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - 1f);
                        Locomotive.ControllerVolts -= 0.05f;
                        if (Locomotive.ControllerVolts < 0)
                            Locomotive.ControllerVolts = 0;
                    }
                    if (Locomotive.ThrottlePercent > 100 && controllerBinding == ControllerBinding.Throttle)
                    {
                        Locomotive.ThrottlePercent = 100;
                    }

                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease(0);
                        }
                    }
                    if (Locomotive.DynamicBrakePercent == -1) Locomotive.SetDynamicBrakePercent(0);
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 2f);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseFast)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease(0);
                        }
                    }
                    if (Locomotive.DynamicBrakePercent == -1) Locomotive.SetDynamicBrakePercent(0);
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 2f);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeDecrease)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        float step = Locomotive.MaxControllerVolts / Locomotive.DynamicBrakeFullRangeDecreaseTimeSeconds;
                        step *= elapsedClockSeconds;
                        Locomotive.DynamicBrakePercent -= step;
                        if (Locomotive.DynamicBrakePercent < 1)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                }
                if ((controllerPosition == ControllerPosition.Drive || controllerPosition == ControllerPosition.ThrottleHold) && Locomotive.ThrottlePercent > 0)
                {
                    if (Locomotive.DynamicBrakePercent < 2 && Locomotive.DynamicBrakePercent > -1)
                    {
                        Locomotive.SetDynamicBrakePercent(-1);
                    }
                    if (Locomotive.DynamicBrakePercent > 1)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeIncrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "apply")
                        {
                            String test = Locomotive.TrainBrakeController.GetStatus().ToLower();
                            Locomotive.StartTrainBrakeIncrease(null, 1);
                        }
                        else
                        {
                            Locomotive.StopTrainBrakeIncrease(0);
                        }
                    }
                }
                else if (controllerPosition == ControllerPosition.Drive)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "release")
                        {
                            String boom = Locomotive.TrainBrakeController.GetStatus().ToString();
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        else
                            Locomotive.StopTrainBrakeDecrease(0);
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeDecrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "release")
                        {
                            String boom = Locomotive.TrainBrakeController.GetStatus().ToString();
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        else
                            Locomotive.StopTrainBrakeDecrease(0);
                    }
                }
                if (controllerPosition == ControllerPosition.EmergencyBrake)
                {
                    EmergencyBrakes();
                    emergencyBrake = true;
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecrease)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 0.2f);
                        if (Locomotive.DynamicBrakePercent < 2)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100)
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + 0.2f);
                        if (Locomotive.ThrottlePercent > 100)
                            Locomotive.SetThrottlePercent(100);
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecreaseFast)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                        if (Locomotive.DynamicBrakePercent < 2)
                        {
                            Locomotive.SetDynamicBrakePercent(0);
                            Locomotive.DynamicBrakeChangeActiveState(false);
                        }
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100)
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + 1);
                        if (Locomotive.ThrottlePercent > 100)
                            Locomotive.SetThrottlePercent(100);
                    }
                }

                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseOrThrottleDecrease)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - 0.2f);
                        if (Locomotive.ThrottlePercent < 0)
                            Locomotive.ThrottlePercent = 0;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 100)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 0.2f);
                        }
                        if (Locomotive.DynamicBrakePercent > 100)
                            Locomotive.SetDynamicBrakePercent(100);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseOrThrottleDecreaseFast)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - 1);
                        if (Locomotive.ThrottlePercent < 0)
                            Locomotive.ThrottlePercent = 0;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 100)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 1);
                        }
                        if (Locomotive.DynamicBrakePercent > 100)
                            Locomotive.SetDynamicBrakePercent(100);
                    }
                }
            }
            else if (haveCruiseControl && ccAutoMode)
            {
                if (controllerBinding == ControllerBinding.TrainBrake)
                    Simulator.Confirmer.MSG(controllerPosition.ToString());
                if (controllerPosition == ControllerPosition.TrainBrakesControllerApplyStart)
                {
                    if (Locomotive.CruiseControl != null)
                        Locomotive.CruiseControl.TrainBrakePriority = true;
                }

                if (Locomotive.CruiseControl.CruiseControlLogic == CruiseControl.ControllerCruiseControlLogic.SpeedOnly)
                {
                    if (controllerPosition == ControllerPosition.ThrottleIncrease)
                    {
                        if (Locomotive.CruiseControl.RestrictedSpeedActive && (Locomotive.LocoType != MSTSLocomotive.LocoTypes.Vectron || Locomotive.SelectedSpeedConfirmed))
                        {
                            Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.CurrentSelectedSpeedMpS;
                            Locomotive.CruiseControl.RestrictedSpeedActive = false;
                        }
                        if (!Locomotive.CruiseControl.ContinuousSpeedIncreasing && movedForward) return;
                        movedForward = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS + Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS > Locomotive.MaxSpeedMpS) Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
                    }
                    if (controllerPosition == ControllerPosition.ThrottleIncreaseFast)
                    {
                        if (Locomotive.CruiseControl.RestrictedSpeedActive && (Locomotive.LocoType != MSTSLocomotive.LocoTypes.Vectron || Locomotive.SelectedSpeedConfirmed))
                        {
                            Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.CurrentSelectedSpeedMpS;
                            Locomotive.CruiseControl.RestrictedSpeedActive = false;
                        }
                        if (!Locomotive.CruiseControl.ContinuousSpeedIncreasing && movedForward) return;
                        movedForward = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS + Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS * 2;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS > Locomotive.MaxSpeedMpS) Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
                    }
                    if (controllerPosition == ControllerPosition.ThrottleDecrease)
                    {
                        if (Locomotive.CruiseControl.RestrictedSpeedActive && (Locomotive.LocoType != MSTSLocomotive.LocoTypes.Vectron || Locomotive.SelectedSpeedConfirmed))
                        {
                            Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.CurrentSelectedSpeedMpS;
                            Locomotive.CruiseControl.RestrictedSpeedActive = false;
                        }
                        if (!Locomotive.CruiseControl.ContinuousSpeedDecreasing && movedAft) return;
                        movedAft = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS - Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS < 0) Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                    }
                    if (controllerPosition == ControllerPosition.ThrottleDecreaseFast)
                    {
                        if (Locomotive.CruiseControl.RestrictedSpeedActive && (Locomotive.LocoType != MSTSLocomotive.LocoTypes.Vectron || Locomotive.SelectedSpeedConfirmed))
                        {
                            Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.CurrentSelectedSpeedMpS;
                            Locomotive.CruiseControl.RestrictedSpeedActive = false;
                        }
                        if (!Locomotive.CruiseControl.ContinuousSpeedDecreasing && movedAft) return;
                        movedAft = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS - Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS * 2;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS < 0) Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                    }
                    return;
                }
                if (controllerPosition == ControllerPosition.ThrottleIncrease)
                {
                    isBraking = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Start;
                    previousDriveModeWasAddPower = true;
                    if (haveCruiseControl)
                    {
                        if (Locomotive.CruiseControl.UseThrottleAsForceSelector)
                        {
                            Locomotive.SelectedMaxAccelerationStep += 0.5f;
                            if (Locomotive.SelectedMaxAccelerationStep > 100)
                                Locomotive.SelectedMaxAccelerationStep = 100;
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.Neutral)
                {
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                }
                if (controllerPosition == ControllerPosition.Drive)
                {
                    bool applyPower = true;
                    if (isBraking && needPowerUpAfterBrake)
                    {
                        if (Locomotive.DynamicBrakePercent < 2)
                        {
                            Locomotive.SetDynamicBrakePercent(-1);
                        }
                        if (Locomotive.DynamicBrakePercent > 1)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                        }
                        if (CanControlTrainBrake)
                        {
                            if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "release")
                            {
                                Locomotive.StartTrainBrakeDecrease(null);
                            }
                            else
                                Locomotive.StopTrainBrakeDecrease(0);
                        }
                        applyPower = false;
                    }
                    if (applyPower) Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.On;
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncrease)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease(0);
                        }
                    }
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        if (Locomotive.DynamicBrakePercent < 0)
                            Locomotive.DynamicBrakeChangeActiveState(true);
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 1f);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseFast)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "apply")
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() == "neutral")
                        {
                            Locomotive.StopTrainBrakeDecrease(0);
                        }
                    }
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 2f);
                    }
                }

                if (controllerPosition == ControllerPosition.TrainBrakeIncrease)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    if (Locomotive.CruiseControl != null)
                    {
                        Locomotive.CruiseControl.TrainBrakePriority = true;
                    }
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "apply")
                        {
                            String test = Locomotive.TrainBrakeController.GetStatus().ToLower();
                            Locomotive.StartTrainBrakeIncrease(null, 1);
                        }
                        else
                        {
                            Locomotive.StopTrainBrakeIncrease(0);
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.EmergencyBrake)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    EmergencyBrakes();
                    emergencyBrake = true;
                }
                if (controllerPosition == ControllerPosition.SelectedSpeedIncrease)
                {
                    Locomotive.CruiseControl.SpeedRegulatorSelectedSpeedIncrease();
                }
                if (controllerPosition == ControllerPosition.SelectedSpeedDecrease)
                {
                    Locomotive.CruiseControl.SpeedRegulatorSelectedSpeedDecrease();
                }
                if (controllerPosition == ControllerPosition.SelectSpeedZero)
                {
                    Locomotive.CruiseControl.SetSpeed(0);
                }
                if (controllerPosition == ControllerPosition.ThrottleDecrease)
                {
                    if (haveCruiseControl && ccAutoMode)
                    {
                        Locomotive.SelectedMaxAccelerationStep -= 0.5f;
                        if (Locomotive.SelectedMaxAccelerationStep < 0)
                            Locomotive.SelectedMaxAccelerationStep = 0;
                    }
                }
            }
        }

        private bool messageDisplayed = false;
        public void DoMovement(Movement movement)
        {
            if (movement == Movement.Aft) movedForward = false;
            if (movement == Movement.Forward) movedAft = false;
            if (movement == Movement.Neutral) movedForward = movedAft = false;
            messageDisplayed = false;
            if (String.IsNullOrEmpty(currentPosition))
            {
                foreach (Position pair in PositionsList)
                {
                    if (pair.Flag.ToLower() == "default")
                    {
                        currentPosition = pair.Type;
                        break;
                    }
                }
            }
            if (movement == Movement.Forward)
            {
                noKeyPressed = false;
                checkNeutral = false;
                bool isFirst = true;
                string previous = "";
                foreach (Position pair in PositionsList)
                {
                    if (pair.Type == currentPosition)
                    {
                        if (isFirst)
                            break;
                        currentPosition = previous;
                        break;
                    }
                    isFirst = false;
                    previous = pair.Type;
                }
            }
            if (movement == Movement.Aft)
            {
                noKeyPressed = false;
                checkNeutral = false;
                bool selectNext = false;
                foreach (Position pair in PositionsList)
                {
                    if (selectNext)
                    {
                        currentPosition = pair.Type;
                        break;
                    }
                    if (pair.Type == currentPosition) selectNext = true;
                }
            }
            if (movement == Movement.Neutral)
            {
                noKeyPressed = true;
                foreach (Position pair in PositionsList)
                {
                    if (pair.Type == currentPosition)
                    {
                        if (pair.Flag.ToLower() == "springloadedbackwards" || pair.Flag.ToLower() == "springloadedforwards")
                        {
                            checkNeutral = true;
                            elapsedSecondsFromLastChange = 0;
                        }
                        if (pair.Flag.ToLower() == "springloadedbackwardsimmediatelly" || pair.Flag.ToLower() == "springloadedforwardsimmediatelly")
                        {
                            if (!MouseInputActive)
                            {
                                CheckNeutralPosition();
                                ReloadPositions();
                            }
                        }
                    }
                }
            }

        }

        public void ReloadPositions()
        {
            if (noKeyPressed)
            {
                foreach (Position pair in PositionsList)
                {
                    if (pair.Type == currentPosition)
                    {
                        if (pair.Flag.ToLower() == "cruisecontrol.needincreaseafteranybrake")
                        {
                            needPowerUpAfterBrake = true;
                        }
                        if (pair.Flag.ToLower() == "springloadedforwards" || pair.Flag.ToLower() == "springloadedbackwards")
                        {
                            if (elapsedSecondsFromLastChange > 0.2f)
                            {
                                elapsedSecondsFromLastChange = 0;
                                checkNeutral = true;
                            }
                        }
                    }
                }
            }
            switch (currentPosition)
            {
                case "ThrottleIncrease":
                    {
                        controllerPosition = ControllerPosition.ThrottleIncrease;
                        break;
                    }
                case "ThrottleIncreaseFast":
                    {
                        controllerPosition = ControllerPosition.ThrottleIncreaseFast;
                        break;
                    }
                case "ThrottleIncreaseOrDynamicBrakeDecrease":
                    {
                        controllerPosition = ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecrease;
                        break;
                    }
                case "ThrottleIncreaseOrDynamicBrakeDecreaseFast":
                    {
                        controllerPosition = ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecreaseFast;
                        break;
                    }
                case "DynamicBrakeIncreaseOrThrottleDecrease":
                    {
                        controllerPosition = ControllerPosition.DynamicBrakeIncreaseOrThrottleDecrease;
                        break;
                    }
                case "DynamicBrakeIncreaseOrThrottleDecreaseFast":
                    {
                        controllerPosition = ControllerPosition.DynamicBrakeIncreaseOrThrottleDecreaseFast;
                        break;
                    }
                case "ThrottleDecrease":
                    {
                        controllerPosition = ControllerPosition.ThrottleDecrease;
                        break;
                    }
                case "ThrottleDecreaseFast":
                    {
                        controllerPosition = ControllerPosition.ThrottleDecreaseFast;
                        break;
                    }
                case "Drive":
                    {
                        controllerPosition = ControllerPosition.Drive;
                        break;
                    }
                case "ThrottleHold":
                    {
                        controllerPosition = ControllerPosition.ThrottleHold;
                        break;
                    }
                case "Neutral":
                    {
                        controllerPosition = ControllerPosition.Neutral;
                        break;
                    }
                case "KeepCurrent":
                    {
                        controllerPosition = ControllerPosition.KeepCurrent;
                        break;
                    }
                case "DynamicBrakeHold":
                    {
                        controllerPosition = ControllerPosition.DynamicBrakeHold;
                        break;
                    }
                case "DynamicBrakeIncrease":
                    {
                        controllerPosition = ControllerPosition.DynamicBrakeIncrease;
                        break;
                    }
                case "DynamicBrakeIncreaseFast":
                    {
                        controllerPosition = ControllerPosition.DynamicBrakeIncreaseFast;
                        break;
                    }
                case "DynamicBrakeDecrease":
                    {
                        controllerPosition = ControllerPosition.DynamicBrakeDecrease;
                        break;
                    }
                case "TrainBrakeIncrease":
                    {
                        controllerPosition = ControllerPosition.TrainBrakeIncrease;
                        break;
                    }
                case "TrainBrakeDecrease":
                    {
                        controllerPosition = ControllerPosition.TrainBrakeDecrease;
                        break;
                    }
                case "EmergencyBrake":
                    {
                        controllerPosition = ControllerPosition.EmergencyBrake;
                        break;
                    }
                case "SelectedSpeedIncrease":
                    {
                        controllerPosition = ControllerPosition.SelectedSpeedIncrease;
                        break;
                    }
                case "SelectedSpeedDecrease":
                    {
                        controllerPosition = ControllerPosition.SelectedSpeedDecrease;
                        break;
                    }
                case "SelectSpeedZero":
                    {
                        controllerPosition = ControllerPosition.SelectSpeedZero;
                        break;
                    }
                case "DynamicBrakeIncreaseWithPriority":
                    {
                        controllerPosition = ControllerPosition.DynamicBrakeIncreaseWithPriority;
                        break;
                    }
                case "TrainBrakesControllerFullQuickReleaseStart":
                    {
                        controllerPosition = ControllerPosition.TrainBrakesControllerFullQuickReleaseStart;
                        break;
                    }
                case "TrainBrakesControllerOverchargeStart":
                    {
                        controllerPosition = ControllerPosition.TrainBrakesControllerOverchargeStart;
                        break;
                    }
                case "TrainBrakesControllerHoldLappedStart":
                    {
                        controllerPosition = ControllerPosition.TrainBrakesControllerHoldLappedStart;
                        break;
                    }
                case "TrainBrakesControllerReleaseStart":
                    {
                        controllerPosition = ControllerPosition.TrainBrakesControllerReleaseStart;
                        break;
                    }
                case "TrainBrakesControllerNeutralhandleOffStart":
                    {
                        controllerPosition = ControllerPosition.TrainBrakesControllerNeutralhandleOffStart;
                        break;
                    }
                case "TrainBrakesControllerApplyStart":
                    {
                        controllerPosition = ControllerPosition.TrainBrakesControllerApplyStart;
                        break;
                    }
                case "TrainBrakesControllerEmergencyStart":
                    {
                        controllerPosition = ControllerPosition.TrainBrakesControllerEmergencyStart;
                        break;
                    }
            }
            if (!messageDisplayed)
            {
                string msg = GetPositionName(currentPosition);
                if (!string.IsNullOrEmpty(msg))
                    Simulator.Confirmer.Information(msg);
            }
            messageDisplayed = true;
        }

        public void CheckNeutralPosition()
        {
            bool setNext = false;
            String previous = "";
            foreach (Position pair in PositionsList)
            {
                if (setNext)
                {
                    currentPosition = pair.Type;
                    break;
                }
                if (pair.Type == currentPosition)
                {
                    if (pair.Flag.ToLower() == "springloadedbackwards" || pair.Flag.ToLower() == "springloadedbackwardsimmediatelly")
                    {
                        setNext = true;
                    }
                    if (pair.Flag.ToLower() == "springloadedforwards" || pair.Flag.ToLower() == "springloadedforwardsimmediatelly")
                    {
                        currentPosition = previous;
                        break;
                    }
                }
                previous = pair.Type;
            }
        }

        protected string GetPositionName(string type)
        {
            string ret = "";
            foreach (Position p in PositionsList)
            {
                if (p.Type.ToLower() == type.ToLower())
                    ret = p.Name;
            }
            return ret;
        }

        protected void EmergencyBrakes()
        {
            Locomotive.SetThrottlePercent(0);
            Locomotive.SetDynamicBrakePercent(100);
            Locomotive.TrainBrakeController.TCSEmergencyBraking = true;
        }
        public enum Movement
        {
            Forward,
            Neutral,
            Aft
        };
        public enum ControllerPosition
        {
            Neutral,
            Drive,
            ThrottleIncrease,
            ThrottleDecrease,
            ThrottleIncreaseFast,
            ThrottleDecreaseFast,
            DynamicBrakeIncrease, DynamicBrakeDecrease,
            DynamicBrakeIncreaseFast,
            TrainBrakeIncrease,
            TrainBrakeDecrease,
            EmergencyBrake,
            ThrottleHold,
            DynamicBrakeHold,
            ThrottleIncreaseOrDynamicBrakeDecreaseFast,
            ThrottleIncreaseOrDynamicBrakeDecrease,
            DynamicBrakeIncreaseOrThrottleDecreaseFast,
            DynamicBrakeIncreaseOrThrottleDecrease,
            KeepCurrent,
            SelectedSpeedIncrease,
            SelectedSpeedDecrease,
            SelectSpeedZero,
            DynamicBrakeIncreaseWithPriority,
            TrainBrakesControllerFullQuickReleaseStart,
            TrainBrakesControllerOverchargeStart,
            TrainBrakesControllerHoldLappedStart,
            TrainBrakesControllerReleaseStart,
            TrainBrakesControllerNeutralhandleOffStart,
            TrainBrakesControllerApplyStart,
            TrainBrakesControllerEmergencyStart
        };

        public enum ControllerBinding
        {
            Throttle,
            DynamicBrake,
            SelectedSpeed,
            TrainBrake
        }

        public float GetDataOf(CabViewControl cvc)
        {
            if (cvc.ControlId != ControllerId)
                return 0;
            float data = 0;
            switch (cvc.ControlType)
            {
                case CABViewControlTypes.ORTS_MULTI_POSITION_CONTROLLER:
                    {
                        switch (controllerPosition)
                        {
                            case ControllerPosition.ThrottleIncrease:
                            case ControllerPosition.TrainBrakesControllerFullQuickReleaseStart:
                                data = 0;
                                break;
                            case ControllerPosition.Drive:
                            case ControllerPosition.ThrottleHold:
                            case ControllerPosition.TrainBrakesControllerOverchargeStart:
                                data = 1;
                                break;
                            case ControllerPosition.Neutral:
                            case ControllerPosition.TrainBrakesControllerHoldLappedStart:
                                data = 2;
                                break;
                            case ControllerPosition.DynamicBrakeIncrease:
                            case ControllerPosition.DynamicBrakeIncreaseWithPriority:
                            case ControllerPosition.TrainBrakesControllerReleaseStart:
                                data = 3;
                                break;
                            case ControllerPosition.TrainBrakeIncrease:
                            case ControllerPosition.TrainBrakesControllerNeutralhandleOffStart:
                                data = 4;
                                break;
                            case ControllerPosition.EmergencyBrake:
                            case ControllerPosition.DynamicBrakeIncreaseFast:
                            case ControllerPosition.TrainBrakesControllerApplyStart:
                                data = 5;
                                break;
                            case ControllerPosition.ThrottleIncreaseFast:
                            case ControllerPosition.TrainBrakesControllerEmergencyStart:
                                data = 6;
                                break;
                            case ControllerPosition.ThrottleDecrease:
                                data = 7;
                                break;
                            case ControllerPosition.ThrottleDecreaseFast:
                                data = 8;
                                break;
                            case ControllerPosition.SelectedSpeedIncrease:
                                data = 9;
                                break;
                            case ControllerPosition.SelectedSpeedDecrease:
                                data = 10;
                                break;
                            case ControllerPosition.SelectSpeedZero:
                                data = 11;
                                break;
                            case ControllerPosition.DynamicBrakeDecrease:
                                data = 12;
                                break;
                        }
                        break;
                    }
                default:
                    data = 0;
                    break;
            }
            return data;
        }

        public class Position
        {
            public string Type { get; set; }
            public string Flag { get; set; }
            public string Name { get; set; }
            public Position(string positionType, string positionFlag, string name)
            {
                Type = positionType;
                Flag = positionFlag;
                Name = name;
            }
        }
    }
}