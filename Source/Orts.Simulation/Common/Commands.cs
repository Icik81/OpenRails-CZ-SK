﻿// COPYRIGHT 2012, 2013, 2014, 2015 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team.

using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Diagnostics;   // Used by Trace.Warnings

namespace Orts.Common
{
    /// <summary>
    /// This Command Pattern allows requests to be encapsulated as objects (http://sourcemaking.com/design_patterns/command).
    /// The pattern provides many advantages, but it allows OR to record the commands and then to save them when the user presses F2.
    /// The commands can later be read from file and replayed.
    /// Writing and reading is done using the .NET binary serialization which is quick to code. (For an editable version, JSON has
    /// been successfully explored.)
    /// 
    /// Immediate commands (e.g. sound horn) are straightforward but continuous commands (e.g. apply train brake) are not. 
    /// OR aims for commands which can be repeated accurately and possibly on a range of hardware. Continuous commands therefore
    /// have a target value which is recorded once the key is released. OR creates an immediate command as soon as the user 
    /// presses the key, but OR creates the continuous command once the user releases the key and the target is known. 
    /// 
    /// All commands record the time when the command is created, but a continuous command backdates the time to when the key
    /// was pressed.
    /// 
    /// Each command class has a Receiver property and calls methods on the Receiver to execute the command.
    /// This property is static for 2 reasons:
    /// - so all command objects of the same class will share the same Receiver object;
    /// - so when a command is serialized to and deserialised from file, its Receiver does not have to be saved 
    ///   (which would be impractical) but is automatically available to commands which have been re-created from file.
    /// 
    /// Before each command class is used, this Receiver must be assigned, e.g.
    ///   ReverserCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
    /// 
    /// </summary>
    public interface ICommand
    {

        /// <summary>
        /// The time when the command was issued (compatible with Simlator.ClockTime).
        /// </summary>
        double Time { get; set; }

        /// <summary>
        /// Call the Receiver to repeat the Command.
        /// Each class of command shares a single object, the Receiver, and the command executes by
        /// call methods of the Receiver.
        /// </summary>
        void Redo();

        /// <summary>
        /// Print the content of the command.
        /// </summary>
        void Report();
    }

    [Serializable()]
    public abstract class Command : ICommand
    {
        public double Time { get; set; }

        /// <summary>
        /// Each command adds itself to the log when it is constructed.
        /// </summary>
        public Command(CommandLog log)
        {
            log.CommandAdd(this as ICommand);
        }

        // Method required by ICommand
        public virtual void Redo() { Trace.TraceWarning("Dummy method"); }
        public virtual void Redo(int from) { Trace.TraceWarning("Dummy method"); }

        public override string ToString()
        {
            return this.GetType().ToString();
        }

        // Method required by ICommand
        public virtual void Report()
        {
            Trace.WriteLine(String.Format(
               "Command: {0} {1}", FormatStrings.FormatPreciseTime(Time), ToString()));
        }
    }

    // <Superclasses>
    [Serializable()]
    public abstract class BooleanCommand : Command
    {
        protected bool ToState;

        public BooleanCommand(CommandLog log, bool toState)
            : base(log)
        {
            ToState = toState;
        }
    }

    [Serializable()]
    public abstract class IndexCommand : Command
    {
        protected int Index;

        public IndexCommand(CommandLog log, int index)
            : base(log)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Superclass for continuous commands. Do not create a continuous command until the operation is complete.
    /// </summary>
    [Serializable()]
    public abstract class ContinuousCommand : BooleanCommand
    {
        protected float? Target;

