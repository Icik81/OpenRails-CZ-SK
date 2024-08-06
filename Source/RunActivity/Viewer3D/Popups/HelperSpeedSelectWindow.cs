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

namespace Orts.Viewer3D.Popups
{
    public class HelperSpeedSelectWindow : Window
    {
        readonly Viewer Viewer;      

        public HelperSpeedSelectWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 13, Window.DecorationSize.Y + 2 + owner.TextFontDefault.Height * 9 + ControlLayout.SeparatorSize * 3, Viewer.Catalog.GetString("Helper Speed Select"))
        {
            Viewer = owner.Viewer;
        }
        
        int CarID;
        protected override ControlLayout Layout(ControlLayout layout)
        {
            CarID = Viewer.HelperOptionsWindow.CarID;                        

            Label buttonSpeedIncrement, buttonSpeedIncrement2, Speed, buttonSpeedDecrement, buttonSpeedDecrement2, buttonStart, buttonReset, buttonClose;

            var vbox = base.Layout(layout).AddLayoutVertical();
            
            vbox.Add(buttonSpeedIncrement = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("+"), LabelAlignment.Center));
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonSpeedIncrement2 = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("++"), LabelAlignment.Center));

            vbox.AddHorizontalSeparator();
            vbox.Add(Speed = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Speed: ") + (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive != null ? (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush : 0) + " km/h", LabelAlignment.Center));            
            Speed.Color = Color.Yellow;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonSpeedDecrement2 = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("--"), LabelAlignment.Center));

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonSpeedDecrement = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("-"), LabelAlignment.Center));        

            vbox.AddHorizontalSeparator();            
            vbox.Add(buttonStart = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Start"), LabelAlignment.Center));
            if (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive != null && (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperPushStart)
                buttonStart.Color = Color.LightGreen;
            
            vbox.AddHorizontalSeparator();
            vbox.Add(buttonReset = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Reset"), LabelAlignment.Center));
            buttonReset.Color = Color.Yellow;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));
            buttonSpeedIncrement.Click += new Action<Control, Point>(buttonSpeedIncrement_Click);            
            buttonSpeedDecrement.Click += new Action<Control, Point>(buttonSpeedDecrement_Click);
            buttonSpeedIncrement2.Click += new Action<Control, Point>(buttonSpeedIncrement2_Click);
            buttonSpeedDecrement2.Click += new Action<Control, Point>(buttonSpeedDecrement2_Click);
            buttonStart.Click += new Action<Control, Point>(buttonStart_Click);
            buttonReset.Click += new Action<Control, Point>(buttonReset_Click);
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

        void buttonSpeedIncrement_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush < 100)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush++;
        }

        void buttonSpeedDecrement_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush > 0)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush--;
        }

        void buttonSpeedIncrement2_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush < 91)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush += 1 * 10;
        }

        void buttonSpeedDecrement2_Click(Control arg1, Point arg2)
        {
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush > 9)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush -= 1 * 10;
        }

        void buttonStart_Click(Control arg1, Point arg2)
        {            
            if (!(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperPushStart)
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperPushStart = true;
            else
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperPushStart = false;
        }

        void buttonReset_Click(Control arg1, Point arg2)
        {            
            (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperSpeedPush = 0;
            (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperPushStart = false;
        }
    }
}
