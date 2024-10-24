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

// Debug for Sound Variables
//#define DEBUG_WHEEL_ANIMATION 

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Simulation;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Viewer3D.RollingStock.SubSystems;
using ORTS.Common;
using ORTS.Common.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSWagonViewer : TrainCarViewer
    {
        protected PoseableShape TrainCarShape;
        protected AnimatedShape FreightShape;
        protected AnimatedShape InteriorShape;
        protected AnimatedShape FrontCouplerShape;
        protected AnimatedShape FrontCouplerOpenShape;
        protected AnimatedShape RearCouplerShape;
        protected AnimatedShape RearCouplerOpenShape;
        public static readonly Action Noop = () => { };
        /// <summary>
        /// Dictionary of built-in locomotive control keyboard commands, Action[] is in the order {KeyRelease, KeyPress}
        /// </summary>
        public Dictionary<UserCommand, Action[]> UserInputCommands = new Dictionary<UserCommand, Action[]>();

        // Wheels are rotated by hand instead of in the shape file.
        float WheelRotationR;        
        List<int> WheelPartIndexes = new List<int>();

        // Icik
        float[] LocoWheelRotationREP = new float[7];
        float[] LocoWheelRotationR = new float[7];        

        // Everything else is animated through the shape file.
        AnimatedPart RunningGear;
        AnimatedPart Pantograph1;
        AnimatedPart Pantograph2;
        AnimatedPart Pantograph3;
        AnimatedPart Pantograph4;
        AnimatedPart LeftDoor;
        AnimatedPart RightDoor;
        AnimatedPart Mirrors;
        protected AnimatedPart Wipers;
        protected AnimatedPart Bell;
        AnimatedPart UnloadingParts;

        // Icik
        AnimatedPart CoolingPlates_W;
        AnimatedPart CoolingPlates_O;

        public Dictionary<string, List<ParticleEmitterViewer>> ParticleDrawers = new Dictionary<string, List<ParticleEmitterViewer>>();

        protected MSTSWagon MSTSWagon { get { return (MSTSWagon)Car; } }


        // Create viewers for special steam/smoke effects on car
        List<ParticleEmitterViewer> HeatingHose = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> HeatingCompartmentSteamTrap = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> HeatingMainPipeSteamTrap = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> WaterScoop = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> WaterScoopReverse = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> TenderWaterOverflow = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> WagonSmoke = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> HeatingSteamBoiler = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> BearingHotBox = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> SteamBrake = new List<ParticleEmitterViewer>();

        // Create viewers for special steam effects on car
        List<ParticleEmitterViewer> WagonGenerator = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> DieselLocoGenerator = new List<ParticleEmitterViewer>();

        bool HasFirstPanto;
        int numBogie1, numBogie2, bogie1Axles, bogie2Axles = 0;
        int bogieMatrix1, bogieMatrix2 = 0;
        FreightAnimationsViewer FreightAnimations;

        public MSTSWagonViewer(Viewer viewer, MSTSWagon car)
            : base(viewer, car)
        {
            string steamTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\smokemain.ace";
            string dieselTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\dieselsmoke.ace";

            // Particle Drawers called in Wagon so that wagons can also have steam effects.
            ParticleDrawers = (
                from effect in MSTSWagon.EffectData
                select new KeyValuePair<string, List<ParticleEmitterViewer>>(effect.Key, new List<ParticleEmitterViewer>(
                    from data in effect.Value
                    select new ParticleEmitterViewer(viewer, data, car.WorldPosition)))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Initaialise particle viewers for special steam effects
            foreach (var emitter in ParticleDrawers)
            {

                // Exhaust for steam heating boiler
                if (emitter.Key.ToLowerInvariant() == "heatingsteamboilerfx")
                {
                    HeatingSteamBoiler.AddRange(emitter.Value);
                    // set flag to indicate that heating boiler is active on this car only - only sets first boiler steam effect found in the train
                    if (!car.IsTrainHeatingBoilerInitialised && !car.HeatingBoilerSet)
                    {
                        car.HeatingBoilerSet = true;
                        car.IsTrainHeatingBoilerInitialised = true;
                    }
                }

                foreach (var drawer in HeatingSteamBoiler)
                {
                    drawer.Initialize(dieselTexture);
                }

                // Exhaust for HEP/Power Generator
                if (emitter.Key.ToLowerInvariant() == "wagongeneratorfx")
                    WagonGenerator.AddRange(emitter.Value);

                foreach (var drawer in WagonGenerator)
                {
                    drawer.Initialize(dieselTexture);
                }

                // Smoke for wood/coal fire
                if (emitter.Key.ToLowerInvariant() == "wagonsmokefx")
                {
                    WagonSmoke.AddRange(emitter.Value);
                    // Icik
                    car.HasWagonSmoke = true;
                }

                foreach (var drawer in WagonSmoke)
                {
                    drawer.Initialize(steamTexture);
                }

                // Smoke for bearing hot box
                if (emitter.Key.ToLowerInvariant() == "bearinghotboxfx")
                    BearingHotBox.AddRange(emitter.Value);

                foreach (var drawer in BearingHotBox)
                {
                    drawer.Initialize(steamTexture);
                }

                // Steam leak in heating hose 

                if (emitter.Key.ToLowerInvariant() == "heatinghosefx")
                {
                    HeatingHose.AddRange(emitter.Value);
                    // Icik
                    car.HasWagonSteamHeatingElements = true;
                }

                foreach (var drawer in HeatingHose)
                {
                    drawer.Initialize(steamTexture);
                }

                // Steam leak in heating compartment steam trap

                if (emitter.Key.ToLowerInvariant() == "heatingcompartmentsteamtrapfx")
                    HeatingCompartmentSteamTrap.AddRange(emitter.Value);

                foreach (var drawer in HeatingCompartmentSteamTrap)
                {
                    drawer.Initialize(steamTexture);
                }

                // Steam leak in heating steam trap

                if (emitter.Key.ToLowerInvariant() == "heatingmainpipesteamtrapfx")
                    HeatingMainPipeSteamTrap.AddRange(emitter.Value);

                foreach (var drawer in HeatingMainPipeSteamTrap)
                {
                    drawer.Initialize(steamTexture);
                }

                // Water spray for when water scoop is in use (use steam effects for the time being) 
                // Forward motion
                if (emitter.Key.ToLowerInvariant() == "waterscoopfx")
                    WaterScoop.AddRange(emitter.Value);

                foreach (var drawer in WaterScoop)
                {
                    drawer.Initialize(steamTexture);
                }

                // Reverse motion

                if (emitter.Key.ToLowerInvariant() == "waterscoopreversefx")
                    WaterScoopReverse.AddRange(emitter.Value);

                foreach (var drawer in WaterScoopReverse)
                {
                    drawer.Initialize(steamTexture);
                }

                // Water overflow when tender is over full during water trough filling (use steam effects for the time being) 

                if (emitter.Key.ToLowerInvariant() == "tenderwateroverflowfx")
                    TenderWaterOverflow.AddRange(emitter.Value);

                foreach (var drawer in TenderWaterOverflow)
                {
                    drawer.Initialize(steamTexture);
                }

                if (emitter.Key.ToLowerInvariant() == "steambrakefx")
                    SteamBrake.AddRange(emitter.Value);

                foreach (var drawer in SteamBrake)
                {
                    drawer.Initialize(steamTexture);
                }

            }

            var wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";

            TrainCarShape = car.MainShapeFileName != string.Empty
                ? new PoseableShape(viewer, wagonFolderSlash + car.MainShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.ShadowCaster)
                : new PoseableShape(viewer, null, car.WorldPosition);

            // This insection initialises the MSTS style freight animation - can either be for a coal load, which will adjust with usage, or a static animation, such as additional shape.
            if (car.FreightShapeFileName != null)
            {

                car.HasFreightAnim = true;
                FreightShape = new AnimatedShape(viewer, wagonFolderSlash + car.FreightShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);

                // Reproducing MSTS "bug" of not allowing tender animation in case both minLevel and maxLevel are 0 or maxLevel <  minLevel 
                // Applies to both a standard tender locomotive or a tank locomotive (where coal load is on same "wagon" as the locomotive -  for the coal load on a tender or tank locomotive - in operation it will raise or lower with caol usage

                if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSSteamLocomotive)
                {

                    var NonTenderSteamLocomotive = MSTSWagon as MSTSSteamLocomotive;

                    if ((MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSLocomotive && (MSTSWagon.EngineType == TrainCar.EngineTypes.Steam && NonTenderSteamLocomotive.IsTenderRequired == 0.0)) && MSTSWagon.FreightAnimMaxLevelM != 0 && MSTSWagon.FreightAnimFlag > 0 && MSTSWagon.FreightAnimMaxLevelM > MSTSWagon.FreightAnimMinLevelM)
                    {
                        // Force allowing animation:
                        if (FreightShape.SharedShape.LodControls.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0 && FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy.Length > 0)
                            FreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[0] = 1;
                    }
                }
            }

            // Initialise Coupler shapes 
            if (car.FrontCouplerShapeFileName != null)
            {
                FrontCouplerShape = new AnimatedShape(viewer, wagonFolderSlash + car.FrontCouplerShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.FrontCouplerOpenShapeFileName != null)
            {
                FrontCouplerOpenShape = new AnimatedShape(viewer, wagonFolderSlash + car.FrontCouplerOpenShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.RearCouplerShapeFileName != null)
            {
                RearCouplerShape = new AnimatedShape(viewer, wagonFolderSlash + car.RearCouplerShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }

            if (car.RearCouplerOpenShapeFileName != null)
            {
                RearCouplerOpenShape = new AnimatedShape(viewer, wagonFolderSlash + car.RearCouplerOpenShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(car.WorldPosition), ShapeFlags.ShadowCaster);
            }


            if (car.InteriorShapeFileName != null)
                InteriorShape = new AnimatedShape(viewer, wagonFolderSlash + car.InteriorShapeFileName + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.Interior, 30.0f);

            RunningGear = new AnimatedPart(TrainCarShape);
            Pantograph1 = new AnimatedPart(TrainCarShape);
            Pantograph2 = new AnimatedPart(TrainCarShape);
            Pantograph3 = new AnimatedPart(TrainCarShape);
            Pantograph4 = new AnimatedPart(TrainCarShape);
            LeftDoor = new AnimatedPart(TrainCarShape);
            RightDoor = new AnimatedPart(TrainCarShape);
            Mirrors = new AnimatedPart(TrainCarShape);
            Wipers = new AnimatedPart(TrainCarShape);
            UnloadingParts = new AnimatedPart(TrainCarShape);
            Bell = new AnimatedPart(TrainCarShape);

            // Icik
            CoolingPlates_W = new AnimatedPart(TrainCarShape);
            CoolingPlates_O = new AnimatedPart(TrainCarShape);

            if (car.FreightAnimations != null)
                FreightAnimations = new FreightAnimationsViewer(viewer, car, wagonFolderSlash);

            LoadCarSounds(wagonFolderSlash);
            //if (!(MSTSWagon is MSTSLocomotive))
            //    LoadTrackSounds();
            Viewer.SoundProcess.AddSoundSource(this, new TrackSoundSource(MSTSWagon, Viewer));

            // Determine if it has first pantograph. So we can match unnamed panto parts correctly
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (TrainCarShape.SharedShape.MatrixNames[i].Contains('1'))
                {
                    if (TrainCarShape.SharedShape.MatrixNames[i].ToUpper().StartsWith("PANTO")) { HasFirstPanto = true; break; }
                }

            // Check bogies and wheels to find out what we have.
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
            {
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE1"))
                {
                    bogieMatrix1 = i;
                    numBogie1 += 1;
                }
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE2"))
                {
                    bogieMatrix2 = i;
                    numBogie2 += 1;
                }
                if (TrainCarShape.SharedShape.MatrixNames[i].Equals("BOGIE"))
                {
                    bogieMatrix1 = i;
                }
                // For now, the total axle count consisting of axles that are part of the bogie are being counted.
                if (TrainCarShape.SharedShape.MatrixNames[i].Contains("WHEELS"))
                    if (TrainCarShape.SharedShape.MatrixNames[i].Length == 8)
                    {
                        var tpmatrix = TrainCarShape.SharedShape.GetParentMatrix(i);
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS11") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS12") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS13") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS22") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS23") && tpmatrix == bogieMatrix1)
                            bogie1Axles += 1;

                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS11") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS12") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS13") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS21") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                        if (TrainCarShape.SharedShape.MatrixNames[i].Equals("WHEELS23") && tpmatrix == bogieMatrix2)
                            bogie2Axles += 1;
                    }
            }

            // Match up all the matrices with their parts.
            for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                if (TrainCarShape.Hierarchy[i] == -1)
                    MatchMatrixToPart(car, i, 0);

            car.SetUpWheels();

            // If we have two pantographs, 2 is the forwards pantograph, unlike when there's only one.
            if (!(car.Flipped ^ (car.Train.IsActualPlayerTrain && Viewer.PlayerLocomotive.Flipped)) && !Pantograph1.Empty() && !Pantograph2.Empty())
                AnimatedPart.Swap(ref Pantograph1, ref Pantograph2);

            Pantograph1.SetState(MSTSWagon.Pantographs[1].CommandUp);
            Pantograph2.SetState(MSTSWagon.Pantographs[2].CommandUp);
            if (MSTSWagon.Pantographs.List.Count > 2) Pantograph3.SetState(MSTSWagon.Pantographs[3].CommandUp);
            if (MSTSWagon.Pantographs.List.Count > 3) Pantograph4.SetState(MSTSWagon.Pantographs[4].CommandUp);
            LeftDoor.SetState(MSTSWagon.DoorLeftOpen);
            RightDoor.SetState(MSTSWagon.DoorRightOpen);
            Mirrors.SetState(MSTSWagon.MirrorOpen);
            UnloadingParts.SetState(MSTSWagon.UnloadingPartsOpen);

            // Icik
            if (Viewer.PlayerLocomotive as MSTSDieselLocomotive != null)
            {
                CoolingPlates_W.SetState(MSTSWagon.DoorLeftOpen || (Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].WaterTempCoolingRunning);
                CoolingPlates_O.SetState(MSTSWagon.DoorRightOpen || (Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].OilTempCoolingRunning);
            }

            InitializeUserInputCommands();
        }

        void MatchMatrixToPart(MSTSWagon car, int matrix, int bogieMatrix)
        {            
            var matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].ToUpper();
            // Gate all RunningGearPartIndexes on this!
            var matrixAnimated = TrainCarShape.SharedShape.Animations != null && TrainCarShape.SharedShape.Animations.Count > 0 && TrainCarShape.SharedShape.Animations[0].anim_nodes.Count > matrix && TrainCarShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0;
            if (matrixName.StartsWith("WHEELS") && (matrixName.Length == 7 || matrixName.Length == 8 || matrixName.Length == 9))
            {
                // Standard WHEELS length would be 8 to test for WHEELS11. Came across WHEELS tag that used a period(.) between the last 2 numbers, changing max length to 9.
                // Changing max length to 9 is not a problem since the initial WHEELS test will still be good.
                var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                //someone uses wheel to animate fans, thus check if the wheel is not too high (lower than 3m), will animate it as real wheel
                if (m.M42 < 3)
                {
                    var id = 0;
                    // Model makers are not following the standard rules, For example, one tender uses naming convention of wheels11/12 instead of using Wheels1,2,3 when not part of a bogie.
                    // The next 2 lines will sort out these axles.
                    var tmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    if (matrixName.Length == 8 && bogieMatrix == 0 && tmatrix == 0) // In this test, both tmatrix and bogieMatrix are 0 since these wheels are not part of a bogie.
                        matrixName = TrainCarShape.SharedShape.MatrixNames[matrix].Substring(0, 7); // Changing wheel name so that it reflects its actual use since it is not p
                    if (matrixName.Length == 8 || matrixName.Length == 9)
                        Int32.TryParse(matrixName.Substring(6, 1), out id);
                    if (matrixName.Length == 8 || matrixName.Length == 9 || !matrixAnimated)
                        WheelPartIndexes.Add(matrix);
                    else
                        RunningGear.AddMatrix(matrix);
                    var pmatrix = TrainCarShape.SharedShape.GetParentMatrix(matrix);
                    car.AddWheelSet(m.M43, id, pmatrix, matrixName.ToString(), bogie1Axles, bogie2Axles);
                }
                // Standard wheels are processed above, but wheels used as animated fans that are greater than 3m are processed here.
                else
                    RunningGear.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("BOGIE") && matrixName.Length <= 6) //BOGIE1 is valid, BOGIE11 is not, it is used by some modelers to indicate this is part of bogie1
            {
                if (matrixName.Length == 6)
                {
                    var id = 1;
                    Int32.TryParse(matrixName.Substring(5), out id);
                    var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.M43, matrix, id, matrixName.ToString(), numBogie1, numBogie2);
                    bogieMatrix = matrix; // Bogie matrix needs to be saved for test with axles.
                }
                else
                {
                    // Since the string content is BOGIE, Int32.TryParse(matrixName.Substring(5), out id) is not needed since its sole purpose is to
                    //  parse the string number from the string.
                    var id = 1;
                    var m = TrainCarShape.SharedShape.GetMatrixProduct(matrix);
                    car.AddBogie(m.M43, matrix, id, matrixName.ToString(), numBogie1, numBogie2);
                    bogieMatrix = matrix; // Bogie matrix needs to be saved for test with axles.
                }
                // Bogies contain wheels!
                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i, bogieMatrix);
            }
            else if (matrixName.StartsWith("WIPER")) // wipers
            {
                Wipers.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("DOOR")) // doors (left / right)
            {
                if (matrixName.StartsWith("DOOR_D") || matrixName.StartsWith("DOOR_E") || matrixName.StartsWith("DOOR_F"))
                    LeftDoor.AddMatrix(matrix);
                else if (matrixName.StartsWith("DOOR_A") || matrixName.StartsWith("DOOR_B") || matrixName.StartsWith("DOOR_C"))
                    RightDoor.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTOGRAPH")) //pantographs (1/2)
            {

                switch (matrixName)
                {
                    case "PANTOGRAPHBOTTOM1":
                    case "PANTOGRAPHBOTTOM1A":
                    case "PANTOGRAPHBOTTOM1B":
                    case "PANTOGRAPHMIDDLE1":
                    case "PANTOGRAPHMIDDLE1A":
                    case "PANTOGRAPHMIDDLE1B":
                    case "PANTOGRAPHTOP1":
                    case "PANTOGRAPHTOP1A":
                    case "PANTOGRAPHTOP1B":
                        Pantograph1.AddMatrix(matrix);
                        break;
                    case "PANTOGRAPHBOTTOM2":
                    case "PANTOGRAPHBOTTOM2A":
                    case "PANTOGRAPHBOTTOM2B":
                    case "PANTOGRAPHMIDDLE2":
                    case "PANTOGRAPHMIDDLE2A":
                    case "PANTOGRAPHMIDDLE2B":
                    case "PANTOGRAPHTOP2":
                    case "PANTOGRAPHTOP2A":
                    case "PANTOGRAPHTOP2B":
                        Pantograph2.AddMatrix(matrix);
                        break;
                    default://someone used other language
                        if (matrixName.Contains("1"))
                            Pantograph1.AddMatrix(matrix);
                        else if (matrixName.Contains("2"))
                            Pantograph2.AddMatrix(matrix);
                        else if (matrixName.Contains("3"))
                            Pantograph3.AddMatrix(matrix);
                        else if (matrixName.Contains("4"))
                            Pantograph4.AddMatrix(matrix);
                        else
                        {
                            if (HasFirstPanto) Pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                            else Pantograph2.AddMatrix(matrix);
                        }
                        break;
                }
            }
            else if (matrixName.StartsWith("MIRROR")) // mirrors
            {
                Mirrors.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("UNLOADINGPARTS")) // unloading parts
            {
                UnloadingParts.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("PANTO"))  // TODO, not sure why this is needed, see above!
            {
                Trace.TraceInformation("Pantograph matrix with unusual name {1} in shape {0}", TrainCarShape.SharedShape.FilePath, matrixName);
                if (matrixName.Contains("1"))
                    Pantograph1.AddMatrix(matrix);
                else if (matrixName.Contains("2"))
                    Pantograph2.AddMatrix(matrix);
                else if (matrixName.Contains("3"))
                    Pantograph3.AddMatrix(matrix);
                else if (matrixName.Contains("4"))
                    Pantograph4.AddMatrix(matrix);
                else
                {
                    if (HasFirstPanto) Pantograph1.AddMatrix(matrix); //some may have no first panto, will put it as panto 2
                    else Pantograph2.AddMatrix(matrix);
                }
            }
            else if (matrixName.StartsWith("ORTSBELL")) // wipers
            {
                Bell.AddMatrix(matrix);
            }
            // Icik
            else if (matrixName.StartsWith("LAMELA_W")) // Lamely na chlazení vody
            {
                CoolingPlates_W.AddMatrix(matrix);
            }
            else if (matrixName.StartsWith("LAMELA_O")) // Lamely na chlazení oleje
            {
                CoolingPlates_O.AddMatrix(matrix);
            }
            else
            {
                if (matrixAnimated && matrix != 0)
                    RunningGear.AddMatrix(matrix);

                for (var i = 0; i < TrainCarShape.Hierarchy.Length; i++)
                    if (TrainCarShape.Hierarchy[i] == matrix)
                        MatchMatrixToPart(car, i, 0);
            }
        }

        public override void InitializeUserInputCommands()
        {
            MSTSLocomotive locomotive = null;
            if (MSTSWagon is MSTSLocomotive) locomotive = MSTSWagon as MSTSLocomotive;
            UserInputCommands.Add(UserCommand.ControlPantograph1, new Action[] { Noop, () => new PantographCommand(Viewer.Log,
                locomotive != null && locomotive.UsingRearCab && MSTSWagon.Pantographs.List.Count > 1 ? 2 : 1, !MSTSWagon.Pantographs[locomotive != null && locomotive.UsingRearCab && MSTSWagon.Pantographs.List.Count > 1 ? 2 : 1].CommandUp) });
            UserInputCommands.Add(UserCommand.ControlPantograph2, new Action[] { Noop, () => new PantographCommand(Viewer.Log,
                locomotive != null && locomotive.UsingRearCab ? 1 : 2, !MSTSWagon.Pantographs[locomotive != null && locomotive.UsingRearCab ? 1 : 2].CommandUp) });
            if (MSTSWagon.Pantographs.List.Count > 2) UserInputCommands.Add(UserCommand.ControlPantograph3, new Action[] { Noop, () => new PantographCommand(Viewer.Log,
                locomotive != null && locomotive.UsingRearCab && locomotive.Pantographs.List.Count > 3 ? 4 : 3, !MSTSWagon.Pantographs[locomotive != null && locomotive.UsingRearCab && locomotive.Pantographs.List.Count > 3 ? 4 : 3].CommandUp) });
            if (MSTSWagon.Pantographs.List.Count > 3) UserInputCommands.Add(UserCommand.ControlPantograph4, new Action[] { Noop, () => new PantographCommand(Viewer.Log,
                locomotive != null && locomotive.UsingRearCab ? 3 : 4, !MSTSWagon.Pantographs[locomotive != null && locomotive.UsingRearCab ? 3 : 4].CommandUp) });
            UserInputCommands.Add(UserCommand.ControlDoorLeft, new Action[] { Noop, () => new ToggleDoorsLeftCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDoorRight, new Action[] { Noop, () => new ToggleDoorsRightCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlMirror, new Action[] { Noop, () => new ToggleMirrorsCommand(Viewer.Log) });
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            foreach (var command in UserInputCommands.Keys)
                if (UserInput.IsPressed(command)) UserInputCommands[command][1]();
                else if (UserInput.IsReleased(command)) UserInputCommands[command][0]();
        }

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            Viewer.Simulator.MSTSWagon = Car;
            Pantograph1.UpdateStatePanto1(MSTSWagon.Pantographs[1].CommandUp, elapsedTime);
            Pantograph2.UpdateStatePanto2(MSTSWagon.Pantographs[2].CommandUp, elapsedTime);
            if (MSTSWagon.Pantographs.List.Count > 2) Pantograph3.UpdateStatePanto3(MSTSWagon.Pantographs[3].CommandUp, elapsedTime);
            if (MSTSWagon.Pantographs.List.Count > 3) Pantograph4.UpdateStatePanto4(MSTSWagon.Pantographs[4].CommandUp, elapsedTime);
            LeftDoor.UpdateState(MSTSWagon.DoorLeftOpen, elapsedTime);
            RightDoor.UpdateState(MSTSWagon.DoorRightOpen, elapsedTime);
            Mirrors.UpdateState(MSTSWagon.MirrorOpen, elapsedTime);
            UnloadingParts.UpdateState(MSTSWagon.UnloadingPartsOpen, elapsedTime);

            // Icik
            if (Viewer.PlayerLocomotive as MSTSDieselLocomotive != null)
            {
                CoolingPlates_W.UpdateState(MSTSWagon.DoorLeftOpen || (Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].WaterTempCoolingRunning, elapsedTime);
                CoolingPlates_O.UpdateState(MSTSWagon.DoorRightOpen || (Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].OilTempCoolingRunning, elapsedTime);
            }

            UpdateAnimation(frame, elapsedTime);

            var car = Car as MSTSWagon;
            // Steam leak in heating hose
            foreach (var drawer in HeatingHose)
            {
                drawer.SetOutput(car.HeatingHoseSteamVelocityMpS, car.HeatingHoseSteamVolumeM3pS, car.HeatingHoseParticleDurationS);
            }

            // Steam leak in heating compartment steamtrap
            foreach (var drawer in HeatingCompartmentSteamTrap)
            {
                drawer.SetOutput(car.HeatingCompartmentSteamTrapVelocityMpS, car.HeatingCompartmentSteamTrapVolumeM3pS, car.HeatingCompartmentSteamTrapParticleDurationS);
            }

            // Steam leak in heating main pipe steamtrap
            foreach (var drawer in HeatingMainPipeSteamTrap)
            {
                drawer.SetOutput(car.HeatingMainPipeSteamTrapVelocityMpS, car.HeatingMainPipeSteamTrapVolumeM3pS, car.HeatingMainPipeSteamTrapDurationS);
            }

            // Heating Steam Boiler Exhaust
            foreach (var drawer in HeatingSteamBoiler)
            {
                drawer.SetOutput(car.HeatingSteamBoilerVolumeM3pS, car.HeatingSteamBoilerDurationS, car.HeatingSteamBoilerSteadyColor);
            }

            // Exhaust for HEP/Electrical Generator
            foreach (var drawer in WagonGenerator)
            {
                drawer.SetOutput(car.WagonGeneratorVolumeM3pS, car.WagonGeneratorDurationS, car.WagonGeneratorSteadyColor);
            }

            // Wagon fire smoke
            foreach (var drawer in WagonSmoke)
            {
                // Icik
                if (car.WagonHasStove && !car.BrakeSystem.HeatingIsOn)
                    car.WagonSmokeVolumeM3pS = 0;
                drawer.SetOutput(car.WagonSmokeVelocityMpS, car.WagonSmokeVolumeM3pS, car.WagonSmokeDurationS, car.WagonSmokeSteadyColor);
            }

            if (car.Train != null) // only process this visual feature if this is a valid car in the train
            {
                // Water spray for water scoop (uses steam effects currently) - Forward direction
                if (car.Direction == Direction.Forward)
                {
                    foreach (var drawer in WaterScoop)
                    {
                        drawer.SetOutput(car.WaterScoopWaterVelocityMpS, car.WaterScoopWaterVolumeM3pS, car.WaterScoopParticleDurationS);
                    }
                }
                // If travelling in reverse turn on rearward facing effect
                else if (car.Direction == Direction.Reverse)
                {
                    foreach (var drawer in WaterScoopReverse)
                    {
                        drawer.SetOutput(car.WaterScoopWaterVelocityMpS, car.WaterScoopWaterVolumeM3pS, car.WaterScoopParticleDurationS);
                    }
                }
            }

            // Water overflow from tender (uses steam effects currently)
            foreach (var drawer in TenderWaterOverflow)
            {
                drawer.SetOutput(car.TenderWaterOverflowVelocityMpS, car.TenderWaterOverflowVolumeM3pS, car.TenderWaterOverflowParticleDurationS);
            }

            // Bearing Hot box smoke
            foreach (var drawer in BearingHotBox)
            {
                drawer.SetOutput(car.BearingHotBoxSmokeVelocityMpS, car.BearingHotBoxSmokeVolumeM3pS, car.BearingHotBoxSmokeDurationS, car.BearingHotBoxSmokeSteadyColor);
            }

            // Steam Brake effects
            foreach (var drawer in SteamBrake)
            {
                drawer.SetOutput(car.SteamBrakeLeaksVelocityMpS, car.SteamBrakeLeaksVolumeM3pS, car.SteamBrakeLeaksDurationS);
            }

            foreach (List<ParticleEmitterViewer> drawers in ParticleDrawers.Values)
                foreach (ParticleEmitterViewer drawer in drawers)
                    drawer.PrepareFrame(frame, elapsedTime);

        }

        float[] AxleWheelSpeedMpS = new float[7];
        int AxleNum = 0;
        private void UpdateAnimation(RenderFrame frame, ElapsedTime elapsedTime)
        {
            float distanceTravelledM = 0.0f; // Distance travelled by non-driven wheels
            float distanceTravelledDrivenM = 0.0f;  // Distance travelled by driven wheels
            //float AnimationWheelRadiusM = 0.0f; // Radius of non driven wheels
            //float AnimationDriveWheelRadiusM = 0.0f; // Radius of driven wheels
            float AnimationWheelRadiusM = MSTSWagon.WheelRadiusM; // Radius of non driven wheels
            float AnimationDriveWheelRadiusM = MSTSWagon.DriverWheelRadiusM; // Radius of driven wheels

            if (MSTSWagon.IsDriveable && MSTSWagon.Simulator.UseAdvancedAdhesion)
            {
                //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                //                                distanceTravelledM = MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;

                distanceTravelledM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
                if (Car.EngineType == Orts.Simulation.RollingStocks.TrainCar.EngineTypes.Steam) // Steam locomotive so set up different speeds for different driver and non-driver wheels
                {
                    //distanceTravelledM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
                    distanceTravelledDrivenM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.WheelSpeedSlipMpS * elapsedTime.ClockSeconds;
                    // Set values of wheel radius - assume that drive wheel and non driven wheel are different sizes
                    //AnimationWheelRadiusM = MSTSWagon.WheelRadiusM;
                    //AnimationDriveWheelRadiusM = MSTSWagon.DriverWheelRadiusM;
                }
                else  // Other driveable rolling stock - all wheels have same speed.
                {
                    //distanceTravelledM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
                    distanceTravelledDrivenM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
                    // Set values of wheel radius - assume that drive wheel and non driven wheel are same sizes
                    //AnimationWheelRadiusM = MSTSWagon.WheelRadiusM;
                    //AnimationDriveWheelRadiusM = MSTSWagon.WheelRadiusM;
                }
            }
            else // set values for simple adhesion
            {

                distanceTravelledM = ((MSTSWagon.IsDriveable && MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;
                //distanceTravelledDrivenM = ((MSTSWagon.IsDriveable && MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;
                // Set values of wheel radius - assume that drive wheel and non driven wheel are same sizes
                //if (Car.EngineType == Orts.Simulation.RollingStocks.TrainCar.EngineTypes.Steam) // set values for steam stock
                //{
                //    AnimationWheelRadiusM = MSTSWagon.WheelRadiusM;
                //    AnimationDriveWheelRadiusM = MSTSWagon.DriverWheelRadiusM;
                //}
                //else // set values for non-driveable stock, eg wagons, and driveable stock such as diesels, electric locomotives 
                //{
                //    AnimationWheelRadiusM = MSTSWagon.WheelRadiusM;
                //    AnimationDriveWheelRadiusM = MSTSWagon.WheelRadiusM;
                //}
                distanceTravelledDrivenM = distanceTravelledM;
            }

            if (Car.BrakeSkid) // if car wheels are skidding because of brakes locking wheels up then stop wheels rotating.
            {
                distanceTravelledM = 0.0f;
                distanceTravelledDrivenM = 0.0f;
            }

            // Running gear and drive wheel rotation (animation) in steam locomotives
            if (!RunningGear.Empty() && AnimationDriveWheelRadiusM > 0.001)
                RunningGear.UpdateLoop(distanceTravelledDrivenM / MathHelper.TwoPi / AnimationDriveWheelRadiusM);


            // Wheel rotation (animation) - for non-drive wheels in steam locomotives and all wheels in other stock
            if (WheelPartIndexes.Count > 0)
            {
                var wheelCircumferenceM = MathHelper.TwoPi * AnimationWheelRadiusM;
                var rotationalDistanceR = MathHelper.TwoPi * distanceTravelledM / wheelCircumferenceM;  // in radians
                WheelRotationR = MathHelper.WrapAngle(WheelRotationR - rotationalDistanceR);
                var wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                foreach (var iMatrix in WheelPartIndexes)
                {
                    // Icik
                    // Počítání rychlosti animace jednotlivých náprav 
                    if ((Car as MSTSLocomotive) != null && (Car as MSTSLocomotive).Train != null && (Car as MSTSLocomotive).IsPlayerTrain)
                    {
                        if ((Car as MSTSLocomotive).extendedPhysics != null)
                        {
                            if (AxleNum >= (Car as MSTSLocomotive).WagonNumAxles)
                            {
                                AxleNum = 0;
                            }
                            AxleNum++;

                            AxleWheelSpeedMpS[AxleNum] = MSTSWagon.Train.SpeedMpS;
                            for (int i = 1; i < 7; i++)
                            {
                                if ((Car as MSTSLocomotive).DriveAxleNumber[i] == AxleNum)
                                {
                                    AxleWheelSpeedMpS[1] = Math.Abs((Car as MSTSLocomotive).extendedPhysics.Undercarriages[0].Axles[0].WheelSpeedMpS);
                                    AxleWheelSpeedMpS[2] = Math.Abs((Car as MSTSLocomotive).extendedPhysics.Undercarriages[0].Axles[1].WheelSpeedMpS);
                                    AxleWheelSpeedMpS[3] = Math.Abs((Car as MSTSLocomotive).extendedPhysics.Undercarriages[1].Axles[0].WheelSpeedMpS);
                                    AxleWheelSpeedMpS[4] = Math.Abs((Car as MSTSLocomotive).extendedPhysics.Undercarriages[1].Axles[1].WheelSpeedMpS);
                                    // Zatím náhrada za 5. a 6. nápravu
                                    AxleWheelSpeedMpS[5] = Math.Abs((Car as MSTSLocomotive).extendedPhysics.Undercarriages[1].Axles[1].WheelSpeedMpS);
                                    AxleWheelSpeedMpS[6] = Math.Abs((Car as MSTSLocomotive).extendedPhysics.Undercarriages[1].Axles[1].WheelSpeedMpS);
                                }
                            }                            

                            distanceTravelledM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * (AxleWheelSpeedMpS[AxleNum] * (Car as MSTSLocomotive).WheelSpeedDirectionMarkerEP) * elapsedTime.ClockSeconds;
                            distanceTravelledDrivenM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * (AxleWheelSpeedMpS[AxleNum] * (Car as MSTSLocomotive).WheelSpeedDirectionMarkerEP) * elapsedTime.ClockSeconds;

                            if (Car.BrakeSkid) // if car wheels are skidding because of brakes locking wheels up then stop wheels rotating.
                            {
                                distanceTravelledM = 0.0f;
                                distanceTravelledDrivenM = 0.0f;
                            }

                            wheelCircumferenceM = MathHelper.TwoPi * AnimationWheelRadiusM;
                            rotationalDistanceR = MathHelper.TwoPi * distanceTravelledM / wheelCircumferenceM;  // in radians
                            LocoWheelRotationREP[AxleNum] = MathHelper.WrapAngle(LocoWheelRotationREP[AxleNum] - rotationalDistanceR);
                            wheelRotationMatrix = Matrix.CreateRotationX(LocoWheelRotationREP[AxleNum]);
                        }
                        else
                        {
                            if (AxleNum >= (Car as MSTSLocomotive).WagonNumAxles)
                            {
                                AxleNum = 0;
                            }
                            AxleNum++;

                            AxleWheelSpeedMpS[AxleNum] = MSTSWagon.Train.SpeedMpS;
                            for (int i = 1; i < 7; i++) 
                            {
                                if ((Car as MSTSLocomotive).DriveAxleNumber[i] == AxleNum)
                                {
                                    AxleWheelSpeedMpS[AxleNum] = (Car as MSTSLocomotive).WheelSpeedMpS;
                                }                                
                            }                                                                                                                    

                            distanceTravelledM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * AxleWheelSpeedMpS[AxleNum] * elapsedTime.ClockSeconds;
                            distanceTravelledDrivenM = ((MSTSWagon.Train != null && MSTSWagon.Train.IsPlayerDriven && ((MSTSLocomotive)MSTSWagon).UsingRearCab) ? -1 : 1) * AxleWheelSpeedMpS[AxleNum] * elapsedTime.ClockSeconds;

                            if (Car.BrakeSkid) // if car wheels are skidding because of brakes locking wheels up then stop wheels rotating.
                            {
                                distanceTravelledM = 0.0f;
                                distanceTravelledDrivenM = 0.0f;
                            }

                            wheelCircumferenceM = MathHelper.TwoPi * AnimationWheelRadiusM;
                            rotationalDistanceR = MathHelper.TwoPi * distanceTravelledM / wheelCircumferenceM;  // in radians
                            LocoWheelRotationR[AxleNum] = MathHelper.WrapAngle(LocoWheelRotationR[AxleNum] - rotationalDistanceR);
                            wheelRotationMatrix = Matrix.CreateRotationX(LocoWheelRotationR[AxleNum]);
                        }
                    }                    
                    TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
                }
            }

#if DEBUG_WHEEL_ANIMATION

            Trace.TraceInformation("========================== Debug Animation in MSTSWagonViewer.cs ==========================================");
            Trace.TraceInformation("Slip speed - Car ID: {0} WheelDistance: {1} SlipWheelDistance: {2}", Car.CarID, distanceTravelledM, distanceTravelledDrivenM);
            Trace.TraceInformation("Wag Speed - Wheelspeed: {0} Slip: {1} Train: {2}", MSTSWagon.WheelSpeedMpS, MSTSWagon.WheelSpeedSlipMpS, MSTSWagon.SpeedMpS);
            Trace.TraceInformation("Wheel Radius - DriveWheel: {0} NonDriveWheel: {1}", AnimationDriveWheelRadiusM, AnimationWheelRadiusM);

#endif

            // truck angle animation
            foreach (var p in Car.Parts)
            {
                if (p.iMatrix <= 0)
                    continue;
                Matrix m = Matrix.Identity;
                m.Translation = TrainCarShape.SharedShape.Matrices[p.iMatrix].Translation;
                m.M11 = p.Cos;
                m.M13 = p.Sin;
                m.M31 = -p.Sin;
                m.M33 = p.Cos;

                // To cancel out any vibration, apply the inverse here. If no vibration is present, this matrix will be Matrix.Identity.
                TrainCarShape.XNAMatrices[p.iMatrix] = Car.VibrationInverseMatrix * m;
            }

            // Display rear coupler in sim if open coupler shape is configured, otherwise skip to next section, and just display closed (default) coupler if configured
            if (FrontCouplerOpenShape != null && Car.IsAdvancedCoupler && Car.FrontCouplerOpenFitted && Car.FrontCouplerOpen && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // The following locates the coupler at the end of the car.
                // Suitable for development but, for release, would be better to implement as a sub-object of the car object.

                // Place the coupler in the centre of the car
                var p = Car.WorldPosition; // abbreviation
                FrontCouplerOpenShape.Location.Location = new Vector3(p.Location.X, p.Location.Y, p.Location.Z);
                FrontCouplerOpenShape.Location.TileX = p.TileX;
                FrontCouplerOpenShape.Location.TileZ = p.TileZ;

                // Get the movement that would be needed to locate the coupler on the car if they were pointing in the default direction.
                Vector3 displacement;
                displacement.X = Car.FrontCouplerOpenAnimWidthM;
                displacement.Y = Car.FrontCouplerOpenAnimHeightM;
                displacement.Z = (Car.FrontCouplerOpenAnimLengthM + (Car.CarLengthM / 2.0f) + Car.FrontCouplerSlackM);

                // Get the orientation of the car as a quaternion
                p.XNAMatrix.Decompose(out Vector3 scale, out Quaternion quaternion, out Vector3 translation);

                // Reverse the y axis (plan view) component - perhaps because XNA is opposite to MSTS
                var quaternionReversed = new Quaternion(quaternion.X, -quaternion.Y, quaternion.Z, quaternion.W);

                // Rotate the displacement to match the orientation of the car
                var rotatedDisplacement = Vector3.Transform(displacement, quaternionReversed);

                // Apply the rotation to the coupler displacement to keep it in place with the wagon
                FrontCouplerOpenShape.Location.Location += rotatedDisplacement;

                // Keep the coupler shape aligned with the wagon
                FrontCouplerOpenShape.Location.XNAMatrix.M11 = Car.WorldPosition.XNAMatrix.M11;
                FrontCouplerOpenShape.Location.XNAMatrix.M12 = Car.WorldPosition.XNAMatrix.M12;
                FrontCouplerOpenShape.Location.XNAMatrix.M13 = Car.WorldPosition.XNAMatrix.M13;
                FrontCouplerOpenShape.Location.XNAMatrix.M21 = Car.WorldPosition.XNAMatrix.M21;
                FrontCouplerOpenShape.Location.XNAMatrix.M22 = Car.WorldPosition.XNAMatrix.M22;
                FrontCouplerOpenShape.Location.XNAMatrix.M23 = Car.WorldPosition.XNAMatrix.M23;
                FrontCouplerOpenShape.Location.XNAMatrix.M31 = Car.WorldPosition.XNAMatrix.M31;
                FrontCouplerOpenShape.Location.XNAMatrix.M32 = Car.WorldPosition.XNAMatrix.M32;
                FrontCouplerOpenShape.Location.XNAMatrix.M33 = Car.WorldPosition.XNAMatrix.M33;

                // Display Animation Shape                    
                FrontCouplerOpenShape.PrepareFrame(frame, elapsedTime);
            }

            // Display rear default coupler in sim, by default it will always be closed position
            else if (FrontCouplerShape != null && Car.IsAdvancedCoupler && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // The following locates the coupler at the end of the car.
                // Suitable for development but, for release, would be better to implement as a sub-object of the car object.

                // Place the coupler in the centre of the car
                var p = Car.WorldPosition; // abbreviation
                FrontCouplerShape.Location.Location = new Vector3(p.Location.X, p.Location.Y, p.Location.Z);
                FrontCouplerShape.Location.TileX = p.TileX;
                FrontCouplerShape.Location.TileZ = p.TileZ;

                // Get the movement that would be needed to locate the coupler on the car if they were pointing in the default direction.
                Vector3 displacement;
                displacement.X = Car.FrontCouplerAnimWidthM;
                displacement.Y = Car.FrontCouplerAnimHeightM;
                displacement.Z = (Car.FrontCouplerAnimLengthM + (Car.CarLengthM / 2.0f) + Car.FrontCouplerSlackM);

                // Get the orientation of the car as a quaternion
                p.XNAMatrix.Decompose(out Vector3 scale, out Quaternion quaternion, out Vector3 translation);

                // Reverse the y axis (plan view) component - perhaps because XNA is opposite to MSTS
                var quaternionReversed = new Quaternion(quaternion.X, -quaternion.Y, quaternion.Z, quaternion.W);

                // Rotate the displacement to match the orientation of the car
                var rotatedDisplacement = Vector3.Transform(displacement, quaternionReversed);

                // Apply the rotation to the coupler displacement to keep it in place with the wagon
                FrontCouplerShape.Location.Location += rotatedDisplacement;

                // Keep the coupler shape aligned with the wagon
                FrontCouplerShape.Location.XNAMatrix.M11 = Car.WorldPosition.XNAMatrix.M11;
                FrontCouplerShape.Location.XNAMatrix.M12 = Car.WorldPosition.XNAMatrix.M12;
                FrontCouplerShape.Location.XNAMatrix.M13 = Car.WorldPosition.XNAMatrix.M13;
                FrontCouplerShape.Location.XNAMatrix.M21 = Car.WorldPosition.XNAMatrix.M21;
                FrontCouplerShape.Location.XNAMatrix.M22 = Car.WorldPosition.XNAMatrix.M22;
                FrontCouplerShape.Location.XNAMatrix.M23 = Car.WorldPosition.XNAMatrix.M23;
                FrontCouplerShape.Location.XNAMatrix.M31 = Car.WorldPosition.XNAMatrix.M31;
                FrontCouplerShape.Location.XNAMatrix.M32 = Car.WorldPosition.XNAMatrix.M32;
                FrontCouplerShape.Location.XNAMatrix.M33 = Car.WorldPosition.XNAMatrix.M33;

                // Display Animation Shape                    
                FrontCouplerShape.PrepareFrame(frame, elapsedTime);
            }

            // Display rear coupler in sim if open coupler shape is configured, otherwise skip to next section, and just display closed (default) coupler if configured
            if (RearCouplerOpenShape != null && Car.IsAdvancedCoupler && Car.RearCouplerOpenFitted && Car.RearCouplerOpen && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // The following locates the coupler at the end of the car.
                // Suitable for development but, for release, would be better to implement as a sub-object of the car object.

                // Place the coupler in the centre of the car
                var p = Car.WorldPosition; // abbreviation
                RearCouplerOpenShape.Location.Location = new Vector3(p.Location.X, p.Location.Y, p.Location.Z);
                RearCouplerOpenShape.Location.TileX = p.TileX;
                RearCouplerOpenShape.Location.TileZ = p.TileZ;

                // Get the movement that would be needed to locate the coupler on the car if they were pointing in the default direction.
                Vector3 displacement;
                displacement.X = Car.RearCouplerOpenAnimWidthM;
                displacement.Y = Car.RearCouplerOpenAnimHeightM;
                displacement.Z = -(Car.RearCouplerOpenAnimLengthM + (Car.CarLengthM / 2.0f) + Car.RearCouplerSlackM);  // Reversed as this is the rear coupler of the wagon

                // Get the orientation of the car as a quaternion
                p.XNAMatrix.Decompose(out Vector3 scale, out Quaternion quaternion, out Vector3 translation);

                // Reverse the y axis (plan view) component - perhaps because XNA is opposite to MSTS
                var quaternionReversed = new Quaternion(quaternion.X, -quaternion.Y, quaternion.Z, quaternion.W);

                // Rotate the displacement to match the orientation of the car
                var rotatedDisplacement = Vector3.Transform(displacement, quaternionReversed);

                // Apply the rotation to the coupler displacement to keep it in place with the wagon
                RearCouplerOpenShape.Location.Location += rotatedDisplacement;

                // Keep the coupler shape aligned with the wagon
                RearCouplerOpenShape.Location.XNAMatrix.M11 = Car.WorldPosition.XNAMatrix.M11;
                RearCouplerOpenShape.Location.XNAMatrix.M12 = Car.WorldPosition.XNAMatrix.M12;
                RearCouplerOpenShape.Location.XNAMatrix.M13 = Car.WorldPosition.XNAMatrix.M13;
                RearCouplerOpenShape.Location.XNAMatrix.M21 = Car.WorldPosition.XNAMatrix.M21;
                RearCouplerOpenShape.Location.XNAMatrix.M22 = Car.WorldPosition.XNAMatrix.M22;
                RearCouplerOpenShape.Location.XNAMatrix.M23 = Car.WorldPosition.XNAMatrix.M23;
                RearCouplerOpenShape.Location.XNAMatrix.M31 = Car.WorldPosition.XNAMatrix.M31;
                RearCouplerOpenShape.Location.XNAMatrix.M32 = Car.WorldPosition.XNAMatrix.M32;
                RearCouplerOpenShape.Location.XNAMatrix.M33 = Car.WorldPosition.XNAMatrix.M33;

                // Display Animation Shape                    
                RearCouplerOpenShape.PrepareFrame(frame, elapsedTime);
            }

            // Display rear default coupler in sim, by default it will always be closed position
            else if (RearCouplerShape != null && Car.IsAdvancedCoupler && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // The following locates the coupler at the end of the car.
                // Suitable for development but, for release, would be better to implement as a sub-object of the car object.

                // Place the coupler in the centre of the car
                var p = Car.WorldPosition; // abbreviation
                RearCouplerShape.Location.Location = new Vector3(p.Location.X, p.Location.Y, p.Location.Z);
                RearCouplerShape.Location.TileX = p.TileX;
                RearCouplerShape.Location.TileZ = p.TileZ;

                // Get the movement that would be needed to locate the coupler on the car if they were pointing in the default direction.
                Vector3 displacement;
                displacement.X = Car.RearCouplerAnimWidthM;
                displacement.Y = Car.RearCouplerAnimHeightM;
                displacement.Z = -(Car.RearCouplerAnimLengthM + (Car.CarLengthM / 2.0f) + Car.RearCouplerSlackM);  // Reversed as this is the rear coupler of the wagon

                // Get the orientation of the car as a quaternion
                p.XNAMatrix.Decompose(out Vector3 scale, out Quaternion quaternion, out Vector3 translation);

                // Reverse the y axis (plan view) component - perhaps because XNA is opposite to MSTS
                var quaternionReversed = new Quaternion(quaternion.X, -quaternion.Y, quaternion.Z, quaternion.W);

                // Rotate the displacement to match the orientation of the car
                var rotatedDisplacement = Vector3.Transform(displacement, quaternionReversed);

                // Apply the rotation to the coupler displacement to keep it in place with the wagon
                RearCouplerShape.Location.Location += rotatedDisplacement;

                // Keep the coupler shape aligned with the wagon
                RearCouplerShape.Location.XNAMatrix.M11 = Car.WorldPosition.XNAMatrix.M11;
                RearCouplerShape.Location.XNAMatrix.M12 = Car.WorldPosition.XNAMatrix.M12;
                RearCouplerShape.Location.XNAMatrix.M13 = Car.WorldPosition.XNAMatrix.M13;
                RearCouplerShape.Location.XNAMatrix.M21 = Car.WorldPosition.XNAMatrix.M21;
                RearCouplerShape.Location.XNAMatrix.M22 = Car.WorldPosition.XNAMatrix.M22;
                RearCouplerShape.Location.XNAMatrix.M23 = Car.WorldPosition.XNAMatrix.M23;
                RearCouplerShape.Location.XNAMatrix.M31 = Car.WorldPosition.XNAMatrix.M31;
                RearCouplerShape.Location.XNAMatrix.M32 = Car.WorldPosition.XNAMatrix.M32;
                RearCouplerShape.Location.XNAMatrix.M33 = Car.WorldPosition.XNAMatrix.M33;

                // Display Animation Shape                    
                RearCouplerShape.PrepareFrame(frame, elapsedTime);
            }


            // Applies MSTS style freight animation for coal load on the locomotive, crews, and other static animations.
            // Takes the form of FreightAnim ( A B C )
            // MSTS allowed crew figures to be inserted into the tender WAG file and thus be displayed on the locomotive.
            // It appears that only one MSTS type FA can be used per vehicle (to be confirmed?)
            // For coal load variation, C should be absent (set to 1 when read in WAG file) or >0 - sets FreightAnimFlag; and A > B
            // To disable coal load variation and insert a static (crew) shape on the tender breech, one of the conditions indicated above
            if (FreightShape != null && !(Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
            {
                // Define default position of shape
                FreightShape.Location.XNAMatrix = Car.WorldPosition.XNAMatrix;
                FreightShape.Location.TileX = Car.WorldPosition.TileX;
                FreightShape.Location.TileZ = Car.WorldPosition.TileZ;

                bool SteamAnimShape = false;
                float FuelControllerLevel = 0.0f;

                // For coal load variation on locomotives determine the current fuel level - and whether locomotive is a tender or tank type locomotive.
                if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSSteamLocomotive)
                {

                    var NonTenderSteamLocomotive = MSTSWagon as MSTSSteamLocomotive;

                    if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender || MSTSWagon is MSTSLocomotive && (MSTSWagon.EngineType == TrainCar.EngineTypes.Steam && NonTenderSteamLocomotive.IsTenderRequired == 0.0))
                    {

                        if (MSTSWagon.TendersSteamLocomotive == null)
                            MSTSWagon.FindTendersSteamLocomotive();

                        if (MSTSWagon.TendersSteamLocomotive != null)
                        {
                            FuelControllerLevel = MSTSWagon.TendersSteamLocomotive.FuelController.CurrentValue;
                            SteamAnimShape = true;
                        }
                        else if (NonTenderSteamLocomotive != null)
                        {
                            FuelControllerLevel = NonTenderSteamLocomotive.FuelController.CurrentValue;
                            SteamAnimShape = true;
                        }
                    }
                }

                // Set height of FAs - if relevant conditions met, use default position co-ords defined above
                if (FreightShape.XNAMatrices.Length > 0)
                {
                    // For tender coal load animation 
                    if (MSTSWagon.FreightAnimFlag > 0 && MSTSWagon.FreightAnimMaxLevelM > MSTSWagon.FreightAnimMinLevelM && SteamAnimShape)
                    {
                        FreightShape.XNAMatrices[0].M42 = MSTSWagon.FreightAnimMinLevelM + FuelControllerLevel * (MSTSWagon.FreightAnimMaxLevelM - MSTSWagon.FreightAnimMinLevelM);
                    }
                    // reproducing MSTS strange behavior; used to display loco crew when attached to tender
                    else if (MSTSWagon.WagonType == TrainCar.WagonTypes.Tender)
                    {
                        FreightShape.Location.XNAMatrix.M42 += MSTSWagon.FreightAnimMaxLevelM;
                    }
                }
                // Display Animation Shape                    
                FreightShape.PrepareFrame(frame, elapsedTime);
            }
            
            if (FreightAnimations != null)
            {                
                foreach (var freightAnim in FreightAnimations.Animations)
                {
                    if (freightAnim.Animation is FreightAnimationStatic)
                    {
                        var animation = freightAnim.Animation as FreightAnimationStatic;
                        if (!((animation.Visibility[(int)FreightAnimationStatic.VisibleFrom.Cab3D] &&
                            Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab) ||
                            (animation.Visibility[(int)FreightAnimationStatic.VisibleFrom.Cab2D] &&
                            Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.Cab) ||
                            (animation.Visibility[(int)FreightAnimationStatic.VisibleFrom.Outside] && (Viewer.Camera.AttachedCar != this.MSTSWagon ||
                            (Viewer.Camera.Style != Camera.Styles.ThreeDimCab && Viewer.Camera.Style != Camera.Styles.Cab))))) continue;
                    }
                    if (freightAnim.FreightShape != null && (!((freightAnim.Animation is FreightAnimationContinuous) && ((freightAnim.Animation as FreightAnimationContinuous).LoadPerCent == 0))
                        || ((freightAnim.Animation is FreightAnimationContinuous) && (MSTSWagon as MSTSWagon).MPWagonLoadPercent > 0)))
                    {
                        freightAnim.FreightShape.Location.XNAMatrix = Car.WorldPosition.XNAMatrix;
                        freightAnim.FreightShape.Location.TileX = Car.WorldPosition.TileX; freightAnim.FreightShape.Location.TileZ = Car.WorldPosition.TileZ;
                        if (freightAnim.FreightShape.XNAMatrices.Length > 0)
                        {
                            if (freightAnim.Animation is FreightAnimationContinuous)
                            {
                                var continuousFreightAnim = freightAnim.Animation as FreightAnimationContinuous;
                                if (MSTSWagon.FreightAnimations.IsGondola) freightAnim.FreightShape.XNAMatrices[0] = TrainCarShape.XNAMatrices[1];

                                // Icik
                                // Procenta nákladu pro MP
                                if ((MSTSWagon as MSTSWagon).MPWagonLoadPercent > 0)
                                {
                                    freightAnim.FreightShape.XNAMatrices[0].M42 = continuousFreightAnim.MinHeight +
                                   (float)(MSTSWagon as MSTSWagon).MPWagonLoadPercent / 100f * (continuousFreightAnim.MaxHeight - continuousFreightAnim.MinHeight);
                                }
                                else
                                {
                                    freightAnim.FreightShape.XNAMatrices[0].M42 = continuousFreightAnim.MinHeight +
                                       continuousFreightAnim.LoadPerCent / 100 * (continuousFreightAnim.MaxHeight - continuousFreightAnim.MinHeight);
                                }
                            }
                            if (freightAnim.Animation is FreightAnimationStatic)
                            {
                                var staticFreightAnim = freightAnim.Animation as FreightAnimationStatic;
                                freightAnim.FreightShape.XNAMatrices[0].M41 = staticFreightAnim.XOffset;
                                freightAnim.FreightShape.XNAMatrices[0].M42 = staticFreightAnim.YOffset;
                                freightAnim.FreightShape.XNAMatrices[0].M43 = staticFreightAnim.ZOffset;
                            }

                        }                        
                        // Forcing rotation of freight shape
                        freightAnim.FreightShape.PrepareFrame(frame, elapsedTime);
                    }
                }
            }

            // Get the current height above "sea level" for the relevant car
            Car.CarHeightAboveSeaLevelM = Viewer.Tiles.GetElevation(Car.WorldPosition.WorldLocation);

            // Control visibility of passenger cabin when inside it
            if (Viewer.Camera.AttachedCar == this.MSTSWagon
                 && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                     Viewer.Camera.Style == Camera.Styles.Passenger)
            {
                // We are in the passenger cabin
                if (InteriorShape != null)
                    InteriorShape.PrepareFrame(frame, elapsedTime);
                else
                    TrainCarShape.PrepareFrame(frame, elapsedTime);
            }
            else
            {
                // Skip drawing if 2D or 3D Cab view - Cab view already drawn - by GeorgeS changed by DennisAT
                if (Viewer.Camera.AttachedCar == this.MSTSWagon &&
                    (Viewer.Camera.Style == Camera.Styles.Cab || Viewer.Camera.Style == Camera.Styles.ThreeDimCab))
                    return;

                // We are outside the passenger cabin
                TrainCarShape.PrepareFrame(frame, elapsedTime);
            }            
        }



        /// <summary>
        /// Unload and release the car - its not longer being displayed
        /// </summary>
        public override void Unload()
        {
            // Removing sound sources from sound update thread
            Viewer.SoundProcess.RemoveSoundSources(this);

            base.Unload();
        }


        /// <summary>
        /// Load the various car sounds
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        public void LoadCarSounds(string wagonFolderSlash)
        {
            if (MSTSWagon.MainSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.MainSoundFileName);
            if (MSTSWagon.InteriorSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.InteriorSoundFileName);
            if (MSTSWagon.Cab3DSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.InteriorSoundFileName);
        }


        /// <summary>
        /// Load the car sound, attach it to the car
        /// check first in the wagon folder, then the global folder for the sound.
        /// If not found, report a warning.
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        /// <param name="filename"></param>
        protected void LoadCarSound(string wagonFolderSlash, string filename)
        {
            if (filename == null)
                return;
            string smsFilePath = wagonFolderSlash + @"sound\" + filename;
            
            if (!File.Exists(smsFilePath))
                smsFilePath = Viewer.Simulator.BasePath + @"\sound\" + filename;
            if (!File.Exists(smsFilePath))
            {
                Trace.TraceWarning("Cannot find {1} car sound file {0}", filename, wagonFolderSlash);
                return;
            }

            try
            {
                Viewer.SoundProcess.AddSoundSource(this, new SoundSource(Viewer, MSTSWagon, smsFilePath));
                
                // Icik
                if (MSTSWagon.CarSoundLoaded == false)
                {
                    string smsGenericFilePath = ""; // Default
                    // GenSound
                    if (!MSTSWagon.GenSoundOff && Program.Simulator.Settings.GenSound && MSTSWagon.CarLengthM > 1.0f && !MSTSWagon.WagonIsServis)
                    {
                        switch (MSTSWagon.WagonNumAxles)
                        {
                            case 2:
                                smsGenericFilePath = "..\\Content\\GenericSound\\2_Wheels\\GenSound_ex.sms";
                                break;
                            case 3:
                                smsGenericFilePath = "..\\Content\\GenericSound\\3_Wheels\\GenSound_ex.sms";
                                break;
                            case 4:
                                smsGenericFilePath = "..\\Content\\GenericSound\\4_Wheels\\GenSound_ex.sms";
                                break;
                            case 6:
                                smsGenericFilePath = "..\\Content\\GenericSound\\6_Wheels\\GenSound_ex.sms";
                                break;
                        }
                        Viewer.SoundProcess.AddSoundSource(this, new SoundSource(Viewer, MSTSWagon, System.IO.Path.Combine(Viewer.ContentPath, smsGenericFilePath)));
                    }
                    
                    // ActivitySound
                    if (MSTSWagon is MSTSLocomotive && MSTSWagon.CarLengthM > 1.0f && !MSTSWagon.WagonIsServis)
                    {
                        smsGenericFilePath = "..\\Content\\ActivitySound\\ActivitySound.sms";
                        Viewer.SoundProcess.AddSoundSource(this, new SoundSource(Viewer, MSTSWagon, System.IO.Path.Combine(Viewer.ContentPath, smsGenericFilePath)));
                    }

                    MSTSWagon.CarSoundLoaded = true;
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(smsFilePath, error));
            }
        }

        /// <summary>
        /// Load the inside and outside sounds for the default level 0 track type.
        /// </summary>
        private void LoadTrackSounds()
        {
            if (Viewer.TrackTypes.Count > 0)  // TODO, still have to figure out if this should be part of the car, or train, or track
            {
                if (!string.IsNullOrEmpty(MSTSWagon.InteriorSoundFileName))
                    LoadTrackSound(Viewer.TrackTypes[0].InsideSound);

                LoadTrackSound(Viewer.TrackTypes[0].OutsideSound);
            }
        }

        /// <summary>
        /// Load the sound source, attach it to the car.
        /// Check first in route\SOUND folder, then in base\SOUND folder.
        /// </summary>
        /// <param name="filename"></param>
        private void LoadTrackSound(string filename)
        {
            if (filename == null)
                return;
            string path = Viewer.Simulator.RoutePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
                path = Viewer.Simulator.BasePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
            {
                Trace.TraceWarning("Cannot find track sound file {0}", filename);
                return;
            }
            Viewer.SoundProcess.AddSoundSource(this, new SoundSource(Viewer, MSTSWagon, path));
        }

        internal override void Mark()
        {
            foreach (var pdl in ParticleDrawers.Values)
                foreach (var pd in pdl)
                    pd.Mark();
            TrainCarShape.Mark();
            if (FreightShape != null)
                FreightShape.Mark();
            if (InteriorShape != null)
                InteriorShape.Mark();
            if (FreightAnimations?.Animations != null)
                foreach (var freightAnimation in FreightAnimations.Animations)
                    freightAnimation.FreightShape?.Mark();
        }
    }
}
