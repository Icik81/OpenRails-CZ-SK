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
using Orts.Simulation;
using Orts.Simulation.Physics;
using ORTS.Common;
using ORTS.Common.Input;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Orts.Viewer3D.Popups
{
    public class TestBrakeWindow : Window
    {
        readonly Viewer Viewer;
        Train PlayerTrain;

        ControlLayoutScrollbox MessageScroller;
        TextFlow Message;

        public TestBrakeWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 25, Window.DecorationSize.Y + owner.TextFontDefault.Height * 20, Viewer.Catalog.GetString("BrakeTest Informartion"))
        {
            Viewer = owner.Viewer;
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {            
            Label buttonContinue;
            var vbox = base.Layout(layout).AddLayoutVertical();            

            var hbox = vbox.AddLayoutHorizontal(vbox.RemainingHeight - (ControlLayout.SeparatorSize + vbox.TextHeight) * 2);
            var scrollbox = hbox.AddLayoutScrollboxVertical(hbox.RemainingWidth);
            scrollbox.Add(Message = new TextFlow(scrollbox.RemainingWidth - scrollbox.TextHeight, Owner.Viewer.Simulator.TestBrakeWindowMessage));
            MessageScroller = (ControlLayoutScrollbox)hbox.Controls.Last();
            
            vbox.AddHorizontalSeparator();
            vbox.AddSpace(0, 10);

            vbox.Add(buttonContinue = new Label(vbox.RemainingWidth, Owner.TextFontDefault.Height, Viewer.Catalog.GetStringFmt("Continue playing ({0})", Owner.Viewer.Settings.Input.Commands[(int)UserCommand.GamePauseMenu]), LabelAlignment.Center));            
            buttonContinue.Click += new Action<Control, Point>(buttonContinue_Click);
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);            

            if (updateFull)
            {
                if (Owner.Viewer.Simulator.FullTestBrakeWindow || Owner.Viewer.Simulator.SimpleTestBrakeWindow)                                        
                {
                    Owner.Viewer.Simulator.FullTestBrakeWindow = false;
                    Owner.Viewer.Simulator.SimpleTestBrakeWindow = false;
                    Layout();                    
                }
            }
        }


        void buttonContinue_Click(Control arg1, Point arg2)
        {
            Visible = false;
            Owner.Viewer.Simulator.FullTestBrakeWindow = false;
            Owner.Viewer.Simulator.SimpleTestBrakeWindow = false;
            Owner.Viewer.Simulator.ShunterFullTestBrakeEnable = false;
            Owner.Viewer.Simulator.ShunterSimpleTestBrakeEnable = false;
            Owner.Viewer.Simulator.ShunterTestingBrakeChanged = true;
        }
    }
}
