// COPYRIGHT 2014, 2018 by the Open Rails project.
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
//

using GNU.Gettext;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Common;
using ORTS.Common;
using ORTS.Content;
using ORTS.Menu;
using ORTS.TrackViewer.Drawing;
using ORTS.TrackViewer.Drawing.Labels;
using ORTS.TrackViewer.Editing;
using ORTS.TrackViewer.Editing.Charts;
using ORTS.TrackViewer.UserInterface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Color = Microsoft.Xna.Framework.Color;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ORTS.TrackViewer
{
    public delegate void MessageDelegate(string message);

    public class TrackViewer : Microsoft.Xna.Framework.Game
    {
        #region Public members
        public readonly static string TrackViewerVersion = "2018/01/09";
        public string ContentPath { get; private set; }
        public Folder InstallFolder { get; private set; }
        public Collection<Route> Routes { get; private set; }
        public Collection<Path> Paths { get; private set; }
        public Route CurrentRoute { get; private set; }
        private Route DefaultRoute;
        public int ScreenW { get; private set; }
        public int ScreenH { get; private set; }
        public RouteData RouteData { get; private set; }
        public DrawTrackDB DrawTrackDB { get; private set; }
        public DrawArea DrawArea { get; private set; }
        public SmoothedData FrameRate { get; private set; }
        public DrawMultiplePaths DrawMultiplePaths { get; private set; }
        public LanguageManager LanguageManager { get; private set; }
        public PathEditor PathEditor { get; private set; }
        public DrawPATfile DrawPATfile { get; private set; }
        #endregion

        #region Private members
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        ShadowDrawArea drawAreaInset;
        DrawScaleRuler drawScaleRuler;
        DrawLongitudeLatitude drawLongitudeLatitude;
        DrawEditorAction drawEditorAction;
        DrawWorldTiles drawWorldTiles;
        DrawPathChart drawPathChart;
        public DrawTerrain drawTerrain;

        DrawLabels drawLabels;

        MenuControl menuControl;
        StatusBarControl statusBarControl;

        private bool lostFocus;
        private int skipDrawAmount;
        private const int maxSkipDrawAmount = 10;

        private FontManager fontManager;
        private string[] commandLineArgs;
        #endregion

        #region Route Markers Data Classes & Lists
        public class RouteVoltagePoint
        {
            public int Id { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int Voltage { get; set; }
            public WorldLocation WorldLocation { get; set; }
        }

        public class RoutePowerSupplyStation
        {
            public int Id { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int PowerSystem { get; set; } // 0 = 3 kV DC, 1 = 25 kV AC, 2 = 15 kV AC
            public WorldLocation WorldLocation { get; set; }
        }

        public class RouteMirelPoint
        {
            public int SignalId { get; set; }
            public string Value { get; set; } // "b" = kódováno, "a" = nekódováno
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public WorldLocation WorldLocation { get; set; }
        }

        public class SceneryShapeMarker
        {
            public string ShapeName { get; set; }
            public WorldLocation WorldLocation { get; set; }
        }

        public List<SceneryShapeMarker> PantoDownMarkers = new List<SceneryShapeMarker>();
        public List<SceneryShapeMarker> PantoUpMarkers = new List<SceneryShapeMarker>();

        public List<RouteVoltagePoint> VoltagePoints = new List<RouteVoltagePoint>();
        private RouteVoltagePoint draggedVoltagePoint = null;

        public List<RoutePowerSupplyStation> PowerSupplyStations = new List<RoutePowerSupplyStation>();
        private RoutePowerSupplyStation draggedPowerSupplyStation = null;

        public List<RouteMirelPoint> MirelPoints = new List<RouteMirelPoint>();
        private RouteMirelPoint draggedMirelPoint = null;
        #endregion

        #region Constructor and Initialization methods
        public TrackViewer(string[] args)
        {
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.CallUpgrade = false;
            }

            this.commandLineArgs = args;

            graphics = new GraphicsDeviceManager(this);
            ContentPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Content");

            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            ScreenH = graphics.PreferredBackBufferHeight;
            ScreenW = graphics.PreferredBackBufferWidth;
            SetAliasing();
            graphics.IsFullScreen = false;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += new System.EventHandler<EventArgs>(Window_ClientSizeChanged);

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(0.05);
            FrameRate = new SmoothedData(0.5f);
            InitLogging();

            LanguageManager = new LanguageManager();
            LanguageManager.LoadLanguage();
        }

        protected override void Initialize()
        {
            TVInputSettings.SetDefaults();

            Control.FromHandle(Window.Handle).Controls.Add(new TextBox() { Top = -100 });

            statusBarControl = new StatusBarControl(this);
            TrackViewer.Localize(statusBarControl);
            menuControl = new MenuControl(this);
            TrackViewer.Localize(menuControl);
            menuControl.PopulateLanguages();
            DrawColors.Initialize(menuControl);

            Localize(statusBarControl);
            Localize(menuControl);

            drawWorldTiles = new DrawWorldTiles();
            drawScaleRuler = new DrawScaleRuler();
            DrawArea = new DrawArea(drawScaleRuler);
            drawAreaInset = new ShadowDrawArea(null)
            {
                StrictChecking = true
            };

            fontManager = FontManager.Instance;
            SetSubwindowSizes();

            this.IsMouseVisible = true;

            if (String.IsNullOrEmpty(Properties.Settings.Default.installDirectory))
            {
                try
                {
                    Properties.Settings.Default.installDirectory = MSTSPath.Base();
                }
                catch { }
            }
            InstallFolder = new Folder("default", Properties.Settings.Default.installDirectory);

            FindRoutes(InstallFolder);

            drawPathChart = new DrawPathChart();

            base.Initialize();
        }

        void SetSubwindowSizes()
        {
            int insetRatio = 10;
            float dpiScale = System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiY / 96;
            int menuHeight = (int)(menuControl.MenuHeight * dpiScale);
            int statusbarHeight = (int)(statusBarControl.StatusbarHeight * dpiScale);
            menuControl.SetScreenSize(ScreenW, menuHeight);
            statusBarControl.SetScreenSize(ScreenW, statusbarHeight, ScreenH);

            DrawArea.SetScreenSize(0, menuHeight, ScreenW, ScreenH - statusbarHeight - menuHeight);
            drawAreaInset.SetScreenSize(ScreenW - ScreenW / insetRatio, menuHeight + 1, ScreenW / insetRatio, ScreenH / insetRatio);

            int halfHeight = (int)(fontManager.DefaultFont.Height / 2);
            drawScaleRuler.SetLocationAndSize(halfHeight, ScreenH - statusbarHeight - halfHeight, 2 * halfHeight);
            drawLongitudeLatitude = new DrawLongitudeLatitude(halfHeight, menuHeight);
            drawEditorAction = new DrawEditorAction(halfHeight, menuHeight + 2 * halfHeight);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            BasicShapes.LoadContent(GraphicsDevice, spriteBatch, ContentPath);
            drawAreaInset.LoadContent(GraphicsDevice, spriteBatch, 2, 2, 2);
        }

        protected override void UnloadContent()
        {
        }

        private void DrawLoadingMessage(string message)
        {
            BeginDraw();
            GraphicsDevice.Clear(DrawColors.colorsNormal.ClearWindow);
            spriteBatch.Begin();
            Vector2 messageLocation = new Vector2((float)Math.Round(ScreenW / 2f), (float)Math.Round(ScreenH / 2f));
            BasicShapes.DrawStringCentered(messageLocation, DrawColors.colorsNormal.Text, message);

            fontManager.Update(GraphicsDevice);
            BasicShapes.DrawStringCentered(messageLocation, DrawColors.colorsNormal.Text, message);

            spriteBatch.End();
            EndDraw();
        }
        #endregion

        public string PantoDownShapeName { get; set; } = "EP1.s";
        public string PantoUpShapeName { get; set; } = "EP2.s";

        public void PromptAndFindPantoSigns()
        {
            if (CurrentRoute == null) return;

            using (var form = new System.Windows.Forms.Form())
            {
                form.Text = catalog.GetString("Pantograph sign shape names");
                form.Width = 380;
                form.Height = 210;
                form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lblDown = new System.Windows.Forms.Label() { Left = 20, Top = 20, Width = 320, Text = catalog.GetString("Pantograph down shape:") };
                var txtDown = new System.Windows.Forms.TextBox() { Left = 20, Top = 45, Width = 320, Text = PantoDownShapeName };

                var lblUp = new System.Windows.Forms.Label() { Left = 20, Top = 80, Width = 320, Text = catalog.GetString("Pantograph up shape:") };
                var txtUp = new System.Windows.Forms.TextBox() { Left = 20, Top = 105, Width = 320, Text = PantoUpShapeName };

                var btnOk = new System.Windows.Forms.Button() { Text = catalog.GetString("Search"), Left = 160, Width = 90, Top = 135, DialogResult = System.Windows.Forms.DialogResult.OK };
                var btnCancel = new System.Windows.Forms.Button() { Text = catalog.GetString("Cancel"), Left = 260, Width = 80, Top = 135, DialogResult = System.Windows.Forms.DialogResult.Cancel };

                form.Controls.AddRange(new System.Windows.Forms.Control[] { lblDown, txtDown, lblUp, txtUp, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PantoDownShapeName = txtDown.Text.Trim();
                    PantoUpShapeName = txtUp.Text.Trim();

                    ReloadPantoSigns();
                }
            }
        }

        public void ReloadPantoSigns()
        {
            if (CurrentRoute == null) return;

            DrawLoadingMessage(catalog.GetString("Searching pantograph sign shapes in route files..."));

            // Vyhledá oba shape objekty v jednom průchodu
            FindSceneryShapes(CurrentRoute.Path, PantoDownShapeName, PantoUpShapeName);

            skipDrawAmount = 0;
        }

        #region Context Menu & Creation of Route Markers
        private void ShowRouteMarkersContextMenu(int screenX, int screenY)
        {
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            WorldLocation clickLoc = DrawArea.MouseLocation;

            const float pickRadiusPx = 15f;

            RouteVoltagePoint hitVoltage = null;
            if (Properties.Settings.Default.showVoltageMarkers)
            {
                foreach (var pt in VoltagePoints)
                {
                    float distMeters = (float)Math.Sqrt(WorldLocation.GetDistanceSquared2D(pt.WorldLocation, clickLoc));
                    if ((distMeters * DrawArea.Scale) <= pickRadiusPx)
                    {
                        hitVoltage = pt;
                        break;
                    }
                }
            }

            RoutePowerSupplyStation hitPss = null;
            if (Properties.Settings.Default.showPowerSupplyStations && hitVoltage == null)
            {
                foreach (var pt in PowerSupplyStations)
                {
                    float distMeters = (float)Math.Sqrt(WorldLocation.GetDistanceSquared2D(pt.WorldLocation, clickLoc));
                    if ((distMeters * DrawArea.Scale) <= pickRadiusPx)
                    {
                        hitPss = pt;
                        break;
                    }
                }
            }

            RouteMirelPoint hitMirel = null;
            if (Properties.Settings.Default.showMirelPoints && hitVoltage == null && hitPss == null)
            {
                foreach (var pt in MirelPoints)
                {
                    float distMeters = (float)Math.Sqrt(WorldLocation.GetDistanceSquared2D(pt.WorldLocation, clickLoc));
                    if ((distMeters * DrawArea.Scale) <= pickRadiusPx)
                    {
                        hitMirel = pt;
                        break;
                    }
                }
            }

            if (hitVoltage != null)
            {
                var lbl = new System.Windows.Forms.ToolStripMenuItem($"Napěťový bod ({hitVoltage.Voltage} V)") { Enabled = false };
                contextMenu.Items.Add(lbl);

                var m3kV = new System.Windows.Forms.ToolStripMenuItem("Změnit na 3 kV DC", null, (s, e) => { hitVoltage.Voltage = 3000; skipDrawAmount = 0; });
                var m25kV = new System.Windows.Forms.ToolStripMenuItem("Změnit na 25 kV AC", null, (s, e) => { hitVoltage.Voltage = 25000; skipDrawAmount = 0; });
                var m15kV = new System.Windows.Forms.ToolStripMenuItem("Změnit na 15 kV AC", null, (s, e) => { hitVoltage.Voltage = 15000; skipDrawAmount = 0; });
                var m0V = new System.Windows.Forms.ToolStripMenuItem("Změnit na 0 V (Bez napětí)", null, (s, e) => { hitVoltage.Voltage = 0; skipDrawAmount = 0; });
                contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { m3kV, m25kV, m15kV, m0V });

                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                var delItem = new System.Windows.Forms.ToolStripMenuItem("Smazat napěťový bod", null, (s, e) =>
                {
                    VoltagePoints.Remove(hitVoltage);
                    skipDrawAmount = 0;
                });
                contextMenu.Items.Add(delItem);
            }
            else if (hitPss != null)
            {
                var lbl = new System.Windows.Forms.ToolStripMenuItem($"Napájecí stanice #{hitPss.Id} (Soustava: {hitPss.PowerSystem})") { Enabled = false };
                contextMenu.Items.Add(lbl);

                var s0 = new System.Windows.Forms.ToolStripMenuItem("Změnit na 3 kV DC (0)", null, (s, e) => { hitPss.PowerSystem = 0; skipDrawAmount = 0; });
                var s1 = new System.Windows.Forms.ToolStripMenuItem("Změnit na 25 kV AC (1)", null, (s, e) => { hitPss.PowerSystem = 1; skipDrawAmount = 0; });
                var s2 = new System.Windows.Forms.ToolStripMenuItem("Změnit na 15 kV AC (2)", null, (s, e) => { hitPss.PowerSystem = 2; skipDrawAmount = 0; });
                contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { s0, s1, s2 });

                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                var delItem = new System.Windows.Forms.ToolStripMenuItem("Smazat napájecí stanici", null, (s, e) =>
                {
                    PowerSupplyStations.Remove(hitPss);
                    skipDrawAmount = 0;
                });
                contextMenu.Items.Add(delItem);
            }
            else if (hitMirel != null)
            {
                var lbl = new System.Windows.Forms.ToolStripMenuItem($"Mirel návěstidlo #{hitMirel.SignalId} (Stav: {hitMirel.Value})") { Enabled = false };
                contextMenu.Items.Add(lbl);

                var toggleItem = new System.Windows.Forms.ToolStripMenuItem(
                    hitMirel.Value == "b" ? "Přepnout na nekódované ('a')" : "Přepnout na kódované ('b')",
                    null,
                    (s, e) =>
                    {
                        hitMirel.Value = hitMirel.Value == "b" ? "a" : "b";
                        skipDrawAmount = 0;
                    });
                contextMenu.Items.Add(toggleItem);

                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                var delItem = new System.Windows.Forms.ToolStripMenuItem("Smazat Mirel bod", null, (s, e) =>
                {
                    MirelPoints.Remove(hitMirel);
                    skipDrawAmount = 0;
                });
                contextMenu.Items.Add(delItem);
            }
            else
            {
                var addHeader = new System.Windows.Forms.ToolStripMenuItem("Vložit nový bod...") { Enabled = false };
                contextMenu.Items.Add(addHeader);

                var addVoltSub = new System.Windows.Forms.ToolStripMenuItem("Napěťový bod");
                addVoltSub.DropDownItems.Add("3000 V (3 kV DC)", null, (s, e) => AddNewVoltagePoint(clickLoc, 3000));
                addVoltSub.DropDownItems.Add("25000 V (25 kV AC)", null, (s, e) => AddNewVoltagePoint(clickLoc, 25000));
                addVoltSub.DropDownItems.Add("15000 V (15 kV AC)", null, (s, e) => AddNewVoltagePoint(clickLoc, 15000));
                addVoltSub.DropDownItems.Add("0 V (Bez napětí)", null, (s, e) => AddNewVoltagePoint(clickLoc, 0));
                contextMenu.Items.Add(addVoltSub);

                var addPssSub = new System.Windows.Forms.ToolStripMenuItem("Napájecí stanice");
                addPssSub.DropDownItems.Add("3 kV DC (0)", null, (s, e) => AddNewPowerSupplyStation(clickLoc, 0));
                addPssSub.DropDownItems.Add("25 kV AC (1)", null, (s, e) => AddNewPowerSupplyStation(clickLoc, 1));
                addPssSub.DropDownItems.Add("15 kV AC (2)", null, (s, e) => AddNewPowerSupplyStation(clickLoc, 2));
                contextMenu.Items.Add(addPssSub);

                var addMirelSub = new System.Windows.Forms.ToolStripMenuItem("Mirel bod");
                addMirelSub.DropDownItems.Add("Kódovaný ('b')", null, (s, e) => AddNewMirelPoint(clickLoc, "b"));
                addMirelSub.DropDownItems.Add("Nekódovaný ('a')", null, (s, e) => AddNewMirelPoint(clickLoc, "a"));
                contextMenu.Items.Add(addMirelSub);
            }

            contextMenu.Show(screenX, screenY);
        }

        private void AddNewVoltagePoint(WorldLocation loc, int voltage)
        {
            double latRad = 0, lonRad = 0;
            var wll = new Orts.Simulation.Common.WorldLatLon();
            wll.ConvertWTC(loc.TileX, loc.TileZ, loc.Location, ref latRad, ref lonRad);

            int maxId = VoltagePoints.Count > 0 ? VoltagePoints.Max(p => p.Id) : 0;
            VoltagePoints.Add(new RouteVoltagePoint
            {
                Id = maxId + 1,
                WorldLocation = loc,
                Latitude = latRad,
                Longitude = lonRad,
                Voltage = voltage
            });

            skipDrawAmount = 0;
        }

        private void AddNewPowerSupplyStation(WorldLocation loc, int powerSystem)
        {
            double latRad = 0, lonRad = 0;
            var wll = new Orts.Simulation.Common.WorldLatLon();
            wll.ConvertWTC(loc.TileX, loc.TileZ, loc.Location, ref latRad, ref lonRad);

            int maxId = PowerSupplyStations.Count > 0 ? PowerSupplyStations.Max(p => p.Id) : 0;
            PowerSupplyStations.Add(new RoutePowerSupplyStation
            {
                Id = maxId + 1,
                WorldLocation = loc,
                Latitude = latRad,
                Longitude = lonRad,
                PowerSystem = powerSystem
            });

            skipDrawAmount = 0;
        }

        private void AddNewMirelPoint(WorldLocation loc, string value)
        {
            double latRad = 0, lonRad = 0;
            var wll = new Orts.Simulation.Common.WorldLatLon();
            wll.ConvertWTC(loc.TileX, loc.TileZ, loc.Location, ref latRad, ref lonRad);

            int maxId = MirelPoints.Count > 0 ? MirelPoints.Max(p => p.SignalId) : 1000;
            MirelPoints.Add(new RouteMirelPoint
            {
                SignalId = maxId + 1,
                Value = value,
                WorldLocation = loc,
                Latitude = latRad,
                Longitude = lonRad
            });

            skipDrawAmount = 0;
        }
        #endregion

        #region Main game methods
        protected override void Update(GameTime gameTime)
        {
            if (!this.IsActive)
            {
                lostFocus = true;
                return;
            }

            TVUserInput.Update();
            if (lostFocus)
            {
                lostFocus = false;
                return;
            }

            fontManager.Update(GraphicsDevice);
            if (DrawTrackDB != null)
            {
                DrawTrackDB.ClearHighlightOverrides();
            }

            if (this.drawPathChart.IsActived)
            {
                if (TVUserInput.IsDown(TVUserCommands.ShiftLeft)) { drawPathChart.Shift(-1); skipDrawAmount = 0; }
                if (TVUserInput.IsDown(TVUserCommands.ShiftRight)) { drawPathChart.Shift(1); skipDrawAmount = 0; }
                if (TVUserInput.IsDown(TVUserCommands.ZoomIn)) { drawPathChart.Zoom(-1); skipDrawAmount = 0; }
                if (TVUserInput.IsDown(TVUserCommands.ZoomOut)) { drawPathChart.Zoom(1); skipDrawAmount = 0; }
            }
            else if (!this.menuControl.IsKeyboardFocusWithin)
            {
                if (TVUserInput.IsDown(TVUserCommands.ShiftLeft)) { DrawArea.ShiftLeft(); skipDrawAmount = 0; }
                if (TVUserInput.IsDown(TVUserCommands.ShiftRight)) { DrawArea.ShiftRight(); skipDrawAmount = 0; }
                if (TVUserInput.IsDown(TVUserCommands.ShiftUp)) { DrawArea.ShiftUp(); skipDrawAmount = 0; }
                if (TVUserInput.IsDown(TVUserCommands.ShiftDown)) { DrawArea.ShiftDown(); skipDrawAmount = 0; }

                if (TVUserInput.IsDown(TVUserCommands.ZoomIn)) { DrawArea.Zoom(-1); skipDrawAmount = 0; }
                if (TVUserInput.IsDown(TVUserCommands.ZoomOut)) { DrawArea.Zoom(1); skipDrawAmount = 0; }
            }

            if (TVUserInput.Changed)
            {
                skipDrawAmount = 0;
            }

            if (TVUserInput.IsPressed(TVUserCommands.Quit)) this.Quit();
            if (TVUserInput.IsPressed(TVUserCommands.ReloadRoute)) this.ReloadRoute();

            if (TVUserInput.IsPressed(TVUserCommands.ShiftToMouseLocation)) DrawArea.ShiftToLocation(DrawArea.MouseLocation);
            if (TVUserInput.IsPressed(TVUserCommands.ZoomInSlow)) DrawArea.Zoom(-1);
            if (TVUserInput.IsPressed(TVUserCommands.ZoomOutSlow)) DrawArea.Zoom(1);
            if (TVUserInput.IsPressed(TVUserCommands.ZoomToTile)) DrawArea.ZoomToTile();
            if (TVUserInput.IsPressed(TVUserCommands.ZoomReset))
            {
                DrawArea.ZoomReset(DrawTrackDB);
                drawAreaInset.ZoomReset(DrawTrackDB);
            }

            var mouseLocationAbsoluteX = Window.ClientBounds.Left + TVUserInput.MouseLocationX;
            var mouseLocationAbsoluteY = Window.ClientBounds.Top + TVUserInput.MouseLocationY;

            if (TVUserInput.IsMouseRightButtonPressed() && (PathEditor == null || !PathEditor.EditingIsActive))
            {
                if (Properties.Settings.Default.showVoltageMarkers ||
                    Properties.Settings.Default.showPowerSupplyStations ||
                    Properties.Settings.Default.showMirelPoints)
                {
                    ShowRouteMarkersContextMenu(mouseLocationAbsoluteX, mouseLocationAbsoluteY);
                }
            }

            if (DrawPATfile != null && Properties.Settings.Default.showPATfile)
            {
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPath)) DrawPATfile.ExtendPath();
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPathFull)) DrawPATfile.ExtendPathFull();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePath)) DrawPATfile.ReducePath();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePathFull)) DrawPATfile.ReducePathFull();
                if (TVUserInput.IsDown(TVUserCommands.ShiftToPathLocation)) DrawArea.ShiftToLocation(DrawPATfile.CurrentLocation);
            }

            if (PathEditor != null && Properties.Settings.Default.showTrainpath)
            {
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPath)) PathEditor.ExtendPath();
                if (TVUserInput.IsPressed(TVUserCommands.ExtendPathFull)) PathEditor.ExtendPathFull();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePath)) PathEditor.ReducePath();
                if (TVUserInput.IsPressed(TVUserCommands.ReducePathFull)) PathEditor.ReducePathFull();
                if (TVUserInput.IsDown(TVUserCommands.ShiftToPathLocation)) DrawArea.ShiftToLocation(PathEditor.CurrentLocation);

                if (TVUserInput.IsPressed(TVUserCommands.EditorUndo)) PathEditor.Undo();
                if (TVUserInput.IsPressed(TVUserCommands.EditorRedo)) PathEditor.Redo();
                if (TVUserInput.IsMouseXButton1Pressed()) PathEditor.Undo();
                if (TVUserInput.IsMouseXButton2Pressed()) PathEditor.Redo();
            }

            if (PathEditor != null && PathEditor.EditingIsActive)
            {
                if (TVUserInput.IsMouseRightButtonPressed())
                {
                    PathEditor.OnLeftMouseRelease();
                    PathEditor.PopupContextMenu(mouseLocationAbsoluteX, mouseLocationAbsoluteY);
                }

                PathEditor.DeterminePossibleActions(TVUserInput.IsDown(TVUserCommands.EditorTakesMouseClickDrag), TVUserInput.IsDown(TVUserCommands.EditorTakesMouseClickAction),
                    TVUserInput.MouseLocationX, TVUserInput.MouseLocationY);

                if (TVUserInput.IsPressed(TVUserCommands.PlaceEndPoint)) PathEditor.PlaceEndPoint();
                if (TVUserInput.IsPressed(TVUserCommands.PlaceWaitPoint)) PathEditor.PlaceWaitPoint();

                if (TVUserInput.IsMouseLeftButtonPressed())
                {
                    PathEditor.OnLeftMouseClick();
                }
                if (TVUserInput.IsMouseLeftButtonDown())
                {
                    PathEditor.OnLeftMouseMoved();
                }
                if (TVUserInput.IsMouseLeftButtonReleased())
                {
                    PathEditor.OnLeftMouseRelease();
                }

                if (TVUserInput.IsReleased(TVUserCommands.EditorTakesMouseClickDrag))
                {
                    PathEditor.OnLeftMouseCancel();
                }
                drawPathChart.DrawDynamics();
            }
            else if (drawLabels != null)
            {
                if (TVUserInput.IsPressed(TVUserCommands.AddLabel))
                {
                    drawLabels.AddLabel(mouseLocationAbsoluteX, mouseLocationAbsoluteY);
                }

                if (TVUserInput.IsDown(TVUserCommands.EditorTakesMouseClickDrag))
                {
                    if (TVUserInput.IsMouseLeftButtonPressed())
                    {
                        drawLabels.OnLeftMouseClick();
                    }
                    if (TVUserInput.IsMouseLeftButtonDown())
                    {
                        drawLabels.OnLeftMouseMoved();
                    }
                    if (TVUserInput.IsMouseLeftButtonReleased())
                    {
                        drawLabels.OnLeftMouseRelease();
                    }
                }
                if (TVUserInput.IsReleased(TVUserCommands.EditorTakesMouseClickDrag))
                {
                    drawLabels.OnLeftMouseCancel();
                }

                if (TVUserInput.IsMouseRightButtonPressed())
                {
                    drawLabels.PopupContextMenu(mouseLocationAbsoluteX, mouseLocationAbsoluteY);
                }
            }

            bool otherWindowHasMouse = menuControl.HasMouse() || drawPathChart.IsActived;
            if (!TVUserInput.IsDown(TVUserCommands.EditorTakesMouseClickDrag) && !otherWindowHasMouse)
            {
                if (draggedVoltagePoint == null && draggedPowerSupplyStation == null && draggedMirelPoint == null && TVUserInput.IsMouseMoved() && TVUserInput.IsMouseLeftButtonDown())
                {
                    DrawArea.ShiftArea(TVUserInput.MouseMoveX(), TVUserInput.MouseMoveY());
                }
            }

            if (TVUserInput.IsMouseWheelChanged())
            {
                int mouseWheelChange = TVUserInput.MouseWheelChange();
                if (!this.drawPathChart.IsActived)
                {
                    if (TVUserInput.IsDown(TVUserCommands.MouseZoomSlow))
                    {
                        DrawArea.Zoom(mouseWheelChange > 0 ? -1 : 1);
                    }
                    else
                    {
                        DrawArea.Zoom(-mouseWheelChange / 40);
                    }
                }
            }

            DrawArea.Update();

            // 1. Obsluha VoltagePoints
            if (VoltagePoints.Count > 0 && !this.menuControl.HasMouse())
            {
                if (TVUserInput.IsMouseLeftButtonPressed())
                {
                    RouteVoltagePoint bestCandidate = null;
                    float bestDistPixels = 15f;

                    foreach (var pt in VoltagePoints)
                    {
                        float distMeters = (float)Math.Sqrt(WorldLocation.GetDistanceSquared2D(pt.WorldLocation, DrawArea.MouseLocation));
                        float distPixels = (float)(distMeters * DrawArea.Scale);

                        if (distPixels < bestDistPixels)
                        {
                            bestDistPixels = distPixels;
                            bestCandidate = pt;
                        }
                    }

                    if (bestCandidate != null)
                    {
                        draggedVoltagePoint = bestCandidate;
                        VoltagePoints.Remove(bestCandidate);
                        VoltagePoints.Add(bestCandidate);
                    }
                }

                if (draggedVoltagePoint != null && TVUserInput.IsMouseLeftButtonDown())
                {
                    DrawArea.Update();
                    draggedVoltagePoint.WorldLocation = DrawArea.MouseLocation;

                    double latRad = 0, lonRad = 0;
                    var wll = new Orts.Simulation.Common.WorldLatLon();
                    if (wll.ConvertWTC(draggedVoltagePoint.WorldLocation.TileX,
                                       draggedVoltagePoint.WorldLocation.TileZ,
                                       draggedVoltagePoint.WorldLocation.Location,
                                       ref latRad, ref lonRad) == 1)
                    {
                        draggedVoltagePoint.Latitude = latRad;
                        draggedVoltagePoint.Longitude = lonRad;
                    }

                    skipDrawAmount = 0;
                }

                if (TVUserInput.IsMouseLeftButtonReleased() && draggedVoltagePoint != null)
                {
                    draggedVoltagePoint = null;
                }
            }

            // 2. Obsluha PowerSupplyStations
            if (PowerSupplyStations.Count > 0 && !this.menuControl.HasMouse() && draggedVoltagePoint == null)
            {
                if (TVUserInput.IsMouseLeftButtonPressed())
                {
                    RoutePowerSupplyStation bestCandidate = null;
                    float bestDistPixels = 15f;

                    foreach (var pt in PowerSupplyStations)
                    {
                        float distMeters = (float)Math.Sqrt(WorldLocation.GetDistanceSquared2D(pt.WorldLocation, DrawArea.MouseLocation));
                        float distPixels = (float)(distMeters * DrawArea.Scale);

                        if (distPixels < bestDistPixels)
                        {
                            bestDistPixels = distPixels;
                            bestCandidate = pt;
                        }
                    }

                    if (bestCandidate != null)
                    {
                        draggedPowerSupplyStation = bestCandidate;
                        PowerSupplyStations.Remove(bestCandidate);
                        PowerSupplyStations.Add(bestCandidate);
                    }
                }

                if (draggedPowerSupplyStation != null && TVUserInput.IsMouseLeftButtonDown())
                {
                    DrawArea.Update();
                    draggedPowerSupplyStation.WorldLocation = DrawArea.MouseLocation;

                    double latRad = 0, lonRad = 0;
                    var wll = new Orts.Simulation.Common.WorldLatLon();
                    if (wll.ConvertWTC(draggedPowerSupplyStation.WorldLocation.TileX,
                                       draggedPowerSupplyStation.WorldLocation.TileZ,
                                       draggedPowerSupplyStation.WorldLocation.Location,
                                       ref latRad, ref lonRad) == 1)
                    {
                        draggedPowerSupplyStation.Latitude = latRad;
                        draggedPowerSupplyStation.Longitude = lonRad;
                    }

                    skipDrawAmount = 0;
                }

                if (TVUserInput.IsMouseLeftButtonReleased() && draggedPowerSupplyStation != null)
                {
                    draggedPowerSupplyStation = null;
                }
            }

            // 3. Obsluha MirelPoints
            if (MirelPoints.Count > 0 && !this.menuControl.HasMouse() && draggedVoltagePoint == null && draggedPowerSupplyStation == null)
            {
                if (TVUserInput.IsMouseLeftButtonPressed())
                {
                    RouteMirelPoint bestCandidate = null;
                    float bestDistPixels = 15f;

                    foreach (var pt in MirelPoints)
                    {
                        float distMeters = (float)Math.Sqrt(WorldLocation.GetDistanceSquared2D(pt.WorldLocation, DrawArea.MouseLocation));
                        float distPixels = (float)(distMeters * DrawArea.Scale);

                        if (distPixels < bestDistPixels)
                        {
                            bestDistPixels = distPixels;
                            bestCandidate = pt;
                        }
                    }

                    if (bestCandidate != null)
                    {
                        draggedMirelPoint = bestCandidate;
                        MirelPoints.Remove(bestCandidate);
                        MirelPoints.Add(bestCandidate);
                    }
                }

                if (draggedMirelPoint != null && TVUserInput.IsMouseLeftButtonDown())
                {
                    DrawArea.Update();
                    draggedMirelPoint.WorldLocation = DrawArea.MouseLocation;

                    double latRad = 0, lonRad = 0;
                    var wll = new Orts.Simulation.Common.WorldLatLon();
                    if (wll.ConvertWTC(draggedMirelPoint.WorldLocation.TileX,
                                       draggedMirelPoint.WorldLocation.TileZ,
                                       draggedMirelPoint.WorldLocation.Location,
                                       ref latRad, ref lonRad) == 1)
                    {
                        draggedMirelPoint.Latitude = latRad;
                        draggedMirelPoint.Longitude = lonRad;
                    }

                    skipDrawAmount = 0;
                }

                if (TVUserInput.IsMouseLeftButtonReleased() && draggedMirelPoint != null)
                {
                    draggedMirelPoint = null;
                }
            }

            drawAreaInset.Update();
            drawAreaInset.Follow(DrawArea, 10f);

            if (TVUserInput.IsPressed(TVUserCommands.ToggleZoomAroundMouse)) menuControl.MenuToggleZoomingAroundMouse();

            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowTerrain)) menuControl.MenuToggleShowTerrain();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowDMTerrain)) menuControl.MenuToggleShowDMTerrain();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowPatchLines)) menuControl.MenuToggleShowPatchLines();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSignals)) menuControl.MenuToggleShowSignals();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSidings)) menuControl.MenuToggleShowSidings();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSidingNames)) menuControl.MenuToggleShowSidingNames();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowPlatforms)) menuControl.MenuToggleShowPlatforms();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowPlatformNames)) menuControl.MenuCirculatePlatformStationNames();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowSpeedLimits)) menuControl.MenuToggleShowSpeedLimits();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowMilePosts)) menuControl.MenuToggleShowMilePosts();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowTrainpath)) menuControl.MenuToggleShowTrainpath();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleShowPatFile)) menuControl.MenuToggleShowPatFile();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleHighlightTracks)) menuControl.MenuToggleHighlightTracks();
            if (TVUserInput.IsPressed(TVUserCommands.ToggleHighlightItems)) menuControl.MenuToggleHighlightItems();

            if (TVUserInput.IsPressed(TVUserCommands.MenuFile)) { menuControl.menuFile.Focus(); menuControl.menuFile.IsSubmenuOpen = true; }
            if (TVUserInput.IsPressed(TVUserCommands.MenuView)) { menuControl.menuView.Focus(); menuControl.menuView.IsSubmenuOpen = true; }
            if (TVUserInput.IsPressed(TVUserCommands.MenuTrackItems)) { menuControl.menuTrackItems.Focus(); menuControl.menuTrackItems.IsSubmenuOpen = true; }
            if (TVUserInput.IsPressed(TVUserCommands.MenuPreferences)) { menuControl.menuPreferences.Focus(); menuControl.menuPreferences.IsSubmenuOpen = true; }
            if (TVUserInput.IsPressed(TVUserCommands.MenuStatusbar)) { menuControl.menuStatusbar.Focus(); menuControl.menuStatusbar.IsSubmenuOpen = true; }
            if (TVUserInput.IsPressed(TVUserCommands.MenuPathEditor)) { menuControl.menuPathEditor.Focus(); menuControl.menuPathEditor.IsSubmenuOpen = true; }
            if (TVUserInput.IsPressed(TVUserCommands.MenuTerrain)) { menuControl.menuTerrain.Focus(); menuControl.menuTerrain.IsSubmenuOpen = true; }
            if (TVUserInput.IsPressed(TVUserCommands.MenuHelp)) { menuControl.menuHelp.Focus(); menuControl.menuHelp.IsSubmenuOpen = true; }

            if (TVUserInput.IsPressed(TVUserCommands.Debug)) RunDebug();

            base.Update(gameTime);
            HandleCommandLineArgs();
        }

        protected override void Draw(GameTime gameTime)
        {
            if (DrawTrackDB != null && Properties.Settings.Default.showInset)
            {
                drawAreaInset.DrawShadowTextures(DrawTrackDB.DrawTracks, DrawColors.colorsNormal.ClearWindowInset);
            }

            if (--skipDrawAmount > 0)
            {
                return;
            }

            GraphicsDevice.Clear(DrawColors.colorsNormal.ClearWindow);
            if (DrawTrackDB == null) return;

            spriteBatch.Begin();

            if (drawTerrain != null) { drawTerrain.Draw(DrawArea); }
            drawWorldTiles.Draw(DrawArea);
            DrawArea.DrawTileGrid();
            if (drawTerrain != null) { drawTerrain.DrawPatchLines(DrawArea); }

            DrawTrackDB.DrawRoads(DrawArea);
            DrawTrackDB.DrawTracks(DrawArea);
            DrawTrackDB.DrawTrackHighlights(DrawArea, true);

            DrawTrackDB.DrawJunctionAndEndNodes(DrawArea);

            if (Properties.Settings.Default.showInset)
            {
                drawAreaInset.DrawBackground(DrawColors.colorsNormal.ClearWindowInset);
                drawAreaInset.DrawShadowedTextures();
                DrawTrackDB.DrawTrackHighlights(drawAreaInset, false);
                drawAreaInset.DrawBorder(Color.Red, DrawArea);
                drawAreaInset.DrawBorder(Color.Black);
            }

            if (DrawMultiplePaths != null) DrawMultiplePaths.Draw(DrawArea);
            if (DrawPATfile != null && Properties.Settings.Default.showPATfile) DrawPATfile.Draw(DrawArea);
            if (PathEditor != null && Properties.Settings.Default.showTrainpath) PathEditor.Draw(DrawArea);
            drawEditorAction.Draw(PathEditor);

            DrawTrackDB.DrawRoadTrackItems(DrawArea);
            DrawTrackDB.DrawTrackItems(DrawArea);
            DrawTrackDB.DrawItemHighlights(DrawArea);

            // Vykreslení VoltagePoints
            if (Properties.Settings.Default.showVoltageMarkers && VoltagePoints.Count > 0)
            {
                for (int i = 0; i < VoltagePoints.Count; i++)
                {
                    var pt = VoltagePoints[i];

                    Color col;
                    switch (pt.Voltage)
                    {
                        case 3000: col = Color.Yellow; break;
                        case 25000: col = Color.Red; break;
                        case 15000: col = Color.Cyan; break;
                        case 0: col = Color.White; break;
                        default: col = Color.Orange; break;
                    }

                    if (pt == draggedVoltagePoint)
                    {
                        DrawArea.DrawTexture(pt.WorldLocation, "disc", 10f, 12, 24, Color.Lime);
                    }
                    else
                    {
                        DrawArea.DrawTexture(pt.WorldLocation, "disc", 6f, 8, 16, col);
                    }

                    DrawArea.DrawExpandingString(pt.WorldLocation, $"{pt.Voltage} V");
                }
            }

            // Vykreslení PowerSupplyStations
            if (Properties.Settings.Default.showPowerSupplyStations && PowerSupplyStations.Count > 0)
            {
                foreach (var pt in PowerSupplyStations)
                {
                    Color col;
                    string sysName;
                    switch (pt.PowerSystem)
                    {
                        case 0: col = Color.Yellow; sysName = "3 kV DC"; break;
                        case 1: col = Color.Red; sysName = "25 kV AC"; break;
                        case 2: col = Color.Cyan; sysName = "15 kV AC"; break;
                        default: col = Color.Orange; sysName = $"{pt.PowerSystem} (Unknown)"; break;
                    }

                    if (pt == draggedPowerSupplyStation)
                    {
                        DrawArea.DrawTexture(pt.WorldLocation, "disc", 12f, 16, 28, Color.Lime);
                    }
                    else
                    {
                        DrawArea.DrawTexture(pt.WorldLocation, "disc", 8f, 10, 20, col);
                    }

                    DrawArea.DrawExpandingString(pt.WorldLocation, $"PSS: {sysName}");
                }
            }

            // Vykreslení MirelPoints
            if (Properties.Settings.Default.showMirelPoints && MirelPoints.Count > 0)
            {
                foreach (var pt in MirelPoints)
                {
                    bool isCoded = pt.Value.Equals("b", StringComparison.OrdinalIgnoreCase);
                    Color col = isCoded ? Color.LightGreen : Color.White;
                    string statusText = isCoded ? "ON" : "OFF";

                    if (pt == draggedMirelPoint)
                    {
                        DrawArea.DrawTexture(pt.WorldLocation, "disc", 10f, 14, 24, Color.Lime);
                    }
                    else
                    {
                        DrawArea.DrawTexture(pt.WorldLocation, "disc", 6f, 8, 16, col);
                    }

                    DrawArea.DrawExpandingString(pt.WorldLocation, $"Mirel #{pt.SignalId}: {statusText}");
                }
            }

            if (Properties.Settings.Default.showVoltageMarkers && PantoDownMarkers.Count > 0)
            {
                foreach (var marker in PantoDownMarkers)
                {
                    // 1. Vykreslení ikony/symbolu
                    // Lze použít "disc", "circle", "hazard" nebo vlastní texturu
                    DrawArea.DrawTexture(marker.WorldLocation, "hazard", 8f, 10, 20, Color.Yellow);

                    // 2. Vykreslení popisku
                    DrawArea.DrawExpandingString(marker.WorldLocation, catalog.GetString("Lower pantograph"));
                }
            }

            if (Properties.Settings.Default.showVoltageMarkers && PantoUpMarkers.Count > 0)
            {
                foreach (var marker in PantoUpMarkers)
                {
                    // 1. Vykreslení ikony/symbolu
                    // Lze použít "disc", "circle", "hazard" nebo vlastní texturu
                    DrawArea.DrawTexture(marker.WorldLocation, "hazard", 8f, 10, 20, Color.Yellow);

                    // 2. Vykreslení popisku
                    DrawArea.DrawExpandingString(marker.WorldLocation, catalog.GetString("Raise pantograph"));
                }
            }

            CalculateFPS(gameTime);
            statusBarControl.Update(this, DrawArea.MouseLocation);

            drawScaleRuler.Draw();
            drawLongitudeLatitude.Draw(DrawArea.MouseLocation);
            drawLabels.Draw(DrawArea);

            DebugWindow.DrawAll();
            spriteBatch.End();

            base.Draw(gameTime);
            skipDrawAmount = maxSkipDrawAmount;
        }
        #endregion

        #region User actions
        public void SetAliasing()
        {
            graphics.PreferMultiSampling = Properties.Settings.Default.doAntiAliasing;
        }

        public void ShowPathChart() => this.drawPathChart.Open();

        public void Quit()
        {
            string message = String.Empty;
            if (PathEditor != null && PathEditor.HasModifiedPath)
            {
                message = catalog.GetString("The path you are working on has un-saved changes.\n");
            }
            message += catalog.GetString("Do you really want to Quit?");

            if (MessageBox.Show(message, "Question", MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                this.Exit();
            }
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            ScreenW = Window.ClientBounds.Width;
            ScreenH = Window.ClientBounds.Height;
            if (menuControl == null || statusBarControl == null || ScreenW == 0 || ScreenH == 0) return;
            SetSubwindowSizes();
        }

        public bool SetTerrainVisibility(bool isVisible, bool isVisibleDM)
        {
            if (drawTerrain == null) return false;
            drawTerrain.SetTerrainVisibility(isVisible, isVisibleDM, DrawArea);
            return true;
        }

        public bool SetPatchLineVisibility(bool showPatchLines)
        {
            if (drawTerrain == null) return false;
            drawTerrain.SetPatchLineVisibility(showPatchLines);
            return true;
        }

        internal void LoadLabels() => drawLabels?.LoadLabels();
        internal void SaveLabels() => drawLabels?.SaveLabels();
        internal void EditMetaData() => PathEditor?.EditMetaData(Window.ClientBounds.Left + 50, Window.ClientBounds.Top + 20);
        internal void ReversePath() => PathEditor?.ReversePath(Window.ClientBounds.Left + 50, Window.ClientBounds.Top + 20);
        internal void SetTerrainReduction() => drawTerrain?.SetTerrainReduction();
        #endregion

        #region Folder, Route and Marker I/O methods
        void HandleCommandLineArgs()
        {
            if (this.commandLineArgs.Length == 0) return;
            string givenPathOrFile = this.commandLineArgs[0];
            this.commandLineArgs = new string[0];

            string routeFolder = givenPathOrFile;
            bool givenFileIsPat = false;
            if (System.IO.Directory.Exists(givenPathOrFile)) { }
            else if (System.IO.File.Exists(givenPathOrFile))
            {
                var extension = System.IO.Path.GetExtension(givenPathOrFile).ToLower();
                switch (extension)
                {
                    case ".trk":
                    case ".tdb":
                        routeFolder = System.IO.Path.GetDirectoryName(givenPathOrFile);
                        break;
                    case ".rdb":
                        routeFolder = System.IO.Path.GetDirectoryName(givenPathOrFile);
                        Properties.Settings.Default.drawRoads = true;
                        menuControl.InitUserSettings();
                        break;
                    case ".pat":
                        routeFolder = System.IO.Directory.GetParent(System.IO.Path.GetDirectoryName(givenPathOrFile).ToString()).ToString();
                        givenFileIsPat = true;
                        break;
                    default:
                        MessageBox.Show(string.Format(catalog.GetString("Route cannot be loaded.\nExtension {0} is not supported"), extension));
                        return;
                }
            }
            else
            {
                MessageBox.Show(string.Format(catalog.GetString("Route cannot be loaded.\n{0} does not exist"), givenPathOrFile));
                return;
            }

            string installFolder = System.IO.Directory.GetParent(System.IO.Directory.GetParent(routeFolder).ToString()).ToString();
            if (!SetSelectedInstallFolder(installFolder))
            {
                MessageBox.Show(string.Format(catalog.GetString("Route cannot be loaded.\nWhile trying to open {0} the folder {1} was inferred as (MSTS or similar) install folder but does not contain expected files"), givenPathOrFile, installFolder));
                return;
            }

            foreach (ORTS.Menu.Route route in this.Routes)
            {
                if (route.Path.ToUpper() == routeFolder.ToUpper())
                {
                    SetRoute(route);
                    if (!givenFileIsPat) return;
                    foreach (Path availablePath in Paths)
                    {
                        if (availablePath.FilePath.ToUpper() == givenPathOrFile.ToUpper())
                        {
                            SetPath(availablePath);
                            menuControl.InitUserSettings();
                            return;
                        }
                    }
                }
            }

            MessageBox.Show(string.Format(catalog.GetString("Route cannot be loaded.\n{0} somehow could not be translated into a loadable route"), givenPathOrFile));
        }

        public bool SelectInstallFolder()
        {
            if (!CanDiscardModifiedPath()) return false;
            string folderPath = "";

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (InstallFolder != null) folderBrowserDialog.SelectedPath = InstallFolder.Path;
            folderBrowserDialog.ShowNewFolderButton = false;
            DialogResult dialogResult = folderBrowserDialog.ShowDialog();

            if (dialogResult == DialogResult.OK) folderPath = folderBrowserDialog.SelectedPath;
            if (String.IsNullOrEmpty(folderPath)) return false;

            return SetSelectedInstallFolder(folderPath);
        }

        private bool SetSelectedInstallFolder(string folderPath)
        {
            drawTerrain?.Clear();
            Folder newInstallFolder = new Folder("installFolder", folderPath);
            bool foundroutes = FindRoutes(newInstallFolder);
            if (!foundroutes)
            {
                MessageBox.Show(folderPath + ": " + catalog.GetString("Directory is not a valid install directory.\nThe install directory needs to contain ROUTES, GLOBAL, ..."));
                return false;
            }

            InstallFolder = newInstallFolder;
            CurrentRoute = null;
            DrawTrackDB = null;
            PathEditor = null;
            DrawMultiplePaths = null;

            Properties.Settings.Default.installDirectory = folderPath;
            Properties.Settings.Default.Save();
            return true;
        }

        private bool FindRoutes(Folder newInstallFolder)
        {
            if (newInstallFolder == null) return false;
            List<Route> newRoutes = Route.GetRoutes(newInstallFolder).OrderBy(r => r.ToString()).ToList();

            if (newRoutes.Count > 0)
            {
                DefaultRoute = newRoutes[0];
                foreach (Route tryRoute in newRoutes)
                {
                    string dirName = tryRoute.Path.Split('\\').Last();
                    if (dirName == Properties.Settings.Default.defaultRoute)
                    {
                        DefaultRoute = tryRoute;
                    }
                }

                Routes = new Collection<Route>(newRoutes);
                menuControl.PopulateRoutes();
                return true;
            }
            return false;
        }

        public void ReloadRoute()
        {
            if (CurrentRoute != null) SetRoute(CurrentRoute);
            else SetRoute(DefaultRoute);
        }

        public void SetRoute(Route newRoute)
        {
            if (newRoute == null) return;
            if (!CanDiscardModifiedPath()) return;

            DrawLoadingMessage(catalog.GetString("Loading route..."));
            MessageDelegate messageHandler = new MessageDelegate(DrawLoadingMessage);

            drawTerrain?.Clear();

            try
            {
                RouteData = new RouteData(newRoute.Path, messageHandler);
                DrawTrackDB = new DrawTrackDB(this.RouteData, messageHandler);
                drawLabels = new DrawLabels(fontManager.DefaultFont.Height);
                CurrentRoute = newRoute;

                LoadVoltageMarkers(newRoute.Path);
                LoadPowerSupplyStations(newRoute.Path);
                LoadMirelPoints(newRoute.Path);

                // Načtení návěstí sběrače podle nastavených názvů modelů
                ReloadPantoSigns();

                Properties.Settings.Default.defaultRoute = CurrentRoute.Path.Split('\\').Last();
                if (Properties.Settings.Default.zoomRoutePath != CurrentRoute.Path)
                {
                    Properties.Settings.Default.zoomScale = -1;
                }
                Properties.Settings.Default.Save();
                DrawArea.ZoomReset(DrawTrackDB);
                drawAreaInset.ZoomReset(DrawTrackDB);
                SetTitle();
            }
            catch
            {
                MessageBox.Show(catalog.GetString("Route cannot be loaded. Sorry"));
            }

            if (CurrentRoute == null) return;

            PathEditor = null;
            DrawMultiplePaths = null;
            try { FindPaths(); } catch { }

            try
            {
                drawWorldTiles.SetRoute(CurrentRoute.Path);
                drawTerrain = new DrawTerrain(CurrentRoute.Path, messageHandler, drawWorldTiles);
                drawTerrain.LoadContent(GraphicsDevice);
                menuControl.MenuSetShowTerrain(false);
                menuControl.MenuSetShowDMTerrain(false);
            }
            catch { }

            menuControl.PopulatePlatforms();
            menuControl.PopulateStations();
            menuControl.PopulateSidings();
        }

        void SetTitle()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyTitleAttribute assemblyTitle = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0] as AssemblyTitleAttribute;
            Window.Title = assemblyTitle.Title + ": " + RouteData.RouteName;
        }

        public void SaveVoltageMarkers(string outputFilePath = null)
        {
            if (CurrentRoute == null || VoltagePoints.Count == 0) return;

            if (string.IsNullOrEmpty(outputFilePath))
            {
                outputFilePath = System.IO.Path.Combine(CurrentRoute.Path, "VoltageChangeMarkers.xml");
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<VoltageChangeMarkers>");

            int idCounter = 1;
            foreach (var pt in VoltagePoints)
            {
                sb.AppendLine("  <VoltageChangeMarker>");
                sb.AppendLine($"    <Id>{(pt.Id > 0 ? pt.Id : idCounter++)}</Id>");
                // Prohozeno podle MSTS konvence: v tagu Latitude je Longitude a v tagu Longitude je Latitude
                sb.AppendLine($"    <Latitude>{pt.Longitude.ToString("0.000000000", CultureInfo.InvariantCulture)}</Latitude>");
                sb.AppendLine($"    <Longitude>{pt.Latitude.ToString("0.000000000", CultureInfo.InvariantCulture)}</Longitude>");
                sb.AppendLine($"    <Voltage>{pt.Voltage}</Voltage>");
                sb.AppendLine("  </VoltageChangeMarker>");
            }

            sb.AppendLine("</VoltageChangeMarkers>");

            System.IO.File.WriteAllText(outputFilePath, sb.ToString(), System.Text.Encoding.UTF8);
            System.Windows.Forms.MessageBox.Show(catalog.GetString($"Voltage Markers were successfully saved to:\n{outputFilePath}"), catalog.GetString("Saved"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        }

        public void SavePowerSupplyStations(string outputFilePath = null)
        {
            if (CurrentRoute == null || PowerSupplyStations.Count == 0) return;

            if (string.IsNullOrEmpty(outputFilePath))
            {
                outputFilePath = System.IO.Path.Combine(CurrentRoute.Path, "PowerSupplyStations.xml");
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<PowerSupplyStations>");

            int idCounter = 1;
            foreach (var pt in PowerSupplyStations)
            {
                sb.AppendLine("  <SupplyStation>");
                sb.AppendLine($"    <Id>{(pt.Id > 0 ? pt.Id : idCounter++)}</Id>");
                // Prohozeno: v tagu Latitude je Longitude a naopak
                sb.AppendLine($"    <Latitude>{pt.Longitude.ToString("0.000000000", CultureInfo.InvariantCulture)}</Latitude>");
                sb.AppendLine($"    <Longitude>{pt.Latitude.ToString("0.000000000", CultureInfo.InvariantCulture)}</Longitude>");
                sb.AppendLine($"    <PowerSystem>{pt.PowerSystem}</PowerSystem>");
                sb.AppendLine("  </SupplyStation>");
            }

            sb.AppendLine("</PowerSupplyStations>");

            System.IO.File.WriteAllText(outputFilePath, sb.ToString(), System.Text.Encoding.UTF8);
            System.Windows.Forms.MessageBox.Show(
                catalog.GetString($"Power Supply Stations were successfully saved to:\n{outputFilePath}"),
                catalog.GetString("Saved"),
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }

        public void SaveMirelPoints(string outputFilePath = null)
        {
            if (CurrentRoute == null || MirelPoints.Count == 0) return;

            if (string.IsNullOrEmpty(outputFilePath))
            {
                outputFilePath = System.IO.Path.Combine(CurrentRoute.Path, "MirelDb.xml");
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<MirelDb>");

            foreach (var pt in MirelPoints)
            {
                sb.AppendLine("  <Signal>");
                sb.AppendLine($"    <Id>{pt.SignalId}</Id>");
                sb.AppendLine($"    <Value>{pt.Value}</Value>");
                if (pt.Latitude != 0 || pt.Longitude != 0)
                {
                    // Prohozeno: v tagu Latitude je Longitude a naopak
                    sb.AppendLine($"    <Latitude>{pt.Longitude.ToString("0.000000000", CultureInfo.InvariantCulture)}</Latitude>");
                    sb.AppendLine($"    <Longitude>{pt.Latitude.ToString("0.000000000", CultureInfo.InvariantCulture)}</Longitude>");
                }
                sb.AppendLine("  </Signal>");
            }

            sb.AppendLine("</MirelDb>");

            System.IO.File.WriteAllText(outputFilePath, sb.ToString(), System.Text.Encoding.UTF8);
            System.Windows.Forms.MessageBox.Show(
                catalog.GetString($"Mirel points were successfully saved to:\n{outputFilePath}"),
                catalog.GetString("Saved"),
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }

        void LoadVoltageMarkers(string routePath)
        {
            VoltagePoints.Clear();
            string filePath = System.IO.Path.Combine(routePath, "VoltageChangeMarkers.xml");
            if (!System.IO.File.Exists(filePath))
                return;

            var lines = System.IO.File.ReadAllLines(filePath);
            var rawMarkers = new List<Tuple<double, double, int>>();

            double curLat = 0;
            double curLon = 0;
            int curVoltage = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.IndexOf("<Latitude>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string s = ExtractTagValue(line, "Latitude").Replace(',', '.');
                    double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out curLat);
                }
                else if (line.IndexOf("<Longitude>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string s = ExtractTagValue(line, "Longitude").Replace(',', '.');
                    double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out curLon);
                }
                else if (line.IndexOf("<Voltage>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string s = ExtractTagValue(line, "Voltage");
                    int.TryParse(s, out curVoltage);
                }
                else if (line.IndexOf("</VoltageChangeMarker>", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         line.IndexOf("</Marker>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (curLat != 0 || curLon != 0)
                    {
                        // Prohození: v tagu Latitude je uložen Longitude a v tagu Longitude je Latitude
                        double finalLat = curLon;
                        double finalLon = curLat;

                        // Zpětná kompatibilita pro staré soubory ve stupních
                        if (Math.Abs(curLat) > Math.PI || Math.Abs(curLon) > Math.PI)
                        {
                            if (curLat < 30.0 && curLon > 40.0)
                            {
                                double tmp = finalLat;
                                finalLat = finalLon;
                                finalLon = tmp;
                            }
                            finalLat *= (Math.PI / 180.0);
                            finalLon *= (Math.PI / 180.0);
                        }

                        rawMarkers.Add(Tuple.Create(finalLat, finalLon, curVoltage));
                    }
                    curLat = 0;
                    curLon = 0;
                    curVoltage = 0;
                }
            }

            if (rawMarkers.Count == 0) return;

            var worldLatLon = new Orts.Simulation.Common.WorldLatLon();
            int idCounter = 1;

            foreach (var marker in rawMarkers)
            {
                double mLat = marker.Item1;
                double mLon = marker.Item2;
                int mVolt = marker.Item3;

                int tileX = 0, tileZ = 0;
                float locX = 0, locZ = 0;

                // Přímý přesný převod z radiánů zpět na WorldLocation
                worldLatLon.ConvertCTW(mLat, mLon, out tileX, out tileZ, out locX, out locZ);

                VoltagePoints.Add(new RouteVoltagePoint
                {
                    Id = idCounter++,
                    Latitude = mLat,
                    Longitude = mLon,
                    Voltage = mVolt,
                    WorldLocation = new WorldLocation(tileX, tileZ, locX, 0, locZ)
                });
            }
        }

        void LoadPowerSupplyStations(string routePath)
        {
            PowerSupplyStations.Clear();
            string filePath = System.IO.Path.Combine(routePath, "PowerSupplyStations.xml");
            if (!System.IO.File.Exists(filePath))
                return;

            var lines = System.IO.File.ReadAllLines(filePath);
            var rawStations = new List<Tuple<int, double, double, int>>();

            int curId = 0;
            double curLat = 0;
            double curLon = 0;
            int curPowerSystem = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.IndexOf("<Id>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int.TryParse(ExtractTagValue(line, "Id"), out curId);
                }
                else if (line.IndexOf("<Latitude>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string s = ExtractTagValue(line, "Latitude").Replace(',', '.');
                    double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out curLat);
                }
                else if (line.IndexOf("<Longitude>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string s = ExtractTagValue(line, "Longitude").Replace(',', '.');
                    double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out curLon);
                }
                else if (line.IndexOf("<PowerSystem>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int.TryParse(ExtractTagValue(line, "PowerSystem"), out curPowerSystem);
                }
                else if (line.IndexOf("</SupplyStation>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (curLat != 0 || curLon != 0)
                    {
                        // Prohození: v tagu Latitude je uložen Longitude a v tagu Longitude je Latitude
                        double finalLat = curLon;
                        double finalLon = curLat;

                        if (Math.Abs(curLat) > Math.PI || Math.Abs(curLon) > Math.PI)
                        {
                            if (curLat < 30.0 && curLon > 40.0)
                            {
                                double tmp = finalLat;
                                finalLat = finalLon;
                                finalLon = tmp;
                            }
                            finalLat *= (Math.PI / 180.0);
                            finalLon *= (Math.PI / 180.0);
                        }

                        rawStations.Add(Tuple.Create(curId, finalLat, finalLon, curPowerSystem));
                    }
                    curId = 0;
                    curLat = 0;
                    curLon = 0;
                    curPowerSystem = 0;
                }
            }

            if (rawStations.Count == 0) return;

            var worldLatLon = new Orts.Simulation.Common.WorldLatLon();
            int fallbackId = 1;

            foreach (var st in rawStations)
            {
                int sId = st.Item1 > 0 ? st.Item1 : fallbackId++;
                double sLat = st.Item2;
                double sLon = st.Item3;
                int sSys = st.Item4;

                int tileX = 0, tileZ = 0;
                float locX = 0, locZ = 0;

                // Přímý přesný převod z radiánů zpět na WorldLocation
                worldLatLon.ConvertCTW(sLat, sLon, out tileX, out tileZ, out locX, out locZ);

                PowerSupplyStations.Add(new RoutePowerSupplyStation
                {
                    Id = sId,
                    Latitude = sLat,
                    Longitude = sLon,
                    PowerSystem = sSys,
                    WorldLocation = new WorldLocation(tileX, tileZ, locX, 0, locZ)
                });
            }
        }

        void LoadMirelPoints(string routePath)
        {
            MirelPoints.Clear();
            string filePath = System.IO.Path.Combine(routePath, "MirelDb.xml");
            if (!System.IO.File.Exists(filePath))
                return;

            var lines = System.IO.File.ReadAllLines(filePath);
            var rawSignals = new List<Tuple<int, string, double, double>>();

            int curId = 0;
            string curVal = "";
            double curLat = 0;
            double curLon = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.IndexOf("<Id>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int.TryParse(ExtractTagValue(line, "Id"), out curId);
                }
                else if (line.IndexOf("<Value>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    curVal = ExtractTagValue(line, "Value");
                }
                else if (line.IndexOf("<Latitude>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string s = ExtractTagValue(line, "Latitude").Replace(',', '.');
                    double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out curLat);
                }
                else if (line.IndexOf("<Longitude>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string s = ExtractTagValue(line, "Longitude").Replace(',', '.');
                    double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out curLon);
                }
                else if (line.IndexOf("</Signal>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (curId > 0 && !string.IsNullOrEmpty(curVal))
                    {
                        rawSignals.Add(Tuple.Create(curId, curVal, curLat, curLon));
                    }
                    curId = 0;
                    curVal = "";
                    curLat = 0;
                    curLon = 0;
                }
            }

            if (rawSignals.Count == 0) return;

            var worldLatLon = new Orts.Simulation.Common.WorldLatLon();

            foreach (var sig in rawSignals)
            {
                int sId = sig.Item1;
                string sVal = sig.Item2;
                // V XML byl v Latitude uložen Longitude a v Longitude Latitude:
                double sLat = sig.Item4;
                double sLon = sig.Item3;
                WorldLocation loc = WorldLocation.None;

                if (Math.Abs(sLat) > Math.PI || Math.Abs(sLon) > Math.PI)
                {
                    sLat *= (Math.PI / 180.0);
                    sLon *= (Math.PI / 180.0);
                }

                // Pokud máme přesné souřadnice, přímo je zrekonstruujeme
                if (sLat != 0 || sLon != 0)
                {
                    int tileX = 0, tileZ = 0;
                    float locX = 0, locZ = 0;
                    worldLatLon.ConvertCTW(sLat, sLon, out tileX, out tileZ, out locX, out locZ);
                    loc = new WorldLocation(tileX, tileZ, locX, 0, locZ);
                }
                else
                {
                    // Záložní načtení z TrItemTable jen v případě, že v XML souřadnice zcela chybí
                    if (RouteData?.TrackDB?.TrItemTable != null && sId >= 0 && sId < RouteData.TrackDB.TrItemTable.Length)
                    {
                        var item = RouteData.TrackDB.TrItemTable[sId];
                        if (item != null)
                        {
                            loc = new WorldLocation(item.TileX, item.TileZ, item.X, item.Y, item.Z);
                            double latRad = 0, lonRad = 0;
                            if (worldLatLon.ConvertWTC(loc.TileX, loc.TileZ, loc.Location, ref latRad, ref lonRad) == 1)
                            {
                                sLat = latRad;
                                sLon = lonRad;
                            }
                        }
                    }
                }

                if (loc != WorldLocation.None)
                {
                    MirelPoints.Add(new RouteMirelPoint
                    {
                        SignalId = sId,
                        Value = sVal,
                        WorldLocation = loc,
                        Latitude = sLat,
                        Longitude = sLon
                    });
                }
            }
        }

        string ExtractTagValue(string source, string tag)
        {
            int start = source.IndexOf("<" + tag + ">", StringComparison.OrdinalIgnoreCase);
            int end = source.IndexOf("</" + tag + ">", StringComparison.OrdinalIgnoreCase);
            if (start >= 0 && end > start)
            {
                start += tag.Length + 2;
                return source.Substring(start, end - start).Trim();
            }
            return "";
        }

        public void FindSceneryShapes(string routePath, string targetDownShapeName, string targetUpShapeName)
        {
            PantoDownMarkers.Clear();
            PantoUpMarkers.Clear();

            string worldDir = System.IO.Path.Combine(routePath, "WORLD");
            if (!System.IO.Directory.Exists(worldDir)) return;

            string[] wFiles;
            try
            {
                wFiles = System.IO.Directory.GetFiles(worldDir, "*.w");
            }
            catch
            {
                return;
            }

            string downTarget = targetDownShapeName?.Trim().ToLower() ?? "";
            string upTarget = targetUpShapeName?.Trim().ToLower() ?? "";

            bool searchDown = !string.IsNullOrEmpty(downTarget);
            bool searchUp = !string.IsNullOrEmpty(upTarget);

            if (!searchDown && !searchUp) return;

            var tokens = new List<TokenID> { TokenID.Static };

            int processed = 0;
            foreach (var file in wFiles)
            {
                processed++;
                if (processed % 100 == 0)
                {
                    DrawLoadingMessage(string.Format(catalog.GetString("Searching route files ({0}/{1})..."), processed, wFiles.Length));
                }

                WorldFile wFile;
                try
                {
                    wFile = new WorldFile(file, tokens);
                }
                catch
                {
                    continue;
                }

                if (wFile.Tr_Worldfile == null) continue;

                int tileX = wFile.TileX;
                int tileZ = wFile.TileZ;

                foreach (var obj in wFile.Tr_Worldfile)
                {
                    if (obj is StaticObj staticObj && !string.IsNullOrEmpty(staticObj.FileName))
                    {
                        string objName = staticObj.FileName.ToLower();

                        bool isDown = searchDown && objName == downTarget;
                        bool isUp = searchUp && objName == upTarget;

                        if (isDown || isUp)
                        {
                            var loc = new WorldLocation(tileX, tileZ, staticObj.Position.X, staticObj.Position.Y, staticObj.Position.Z);
                            loc.Normalize();

                            if (isDown)
                            {
                                PantoDownMarkers.Add(new SceneryShapeMarker
                                {
                                    ShapeName = staticObj.FileName,
                                    WorldLocation = loc
                                });
                            }

                            if (isUp)
                            {
                                PantoUpMarkers.Add(new SceneryShapeMarker
                                {
                                    ShapeName = staticObj.FileName,
                                    WorldLocation = loc
                                });
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Path methods
        private void FindPaths()
        {
            List<Path> newPaths = Path.GetPaths(CurrentRoute, true).OrderBy(r => r.Name).ToList();
            Paths = new Collection<Path>(newPaths);
            menuControl.PopulatePaths();
            SetPath(null);
            DrawMultiplePaths = new DrawMultiplePaths(this.RouteData, Paths);
        }

        internal void SetPath(Path path)
        {
            if (!CanDiscardModifiedPath()) return;

            if (path == null)
            {
                DrawPATfile = null;
                PathEditor = null;
                drawPathChart.Close();
            }
            else
            {
                DrawLoadingMessage(catalog.GetString("Loading .pat file ..."));
                DrawPATfile = new DrawPATfile(path);

                DrawLoadingMessage(catalog.GetString("Processing .pat file ..."));
                PathEditor = new PathEditor(this.RouteData, this.DrawTrackDB, path);
                drawPathChart.SetPathEditor(this.RouteData, this.PathEditor);

                DrawLoadingMessage(" ...");
            }
        }

        internal void NewPath()
        {
            if (!CanDiscardModifiedPath()) return;
            string pathsDirectory = System.IO.Path.Combine(CurrentRoute.Path, "PATHS");
            PathEditor = new PathEditor(this.RouteData, this.DrawTrackDB, pathsDirectory);
            drawPathChart.SetPathEditor(this.RouteData, this.PathEditor);
            DrawPATfile = null;
            menuControl.SetEnableEditing();
            EditMetaData();
        }

        bool CanDiscardModifiedPath()
        {
            if (PathEditor == null) return true;
            if (!PathEditor.HasModifiedPath) return true;
            DialogResult dialogResult = MessageBox.Show(
                        catalog.GetString("Path has been modified. Loading a new path will discard changes.") + "\n" +
                        catalog.GetString("Do you want to continue?"),
                        catalog.GetString("Trackviewer Path Editor"), MessageBoxButtons.OKCancel,
                        System.Windows.Forms.MessageBoxIcon.Question);
            return (dialogResult == DialogResult.OK);
        }
        #endregion

        #region Centering methods
        public void CenterAroundTrackNode(int trackNumberIndex)
        {
            CenterAround(DrawTrackDB.TrackNodeHighlightOverride(trackNumberIndex));
        }

        public void CenterAroundTrackNodeRoad(int trackNumberIndex)
        {
            CenterAround(DrawTrackDB.TrackNodeHighlightOverrideRoad(trackNumberIndex));
        }

        public void CenterAroundTrackItem(int trackItemIndex)
        {
            WorldLocation itemLocation = DrawTrackDB.TrackItemHighlightOverride(trackItemIndex);
            if (itemLocation == WorldLocation.None) return;
            CenterAround(itemLocation);
        }

        public void CenterAroundTrackItemRoad(int trackItemIndex)
        {
            WorldLocation itemLocation = DrawTrackDB.TrackItemHighlightOverrideRoad(trackItemIndex);
            if (itemLocation == WorldLocation.None) return;
            CenterAround(itemLocation);
        }

        public void CenterAround(WorldLocation centerLocation)
        {
            if (centerLocation == WorldLocation.None) return;

            DrawArea.ShiftToLocation(centerLocation);
            DrawArea.Update();
            DrawArea.MouseLocation = centerLocation;
            drawAreaInset.Follow(DrawArea, 10f);
            BeginDraw();
            skipDrawAmount = 0;
            Draw(new GameTime());
            EndDraw();
        }
        #endregion

        #region RestoreBrokenPaths
        public void AutoRestorePaths()
        {
            DialogResult dialogResult = MessageBox.Show(
                        catalog.GetString("This will open every single .pat file for this route, (try to) fix all broken nodes, and save the modified path. ") +
                        catalog.GetString("Potentially it will therefore change all .pat files on disc.") + "\n" +
                        catalog.GetString("This can be useful when a route has been changed and you want all paths to be corrected.") + "\n\n" +
                        catalog.GetString("Do you want to continue?"),
                        catalog.GetString("Trackviewer Path Editor"), MessageBoxButtons.OKCancel,
                        System.Windows.Forms.MessageBoxIcon.Question);
            if (dialogResult != DialogResult.OK) { return; }

            if (!CanDiscardModifiedPath()) return;
            PathEditor = null;
            DrawMultiplePaths?.ClearAll();

            var fixer = new AutoFixAllPaths(this.RouteData, this.DrawTrackDB);
            TrackViewer.Localize(fixer);
            fixer.FixallAndShowResults(Paths, (message) => DrawLoadingMessage(message));
        }
        #endregion

        #region Debug methods
        void RunDebug()
        {
        }

        static void InitLogging()
        {
        }

        void CalculateFPS(GameTime gameTime)
        {
            float elapsedRealTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            FrameRate.Update(elapsedRealTime, 1f / elapsedRealTime);
        }
        #endregion

        #region Language and localization
        public static GettextResourceManager catalog = new GettextResourceManager("Contrib");

        public static void Localize(System.Windows.FrameworkElement element)
        {
            foreach (var child in System.Windows.LogicalTreeHelper.GetChildren(element))
            {
                System.Windows.FrameworkElement childAsElement = child as System.Windows.FrameworkElement;
                if (childAsElement != null)
                {
                    Localize(childAsElement);
                }
            }

            var objType = element.GetType();
            PropertyInfo property;
            string[] propertyTags = { "Content", "Header", "Text", "Title", "ToolTip" };

            foreach (var tag in propertyTags)
            {
                property = objType.GetProperty(tag);
                if (property != null && property.CanRead && property.CanWrite && property.GetValue(element, null) is String)
                    property.SetValue(element, catalog.GetString(property.GetValue(element, null) as string), null);
            }
        }

        public void SelectLanguage(string languageCode)
        {
            LanguageManager.SelectLanguage(languageCode);
        }
        #endregion
    }
}