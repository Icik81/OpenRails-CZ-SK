// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Simulation;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using ORTS.Common.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game = Orts.Viewer3D.Processes.Game;

namespace Orts.Viewer3D
{
    public enum RenderPrimitiveSequence
    {
        CabOpaque,
        Sky,
        WorldOpaque,
        WorldBlended,
        Lights,
        Precipitation,
        Particles,
        InteriorOpaque,
        InteriorBlended,
        Labels,
        CabBlended,
        OverlayOpaque,
        OverlayBlended,
        Sentinel
    }

    public enum RenderPrimitiveGroup
    {
        Cab,
        Sky,
        World,
        Lights,
        Precipitation,
        Particles,
        Interior,
        Labels,
        Overlay
    }

    public abstract class RenderPrimitive
    {
        public static readonly RenderPrimitiveSequence[] SequenceForBlended = new[] {
            RenderPrimitiveSequence.CabBlended,
            RenderPrimitiveSequence.Sky,
            RenderPrimitiveSequence.WorldBlended,
            RenderPrimitiveSequence.Lights,
            RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
            RenderPrimitiveSequence.InteriorBlended,
            RenderPrimitiveSequence.Labels,
            RenderPrimitiveSequence.OverlayBlended,
        };

        public static readonly RenderPrimitiveSequence[] SequenceForOpaque = new[] {
            RenderPrimitiveSequence.CabOpaque,
            RenderPrimitiveSequence.Sky,
            RenderPrimitiveSequence.WorldOpaque,
            RenderPrimitiveSequence.Lights,
            RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
            RenderPrimitiveSequence.InteriorOpaque,
            RenderPrimitiveSequence.Labels,
            RenderPrimitiveSequence.OverlayOpaque,
        };

        public float ZBias;
        public float SortIndex;

        public abstract void Draw(GraphicsDevice graphicsDevice);

