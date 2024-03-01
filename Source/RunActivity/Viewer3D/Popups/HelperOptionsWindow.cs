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
using System.Windows.Forms;

namespace Orts.Viewer3D.Popups
{
    public class HelperOptionsWindow : Window
    {
        readonly Viewer Viewer;      

        public HelperOptionsWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 13, Window.DecorationSize.Y + (owner.TextFontDefault.Height + 2) * 5 + ControlLayout.SeparatorSize * 3, Viewer.Catalog.GetString("Helper Options"))
        {
            Viewer = owner.Viewer;
        }
        
        public int CarID;
        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();

            if (Viewer.CarOperationsWindow.HelperOptionsOpened)
            {
                CarID = Viewer.CarOperationsWindow.CarPosition;
                Viewer.CarOperationsWindow.HelperOptionsOpened = false;
            }
            if (CarID >= Viewer.PlayerTrain.Cars.Count)
                CarID = Viewer.PlayerTrain.Cars.Count - 1;

            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive) == null)
            {
                Viewer.HelperOptionsWindow.Visible = false;
                Viewer.HelperSpeedSelectWindow.Visible = false;
                Viewer.CarOperationsWindow.HelperOptionsOpened = false;                
                return vbox;
            }
            if ((!(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoPush && !(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoFollow && !(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoDontPush)
                || (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).CarIsPlayerLoco)
            {
                Viewer.HelperOptionsWindow.Visible = false;
                Viewer.HelperSpeedSelectWindow.Visible = false;
                Viewer.CarOperationsWindow.HelperOptionsOpened = false;
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperOptionsOpened = false;
                return vbox;
            }

            if (Viewer.Simulator.GameTime > 0.1f)
            {
                if (CarID != Viewer.CarOperationsWindow.CarPosition)
                    (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperOptionsOpened = false;
                else
                    (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperOptionsOpened = true;
            }

            if (!(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoPush)
            {
                Viewer.HelperSpeedSelectWindow.Visible = false;
            }

            Label ID, buttonDontPush, buttonPush, buttonFollow, buttonClose;            

            vbox.Add(ID = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Car ID") + "  " + (CarID >= Viewer.PlayerTrain.Cars.Count ? " " : Viewer.PlayerTrain.Cars[CarID].CarID), LabelAlignment.Center));
            ID.Color = Color.Yellow;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonDontPush = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Don`t push!"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoDontPush)
                buttonDontPush.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonPush = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Push!"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoPush)
                buttonPush.Color = Color.LightGreen;

            vbox.AddHorizontalSeparator();
            vbox.Add(buttonFollow = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Follow!"), LabelAlignment.Center));
            if ((Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoFollow)
                buttonFollow.Color = Color.LightGreen;


            vbox.AddHorizontalSeparator();
            vbox.Add(buttonClose = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetString("Close window"), LabelAlignment.Center));
            buttonDontPush.Click += new Action<Control, Point>(buttonDontPush_Click);
            buttonPush.Click += new Action<Control, Point>(buttonPush_Click);
            buttonFollow.Click += new Action<Control, Point>(buttonFollow_Click);
            buttonClose.Click += new Action<Control, Point>(buttonClose_Click);

            return vbox;
        }

        void buttonClose_Click(Control arg1, Point arg2)
        {
            (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperOptionsOpened = false;
            Viewer.HelperSpeedSelectWindow.Visible = false;
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

        void buttonDontPush_Click(Control arg1, Point arg2)
        {
            if (!(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoDontPush)
            {
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoPush = false;
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoFollow = false;
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoDontPush = true;
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Don`t Push: ") + Viewer.Catalog.GetString("On"));
            }
            Viewer.HelperSpeedSelectWindow.Visible = false;
            (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperPushStart = false;
        }

        void buttonPush_Click(Control arg1, Point arg2)
        {
            if (!(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoPush)
            {
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoDontPush = false;
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoFollow = false;
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoPush = true;
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Push: ") + Viewer.Catalog.GetString("On"));                
            }
            Viewer.HelperSpeedSelectWindow.Visible = true;
        }

        void buttonFollow_Click(Control arg1, Point arg2)
        {
            if (!(Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoFollow)
            {
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoDontPush = false;
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoPush = false;
                (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperLocoFollow = true;
                Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Follow: ") + Viewer.Catalog.GetString("On"));
            }           
            Viewer.HelperSpeedSelectWindow.Visible = false;
            (Viewer.PlayerTrain.Cars[CarID] as MSTSLocomotive).HelperPushStart = false;
        }
    }
}
