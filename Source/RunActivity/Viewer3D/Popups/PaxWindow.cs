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

using Orts.Simulation.RollingStocks;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Viewer3D.Popups
{
    public class PaxWindow : Window
    {
        public PaxWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 40, Window.DecorationSize.Y + owner.TextFontDefault.Height * 50, Viewer.Catalog.GetString("Passenger Info"))
        {
        }

        Label Name;
        Label From;
        Label To;
        ControlLayout scrollbox;
        int colWidth;
        protected override ControlLayout Layout(ControlLayout layout)
        {
            var vbox = base.Layout(layout).AddLayoutVertical();
            var line = vbox.AddLayoutHorizontalLineOfText();
            colWidth = (vbox.RemainingWidth - vbox.TextHeight * 2) / 3;
            {
                line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Jméno (hmotnost/věk)")));
                line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Ze stanice"), LabelAlignment.Left));
                line.Add(new Label(colWidth, line.RemainingHeight, Viewer.Catalog.GetString("Do stanice"), LabelAlignment.Left));
            }
            vbox.AddHorizontalSeparator();
            scrollbox = vbox.AddLayoutScrollboxVertical(vbox.RemainingWidth);
            {
                var info = scrollbox.AddLayoutHorizontalLineOfText();
                info.Add(Name = new Label(colWidth, info.RemainingWidth, ""));
                info.Add(From = new Label(colWidth, info.RemainingWidth, ""));
                info.Add(To = new Label(colWidth, info.RemainingWidth, ""));
            }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, true);
            if (Name == null || From == null || To == null)
                return;
            int top = 0;
            Name.Text = "";
            From.Text = "";
            To.Text = "";
            int totalPax = 0;
            int totalWeight = 0;
            var train0 = Owner.Viewer.Simulator.Trains.Find(item => item.IsActualPlayerTrain);
            if (train0 != null)
            {
                int carNum = 1;
                foreach (TrainCar tc in train0.Cars)
                {
                    if (tc.PassengerList.Count > 0)
                    {
                        List<Passenger> sorted = tc.PassengerList.OrderBy(c => c.StationOrderIndex).ToList();
                        Name.Text += Viewer.Catalog.GetString("Vůz č. ") + carNum.ToString() + " (cestujících: " + tc.PassengerList.Count + ", kapacita " + tc.PassengerCapacity.ToString() + ")" + Environment.NewLine;
                        From.Text += Environment.NewLine;
                        To.Text += Environment.NewLine;
                        top += scrollbox.TextHeight;
                        carNum++;
                        foreach (Passenger pax in sorted)
                        {
                            Name.Text += pax.FirstName.Replace("\"", "") + " " + pax.Surname.Replace("\"", "") + " (" + pax.Weight.ToString() + "kg/" + pax.Age.ToString() + ")" + Environment.NewLine;
                            From.Text += pax.DepartureStationName + Environment.NewLine;
                            To.Text += pax.ArrivalStationName + Environment.NewLine;
                            totalPax++;
                            totalWeight += (int)pax.Weight;
                            top += scrollbox.TextHeight;
                        }
                        Name.Text += Environment.NewLine;
                        From.Text += Environment.NewLine;
                        To.Text += Environment.NewLine;
                    }
                }
            }
            Name.Text += "Celkem:";
            var separator = new System.Globalization.NumberFormatInfo()
            {
                NumberDecimalDigits = 0,
                NumberGroupSeparator = "."
            };
            From.Text += "Hmotnost cestujících: " + totalWeight.ToString("N", separator) + "kg/počet cestujících: " + totalPax.ToString("N", separator);
            scrollbox.CurrentTop = top + 100;
        }
    }
}
