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
using Orts.Common;
using ORTS.Common.Input;
using System;
using System.Windows.Forms;

namespace Orts.Viewer3D.Popups
{
    public class UnprotectedLvlCrossWindow : Window
    {
        public UnprotectedLvlCrossWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 28, Window.DecorationSize.Y + owner.TextFontDefault.Height * 5, Viewer.Catalog.GetString("Warning Event"))
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            Label buttonContinue, MSG;
            var vbox = base.Layout(layout).AddLayoutVertical();
            var heightForLabels = 10;
            heightForLabels = (vbox.RemainingHeight - 2 * ControlLayout.SeparatorSize) / 2;
            var spacing = (heightForLabels - Owner.TextFontDefault.Height) / 2;

            vbox.AddSpace(0, spacing + 2);
            vbox.Add(MSG = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, "    " + Viewer.Catalog.GetStringFmt("You didn't give a corresponding sound sign at an unprotected crossing!", LabelAlignment.Center)));
            //vbox.Add(MSG = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetStringFmt("Nedali jste odpovídající zvukové znamení před nechráněným přejezdem!", LabelAlignment.Center)));
            
            vbox.AddSpace(0, spacing);
            vbox.AddSpace(0, spacing);
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, spacing - 3);

            vbox.Add(buttonContinue = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetStringFmt("Continue playing ({0})", Owner.Viewer.Settings.Input.Commands[(int)UserCommand.GamePauseMenu]), LabelAlignment.Center));
            buttonContinue.Click += new Action<Control, Point>(buttonContinue_Click);
            return vbox;
        }

        void buttonContinue_Click(Control arg1, Point arg2)
        {
            Visible = Owner.Viewer.Simulator.Paused = false;
            if (Owner.Viewer.Log.PauseState == ReplayPauseState.During)
            {
                Owner.Viewer.Log.PauseState = ReplayPauseState.Done;
            }
            Owner.Viewer.ResumeReplaying();
        }
    }
}
