// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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
using Orts.Formats.Msts;
using Orts.Simulation;
using ORTS.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Orts.Simulation.Simulation;
using Orts.Simulation.Simulation.Physics;
using static Orts.Simulation.Simulation.Physics.Train;

namespace Orts.Viewer3D
{
    public class SceneryDrawer
    {
        readonly Viewer Viewer;

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
        bool FirstRunIsDay = true;

        [CallOnThread("Loader")]
        public void Load()
        {
            if (Viewer.World.Sky.solarDirection.Y != 0)
            {
                if (Viewer.World.Sky.solarDirection.Y > 0 && (!Viewer.IsDay || FirstRunIsDay))
                {
                    Viewer.IsDay = true;
                    Viewer.Simulator.RefreshWorld = true;
                }
                if (Viewer.World.Sky.solarDirection.Y <= 0 && (Viewer.IsDay || FirstRunIsDay))
                {
                    Viewer.IsDay = false;
                    Viewer.Simulator.RefreshWorld = true;
                }
                FirstRunIsDay = false;
            }

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

                var tileLookup = new Dictionary<long, WorldFile>(worldFiles.Count);
                foreach (var wf in worldFiles)
                {
                    long key = ((long)wf.TileX << 32) | (uint)wf.TileZ;
                    tileLookup[key] = wf;
                }

                var newWorldFiles = new List<WorldFile>();
                var oldWorldFiles = new List<WorldFile>(worldFiles);
                var needed = (int)Math.Ceiling((float)Viewer.Settings.ViewingDistance / 2048f);

                for (var x = -needed; x <= needed; x++)
                {
                    for (var z = -needed; z <= needed; z++)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        int targetTileX = TileX + x;
                        int targetTileZ = TileZ + z;
                        long tileKey = ((long)targetTileX << 32) | (uint)targetTileZ;

                        tileLookup.TryGetValue(tileKey, out var tile);

                        var cameraTile = CameraTile;
                        CameraTileX = (int)(cameraTile / 100000);
                        CameraTileZ = (int)(Math.Abs(cameraTile) - (long)Math.Abs(CameraTileX) * 100000);
                        if ((CameraTileX != TileX || CameraTileZ != TileZ) && (Math.Abs(CameraTileX - targetTileX) > needed || Math.Abs(CameraTileZ - targetTileZ) > needed))
                            continue;

                        if (tile == null || Viewer.Simulator.RefreshWorld || Viewer.Simulator.RefreshWire)
                            tile = LoadWorldFile(targetTileX, targetTileZ, x == 0 && z == 0);

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
                Viewer.tryLoadingNightTextures = true;
                Viewer.tryLoadingDayTextures = true;
            }
            else if (Viewer.NightTexturesNotLoaded && Program.Simulator.ClockTime % 86400 >= 43200 && Viewer.tryLoadingNightTextures)
            {
                var sunHeight = Viewer.MaterialManager.sunDirection.Y;
                if (sunHeight < 0.10f && sunHeight > 0.01)
                {
                    var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.HUDWindow.GetWorkingSetSize();
                    if (remainingMemorySpace >= 0)
                    {
                        var success = Viewer.MaterialManager.LoadNightTextures();
                        if (success)
                        {
                            Viewer.NightTexturesNotLoaded = false;
                        }
                    }
                    Viewer.tryLoadingNightTextures = false;
                }
                else if (sunHeight <= 0.01)
                    Viewer.NightTexturesNotLoaded = false;
            }
            else if (Viewer.DayTexturesNotLoaded && Program.Simulator.ClockTime % 86400 < 43200 && Viewer.tryLoadingDayTextures)
            {
                var sunHeight = Viewer.MaterialManager.sunDirection.Y;
                if (sunHeight > -0.10f && sunHeight < -0.01)
                {
                    var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.HUDWindow.GetWorkingSetSize();
                    if (remainingMemorySpace >= 0)
                    {
                        var success = Viewer.MaterialManager.LoadDayTextures();
                        if (success)
                        {
                            Viewer.DayTexturesNotLoaded = false;
                        }
                    }
                    Viewer.tryLoadingDayTextures = false;
                }
                else if (sunHeight >= -0.01)
                    Viewer.DayTexturesNotLoaded = false;
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
            {
                Viewer.Simulator.RefreshWorld = false;
            }
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
            while (x >= 1024) { x -= 2048; tileX++; }
            while (x < -1024) { x += 2048; tileX--; }
            while (z >= 1024) { z += 2048; tileZ++; }
            while (z < -1024) { z += 2048; tileZ--; }

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

    public class PlatformPassengerShape : StaticShape
    {
        public bool Visible { get; set; } = false;
        public string StationName { get; set; } = string.Empty;
        public int TCSectionIndex { get; set; } = -1;

        public PlatformPassengerShape(Viewer viewer, string path, WorldPosition position)
            : base(viewer, path, position, ShapeFlags.None)
        {
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!Visible)
                return;

            base.PrepareFrame(frame, elapsedTime);
        }
    }

    public class PlatformPassengerData
    {
        public string ShapePath;
        public WorldPosition Position;
        public string StationName;
        public int TCSectionIndex;
    }

    public class SpawnedPlatformEntry
    {
        public List<PlatformPassengerData> Shapes = new List<PlatformPassengerData>();
        public int RemainingPassengers = -1;
    }

    [CallOnThread("Loader")]
    public class WorldFile
    {
        const int MinimumInstanceCount = 5;

        static readonly ConcurrentDictionary<string, string> ShapePathCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static readonly ConcurrentDictionary<string, ShapeDescriptorFile> SdFileCache = new ConcurrentDictionary<string, ShapeDescriptorFile>(StringComparer.OrdinalIgnoreCase);

        // Perzistentní mezipaměť vygenerovaných nástupišť včetně zbývajícího počtu panáčků
        public static readonly Dictionary<string, SpawnedPlatformEntry> SpawnedPlatformDatabase = new Dictionary<string, SpawnedPlatformEntry>(StringComparer.OrdinalIgnoreCase);

        // Evidence kompletně odbavených nástupišť, aby se panáčci po nástupu znovu neobjevili
        public static readonly HashSet<string> ClearedPlatformKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Časovače pozdržení po odbavení pro konkrétní perón (klíčem je TCSectionIndex)
        public readonly Dictionary<int, float> DepartHoldTimers = new Dictionary<int, float>();

        public readonly List<PlatformPassengerShape> PlatformPassengers = new List<PlatformPassengerShape>();

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

        bool VisibleCondition(int staticDetailLevel)
        {
            switch (staticDetailLevel)
            {
                case 11: return Program.Simulator.Season != SeasonType.Winter;
                case 12: return Program.Simulator.Season == SeasonType.Winter;
                case 13: if (Viewer.World.Sky.solarDirection.Y != 0) return Viewer.IsDay; else return false;
                case 14: if (Viewer.World.Sky.solarDirection.Y != 0) return !Viewer.IsDay; else return false;
            }
            return staticDetailLevel <= Viewer.Settings.WorldObjectDensity;
        }

        public WorldFile(Viewer viewer, int tileX, int tileZ, bool visible)
        {
            Viewer = viewer;
            TileX = tileX;
            TileZ = tileZ;

            var cancellation = Viewer.LoaderProcess.CancellationToken;

            var WFileName = WorldFileNameFromTileCoordinates(tileX, tileZ);
            var WFilePath = viewer.Simulator.RoutePath + @"\World\" + WFileName;

            var pendingPlatforms = new List<Tuple<PlatformObj, WorldPosition>>();

            if (!File.Exists(WFilePath))
            {
                if (visible)
                    Trace.TraceWarning("World file missing - {0}", WFilePath);
                return;
            }

            var WFile = new Orts.Formats.Msts.WorldFile(WFilePath);

            WFilePath = viewer.Simulator.RoutePath + @"\World\Openrails\" + WFileName;
            if (File.Exists(WFilePath))
            {
                WFile.InsertORSpecificData(WFilePath);
            }

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

            foreach (var worldObject in WFile.Tr_Worldfile)
            {
                if (!VisibleCondition(worldObject.StaticDetailLevel))
                    continue;

                if (cancellation.IsCancellationRequested)
                    break;

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
                var isAnalogORClock = ShapeIsORClock(worldObject.FileName) == "analog";
                var global = (worldObject is TrackObj) || (worldObject is HazardObj) || (worldObject.StaticFlags & (uint)StaticFlag.Global) != 0;
                var fileNameIsNotShape = (worldObject is TransferObj || worldObject is HazardObj);

                string shapeFilePath = null;
                if (!fileNameIsNotShape && !string.IsNullOrEmpty(worldObject.FileName))
                {
                    string rawKey = (global ? "G:" : "R:") + worldObject.FileName;
                    if (!ShapePathCache.TryGetValue(rawKey, out shapeFilePath))
                    {
                        var candidate = global ? viewer.Simulator.BasePath + @"\Global\Shapes\" + worldObject.FileName : viewer.Simulator.RoutePath + @"\Shapes\" + worldObject.FileName;
                        candidate = Path.GetFullPath(candidate);
                        shapeFilePath = File.Exists(candidate) ? candidate : string.Empty;
                        ShapePathCache.TryAdd(rawKey, shapeFilePath);
                    }
                    if (shapeFilePath.Length == 0)
                    {
                        Trace.TraceWarning("{0} scenery object {1} with StaticFlags {3:X8} references non-existent {2}", WFileName, worldObject.UID, worldObject.FileName, worldObject.StaticFlags);
                        shapeFilePath = null;
                    }
                }

                if (shapeFilePath != null)
                {
                    var sdPath = shapeFilePath + "d";
                    if (!SdFileCache.TryGetValue(sdPath, out var shapeDesc))
                    {
                        if (File.Exists(sdPath))
                        {
                            try { shapeDesc = new ShapeDescriptorFile(sdPath); }
                            catch { shapeDesc = null; }
                        }
                        SdFileCache.TryAdd(sdPath, shapeDesc);
                    }

                    if (shapeDesc?.shape.ESD_Bounding_Box != null)
                    {
                        var min = shapeDesc.shape.ESD_Bounding_Box.Min;
                        var max = shapeDesc.shape.ESD_Bounding_Box.Max;
                        var transform = Matrix.Invert(worldMatrix.XNAMatrix);
                        BoundingBoxes.Add(new BoundingBox(transform, new Vector3((max.X - min.X) / 2, (max.Y - min.Y) / 2, (max.Z - min.Z) / 2), worldMatrix.XNAMatrix.Translation.Y));
                    }
                }

                try
                {
                    if (worldObject.GetType() == typeof(TrackObj))
                    {
                        var trackObj = (TrackObj)worldObject;
                        var trJunctionNode = trackObj.JNodePosn != null ? viewer.Simulator.TDB.GetTrJunctionNode(TileX, TileZ, (int)trackObj.UID) : null;
                        if (trJunctionNode != null)
                        {
                            if (viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge) SuperElevationManager.DecomposeStaticSuperElevation(viewer, dTrackList, trackObj, worldMatrix, TileX, TileZ, shapeFilePath);
                            sceneryObjects.Add(new SwitchTrackShape(viewer, shapeFilePath, worldMatrix, trJunctionNode));
                        }
                        else
                        {
                            if ((viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge)
                                && SuperElevationManager.DecomposeStaticSuperElevation(viewer, dTrackList, trackObj, worldMatrix, TileX, TileZ, shapeFilePath))
                            {
                            }
                            else if (!containsMovingTable) sceneryObjects.Add(new StaticTrackShape(viewer, shapeFilePath, worldMatrix));
                            else
                            {
                                var found = false;
                                foreach (var movingTable in Program.Simulator.MovingTables)
                                {
                                    if (worldObject.UID == movingTable.UID && WFileName == movingTable.WFile)
                                    {
                                        found = true;
                                        if (movingTable is Turntable)
                                        {
                                            var turntable = movingTable as Turntable;
                                            turntable.ComputeCenter(worldMatrix);
                                            var startingY = Math.Asin(-2 * (worldObject.QDirection.A * worldObject.QDirection.C - worldObject.QDirection.B * worldObject.QDirection.D));
                                            sceneryObjects.Add(new TurntableShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None, turntable, startingY));
                                        }
                                        else
                                        {
                                            var transfertable = movingTable as Transfertable;
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
                            && worldObject.StaticDetailLevel != 2
                            && worldObject.StaticDetailLevel != 3)
                        {
                            int success = Wire.DecomposeStaticWire(viewer, dTrackList, trackObj, worldMatrix);
                            if (success == 0 && trackObj.FileName.Contains("Dyna")) Wire.DecomposeConvertedDynamicWire(viewer, dTrackList, trackObj, worldMatrix);
                        }
                    }
                    else if (worldObject.GetType() == typeof(DyntrackObj))
                    {
                        if (viewer.Simulator.Settings.Wire == true && viewer.Simulator.TRK.Tr_RouteFile.Electrified == true
                            && worldObject.StaticDetailLevel != 2
                            && worldObject.StaticDetailLevel != 3)
                            Wire.DecomposeDynamicWire(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);

                        if ((viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge) && SuperElevationManager.UseSuperElevationDyn(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix))
                            SuperElevationManager.DecomposeDynamicSuperElevation(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);
                        else DynamicTrack.Decompose(viewer, dTrackList, (DyntrackObj)worldObject, worldMatrix);
                    }
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
                        pendingPlatforms.Add(Tuple.Create((PlatformObj)worldObject, worldMatrix));
                    }
                    else if (worldObject.GetType() == typeof(StaticObj))
                    {
                        if (isAnalogORClock)
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
                    else
                    {
                        sceneryObjects.Add(new StaticShape(viewer, shapeFilePath, worldMatrix, shadowCaster ? ShapeFlags.ShadowCaster : ShapeFlags.None));
                    }
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(String.Format("{0} scenery object {1} failed to load", worldMatrix, worldObject.UID), error));
                }
            }

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
                            int TempWarningSpeedShapeNamesNr = 0;
                            int TempSpeedShapeNamesNr = 1;

                            if (tempSpeedItem.RestrictedZoneLocation == null)
                                tempSpeedItem.RestrictedZoneLocation = "default";

                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "cz")
                                TempSpeedShapeNamesNr = 1;
                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "sk")
                                TempSpeedShapeNamesNr = 3;

                            if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "cz" || tempSpeedItem.RestrictedZoneLocation.ToLower() == "sk")
                            {
                                float ZoneSpeed = (int)(ORTS.Common.MpS.ToKpH(tempSpeedItem.RestrictedZoneSpeed) + 0.1f);
                                if (tempSpeedItem.IsWarning)
                                {
                                    tempSpeedItem.WorldPosition.XNAMatrix.M11 *= -1;
                                    tempSpeedItem.WorldPosition.XNAMatrix.M13 *= -1;
                                    tempSpeedItem.WorldPosition.XNAMatrix.M31 *= -1;
                                    tempSpeedItem.WorldPosition.XNAMatrix.M33 *= -1;

                                    if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "cz")
                                    {
                                        switch (ZoneSpeed)
                                        {
                                            case 5: case 10: case 15: TempWarningSpeedShapeNamesNr = 0; break;
                                            case 20: case 25: TempWarningSpeedShapeNamesNr = 1; break;
                                            case 30: case 35: TempWarningSpeedShapeNamesNr = 2; break;
                                            case 40: case 45: TempWarningSpeedShapeNamesNr = 3; break;
                                            case 50: TempWarningSpeedShapeNamesNr = 4; break;
                                            case 60: TempWarningSpeedShapeNamesNr = 5; break;
                                            case 70: TempWarningSpeedShapeNamesNr = 6; break;
                                            case 80: TempWarningSpeedShapeNamesNr = 7; break;
                                            case 90: TempWarningSpeedShapeNamesNr = 8; break;
                                        }
                                    }
                                    if (tempSpeedItem.RestrictedZoneLocation.ToLower() == "sk")
                                    {
                                        switch (ZoneSpeed)
                                        {
                                            case 5: case 10: case 15: TempWarningSpeedShapeNamesNr = 9; break;
                                            case 20: case 25: TempWarningSpeedShapeNamesNr = 10; break;
                                            case 30: case 35: TempWarningSpeedShapeNamesNr = 11; break;
                                            case 40: case 45: TempWarningSpeedShapeNamesNr = 12; break;
                                            case 50: TempWarningSpeedShapeNamesNr = 13; break;
                                            case 60: TempWarningSpeedShapeNamesNr = 14; break;
                                            case 70: TempWarningSpeedShapeNamesNr = 15; break;
                                            case 80: TempWarningSpeedShapeNamesNr = 16; break;
                                            case 90: TempWarningSpeedShapeNamesNr = 17; break;
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
                var instances = new Dictionary<string, List<StaticShape>>(StringComparer.OrdinalIgnoreCase);
                foreach (var shape in sceneryObjects)
                {
                    if (shape is PlatformPassengerShape)
                        continue;

                    if (shape.GetType() != typeof(StaticShape) && shape.GetType() != typeof(StaticTrackShape))
                        continue;

                    var path = shape.SharedShape?.FilePath;
                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (!instances.TryGetValue(path, out var list))
                    {
                        list = new List<StaticShape>();
                        instances.Add(path, list);
                    }
                    list.Add(shape);
                }

                foreach (var pair in instances)
                {
                    if (pair.Value.Count >= MinimumInstanceCount)
                    {
                        var sharedInstance = new SharedStaticShapeInstance(Viewer, pair.Key, pair.Value);
                        foreach (var model in pair.Value)
                            sceneryObjects.Remove(model);
                        sceneryObjects.Add(sharedInstance);
                    }
                }
            }

            if (viewer.Simulator.UseSuperElevation > 0 || viewer.Simulator.TRK.Tr_RouteFile.ChangeTrackGauge) SuperElevationManager.DecomposeStaticSuperElevation(Viewer, dTrackList, TileX, TileZ);

            if (!Viewer.Simulator.RefreshWorld && !Viewer.Simulator.RefreshWire)
            {
                if (Viewer.World.Sounds != null) Viewer.World.Sounds.AddByTile(TileX, TileZ);
            }

            // Generování cestujících na nástupištích
            foreach (var p in pendingPlatforms)
            {
                SpawnPlatformPassengers(p.Item1, p.Item2);
            }
        }

        public string ShapeIsORClock(string shape)
        {
            if (Program.Simulator.ClockLists != null && shape != null)
            {
                for (var i = 0; i <= Program.Simulator.ClockLists[0].shapeNames.Count() - 1; i++)
                {
                    if (shape.ToLowerInvariant() == Path.GetFileName(Program.Simulator.ClockLists[0].shapeNames[i]).ToLowerInvariant())
                    {
                        string clockType = Program.Simulator.ClockLists[0].clockType[i].ToLowerInvariant();
                        if (clockType == "analog" || clockType == "digital")
                            return clockType;
                        else
                            return "unknown";
                    }
                }
            }
            return "";
        }

        [CallOnThread("Loader")]
        public float GetPlatformHeightFromScenery(float queryX, float queryZ, float trackY, bool usePositiveZ, float searchRadius = 12.0f)
        {
            float searchRadiusSq = searchRadius * searchRadius;
            Vector2 queryPoint = new Vector2(queryX, queryZ);

            Dictionary<float, int> heightCounts = new Dictionary<float, int>();

            foreach (var shape in sceneryObjects)
            {
                if (shape == null || shape.SharedShape == null)
                    continue;

                if (shape is StaticTrackShape || shape is PlatformPassengerShape || shape is SignalShape)
                    continue;

                Vector3 shapeTrans = shape.Location.XNAMatrix.Translation;
                Vector2 shapePoint = new Vector2(shapeTrans.X, usePositiveZ ? shapeTrans.Z : -shapeTrans.Z);

                float distSq = Vector2.DistanceSquared(queryPoint, shapePoint);

                if (distSq < searchRadiusSq)
                {
                    float modelMaxY = 0f;
                    if (shape.SharedShape.Matrices.Length > 0)
                    {
                        modelMaxY = shape.SharedShape.Matrices[0].Translation.Y;
                    }

                    float candidateSurfaceY = shapeTrans.Y + modelMaxY;
                    float heightAboveTrack = candidateSurfaceY - trackY;

                    if (heightAboveTrack >= 0.35f && heightAboveTrack <= 1.15f)
                    {
                        if (heightCounts.TryGetValue(candidateSurfaceY, out int count))
                            heightCounts[candidateSurfaceY] = count + 1;
                        else
                            heightCounts[candidateSurfaceY] = 1;
                    }
                }
            }

            if (heightCounts.Count == 0)
                return float.MinValue;

            float bestHeight = float.MinValue;
            int maxOccurrences = 0;

            foreach (var kvp in heightCounts)
            {
                if (kvp.Value > maxOccurrences)
                {
                    maxOccurrences = kvp.Value;
                    bestHeight = kvp.Key;
                }
            }

            return bestHeight;
        }

        Vector3 localVectorToPlatform;

        // Osazení cestujících na nástupištích pro jakýkoliv vlak (hráč i AI)
        void SpawnPlatformPassengers(PlatformObj platformObj, WorldPosition platformWorldPos)
        {
            if (Viewer.Simulator.PassengerList == null || Viewer.Simulator.PassengerList.Models.Count == 0)
                return;

            var trId1 = platformObj.getTrItemID(0);
            var trId2 = platformObj.getTrItemID(1);
            if (trId1 < 0) return;

            var trItemTable = Viewer.Simulator.TDB.TrackDB.TrItemTable;
            if (trId1 >= trItemTable.Length || !(trItemTable[trId1] is PlatformItem))
                return;

            var pItem1 = (PlatformItem)trItemTable[trId1];
            var pItem2 = (trId2 >= 0 && trId2 < trItemTable.Length) ? trItemTable[trId2] as PlatformItem : null;

            string platformStationName = !string.IsNullOrEmpty(pItem1.Station) ? pItem1.Station : pItem1.ItemName;
            if (string.IsNullOrEmpty(platformStationName))
                return;

            var startLoc = new WorldLocation(pItem1.TileX, pItem1.TileZ, pItem1.X, pItem1.Y, pItem1.Z);
            var trackNodes = Viewer.Simulator.TDB.TrackDB.TrackNodes;
            var tsection = Viewer.Simulator.TSectionDat;

            var traveller = new Traveller(tsection, trackNodes, startLoc.TileX, startLoc.TileZ, startLoc.Location.X, startLoc.Location.Z);
            var tcPos = new TCPosition();
            tcPos.SetTCPosition(traveller.TN.TCCrossReference, traveller.TrackNodeOffset, (int)traveller.Direction);
            int tcSectionIdx = tcPos.TCSectionIndex;

            string platformKey = $"{platformStationName}_{tcSectionIdx}";

            // Pokud již bylo nástupiště odbaveno (cestující nastoupili), znovu panáčky nevytváříme
            lock (ClearedPlatformKeys)
            {
                if (ClearedPlatformKeys.Contains(platformKey))
                    return;
            }

            // Získání všech vlaků (hráč i AI) v simulátoru
            var allTrains = new List<Train>();
            if (Viewer.Simulator.Trains != null)
                allTrains.AddRange(Viewer.Simulator.Trains);
            if (Viewer.Simulator.AI?.AITrains != null)
            {
                foreach (var ai in Viewer.Simulator.AI.AITrains)
                {
                    if (!allTrains.Contains(ai))
                        allTrains.Add(ai);
                }
            }

            Train targetTrain = null;
            StationStop currentStop = null;

            foreach (var t in allTrains)
            {
                if (t == null || t.StationStops == null || t.StationStops.Count == 0)
                    continue;

                if (t.PreviousStop?.PlatformItem != null)
                {
                    string prevName = t.PreviousStop.PlatformItem.Name;
                    if (!string.IsNullOrEmpty(prevName) && prevName.Equals(platformStationName, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var stop = t.StationStops[0];
                if (stop?.PlatformItem == null)
                    continue;

                string stopName = stop.PlatformItem.Name;
                if (!string.IsNullOrEmpty(stopName) &&
                    platformStationName.Equals(stopName, StringComparison.OrdinalIgnoreCase) &&
                    stop.TCSectionIndex == tcSectionIdx)
                {
                    targetTrain = t;
                    currentStop = stop;
                    break;
                }
            }

            if (targetTrain == null || currentStop == null)
                return;

            // KONTROLA MEZIPAMĚTI: Pokud již perón existuje, obnovíme přesně ty samé tvary a pozice
            lock (SpawnedPlatformDatabase)
            {
                if (SpawnedPlatformDatabase.TryGetValue(platformKey, out var existingEntry))
                {
                    int visibleCount;
                    // Respektujeme rozpracovaný nástup (pokud už vlak nabírá, nepřepisujeme ho plným počtem)
                    if (targetTrain.ActualPassengerCountAtStation >= 0 && targetTrain.ActualPassengerCountAtStation <= existingEntry.RemainingPassengers)
                    {
                        visibleCount = targetTrain.ActualPassengerCountAtStation;
                        existingEntry.RemainingPassengers = visibleCount;
                    }
                    else if (existingEntry.RemainingPassengers >= 0)
                    {
                        visibleCount = existingEntry.RemainingPassengers;
                        targetTrain.ActualPassengerCountAtStation = visibleCount;
                    }
                    else
                    {
                        visibleCount = targetTrain.ActualPassengerCountAtStation;
                    }

                    for (int k = 0; k < existingEntry.Shapes.Count; k++)
                    {
                        var data = existingEntry.Shapes[k];
                        var pShape = new PlatformPassengerShape(Viewer, data.ShapePath, data.Position)
                        {
                            StationName = data.StationName,
                            TCSectionIndex = data.TCSectionIndex,
                            Visible = (k < visibleCount)
                        };
                        PlatformPassengers.Add(pShape);
                    }
                    return;
                }
            }

            // Inicializace počtu cestujících POUZE pokud ještě nebyl nastaven (a není v mezipaměti)
            if (!targetTrain.IsActualPlayerTrain && targetTrain.ActualPassengerCountAtStation <= 0 && currentStop.PlatformItem != null)
            {
                targetTrain.ActualPassengerCountAtStation = currentStop.PlatformItem.NumPassengersWaiting;
            }

            int passengerCount = targetTrain.ActualPassengerCountAtStation;
            if (passengerCount <= 0)
                return;

            var endLoc = pItem2 != null
                ? new WorldLocation(pItem2.TileX, pItem2.TileZ, pItem2.X, pItem2.Y, pItem2.Z)
                : startLoc;

            float length = traveller.DistanceTo(endLoc.TileX, endLoc.TileZ, endLoc.Location.X, endLoc.Location.Y, endLoc.Location.Z);
            if (length <= 0)
            {
                traveller.ReverseDirection();
                length = traveller.DistanceTo(endLoc.TileX, endLoc.TileZ, endLoc.Location.X, endLoc.Location.Y, endLoc.Location.Z);
            }
            if (length <= 1.0f)
                length = 40.0f;

            float midPoint = length * 0.5f;
            float spawnStart = midPoint * (1.0f / 10.0f);
            float spawnEnd = midPoint + (midPoint * (9.0f / 10.0f));
            float availableLength = Math.Max(1.0f, spawnEnd - spawnStart);
            float baseLateralOffset = 2.5f;
            var rand = Simulator.Random;
            var placedPoints = new List<Vector2>(passengerCount);
            const float minDistance = 0.85f;
            const float minDistanceSq = minDistance * minDistance;
            const float defaultPlatformOffset = 0.750f;
            float lastKnownPlatformHeightOffset = defaultPlatformOffset;

            #region Výpočet výšky nástupiště
            float topY0 = 0;
            for (int i = 0; i < passengerCount; i++)
            {
                Vector3 candidatePos = Vector3.Zero;
                Traveller selectedTraveller = null;
                bool validPosFound = false;

                float progress = passengerCount > 1 ? (float)i / (passengerCount - 1) : 0f;
                float baseRangeFraction = MathHelper.Clamp(0.25f + progress * 0.75f, 0.25f, 1.0f);

                for (int attempt = 0; attempt < 25; attempt++)
                {
                    float attemptExpansion = (float)attempt / 24f;
                    float currentAllowedLength = availableLength * Math.Min(1.0f, baseRangeFraction + attemptExpansion * (1.0f - baseRangeFraction));

                    double r = rand.NextDouble();
                    float distFromStart = (float)(r * r) * currentAllowedLength;
                    float distAlong = spawnStart + distFromStart;

                    var pTrav = new Traveller(traveller);
                    pTrav.Move(distAlong);

                    float rotY = pTrav.RotY;
                    Vector3 trackTangent = new Vector3((float)Math.Sin(rotY), 0, (float)Math.Cos(rotY));
                    Vector3 localTrackRight = new Vector3(trackTangent.Z, 0, -trackTangent.X);
                    localVectorToPlatform = currentStop.PlatformItem.PlatformSide[0] ? localTrackRight : -localTrackRight;

                    float lateralDist = baseLateralOffset + (float)rand.NextDouble() * 0.7f;
                    float mstsQueryX = pTrav.X + localVectorToPlatform.X * lateralDist;
                    float mstsQueryZ = pTrav.Z + localVectorToPlatform.Z * lateralDist;

                    float topY = GetBoundingBoxTop(mstsQueryX, mstsQueryZ, 1.5f);
                    float topY1 = float.MinValue;
                    float topY2 = float.MinValue;

                    if (topY <= float.MinValue + 1000f)
                    {
                        topY1 = GetPlatformHeightFromScenery(mstsQueryX, mstsQueryZ, pTrav.Y, false, 15.0f);
                    }
                    if (topY <= float.MinValue + 1000f)
                    {
                        topY2 = GetPlatformHeightFromScenery(mstsQueryX, mstsQueryZ, pTrav.Y, true, 15.0f);
                    }

                    topY0 = Math.Max(topY0, Math.Max(topY1, topY2));
                    topY = topY0;

                    float finalPlatformY;
                    if (topY > float.MinValue + 1000f)
                    {
                        float measuredOffset = topY - pTrav.Y;
                        if (measuredOffset >= 0.35f && measuredOffset <= 1.15f)
                        {
                            finalPlatformY = topY;
                            lastKnownPlatformHeightOffset = measuredOffset;
                        }
                        else
                        {
                            finalPlatformY = pTrav.Y + lastKnownPlatformHeightOffset;
                        }
                    }
                    else
                    {
                        finalPlatformY = pTrav.Y + lastKnownPlatformHeightOffset;
                    }

                    Vector3 testPos = new Vector3(
                        mstsQueryX,
                        finalPlatformY,
                        -mstsQueryZ
                    );

                    Vector2 testPoint = new Vector2(testPos.X, testPos.Z);
                    bool overlaps = false;
                    for (int p = 0; p < placedPoints.Count; p++)
                    {
                        if (Vector2.DistanceSquared(placedPoints[p], testPoint) < minDistanceSq)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        candidatePos = testPos;
                        selectedTraveller = pTrav;
                        placedPoints.Add(testPoint);
                        validPosFound = true;
                        break;
                    }
                }

                if (!validPosFound || selectedTraveller == null)
                    continue;
            }
            #endregion

            var newPlatformDataList = new List<PlatformPassengerData>(passengerCount);
            placedPoints = new List<Vector2>(passengerCount);

            for (int i = 0; i < passengerCount; i++)
            {
                string shapeName = Viewer.Simulator.PassengerList.GetRandomShape(rand);
                if (string.IsNullOrEmpty(shapeName))
                    continue;

                string shapePath = Viewer.Simulator.RoutePath + @"\Shapes\OpenRailsCZSK\" + shapeName;
                if (!File.Exists(shapePath))
                    shapePath = Viewer.Simulator.BasePath + @"\Global\Shapes\OpenRailsCZSK\" + shapeName;

                if (!File.Exists(shapePath))
                    continue;

                Vector3 candidatePos = Vector3.Zero;
                Traveller selectedTraveller = null;
                bool validPosFound = false;

                float progress = passengerCount > 1 ? (float)i / (passengerCount - 1) : 0f;
                float baseRangeFraction = MathHelper.Clamp(0.25f + progress * 0.75f, 0.25f, 1.0f);

                for (int attempt = 0; attempt < 25; attempt++)
                {
                    float attemptExpansion = (float)attempt / 24f;
                    float currentAllowedLength = availableLength * Math.Min(1.0f, baseRangeFraction + attemptExpansion * (1.0f - baseRangeFraction));

                    double r = rand.NextDouble();
                    float distFromStart = (float)(r * r) * currentAllowedLength;
                    float distAlong = spawnStart + distFromStart;

                    var pTrav = new Traveller(traveller);
                    pTrav.Move(distAlong);

                    float rotY = pTrav.RotY;
                    Vector3 trackTangent = new Vector3((float)Math.Sin(rotY), 0, (float)Math.Cos(rotY));
                    Vector3 localTrackRight = new Vector3(trackTangent.Z, 0, -trackTangent.X);
                    localVectorToPlatform = currentStop.PlatformItem.PlatformSide[0] ? localTrackRight : -localTrackRight;

                    float lateralDist = baseLateralOffset + (float)rand.NextDouble() * 0.7f;
                    float mstsQueryX = pTrav.X + localVectorToPlatform.X * lateralDist;
                    float mstsQueryZ = pTrav.Z + localVectorToPlatform.Z * lateralDist;

                    var finalPlatformY = pTrav.Y + lastKnownPlatformHeightOffset;

                    Vector3 testPos = new Vector3(
                        mstsQueryX,
                        finalPlatformY,
                        -mstsQueryZ
                    );

                    Vector2 testPoint = new Vector2(testPos.X, testPos.Z);
                    bool overlaps = false;
                    for (int p = 0; p < placedPoints.Count; p++)
                    {
                        if (Vector2.DistanceSquared(placedPoints[p], testPoint) < minDistanceSq)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        candidatePos = testPos;
                        selectedTraveller = pTrav;
                        placedPoints.Add(testPoint);
                        validPosFound = true;
                        break;
                    }
                }

                if (!validPosFound || selectedTraveller == null)
                    continue;

                Vector3 lookToTrain = localVectorToPlatform;
                float baseAngle = (float)Math.Atan2(lookToTrain.X, -lookToTrain.Z);

                float passengerYaw;
                if (rand.NextDouble() < 0.7f)
                {
                    float variation = ((float)rand.NextDouble() - 0.5f) * 0.5f;
                    passengerYaw = baseAngle + variation;
                }
                else
                {
                    passengerYaw = (float)(rand.NextDouble() * Math.PI * (2 - rand.NextDouble()));
                }

                var rot = Matrix.CreateRotationY(passengerYaw);
                rot.M41 = candidatePos.X;
                rot.M42 = candidatePos.Y;
                rot.M43 = candidatePos.Z;

                var worldPos = new WorldPosition
                {
                    TileX = selectedTraveller.TileX,
                    TileZ = selectedTraveller.TileZ,
                    XNAMatrix = rot
                };

                newPlatformDataList.Add(new PlatformPassengerData
                {
                    ShapePath = shapePath,
                    Position = worldPos,
                    StationName = platformStationName,
                    TCSectionIndex = tcSectionIdx
                });

                var passengerShape = new PlatformPassengerShape(Viewer, shapePath, worldPos)
                {
                    StationName = platformStationName,
                    TCSectionIndex = tcSectionIdx,
                    Visible = (i < targetTrain.ActualPassengerCountAtStation)
                };
                PlatformPassengers.Add(passengerShape);
            }

            // Uložení vygenerované podoby perónu do trvalé mezipaměti
            lock (SpawnedPlatformDatabase)
            {
                SpawnedPlatformDatabase[platformKey] = new SpawnedPlatformEntry
                {
                    Shapes = newPlatformDataList,
                    RemainingPassengers = passengerCount
                };
            }
        }

        [CallOnThread("Loader")]
        public void Unload()
        {
            foreach (var pax in PlatformPassengers)
            {
                pax.Visible = false;
                pax.Unload();
            }
            PlatformPassengers.Clear();

            foreach (var obj in sceneryObjects)
                obj.Unload();

            if (!Viewer.Simulator.RefreshWorld && !Viewer.Simulator.RefreshWire)
            {
                if (Viewer.World.Sounds != null) Viewer.World.Sounds.RemoveByTile(TileX, TileZ);
            }
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
            foreach (var pax in PlatformPassengers)
                pax.Mark();
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

            if (PlatformPassengers.Count > 0)
            {
                var allTrains = new List<Train>();
                if (Viewer.Simulator.Trains != null)
                    allTrains.AddRange(Viewer.Simulator.Trains);
                if (Viewer.Simulator.AI?.AITrains != null)
                {
                    foreach (var ai in Viewer.Simulator.AI.AITrains)
                    {
                        if (!allTrains.Contains(ai))
                            allTrains.Add(ai);
                    }
                }

                var groups = PlatformPassengers.GroupBy(p => new { p.StationName, p.TCSectionIndex });

                foreach (var group in groups)
                {
                    string pKey = $"{group.Key.StationName}_{group.Key.TCSectionIndex}";

                    // Pokud už bylo nástupiště odbaveno, cestující trvale zůstávají skrytí
                    bool isCleared;
                    lock (ClearedPlatformKeys)
                    {
                        isCleared = ClearedPlatformKeys.Contains(pKey);
                    }

                    if (isCleared)
                    {
                        foreach (var pax in group)
                            pax.Visible = false;
                        continue;
                    }

                    Train relevantTrain = null;

                    foreach (var t in allTrains)
                    {
                        if (t == null || t.StationStops == null || t.StationStops.Count == 0)
                            continue;

                        if (t.PreviousStop?.PlatformItem != null)
                        {
                            string prevName = t.PreviousStop.PlatformItem.Name;
                            if (!string.IsNullOrEmpty(prevName) && prevName.Equals(group.Key.StationName, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        var stop = t.StationStops[0];
                        if (stop?.PlatformItem == null)
                            continue;

                        string stopName = stop.PlatformItem.Name;
                        if (!string.IsNullOrEmpty(stopName) &&
                            group.Key.StationName.Equals(stopName, StringComparison.OrdinalIgnoreCase) &&
                            stop.TCSectionIndex == group.Key.TCSectionIndex)
                        {
                            relevantTrain = t;
                            break;
                        }
                    }

                    int targetCount = 0;
                    if (relevantTrain != null)
                    {
                        // Pokud vlak ještě nestaví / nezačal odpočet nástupu
                        if (relevantTrain.ActualPassengerCountAtStation <= 0)
                        {
                            var stop = relevantTrain.StationStops.FirstOrDefault(s =>
                                s?.PlatformItem != null &&
                                group.Key.StationName.Equals(s.PlatformItem.Name, StringComparison.OrdinalIgnoreCase) &&
                                s.TCSectionIndex == group.Key.TCSectionIndex);

                            if (stop?.PlatformItem != null)
                                targetCount = stop.PlatformItem.NumPassengersWaiting;
                        }
                        else
                        {
                            targetCount = relevantTrain.ActualPassengerCountAtStation;
                        }

                        // Průběžná aktualizace stavu v mezipaměti, aby RefreshWorld nevrátil již nastoupené cestující
                        lock (SpawnedPlatformDatabase)
                        {
                            if (SpawnedPlatformDatabase.TryGetValue(pKey, out var entry))
                            {
                                entry.RemainingPassengers = targetCount;
                            }
                        }

                        // V momentě, kdy všichni nastoupili (počet klesl na nulu)
                        if (targetCount == 0)
                        {
                            lock (SpawnedPlatformDatabase)
                            {
                                SpawnedPlatformDatabase.Remove(pKey);
                            }
                            lock (ClearedPlatformKeys)
                            {
                                ClearedPlatformKeys.Add(pKey);
                            }
                        }
                    }
                    else
                    {
                        // Vlak už stanici odbavil a odstranil ji ze StationStops dříve, než odjel
                        targetCount = 0;
                        lock (SpawnedPlatformDatabase)
                        {
                            SpawnedPlatformDatabase.Remove(pKey);
                        }
                        lock (ClearedPlatformKeys)
                        {
                            ClearedPlatformKeys.Add(pKey);
                        }
                    }

                    int idx = 0;
                    foreach (var pax in group)
                    {
                        pax.Visible = (idx < targetCount);
                        idx++;
                    }
                }
            }
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

            // Seznam všech vlaků
            var allTrains = new List<Train>();
            if (Viewer.Simulator.Trains != null)
                allTrains.AddRange(Viewer.Simulator.Trains);
            if (Viewer.Simulator.AI?.AITrains != null)
            {
                foreach (var ai in Viewer.Simulator.AI.AITrains)
                {
                    if (!allTrains.Contains(ai))
                        allTrains.Add(ai);
                }
            }

            // Zjistíme unikátní sekce perónů na této dlaždici
            for (int i = 0; i < PlatformPassengers.Count; i++)
            {
                var pax = PlatformPassengers[i];
                int tcIdx = pax.TCSectionIndex;

                if (!DepartHoldTimers.ContainsKey(tcIdx))
                    DepartHoldTimers[tcIdx] = 0.0f;

                // Hledáme vlak stojící POUZE na tomto konkrétním perónu (striktní kontrola TCSectionIndex)
                Train trainOnPlatform = null;
                foreach (var t in allTrains)
                {
                    if (t == null) continue;

                    // Kontrola aktuální zastávky podle TCSectionIndex
                    if (t.StationStops != null && t.StationStops.Count > 0 && t.StationStops[0]?.PlatformItem != null)
                    {
                        if (t.StationStops[0].TCSectionIndex == tcIdx)
                        {
                            trainOnPlatform = t;
                            break;
                        }
                    }
                    // Kontrola minulé zastávky podle TCSectionIndex
                    else if (t.PreviousStop?.PlatformItem != null && t.PreviousStop.TCSectionIndex == tcIdx)
                    {
                        trainOnPlatform = t;
                        break;
                    }
                }

                // Pokud vlak na tomto perónu dokončil nástup (0 cestujících)
                if (trainOnPlatform != null && trainOnPlatform.ActualPassengerCountAtStation == 0)
                {
                    // Pokud vlak ještě neodjel ze stanice (stojí nebo se teprve rozjíždí pod 1.0 m/s)
                    if (trainOnPlatform.SpeedMpS < 1.0f)
                    {
                        DepartHoldTimers[tcIdx] = 6.0f; // Aktivujeme pozdržení na 6 sekund po rozjezdu
                    }
                }

                // Odpočet časovače pozdržení pro daný perón
                if (DepartHoldTimers[tcIdx] > 0.0f)
                {
                    if (trainOnPlatform == null)
                    {
                        DepartHoldTimers[tcIdx] -= elapsedTime.RealSeconds;
                        if (DepartHoldTimers[tcIdx] < 0.0f)
                        {
                            DepartHoldTimers[tcIdx] = 0.0f;
                        }
                    }

                    // Vykreslení tohoto panáčka se vynechá (je na odbaveném perónu)
                    continue;
                }

                pax.PrepareFrame(frame, elapsedTime);
            }
        }

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

        public static string WorldFileNameFromTileCoordinates(int tileX, int tileZ)
        {
            return "w" + FormatTileCoordinate(tileX) + FormatTileCoordinate(tileZ) + ".w";
        }

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