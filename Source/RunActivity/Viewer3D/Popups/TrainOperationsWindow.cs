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
using Orts.Simulation;
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
        internal static Texture2D LocoDTexture;
        internal static Texture2D LocoETexture;
        internal static Texture2D LocoSTexture;
        internal static Texture2D DMUTexture;
        internal static Texture2D TenderTexture;
        internal static Texture2D Passenger2CarTexture;
        internal static Texture2D Passenger4CarTexture;
        internal static Texture2D Freight2CarTexture;
        internal static Texture2D Freight4CarTexture;
        Train PlayerTrain;
        int LastPlayerTrainCars;
        bool LastPlayerLocomotiveFlippedState;
        ControlLayoutScrollbox TrainOperationsMenuScroller;
        ControlLayoutScrollbox TrainOperationsMenuScroller2;
        const int TextBoxHeight = 50;

        public TrainOperationsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 23, Window.DecorationSize.Y + CarListPadding + owner.TextFontDefault.Height * TextBoxHeight, Viewer.Catalog.GetString("Train Operations"))
        {
        }

        protected internal override void Initialize()
        {
            base.Initialize();
            if (CouplerTexture == null)
                // TODO: This should happen on the loader thread.
                CouplerTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsCoupler.png"));

            if (LocoDTexture == null)
                LocoDTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsDLoco.png"));
            if (LocoETexture == null)
                LocoETexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsELoco.png"));
            if (LocoSTexture == null)
                LocoSTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsSLoco.png"));
            if (DMUTexture == null)
                DMUTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsDMU.png"));

            if (TenderTexture == null)
                TenderTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperationsTender.png"));

            if (Passenger2CarTexture == null)
                Passenger2CarTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperations2PassengerCar.png"));
            if (Passenger4CarTexture == null)
                Passenger4CarTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperations4PassengerCar.png"));

            if (Freight2CarTexture == null)
                Freight2CarTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperations2FreightCar.png"));
            if (Freight4CarTexture == null)
                Freight4CarTexture = SharedTextureManager.Get(Owner.Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Owner.Viewer.ContentPath, "TrainOperations4FreightCar.png"));
        }
            
        protected override ControlLayout Layout(ControlLayout layout)
        {
            var textHeight = Owner.TextFontDefault.Height;
            var hbox = base.Layout(layout).AddLayoutVertical();
            var hbox2 = base.Layout(layout).AddLayoutVertical();
            var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);            
            var scrollbox2 = hbox2.AddLayoutScrollboxVertical(hbox2.RemainingWidth);

            TrainOperationsMenuScroller = (ControlLayoutScrollbox)hbox.Controls.Last();
            TrainOperationsMenuScroller2 = (ControlLayoutScrollbox)hbox2.Controls.Last();
            scrollbox.NumMenu = 1;            

            if (PlayerTrain != null)
            {                                                                
                scrollbox.Add(new TrainOperationsInfo(textHeight * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth, textHeight, Owner.Viewer, LabelAlignment.Center));
                scrollbox.AddHorizontalSeparator();
                scrollbox.Add(new TrainOperationsBrakePercent(textHeight * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth, textHeight, Owner.Viewer, LabelAlignment.Center));
                scrollbox.AddHorizontalSeparator();

                scrollbox2.Add(new Label(textHeight * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth, textHeight, Viewer.Catalog.GetString(""), LabelAlignment.Center));
                scrollbox2.AddHorizontalSeparator();
                scrollbox2.Add(new Label(textHeight * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth, textHeight, Viewer.Catalog.GetString(""), LabelAlignment.Center));
                scrollbox2.AddHorizontalSeparator();

                int carPosition = 0;
                for (int i = carPosition; i < PlayerTrain.Cars.Count; i++)
                {
                    var car = PlayerTrain.Cars[i];
                    var carLabel = new TrainOperationsLabel(textHeight * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth, textHeight + (textHeight / 2), Owner.Viewer, car, carPosition, LabelAlignment.Center);
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
                        carLabel.Color = Color.LightGreen;

                    //if (car.BrakeSystem.HandBrakeActive)
                    //    carLabel.Color = Color.Orange;

                    if (car.SelectedCar)
                        carLabel.Color = Color.Yellow;

                    Owner.Viewer.PlayerTrain.Simulator.ChangeCabActivated = false;                    

                    scrollbox.Add(carLabel);
                    scrollbox2.Add(new TrainOperationsIcon(10, 0, textHeight + (textHeight / 2), Owner.Viewer, car, carPosition));
                    if (car != PlayerTrain.Cars.Last())
                    {
                        scrollbox.Add(new TrainOperationsCoupler((int)(textHeight * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth / 2 - (textHeight / 2)), 0, textHeight + (textHeight / 2), Owner.Viewer, car, carPosition));                        
                        scrollbox2.Add(new TrainOperationsCoupler2((int)(textHeight * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth / 2 - (textHeight / 2)), 0, textHeight + (textHeight / 2), Owner.Viewer, car, carPosition));
                    }
                    carPosition++;
                }
                TrainOperationsMenuScroller.SetScrollPosition(Owner.Viewer.PlayerTrain.Simulator.TrainOperationsMenuSetScrollPosition);                
            }
            return hbox;
        }

        void carLabel_Click(Control arg1, Point arg2)
        {

        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);
            TrainOperationsMenuScroller2.SetScrollPosition(Owner.Viewer.PlayerTrain.Simulator.TrainOperationsMenuSetScrollPosition);

            if (updateFull)
            {
                if (PlayerTrain != Owner.Viewer.PlayerTrain || Owner.Viewer.PlayerTrain.Cars.Count != LastPlayerTrainCars || (Owner.Viewer.PlayerLocomotive != null &&
                    LastPlayerLocomotiveFlippedState != Owner.Viewer.PlayerLocomotive.Flipped)
                    // Icik
                    || Owner.Viewer.PlayerTrain.Simulator.ChangeCabActivated
                    || Owner.Viewer.PlayerTrain.PlayerTrainBrakePercentChange
                    || Owner.Viewer.Simulator.ScreenSizeY != Owner.ScreenSize.Y
                    || Owner.Viewer.PlayerTrain.Simulator.HandBrakeStatusChange
                    || Owner.Viewer.Simulator.CarPositionChanged
                    )
                {
                    Owner.Viewer.PlayerTrain.PlayerTrainBrakePercentChange = false;
                    Owner.Viewer.PlayerTrain.Simulator.HandBrakeStatusChange = false;
                    Owner.Viewer.Simulator.CarPositionChanged = false;
                    PlayerTrain = Owner.Viewer.PlayerTrain;
                    LastPlayerTrainCars = Owner.Viewer.PlayerTrain.Cars.Count;
                    if (Owner.Viewer.PlayerLocomotive != null) LastPlayerLocomotiveFlippedState = Owner.Viewer.PlayerLocomotive.Flipped;

                    Owner.Viewer.Simulator.ScreenSizeY = Owner.ScreenSize.Y;
                    int Y_Height = Window.DecorationSize.Y + (Owner.TextFontDefault.Height * (PlayerTrain.Cars.Count * 3 + 2)) + (ControlLayout.SeparatorSize * 3);
                    if (Y_Height > Owner.ScreenSize.Y - 20) Y_Height = Owner.ScreenSize.Y - 20;

                    var carLabel1 = new TrainOperationsInfo(1, 1, Owner.Viewer, LabelAlignment.Center);
                    Owner.Viewer.Simulator.TrainOperationsMenuTextWidth = 0;                    
                    int carPosition = 0;
                    foreach (var car in PlayerTrain.Cars)
                    {                        
                        var carLabel2 = new TrainOperationsLabel(1, 1, Owner.Viewer, car, carPosition, LabelAlignment.Center);
                        carPosition++;
                    }                    

                    SizeTo(Window.DecorationSize.X + Owner.TextFontDefault.Height * Owner.Viewer.Simulator.TrainOperationsMenuTextWidth, Y_Height);
                    Layout();                    
                }
            }
        }
    }

    class TrainOperationsCoupler : Image
    {
        readonly Viewer Viewer;
        int CarPosition;

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
                {
                    Viewer.CarOperationsWindow.Visible = false;
                    Viewer.BrakeModeOptionsWindow.Visible = false;
                    if (CarPosition > Viewer.PlayerTrain.Cars.Count - 1)
                        CarPosition = Viewer.PlayerTrain.Cars.Count - 1;
                    Viewer.Simulator.attachedCar = Viewer.PlayerTrain.Cars[CarPosition];
                }
            }            
        }
    }

    class TrainOperationsCoupler2 : Image
    {
        readonly Viewer Viewer;
        int CarPosition;

        public TrainOperationsCoupler2(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
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
            string TextGap = "";

            if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSLocomotive)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).LocomotiveName == null)
                    Text = TextGap + Viewer.Catalog.GetString("Car ID") + " " + car.CarID;
                else 
                if(car.BrakeSystem.HandBrakeActive)
                    Text = TextGap + "(P)  " + Viewer.Catalog.GetString("Car ID") + " " + car.CarID + "  -  " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).LocomotiveName;
                else                
                    Text = TextGap + Viewer.Catalog.GetString("Car ID") + " " + car.CarID + "  -  " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSLocomotive).LocomotiveName;
            }
            else
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonName == null)
                    Text = TextGap + Viewer.Catalog.GetString("Car ID") + " " + car.CarID;
                else
                if (car.BrakeSystem.HandBrakeActive)
                    Text = TextGap + "(P)  " + Viewer.Catalog.GetString("Car ID") + " " + car.CarID + "  -  " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonName;
                else
                    Text = TextGap + Viewer.Catalog.GetString("Car ID") + " " + car.CarID + "  -  " + (Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonName;
            }

            Click += new Action<Control, Point>(TrainOperationsLabel_Click);

            // Icik            
            //if (!Viewer.CarOperationsWindow.Visible)
            //    car.SelectedCar = false;

            Viewer.Simulator.TrainOperationsMenuTextWidth = Math.Max(Viewer.Simulator.TrainOperationsMenuTextWidth, (int)Math.Round((Text.Length - TextGap.Length) / 2f, 0) + 3);
            Viewer.Simulator.TrainOperationsMenuTextWidth = Math.Max(Viewer.Simulator.TrainOperationsMenuMinimumTextWidth, Viewer.Simulator.TrainOperationsMenuTextWidth);
        }

        void TrainOperationsLabel_Click(Control arg1, Point arg2)
        {
            if (!Viewer.PlayerTrain.Cars[CarPosition].SelectedCar)
            {
                Viewer.Simulator.CarPositionChanged = true;
                foreach (var car in Viewer.PlayerTrain.Cars)
                    car.SelectedCar = false;
                Viewer.PlayerTrain.Cars[CarPosition].SelectedCar = true;
            }
            Viewer.CarOperationsWindow.CarPosition = CarPosition;
            Viewer.CarOperationsWindow.Visible = true;
            //Viewer.Simulator.attachedCar = Viewer.PlayerTrain.Cars[CarPosition];            
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

            Viewer.Simulator.TrainOperationsMenuMinimumTextWidth = (int)Math.Round(Text.Length / 2f) + 3;
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

    class TrainOperationsIcon : Image
    {
        readonly Viewer Viewer;
        int CarPosition;

        public TrainOperationsIcon(int x, int y, int size, Viewer viewer, TrainCar car, int carPosition)
            : base(x, y, 2 * size, size)
        {
            Viewer = viewer;
            CarPosition = carPosition;
            if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSLocomotive)
            {
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSDieselLocomotive)
                {
                    Texture = TrainOperationsWindow.LocoDTexture;
                    Source = new Rectangle(0, 0, 209, 74);
                }
                else
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSElectricLocomotive)
                {
                    Texture = TrainOperationsWindow.LocoETexture;
                    Source = new Rectangle(0, 0, 250, 92);
                }
                else
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSSteamLocomotive)
                {
                    Texture = TrainOperationsWindow.LocoSTexture;
                    Source = new Rectangle(0, 0, 392, 142);
                }
                else
                if (Viewer.PlayerTrain.Cars[CarPosition] is MSTSControlUnit)
                {
                    Texture = TrainOperationsWindow.DMUTexture;
                    Source = new Rectangle(0, 0, 300, 76);
                }
            }
            else
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonType == TrainCar.WagonTypes.Tender)
            {
                Texture = TrainOperationsWindow.TenderTexture;
                Source = new Rectangle(0, 0, 124, 106);
            } 
            else
            if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).HasPassengerCapacity)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonNumAxles <= 2)
                {
                    Texture = TrainOperationsWindow.Passenger2CarTexture;
                    Source = new Rectangle(0, 0, 201, 79);
                }
                else
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonNumAxles > 2)
                {
                    Texture = TrainOperationsWindow.Passenger4CarTexture;
                    Source = new Rectangle(0, 0, 242, 76);
                }
            }
            else
            if (!(Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).HasPassengerCapacity)
            {
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonNumAxles <= 2)
                {
                    Texture = TrainOperationsWindow.Freight2CarTexture;
                    Source = new Rectangle(0, 0, 186, 87);
                }
                else
                if ((Viewer.PlayerTrain.Cars[CarPosition] as MSTSWagon).WagonNumAxles > 2)
                {
                    Texture = TrainOperationsWindow.Freight4CarTexture;
                    Source = new Rectangle(0, 0, 189, 75);
                }
            }            
            Click += new Action<Control, Point>(TrainOperationsIcon_Click);
        }

        void TrainOperationsIcon_Click(Control arg1, Point arg2)
        {
            Viewer.CarOperationsWindow.Visible = false;
            Viewer.Simulator.attachedCar = Viewer.PlayerTrain.Cars[CarPosition];
            Viewer.Simulator.CarPositionChanged = true;
            foreach (var car in Viewer.PlayerTrain.Cars)
                car.SelectedCar = false;
            Viewer.PlayerTrain.Cars[CarPosition].SelectedCar = true;
        }
    }
}
