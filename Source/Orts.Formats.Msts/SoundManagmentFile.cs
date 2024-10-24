﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using Orts.Parsers.Msts;
using System;
using System.Collections.Generic;

namespace Orts.Formats.Msts
{

    /// <summary>
    /// Utility class to avoid loading multiple copies of the same file.
    /// </summary>
    public class SharedSMSFileManager
    {
        private static Dictionary<string, SoundManagmentFile> SharedSMSFiles = new Dictionary<string, SoundManagmentFile>();

        public static int SwitchSMSNumber;
        public static int CurveSMSNumber;
        public static int CurveSwitchSMSNumber;
        public static bool AutoTrackSound = false;

        public static SoundManagmentFile Get(string path)
        {
            if (!SharedSMSFiles.ContainsKey(path))
            {
                SoundManagmentFile smsFile = new SoundManagmentFile(path);
                SharedSMSFiles.Add(path, smsFile);
                return smsFile;
            }
            else
            {
                return SharedSMSFiles[path];
            }
        }
    }

    /// <summary>
    /// Represents the hiearchical structure of the SMS File
    /// </summary>
    public class SoundManagmentFile
    {
        public Tr_SMS Tr_SMS;

        public SoundManagmentFile(string filePath)
        {
            ReadFile(filePath);
        }