        static VertexBuffer DummyVertexBuffer;
        static internal VertexBuffer GetDummyVertexBuffer(GraphicsDevice graphicsDevice)
        {
            if (DummyVertexBuffer == null)
            {
                var vertexBuffer = new VertexBuffer(graphicsDevice, new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements), 1, BufferUsage.WriteOnly);
                vertexBuffer.SetData(new Matrix[] { Matrix.Identity });
                DummyVertexBuffer = vertexBuffer;
            }
            return DummyVertexBuffer;
        }
    }

    [DebuggerDisplay("{Material} {RenderPrimitive} {Flags}")]
    public struct RenderItem
    {
        public Material Material;
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
        public ShapeFlags Flags;
        public object ItemData;

        public RenderItem(Material material, RenderPrimitive renderPrimitive, ref Matrix xnaMatrix, ShapeFlags flags, object itemData = null)
        {
            Material = material;
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
            Flags = flags;
            ItemData = itemData;
        }

        public class Comparer : IComparer<RenderItem>
        {
            readonly Vector3 XNAViewerPos;

            public Comparer(Vector3 viewerPos)
            {
                XNAViewerPos = viewerPos;
                XNAViewerPos.Z *= -1;
            }

            public int Compare(RenderItem x, RenderItem y)
            {
                var xd = (x.XNAMatrix.Translation - XNAViewerPos).LengthSquared();
                var yd = (y.XNAMatrix.Translation - XNAViewerPos).LengthSquared();

                if (x.Material is WaterMaterial && y.Material is WaterMaterial && Math.Abs(yd - xd) < 1.0f && x.XNAMatrix.Translation.Y < XNAViewerPos.Y)
                {
                    return Math.Sign(x.XNAMatrix.Translation.Y - y.XNAMatrix.Translation.Y);
                }

                if (Math.Abs(yd - xd) >= 0.000001f) // Čtverec 1mm tolerance
                    return Math.Sign(yd - xd);

                return Math.Sign(x.RenderPrimitive.SortIndex - y.RenderPrimitive.SortIndex);
            }
        }
    }

    public class RenderItemCollection : IList<RenderItem>, IEnumerator<RenderItem>
    {
        RenderItem[] Items = new RenderItem[32]; // Zvětšená počáteční kapacita
        int ItemCount;
        int EnumeratorIndex;

        public RenderItemCollection() { }

        public int Capacity => Items.Length;
        public int Count => ItemCount;

        public void Sort(IComparer<RenderItem> comparer)
        {
            Array.Sort(Items, 0, ItemCount, comparer);
        }

        public void Add(RenderItem item)
        {
            if (ItemCount == Items.Length)
            {
                var items = new RenderItem[Items.Length * 2];
                Array.Copy(Items, 0, items, 0, Items.Length);
                Items = items;
            }
            Items[ItemCount++] = item;
        }

        public void Clear()
        {
            Array.Clear(Items, 0, ItemCount);
            ItemCount = 0;
        }

        #region IList & Collection Members
        public int IndexOf(RenderItem item) => throw new NotSupportedException();
        public void Insert(int index, RenderItem item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
        public RenderItem this[int index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public bool Contains(RenderItem item) => throw new NotSupportedException();
        public void CopyTo(RenderItem[] array, int arrayIndex) => throw new NotSupportedException();
        int ICollection<RenderItem>.Count => ItemCount;
        public bool IsReadOnly => false;
        public bool Remove(RenderItem item) => throw new NotSupportedException();
        #endregion

        #region IEnumerator Members
        public IEnumerator<RenderItem> GetEnumerator()
        {
            Reset();
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public RenderItem Current => Items[EnumeratorIndex];
        object System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            EnumeratorIndex++;
            return EnumeratorIndex < ItemCount;
        }

        public void Reset() => EnumeratorIndex = -1;
        public void Dispose() { }
        #endregion
    }

    public class RenderFrame
    {
        readonly Game Game;

        static RenderTarget2D[] ShadowMap;
        static RenderTarget2D[] ShadowMapRenderTarget;
        static Vector3 SteppedSolarDirection = Vector3.UnitX;

        Matrix[] ShadowMapLightView;
        Matrix[] ShadowMapLightProj;
        Matrix[] ShadowMapLightViewProjShadowProj;
        Vector3 ShadowMapX;
        Vector3 ShadowMapY;
        Vector3[] ShadowMapCenter;

        readonly Material DummyBlendedMaterial;
        readonly Dictionary<Material, RenderItemCollection>[] RenderItems = new Dictionary<Material, RenderItemCollection>[(int)RenderPrimitiveSequence.Sentinel];
        readonly RenderItemCollection[] RenderShadowSceneryItems;
        readonly RenderItemCollection[] RenderShadowForestItems;
        readonly RenderItemCollection[] RenderShadowTerrainItems;
        readonly RenderItemCollection RenderItemsSequence = new RenderItemCollection();

        public bool IsScreenChanged { get; internal set; }
        ShadowMapMaterial ShadowMapMaterial;
        SceneryShader SceneryShader;
        Vector3 SolarDirection;
        Camera Camera;
        Vector3 CameraLocation;
        Vector3 XNACameraLocation;
        Matrix XNACameraView;
        Matrix XNACameraProjection;

        public RenderFrame(Game game)
        {
            Game = game;
            DummyBlendedMaterial = new EmptyMaterial(null);

            for (int i = 0; i < RenderItems.Length; i++)
                RenderItems[i] = new Dictionary<Material, RenderItemCollection>();

            if (Game.Settings.DynamicShadows)
            {
                if (ShadowMap == null)
                {
                    var shadowMapSize = Game.Settings.ShadowMapResolution;
                    ShadowMap = new RenderTarget2D[RenderProcess.ShadowMapCount];
                    ShadowMapRenderTarget = new RenderTarget2D[RenderProcess.ShadowMapCount];
                    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                    {
                        ShadowMapRenderTarget[shadowMapIndex] = new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
                        ShadowMap[shadowMapIndex] = new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
                    }
                }

                ShadowMapLightView = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapLightViewProjShadowProj = new Matrix[RenderProcess.ShadowMapCount];
                ShadowMapCenter = new Vector3[RenderProcess.ShadowMapCount];

                RenderShadowSceneryItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowForestItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                RenderShadowTerrainItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    RenderShadowSceneryItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowForestItems[shadowMapIndex] = new RenderItemCollection();
                    RenderShadowTerrainItems[shadowMapIndex] = new RenderItemCollection();
                }
            }

            XNACameraView = Matrix.Identity;
            XNACameraProjection = Matrix.CreateOrthographic(game.RenderProcess.DisplaySize.X, game.RenderProcess.DisplaySize.Y, 1, 100);
        }

        public void Clear()
        {
            // Optimalizace: Rychlé nulování kolekcí bez alokací enumerátorů LINQ
            for (var i = 0; i < RenderItems.Length; i++)
            {
                foreach (var collection in RenderItems[i].Values)
                {
                    collection.Clear();
                }
            }

            if (Game.Settings.DynamicShadows)
            {
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    RenderShadowSceneryItems[shadowMapIndex].Clear();
                    RenderShadowForestItems[shadowMapIndex].Clear();
                    RenderShadowTerrainItems[shadowMapIndex].Clear();
                }
            }
        }

        public void PrepareFrame(Viewer viewer)
        {
            if (viewer.Settings.UseMSTSEnv == false)
                SolarDirection = viewer.World.Sky.solarDirection;
            else
                SolarDirection = viewer.World.MSTSSky.mstsskysolarDirection;

            if (ShadowMapMaterial == null)
                ShadowMapMaterial = (ShadowMapMaterial)viewer.MaterialManager.Load("ShadowMap");
            if (SceneryShader == null)
                SceneryShader = viewer.MaterialManager.SceneryShader;
        }

        public void SetCamera(Camera camera)
        {
            Camera = camera;
            XNACameraLocation = CameraLocation = Camera.Location;
            XNACameraLocation.Z *= -1;
            XNACameraView = Camera.XnaView;
            XNACameraProjection = Camera.XnaProjection;
        }

        static bool LockShadows;
        [CallOnThread("Updater")]
        public void PrepareFrame(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommand.DebugLockShadows))
                LockShadows = !LockShadows;

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && !LockShadows)
            {
                var solarDirection = SolarDirection;
                solarDirection.Normalize();
                if (Vector3.Dot(SteppedSolarDirection, solarDirection) < 0.99999f)
                    SteppedSolarDirection = solarDirection;

                var cameraDirection = new Vector3(-XNACameraView.M13, -XNACameraView.M23, -XNACameraView.M33);
                cameraDirection.Normalize();

                var shadowMapAlignAxisX = Vector3.Cross(SteppedSolarDirection, Vector3.UnitY);
                var shadowMapAlignAxisY = Vector3.Cross(shadowMapAlignAxisX, SteppedSolarDirection);
                shadowMapAlignAxisX.Normalize();
                shadowMapAlignAxisY.Normalize();
                ShadowMapX = shadowMapAlignAxisX;
                ShadowMapY = shadowMapAlignAxisY;

                var shadowMapResolution = Game.Settings.ShadowMapResolution;
                var viewingDistance = Game.Settings.ViewingDistance;

                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    var shadowMapDiameter = RenderProcess.ShadowMapDiameter[shadowMapIndex];
                    var shadowMapLocation = XNACameraLocation + RenderProcess.ShadowMapDistance[shadowMapIndex] * cameraDirection;

                    var shadowMapAlignmentGrid = (float)shadowMapDiameter / shadowMapResolution;
                    var adjustX = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisX, shadowMapLocation), shadowMapAlignmentGrid);
                    var adjustY = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisY, shadowMapLocation), shadowMapAlignmentGrid);

                    shadowMapLocation -= shadowMapAlignAxisX * adjustX + shadowMapAlignAxisY * adjustY;

                    ShadowMapLightView[shadowMapIndex] = Matrix.CreateLookAt(shadowMapLocation + viewingDistance * SteppedSolarDirection, shadowMapLocation, Vector3.Up);
                    ShadowMapLightProj[shadowMapIndex] = Matrix.CreateOrthographic(shadowMapDiameter, shadowMapDiameter, 0, viewingDistance + shadowMapDiameter / 2);
                    ShadowMapLightViewProjShadowProj[shadowMapIndex] = ShadowMapLightView[shadowMapIndex] * ShadowMapLightProj[shadowMapIndex] * new Matrix(0.5f, 0, 0, 0, 0, -0.5f, 0, 0, 0, 0, 1, 0, 0.5f + 0.5f / shadowMapResolution, 0.5f + 0.5f / shadowMapResolution, 0, 1);
                    ShadowMapCenter[shadowMapIndex] = shadowMapLocation;
                }
            }
        }

        [CallOnThread("Updater")]
        public void AddAutoPrimitive(Vector3 mstsLocation, float objectRadius, float objectViewingDistance, Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (float.IsPositiveInfinity(objectViewingDistance) || (Camera != null && Camera.InRange(mstsLocation, objectRadius, objectViewingDistance)))
            {
                if (Camera != null && Camera.InFov(mstsLocation, objectRadius))
                    AddPrimitive(material, primitive, group, ref xnaMatrix, flags);
            }

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ((flags & ShapeFlags.ShadowCaster) != 0))
            {
                for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                {
                    if (IsInShadowMap(shadowMapIndex, mstsLocation, objectRadius, objectViewingDistance))
                        AddShadowPrimitive(shadowMapIndex, material, primitive, ref xnaMatrix, flags);
                }
            }
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, ShapeFlags.None, null);
        }

        static readonly bool[] PrimitiveBlendedScenery = new bool[] { true, false };
        static readonly bool[] PrimitiveBlended = new bool[] { true };
        static readonly bool[] PrimitiveNotBlended = new bool[] { false };

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, flags, null);
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags, object itemData)
        {
            var getBlending = material.GetBlending();
            var blending = getBlending && material is SceneryMaterial ? PrimitiveBlendedScenery : getBlending ? PrimitiveBlended : PrimitiveNotBlended;

            for (int i = 0; i < blending.Length; i++)
            {
                var blended = blending[i];
                var sortingMaterial = blended ? DummyBlendedMaterial : material;
                var sequence = RenderItems[(int)GetRenderSequence(group, blended)];

                if (!sequence.TryGetValue(sortingMaterial, out RenderItemCollection items))
                {
                    items = new RenderItemCollection();
                    sequence.Add(sortingMaterial, items);
                }
                items.Add(new RenderItem(material, primitive, ref xnaMatrix, flags, itemData));
            }
            if (((flags & ShapeFlags.AutoZBias) != 0) && (primitive.ZBias == 0))
                primitive.ZBias = 1;
        }

        [CallOnThread("Updater")]
        void AddShadowPrimitive(int shadowMapIndex, Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (material is SceneryMaterial)
                RenderShadowSceneryItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is ForestMaterial)
                RenderShadowForestItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else if (material is TerrainMaterial)
                RenderShadowTerrainItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
        }

        [CallOnThread("Updater")]
        public void Sort()
        {
            var renderItemComparer = new RenderItem.Comparer(CameraLocation);
            for (int i = 0; i < RenderItems.Length; i++)
            {
                if (RenderItems[i].TryGetValue(DummyBlendedMaterial, out RenderItemCollection items) && items.Count > 0)
                {
                    items.Sort(renderItemComparer);
                }
            }
        }

        bool IsInShadowMap(int shadowMapIndex, Vector3 mstsLocation, float objectRadius, float objectViewingDistance)
        {
            if (ShadowMapRenderTarget == null)
                return false;

            if (Program.Simulator.Settings.ShadowSettings == 4)
            {
                if (objectRadius < 100)
                    objectRadius *= 200 / objectRadius;
                else
                    objectRadius *= 2;
            }

            // Převod do lokálního prostoru stínové mapy
            float relX = mstsLocation.X - ShadowMapCenter[shadowMapIndex].X;
            float relY = mstsLocation.Y - ShadowMapCenter[shadowMapIndex].Y;
            float relZ = -mstsLocation.Z - ShadowMapCenter[shadowMapIndex].Z;

            objectRadius += RenderProcess.ShadowMapDiameter[shadowMapIndex] * 0.5f;
            float radiusSq = objectRadius * objectRadius;

            // Rychlý AABB test před výpočtem delších vektorů
            if (Math.Abs(relX) > objectRadius || Math.Abs(relY) > objectRadius || Math.Abs(relZ) > objectRadius)
            {
                if (relX * relX + relY * relY + relZ * relZ > radiusSq)
                    return false;
            }

            Vector3 relLoc = new Vector3(relX, relY, relZ);

            if (Math.Abs(Vector3.Dot(relLoc, ShadowMapX)) > objectRadius)
                return false;

            if (Math.Abs(Vector3.Dot(relLoc, ShadowMapY)) > objectRadius)
                return false;

            if (Vector3.Dot(relLoc, SteppedSolarDirection) < 0)
                return false;

            return true;
        }

        static RenderPrimitiveSequence GetRenderSequence(RenderPrimitiveGroup group, bool blended)
        {
            return blended ? RenderPrimitive.SequenceForBlended[(int)group] : RenderPrimitive.SequenceForOpaque[(int)group];
        }

        [CallOnThread("Render")]
        public void Draw(GraphicsDevice graphicsDevice)
        {
            var logging = UserInput.IsPressed(UserCommand.DebugLogRenderFrame);

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ShadowMapMaterial != null)
                DrawShadows(graphicsDevice, logging);

            DrawSimple(graphicsDevice, logging);

            // Nahrazení LINQ Sum klasickým cyklem bez alokací
            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                int totalCount = 0;
                foreach (var collection in RenderItems[i].Values)
                {
                    totalCount += collection.Count;
                }
                Game.RenderProcess.PrimitiveCount[i] = totalCount;
            }
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging)
        {
            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                DrawShadows(graphicsDevice, logging, shadowMapIndex);

            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
            {
                Game.RenderProcess.ShadowPrimitiveCount[shadowMapIndex] =
                    RenderShadowSceneryItems[shadowMapIndex].Count +
                    RenderShadowForestItems[shadowMapIndex].Count +
                    RenderShadowTerrainItems[shadowMapIndex].Count;
            }
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging, int shadowMapIndex)
        {
            graphicsDevice.SetRenderTarget(ShadowMapRenderTarget[shadowMapIndex]);
            graphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.White, 1, 0);

            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowSceneryItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Forest);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowForestItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);
            graphicsDevice.Indices = TerrainPrimitive.SharedPatchIndexBuffer;
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowTerrainItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Blocker);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowTerrainItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            ShadowMapMaterial.ResetState(graphicsDevice);
            graphicsDevice.SetRenderTarget(null);

            if (Game.Settings.ShadowMapBlur)
            {
                ShadowMap[shadowMapIndex] = ShadowMapMaterial.ApplyBlur(graphicsDevice, ShadowMap[shadowMapIndex], ShadowMapRenderTarget[shadowMapIndex]);
            }
            else
            {
                ShadowMap[shadowMapIndex] = ShadowMapRenderTarget[shadowMapIndex];
            }
        }

        void DrawSimple(GraphicsDevice graphicsDevice, bool logging)
        {
            if (Game.Settings.DistantMountains)
            {
                graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
                DrawSequencesDistantMountains(graphicsDevice, logging);
                graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1, 0);
                DrawSequences(graphicsDevice, logging);
            }
            else
            {
                graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
                DrawSequences(graphicsDevice, logging);
            }
        }

        void DrawSequences(GraphicsDevice graphicsDevice, bool logging)
        {
            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && SceneryShader != null)
                SceneryShader.SetShadowMap(ShadowMapLightViewProjShadowProj, ShadowMap, RenderProcess.ShadowMapLimit);

            var renderItems = RenderItemsSequence;
            renderItems.Clear();

            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                var sequence = RenderItems[i];
                foreach (var sequenceMaterial in sequence)
                {
                    if (sequenceMaterial.Value.Count == 0)
                        continue;

                    if (sequenceMaterial.Key == DummyBlendedMaterial)
                    {
                        Material lastMaterial = null;
                        foreach (var renderItem in sequenceMaterial.Value)
                        {
                            if (lastMaterial != renderItem.Material)
                            {
                                if (renderItems.Count > 0)
                                {
                                    lastMaterial.Render(graphicsDevice, renderItems, ref XNACameraView, ref XNACameraProjection);
                                    renderItems.Clear();
                                }
                                if (lastMaterial != null)
                                    lastMaterial.ResetState(graphicsDevice);

                                renderItem.Material.SetState(graphicsDevice, lastMaterial);
                                lastMaterial = renderItem.Material;
                            }
                            renderItems.Add(renderItem);
                        }
                        if (renderItems.Count > 0)
                        {
                            lastMaterial.Render(graphicsDevice, renderItems, ref XNACameraView, ref XNACameraProjection);
                            renderItems.Clear();
                        }
                        if (lastMaterial != null)
                            lastMaterial.ResetState(graphicsDevice);
                    }
                    else
                    {
                        if (Game.Settings.DistantMountains && (sequenceMaterial.Key is TerrainSharedDistantMountain || sequenceMaterial.Key is SkyMaterial || sequenceMaterial.Key is MSTSSkyMaterial))
                            continue;

                        sequenceMaterial.Key.SetState(graphicsDevice, null);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNACameraView, ref XNACameraProjection);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
                    }
                }
            }

            if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && SceneryShader != null)
                SceneryShader.ClearShadowMap();
        }

        void DrawSequencesDistantMountains(GraphicsDevice graphicsDevice, bool logging)
        {
            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                var sequence = RenderItems[i];
                foreach (var sequenceMaterial in sequence)
                {
                    if (sequenceMaterial.Value.Count == 0)
                        continue;

                    if (sequenceMaterial.Key is TerrainSharedDistantMountain || sequenceMaterial.Key is SkyMaterial || sequenceMaterial.Key is MSTSSkyMaterial)
                    {
                        sequenceMaterial.Key.SetState(graphicsDevice, null);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNACameraView, ref Camera.XnaDistantMountainProjection);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
                    }
                }
            }
        }
    }
}