// COPYRIGHT 2011, 2012, 2013, 2014 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using System;
using System.Collections.Generic;
using Orts.Simulation.Simulation.RollingStocks;

namespace Orts.Viewer3D
{
    public class ParticleEmitterViewer
    {
        public const float VolumeScale = 1f / 100;
        public const float Rate = 0.1f;
        public const float DecelerationTime = 0.2f;
        public const float InitialSpreadRate = 1;
        public const float SpreadRate = 0.75f;
        public const float DurationVariation = 0.5f;

        public const float MaxParticlesPerSecond = 50f;
        public const float MaxParticleDuration = 50f;

        readonly Viewer Viewer;
        readonly float EmissionHoleM2 = 1;
        readonly ParticleEmitterPrimitive Emitter;

        ParticleEmitterMaterial Material;

        public ParticleEmitterViewer(Viewer viewer, ParticleEmitterData data, WorldPosition worldPosition)
        {
            Viewer = viewer;
            EmissionHoleM2 = (MathHelper.Pi * ((data.NozzleWidth / 2f) * (data.NozzleWidth / 2f)));
            Emitter = new ParticleEmitterPrimitive(viewer, data, worldPosition);
        }

        public void Initialize(string textureName)
        {
            Material = (ParticleEmitterMaterial)Viewer.MaterialManager.Load("ParticleEmitter", textureName);
        }

        public void SetOutput(float volumeM3pS)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.XNADirection * volumeM3pS / EmissionHoleM2 * VolumeScale;
            Emitter.ParticlesPerSecond = volumeM3pS / EmissionHoleM2 * Rate;
        }

        public void SetOutput(float volumeM3pS, float durationS, Color color)
        {
            SetOutput(volumeM3pS);
            Emitter.ParticleDuration = durationS;
            Emitter.ParticleColor = color;
        }