        private void ReadFile(string filePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tr_sms", ()=>{ Tr_SMS = new Tr_SMS(stf); }),
                });
        }

    } // class SMSFile

    public class Tr_SMS
    {
        public List<ScalabiltyGroup> ScalabiltyGroups = new List<ScalabiltyGroup>();

        public Tr_SMS(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("scalabiltygroup", ()=>{ ScalabiltyGroups.Add(new ScalabiltyGroup(stf)); }),
            });
        }
    } // class Tr_SMS

    public partial class ScalabiltyGroup
    {
        public int DetailLevel;
        public SMSStreams Streams;
        public float Volume = 1.0f;
        public bool Stereo;
        public bool Ignore3D;
        public Activation Activation;
        public Deactivation Deactivation;

        public ScalabiltyGroup(STFReader stf)
        {
            stf.MustMatch("(");
            DetailLevel = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("activation", ()=>{ Activation = new Activation(stf); }),
                new STFReader.TokenProcessor("deactivation", ()=>{ Deactivation = new Deactivation(stf); }),
                new STFReader.TokenProcessor("streams", ()=>{ Streams = new SMSStreams(stf); }),
                new STFReader.TokenProcessor("volume", ()=>{ Volume = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("stereo", ()=>{ Stereo = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("ignore3d", ()=>{ Ignore3D = stf.ReadBoolBlock(true); }),
            });
        }
    } // class ScalabiltyGroup

    public class Activation
    {
        public bool ExternalCam;
        public bool CabCam;
        public bool PassengerCam;
        public float Distance = 1000;  // by default we are 'in range' to hear this        
        public int TrackType = -1;

        // Icik
        public bool FrontCabCam;
        public bool RearCabCam;

        public Activation(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("externalcam", ()=>{ ExternalCam = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("cabcam", ()=>{ CabCam = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("passengercam", ()=>{ PassengerCam = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("distance", ()=>{ Distance = stf.ReadFloatBlock(STFReader.UNITS.Distance, Distance); }),
                new STFReader.TokenProcessor("tracktype", ()=>{ TrackType = stf.ReadIntBlock(null); }),

                // Icik
                new STFReader.TokenProcessor("frontcabcam", ()=>{ FrontCabCam = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("rearcabcam", ()=>{ RearCabCam = stf.ReadBoolBlock(true); }),
            });
        }

        // for precompiled sound sources for activity sound
        public Activation()
        { }

    }

    public class Deactivation : Activation
    {
        public Deactivation(STFReader stf) : base(stf)
        {
        }

        // for precompiled sound sources for activity sound
        public Deactivation() : base()
        { }
    }

    public class SMSStreams : List<SMSStream>
    {
        public SMSStreams(STFReader stf)
        {
            stf.MustMatch("(");
            var count = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("stream", ()=>{
                    if (--count < 0)
                        STFException.TraceWarning(stf, "Skipped extra Stream");
                    else
                        Add(new SMSStream(stf));
                }),
            });
            if (count > 0)
                STFException.TraceWarning(stf, count + " missing Stream(s)");
        }
    }

    public class SMSStream
    {
        public int Priority;
        public Triggers Triggers;
        public float Volume = 1.0f;
        public List<VolumeCurve> VolumeCurves = new List<VolumeCurve>();
        public FrequencyCurve FrequencyCurve;

        public SMSStream(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("priority", ()=>{ Priority = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("triggers", ()=>{ Triggers = new Triggers(stf); }),
                new STFReader.TokenProcessor("volumecurve", ()=>{ VolumeCurves.Add(new VolumeCurve(stf)); }),
                new STFReader.TokenProcessor("frequencycurve", ()=>{ FrequencyCurve = new FrequencyCurve(stf); }),
                new STFReader.TokenProcessor("volume", ()=>{ Volume = stf.ReadFloatBlock(STFReader.UNITS.None, Volume); }),
            });
            //if (Volume > 1)  Volume /= 100f;
        }
    }

    public struct CurvePoint
    {
        public float X, Y;
    }

    public class VolumeCurve
    {
        public enum Controls
        {
            None, DistanceControlled, SpeedControlled,
            WheelSpeedControlled, WheelSpeedACControlled, WheelSpeedDCControlled, WheelSpeed1Controlled, WheelSpeed2Controlled, WheelSpeed3Controlled, WheelSpeed4Controlled,
            SlipSpeedControlled,
            VibrationControlled,
            Variable1Controlled, Variable1ACControlled, Variable1DCControlled,
            Variable2Controlled, Variable2ACControlled, Variable2DCControlled,
            Variable3Controlled, Variable3ACControlled, Variable3DCControlled,
            Variable4Controlled,
            Variable5Controlled,
            Variable6Controlled,
            Variable7Controlled,
            Variable8Controlled,
            Variable9Controlled,
            Variable10Controlled,
            Variable11Controlled,
            Variable12Controlled,
            TrainBrakeControllerControlled,
            EngineBrakeControllerControlled,
            BrakePipeChangeRateControlled,
            CylinderChangeRateControlled,
            BrakeCylControlled,
            CurveForceControlled,
            MotorWaterTemperatureControlled,
            MotorOilTemperatureControlled,
            WheelDamageValueControlled,
            PullPushValueControlled,
            TrackFactorValueControlled,
            PricipitationIntensityControlled,            
        };

        public Controls Control = Controls.None;
        public float Granularity = 1.0f;

        public CurvePoint[] CurvePoints;

        public VolumeCurve(STFReader stf)
        {
            stf.MustMatch("(");
            var type = stf.ReadString();
            switch (type.ToLower())
            {
                case "distancecontrolled": Control = Controls.DistanceControlled; break;
                case "speedcontrolled": Control = Controls.SpeedControlled; break;
                case "wheelspeedcontrolled": Control = Controls.WheelSpeedControlled; break;
                case "wheelspeedaccontrolled": Control = Controls.WheelSpeedACControlled; break;
                case "wheelspeeddccontrolled": Control = Controls.WheelSpeedDCControlled; break;
                case "wheelspeed1controlled": Control = Controls.WheelSpeed1Controlled; break;
                case "wheelspeed2controlled": Control = Controls.WheelSpeed2Controlled; break;
                case "wheelspeed3controlled": Control = Controls.WheelSpeed3Controlled; break;
                case "wheelspeed4controlled": Control = Controls.WheelSpeed4Controlled; break;
                case "slipspeedcontrolled": Control = Controls.SlipSpeedControlled; break;
                case "vibrationcontrolled": Control = Controls.VibrationControlled; break;
                case "variable1controlled": Control = Controls.Variable1Controlled; break;
                case "variable1accontrolled": Control = Controls.Variable1ACControlled; break;
                case "variable1dccontrolled": Control = Controls.Variable1DCControlled; break;
                case "variable2controlled": Control = Controls.Variable2Controlled; break;
                case "variable2accontrolled": Control = Controls.Variable2ACControlled; break;
                case "variable2dccontrolled": Control = Controls.Variable2DCControlled; break;
                case "variable3controlled": Control = Controls.Variable3Controlled; break;
                case "variable3accontrolled": Control = Controls.Variable3ACControlled; break;
                case "variable3dccontrolled": Control = Controls.Variable3DCControlled; break;
                case "variable4controlled": Control = Controls.Variable4Controlled; break;
                case "variable5controlled": Control = Controls.Variable5Controlled; break;
                case "variable6controlled": Control = Controls.Variable6Controlled; break;
                case "variable7controlled": Control = Controls.Variable7Controlled; break;
                case "variable8controlled": Control = Controls.Variable8Controlled; break;
                case "variable9controlled": Control = Controls.Variable9Controlled; break;
                case "variable10controlled": Control = Controls.Variable10Controlled; break;
                case "variable11controlled": Control = Controls.Variable11Controlled; break;
                case "variable12controlled": Control = Controls.Variable12Controlled; break;
                case "trainbrakecontrollercontrolled": Control = Controls.TrainBrakeControllerControlled; break;
                case "enginebrakecontrollercontrolled": Control = Controls.EngineBrakeControllerControlled; break;
                case "brakepipechangeratecontrolled": Control = Controls.BrakePipeChangeRateControlled; break;
                case "cylinderchangeratecontrolled": Control = Controls.CylinderChangeRateControlled; break;
                case "brakecylcontrolled": Control = Controls.BrakeCylControlled; break;
                case "curveforcecontrolled": Control = Controls.CurveForceControlled; break;
                case "motorwatertemperaturecontrolled": Control = Controls.MotorWaterTemperatureControlled; break;
                case "motoroiltemperaturecontrolled": Control = Controls.MotorOilTemperatureControlled; break;
                case "wheeldamagevaluecontrolled": Control = Controls.WheelDamageValueControlled; break;
                case "pullpushvaluecontrolled": Control = Controls.PullPushValueControlled; break;
                case "trackfactorvaluecontrolled": Control = Controls.TrackFactorValueControlled; break;
                case "pricipitationintensitycontrolled": Control = Controls.PricipitationIntensityControlled; break;
                default: STFException.TraceWarning(stf, "Crash expected: Skipped unknown VolumeCurve/Frequencycurve type " + type); stf.SkipRestOfBlock(); return;
            }
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("granularity", ()=>{ Granularity = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("curvepoints", ()=>{
                    stf.MustMatch("(");
                    int count = stf.ReadInt(null);
                    CurvePoints = new CurvePoint[count];
                    for (int i = 0; i < count; ++i)
                    {
                        CurvePoints[i].X = stf.ReadFloat(STFReader.UNITS.None, null);
                        if (Control == Controls.DistanceControlled)
                        {
                            if (CurvePoints[i].X >= 0) CurvePoints[i].X *= CurvePoints[i].X;
                            else CurvePoints[i].X *= -CurvePoints[i].X;
                        }
                        CurvePoints[i].Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    }
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class FrequencyCurve : VolumeCurve
    {
        public FrequencyCurve(STFReader stf)
            : base(stf)
        {
        }
    }


    public class Triggers : List<Trigger>
    {
        public Triggers(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("dist_travelled_trigger", ()=>{ Add(new Dist_Travelled_Trigger(stf)); }),
                new STFReader.TokenProcessor("discrete_trigger", ()=>{ Add(new Discrete_Trigger(stf)); }),
                new STFReader.TokenProcessor("random_trigger", ()=>{ Add(new Random_Trigger(stf)); }),
                new STFReader.TokenProcessor("variable_trigger", ()=>{ Add(new Variable_Trigger(stf)); }),
                new STFReader.TokenProcessor("initial_trigger", ()=>{ Add(new Initial_Trigger(stf)); }),
            });
            foreach (Trigger trigger in this)
                if (trigger.SoundCommand == null)
                    STFException.TraceWarning(stf, "Trigger lacks a sound command");
        }
    }

    public class Trigger
    {
        public SoundCommand SoundCommand;

        int playcommandcount;

        protected void ParsePlayCommand(STFReader f, string lowertoken)
        {
            switch (lowertoken)
            {
                case "playoneshot":
                case "startloop":
                case "releaselooprelease":
                case "startlooprelease":
                case "releaseloopreleasewithjump":
                case "disabletrigger":
                case "enabletrigger":
                case "setstreamvolume":
                    ++playcommandcount;
                    if (playcommandcount > 1)
                        STFException.TraceWarning(f, "Replaced play command");
                    break;
                default:
                    break;
            }

            switch (lowertoken)
            {
                case "playoneshot": SoundCommand = new PlayOneShot(f); break;
                case "startloop": SoundCommand = new StartLoop(f); break;
                case "releaselooprelease": SoundCommand = new ReleaseLoopRelease(f); break;
                case "startlooprelease": SoundCommand = new StartLoopRelease(f); break;
                case "releaseloopreleasewithjump": SoundCommand = new ReleaseLoopReleaseWithJump(f); break;
                case "disabletrigger": SoundCommand = new DisableTrigger(f); break;
                case "enabletrigger": SoundCommand = new EnableTrigger(f); break;
                case "setstreamvolume": SoundCommand = new SetStreamVolume(f); break;
                case "(": f.SkipRestOfBlock(); break;
            }
        }
    }

    public class Initial_Trigger : Trigger
    {

        public Initial_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadString().ToLower());
        }
    }

    public class Discrete_Trigger : Trigger
    {

        public int TriggerID;

        public Discrete_Trigger(STFReader f)
        {
            f.MustMatch("(");
            TriggerID = f.ReadInt(null);
            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadString().ToLower());
        }
    }

    public class Variable_Trigger : Trigger
    {
        public enum Events
        {
            Speed_Inc_Past, Speed_Dec_Past, Speed_Equals_To, Speed_NEquals_To,
            WheelSpeed_Inc_Past, WheelSpeed_Dec_Past, WheelSpeed_Equals_To, WheelSpeed_NEquals_To,
            WheelSpeedAC_Inc_Past, WheelSpeedAC_Dec_Past, WheelSpeedAC_Equals_To, WheelSpeedAC_NEquals_To,
            WheelSpeedDC_Inc_Past, WheelSpeedDC_Dec_Past, WheelSpeedDC_Equals_To, WheelSpeedDC_NEquals_To,
            WheelSpeed1_Inc_Past, WheelSpeed1_Dec_Past, WheelSpeed1_Equals_To, WheelSpeed1_NEquals_To,
            WheelSpeed2_Inc_Past, WheelSpeed2_Dec_Past, WheelSpeed2_Equals_To, WheelSpeed2_NEquals_To,
            WheelSpeed3_Inc_Past, WheelSpeed3_Dec_Past, WheelSpeed3_Equals_To, WheelSpeed3_NEquals_To,
            WheelSpeed4_Inc_Past, WheelSpeed4_Dec_Past, WheelSpeed4_Equals_To, WheelSpeed4_NEquals_To,
            SlipSpeed_Inc_Past, SlipSpeed_Dec_Past, SlipSpeed_Equals_To, SlipSpeed_NEquals_To,
            Vibration_Inc_Past, Vibration_Dec_Past, Vibration_Equals_To, Vibration_NEquals_To,
            Distance_Inc_Past, Distance_Dec_Past, Distance_Equals_To, Distance_NEquals_To,
            Variable1_Inc_Past, Variable1_Dec_Past, Variable1_Equals_To, Variable1_NEquals_To,
            Variable1AC_Inc_Past, Variable1AC_Dec_Past, Variable1AC_Equals_To, Variable1AC_NEquals_To,
            Variable1DC_Inc_Past, Variable1DC_Dec_Past, Variable1DC_Equals_To, Variable1DC_NEquals_To,
            Variable2_Inc_Past, Variable2_Dec_Past, Variable2_Equals_To, Variable2_NEquals_To,
            Variable2AC_Inc_Past, Variable2AC_Dec_Past, Variable2AC_Equals_To, Variable2AC_NEquals_To,
            Variable2DC_Inc_Past, Variable2DC_Dec_Past, Variable2DC_Equals_To, Variable2DC_NEquals_To,
            Variable3_Inc_Past, Variable3_Dec_Past, Variable3_Equals_To, Variable3_NEquals_To,
            Variable3AC_Inc_Past, Variable3AC_Dec_Past, Variable3AC_Equals_To, Variable3AC_NEquals_To,
            Variable3DC_Inc_Past, Variable3DC_Dec_Past, Variable3DC_Equals_To, Variable3DC_NEquals_To,
            Variable4_Inc_Past, Variable4_Dec_Past, Variable4_Equals_To, Variable4_NEquals_To, // DieselMotor RPM
            Variable5_Inc_Past, Variable5_Dec_Past, Variable5_Equals_To, Variable5_NEquals_To, // DieselMotor vstupní síla kN
            Variable6_Inc_Past, Variable6_Dec_Past, Variable6_Equals_To, Variable6_NEquals_To, // DieselMotor zátěž %
            Variable7_Inc_Past, Variable7_Dec_Past, Variable7_Equals_To, Variable7_NEquals_To, // DieselMotor turbo zátěž %
            Variable8_Inc_Past, Variable8_Dec_Past, Variable8_Equals_To, Variable8_NEquals_To, // DieselMotor turbo RPM
            TrainBrakeController_Inc_Past, TrainBrakeController_Dec_Past, TrainBrakeController_Equals_To, TrainBrakeController_NEquals_To,
            EngineBrakeController_Inc_Past, EngineBrakeController_Dec_Past, EngineBrakeController_Equals_To, EngineBrakeController_NEquals_To,
            BrakeCyl_Inc_Past, BrakeCyl_Dec_Past, BrakeCyl_Equals_To, BrakeCyl_NEquals_To,
            CurveForce_Inc_Past, CurveForce_Dec_Past, CurveForce_Equals_To, CurveForce_NEquals_To,
            StepController_Inc_Past, StepController_Dec_Past, StepController_Equals_To, StepController_NEquals_To,
            AripotController_Inc_Past, AripotController_Dec_Past, AripotController_Equals_To, AripotController_NEquals_To
        };

        public Events Event;
        public float Threshold;

        public Variable_Trigger(STFReader f)
        {
            f.MustMatch("(");

            string eventString = f.ReadString();

            Threshold = f.ReadFloat(STFReader.UNITS.None, null);

            switch (eventString.ToLower())
            {
                case "speed_inc_past": Event = Events.Speed_Inc_Past; break;
                case "speed_dec_past": Event = Events.Speed_Dec_Past; break;
                case "speed_equals_to": Event = Events.Speed_Equals_To; break;
                case "speed_nequals_to": Event = Events.Speed_NEquals_To; break;
                case "wheelspeed_inc_past": Event = Events.WheelSpeed_Inc_Past; break;
                case "wheelspeed_dec_past": Event = Events.WheelSpeed_Dec_Past; break;
                case "wheelspeed_equals_to": Event = Events.WheelSpeed_Equals_To; break;
                case "wheelspeed_nequals_to": Event = Events.WheelSpeed_NEquals_To; break;
                case "wheelspeedac_inc_past": Event = Events.WheelSpeedAC_Inc_Past; break;
                case "wheelspeedac_dec_past": Event = Events.WheelSpeedAC_Dec_Past; break;
                case "wheelspeedac_equals_to": Event = Events.WheelSpeedAC_Equals_To; break;
                case "wheelspeedac_nequals_to": Event = Events.WheelSpeedAC_NEquals_To; break;
                case "wheelspeeddc_inc_past": Event = Events.WheelSpeedDC_Inc_Past; break;
                case "wheelspeeddc_dec_past": Event = Events.WheelSpeedDC_Dec_Past; break;
                case "wheelspeeddc_equals_to": Event = Events.WheelSpeedDC_Equals_To; break;
                case "wheelspeeddc_nequals_to": Event = Events.WheelSpeedDC_NEquals_To; break;
                case "wheelspeed1_inc_past": Event = Events.WheelSpeed1_Inc_Past; break;
                case "wheelspeed1_dec_past": Event = Events.WheelSpeed1_Dec_Past; break;
                case "wheelspeed1_equals_to": Event = Events.WheelSpeed1_Equals_To; break;
                case "wheelspeed1_nequals_to": Event = Events.WheelSpeed1_NEquals_To; break;
                case "wheelspeed2_inc_past": Event = Events.WheelSpeed2_Inc_Past; break;
                case "wheelspeed2_dec_past": Event = Events.WheelSpeed2_Dec_Past; break;
                case "wheelspeed2_equals_to": Event = Events.WheelSpeed2_Equals_To; break;
                case "wheelspeed2_nequals_to": Event = Events.WheelSpeed2_NEquals_To; break;
                case "wheelspeed3_inc_past": Event = Events.WheelSpeed3_Inc_Past; break;
                case "wheelspeed3_dec_past": Event = Events.WheelSpeed3_Dec_Past; break;
                case "wheelspeed3_equals_to": Event = Events.WheelSpeed3_Equals_To; break;
                case "wheelspeed3_nequals_to": Event = Events.WheelSpeed3_NEquals_To; break;
                case "wheelspeed4_inc_past": Event = Events.WheelSpeed4_Inc_Past; break;
                case "wheelspeed4_dec_past": Event = Events.WheelSpeed4_Dec_Past; break;
                case "wheelspeed4_equals_to": Event = Events.WheelSpeed4_Equals_To; break;
                case "wheelspeed4_nequals_to": Event = Events.WheelSpeed4_NEquals_To; break;
                case "slipspeed_inc_past": Event = Events.SlipSpeed_Inc_Past; break;
                case "slipspeed_dec_past": Event = Events.SlipSpeed_Dec_Past; break;
                case "slipspeed_equals_to": Event = Events.SlipSpeed_Equals_To; break;
                case "slipspeed_nequals_to": Event = Events.SlipSpeed_NEquals_To; break;
                case "vibration_inc_past": Event = Events.Vibration_Inc_Past; break;
                case "vibration_dec_past": Event = Events.Vibration_Dec_Past; break;
                case "vibration_equals_to": Event = Events.Vibration_Equals_To; break;
                case "vibration_nequals_to": Event = Events.Vibration_NEquals_To; break;
                case "distance_inc_past":
                    {
                        Event = Events.Distance_Inc_Past;
                        Threshold = Threshold * Threshold;
                        break;
                    }
                case "distance_dec_past":
                    {
                        Event = Events.Distance_Dec_Past;
                        Threshold = Threshold * Threshold;
                        break;
                    }
                case "distance_equals_to":
                    {
                        Event = Events.Distance_Equals_To;
                        Threshold = Threshold * Threshold;
                        break;
                    }
                case "distance_nequals_to":
                    {
                        Event = Events.Distance_NEquals_To;
                        Threshold = Threshold * Threshold;
                        break;
                    }
                case "variable1_inc_past": Event = Events.Variable1_Inc_Past; break;
                case "variable1_dec_past": Event = Events.Variable1_Dec_Past; break;
                case "variable1_equals_to": Event = Events.Variable1_Equals_To; break;
                case "variable1_nequals_to": Event = Events.Variable1_NEquals_To; break;
                case "variable1ac_inc_past": Event = Events.Variable1AC_Inc_Past; break;
                case "variable1ac_dec_past": Event = Events.Variable1AC_Dec_Past; break;
                case "variable1ac_equals_to": Event = Events.Variable1AC_Equals_To; break;
                case "variable1ac_nequals_to": Event = Events.Variable1AC_NEquals_To; break;
                case "variable1dc_inc_past": Event = Events.Variable1DC_Inc_Past; break;
                case "variable1dc_dec_past": Event = Events.Variable1DC_Dec_Past; break;
                case "variable1dc_equals_to": Event = Events.Variable1DC_Equals_To; break;
                case "variable1dc_nequals_to": Event = Events.Variable1DC_NEquals_To; break;
                case "variable2_inc_past": Event = Events.Variable2_Inc_Past; break;
                case "variable2_dec_past": Event = Events.Variable2_Dec_Past; break;
                case "variable2_equals_to": Event = Events.Variable2_Equals_To; break;
                case "variable2_nequals_to": Event = Events.Variable2_NEquals_To; break;
                case "variable2ac_inc_past": Event = Events.Variable2AC_Inc_Past; break;
                case "variable2ac_dec_past": Event = Events.Variable2AC_Dec_Past; break;
                case "variable2ac_equals_to": Event = Events.Variable2AC_Equals_To; break;
                case "variable2ac_nequals_to": Event = Events.Variable2AC_NEquals_To; break;
                case "variable2dc_inc_past": Event = Events.Variable2DC_Inc_Past; break;
                case "variable2dc_dec_past": Event = Events.Variable2DC_Dec_Past; break;
                case "variable2dc_equals_to": Event = Events.Variable2DC_Equals_To; break;
                case "variable2dc_nequals_to": Event = Events.Variable2DC_NEquals_To; break;
                case "variable3_inc_past": Event = Events.Variable3_Inc_Past; break;
                case "variable3_dec_past": Event = Events.Variable3_Dec_Past; break;
                case "variable3_equals_to": Event = Events.Variable3_Equals_To; break;
                case "variable3_nequals_to": Event = Events.Variable3_NEquals_To; break;
                case "variable3ac_inc_past": Event = Events.Variable3AC_Inc_Past; break;
                case "variable3ac_dec_past": Event = Events.Variable3AC_Dec_Past; break;
                case "variable3ac_equals_to": Event = Events.Variable3AC_Equals_To; break;
                case "variable3ac_nequals_to": Event = Events.Variable3AC_NEquals_To; break;
                case "variable3dc_inc_past": Event = Events.Variable3DC_Inc_Past; break;
                case "variable3dc_dec_past": Event = Events.Variable3DC_Dec_Past; break;
                case "variable3dc_equals_to": Event = Events.Variable3DC_Equals_To; break;
                case "variable3dc_nequals_to": Event = Events.Variable3DC_NEquals_To; break;
                case "variable4_inc_past": Event = Events.Variable4_Inc_Past; break;
                case "variable4_dec_past": Event = Events.Variable4_Dec_Past; break;
                case "variable4_equals_to": Event = Events.Variable4_Equals_To; break;
                case "variable4_nequals_to": Event = Events.Variable4_NEquals_To; break;
                case "variable5_inc_past": Event = Events.Variable5_Inc_Past; break;
                case "variable5_dec_past": Event = Events.Variable5_Dec_Past; break;
                case "variable5_equals_to": Event = Events.Variable5_Equals_To; break;
                case "variable5_nequals_to": Event = Events.Variable5_NEquals_To; break;
                case "variable6_inc_past": Event = Events.Variable6_Inc_Past; break;
                case "variable6_dec_past": Event = Events.Variable6_Dec_Past; break;
                case "variable6_equals_to": Event = Events.Variable6_Equals_To; break;
                case "variable6_nequals_to": Event = Events.Variable6_NEquals_To; break;
                case "variable7_inc_past": Event = Events.Variable7_Inc_Past; break;
                case "variable7_dec_past": Event = Events.Variable7_Dec_Past; break;
                case "variable7_equals_to": Event = Events.Variable7_Equals_To; break;
                case "variable7_nequals_to": Event = Events.Variable7_NEquals_To; break;
                case "variable8_inc_past": Event = Events.Variable8_Inc_Past; break;
                case "variable8_dec_past": Event = Events.Variable8_Dec_Past; break;
                case "variable8_equals_to": Event = Events.Variable8_Equals_To; break;
                case "variable8_nequals_to": Event = Events.Variable8_NEquals_To; break;
                case "trainbrakecontroller_inc_past": Event = Events.TrainBrakeController_Inc_Past; break;
                case "trainbrakecontroller_dec_past": Event = Events.TrainBrakeController_Dec_Past; break;
                case "trainbrakecontroller_equals_to": Event = Events.TrainBrakeController_Equals_To; break;
                case "trainbrakecontroller_nequals_to": Event = Events.TrainBrakeController_NEquals_To; break;
                case "enginebrakecontroller_inc_past": Event = Events.EngineBrakeController_Inc_Past; break;
                case "enginebrakecontroller_dec_past": Event = Events.EngineBrakeController_Dec_Past; break;
                case "enginebrakecontroller_equals_to": Event = Events.EngineBrakeController_Equals_To; break;
                case "enginebrakecontroller_nequals_to": Event = Events.EngineBrakeController_NEquals_To; break;
                case "brakecyl_inc_past": Event = Events.BrakeCyl_Inc_Past; break;
                case "brakecyl_dec_past": Event = Events.BrakeCyl_Dec_Past; break;
                case "brakecyl_equals_to": Event = Events.BrakeCyl_Equals_To; break;
                case "brakecyl_nequals_to": Event = Events.BrakeCyl_NEquals_To; break;
                case "curveforce_inc_past": Event = Events.CurveForce_Inc_Past; break;
                case "curveforce_dec_past": Event = Events.CurveForce_Dec_Past; break;
                case "curveforce_equals_to": Event = Events.CurveForce_Equals_To; break;
                case "curveforce_nequals_to": Event = Events.CurveForce_NEquals_To; break;
                case "stepcontroller_inc_past": Event = Events.StepController_Inc_Past; break;
                case "stepcontroller_dec_past": Event = Events.StepController_Dec_Past; break;
                case "stepcontroller_equals_to": Event = Events.StepController_Equals_To; break;
                case "stepcontroller_nequals_to": Event = Events.StepController_NEquals_To; break;
                case "aripotcontroller_inc_past": Event = Events.AripotController_Inc_Past; break;
                case "aripotcontroller_dec_past": Event = Events.AripotController_Dec_Past; break;
                case "aripotcontroller_equals_to": Event = Events.AripotController_Equals_To; break;
                case "aripotcontroller_nequals_to": Event = Events.AripotController_NEquals_To; break;
            }



            while (!f.EndOfBlock())
                ParsePlayCommand(f, f.ReadString().ToLower());
        }
    }

    public class Dist_Travelled_Trigger : Trigger
    {
        public float Dist_Min = 80;
        public float Dist_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Dist_Travelled_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "dist_min_max": f.MustMatch("("); Dist_Min = f.ReadFloat(STFReader.UNITS.Distance, null); Dist_Max = f.ReadFloat(STFReader.UNITS.Distance, null); f.SkipRestOfBlock(); break;
                    case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }

    public class Random_Trigger : Trigger
    {
        public float Delay_Min = 80;
        public float Delay_Max = 100;
        public float Volume_Min = 0.9f;
        public float Volume_Max = 1.0f;

        public Random_Trigger(STFReader f)
        {
            f.MustMatch("(");
            while (!f.EndOfBlock())
            {
                string lowtok = f.ReadString().ToLower();
                switch (lowtok)
                {
                    case "delay_min_max": f.MustMatch("("); Delay_Min = f.ReadFloat(STFReader.UNITS.None, null); Delay_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    case "volume_min_max": f.MustMatch("("); Volume_Min = f.ReadFloat(STFReader.UNITS.None, null); Volume_Max = f.ReadFloat(STFReader.UNITS.None, null); f.SkipRestOfBlock(); break;
                    default: ParsePlayCommand(f, lowtok); break;
                }
            }
        }
    }
    public class SoundCommand
    {
        public enum SelectionMethods { RandomSelection, SequentialSelection };
    }

    public class SetStreamVolume : SoundCommand
    {
        public float Volume;

        public SetStreamVolume(STFReader f)
        {
            f.MustMatch("(");
            Volume = f.ReadFloat(STFReader.UNITS.None, null);
            f.SkipRestOfBlock();
        }
    }

    public class DisableTrigger : SoundCommand
    {
        public int TriggerID;

        public DisableTrigger(STFReader f)
        {
            f.MustMatch("(");
            TriggerID = f.ReadInt(null);
            f.SkipRestOfBlock();
        }
    }

    public class EnableTrigger : DisableTrigger
    {
        public EnableTrigger(STFReader f)
            : base(f)
        {
        }
    }

    public class ReleaseLoopRelease : SoundCommand
    {
        public ReleaseLoopRelease(STFReader f)
        {
            f.MustMatch("(");
            f.SkipRestOfBlock();
        }
    }

    public class ReleaseLoopReleaseWithJump : SoundCommand
    {
        public ReleaseLoopReleaseWithJump(STFReader f)
        {
            f.MustMatch("(");
            f.SkipRestOfBlock();
        }
    }

    public class SoundPlayCommand : SoundCommand
    {
        public string[] Files;
        public SelectionMethods SelectionMethod = SelectionMethods.SequentialSelection;
    }

    public class PlayOneShot : SoundPlayCommand
    {

        public PlayOneShot(STFReader f)
        {
            f.MustMatch("(");
            int count = f.ReadInt(null);
            Files = new string[count];
            int iFile = 0;
            while (!f.EndOfBlock())
                switch (f.ReadString().ToLower())
                {
                    case "file":
                        if (iFile < count)
                        {
                            f.MustMatch("(");
                            Files[iFile++] = f.ReadString();
                            f.ReadInt(null);
                            f.SkipRestOfBlock();
                        }
                        else  // MSTS skips extra files
                        {
                            STFException.TraceWarning(f, "Skipped extra File");
                            f.SkipBlock();
                        }
                        break;
                    case "selectionmethod":
                        f.MustMatch("(");
                        string s = f.ReadString();
                        switch (s.ToLower())
                        {
                            case "randomselection": SelectionMethod = SelectionMethods.RandomSelection; break;
                            case "sequentialselection": SelectionMethod = SelectionMethods.SequentialSelection; break;
                            default: STFException.TraceWarning(f, "Skipped unknown selection method " + s); break;
                        }
                        f.SkipRestOfBlock();
                        break;
                    case "(": f.SkipRestOfBlock(); break;
                }
        }
    }// PlayOneShot

    public class StartLoop : PlayOneShot
    {
        public StartLoop(STFReader f) : base(f)
        {
        }
    }

    public class StartLoopRelease : PlayOneShot
    {
        public StartLoopRelease(STFReader f)
            : base(f)
        {
        }
    }


} // namespace
