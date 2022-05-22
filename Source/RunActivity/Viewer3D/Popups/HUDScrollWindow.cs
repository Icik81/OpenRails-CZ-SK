﻿// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

namespace Orts.Viewer3D.Popups
{
    public class HUDScrollWindow : Window
    {

        Label pageDown;
        Label pageUp;
        Label pageLeft;
        Label pageRight;
        Label nextLoco;
        Label prevLoco;
        Label screenMode;

        public HUDScrollWindow(WindowManager owner)
            : base(owner, Window.DecorationSize.X + owner.TextFontDefault.Height * 8, Window.DecorationSize.Y + owner.TextFontDefault.Height * 9 + ControlLayout.SeparatorSize * 2, Viewer.Catalog.GetString("HUD Scroll"))
        {
        }

        private void ScreenMode_Click(Control arg1, Point arg2)
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            screenMode.Color = Color.White;
            if (!HudWindow.hudWindowFullScreen && (HudWindow.hudWindowColumnsPagesCount > 0 || HudWindow.hudWindowColumnsActualPage > 0 || HudWindow.hudWindowLinesPagesCount > 1 || HudWindow.hudWindowLinesActualPage > 1))
            {
                HudWindow.hudWindowColumnsActualPage = 0;
                HudWindow.hudWindowLinesActualPage = 1;
                HudWindow.hudWindowFullScreen = true;

            }
            else
            {
                HudWindow.hudWindowColumnsActualPage = 0;
                HudWindow.hudWindowLinesActualPage = 1;
                HudWindow.hudWindowFullScreen = false;
            }
        }

