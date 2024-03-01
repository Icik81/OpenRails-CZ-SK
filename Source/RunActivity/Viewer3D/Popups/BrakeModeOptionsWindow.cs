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
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using ORTS.Common;
using System;
using System.Windows.Forms;

namespace Orts.Viewer3D.Popups
{
    public class BrakeModeOptionsWindow : Window
    {
        readonly Viewer Viewer;

        public BrakeModeOptionsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 20, Window.DecorationSize.Y + (owner.TextFontDefault.Height + 2) * 6 + ControlLayout.SeparatorSize * 3, Viewer.Catalog.GetString("Car Brake Mode"))
        {
            Viewer = owner.Viewer;
        }

        float AllCarChangeTimer1;
        float AllCarChangeTimer2;        
        int CarID;
        int preCarID;
        protected override ControlLayout Layout(ControlLayout layout)
        {            
            CarID = Viewer.CarOperationsWindow.CarPosition;
            
            if (CarID >= Viewer.PlayerTrain.Cars.Count)
                CarID = Viewer.PlayerTrain.Cars.Count - 1;

            Label ID, buttonBrakeCarMode, buttonBrakeCarModeAll ,buttonBrakeCarModePL, buttonBrakeCarModePLAll, buttonClose;
            var vbox = base.Layout(layout).AddLayoutVertical();
            var vbox2 = base.Layout(layout).AddLayoutVertical();

            vbox.Add(ID = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car ID") + "  " + (CarID >= Viewer.PlayerTrain.Cars.Count ? " " : Viewer.PlayerTrain.Cars[CarID].CarID), LabelAlignment.Center));
            ID.Color = Color.Yellow;
            vbox2.Add(ID = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString(""), LabelAlignment.Right));

            vbox.AddHorizontalSeparator(); vbox2.AddHorizontalSeparator();
            vbox.Add(buttonBrakeCarMode = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, "         " + Viewer.Catalog.GetString("Brake Mode G/P/R/R+Mg"), LabelAlignment.Left));
            vbox2.Add(buttonBrakeCarMode = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Set") + ": " + (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeText + "      ", LabelAlignment.Right));
            buttonBrakeCarMode.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator(); vbox2.AddHorizontalSeparator();
            if (AllCarChangeTimer1 > 0)
            {
                AllCarChangeTimer1 -= Viewer.Simulator.OneSecondLoop;
                vbox.Add(buttonBrakeCarModeAll = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Setting..."), LabelAlignment.Center));
                buttonBrakeCarModeAll.Color = Color.LightGreen;
                vbox2.Add(buttonBrakeCarModeAll = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString(""), LabelAlignment.Right));
                if (AllCarChangeTimer1 <= 0)
                {
                    for (int i = 0; i < Viewer.PlayerTrain.Cars.Count; i++)
                    {
                        if (!(Viewer.PlayerTrain.Cars[i] is MSTSLocomotive))
                        {
                            if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).WagonType == TrainCar.WagonTypes.Freight)
                            {
                                if ((Viewer.PlayerTrain.Cars[preCarID] as MSTSWagon).BrakeSystem.BrakeCarMode > 1)
                                {
                                    goto SkipChangeBrakeMode;
                                }
                            }
                            if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).WagonType == TrainCar.WagonTypes.Passenger)
                            {
                                if ((Viewer.PlayerTrain.Cars[preCarID] as MSTSWagon).BrakeSystem.BrakeCarMode == 0)
                                {
                                    goto SkipChangeBrakeMode;
                                }
                            }

                            (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarMode = (Viewer.PlayerTrain.Cars[preCarID] as MSTSWagon).BrakeSystem.BrakeCarMode;

                            if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarMode == 0)
                            {
                                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode G"));
                                (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModeText = "G";
                            }
                            if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarMode == 1)
                            {
                                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode P"));
                                (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModeText = "P";
                            }
                            if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarMode == 2)
                            {
                                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode R"));
                                (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModeText = "R";
                            }
                            if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarMode == 3)
                            {
                                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode R+Mg"));
                                (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModeText = "R+Mg";
                            }
                        SkipChangeBrakeMode: continue;
                        }
                    }
                }
            }
            else
            {
                vbox.Add(buttonBrakeCarModeAll = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Brake Mode") + " " + (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeText + " " + Viewer.Catalog.GetString("for all wagons"), LabelAlignment.Center));
                vbox2.Add(buttonBrakeCarModeAll = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString(""), LabelAlignment.Right));
            }

            // Vůz je nákladní a není možný režim R+Mg
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.WagonType == 4 && !(Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.AutoLoadRegulatorEquipped)
            {
                vbox.AddHorizontalSeparator(); vbox2.AddHorizontalSeparator();
                vbox.Add(buttonBrakeCarModePL = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, "   " + Viewer.Catalog.GetString("Car Mode Empty/Loaded"), LabelAlignment.Left));
                vbox2.Add(buttonBrakeCarModePL = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Set") + ": " + (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL + "   ", LabelAlignment.Right));
                buttonBrakeCarModePL.Color = Color.LightGreen;                
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 2;
            }
            // Ostatní vozy
            else
            {
                vbox.AddHorizontalSeparator();
                vbox.Add(buttonBrakeCarModePL = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("N/A"), LabelAlignment.Center));
                buttonBrakeCarModePL.Color = Color.Gray;
                vbox2.Add(buttonBrakeCarModePL = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString(""), LabelAlignment.Right));                
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 4;
                if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.AutoLoadRegulatorEquipped)
                    (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.NumberBrakeCarMode = 2;
            }

            vbox.AddHorizontalSeparator();
            if (AllCarChangeTimer2 > 0)
            {
                AllCarChangeTimer2 -= Viewer.Simulator.OneSecondLoop;
                vbox.Add(buttonBrakeCarModePLAll = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Setting..."), LabelAlignment.Center));
                buttonBrakeCarModePLAll.Color = Color.LightGreen;
                vbox2.Add(buttonBrakeCarModePLAll = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString(""), LabelAlignment.Right));
                if (AllCarChangeTimer2 <= 0)
                {
                    for (int i = 0; i < Viewer.PlayerTrain.Cars.Count; i++)
                    {
                        if (!(Viewer.PlayerTrain.Cars[i] is MSTSLocomotive))
                        {
                            if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).WagonType == TrainCar.WagonTypes.Freight)
                            {
                                (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModePL = (Viewer.PlayerTrain.Cars[preCarID] as MSTSWagon).BrakeSystem.BrakeCarModePL;

                                if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModePL == 0)
                                {
                                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Empty Car"));
                                    (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = Viewer.Catalog.GetString("Empty");
                                }
                                if ((Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModePL == 1)
                                {
                                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Loaded Car"));
                                    (Viewer.PlayerTrain.Cars[i] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = Viewer.Catalog.GetString("Loaded");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                vbox.Add(buttonBrakeCarModePLAll = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car Mode") + " " + (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL + " " + Viewer.Catalog.GetString("for all wagons"), LabelAlignment.Center));
                if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).WagonType != TrainCar.WagonTypes.Freight)
                    buttonBrakeCarModePLAll.Color = Color.Gray;
                else
                    buttonBrakeCarModePLAll.Color = Color.White;
                vbox2.Add(buttonBrakeCarModePLAll = new Label(vbox2.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString(""), LabelAlignment.Right));
            }

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));
            buttonBrakeCarMode.Click += new Action<Control, Point>(buttonBrakeCarMode_Click);
            buttonBrakeCarModeAll.Click += new Action<Control, Point>(buttonBrakeCarModeAll_Click);
            buttonBrakeCarModePL.Click += new Action<Control, Point>(buttonBrakeCarModePL_Click);
            buttonBrakeCarModePLAll.Click += new Action<Control, Point>(buttonBrakeCarModePLAll_Click);
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

        void buttonBrakeCarMode_Click(Control arg1, Point arg2)
        {            
            new BrakeCarModeCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarMode += 1);

            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarMode > (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.NumberBrakeCarMode - 1)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarMode = 0;

            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarMode == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode G"));
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeText = "G";
            }
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarMode == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode P"));
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeText = "P";
            }
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarMode == 2)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode R"));
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeText = "R";
            }
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarMode == 3)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Car Mode R+Mg"));
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeText = "R+Mg";
            }
        }
        void buttonBrakeCarModeAll_Click(Control arg1, Point arg2)
        {
            if (AllCarChangeTimer1 > 0 || AllCarChangeTimer2 > 0) return;
            AllCarChangeTimer1 = Viewer.PlayerTrain.Cars.Count * 0.02f;
            preCarID = CarID;
        }
        void buttonBrakeCarModePL_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).WagonType != TrainCar.WagonTypes.Freight)
                return;

            new BrakeCarModePLCommand(Viewer.Log, (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon), (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModePL += 1);

            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModePL > 1) (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModePL = 0;

            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModePL == 0)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Empty Car"));
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = Viewer.Catalog.GetString("Empty");
            }
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModePL == 1)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Loaded Car"));
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.BrakeCarModeTextPL = Viewer.Catalog.GetString("Loaded");
            }
        }
        void buttonBrakeCarModePLAll_Click(Control arg1, Point arg2)
        {
            if (AllCarChangeTimer1 > 0 || AllCarChangeTimer2 > 0) return;
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.WagonType == 4 && !(Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).BrakeSystem.AutoLoadRegulatorEquipped)
            {
                AllCarChangeTimer2 = Viewer.PlayerTrain.Cars.Count * 0.02f;
                preCarID = CarID;
            }
        }
    }
}
