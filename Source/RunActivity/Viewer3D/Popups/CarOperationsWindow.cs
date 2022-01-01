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
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
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

        public CarOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 22, Window.DecorationSize.Y + (owner.TextFontDefault.Height + 2) * 16 + ControlLayout.SeparatorSize * 9, Viewer.Catalog.GetString("*** Car Operation Menu ***"))
        {
            Viewer = owner.Viewer;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label ID, buttonHandbrake, buttonTogglePower, buttonToggleMU, buttonToggleBrakeHose, buttonToggleAngleCockA, buttonToggleAngleCockB, buttonToggleBleedOffValve, buttonBrakeCarMode, buttonBrakeCarModePL, buttonBrakeCarDeactivate, buttonTwoPipesConnection, buttonLeftDoor, buttonRightDoor, buttonHeating, buttonClose;

            var vbox = base.Layout(layout).AddLayoutVertical();
            vbox.Add(ID = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car ID") + "  " + (CarPosition >= Viewer.PlayerTrain.Cars.Count? " " :Viewer.PlayerTrain.Cars[CarPosition].CarID), LabelAlignment.Center));
            ID.Color = Color.Yellow;
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonHandbrake = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Handbrake"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonTogglePower = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Power"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleMU = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle MU Connection"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleBrakeHose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Toggle Brake Hose Connection"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleAngleCockA = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Front Angle Cock"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleAngleCockB = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Rear Angle Cock"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonToggleBleedOffValve = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Open/Close Bleed Off Valve"), LabelAlignment.Center));
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonBrakeCarMode = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Režim vozu G/P/R/R+Mg") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText, LabelAlignment.Center));
            buttonBrakeCarMode.Color = Color.LightGreen;

            // Vůz je nákladní a není možný režim R+Mg
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.WagonType == 4 && !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AutoLoadRegulatorEquipped)
            {
                vbox.AddHorizontalSeparator();
                vbox.Add(buttonBrakeCarModePL = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Režim vozu Prázdný/Ložený ") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL, LabelAlignment.Center));
                buttonBrakeCarModePL.Color = Color.DarkOrange;
                buttonBrakeCarModePL.Click += new Action<Control, Point>(buttonBrakeCarModePL_Click);
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 2;
            }
            // Ostatní vozy
            else
            {
                vbox.AddHorizontalSeparator();
                vbox.Add(buttonBrakeCarModePL = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("N/A"), LabelAlignment.Center));
                buttonBrakeCarModePL.Color = Color.DarkOrange;
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 4;
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.AutoLoadRegulatorEquipped)
                    (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 2;
            }
            
            vbox.AddHorizontalSeparator();            
            vbox.Add(buttonBrakeCarDeactivate = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Brzda vozu") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateText, LabelAlignment.Center));
            buttonBrakeCarDeactivate.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonTwoPipesConnection = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Napájecí hadice") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionText, LabelAlignment.Center));
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonLeftDoor = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Levé dveře") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorText, LabelAlignment.Center));

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonRightDoor = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Pravé dveře") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorText, LabelAlignment.Center));

            vbox.AddHorizontalSeparator();
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).DieselHeaterPower > 0)
            {
                vbox.Add(buttonHeating = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Bufík") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingText, LabelAlignment.Center));
            }
            else
                vbox.Add(buttonHeating = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("El.topení/klimatizace") + "     Nastaveno: " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.HeatingText, LabelAlignment.Center));

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));
            buttonHandbrake.Click += new Action<Control, Point>(buttonHandbrake_Click);
            buttonTogglePower.Click += new Action<Control, Point>(buttonTogglePower_Click);
            buttonToggleMU.Click += new Action<Control, Point>(buttonToggleMU_Click);
            buttonToggleBrakeHose.Click += new Action<Control, Point>(buttonToggleBrakeHose_Click);
            buttonToggleAngleCockA.Click += new Action<Control, Point>(buttonToggleAngleCockA_Click);
            buttonToggleAngleCockB.Click += new Action<Control, Point>(buttonToggleAngleCockB_Click);
            buttonToggleBleedOffValve.Click += new Action<Control, Point>(buttonToggleBleedOffValve_Click);
            buttonBrakeCarMode.Click += new Action<Control, Point>(buttonBrakeCarMode_Click);
            buttonBrakeCarDeactivate.Click += new Action<Control, Point>(buttonBrakeCarDeactivate_Click);
            buttonTwoPipesConnection.Click += new Action<Control, Point>(buttonTwoPipesConnection_Click);
            buttonLeftDoor.Click += new Action<Control, Point>(buttonLeftDoor_Click);
            buttonRightDoor.Click += new Action<Control, Point>(buttonRightDoor_Click);
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
            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive)))
            {
                new PowerCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerOn);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).PowerOn)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power OFF command sent"));
                else
                {
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Power ON command sent"));
                    if (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                        (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).HVOn = true;
                }
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No power command for this type of car!"));
        }

        void buttonToggleMU_Click(Control arg1, Point arg2)
        {

            if ((Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSElectricLocomotive))
                ||
              (Viewer.PlayerTrain.Cars[CarPosition].GetType() == typeof(MSTSDieselLocomotive)))
            {
                new ToggleMUCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive), !(Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptMUSignals);
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).AcceptMUSignals)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal connected"));
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("MU signal disconnected"));
            }
            else
                Viewer.Simulator.Confirmer.Warning(Viewer.Catalog.GetString("No MU command for this type of car!"));
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
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Režim vozu G"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText = "G";
    }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Režim vozu P"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText = "P";
}
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode == 2)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Režim vozu R"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeText = "R";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarMode == 3)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Režim vozu R+Mg"));
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
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Prázdný vůz"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = "Prázdný";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModePL == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Ložený vůz"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = "Ložený";
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
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Napájecí hadice odpojeny"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionText = "odpojeny";                
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Napájecí hadice zapojeny"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.TwoPipesConnectionText = "zapojeny";
            }
        }

        void buttonBrakeCarDeactivate_Click (Control arg1, Point arg2)
        {
            new BrakeCarDeactivateCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Brzda vozu zapnuta"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateText = "zapnuta";
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivate = false;
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Brzda vozu vypnuta"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivateText = "vypnuta";
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.BrakeCarDeactivate = true;
            }
        }

        void buttonLeftDoor_Click(Control arg1, Point arg2)
        {
            new LeftDoorCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu += 1);

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Levé dveře zavřeno"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorText = "zavřeno";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorMenu == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Levé dveře otevřeno"));
                (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorText = "otevřeno";
            }
            (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.LeftDoorCycle = 0;
        }

        void buttonRightDoor_Click(Control arg1, Point arg2)
        {
            new RightDoorCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu += 1);
            
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu > 1) (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu = 0;

            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Pravé dveře zavřeno"));
                //(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorText = "zavřeno";
            }
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorMenu == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Pravé dveře otevřeno"));
                //(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorText = "otevřeno";
            }
            (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).BrakeSystem.RightDoorCycle = 0;
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