        private void PageRight_Click(Control arg1, Point arg2)
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            LabelReset();
            if (HudWindow.hudWindowColumnsPagesCount > 0 && HudWindow.hudWindowColumnsPagesCount > HudWindow.hudWindowColumnsActualPage)
            {
                HudWindow.hudWindowColumnsActualPage += 1;
                pageRight.Color = Color.White;
            }
        }

        private void PageLeft_Click(Control arg1, Point arg2)
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            if (HudWindow.hudWindowColumnsActualPage > 0)
            {
                HudWindow.hudWindowColumnsActualPage -= 1;
                pageLeft.Color = Color.White;
            }
        }

        private void PageUp_Click(Control arg1, Point arg2)
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            if (!HudWindow.BrakeInfoVisible && HudWindow.hudWindowLinesActualPage > 1)
            {
                HudWindow.hudWindowLinesActualPage -= 1;
                pageUp.Color = Color.White;
            }
        }

        private void PageDown_Click(Control arg1, Point arg2)
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            if (!HudWindow.BrakeInfoVisible && HudWindow.hudWindowLinesPagesCount > 1 && HudWindow.hudWindowLinesPagesCount > HudWindow.hudWindowLinesActualPage)
            {
                HudWindow.hudWindowLinesActualPage += 1;
                pageDown.Color = Color.White;
            }
        }

        private void NextLoco_Click(Control arg1, Point arg2)
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            if (!HudWindow.hudWindowSteamLocoLead && HudWindow.hudWindowLocoPagesCount > 0 && HudWindow.hudWindowLocoPagesCount > HudWindow.hudWindowLocoActualPage)
            {
                HudWindow.hudWindowLocoActualPage += 1;
                nextLoco.Color = Color.White;
            }
        }

        private void PrevLoco_Click(Control arg1, Point arg2)
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            if (!HudWindow.hudWindowSteamLocoLead && HudWindow.hudWindowLocoActualPage > 0)
            {
                HudWindow.hudWindowLocoActualPage -= 1;
                prevLoco.Color = Color.White;
                if (HudWindow.hudWindowLocoActualPage == 0)
                {//Restore to initial values
                    HudWindow.hudWindowLinesActualPage = 1;
                    HudWindow.hudWindowColumnsActualPage = 0;
                }
            }
        }

        private void LabelReset()
        {
            var HudWindow = Owner.Viewer.HUDWindow;

            if (HudWindow.hudWindowLinesPagesCount == 1) pageDown.Text = Viewer.Catalog.GetString("▼ Page Down");
            if (HudWindow.hudWindowLinesPagesCount > 1) pageUp.Text = Viewer.Catalog.GetString("▲ Page Up");
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            var HudWindow = Owner.Viewer.HUDWindow;
            var vbox = base.Layout(layout).AddLayoutVertical();
            {
                var hbox = vbox.AddLayoutHorizontalLineOfText();
                pageDown = new Label(hbox.RemainingWidth, hbox.RemainingHeight, HudWindow.hudWindowLinesPagesCount > 1 ? Viewer.Catalog.GetString("▼ Page Down (" + HudWindow.hudWindowLinesActualPage + "/" + HudWindow.hudWindowLinesPagesCount + ")") : Viewer.Catalog.GetString("▼ Page Down")) { Color = HudWindow.WebServerEnabled || (HudWindow.hudWindowLinesPagesCount > HudWindow.hudWindowLinesActualPage && !HudWindow.BrakeInfoVisible) ? Color.Gray : Color.Black };
                pageDown.Click += PageDown_Click;
                vbox.Add(pageDown);

                pageUp = new Label(hbox.RemainingWidth, hbox.RemainingHeight, HudWindow.hudWindowLinesPagesCount > 1 ? Viewer.Catalog.GetString("▲ Page Up (" + HudWindow.hudWindowLinesActualPage + " / " + HudWindow.hudWindowLinesPagesCount + ")") : Viewer.Catalog.GetString("▲ Page Up")) { Color = HudWindow.WebServerEnabled || (HudWindow.hudWindowLinesActualPage > 1 && !HudWindow.BrakeInfoVisible) ? Color.Gray : Color.Black };
                pageUp.Click += PageUp_Click;
                vbox.Add(pageUp);

                vbox.AddHorizontalSeparator();
                pageLeft = new Label(hbox.RemainingWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("◄ Page Left")) { Color = HudWindow.WebServerEnabled || HudWindow.hudWindowColumnsActualPage > 0 ? Color.Gray : Color.Black };
                pageLeft.Click += PageLeft_Click;
                vbox.Add(pageLeft);

                pageRight = new Label(hbox.RemainingWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("► Page Right")) { Color = HudWindow.WebServerEnabled || (HudWindow.hudWindowColumnsPagesCount > 0 && HudWindow.hudWindowColumnsActualPage < HudWindow.hudWindowColumnsPagesCount) ? Color.Gray : Color.Black };
                pageRight.Click += PageRight_Click;
                vbox.Add(pageRight);

                vbox.AddHorizontalSeparator();
                nextLoco = new Label(hbox.RemainingWidth, hbox.RemainingHeight, !HudWindow.hudWindowSteamLocoLead && HudWindow.hudWindowLocoActualPage > 0 ? Viewer.Catalog.GetString("▼ Next Loco (" + HudWindow.hudWindowLocoActualPage + "/" + HudWindow.hudWindowLocoPagesCount + ")") : Viewer.Catalog.GetPluralStringFmt("= One Locomotive.", "= All Locomotives.", (long)HudWindow.hudWindowLocoPagesCount), LabelAlignment.Left) { Color = HudWindow.WebServerEnabled || (HudWindow.hudWindowSteamLocoLead || HudWindow.hudWindowLocoPagesCount > HudWindow.hudWindowLocoActualPage) ? Color.Gray : Color.Black };
                nextLoco.Click += NextLoco_Click;
                vbox.Add(nextLoco);

                prevLoco = new Label(hbox.RemainingWidth, hbox.RemainingHeight, Viewer.Catalog.GetString("▲ Prev. Loco")) { Color = HudWindow.WebServerEnabled || (!HudWindow.hudWindowSteamLocoLead && HudWindow.hudWindowLocoActualPage > 0) ? Color.Gray : Color.Black };
                prevLoco.Click += PrevLoco_Click;
                vbox.Add(prevLoco);

                vbox.AddHorizontalSeparator();
                screenMode = new Label(hbox.RemainingWidth, hbox.RemainingHeight, (HudWindow.hudWindowFullScreen ? "Screen: Normal" : "Screen: Full"), LabelAlignment.Center) { Color = Color.Gray };
                screenMode.Click += ScreenMode_Click;
                vbox.Add(screenMode);
            }
            return vbox;
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            var MovingCurrentWindow = UserInput.IsMouseLeftButtonDown &&
                   UserInput.MouseX >= Location.X && UserInput.MouseX <= Location.X + Location.Width &&
                   UserInput.MouseY >= Location.Y && UserInput.MouseY <= Location.Y + Location.Height ?
                   true : false;

            if (!MovingCurrentWindow && updateFull)
                Layout();
        }
    }
}
