﻿// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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
#define SHOW_PHYSICS_GRAPHS     //Matej Pacha - if commented, the physics graphs are not ready for public release

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Orts.Viewer3D.Popups
{
    public class HUDWindow : LayeredWindow
    {
        // Set this to the width of each column in font-height units.
        readonly int ColumnWidth = 5;

        // Set to distance from top-left corner to place text.
        const int TextOffset = 10;

        readonly int ProcessorCount = System.Environment.ProcessorCount;

        readonly PerformanceCounter AllocatedBytesPerSecCounter; // \.NET CLR Memory(*)\Allocated Bytes/sec
        float AllocatedBytesPerSecLastValue;

        readonly Viewer Viewer;
        readonly Action<TableData>[] TextPages;
        readonly WindowTextFont TextFont;
        readonly HUDGraphMaterial HUDGraphMaterial;

        //Set lines rows HUDScroll.
        public int nLinesShow;
        public int charFitPerLine;
        public int columnsCount = 0;
        public int headerToRestore = 0;
        public int PathHeaderColumn = 0;
        public static int columnsChars = 0;
        public int[] lineOffsetLocoInfo = { 0, 0, 0, 0, 0, 0 };
        public int hudWindowLinesActualPage = 1;
        public int hudWindowLinesPagesCount = 1;
        public int hudWindowColumnsActualPage = 0;
        public int hudWindowColumnsPagesCount = 0;
        public int hudWindowLocoActualPage = 0;
        public int hudWindowLocoPagesCount = 1;
        public bool hudWindowFullScreen = false;
        public bool hudWindowHorizontalScroll = false;
        public bool hudWindowSteamLocoLead = false;
        List<string> stringStatus = new List<string>();
        public bool BrakeInfoVisible = false;

        // Save current language. Valid until locates was right.
        public static CultureInfo LocaleCurrentCulture = Thread.CurrentThread.CurrentUICulture;
        public static CultureInfo HUDWebServerCurrentCulture = new System.Globalization.CultureInfo("en");

        public int WebServerPageNo = 0;
        public bool WebServerEnabled = false;
        int TextPage;
        int LocomotivePage = 2;
        int LastTextPage;
        TableData TextTable = new TableData() { Cells = new string[0, 0] };

        HUDGraphSet ForceGraphs;
        HUDGraphMesh ForceGraphMotiveForce;
        HUDGraphMesh ForceGraphDynamicForce;
        HUDGraphMesh ForceGraphNumOfSubsteps;

        HUDGraphSet LocomotiveGraphs;
        HUDGraphMesh LocomotiveGraphsThrottle;
        HUDGraphMesh LocomotiveGraphsInputPower;
        HUDGraphMesh LocomotiveGraphsOutputPower;

        HUDGraphSet DebugGraphs;
        HUDGraphMesh DebugGraphMemory;
        HUDGraphMesh DebugGraphGCs;
        HUDGraphMesh DebugGraphFrameTime;
        HUDGraphMesh DebugGraphProcessRender;
        HUDGraphMesh DebugGraphProcessUpdater;
        HUDGraphMesh DebugGraphProcessLoader;
        HUDGraphMesh DebugGraphProcessSound;

        public HUDWindow(WindowManager owner)
            : base(owner, TextOffset, TextOffset, "HUD")
        {
            Viewer = owner.Viewer;
            LastTextPage = LocomotivePage;

            ProcessVirtualAddressLimit = GetVirtualAddressLimit();

            try
            {
                var counterDotNetClrMemory = new PerformanceCounterCategory(".NET CLR Memory");
                foreach (var process in counterDotNetClrMemory.GetInstanceNames())
                {
                    var processId = new PerformanceCounter(".NET CLR Memory", "Process ID", process);
                    if (processId.NextValue() == Process.GetCurrentProcess().Id)
                    {
                        AllocatedBytesPerSecCounter = new PerformanceCounter(".NET CLR Memory", "Allocated Bytes/sec", process);
                        break;
                    }
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
                Trace.TraceWarning("Unable to access Microsoft .NET Framework performance counters. This may be resolved by following the instructions at http://support.microsoft.com/kb/300956");
            }

            Debug.Assert(GC.MaxGeneration == 2, "Runtime is expected to have a MaxGeneration of 2.");

            var textPages = new List<Action<TableData>>();
            textPages.Add(TextPageCommon);
            textPages.Add(TextPageConsistInfo);
            textPages.Add(TextPageLocomotiveInfo);
            textPages.Add(TextPageBrakeInfo);
            textPages.Add(TextPageForceInfo);
            textPages.Add(TextPageDispatcherInfo);
            textPages.Add(TextPageWeather);
            textPages.Add(TextPageDebugInfo);
            TextPages = textPages.ToArray();

            TextFont = owner.TextFontMonoSpacedOutlined;

            ColumnWidth *= TextFont.Height;

            // Avoids conflict with HudScrollNav window
            WebServerEnabled = owner.Viewer.Settings.WebServer;

            HUDGraphMaterial = (HUDGraphMaterial)Viewer.MaterialManager.Load("Debug");

            LocomotiveGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            LocomotiveGraphsThrottle = LocomotiveGraphs.Add(Viewer.Catalog.GetString("Throttle"), "0", "100%", Color.Blue, 50);
            LocomotiveGraphsInputPower = LocomotiveGraphs.Add(Viewer.Catalog.GetString("Power In/Out"), "0", "100%", Color.Yellow, 50);
            LocomotiveGraphsOutputPower = LocomotiveGraphs.AddOverlapped(Color.Green, 50);

            ForceGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            ForceGraphMotiveForce = ForceGraphs.Add(Viewer.Catalog.GetString("Motive force"), "0%", "100%", Color.Green, 75);
            ForceGraphDynamicForce = ForceGraphs.AddOverlapped(Color.Red, 75);
            ForceGraphNumOfSubsteps = ForceGraphs.Add(Viewer.Catalog.GetString("Num of substeps"), "0", "300", Color.Blue, 25);

            DebugGraphs = new HUDGraphSet(Viewer, HUDGraphMaterial);
            DebugGraphMemory = DebugGraphs.Add(Viewer.Catalog.GetString("Memory"), "0GB", String.Format("{0:F0}GB", (float)ProcessVirtualAddressLimit / 1024 / 1024 / 1024), Color.Orange, 50);
            DebugGraphGCs = DebugGraphs.Add(Viewer.Catalog.GetString("GCs"), "0", "2", Color.Magenta, 20); // Multiple of 4
            DebugGraphFrameTime = DebugGraphs.Add(Viewer.Catalog.GetString("Frame time"), "0.0s", "0.1s", Color.LightGreen, 50);
            DebugGraphProcessRender = DebugGraphs.Add(Viewer.Catalog.GetString("Render process"), "0%", "100%", Color.Red, 20);
            DebugGraphProcessUpdater = DebugGraphs.Add(Viewer.Catalog.GetString("Updater process"), "0%", "100%", Color.Yellow, 20);
            DebugGraphProcessLoader = DebugGraphs.Add(Viewer.Catalog.GetString("Loader process"), "0%", "100%", Color.Magenta, 20);
            DebugGraphProcessSound = DebugGraphs.Add(Viewer.Catalog.GetString("Sound process"), "0%", "100%", Color.Cyan, 20);
#if WITH_PATH_DEBUG
            TextPage = 5;
#endif
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(TextPage);
            outf.Write(LastTextPage);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            var page = inf.ReadInt32();
            if (page >= 0 && page <= TextPages.Length)
                TextPage = page;
            page = inf.ReadInt32();
            if (page > 0 && page <= TextPages.Length)
                LastTextPage = page;
            else LastTextPage = LocomotivePage;
        }

        public override void Mark()
        {
            base.Mark();
            HUDGraphMaterial.Mark();
        }

        public override bool Interactive
        {
            get
            {
                return false;
            }
        }

        public override void TabAction()
        {
            TextPage = (TextPage + 1) % TextPages.Length;
            if (TextPage != 0)
            {
                LastTextPage = TextPage;
                lResetHudScroll = false;
            }
        }

        public void ToggleBasicHUD()
        {
            TextPage = TextPage == 0 ? LastTextPage : 0;
        }

        int[] lastGCCounts = new int[3];

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(frame, elapsedTime, updateFull);
#if SHOW_PHYSICS_GRAPHS
            if (Visible && TextPages[TextPage] == TextPageForceInfo)
            {
                var loco = Viewer.PlayerLocomotive as MSTSLocomotive;
                ForceGraphMotiveForce.AddSample(loco.MotiveForceN / loco.MaxForceN);
                ForceGraphDynamicForce.AddSample(-loco.MotiveForceN / loco.MaxForceN);
                ForceGraphNumOfSubsteps.AddSample((float)loco.LocomotiveAxle.AxleRevolutionsInt.NumOfSubstepsPS / (float)loco.LocomotiveAxle.AxleRevolutionsInt.MaxSubsteps);

                ForceGraphs.PrepareFrame(frame);
            }

            if (Visible && TextPages[TextPage] == TextPageLocomotiveInfo)
            {
                var loco = Viewer.PlayerLocomotive as MSTSLocomotive;
                var locoD = Viewer.PlayerLocomotive as MSTSDieselLocomotive;
                var locoE = Viewer.PlayerLocomotive as MSTSElectricLocomotive;
                var locoS = Viewer.PlayerLocomotive as MSTSSteamLocomotive;
                LocomotiveGraphsThrottle.AddSample(loco.ThrottlePercent * 0.01f);
                if (locoD != null)
                {
                    LocomotiveGraphsInputPower.AddSample(locoD.DieselEngines.MaxOutputPowerW / locoD.DieselEngines.MaxPowerW);
                    LocomotiveGraphsOutputPower.AddSample(locoD.DieselEngines.PowerW / locoD.DieselEngines.MaxPowerW);
                }
                if (locoE != null)
                {
                    LocomotiveGraphsInputPower.AddSample(loco.ThrottlePercent * 0.01f);
                    LocomotiveGraphsOutputPower.AddSample((loco.MotiveForceN / loco.MaxPowerW) * loco.SpeedMpS);
                }
                //TODO: plot correct values
                if (locoS != null)
                {
                    LocomotiveGraphsInputPower.AddSample(loco.ThrottlePercent * 0.01f);
                    LocomotiveGraphsOutputPower.AddSample((loco.MotiveForceN / loco.MaxPowerW) * loco.SpeedMpS);
                }

                LocomotiveGraphs.PrepareFrame(frame);
            }
#endif
            if (Visible && TextPages[TextPage] == TextPageDebugInfo)
            {
                var gcCounts = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
                DebugGraphMemory.AddSample((float)GetWorkingSetSize() / ProcessVirtualAddressLimit);
                DebugGraphGCs.AddSample(gcCounts[2] > lastGCCounts[2] ? 1.0f : gcCounts[1] > lastGCCounts[1] ? 0.5f : gcCounts[0] > lastGCCounts[0] ? 0.25f : 0);
                DebugGraphFrameTime.AddSample(Viewer.RenderProcess.FrameTime.Value * 10);
                DebugGraphProcessRender.AddSample(Viewer.RenderProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessUpdater.AddSample(Viewer.UpdaterProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessLoader.AddSample(Viewer.LoaderProcess.Profiler.Wall.Value / 100);
                DebugGraphProcessSound.AddSample(Viewer.SoundProcess.Profiler.Wall.Value / 100);
                lastGCCounts = gcCounts;
                DebugGraphs.PrepareFrame(frame);
            }
        }

        public override void PrepareFrame(ElapsedTime elapsedTime, bool updateFull)
        {
            base.PrepareFrame(elapsedTime, updateFull);

            if (updateFull)
            {
                var table = new TableData() { Cells = new string[TextTable.Cells.GetLength(0), TextTable.Cells.GetLength(1)] };
                //Normal screen or full screen
                if (!hudWindowFullScreen || (Viewer.HUDScrollWindow.Visible && TextPage == 0))
                    TextPages[0](table);

                if (TextPage > 0)
                    TextPages[TextPage](table);
                TextTable = table;
            }
        }

        // ==========================================================================================================================================
        //      Method to construct the various Heads Up Display pages for use by the WebServer 
        //      Replaces the Prepare Frame Method
        //      djr - 20171221
        // ==========================================================================================================================================
        public TableData PrepareTable(int PageNo)
        {
            if (WebServerEnabled && (HUDWebServerCurrentCulture.Name != Viewer.Catalog.GetString("en") || Thread.CurrentThread.CurrentUICulture.Name != Viewer.Catalog.GetString("en")))
                Thread.CurrentThread.CurrentUICulture = HUDWebServerCurrentCulture = new System.Globalization.CultureInfo("en");

            var table = new TableData() { Cells = new string[1, 1] };
            WebServerPageNo = PageNo;

            TextPages[PageNo](table);
            return (table);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Completely customise the rendering of the HUD - don't call base.Draw(spriteBatch).
            for (var row = 0; row < TextTable.Cells.GetLength(0); row++)
            {
                for (var column = 0; column < TextTable.Cells.GetLength(1); column++)
                {
                    if (TextTable.Cells[row, column] != null)
                    {
                        var text = TextTable.Cells[row, column];
                        var align = text.StartsWith(" ") ? LabelAlignment.Right : LabelAlignment.Left;
                        var color = Color.White;
                        if (text.Contains("!!!"))
                        {//Change to red color, an example: overspeed.
                            color = Color.OrangeRed;
                            text = text.Contains("!!!") && text.Contains("???") ? text.Substring(0, text.Length - 6) : text.Substring(0, text.Length - 3);
                        }
                        else if (text.EndsWith("!!!") || text.EndsWith("???"))
                        {
                            color = text.EndsWith("!!!") ? Color.OrangeRed : Color.Yellow;
                            text = text.Substring(0, text.Length - 3);
                        }
                        else if (text.EndsWith("%%%"))
                        {
                            color = Color.Cyan;
                            text = text.Substring(0, text.Length - 3);
                        }
                        else if (text.EndsWith("$$$"))
                        {
                            color = Color.Pink;
                            text = text.Substring(0, text.Length - 3);
                        }
                        TextFont.Draw(spriteBatch, new Rectangle(TextOffset + column * ColumnWidth, TextOffset + row * TextFont.Height, ColumnWidth, TextFont.Height), Point.Zero, text, align, color);
                    }
                }
            }

#if SHOW_PHYSICS_GRAPHS
            if (Visible && TextPages[TextPage] == TextPageForceInfo)
                ForceGraphs.Draw(spriteBatch);
            if (Visible && TextPages[TextPage] == TextPageLocomotiveInfo)
                LocomotiveGraphs.Draw(spriteBatch);
#endif
            if (Visible && TextPages[TextPage] == TextPageDebugInfo)
                DebugGraphs.Draw(spriteBatch);
        }

        #region Table handling
        public sealed class TableData
        {
            public string[,] Cells;
            public int CurrentRow;
            public int CurrentLabelColumn;
            public int CurrentValueColumn;
        }

        static void TableSetCell(TableData table, int cellColumn, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, cellColumn, format, args);
        }

        static void TableSetCell(TableData table, int cellRow, int cellColumn, string format, params object[] args)
        {
            if (cellRow > table.Cells.GetUpperBound(0) || cellColumn > table.Cells.GetUpperBound(1))
            {
                var newCells = new string[Math.Max(cellRow + 1, table.Cells.GetLength(0)), Math.Max(cellColumn + 1, table.Cells.GetLength(1))];
                for (var row = 0; row < table.Cells.GetLength(0); row++)
                    for (var column = 0; column < table.Cells.GetLength(1); column++)
                        newCells[row, column] = table.Cells[row, column];
                table.Cells = newCells;
            }
            Debug.Assert(!format.Contains('\n'), "HUD table cells must not contain newlines. Use the table positioning instead.");
            table.Cells[cellRow, cellColumn] = args.Length > 0 ? String.Format(format, args) : format;
        }

        static void TableSetCells(TableData table, int startColumn, params string[] columns)
        {
            for (var i = 0; i < columns.Length; i++)
                TableSetCell(table, startColumn + i, columns[i]);
        }

        static void TableAddLine(TableData table)
        {
            table.CurrentRow++;
        }

        static void TableAddLine(TableData table, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, 0, format, args);
            table.CurrentRow++;
        }

        static void TableAddLines(TableData table, string lines)
        {
            if (lines == null)
                return;

            foreach (var line in lines.Split('\n'))
            {
                var column = 0;
                foreach (var cell in line.Split('\t'))
                    TableSetCell(table, column++, "{0}", cell);
                table.CurrentRow++;
            }
        }

        static void TableSetLabelValueColumns(TableData table, int labelColumn, int valueColumn)
        {
            table.CurrentLabelColumn = labelColumn;
            table.CurrentValueColumn = valueColumn;
        }

        static void TableAddLabelValue(TableData table, string label, string format, params object[] args)
        {
            TableSetCell(table, table.CurrentRow, table.CurrentLabelColumn, label);
            TableSetCell(table, table.CurrentRow, table.CurrentValueColumn, format, args);
            table.CurrentRow++;
        }
        #endregion

        void TextPageCommon(TableData table)
        {
            var playerTrain = Viewer.PlayerLocomotive.Train;
            var showMUReverser = Math.Abs(playerTrain.MUReverserPercent) != 100;
            var showRetainers = playerTrain.RetainerSetting != RetainerSetting.Exhaust;
            var engineBrakeStatus = Viewer.PlayerLocomotive.GetEngineBrakeStatus();
            var brakemanBrakeStatus = Viewer.PlayerLocomotive.GetBrakemanBrakeStatus();
            var dynamicBrakeStatus = Viewer.PlayerLocomotive.GetDynamicBrakeStatus();
            var locomotiveStatus = Viewer.PlayerLocomotive.GetStatus();
            var stretched = playerTrain.Cars.Count > 1 && playerTrain.NPull == playerTrain.Cars.Count - 1;
            var bunched = !stretched && playerTrain.Cars.Count > 1 && playerTrain.NPush == playerTrain.Cars.Count - 1;

            //Disable Hudscroll.
            if (Viewer.HUDScrollWindow.Visible)
                Viewer.HUDScrollWindow.Visible = TextPage == 0 && !WebServerEnabled ? false : true;

            TableSetLabelValueColumns(table, 0, 2);
            TableAddLabelValue(table, Viewer.Catalog.GetString("Version"), VersionInfo.VersionOrBuild);

            // Client and server may have a time difference.
            if (Orts.MultiPlayer.MPManager.IsClient())
                TableAddLabelValue(table, Viewer.Catalog.GetString("Time"), FormatStrings.FormatTime(Viewer.Simulator.ClockTime + Orts.MultiPlayer.MPManager.Instance().serverTimeDifference));
            else
                TableAddLabelValue(table, Viewer.Catalog.GetString("Time"), FormatStrings.FormatTime(Viewer.Simulator.ClockTime));

            if (Viewer.Simulator.IsReplaying)
                TableAddLabelValue(table, Viewer.Catalog.GetString("Replay"), FormatStrings.FormatTime(Viewer.Log.ReplayEndsAt - Viewer.Simulator.ClockTime));

            TableAddLabelValue(table, Viewer.Catalog.GetString("Speed"), FormatStrings.FormatSpeedDisplay(Viewer.PlayerLocomotive.SpeedMpS, Viewer.PlayerLocomotive.IsMetric));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Gradient"), "{0:F1}%", -Viewer.PlayerLocomotive.CurrentElevationPercent);
            TableAddLabelValue(table, Viewer.Catalog.GetString("Direction"), showMUReverser ? "{1:F0} {0}" : "{0}", FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(Viewer.PlayerLocomotive.Direction)), Math.Abs(playerTrain.MUReverserPercent));
            TableAddLabelValue(table, Viewer.PlayerLocomotive is MSTSSteamLocomotive ? Viewer.Catalog.GetString("Regulator") : Viewer.Catalog.GetString("Throttle"), "{0:F0}%", Viewer.PlayerLocomotive.ThrottlePercent);
            if ((Viewer.PlayerLocomotive as MSTSLocomotive).TrainBrakeFitted)
                TableAddLabelValue(table, Viewer.Catalog.GetString("Train brake"), "{0}", Viewer.PlayerLocomotive.GetTrainBrakeStatus());
            if (showRetainers)
                TableAddLabelValue(table, Viewer.Catalog.GetString("Retainers"), "{0}% {1}", playerTrain.RetainerPercent, Viewer.Catalog.GetString(GetStringAttribute.GetPrettyName(playerTrain.RetainerSetting)));
            if ((Viewer.PlayerLocomotive as MSTSLocomotive).EngineBrakeFitted) // ideally this test should be using "engineBrakeStatus != null", but this currently does not work, as a controller is defined by default
                TableAddLabelValue(table, Viewer.Catalog.GetString("Engine brake"), "{0}", engineBrakeStatus);
            if ((Viewer.PlayerLocomotive as MSTSLocomotive).BrakemanBrakeFitted)
                TableAddLabelValue(table, Viewer.Catalog.GetString("Brakemen brake"), "{0}", brakemanBrakeStatus);
            if (dynamicBrakeStatus != null)
                TableAddLabelValue(table, Viewer.Catalog.GetString("Dynamic brake"), "{0}", dynamicBrakeStatus);
            if (locomotiveStatus != null)
            {
                var lines = locomotiveStatus.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Length > 0)
                    {
                        var parts = line.Split(new[] { " = " }, 2, StringSplitOptions.None);
                        TableAddLabelValue(table, parts[0], parts.Length > 1 ? parts[1] : "");
                    }
                }
            }
            TableAddLine(table);
            TableAddLabelValue(table, Viewer.Catalog.GetString("FPS"), "{0:F0}", Viewer.RenderProcess.FrameRate.SmoothedValue);
            TableAddLine(table);

            if (Viewer.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
                TableAddLine(table, Viewer.Catalog.GetString("Autopilot") + "???");

            if (Viewer.PlayerTrain.IsWheelSlip)
                TableAddLine(table, Viewer.Catalog.GetString("Wheel slip") + "!!!");
            else if (Viewer.PlayerTrain.IsWheelSlipWarninq)
                TableAddLine(table, Viewer.Catalog.GetString("Wheel slip warning") + "???");

            if (Viewer.PlayerTrain.IsBrakeSkid)
                TableAddLine(table, Viewer.Catalog.GetString("Wheel skid") + "!!!");

            if (Viewer.PlayerLocomotive.GetSanderOn())
            {
                var sanderBlocked = Viewer.PlayerLocomotive is MSTSLocomotive && Math.Abs(playerTrain.SpeedMpS) > ((MSTSLocomotive)Viewer.PlayerLocomotive).SanderSpeedOfMpS;
                if (sanderBlocked)
                    TableAddLine(table, Viewer.Catalog.GetString("Sander blocked") + "!!!");
                else
                    TableAddLine(table, Viewer.Catalog.GetString("Amount of sand") + ": " + (Viewer.PlayerLocomotive as MSTSLocomotive).CurrentTrackSandBoxCapacityKG + "kg" + "???");
            }

            if ((Viewer.PlayerLocomotive as MSTSWagon).DoorLeftOpen || (Viewer.PlayerLocomotive as MSTSWagon).DoorRightOpen)
            {
                var color = Math.Abs(Viewer.PlayerLocomotive.SpeedMpS) > 0.1f ? "!!!" : "???";
                var status = "";
                if ((Viewer.PlayerLocomotive as MSTSWagon).DoorLeftOpen)
                    status += Viewer.Catalog.GetString((Viewer.PlayerLocomotive as MSTSLocomotive).GetCabFlipped() ? "Right" : "Left");
                if ((Viewer.PlayerLocomotive as MSTSWagon).DoorRightOpen)
                    status += string.Format(status == "" ? "{0}" : " {0}", Viewer.Catalog.GetString((Viewer.PlayerLocomotive as MSTSLocomotive).GetCabFlipped() ? "Left" : "Right"));
                status += color;

                TableAddLabelValue(table, Viewer.Catalog.GetString("Doors open") + color, status);
            }
            if (Orts.MultiPlayer.MPManager.IsMultiPlayer())
            {
                var text = Orts.MultiPlayer.MPManager.Instance().GetOnlineUsersInfo();

                TableAddLabelValue(table, Viewer.Catalog.GetString("MultiPlayerStatus: "), "{0}", Orts.MultiPlayer.MPManager.IsServer()
                    ? Viewer.Catalog.GetString("Dispatcher") : Orts.MultiPlayer.MPManager.Instance().AmAider
                    ? Viewer.Catalog.GetString("Push") : Orts.MultiPlayer.MPManager.IsClient()
                    ? Viewer.Catalog.GetString("Client") : "");
                TableAddLine(table);
                foreach (var t in text.Split('\t'))
                    TableAddLine(table, "{0}", t);
            }
        }

        void TextPageConsistInfo(TableData table)
        {
            TextPageHeading(table, Viewer.Catalog.GetString("CONSIST INFORMATION"));

            var locomotive = Viewer.PlayerLocomotive;
            var mstsLocomotive = locomotive as MSTSLocomotive;
            var train = locomotive.Train;
            float tonnage = 0f;
            foreach (var car in train.Cars)
            {
                if (car.WagonType == TrainCar.WagonTypes.Freight || car.WagonType == TrainCar.WagonTypes.Passenger)
                    tonnage += car.MassKG;
            }

            ResetHudScroll();//Reset Hudscroll.

            List<string> statusConsist = new List<string>();
            //Consist information. Header.
            statusConsist.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t\t{5}\t{6}\t\t{7}\t\t{8}",
                Viewer.Catalog.GetString("Player"),
                Viewer.Catalog.GetString("Tilted"),
                Viewer.Catalog.GetString("Type"),
                Viewer.Catalog.GetString("Length"),
                Viewer.Catalog.GetString("Weight"), //"",
                Viewer.Catalog.GetString("Tonnage"), //"",
                Viewer.Catalog.GetString("Control Mode"),// "",
                Viewer.Catalog.GetString("Out of Control"),// "",
                Viewer.Catalog.GetString("Cab Aspect")
                //Add new header data here, if adding additional column.
                ));

            //Consist information. Data.
            statusConsist.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t\t{5}\t{6}\t\t{7}\t\t{8}",
                locomotive.CarID + " " + (mstsLocomotive == null ? "" : mstsLocomotive.UsingRearCab ? Viewer.Catalog.GetParticularString("Cab", "R") : Viewer.Catalog.GetParticularString("Cab", "F")),
                (train.IsTilting ? Viewer.Catalog.GetString("Yes") : Viewer.Catalog.GetString("No")),
                (train.IsFreight ? Viewer.Catalog.GetString("Freight") : Viewer.Catalog.GetString("Pass")),
                FormatStrings.FormatShortDistanceDisplay(train.Length, locomotive.IsMetric),
                FormatStrings.FormatLargeMass(train.MassKg, locomotive.IsMetric, locomotive.IsUK),
                FormatStrings.FormatLargeMass(tonnage, locomotive.IsMetric, locomotive.IsUK),
                train.ControlMode.ToString(),
                train.OutOfControlReason.ToString(),
                mstsLocomotive.TrainControlSystem.CabSignalAspect.ToString()
                //Add new data here, if adding additional column.
                ));
            string message = "";
            for (int i = 0; i < train.Cars.Count; i++)
            {
                var wagon = (train.Cars[i] as MSTSWagon);
                if (!(wagon is MSTSLocomotive) && !wagon.FreightDoors)
                {
                    message += wagon.MassKG.ToString() + " ";
                }
            }
            if (locomotive.Simulator.SuperUser)
                locomotive.Simulator.Confirmer.Information(message + train.TotalOnBoard.ToString());
            //Car information
            statusConsist.Add(string.Format("\n{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}\t{20}\t{21}",
                Viewer.Catalog.GetString("Car"),
                Viewer.Catalog.GetString("Flipped"),
                Viewer.Catalog.GetString("Type"),
                Viewer.Catalog.GetString("InterT"),
                Viewer.Catalog.GetString("Status"),
                Viewer.Catalog.GetString("H/AUnit"),
                Viewer.Catalog.GetString("HeatType"),
                Viewer.Catalog.GetString("DieselVol"),
                Viewer.Catalog.GetString("Doors"),
                Viewer.Catalog.GetString("Length"),
                Viewer.Catalog.GetString("Weight"),
                Viewer.Catalog.GetString("Drv/Cabs"),
                Viewer.Catalog.GetString("Wheels"),
                Viewer.Catalog.GetString("A1 Mass"),
                Viewer.Catalog.GetString("A2 Mass"),
                Viewer.Catalog.GetString("A3 Mass"),
                Viewer.Catalog.GetString("A4 Mass"),
                Viewer.Catalog.GetString("A1 Speed"),
                Viewer.Catalog.GetString("A2 Speed"),
                Viewer.Catalog.GetString("A3 Speed"),
                Viewer.Catalog.GetString("A4 Speed"),
                Viewer.Catalog.GetString("A5 Speed")

                //Add new header data here, if adding additional column.
                ));

            //Pages count from number of nLinesShow.
            columnsCount = statusConsist[statusConsist.Count - 1].Count(x => x == '\t') + (statusConsist[statusConsist.Count - 1].EndsWith("\t") ? 0 : 1);
            //table.CurrentRow + 1 (Consist information. Header) + 1 (Consist information. Data.) + 1 (TableAddLine(table)) + 1 (Car information)
            TextLineNumber(train.Cars.Count, table.CurrentRow + 4, columnsCount);//HudScroll

            for (var i = (hudWindowLinesActualPage * nLinesShow) - nLinesShow; i < (train.Cars.Count > hudWindowLinesActualPage * nLinesShow ? hudWindowLinesActualPage * nLinesShow : train.Cars.Count); i++)
            {
                string NoText = "";
                string TypeHeatText;
                var j = (i == 0) ? 0 : i;
                var car = train.Cars[j];

                if (car.WagonHasTemperature && car.DieselHeaterPower > 0 && !car.LocomotiveCab)
                    TypeHeatText = Viewer.Catalog.GetString("DieselHeat");
                else
                if (car.WagonHasTemperature && car.DieselHeaterPower == 0 && !car.Train.SteamHeatingIsAvailable && !car.WagonHasStove && !car.LocomotiveCab)
                    TypeHeatText = Viewer.Catalog.GetString("ElecHeat");
                else
                if (car.WagonHasTemperature && car.DieselHeaterPower == 0 && car.Train.SteamHeatingIsAvailable && !car.LocomotiveCab)
                    TypeHeatText = Viewer.Catalog.GetString("SteamHeat");
                else
                if (car.WagonHasTemperature && car.DieselHeaterPower == 0 && car.WagonHasStove && !car.LocomotiveCab)
                    TypeHeatText = Viewer.Catalog.GetString("StoveHeat");
                else
                if (car.WagonHasTemperature && car.DieselHeaterPower > 0 && car.LocomotiveCab)
                    TypeHeatText = Viewer.Catalog.GetString("DieselHeat");
                else
                if (car.WagonHasTemperature && !(car is MSTSElectricLocomotive) && car.DieselHeaterPower == 0 && car.LocomotiveCab)
                    TypeHeatText = Viewer.Catalog.GetString("Calorifer");
                else
                if (car.WagonHasTemperature && car.DieselHeaterPower == 0 && car.LocomotiveCab)
                    TypeHeatText = Viewer.Catalog.GetString("ElecHeat");
                else
                    TypeHeatText = "";

                statusConsist.Add(car.CarID + "\t" +
                    (car.Flipped ? Viewer.Catalog.GetString("Yes") : Viewer.Catalog.GetString("No")) + "\t" +
                    (train.IsFreight ? Viewer.Catalog.GetString("Freight") : Viewer.Catalog.GetString("Pass")) + "\t" +
                    (car.WagonHasTemperature ? Math.Round(car.WagonTemperature).ToString() + " °C" : NoText) + "\t" +
                    ((car.WagonHasTemperature && car.StatusHeatIsOn && !car.Train.CarSteamHeatOn) || (car.Train.CarSteamHeatOn && !car.LocomotiveCab) || (car.LocomotiveCab && car.StatusHeatIsOn) ? Viewer.Catalog.GetString("Active") : NoText) + "\t" +
                    (car.WagonHasTemperature && car.BrakeSystem.HeatingIsOn && !car.Train.CarSteamHeatOn ? Viewer.Catalog.GetString("On") : Viewer.Catalog.GetString("Off")) + "\t" +
                    (TypeHeatText) + "\t" +
                    (car.WagonHasTemperature && car.DieselHeaterPower > 0 ? FormatStrings.FormatFuelVolume(car.DieselHeaterTankCapacityMark, true, false) : NoText) + "\t" +
                    (car.BrakeSystem.DoorsOpen ? Viewer.Catalog.GetString("opened") : NoText) + "\t" +
                    FormatStrings.FormatShortDistanceDisplay(car.CarLengthM, locomotive.IsMetric) + "\t" +
                    FormatStrings.FormatLargeMass(car.MassKG, locomotive.IsMetric, locomotive.IsUK) + "\t" +
                    (car.IsDriveable ? Viewer.Catalog.GetParticularString("Cab", "D") : "") + (car.HasFrontCab || car.HasFront3DCab ? Viewer.Catalog.GetParticularString("Cab", "F") : "") + (car.HasRearCab || car.HasRear3DCab ? Viewer.Catalog.GetParticularString("Cab", "R") : "") + "\t" +
                    GetCarWhyteLikeNotation(car) + "\t" +
                    GetAxleParamsEP(car) + "\t");
                //Add new data here, if adding additional column.
            }

            DrawScrollArrows(statusConsist, table, false);
        }

        static string GetAxleParamsEP(TrainCar car)
        {
            string ret = "";
            MSTSLocomotive loco = null;
            if (car is MSTSLocomotive)
            {
                loco = (MSTSLocomotive)car;
            }
            else
            {
                return "";
            }
            if (loco.extendedPhysics != null)
            {
                ret = Math.Round(loco.extendedPhysics.Undercarriages[0].Axles[0].Mass, 2).ToString() + "t\t";
                ret += Math.Round(loco.extendedPhysics.Undercarriages[0].Axles[1].Mass, 2).ToString() + "t\t";
                ret += Math.Round(loco.extendedPhysics.Undercarriages[1].Axles[0].Mass, 2).ToString() + "t\t";
                ret += Math.Round(loco.extendedPhysics.Undercarriages[1].Axles[1].Mass, 2).ToString() + "t\t";
                ret += Math.Round(MpS.ToKpH(loco.extendedPhysics.Undercarriages[0].Axles[0].WheelSpeedMpS), 1) + "kmh\t";
                ret += Math.Round(MpS.ToKpH(loco.extendedPhysics.Undercarriages[0].Axles[1].WheelSpeedMpS), 1) + "kmh\t";
                ret += Math.Round(MpS.ToKpH(loco.extendedPhysics.Undercarriages[1].Axles[0].WheelSpeedMpS), 1) + "kmh\t";
                ret += Math.Round(MpS.ToKpH(loco.extendedPhysics.Undercarriages[1].Axles[1].WheelSpeedMpS), 1) + "kmh\t";
                ret += Math.Round(MpS.ToKpH(loco.extendedPhysics.AverageAxleSpeedMpS), 1) + "kmh\t";
            }
            return ret;
        }

        static string GetCarWhyteLikeNotation(TrainCar car)
        {
            if (car.WheelAxles.Count == 0)
                return "";

            var whyte = new List<string>();
            var currentCount = 0;
            var currentBogie = car.WheelAxles[0].BogieIndex;
            foreach (var axle in car.WheelAxles)
            {
                if (currentBogie != axle.BogieIndex)
                {
                    whyte.Add(currentCount.ToString());
                    currentBogie = axle.BogieIndex;
                    currentCount = 0;
                }
                currentCount += 2;
            }
            whyte.Add(currentCount.ToString());
            return String.Join("-", whyte.ToArray());
        }

        void TextPageLocomotiveInfo(TableData table)
        {
            TextPageHeading(table, Viewer.Catalog.GetString("LOCOMOTIVE INFORMATION"));

            var locomotive = Viewer.PlayerLocomotive;
            var train = locomotive.Train;
            ResetHudScroll();//Reset Hudscroll.
            Viewer.HUDScrollWindow.Visible = WebServerPageNo > 0 ? true : false;//HudScroll

            //HudScroll
            //Store status for each locomotive
            List<string> LocomotiveID = new List<string>();
            List<string> LocomotiveName = new List<string>(); List<string> statusHeader = new List<string>();
            List<string> statusData = new List<string>();
            Dictionary<string, int> sectionsLocomotive = new Dictionary<string, int>();
            bool IsSteamLocomotive = false;
            int maxColumns = 0;
            string space = new string(' ', columnsChars);
            hudWindowLocoPagesCount = 0;

            //Count Locomotive.
            foreach (var car in train.Cars)
            {
                if (car is MSTSLocomotive)
                {
                    LocomotiveID.Add(car.CarID);
                    hudWindowLocoPagesCount++;
                    IsSteamLocomotive = !IsSteamLocomotive && car.EngineType == TrainCar.EngineTypes.Steam ? true : false;
                }
            }

            //Disable loco nav scroll button when only one loco.
            hudWindowSteamLocoLead = LocomotiveID.Count == 1 && IsSteamLocomotive ? true : false;

            //PlayerLoco data to display
            statusHeader.Add(String.Format("{9}\t{0}\t{4}\t{1}\t{5:F0}%\t{2}\t{6:F0}%\t{3}\t\t{7}\t\t{8}\n",
                //0
                Viewer.Catalog.GetString("Direction"),
                //1
                Viewer.PlayerLocomotive is MSTSSteamLocomotive ? Viewer.Catalog.GetParticularString("Steam", "Reverser") : Viewer.Catalog.GetParticularString("NonSteam", "Reverser"),
                //2
                Viewer.PlayerLocomotive is MSTSSteamLocomotive ? Viewer.Catalog.GetString("Regulator") : Viewer.Catalog.GetString("Throttle"),
                //3
                Viewer.Catalog.GetString("Dynamic brake"),
                //4
                FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(train.MUDirection)),
                //5
                train.MUReverserPercent,
                //6
                train.MUThrottlePercent,
                //7
                train.MUDynamicBrakePercent >= 0 ? string.Format("{0:F0}%", train.MUDynamicBrakePercent) : Viewer.Catalog.GetString("off"),
                //8
                Viewer.PlayerLocomotive is MSTSElectricLocomotive ? string.Format(Viewer.Catalog.GetString("Route Voltage") + ": {0:F0}V", Viewer.PlayerLocomotive.RouteVoltageVInfo) : Viewer.Catalog.GetString(""),
            //9
            Viewer.Catalog.GetString("PlayerLoco")
            //Add new Header data here, if adding additional column.
            ));

            foreach (var car in train.Cars)
            {
                if (car is MSTSLocomotive && (hudWindowLocoActualPage > 0 ? car.CarID == LocomotiveID[hudWindowLocoActualPage - 1] : true))
                {
                    foreach (var line in car.GetDebugStatus().Split('\n'))
                    {
                        if (line.Contains(car.CarID) && !statusHeader.Contains(car.CarID))
                        {
                            //Header. Supports different types of locomotives.
                            statusHeader.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t\t{6}\t{7}\t{8}\t{9}\t\t{10}\t{11}\t{12}\t\t{13}\t{14}\t{15}\t{16}\t\t{17}\t\t{18}\t{19}\t{20}",
                            //00
                            Viewer.Catalog.GetString("Loco"),
                            //01
                            Viewer.Catalog.GetString("Direction"),
                            //02
                            Viewer.Catalog.GetString("Flipped"),
                            //03
                            Viewer.Catalog.GetString("MU'd"),
                            //04
                            Viewer.Catalog.GetString("Throttle"),
                            //05
                            Viewer.Catalog.GetString("Speed"),
                            //06
                            Viewer.Catalog.GetString("TracPower"),
                            //07
                            Viewer.Catalog.GetString("TracForce"),
                            //08
                            car is MSTSDieselLocomotive && (car as MSTSDieselLocomotive).DieselEngines[0].HasGearBox ?
                            Viewer.Catalog.GetString("Gear") : Viewer.Catalog.GetString(""),
                            //09
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("Fuel") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("Circuit breaker") : Viewer.Catalog.GetString(""),
                            //10
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("Status") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("TCS") : Viewer.Catalog.GetString(""),
                            //11
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("PowerDE") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("Driver") : Viewer.Catalog.GetString(""),
                            //12
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("LoadDE") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("Auxiliar power") : Viewer.Catalog.GetString(""),
                            //13
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("RpmDE") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("CarType") : Viewer.Catalog.GetString(""),
                            //14
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("ConsDE") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString(""),
                            //15
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("TempDE") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("PantoVoltage") : Viewer.Catalog.GetString(""),
                            //16
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("OilPressDE") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString(""),
                            //17
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("CarType") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString(""),
                            //18
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("TurboRPM") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString(""),
                            //19
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("TurboPress") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString(""),
                            //20
                            car is MSTSSteamLocomotive ? Viewer.Catalog.GetString("") :
                            car is MSTSDieselLocomotive ? Viewer.Catalog.GetString("TurboLoad") :
                            car is MSTSElectricLocomotive ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString("")
                            //Add new Locomotive header here, if required.
                            ));

                            statusHeader.Add(line);
                        }
                        var column = 0;
                        if (line.Length > 1)
                        {
                            foreach (var cell in line.Split('\t'))
                            {
                                column++;
                                if (cell.Contains(car.CarID)) LocomotiveName.Add(cell);
                            }
                            if (column > maxColumns) maxColumns = column;

                            statusData.Add(line);
                        }
                        else
                            statusData.Add("");
                    }
                }
            }
            //statusHeader[0] contains PlayerLoco data.
            maxColumns = statusHeader[1].TrimEnd('\t').Count(x => x == '\t');
            //The lines that fit by pages.
            TextLineNumber(hudWindowLocoActualPage == 0 ? hudWindowLocoPagesCount : statusHeader.Count, hudWindowLocoActualPage == 0 ? hudWindowLocoPagesCount : table.CurrentRow, hudWindowLocoActualPage == 0 ? 1 : maxColumns);
            var initialHeaderRow = table.CurrentRow;
            //Display headers
            DrawScrollArrows(statusHeader, table, IsSteamLocomotive);

            var HeaderRows = table.CurrentRow - initialHeaderRow;
            var nLine = 0;
            var cLine = "";
            foreach (var Line in statusData)
            {
                nLine++;
                if (Line.Contains("==="))
                {
                    if (sectionsLocomotive.ContainsKey(cLine))
                        sectionsLocomotive[cLine] = nLine;

                    if (!sectionsLocomotive.ContainsKey(Line))
                    {
                        cLine = Line;
                        sectionsLocomotive.Add(cLine, nLine);
                        nLine = 0;
                    }
                }
            }
            //Last section header.
            sectionsLocomotive[cLine] = nLine;

            //Lines that fit by pages.
            TextLineNumber(hudWindowLocoActualPage == 0 ? hudWindowLocoPagesCount : statusData.Count, hudWindowLocoActualPage == 0 ? hudWindowLocoPagesCount : table.CurrentRow, maxColumns);

            var CurrentFirstLine = (hudWindowLinesActualPage * nLinesShow) - nLinesShow;
            var CurrentLastLine = hudWindowLinesActualPage * nLinesShow;

            for (var i = CurrentFirstLine; i < CurrentLastLine + 1; i++)
            {
                //Locomotive info, line limit.
                if ((CurrentFirstLine >= nLinesShow ? hudWindowLocoActualPage == 0 || hudWindowLinesActualPage > 1 ? i - lineOffsetLocoInfo[hudWindowLinesActualPage] : i - 1 : i) > statusData.Count - 1)
                    break;
                //Locomotive, line limit.
                if (hudWindowLocoActualPage == 0 && i > HeaderRows - 3)
                    break;

                // Icik
                //if (i - CurrentFirstLine < (hudWindowLocoActualPage == 0 ? hudWindowLocoPagesCount : 1))
                //{
                //    int index = statusData.FindIndex(x => x.Contains(LocomotiveName[i - CurrentFirstLine]));
                //    TextColNumber(statusData[index], 0, IsSteamLocomotive);//Horizontal string width to display Locomotives.
                //}
                //else if ((i > nLinesShow ? i - lineOffsetLocoInfo[hudWindowLinesActualPage] : i) < statusData.Count || nLinesShow > statusData.Count)
                //{
                //    //Avoid to truncated sections.
                //    var offsetI = (i > nLinesShow ? i - lineOffsetLocoInfo[hudWindowLinesActualPage] : i);
                //    var dataText = statusData[offsetI];
                //    if (statusData[offsetI].Contains("===") && CurrentLastLine < i + sectionsLocomotive[statusData[offsetI]])
                //    {   //Calc string col number to display.
                //        lineOffsetLocoInfo[hudWindowLinesActualPage + 1] = nLinesShow - (i - CurrentFirstLine) + 2;//2 = First line + empty line
                //        break;
                //    }
                //    else
                //    {
                //        var newI = i > nLinesShow ? i - lineOffsetLocoInfo[hudWindowLinesActualPage] : i;
                //        var newStatus = statusData[newI];
                //        TextColNumber(statusData[offsetI], 0, IsSteamLocomotive);
                //    }
                //}

                if (hudWindowLocoActualPage > 0 && !statusData[i >= nLinesShow ? i - lineOffsetLocoInfo[hudWindowLinesActualPage] : i].StartsWith(LocomotiveName[0]))
                {
                    if (hudWindowColumnsActualPage > 0)
                    {
                        //Nav arrows. Don't use DrawScrollArrows() function with locomotive info.
                        //◄ \u25C0 - ► \u25B6 - ↔ \u2194
                        if (stringStatus.Count > 1 && stringStatus.Count <= hudWindowColumnsActualPage)
                            TableAddLines(table, hudWindowColumnsActualPage > 1 ? "◄" + stringStatus[(stringStatus.Count < hudWindowColumnsActualPage ? stringStatus.Count - 1 : hudWindowColumnsActualPage - 1)] : stringStatus[hudWindowColumnsActualPage - 1]);
                        else if (stringStatus.Count > 1 && hudWindowColumnsActualPage == 1)
                            TableAddLines(table, hudWindowColumnsActualPage > 0 ? "►" + stringStatus[hudWindowColumnsActualPage - 1] : stringStatus[hudWindowColumnsActualPage - 1]);
                        else if (stringStatus.Count > 1 && hudWindowColumnsActualPage > 1 && stringStatus.Count >= hudWindowColumnsActualPage)
                            TableAddLines(table, hudWindowColumnsActualPage > 0 ? "↔" + stringStatus[hudWindowColumnsActualPage - 1] : stringStatus[hudWindowColumnsActualPage - 1]);
                        else
                        {
                            if (stringStatus.Count > 0)
                            {
                                if (!stringStatus[0].StartsWith(LocomotiveName[0]))
                                    TableAddLines(table, stringStatus[0]);
                            }
                            else
                                TableAddLines(table, statusData[i > nLinesShow ? i - lineOffsetLocoInfo[hudWindowLinesActualPage] : i]);
                        }
                    }
                    else
                    {
                        if (i - CurrentFirstLine < (hudWindowLocoActualPage == 0 ? hudWindowLocoPagesCount : 1) && hudWindowColumnsActualPage == 0)
                        { }
                        else
                        {   //Display other locomotive info data.
                            var x = 0;
                            if (CurrentFirstLine >= nLinesShow)
                            {
                                if (hudWindowLocoActualPage == 0 || hudWindowLinesActualPage > 1)
                                {
                                    x = i - lineOffsetLocoInfo[hudWindowLinesActualPage];
                                }
                                else
                                    x = i - 1;
                            }
                            else
                                x = i;

                            var textout = statusData[x];
                            TableAddLines(table, textout);
                        }
                    }
                }
            }
        }

        void TextPageBrakeInfo(TableData table)
        {
            TextPageHeading(table, Viewer.Catalog.GetString("BRAKE INFORMATION"));

            ResetHudScroll(); //Reset Hudscroll.

            var train = Viewer.PlayerLocomotive.Train;
            var mstsLocomotive = Viewer.PlayerLocomotive as MSTSLocomotive;
            var HUDSteamEngineType = mstsLocomotive.SteamEngineType;
            var HUDEngineType = mstsLocomotive.EngineType;

            if ((Viewer.PlayerLocomotive as MSTSLocomotive).TrainBrakeFitted) // Only display the following information if a train brake is defined.
            {
                // If vacuum brakes are used then use this display
                if ((Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystem is VacuumSinglePipe)
                {
                    if ((Viewer.PlayerLocomotive as MSTSLocomotive).VacuumBrakeEQFitted)
                    {
                        TableAddLines(table, String.Format("{0}\t\t{1}\t\t{2}",
                        Viewer.Catalog.GetString("PlayerLoco"),
                        Viewer.Catalog.GetString("Exhauster"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).VacuumExhausterIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off")));
                    }

                    else if ((Viewer.PlayerLocomotive as MSTSLocomotive).VacuumPumpFitted && (Viewer.PlayerLocomotive as MSTSLocomotive).SmallEjectorControllerFitted)
                    {
                        // Display if vacuum pump, large ejector and small ejector fitted
                        TableAddLines(table, String.Format("{0}\t\t{1}\t\t{2}\t{3}\t\t{4}\t{5}\t{6}\t\t{7}\t\t{8}",
                        Viewer.Catalog.GetString("PlayerLoco"),
                        Viewer.Catalog.GetString("Large Ejector"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).LargeSteamEjectorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                        Viewer.Catalog.GetString("Small Ejector"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).SmallSteamEjectorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                        Viewer.Catalog.GetString("Pressure"),
                        FormatStrings.FormatPressure((Viewer.PlayerLocomotive as MSTSLocomotive).SteamEjectorSmallPressurePSI, PressureUnit.PSI, (Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir], true),
                        Viewer.Catalog.GetString("Vacuum Pump"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).VacuumPumpOperating ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off")
                        ));
                    }
                    else if ((Viewer.PlayerLocomotive as MSTSLocomotive).VacuumPumpFitted && !(Viewer.PlayerLocomotive as MSTSLocomotive).SmallEjectorControllerFitted) // Change display so that small ejector is not displayed for vacuum pump operated locomotives
                    {
                        // Display if vacuum pump, and large ejector only fitted
                        TableAddLines(table, String.Format("{0}\t\t{1}\t\t{2}\t{3}\t\t{4}",
                        Viewer.Catalog.GetString("PlayerLoco"),
                        Viewer.Catalog.GetString("Large Ejector"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).LargeSteamEjectorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                        Viewer.Catalog.GetString("Vacuum Pump"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).VacuumPumpOperating ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off")));
                    }
                    else
                    {
                        // Display if large ejector and small ejector only fitted
                        TableAddLines(table, String.Format("{0}\t\t{1}\t\t{2}\t{3}\t\t{4}\t{5}\t{6}",
                        Viewer.Catalog.GetString("PlayerLoco"),
                        Viewer.Catalog.GetString("Large Ejector"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).LargeSteamEjectorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                        Viewer.Catalog.GetString("Small Ejector"),
                        (Viewer.PlayerLocomotive as MSTSLocomotive).SmallSteamEjectorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                        Viewer.Catalog.GetString("Pressure"),
                        FormatStrings.FormatPressure((Viewer.PlayerLocomotive as MSTSLocomotive).SteamEjectorSmallPressurePSI, PressureUnit.PSI, (Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir], true)));
                    }

                    // Lines to show brake system volumes
                    TableAddLines(table, String.Format("{0}\t\t{1}\t\t{2}\t{3}\t\t{4}\t{5}\t{6}",
                    Viewer.Catalog.GetString("Brake Sys Vol"),
                    Viewer.Catalog.GetString("Train Pipe"),
                    FormatStrings.FormatVolume(train.TotalTrainBrakePipeVolumeM3, mstsLocomotive.IsMetric),
                    Viewer.Catalog.GetString("Brake Cyl"),
                    FormatStrings.FormatVolume(train.TotalTrainBrakeCylinderVolumeM3, mstsLocomotive.IsMetric),
                    Viewer.Catalog.GetString("Air Vol"),
                    FormatStrings.FormatVolume(train.TotalCurrentTrainBrakeSystemVolumeM3, mstsLocomotive.IsMetric)
                    ));
                }
                else  // Default to air or electronically braked, use this display
                {
                    // Icik
                    float AuxResVolumeL = (Viewer.PlayerLocomotive as MSTSLocomotive).AuxResPressurePSI / 14.50377f * (Viewer.PlayerLocomotive as MSTSLocomotive).AuxResVolumeM3 * 1000;
                    AuxResVolumeL = (float)Math.Round(AuxResVolumeL, 0);

                    TableAddLines(table, String.Format("{0}\t\t{1}\t\t{2}\t{3}\t\t{4}\t{5}\t\t{6}\t{7}\t\t{8}\t{9}\t{10}",
                    Viewer.Catalog.GetString("PlayerLoco"),
                    Viewer.Catalog.GetString("Main reservoir"),
                    FormatStrings.FormatPressure((Viewer.PlayerLocomotive as MSTSLocomotive).MainResPressurePSI, PressureUnit.PSI, (Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir], true),
                    Viewer.Catalog.GetString("Compressor I"),
                    (Viewer.PlayerLocomotive as MSTSLocomotive).CompressorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                    Viewer.Catalog.GetString("Compressor II"),
                    (Viewer.PlayerLocomotive as MSTSLocomotive).Compressor2IsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                    Viewer.Catalog.GetString("AuxCompressor"),
                    (Viewer.PlayerLocomotive as MSTSLocomotive).AuxCompressorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                    FormatStrings.FormatPressure((Viewer.PlayerLocomotive as MSTSLocomotive).AuxResPressurePSI, PressureUnit.PSI, (Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir], true),
                    AuxResVolumeL + " L"));
                }

                // Display data for other locomotives
                for (var i = 0; i < train.Cars.Count; i++)
                {
                    var car = train.Cars[i];
                    if (car is MSTSLocomotive && car != Viewer.PlayerLocomotive)
                    {
                        // Icik
                        float AuxResVolumeL = (car as MSTSLocomotive).AuxResPressurePSI / 14.50377f * (car as MSTSLocomotive).AuxResVolumeM3 * 1000;
                        AuxResVolumeL = (float)Math.Round(AuxResVolumeL, 0);

                        TableAddLines(table, String.Format("{0}\t{1}\t{2}\t\t{3}\t{4}\t\t{5}\t{6}\t\t{7}\t{8}\t\t{9}\t{10}\t{11}",
                            Viewer.Catalog.GetString("Loco"),
                            car.CarID,
                            Viewer.Catalog.GetString("Main reservoir"),
                            FormatStrings.FormatPressure((car as MSTSLocomotive).MainResPressurePSI, PressureUnit.PSI, (car as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir], true),
                            Viewer.Catalog.GetString("Compressor I"),
                            (car as MSTSLocomotive).CompressorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                            Viewer.Catalog.GetString("Compressor II"),
                            (car as MSTSLocomotive).Compressor2IsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                            Viewer.Catalog.GetString("AuxCompressor"),
                            (car as MSTSLocomotive).AuxCompressorIsOn ? Viewer.Catalog.GetString("on") : Viewer.Catalog.GetString("off"),
                            FormatStrings.FormatPressure((car as MSTSLocomotive).AuxResPressurePSI, PressureUnit.PSI, (car as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir], true),
                            AuxResVolumeL + " L"));
                    }
                }
                TableAddLine(table);
            }

            //Initialize
            List<string> statusBrake = new List<string>();
            List<string> statusHeader = new List<string>();
            string[] stringStatusToList;//Allow to change data from TableAddLines to TableSetCell
            hudWindowLocoPagesCount = 0;
            int n = train.Cars.Count;

            // Different display depending upon whether vacuum braked or air braked
            for (var i = 0; i < n; i++)
            {
                var car = train.Cars[i];
                if (car.BrakeSystem is VacuumSinglePipe)
                {
                    statusHeader.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}",
                        //0
                        Viewer.Catalog.GetString("Car"),
                        //1
                        Viewer.Catalog.GetString("Type"),
                        //2
                        Viewer.Catalog.GetString("BrkCyl"),
                        //3
                        Viewer.Catalog.GetString("BrkPipe"),
                        //4
                        (car as MSTSWagon).NonAutoBrakePresent ? Viewer.Catalog.GetString("") : Viewer.Catalog.GetString("VacRes"),
                        //                      Viewer.Catalog.GetString(""),
                        //5
                        Viewer.Catalog.GetString(""),
                        //6
                        Viewer.Catalog.GetString(""),
                        //7
                        Viewer.Catalog.GetString(""),
                        //8
                        Viewer.Catalog.GetString(""),
                        //9
                        Viewer.Catalog.GetString("Handbrk"),
                        //10
                        Viewer.Catalog.GetString("Conn"),
                        //11
                        Viewer.Catalog.GetString("AnglCock")
                        //Add new header data here, if addining additional column.
                        ));
                }
                else if (car.BrakeSystem is ManualBraking)
                {
                    statusHeader.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                        //0
                        Viewer.Catalog.GetString("Car"),
                        //1
                        Viewer.Catalog.GetString("Type"),
                        //2
                        Viewer.Catalog.GetString("Brk"),
                        //3
                        Viewer.Catalog.GetString(""),
                        //4
                        Viewer.Catalog.GetString(""),
                        //5
                        Viewer.Catalog.GetString(""),
                        //6
                        Viewer.Catalog.GetString(""),
                        //7
                        Viewer.Catalog.GetString(""),
                        //8
                        Viewer.Catalog.GetString(""),
                        //9
                        Viewer.Catalog.GetString(""),
                        //10
                        Viewer.Catalog.GetString("Handbrk")
                    ));
                }
                else // default air braked
                {
                    statusHeader.Add(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                    //0
                    Viewer.Catalog.GetString("Car"),
                    //1
                    Viewer.Catalog.GetString("Type"),
                    //2
                    Viewer.Catalog.GetString("BrkCyl"),
                    //3
                    Viewer.Catalog.GetString("BrkPipe"),
                    //4
                    Viewer.Catalog.GetString("AuxRes"),
                    //5
                    //Viewer.Catalog.GetString("ErgRes"),
                    //6
                    //Viewer.Catalog.GetString("MRPipe"),
                    //7
                    //Viewer.Catalog.GetString("RetValve"),
                    //5
                    Viewer.Catalog.GetString("TripleValve"),
                    //9
                    //Viewer.Catalog.GetString(""),
                    //6
                    Viewer.Catalog.GetString("Handbrk"),
                    //7
                    Viewer.Catalog.GetString("AirHoses"),
                    //8
                    Viewer.Catalog.GetString("AnglCock"),
                    //9
                    Viewer.Catalog.GetString("BleedOff"),
                    //10
                    Viewer.Catalog.GetString("AntiSkid"),
                    //11                  
                    Viewer.Catalog.GetString("LeakBrkPipes"),
                    //12
                    Viewer.Catalog.GetString(""),
                    //13
                    Viewer.Catalog.GetString("VolBrkPipes"),
                    //14
                    Viewer.Catalog.GetString("VolBrkCyl"),
                    //15
                    Viewer.Catalog.GetString("VolMainRes"),
                    //16
                    Viewer.Catalog.GetString("BrkMode"),
                    //17
                    Viewer.Catalog.GetString("BrkMass")
                    //Add new header data here, if addining additional column.
                    ));
                }
            }
            //TableAddLine(table);
            columnsCount = statusHeader[statusHeader.Count - 1].Count(x => x == '\t');
            //The lines that fit by pages.
            TextLineNumber(train.Cars.Count, table.CurrentRow + 1, columnsCount);

            for (var i = 0; i < train.Cars.Count; i++)
            {
                var j = (i == 0) ? 0 : i;
                var car = train.Cars[j];
                var statusString = "";
                foreach (var cell in car.BrakeSystem.GetDebugStatus((Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits))
                {
                    statusString = statusString + cell + "\t";
                }
                if (statusString.StartsWith("1V"))
                {
                    var indexMatch = statusHeader.FindIndex(x => x.Contains(Viewer.Catalog.GetString("VacRes")));
                    if (!statusBrake.Contains(statusHeader[indexMatch]))//Avoid header duplicity
                        statusBrake.Add(statusHeader[indexMatch]);
                }
                else
                {
                    var indexMatch = statusHeader.FindIndex(x => x.Contains(Viewer.Catalog.GetString("AuxRes")));
                    if (!statusBrake.Contains(statusHeader[indexMatch]))//Avoid header duplicity
                        statusBrake.Add(statusHeader[indexMatch]);
                }
                statusBrake.Add(car.CarID + "\t" + statusString);
            }

            //HudScroll. Pages count from nLinesShow number.
            TextLineNumber(statusBrake.Count, table.CurrentRow, columnsCount);

            //Number of lines to show. HudScroll
            for (var i = (hudWindowLinesActualPage * nLinesShow) - nLinesShow; i < (statusBrake.Count > hudWindowLinesActualPage * nLinesShow ? hudWindowLinesActualPage * nLinesShow : statusBrake.Count); i++)
            {
                if (i > 0 && i < 2 && (statusBrake[i] == stringStatus[i - 1]) || i > 1 && (statusBrake[i - 2] == statusBrake[i]))
                    continue;

                if (statusBrake.Count > i)
                {
                    //Calc col number and take in count 1 left column carID
                    if (i > 0 && i % nLinesShow == 0 && hudWindowColumnsActualPage > 0)
                        TextColNumber(statusBrake[0], 0, false);
                    else
                        TextColNumber(statusBrake[i], 0, false);

                    //Update Brake Information view to show selected car from TrainOperationsWindow (F9)
                    //Once a car clicked CarOperationsWindow remains visible
                    BrakeInfoVisible = false;
                    if (Viewer.CarOperationsWindow.Visible)
                    {
                        int indexMatch = statusBrake.FindIndex(x => x.Contains(Viewer.Catalog.GetString((Viewer.CarOperationsWindow.CarPosition >= Viewer.PlayerTrain.Cars.Count ? " " : Viewer.PlayerTrain.Cars[Viewer.CarOperationsWindow.CarPosition].CarID))));
                        hudWindowLinesActualPage = (int)Math.Ceiling(Convert.ToDouble(indexMatch / nLinesShow) + 0.5);
                        BrakeInfoVisible = true;
                    }

                    //Add yellow color to string when car was selected at CarOperationWindow (F9).
                    stringStatusToList = (statusBrake[i].TrimEnd('\t').Split('\t'));//Convert to string[]

                    var EndText = stringStatusToList[0] == (Viewer.CarOperationsWindow.CarPosition >= Viewer.PlayerTrain.Cars.Count ? " " : Viewer.PlayerTrain.Cars[Viewer.CarOperationsWindow.CarPosition].CarID) ? "???" : "";
                    var arrow = "";

                    //DrawScrollArrows() can't be used because it works with TableAddLines, here we work with TableSetCell.
                    if (hudWindowColumnsActualPage > 0)
                    {
                        if (stringStatus.Count > 1 && stringStatus.Count <= hudWindowColumnsActualPage)
                        {
                            arrow = arrow + "◄";// \u25C0
                        }
                        else if (stringStatus.Count > 1 && hudWindowColumnsActualPage == 1)
                        {
                            arrow = arrow + "►";// \u25B6
                        }

                        if (i > 0 && i % nLinesShow == 0)
                        {//Display header when page was changed
                            TextColNumber(statusBrake[0], 0, false);
                            stringStatusToList = stringStatus[hudWindowColumnsActualPage - 1].TrimEnd('\t').Split('\t');// string[]
                            stringStatusToList[0] = arrow + stringStatusToList[0];
                            BrakeInfoData(table, stringStatusToList, "");
                            TableAddLine(table);
                            TextColNumber(statusBrake[i], 0, false);
                        }

                        if (stringStatus.Count >= hudWindowColumnsActualPage)
                        {//Avoid crash when not same brake system
                            stringStatusToList = stringStatus[hudWindowColumnsActualPage - 1].TrimEnd('\t').Split('\t');// string[]
                            stringStatusToList[0] = arrow + stringStatusToList[0];
                            BrakeInfoData(table, stringStatusToList, EndText);
                        }
                        else if (stringStatus.Count == 1)
                        {
                            stringStatusToList = stringStatus[0].TrimEnd('\t').Split('\t');// string[]
                            stringStatusToList[0] = arrow + stringStatusToList[0];
                            BrakeInfoData(table, stringStatusToList, EndText);
                        }
                    }
                    else
                    {   //hudWindowColumnsActualPage == 0.
                        if (i > 0 && i % nLinesShow == 0)
                        {//Display header when page was changed
                            stringStatusToList = statusBrake[0].TrimEnd('\t').Split('\t');// string[]
                            BrakeInfoData(table, stringStatusToList, "");
                            TableAddLine(table);
                        }

                        stringStatusToList = statusBrake[i].TrimEnd('\t').Split('\t');// string[]
                        BrakeInfoData(table, stringStatusToList, EndText);
                    }
                    TableAddLine(table);
                }
            }
        }

        /// <summary>
        /// Allow to draw brake info data.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="stringToDraw"></param>
        /// <param name="endtext"></param>
        void BrakeInfoData(TableData table, string[] stringToDraw, string endtext)
        {
            for (int iCell = 0; iCell < stringToDraw.Length; iCell++)
                TableSetCell(table, table.CurrentRow, iCell, stringToDraw[iCell] + endtext);
        }


        void TextPageForceInfo(TableData table)
        {
            TextPageHeading(table, Viewer.Catalog.GetString("FORCE INFORMATION"));

            var train = Viewer.PlayerLocomotive.Train;
            var mstsLocomotive = Viewer.PlayerLocomotive as MSTSLocomotive;

            ResetHudScroll(); //Reset Hudscroll.
            if (hudWindowFullScreen || WebServerEnabled)
                TableSetLabelValueColumns(table, 0, 2);

            if (mstsLocomotive != null)
            {
                if (mstsLocomotive.AdvancedAdhesionModel)
                {
                    // Icik
                    string TreeLeavesMSG = "";
                    if (mstsLocomotive.TreeLeavesLevel > 0)
                        TreeLeavesMSG = Viewer.Catalog.GetString("Tree leaves on track!");
                    
                    var HUDSteamEngineType = mstsLocomotive.SteamEngineType;
                    var HUDEngineType = mstsLocomotive.EngineType;

                    if (HUDEngineType == TrainCar.EngineTypes.Steam && (HUDSteamEngineType == TrainCar.SteamEngineTypes.Compound || HUDSteamEngineType == TrainCar.SteamEngineTypes.Simple || HUDSteamEngineType == TrainCar.SteamEngineTypes.Unknown)) // For display of steam locomotive adhesion info
                    {
                        TableAddLine(table, Viewer.Catalog.GetString("(Advanced adhesion model)"));
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Loco Adhesion"), "{0:F0}%", mstsLocomotive.LocomotiveCoefficientFrictionHUD * 100.0f);
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Wag Adhesion"), "{0:F0}%", mstsLocomotive.WagonCoefficientFrictionHUD * 100.0f);
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Tang. Force"), "{0:F0}", FormatStrings.FormatForce(N.FromLbf(mstsLocomotive.SteamTangentialWheelForce), mstsLocomotive.IsMetric));
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Static Force"), "{0:F0}", FormatStrings.FormatForce(N.FromLbf(mstsLocomotive.SteamStaticWheelForce), mstsLocomotive.IsMetric));
                        //  TableAddLabelValue(table, Viewer.Catalog.GetString("Axle brake force"), "{0}", FormatStrings.FormatForce(mstsLocomotive.LocomotiveAxle.BrakeForceN, mstsLocomotive.IsMetric));
                    }
                    else  // Advanced adhesion non steam locomotives HUD display
                    {
                        TableAddLine(table, Viewer.Catalog.GetString("(Advanced adhesion model)"));
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Wheel slip"), "{0:F0}% ({1:F0}%/{2})", mstsLocomotive.LocomotiveAxle.SlipSpeedPercent, mstsLocomotive.LocomotiveAxle.SlipDerivationPercentpS, FormatStrings.s);                        
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Conditions"), "{0:F0}% " + TreeLeavesMSG, mstsLocomotive.LocomotiveAxle.AdhesionConditions * 100.0f);
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Axle drive force"), "{0} ({1})", FormatStrings.FormatForce(mstsLocomotive.LocomotiveAxle.DriveForceN, mstsLocomotive.IsMetric),
                        FormatStrings.FormatPower(mstsLocomotive.LocomotiveAxle.DriveForceN * mstsLocomotive.AbsTractionSpeedMpS, mstsLocomotive.IsMetric, false, false));
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Axle brake force"), "{0}", FormatStrings.FormatForce(mstsLocomotive.LocomotiveAxle.BrakeRetardForceN, mstsLocomotive.IsMetric));
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Number of substeps"), "{0:F0} ({1})", mstsLocomotive.LocomotiveAxle.AxleRevolutionsInt.NumOfSubstepsPS,
                        Viewer.Catalog.GetStringFmt("filtered by {0:F0}", mstsLocomotive.LocomotiveAxle.FilterMovingAverage.Size));
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Solver"), "{0}", mstsLocomotive.LocomotiveAxle.AxleRevolutionsInt.Method.ToString());
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Stability correction"), "{0:F0}", mstsLocomotive.LocomotiveAxle.AdhesionK);
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Axle out force"), "{0} ({1})",
                            FormatStrings.FormatForce(mstsLocomotive.LocomotiveAxle.AxleForceN, mstsLocomotive.IsMetric),
                            FormatStrings.FormatPower(mstsLocomotive.LocomotiveAxle.AxleForceN * mstsLocomotive.AbsTractionSpeedMpS, mstsLocomotive.IsMetric, false, false));
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Wheel Speed"), "{0} ({1})",
                            FormatStrings.FormatSpeedDisplay(mstsLocomotive.AbsWheelSpeedMpS, mstsLocomotive.IsMetric),
                            FormatStrings.FormatSpeedDisplay(mstsLocomotive.LocomotiveAxle.SlipSpeedMpS, mstsLocomotive.IsMetric)
                            );
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Loco Adhesion"), "{0:F0}%", mstsLocomotive.LocomotiveCoefficientFrictionHUD * 100.0f);
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Wagon Adhesion"), "{0:F0}%", mstsLocomotive.WagonCoefficientFrictionHUD * 100.0f);

                        // Icik
                        TableAddLabelValue(table, Viewer.Catalog.GetString("Track quality"), "{0:F0}%", mstsLocomotive.TrackFactor * 100.0f);
                    }
                }
                else
                {
                    TableAddLine(table, Viewer.Catalog.GetString("(Simple adhesion model)"));
                    TableAddLabelValue(table, Viewer.Catalog.GetString("Axle out force"), "{0:F0} N ({1:F0} kW)", mstsLocomotive.MotiveForceN, mstsLocomotive.MotiveForceN * mstsLocomotive.SpeedMpS / 1000.0f);
                    TableAddLabelValue(table, Viewer.Catalog.GetString("Loco Adhesion"), "{0:F0}%", mstsLocomotive.LocomotiveCoefficientFrictionHUD * 100.0f);
                    TableAddLabelValue(table, Viewer.Catalog.GetString("Wagon Adhesion"), "{0:F0}%", mstsLocomotive.WagonCoefficientFrictionHUD * 100.0f);
                    // Icik
                    TableAddLabelValue(table, Viewer.Catalog.GetString("Track quality"), "{0:F0}%", mstsLocomotive.TrackFactor * 100.0f);
                }
                TableAddLine(table);

                if (train.TrainWindResistanceDependent) // Only show this information if wind resistance is selected
                {
                    var status = new StringBuilder();
                    if (hudWindowColumnsActualPage > 0)
                    {
                        status.AppendFormat("\n{0}\t{1:N2} mph\t{2}\t{3:N2} mph\n",
                        Viewer.Catalog.GetString("ResWind:"), train.ResultantWindComponentDeg,
                        Viewer.Catalog.GetString("ResSpeed:"), Me.ToMi(pS.TopH(train.WindResultantSpeedMpS)));
                    }
                    else
                    {
                        status.AppendFormat("\n{0}\t{1:N2} mph\t{2}\t\t{3:N2} Deg\t{4}\t\t{5:N2} Deg\t{6}\t{7:N2} mph\t{8}\t{9:N2} mph\n",
                        Viewer.Catalog.GetString("Wind Speed:"), Me.ToMi(pS.TopH(train.PhysicsWindSpeedMpS)),
                        Viewer.Catalog.GetString("Wind Direction:"), train.PhysicsWindDirectionDeg,
                        Viewer.Catalog.GetString("Train Direction:"), train.PhysicsTrainLocoDirectionDeg,
                        Viewer.Catalog.GetString("ResWind:"), train.ResultantWindComponentDeg,
                        Viewer.Catalog.GetString("ResSpeed:"), Me.ToMi(pS.TopH(train.WindResultantSpeedMpS))
                        //Add new header + data here, if required.
                        );
                    }
                    TableAddLines(table, status.ToString());
                }
            }
            //HudScroll
            if (hudWindowColumnsActualPage > 0)
            {
                //HudScroll
                TableSetCells(table, 0,
                Viewer.Catalog.GetString("Car"),
                Viewer.Catalog.GetString("Coupler"),
                Viewer.Catalog.GetString("Slack"),
                Viewer.Catalog.GetString("Mass"),
                Viewer.Catalog.GetString("Gradient"),
                Viewer.Catalog.GetString("Curve"),
                Viewer.Catalog.GetString("Brk Frict."),
                Viewer.Catalog.GetString("Brk Slide"),
                Viewer.Catalog.GetString("Bear Temp")
                //Add new header data here, if adding additional column.
                // Possibly needed for buffing forces
                //                Viewer.Catalog.GetString("VertD"),
                //                Viewer.Catalog.GetString("VertL"),
                //                Viewer.Catalog.GetString("BuffExc"),
                //                Viewer.Catalog.GetString("CplAng")
                );
            }
            else
            {
                //Normal view
                TableSetCells(table, 0,
                Viewer.Catalog.GetString("Car"),
                Viewer.Catalog.GetString("Total"),
                Viewer.Catalog.GetString("Motive"),
                Viewer.Catalog.GetString("Brake"),
                Viewer.Catalog.GetString("Friction"),
                Viewer.Catalog.GetString("Gravity"),
                Viewer.Catalog.GetString("Curve"),
                Viewer.Catalog.GetString("Tunnel"),
                Viewer.Catalog.GetString("Wind"),
                Viewer.Catalog.GetString("Coupler"),
                Viewer.Catalog.GetString("Coupler"),
                Viewer.Catalog.GetString("Slack"),
                Viewer.Catalog.GetString("Mass"),
                Viewer.Catalog.GetString("Gradient"),
                Viewer.Catalog.GetString("Curve"),
                Viewer.Catalog.GetString("Brk Frict."),
                Viewer.Catalog.GetString("Brk Slide"),
                Viewer.Catalog.GetString("Bear Temp")

                // Possibly needed for buffing forces
                //                Viewer.Catalog.GetString("VertD"),
                //                Viewer.Catalog.GetString("VertL"),
                //                Viewer.Catalog.GetString("BuffExc"),
                //                Viewer.Catalog.GetString("CplAng")

                );
            }
            //Columns. HudScroll
            var columnsCount = ColumnsCount(table, false);

            TableAddLine(table);

            //Pages count from number of nLinesShow.
            TextLineNumber(train.Cars.Count, table.CurrentRow + 1, columnsCount);
            //Number of lines to show
            for (var i = (hudWindowLinesActualPage * nLinesShow) - nLinesShow; i < (train.Cars.Count > hudWindowLinesActualPage * nLinesShow ? hudWindowLinesActualPage * nLinesShow : train.Cars.Count); i++)
            {
                var j = (i == 0) ? 0 : i;
                var car = train.Cars[j];
                TableSetCell(table, 0, "{0}", car.CarID);
                if (hudWindowColumnsActualPage > 0)
                {
                    TableSetCell(table, 1, "{0} : {1}", car.GetCouplerRigidIndication() ? "R" : "F", car.CouplerExceedBreakLimit ? "xxx" + "!!!" : car.CouplerOverloaded ? "O/L" + "???" : car.HUDCouplerForceIndication == 1 ? "Pull" : car.HUDCouplerForceIndication == 2 ? "Push" : "-");
                    TableSetCell(table, 2, "{0}", FormatStrings.FormatVeryShortDistanceDisplay(car.CouplerSlackM, car.IsMetric));
                    TableSetCell(table, 3, "{0}", FormatStrings.FormatLargeMass(car.MassKG, car.IsMetric, car.IsUK));
                    TableSetCell(table, 4, "{0:F2}%", -car.CurrentElevationPercent);
                    TableSetCell(table, 5, "{0}", FormatStrings.FormatDistance(car.CurrentCurveRadius, car.IsMetric));
                    TableSetCell(table, 6, "{0:F0}%", car.BrakeShoeCoefficientFriction * 100.0f);
                    TableSetCell(table, 7, car.HUDBrakeSkid ? Viewer.Catalog.GetString("Yes") : "No");
                    TableSetCell(table, 8, "{0} {1}", FormatStrings.FormatTemperature(car.WheelBearingTemperatureDegC, car.IsMetric, false), car.DisplayWheelBearingTemperatureStatus);

                    // Possibly needed for buffing forces
                    //                TableSetCell(table, 17, "{0}", FormatStrings.FormatForce(car.WagonVerticalDerailForceN, car.IsMetric));
                    //                TableSetCell(table, 18, "{0}", FormatStrings.FormatForce(car.TotalWagonLateralDerailForceN, car.IsMetric));
                    //                TableSetCell(table, 19, car.BuffForceExceeded ? Viewer.Catalog.GetString("Yes") : "No");
                    //                TableSetCell(table, 20, "{0:F2}", MathHelper.ToDegrees(car.WagonFrontCouplerAngleRad));


                    TableSetCell(table, 9, car.Flipped ? Viewer.Catalog.GetString("Flipped") : "");
                    TableAddLine(table);
                    TableSetCell(table, 1, "Tot.Slack:");
                    TableSetCell(table, 2, "{0}", FormatStrings.FormatShortDistanceDisplay(train.TotalCouplerSlackM, mstsLocomotive.IsMetric));
                }
                else
                {
                    TableSetCell(table, 1, "{0}", FormatStrings.FormatForce(car.TotalForceN, car.IsMetric));
                    TableSetCell(table, 2, "{0}{1}", FormatStrings.FormatForce(car.MotiveForceN, car.IsMetric), car.WheelSlip ? "!!!" : car.WheelSlipWarning ? "???" : "");
                    TableSetCell(table, 3, "{0}", FormatStrings.FormatForce(car.BrakeForceN, car.IsMetric));
                    TableSetCell(table, 4, "{0}", FormatStrings.FormatForce(car.FrictionForceN, car.IsMetric));
                    TableSetCell(table, 5, "{0}", FormatStrings.FormatForce(car.GravityForceN, car.IsMetric));
                    TableSetCell(table, 6, "{0}", FormatStrings.FormatForce(car.CurveForceN, car.IsMetric));
                    TableSetCell(table, 7, "{0}", FormatStrings.FormatForce(car.TunnelForceN, car.IsMetric));
                    TableSetCell(table, 8, "{0}", FormatStrings.FormatForce(car.WindForceN, car.IsMetric));
                    TableSetCell(table, 9, "{0}", FormatStrings.FormatForce(car.CouplerForceU, car.IsMetric));
                    TableSetCell(table, 10, "{0} : {1}", car.GetCouplerRigidIndication() ? "R" : "F", car.CouplerExceedBreakLimit ? "xxx" + "!!!" : car.CouplerOverloaded ? "O/L" + "???" : car.HUDCouplerForceIndication == 1 ? "Pull" : car.HUDCouplerForceIndication == 2 ? "Push" : "-");
                    TableSetCell(table, 11, "{0}", FormatStrings.FormatVeryShortDistanceDisplay(car.CouplerSlackM, car.IsMetric));
                    TableSetCell(table, 12, "{0}", FormatStrings.FormatLargeMass(car.MassKG, car.IsMetric, car.IsUK));
                    TableSetCell(table, 13, "{0:F2}%", -car.CurrentElevationPercent);
                    TableSetCell(table, 14, "{0}", FormatStrings.FormatDistance(car.CurrentCurveRadius, car.IsMetric));
                    TableSetCell(table, 15, "{0:F0}%", car.BrakeShoeCoefficientFriction * 100.0f);
                    TableSetCell(table, 16, car.HUDBrakeSkid ? Viewer.Catalog.GetString("Yes") : Viewer.Catalog.GetString("No"));
                    TableSetCell(table, 17, "{0} {1}", FormatStrings.FormatTemperature(car.WheelBearingTemperatureDegC, car.IsMetric, false), car.DisplayWheelBearingTemperatureStatus);
                    TableSetCell(table, 18, car.Flipped ? Viewer.Catalog.GetString("Flipped") : "");

                    TableAddLine(table);
                    TableSetCell(table, 10, "Tot.Slack:");
                    TableSetCell(table, 11, "{0}", FormatStrings.FormatShortDistanceDisplay(train.TotalCouplerSlackM, mstsLocomotive.IsMetric));
                }
            }
        }

        void TextPageDispatcherInfo(TableData table)
        {
            // count active trains
            int totalactive = 0;
            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC && thisTrain.TrainType != Train.TRAINTYPE.AI_INCORPORATED)
                {
                    totalactive++;
                }
            }

            TextPageHeading(table, $"{Viewer.Catalog.GetString("DISPATCHER INFORMATION : active trains : ")}{totalactive}");

            ResetHudScroll();//Reset HudScroll

            if (hudWindowColumnsActualPage > 0)
            {
                //HudScroll
                TableSetCells(table, 0,
                    Viewer.Catalog.GetString("Train"),
                    Viewer.Catalog.GetString("Consist"),
                    Viewer.Catalog.GetString("Path"));
                //Add new header data here, if adding additional column.

            }
            else
            {
                //Normal view
                TableSetCells(table, 0,
                    Viewer.Catalog.GetString("Train"),
                    Viewer.Catalog.GetString("Travelled"),
                    Viewer.Catalog.GetString("Speed"),
                    Viewer.Catalog.GetString("Max"),
                    Viewer.Catalog.GetString("AI mode"),
                    Viewer.Catalog.GetString("AI data"),
                    Viewer.Catalog.GetString("Mode"),
                    Viewer.Catalog.GetString("Auth"),
                    Viewer.Catalog.GetString("Distance"),
                    Viewer.Catalog.GetString("Signal"),
                    Viewer.Catalog.GetString("Distance"),
                    Viewer.Catalog.GetString("Consist"),
                    Viewer.Catalog.GetString("Path"));
                //New added items, here

                //Find 'Path' header column
                //Requiered to avoid conflict with human dispatcher code.
                PathHeaderColumn = ColumnsCount(table, true);
            }

            //HudScroll. Columns
            var columnsCount = ColumnsCount(table, false);

            List<string[]> statusDispatcher = new List<string[]>();
            statusDispatcher.Clear();
            var TextToYellowColor = "#";

            TableAddLine(table);

            // first is player train
            foreach (var thisTrain in Viewer.Simulator.Trains)
            {
                if (thisTrain.TrainType == Train.TRAINTYPE.PLAYER || (thisTrain.TrainType == Train.TRAINTYPE.REMOTE && Orts.MultiPlayer.MPManager.IsServer())
                    || thisTrain.IsActualPlayerTrain)
                {
                    var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                    if (thisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING) status = ((AITrain)thisTrain).AddMovementState(status, Viewer.MilepostUnitsMetric);
                    else if (thisTrain == Program.Simulator.OriginalPlayerTrain && Program.Simulator.Activity != null) status = thisTrain.AddRestartTime(status);
                    else if (thisTrain.IsActualPlayerTrain && Program.Simulator.Activity != null && thisTrain.ControlMode != Train.TRAIN_CONTROL.EXPLORER && !thisTrain.IsPathless)
                        status = thisTrain.AddRestartTime(status);

                    //HudScroll
                    if (Viewer.SelectedTrain.Name == thisTrain.Name)
                        TextToYellowColor = status[0];

                    statusDispatcher.Add(status);
                }
            }

            // next is active AI trains which are delayed
            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC && thisTrain.TrainType != Train.TRAINTYPE.PLAYER
                    && thisTrain.TrainType != Train.TRAINTYPE.AI_INCORPORATED)
                {
                    if (thisTrain.Delay.HasValue && thisTrain.Delay.Value.TotalMinutes >= 1)
                    {
                        var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                        status = thisTrain.AddMovementState(status, Viewer.MilepostUnitsMetric);
                        //HudScroll
                        if (Viewer.SelectedTrain.Name == thisTrain.Name)
                            TextToYellowColor = status[0];

                        statusDispatcher.Add(status);
                    }
                }
            }

            // next is active AI trains which are not delayed
            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC && thisTrain.TrainType != Train.TRAINTYPE.PLAYER
                    && thisTrain.TrainType != Train.TRAINTYPE.AI_INCORPORATED)
                {
                    if (!thisTrain.Delay.HasValue || thisTrain.Delay.Value.TotalMinutes < 1)
                    {
                        var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                        status = thisTrain.AddMovementState(status, Viewer.MilepostUnitsMetric);
                        //HudScroll
                        if (Viewer.SelectedTrain.Name == thisTrain.Name)
                            TextToYellowColor = status[0];

                        statusDispatcher.Add(status);
                    }
                }
            }

            // finally is static AI trains
            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState == AITrain.AI_MOVEMENT_STATE.AI_STATIC && thisTrain.TrainType != Train.TRAINTYPE.PLAYER)
                {
                    var status = thisTrain.GetStatus(Viewer.MilepostUnitsMetric);
                    status = thisTrain.AddMovementState(status, Viewer.MilepostUnitsMetric);

                    //HudScroll
                    if (Viewer.SelectedTrain.Name == thisTrain.Name)
                        TextToYellowColor = status[0];

                    statusDispatcher.Add(status);
                }
            }

            //HudScroll. Pages count from nLinesShow number.
            TextLineNumber(statusDispatcher.Count, table.CurrentRow, columnsCount);

            //Number of lines to show. HudScroll
            for (var i = (hudWindowLinesActualPage * nLinesShow) - nLinesShow; i < (Viewer.Simulator.Trains.Count > hudWindowLinesActualPage * nLinesShow ? hudWindowLinesActualPage * nLinesShow : Viewer.Simulator.Trains.Count); i++)
            {
                if (statusDispatcher.Count > i)
                {
                    //Calc col number and take in count 2 left columns car and consist name
                    TextColNumber(statusDispatcher[i][PathHeaderColumn], 2, false);

                    var arrow = "";
                    //Add yellow color to string.
                    var EndText = statusDispatcher[i][0].Length == TextToYellowColor.Length && statusDispatcher[i][0].Contains(TextToYellowColor) ? "???" : "";

                    //DrawScrollArrows() can't be used because it works with TableAddLines, here we work with TableSetCell.
                    if (hudWindowColumnsActualPage > 0)
                    {
                        var statusIndex = stringStatus.Count >= hudWindowColumnsActualPage ? hudWindowColumnsActualPage - 1 : stringStatus.Count - 1;
                        if (statusDispatcher[i][PathHeaderColumn].Contains(stringStatus[statusIndex]) || stringStatus[statusIndex].EndsWith("???"))
                            EndText = "";

                        if (stringStatus.Count > 1 && stringStatus.Count <= hudWindowColumnsActualPage)
                        {
                            arrow = "◄";// \u25C0
                            TableSetCell(table, 2, hudWindowColumnsActualPage > 1 ? stringStatus[(stringStatus.Count < hudWindowColumnsActualPage ? stringStatus.Count - 1 : hudWindowColumnsActualPage - 1)] + EndText : stringStatus[hudWindowColumnsActualPage - 1] + EndText);
                        }
                        else if (stringStatus.Count > 1 && hudWindowColumnsActualPage == 1)
                        {
                            arrow = "►";// \u25B6
                            TableSetCell(table, 2, hudWindowColumnsActualPage > 0 ? stringStatus[hudWindowColumnsActualPage - 1] + EndText : stringStatus[hudWindowColumnsActualPage - 1] + EndText);
                        }
                        else if (stringStatus.Count > 1 && hudWindowColumnsActualPage > 1 && stringStatus.Count >= hudWindowColumnsActualPage)
                        {
                            arrow = "↔";// \u2194
                            TableSetCell(table, 2, hudWindowColumnsActualPage > 0 ? stringStatus[hudWindowColumnsActualPage - 1] + EndText : stringStatus[hudWindowColumnsActualPage - 1] + EndText);
                        }
                        else if (stringStatus.Count == 1 && hudWindowColumnsActualPage == 1 && stringStatus.Count >= hudWindowColumnsActualPage)
                        {
                            arrow = "◄";// \u25C0
                            TableSetCell(table, 2, statusDispatcher[i][PathHeaderColumn]);
                        }
                        else
                            TableSetCell(table, table.CurrentRow, 2, statusDispatcher[i][PathHeaderColumn]);

                        //Add yellow color to string.
                        EndText = statusDispatcher[i][0].Length == TextToYellowColor.Length && statusDispatcher[i][0].Contains(TextToYellowColor) ? "???" : "";
                        TableSetCell(table, table.CurrentRow, 0, arrow + statusDispatcher[i][0] + EndText);
                        TableSetCell(table, table.CurrentRow, 1, statusDispatcher[i][11] + EndText);
                    }
                    else
                        for (int iCell = 0; iCell < statusDispatcher[0].Length; iCell++)
                            TableSetCell(table, table.CurrentRow, iCell, statusDispatcher[i][iCell] + (iCell == PathHeaderColumn && !statusDispatcher[i][PathHeaderColumn].EndsWith("???") ? "" : EndText));//Avoid yellow color for Path info

                    TableAddLine(table);
                }
            }




