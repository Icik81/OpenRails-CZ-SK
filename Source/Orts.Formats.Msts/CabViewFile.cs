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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{

	// TODO - this is an incomplete parse of the cvf file.
	public class CabViewFile
	{
        public List<Vector3> Locations = new List<Vector3>();   // Head locations for front, left and right views
        protected List<Vector3> locationShift = new List<Vector3>();
        public List<Vector3> Directions = new List<Vector3>();  // Head directions for each view
        protected List<Vector3> directionShift = new List<Vector3>(); 
        public List<string> TwoDViews = new List<string>();     // 2D CAB Views - by GeorgeS
        public List<string> NightViews = new List<string>();    // Night CAB Views - by GeorgeS
        public List<string> LightViews = new List<string>();    // Light CAB Views - by GeorgeS
        public CabViewControls CabViewControls;                 // Controls in CAB - by GeorgeS

        public CabViewFile(string filePath, string basePath)
		{
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tr_cabviewfile", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("position", ()=>
                        { 
                            Locations.Add(stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()));

                            //int x = 0;
                            //for (int i = 0; i < Locations.Count; i++)
                            //{
                            //    Vector3 v3 = Locations[i];
                            //    v3.X = Locations[x].X + 0;
                            //    v3.Y = Locations[x].Y + 0;
                            //    v3.Z = Locations[x].Z + 0;
                            //    Locations[i] = v3;
                            //    x++;
                            //    if (x + 1 > Locations.Count)
                            //        break;
                            //}
                        }),
                        
                        
                        new STFReader.TokenProcessor("positionshift", ()=>
                        {
                            locationShift.Add(stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()));
                            int x = 0;
                            for (int i = 0; i < Locations.Count; i++)
                            {
                                Vector3 v3 = Locations[i];
                                v3.X = locationShift[x].X;
                                v3.Y = locationShift[x].Y;
                                v3.Z = locationShift[x].Z;
                                Locations[i] = v3;
                                x++;
                                if (x + 1 > locationShift.Count)
                                    break;
                            }
                        }),
                        new STFReader.TokenProcessor("direction", ()=>
                        {
                            Directions.Add(stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()));

                            //int x = 0;
                            //for (int i = 0; i < Directions.Count; i++)
                            //{
                            //    Vector3 v3 = Directions[i];
                            //    v3.X = Directions[x].X + 4;
                            //    v3.Y = Directions[x].Y - ((i + 1) * 2);
                            //    v3.Z = Directions[x].Z + 0;
                            //    Directions[i] = v3;
                            //    x++;
                            //    if (x + 1 > Directions.Count)
                            //        break;
                            //}
                        }),
                        new STFReader.TokenProcessor("directionshift", ()=>
                        {
                            directionShift.Add(stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()));
                            int x = 0;
                            for (int i = 0; i < Directions.Count; i++)
                            {
                                Vector3 v3 = Directions[i];
                                v3.X = directionShift[x].X;
                                v3.Y = directionShift[x].Y;
                                v3.Z = directionShift[x].Z;
                                Directions[i] = v3;
                                x++;
                                if (x + 1 > directionShift.Count)
                                    break;
                            }
                        }),
                        new STFReader.TokenProcessor("cabviewfile", ()=>{
                            var fileName = stf.ReadStringBlock(null);
                            var path = Path.Combine(basePath, Path.GetDirectoryName(fileName));
                            var name = Path.GetFileName(fileName);

                            // Use *Frnt1024.ace if available
                            string s = name;
                            string[] nameParts = s.Split('.');
                            string name1024 = nameParts[0] + "1024." + nameParts[1];
                            var tstFileName1024 = Path.Combine(path, name1024);
                            if (File.Exists(tstFileName1024))
                                name = name1024;

                            TwoDViews.Add(Path.Combine(path, name));
                            NightViews.Add(Path.Combine(path, Path.Combine("NIGHT", name)));
                            LightViews.Add(Path.Combine(path, Path.Combine("CABLIGHT", name)));
                        }),
                        new STFReader.TokenProcessor("cabviewcontrols", ()=>{ CabViewControls = new CabViewControls(stf, basePath); }),
                    });}),
                });
		}

	} // class CVFFile

    public enum CABViewControlTypes
    {
        NONE,
        SPEEDOMETER,
        MAIN_RES,
        EQ_RES,
        BRAKE_CYL,
        BRAKE_PIPE,
        LINE_VOLTAGE,
        AMMETER,
        AMMETER_ABS,
        LOAD_METER,
        THROTTLE,
        PANTOGRAPH,
        TRAIN_BRAKE,
        FRICTION_BRAKE,
        ENGINE_BRAKE,
        BRAKEMAN_BRAKE,
        DYNAMIC_BRAKE,
        DYNAMIC_BRAKE_DISPLAY,
        SANDERS,
        WIPERS,
        VACUUM_EXHAUSTER,
        HORN,
        BELL,
        FRONT_HLIGHT,
        DIRECTION,
        ASPECT_DISPLAY,
        THROTTLE_DISPLAY,
        CPH_DISPLAY,
        PANTO_DISPLAY,
        DIRECTION_DISPLAY,
        CP_HANDLE,
        PANTOGRAPH2,
        CLOCK,
        SANDING,
        ALERTER_DISPLAY,
        TRACTION_BRAKING,
        ACCELEROMETER,
        WHEELSLIP,
        FRICTION_BRAKING,
        PENALTY_APP,
        EMERGENCY_BRAKE,
        RESET,
        CAB_RADIO,
        OVERSPEED,
        SPEEDLIM_DISPLAY,
        FUEL_GAUGE,
        WHISTLE,
        REGULATOR,
        CYL_COCKS,
        BLOWER,
        STEAM_INJ1,
        STEAM_INJ2,
        ORTS_BLOWDOWN_VALVE,
        DAMPERS_FRONT,
        DAMPERS_BACK,
        STEAM_HEAT,
        WATER_INJECTOR1,
        WATER_INJECTOR2,
        SMALL_EJECTOR,
        STEAM_PR,
        STEAMCHEST_PR,
        TENDER_WATER,
        BOILER_WATER,
        REVERSER_PLATE,
        STEAMHEAT_PRESSURE,
        FIREBOX,
        RPM,
        FIREHOLE,
        CUTOFF,
        VACUUM_RESERVOIR_PRESSURE,
        GEARS,
        DOORS_DISPLAY,
        SPEED_PROJECTED,
        SPEEDLIMIT,
        PANTOGRAPHS_4,
        PANTOGRAPHS_4C,
        PANTOGRAPHS_5,
        ORTS_OIL_PRESSURE,
        ORTS_DIESEL_TEMPERATURE,
        ORTS_CYL_COMP,
        GEARS_DISPLAY,
        DYNAMIC_BRAKE_FORCE,
        ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER,
        ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER,
        ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION,
        ORTS_CIRCUIT_BREAKER_STATE,
        ORTS_CIRCUIT_BREAKER_CLOSED,
        ORTS_CIRCUIT_BREAKER_OPEN,
        ORTS_CIRCUIT_BREAKER_AUTHORIZED,
        ORTS_CIRCUIT_BREAKER_OPEN_AND_AUTHORIZED,
        ORTS_PLAYER_DIESEL_ENGINE,
        ORTS_HELPERS_DIESEL_ENGINES,
        ORTS_PLAYER_DIESEL_ENGINE_STATE,
        ORTS_PLAYER_DIESEL_ENGINE_STARTER,
        ORTS_PLAYER_DIESEL_ENGINE_STOPPER,
        ORTS_CABLIGHT,
        ORTS_LEFTDOOR,
        ORTS_RIGHTDOOR,
        ORTS_MIRRORS,
        ORTS_PANTOGRAPH3,
        ORTS_PANTOGRAPH4,
        ORTS_LARGE_EJECTOR,
        ORTS_WATER_SCOOP,
        ORTS_HOURDIAL,
        ORTS_MINUTEDIAL,
        ORTS_SECONDDIAL,
        ORTS_SIGNED_TRACTION_BRAKING,
        ORTS_SIGNED_TRACTION_TOTAL_BRAKING,
        ORTS_BAILOFF,
        ORTS_QUICKRELEASE,
        ORTS_OVERCHARGE,
        ORTS_BATTERY,
        ORTS_POWERKEY,
        ORTS_2DEXTERNALWIPERS,

        // TCS Controls
        ORTS_TCS1,
        ORTS_TCS2,
        ORTS_TCS3,
        ORTS_TCS4,
        ORTS_TCS5,
        ORTS_TCS6,
        ORTS_TCS7,
        ORTS_TCS8,
        ORTS_TCS9,
        ORTS_TCS10,
        ORTS_TCS11,
        ORTS_TCS12,
        ORTS_TCS13,
        ORTS_TCS14,
        ORTS_TCS15,
        ORTS_TCS16,
        ORTS_TCS17,
        ORTS_TCS18,
        ORTS_TCS19,
        ORTS_TCS20,
        ORTS_TCS21,
        ORTS_TCS22,
        ORTS_TCS23,
        ORTS_TCS24,
        ORTS_TCS25,
        ORTS_TCS26,
        ORTS_TCS27,
        ORTS_TCS28,
        ORTS_TCS29,
        ORTS_TCS30,
        ORTS_TCS31,
        ORTS_TCS32,
        ORTS_TCS33,
        ORTS_TCS34,
        ORTS_TCS35,
        ORTS_TCS36,
        ORTS_TCS37,
        ORTS_TCS38,
        ORTS_TCS39,
        ORTS_TCS40,
        ORTS_TCS41,
        ORTS_TCS42,
        ORTS_TCS43,
        ORTS_TCS44,
        ORTS_TCS45,
        ORTS_TCS46,
        ORTS_TCS47,
        ORTS_TCS48,
        ORTS_ETCS,

        // Jindřich
        ORTS_SELECTED_SPEED,
        ORTS_SELECTED_SPEED_DISPLAY,
        ORTS_SELECTED_SPEED_MODE,
        ORTS_SELECTED_SPEED_REGULATOR_MODE,
        ORTS_SELECTED_SPEED_MAXIMUM_ACCELERATION,
        ORTS_RESTRICTED_SPEED_ZONE_ACTIVE,
        ORTS_NUMBER_OF_AXES_DISPLAY_UNITS,
        ORTS_NUMBER_OF_AXES_DISPLAY_TENS,
        ORTS_NUMBER_OF_AXES_DISPLAY_HUNDREDS,
        ORTS_TRAIN_LENGTH_METERS,
        ORTS_REMAINING_TRAIN_LENGHT_SPEED_RESTRICTED,
        ORTS_REMAINING_TRAIN_LENGTH_PERCENT,
        ORTS_MOTIVE_FORCE,
        ORTS_MOTIVE_FORCE_KILONEWTON,
        ORTS_MAXIMUM_FORCE,
        ORTS_FORCE_IN_PERCENT_THROTTLE_AND_DYNAMIC_BRAKE,
        ORTS_TRAIN_TYPE_PAX_OR_CARGO,
        ORTS_CONTROLLER_VOLTAGE,
        ORTS_AMPERS_BY_CONTROLLER_VOLTAGE,
        ORTS_ACCELERATION_IN_TIME,
        ORTS_ODOMETER,
        ORTS_CC_SELECT_SPEED,
        ORTS_NUMBER_OF_AXES_INCREASE,
        ORTS_NUMBER_OF_AXES_DECREASE,
        ORTS_MULTI_POSITION_CONTROLLER,
        ORTS_CC_SPEED_0,
        ORTS_CC_SPEED_10,
        ORTS_CC_SPEED_20,
        ORTS_CC_SPEED_30,
        ORTS_CC_SPEED_40,
        ORTS_CC_SPEED_50,
        ORTS_CC_SPEED_60,
        ORTS_CC_SPEED_70,
        ORTS_CC_SPEED_80,
        ORTS_CC_SPEED_90,
        ORTS_CC_SPEED_100,
        ORTS_CC_SPEED_110,
        ORTS_CC_SPEED_120,
        ORTS_CC_SPEED_130,
        ORTS_CC_SPEED_140,
        ORTS_CC_SPEED_150,
        ORTS_CC_SPEED_160,
        ORTS_CC_SPEED_170,
        ORTS_CC_SPEED_180,
        ORTS_CC_SPEED_190,
        ORTS_CC_SPEED_200,
        ORTS_DIGITAL_STRING,
        ORTS_DATE,
        ORTS_DIGITAL_STRING_SELECTED_STRING_INCREASE,
        ORTS_DIGITAL_STRING_SELECTED_STRING_DECREASE,
        ORTS_DIGITAL_STRING_ACTION_BUTTON,
        ORTS_MIREL_SPEED,
        ORTS_MIREL_DISPLAY,
        ORTS_DISPLAY_BLUE_LIGHT,
        ORTS_MIREL_DRIVE_MODE,
        ORTS_MIREL_DRIVE_MODE_OPTIONS,
        ORTS_MIREL_FULL_DISPLAY,
        ORTS_MIREL_DISPLAY_FLASH_MASK,
        ORTS_MIREL_DISPLAY_NUM_1,
        ORTS_MIREL_DISPLAY_NUM_2,
        ORTS_MIREL_DISPLAY_NUM_3,
        ORTS_MIREL_DISPLAY_D1,
        ORTS_MIREL_DISPLAY_TEST1,
        ORTS_MIREL_DISPLAY_TEST2,
        ORTS_MIREL_DISPLAY_TEST3,
        ORTS_MIREL_DISPLAY_TEST4,
        ORTS_MIREL_DISPLAY_TEST5,
        ORTS_MIREL_DISPLAY_TEST6,
        ORTS_MIREL_DISPLAY_TEST7,
        ORTS_MIREL_NZOK,
        ORTS_MIREL_NZ1,
        ORTS_MIREL_NZ2,
        ORTS_MIREL_NZ3,
        ORTS_MIREL_NZ4,
        ORTS_MIREL_NZ5,
        ORTS_MIREL_MAN,
        ORTS_MIREL_ZAP,
        ORTS_MIREL_VYP,
        ORTS_MIREL_TOP_LEFT_DOT,
        ORTS_MIREL_TRANS_FRQ,
        ORTS_MIREL_M,
        ORTS_MIREL_START_REDUCE_SPEED,
        ORTS_MIREL_PLUS,
        ORTS_MIREL_MINUS,
        ORTS_MIREL_ENTER,
        ORTS_ACTIVE_CAB,
        ORTS_REPEATER_LIGHTS_MASK,
        ORTS_STATION,
        ORTS_LS90_POWER,
        ORTS_LS90_LED,
        ORTS_AVV_SIGNAL,

        // Icik
        HV5,
        PANTOGRAPH_4_SWITCH,
        COMPRESSOR_START,
        COMPRESSOR_COMBINED,
        COMPRESSOR_COMBINED2,
        AUXCOMPRESSOR_MODE_OFFON,
        COMPRESSOR_MODE_OFFAUTO,
        COMPRESSOR_MODE2_OFFAUTO,
        HEATING_OFFON,
        HEATING_POWER,
        SWITCHINGVOLTAGEMODE_OFF_DC,
        SWITCHINGVOLTAGEMODE_OFF_AC,        
        SWITCHINGVOLTAGEMODE_DC_OFF_AC,        
        WARNING_NEUTRAL,
        ORTS_CIRCUIT_BREAKER_STATE_MULTISYSTEM,
        LINE_VOLTAGE_AC,
        LINE_VOLTAGE_DC,
        ORTS_CIRCUIT_BREAKER_CLOSED_AC,
        ORTS_CIRCUIT_BREAKER_CLOSED_DC,
        ORTS_CIRCUIT_BREAKER_OPEN_AC,
        ORTS_CIRCUIT_BREAKER_OPEN_DC,
        QUICK_RELEASE_BUTTON,
        LOWPRESSURE_RELEASE_BUTTON,

        // Further CabViewControlTypes must be added above this line, to avoid their malfunction in 3DCabs
        EXTERNALWIPERS,
        LEFTDOOR,
        RIGHTDOOR,
        MIRRORS
    }

    public enum CABViewControlStyles
    {
        NONE,
        NEEDLE,
        POINTER,
        SOLID,
        LIQUID,
        SPRUNG,
        NOT_SPRUNG,
        WHILE_PRESSED,
        PRESSED,
        ONOFF, 
        _24HOUR, 
        _12HOUR
    }

    public enum CABViewControlUnits
    {
        NONE,
        BAR,
        PSI,
        KILOPASCALS,
        KGS_PER_SQUARE_CM,
        AMPS,
        VOLTS,
        KILOVOLTS,

        KM_PER_HOUR,
        MILES_PER_HOUR, 
        METRESµSECµSEC,
        METRES_SEC_SEC,
        KMµHOURµHOUR,
        KM_HOUR_HOUR,
        KMµHOURµSEC,
        KM_HOUR_SEC,
        METRESµSECµHOUR,
        METRES_SEC_HOUR,
        MILES_HOUR_MIN,
        MILES_HOUR_HOUR,

        NEWTONS, 
        KILO_NEWTONS,
        KILO_LBS,
        METRES_PER_SEC,
        LITRES,
        GALLONS,
        INCHES_OF_MERCURY,
        MILI_AMPS,
        RPM,
        LBS,
        KILOMETRES
    }

    public class CabViewControls : List<CabViewControl>
    {
        public CabViewControls(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);

            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("ortsanimateddisplay", ()=>{ Add(new CVCAnimatedDisplay(stf, basepath)); }),
                new STFReader.TokenProcessor("dial", ()=>{ Add(new CVCDial(stf, basepath)); }),
                new STFReader.TokenProcessor("gauge", ()=>{ Add(new CVCGauge(stf, basepath)); }),
                new STFReader.TokenProcessor("lever", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("twostate", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("tristate", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("multistate", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("multistatedisplay", ()=>{ Add(new CVCMultiStateDisplay(stf, basepath)); }),
                new STFReader.TokenProcessor("cabsignaldisplay", ()=>{ Add(new CVCSignal(stf, basepath)); }), 
                new STFReader.TokenProcessor("digital", ()=>{ Add(new CVCDigital(stf, basepath)); }), 
                new STFReader.TokenProcessor("combinedcontrol", ()=>{ Add(new CVCDiscrete(stf, basepath)); }),
                new STFReader.TokenProcessor("firebox", ()=>{ Add(new CVCFirebox(stf, basepath)); }),
                new STFReader.TokenProcessor("dialclock", ()=>{ ProcessDialClock(stf, basepath);  }),
                new STFReader.TokenProcessor("digitalclock", ()=>{ Add(new CVCDigitalClock(stf, basepath)); }),
                new STFReader.TokenProcessor("screendisplay", ()=>{ Add(new CVCScreen(stf, basepath)); })
            });
            
            //TODO Uncomment when parsed all type
            /*
            if (count != this.Count) STFException.ReportWarning(inf, "CabViewControl count mismatch");
            */
        }

        private void ProcessDialClock(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[]
            {
                new STFReader.TokenProcessor("hours", ()=>{ Add(new CVCDial(CABViewControlTypes.ORTS_HOURDIAL, 12, stf, basepath));  }),
                new STFReader.TokenProcessor("minutes", ()=>{ Add(new CVCDial(CABViewControlTypes.ORTS_MINUTEDIAL, 60, stf, basepath));  }),
                new STFReader.TokenProcessor("seconds", ()=>{ Add(new CVCDial(CABViewControlTypes.ORTS_SECONDDIAL, 60, stf, basepath));  }),
            });
        }
    }

    #region CabViewControl
    public class CabViewControl
    {
        public double PositionX;
        public double PositionY;
        public double Width;
        public double Height;

        public double MinValue;
        public double MaxValue;
        public double OldValue;
        public string ACEFile = "";
        public string Label = "";

        // Icik
        public double UpdateTime;
        public float ElapsedTime;
        public float ElapsedTime2;
        public float PreviousData;
        public float Precision;
        public float Vibration;

        // Jindrich
        public int ControlId = 0;
        public string StringValue = "";
        public int Length = 0;
        public int DisplayID = -1;
        public bool BlankDisplay = false;
        public float ArrayIndex;
        public string PropertyName;
        public string EditablePositionCharacter;
        public string SendChar;
        public float ItemIndex;
        public string MouseHoverCursor = "Hand";
        public int ScreenId = 0;
        public int ContainerGroup = 0;
        public int ActivateScreen = 0;
        public bool IsActive = false;
        public bool IsVisible = true;
        public bool IsEditable = false;
        public int ScreenContainer = 0;
        public string CurrentSource = "";
        public string CurrentType = "";
        public int CurrentSourceID = -1;
        public float MaxNeedleSpeed = 0; // time in seconds from max to min and vice versa
        public double MinValueExtendedPhysics = 0;
        public double MaxValueExtendedPhysics = 0;
        public string DateFormat = "dd.MM.yy";

        public CABViewControlTypes ControlType = CABViewControlTypes.NONE;
        public CABViewControlStyles ControlStyle = CABViewControlStyles.NONE;
        public CABViewControlUnits Units = CABViewControlUnits.NONE;

        protected void ParseType(STFReader stf)
        {
            stf.MustMatch("(");
            try
            {
                ControlType = (CABViewControlTypes)Enum.Parse(typeof(CABViewControlTypes), stf.ReadString());
            }
            catch(ArgumentException)
            {
                stf.StepBackOneItem();
                STFException.TraceInformation(stf, "Skipped unknown ControlType " + stf.ReadString());
                ControlType = CABViewControlTypes.NONE;
            }
            //stf.ReadItem(); // Skip repeated Class Type 
            stf.SkipRestOfBlock();
        }
        protected void ParsePosition(STFReader stf)
        {
            stf.MustMatch("(");
            PositionX = stf.ReadDouble(null);
            PositionY = stf.ReadDouble(null);
            Width = stf.ReadDouble(null);
            Height = stf.ReadDouble(null);

            // Handling middle values
            while (!stf.EndOfBlock())
            {
                STFException.TraceWarning(stf, "Ignored additional positional parameters");
                Width = Height;
                Height = stf.ReadInt(null);
            }
        }
        protected void ParseScaleRange(STFReader stf)
        {
            stf.MustMatch("(");
            MinValue = stf.ReadDouble(null);
            MaxValue = stf.ReadDouble(null);
            stf.SkipRestOfBlock();
        }
        protected void ParseScaleRangeExtendedPhysics(STFReader stf)
        {
            stf.MustMatch("(");
            MinValueExtendedPhysics = stf.ReadDouble(null);
            MaxValueExtendedPhysics = stf.ReadDouble(null);
            stf.SkipRestOfBlock();
        }
        protected void ParseGraphic(STFReader stf, string basepath)
        {
            ACEFile = Path.Combine(basepath, stf.ReadStringBlock(null));
        }
        protected void ParseStyle(STFReader stf)
        {
            stf.MustMatch("(");
            try
            {
                string sStyle = stf.ReadString();
                int checkNumeric = 0;
                if(int.TryParse(sStyle.Substring(0, 1), out checkNumeric) == true)
                {
                    sStyle = sStyle.Insert(0, "_");
                }
                ControlStyle = (CABViewControlStyles)Enum.Parse(typeof(CABViewControlStyles), sStyle);
            }
            catch (ArgumentException)
            {
                stf.StepBackOneItem();
                STFException.TraceInformation(stf, "Skipped unknown ControlStyle " + stf.ReadString());
                ControlStyle = CABViewControlStyles.NONE;
            }
            stf.SkipRestOfBlock();
        }
        protected void ParseUnits(STFReader stf)
        {
            stf.MustMatch("(");
            try
            {
                string sUnits = stf.ReadItem();
                // sUnits = sUnits.Replace('/', '?');
                sUnits = sUnits.Replace('/', '_');
                Units = (CABViewControlUnits)Enum.Parse(typeof(CABViewControlUnits), sUnits);
            }
            catch (ArgumentException)
            {
                stf.StepBackOneItem();
                STFException.TraceInformation(stf, "Skipped unknown ControlStyle " + stf.ReadItem());
                Units = CABViewControlUnits.NONE;
            }
            stf.SkipRestOfBlock();
        }
        // Used by subclasses CVCGauge and CVCDigital
        protected virtual color ParseControlColor( STFReader stf )
        {
            stf.MustMatch("(");
            color colour = new color { A = 1, R = stf.ReadInt(0) / 255f, G = stf.ReadInt(0) / 255f, B = stf.ReadInt(0) / 255f };
            stf.SkipRestOfBlock();
            return colour;
        }
        protected virtual float ParseSwitchVal(STFReader stf)
        {
            stf.MustMatch("(");
            var switchVal = (float)(stf.ReadDouble(0));
            stf.SkipRestOfBlock();
            return switchVal;
        }
        protected virtual float ParseRotation(STFReader stf)
        {
            stf.MustMatch("(");
            var rotation = -MathHelper.ToRadians((float)stf.ReadDouble(0));
            stf.SkipRestOfBlock();
            return rotation;
        }

        protected virtual Point ParseSkew(STFReader stf)
        {
            Point point = new Point(0, 0);
            stf.MustMatch("(");
            point = new Point { X = stf.ReadInt(0), Y = stf.ReadInt(0) };
            stf.SkipRestOfBlock();
            return point;
        }
    }
    #endregion

    #region Dial controls
    public class CVCDial : CabViewControl
    {
        public float FromDegree;
        public float ToDegree;
        public float Center;
        public int Direction;

        // constructor for clock dials
        public CVCDial(CABViewControlTypes dialtype, int maxvalue, STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("pivot", ()=>{ Center = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                });
            ControlType = dialtype;
            ControlStyle = CABViewControlStyles.NEEDLE;
            Direction = 0;
            MaxValue = maxvalue;
            MinValue = 0;
            FromDegree = 181;
            ToDegree = 179;
        }

        // constructor for standard dials
        public CVCDial(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("scalerangeextendedphysics", ()=>{ ParseScaleRangeExtendedPhysics(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),

                new STFReader.TokenProcessor("pivot", ()=>{ Center = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("dirincrease", ()=>{ Direction = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("scalepos", ()=>{
                    stf.MustMatch("(");
                    FromDegree = stf.ReadFloat(STFReader.UNITS.None, null);
                    ToDegree = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("label", ()=>{
                    stf.MustMatch("(");
                    Label = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("updatetime", () =>
                {
                    stf.MustMatch("(");
                    UpdateTime = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("precision", () =>
                {
                    stf.MustMatch("(");
                    Precision = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("vibration", () =>
                {
                    stf.MustMatch("(");
                    Vibration = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("currentsource", () =>
                {
                    stf.MustMatch("(");
                    CurrentSource = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("currentsourceid", () =>
                {
                    stf.MustMatch("(");
                    CurrentSourceID = stf.ReadInt(0);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("currenttype", () =>
                {
                    stf.MustMatch("(");
                    CurrentType = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("maxneedlespeed", () =>
                {
                    stf.MustMatch("(");
                    MaxNeedleSpeed = stf.ReadFloat(STFReader.UNITS.None, 0);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("dateformat", () =>
                {
                    stf.MustMatch("(");
                    DateFormat = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),

            });
        }
    }
    #endregion

    #region Gauges
    public class CVCGauge : CabViewControl
    {
        public Rectangle Area = new Rectangle();
        public int ZeroPos;
        public int Orientation;
        public int Direction;
        public color PositiveColor { get; set; }
        public color SecondPositiveColor { get; set; }
        public float PositiveSwitchVal { get; set; }
        public color NegativeColor { get; set; }
        public float NegativeSwitchVal { get; set; }
        public color SecondNegativeColor { get; set; }
        public int NumPositiveColors { get; set; }
        public int NumNegativeColors { get; set; }
        public color DecreaseColor { get; set; }
        public float Rotation { get; set; }

        public CVCGauge() { }

        public CVCGauge(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("scalerangeextendedphysics", ()=>{ ParseScaleRangeExtendedPhysics(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),

                new STFReader.TokenProcessor("zeropos", ()=>{ ZeroPos = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("orientation", ()=>{ Orientation = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("dirincrease", ()=>{ Direction = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("area", ()=>{ 
                    stf.MustMatch("(");
                    int x = stf.ReadInt(null);
                    int y = stf.ReadInt(null);
                    int width = stf.ReadInt(null);
                    int height = stf.ReadInt(null);
                    Area = new Rectangle(x, y, width, height);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("positivecolour", ()=>{ 
                    stf.MustMatch("(");
                    NumPositiveColors = stf.ReadInt(0);
                    if((stf.EndOfBlock() == false))
                    {
                       List <color> Colorset = new List<color>();
                       stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ Colorset.Add(ParseControlColor(stf));}), 
                            new STFReader.TokenProcessor("switchval", () => { PositiveSwitchVal = ParseSwitchVal(stf); }) });
                    PositiveColor = Colorset [0];
                    if ((NumPositiveColors >= 2) && (Colorset.Count >= 2 ))SecondPositiveColor = Colorset [1];
                    }
                   }),
               new STFReader.TokenProcessor("negativecolour", ()=>{ 
                    stf.MustMatch("(");
                    NumNegativeColors = stf.ReadInt(0);
                    if ((stf.EndOfBlock() == false))
                    {
                        List<color> Colorset = new List<color>();
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ Colorset.Add(ParseControlColor(stf));}), 
                            new STFReader.TokenProcessor("switchval", () => { NegativeSwitchVal = ParseSwitchVal(stf); }) });
                        NegativeColor = Colorset[0];
                        if ((NumNegativeColors >= 2) && (Colorset.Count >= 2)) SecondNegativeColor = Colorset[1];
                     }
                    }),
                new STFReader.TokenProcessor("decreasecolour", ()=>{
                    stf.MustMatch("(");
                    stf.ReadInt(0);
                    if(stf.EndOfBlock() == false)
                    {
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ DecreaseColor = ParseControlColor(stf); }) });
                    }
                }),
                new STFReader.TokenProcessor("ortsangle", () =>{ Rotation = ParseRotation(stf); }),
                new STFReader.TokenProcessor("label", ()=>{
                    stf.MustMatch("(");
                    Label = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class CVCFirebox : CVCGauge
    {
        public string FireACEFile;

        public CVCFirebox(STFReader stf, string basepath) 
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseFireACEFile(stf, basepath); }),
                new STFReader.TokenProcessor("fuelcoal", ()=>{ ParseGraphic(stf, basepath); }),
            });

            Direction = 1;
            Orientation = 1;
            MaxValue = 1;
            MinValue = 0;
            ControlStyle = CABViewControlStyles.POINTER;
            Area = new Rectangle(0, 0, (int)Width, (int)Height);
            PositionY += Height / 2;
        }

        protected void ParseFireACEFile(STFReader stf, string basepath)
        {
            FireACEFile = Path.Combine(basepath, stf.ReadStringBlock(null));
        }

    }
    #endregion

    #region Digital controls
    public class CVCDigital : CabViewControl
    {
        public Point Skew { get; set; }
        public int LeadingZeros { get; set; }
        public double Accuracy { get; set; }
        public double AccuracySwitch { get; set; }
        public int Justification { get; set; }
        public color PositiveColor { get; set; }
        public color SecondPositiveColor { get; set; }
        public float PositiveSwitchVal { get; set; }
        public color NegativeColor { get; set; }
        public float NegativeSwitchVal { get; set; }
        public color SecondNegativeColor { get; set; }
        public int NumPositiveColors { get; set; }
        public int NumNegativeColors { get; set; }
        public color DecreaseColor { get; set; }
        public float FontSize { get; set; }
        public int FontStyle { get; set; }
        public string FontFamily = "";
        public float Rotation { get; set; }

        public CVCDigital()
        {
        }

        public CVCDigital(STFReader stf, string basepath)
        {
            // Set white as the default positive colour for digital displays
            color white = new color();
            white.R = 255f;
            white.G = 255f;
            white.B = 255f;
            PositiveColor = white;
            FontSize = 8;
            FontStyle = 0;
            FontFamily = "Lucida Sans";
            
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("scalerangeextendedphysics", ()=>{ ParseScaleRangeExtendedPhysics(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),
                new STFReader.TokenProcessor("leadingzeros", ()=>{ ParseLeadingZeros(stf); }),
                new STFReader.TokenProcessor("accuracy", ()=>{ ParseAccuracy(stf); }), 
                new STFReader.TokenProcessor("accuracyswitch", ()=>{ ParseAccuracySwitch(stf); }), 
                new STFReader.TokenProcessor("justification", ()=>{ ParseJustification(stf); }),
                new STFReader.TokenProcessor("positivecolour", ()=>{ 
                    stf.MustMatch("(");
                    NumPositiveColors = stf.ReadInt(0);
                    if((stf.EndOfBlock() == false))
                    {
                       List <color> Colorset = new List<color>();
                       stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ Colorset.Add(ParseControlColor(stf));}), 
                            new STFReader.TokenProcessor("switchval", () => { PositiveSwitchVal = ParseSwitchVal(stf); }) });
                    PositiveColor = Colorset [0];
                    if ((NumPositiveColors >= 2) && (Colorset.Count >= 2 ))SecondPositiveColor = Colorset [1];
                    }
                   }),
               new STFReader.TokenProcessor("negativecolour", ()=>{ 
                    stf.MustMatch("(");
                    NumNegativeColors = stf.ReadInt(0);
                    if ((stf.EndOfBlock() == false))
                    {
                        List<color> Colorset = new List<color>();
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ Colorset.Add(ParseControlColor(stf));}), 
                            new STFReader.TokenProcessor("switchval", () => { NegativeSwitchVal = ParseSwitchVal(stf); }) });
                        NegativeColor = Colorset[0];
                        if ((NumNegativeColors >= 2) && (Colorset.Count >= 2)) SecondNegativeColor = Colorset[1];
                     }
                    }),
                new STFReader.TokenProcessor("decreasecolour", ()=>{
                    stf.MustMatch("(");
                    stf.ReadInt(0);
                    if(stf.EndOfBlock() == false)
                    {
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("controlcolour", ()=>{ DecreaseColor = ParseControlColor(stf); }) });
                    }
                }),
                new STFReader.TokenProcessor("ortsfont", ()=>{ParseFont(stf); }),
                new STFReader.TokenProcessor("ortsangle", () => {Rotation = ParseRotation(stf); }),
                new STFReader.TokenProcessor("skew", () => { Skew = ParseSkew(stf); }),
                new STFReader.TokenProcessor("label", ()=>{
                    stf.MustMatch("(");
                    Label = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("updatetime", () =>
                {
                    stf.MustMatch("(");
                    UpdateTime = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("precision", () =>
                {
                    stf.MustMatch("(");
                    Precision = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("displayid", ()=>{
                    stf.MustMatch("(");
                    DisplayID = (int)stf.ReadFloat(STFReader.UNITS.None, 0);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("cursor", ()=>{
                    stf.MustMatch("(");
                    MouseHoverCursor = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("length", ()=>{
                    stf.MustMatch("(");
                    Length = (int)stf.ReadFloat(STFReader.UNITS.None, 0);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("screencontainer", ()=>{
                    stf.MustMatch("(");
                    ScreenContainer = (int)stf.ReadFloat(STFReader.UNITS.None, 0);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("dateformat", () =>
                {
                    stf.MustMatch("(");
                    DateFormat = stf.ReadString();
                    stf.SkipRestOfBlock();
                }),
            });
        }

        protected virtual void ParseLeadingZeros(STFReader stf)
        {
            stf.MustMatch("(");
            LeadingZeros = stf.ReadInt(0);
            stf.SkipRestOfBlock();
        }

        protected virtual void ParseDisplayID(STFReader stf)
        {
            stf.MustMatch("(");
            DisplayID = stf.ReadInt(0);
            stf.SkipRestOfBlock();
        }

        protected virtual void ParseAccuracy(STFReader stf)
        {
            stf.MustMatch("(");
            Accuracy = stf.ReadDouble(0);
            stf.SkipRestOfBlock();
        }

        protected virtual void ParseAccuracySwitch(STFReader stf)
        {
            stf.MustMatch("(");
            AccuracySwitch = stf.ReadDouble(0);
            stf.SkipRestOfBlock();
        }

        protected virtual void ParseJustification(STFReader stf)
        {
            stf.MustMatch("(");
            Justification = stf.ReadInt(3);
            stf.SkipRestOfBlock();
        }

        protected void ParseFont(STFReader stf)
        {
            stf.MustMatch("(");
            FontSize = (float)stf.ReadDouble(10);
            FontStyle = stf.ReadInt(0);
            var fontFamily = stf.ReadString();
            if (fontFamily != null) FontFamily = fontFamily;
            stf.SkipRestOfBlock();
         }
    }

    public class CVCDigitalClock : CVCDigital
    {

        public CVCDigitalClock(STFReader stf, string basepath)
        {
            FontSize = 8;
            FontStyle = 0;
            FontFamily = "Lucida Sans";
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                new STFReader.TokenProcessor("accuracy", ()=>{ ParseAccuracy(stf); }), 
                new STFReader.TokenProcessor("controlcolour", ()=>{ PositiveColor = ParseControlColor(stf); }),
                new STFReader.TokenProcessor("ortsfont", ()=>{ParseFont(stf); }),
                new STFReader.TokenProcessor("ortsangle", () => { Rotation = ParseRotation(stf); }),
                new STFReader.TokenProcessor("displayid", ()=>{ParseDisplayID(stf); })
            });
        }

        
    }
    #endregion

    #region Frames controls
    public abstract class CVCWithFrames : CabViewControl
    {
        private List<double> values = new List<double>();

        public int FramesCount { get; set; }
        public int FramesX { get; set; }
        public int FramesY { get; set; }
        public bool MouseControl;
        public int Orientation;
        public int Direction;

        public List<double> Values 
        {
            get
            {
                return values;
            }
        }
    }

    public class CVCDiscrete : CVCWithFrames
    {
        public List<int> Positions = new List<int>();

        private int _ValuesRead;
        private int numPositions;
        private bool canFill = true;

        public CVCDiscrete(STFReader stf, string basepath)
        {
//            try
            {
                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                    new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                    new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                    new STFReader.TokenProcessor("scalerangeextendedphysics", ()=>{ ParseScaleRangeExtendedPhysics(stf); }),
                    new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                    new STFReader.TokenProcessor("style", ()=>{ ParseStyle(stf); }),
                    new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),
                    new STFReader.TokenProcessor("mousecontrol", ()=>{ MouseControl = stf.ReadBoolBlock(false); }),
                    new STFReader.TokenProcessor("orientation", ()=>{ Orientation = stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("dirincrease", ()=>{ Direction = stf.ReadIntBlock(null); }),

                    new STFReader.TokenProcessor("numframes", ()=>{
                        stf.MustMatch("(");
                        FramesCount = stf.ReadInt(null);
                        FramesX = stf.ReadInt(null);
                        FramesY = stf.ReadInt(null);
                        stf.SkipRestOfBlock();
                    }),
                    // <CJComment> Would like to revise this, as it is difficult to follow and debug.
                    // Can't do that until interaction of ScaleRange, NumFrames, NumPositions and NumValues is more fully specified.
                    // What is needed is samples of data that must be accommodated.
                    // Some decisions appear unwise but they might be a pragmatic solution to a real problem. </CJComment>
                    //
                    // Code accommodates:
                    // - NumValues before NumPositions or the other way round.
                    // - More NumValues than NumPositions and the other way round - perhaps unwise.
                    // - The count of NumFrames, NumValues and NumPositions is ignored - perhaps unwise.
                    // - Abbreviated definitions so that values at intermediate unspecified positions can be omitted.
                    //   Strangely, these values are set to 0 and worked out later when drawing.
                    // Max and min NumValues which don't match the ScaleRange are ignored - perhaps unwise.
                    new STFReader.TokenProcessor("numpositions", ()=>{
                        stf.MustMatch("(");
                        // If Positions are not filled before by Values
                        bool shouldFill = (Positions.Count == 0);
                        numPositions = stf.ReadInt(null); // Number of Positions

                        var minPosition = 0;
                        var positionsRead = 0;
                        while (!stf.EndOfBlock())
                        {
                            int p = stf.ReadInt(null);

                            minPosition = positionsRead == 0 ? p : Math.Min(minPosition, p);  // used to get correct offset
                            positionsRead++;

                            // If Positions are not filled before by Values
                            if (shouldFill) Positions.Add(p);
                        }
                        
                            // If positions do not start at 0, add offset to shift them all so they do.
                            // An example of this is RENFE 400 (from http://www.trensim.com/lib/msts/index.php?act=view&id=186)
                            // which has a COMBINED_CONTROL with:
                            //   NumPositions ( 21 -11 -10 -9 -8 -7 -6 -5 -4 -3 -2 -1 0 1 2 3 4 5 6 7 8 9 )
                            // Also handles definitions with position in reverse order, e.g.
                            //   NumPositions ( 5 8 7 2 1 0 )
                            positionsRead++;

                        if (minPosition < 0)
                        {
                            for (int iPos = 0; iPos <= Positions.Count - 1; iPos++)
                            {
                                Positions[iPos] -= minPosition;
                            }
                        }

                        // This is a hack for SLI locomotives which have the positions listed as "1056964608 0 0 0 ...".
                        if (Positions.Any(p => p > 0xFFFF))
                        {
                            STFException.TraceInformation(stf, "Renumbering cab control positions from zero due to value > 0xFFFF");
                            for (var i = 0; i < Positions.Count; i++)
                                Positions[i] = i;
                        }

                        // Check if eligible for filling

                        if (Positions.Count > 1 && Positions[0] != 0) canFill = false;
                        else
                        {
                            for (var iPos = 1; iPos <= Positions.Count - 1; iPos++)
                            {
                                if (Positions[iPos] > Positions[iPos-1]) continue;
                                canFill = false;
                                break;
                            }
                        }

                        // This is a protection against GP40 locomotives that erroneously have positions pointing beyond frame count limit.

                        if (Positions.Count > 1 && canFill && Positions.Count < FramesCount && Positions[Positions.Count-1] >= FramesCount && Positions[0] == 0)
                        {
                            STFException.TraceInformation(stf, "Some NumPositions entries refer to non-exisiting frames, trying to renumber");
                            Positions[Positions.Count - 1] = FramesCount - 1;
                            for (var iPos = Positions.Count -2 ; iPos >= 1; iPos--)
                            {
                                if ((Positions[iPos] >= FramesCount || Positions[iPos] >= Positions[iPos + 1])) Positions[iPos] = Positions[iPos + 1] - 1;
                                else break;
                            }
                        }

                    }),
                    new STFReader.TokenProcessor("numvalues", ()=>{
                        stf.MustMatch("(");
                        var numValues = stf.ReadDouble(null); // Number of Values
                        while (!stf.EndOfBlock())
                        {
                            double v = stf.ReadDouble(null);
                            // If the Positions are less than expected add new Position(s)
                            while (Positions.Count <= _ValuesRead)
                            {
                                Positions.Add(_ValuesRead);
                            }
                            // Avoid later repositioning, put every value to its Position
                            // But before resize Values if needed
                            if (numValues != numPositions)
                            {
                                while (Values.Count <= Positions[_ValuesRead])
                                {
                                    Values.Add(0);
                                }
                                // Avoid later repositioning, put every value to its Position
                                Values[Positions[_ValuesRead]] = v;
                            }
                            Values.Add(v);
                            _ValuesRead++;
                        }
                    }),
                    new STFReader.TokenProcessor("label", ()=>{
                        stf.MustMatch("(");
                        Label = stf.ReadString();
                        stf.SkipRestOfBlock();
                    }),
                    new STFReader.TokenProcessor("controlid", ()=> { ControlId = stf.ReadIntBlock(0); }),
                });

                // If no ACE, just don't need any fixup
                // Because Values are tied to the image Frame to be shown
                if (string.IsNullOrEmpty(ACEFile)) return;

                // Now, we have an ACE.

                // If read any Values, or the control requires Values to control
                //     The twostate, tristate, signal displays are not in these
                // Need check the Values collection for validity
                if (_ValuesRead > 0 || ControlStyle == CABViewControlStyles.SPRUNG || ControlStyle == CABViewControlStyles.NOT_SPRUNG ||
                    FramesCount  > 0 || (FramesX > 0 && FramesY > 0 ))
                {
                    // Check max number of Frames
                    if (FramesCount == 0)
                    {
                        // Check valid Frame information
                        if (FramesX == 0 || FramesY == 0)
                        {
                            // Give up, it won't work
                            // Because later we won't know how to display frames from that
                            Trace.TraceWarning("Invalid Frames information given for ACE {0} in {1}", ACEFile, stf.FileName);
                            ACEFile = "";
                            return;
                        }

                        // Valid frames info, set FramesCount
                        FramesCount = FramesX * FramesY;
                    }

                    // Now we have an ACE and Frames for it.

                    // Only shuffle data in following cases

                    if (Values.Count != Positions.Count || (Values.Count < FramesCount & canFill)|| ( Values.Count > 0 && Values[0] == Values[Values.Count - 1] && Values[0] == 0))
                    {

                        // Fixup Positions and Values collections first

                        // If the read Positions and Values are not match
                        // Or we didn't read Values but have Frames to draw
                        // Do not test if FramesCount equals Values count, we trust in the creator -
                        //     maybe did not want to display all Frames
                        // (If there are more Values than Frames it will checked at draw time)
                        // Need to fix the whole Values
                        if (Positions.Count != _ValuesRead || (FramesCount > 0 && (Values.Count == 0 || Values.Count == 1)))
                        {
                            //This if clause covers among others following cases:
                            // Case 1 (e.g. engine brake lever of Dash 9):
                            //NumFrames ( 22 11 2 )
			                //NumPositions ( 1 0 )
			                //NumValues ( 1 0 )
			                //Orientation ( 1 )
			                //DirIncrease ( 1 )
			                //ScaleRange ( 0 1 )
                            //
                            // Case 2 (e.g. throttle lever of Acela):
			                //NumFrames ( 25 5 5 )
			                //NumPositions ( 0 )
			                //NumValues ( 0 )
			                //Orientation ( 1 )
			                //DirIncrease ( 1 )
			                //ScaleRange ( 0 1 )
                            //
                            // Clear existing
                            Positions.Clear();
                            Values.Clear();

                            // Add the two sure positions, the two ends
                            Positions.Add(0);
                            // We will need the FramesCount later!
                            // We use Positions only here
                            Positions.Add(FramesCount);

                            // Fill empty Values
                            for (int i = 0; i < FramesCount; i++)
                                Values.Add(0);
                            Values[0] = MinValue;

                            Values.Add(MaxValue);
                        }
                        else if (Values.Count == 2 && Values[0] == 0 && Values[1] < MaxValue && Positions[0] == 0 && Positions[1] == 1 && Values.Count < FramesCount)
                        {
                            //This if clause covers among others following cases:
                            // Case 1 (e.g. engine brake lever of gp38):
			                //NumFrames ( 18 2 9 )
			                //NumPositions ( 2 0 1 )
			                //NumValues ( 2 0 0.3 )
			                //Orientation ( 0 )
			                //DirIncrease ( 0 )
			                //ScaleRange ( 0 1 )
                            Positions.Add(FramesCount);
                            // Fill empty Values
                            for (int i = Values.Count; i < FramesCount; i++)
                                Values.Add(Values[1]);
                            Values.Add(MaxValue);                            
                        }

                        else
                        {
                            //This if clause covers among others following cases:
                            // Case 1 (e.g. train brake lever of Acela): 
			                //NumFrames ( 12 4 3 )
			                //NumPositions ( 5 0 1 9 10 11 )
			                //NumValues ( 5 0 0.2 0.85 0.9 0.95 )
			                //Orientation ( 1 )
			                //DirIncrease ( 1 )
			                //ScaleRange ( 0 1 )
                            //
                            // Fill empty Values
                            int iValues = 1;
                            for (int i = 1; i < FramesCount && i <= Positions.Count - 1 && Values.Count < FramesCount; i++)
                            {
                                var deltaPos = Positions[i] - Positions[i - 1];
                                while (deltaPos > 1 && Values.Count < FramesCount)
                                {

                                    Values.Insert(iValues, 0);
                                    iValues++;
                                    deltaPos--;
                                }
                                iValues++;
                            }

                            // Add the maximums to the end, the Value will be removed
                            // We use Positions only here
                            if (Values.Count > 0 && Values[0] <= Values[Values.Count - 1]) Values.Add(MaxValue);
                            else if (Values.Count > 0 && Values[0] > Values[Values.Count - 1]) Values.Add(MinValue);
                        }

                        // OK, we have a valid size of Positions and Values

                        // Now it is the time for checking holes in the given data
                        if ((Positions.Count < FramesCount - 1 && Values[0] <= Values[Values.Count - 1]) || (Values.Count > 1 && Values[0] == Values[Values.Count - 2] && Values[0] == 0))
                        {
                            int j = 1;
                            int p = 0;
                            // Skip the 0 element, that is the default MinValue
                            for (int i = 1; i < Positions.Count; i++)
                            {
                                // Found a hole
                                if (Positions[i] != p + 1)
                                {
                                    // Iterate to the next valid data and fill the hole
                                    for (j = p + 1; j < Positions[i]; j++)
                                    {
                                        // Extrapolate into the hole
                                        Values[j] = MathHelper.Lerp((float)Values[p], (float)Values[Positions[i]], (float)j / (float)Positions[i]);
                                    }
                                }
                                p = Positions[i];
                            }
                        }

                        // Don't need the MaxValue added before, remove it
                        Values.RemoveAt(Values.Count - 1);
                    }
                }

                // MSTS ignores/overrides various settings by the following exceptional cases:
                if (ControlType == CABViewControlTypes.CP_HANDLE)
                    ControlStyle = CABViewControlStyles.NOT_SPRUNG;
                if (ControlType == CABViewControlTypes.PANTOGRAPH || ControlType == CABViewControlTypes.PANTOGRAPH2 ||
                    ControlType == CABViewControlTypes.ORTS_PANTOGRAPH3 || ControlType == CABViewControlTypes.ORTS_PANTOGRAPH4)
                    ControlStyle = CABViewControlStyles.ONOFF;
                if (ControlType == CABViewControlTypes.HORN || ControlType == CABViewControlTypes.SANDERS || ControlType == CABViewControlTypes.BELL 
                    || ControlType == CABViewControlTypes.RESET || ControlType == CABViewControlTypes.VACUUM_EXHAUSTER)
                    ControlStyle = CABViewControlStyles.WHILE_PRESSED;
                if (ControlType == CABViewControlTypes.DIRECTION && Orientation == 0)
                    Direction = 1 - Direction;
            }
//            catch (Exception error)
//            {
//                if (error is STFException) // Parsing error, so pass it on
//                    throw;
//                else                       // Unexpected error, so provide a hint
//                    throw new STFException(stf, "Problem with NumPositions/NumValues/NumFrames/ScaleRange");
//            } // End of Need check the Values collection for validity
        } // End of Constructor
    }
    #endregion

    #region Multistate Display Controls
    public class CVCMultiStateDisplay : CVCWithFrames
    {
         public List<double> MSStyles = new List<double>();

           public CVCMultiStateDisplay(STFReader stf, string basepath)
        {

            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("scalerangeextendedphysics", ()=>{ ParseScaleRangeExtendedPhysics(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),
                new STFReader.TokenProcessor("updatetime", () =>
                {
                    stf.MustMatch("(");
                    UpdateTime = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),

                new STFReader.TokenProcessor("states", ()=>{
                    stf.MustMatch("(");
                    FramesCount = stf.ReadInt(null);
                    FramesX = stf.ReadInt(null);
                    FramesY = stf.ReadInt(null);
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("state", ()=>{ 
                            stf.MustMatch("(");
                            stf.ParseBlock( new STFReader.TokenProcessor[] {
                                new STFReader.TokenProcessor("style", ()=>{ MSStyles.Add(ParseNumStyle(stf));
                                }),
                                new STFReader.TokenProcessor("switchval", ()=>{ Values.Add(stf.ReadFloatBlock(STFReader.UNITS.None, null))
                                ; }),
                        });}),
                    });
                    if (Values.Count > 0) MaxValue = Values.Last();
                    for (int i = Values.Count; i < FramesCount; i++)
                        Values.Add(-10000);
                }),
            });
        }
        protected int ParseNumStyle(STFReader stf)
        {
            stf.MustMatch("(");
            var style = stf.ReadInt(0);
            stf.SkipRestOfBlock();
            return style;
        }
    }

    public class CVCAnimatedDisplay : CVCWithFrames
    {
        public List<double> MSStyles = new List<double>();
        public float CycleTimeS;

        public CVCAnimatedDisplay(STFReader stf, string basepath)
        {

            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf);  }),
                new STFReader.TokenProcessor("scalerange", ()=>{ ParseScaleRange(stf); }),
                new STFReader.TokenProcessor("scalerangeextendedphysics", ()=>{ ParseScaleRangeExtendedPhysics(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),
                new STFReader.TokenProcessor("ortscycletime", ()=>{
                    CycleTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, null); }),
                new STFReader.TokenProcessor("states", ()=>{
                    stf.MustMatch("(");
                    FramesCount = stf.ReadInt(null);
                    FramesX = stf.ReadInt(null);
                    FramesY = stf.ReadInt(null);
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("state", ()=>{
                            stf.MustMatch("(");
                            stf.ParseBlock( new STFReader.TokenProcessor[] {
                                new STFReader.TokenProcessor("style", ()=>{ MSStyles.Add(ParseNumStyle(stf));
                                }),
                                new STFReader.TokenProcessor("switchval", ()=>{ Values.Add(stf.ReadFloatBlock(STFReader.UNITS.None, null))
                                ; }),
                        });}),
                    });
                    if (Values.Count > 0) MaxValue = Values.Last();
                    for (int i = Values.Count; i < FramesCount; i++)
                        Values.Add(-10000);
                }),
            });
        }
        protected int ParseNumStyle(STFReader stf)
        {
            stf.MustMatch("(");
            var style = stf.ReadInt(0);
            stf.SkipRestOfBlock();
            return style;
        }
    }

    #endregion

    #region Screen based controls
    public class CVCScreen : CabViewControl
    {
        public readonly Dictionary<string, string> CustomParameters = new Dictionary<string, string>();
        public CVCScreen()
        {
        }

        public CVCScreen(STFReader stf, string basepath)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("type", ()=>{ ParseType(stf); }),
                new STFReader.TokenProcessor("position", ()=>{ ParsePosition(stf); }),
                new STFReader.TokenProcessor("graphic", ()=>{ ParseGraphic(stf, basepath); }),
                new STFReader.TokenProcessor("units", ()=>{ ParseUnits(stf); }),
                new STFReader.TokenProcessor("parameters", ()=>{ ParseCustomParameters(stf); }),
            });
        }
        protected void ParseCustomParameters(STFReader stf)
        {
            stf.MustMatch("(");
            while (!stf.EndOfBlock())
            {
                CustomParameters[stf.ReadString().ToLower()] = stf.ReadString().ToLower();
            }
        }
    }
    #endregion

    #region other controls
    public class CVCSignal : CVCDiscrete
    {
        public CVCSignal(STFReader inf, string basepath)
            : base(inf, basepath)
        {
            FramesCount = 8;
            FramesX = 4;
            FramesY = 2;

            MinValue = 0;
            MaxValue = 1;

            Positions.Add(1);
            Values.Add(1);
        }
    }
    #endregion
}

