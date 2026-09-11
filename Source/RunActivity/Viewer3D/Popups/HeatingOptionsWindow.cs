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
using ORTS.Common;
using System;
using Orts.Simulation.Simulation.RollingStocks;

namespace Orts.Viewer3D.Popups
{
    public class HeatingOptionsWindow : Window
    {
        readonly Viewer Viewer;

        public HeatingOptionsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 13, Window.DecorationSize.Y + (owner.TextFontDefault.Height * 4) + (ControlLayout.SeparatorSize * 3), Viewer.Catalog.GetString("Heating Options"))
        {
            Viewer = owner.Viewer;
        }

        int CarID;
        protected override ControlLayout Layout(ControlLayout layout)
        {
            CarID = Viewer.CarOperationsWindow.CarPosition;

            Label buttonTempIncrement, Temp, buttonTempDecrement, buttonClose;

            var vbox = base.Layout(layout).AddLayoutVertical();
            vbox.Add(buttonTempIncrement = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, "+", LabelAlignment.Center));
            
            vbox.AddHorizontalSeparator();
            vbox.Add(Temp = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Set Temperature: ") + (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).SetTempCThreshold + " °C", LabelAlignment.Center));
            Temp.Color = Color.Yellow;
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonTempDecrement = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, "-", LabelAlignment.Center));
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));

            buttonTempIncrement.Click += new Action<Control, Point>(buttonTempIncrement_Click);
            buttonTempDecrement.Click += new Action<Control, Point>(buttonTempDecrement_Click);            
            buttonClose.Click += new Action<Control, Point>(buttonClose_Click);

            return vbox;
        }

        void buttonClose_Click(Control arg1, Point arg2)
        {
            Visible = false;
            Viewer.CarOperationsWindow.HeatingOptionsOpened = false;
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

        void buttonTempIncrement_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).SetTempCThreshold < 40)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).SetTempCThreshold++;
        }

        void buttonTempDecrement_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).SetTempCThreshold > 15)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSWagon).SetTempCThreshold--;
        }        
    }
}