        public void SetOutput(float initialVelocityMpS, float volumeM3pS, float durationS)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.XNADirection * initialVelocityMpS / 10;
            Emitter.ParticlesPerSecond = volumeM3pS / Rate * 0.2f;
            Emitter.ParticleDuration = durationS;
        }

        public void SetOutput(float initialVelocityMpS, float volumeM3pS, float durationS, Color color)
        {
            Emitter.XNAInitialVelocity = Emitter.EmitterData.XNADirection * initialVelocityMpS / 10;
            Emitter.ParticlesPerSecond = volumeM3pS / Rate * 0.2f;
            Emitter.ParticleDuration = durationS;
            Emitter.ParticleColor = color;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var gameTime = (float)Viewer.Simulator.GameTime;
            Emitter.Update(gameTime, elapsedTime);

            var XNAWorldLocation = Matrix.Identity;
            XNAWorldLocation.M11 = gameTime;
            XNAWorldLocation.M21 = Viewer.Camera.TileX;
            XNAWorldLocation.M22 = Viewer.Camera.TileZ;

            if (Emitter.HasParticlesToRender())
                frame.AddPrimitive(Material, Emitter, RenderPrimitiveGroup.Particles, ref XNAWorldLocation);
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            if (Material != null)
                Material.Mark();
        }
    }

    public class ParticleEmitterPrimitive : RenderPrimitive
    {
        const int IndiciesPerParticle = 6;
        const int VerticiesPerParticle = 4;
        const int PrimitivesPerParticle = 2;

        readonly int MaxParticles;
        readonly ParticleVertex[] Vertices;
        readonly VertexDeclaration VertexDeclaration;
        readonly DynamicVertexBuffer VertexBuffer;
        readonly IndexBuffer IndexBuffer;

        readonly float[] PerlinStart;

        // Rychlá LUT tabulka šumu eliminující volání Noise.Generate na CPU
        const int NoiseTableSize = 1024;
        const int NoiseMask = NoiseTableSize - 1;
        static readonly float[] FastNoiseTable = new float[NoiseTableSize];

        static ParticleEmitterPrimitive()
        {
            var rand = new Random(1337);
            for (int i = 0; i < NoiseTableSize; i++)
            {
                FastNoiseTable[i] = (float)(rand.NextDouble() * 2.0 - 1.0);
            }
        }

        private static float GetFastNoise(float time)
        {
            int index = (int)(time * 16.0f) & NoiseMask;
            return FastNoiseTable[index];
        }

        struct ParticleVertex
        {
            public Vector4 StartPosition_StartTime;
            public Vector4 InitialVelocity_EndTime;
            public Vector4 TargetVelocity_TargetTime;
            public Vector4 TileXY_Vertex_ID;
            public Color Color_Random;

            public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.Position, 1),
                new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.Position, 2),
                new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.Position, 3),
                new VertexElement(64, VertexElementFormat.Color, VertexElementUsage.Position, 4)
            };

            public const int VertexStride = 68;
        }

        internal ParticleEmitterData EmitterData;
        internal Vector3 XNAInitialVelocity;
        internal Vector3 XNATargetVelocity;
        internal float ParticlesPerSecond;
        internal float ParticleDuration;
        internal Color ParticleColor;

        internal WorldPosition WorldPosition;
        internal WorldPosition LastWorldPosition;

        int FirstActiveParticle;
        int FirstNewParticle;
        int FirstFreeParticle;
        int FirstRetiredParticle;

        float TimeParticlesLastEmitted;
        int DrawCounter;

        Viewer viewer;
        GraphicsDevice graphicsDevice;

        static float windDisplacementX;
        static float windDisplacementZ;

        public ParticleEmitterPrimitive(Viewer viewer, ParticleEmitterData data, WorldPosition worldPosition)
        {
            this.viewer = viewer;
            this.graphicsDevice = viewer.GraphicsDevice;

            MaxParticles = (int)(ParticleEmitterViewer.MaxParticlesPerSecond * ParticleEmitterViewer.MaxParticleDuration);
            Vertices = new ParticleVertex[MaxParticles * VerticiesPerParticle];
            VertexDeclaration = new VertexDeclaration(ParticleVertex.VertexStride, ParticleVertex.VertexElements);
            VertexBuffer = new DynamicVertexBuffer(graphicsDevice, VertexDeclaration, MaxParticles * VerticiesPerParticle, BufferUsage.WriteOnly);
            IndexBuffer = InitIndexBuffer(graphicsDevice, MaxParticles * IndiciesPerParticle);

            EmitterData = data;
            XNAInitialVelocity = data.XNADirection;
            XNATargetVelocity = Vector3.Up;
            ParticlesPerSecond = 0;
            ParticleDuration = 3;
            ParticleColor = Color.White;

            WorldPosition = worldPosition;
            LastWorldPosition = new WorldPosition(worldPosition);

            TimeParticlesLastEmitted = (float)viewer.Simulator.GameTime;

            PerlinStart = new float[] {
                (float)Viewer.Random.NextDouble() * 30000f,
                (float)Viewer.Random.NextDouble() * 30000f,
                (float)Viewer.Random.NextDouble() * 30000f,
                (float)Viewer.Random.NextDouble() * 30000f,
            };
        }

        void VertexBuffer_ContentLost()
        {
            VertexBuffer.SetData(0, Vertices, 0, Vertices.Length, ParticleVertex.VertexStride, SetDataOptions.NoOverwrite);
        }

        static IndexBuffer InitIndexBuffer(GraphicsDevice graphicsDevice, int numIndicies)
        {
            var indices = new ushort[numIndicies];
            var index = 0;
            for (var i = 0; i < numIndicies; i += IndiciesPerParticle)
            {
                indices[i] = (ushort)index;
                indices[i + 1] = (ushort)(index + 1);
                indices[i + 2] = (ushort)(index + 2);

                indices[i + 3] = (ushort)(index + 2);
                indices[i + 4] = (ushort)(index + 3);
                indices[i + 5] = (ushort)(index);

                index += VerticiesPerParticle;
            }
            var indexBuffer = new IndexBuffer(graphicsDevice, typeof(ushort), numIndicies, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
            return indexBuffer;
        }

        public float EmitSize => EmitterData.NozzleWidth;

        void RetireActiveParticles(float currentTime)
        {
            while (FirstActiveParticle != FirstNewParticle)
            {
                var vertex = FirstActiveParticle * VerticiesPerParticle;
                if (Vertices[vertex].InitialVelocity_EndTime.W > currentTime)
                    break;

                Vertices[vertex].StartPosition_StartTime.W = (float)DrawCounter;
                FirstActiveParticle = (FirstActiveParticle + 1) % MaxParticles;
            }
        }

        void FreeRetiredParticles()
        {
            while (FirstRetiredParticle != FirstActiveParticle)
            {
                var vertex = FirstRetiredParticle * VerticiesPerParticle;
                var age = DrawCounter - (int)Vertices[vertex].StartPosition_StartTime.W;

                if (age < 2)
                    break;

                FirstRetiredParticle = (FirstRetiredParticle + 1) % MaxParticles;
            }
        }

        int GetCountFreeParticles()
        {
            var nextFree = (FirstFreeParticle + 1) % MaxParticles;
            if (nextFree <= FirstRetiredParticle)
                return FirstRetiredParticle - nextFree;

            return (MaxParticles - nextFree) + FirstRetiredParticle;
        }

        public void Update(float currentTime, ElapsedTime elapsedTime)
        {
            if (viewer.Simulator.WeatherResetEmitter)
            {
                TimeParticlesLastEmitted = currentTime;
                viewer.Simulator.WeatherResetEmitter = false;
            }

            windDisplacementX = viewer.Simulator.Weather.WindSpeedMpS.X * 0.25f;
            windDisplacementZ = viewer.Simulator.Weather.WindSpeedMpS.Y * 0.25f;

            var velocity = WorldPosition.Location - LastWorldPosition.Location;
            velocity.X += (WorldPosition.TileX - LastWorldPosition.TileX) * 2048;
            velocity.Z += (WorldPosition.TileZ - LastWorldPosition.TileZ) * -2048;

            if (elapsedTime.ClockSeconds > 0.0001f)
                velocity /= elapsedTime.ClockSeconds;
            else
                velocity = Vector3.Zero;

            LastWorldPosition.Location = WorldPosition.Location;
            LastWorldPosition.TileX = WorldPosition.TileX;
            LastWorldPosition.TileZ = WorldPosition.TileZ;

            RetireActiveParticles(currentTime);
            FreeRetiredParticles();

            if (ParticlesPerSecond < 0.1f || (currentTime - TimeParticlesLastEmitted > 1.0f))
                TimeParticlesLastEmitted = currentTime;

            var numToBeEmitted = (int)((currentTime - TimeParticlesLastEmitted) * ParticlesPerSecond);
            var numCanBeEmitted = GetCountFreeParticles();
            var numToEmit = Math.Min(numToBeEmitted, numCanBeEmitted);

            if (numToEmit > 0)
            {
                var rotation = WorldPosition.XNAMatrix;
                rotation.Translation = Vector3.Zero;

                var position = Vector3.Transform(EmitterData.XNALocation, rotation) + WorldPosition.XNAMatrix.Translation;
                var globalInitialVelocity = Vector3.Transform(XNAInitialVelocity, rotation) + velocity;
                var globalTargetVelocity = Vector3.Transform(XNATargetVelocity, rotation);

                var time = TimeParticlesLastEmitted;
                var step = 1f / ParticlesPerSecond;
                float tileX = WorldPosition.TileX;
                float tileZ = WorldPosition.TileZ;

                for (var i = 0; i < numToEmit; i++)
                {
                    time += step;

                    var particle = (FirstFreeParticle + 1) % MaxParticles;
                    var vertex = particle * VerticiesPerParticle;
                    var texture = Viewer.Random.Next(16);
                    var color_Random = new Color(ParticleColor.R / 255f, ParticleColor.G / 255f, ParticleColor.B / 255f, (float)Viewer.Random.NextDouble());

                    var initialVelocity = globalInitialVelocity;
                    initialVelocity.X += (float)(Viewer.Random.NextDouble() - 0.5) * ParticleEmitterViewer.InitialSpreadRate;
                    initialVelocity.Z += (float)(Viewer.Random.NextDouble() - 0.5) * ParticleEmitterViewer.InitialSpreadRate;

                    // Extrémně rychlý rozptyl přes LUT namísto analytického Perlin šumu
                    var targetVelocity = globalTargetVelocity;
                    targetVelocity.X += GetFastNoise(time + PerlinStart[0]) * ParticleEmitterViewer.SpreadRate + windDisplacementX;
                    targetVelocity.Y += GetFastNoise(time + PerlinStart[1]) * ParticleEmitterViewer.SpreadRate;
                    targetVelocity.Z += GetFastNoise(time + PerlinStart[2]) * ParticleEmitterViewer.SpreadRate + windDisplacementZ;

                    var duration = ParticleDuration * (1f + GetFastNoise(time + PerlinStart[3]) * ParticleEmitterViewer.DurationVariation);

                    // Příprava společných vektorů pro 4 vrcholy najednou
                    var startPosTime = new Vector4(position, time);
                    var initVelEndTime = new Vector4(initialVelocity, time + duration);
                    var targetVelTargetTime = new Vector4(targetVelocity, ParticleEmitterViewer.DecelerationTime);

                    // Přímé nastavení 4 vrcholů bez vnitřní smyčky a redundancí
                    Vertices[vertex].StartPosition_StartTime = startPosTime;
                    Vertices[vertex].InitialVelocity_EndTime = initVelEndTime;
                    Vertices[vertex].TargetVelocity_TargetTime = targetVelTargetTime;
                    Vertices[vertex].TileXY_Vertex_ID = new Vector4(tileX, tileZ, 0, texture);
                    Vertices[vertex].Color_Random = color_Random;

                    Vertices[vertex + 1].StartPosition_StartTime = startPosTime;
                    Vertices[vertex + 1].InitialVelocity_EndTime = initVelEndTime;
                    Vertices[vertex + 1].TargetVelocity_TargetTime = targetVelTargetTime;
                    Vertices[vertex + 1].TileXY_Vertex_ID = new Vector4(tileX, tileZ, 1, texture);
                    Vertices[vertex + 1].Color_Random = color_Random;

                    Vertices[vertex + 2].StartPosition_StartTime = startPosTime;
                    Vertices[vertex + 2].InitialVelocity_EndTime = initVelEndTime;
                    Vertices[vertex + 2].TargetVelocity_TargetTime = targetVelTargetTime;
                    Vertices[vertex + 2].TileXY_Vertex_ID = new Vector4(tileX, tileZ, 2, texture);
                    Vertices[vertex + 2].Color_Random = color_Random;

                    Vertices[vertex + 3].StartPosition_StartTime = startPosTime;
                    Vertices[vertex + 3].InitialVelocity_EndTime = initVelEndTime;
                    Vertices[vertex + 3].TargetVelocity_TargetTime = targetVelTargetTime;
                    Vertices[vertex + 3].TileXY_Vertex_ID = new Vector4(tileX, tileZ, 3, texture);
                    Vertices[vertex + 3].Color_Random = color_Random;

                    FirstFreeParticle = particle;
                }

                TimeParticlesLastEmitted = time;
            }
        }

        void AddNewParticlesToVertexBuffer()
        {
            if (FirstNewParticle < FirstFreeParticle)
            {
                var numParticlesToAdd = FirstFreeParticle - FirstNewParticle;
                VertexBuffer.SetData(
                    FirstNewParticle * ParticleVertex.VertexStride * VerticiesPerParticle,
                    Vertices,
                    FirstNewParticle * VerticiesPerParticle,
                    numParticlesToAdd * VerticiesPerParticle,
                    ParticleVertex.VertexStride,
                    SetDataOptions.NoOverwrite);
            }
            else
            {
                var numParticlesToAddAtEnd = MaxParticles - FirstNewParticle;
                VertexBuffer.SetData(
                    FirstNewParticle * ParticleVertex.VertexStride * VerticiesPerParticle,
                    Vertices,
                    FirstNewParticle * VerticiesPerParticle,
                    numParticlesToAddAtEnd * VerticiesPerParticle,
                    ParticleVertex.VertexStride,
                    SetDataOptions.NoOverwrite);

                if (FirstFreeParticle > 0)
                {
                    VertexBuffer.SetData(
                        0,
                        Vertices,
                        0,
                        FirstFreeParticle * VerticiesPerParticle,
                        ParticleVertex.VertexStride,
                        SetDataOptions.NoOverwrite);
                }
            }

            FirstNewParticle = FirstFreeParticle;
        }

        public bool HasParticlesToRender() => FirstActiveParticle != FirstFreeParticle;

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (VertexBuffer.IsContentLost)
                VertexBuffer_ContentLost();

            if (FirstNewParticle != FirstFreeParticle)
                AddNewParticlesToVertexBuffer();

            if (HasParticlesToRender())
            {
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.SetVertexBuffer(VertexBuffer);

                if (FirstActiveParticle < FirstFreeParticle)
                {
                    var numParticles = FirstFreeParticle - FirstActiveParticle;
                    if (numParticles > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, FirstActiveParticle * IndiciesPerParticle, numParticles * PrimitivesPerParticle);
                }
                else
                {
                    var numParticlesAtEnd = MaxParticles - FirstActiveParticle;
                    if (numParticlesAtEnd > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, FirstActiveParticle * IndiciesPerParticle, numParticlesAtEnd * PrimitivesPerParticle);
                    if (FirstFreeParticle > 0)
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, FirstFreeParticle * PrimitivesPerParticle);
                }
            }

            DrawCounter++;
        }
    }

    public class ParticleEmitterMaterial : Material
    {
        public Texture2D Texture;
        IEnumerator<EffectPass> ShaderPasses;

        public ParticleEmitterMaterial(Viewer viewer, string textureName)
            : base(viewer, null)
        {
            Texture = viewer.TextureManager.Get(textureName, true);
            ShaderPasses = Viewer.MaterialManager.ParticleEmitterShader.Techniques["ParticleEmitterTechnique"].Passes.GetEnumerator();
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.ParticleEmitterShader;
            if (!Viewer.Settings.UseMSTSEnv)
                shader.LightVector = Viewer.World.Sky.solarDirection;
            else
                shader.LightVector = Viewer.World.MSTSSky.mstsskysolarDirection;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.ParticleEmitterShader;

            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    shader.CameraTileXY = new Vector2(item.XNAMatrix.M21, item.XNAMatrix.M22);
                    shader.CurrentTime = item.XNAMatrix.M11;

                    var emitter = (ParticleEmitterPrimitive)item.RenderPrimitive;
                    shader.EmitSize = emitter.EmitSize;
                    shader.Texture = Texture;
                    shader.SetMatrix(Matrix.Identity, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    ShaderPasses.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending() => true;

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            base.Mark();
        }
    }
}