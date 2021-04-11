// COPYRIGHT 2015 by the Open Rails project.
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
using System;
using System.Collections.Generic;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Viewer3D.RollingStock;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.RollingStock.SubSystems
{
    public class CruiseControlViewer
    {
        MSTSLocomotiveViewer MSTSLocomotiveViewer;
        MSTSLocomotive Locomotive;
        CruiseControl CruiseControl;
        public CruiseControlViewer(MSTSLocomotiveViewer locomotiveViewer, MSTSLocomotive locomotive, CruiseControl cruiseControl)
        {
            MSTSLocomotiveViewer = locomotiveViewer;
            Locomotive = locomotive;
            CruiseControl = cruiseControl;
        }

        public void InitializeUserInputCommands()
        {
            var UserInputCommands = MSTSLocomotiveViewer.UserInputCommands;
            var Noop = MSTSLocomotiveViewer.Noop;
            UserInputCommands.Add(UserCommand.ControlSpeedRegulatorMaxAccelerationDecrease, new Action[] { () => CruiseControl.SpeedRegulatorMaxForceStopDecrease(), () => CruiseControl.SpeedRegulatorMaxForceStartDecrease() });
            UserInputCommands.Add(UserCommand.ControlSpeedRegulatorMaxAccelerationIncrease, new Action[] { () => CruiseControl.SpeedRegulatorMaxForceStopIncrease(), () => CruiseControl.SpeedRegulatorMaxForceStartIncrease() });
            UserInputCommands.Add(UserCommand.ControlSpeedRegulatorModeDecrease, new Action[] { Noop, () => CruiseControl.SpeedRegulatorModeDecrease() });
            UserInputCommands.Add(UserCommand.ControlSpeedRegulatorModeIncrease, new Action[] { Noop, () => CruiseControl.SpeedRegulatorModeIncrease() });
            UserInputCommands.Add(UserCommand.ControlSpeedRegulatorSelectedSpeedDecrease, new Action[] { () => CruiseControl.SpeedRegulatorSelectedSpeedStopDecrease(), () => CruiseControl.SpeedRegulatorSelectedSpeedStartDecrease() });
            UserInputCommands.Add(UserCommand.ControlSpeedRegulatorSelectedSpeedIncrease, new Action[] { () => CruiseControl.SpeedRegulatorSelectedSpeedStopIncrease(), () => CruiseControl.SpeedRegulatorSelectedSpeedStartIncrease() });
            UserInputCommands.Add(UserCommand.ControlNumberOfAxlesDecrease, new Action[] { Noop, () => CruiseControl.NumberOfAxlesDecrease() });
            UserInputCommands.Add(UserCommand.ControlNumberOfAxlesIncrease, new Action[] { Noop, () => CruiseControl.NumerOfAxlesIncrease() });
            UserInputCommands.Add(UserCommand.ControlRestrictedSpeedZoneActive, new Action[] { Noop, () => CruiseControl.ActivateRestrictedSpeedZone() });
            UserInputCommands.Add(UserCommand.ControlCruiseControlModeIncrease, new Action[] { () => CruiseControl.SpeedSelectorModeStopIncrease(), () => CruiseControl.SpeedSelectorModeStartIncrease() });
            UserInputCommands.Add(UserCommand.ControlCruiseControlModeDecrease, new Action[] { Noop, () => CruiseControl.SpeedSelectorModeDecrease() });
            UserInputCommands.Add(UserCommand.ControlTrainTypePaxCargo, new Action[] { Noop, () => Locomotive.ChangeTrainTypePaxCargo() });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed10, new Action[] { Noop, () => CruiseControl.SetSpeed(10) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed20, new Action[] { Noop, () => CruiseControl.SetSpeed(20) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed30, new Action[] { Noop, () => CruiseControl.SetSpeed(30) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed40, new Action[] { Noop, () => CruiseControl.SetSpeed(40) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed50, new Action[] { Noop, () => CruiseControl.SetSpeed(50) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed60, new Action[] { Noop, () => CruiseControl.SetSpeed(60) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed70, new Action[] { Noop, () => CruiseControl.SetSpeed(70) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed80, new Action[] { Noop, () => CruiseControl.SetSpeed(80) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed90, new Action[] { Noop, () => CruiseControl.SetSpeed(90) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed100, new Action[] { Noop, () => CruiseControl.SetSpeed(100) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed110, new Action[] { Noop, () => CruiseControl.SetSpeed(110) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed120, new Action[] { Noop, () => CruiseControl.SetSpeed(120) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed130, new Action[] { Noop, () => CruiseControl.SetSpeed(130) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed140, new Action[] { Noop, () => CruiseControl.SetSpeed(140) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed150, new Action[] { Noop, () => CruiseControl.SetSpeed(150) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed160, new Action[] { Noop, () => CruiseControl.SetSpeed(160) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed170, new Action[] { Noop, () => CruiseControl.SetSpeed(170) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed180, new Action[] { Noop, () => CruiseControl.SetSpeed(180) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed190, new Action[] { Noop, () => CruiseControl.SetSpeed(190) });
            UserInputCommands.Add(UserCommand.ControlSelectSpeed200, new Action[] { Noop, () => CruiseControl.SetSpeed(200) });
        }
    }
}
