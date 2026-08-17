// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// Uncomment either or both of these for debugging information about lights.
//#define DEBUG_LIGHT_STATES
//#define DEBUG_LIGHT_TRANSITIONS
//#define DEBUG_LIGHT_CONE
//#define DEBUG_LIGHT_CONE_FULL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Orts.Viewer3D
{
    public class LightViewer
    {
        readonly Viewer Viewer;
        readonly TrainCar Car;
        readonly Material LightGlowMaterial;
        readonly Material LightConeMaterial;

        public int TrainHeadlight;
        public bool CarIsReversed;
        public bool CarIsFirst;
        public bool CarIsLast;
        public bool Penalty;
        public bool CarIsPlayer;
        public bool CarInService;
        public bool IsDay;
        public WeatherType Weather;
        public bool CarCoupledFront;
        public bool CarCoupledRear;

        // Icik
        public bool CarLightFrontLW;
        public bool CarLightFrontRW;
        public bool CarLightRearLW;
        public bool CarLightRearRW;
        public bool CarLightFrontLR;
        public bool CarLightFrontRR;
        public bool CarLightRearLR;
        public bool CarLightRearRR;
        public bool CarFrontHeadLight;
        public bool CarRearHeadLight;
        public int TrainHeadlightFront;
        public int TrainHeadlightRear;        
        
        List<LightPrimitive> LightPrimitives = new List<LightPrimitive>();        
        readonly List<LightConePrimitive> ActiveLightCones = new List<LightConePrimitive>();
        
        public float LightConeFadeIn;
        public float LightConeFadeOut;
        public Vector3 LightConePosition;
        public Vector3 LightConeDirection;
        public float LightConeDistance;
        public float LightConeMinDotProduct;
        public Vector4 LightConeColor;

        public LightViewer(Viewer viewer, TrainCar car)
        {
            Viewer = viewer;
            Car = car;
            LightGlowMaterial = viewer.MaterialManager.Load("LightGlow", System.IO.Path.Combine(Viewer.ContentPath, "..\\Content\\FX\\Bulb.png"));
            LightConeMaterial = viewer.MaterialManager.Load("LightCone");

            UpdateState();

            // Automatické doplnění chybějících kuželů pozičních světel
            AutoGeneratePositionalLightCones(car);

            if (Car.Lights != null)
            {                
                foreach (var light in Car.Lights.Lights)
                {
                    if (!Car.Train.LightDimFound) Car.Train.LightDimFound = light.LightDimFound;

                    switch (light.Type)
                    {
                        case LightType.Glow:
                            LightPrimitives.Add(new LightGlowPrimitive(this, Viewer.RenderProcess, light));
                            
                            // Základní typy světelných masek
                            switch (light.LightGlowType)
                            {
                                case LightGlowType.Bulb:
                                    (LightPrimitives.Last() as LightGlowPrimitive).LightGlowMaterial = viewer.MaterialManager.Load("LightGlow", System.IO.Path.Combine(Viewer.ContentPath, "..\\Content\\FX\\Bulb.png"));
                                    break;
                                case LightGlowType.Led:
                                    (LightPrimitives.Last() as LightGlowPrimitive).LightGlowMaterial = viewer.MaterialManager.Load("LightGlow", System.IO.Path.Combine(Viewer.ContentPath, "..\\Content\\FX\\Led.png"));
                                    break;
                                case LightGlowType.Star:
                                    (LightPrimitives.Last() as LightGlowPrimitive).LightGlowMaterial = viewer.MaterialManager.Load("LightGlow", System.IO.Path.Combine(Viewer.ContentPath, "..\\Content\\FX\\Star.png"));
                                    break;                                                             
                            }                           
                            
                            // Uživatelsky definovaná maska
                            if (light.LightGlowName != null)                            
                                (LightPrimitives.Last() as LightGlowPrimitive).LightGlowMaterial = viewer.MaterialManager.Load("LightGlow", System.IO.Path.Combine(Viewer.ContentPath, "..\\Content\\FX\\" + light.LightGlowName));                            

                            break;
                        case LightType.Cone:                                                        
                                LightPrimitives.Add(new LightConePrimitive(this, Viewer.RenderProcess, light));                            
                            break;
                    }
                }
            }            
        }

        private void AutoGeneratePositionalLightCones(TrainCar car)
        {
            if (Car.Lights == null) return;

            // Zjistíme, které typy kuželů už v eng souboru existují
            var existingCones = LightPrimitives.Select(p => p.Light)
                .Concat(Car.Lights.Lights)
                .Where(l => l.Type == LightType.Cone)
                .Select(l => l.UnitSide)
                .ToHashSet();

            // Výchozí pozice podle délky vozidla (pokud nenajdeme Glow světla)
            float frontZ = Car.CarLengthM / 2.0f;
            float rearZ = -Car.CarLengthM / 2.0f;
            float posY = 0.5f;
            uint whiteColor = ConvertMstsColor(0xAAE0FFFF);
            uint redColor = ConvertMstsColor(0x88E00000);

            // Pokusíme se zpřesnit pozici Z a Y podle předních a zadních Glow světel
            var frontGlow = Car.Lights.Lights.FirstOrDefault(l => l.Type == LightType.Glow && l.UnitSide == LightHandleCondition.FrontLW && l.States.Count > 0 && l.States[0].Position.Y < 2.0f);
            if (frontGlow != null)
            {
                frontZ = frontGlow.States[0].Position.Z;
                posY = frontGlow.States[0].Position.Y;
                whiteColor = frontGlow.States[0].Color;
            }
            else
            {
                frontGlow = Car.Lights.Lights.FirstOrDefault(l => l.Type == LightType.Glow && l.States.Count > 0 && l.States[0].Position.Y < 2.0f && l.States[0].Position.Z > 0 && l.States[0].Azimuth.Z == 0f);
                if (frontGlow != null)
                {
                    frontZ = frontGlow.States[0].Position.Z;
                    posY = frontGlow.States[0].Position.Y;                    
                    whiteColor = frontGlow.States[0].Color;
                    car.NoUnitSideCar = true;
                }
            }

            var rearGlow = Car.Lights.Lights.FirstOrDefault(l => l.Type == LightType.Glow && l.UnitSide == LightHandleCondition.RearLR && l.States.Count > 0 && l.States[0].Position.Y < 2.0f);            
            if (rearGlow != null)
            {
                rearZ = rearGlow.States[0].Position.Z;                
            }
            else
            {
                rearGlow = Car.Lights.Lights.FirstOrDefault(l => l.Type == LightType.Glow && l.States.Count > 0 && l.States[0].Position.Y < 2.0f && l.States[0].Position.Z < 0 && l.States[0].Azimuth.Z == 180f);                
                if (rearGlow != null)
                {
                    rearZ = rearGlow.States[0].Position.Z;                                        
                    car.NoUnitSideCar = true;
                }
            }            

            // 11 - FrontW (Přední bílá)
            if (!existingCones.Contains(LightHandleCondition.FrontW))
            {
                AddConeLightW(LightHandleCondition.FrontW, new Vector3(0, posY, frontZ), new Vector3(0, 0, 0), whiteColor);
            }

            // 12 - RearW (Zadní bílá)
            if (!existingCones.Contains(LightHandleCondition.RearW))
            {
                AddConeLightW(LightHandleCondition.RearW, new Vector3(0, posY, rearZ), new Vector3(180, 180, 180), whiteColor);
            }

            // 13 - FrontR (Přední červená)
            if (!existingCones.Contains(LightHandleCondition.FrontR))
            {
                AddConeLightR(LightHandleCondition.FrontR, new Vector3(0, posY, frontZ), new Vector3(0, 0, 0), redColor);
            }

            // 14 - RearR (Zadní červená)
            if (!existingCones.Contains(LightHandleCondition.RearR))
            {
                AddConeLightR(LightHandleCondition.RearR, new Vector3(0, posY, rearZ), new Vector3(180, 180, 180), redColor);
            }
        }

        private void AddConeLightW(LightHandleCondition unitSide, Vector3 position, Vector3 azimuth, uint color)
        {
            var state = new LightState(color, position, azimuth, angle: 150f, radius: 35f);
            var light = new Light(
                Car.Lights.Lights.Count,
                LightType.Cone,
                unitSide,
                LightControlCondition.Player,
                new List<LightState> { state }
            );

            Car.Lights.Lights.Add(light);
        }
        private void AddConeLightR(LightHandleCondition unitSide, Vector3 position, Vector3 azimuth, uint color)
        {
            var state = new LightState(color, position, azimuth, angle: 150f, radius: 15f);
            var light = new Light(
                Car.Lights.Lights.Count,
                LightType.Cone,
                unitSide,
                LightControlCondition.Player,
                new List<LightState> { state }
            );

            Car.Lights.Lights.Add(light);
        }

        private uint ConvertMstsColor(uint color)
        {
            return new Color()
            {
                B = (byte)(color),
                G = (byte)(color >> 8),
                R = (byte)(color >> 16),
                A = (byte)(color >> 24)
            }.PackedValue;
        }

        void UpdateActiveLightCone()
        {
            // Načtení všech aktivních světelných kuželů
            ActiveLightCones.Clear();
            var enabledCones = LightPrimitives.OfType<LightConePrimitive>().Where(lm => lm.Enabled);
            ActiveLightCones.AddRange(enabledCones);           
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Icik            
            var locomotive = Car.Train != null && Car.Train.IsActualPlayerTrain ? Viewer.PlayerLocomotive : null;
            var mstsLocomotive = locomotive as MSTSLocomotive;

            // Pokud má vlak vypnuté napájení světel, tak se světla nezobrazují, také pokud je vůz bez UnitSide reflektorů a není první ani poslední vůz vlaku, tak se světla nezobrazují
            if (Car.Train != null && (!Car.CarLightsPowerOn || (Car.NoUnitSideCar && Car != Car.Train.FirstCar && Car != Car.Train.LastCar)))                              
            {                
                return;
            }
            
            if (UpdateState())
            {
                foreach (var lightPrimitive in LightPrimitives)
                    lightPrimitive.UpdateState(this);

                UpdateActiveLightCone();
            }

            foreach (var lightPrimitive in LightPrimitives)
                lightPrimitive.PrepareFrame(frame, elapsedTime);

            int dTileX = Car.WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = Car.WorldPosition.TileZ - Viewer.Camera.TileZ;
            Matrix xnaDTileTranslation = Matrix.CreateTranslation(dTileX * 2048, 0, -dTileZ * 2048);  // object is offset from camera this many tiles
            xnaDTileTranslation = Car.WorldPosition.XNAMatrix * xnaDTileTranslation;

            Vector3 mstsLocation = new Vector3(xnaDTileTranslation.Translation.X, xnaDTileTranslation.Translation.Y, -xnaDTileTranslation.Translation.Z);

            float objectRadius = 20; // Even more arbitrary.
            float objectViewingDistance = Viewer.Settings.ViewingDistance > 2000f ? 2000 : Viewer.Settings.ViewingDistance; // Arbitrary.            

            if (Viewer.Camera.CanSee(mstsLocation, objectRadius, objectViewingDistance))
                foreach (var lightPrimitive in LightPrimitives)
                {
                    if ((lightPrimitive.Enabled || lightPrimitive.FadeOut))
                    {
                        if (lightPrimitive is LightGlowPrimitive)
                            frame.AddPrimitive((lightPrimitive as LightGlowPrimitive).LightGlowMaterial, lightPrimitive, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);

                        if (lightPrimitive is LightConePrimitive)
                            frame.AddPrimitive(LightConeMaterial, lightPrimitive, RenderPrimitiveGroup.Lights, ref xnaDTileTranslation);
                    }                                       
                }                            
            
            // Registrace všech aktivních i dohasínajících světelných kuželů
            var visibleCones = LightPrimitives.OfType<LightConePrimitive>()
                .Where(c => c.Enabled || c.FadeOut || c.Fade.X > 0f);

            int visibleConesCount = 0;
            foreach (var cone in visibleCones)
            {                
                Vector3 conePos = Vector3.Transform(Vector3.Lerp(cone.Position1, cone.Position2, cone.Fade.Y), xnaDTileTranslation);
                Vector3 coneDir = Vector3.Transform(Vector3.Lerp(cone.Direction1, cone.Direction2, cone.Fade.Y), Car.WorldPosition.XNAMatrix);
                coneDir -= Car.WorldPosition.XNAMatrix.Translation;
                coneDir.Normalize();

                float coneDist = MathHelper.Lerp(cone.Distance1, cone.Distance2, cone.Fade.Y);
                float coneMinDot = (float)Math.Cos(MathHelper.Lerp(cone.Angle1, cone.Angle2, cone.Fade.Y));
                Vector4 coneColor = Vector4.Lerp(cone.Color1, cone.Color2, cone.Fade.Y) * cone.Fade.X;

                Viewer.RegisterActiveLightCone(
                    conePos,
                    coneDir,
                    coneColor,
                    coneDist,
                    coneMinDot
                );
                
                visibleConesCount++;
            }
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            LightGlowMaterial.Mark();
            LightConeMaterial.Mark();
            foreach (var lightPrimitive in LightPrimitives)
                if (lightPrimitive is LightGlowPrimitive)
                    (lightPrimitive as LightGlowPrimitive).LightGlowMaterial.Mark();
        }

        public static void CalculateLightCone(LightState lightState, out Vector3 position, out Vector3 direction, out float angle, out float radius, out float distance, out Vector4 color)
        {
            position = lightState.Position;                        
            position.Z *= -1;
            direction = -Vector3.UnitZ;
            direction = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-lightState.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-lightState.Azimuth.Y)));
            angle = MathHelper.ToRadians(lightState.Angle) / 2;
            radius = lightState.Radius / 2;
            distance = (float)(radius / Math.Sin(angle));
            color = new Color() { PackedValue = lightState.Color }.ToVector4();
        }

        int LightCycle = 0;
        bool UpdateState()
        {
            Debug.Assert(Viewer.PlayerTrain.LeadLocomotive == Viewer.PlayerLocomotive || Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING ||
                Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.REMOTE || Viewer.PlayerTrain.TrainType == Train.TRAINTYPE.STATIC, "PlayerTrain.LeadLocomotive must be PlayerLocomotive.");
            var locomotive = Car.Train != null && Car.Train.IsActualPlayerTrain ? Viewer.PlayerLocomotive : null;
            if (locomotive == null && Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.REMOTE && Car is MSTSLocomotive && (Car as MSTSLocomotive) == Car.Train.LeadLocomotive)
                locomotive = Car.Train.LeadLocomotive;
            var mstsLocomotive = locomotive != null ? locomotive as MSTSLocomotive : null;

            // Headlight
            //var newTrainHeadlight = locomotive != null && mstsLocomotive.Battery ? locomotive.Headlight : Car.Train != null && Car.Train.TrainType != Train.TRAINTYPE.STATIC ? 2 : 0;
            var newTrainHeadlight = locomotive != null ? locomotive.Headlight[mstsLocomotive.LocoStation] : 0;            

            // Unit
            var locomotiveFlipped = locomotive != null && locomotive.Flipped;
            var locomotiveReverseCab = mstsLocomotive != null && mstsLocomotive.UsingRearCab;
            var newCarIsReversed = Car.Flipped ^ locomotiveFlipped ^ locomotiveReverseCab;
            var newCarIsFirst = Car.Train == null || (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train.LastCar : Car.Train.FirstCar) == Car;
            var newCarIsLast = Car.Train == null || (locomotiveFlipped ^ locomotiveReverseCab ? Car.Train.FirstCar : Car.Train.LastCar) == Car;
            // Penalty
            var newPenalty = mstsLocomotive != null && mstsLocomotive.TrainBrakeController.EmergencyBraking;
            // Control
            var newCarIsPlayer = (Car.Train != null && Car.Train == Viewer.PlayerTrain)
                || (Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.REMOTE);
            // Service - if a player or AI train, then will considered to be in servie, loose consists will not be considered to be in service.
            var newCarInService = (Car.Train != null && Car.Train == Viewer.PlayerTrain)
                || (Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.REMOTE)
                || (Car.Train != null && Car.Train.TrainType == Train.TRAINTYPE.AI);

            // Time of day
            bool newIsDay = false;
            if (Viewer.Settings.UseMSTSEnv == false)
                newIsDay = Viewer.World.Sky.solarDirection.Y > 0;
            else
                newIsDay = Viewer.World.MSTSSky.mstsskysolarDirection.Y > 0;
            // Weather
            var newWeather = Viewer.Simulator.WeatherType;
            // Coupling
            var newCarCoupledFront = Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.LastCar : Car.Train.FirstCar) != Car);
            var newCarCoupledRear = Car.Train != null && (Car.Train.Cars.Count > 1) && ((Car.Flipped ? Car.Train.FirstCar : Car.Train.LastCar) != Car);
            
            // Icik
            // Ovládání obou reflektorů v jedné kabině 
            if (locomotive != null && mstsLocomotive?.HeadLight2Enable == true)
            {
                if (Car.FrontHeadLight)
                {
                    newTrainHeadlight = locomotive.Headlight[1];
                    newCarIsReversed = false;
                }
                else
                    if (Car.RearHeadLight)
                    {
                        newTrainHeadlight = locomotive.Headlight[2];
                        newCarIsReversed = true;
                    }
            }            

            // Ovládání reflektorů u vozů bez UnitSide reflektorů
            #region Poziční kužely světla k reflektorům
            if (Car.NoUnitSideCar || (Car.Train != null && (Car.Train.TrainType == Train.TRAINTYPE.AI || Car.Train.Simulator.PlayerTrainInAutopilotMode)))
            { 
                Car.LightFrontLW = false; Car.LightFrontRW = false; Car.LightRearLW = false; Car.LightRearRW = false;
                Car.LightFrontLR = false; Car.LightFrontRR = false; Car.LightRearLR = false; Car.LightRearRR = false;

                // Vozy
                if (!(Car is MSTSLocomotive))
                {
                    newTrainHeadlight = 7;
                    if (newCarIsFirst)
                    {
                        if (!newCarIsReversed)
                        {
                            Car.LightFrontLW = true;
                            Car.LightFrontRW = true;
                        }
                        else
                        {
                            Car.LightRearLW = true;
                            Car.LightRearRW = true;
                        }
                    }

                    if (newCarIsLast)
                    {
                        if (newCarIsReversed)
                        {
                            Car.LightFrontLR = true;
                            Car.LightFrontRR = true;
                        }
                        else
                        {
                            Car.LightRearLR = true;
                            Car.LightRearRR = true;
                        }
                    }
                }

                // Lokomotivy
                if (Car is MSTSLocomotive)
                {
                    if (Car.Train != null && Car.Train.IsActualPlayerTrain)
                    {                        
                        if (newCarIsFirst && !newCarIsLast)
                        {
                            if (!newCarIsReversed)
                            {
                                Car.LightFrontLW = true;
                                Car.LightFrontRW = true;
                            }
                            else
                            {
                                Car.LightRearLW = true;
                                Car.LightRearRW = true;
                            }
                        }

                        if (newCarIsLast && !newCarIsFirst)
                        {
                            if (!newCarIsReversed)
                            {
                                if (newCarCoupledFront)
                                {
                                    Car.LightRearLR = true;
                                    Car.LightRearRR = true;
                                }
                                if (newCarCoupledRear)
                                {
                                    Car.LightFrontLR = true;
                                    Car.LightFrontRR = true;
                                }
                            }
                            else
                            {
                                if (newCarCoupledFront)
                                {
                                    Car.LightFrontLR = true;
                                    Car.LightFrontRR = true;
                                }
                                if (newCarCoupledRear)
                                {
                                    Car.LightRearLR = true;
                                    Car.LightRearRR = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (newCarIsFirst && !newCarIsLast)
                        {
                            if (!newCarIsReversed)
                            {
                                Car.LightFrontLW = true;
                                Car.LightFrontRW = true;
                            }
                            else
                            {
                                Car.LightRearLW = true;
                                Car.LightRearRW = true;
                            }
                        }

                        if (newCarIsLast && !newCarIsFirst)
                        {
                            if (newCarIsReversed)
                            {
                                Car.LightFrontLR = true;
                                Car.LightFrontRR = true;
                            }
                            else
                            {
                                Car.LightRearLR = true;
                                Car.LightRearRR = true;
                            }
                        }
                    }

                    // Sólo
                    if (newTrainHeadlight > 0 && !Car.CarIsShunting)
                    {
                        if (newCarIsFirst && newCarIsLast)
                        {
                            if (!newCarIsReversed)
                            {
                                Car.LightFrontLW = true;
                                Car.LightFrontRW = true;
                                Car.LightRearLR = true;
                                Car.LightRearRR = true;
                            }
                            else
                            {
                                Car.LightRearLW = true;
                                Car.LightRearRW = true;
                                Car.LightFrontLR = true;
                                Car.LightFrontRR = true;
                            }
                        }
                    }
                    else
                    {
                        if (newCarIsFirst && newCarIsLast)
                        {
                            if (!newCarIsReversed)
                            {
                                Car.LightFrontLW = true;
                                Car.LightFrontRW = true;
                                Car.LightRearLW = true;
                                Car.LightRearRW = true;
                            }
                            else
                            {
                                Car.LightRearLW = true;
                                Car.LightRearRW = true;
                                Car.LightFrontLW = true;
                                Car.LightFrontRW = true;
                            }
                        }
                    }
                }
            }
            #endregion 

            var newCarLightFrontLW = Car.LightFrontLW;
            var newCarLightFrontRW = Car.LightFrontRW;
            var newCarLightRearLW = Car.LightRearLW;
            var newCarLightRearRW = Car.LightRearRW;
            var newCarLightFrontLR = Car.LightFrontLR;
            var newCarLightFrontRR = Car.LightFrontRR;
            var newCarLightRearLR = Car.LightRearLR;
            var newCarLightRearRR = Car.LightRearRR;
            var newCarFrontHeadLight = Car.FrontHeadLight;
            var newCarRearHeadLight = Car.RearHeadLight;
            var newTrainHeadlightFront = Car.Train != null && Car is MSTSLocomotive ? Car.Headlight[1] : 0;
            var newTrainHeadlightRear = Car.Train != null && Car is MSTSLocomotive ? Car.Headlight[2] : 0;                        
            

            if (LightCycle < 1 && Car.Train != null && (Car.Train.TrainType == Train.TRAINTYPE.AI || Car.Train.Simulator.PlayerTrainInAutopilotMode))
            {
                LightCycle++;
                return true;
            }

            // Dovolí zapnout reflektor i pokud má před sebou vozy
            if (locomotive != null && (Car as MSTSLocomotive) == Car.Train.LeadLocomotive && newCarIsLast)
            {
                newCarIsLast = true;
                newCarIsFirst = true;
            }

            // Povolí kužel reflektoru v tunelu
            if (Viewer.Simulator.PlayerCarIsInTunnel)
            {
                newIsDay = false;
            }

            #region AI
            // Světla pro AI
            if (Car.Train != null && (Car.Train.TrainType == Train.TRAINTYPE.AI || Car.Train.Simulator.PlayerTrainInAutopilotMode))
            {
                // AI posunuje
                if (Car.CarIsShunting && newCarIsFirst && newCarIsLast)
                {
                    newTrainHeadlight = 0;
                    TrainHeadlight = 1;                    
                }

                // AI vyčkává na místě
                if (Car.CarIsWaiting)
                {
                    newTrainHeadlight = 7;                    
                    TrainHeadlight = 1;                    
                }                

                // Jízda v noci nebo za mlhy
                if (!newIsDay || Viewer.Simulator.Weather.FogDistance < 1000.0f)
                {
                    newTrainHeadlight = 2;
                    newIsDay = false;

                    if (Math.Abs(Car.Train.SpeedMpS) < 50.0 / 3.6f)
                    {
                        newTrainHeadlight = 1;                        
                    }

                    if (Car.CarIsShunting && newCarIsFirst && newCarIsLast)
                    {
                        newTrainHeadlight = 0;
                    }

                    // AI stojí
                    if (Math.Abs(Car.Train.SpeedMpS) < 0.01f)
                    {
                        newTrainHeadlight = 7;                        
                    }

                    TrainHeadlight = 3;
                }
                else
                {
                    // Normální jízda
                    if (!Car.CarIsShunting && !Car.CarIsWaiting)
                    {
                        newTrainHeadlight = 1;
                        TrainHeadlight = 0;                        
                    }
                }

                if ((Car is MSTSLocomotive) && (Car as MSTSLocomotive).OtherTrainFlash)
                {
                    if ((Car as MSTSLocomotive).OtherTrainFlashOn)
                    {
                        newTrainHeadlight = 2;
                        newIsDay = false;
                        TrainHeadlight = 0;                        
                    }
                    else
                    {
                        newTrainHeadlight = 1;
                        newIsDay = false;
                        TrainHeadlight = 0;                        
                    }
                }
                
                // Vlaky bez tlumeného reflektoru
                if (newTrainHeadlight == 1 && !Car.Train.LightDimFound) 
                {
                    //newTrainHeadlight = 2; 
                }
            }
            #endregion

            if (
                (TrainHeadlight != newTrainHeadlight) ||
                (CarIsReversed != newCarIsReversed) ||
                (CarIsFirst != newCarIsFirst) ||
                (CarIsLast != newCarIsLast) ||
                (Penalty != newPenalty) ||
                (CarIsPlayer != newCarIsPlayer) ||
                (CarInService != newCarInService) ||
                (IsDay != newIsDay) ||
                (Weather != newWeather) ||
                (CarCoupledFront != newCarCoupledFront) ||
                (CarCoupledRear != newCarCoupledRear) ||                
                (CarLightFrontLW != newCarLightFrontLW) ||
                (CarLightFrontRW != newCarLightFrontRW) ||
                (CarLightRearLW != newCarLightRearLW) ||
                (CarLightRearRW != newCarLightRearRW) ||
                (CarLightFrontLR != newCarLightFrontLR) ||
                (CarLightFrontRR != newCarLightFrontRR) ||
                (CarLightRearLR != newCarLightRearLR) ||
                (CarLightRearRR != newCarLightRearRR) ||
                (CarFrontHeadLight != newCarFrontHeadLight) ||
                (CarRearHeadLight != newCarRearHeadLight) ||
                (TrainHeadlightFront != newTrainHeadlightFront) ||
                (TrainHeadlightRear != newTrainHeadlightRear) 
                )
            {
                TrainHeadlight = newTrainHeadlight;
                CarIsReversed = newCarIsReversed;
                CarIsFirst = newCarIsFirst;
                CarIsLast = newCarIsLast;
                Penalty = newPenalty;
                CarIsPlayer = newCarIsPlayer;
                CarInService = newCarInService;
                IsDay = newIsDay;
                Weather = newWeather;
                CarCoupledFront = newCarCoupledFront;
                CarCoupledRear = newCarCoupledRear;                
                CarLightFrontLW = newCarLightFrontLW;
                CarLightFrontRW = newCarLightFrontRW;
                CarLightRearLW = newCarLightRearLW;
                CarLightRearRW = newCarLightRearRW;
                CarLightFrontLR = newCarLightFrontLR;
                CarLightFrontRR = newCarLightFrontRR;
                CarLightRearLR = newCarLightRearLR;
                CarLightRearRR = newCarLightRearRR;
                CarFrontHeadLight = newCarFrontHeadLight;
                CarRearHeadLight = newCarRearHeadLight;
                TrainHeadlightFront = newTrainHeadlightFront;
                TrainHeadlightRear = newTrainHeadlightRear;
    
                return true;
            }
            return false;
        }
    }

    public abstract class LightPrimitive : RenderPrimitive
    {
        public Light Light;
        public bool Enabled;
        public Vector2 Fade;
        public bool FadeIn;
        public bool FadeOut;
        protected float FadeTime;
        protected int State;
        protected int StateCount;
        protected float StateTime;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightPrimitive(Light light)
        {
            Light = light;
            StateCount = Light.Cycle ? 2 * Light.States.Count - 2 : Light.States.Count;
            if (StateCount == 0)
                StateCount = 1;
            UpdateStates(State, (State + 1) % StateCount);            
        }

        protected void SetUpTransitions(Action<int, int, int> transitionHandler)
        {
#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
            Console.WriteLine("LightPrimitive transitions:");
#endif
            if (Light.Cycle)
            {
                for (var i = 0; i < Light.States.Count - 1; i++)
                    transitionHandler(i, i, i + 1);
                for (var i = Light.States.Count - 1; i > 0; i--)
                    transitionHandler(Light.States.Count * 2 - 1 - i, i, i - 1);
            }
            else
            {
                for (var i = 0; i < Light.States.Count; i++)
                    transitionHandler(i, i, (i + 1) % Light.States.Count);
            }
#if DEBUG_LIGHT_TRANSITIONS
            Console.WriteLine();
#endif
        }

        int LightCycle = 1;
        internal void UpdateState(LightViewer lightViewer)
        {
            var oldEnabled = Enabled;

            if (LightCycle == 0 && (Program.Viewer.PlayerLocomotive as MSTSLocomotive) == Program.Viewer.PlayerLocomotive.Train.LeadLocomotive)
            {
                LightCycle++;
                Enabled = false;
            }
            else
                Enabled = true;

            if (Light.Headlight != LightHeadlightCondition.Ignore)
            {                                
                if (Light.Headlight == LightHeadlightCondition.Off)
                    Enabled &= lightViewer.TrainHeadlight == 0;                
                else if (Light.Headlight == LightHeadlightCondition.Dim)
                    Enabled &= lightViewer.TrainHeadlight == 1;
                else if (Light.Headlight == LightHeadlightCondition.Bright)
                    Enabled &= lightViewer.TrainHeadlight == 2;                
                else if (Light.Headlight == LightHeadlightCondition.DLight)
                    Enabled &= lightViewer.TrainHeadlight == 7 || lightViewer.TrainHeadlight == 1 || lightViewer.TrainHeadlight == 2;
                else if (Light.Headlight == LightHeadlightCondition.DimBright)
                    Enabled &= lightViewer.TrainHeadlight >= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffDim)
                    Enabled &= lightViewer.TrainHeadlight <= 1;
                else if (Light.Headlight == LightHeadlightCondition.OffBright)
                    Enabled &= lightViewer.TrainHeadlight != 1;                
                else
                    Enabled &= false;

                
            }
            if (Light.Unit != LightUnitCondition.Ignore)
            {
                if (Light.Unit == LightUnitCondition.Middle)
                    Enabled &= !lightViewer.CarIsFirst && !lightViewer.CarIsLast;
                else if (Light.Unit == LightUnitCondition.First)
                    Enabled &= lightViewer.CarIsFirst && (!lightViewer.CarIsReversed || lightViewer.TrainHeadlight == 0);
                else if (Light.Unit == LightUnitCondition.Last)
                    Enabled &= lightViewer.CarIsLast && !lightViewer.CarIsReversed && lightViewer.TrainHeadlight != 0;
                else if (Light.Unit == LightUnitCondition.LastRev)
                    Enabled &= lightViewer.CarIsLast && lightViewer.CarIsReversed && lightViewer.TrainHeadlight != 0;
                else if (Light.Unit == LightUnitCondition.FirstRev)
                    Enabled &= lightViewer.CarIsFirst && (lightViewer.CarIsReversed || lightViewer.TrainHeadlight == 0);                
                else
                    Enabled &= false;
                
            }
            if (Light.Penalty != LightPenaltyCondition.Ignore)
            {
                if (Light.Penalty == LightPenaltyCondition.No)
                    Enabled &= !lightViewer.Penalty;
                else if (Light.Penalty == LightPenaltyCondition.Yes)
                    Enabled &= lightViewer.Penalty;
                else
                    Enabled &= false;
            }
            if (Light.Control != LightControlCondition.Ignore)
            {
                if (Light.Control == LightControlCondition.AI)
                    Enabled &= !lightViewer.CarIsPlayer;
                else if (Light.Control == LightControlCondition.Player)
                    Enabled &= (lightViewer.CarIsPlayer || !lightViewer.CarIsPlayer); // AI jako hráč, aby se reflektory rozsvítily i na AI vlacích
                else
                    Enabled &= false;
            }
            if (Light.Service != LightServiceCondition.Ignore)
            {
                if (Light.Service == LightServiceCondition.No)
                    Enabled &= !lightViewer.CarInService;
                else if (Light.Service == LightServiceCondition.Yes)
                    Enabled &= lightViewer.CarInService;
                else
                    Enabled &= false;
            }
            if (Light.TimeOfDay != LightTimeOfDayCondition.Ignore)
            {
                if (Light.TimeOfDay == LightTimeOfDayCondition.Day)
                    Enabled &= lightViewer.IsDay;
                else if (Light.TimeOfDay == LightTimeOfDayCondition.Night)
                    Enabled &= !lightViewer.IsDay;
                else
                    Enabled &= false;
            }
            if (Light.Weather != LightWeatherCondition.Ignore)
            {
                if (Light.Weather == LightWeatherCondition.Clear)
                    Enabled &= lightViewer.Weather == WeatherType.Clear;
                else if (Light.Weather == LightWeatherCondition.Rain)
                    Enabled &= lightViewer.Weather == WeatherType.Rain;
                else if (Light.Weather == LightWeatherCondition.Snow)
                    Enabled &= lightViewer.Weather == WeatherType.Snow;
                else
                    Enabled &= false;
            }
            if (Light.Coupling != LightCouplingCondition.Ignore)
            {
                if (Light.Coupling == LightCouplingCondition.Front)
                    Enabled &= lightViewer.CarCoupledFront && !lightViewer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Rear)
                    Enabled &= !lightViewer.CarCoupledFront && lightViewer.CarCoupledRear;
                else if (Light.Coupling == LightCouplingCondition.Both)
                    Enabled &= lightViewer.CarCoupledFront && lightViewer.CarCoupledRear;
                else
                    Enabled &= false;
            }

            // Icik
            if (Light.UnitSide != LightHandleCondition.Ignore)
            {
                if (Light.UnitSide == LightHandleCondition.FrontLW)
                    Enabled &= lightViewer.CarLightFrontLW;
                else if (Light.UnitSide == LightHandleCondition.FrontRW)
                    Enabled &= lightViewer.CarLightFrontRW;
                else if (Light.UnitSide == LightHandleCondition.RearLW)
                    Enabled &= lightViewer.CarLightRearLW;
                else if (Light.UnitSide == LightHandleCondition.RearRW)
                    Enabled &= lightViewer.CarLightRearRW;
                else if (Light.UnitSide == LightHandleCondition.FrontLR)
                    Enabled &= lightViewer.CarLightFrontLR;
                else if (Light.UnitSide == LightHandleCondition.FrontRR)
                    Enabled &= lightViewer.CarLightFrontRR;
                else if (Light.UnitSide == LightHandleCondition.RearLR)
                    Enabled &= lightViewer.CarLightRearLR;
                else if (Light.UnitSide == LightHandleCondition.RearRR)
                    Enabled &= lightViewer.CarLightRearRR;
                else if (Light.UnitSide == LightHandleCondition.FrontHeadLight)
                    Enabled &= lightViewer.CarFrontHeadLight;
                else if (Light.UnitSide == LightHandleCondition.RearHeadLight)
                    Enabled &= lightViewer.CarRearHeadLight;                
                else if (Light.UnitSide == LightHandleCondition.FrontW)
                    Enabled &= (lightViewer.CarLightFrontLW || lightViewer.CarLightFrontRW);
                else if (Light.UnitSide == LightHandleCondition.RearW)
                    Enabled &= (lightViewer.CarLightRearLW || lightViewer.CarLightRearRW);
                else if (Light.UnitSide == LightHandleCondition.FrontR)
                    Enabled &= (lightViewer.CarLightFrontLR || lightViewer.CarLightFrontRR);
                else if (Light.UnitSide == LightHandleCondition.RearR)
                    Enabled &= (lightViewer.CarLightRearLR || lightViewer.CarLightRearRR);
                else
                    Enabled &= false;
            }

            if (Light.HeadlightFront != LightHeadlightCondition.Ignore)
            {
                if (Light.HeadlightFront == LightHeadlightCondition.Off)
                    Enabled &= lightViewer.TrainHeadlightFront == 0;
                else if (Light.HeadlightFront == LightHeadlightCondition.Dim)
                    Enabled &= lightViewer.TrainHeadlightFront == 1;
                else if (Light.HeadlightFront == LightHeadlightCondition.Bright)
                    Enabled &= lightViewer.TrainHeadlightFront == 2;
                else if (Light.HeadlightFront == LightHeadlightCondition.DLight)
                    Enabled &= lightViewer.TrainHeadlightFront == 7 || lightViewer.TrainHeadlight == 1 || lightViewer.TrainHeadlight == 2;
                else if (Light.HeadlightFront == LightHeadlightCondition.DimBright)
                    Enabled &= lightViewer.TrainHeadlightFront >= 1;
                else if (Light.HeadlightFront == LightHeadlightCondition.OffDim)
                    Enabled &= lightViewer.TrainHeadlightFront <= 1;
                else if (Light.HeadlightFront == LightHeadlightCondition.OffBright)
                    Enabled &= lightViewer.TrainHeadlightFront != 1;
                else
                    Enabled &= false;
            }
            if (Light.HeadlightRear != LightHeadlightCondition.Ignore)
            {
                if (Light.HeadlightRear == LightHeadlightCondition.Off)
                    Enabled &= lightViewer.TrainHeadlightRear == 0;
                else if (Light.HeadlightRear == LightHeadlightCondition.Dim)
                    Enabled &= lightViewer.TrainHeadlightRear == 1;
                else if (Light.HeadlightRear == LightHeadlightCondition.Bright)
                    Enabled &= lightViewer.TrainHeadlightRear == 2;
                else if (Light.HeadlightRear == LightHeadlightCondition.DLight)
                    Enabled &= lightViewer.TrainHeadlightRear == 7 || lightViewer.TrainHeadlight == 1 || lightViewer.TrainHeadlight == 2;
                else if (Light.HeadlightRear == LightHeadlightCondition.DimBright)
                    Enabled &= lightViewer.TrainHeadlightRear >= 1;
                else if (Light.HeadlightRear == LightHeadlightCondition.OffDim)
                    Enabled &= lightViewer.TrainHeadlightRear <= 1;
                else if (Light.HeadlightRear == LightHeadlightCondition.OffBright)
                    Enabled &= lightViewer.TrainHeadlightRear != 1;
                else
                    Enabled &= false;
            }

            if (oldEnabled != Enabled)
            {
                FadeIn = Enabled;
                FadeOut = !Enabled;
                FadeTime = 0;
            }

#if DEBUG_LIGHT_STATES
            Console.WriteLine(LightViewer.PrimitiveStateFormat, Light.Index, Enabled, Light.Type, Light.Headlight, Light.Unit, Light.Penalty, Light.Control, Light.Service, Light.TimeOfDay, Light.Weather, Light.Coupling);
#endif
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (StateCount > 1)
            {
                StateTime += elapsedTime.ClockSeconds;
                if (StateTime >= Light.States[State % Light.States.Count].Duration)
                {
                    StateTime -= Light.States[State % Light.States.Count].Duration;
                    State = (State + 1) % StateCount;
                    UpdateStates(State, (State + 1) % StateCount);
                    Fade.Y = 0;
                }
                if (Light.States[State % Light.States.Count].Transition)
                    Fade.Y = StateTime / Light.States[State % Light.States.Count].Duration;
            }
            if (Light.FadeIn <= 0) Light.FadeIn = 0.01f;
            if (FadeIn)
            {
                FadeTime += elapsedTime.ClockSeconds;
                Fade.X = FadeTime / Light.FadeIn;
                if (Fade.X > 1)
                {
                    FadeIn = false;
                    Fade.X = 1;
                }
            }
            else if (FadeOut)
            {
                FadeTime += elapsedTime.ClockSeconds;
                Fade.X = 1 - FadeTime / Light.FadeIn;
                if (Fade.X < 0)
                {
                    FadeOut = false;
                    Fade.X = 0;
                }
            }
        }

        protected virtual void UpdateStates(int stateIndex1, int stateIndex2)
        {
        }
    }

    public class LightGlowPrimitive : LightPrimitive
    {
        static VertexDeclaration VertexDeclaration;
        VertexBuffer VertexBuffer;
        static IndexBuffer IndexBuffer;
        public Material LightGlowMaterial;

        public LightGlowPrimitive(LightViewer lightViewer, RenderProcess renderProcess, Light light)
            : base(light)
        {
            Debug.Assert(light.Type == LightType.Glow, "LightGlowPrimitive is only for LightType.Glow lights.");

            if (VertexDeclaration == null)
                VertexDeclaration = new VertexDeclaration(LightGlowVertex.SizeInBytes, LightGlowVertex.VertexElements);
            if (VertexBuffer == null)
            {
                var vertexData = new LightGlowVertex[6 * StateCount];
                SetUpTransitions((state, stateIndex1, stateIndex2) =>
                {
                    var state1 = Light.States[stateIndex1];
                    var state2 = Light.States[stateIndex2];

#if DEBUG_LIGHT_TRANSITIONS
                    Console.WriteLine("    Transition {0} is from state {1} to state {2} over {3:F1}s", state, stateIndex1, stateIndex2, state1.Duration);
#endif

                    // FIXME: Is conversion of "azimuth" to a normal right?
                    // Icik
                    var position1 = state1.Position; position1.Z = (position1.Z + (position1.Z / Math.Abs(position1.Z) * 0.008f)) * -1;
                    var normal1 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state1.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state1.Azimuth.Y)));
                    var color1 = new Color() { PackedValue = state1.Color }.ToVector4();
                    // Icik
                    var position2 = state2.Position; position2.Z = (position2.Z + (position2.Z / Math.Abs(position2.Z) * 0.008f)) * -1;
                    var normal2 = Vector3.Transform(Vector3.Transform(-Vector3.UnitZ, Matrix.CreateRotationX(MathHelper.ToRadians(-state2.Elevation.Y))), Matrix.CreateRotationY(MathHelper.ToRadians(-state2.Azimuth.Y)));
                    var color2 = new Color() { PackedValue = state2.Color }.ToVector4();

                    vertexData[6 * state + 0] = new LightGlowVertex(new Vector2(1, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 1] = new LightGlowVertex(new Vector2(0, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 2] = new LightGlowVertex(new Vector2(1, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 3] = new LightGlowVertex(new Vector2(1, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 4] = new LightGlowVertex(new Vector2(0, 1), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                    vertexData[6 * state + 5] = new LightGlowVertex(new Vector2(0, 0), position1, position2, normal1, normal2, color1, color2, state1.Radius, state2.Radius);
                });
                VertexBuffer = new VertexBuffer(renderProcess.GraphicsDevice, VertexDeclaration, vertexData.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(vertexData);
            }
            if (IndexBuffer == null)
            {
                var indexData = new short[] {
                    0, 1, 2, 3, 4, 5
                };
                IndexBuffer = new IndexBuffer(renderProcess.GraphicsDevice, typeof(short), indexData.Length, BufferUsage.WriteOnly);
                IndexBuffer.SetData(indexData);
            }

            UpdateState(lightViewer);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 6 * State, startIndex: 0, primitiveCount: 2);
        }
    }

    struct LightGlowVertex
    {
        public Vector3 PositionO;
        public Vector3 PositionT;
        public Vector3 NormalO;
        public Vector3 NormalT;
        public Vector4 ColorO;
        public Vector4 ColorT;
        public Vector2 TexCoords;
        public float RadiusO;
        public float RadiusT;

        public LightGlowVertex(Vector2 texCoords, Vector3 position1, Vector3 position2, Vector3 normal1, Vector3 normal2, Vector4 color1, Vector4 color2, float radius1, float radius2)
        {
            PositionO = position1;
            PositionT = position2;
            NormalO = normal1;
            NormalT = normal2;
            ColorO = color1;
            ColorT = color2;
            TexCoords = texCoords;
            RadiusO = radius1;
            RadiusT = radius2;
        }

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(sizeof(float) * 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * (3), VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
            new VertexElement(sizeof(float) * (3 + 3), VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(sizeof(float) * (3 + 3 + 3), VertexElementFormat.Vector3, VertexElementUsage.Normal, 1),
            new VertexElement(sizeof(float) * (3 + 3 + 3 + 3), VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(sizeof(float) * (3 + 3 + 3 + 3 + 4), VertexElementFormat.Vector4, VertexElementUsage.Color, 1),
            new VertexElement(sizeof(float) * (3 + 3 + 3 + 3 + 4 + 4), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
        };

        public static int SizeInBytes = sizeof(float) * (3 + 3 + 3 + 3 + 4 + 4 + 4);
    }

    public class LightConePrimitive : LightPrimitive
    {        
        public LightConePrimitive(LightViewer lightViewer, RenderProcess renderProcess, Light light)
            : base(light)
        {
        }        

        public override void Draw(GraphicsDevice graphicsDevice)
        {            
        }

        public Vector3 Position1, Position2, Direction1, Direction2;
        public float Angle1, Angle2, Radius1, Radius2, Distance1, Distance2;
        public Vector4 Color1, Color2;

        protected override void UpdateStates(int stateIndex1, int stateIndex2)
        {
            var state1 = Light.States[stateIndex1];
            var state2 = Light.States[stateIndex2];

            LightViewer.CalculateLightCone(state1, out Position1, out Direction1, out Angle1, out Radius1, out Distance1, out Color1);
            LightViewer.CalculateLightCone(state2, out Position2, out Direction2, out Angle2, out Radius2, out Distance2, out Color2);
        }
    }

    struct LightConeVertex
    {
        public Vector3 PositionO;
        public Vector3 PositionT;
        public Vector4 ColorO;
        public Vector4 ColorT;

        public LightConeVertex(Vector3 position1, Vector3 position2, Vector4 color1, Vector4 color2)
        {
            PositionO = position1;
            PositionT = position2;
            ColorO = color1;
            ColorT = color2;
        }

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(sizeof(float) * 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * (3), VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
            new VertexElement(sizeof(float) * (3 + 3), VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
            new VertexElement(sizeof(float) * (3 + 3 + 4), VertexElementFormat.Vector4, VertexElementUsage.Color, 1),
        };

        public static int SizeInBytes = sizeof(float) * (3 + 3 + 4 + 4);
    }

    public class LightGlowMaterial : Material
    {
        readonly Texture2D LightGlowTexture;
        readonly Texture2D BulbTexture; // Záložní textura pro slití světla

        public LightGlowMaterial(Viewer viewer, string textureName)
            : base(viewer, textureName)
        {
            // TODO: This should happen on the loader thread.
            LightGlowTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, textureName);
            BulbTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "..\\Content\\FX\\Star.png"));
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {            
            var shader = Viewer.MaterialManager.LightGlowShader;
            shader.CurrentTechnique = shader.Techniques["LightGlow"];
            shader.LightGlowTexture = LightGlowTexture;           
            
            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.LightGlowShader;

            // Nastavení záložní textury do druhého slotu
            shader.LightGlowTexture2 = BulbTexture;
            shader.TextureBlend = 0.0f;

            foreach (var pass in shader.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAViewMatrix * Viewer.Camera.XnaProjection;
                    shader.SetMatrix(ref wvp);
                    shader.SetFade(((LightPrimitive)item.RenderPrimitive).Fade);

                    // Matice transformující objekt přímo do prostoru kamery (View Space)
                    Matrix worldView = item.XNAMatrix * XNAViewMatrix;

                    // Kamera je ve View Space v bodě (0,0,0), takže délka vektoru pozice = reálná vzdálenost od kamery
                    float distance = worldView.Translation.Length();

                    // 2. Definovat násobič velikosti rádiusu (např. základ 1.0 + nárůst se vzdáleností)
                    // Lze nastavit min/max limity pomocí MathHelper.Clamp
                    float minDistance = 40f;
                    float maxDistance = 100f;
                    float factor = MathHelper.Clamp((distance - minDistance) / (maxDistance - minDistance), 0f, 1f);

                    // Plynulý přechod textur
                    float fadeStart = 40f;
                    float fadeEnd = 60f;
                    float blendFactor = MathHelper.Clamp((distance - fadeStart) / (fadeEnd - fadeStart), 0f, 1f);
                    shader.TextureBlend = blendFactor;                    

                    // Rádius vzroste např. až na 3-násobek původní velikosti
                    float glowScale = MathHelper.Lerp(1.0f, 2.0f, factor);

                    if (Program.Viewer.IsDay)
                        glowScale = MathHelper.Lerp(1.0f, 1.25f, factor);

                    // 3. Předat hodnotu do shaderu
                    shader.SetGlowScale(glowScale * (1f + blendFactor));

                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending()
        {
            return true;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(LightGlowTexture);
            Viewer.TextureManager.Mark(BulbTexture);
            base.Mark();
        }
    }

    public class LightConeMaterial : Material
    {
        public LightConeMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }        
    }
}