#if WITH_PATH_DEBUG
            TextPageHeading(table, "PATH info");

            TableSetCells(table, 0, "Train", "Path ");
            TableSetCells(table, 8, "Type", "Info");
            TableAddLine(table);

            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                {
                    TextPagePathInfo(thisTrain, table);
                }
            }
            TextPageHeading(table, "ACTIONs info");

            TableSetCells(table, 0, "Train", "Actions ");
            TableAddLine(table);

            foreach (var thisTrain in Viewer.Simulator.AI.AITrains)
            {
                if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                {
                    TextPageActionsInfo(thisTrain, table);
                }
            }
#endif

        }
#if WITH_PATH_DEBUG
        void TextPagePathInfo(AITrain thisTrain, TableData table)
        {
            // next is active AI trains
            if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
            {
                var status = thisTrain.GetPathStatus(Viewer.MilepostUnitsMetric);
                status = thisTrain.AddPathInfo(status, Viewer.MilepostUnitsMetric);
                for (var iCell = 0; iCell < status.Length; iCell++)
                    TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                TableAddLine(table);
            }
        }

        void TextPageActionsInfo(AITrain thisTrain, TableData table)
        {
            // next is active AI trains
            if (thisTrain.MovementState != AITrain.AI_MOVEMENT_STATE.AI_STATIC)
            {
                var status = thisTrain.GetActionStatus(Viewer.MilepostUnitsMetric);
                for (var iCell = 0; iCell < status.Length; iCell++)
                    TableSetCell(table, table.CurrentRow, iCell, status[iCell]);
                TableAddLine(table);
            }
        }
