﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

/* SCENERY
 * 
 * Scenery objects are specified in WFiles located in the WORLD folder of the route.
 * Each WFile describes scenery for a 2048 meter square region of the route.
 * This assembly is responsible for loading and unloading the WFiles as 
 * the camera moves over the route.  
 * 
 * Loaded WFiles are each represented by an instance of the WorldFile class. 
 * 
 * A SceneryDrawer object is created by the Viewer. Each time SceneryDrawer.Update is 
 * called, it disposes of WorldFiles that have gone out of range, and creates new 
 * WorldFile objects for WFiles that have come into range.
 * 
 * Currently the SceneryDrawer. Update is called 10 times a second from a background 
 * thread in the Viewer class.
 * 
 * SceneryDrawer loads the WFile in which the viewer is located, and the 8 WFiles 
 * surrounding the viewer.
 * 
 * When a WorldFile object is created, it creates StaticShape objects for each scenery
 * item.  The StaticShape objects add themselves to the Viewer's content list, sharing
 * mesh files and textures wherever possible.
 * 
 */

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Simulation;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Viewer3D
{
    public class SceneryDrawer
    {
        readonly Viewer Viewer;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        public List<WorldFile> WorldFiles = new List<WorldFile>();
        int TileX;
        int TileZ;
        int VisibleTileX;
        int VisibleTileZ;
        long CameraTile;
        int CameraTileX;
        int CameraTileZ;

        public SceneryDrawer(Viewer viewer)
        {
            Viewer = viewer;
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            var cancellation = Viewer.LoaderProcess.CancellationToken;
            Viewer.DontLoadNightTextures = (Program.Simulator.Settings.ConditionalLoadOfDayOrNightTextures &&
            ((Viewer.MaterialManager.sunDirection.Y > 0.05f && Program.Simulator.ClockTime % 86400 < 43200) ||
            (Viewer.MaterialManager.sunDirection.Y > 0.15f && Program.Simulator.ClockTime % 86400 >= 43200))) ? true : false;
            Viewer.DontLoadDayTextures = (Program.Simulator.Settings.ConditionalLoadOfDayOrNightTextures &&
            ((Viewer.MaterialManager.sunDirection.Y < -0.05f && Program.Simulator.ClockTime % 86400 >= 43200) ||
            (Viewer.MaterialManager.sunDirection.Y < -0.15f && Program.Simulator.ClockTime % 86400 < 43200))) ? true : false;
            if (TileX != VisibleTileX || TileZ != VisibleTileZ || Viewer.Simulator.RefreshWorld || Viewer.Simulator.RefreshWire)
            {
                TileX = VisibleTileX;
                TileZ = VisibleTileZ;
                var worldFiles = WorldFiles;
                var newWorldFiles = new List<WorldFile>();
                var oldWorldFiles = new List<WorldFile>(worldFiles);
                var needed = (int)Math.Ceiling((float)Viewer.Settings.ViewingDistance / 2048f);
                for (var x = -needed; x <= needed; x++)
                {
                    for (var z = -needed; z <= needed; z++)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;
                        var tile = worldFiles.FirstOrDefault(t => t.TileX == TileX + x && t.TileZ == TileZ + z);
                        var cameraTile = CameraTile;
                        CameraTileX = (int)(cameraTile / 100000);
                        CameraTileZ = (int)(Math.Abs(cameraTile) - (long)Math.Abs(CameraTileX) * 100000);
                        if ((CameraTileX != TileX || CameraTileZ != TileZ) && (Math.Abs(CameraTileX - (TileX + x)) > needed || Math.Abs(CameraTileZ - (TileZ + z)) > needed))
                            continue;
                        if (tile == null || Viewer.Simulator.RefreshWorld || Viewer.Simulator.RefreshWire)
                            tile = LoadWorldFile(TileX + x, TileZ + z, x == 0 && z == 0);
                        if (tile != null)
                        {
                            newWorldFiles.Add(tile);
                            oldWorldFiles.Remove(tile);
                        }
                    }
                }
                foreach (var tile in oldWorldFiles)
                    tile.Unload();
                WorldFiles = newWorldFiles;
                Viewer.tryLoadingNightTextures = true; // when Tiles loaded change you can try
                Viewer.tryLoadingDayTextures = true; // when Tiles loaded change you can try
            }
            else if (Viewer.NightTexturesNotLoaded && Program.Simulator.ClockTime % 86400 >= 43200 && Viewer.tryLoadingNightTextures)
            {
                var sunHeight = Viewer.MaterialManager.sunDirection.Y;
                if (sunHeight < 0.10f && sunHeight > 0.01)
                {
                    var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.HUDWindow.GetWorkingSetSize();
                    if (remainingMemorySpace >= 0) // if not we'll try again
                    {
                        // Night is coming, it's time to load the night textures
                        var success = Viewer.MaterialManager.LoadNightTextures();
                        if (success)
                        {
                            Viewer.NightTexturesNotLoaded = false;
                        }
                    }
                    Viewer.tryLoadingNightTextures = false;
                }
                else if (sunHeight <= 0.01)
                    Viewer.NightTexturesNotLoaded = false; // too late to try, we must give up and we don't load the night textures
            }
            else if (Viewer.DayTexturesNotLoaded && Program.Simulator.ClockTime % 86400 < 43200 && Viewer.tryLoadingDayTextures)
            {
                var sunHeight = Viewer.MaterialManager.sunDirection.Y;
                if (sunHeight > -0.10f && sunHeight < -0.01)
                {
                    var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.HUDWindow.GetWorkingSetSize();
                    if (remainingMemorySpace >= 0) // if not we'll try again
                    {
                        // Day is coming, it's time to load the day textures
                        var success = Viewer.MaterialManager.LoadDayTextures();
                        if (success)
                        {
                            Viewer.DayTexturesNotLoaded = false;
                        }
                    }
                    Viewer.tryLoadingDayTextures = false;
                }
                else if (sunHeight >= -0.01)
                    Viewer.DayTexturesNotLoaded = false; // too late to try, we must give up and we don't load the day textures. TODO: is this OK?
            }

            if (Viewer.Simulator.RefreshWire)
            {
                if (Viewer.Simulator.WireHeigth > 0)
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Wire heigth set to:") + " " + Viewer.Simulator.WireHeigth + " m");
                else
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Wire hidden"));
                Viewer.Simulator.WireHeightSwitch57 = false;
                Viewer.Simulator.WireHeightSwitch62 = false;
                Viewer.Simulator.WireHeightSwitchHidden = false;
                Viewer.Simulator.WireHeigthSet = false;
                Viewer.Simulator.RefreshWire = false;
            }

            if (Viewer.Simulator.RefreshWorld)
                Viewer.Simulator.Confirmer.Information(Simulator.Catalog.GetString("World Object reloaded!"));
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            var worldFiles = WorldFiles;
            foreach (var tile in worldFiles)
                tile.Mark();
        }

        [CallOnThread("Updater")]
        public float GetBoundingBoxTop(WorldLocation location, float blockSize)
        {
            return GetBoundingBoxTop(location.TileX, location.TileZ, location.Location.X, location.Location.Z, blockSize);
        }

        [CallOnThread("Updater")]
        public float GetBoundingBoxTop(int tileX, int tileZ, float x, float z, float blockSize)
        {
            // Normalize the coordinates to the right tile.
            while (x >= 1024) { x -= 2048; tileX++; }
            while (x < -1024) { x += 2048; tileX--; }
            while (z >= 1024) { z -= 2048; tileZ++; }
            while (z < -1024) { z += 2048; tileZ--; }

            // Fetch the tile we're looking up elevation for; if it isn't loaded, no elevation.
            var worldFiles = WorldFiles;
            var worldFile = worldFiles.FirstOrDefault(wf => wf.TileX == tileX && wf.TileZ == tileZ);
            if (worldFile == null)
                return float.MinValue;

            return worldFile.GetBoundingBoxTop(x, z, blockSize);
        }

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
            var worldFiles = WorldFiles;
            foreach (var worldFile in worldFiles)
                worldFile.Update(elapsedTime);
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            VisibleTileX = Viewer.Camera.TileX;
            VisibleTileZ = Viewer.Camera.TileZ;
        }

        [CallOnThread("Updater")]
        public void GetCameraTile(long cameraTile)
        {
            CameraTile = cameraTile;
        }
        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var worldFiles = WorldFiles;
            foreach (var worldFile in worldFiles)
                // TODO: This might impair some shadows.
                if (Viewer.Camera.InFov(new Vector3((worldFile.TileX - Viewer.Camera.TileX) * 2048, 0, (worldFile.TileZ - Viewer.Camera.TileZ) * 2048), 1448))
                    worldFile.PrepareFrame(frame, elapsedTime);
        }

        WorldFile LoadWorldFile(int tileX, int tileZ, bool visible)
        {
            Trace.Write("W");
            try
            {
                return new WorldFile(Viewer, tileX, tileZ, visible);
            }
            catch (FileLoadException error)
            {
                Trace.WriteLine(error);
                return null;
            }
        }
    }

    [CallOnThread("Loader")]
    public class WorldFile
    {
        const int MinimumInstanceCount = 5;

        // Dynamic track objects in the world file
        public struct DyntrackParams
        {
            public int isCurved;
            public float param1;
            public float param2;
        }

        public readonly int TileX, TileZ;
        public List<StaticShape> sceneryObjects = new List<StaticShape>();
        public List<DynamicTrackViewer> dTrackList = new List<DynamicTrackViewer>();
        public List<ForestViewer> forestList = new List<ForestViewer>();
        public List<RoadCarSpawner> carSpawners = new List<RoadCarSpawner>();
        public List<TrItemLabel> sidings = new List<TrItemLabel>();
        public List<TrItemLabel> platforms = new List<TrItemLabel>();
        public List<PickupObj> PickupList = new List<PickupObj>();
        public List<BoundingBox> BoundingBoxes = new List<BoundingBox>();

        readonly Viewer Viewer;        

        /// <summary>
        /// Open the specified WFile and load all the scenery objects into the viewer.
        /// If the file doesn't exist, then return an empty WorldFile object.
        /// </summary>
        /// <param name="visible">Tiles adjacent to the current visible tile may not be modelled.
        /// This flag decides whether a missing file leads to a warning message.</param>
        public WorldFile(Viewer viewer, int tileX, int tileZ, bool visible)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;

            var cancellation = Viewer.LoaderProcess.CancellationToken;

            // determine file path to the WFile at the specified tile coordinates
            var WFileName = WorldFileNameFromTileCoordinates(tileX, tileZ);
            var WFilePath = viewer.Simulator.RoutePath + @"\World\" + WFileName;

            // if there isn't a file, then return with an empty WorldFile object
            if (!File.Exists(WFilePath))
            {
                if (visible)
                    Trace.TraceWarning("World file missing - {0}", WFilePath);
                return;
            }

            // read the world file 
            var WFile = new Orts.Formats.Msts.WorldFile(WFilePath);

            // check for existence of world file in OpenRails subfolder

            WFilePath = viewer.Simulator.RoutePath + @"\World\Openrails\" + WFileName;
            if (File.Exists(WFilePath))
            {
                // We have an OR-specific addition to world file
                WFile.InsertORSpecificData(WFilePath);
            }



            // to avoid loop checking for every object this pre-check is performed
            bool containsMovingTable = false;
            if (Program.Simulator.MovingTables != null)
            {
                foreach (var movingTable in Program.Simulator.MovingTables)
                    if (movingTable.WFile == WFileName)
                    {
                        containsMovingTable = true;
                        break;
                    }
            }

            // create all the individual scenery objects specified in the WFile
            foreach (var worldObject in WFile.Tr_Worldfile)
            {
                if (worldObject.StaticDetailLevel > viewer.Settings.WorldObjectDensity)
                    continue;

                // If the loader has been asked to temrinate, bail out early.
                if (cancellation.IsCancellationRequested)
                    break;

                // Get the position of the scenery object into ORTS coordinate space.
                WorldPosition worldMatrix;
                if (worldObject.Matrix3x3 != null && worldObject.Position != null)
                    worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX, WFile.TileZ, worldObject.Position, worldObject.Matrix3x3);
                else if (worldObject.QDirection != null && worldObject.Position != null)
                    worldMatrix = WorldPositionFromMSTSLocation(WFile.TileX, WFile.TileZ, worldObject.Position, worldObject.QDirection);
                else
                {
                    Trace.TraceWarning("{0} scenery object {1} is missing Matrix3x3 and QDirection", WFileName, worldObject.UID);
                    continue;
                }

                var shadowCaster = (worldObject.StaticFlags & (uint)StaticFlag.AnyShadow) != 0 || viewer.Settings.ShadowAllShapes;
                var animated = (worldObject.StaticFlags & (uint)StaticFlag.Animate) != 0;
                var isAnalogORClock = ShapeIsORClock(worldObject.FileName) == "analog"; //check if worldObject is analog OR-Clock
                var global = (worldObject is TrackObj) || (worldObject is HazardObj) || (worldObject.StaticFlags & (uint)StaticFlag.Global) != 0;

                // TransferObj have a FileName but it is not a shape, so we need to avoid sanity-checking it as if it was.
                var fileNameIsNotShape = (worldObject is TransferObj || worldObject is HazardObj);

                // Determine the file path to the shape file for this scenery object and check it exists as expected.
                var shapeFilePath = fileNameIsNotShape || String.IsNullOrEmpty(worldObject.FileName) ? null : global ? viewer.Simulator.BasePath + @"\Global\Shapes\" + worldObject.FileName : viewer.Simulator.RoutePath + @"\Shapes\" + worldObject.FileName;
                if (shapeFilePath != null)
                {
                    shapeFilePath = Path.GetFullPath(shapeFilePath);
                    if (!File.Exists(shapeFilePath))
                    {
                        Trace.TraceWarning("{0} scenery object {1} with StaticFlags {3:X8} references non-existent {2}", WFileName, worldObject.UID, shapeFilePath, worldObject.StaticFlags);
                        shapeFilePath = null;
                    }
                }

                if (shapeFilePath != null && File.Exists(shapeFilePath + "d"))
                {
                    var shape = new ShapeDescriptorFile(shapeFilePath + "d");
                    if (shape.shape.ESD_Bounding_Box != null)
                    {
                        var min = shape.shape.ESD_Bounding_Box.Min;
                        var max = shape.shape.ESD_Bounding_Box.Max;
                        var transform = Matrix.Invert(worldMatrix.XNAMatrix);
                        // Not sure if this is needed, but it is to correct for center-of-gravity being not the center of the box.
                        //transform.M41 += (max.X + min.X) / 2;
                        //transform.M42 += (max.Y + min.Y) / 2;
                        //transform.M43 += (max.Z + min.Z) / 2;
                        BoundingBoxes.Add(new BoundingBox(transform, new Vector3((max.X - min.X) / 2, (max.Y - min.Y) / 2, (max.Z - min.Z) / 2), worldMatrix.XNAMatrix.Translation.Y));
                    }
                }

                try
                {
                    if (worldObject.GetType() == typeof(TrackObj))
                    {
                        var trackObj = (TrackObj)worldObject;
                        // Switch tracks need a link to the simulator engine so they can animate the points.
                        var trJunctionNode = trackObj.JNodePosn != null ? viewer.Simulator.TDB.GetTrJunctionNode(TileX, TileZ, (int)trackObj.UID) : null;
                        // We might not have found the junction node; if so, fall back to the static track shape.
                        if (trJunctionNode != null)
                        {
                            if (viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge) SuperElevationManager.DecomposeStaticSuperElevation(viewer, dTrackList, trackObj, worldMatrix, TileX, TileZ, shapeFilePath);
                            sceneryObjects.Add(new SwitchTrackShape(viewer, shapeFilePath, worldMatrix, trJunctionNode));
                        }
                        else
                        {
                            //if want to use super elevation, we will generate tracks using dynamic tracks
                            if ((viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge)
                                && SuperElevationManager.DecomposeStaticSuperElevation(viewer, dTrackList, trackObj, worldMatrix, TileX, TileZ, shapeFilePath))
                            {
                                //var success = SuperElevation.DecomposeStaticSuperElevation(viewer, dTrackList, trackObj, worldMatrix, TileX, TileZ, shapeFilePath);
                                //if (success == 0) sceneryObjects.Add(new StaticTrackShape(viewer, shapeFilePath, worldMatrix));
                            }
                            //otherwise, use shapes
                            else if (!containsMovingTable) sceneryObjects.Add(new StaticTrackShape(viewer, shapeFilePath, worldMatrix));
                            else
                            {
                                var found = false;
                                foreach (var movingTable in Program.Simulator.MovingTables)
                                {
                                    if (worldObject.UID == movingTable.UID && WFileName == movingTable.WFile)
                                    {
                                        found = true;
                                        if (movingTable is Simulation.Turntable)
                                        {
                                            var turntable = movingTable as Simulation.Turntable;
                                            turntable.ComputeCenter(worldMatrix);
                                            var startingY = Math.Asin(-2 * (worldObject.QDirection.A * worldObject.QDirection.C - worldObject.QDirection.B * worldObject.QDirection.D));
                                            sceneryObjects.Add(new TurntableShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None, turntable, startingY));
                                        }
                                        else
                                        {
                                            var transfertable = movingTable as Simulation.Transfertable;
                                            transfertable.ComputeCenter(worldMatrix);
                                            sceneryObjects.Add(new TransfertableShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None, transfertable));
                                        }
                                        break;
                                    }
                                }
                                if (!found) sceneryObjects.Add(new StaticTrackShape(viewer, shapeFilePath, worldMatrix));
                            }
                        }
                        if (viewer.Simulator.Settings.Wire == true && viewer.Simulator.TRK.Tr_RouteFile.Electrified == true
                            && worldObject.StaticDetailLevel != 2   // Make it compatible with routes that use 'HideWire', a workaround for MSTS that 
                            && worldObject.StaticDetailLevel != 3   // allowed a mix of electrified and non electrified track see http://msts.steam4me.net/tutorials/hidewire.html
                            )
                        {
                            int success = Wire.DecomposeStaticWire(viewer, dTrackList, trackObj, worldMatrix);
                            //if cannot draw wire, try to see if it is converted. modified for DynaTrax
                            if (success == 0 && trackObj.FileName.Contains("Dyna")) Wire.DecomposeConvertedDynamicWire(viewer, dTrackList, trackObj, worldMatrix);
                        }
                    }
                    else if (worldObject.GetType() == typeof(DyntrackObj))
                    {
                        if (viewer.Simulator.Settings.Wire == true && viewer.Simulator.TRK.Tr_RouteFile.Electrified == true
                            // Icik
                            && worldObject.StaticDetailLevel != 2   // Make it compatible with routes that use 'HideWire', a workaround for MSTS that 
                            && worldObject.StaticDetailLevel != 3   // allowed a mix of electrified and non electrified track see http://msts.steam4me.net/tutorials/hidewire.html
                            )
                            Wire.DecomposeDynamicWire(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);
                        // Add DyntrackDrawers for individual subsections
                        if ((viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge) && SuperElevationManager.UseSuperElevationDyn(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix))
                            SuperElevationManager.DecomposeDynamicSuperElevation(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);
                        else DynamicTrack.Decompose(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);

                    } // end else if DyntrackObj
                    else if (worldObject.GetType() == typeof(ForestObj))
                    {
                        if (!(worldObject as ForestObj).IsYard)
                            forestList.Add(new ForestViewer(viewer, (ForestObj)worldObject, worldMatrix));
                    }
                    else if (worldObject.GetType() == typeof(SignalObj))
                    {
                        sceneryObjects.Add(new SignalShape(viewer, (SignalObj)worldObject, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                    }
                    else if (worldObject.GetType() == typeof(TransferObj))
                    {
                        sceneryObjects.Add(new TransferShape(viewer, (TransferObj)worldObject, worldMatrix));
                    }
                    else if (worldObject.GetType() == typeof(LevelCrossingObj))
                    {
                        sceneryObjects.Add(new LevelCrossingShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None, (LevelCrossingObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(HazardObj))
                    {
                        var h = HazzardShape.CreateHazzard(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None, (HazardObj)worldObject);
                        if (h != null) sceneryObjects.Add(h);
                    }
                    else if (worldObject.GetType() == typeof(SpeedPostObj))
                    {
                        sceneryObjects.Add(new SpeedPostShape(viewer, shapeFilePath, worldMatrix, (SpeedPostObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(CarSpawnerObj))
                    {
                        if (Program.Simulator.CarSpawnerLists != null && ((CarSpawnerObj)worldObject).ListName != null)
                        {
                            ((CarSpawnerObj)worldObject).CarSpawnerListIdx = Program.Simulator.CarSpawnerLists.FindIndex(x => x.ListName == ((CarSpawnerObj)worldObject).ListName);
                            if (((CarSpawnerObj)worldObject).CarSpawnerListIdx < 0 || ((CarSpawnerObj)worldObject).CarSpawnerListIdx > Program.Simulator.CarSpawnerLists.Count - 1) ((CarSpawnerObj)worldObject).CarSpawnerListIdx = 0;
                        }
                        else ((CarSpawnerObj)worldObject).CarSpawnerListIdx = 0;
                        carSpawners.Add(new RoadCarSpawner(viewer, worldMatrix, (CarSpawnerObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(SidingObj))
                    {
                        sidings.Add(new TrItemLabel(viewer, worldMatrix, (SidingObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(PlatformObj))
                    {
                        platforms.Add(new TrItemLabel(viewer, worldMatrix, (PlatformObj)worldObject));
                    }
                    else if (worldObject.GetType() == typeof(StaticObj))
                    {
                        if (isAnalogORClock) //worldObject of type StaticObj is analog OR-Clock
                        {
                            sceneryObjects.Add(new AnalogClockShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                        }
                        else if (animated)
                            sceneryObjects.Add(new AnimatedShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                        else
                            sceneryObjects.Add(new StaticShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                    }
                    else if (worldObject.GetType() == typeof(PickupObj))
                    {
                        sceneryObjects.Add(new FuelPickupItemShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None, (PickupObj)worldObject));
                        PickupList.Add((PickupObj)worldObject);
                    }
                    else // It's some other type of object - not one of the above.
                    {
                        sceneryObjects.Add(new StaticShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                    }
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(String.Format("{0} scenery object {1} failed to load", worldMatrix, worldObject.UID), error));
                }
            }

            // Check if there are activity restricted speedposts to be loaded

            if (Viewer.Simulator.ActivityRun != null && Viewer.Simulator.Activity.Tr_Activity.Tr_Activity_File.ActivityRestrictedSpeedZones != null)
            {
                foreach (TempSpeedPostItem tempSpeedItem in Viewer.Simulator.ActivityRun.TempSpeedPostItems)
                {
                    if (tempSpeedItem.WorldPosition.TileX == TileX && tempSpeedItem.WorldPosition.TileZ == TileZ)
                    {
                        if (Viewer.SpeedpostDatFileCZSK == null && Viewer.SpeedpostDatFile == null)
                        {
                            Trace.TraceWarning(String.Format("{0} missing; speed posts for temporary speed restrictions in tile {1} {2} will not be visible.", Viewer.Simulator.RoutePath + @"\speedpost.dat", TileX, TileZ));
                            break;
                        }
                        else
                        {
                            // Icik
                            // Vybere správnou návěst pro PJ
                            int TempWarningSpeedShapeNamesNr = 0;
                            int TempSpeedShapeNamesNr = 1;

                            if (tempSpeedItem.RestrictedZoneLocation == null)
                                tempSpeedItem.RestrictedZoneLocation = "default";

                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "cz")
                            {
                                TempSpeedShapeNamesNr = 1;
                            }
                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "sk")
                            {
                                TempSpeedShapeNamesNr = 3;
                            }

                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "cz" || tempSpeedItem.RestrictedZoneLocation.ToLower() == "sk")
                            {
                                float ZoneSpeed = (int)(ORTS.Common.MpS.ToKpH(tempSpeedItem.RestrictedZoneSpeed) + 0.1f);
                                if (tempSpeedItem.IsWarning)
                                {
                                    // Nastaví správný směr označníku
                                    tempSpeedItem.WorldPosition.XNAMatrix.M11 *= -1;
                                    tempSpeedItem.WorldPosition.XNAMatrix.M13 *= -1;
                                    tempSpeedItem.WorldPosition.XNAMatrix.M31 *= -1;
                                    tempSpeedItem.WorldPosition.XNAMatrix.M33 *= -1;

                                    // CZ
                                    if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "cz")
                                    {
                                        switch (ZoneSpeed) // km/h
                                        {
                                            case 5:
                                            case 10:
                                            case 15:
                                                TempWarningSpeedShapeNamesNr = 0;
                                                break;
                                            case 20:
                                            case 25:
                                                TempWarningSpeedShapeNamesNr = 1;
                                                break;
                                            case 30:
                                            case 35:
                                                TempWarningSpeedShapeNamesNr = 2;
                                                break;
                                            case 40:
                                            case 45:
                                                TempWarningSpeedShapeNamesNr = 3;
                                                break;
                                            case 50:
                                                TempWarningSpeedShapeNamesNr = 4;
                                                break;
                                        }
                                    }
                                    // SK
                                    if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "sk")
                                    {
                                        switch (ZoneSpeed) // km/h
                                        {
                                            case 5:
                                            case 10:
                                            case 15:
                                                TempWarningSpeedShapeNamesNr = 5;
                                                break;
                                            case 20:
                                            case 25:
                                                TempWarningSpeedShapeNamesNr = 6;
                                                break;
                                            case 30:
                                            case 35:
                                                TempWarningSpeedShapeNamesNr = 7;
                                                break;
                                            case 40:
                                            case 45:
                                                TempWarningSpeedShapeNamesNr = 8;
                                                break;
                                            case 50:
                                                TempWarningSpeedShapeNamesNr = 9;
                                                break;
                                        }
                                    }
                                }
                            }

                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "cz" || tempSpeedItem.RestrictedZoneLocation.ToLower() == "sk")
                            {
                                sceneryObjects.Add(new StaticShape(viewer,
                                tempSpeedItem.IsWarning ? Viewer.SpeedpostDatFileCZSK.TempWarningSpeedShapeNamesCZSK[TempWarningSpeedShapeNamesNr] : (tempSpeedItem.IsResume ? Viewer.SpeedpostDatFileCZSK.TempSpeedShapeNamesCZSK[TempSpeedShapeNamesNr + 1] : Viewer.SpeedpostDatFileCZSK.TempSpeedShapeNamesCZSK[TempSpeedShapeNamesNr]),
                                tempSpeedItem.WorldPosition, ShapeFlags.None));
                            }

                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "default")
                            {
                                sceneryObjects.Add(new StaticShape(viewer,
                                tempSpeedItem.IsWarning ? Viewer.SpeedpostDatFile.TempSpeedShapeNames[0] : (tempSpeedItem.IsResume ? Viewer.SpeedpostDatFile.TempSpeedShapeNames[2] : Viewer.SpeedpostDatFile.TempSpeedShapeNames[1]),
                                tempSpeedItem.WorldPosition, ShapeFlags.None));
                            }
                        }
                    }
                }
            }

            if (Viewer.Settings.ModelInstancing)
            {
                // Instancing collapsed multiple copies of the same model in to a single set of data (the normal model
                // data, plus a list of position information for each copy) and then draws them in a single batch.
                var instances = new Dictionary<string, List<StaticShape>>();
                foreach (var shape in sceneryObjects)
                {
                    // Only allow StaticShape and StaticTrackShape instances for now.
                    if (shape.GetType() != typeof(StaticShape) && shape.GetType() != typeof(StaticTrackShape))
                        continue;

                    // Must have a file path so we can collapse instances on something.
                    var path = shape.SharedShape.FilePath;
                    if (path == null)
                        continue;

                    if (path != null && !instances.ContainsKey(path))
                        instances.Add(path, new List<StaticShape>());

                    if (path != null)
                        instances[path].Add(shape);
                }
                foreach (var path in instances.Keys)
                {
                    if (instances[path].Count >= MinimumInstanceCount)
                    {
                        var sharedInstance = new SharedStaticShapeInstance(Viewer, path, instances[path]);
                        foreach (var model in instances[path])
                            sceneryObjects.Remove(model);
                        sceneryObjects.Add(sharedInstance);
                    }
                }
            }

            if (viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge) SuperElevationManager.DecomposeStaticSuperElevation(Viewer, dTrackList, TileX, TileZ);
            
            // Icik
            if (viewer.Simulator.RefreshWorld || viewer.Simulator.RefreshWire)            
                Unload();
            
            if (Viewer.World.Sounds != null) Viewer.World.Sounds.AddByTile(TileX, TileZ);
        }

        //Method to check a shape name is listed in "openrails\clocks.dat"
        public string ShapeIsORClock(string shape)
        {
            if (Program.Simulator.ClockLists != null && shape != null) //OR-Clocks list given by "openrails\clocks.dat" and given shape are not null
            {
                for (var i = 0; i <= Program.Simulator.ClockLists[0].shapeNames.Count() - 1; i++)                                       //always the first (Default) list is used by now
                {
                    if (shape.ToLowerInvariant() == Path.GetFileName(Program.Simulator.ClockLists[0].shapeNames[i]).ToLowerInvariant()) //shape is an OR-Clock
                    {
                        string clockType = Program.Simulator.ClockLists[0].clockType[i].ToLowerInvariant();                             //Type of OR-Clock given by "openrails\clocks.dat"
                        if (clockType == "analog" || clockType == "digital")
                            return clockType; //Return OR-Clock-Type, analog or digital
                        else
                            return "unknown"; //Return OR-Clock-Type as unknown
                    }
                }
            }
            return "";                        //Return empty string -> shape is not an OR-Clock
        }

        [CallOnThread("Loader")]
        public void Unload()
        {
            foreach (var obj in sceneryObjects)
                obj.Unload();
            if (Viewer.World.Sounds != null) Viewer.World.Sounds.RemoveByTile(TileX, TileZ);
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            foreach (var shape in sceneryObjects)
                shape.Mark();
            foreach (var dTrack in dTrackList)
                dTrack.Mark();
            foreach (var forest in forestList)
                forest.Mark();
        }

        [CallOnThread("Updater")]
        public float GetBoundingBoxTop(float x, float z, float blockSize)
        {
            var location = new Vector3(x, float.MinValue, -z);
            foreach (var boundingBox in BoundingBoxes)
            {
                if (boundingBox.Size.X < blockSize / 2 || boundingBox.Size.Z < blockSize / 2)
                    continue;

                var boxLocation = Vector3.Transform(location, boundingBox.Transform);
                if (-boundingBox.Size.X <= boxLocation.X && boxLocation.X <= boundingBox.Size.X && -boundingBox.Size.Z <= boxLocation.Z && boxLocation.Z <= boundingBox.Size.Z)
                    location.Y = Math.Max(location.Y, boundingBox.Height + boundingBox.Size.Y);
            }
            return location.Y;
        }

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
            foreach (var spawner in carSpawners)
                spawner.Update(elapsedTime);
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            foreach (var shape in sceneryObjects)
                shape.PrepareFrame(frame, elapsedTime);
            foreach (var dTrack in dTrackList)
                dTrack.PrepareFrame(frame, elapsedTime);
            foreach (var forest in forestList)
                forest.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// MSTS WFiles represent some location with a position, quaternion and tile coordinates
        /// This converts it to the ORTS WorldPosition representation
        /// </summary>
        static WorldPosition WorldPositionFromMSTSLocation(int tileX, int tileZ, STFPositionItem MSTSPosition, STFQDirectionItem MSTSQuaternion)
        {
            var XNAQuaternion = new Quaternion((float)MSTSQuaternion.A, (float)MSTSQuaternion.B, -(float)MSTSQuaternion.C, (float)MSTSQuaternion.D);
            var XNAPosition = new Vector3((float)MSTSPosition.X, (float)MSTSPosition.Y, -(float)MSTSPosition.Z);
            var XNAMatrix = Matrix.CreateFromQuaternion(XNAQuaternion);
            XNAMatrix *= Matrix.CreateTranslation(XNAPosition);

            var worldMatrix = new WorldPosition();
            worldMatrix.TileX = tileX;
            worldMatrix.TileZ = tileZ;
            worldMatrix.XNAMatrix = XNAMatrix;

            return worldMatrix;
        }

        /// <summary>
        /// MSTS WFiles represent some location with a position, 3x3 matrix and tile coordinates
        /// This converts it to the ORTS WorldPosition representation
        /// </summary>
        static WorldPosition WorldPositionFromMSTSLocation(int tileX, int tileZ, STFPositionItem MSTSPosition, Matrix3x3 MSTSMatrix)
        {
            var XNAPosition = new Vector3((float)MSTSPosition.X, (float)MSTSPosition.Y, -(float)MSTSPosition.Z);
            var XNAMatrix = Matrix.Identity;
            XNAMatrix.M11 = MSTSMatrix.AX;
            XNAMatrix.M12 = MSTSMatrix.AY;
            XNAMatrix.M13 = -MSTSMatrix.AZ;
            XNAMatrix.M14 = 0;
            XNAMatrix.M21 = MSTSMatrix.BX;
            XNAMatrix.M22 = MSTSMatrix.BY;
            XNAMatrix.M23 = -MSTSMatrix.BZ;
            XNAMatrix.M24 = 0;
            XNAMatrix.M31 = -MSTSMatrix.CX;
            XNAMatrix.M32 = -MSTSMatrix.CY;
            XNAMatrix.M33 = MSTSMatrix.CZ;
            XNAMatrix.M34 = 0;
            XNAMatrix.M41 = 0;
            XNAMatrix.M42 = 0;
            XNAMatrix.M43 = 0;
            XNAMatrix.M44 = 1;
            XNAMatrix *= Matrix.CreateTranslation(XNAPosition);

            var worldMatrix = new WorldPosition();
            worldMatrix.TileX = tileX;
            worldMatrix.TileZ = tileZ;
            worldMatrix.XNAMatrix = XNAMatrix;

            return worldMatrix;
        }

        /// <summary>
        /// Build a w filename from tile X and Z coordinates.
        /// Returns a string eg "w-011283+014482.w"
        /// </summary>
        public static string WorldFileNameFromTileCoordinates(int tileX, int tileZ)
        {
            var filename = "w" + FormatTileCoordinate(tileX) + FormatTileCoordinate(tileZ) + ".w";
            return filename;
        }

        /// <summary>
        /// For building a filename from tile X and Z coordinates.
        /// Returns the string representation of a coordinate
        /// eg "+014482"
        /// </summary>
        static string FormatTileCoordinate(int tileCoord)
        {
            var sign = "+";
            if (tileCoord < 0)
            {
                sign = "-";
                tileCoord *= -1;
            }
            return sign + tileCoord.ToString("000000");
        }
    }

    public struct BoundingBox
    {
        public readonly Matrix Transform;
        public readonly Vector3 Size;
        public readonly float Height;

        internal BoundingBox(Matrix transform, Vector3 size, float height)
        {
            Transform = transform;
            Size = size;
            Height = height;
        }
    }
}
