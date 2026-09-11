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

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Orts.Simulation.Simulation.RollingStocks;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Support for animating any sub-part of a wagon or locomotive. Supports both on/off toggled animations and continuous-running ones.
    /// </summary>
    public class AnimatedPart
    {
        // Shape that we're animating.
        readonly PoseableShape PoseableShape;

        // Number of animation key-frames that are used by this part. This is calculated from the matrices provided.
        public int FrameCount;

        // Current frame of the animation.
        float AnimationKey;

        // List of the matrices we're animating for this part.
        public List<int> MatrixIndexes = new List<int>();

        /// <summary>
        /// Construct with a link to the shape that contains the animated parts 
        /// </summary>
        public AnimatedPart(PoseableShape poseableShape)
        {
            PoseableShape = poseableShape;
        }

        /// <summary>
        /// All the matrices associated with this part are added during initialization by the MSTSWagon constructor
        /// </summary>
        public void AddMatrix(int matrix)
        {
            if (matrix < 0) return;
            MatrixIndexes.Add(matrix);
            UpdateFrameCount(matrix);
        }

        void UpdateFrameCount(int matrix)
        {
            if (PoseableShape.SharedShape.Animations != null
                && PoseableShape.SharedShape.Animations.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes.Count > matrix
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].Count > 0)
            {
                FrameCount = Math.Max(FrameCount, PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].ToArray().Cast<KeyPosition>().Last().Frame);
                // Sometimes there are more frames in the second controller than in the first
                if (PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 1
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[1].Count > 0)
                    FrameCount = Math.Max(FrameCount, PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[1].ToArray().Cast<KeyPosition>().Last().Frame);
            }
            for (var i = 0; i < PoseableShape.Hierarchy.Length; i++)
                if (PoseableShape.Hierarchy[i] == matrix)
                    UpdateFrameCount(i);
        }

        /// <summary>
        /// Ensure the shape file contained parts of this type 
        /// and those parts have an animation section.
        /// </summary>
        public bool Empty()
        {
            return MatrixIndexes.Count == 0;
        }

        void SetFrame(float frame)
        {
            AnimationKey = frame;
            foreach (var matrix in MatrixIndexes)
                PoseableShape.AnimateMatrix(matrix, AnimationKey);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst clamping it to the frame count range.
        /// </summary>
        public void SetFrameClamp(float frame)
        {
            if (frame > FrameCount) frame = FrameCount;
            if (frame < 0) frame = 0;
            SetFrame(frame);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst cycling back to the start as input goes beyond the last frame.
        /// </summary>
        public void SetFrameCycle(float frame)
        {
            // Animates from 0-FrameCount then FrameCount-0 for values of 0>=frame<=2*FrameCount.
            SetFrameClamp(FrameCount - Math.Abs(frame - FrameCount));
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst wrapping it around the frame count range.
        /// </summary>
        public void SetFrameWrap(float frame)
        {
            // Wrap the frame around 0-FrameCount without hanging when FrameCount=0.
            while (FrameCount > 0 && frame < 0) frame += FrameCount;
            if (frame < 0) frame = 0;
            frame %= FrameCount;
            SetFrame(frame);
        }

        /// <summary>
        /// Bypass the normal slow transition and jump the part immediately to this new state
        /// </summary>
        public void SetState(bool state)
        {
            SetFrame(state ? FrameCount : 0);
        }

        /// <summary>
        /// Updates an animated part that toggles between two states (e.g. pantograph, doors, mirrors).
        /// </summary>
        public void UpdateState(bool state, ElapsedTime elapsedTime)
        {
            SetFrameClamp(AnimationKey + (state ? 1 : -1) * elapsedTime.ClockSeconds);
        }

        float pantoVibratesTimer;
        float pantoVibratesTime;
        bool pantoVibrates;
        int pantoVibratesCycleCount;
        int pantoVibratesCycle;
        bool pantoVibratesEnable;
        bool pantoVibratesDone;
        float pantoVibratesRatioCoef;
        float pantoVibratesRatioCoefMax;
        public void PantoVibrates(ElapsedTime elapsedTime)
        {
            // Nastavení vibrace pantografu
            if (!pantoVibrates)
            {
                pantoVibratesTimer = 0;
                pantoVibratesTime = 0.3f;
                pantoVibratesCycleCount = 5;
                pantoVibratesRatioCoefMax = 0.15f;
            }
            
            pantoVibrates = true;
            pantoVibratesEnable = true;
                        
            if (pantoVibratesTimer == 0)
            {
                pantoVibratesCycle++;
                pantoVibratesRatioCoef = MathHelper.Clamp(pantoVibratesRatioCoefMax / (pantoVibratesCycle / 2f), 0, pantoVibratesRatioCoefMax);                
                //Program.Viewer.Simulator.Confirmer.Warning("pantoVibratesRatioCoef: " + pantoVibratesRatioCoef);
            }

            if (pantoVibratesCycle < pantoVibratesCycleCount + 1)
            {
                pantoVibratesTimer += elapsedTime.ClockSeconds;

                if (pantoVibratesTimer < pantoVibratesTime)
                    SetFrameClamp(AnimationKey - (pantoVibratesRatioCoef * elapsedTime.ClockSeconds));
                if (pantoVibratesTimer > pantoVibratesTime)
                    SetFrameClamp(AnimationKey + (pantoVibratesRatioCoef * elapsedTime.ClockSeconds));

                if (pantoVibratesTimer > 2f * pantoVibratesTime)                
                    pantoVibratesTimer = 0;                
            }
            else
            {                
                pantoVibratesCycle = 0;
                pantoVibratesEnable = false;
                pantoVibratesDone = true;
            }
        }

        // Icik
        float PantoAnimSlowingUp;
        float PantoAnimSlowingDown;
        float preWireHeight;
        float PantoAnimSlowingDownTimer;
        public void UpdateStatePanto1(bool state, ElapsedTime elapsedTime)
        {
            var ELoco = (Program.Viewer.Simulator.MSTSWagon as MSTSElectricLocomotive);
            if (ELoco != null)
            {
                float Panto57HeightCorrection = ELoco.Pantographs[1].Panto57HeightCorrection / 100f;
                float LimitPantoHeight = FrameCount * (1f + Panto57HeightCorrection);
                ELoco.Panto3AnimFrameCount = FrameCount;

                //Program.Viewer.Simulator.Confirmer.Information("PantoAnimSlowingDown: " + PantoAnimSlowingUp);
                if (!state && AnimationKey > 0.1f || state && (AnimationKey > 0 && AnimationKey < 0.3f) || AnimationKey > 0.1f * FrameCount && AnimationKey < 0.3f * FrameCount || AnimationKey == 0)
                {
                    pantoVibrates = false;
                    pantoVibratesDone = false;
                }
                
                if (state)
                {
                    PantoAnimSlowingDownTimer = 0;
                    // Vibrace pantografu
                    if (ELoco.Pantographs[1].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * LimitPantoHeight ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * LimitPantoHeight || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }
                    else
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * FrameCount ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * FrameCount || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }

                    if (!pantoVibrates || preWireHeight != ELoco.Simulator.WireHeigth)
                    {
                        if (ELoco.Pantographs[1].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                        {
                            if (AnimationKey < 0.99f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[1].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            else
                            if (AnimationKey > 1.01f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[1].AnimCorrectTimeCoefDown));
                            if (AnimationKey > 0.99f * LimitPantoHeight && AnimationKey < 1.01f * LimitPantoHeight)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                        else
                        {
                            SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[1].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            if (AnimationKey > 0.99f * FrameCount)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                    }
                }
                else
                {
                    if (AnimationKey > 0.1f)
                    {
                        PantoAnimSlowingDown = 1f;
                        PantoAnimSlowingDownTimer = 0;                        
                    }                    

                    if (AnimationKey < 0.1f && AnimationKey != 0)
                    {
                        PantoAnimSlowingDown = 0;

                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0 || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);

                        PantoAnimSlowingDownTimer += elapsedTime.ClockSeconds;

                        if (PantoAnimSlowingDownTimer > 2f)
                        {
                            pantoVibrates = false;
                            PantoAnimSlowingDown = 1.5f;
                            if (AnimationKey == 0) PantoAnimSlowingDownTimer = 0;
                        }
                    }
                    if (!pantoVibrates)
                        SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[1].AnimCorrectTimeCoefDown * PantoAnimSlowingDown));                    
                }
            }            
        }
        public void UpdateStatePanto2(bool state, ElapsedTime elapsedTime)
        {
            var ELoco = (Program.Viewer.Simulator.MSTSWagon as MSTSElectricLocomotive);
            if (ELoco != null)
            {
                float Panto57HeightCorrection = ELoco.Pantographs[2].Panto57HeightCorrection / 100f;
                float LimitPantoHeight = FrameCount * (1f + Panto57HeightCorrection);
                ELoco.Panto4AnimFrameCount = FrameCount;

                //Program.Viewer.Simulator.Confirmer.Information("PantoAnimSlowingDown: " + PantoAnimSlowingUp);
                if (!state && AnimationKey > 0.1f || state && (AnimationKey > 0 && AnimationKey < 0.3f) || AnimationKey > 0.1f * FrameCount && AnimationKey < 0.3f * FrameCount || AnimationKey == 0)
                {
                    pantoVibrates = false;
                    pantoVibratesDone = false;
                }

                if (state)
                {
                    // Vibrace pantografu
                    if (ELoco.Pantographs[2].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * LimitPantoHeight ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * LimitPantoHeight || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }
                    else
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * FrameCount ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * FrameCount || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }

                    if (!pantoVibrates || preWireHeight != ELoco.Simulator.WireHeigth)
                    {
                        if (ELoco.Pantographs[2].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                        {
                            if (AnimationKey < 0.99f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[2].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            else
                            if (AnimationKey > 1.01f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[2].AnimCorrectTimeCoefDown));
                            if (AnimationKey > 0.99f * LimitPantoHeight && AnimationKey < 1.01f * LimitPantoHeight)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                        else
                        {
                            SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[2].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            if (AnimationKey > 0.99f * FrameCount)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                    }
                }
                else
                {
                    if (AnimationKey > 0.1f)
                    {
                        PantoAnimSlowingDown = 1f;
                        PantoAnimSlowingDownTimer = 0;
                    }

                    if (AnimationKey < 0.1f && AnimationKey != 0)
                    {
                        PantoAnimSlowingDown = 0;

                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0 || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);

                        PantoAnimSlowingDownTimer += elapsedTime.ClockSeconds;

                        if (PantoAnimSlowingDownTimer > 2f)
                        {
                            pantoVibrates = false;
                            PantoAnimSlowingDown = 1.5f;
                            if (AnimationKey == 0) PantoAnimSlowingDownTimer = 0;
                        }
                    }
                    if (!pantoVibrates)
                        SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[2].AnimCorrectTimeCoefDown * PantoAnimSlowingDown));
                }
            }
        }        
        public void UpdateStatePanto3(bool state, ElapsedTime elapsedTime)
        {
            var ELoco = (Program.Viewer.Simulator.MSTSWagon as MSTSElectricLocomotive);
            if (ELoco != null)
            {
                float Panto57HeightCorrection = ELoco.Pantographs[3].Panto57HeightCorrection / 100f;
                float LimitPantoHeight = FrameCount * (1f + Panto57HeightCorrection);
                                
                if (FrameCount == 0)
                {
                    FrameCount = ELoco.Panto3AnimFrameCount;
                    ELoco.Pantographs[3].AnimCorrectTimeCoefUp *= 1.6f;
                    ELoco.Pantographs[3].AnimCorrectTimeCoefDown *= 2.0f;
                }
                ELoco.Panto3AnimFrame = AnimationKey / FrameCount;
                
                //Program.Viewer.Simulator.Confirmer.Information("PantoAnimSlowingDown: " + PantoAnimSlowingUp);
                if (!state && AnimationKey > 0.1f || state && (AnimationKey > 0 && AnimationKey < 0.3f) || AnimationKey > 0.1f * FrameCount && AnimationKey < 0.3f * FrameCount || AnimationKey == 0)
                {
                    pantoVibrates = false;
                    pantoVibratesDone = false;
                }

                if (state)
                {
                    // Vibrace pantografu
                    if (ELoco.Pantographs[3].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * LimitPantoHeight ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * LimitPantoHeight || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }
                    else
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * FrameCount ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * FrameCount || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }

                    if (!pantoVibrates || preWireHeight != ELoco.Simulator.WireHeigth)
                    {
                        if (ELoco.Pantographs[3].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                        {
                            if (AnimationKey < 0.99f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[3].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            else
                            if (AnimationKey > 1.01f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[3].AnimCorrectTimeCoefDown));
                            if (AnimationKey > 0.99f * LimitPantoHeight && AnimationKey < 1.01f * LimitPantoHeight)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                        else
                        {
                            SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[3].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            if (AnimationKey > 0.99f * FrameCount)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                    }
                }
                else
                {
                    if (AnimationKey > 0.1f)
                    {
                        PantoAnimSlowingDown = 1f;
                        PantoAnimSlowingDownTimer = 0;
                    }

                    if (AnimationKey < 0.1f && AnimationKey != 0)
                    {
                        PantoAnimSlowingDown = 0;

                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0 || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);

                        PantoAnimSlowingDownTimer += elapsedTime.ClockSeconds;

                        if (PantoAnimSlowingDownTimer > 2f)
                        {
                            pantoVibrates = false;
                            PantoAnimSlowingDown = 1.5f;
                            if (AnimationKey == 0) PantoAnimSlowingDownTimer = 0;
                        }
                    }
                    if (!pantoVibrates)
                        SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[3].AnimCorrectTimeCoefDown * PantoAnimSlowingDown));
                }
            }
        }
        public void UpdateStatePanto4(bool state, ElapsedTime elapsedTime)
        {
            var ELoco = (Program.Viewer.Simulator.MSTSWagon as MSTSElectricLocomotive);
            if (ELoco != null)
            {
                float Panto57HeightCorrection = ELoco.Pantographs[4].Panto57HeightCorrection / 100f;
                float LimitPantoHeight = FrameCount * (1f + Panto57HeightCorrection);

                if (FrameCount == 0)
                {
                    FrameCount = ELoco.Panto4AnimFrameCount;
                    ELoco.Pantographs[4].AnimCorrectTimeCoefUp *= 1.6f;
                    ELoco.Pantographs[4].AnimCorrectTimeCoefDown *= 2.0f;
                }
                ELoco.Panto4AnimFrame = AnimationKey / FrameCount;

                //Program.Viewer.Simulator.Confirmer.Information("PantoAnimSlowingDown: " + PantoAnimSlowingUp);
                if (!state && AnimationKey > 0.1f || state && (AnimationKey > 0 && AnimationKey < 0.3f) || AnimationKey > 0.1f * FrameCount && AnimationKey < 0.3f * FrameCount || AnimationKey == 0)
                {
                    pantoVibrates = false;
                    pantoVibratesDone = false;
                }

                if (state)
                {
                    // Vibrace pantografu
                    if (ELoco.Pantographs[4].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * LimitPantoHeight ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * LimitPantoHeight || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }
                    else
                    {
                        PantoAnimSlowingUp = AnimationKey < 0.85f * FrameCount ? 1.5f : 0.5f;
                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0.99f * FrameCount || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);
                    }

                    if (!pantoVibrates || preWireHeight != ELoco.Simulator.WireHeigth)
                    {
                        if (ELoco.Pantographs[4].PantoIs62 && ELoco.Simulator.WireHeigth == 5.7f)
                        {
                            if (AnimationKey < 0.99f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[4].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            else
                            if (AnimationKey > 1.01f * LimitPantoHeight)
                                SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[4].AnimCorrectTimeCoefDown));
                            if (AnimationKey > 0.99f * LimitPantoHeight && AnimationKey < 1.01f * LimitPantoHeight)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                        else
                        {
                            SetFrameClamp(AnimationKey + (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[4].AnimCorrectTimeCoefUp * PantoAnimSlowingUp));
                            if (AnimationKey > 0.99f * FrameCount)
                                preWireHeight = ELoco.Simulator.WireHeigth;
                        }
                    }
                }
                else
                {
                    if (AnimationKey > 0.1f)
                    {
                        PantoAnimSlowingDown = 1f;
                        PantoAnimSlowingDownTimer = 0;
                    }

                    if (AnimationKey < 0.1f && AnimationKey != 0)
                    {
                        PantoAnimSlowingDown = 0;

                        if (!pantoVibratesDone && (!pantoVibrates && AnimationKey > 0 || pantoVibratesEnable))
                            PantoVibrates(elapsedTime);

                        PantoAnimSlowingDownTimer += elapsedTime.ClockSeconds;

                        if (PantoAnimSlowingDownTimer > 2f)
                        {
                            pantoVibrates = false;
                            PantoAnimSlowingDown = 1.5f;
                            if (AnimationKey == 0) PantoAnimSlowingDownTimer = 0;
                        }
                    }
                    if (!pantoVibrates)
                        SetFrameClamp(AnimationKey - (1 * elapsedTime.ClockSeconds * ELoco.Pantographs[4].AnimCorrectTimeCoefDown * PantoAnimSlowingDown));
                }
            }
        }

        /// <summary>
        /// Updates an animated part that loops (e.g. running gear), changing by the given amount.
        /// </summary>
        public void UpdateLoop(float change)
        {
            if (PoseableShape.SharedShape.Animations == null || PoseableShape.SharedShape.Animations.Count == 0 || FrameCount == 0)
                return;

            // The speed of rotation is set at 8 frames of animation per rotation at 30 FPS (so 16 frames = 60 FPS, etc.).
            var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * 8 / 30f;
            SetFrameWrap(AnimationKey + change * frameRate);
        }

        /// <summary>
        /// Updates an animated part that loops only when enabled (e.g. wipers).
        /// </summary>
        public void UpdateLoop(bool running, ElapsedTime elapsedTime, float frameRateMultiplier = 1.5f)
        {
            if (PoseableShape.SharedShape.Animations == null || PoseableShape.SharedShape.Animations.Count == 0 || FrameCount == 0)
                return;

            // The speed of cycling is as default 1.5 frames of animation per second at 30 FPS.
            var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * frameRateMultiplier / 30f;
            if (running || (AnimationKey > 0 && AnimationKey + elapsedTime.ClockSeconds * frameRate < FrameCount))
                SetFrameWrap(AnimationKey + elapsedTime.ClockSeconds * frameRate);
            else
                SetFrame(0);
        }

        /// <summary>
        /// Swap the pointers around.
        /// </summary>
        public static void Swap(ref AnimatedPart a, ref AnimatedPart b)
        {
            AnimatedPart temp = a;
            a = b;
            b = temp;
        }
    }
}
