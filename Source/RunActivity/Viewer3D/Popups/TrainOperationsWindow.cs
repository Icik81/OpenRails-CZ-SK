// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using ORTS.Common;
using System;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class TrainOperationsWindow : Window
    {
        const int CarListPadding = 2;
        internal static Texture2D CouplerTexture;
        Train PlayerTrain;
        int LastPlayerTrainCars;
        bool LastPlayerLocomotiveFlippedState;

        const int TextBoxHeight = 50;

        public TrainOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 22, Window.DecorationSize.Y + CarListPadding + owner.TextFontDefault.Height * TextBoxHeight, Viewer.Catalog.GetString("Train Operations"))
        {
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (CouplerTexture == null)
                // TODO: This should happen on the loader thread.
                CouplerTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCoupler.png"));
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var textHeight = Owner.TextFontDefault.Height;
            var hbox = base.Layout(layout).AddLayoutVertical();
            var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);
            if (PlayerTrain != null)
            {
                int carPosition = 0;                
                scrollbox.Add(new TrainOperationsInfo(textHeight * 22, textHeight + (textHeight / 2), Owner.Viewer, LabelAlignment.Center));
                scrollbox.AddHorizontalSeparator();
                scrollbox.Add(new TrainOperationsBrakePercent(textHeight * 22, textHeight + (textHeight / 2), Owner.Viewer, LabelAlignment.Center));
                scrollbox.AddHorizontalSeparator();

                foreach (var car in PlayerTrain.Cars)
                {
                    var carLabel = new TrainOperationsLabel(textHeight * 22, textHeight + (textHeight / 2), Owner.Viewer, car, carPosition, LabelAlignment.Center);
                    carLabel.Click += new Action<Control, Point>(carLabel_Click);

                    if (car.CarHasBrakePipeConnected)
                        carLabel.Color = Color.White;
                    else
                        carLabel.Color = Color.Gray;

                    car.BrakeCarStatus();
                    if (car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake)
                        carLabel.Color = Color.Red;
                    
                    if (car.BrakeSystem.BrakeCarDeactivate)
                        carLabel.Color = Color.IndianRed;                    

                    //if (car.SelectedCar) carLabel.Color = Color.Yellow;
                    if (car == PlayerTrain.LeadLocomotive)
                        carLabel.Color = Color.GreenYellow;

                    Owner.Viewer.PlayerTrain.Simulator.ChangeCabActivated = false;                    

                    scrollbox.Add(carLabel);
                    if (car != PlayerTrain.Cars.Last())
                        scrollbox.Add(new TrainOperationsCoupler((int)(textHeight * 22 / 2 - (textHeight / 2)), 0, textHeight + (textHeight / 2), Owner.Viewer, car, carPosition));
                    carPosition++;
                }
            }
            return hbox;
        }

        void carLabel_Click(Control arg1, Point arg2)
        {

        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                if (PlayerTrain != Owner.Viewer.PlayerTrain || Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars || (Owner.Viewer.PlayerLocomotive != null &&
                    LastPlayerLocomotiveFlippedState != Owner.Viewer.PlayerLocomotive.Flipped)
                    // Icik
                    || Owner.Viewer.PlayerTrain.Simulator.ChangeCabActivated
                    || Owner.Viewer.PlayerTrain.PlayerTrainBrakePercentChange
                    )
                {
                    Owner.Viewer.PlayerTrain.PlayerTrainBrakePercentChange = false;
                    PlayerTrain = Owner.Viewer.PlayerTrain;
                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    if (Owner.Viewer.PlayerLocomotive != null) LastPlayerLocomotiveFlippedState = Owner.Viewer.PlayerLocomotive.Flipped;
                    Layout();
                }
            }
        }
    }

    class TrainOperationsCoupler : Image
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public TrainOperationsCoupler(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;
            Texture = TrainOperationsWindow.CouplerTexture;
            Source = new Rectangle(0, 0, size, size);
            Click += new Action<Control, Point>(TrainOperationsCoupler_Click);
        }

        void TrainOperationsCoupler_Click(Control arg1, Point arg2)
        {
            if (Viewer.Simulator.TimetableMode)
            {
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("In Timetable Mode uncoupling using this window is not allowed"));
            }
            else
            {
                new UncoupleCommand(Viewer.Log, CarPosition);
                Viewer.Simulator.CarByUserUncoupled = true;
                if (Viewer.CarOperationsWindow.CarPosition > CarPosition)
                    Viewer.CarOperationsWindow.Visible = false;
            }
        }
    }

    class TrainOperationsLabel : Label
    {
        readonly Viewer Viewer;
        readonly int CarPosition;

        public TrainOperationsLabel(int x, int y, Viewer viewer, TrainCar car, int carPosition, LabelAlignment alignment)
            : base(x, y, "", alignment)
        {
            Viewer = viewer;
            CarPosition = carPosition;

            if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSLocomotive)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).LocomotiveName == null)
                    Text = Viewer.Catalog.GetString("Car ID") + " " + car.CarID;
                else
                    Text = Viewer.Catalog.GetString("Car ID") + " " + car.CarID + "  -  " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).LocomotiveName;
            }
            else
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonName == null)
                    Text = Viewer.Catalog.GetString("Car ID") + " " + car.CarID;
                else
                    Text = Viewer.Catalog.GetString("Car ID") + " " + car.CarID + "  -  " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonName;
            }

            Click += new Action<Control, Point>(TrainOperationsLabel_Click);

            // Icik
            if (!Viewer.CarOperationsWindow.Visible)
                car.SelectedCar = false;
        }

        void TrainOperationsLabel_Click(Control arg1, Point arg2)
        {
            Viewer.CarOperationsWindow.CarPosition = CarPosition;
            Viewer.CarOperationsWindow.Visible = true;

            // Icik
            Viewer.Simulator.attachedCar = Viewer.PlayerTrain.Cars[CarPosition];

            foreach (var car in Viewer.PlayerTrain.Cars)
                car.SelectedCar = false;
            Viewer.PlayerTrain.Cars[CarPosition].SelectedCar = true;
        }        
    }

    class TrainOperationsInfo : Label
    {
        readonly Viewer Viewer;        

        public TrainOperationsInfo(int x, int y, Viewer viewer, LabelAlignment alignment)
            : base(x, y, "", alignment)
        {
            Viewer = viewer;

            Text = Viewer.Catalog.GetString("Length") + " " + (int)Viewer.PlayerTrain.Length + " m    "
                + Viewer.Catalog.GetString("Mass") + " " + (int)(Viewer.PlayerTrain.MassKg / 1000f) + " t    "
                + Viewer.Catalog.GetString("Cars count") + " " + Viewer.PlayerTrain.Cars.Count;
            Color = Color.Yellow;
        }
    }
    class TrainOperationsBrakePercent : Label
    {
        readonly Viewer Viewer;

        public TrainOperationsBrakePercent(int x, int y, Viewer viewer, LabelAlignment alignment)
            : base(x, y, "", alignment)
        {
            Viewer = viewer;
            Train PlayerTrain = Viewer.PlayerTrain;
            
            Text = Viewer.Catalog.GetString("Real braking percentages") + " " + (int)PlayerTrain.PlayerTrainBrakePercent + " %";                
            Color = Color.Yellow;
        }
    }
}
