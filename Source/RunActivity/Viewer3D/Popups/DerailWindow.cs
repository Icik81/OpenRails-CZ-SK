// COPYRIGHT 2012, 2013 by the Open Rails project.
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
using ORTS.Common.Input;
using System;
using System.Windows.Forms;

namespace Orts.Viewer3D.Popups
{
    public class DerailWindow : Window
    {
        public DerailWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 20, Window.DecorationSize.Y + owner.TextFontDefault.Height * 5, Viewer.Catalog.GetString("Emergency"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label buttonQuit, MSG;
            var vbox = base.Layout(layout).AddLayoutVertical();
            var heightForLabels = 10;
            heightForLabels = (vbox.RemainingHeight - 2 * ControlLayout.SeparatorSize) / 2;
            var spacing = (heightForLabels - Owner.TextFontDefault.Height) / 2;

            vbox.AddSpace(0, spacing + 2);
            vbox.Add(MSG = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, "     " + Viewer.Catalog.GetStringFmt("Train derailed! You caused an emergency. Get out!", Application.ProductName, LabelAlignment.Center)));

            vbox.AddSpace(0, spacing);
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, spacing - 3);


            vbox.Add(buttonQuit = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetStringFmt("Quit {1} ({0})", Owner.Viewer.Settings.Input.Commands[(int)UserCommand.GameQuit], Application.ProductName), LabelAlignment.Center));
            buttonQuit.Click += new Action<Control, Point>(buttonQuit_Click);
            return vbox;
        }

        void buttonQuit_Click(Control arg1, Point arg2)
        {
            Owner.Viewer.Game.PopState();
        }
    }
}
