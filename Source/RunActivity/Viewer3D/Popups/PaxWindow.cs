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

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.Popups
{
    public class PaxWindow : Window
    {
        public PaxWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 20, Window.DecorationSize.Y + owner.TextFontDefault.Height * 30, Viewer.Catalog.GetString("Passenger Info"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            if (Owner.Viewer.Simulator.Activity != null || Owner.Viewer.Simulator.TimetableMode)
            {
                var colWidth = (vbox.RemainingWidth - vbox.TextHeight * 2) / 3;
                {
                    var line = vbox.AddLayoutHorizontalLineOfText();
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Jméno")));
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Ze stanice"), LabelAlignment.Left));
                    line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Do stanice"), LabelAlignment.Right));
                }
                vbox.AddHorizontalSeparator();
                var scrollbox = vbox.AddLayoutScrollboxVertical(vbox.RemainingWidth);
                var train0 = Owner.Viewer.Simulator.Trains.Find(item => item.IsActualPlayerTrain);
                Simulation.Simulator simulator = train0.Simulator;
                if (train0 != null)
                {

                }
            }
            return vbox;
        }
    }
 }
