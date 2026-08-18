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

using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using System.Collections.Generic;
using System.IO;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// A LightState object encapsulates the data for each State in the States subblock.
    /// </summary>
    public class LightState
    {
        public float Duration;
        public uint Color;
        public Vector3 Position;
        public float Radius;
        public Vector3 Azimuth;
        public Vector3 Elevation;
        public bool Transition;
        public float Angle;

        public LightState(uint color, Vector3 position, Vector3 azimuth, float angle = 150f, float radius = 30f)
        {
            Duration = 0f;
            Color = color;
            Position = position;
            Radius = radius;
            Azimuth = azimuth;
            Elevation = Vector3.Zero;
            Transition = true;
            Angle = angle;
        }

        public LightState(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("duration", ()=>{ Duration = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("lightcolour", ()=>{ Color = stf.ReadHexBlock(null); }),
                new STFReader.TokenProcessor("position", ()=>{ Position = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("radius", ()=>{ Radius = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("azimuth", ()=>{ Azimuth = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("elevation", ()=>{ Elevation = stf.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero); }),
                new STFReader.TokenProcessor("transition", ()=>{ Transition = 1 <= stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
                new STFReader.TokenProcessor("angle", ()=>{ Angle = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
            });
            // Color byte order changed in XNA 4 from BGRA to RGBA
            Color = new Color()
            {
                B = (byte)(Color),
                G = (byte)(Color >> 8),
                R = (byte)(Color >> 16),
                A = (byte)(Color >> 24)
            }.PackedValue;
        }

        public LightState(LightState state, bool reverse)
        {
            Duration = state.Duration;
            Color = state.Color;
            Position = state.Position;
            Radius = state.Radius;
            Azimuth = state.Azimuth;
            Elevation = state.Elevation;
            Transition = state.Transition;
            Angle = state.Angle;

            if (reverse)
            {
                Azimuth.X += 180;
                Azimuth.X %= 360;
                Azimuth.Y += 180;
                Azimuth.Y %= 360;
                Azimuth.Z += 180;
                Azimuth.Z %= 360;
                Position.X *= -1;
                Position.Z *= -1;
            }
        }
    }

    #region Light enums
    /// <summary>
    /// Specifies whether a wagon light is glow (simple light texture) or cone (projected light cone).
    /// </summary>
    public enum LightType
    {
        Glow,
        Cone,
    }

    /// <summary>
    /// Specifies in which headlight positions (off, dim, bright) the wagon light is illuminated.
    /// </summary>
    public enum LightHeadlightCondition
    {
        Ignore,
        Off,        
        Dim,
        Bright,        
        DimBright, // MSTSBin
        OffBright, // MSTSBin
        OffDim, // MSTSBin
        DLight, // Poziční světla
        FrontLightCone, // Světla pro přední světelný kužel
        RearLightCone, // Světla pro zadní světelný kužel
        // TODO: DimBright?, // MSTSBin labels this the same as DimBright. Not sure what it means.
    }

    /// <summary>
    /// Specifies on which units of a consist (first, middle, last) the wagon light is illuminated.
    /// </summary>
    public enum LightUnitCondition
    {
        Ignore,
        Middle,
        First,
        Last,
        LastRev, // MSTSBin
        FirstRev, // MSTSBin
    }

    /// <summary>
    /// Specifies in which penalty states (no, yes) the wagon light is illuminated.
    /// </summary>
    public enum LightPenaltyCondition
    {
        Ignore,
        No,
        Yes,
    }

    /// <summary>
    /// Specifies on which types of trains (AI, player) the wagon light is illuminated.
    /// </summary>
    public enum LightControlCondition
    {
        Ignore,
        AI,
        Player,
    }

    /// <summary>
    /// Specifies in which in-service states (no, yes) the wagon light is illuminated.
    /// </summary>
    public enum LightServiceCondition
    {
        Ignore,
        No,
        Yes,
    }

    /// <summary>
    /// Specifies during which times of day (day, night) the wagon light is illuminated.
    /// </summary>
    public enum LightTimeOfDayCondition
    {
        Ignore,
        Day,
        Night,
    }

    /// <summary>
    /// Specifies in which weather conditions (clear, rain, snow) the wagon light is illuminated.
    /// </summary>
    public enum LightWeatherCondition
    {
        Ignore,
        Clear,
        Rain,
        Snow,
    }

    /// <summary>
    /// Specifies on which units of a consist by coupling (front, rear, both) the wagon light is illuminated.
    /// </summary>
    public enum LightCouplingCondition
    {
        Ignore,
        Front,
        Rear,
        Both,
    }

    // Icik
    /// <summary>
    /// Specifies on which sides of the unit light is illuminated.
    /// </summary>
    public enum LightHandleCondition
    {
        Ignore,  // 0
        FrontLW, // 1
        FrontLR, // 2
        FrontRW, // 3
        FrontRR, // 4
        RearLW,  // 5
        RearLR,  // 6
        RearRW,  // 7
        RearRR,  // 8
        FrontHeadLight, // 9
        RearHeadLight, // 10
        FrontW,  // 11 - Přední bílá na obou stranách (LW nebo RW)
        RearW,   // 12 - Zadní bílá na obou stranách (LW nebo RW)
        FrontR,  // 13 - Přední červená na obou stranách (LR nebo RR)
        RearR,   // 14 - Zadní červená na obou stranách (LR nebo RR)
        ConeFDim, // 15 - Přední reflektor tlumený
        ConeFBright, // 16  - Přední reflektor dálkový
        ConeRDim, // 17  - Zadní reflektor tlumený
        ConeRBright, // 18  - Zadní reflektor dálkový
    }

    public enum LightGlowType
    {
        Bulb,  // 0
        Led, // 1        
        Star, // 2                
    }    
    #endregion

    /// <summary>
    /// The Light class encapsulates the data for each Light object 
    /// in the Lights block of an ENG/WAG file. 
    /// </summary>
    public class Light
    {
        public int Index;
        public LightType Type;
        public LightHeadlightCondition Headlight;
        public LightUnitCondition Unit;
        public LightPenaltyCondition Penalty;
        public LightControlCondition Control;
        public LightServiceCondition Service;
        public LightTimeOfDayCondition TimeOfDay;
        public LightWeatherCondition Weather;
        public LightCouplingCondition Coupling;
        
        // Icik
        public LightHandleCondition UnitSide;
        public LightHeadlightCondition HeadlightFront;
        public LightHeadlightCondition HeadlightRear;
        public LightGlowType LightGlowType;
        public string LightGlowName;
        public bool LightDimFound;

        public bool Cycle;
        public float FadeIn;
        public float FadeOut;
        public List<LightState> States = new List<LightState>();

        public Light(int index, LightType type, LightHandleCondition unitSide, LightControlCondition control, List<LightState> states)
        {
            Index = index;
            Type = type;
            UnitSide = unitSide;
            Control = control;
            Headlight = LightHeadlightCondition.Ignore;
            Unit = LightUnitCondition.Ignore;
            Penalty = LightPenaltyCondition.Ignore;
            Service = LightServiceCondition.Ignore;
            TimeOfDay = LightTimeOfDayCondition.Ignore;
            Weather = LightWeatherCondition.Ignore;
            Coupling = LightCouplingCondition.Ignore;
            Cycle = false;
            FadeIn = 0.0f;
            FadeOut = 0.0f;
            States = states;
        }

        public Light(int index, STFReader stf)
        {
            Index = index;
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("type", ()=>{ Type = (LightType)stf.ReadIntBlock(null);  if (Type == LightType.Cone) Unit = LightUnitCondition.First; }),
                new STFReader.TokenProcessor("lightglowtype", ()=>{ LightGlowType = (LightGlowType)stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("lightglowname", ()=>{ LightGlowName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("conditions", ()=>{ stf.MustMatch("("); stf.ParseBlock(new[] {
                    new STFReader.TokenProcessor("headlight", ()=>{ Headlight = (LightHeadlightCondition)stf.ReadIntBlock(null); if (Headlight == LightHeadlightCondition.Dim) LightDimFound = true; }),
                    new STFReader.TokenProcessor("unit", ()=>{ Unit = (LightUnitCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("penalty", ()=>{ Penalty = (LightPenaltyCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("control", ()=>{ Control = (LightControlCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("service", ()=>{ Service = (LightServiceCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("timeofday", ()=>{ TimeOfDay = (LightTimeOfDayCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("weather", ()=>{ Weather = (LightWeatherCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("coupling", ()=>{ Coupling = (LightCouplingCondition)stf.ReadIntBlock(null); }),
                    // Icik
                    new STFReader.TokenProcessor("unitside", ()=>{ UnitSide = (LightHandleCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("headlightfront", ()=>{ HeadlightFront = (LightHeadlightCondition)stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("headlightrear", ()=>{ HeadlightRear = (LightHeadlightCondition)stf.ReadIntBlock(null); }),                    
                });}),
                new STFReader.TokenProcessor("cycle", ()=>{ Cycle = 0 != stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("fadein", ()=>{ FadeIn = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("fadeout", ()=>{ FadeOut = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("states", ()=>{
                    stf.MustMatch("(");
                    var count = stf.ReadInt(null);
                    stf.ParseBlock(new[] {
                        new STFReader.TokenProcessor("state", ()=>{
                            if (States.Count >= count)
                                STFException.TraceWarning(stf, "Skipped extra State");
                            else
                                States.Add(new LightState(stf));
                        }),
                    });
                    if (States.Count < count)
                        STFException.TraceWarning(stf, (count - States.Count).ToString() + " missing State(s)");
                }),
            });
        }

        public Light(Light light, bool reverse)
        {
            Index = light.Index;
            Type = light.Type;
            Headlight = light.Headlight;
            Unit = light.Unit;
            Penalty = light.Penalty;
            Control = light.Control;
            Service = light.Service;
            TimeOfDay = light.TimeOfDay;
            Weather = light.Weather;
            Coupling = light.Coupling;
            Cycle = light.Cycle;
            FadeIn = light.FadeIn;
            FadeOut = light.FadeOut;

            // Icik
            UnitSide = light.UnitSide;
            HeadlightFront = light.HeadlightFront;
            HeadlightRear = light.HeadlightRear;
            LightGlowType = light.LightGlowType;
            LightGlowName = light.LightGlowName;
            LightDimFound = light.LightDimFound;

            foreach (var state in light.States)
                States.Add(new LightState(state, reverse));

            if (reverse)
            {
                if (Unit == LightUnitCondition.First)
                    Unit = LightUnitCondition.FirstRev;
                else if (Unit == LightUnitCondition.Last)
                    Unit = LightUnitCondition.LastRev;
            }
        }
    }

    /// <summary>
    /// A Lights object is created for any engine or wagon having a 
    /// Lights block in its ENG/WAG file. It contains a collection of
    /// Light objects.
    /// Called from within the MSTSWagon class.
    /// </summary>
    public class LightCollection
    {
        public List<Light> Lights = new List<Light>();

        public LightCollection(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ReadInt(null); // count; ignore this because its not always correct
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("light", ()=>{ Lights.Add(new Light(Lights.Count, stf)); }),
            });
            if (Lights.Count == 0)
                throw new InvalidDataException("lights with no lights");

            // MSTSBin created reverse headlight cones automatically, so we shall do so too.
            foreach (var light in Lights.ToArray())
                if (light.Type == LightType.Cone)
                {
                    var reverseLight = light.UnitSide != LightHandleCondition.FrontW && light.UnitSide != LightHandleCondition.RearW && light.UnitSide != LightHandleCondition.FrontR && light.UnitSide != LightHandleCondition.RearR
                        && light.UnitSide != LightHandleCondition.ConeFDim && light.UnitSide != LightHandleCondition.ConeFBright && light.UnitSide != LightHandleCondition.ConeRDim && light.UnitSide != LightHandleCondition.ConeRBright;
                    Lights.Add(new Light(light, reverseLight));
                }
        }
    }
}