        public ContinuousCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState)
        {
            Target = target;
            this.Time = startTime;   // Continuous commands are created at end of change, so overwrite time when command was created
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "increase" : "decrease") + ", target = " + Target.ToString();
        }
    }

    [Serializable()]
    public abstract class PausedCommand : Command
    {
        public double PauseDurationS;

        public PausedCommand(CommandLog log, double pauseDurationS)
            : base(log)
        {
            PauseDurationS = pauseDurationS;
        }

        public override string ToString()
        {
            return String.Format("{0} Paused Duration: {1}", base.ToString(), PauseDurationS);
        }
    }

    [Serializable()]
    public abstract class CameraCommand : Command
    {
        public CameraCommand(CommandLog log)
            : base(log)
        {
        }
    }

    [Serializable()]
    public sealed class SaveCommand : Command
    {
        public string FileStem;

        public SaveCommand(CommandLog log, string fileStem)
            : base(log)
        {
            this.FileStem = fileStem;
            Redo();
        }

        public override void Redo()
        {
            // Redo does nothing as SaveCommand is just a marker and saves the fileStem but is not used during replay to redo the save.
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " to file \"" + FileStem + ".replay\"";
        }
    }

    // Direction
    [Serializable()]
    public sealed class ReverserCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ReverserCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (ToState)
            {
                Receiver.StartReverseIncrease(null);
            }
            else
            {
                Receiver.StartReverseDecrease(null);
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "step forward" : "step back");
        }
    }

    [Serializable()]
    public sealed class ContinuousReverserCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousReverserCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ReverserChangeTo(ToState, Target);
            // Report();
        }
    }

    // Power : Raise/lower pantograph
    [Serializable()]
    public sealed class PantographCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }
        private int item;

        public PantographCommand(CommandLog log, int item, bool toState)
            : base(log, toState)
        {
            this.item = item;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null && Receiver.Train != null)
            {
                // Icik
                if (Receiver.Pantograph3Enable)
                    Receiver.TogglePantograph3Switch();
                else
                if (Receiver.Pantograph4Enable)
                    Receiver.TogglePantograph4Switch();
                else
                if (Receiver.Pantograph4NCEnable)
                    Receiver.TogglePantograph4NCSwitch();
                else
                if (Receiver.Pantograph5Enable)
                    Receiver.TogglePantograph5Switch();
                else
                {
                    Receiver.Train.SignalEvent(ToState ? PowerSupplyEvent.RaisePantograph : PowerSupplyEvent.LowerPantograph, item);
                    Receiver.PantoCommandDown = ToState;
                }
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "raise" : "lower") + ", item = " + item.ToString();
        }
    }

    // Power : Close/open circuit breaker
    [Serializable()]
    public sealed class CircuitBreakerClosingOrderCommand : BooleanCommand
    {
        public static MSTSElectricLocomotive Receiver { get; set; }

        public CircuitBreakerClosingOrderCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null && Receiver.Train != null)
            {
                Receiver.Train.SignalEvent(ToState ? PowerSupplyEvent.CloseCircuitBreaker : PowerSupplyEvent.OpenCircuitBreaker);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "close" : "open");
        }
    }

    // Power : Close circuit breaker button
    [Serializable()]
    public sealed class CircuitBreakerClosingOrderButtonCommand : BooleanCommand
    {
        public static MSTSElectricLocomotive Receiver { get; set; }

        public CircuitBreakerClosingOrderButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null && Receiver.Train != null)
            {
                Receiver.Train.SignalEvent(ToState ? PowerSupplyEvent.CloseCircuitBreakerButtonPressed : PowerSupplyEvent.CloseCircuitBreakerButtonReleased);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power : Open circuit breaker button
    [Serializable()]
    public sealed class CircuitBreakerOpeningOrderButtonCommand : BooleanCommand
    {
        public static MSTSElectricLocomotive Receiver { get; set; }

        public CircuitBreakerOpeningOrderButtonCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null && Receiver.Train != null)
            {
                Receiver.Train.SignalEvent(ToState ? PowerSupplyEvent.OpenCircuitBreakerButtonPressed : PowerSupplyEvent.OpenCircuitBreakerButtonReleased);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "pressed" : "released");
        }
    }

    // Power : Give/remove circuit breaker authorization
    [Serializable()]
    public sealed class CircuitBreakerClosingAuthorizationCommand : BooleanCommand
    {
        public static MSTSElectricLocomotive Receiver { get; set; }

        public CircuitBreakerClosingAuthorizationCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null && Receiver.Train != null)
            {
                Receiver.Train.SignalEvent(ToState ? PowerSupplyEvent.GiveCircuitBreakerClosingAuthorization : PowerSupplyEvent.RemoveCircuitBreakerClosingAuthorization);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "given" : "removed");
        }
    }

    // Power
    [Serializable()]
    public sealed class PowerCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public PowerCommand(CommandLog log, MSTSLocomotive receiver, bool toState)
            : base(log, toState)
        {
            Receiver = receiver;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;//no receiver of this panto
            Receiver.SetPower(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "ON" : "OFF");
        }
    }

    // MU commands connection
    [Serializable()]
    public sealed class ToggleMUCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleMUCommand(CommandLog log, MSTSLocomotive receiver, bool toState)
            : base(log, toState)
        {
            Receiver = receiver;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;//no receiver of this panto
            Receiver.ToggleMUCommand(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "ON" : "OFF");
        }
    }

    // Icik
    // Helper commands connection
    [Serializable()]
    public sealed class ToggleHelperCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHelperCommand(CommandLog log, MSTSLocomotive receiver, bool toState)
            : base(log, toState)
        {
            Receiver = receiver;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;//no receiver of this panto
            Receiver.ToggleHelperCommand(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "ON" : "OFF");
        }
    }

    [Serializable()]
    public sealed class NotchedThrottleCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public NotchedThrottleCommand(CommandLog log, bool toState) : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AdjustNotchedThrottle(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "step forward" : "step back");
        }
    }

    [Serializable()]
    public sealed class ContinuousThrottleCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ContinuousThrottleCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ThrottleChangeTo(ToState, Target);
            // Report();
        }
    }

    // Brakes
    [Serializable()]
    public sealed class TrainBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TrainBrakeCommand(CommandLog log, bool toState, float? target, double startTime, int from)
            : base(log, toState, target, startTime)
        {
            Redo(from);
        }

        public override void Redo(int from)
        {
            Receiver.TrainBrakeChangeTo(ToState, Target, from);
            // Report();
        }
    }

    [Serializable()]
    public sealed class EngineBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EngineBrakeCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.EngineBrakeChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class BrakemanBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }
        public BrakemanBrakeCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakemanBrakeChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class DynamicBrakeCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public DynamicBrakeCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null)
                Receiver.DynamicBrakeChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class InitializeBrakesCommand : Command
    {
        public static Train Receiver { get; set; }

        public InitializeBrakesCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.UnconditionalInitializeBrakes();
            // Report();
        }
    }

    [Serializable()]
    public sealed class EmergencyPushButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public EmergencyPushButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.EmergencyButtonPressed = !Receiver.EmergencyButtonPressed;
            Receiver.TrainBrakeController.EmergencyBrakingPushButton = Receiver.EmergencyButtonPressed;
            // Report();
        }
    }

    [Serializable()]
    public sealed class BailOffCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BailOffCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetBailOff(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "disengage" : "engage");
        }
    }

    [Serializable()]
    public sealed class QuickReleaseCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public QuickReleaseCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TrainBrakeController.QuickReleaseButtonPressed = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "off" : "on");
        }
    }

    [Serializable()]
    public sealed class BrakeOverchargeCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BrakeOverchargeCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TrainBrakeController.OverchargeButtonPressed = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "off" : "on");
        }
    }

    [Serializable()]
    public sealed class HandbrakeCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public HandbrakeCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetTrainHandbrake(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public sealed class WagonHandbrakeCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public WagonHandbrakeCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetWagonHandbrake(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public sealed class RetainersCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public RetainersCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetTrainRetainers(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }

    [Serializable()]
    public sealed class BrakeHoseConnectCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BrakeHoseConnectCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeHoseConnect(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "connect" : "disconnect");
        }
    }

    [Serializable()]
    public sealed class WagonBrakeHoseConnectCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public WagonBrakeHoseConnectCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.FrontBrakeHoseConnected = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "connect" : "disconnect");
        }
    }

    [Serializable()]
    public sealed class ToggleAngleCockACommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleAngleCockACommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.AngleCockAOpen = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "open" : "close");
        }
    }

    [Serializable()]
    public sealed class ToggleAngleCockBCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleAngleCockBCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.AngleCockBOpen = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "open" : "close");
        }
    }

    [Serializable()]
    public sealed class ToggleBleedOffValveCommand : BooleanCommand
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleBleedOffValveCommand(CommandLog log, MSTSWagon car, bool toState)
            : base(log, toState)
        {
            Receiver = car;
            Redo();
        }

        public override void Redo()
        {
            Receiver.BrakeSystem.BleedOffValveOpen = ToState;
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "open" : "close");
        }
    }

    [Serializable()]
    public sealed class SanderCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public SanderCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (ToState)
            {
                if (!Receiver.Sander)
                    Receiver.SignalEvent(Event.SanderOn);
            }
            else
            {
                Receiver.SignalEvent(Event.SanderOff);
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "on" : "off");
        }
    }

    [Serializable()]
    public sealed class AlerterCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public AlerterCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        bool AlerterPressed;
        public override void Redo()
        {
            if (Receiver.LocoType == MSTSLocomotive.LocoTypes.Vectron && Receiver.CruiseControl != null && ToState)
            {
                TimeSpan ts = DateTime.Now - Receiver.AlerterPressedAt;
                if (ts.TotalSeconds < 1)
                {
                    Receiver.CruiseControl.ActivateRestrictedSpeedZone();
                }
                Receiver.AlerterPressedAt = DateTime.Now;
            }
            if (ToState && !AlerterPressed)
            {
                Receiver.SignalEvent(Event.VigilanceAlarmResetPush); // There is no Event.VigilanceAlarmResetReleased
                AlerterPressed = true;
            }
            else
            {
                Receiver.SignalEvent(Event.VigilanceAlarmResetRelease);
                AlerterPressed = false;
            }
            Receiver.AlerterPressed(ToState);
            Receiver.DisplaySelectedSpeed = ToState;
            // Report();
        }
    }

    [Serializable()]
    public sealed class VacuumExhausterCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public VacuumExhausterCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (ToState)
            {
                if (!Receiver.VacuumExhausterPressed)
                    Receiver.Train.SignalEvent(Event.VacuumExhausterOn);
            }
            else
            {
                Receiver.Train.SignalEvent(Event.VacuumExhausterOff);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "fast" : "normal");
        }
    }

    [Serializable()]
    public sealed class HornCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public HornCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ManualHorn = ToState;
            Receiver.Mirel.ResetVigilance();
            if (ToState)
            {
                Receiver.AlerterReset(TCSEvent.HornActivated);
                Receiver.Simulator.HazzardManager.Horn();
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "sound" : "off");
        }
    }

    [Serializable()]
    public sealed class BellCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public BellCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ManualBell = ToState;
            Receiver.Simulator.HazzardManager.Bell();
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "ring" : "off");
        }
    }

    [Serializable()]
    public sealed class ToggleCabLightCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCabLightCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCabLight();
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleCabFloodLightCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCabFloodLightCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCabFloodLight();
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class HeadlightUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public HeadlightUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleHeadLightsUp();
        }
    }
    [Serializable()]
    public sealed class HeadlightDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public HeadlightDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleHeadLightsDown();
        }
    }
    [Serializable()]
    public sealed class Switch5LightUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public Switch5LightUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleSwitch5LightsUp();
        }
    }
    [Serializable()]
    public sealed class Switch5LightDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public Switch5LightDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleSwitch5LightsDown();
        }
    }
    [Serializable()]
    public sealed class Switch6LightUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public Switch6LightUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleSwitch6LightsUp();
        }
    }
    [Serializable()]
    public sealed class Switch6LightDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public Switch6LightDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleSwitch6LightsDown();
        }
    }
    [Serializable()]
    public sealed class WipersCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public WipersCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleWipers(ToState);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleDoorsLeftCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleDoorsLeftCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.GetCabFlipped()) Receiver.ToggleDoorsRight();
            else Receiver.ToggleDoorsLeft();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleDoorsRightCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleDoorsRightCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.GetCabFlipped()) Receiver.ToggleDoorsLeft();
            else Receiver.ToggleDoorsRight();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleMirrorsCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public ToggleMirrorsCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleMirrors();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleBatteryCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleBatteryCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleBattery();
            // Report();
        }
    }

    //[Serializable()]
    //public sealed class TogglePowerKeyCommand : Command
    //{
    //    public static MSTSLocomotive Receiver { get; set; }

    //    public TogglePowerKeyCommand(CommandLog log)
    //        : base(log)
    //    {
    //        Redo();
    //    }

    //    public override void Redo()
    //    {
    //        Receiver.TogglePowerKey();
    //        // Report();
    //    }
    //}
    // Steam controls
    [Serializable()]
    public sealed class ContinuousSteamHeatCommand : ContinuousCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ContinuousSteamHeatCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            {
                Receiver.SteamHeatChangeTo(ToState, Target);
            }
            // Report();
        }
    }

    // Large Ejector command
    [Serializable()]
    public sealed class ContinuousLargeEjectorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousLargeEjectorCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            {
                Receiver.LargeEjectorChangeTo(ToState, Target);
            }
            // Report();
        }
    }


    [Serializable()]
    public sealed class ContinuousSmallEjectorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousSmallEjectorCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            {
                Receiver.SmallEjectorChangeTo(ToState, Target);
            }
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousInjectorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }
        int Injector;

        public ContinuousInjectorCommand(CommandLog log, int injector, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Injector = injector;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            switch (Injector)
            {
                case 1: { Receiver.Injector1ChangeTo(ToState, Target); break; }
                case 2: { Receiver.Injector2ChangeTo(ToState, Target); break; }
            }
            // Report();
        }

        public override string ToString()
        {
            return String.Format("Command: {0} {1} {2}", FormatStrings.FormatPreciseTime(Time), this.GetType().ToString(), Injector)
                + (ToState ? "open" : "close") + ", target = " + Target.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleInjectorCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }
        private int injector;

        public ToggleInjectorCommand(CommandLog log, int injector)
            : base(log)
        {
            this.injector = injector;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            switch (injector)
            {
                case 1: { Receiver.ToggleInjector1(); break; }
                case 2: { Receiver.ToggleInjector2(); break; }
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + injector.ToString();
        }
    }

    [Serializable()]
    public sealed class ContinuousBlowerCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousBlowerCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.BlowerChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousDamperCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousDamperCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.DamperChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousFireboxDoorCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousFireboxDoorCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.FireboxDoorChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ContinuousFiringRateCommand : ContinuousCommand
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ContinuousFiringRateCommand(CommandLog log, bool toState, float? target, double startTime)
            : base(log, toState, target, startTime)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.FiringRateChangeTo(ToState, Target);
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleManualFiringCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleManualFiringCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleManualFiring();
            // Report();
        }
    }

    [Serializable()]
    public sealed class AIFireOnCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public AIFireOnCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AIFireOn();

        }

    }

    [Serializable()]
    public sealed class AIFireOffCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public AIFireOffCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AIFireOff();

        }

    }

    [Serializable()]
    public sealed class AIFireResetCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public AIFireResetCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.AIFireReset();

        }

    }

    [Serializable()]
    public sealed class FireShovelfullCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public FireShovelfullCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.FireShovelfull();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleOdometerCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleOdometerCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.OdometerToggle();
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ResetOdometerCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ResetOdometerCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.OdometerReset();
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleOdometerDirectionCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleOdometerDirectionCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.OdometerToggleDirection();
            // Report();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [Serializable()]
    public sealed class ToggleWaterScoopCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleWaterScoopCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleWaterScoop();
        }
    }

    // Cylinder Cocks command
    [Serializable()]
    public sealed class ToggleCylinderCocksCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleCylinderCocksCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleCylinderCocks();
            // Report();
        }
    }

    // Compound Valve command
    [Serializable()]
    public sealed class ToggleCylinderCompoundCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleCylinderCompoundCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleCylinderCompound();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleBlowdownValveCommand : Command
    {
        public static MSTSSteamLocomotive Receiver { get; set; }

        public ToggleBlowdownValveCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleBlowdownValve();
            // Report();
        }
    }

    // Diesel player engine on / off command
    [Serializable()]
    public sealed class TogglePlayerEngineCommand : Command
    {
        public static MSTSDieselLocomotive Receiver { get; set; }

        public TogglePlayerEngineCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.TogglePlayerEngine();
            // Report();
        }
    }

    // Diesel helpers engine on / off command
    [Serializable()]
    public sealed class ToggleHelpersEngineCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHelpersEngineCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver == null) return;
            Receiver.ToggleHelpersEngine();
            // Report();
        }
    }

    // Cab radio switch on-switch off command
    [Serializable()]
    public sealed class CabRadioCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public CabRadioCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver != null)
            {
                Receiver.ToggleCabRadio(ToState);
            }
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "switched on" : "switched off");
        }
    }

    [Serializable()]
    public sealed class TurntableClockwiseCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableClockwiseCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.StartContinuous(true);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Clockwise";
        }
    }


    [Serializable()]
    public sealed class TurntableClockwiseTargetCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableClockwiseTargetCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ComputeTarget(true);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Clockwise with target";
        }
    }

    [Serializable()]
    public sealed class TurntableCounterclockwiseCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableCounterclockwiseCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.StartContinuous(false);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Counterclockwise";
        }
    }


    [Serializable()]
    public sealed class TurntableCounterclockwiseTargetCommand : Command
    {
        public static MovingTable Receiver { get; set; }
        public TurntableCounterclockwiseTargetCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ComputeTarget(false);
        }

        public override string ToString()
        {
            return base.ToString() + " " + "Counterclockwise with target";
        }
    }

    // Icik
    [Serializable()]
    public sealed class BrakeCarModeCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public BrakeCarModeCommand(CommandLog log, MSTSWagon car, float BrakeCarMode)
            : base(log)
        {
            Receiver = car;
        }
    }
    [Serializable()]
    public sealed class BrakeCarModePLCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public BrakeCarModePLCommand(CommandLog log, MSTSWagon car, float BrakeCarModePL)
            : base(log)
        {
            Receiver = car;
        }
    }
    [Serializable()]
    public sealed class TwoPipesConnectionCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public TwoPipesConnectionCommand(CommandLog log, MSTSWagon car, float TwoPipesConnection)
            : base(log)
        {
            Receiver = car;
        }
    }
    [Serializable()]
    public sealed class BrakeCarDeactivateCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public BrakeCarDeactivateCommand(CommandLog log, MSTSWagon car, float BrakeCarDeactivate)
            : base(log)
        {
            Receiver = car;
        }
    }
    [Serializable()]
    public sealed class TogglePantoActivationSwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantoActivationSwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.TogglePantoActivationSwitchUp();
        }
    }
    [Serializable()]
    public sealed class TogglePantoActivationSwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantoActivationSwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TogglePantoActivationSwitchDown();
        }
    }
    [Serializable()]
    public sealed class ToggleVoltageSelectionSwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleVoltageSelectionSwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleVoltageSelectionSwitchUp();
        }
    }
    [Serializable()]
    public sealed class ToggleVoltageSelectionSwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleVoltageSelectionSwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleVoltageSelectionSwitchDown();
        }
    }
    [Serializable()]
    public sealed class ToggleHV3NASwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV3NASwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleHV3NASwitchUp();
        }
    }
    [Serializable()]
    public sealed class ToggleHV3NASwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV3NASwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleHV3NASwitchDown();
        }
    }
    [Serializable()]
    public sealed class ToggleARRConfirmButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleARRConfirmButtonCommand(CommandLog log)
            : base(log)
        {
        }
    }
    [Serializable()]
    public sealed class ToggleARRDriveOutButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleARRDriveOutButtonCommand(CommandLog log)
            : base(log)
        {
        }
    }
    [Serializable()]
    public sealed class ToggleARRParkingButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleARRParkingButtonCommand(CommandLog log)
            : base(log)
        {
        }
    }
    [Serializable()]
    public sealed class ToggleHV2SwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV2SwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleHV2SwitchUp();
        }
    }
    [Serializable()]
    public sealed class ToggleHV3SwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV3SwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleHV3SwitchUp();
        }
    }
    [Serializable()]
    public sealed class ToggleHV3SwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV3SwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleHV3SwitchDown();
        }
    }
    [Serializable()]
    public sealed class ToggleHV4SwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV4SwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleHV4SwitchUp();
        }
    }
    [Serializable()]
    public sealed class ToggleHV4SwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV4SwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleHV4SwitchDown();
        }
    }

    [Serializable()]
    public sealed class ToggleHV5SwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV5SwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleHV5SwitchUp();
        }
    }
    [Serializable()]
    public sealed class ToggleHV5SwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHV5SwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleHV5SwitchDown();
        }
    }


    [Serializable()]
    public sealed class TogglePantograph3SwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantograph3SwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.TogglePantograph3SwitchUp();
        }
    }
    [Serializable()]
    public sealed class TogglePantograph3SwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantograph3SwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TogglePantograph3SwitchDown();
        }
    }
    [Serializable()]
    public sealed class TogglePantograph4SwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantograph4SwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.TogglePantograph4SwitchUp();
            Receiver.TogglePantograph4NCSwitchUp();
        }
    }
    [Serializable()]
    public sealed class TogglePantograph4SwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantograph4SwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TogglePantograph4SwitchDown();
            Receiver.TogglePantograph4NCSwitchDown();
        }
    }
    [Serializable()]
    public sealed class TogglePantograph5SwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantograph5SwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.TogglePantograph5SwitchUp();
        }
    }
    [Serializable()]
    public sealed class TogglePantograph5SwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePantograph5SwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TogglePantograph5SwitchDown();
        }
    }
    [Serializable()]
    public sealed class ToggleCompressorCombinedSwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCompressorCombinedSwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleCompressorCombinedSwitchUp();
            Receiver.ToggleCompressorOffAutoOnSwitchUp();
        }
    }
    [Serializable()]
    public sealed class ToggleCompressorCombinedSwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCompressorCombinedSwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCompressorCombinedSwitchDown();
            Receiver.ToggleCompressorOffAutoOnSwitchDown();
        }
    }
    [Serializable()]
    public sealed class ToggleCompressorCombinedSwitch2UpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCompressorCombinedSwitch2UpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleCompressorCombinedSwitch2Up();
            Receiver.ToggleCompressorOffAutoOnSwitch2Up();
        }
    }
    [Serializable()]
    public sealed class ToggleCompressorCombinedSwitch2DownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCompressorCombinedSwitch2DownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCompressorCombinedSwitch2Down();
            Receiver.ToggleCompressorOffAutoOnSwitch2Down();
        }
    }
    [Serializable()]
    public sealed class ToggleAuxCompressorMode_OffOnCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAuxCompressorMode_OffOnCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAuxCompressorMode_OffOn();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleCompressorMode_OffAutoCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCompressorMode_OffAutoCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCompressorMode_OffAuto();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleCompressorMode2_OffAutoCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCompressorMode2_OffAutoCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCompressorMode2_OffAuto();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleHeating_OffOnCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleHeating_OffOnCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleHeating_OffOn();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleCabHeating_OffOnCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleCabHeating_OffOnCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleCabHeating_OffOn();
            // Report();
        }
    }
    [Serializable()]
    public sealed class HeatingCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public HeatingCommand(CommandLog log, MSTSWagon car, float Heating)
            : base(log)
        {
            Receiver = car;
        }
    }
    [Serializable()]
    public sealed class LeftDoorCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public LeftDoorCommand(CommandLog log, MSTSWagon car, float LeftDoor)
            : base(log)
        {
            Receiver = car;
        }
    }
    [Serializable()]
    public sealed class RightDoorCommand : Command
    {
        public static MSTSWagon Receiver { get; set; }

        public RightDoorCommand(CommandLog log, MSTSWagon car, float RightDoor)
            : base(log)
        {
            Receiver = car;
        }
    }
    [Serializable()]
    public sealed class ToggleControlRouteVoltageCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleControlRouteVoltageCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleControlRouteVoltage();
        }
    }
    [Serializable()]
    public sealed class ToggleQuickReleaseButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleQuickReleaseButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleQuickReleaseButton(true);
        }
    }
    [Serializable()]
    public sealed class ToggleLowPressureReleaseButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLowPressureReleaseButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLowPressureReleaseButton(true);
        }
    }
    [Serializable()]
    public sealed class ToggleBreakPowerButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleBreakPowerButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleBreakPowerButton(true);
        }
    }
    [Serializable()]
    public sealed class ToggleLapButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLapButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLapButton(true);
        }
    }
    [Serializable()]
    public sealed class ToggleBreakEDBButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleBreakEDBButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleBreakEDBButton(true);
        }
    }
    [Serializable()]
    public sealed class ToggleDieselDirectionControllerUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleDieselDirectionControllerUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {

            Receiver.ToggleDieselDirectionControllerUp();
        }
    }
    [Serializable()]
    public sealed class ToggleDieselDirectionControllerDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleDieselDirectionControllerDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleDieselDirectionControllerDown();
        }
    }
    [Serializable()]
    public sealed class ToggleDieselDirectionControllerInOutCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleDieselDirectionControllerInOutCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleDieselDirectionControllerInOut();
        }
    }
    [Serializable()]
    public sealed class ToggleRDSTBreakerCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleRDSTBreakerCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleRDSTBreaker();
        }
    }
    [Serializable()]
    public sealed class ToggleRefreshWorldCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleRefreshWorldCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleRefreshWorld(true);
        }
    }
    [Serializable()]
    public sealed class ToggleRefreshWireCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleRefreshWireCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleRefreshWire(true);
        }
    }
    [Serializable()]
    public sealed class ToggleRefreshCabCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleRefreshCabCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleRefreshCab(true);
        }
    }
    // Lights
    [Serializable()]
    public sealed class ToggleLightFrontLUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightFrontLUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightFrontLUp();
        }
    }
    [Serializable()]
    public sealed class ToggleLightFrontLDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightFrontLDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightFrontLDown();
        }
    }
    [Serializable()]
    public sealed class ToggleLightFrontRUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightFrontRUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightFrontRUp();
        }
    }
    [Serializable()]
    public sealed class ToggleLightFrontRDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightFrontRDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightFrontRDown();
        }
    }
    [Serializable()]
    public sealed class ToggleLightRearLUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightRearLUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightRearLUp();
        }
    }
    [Serializable()]
    public sealed class ToggleLightRearLDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightRearLDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightRearLDown();
        }
    }
    [Serializable()]
    public sealed class ToggleLightRearRUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightRearRUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightRearRUp();
        }
    }
    [Serializable()]
    public sealed class ToggleLightRearRDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleLightRearRDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleLightRearRDown();
        }
    }
    // End of Lights

    [Serializable()]
    public sealed class ToggleSeasonSwitchCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleSeasonSwitchCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleSeasonSwitch();
        }
    }
    [Serializable()]
    public sealed class ToggleMirerControllerUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleMirerControllerUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleMirerControllerUp();
        }
    }
    [Serializable()]
    public sealed class ToggleMirerControllerDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleMirerControllerDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleMirerControllerDown();
        }
    }

    [Serializable()]
    public sealed class TogglePowerKeyUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePowerKeyUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TogglePowerKeyUp();
            // Report();
        }
    }
    [Serializable()]
    public sealed class TogglePowerKeyDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public TogglePowerKeyDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TogglePowerKeyDown();
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleVentilationUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleVentilationUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleVentilationUp();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleVentilationDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleVentilationDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleVentilationDown();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleAutoDriveButtonCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAutoDriveButtonCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAutoDriveButton(true);
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleAutoDriveSpeedSelectorUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAutoDriveSpeedSelectorUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAutoDriveSpeedSelectorUp();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleAutoDriveSpeedSelectorDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAutoDriveSpeedSelectorDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAutoDriveSpeedSelectorDown();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleAxleCounterUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAxleCounterUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAxleCounterUp();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleAxleCounterDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAxleCounterDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAxleCounterDown();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleAxleCounterConfirmerCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAxleCounterConfirmerCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAxleCounterConfirmer();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleAxleCounterRestrictedSpeedZoneActiveCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleAxleCounterRestrictedSpeedZoneActiveCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ToggleAxleCounterRestrictedSpeedZoneActive(true);
            // Report();
        }
    }
    [Serializable()]
    public sealed class Horn2Command : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public Horn2Command(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ManualHorn2 = ToState;
            Receiver.Mirel.ResetVigilance();
            if (ToState)
            {
                Receiver.AlerterReset(TCSEvent.HornActivated);
                Receiver.Simulator.HazzardManager.Horn();
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "sound" : "off");
        }
    }
    [Serializable()]
    public sealed class Horn12Command : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public Horn12Command(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.ManualHorn12 = ToState;
            Receiver.Mirel.ResetVigilance();
            if (ToState)
            {
                Receiver.AlerterReset(TCSEvent.HornActivated);
                Receiver.Simulator.HazzardManager.Horn();
            }
        }

        public override string ToString()
        {
            return base.ToString() + " " + (ToState ? "sound" : "off");
        }
    }
    [Serializable()]
    public sealed class PlayerLocomotiveHandbrakeCommand : BooleanCommand
    {
        public static MSTSLocomotive Receiver { get; set; }

        public PlayerLocomotiveHandbrakeCommand(CommandLog log, bool toState)
            : base(log, toState)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SetPlayerLocomotiveHandbrake(ToState);
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + " - " + (ToState ? "apply" : "release");
        }
    }
    [Serializable()]
    public sealed class ToggleTractionSwitchUpCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleTractionSwitchUpCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TractionSwitchUp();
            // Report();
        }
    }
    [Serializable()]
    public sealed class ToggleTractionSwitchDownCommand : Command
    {
        public static MSTSLocomotive Receiver { get; set; }

        public ToggleTractionSwitchDownCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.TractionSwitchDown();
            // Report();
        }
    }
}