#endif

        void TextPageWeather(TableData table)
        {
            TextPageHeading(table, Viewer.Catalog.GetString("WEATHER INFORMATION"));

            //Disable Hudscroll.
            Viewer.HUDScrollWindow.Visible = WebServerPageNo > 0 ? true : false;//HudScroll

            if (hudWindowFullScreen || WebServerEnabled)
                TableSetLabelValueColumns(table, 0, 2);

            TableAddLabelValue(table, Viewer.Catalog.GetString("Visibility"), Viewer.Catalog.GetStringFmt("{0:N0} m", Viewer.Simulator.Weather.FogDistance));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Cloud cover"), Viewer.Catalog.GetStringFmt("{0:F0} %", Viewer.Simulator.Weather.OvercastFactor * 100));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Intensity"), Viewer.Catalog.GetStringFmt("{0:F4} p/s/m^2", Viewer.Simulator.Weather.PricipitationIntensityPPSPM2));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Liquidity"), Viewer.Catalog.GetStringFmt("{0:F0} %", Viewer.Simulator.Weather.PrecipitationLiquidity * 100));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Wind"), Viewer.Catalog.GetStringFmt("{0:F1},{1:F1} m/s", Viewer.Simulator.Weather.WindSpeedMpS.X, Viewer.Simulator.Weather.WindSpeedMpS.Y));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Amb Temp"), FormatStrings.FormatTemperature(Viewer.PlayerLocomotive.Train.TrainOutsideTempC, Viewer.PlayerLocomotive.IsMetric, false));
        }

        void TextPageDebugInfo(TableData table)
        {
            TextPageHeading(table, Viewer.Catalog.GetString("DEBUG INFORMATION"));

            //Disable Hudscroll.
            Viewer.HUDScrollWindow.Visible = WebServerPageNo > 0 ? true : false;//HudScroll

            if (hudWindowFullScreen || WebServerEnabled)
                TableSetLabelValueColumns(table, 0, 2);

            var allocatedBytesPerSecond = AllocatedBytesPerSecCounter == null ? 0 : AllocatedBytesPerSecCounter.NextValue();
            if (allocatedBytesPerSecond >= 1 && AllocatedBytesPerSecLastValue != allocatedBytesPerSecond)
                AllocatedBytesPerSecLastValue = allocatedBytesPerSecond;

            TableAddLabelValue(table, Viewer.Catalog.GetString("Logging enabled"), Viewer.Settings.DataLogger ? Viewer.Catalog.GetString("Yes") : Viewer.Catalog.GetString("No"));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Build"), VersionInfo.Build);
            TableAddLabelValue(table, Viewer.Catalog.GetString("Memory"), Viewer.Catalog.GetStringFmt("{0:F0} MB ({5}, {6}, {7}, {8}, {9}, {10}, {1:F0} MB managed, {11:F0} kB/frame allocated, {2:F0}/{3:F0}/{4:F0} GCs)", GetWorkingSetSize() / 1024 / 1024, GC.GetTotalMemory(false) / 1024 / 1024, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), Viewer.TextureManager.GetStatus(), Viewer.MaterialManager.GetStatus(), Viewer.ShapeManager.GetStatus(), Viewer.World.Terrain.GetStatus(), SoundItem.GetStatus(), ALSoundSource.GetStatus(), AllocatedBytesPerSecLastValue / Viewer.RenderProcess.FrameRate.SmoothedValue / 1024));
            TableAddLabelValue(table, Viewer.Catalog.GetString("CPU"), Viewer.Catalog.GetStringFmt("{0:F0}% ({1})", (Viewer.RenderProcess.Profiler.CPU.SmoothedValue + Viewer.UpdaterProcess.Profiler.CPU.SmoothedValue + Viewer.LoaderProcess.Profiler.CPU.SmoothedValue + Viewer.SoundProcess.Profiler.CPU.SmoothedValue) / ProcessorCount, Viewer.Catalog.GetPluralStringFmt("{0} logical processor", "{0} logical processors", ProcessorCount)));
            TableAddLabelValue(table, Viewer.Catalog.GetString("GPU"), Viewer.Catalog.GetStringFmt("{0:F0} FPS (50th/95th/99th percentiles {1:F1} / {2:F1} / {3:F1} ms) ", Viewer.RenderProcess.FrameRate.SmoothedValue, Viewer.RenderProcess.FrameTime.SmoothedP50 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP95 * 1000, Viewer.RenderProcess.FrameTime.SmoothedP99 * 1000));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Adapter"), Viewer.Catalog.GetStringFmt("{0} ({1:F0} MB) ({2:F0} pixels x {3:F0} pixels)", Viewer.AdapterDescription, Viewer.AdapterMemory / 1024 / 1024, Viewer.DisplaySize.X, Viewer.DisplaySize.Y));
            if (Viewer.Settings.DynamicShadows)
            {
                TableSetCells(table, 3, Enumerable.Range(0, RenderProcess.ShadowMapCount).Select(i => String.Format(Viewer.Catalog.GetStringFmt("{0}/{1}", RenderProcess.ShadowMapDistance[i], RenderProcess.ShadowMapDiameter[i]))).ToArray());
                TableSetCell(table, 3 + RenderProcess.ShadowMapCount, Viewer.Catalog.GetStringFmt("({0}x{0})", Viewer.Settings.ShadowMapResolution));
                TableAddLine(table, Viewer.Catalog.GetString("Shadow maps"));
                TableSetCells(table, 3, Viewer.RenderProcess.ShadowPrimitivePerFrame.Select(p => p.ToString("F0")).ToArray());
                TableAddLabelValue(table, Viewer.Catalog.GetString("Shadow primitives"), Viewer.Catalog.GetStringFmt("{0:F0}", Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum()));
            }
            TableSetCells(table, 3, Viewer.RenderProcess.PrimitivePerFrame.Select(p => p.ToString("F0")).ToArray());
            TableAddLabelValue(table, Viewer.Catalog.GetString("Render primitives"), Viewer.Catalog.GetStringFmt("{0:F0}", Viewer.RenderProcess.PrimitivePerFrame.Sum()));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Render process"), Viewer.Catalog.GetStringFmt("{0:F0}% ({1:F0}% {2})", Viewer.RenderProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue, Viewer.Catalog.GetString("wait")));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Updater process"), Viewer.Catalog.GetStringFmt("{0:F0}% ({1:F0}% {2})", Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue, Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue, Viewer.Catalog.GetString("wait")));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Loader process"), Viewer.Catalog.GetStringFmt("{0:F0}% ({1:F0}% {2})", Viewer.LoaderProcess.Profiler.Wall.SmoothedValue, Viewer.LoaderProcess.Profiler.Wait.SmoothedValue, Viewer.Catalog.GetString("wait")));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Sound process"), Viewer.Catalog.GetStringFmt("{0:F0}% ({1:F0}% {2})", Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.SoundProcess.Profiler.Wait.SmoothedValue, Viewer.Catalog.GetString("wait")));
            TableAddLabelValue(table, Viewer.Catalog.GetString("Total process"), Viewer.Catalog.GetStringFmt("{0:F0}% ({1:F0}% {2})", Viewer.RenderProcess.Profiler.Wall.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue + Viewer.LoaderProcess.Profiler.Wall.SmoothedValue + Viewer.SoundProcess.Profiler.Wall.SmoothedValue, Viewer.RenderProcess.Profiler.Wait.SmoothedValue + Viewer.UpdaterProcess.Profiler.Wait.SmoothedValue + Viewer.LoaderProcess.Profiler.Wait.SmoothedValue + Viewer.SoundProcess.Profiler.Wait.SmoothedValue, Viewer.Catalog.GetString("wait")));
            TableSetCells(table, 0, Viewer.Catalog.GetString("Camera"), "", Viewer.Camera.TileX.ToString("F0"), Viewer.Camera.TileZ.ToString("F0"), Viewer.Camera.Location.X.ToString("F2"), Viewer.Camera.Location.Y.ToString("F2"), Viewer.Camera.Location.Z.ToString("F2"), String.Format("{0:F1} {1}", Viewer.Tiles.GetElevation(Viewer.Camera.CameraWorldLocation), FormatStrings.m), Viewer.Settings.LODBias + "%", String.Format("{0} {1}", Viewer.Settings.ViewingDistance, FormatStrings.m), Viewer.Settings.DistantMountains ? String.Format("{0:F0} {1}", (float)Viewer.Settings.DistantMountainsViewingDistance * 1e-3f, FormatStrings.km) : "");
            TableAddLine(table);
        }

        /// <summary>
        /// Columns count
        /// Used in TextLineNumber(int CarsCount, int CurrentRow, int ColumnCount)
        /// PathColumn == true return 'Path' header column position
        /// </summary>
        /// <param name="table"></param>
        /// <param name="PathColumn"></param>
        /// <returns "nColumnsCount"></returns>
        private int ColumnsCount(TableData table, bool PathColumn)
        {
            //Check columns for not null value. HudScroll
            int nColumnsCount = 0;
            for (int i = 0; i < table.Cells.GetLength(1); i++)
            {
                if (table.Cells[table.CurrentRow, i] != null)
                {
                    //Search Path column position. Dispatcher Information
                    //Avoid conflict with human dispatcher
                    var dato = table.Cells[table.CurrentRow, i].ToString();
                    if (PathColumn && table.Cells[table.CurrentRow, i].ToString() == Viewer.Catalog.GetString("Path")) break;

                    nColumnsCount++;
                }
            }
            return nColumnsCount;
        }

        /// <summary>
        /// Compute the max. lines to show.
        /// </summary>
        /// <param name="CarsCount"></param>
        /// <param name="CurrentRow"></param>
        /// <param name="ColumnCount"></param>
        void TextLineNumber(int CarsCount, int CurrentRow, int ColumnCount)
        {
            //LinesPages
            nLinesShow = (Viewer.DisplaySize.Y / TextFont.Height) - CurrentRow - 1;
            if (nLinesShow < 1) nLinesShow = 1;
            hudWindowLinesPagesCount = (nLinesShow >= CarsCount) ? 1 : (int)Math.Ceiling(Convert.ToDouble(CarsCount / nLinesShow) + 0.5);

            //Character per line
            charFitPerLine = CharFitPerLine("");

            //Columns pages
            int statusPathLenght = ColumnCount * columnsChars;
            hudWindowColumnsPagesCount = (statusPathLenght < charFitPerLine) ? 0 : (int)Math.Ceiling(Convert.ToDouble(statusPathLenght / charFitPerLine) + 0.5);

            //Hide - Show HUDScrollWindow
            if (Viewer.HUDScrollWindow.Visible && WebServerPageNo == 0 && (hudWindowLinesActualPage == 1 && hudWindowLinesPagesCount == 1 && hudWindowColumnsActualPage == 0 && hudWindowColumnsPagesCount == 0) && TextPages[TextPage] != TextPageLocomotiveInfo && !hudWindowFullScreen)
                Viewer.HUDScrollWindow.Visible = false;
            if (!Viewer.HUDScrollWindow.Visible && WebServerPageNo > 0 && (hudWindowLinesPagesCount > 1 || hudWindowColumnsPagesCount > 0))
                Viewer.HUDScrollWindow.Visible = true;
        }

        /// <summary>
        /// Compute the string width to be displayed.
        /// </summary>
        /// <param name="StringStatus"></param>
        /// <param name="initColumn"></param>
        void TextColNumber(string StringStatus, int initColumn, bool IsSteamLocomotive)
        {
            char[] stringToChar = StringStatus.TrimEnd('\t').ToCharArray();

            stringStatus.Clear();//Reset
            var tabCount = StringStatus.TrimEnd('\t').Count(x => x == '\t');
            //Character per line
            charFitPerLine = CharFitPerLine("");
            string space = new string('X', columnsChars);

            var StringStatusLength = tabCount * columnsChars;
            var lastText = StringStatus.Substring(StringStatus.LastIndexOf("\t") + 1);
            StringStatusLength = StringStatus.EndsWith("\t") ? StringStatusLength : StringStatusLength + lastText.Length;
            var CurrentPathColumnsPagesCount = (StringStatusLength < charFitPerLine) ? 0 : (int)Math.Ceiling(Convert.ToDouble(StringStatusLength / charFitPerLine) + 0.5);

            //TO DO: Apply new code to DispacherInfo
            if (CurrentPathColumnsPagesCount == 0 && TextPages[TextPage] == TextPageDispatcherInfo)
                CurrentPathColumnsPagesCount = 1;

            //Update columns pages count.
            if (CurrentPathColumnsPagesCount > hudWindowColumnsPagesCount)
                hudWindowColumnsPagesCount = CurrentPathColumnsPagesCount;

            List<string> cellTextList = new List<string>();
            int i = 0;//Counter original status with tab code
            //87 = character fit per line with minimun 800*600 display size.
            //Font Monospace.
            if (StringStatus.Contains("\t"))
            {
                i = 0;
                bool lText = false;
                List<string> dataText = new List<string>();
                List<string> dataValue = new List<string>();
                List<string> dataTextString = new List<string>();
                string[] statusSplit = StringStatus.TrimEnd('\t').Split('\t');
                Dictionary<int, string> CumulativeTextStatus = new Dictionary<int, string>();
                Dictionary<int, string> CumulativeTabStatus = new Dictionary<int, string>();
                var cumulativeLenght = 0;
                var cumulativeTextStatus = "";
                var cumulativeTabStatus = "";
                var cellString = "";
                var cellIndex = 0;
                CumulativeTabStatus.Clear();
                CumulativeTextStatus.Clear();
                foreach (var cell in statusSplit)
                {
                    cellIndex++;
                    if (cell == "")
                    {
                        var cellStringLength = cellString.Length > columnsChars ? 0 : columnsChars;
                        cumulativeLenght = cumulativeLenght + cellStringLength;
                        CumulativeTextStatus.Add(cumulativeLenght, cumulativeTextStatus + (cellString.Length > columnsChars ? "" : space));
                        CumulativeTabStatus.Add(cumulativeLenght, cumulativeTabStatus + "\t");
                        lText = false;
                        //Reset
                        cellString = "";
                        cumulativeTextStatus = "";
                        cumulativeTabStatus = "";
                        continue;
                    }
                    else if (cell.Length > 0 && lText && !CumulativeTextStatus.ContainsKey(cumulativeLenght))
                    {
                        CumulativeTextStatus.Add(cumulativeLenght, cumulativeTextStatus);
                        CumulativeTabStatus.Add(cumulativeLenght, cumulativeTabStatus);
                        lText = false;
                    }
                    //Avoid cell > columnsChars
                    cellString = cell.Length > columnsChars ? cell + CellTabSpace(cell.Substring(columnsChars, cell.Length - columnsChars), 1) : cell + CellTabSpace(cell, 1);
                    cumulativeLenght = cumulativeLenght + cellString.Length;
                    cumulativeTextStatus = cellString;
                    cumulativeTabStatus = cell + "\t";
                    lText = true;

                    if (lText && statusSplit.Length == cellIndex)
                    {
                        CumulativeTextStatus.Add(cumulativeLenght, cumulativeTextStatus.TrimEnd('X'));
                        CumulativeTabStatus.Add(cumulativeLenght, cumulativeTabStatus.TrimEnd('\t'));
                    }
                }

                var cumulativeTabString = "";
                var cumulativeTextString = "";
                var offsetFlag = 0;
                cellIndex = 0;
                foreach (var cell in CumulativeTextStatus)
                {
                    cumulativeTabString = cumulativeTabString + CumulativeTabStatus[cell.Key];
                    cumulativeTextString = cumulativeTextString + cell.Value;
                    if (cell.Key - offsetFlag > charFitPerLine || (cell.Key - offsetFlag < charFitPerLine && CumulativeTextStatus.Keys.Last() == cell.Key))
                    {
                        //Place first column data at cumulativeTabString begin
                        cumulativeTabString = cumulativeTabString.Contains(CumulativeTabStatus.Values.First()) ?
                                   cumulativeTabString :
                                   CumulativeTabStatus.Values.First() + cumulativeTabString;

                        cumulativeTextString = cumulativeTextString.Contains(CumulativeTextStatus.Values.First()) ?
                                   cumulativeTextString :
                                   CumulativeTextStatus.Values.First() + cumulativeTextString;

                        if (CumulativeTextStatus.ElementAt(0).Value.Contains(":"))
                        {
                            //cumulativeTextString.TrimEnd('X').Length
                            if (cumulativeTextString.Length <= charFitPerLine)
                            {
                                stringStatus.Add(cumulativeTabString.TrimEnd('\t'));

                                offsetFlag = cell.Key;
                                cumulativeTabString = "";
                                cumulativeTextString = "";
                            }
                            else
                            {
                                //string > charFitPerLine
                                //truncate to charFitPerLine
                                int countDown = cellIndex;
                                while (CumulativeTabStatus.ElementAt(countDown).Key - offsetFlag > charFitPerLine)
                                {
                                    countDown--;
                                }
                                int newCellKey = CumulativeTabStatus.ElementAt(countDown).Key;
                                if (cellIndex - countDown == 1)
                                    stringStatus.Add(cumulativeTabString.Substring(0, cumulativeTabString.Length - (charFitPerLine - newCellKey < columnsChars ? CumulativeTabStatus[cell.Key].Length : CumulativeTabStatus[newCellKey].Length)));
                                else
                                    stringStatus.Add(cumulativeTabString.Substring(0, cumulativeTabString.Length - CumulativeTabStatus[cell.Key].Length));

                                offsetFlag = newCellKey;
                                cumulativeTabString = cellIndex - countDown == 1 && charFitPerLine - newCellKey < columnsChars ? CumulativeTabStatus[cell.Key] : CumulativeTabStatus[cell.Key];
                                cumulativeTextString = cellIndex - countDown == 1 && charFitPerLine - newCellKey < columnsChars ? CumulativeTextStatus[cell.Key] : cell.Value.TrimStart('X');
                            }
                        }
                        else
                        {
                            if (cumulativeTextString.Length <= charFitPerLine)
                            {
                                stringStatus.Add(cumulativeTabString.TrimEnd('\t'));
                                offsetFlag = cell.Key;
                                cumulativeTabString = "";
                                cumulativeTextString = "";
                            }
                            else
                            {
                                //string > charFitPerLine
                                //truncate to charFitPerLine
                                int countDown = cellIndex;
                                while (CumulativeTabStatus.ElementAt(countDown).Key - offsetFlag > charFitPerLine)
                                {
                                    countDown--;
                                }
                                int newCellKey = CumulativeTabStatus.ElementAt(countDown).Key;
                                if (cellIndex - countDown == 1)
                                    stringStatus.Add(cumulativeTabString.Substring(0, cumulativeTabString.Length - CumulativeTabStatus[cell.Key].Length));
                                else
                                    stringStatus.Add(cumulativeTabString.Substring(0, cumulativeTabString.Length - CumulativeTabStatus[cell.Key].Length));

                                offsetFlag = newCellKey;
                                cumulativeTabString = cellIndex - countDown == 1 && charFitPerLine - newCellKey < columnsChars ? CumulativeTabStatus[cell.Key] : CumulativeTabStatus[cell.Key];
                                cumulativeTextString = cellIndex - countDown == 1 && charFitPerLine - newCellKey < columnsChars ? CumulativeTextStatus[cell.Key] : cell.Value.TrimStart('X');
                            }
                        }
                    }
                    cellIndex++;
                }
                if (cumulativeTextString.Length > 0 && cumulativeTextString.TrimEnd('X').Length <= charFitPerLine)
                {
                    stringStatus.Add(cumulativeTabString.Contains(CumulativeTabStatus.Values.First()) ? cumulativeTabString.TrimEnd('\t') : CumulativeTabStatus.Values.First() + cumulativeTabString.TrimEnd('\t'));
                    cumulativeTabString = "";
                    cumulativeTextString = "";
                }

                //Add '\n' to all stringStatus when 'PlayerLoco' Header
                if (stringStatus.Count > 0 && stringStatus[0].Contains(Viewer.Catalog.GetString("PlayerLoco")))
                {
                    if (stringStatus[stringStatus.Count - 1].EndsWith("\n"))
                    {//TO DO: rewrite this code using LINQ
                        for (int n = 0; n < stringStatus.Count - 1; n++)
                        {
                            if (!stringStatus[n].EndsWith("\n"))
                                stringStatus[n] = stringStatus[n] + "\n";
                        }
                    }
                }
                //Update 'page right' and 'page left' labels.
                if (stringStatus.Count > 1 && stringStatus.Count > hudWindowColumnsPagesCount)
                    hudWindowColumnsPagesCount = stringStatus.Count;
            }
            else
            {   //StringStatus without \t.
                //Only Force information and Dispatcher information.
                //Horizontal scroll.
                stringStatus.Clear();//Reset
                var n = 0;
                //Take in count left columns.
                charFitPerLine = initColumn > 0 ? charFitPerLine - (columnsChars * initColumn) - 1 : charFitPerLine;
                for (i = 0; i < StringStatusLength; i += charFitPerLine)
                {
                    if (StringStatusLength - i > charFitPerLine)
                        stringStatus.Add(StringStatus.Substring(i, charFitPerLine) + (StringStatus.EndsWith("???") ? "???" : ""));//Required by human dispacher path data
                    else
                        stringStatus.Add(StringStatus.Substring(i, StringStatus.Length - i));
                    n++;
                    if (n > hudWindowColumnsPagesCount) break;
                }

                //Update 'page right' and 'page left' labels.
                if (stringStatus.Count > hudWindowColumnsPagesCount)
                    hudWindowColumnsPagesCount = stringStatus.Count;
            }

            CurrentPathColumnsPagesCount = (StringStatusLength < charFitPerLine) ? 0 : (int)Math.Ceiling(Convert.ToDouble(StringStatusLength / charFitPerLine) + 0.5);
            //Update columns pages count.
            if (CurrentPathColumnsPagesCount > hudWindowColumnsPagesCount)
                hudWindowColumnsPagesCount = CurrentPathColumnsPagesCount;

            //Hide - Show HUDScrollWindow
            var locomotive = Viewer.PlayerLocomotive;
            var train = locomotive.Train;
            if (Viewer.HUDScrollWindow.Visible && WebServerPageNo == 0 && hudWindowColumnsPagesCount == 0 && hudWindowLinesPagesCount == 1 && TextPages[TextPage] != TextPageLocomotiveInfo && !hudWindowFullScreen)
                Viewer.HUDScrollWindow.Visible = false;
            if ((!Viewer.HUDScrollWindow.Visible && hudWindowColumnsPagesCount > 0) || WebServerPageNo > 0 || (TextPages[TextPage] == TextPageLocomotiveInfo && (IsSteamLocomotive || hudWindowLocoPagesCount > 1)))
                Viewer.HUDScrollWindow.Visible = true;
        }

        /// <summary>
        /// Count space requiered to fit a column
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="tabCount"></param>
        /// <returns></returns>
        private string CellTabSpace(string cell, int tabCount)
        {
            var cellSpace = "";
            string space = new string('X', columnsChars);
            int cellColumns = (int)Math.Ceiling((decimal)cell.Length / columnsChars);
            cellSpace = cell.Length > 0 && tabCount > 0 ? space.Substring(0, Math.Abs((columnsChars * cellColumns) - cell.Length)) : "";
            cellSpace = tabCount > cellColumns ? cellSpace + space : cellSpace;
            return cellSpace;
        }

        /// <summary>
        /// Compute how many character fit per line.
        /// </summary>
        /// <returns> x
        /// </returns>
        public int CharFitPerLine(string status)
        {
            var stringReference = status.Length > 0 ? status : "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890 \",.-+|!$%&/()=?;:'_[]";
            var textStringPixels = TextFont.MeasureString(stringReference);
            var charWidth = textStringPixels / stringReference.Length;
            columnsChars = (int)Math.Ceiling(Convert.ToDouble(ColumnWidth / charWidth));
            //Minus left and right space
            var x = (Viewer.DisplaySize.X - (TextOffset * 2)) / charWidth;
            return (int)x;
        }

        bool lResetHudScroll = false;
        /// <summary>
        /// Reset Scroll Control window
        /// </summary>
        private void ResetHudScroll()
        {
            if (!lResetHudScroll)
            {
                hudWindowLinesActualPage = 1;
                hudWindowLinesPagesCount = 1;
                hudWindowColumnsActualPage = 0;
                hudWindowColumnsPagesCount = 0;
                //Allow to show loco info by default
                hudWindowLocoActualPage = TextPages[TextPage] == TextPageLocomotiveInfo || (WebServerEnabled && TextPages[WebServerPageNo] == TextPageLocomotiveInfo) ? 1 : 0;
                hudWindowLocoPagesCount = 1;
                hudWindowSteamLocoLead = false;
                lResetHudScroll = true;
                BrakeInfoVisible = false;//Enable PgUp & PgDown (Scroll nav window)
            }
        }

        /// <summary>
        /// Allow to draw arrows.
        /// Consist info data.
        /// Locomotive info headers.
        /// </summary>
        /// <param name="statusConsist"></param>
        /// <param name="table"></param>
        private void DrawScrollArrows(List<string> statusConsist, TableData table, bool IsSteamLocomotive)
        {
            for (int i = 0; i < statusConsist.Count; i++)
            {
                if (i > 0 && stringStatus.Count > 0 && i < 2 && (statusConsist[i] == stringStatus[i - 1]) || i > 1 && (statusConsist[i - 2] == statusConsist[i]))
                    continue;

                TextColNumber(statusConsist[i], 0, IsSteamLocomotive);
                if (hudWindowColumnsActualPage > 0)
                {//◄ \u25C0 - ► \u25B6 - ↔ \u2194
                    if (stringStatus.Count > 1 && stringStatus.Count <= hudWindowColumnsActualPage)
                        TableAddLines(table, hudWindowColumnsActualPage > 1 ? "◀" + stringStatus[(stringStatus.Count < hudWindowColumnsActualPage ? stringStatus.Count - 1 : hudWindowColumnsActualPage - 1)] : stringStatus[hudWindowColumnsActualPage - 1]);
                    else if (stringStatus.Count > 1 && hudWindowColumnsActualPage == 1)
                        TableAddLines(table, hudWindowColumnsActualPage > 0 ? "▶" + stringStatus[hudWindowColumnsActualPage - 1] : stringStatus[hudWindowColumnsActualPage - 1]);
                    else if (stringStatus.Count > 1 && hudWindowColumnsActualPage > 1 && stringStatus.Count >= hudWindowColumnsActualPage)
                        TableAddLines(table, hudWindowColumnsActualPage > 0 ? "↔" + stringStatus[hudWindowColumnsActualPage - 1] : stringStatus[hudWindowColumnsActualPage - 1]);
                    else
                    {
                        TableAddLines(table, (stringStatus.Count > 0 ? stringStatus[0] : statusConsist[i]));
                    }
                }
                else
                    TableAddLines(table, statusConsist[i]);
            }
        }

        static void TextPageHeading(TableData table, string name)
        {
            TableAddLine(table);
            TableAddLine(table, name);
        }

        #region Native code
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public class MEMORYSTATUSEX
        {
            public uint Size;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

        readonly ulong ProcessVirtualAddressLimit;
        #endregion

        public ulong GetWorkingSetSize()
        {
            // Get memory usage (working set).
            return (ulong)Process.GetCurrentProcess().WorkingSet64;
        }

        public ulong GetVirtualAddressLimit()
        {
            var buffer = new MEMORYSTATUSEX { Size = 64 };
            GlobalMemoryStatusEx(buffer);
            return Math.Min(buffer.TotalVirtual, buffer.TotalPhysical);
        }
    }

    public class HUDGraphSet
    {
        readonly Viewer Viewer;
        readonly Material Material;
        readonly Vector2 Margin = new Vector2(40, 10);
        readonly int Spacing;
        readonly List<Graph> Graphs = new List<Graph>();

        public HUDGraphSet(Viewer viewer, Material material)
        {
            Viewer = viewer;
            Material = material;
            Spacing = Viewer.WindowManager.TextFontSmallOutlined.Height + 2;
        }

        public HUDGraphMesh AddOverlapped(Color color, int height)
        {
            return Add("", "", "", color, height, true);
        }

        public HUDGraphMesh Add(string labelName, string labelMin, string labelMax, Color color, int height)
        {
            return Add(labelName, labelMin, labelMax, color, height, false);
        }

        HUDGraphMesh Add(string labelName, string labelMin, string labelMax, Color color, int height, bool overlapped)
        {
            HUDGraphMesh mesh;
            Graphs.Add(new Graph()
            {
                Mesh = mesh = new HUDGraphMesh(Viewer, color, height),
                LabelName = labelName,
                LabelMin = labelMin,
                LabelMax = labelMax,
                Overlapped = overlapped,
            });
            for (var i = Graphs.Count - 1; i >= 0; i--)
            {
                var previousGraphs = Graphs.Skip(i + 1).Where(g => !g.Overlapped);
                Graphs[i].YOffset = (int)previousGraphs.Sum(g => g.Mesh.GraphPos.W) + Spacing * previousGraphs.Count();
            }
            return mesh;
        }

        public void PrepareFrame(RenderFrame frame)
        {
            var matrix = Matrix.Identity;
            for (var i = 0; i < Graphs.Count; i++)
            {
                Graphs[i].Mesh.GraphPos.X = Viewer.DisplaySize.X - Margin.X - Graphs[i].Mesh.GraphPos.Z;
                Graphs[i].Mesh.GraphPos.Y = Margin.Y + Graphs[i].YOffset;
                frame.AddPrimitive(Material, Graphs[i].Mesh, RenderPrimitiveGroup.Overlay, ref matrix);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var box = new Rectangle();
            for (var i = 0; i < Graphs.Count; i++)
            {
                if (!string.IsNullOrEmpty(Graphs[i].LabelName))
                {
                    box.X = (int)Graphs[i].Mesh.GraphPos.X;
                    box.Y = Viewer.DisplaySize.Y - (int)Graphs[i].Mesh.GraphPos.Y - (int)Graphs[i].Mesh.GraphPos.W - Spacing;
                    box.Width = (int)Graphs[i].Mesh.GraphPos.Z;
                    box.Height = Spacing;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box, Point.Zero, Graphs[i].LabelName, LabelAlignment.Right, Color.White);
                    box.X = box.Right + 3;
                    box.Y += Spacing - 3;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box.Location, Graphs[i].LabelMax, Color.White);
                    box.Y += (int)Graphs[i].Mesh.GraphPos.W - Spacing + 7;
                    Viewer.WindowManager.TextFontSmallOutlined.Draw(spriteBatch, box.Location, Graphs[i].LabelMin, Color.White);
                }
            }
        }

        class Graph
        {
            public HUDGraphMesh Mesh;
            public string LabelName;
            public string LabelMin;
            public string LabelMax;
            public int YOffset;
            public bool Overlapped;
        }
    }

    public class HUDGraphMesh : RenderPrimitive
    {
        const int SampleCount = 1024 - 10 - 40; // Widest graphs we can fit in 1024x768.
        const int VerticiesPerSample = 6;
        const int PrimitivesPerSample = 2;
        const int VertexCount = VerticiesPerSample * SampleCount;

        readonly DynamicVertexBuffer VertexBuffer;
        readonly VertexBuffer BorderVertexBuffer;
        readonly Color Color;

        int SampleIndex;
        VertexPositionColor[] Samples = new VertexPositionColor[VertexCount];

        public Vector4 GraphPos; // xy = xy position, zw = width/height
        public Vector2 Sample; // x = index, y = count

        public HUDGraphMesh(Viewer viewer, Color color, int height)
        {
            VertexBuffer = new DynamicVertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), VertexCount, BufferUsage.WriteOnly);
            BorderVertexBuffer = new VertexBuffer(viewer.GraphicsDevice, typeof(VertexPositionColor), 10, BufferUsage.WriteOnly);
            var borderOffset = new Vector2(1f / SampleCount, 1f / height);
            var borderColor = new Color(1f, 1f, 1f, 0f);
            BorderVertexBuffer.SetData(new[] {
                // Bottom left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 0 - borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 0, 1), borderColor),
                // Bottom right
                new VertexPositionColor(new Vector3(1 + borderOffset.X, 0 - borderOffset.Y, 0), borderColor),
                new VertexPositionColor(new Vector3(1, 0, 0), borderColor),
                // Top right
                new VertexPositionColor(new Vector3(1 + borderOffset.X, 1 + borderOffset.Y, 0), borderColor),
                new VertexPositionColor(new Vector3(1, 1, 0), borderColor),
                // Top left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 1 + borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 1, 1), borderColor),
                // Bottom left
                new VertexPositionColor(new Vector3(0 - borderOffset.X, 0 - borderOffset.Y, 1), borderColor),
                new VertexPositionColor(new Vector3(0, 0, 1), borderColor),
            });
            Color = color;
            Color.A = 255;
            GraphPos.Z = SampleCount;
            GraphPos.W = height;
            Sample.Y = SampleCount;
        }

        void VertexBuffer_ContentLost()
        {
            VertexBuffer.SetData(0, Samples, 0, Samples.Length, VertexPositionColor.VertexDeclaration.VertexStride, SetDataOptions.NoOverwrite);
        }

        public void AddSample(float value)
        {
            value = MathHelper.Clamp(value, 0, 1);
            var x = Sample.X / Sample.Y;

            Samples[(int)Sample.X * VerticiesPerSample + 0] = new VertexPositionColor(new Vector3(x, value, 0), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 1] = new VertexPositionColor(new Vector3(x, value, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 2] = new VertexPositionColor(new Vector3(x, 0, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 3] = new VertexPositionColor(new Vector3(x, 0, 1), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 4] = new VertexPositionColor(new Vector3(x, value, 0), Color);
            Samples[(int)Sample.X * VerticiesPerSample + 5] = new VertexPositionColor(new Vector3(x, 0, 0), Color);
            VertexBuffer.SetData((int)Sample.X * VerticiesPerSample * VertexPositionColor.VertexDeclaration.VertexStride, Samples, (int)Sample.X * VerticiesPerSample, VerticiesPerSample, VertexPositionColor.VertexDeclaration.VertexStride, SetDataOptions.NoOverwrite);

            SampleIndex = (SampleIndex + 1) % SampleCount;
            Sample.X = SampleIndex;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (VertexBuffer.IsContentLost)
                VertexBuffer_ContentLost();

            // Draw border
            graphicsDevice.SetVertexBuffer(BorderVertexBuffer);
            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 8);

            // Draw graph area (skipping the next value to be written)
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            if (SampleIndex > 0)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, SampleIndex * PrimitivesPerSample);
            if (SampleIndex + 1 < SampleCount)
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, (SampleIndex + 1) * VerticiesPerSample, (SampleCount - SampleIndex - 1) * PrimitivesPerSample);
        }
    }

    public class HUDGraphMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesGraph;

        public HUDGraphMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.DebugShader;
            shader.CurrentTechnique = shader.Techniques["Graph"];
            if (ShaderPassesGraph == null) ShaderPassesGraph = shader.Techniques["Graph"].Passes.GetEnumerator();
            shader.ScreenSize = new Vector2(Viewer.DisplaySize.X, Viewer.DisplaySize.Y);

            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.DebugShader;

            ShaderPassesGraph.Reset();
            while (ShaderPassesGraph.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var graphMesh = item.RenderPrimitive as HUDGraphMesh;
                    if (graphMesh != null)
                    {
                        shader.GraphPos = graphMesh.GraphPos;
                        shader.GraphSample = graphMesh.Sample;
                        ShaderPassesGraph.Current.Apply();
                    }
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
    }
}
