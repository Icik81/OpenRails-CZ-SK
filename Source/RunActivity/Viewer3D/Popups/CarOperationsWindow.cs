// COPYRIGHT 2013, 2014, 2015 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Simulation;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using ORTS.Common;
using System;

namespace Orts.Viewer3D.Popups
{
    public class CarOperationsWindow : Window
    {
        readonly Viewer Viewer;

        public int CarPosition
        {
            set;
            get;
        }

        public bool HelperOptionsOpened;

        public CarOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 23, Window.DecorationSize.Y + (owner.TextFontDefault.Height + 2) * 21 + ControlLayout.SeparatorSize * 12, Viewer.Catalog.GetString("*** Car Operation Menu ***"))
        {
            Viewer = owner.Viewer;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label ID, buttonLocoChange, buttonHandbrake, buttonTogglePower, buttonToggleMUCable, buttonToggleMUPower, buttonToggleHelper, buttonToggleHelperOptions, buttonToggleBrakeHose, buttonToggleAngleCockA, buttonToggleAngleCockB, buttonToggleBleedOffValve, buttonBrakeCarMode, buttonBrakeCarModePL, buttonBrakeCarDeactivate, buttonTwoPipesConnection, buttonLeftDoor, buttonRightDoor, buttonNoPaxsMode, buttonHeating, buttonClose;

            var vbox = base.Layout(layout).AddLayoutVertical();

            if (CarPosition >= Viewer.PlayerTrain.Cars.Count)
                CarPosition = Viewer.PlayerTrain.Cars.Count - 1;

            if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSLocomotive)
                vbox.Add(ID = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car ID ") + "  " + (CarPosition >= Viewer.PlayerTrain.Cars.Count ? " " : Viewer.PlayerTrain.Cars[CarPosition].CarID)
                + "    " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).LocomotiveName, LabelAlignment.Center));
            else            
                vbox.Add(ID = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car ID ") + "  " + (CarPosition >= Viewer.PlayerTrain.Cars.Count ? " " : Viewer.PlayerTrain.Cars[CarPosition].CarID)
                + "    " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonName, LabelAlignment.Center));
            ID.Color = Color.Yellow;

            vbox.AddHorizontalSeparator();
            if (!(Viewer.PlayerTrain.Cars[CarPosition] is MSTSLocomotive))
                vbox.Add(buttonLocoChange = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("N/A"), LabelAlignment.Center));
            else
                vbox.Add(buttonLocoChange = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Togle Locomotive Station"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).CarIsPlayerLoco)
                buttonLocoChange.Color = Color.Yellow;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonHandbrake = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Handbrake"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus())
                buttonHandbrake.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) == null || (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).IsLeadLocomotive())
                vbox.Add(buttonTogglePower = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("N/A"), LabelAlignment.Center));
            else
            {
                vbox.Add(buttonTogglePower = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Power"), LabelAlignment.Center));
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) != null && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerOn)
                    buttonTogglePower.Color = Color.LightGreen;
            }

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleMUCable = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Cable MU"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) != null && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptCableSignals)
                buttonToggleMUCable.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleMUPower = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Power MU"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) != null && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptPowerSignals)
                buttonToggleMUPower.Color = Color.LightGreen;            
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleHelper = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Connect/Unmount Helper"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) != null && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptHelperSignals)
                buttonToggleHelper.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleHelperOptions = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Helper Options"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) != null && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).HelperOptionsOpened)
                buttonToggleHelperOptions.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleBrakeHose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Brake Hose Connection"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected)
                buttonToggleBrakeHose.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();            
            vbox.Add(buttonToggleAngleCockA = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Front Angle Cock"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen)
                buttonToggleAngleCockA.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleAngleCockB = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Rear Angle Cock"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen)
                buttonToggleAngleCockB.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleBleedOffValve = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Bleed Off Valve"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen)
                buttonToggleBleedOffValve.Color = Color.LightGreen;
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonBrakeCarMode = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Brake Mode G/P/R/R+Mg") + "      " + Viewer.Catalog.GetString("Set") + ": " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText, LabelAlignment.Center));

            // Vůz je nákladní a není možný režim R+Mg
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.WagonType == 4 && !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AutoLoadRegulatorEquipped)
            {
                vbox.AddHorizontalSeparator();
                vbox.Add(buttonBrakeCarModePL = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car Mode Empty/Loaded") + "      " + Viewer.Catalog.GetString("Set") + ": " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL, LabelAlignment.Center));
                buttonBrakeCarModePL.Click += new Action<Control, Point>(buttonBrakeCarModePL_Click);
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 2;
            }
            // Ostatní vozy
            else
            {
                vbox.AddHorizontalSeparator();
                vbox.Add(buttonBrakeCarModePL = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("N/A"), LabelAlignment.Center));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 4;
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AutoLoadRegulatorEquipped)
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 2;
            }

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonBrakeCarDeactivate = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car Brake"), LabelAlignment.Center));
            if (!(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivate)
                buttonBrakeCarDeactivate.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonTwoPipesConnection = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Air Twin Pipe Hoses"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnection)
                buttonTwoPipesConnection.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonLeftDoor = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Left Door"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorIsOpened)
                buttonLeftDoor.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonRightDoor = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Right Door"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorIsOpened)
                buttonRightDoor.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonNoPaxsMode = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("No Pax`s mode"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).NoPaxsMode)
                buttonNoPaxsMode.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonHasSteamHeating || (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon) is MSTSSteamLocomotive)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonHasStove)
                {
                    vbox.Add(buttonHeating = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Stove Heating"), LabelAlignment.Center));
                    if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingIsOn)
                        buttonHeating.Color = Color.LightGreen;
                }
                else
                    vbox.Add(buttonHeating = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Steam Heating"), LabelAlignment.Center));                                    
            }
            else
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).DieselHeaterPower > 0)
                    vbox.Add(buttonHeating = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Diesel Heating"), LabelAlignment.Center));
                else
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).DieselHeaterPower == 0 
                    && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonType == TrainCar.WagonTypes.Engine 
                    && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).EngineType != TrainCar.EngineTypes.Electric)
                    vbox.Add(buttonHeating = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Calorifer Heating"), LabelAlignment.Center));
                else
                    vbox.Add(buttonHeating = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Electric Heating/Air"), LabelAlignment.Center));
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingIsOn)
                    buttonHeating.Color = Color.LightGreen;
            }
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));
            buttonLocoChange.Click += new Action<Control, Point>(buttonLocoChange_Click);
            buttonHandbrake.Click += new Action<Control, Point>(buttonHandbrake_Click);
            buttonTogglePower.Click += new Action<Control, Point>(buttonTogglePower_Click);
            buttonToggleMUCable.Click += new Action<Control, Point>(buttonToggleMUCable_Click);
            buttonToggleMUPower.Click += new Action<Control, Point>(buttonToggleMUPower_Click);
            buttonToggleHelper.Click += new Action<Control, Point>(buttonToggleHelper_Click);
            buttonToggleHelperOptions.Click += new Action<Control, Point>(buttonToggleHelperOptions_Click);
            buttonToggleBrakeHose.Click += new Action<Control, Point>(buttonToggleBrakeHose_Click);
            buttonToggleAngleCockA.Click += new Action<Control, Point>(buttonToggleAngleCockA_Click);
            buttonToggleAngleCockB.Click += new Action<Control, Point>(buttonToggleAngleCockB_Click);
            buttonToggleBleedOffValve.Click += new Action<Control, Point>(buttonToggleBleedOffValve_Click);
            buttonBrakeCarMode.Click += new Action<Control, Point>(buttonBrakeCarMode_Click);
            buttonBrakeCarDeactivate.Click += new Action<Control, Point>(buttonBrakeCarDeactivate_Click);
            buttonTwoPipesConnection.Click += new Action<Control, Point>(buttonTwoPipesConnection_Click);
            buttonLeftDoor.Click += new Action<Control, Point>(buttonLeftDoor_Click);
            buttonRightDoor.Click += new Action<Control, Point>(buttonRightDoor_Click);
            buttonNoPaxsMode.Click += new Action<Control, Point>(buttonNoPaxsMode_Click);
            buttonHeating.Click += new Action<Control, Point>(buttonHeating_Click);
            buttonClose.Click += new Action<Control, Point>(buttonClose_Click);

            return vbox;
        }

        void buttonClose_Click(Control arg1, Point arg2)
        {
            Visible = false;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            var MovingCurrentWindow = UserInput.IsMouseLeftButtonDown &&
                  UserInput.MouseX >= Location.X && UserInput.MouseX <= Location.X + Location.Width &&
                  UserInput.MouseY >= Location.Y && UserInput.MouseY <= Location.Y + Location.Height ?
                  true : false;

            if (!MovingCurrentWindow && updateFull)
            {
                Layout();
            }
            base.PrepareFrame(elapsedTime, updateFull);
        }

        void buttonLocoChange_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) == null || (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).CarIsPlayerLoco)
                return;
            Viewer.Simulator.PlayerLocomotiveChange = true;
            Viewer.Simulator.LeadLocomotiveIndex = CarPosition;            
            Viewer.ChangeCab();
        }

        void buttonHandbrake_Click(Control arg1, Point arg2)
        {
            new WagonHandbrakeCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus());
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).GetTrainHandbrakeStatus())
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake set"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Handbrake off"));
        }

        void buttonTogglePower_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive) != null && (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).IsLeadLocomotive())
                return;

            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSSteamLocomotive)))
            {
                new PowerCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerOn);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerOn)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power OFF command sent"));
                    // Icik
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).Battery = false;
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerKey = false;
                    if (Viewer.PlayerTrain.Cars[CarPosition] as MSTSDieselLocomotive != null)
                        (Viewer.PlayerTrain.Cars[CarPosition] as MSTSDieselLocomotive).StartLooseCon = false;
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).UserPowerOff = true;
                }
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power ON command sent"));
                    // Icik
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).Battery = true;
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerKey = true;
                    if (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                        (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).HVOn = true;
                    if (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive))
                        (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).StartLooseCon = true;
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).UserPowerOff = false;
                }
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No power command for this type of car!"));
        }

        void buttonToggleMUCable_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSSteamLocomotive)))
            {
                new ToggleMUCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptCableSignals);
                if (!(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptCableSignals)
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Cable MU disconnected"));
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptPowerSignals = false;
                }
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Cable MU connected"));
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No Cable MU command for this type of car!"));
        }

        void buttonToggleMUPower_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSSteamLocomotive)))
            {
                ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptPowerSignals) = !((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptPowerSignals);
                if (!(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptPowerSignals)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power MU disconnected"));
                else
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptCableSignals)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power MU connected"));
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No Power MU command for this type of car!"));
        }

        void buttonToggleHelper_Click(Control arg1, Point arg2)
        {
            if (!Viewer.PlayerTrain.Cars[CarPosition].CarIsPlayerLoco
                &&
              ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive)))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSSteamLocomotive)))
            {
                new ToggleHelperCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptHelperSignals);
                if (!(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptHelperSignals)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Helper disconnected"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Helper connected"));
            }
            else            
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No Helper command for this type of car!"));                            
        }
        void buttonToggleHelperOptions_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSSteamLocomotive)))
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptHelperSignals)
                {                                                           
                    HelperOptionsOpened = true;                    
                    Viewer.HelperOptionsWindow.Visible = true;
                }
            }
        }

        void buttonToggleBrakeHose_Click(Control arg1, Point arg2)
        {
            new WagonBrakeHoseConnectCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.FrontBrakeHoseConnected)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose connected"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front brake hose disconnected"));
        }

        void buttonToggleAngleCockA_Click(Control arg1, Point arg2)
        {
            new ToggleAngleCockACommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockAOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Front angle cock closed"));
        }

        void buttonToggleAngleCockB_Click(Control arg1, Point arg2)
        {
            new ToggleAngleCockBCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AngleCockBOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Rear angle cock closed"));
        }

        void buttonToggleBleedOffValve_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe)
                return;

            new ToggleBleedOffValveCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen);
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BleedOffValveOpen)
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve opened"));
            else
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Bleed off valve closed"));
        }

        void buttonBrakeCarMode_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe)
                return;

            new BrakeCarModeCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode > (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.NumberBrakeCarMode - 1)
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode G"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText = "G";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode P"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText = "P";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode == 2)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode R"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText = "R";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode == 3)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode R+Mg"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText = "R+Mg";
            }
        }
        void buttonBrakeCarModePL_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe)
                return;

            new BrakeCarModePLCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModePL += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModePL > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModePL = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModePL == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Empty Car"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = Viewer.Catalog.GetString("Empty");
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModePL == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Loaded Car"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = Viewer.Catalog.GetString("Loaded");
            }
        }

        void buttonTwoPipesConnection_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem is SingleTransferPipe)
                return;

            new TwoPipesConnectionCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionMenu += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionMenu == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Air Twin Pipe Hoses disconnected"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionText = Viewer.Catalog.GetString("disconnect");
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Air Twin Pipe Hoses connected"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionText = Viewer.Catalog.GetString("connect");
            }
        }

        void buttonBrakeCarDeactivate_Click(Control arg1, Point arg2)
        {
            new BrakeCarDeactivateCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Brake On"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateText = Viewer.Catalog.GetString("on");
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivate = false;
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Brake Off"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateText = Viewer.Catalog.GetString("off");
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivate = true;
            }
        }

        void buttonLeftDoor_Click(Control arg1, Point arg2)
        {
            new LeftDoorCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Left Door Closed"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorText = Viewer.Catalog.GetString("closed");
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Left Door Opened"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorText = Viewer.Catalog.GetString("opened");
            }
            (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorCycle = 0;
        }

        void buttonRightDoor_Click(Control arg1, Point arg2)
        {
            new RightDoorCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Right Door Closed"));
                //(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorText = "closed";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Right Door Opened"));
                //(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorText = "otevřeno";
            }
            (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorCycle = 0;
        }

        void buttonNoPaxsMode_Click(Control arg1, Point arg2)
        {
            (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).NoPaxsMode = !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).NoPaxsMode;
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).NoPaxsMode)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("No Pax`s mode"));                
            }
            if (!(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).NoPaxsMode)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Pax`s mode"));                
            }            
        }

        void buttonHeating_Click(Control arg1, Point arg2)
        {
            new HeatingCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingMenu += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingMenu == 0)
            {
                //Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Topení/klimatizace zapnuto"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingIsOn = true;
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingMenu == 1)
            {
                //Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Topení/klimatizace vypnuto"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingIsOn = false;
            }
        }

    }
}
