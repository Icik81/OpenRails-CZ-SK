﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

// Define this to log the wheel configurations on cars as they are loaded.
//#define DEBUG_WHEELS

// Debug car heat losses
// #define DEBUG_CAR_HEATLOSS

// Debug curve speed
// #define DEBUG_CURVE_SPEED

//Debug Tunnel Resistance
//   #define DEBUG_TUNNEL_RESISTANCE

// Debug User SuperElevation
//#define DEBUG_USER_SUPERELEVATION

// Debug Brake Slide Calculations
//#define DEBUG_BRAKE_SLIDE

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using Orts.Simulation.Signalling;
using Orts.Simulation.Timetables;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Event = Orts.Common.Event;


namespace Orts.Simulation.RollingStocks
{
    public class ViewPoint
    {
        public Vector3 Location;
        public Vector3 StartDirection;
        public Vector3 RotationLimit;

        public ViewPoint()
        {
        }

        public ViewPoint(Vector3 location)
        {
            Location = location;
        }

        public ViewPoint(ViewPoint copy, bool rotate)
        {
            Location = copy.Location;
            StartDirection = copy.StartDirection;
            RotationLimit = copy.RotationLimit;
            if (rotate)
            {
                Location.X *= -1;
                Location.Z *= -1;
                /*StartDirection.X += 180;
                StartDirection.Z += 180;*/
            }
        }
    }

    public class PassengerViewPoint : ViewPoint
    {
        // Remember direction of passenger camera and apply when user returns to it.
        public float RotationXRadians;
        public float RotationYRadians;
    }

    public abstract class TrainCar
    {
        public readonly Simulator Simulator;
        public readonly string WagFilePath;
        public string RealWagFilePath; //we are substituting missing remote cars in MP, so need to remember this

        public static int DbfEvalTravellingTooFast;//Debrief eval
        public static int DbfEvalTravellingTooFastSnappedBrakeHose;//Debrief eval
        public bool dbfEvalsnappedbrakehose = false;//Debrief eval
        public bool ldbfevalcurvespeed = false;//Debrief eval
        static float dbfmaxsafecurvespeedmps;//Debrief eval
        public static int DbfEvalTrainOverturned;//Debrief eval
        public bool ldbfevaltrainoverturned = false;

        // original consist of which car was part (used in timetable for couple/uncouple options)
        public string OrgConsist = string.Empty;

        // sound related variables
        public bool IsPartOfActiveTrain = true;
        public List<int> SoundSourceIDs = new List<int>();

        // Used to calculate Carriage Steam Heat Loss - ToDo - ctn_steamer - consolidate these parameters with other steam heat ones, also check as some now may be obsolete
        public Interpolator TrainHeatBoilerWaterUsageGalukpH;
        public Interpolator TrainHeatBoilerFuelUsageGalukpH;

        // Input values to allow the water and fuel usage of steam heating boiler to be calculated based upon Spanner SwirlyFlo Mk111 Boiler
        static float[] SteamUsageLbpH = new float[]
        {
           0.0f, 3000.0f
        };

        // Water Usage
        static float[] WaterUsageGalukpH = new float[]
        {
           0.0f, 300.0f
        };

        // Fuel usage
        static float[] FuelUsageGalukpH = new float[]
        {
           0.0f, 31.0f
        };

        public static Interpolator SteamHeatBoilerWaterUsageGalukpH()
        {
            return new Interpolator(SteamUsageLbpH, WaterUsageGalukpH);
        }

        public static Interpolator SteamHeatBoilerFuelUsageGalukpH()
        {
            return new Interpolator(SteamUsageLbpH, FuelUsageGalukpH);
        }

        public float MainSteamHeatPipeOuterDiaM = Me.FromIn(2.4f); // Steel pipe OD = 1.9" + 0.5" insulation (0.25" either side of pipe)
        public float MainSteamHeatPipeInnerDiaM = Me.FromIn(1.50f); // Steel pipe ID = 1.5"
        public float CarConnectSteamHoseOuterDiaM = Me.FromIn(2.05f); // Rubber hose OD = 2.05"
        public float CarConnectSteamHoseInnerDiaM = Me.FromIn(1.50f); // Rubber hose ID = 1.5"
        public bool IsSteamHeatBoilerLockedOut = false;
        public float MaximumSteamHeatingBoilerSteamUsageRateLbpS;
        public float MaximiumSteamHeatBoilerFuelTankCapacityL = 1500.0f; // Capacity of the fuel tank for the steam heating boiler
        public float CurrentCarSteamHeatBoilerWaterCapacityL;  // Current water level
        public float CurrentSteamHeatBoilerFuelCapacityL;  // Current fuel level - only on steam vans, diesels use main diesel tank
        public float MaximumSteamHeatBoilerWaterTankCapacityL = L.FromGUK(800.0f); // Capacity of the water feed tank for the steam heating boiler
        public float CompartmentHeatingPipeAreaFactor = 3.0f;
        public float DesiredCompartmentTempSetpointC = C.FromF(55.0f); // This is the desired temperature for the passenger compartment heating
        public float WindowDeratingFactor = 0.275f;   // fraction of windows in carriage side - 27.5% of space are windows
        public bool SteamHeatingBoilerOn = false;
        public bool SteamHeatingCompartmentSteamTrapOn = false;
        public float TotalCarCompartmentHeatLossWpT;      // Transmission loss for the wagon
        public float CarHeatCompartmentPipeAreaM2;  // Area of surface of car pipe
        public bool IsCarSteamHeatInitial = true; // Allow steam heat to be initialised.
        public float CarHeatSteamMainPipeHeatLossBTU;  // BTU /hr
        public float CarHeatConnectSteamHoseHeatLossBTU;
        public float CarSteamHeatMainPipeCurrentHeatBTU;
        public float CarSteamHeatMainPipeSteamPressurePSI;
        public float CarCompartmentSteamPipeHeatConvW;
        public float CarCompartmentSteamHeatPipeRadW;
        public float DisplayTrainNetSteamHeatLossWpTime;  // Display Net Steam loss - Loss in Cars vs Steam Pipe Heat
        public bool CarHeatCompartmentHeaterOn = false;
        public float CarHeatSteamTrapUsageLBpS;
        public float CarHeatConnectingSteamHoseLeakageLBpS;
        public float SteamHoseLeakRateRandom;
        public float CarNetSteamHeatLossWpTime;        // Net Steam loss - Loss in Cars vs Steam Pipe Heat
        public float CarHeatCompartmentSteamPipeHeatW; // Heat generated by steam exchange area in compartment
        public float CarHeatCurrentCompartmentHeatW;
        public float CarCurrentCarriageHeatTempC;
        public float TotalPossibleCarHeatW; // Total possible heat of the car, based upon the desired car temperature
        public float CarCurrentCarriageHeatDeltaTempC;

        // some properties of this car
        public float CarWidthM = 2.5f;
        public float CarLengthM = 40;       // derived classes must overwrite these defaults
        public float CarHeightM = 4;        // derived classes must overwrite these defaults
        public float MassKG = 10000;        // Mass in KG at runtime; coincides with InitialMassKG if there is no load and no ORTS freight anim
        public float InitialMassKG = 10000;
        public bool IsDriveable;
        public bool HasFreightAnim = false;
        public bool HasPassengerCapacity = false;
        public int PassengerCapacity = 0;
        public int PassengerLoad = 0;
        public int ExitQueue = 0;
        public int EnterQueue = 0;
        public List<Passenger> PassengerList = new List<Passenger>();

        public bool UnboardingComplete = true;
        public bool BoardingComplete = false;
        public float FirstPaxActionDelay = 0;
        public bool HasInsideView = false;
        public float CarHeightAboveSeaLevelM;

        public float MaxHandbrakeForceN;
        public float MaxBrakeForceN = 89e3f;
        public float InitialMaxHandbrakeForceN;  // Initial force when agon initialised
        public float InitialMaxBrakeForceN = 89e3f;   // Initial force when agon initialised

        // Coupler Animation
        public string FrontCouplerShapeFileName;
        public float FrontCouplerAnimLengthM;
        public float FrontCouplerAnimWidthM;
        public float FrontCouplerAnimHeightM;

        public string FrontCouplerOpenShapeFileName;
        public float FrontCouplerOpenAnimLengthM;
        public float FrontCouplerOpenAnimWidthM;
        public float FrontCouplerOpenAnimHeightM;
        public bool FrontCouplerOpenFitted = false;
        public bool FrontCouplerOpen = false;

        public string RearCouplerShapeFileName;
        public float RearCouplerAnimLengthM;
        public float RearCouplerAnimWidthM;
        public float RearCouplerAnimHeightM;

        public string RearCouplerOpenShapeFileName;
        public float RearCouplerOpenAnimLengthM;
        public float RearCouplerOpenAnimWidthM;
        public float RearCouplerOpenAnimHeightM;
        public bool RearCouplerOpenFitted = false;
        public bool RearCouplerOpen = false;

        // Used to calculate Carriage Steam Heat Loss
        public float CarHeatLossWpT;      // Transmission loss for the wagon
        public float CarHeatVolumeM3;     // Volume of car for heating purposes
        public float CarHeatPipeAreaM2;  // Area of surface of car pipe
        public float CarOutsideTempC;   // Ambient temperature outside of car
        public float InitialCarOutsideTempC;
        public bool IsTrainHeatingBoilerInitialised { get { return Train.TrainHeatingBoilerInitialised; } set { Train.TrainHeatingBoilerInitialised = value; } }

        // Used to calculate wheel sliding for locked brake
        public bool BrakeSkid = false;
        public bool HUDBrakeSkid = false;
        public float BrakeShoeCoefficientFriction = 1.0f; // Brake Shoe coefficient - for simple adhesion model set to 1
        public float BrakeShoeCoefficientFrictionAdjFactor = 1.0f; // Factor to adjust Brake force by - based upon changing friction coefficient with speed, will change when wheel goes into skid
        public float BrakeShoeRetardCoefficientFrictionAdjFactor = 1.0f; // Factor of adjust Retard Brake force by - independent of skid
        float DefaultBrakeShoeCoefficientFriction;  // A default value of brake shoe friction is no user settings are present.
        float BrakeWheelTreadForceN; // The retarding force apparent on the tread of the wheel
        float WagonBrakeAdhesiveForceN; // The adhesive force existing on the wheels of the wagon
        public float SkidFriction = 0.16f; // Friction if wheel starts skidding - based upon wheel dynamic friction of approx 0.08

        public float AuxTenderWaterMassKG;    // Water mass in auxiliary tender
        public string AuxWagonType;           // Store wagon type for use with auxilary tender calculations

        public LightCollection Lights;
        public FreightAnimations FreightAnimations;
        public int[] Headlight = new int[3];

        // instance variables set by train physics when it creates the traincar
        public Train Train;  // the car is connected to this train
                             //        public bool IsPlayerTrain { get { return Train.TrainType == ORTS.Train.TRAINTYPE.PLAYER ? true : false; } set { } }
        public bool IsPlayerTrain {
            get
            {
                if (Train == null)
                    Train = Simulator.playerTTTrain;
                return Train.IsPlayerDriven;
            }
            set { }
        }

        public bool Flipped; // the car is reversed in the consist
        public int UiD;
        public string CarID = "AI"; //CarID = "0 - UID" if player train, "ActivityID - UID" if loose consist, "AI" if AI train

        // status of the traincar - set by the train physics after it calls TrainCar.Update()
        public WorldPosition WorldPosition = new WorldPosition();  // current position of the car
        public float DistanceM;  // running total of distance travelled - always positive, updated by train physics
        public float _SpeedMpS; // meters per second; updated by train physics, relative to direction of car  50mph = 22MpS
        public float _PrevSpeedMpS;
        public float AbsSpeedMpS; // Math.Abs(SpeedMps) expression is repeated many times in the subclasses, maybe this deserves a class variable
        public float CouplerSlackM;  // extra distance between cars (calculated based on relative speeds)
        public int HUDCouplerForceIndication = 0; // Flag to indicate whether coupler is 1 - pulling, 2 - pushing or 0 - neither
        public float CouplerSlack2M;  // slack calculated using draft gear force
        public bool IsAdvancedCoupler = false; // Flag to indicate that coupler is to be treated as an advanced coupler
        public float FrontCouplerSlackM; // Slack in car front coupler
        public float RearCouplerSlackM;  // Slack in rear coupler

        public float AdvancedCouplerDynamicTensionSlackLimitM;   // Varies as coupler moves
        public float AdvancedCouplerDynamicCompressionSlackLimitM; // Varies as coupler moves

        public bool WheelSlip;  // true if locomotive wheels slipping
        public bool WheelSlipWarning;
        public bool WheelSkid;  // True if wagon wheels lock up.
        public float _AccelerationMpSS;
        protected IIRFilter AccelerationFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, 1.0f, 0.1f);

        // Wheel Bearing Temperature parameters
        public float WheelBearingTemperatureDegC = 40.0f;
        public string DisplayWheelBearingTemperatureStatus;
        public float WheelBearingTemperatureRiseTimeS = 0;
        public float HotBoxTemperatureRiseTimeS = 0;
        public float WheelBearingTemperatureDeclineTimeS = 0;
        public float InitialWheelBearingDeclineTemperatureDegC;
        public float InitialWheelBearingRiseTemperatureDegC;
        public float InitialHotBoxRiseTemperatureDegS;
        public bool WheelBearingFailed = false;
        public bool WheelBearingHot = false;
        public bool HotBoxActivated = false;
        public bool HotBoxHasBeenInitialized = false;
        public bool HotBoxSoundActivated = false;
        public float HotBoxDelayS;
        public float ActivityHotBoxDurationS;
        public float ActivityElapsedDurationS;
        public float HotBoxStartTimeS;

        // Icik        
        public bool VibrationType_1 = false;
        public bool VibrationType_2 = false;
        public bool VibrationType_3 = false;
        public int Factor_vibration;
        public int direction1 = 0;
        public int direction2 = 0;
        public float LocoBrakeAdhesiveForceN;
        public float MaxBrakeForceNRMg;
        public float RMgShoeCoefficientFrictionAdjFactor;
        public float DefaultRMgShoeCoefficientFriction;
        public float PowerReductionByHeating;
        public float PowerReductionByAirCondition;
        public float PowerReductionByHeating0;
        public float PowerReductionByHeatingSum;
        public float PowerReductionByAirCondition0;
        public float PowerReductionByAuxEquipment;
        public float PowerReductionByAuxEquipment0;
        public float PowerReductionByAuxEquipmentSum;
        public float VibrationSpringConstantPrimepSpS = 0;
        public float VibratioDampingCoefficient = 0;
        public float WagonTemperature;
        public float SteamLocoCabTemperatureBase;
        public float TempCDelta;
        public float TempCDeltaAir;
        public bool ThermostatOn = false;
        public float ThermostatCoef;
        public float SetTempCThreshold;
        public float SetTemperatureCHeat;
        public float SetTemperatureCFrost;
        public bool WagonHasTemperature = false;
        public float CarOutsideTempC0;
        public bool StatusHeatIsOn = false;
        public float CarOutsideTempCBase = -1000;
        public float CarOutsideTempCBaseFinish;
        public float CarOutsideTempCLastStatus;
        public float TempCClockDelta;
        public bool LocomotiveCab = false;
        public bool SteamHeatOn;
        public bool[] RDSTBreaker = new bool[3];
        public bool[] CabHeating_OffOn = new bool[3];
        public bool CabHeatingIsOn = false;
        public bool AIStart;
        public bool PowerUnit;
        public bool ControlUnit;
        public bool PowerUnitWithControl;
        public float RouteVoltageVInfo;
        public bool SelectedCar;
        public bool StartLooseCon;
        public bool AuxPowerOff;
        public bool LocoHelperOn;
        public bool UserPowerOff;
        public bool CarLightsPowerOn;
        public bool CarIsPlayerLoco;
        public bool CarIsFlipped;
        public bool CarIsWaiting;
        public bool CarIsShunting;
        public bool CarIsRunning;
        public bool CarCabHeatingIsSetOn;
        public float AICompressorStartDelay;
        public float WheelDamageValue;
        public float PrevWheelDamageValue;
        public bool CouplerPull;
        public bool CarSoundLoaded;
        public float PullPushValue;
        public float TrackFactorValue;
        public bool GenSoundOff;
        public bool LightFrontLW;
        public bool LightFrontRW;
        public bool LightRearLW;
        public bool LightRearRW;
        public bool LightFrontLR;
        public bool LightFrontRR;
        public bool LightRearLR;
        public bool LightRearRR;
        public bool HasWagonSmoke;
        public bool HasWagonSteamHeatingElements;
        public bool WagonCanEnableSteamHeating;
        public bool WagonHasStove;
        public bool WagonHasSteamHeating;
        public bool CarPowerKey;
        public int CarFrameUpdateState = 1;
        public bool DirectionControllerBlocked;
        public int[] PowerKeyPosition = new int[3];
        public int[] prevPowerKeyPosition = new int[3];
        public bool CarHavePocketPowerKey;
        public int LightFrontLPosition;
        public int LightFrontRPosition;
        public int LightRearLPosition;
        public int LightRearRPosition;
        public bool FrontHeadLight;
        public bool RearHeadLight;
        public bool CarSteamHeatOn;
        public string WagonName;
        public float SlipSpeedDiference;
        public bool CarHasHeatingReady;
        public bool CarHasBrakePipeConnected;
        public bool MasterLoco;
        public bool SlaveLoco;
        public int WagonRealPantoCount;
        public bool WagonIsStatic;
        public int MPBrakeCarMode = 0;
        public int MPBrakeCarModePL = 0;

        public float PowerReductionResult1;  // Redukce výkonu od topení, klimatizace, kompresoru
        public float PowerReductionResult2;  // Redukce výkonu od nedostatečného tlaku vzduchu v potrubí
        public float PowerReductionResult3;  // Redukce výkonu při nadproudu topení, klimatizace 
        public float PowerReductionResult4;  // Redukce výkonu při nadproudu pohonu, skluz 
        public float PowerReductionResult5;  // Redukce výkonu při tlaku v brzdovém válci 
        public float PowerReductionResult6;  // Redukce výkonu při motoru pod provozní teplotou
        public float PowerReductionResult7;  // Odpojení TM při selhání
        public float PowerReductionResult8;  // Odpojení TM při nezapnuté RDST
        public float PowerReductionResult9;  // Redukce výkonu při poškozeném motoru 
        public float PowerReductionResult10;  // Redukce výkonu při vypnutém proudu
        public float PowerReductionResult11;  // Redukce výkonu při vypnutém pultu řízení
        public float PowerReductionResult12;  // Redukce výkonu pro postrk při Don`t push!
        public float PowerReductionResult13;  // Redukce výkonu při odblokování automatických dveří
        public float PowerReductionResult14;  // Redukce výkonu při vypnutí trakce

        public float DieselHeaterPower;
        public float DieselHeaterPower0;
        public float DieselHeaterConsumptionPerHour;
        public float DieselHeaterConsumptionPerHour0;
        public float DieselHeaterTankCapacity;
        public float DieselHeaterTankCapacityMark;

        // Setup for ambient temperature dependency
        Interpolator OutsideWinterTempbyLatitudeC;
        Interpolator OutsideAutumnTempbyLatitudeC;
        Interpolator OutsideSpringTempbyLatitudeC;
        Interpolator OutsideSummerTempbyLatitudeC;
        public bool AmbientTemperatureInitialised;

        // Input values to allow the temperature for different values of latitude to be calculated
        static float[] WorldLatitudeDeg = new float[]
        {
           -50.0f, -40.0f, -30.0f, -20.0f, -10.0f, 0.0f, 10.0f, 20.0f, 30.0f, 40.0f, 50.0f, 60.0f
        };

        // Temperature in deg Celcius
        static float[] WorldTemperatureWinter = new float[]
        {
            0.9f, 8.7f, 12.4f, 17.2f, 20.9f, 25.9f, 22.8f, 18.2f, 11.1f, -1.1f, -5.2f, -10.7f
         };

        static float[] WorldTemperatureAutumn = new float[]
        {
            7.5f, 13.7f, 18.8f, 22.0f, 24.0f, 26.0f, 25.0f, 21.6f, 18.0f, 15.3f, 13.0f, 8.8f
         };

        static float[] WorldTemperatureSpring = new float[]
        {
            8.5f, 13.1f, 17.6f, 18.6f, 24.6f, 25.9f, 26.8f, 23.4f, 20.5f, 18.6f, 15.5f, 10.7f
         };

        static float[] WorldTemperatureSummer = new float[]
        {
            23.4f, 28.3f, 32.8f, 34.3f, 34.4f, 35.0f, 35.2f, 32.5f, 36.6f, 34.8f, 29.4f, 24.3f
         };

        public static Interpolator WorldWinterLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureWinter);
        }

        public static Interpolator WorldAutumnLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureAutumn);
        }

        public static Interpolator WorldSpringLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureSpring);
        }

        public static Interpolator WorldSummerLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureSummer);
        }

        public bool AcceptMUSignals = false; //indicates if the car accepts multiple unit signals
        public bool AcceptHelperSignals = false;
        public bool AcceptPowerSignals = true;
        public bool AcceptCableSignals = false;
        public bool IsMetric;
        public bool IsUK;
        public float prevElev = -100f;

        public float SpeedMpS
        {
            get
            {
                return _SpeedMpS;
            }
            set
            {
                _SpeedMpS = value;
            }
        }

        public float AccelerationMpSS
        {
            get { return _AccelerationMpSS; }
        }

        public float LocalThrottlePercent;
        // represents the MU line travelling through the train.  Uncontrolled locos respond to these commands.
        public float ThrottlePercent
        {
            get
            {
                if (AcceptMUSignals && Train != null)
                {
                    return Train.MUThrottlePercent;
                }
                else
                    return LocalThrottlePercent;
            }
            set
            {
                if (AcceptMUSignals && Train != null)
                    Train.MUThrottlePercent = value;
                else
                    LocalThrottlePercent = value;
            }
        }

        public int LocalGearboxGearIndex;
        public int GearboxGearIndex
        {
            get
            {
                if (AcceptMUSignals)
                    return Train.MUGearboxGearIndex;
                else
                    return LocalGearboxGearIndex;
            }
            set
            {
                if (AcceptMUSignals)
                    Train.MUGearboxGearIndex = value;
                else
                    LocalGearboxGearIndex = value;
            }
        }

        public float LocalDynamicBrakePercent;
        public float DynamicBrakePercent
        {
            get
            {
                if (AcceptMUSignals && Train != null)
                {
                    if (Train.LeadLocomotive != null && ((MSTSLocomotive)Train.LeadLocomotive).TrainControlSystem.FullDynamicBrakingOrder)
                    {
                        return 100;
                    }
                    else
                    {
                        return Train.MUDynamicBrakePercent;
                    }
                }
                else
                    return LocalDynamicBrakePercent;
            }
            set
            {
                // Icik
                if (AcceptMUSignals && Train != null && CarIsPlayerLoco)
                    Train.MUDynamicBrakePercent = value;
                else
                    LocalDynamicBrakePercent = value;
            }
        }
        public Direction Direction
        {
            //TODO: following code lines have been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
            // To achieve the same result with other means, without flipping trainset physics, the code lines probably should be changed
            get
            {
                if (IsDriveable && Train.IsActualPlayerTrain)
                {
                    var loco = this as MSTSLocomotive;
                    return Flipped ^ loco.UsingRearCab ? DirectionControl.Flip(Train.MUDirection) : Train.MUDirection;
                }
                else
                {
                    return Flipped ? DirectionControl.Flip(Train.MUDirection) : Train.MUDirection;
                }
            }
            set
            {
                var loco = this as MSTSLocomotive;
                Train.MUDirection = Flipped ^ loco.UsingRearCab ? DirectionControl.Flip(value) : value;
            }
        }
        public BrakeSystem BrakeSystem;

        public float PreviousSteamBrakeCylinderPressurePSI;

        protected float motiveForceN = 0;
        // TrainCar.Update() must set these variables
        public float MotiveForceN 
        {
            get
            {
                return motiveForceN;
            }
            set
            {
                motiveForceN = value;
                if (GetType() == typeof(MSTSElectricLocomotive))
                {
                    var myLoco = (MSTSElectricLocomotive)this;
                    if (myLoco.LocoType == MSTSLocomotive.LocoTypes.Vectron)
                    {
                        motiveForceN  = value * 1.1f;
                    }
                }
            }
        }   // ie motor power in Newtons  - signed relative to direction of car -
        public float TractiveForceN = 0f; // Raw tractive force for electric sound variable2
        public SmoothedData MotiveForceSmoothedN = new SmoothedData(0.5f);
        public float PrevMotiveForceN;
        // Gravity forces have negative values on rising grade. 
        // This means they have the same sense as the motive forces and will push the train downhill.
        public float GravityForceN;  // Newtons  - signed relative to direction of car.
        public float CurveForceN;   // Resistive force due to curve, in Newtons
        public float WindForceN;  // Resistive force due to wind
        public float DynamicBrakeForceN = 0f; // Raw dynamic brake force for diesel and electric locomotives

        // Derailment variables
        public float WagonVerticalDerailForceN; // Vertical force of wagon/car - essentially determined by the weight
        public float TotalWagonLateralDerailForceN;
        public float LateralWindForceN;
        public float WagonFrontCouplerAngleRad;
        //        public float WagonVerticalForceN; // Vertical force of wagon/car - essentially determined by the weight

        public bool BuffForceExceeded;

        // filter curve force for audio to prevent rapid changes.
        //private IIRFilter CurveForceFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, 1.0f, 0.9f);
        protected SmoothedData CurveForceFilter = new SmoothedData(0.75f);
        public float CurveForceNFiltered;

        public float TunnelForceN;  // Resistive force due to tunnel, in Newtons
        public float FrictionForceN; // in Newtons ( kg.m/s^2 ) unsigned, includes effects of curvature
        public float BrakeForceN;    // brake force applied to slow train (Newtons) - will be impacted by wheel/rail friction
        public float BrakeRetardForceN;    // brake force applied to wheel by brakeshoe (Newtons) independent of friction wheel/rail friction

        // Sum of all the forces acting on a Traincar in the direction of driving.
        // MotiveForceN and GravityForceN act to accelerate the train. The others act to brake the train.
        public float TotalForceN; // 

        public string CarBrakeSystemType;

        public float CurrentElevationPercent;

        public bool CurveResistanceDependent;
        public bool CurveSpeedDependent;
        public bool TunnelResistanceDependent;


        protected float MaxDurableSafeCurveSpeedMpS;

        // temporary values used to compute coupler forces
        public float CouplerForceA; // left hand side value below diagonal
        public float CouplerForceB; // left hand side value on diagonal
        public float CouplerForceC; // left hand side value above diagonal
        public float CouplerForceG; // temporary value used by solver
        public float CouplerForceR; // right hand side value
        public float CouplerForceU; // result
        public float ImpulseCouplerForceUN;
        public SmoothedData CouplerForceUSmoothed = new SmoothedData(1.0f);
        public float PreviousCouplerSlackM;
        public float SmoothedCouplerForceUN;
        public bool CouplerExceedBreakLimit; //true when coupler force is higher then Break limit (set by 2nd parameter in Break statement)
        public bool CouplerOverloaded; //true when coupler force is higher then Proof limit, thus overloaded, but not necessarily broken (set by 1nd parameter in Break statement)
        public bool BrakesStuck; //true when brakes stuck

        // set when model is loaded
        public List<WheelAxle> WheelAxles = new List<WheelAxle>();
        public bool WheelAxlesLoaded;
        public List<TrainCarPart> Parts = new List<TrainCarPart>();

        // For use by cameras, initialized in MSTSWagon class and its derived classes
        public List<PassengerViewPoint> PassengerViewpoints = new List<PassengerViewPoint>();
        public List<PassengerViewPoint> CabViewpoints; //three dimensional cab view point
        public List<ViewPoint> HeadOutViewpoints = new List<ViewPoint>();

        // Used by Curve Speed Method
        protected float TrackGaugeM = 1.435f;  // Track gauge - read in MSTSWagon
        protected Vector3 InitialCentreOfGravityM = new Vector3(0, 1.8f, 0); // get centre of gravity - read in MSTSWagon
        protected Vector3 CentreOfGravityM = new Vector3(0, 1.8f, 0); // get centre of gravity after adjusted for freight animation
        protected float SuperelevationM; // Super elevation on the curve
        protected float UnbalancedSuperElevationM;  // Unbalanced superelevation, read from MSTS Wagon File
        protected float SuperElevationTotalM; // Total superelevation
        protected bool IsMaxSafeCurveSpeed = false; // Has equal loading speed around the curve been exceeded, ie are all the wheesl still on the track?
        public bool IsCriticalMaxSpeed = false; // Has the critical maximum speed around the curve been reached, is the wagon about to overturn?
        public bool IsCriticalMinSpeed = false; // Is the speed less then the minimum required for the wagon to travel around the curve
        protected float MaxCurveEqualLoadSpeedMps; // Max speed that rolling stock can do whist maintaining equal load on track
        protected float StartCurveResistanceFactor = 2.0f; // Set curve friction at Start = 200%
        protected float RouteSpeedMpS; // Max Route Speed Limit
        protected const float GravitationalAccelerationMpS2 = 9.80665f; // Acceleration due to gravity 9.80665 m/s2
        public int WagonNumAxles; // Number of axles on a wagon
        protected float MSTSWagonNumWheels; // Number of axless on a wagon - used to read MSTS value as default
        protected int LocoNumDrvAxles; // Number of drive axles on locomotive
        protected float MSTSLocoNumDrvWheels; // Number of drive axles on locomotive - used to read MSTS value as default
        public float DriverWheelRadiusM = Me.FromIn(30.0f); // Drive wheel radius of locomotive wheels - Wheel radius of loco drive wheels can be anywhere from about 10" to 40".

        public enum SteamEngineTypes
        {
            Unknown,
            Simple,
            Geared,
            Compound,
        }

        public SteamEngineTypes SteamEngineType;

        public enum WagonTypes
        {
            Unknown,
            Engine,
            Tender,
            Passenger,
            Freight,
        }
        public WagonTypes WagonType;

        public enum EngineTypes
        {
            Steam,
            Diesel,
            Electric,
            Control,
        }
        public EngineTypes EngineType;

        public enum WagonSpecialTypes
        {
            Unknown,
            HeatingBoiler,
            Heated,
            PowerVan,
        }
        public WagonSpecialTypes WagonSpecialType;

        protected float CurveResistanceZeroSpeedFactor = 0.5f; // Based upon research (Russian experiments - 1960) the older formula might be about 2x actual value
        protected float RigidWheelBaseM;   // Vehicle rigid wheelbase, read from MSTS Wagon file
        protected float TrainCrossSectionAreaM2; // Cross sectional area of the train
        protected float DoubleTunnelCrossSectAreaM2;
        protected float SingleTunnelCrossSectAreaM2;
        protected float DoubleTunnelPerimeterM;
        protected float SingleTunnelPerimeterAreaM;
        protected float TunnelCrossSectionAreaM2 = 0.0f;
        protected float TunnelPerimeterM = 0.0f;

        // used by tunnel processing
        public struct CarTunnelInfoData
        {
            public float? FrontPositionBeyondStartOfTunnel;          // position of front of wagon wrt start of tunnel
            public float? LengthMOfTunnelAheadFront;                 // Length of tunnel remaining ahead of front of wagon (negative if front of wagon out of tunnel)
            public float? LengthMOfTunnelBehindRear;                 // Length of tunnel behind rear of wagon (negative if rear of wagon has not yet entered tunnel)
            public int numTunnelPaths;                               // Number of paths through tunnel
        }

        public CarTunnelInfoData CarTunnelData;

        public virtual void Initialize()
        {
            CurveResistanceDependent = Simulator.Settings.CurveResistanceDependent;
            CurveSpeedDependent = Simulator.Settings.CurveSpeedDependent;
            TunnelResistanceDependent = Simulator.Settings.TunnelResistanceDependent;

            //CurveForceFilter.Initialize();
            // Initialize tunnel resistance values

            DoubleTunnelCrossSectAreaM2 = (float)Simulator.TRK.Tr_RouteFile.DoubleTunnelAreaM2;
            SingleTunnelCrossSectAreaM2 = (float)Simulator.TRK.Tr_RouteFile.SingleTunnelAreaM2;
            DoubleTunnelPerimeterM = (float)Simulator.TRK.Tr_RouteFile.DoubleTunnelPerimeterM;
            SingleTunnelPerimeterAreaM = (float)Simulator.TRK.Tr_RouteFile.SingleTunnelPerimeterM;

            // get route speed limit
            RouteSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;

            // if no values are in TRK file, calculate default values.
            // Single track Tunnels

            if (SingleTunnelCrossSectAreaM2 == 0)
            {

                if (RouteSpeedMpS >= 97.22) // if route speed greater then 350km/h
                {
                    SingleTunnelCrossSectAreaM2 = 70.0f;
                    SingleTunnelPerimeterAreaM = 32.0f;
                }
                else if (RouteSpeedMpS >= 69.4 && RouteSpeedMpS < 97.22) // Route speed greater then 250km/h and less then 350km/h
                {
                    SingleTunnelCrossSectAreaM2 = 70.0f;
                    SingleTunnelPerimeterAreaM = 32.0f;
                }
                else if (RouteSpeedMpS >= 55.5 && RouteSpeedMpS < 69.4) // Route speed greater then 200km/h and less then 250km/h
                {
                    SingleTunnelCrossSectAreaM2 = 58.0f;
                    SingleTunnelPerimeterAreaM = 28.0f;
                }
                else if (RouteSpeedMpS >= 44.4 && RouteSpeedMpS < 55.5) // Route speed greater then 160km/h and less then 200km/h
                {
                    SingleTunnelCrossSectAreaM2 = 50.0f;
                    SingleTunnelPerimeterAreaM = 25.5f;
                }
                else if (RouteSpeedMpS >= 33.3 && RouteSpeedMpS < 44.4) // Route speed greater then 120km/h and less then 160km/h
                {
                    SingleTunnelCrossSectAreaM2 = 42.0f;
                    SingleTunnelPerimeterAreaM = 22.5f;
                }
                else       // Route speed less then 120km/h
                {
                    SingleTunnelCrossSectAreaM2 = 21.0f;  // Typically older slower speed designed tunnels
                    SingleTunnelPerimeterAreaM = 17.8f;
                }
            }

            // Double track Tunnels

            if (DoubleTunnelCrossSectAreaM2 == 0)
            {

                if (RouteSpeedMpS >= 97.22) // if route speed greater then 350km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 100.0f;
                    DoubleTunnelPerimeterM = 37.5f;
                }
                else if (RouteSpeedMpS >= 69.4 && RouteSpeedMpS < 97.22) // Route speed greater then 250km/h and less then 350km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 100.0f;
                    DoubleTunnelPerimeterM = 37.5f;
                }
                else if (RouteSpeedMpS >= 55.5 && RouteSpeedMpS < 69.4) // Route speed greater then 200km/h and less then 250km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 90.0f;
                    DoubleTunnelPerimeterM = 35.0f;
                }
                else if (RouteSpeedMpS >= 44.4 && RouteSpeedMpS < 55.5) // Route speed greater then 160km/h and less then 200km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 80.0f;
                    DoubleTunnelPerimeterM = 34.5f;
                }
                else if (RouteSpeedMpS >= 33.3 && RouteSpeedMpS < 44.4) // Route speed greater then 120km/h and less then 160km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 76.0f;
                    DoubleTunnelPerimeterM = 31.0f;
                }
                else       // Route speed less then 120km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 41.8f;  // Typically older slower speed designed tunnels
                    DoubleTunnelPerimeterM = 25.01f;
                }
            }

#if DEBUG_TUNNEL_RESISTANCE
                Trace.TraceInformation("================================== TrainCar.cs - Tunnel Resistance Initialisation ==============================================================");
                Trace.TraceInformation("Tunnel 1 tr perimeter {0} Tunnel 1 tr area {1}", SingleTunnelPerimeterAreaM, SingleTunnelPerimeterAreaM);
                Trace.TraceInformation("Tunnel 2 tr perimeter {0} Tunnel 2 tr area {1}", DoubleTunnelPerimeterM, DoubleTunnelCrossSectAreaM2);
#endif

        }

        // Icik
        public virtual void AntiSkidSystem()
        {
            if (BrakeSystem.AntiSkidSystemEquipped)
            {
                if (BrakeSkid)
                    BrakeSystem.BailOffOnAntiSkid = true;
                else
                    BrakeSystem.BailOffOnAntiSkid = false;
                if (AbsSpeedMpS < 0.01) BrakeSystem.BailOffOnAntiSkid = false;
            }
        }

        // Icik                
        public virtual void BrakeMassKG()
        {                        
            if (!BrakeSystem.BrakeCarDeactivate && BrakeSystem.CarHasMechanicStuckBrake_1)
            {
                BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassG / 3;
                return;
            }

            BrakeSystem.BrakeMassKG = 0;
            BrakeSystem.BrakeMassKGRMg = 0;

            if (BrakeSystem.BrakeCarDeactivate)
            {
                BrakeSystem.BleedOffValveOpen = true;
                return;
            }

            if (BrakeSystem.TwoStateBrake)  // Pokud bude TwoState brzda, použije se brzdící váha pro režim R
                BrakeSystem.BrakeMassKG_TwoStateBrake = BrakeSystem.BrakeMassR;

            if (WagonType == WagonTypes.Passenger || WagonType == WagonTypes.Engine || WagonType == WagonTypes.Unknown)    //  Osobní vozy, lokomotivy a ostatní
                switch (BrakeSystem.BrakeCarMode)
                {
                    case 0: // Režim G  
                        if (BrakeSystem.BrakeMassG == 0) BrakeSystem.BrakeMassKG = BrakeSystem.CoefMode * MassKG;
                        else BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassG;
                        break;
                    case 1: // Režim P  
                        if (BrakeSystem.BrakeMassP == 0) BrakeSystem.BrakeMassKG = BrakeSystem.CoefMode * MassKG;
                        else BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassP;
                        break;
                    case 2: // Režim R  
                        if (BrakeSystem.BrakeMassR == 0) BrakeSystem.BrakeMassKG = BrakeSystem.CoefMode * MassKG;
                        else BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassR;
                        break;
                    case 3: // Režim R+Mg  
                        if ((Math.Abs(SpeedMpS) * 3.6f) > 50.0f
                        && BrakeSystem.AirForWagon
                        && BrakeSystem.BrakeLine1PressurePSI < BrakeSystem.BrakePipePressureForMGbrakeActivationPSI)
                        //&& BrakeSystem.PowerForRMg 
                        //&& BrakeSystem.EmergencyBrakeForRMg)                        
                        {
                            BrakeSystem.BrakeModeRMgActive = true;
                            if (BrakeSystem.BrakeMassRMg != 0)
                            {
                                BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassR;
                                BrakeSystem.BrakeMassKGRMg = BrakeSystem.BrakeMassRMg - BrakeSystem.BrakeMassR;
                            }
                            else
                            if (BrakeSystem.BrakeMassR != 0)
                            {
                                BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassR;
                                BrakeSystem.BrakeMassKGRMg = (1.44f * BrakeSystem.BrakeMassR) - BrakeSystem.BrakeMassR;
                            }

                            else
                            {
                                BrakeSystem.BrakeMassKG = BrakeSystem.CoefMode * MassKG;
                                BrakeSystem.BrakeMassKGRMg = (1.44f * BrakeSystem.CoefMode * MassKG) - (BrakeSystem.CoefMode * MassKG);
                            }
                        }
                        else
                        {
                            BrakeSystem.BrakeModeRMgActive = false;
                            if (BrakeSystem.BrakeMassR == 0) BrakeSystem.BrakeMassKG = BrakeSystem.CoefMode * MassKG;
                            else BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassR;
                        }
                        break;
                }

            if (WagonType == WagonTypes.Freight || WagonType == WagonTypes.Tender)    //  Nákladní vozy a tendry            
                switch (BrakeSystem.BrakeCarModePL)
                {
                    case 0: // Režim Prázdný  
                        if (BrakeSystem.BrakeMassEmpty == 0) BrakeSystem.BrakeMassKG = BrakeSystem.CoefMode * MassKG;
                        else BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassEmpty;
                        break;
                    case 1: // Režim Ložený  
                        if (BrakeSystem.BrakeMassLoaded == 0) BrakeSystem.BrakeMassKG = BrakeSystem.CoefMode * MassKG;
                        else BrakeSystem.BrakeMassKG = BrakeSystem.BrakeMassLoaded;
                        break;
                }
        }

        // Icik
        bool ChanceToProblemWithBrake;
        float ChanceToProblemWithBrakeTimer;
        public void BrakeCarStatus()
        {
            if (!IsPlayerTrain)
                return;

            // Závada se může stát i během jízdy
            if (Simulator.GameTime > 60)
                ChanceToProblemWithBrakeTimer += Simulator.OneSecondLoop; 

            if (ChanceToProblemWithBrakeTimer > 60 * 60)
            {
                BrakeSystem.BrakeCarHasStatus = (Simulator.Random.Next(0, 2)) == 1 ? false : true;                
                ChanceToProblemWithBrakeTimer = 0;
            }

            if (BrakeSystem.CarHasAirStuckBrake_1 || BrakeSystem.CarHasAirStuckBrake_2 || BrakeSystem.CarHasAirStuckBrake_3
                || BrakeSystem.CarHasMechanicStuckBrake_1 || BrakeSystem.CarHasMechanicStuckBrake_2)
                BrakeSystem.CarHasProblemWithBrake = true;

            var car = this as MSTSWagon;
            if (!BrakeSystem.BrakeCarHasStatus 
                && !BrakeSystem.CarHasProblemWithBrake
                && WagonType != WagonTypes.Engine 
                && WagonType != WagonTypes.Passenger 
                && car != Train.Cars[Train.Cars.Count - 1] 
                && Train.Cars.Count > 5)
            {
                BrakeSystem.BrakeCarHasStatus = true;
                switch (Simulator.Random.Next(0, 300))
                {                    
                    case 150:
                        BrakeSystem.CarHasAirStuckBrake_1 = true; // Nejde odbrzdit                        
                        break;
                    case 151:
                        BrakeSystem.CarHasAirStuckBrake_2 = true; // Nejde zabrzdit                        
                        break;
                    case 152:
                        BrakeSystem.CarHasAirStuckBrake_3 = true; // Netěsný vůz                        
                        break;
                    case 153:
                        BrakeSystem.CarHasMechanicStuckBrake_1 = true; // Brzdí málo                        
                        break;
                    case 154:
                        BrakeSystem.CarHasMechanicStuckBrake_2 = true; // Je zaseklý
                        WheelDamageValue = Simulator.Random.Next(5, 15);                                                                       
                        break;
                }
                if (Simulator.GameTime == 0)
                {
                    int ChanceToWheelDamageValue = Simulator.Random.Next(0, 20);
                    if (ChanceToWheelDamageValue == 10)
                        WheelDamageValue = Simulator.Random.Next(0, 10);
                }
            }        
        }
        
        // called when it's time to update the MotiveForce and FrictionForce
        public virtual void Update(float elapsedClockSeconds)
        {
            // Icik
            if (float.IsNaN(MassKG))
                MassKG = InitialMassKG;

            BrakeCarStatus();

            if (Simulator.GameTimeCyklus10 == 10)
            {                
                foreach (TrainCar car in Train.Cars)
                {
                    if (car is MSTSLocomotive)
                        continue;
                    else                    
                        car.CarLightsPowerOn = true;

                    // Detekce vozu s kamny
                    if (car.WagonSpecialType == MSTSWagon.WagonSpecialTypes.Heated)
                    {                                                
                        if (car.HasWagonSmoke)
                            car.WagonHasStove = true;
                    }                    
                    if (!IsPlayerTrain)
                    {
                        if (CarHasHeatingReady)
                        {
                            car.BrakeSystem.HeatingIsOn = true;
                            car.BrakeSystem.HeatingMenu = 0;

                        }
                        // Nad 20°C budou kamna vypnutá
                        if (car.WagonHasStove)
                        {
                            car.BrakeSystem.HeatingIsOn = false;
                            car.BrakeSystem.HeatingMenu = 1;
                            if (car.CarOutsideTempC < 20)
                            {
                                car.BrakeSystem.HeatingIsOn = true;
                                car.BrakeSystem.HeatingMenu = 0;
                            }                            
                        }
                    }
                }

                // Výpočet max brzdné síly
                float CoefE = 0.84f; // Lokomotiva
                float CoefP = 0.60f; // Osobní vůz
                float CoefF = 0.60f; // Nákladní vůz
                float CoefHB = 0.50f; // Ruční brzda

                switch (WagonNumAxles)
                {
                    case 2:
                        CoefE /= 1.25f;
                        CoefP /= 1.25f;
                        CoefF /= 1.25f;
                        break;
                    case 4:
                        CoefE = 0.84f;
                        CoefP = 0.60f;
                        CoefF = 0.60f;
                        break;
                }
                
                AntiSkidSystem();
                BrakeSystem.WagonType = (int)WagonType;

                // DebugKoef pro doladění MaxBrakeForce
                if (BrakeSystem.DebugKoef1 == 0) BrakeSystem.DebugKoef1 = 1.0f;
                BrakeSystem.DebugKoef = BrakeSystem.DebugKoef1 * BrakeSystem.GetDebugKoef2();

                if (WagonType == WagonTypes.Freight || WagonType == WagonTypes.Tender)    //  Nákladní vozy a tendry
                {
                    if (!BrakeSystem.AutoLoadRegulatorEquipped)
                        switch (BrakeSystem.BrakeCarModePL)
                        {
                            case 0: // Režim Prázdný                     
                                BrakeSystem.CoefMode = 1.00f;
                                BrakeMassKG();
                                break;
                            case 1: // Režim Ložený                    
                                BrakeSystem.CoefMode = 0.58f;
                                BrakeMassKG();
                                break;
                        }
                    else
                        if (MassKG < BrakeSystem.AutoLoadRegulatorMaxBrakeMass)
                        BrakeSystem.BrakeMassKG = MassKG;
                    else BrakeSystem.BrakeMassKG = BrakeSystem.AutoLoadRegulatorMaxBrakeMass;

                    if (BrakeSystem.DebugKoef == 1) MaxBrakeForceN = CoefF * BrakeSystem.BrakeMassKG * 9.964016384f * 0.31f;
                    else MaxBrakeForceN = BrakeSystem.DebugKoef * BrakeSystem.BrakeMassKG * 9.964016384f * 0.31f;
                }
                if (WagonType == WagonTypes.Passenger)    //  Osobní vozy
                {
                    switch (BrakeSystem.BrakeCarMode)
                    {
                        case 0: // Režim G                     
                            BrakeSystem.CoefMode = 0.0f;
                            BrakeMassKG();
                            break;
                        case 1: // Režim P                    
                            BrakeSystem.CoefMode = 1.05f;
                            switch (WagonNumAxles)
                            {
                                case 2:
                                    BrakeSystem.CoefMode = 1.25f;
                                    break;
                                case 4:
                                    BrakeSystem.CoefMode = 1.05f;
                                    break;
                            }
                            BrakeMassKG();
                            break;
                        case 2: // Režim R
                            BrakeSystem.CoefMode = 1.49f;
                            BrakeMassKG();
                            break;
                        case 3: // Režim R+Mg
                            BrakeSystem.CoefMode = 1.49f;
                            BrakeMassKG();
                            break;
                    }
                    if (BrakeSystem.DebugKoef == 1)
                    {
                        MaxBrakeForceN = CoefP * BrakeSystem.BrakeMassKG * 9.964016384f * 0.31f;
                        MaxBrakeForceNRMg = CoefP * BrakeSystem.BrakeMassKGRMg * 9.964016384f * 0.31f;
                    }
                    else
                    {
                        MaxBrakeForceN = BrakeSystem.DebugKoef * BrakeSystem.BrakeMassKG * 9.964016384f * 0.31f;
                        MaxBrakeForceNRMg = BrakeSystem.DebugKoef * BrakeSystem.BrakeMassKGRMg * 9.964016384f * 0.31f;
                        if (BrakeSystem.TwoStateBrake)
                            MaxBrakeForceN = BrakeSystem.DebugKoef * BrakeSystem.BrakeMassKG_TwoStateBrake * 9.964016384f * 0.31f;
                    }
                }
                if (WagonType == WagonTypes.Engine || WagonType == WagonTypes.Unknown)    //  Lokomotivy a ostatní
                {
                    switch (BrakeSystem.BrakeCarMode)
                    {
                        case 0: // Režim G                     
                            BrakeSystem.CoefMode = 0.5f;
                            BrakeMassKG();
                            break;
                        case 1: // Režim P                    
                            BrakeSystem.CoefMode = 0.65f;
                            switch (WagonNumAxles)
                            {
                                case 2:
                                    BrakeSystem.CoefMode = 1.12f;
                                    break;
                                case 4:
                                    BrakeSystem.CoefMode = 0.65f;
                                    break;
                            }
                            BrakeMassKG();
                            break;
                        case 2: // Režim R
                            BrakeSystem.CoefMode = 1.66f;
                            BrakeMassKG();
                            break;
                        case 3: // Režim R+Mg
                            BrakeSystem.CoefMode = 1.66f;
                            BrakeMassKG();
                            break;
                    }
                    if (BrakeSystem.DebugKoef == 1)
                    {
                        MaxBrakeForceN = CoefE * BrakeSystem.BrakeMassKG * 9.964016384f * 0.31f;
                        MaxBrakeForceNRMg = CoefE * BrakeSystem.BrakeMassKGRMg * 9.964016384f * 0.31f;
                    }
                    else
                    {
                        MaxBrakeForceN = BrakeSystem.DebugKoef * BrakeSystem.BrakeMassKG * 9.964016384f * 0.31f;
                        MaxBrakeForceNRMg = BrakeSystem.DebugKoef * BrakeSystem.BrakeMassKGRMg * 9.964016384f * 0.31f;
                        if (BrakeSystem.TwoStateBrake)
                            MaxBrakeForceN = BrakeSystem.DebugKoef * BrakeSystem.BrakeMassKG_TwoStateBrake * 9.964016384f * 0.31f;
                    }
                }

                // Výpočet pro ruční brzdu
                MaxHandbrakeForceN = CoefHB * (MassKG / 6.4f) * 9.964016384f * 0.31f;


                // Initialise ambient temperatures on first initial loop, then ignore
                if (!AmbientTemperatureInitialised)
                {
                    InitializeCarTemperatures();
                    AmbientTemperatureInitialised = true;
                }

                // Update temperature variation for height of car above sea level
                float TemperatureHeightVariationDegC = 0;
                TemperatureHeightVariationDegC = Me.ToKiloM(CarHeightAboveSeaLevelM) * 5.5f;
                TemperatureHeightVariationDegC = MathHelper.Clamp(TemperatureHeightVariationDegC, 0.00f, 30.0f);

                float TClock = (float)Simulator.ClockTime / 60 / 60;
                while (TClock > 24) TClock = TClock - 24;

                float[] TimeOfDayCoef = new float[7];
                switch (Simulator.Season)
                {
                    //  Hodiny                           0        3       6       9      12      15      18      21
                    case SeasonType.Spring:
                        TimeOfDayCoef = new float[] { -0.30f, -0.35f, -0.30f, -0.20f, +0.10f, +0.20f, -0.10f, -0.20f };
                        break;
                    case SeasonType.Summer:
                        TimeOfDayCoef = new float[] { -0.30f, -0.35f, -0.25f, -0.10f, +0.10f, +0.20f, -0.10f, -0.20f };
                        break;
                    case SeasonType.Autumn:
                        TimeOfDayCoef = new float[] { -0.40f, -0.60f, -0.50f, -0.30f, +0.10f, +0.20f, -0.20f, -0.30f };
                        break;
                    case SeasonType.Winter:
                        TimeOfDayCoef = new float[] { -0.40f, -0.50f, -0.60f, -0.40f, +0.10f, +0.20f, -0.10f, -0.30f };
                        break;
                }

                // Přírůstek teploty v závislosti na denním čase
                if (TClock > 0 && TClock <= 3)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[0]);
                if (TClock > 3 && TClock <= 6)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[1]);
                if (TClock > 6 && TClock <= 9)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[2]);
                if (TClock > 9 && TClock <= 12)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[3]);
                if (TClock > 12 && TClock <= 15)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[4]);
                if (TClock > 15 && TClock <= 18)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[5]);
                if (TClock > 18 && TClock <= 21)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[6]);
                if (TClock > 21 && TClock <= 24)
                    TempCClockDelta = (Math.Abs(InitialCarOutsideTempC) * TimeOfDayCoef[7]);

                // Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("TClock " + TClock));                            
                CarOutsideTempCBaseFinish = (InitialCarOutsideTempC - TemperatureHeightVariationDegC + TempCClockDelta) * Simulator.OvercastAmbientLightCoef;
                CarOutsideTempCBaseFinish = MathHelper.Clamp(CarOutsideTempCBaseFinish, -25, 40);

                //Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("CarOutsideTempCBaseFinish " + CarOutsideTempCBaseFinish));

                if (CarOutsideTempCBase == -1000)
                    CarOutsideTempCBase = CarOutsideTempCBaseFinish;

                if (CarOutsideTempCBase > CarOutsideTempCBaseFinish)
                    CarOutsideTempCBase -= 0.01f * elapsedClockSeconds * Simulator.TimeSpeedCoef;
                else
                if (CarOutsideTempCBase < CarOutsideTempCBaseFinish)
                    CarOutsideTempCBase += 0.01f * elapsedClockSeconds * Simulator.TimeSpeedCoef;

                // Okolní teplota závisí na podmínkách (oblačno, déšť, mlha)
                switch (Simulator.Season)
                {
                    case SeasonType.Spring:
                        CarOutsideTempC = CarOutsideTempCBase - (Simulator.Weather.PricipitationIntensityPPSPM2 * 2) - (1 - (Simulator.Weather.FogDistance / 100000 * 15));                        
                        break;
                    case SeasonType.Summer:
                        CarOutsideTempC = CarOutsideTempCBase - (Simulator.Weather.PricipitationIntensityPPSPM2 * 2) - (1 - (Simulator.Weather.FogDistance / 100000 * 15));                        
                        break;
                    case SeasonType.Autumn:
                        CarOutsideTempC = CarOutsideTempCBase - (Simulator.Weather.PricipitationIntensityPPSPM2 * 1) - (1 - (Simulator.Weather.FogDistance / 100000 * 15));                        
                        break;
                    case SeasonType.Winter:
                        CarOutsideTempC = CarOutsideTempCBase - (Simulator.Weather.PricipitationIntensityPPSPM2 * 1) - (1 - (Simulator.Weather.FogDistance / 100000 * 15));                        
                        break;
                }
            }

            // gravity force, M32 is up component of forward vector
            GravityForceN = MassKG * GravitationalAccelerationMpS2 * WorldPosition.XNAMatrix.M32;
            CurrentElevationPercent = 100f * WorldPosition.XNAMatrix.M32;
            AbsSpeedMpS = Math.Abs(_SpeedMpS);

            //TODO: next if block has been inserted to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
            // To achieve the same result with other means, without flipping trainset physics, the block should be deleted
            //      
            if (IsDriveable && Train != null & Train.IsPlayerDriven && (this as MSTSLocomotive).UsingRearCab)
            {
                GravityForceN = -GravityForceN;
                CurrentElevationPercent = -CurrentElevationPercent;
            }

            UpdateCurveSpeedLimit(); // call this first as it will provide inputs for the curve force.
            UpdateCurveForce(elapsedClockSeconds);
            UpdateTunnelForce();
            UpdateBrakeSlideCalculation(elapsedClockSeconds);
            //UpdateTrainDerailmentRisk();

            CalculatePositionInTunnel();

            if (this is MSTSLocomotive) // Set train outside temperature the same as the locomotive - TO BE RECHECKED
            {
                Train.TrainOutsideTempC = CarOutsideTempC;
            }

            // acceleration
            if (elapsedClockSeconds > 0.0f)
            {
                _AccelerationMpSS = (_SpeedMpS - _PrevSpeedMpS) / elapsedClockSeconds;

                if (Simulator.UseAdvancedAdhesion)
                    _AccelerationMpSS = AccelerationFilter.Filter(_AccelerationMpSS, elapsedClockSeconds);

                _PrevSpeedMpS = _SpeedMpS;
            }
        }

        /// <summary>
        /// Initialise Train Temperatures
        /// <\summary>           
        public void InitializeCarTemperatures()
        {
            OutsideWinterTempbyLatitudeC = WorldWinterLatitudetoTemperatureC();
            OutsideAutumnTempbyLatitudeC = WorldAutumnLatitudetoTemperatureC();
            OutsideSpringTempbyLatitudeC = WorldSpringLatitudetoTemperatureC();
            OutsideSummerTempbyLatitudeC = WorldSummerLatitudetoTemperatureC();

            // Find the latitude reading and set outside temperature
            double latitude = 0;
            double longitude = 0;

            new Orts.Common.WorldLatLon().ConvertWTC(WorldPosition.TileX, WorldPosition.TileZ, WorldPosition.Location, ref latitude, ref longitude);

            float LatitudeDeg = MathHelper.ToDegrees((float)latitude);


            // Sets outside temperature dependent upon the season
            if (Simulator.Season == SeasonType.Winter)
            {
                // Winter temps
                InitialCarOutsideTempC = OutsideWinterTempbyLatitudeC[LatitudeDeg];
            }
            else if (Simulator.Season == SeasonType.Autumn)
            {
                // Autumn temps
                InitialCarOutsideTempC = OutsideAutumnTempbyLatitudeC[LatitudeDeg];
            }
            else if (Simulator.Season == SeasonType.Spring)
            {
                // Spring temps
                InitialCarOutsideTempC = OutsideSpringTempbyLatitudeC[LatitudeDeg];
            }
            else
            {
                // Summer temps
                InitialCarOutsideTempC = OutsideSummerTempbyLatitudeC[LatitudeDeg];
            }            

            // Initialise wheel bearing temperature to ambient temperature
            if (WheelBearingTemperatureDegC == 0)
                WheelBearingTemperatureDegC = InitialCarOutsideTempC;

            if (InitialWheelBearingRiseTemperatureDegC == 0)
                InitialWheelBearingRiseTemperatureDegC = InitialCarOutsideTempC;
            
            if (InitialWheelBearingDeclineTemperatureDegC == 0)
                InitialWheelBearingDeclineTemperatureDegC = InitialCarOutsideTempC;
        }

        #region Calculate Brake Skid

        /// <summary>
        /// This section calculates:
        /// i) Changing brake shoe friction coefficient due to changes in speed
        /// ii) force on the wheel due to braking, and whether sliding will occur.
        /// 
        /// </summary>

        float PulseTracker;
        int NextPulse = 1;
        public float TrackFactor = 1;
        public virtual void UpdateBrakeSlideCalculation(float elapsedClockSeconds)
        {
            // WheelDamage 
            if (this is MSTSSteamLocomotive)
            {
                // Pro parní trakci nepočítej pulsy
            }
            else
            {
                if (!BrakeSkid && (this as MSTSWagon).AbsWheelSpeedMpS > 0 && WheelDamageValue > 0)
                {
                    // Variable1 is proportional to angular speed, value of 10 means 1 rotation/second.                                        
                    var variable1 = Math.Abs((this as MSTSWagon).WheelSpeedMpS / DriverWheelRadiusM / MathHelper.Pi * 5);
                    const int rotations = 2;
                    const int fullLoop = 10 * rotations;
                    int numPulses = 4 * 2 * rotations;
                    var dPulseTracker = variable1 / fullLoop * numPulses * elapsedClockSeconds;
                    PulseTracker += dPulseTracker;
                    if (PulseTracker > (float)NextPulse - dPulseTracker / 2)
                    {
                        SignalEvent((Event)((int)Event.SteamPulse1 + NextPulse - 1));
                        PulseTracker %= numPulses;
                        NextPulse %= numPulses;
                        NextPulse++;
                    }
                }
            }

            // Only apply slide, and advanced brake friction, if advanced adhesion is selected, and it is a Player train
            if (Simulator.UseAdvancedAdhesion && IsPlayerTrain)
            {
                // Get user defined brake shoe coefficient if defined in WAG file
                float UserFriction = GetUserBrakeShoeFrictionFactor();
                float ZeroUserFriction = GetZeroUserBrakeShoeFrictionFactor();
                float AdhesionMultiplier = Simulator.Settings.AdhesionFactor / 100.0f; // User set adjustment factor - convert to a factor where 100% = no change to adhesion

                // This section calculates an adjustment factor for the brake force dependent upon the "base" (zero speed) friction value. 
                //For a user defined case the base value is the zero speed value from the curve entered by the user.
                // For a "default" case where no user data has been added to the WAG file, the base friction value has been assumed to be 0.2, thus maximum value of 20% applied.
                if (UserFriction != 0)  // User defined friction has been applied in WAG file - Assume MaxBrakeForce is correctly set in the WAG, so no adjustment required 
                {
                    BrakeShoeCoefficientFrictionAdjFactor = UserFriction / ZeroUserFriction * AdhesionMultiplier; // Factor calculated by normalising zero speed value on friction curve applied in WAG file
                    BrakeShoeRetardCoefficientFrictionAdjFactor = UserFriction / ZeroUserFriction * AdhesionMultiplier;
                    BrakeShoeCoefficientFriction = UserFriction * AdhesionMultiplier; // For display purposes on HUD
                    //BrakeShoeRetardCoefficientFrictionAdjFactor = BrakeShoeCoefficientFriction / 0.2f * AdhesionMultiplier;
                }
                else
                // User defined friction NOT applied in WAG file - Assume MaxBrakeForce is incorrectly set in the WAG, so adjustment is required 
                {
                    DefaultBrakeShoeCoefficientFriction = (7.6f / (MpS.ToKpH(AbsSpeedMpS) + 17.5f) + 0.07f) * AdhesionMultiplier; // Base Curtius - Kniffler equation - u = 0.50, all other values are scaled off this formula
                    BrakeShoeCoefficientFrictionAdjFactor = DefaultBrakeShoeCoefficientFriction / 0.2f * AdhesionMultiplier;  // Assuming that current MaxBrakeForce has been set with an existing Friction Coff of 0.2f, an adjustment factor needs to be developed to reduce the MAxBrakeForce by a relative amount
                    BrakeShoeRetardCoefficientFrictionAdjFactor = DefaultBrakeShoeCoefficientFriction / 0.2f * AdhesionMultiplier;
                    BrakeShoeCoefficientFriction = DefaultBrakeShoeCoefficientFriction * AdhesionMultiplier;  // For display purposes on HUD
                }

                // Icik                                
                if (BrakeSystem.BrakeModeRMgActive)
                {
                    float UserFrictionRMg = GetUserRMgShoeFrictionFactor();
                    float ZeroUserFrictionRMg = GetZeroUserRMgShoeFrictionFactor();
                    if (UserFrictionRMg != 0)  // User defined friction has been applied in WAG file - Assume MaxBrakeForce is correctly set in the WAG, so no adjustment required 
                    {
                        RMgShoeCoefficientFrictionAdjFactor = UserFrictionRMg / ZeroUserFrictionRMg * AdhesionMultiplier;
                    }
                    else
                    {
                        DefaultRMgShoeCoefficientFriction = (7.6f / (MpS.ToKpH(AbsSpeedMpS) + 17.5f) + 0.07f) * AdhesionMultiplier;
                        RMgShoeCoefficientFrictionAdjFactor = DefaultRMgShoeCoefficientFriction / 0.18f * AdhesionMultiplier;
                    }
                    RMgShoeCoefficientFrictionAdjFactor = MathHelper.Clamp(RMgShoeCoefficientFrictionAdjFactor, 0.01f, 1.0f);
                }

                // Clamp adjustment factor to a value of 1.0 - i.e. the brakeforce can never exceed the Brake Force value defined in the WAG file                
                BrakeShoeCoefficientFrictionAdjFactor = MathHelper.Clamp(BrakeShoeCoefficientFrictionAdjFactor, 0.01f, 1.0f);
                BrakeShoeRetardCoefficientFrictionAdjFactor = MathHelper.Clamp(BrakeShoeRetardCoefficientFrictionAdjFactor, 0.01f, 1.0f);

                var PlayerLoco = this as MSTSLocomotive;
                if (PlayerLoco != null)
                {
                    if (!BrakeSkid)
                    {
                        if (PlayerLoco.WheelSpeedMpS_Cab < PlayerLoco.WheelSpeedMpS)
                            PlayerLoco.WheelSpeedMpS_Cab += 10f / 3.6f * elapsedClockSeconds;
                        if (PlayerLoco.WheelSpeedMpS_Cab > PlayerLoco.WheelSpeedMpS)
                        {
                            PlayerLoco.WheelSpeedMpS_Cab = PlayerLoco.WheelSpeedMpS;
                            //if (PlayerLoco.extendedPhysics != null)
                            //{
                            //    foreach (Undercarriage uc in PlayerLoco.extendedPhysics.Undercarriages)
                            //    {
                            //        foreach (ExtendedAxle ea in uc.Axles)
                            //        {
                            //            if (ea.HaveSpeedometerSensor)
                            //                PlayerLoco.WheelSpeedMpS_Cab = Math.Abs(ea.WheelSpeedMpS);                                       
                            //        }                                    
                            //    }
                            //}
                        }
                    }
                    else
                    {                        
                        if (PlayerLoco.WheelSpeedMpS_Cab > 0)
                            PlayerLoco.WheelSpeedMpS_Cab -= 10f / 3.6f * elapsedClockSeconds;
                        if (PlayerLoco.WheelSpeedMpS_Cab < 0)
                            PlayerLoco.WheelSpeedMpS_Cab = 0;
                    }
                }
                
                // WheelDamage - plošky na kolech při zaseknutých kolech                
                if (BrakeSkid)
                {
                    // Spustí trigger BrakeSkid
                    if (WheelDamageValue == 0)
                        this.SignalEvent(Event.BrakeSkidStart);
                    WheelDamageValue += 0.1f * elapsedClockSeconds * (1.2f - Simulator.Weather.PricipitationIntensityPPSPM2) * (1 - (1 - (this.MassKG / 85000)));                    
                }
                else
                {
                    // Ukončí trigger BrakeSkid
                    if (WheelDamageValue != PrevWheelDamageValue)
                        this.SignalEvent(Event.BrakeSkidStop);
                    PrevWheelDamageValue = WheelDamageValue;
                }

                // Brake Skid
                // Výpočet adheze pro lokomotivy a vozy                    
                LocoBrakeAdhesiveForceN = 0;
                WagonBrakeAdhesiveForceN = 0;
                if (this is MSTSDieselLocomotive || this is MSTSElectricLocomotive || this is MSTSSteamLocomotive)
                    LocoBrakeAdhesiveForceN = MassKG * GravitationalAccelerationMpS2 * Train.LocomotiveCoefficientFriction + Math.Abs(TractiveForceN); // Adheze pro lokomotivy
                else
                    WagonBrakeAdhesiveForceN = MassKG * GravitationalAccelerationMpS2 * Train.WagonCoefficientFriction; // Adheze pro vozy
                           
                float BrakeAdhesiveForceN = LocoBrakeAdhesiveForceN + WagonBrakeAdhesiveForceN;

                // Test if wheel forces are high enough to induce a slip. Set slip flag if slip occuring 
                if (!BrakeSkid && AbsSpeedMpS > 0.01)  // Train must be moving forward to experience skid
                {
                    if (BrakeRetardForceN > BrakeAdhesiveForceN)
                    {
                        BrakeSkid = true;   // wagon wheel is slipping
                        var message = Simulator.Catalog.GetStringFmt("Car ID: ") + CarID + Simulator.Catalog.GetStringFmt(" - experiencing braking force wheel skid.");
                        string TreeLeavesMSG = "";
                        if (PlayerLoco != null && PlayerLoco.TreeLeavesLevel > 0)
                            TreeLeavesMSG = Simulator.Catalog.GetString("Tree leaves on track!");
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, message + "   " + TreeLeavesMSG);
                    }
                }
                else if (BrakeSkid && AbsSpeedMpS > 0.01)
                {
                    if (BrakeRetardForceN < BrakeAdhesiveForceN || BrakeForceN == 0.0f)
                    {
                        BrakeSkid = false;  // wagon wheel is not slipping
                    }
                }
                else
                {
                    BrakeSkid = false;  // wagon wheel is not slipping
                }
            }
            else  // set default values if simple adhesion model, or if diesel or electric locomotive is used, which doesn't check for brake skid.
            {
                BrakeSkid = false; 	// wagon wheel is not slipping
                BrakeShoeCoefficientFrictionAdjFactor = 1.0f;  // Default value set to leave existing brakeforce constant regardless of changing speed
                BrakeShoeRetardCoefficientFrictionAdjFactor = 1.0f;
                BrakeShoeCoefficientFriction = 1.0f;  // Default value for display purposes

            }

#if DEBUG_BRAKE_SLIDE

            Trace.TraceInformation("================================== Brake Force Slide (TrainCar.cs) ===================================");
            Trace.TraceInformation("Brake Shoe Friction- Car: {0} Speed: {1} Brake Force: {2} Advanced Adhesion: {3}", CarID, MpS.ToMpH(SpeedMpS), BrakeForceN, Simulator.UseAdvancedAdhesion);
            Trace.TraceInformation("BrakeSkidCheck: {0}", BrakeSkidCheck);
            Trace.TraceInformation("Brake Shoe Friction- Coeff: {0} Adjust: {1}", BrakeShoeCoefficientFriction, BrakeShoeCoefficientFrictionAdjFactor);
            Trace.TraceInformation("Brake Shoe Force - Ret: {0} Adjust: {1} Skid {2} Adj {3}", BrakeRetardForceN, BrakeShoeRetardCoefficientFrictionAdjFactor, BrakeSkid, SkidFriction);
            Trace.TraceInformation("Tread: {0} Adhesive: {1}", BrakeWheelTreadForceN, WagonBrakeAdhesiveForceN);
            Trace.TraceInformation("Mass: {0} Rail Friction: {1}", MassKG, Train.WagonCoefficientFriction);
#endif

        }


        #endregion

        #region Calculate resistance due to tunnels
        /// <summary>
        /// Tunnel force (resistance calculations based upon formula presented in papaer titled "Reasonable compensation coefficient of maximum gradient in long railway tunnels"
        /// </summary>
        public virtual void UpdateTunnelForce()
        {
            if (Train.IsPlayerDriven)   // Only calculate tunnel resistance when it is the player train.
            {
                if (TunnelResistanceDependent)
                {
                    if (CarTunnelData.FrontPositionBeyondStartOfTunnel.HasValue)
                    {

                        float? TunnelStart;
                        float? TunnelAhead;
                        float? TunnelBehind;

                        TunnelStart = CarTunnelData.FrontPositionBeyondStartOfTunnel;      // position of front of wagon wrt start of tunnel
                        TunnelAhead = CarTunnelData.LengthMOfTunnelAheadFront;            // Length of tunnel remaining ahead of front of wagon (negative if front of wagon out of tunnel)
                        TunnelBehind = CarTunnelData.LengthMOfTunnelBehindRear;           // Length of tunnel behind rear of wagon (negative if rear of wagon has not yet entered tunnel)

                        // Calculate tunnel default effective cross-section area, and tunnel perimeter - based upon the designed speed limit of the railway (TRK File)

                        float TunnelLengthM = CarTunnelData.LengthMOfTunnelAheadFront.Value + CarTunnelData.LengthMOfTunnelBehindRear.Value;
                        float TrainLengthTunnelM = Train.Length;
                        float TrainMassTunnelKg = Train.MassKg;
                        float PrevTrainCrossSectionAreaM2 = TrainCrossSectionAreaM2;
                        TrainCrossSectionAreaM2 = CarWidthM * CarHeightM;
                        if (TrainCrossSectionAreaM2 < PrevTrainCrossSectionAreaM2)
                        {
                            TrainCrossSectionAreaM2 = PrevTrainCrossSectionAreaM2;  // Assume locomotive cross-sectional area is the largest, if not use new one.
                        }
                        const float DensityAirKgpM3 = 1.2f;

                        // Determine tunnel X-sect area and perimeter based upon number of tracks
                        if (CarTunnelData.numTunnelPaths >= 2)
                        {
                            TunnelCrossSectionAreaM2 = DoubleTunnelCrossSectAreaM2; // Set values for double track tunnels and above
                            TunnelPerimeterM = DoubleTunnelPerimeterM;
                        }
                        else
                        {
                            TunnelCrossSectionAreaM2 = SingleTunnelCrossSectAreaM2; // Set values for single track tunnels
                            TunnelPerimeterM = SingleTunnelPerimeterAreaM;
                        }

                        // 
                        // Calculate first tunnel factor

                        float TunnelAComponent = (0.00003318f * DensityAirKgpM3 * TunnelCrossSectionAreaM2) / ((1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)));
                        float TunnelBComponent = 174.419f * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2));
                        float TunnelCComponent = (2.907f * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2))) / (4.0f * (TunnelCrossSectionAreaM2 / TunnelPerimeterM));

                        float TempTunnel1 = (float)Math.Sqrt(TunnelBComponent + (TunnelCComponent * (TunnelLengthM - TrainLengthTunnelM) / TrainLengthTunnelM));
                        float TempTunnel2 = (1.0f - (1.0f / (1.0f + TempTunnel1))) * (1.0f - (1.0f / (1.0f + TempTunnel1)));

                        float UnitAerodynamicDrag = ((TunnelAComponent * TrainLengthTunnelM) / Kg.ToTonne(TrainMassTunnelKg)) * TempTunnel2;

                        TunnelForceN = UnitAerodynamicDrag * Kg.ToTonne(MassKG) * AbsSpeedMpS * AbsSpeedMpS;
                    }
                    else
                    {
                        TunnelForceN = 0.0f; // Reset tunnel force to zero when train is no longer in the tunnel
                    }
                }
            }
        }

        // Icik
        public void CalculatePositionInTunnel()
        {
            //Simulator.Confirmer.MSG("InTunnel: " + Train.inTunnel);            
            //Simulator.Confirmer.MSG2("Simulator.PlayerCarIsInTunnelEndM: " + Simulator.PlayerCarIsInTunnelEndM);
            //Simulator.Confirmer.MSG3("Simulator.PlayerCarIsInTunnelBeginM: " + Simulator.PlayerCarIsInTunnelBeginM);
            
            if (CarIsPlayerLoco && CarTunnelData.LengthMOfTunnelAheadFront != null && CarTunnelData.LengthMOfTunnelBehindRear != null)
            {                
                Simulator.PlayerCarIsInTunnelBeginM = (float)(CarTunnelData.LengthMOfTunnelAheadFront.Value + CarTunnelData.LengthMOfTunnelBehindRear.Value - CarTunnelData.LengthMOfTunnelAheadFront);
                Simulator.PlayerCarIsInTunnelEndM = (float)(CarTunnelData.LengthMOfTunnelAheadFront.Value + CarTunnelData.LengthMOfTunnelBehindRear.Value - CarTunnelData.LengthMOfTunnelBehindRear);
                Simulator.TunnelLengthM = (float)(CarTunnelData.LengthMOfTunnelAheadFront.Value + CarTunnelData.LengthMOfTunnelBehindRear.Value);
            }
        }


        #endregion

        #region Calculate risk of train derailing

        //================================================================================================//
        /// <summary>
        /// Update Risk of train derailing
        /// <\summary>

        public void UpdateTrainDerailmentRisk()
        {
            // Train will derail if lateral forces on the train exceed the vertical forces holding the train on the railway track. 
            // Typically the train is most at risk when travelling around a curve

            // Based upon ??????

            // Calculate Lateral forces

            foreach (var w in WheelAxles)
            {
                //               Trace.TraceInformation("Car ID {0} Length {1} Bogie {2} Offset {3} MAtrix {4}", CarID, CarLengthM,  w.BogieIndex, w.OffsetM, w.BogieMatrix);

            }

            // Calculate the vertival force on the wheel of the car, to determine whether wagon derails or not
            WagonVerticalDerailForceN = MassKG * GravitationalAccelerationMpS2 * Train.WagonCoefficientFriction;



            // Calculate coupler angle when travelling around curve

            float OverhangCarIM = 2.545f; // Vehicle overhang - B
            float OverhangCarI1M = 2.545f;  // Vehicle overhang - B
            float CouplerDistanceM = 2.4f; // Coupler distance - D
            float BogieDistanceIM = 8.23f; // 0.5 * distance between bogie centres - A
            float BogieDistanceI1M = 8.23f;  // 0.5 * distance between bogie centres - A
            float CouplerAlphaAngleRad;
            float CouplerBetaAngleRad;
            float CouplerGammaAngleRad;


            float BogieCentresAdjVehiclesM = OverhangCarIM + OverhangCarI1M + CouplerDistanceM; // L value

            if (CurrentCurveRadius != 0)
            {
                CouplerAlphaAngleRad = BogieDistanceIM / CurrentCurveRadius;  // 
                CouplerBetaAngleRad = BogieDistanceI1M / CurrentCurveRadius;
                CouplerGammaAngleRad = BogieCentresAdjVehiclesM / (2.0f * CurrentCurveRadius);

                float AngleBetweenCarbodies = CouplerAlphaAngleRad + CouplerBetaAngleRad + 2.0f * CouplerGammaAngleRad;

                WagonFrontCouplerAngleRad = (BogieCentresAdjVehiclesM * (CouplerGammaAngleRad + CouplerAlphaAngleRad) - OverhangCarI1M * AngleBetweenCarbodies) / CouplerDistanceM;

                //      Trace.TraceInformation("Centre {0} Gamma {1} Alpha {2} Between {3} CouplerDist {4}", BogieCentresAdjVehiclesM, CouplerGammaAngleRad, CouplerAlphaAngleRad, AngleBetweenCarbodies, CouplerDistanceM);
            }
            else
            {
                WagonFrontCouplerAngleRad = 0.0f;
            }

            // Lateral Force = Coupler force x Sin (Coupler Angle)

            float CouplerLateralForceN = CouplerForceU * (float)Math.Sin(WagonFrontCouplerAngleRad);


            TotalWagonLateralDerailForceN = CouplerLateralForceN;

            if (TotalWagonLateralDerailForceN > WagonVerticalDerailForceN)
            {
                BuffForceExceeded = true;
            }
            else
            {
                BuffForceExceeded = false;
            }

        }

        #endregion



        #region Calculate permissible speeds around curves
        /// <summary>
        /// Reads current curve radius and computes the maximum recommended speed around the curve based upon the 
        /// superelevation of the track
        /// Based upon information extracted from - Critical Speed Analysis of Railcars and Wheelsets on Curved and Straight Track - https://scarab.bates.edu/cgi/viewcontent.cgi?article=1135&context=honorstheses
        /// </summary>
        public virtual void UpdateCurveSpeedLimit()
        {
            float s = AbsSpeedMpS; // speed of train
            var train = Simulator.PlayerLocomotive.Train;//Debrief Eval

            // get curve radius

            if (CurveSpeedDependent || CurveResistanceDependent)  // Function enabled by menu selection for either curve resistance or curve speed limit
            {


                if (CurrentCurveRadius > 0)  // only check curve speed if it is a curve
                {
                    float SpeedToleranceMpS = Me.FromMi(pS.FrompH(2.5f));  // Set bandwidth tolerance for resetting notifications

                    // If super elevation set in Route (TRK) file
                    if (Simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM != null)
                    {
                        SuperelevationM = Simulator.TRK.Tr_RouteFile.SuperElevationHgtpRadiusM[CurrentCurveRadius];

                    }
                    else
                    {
                        // Set to OR default values
                        if (CurrentCurveRadius > 2000)
                        {
                            if (RouteSpeedMpS > 55.0)   // If route speed limit is greater then 200km/h, assume high speed passenger route
                            {
                                // Calculate superelevation based upon the route speed limit and the curve radius
                                // SE = ((TrackGauge x Velocity^2 ) / Gravity x curve radius)

                                SuperelevationM = (TrackGaugeM * RouteSpeedMpS * RouteSpeedMpS) / (GravitationalAccelerationMpS2 * CurrentCurveRadius);

                            }
                            else
                            {
                                SuperelevationM = 0.0254f;  // Assume minimal superelevation if conventional mixed route
                            }

                        }
                        // Set Superelevation value - based upon standard figures
                        else if (CurrentCurveRadius <= 2000 & CurrentCurveRadius > 1600)
                        {
                            SuperelevationM = 0.0254f;  // Assume 1" (or 0.0254m)
                        }
                        else if (CurrentCurveRadius <= 1600 & CurrentCurveRadius > 1200)
                        {
                            SuperelevationM = 0.038100f;  // Assume 1.5" (or 0.038100m)
                        }
                        else if (CurrentCurveRadius <= 1200 & CurrentCurveRadius > 1000)
                        {
                            SuperelevationM = 0.050800f;  // Assume 2" (or 0.050800m)
                        }
                        else if (CurrentCurveRadius <= 1000 & CurrentCurveRadius > 800)
                        {
                            SuperelevationM = 0.063500f;  // Assume 2.5" (or 0.063500m)
                        }
                        else if (CurrentCurveRadius <= 800 & CurrentCurveRadius > 600)
                        {
                            SuperelevationM = 0.0889f;  // Assume 3.5" (or 0.0889m)
                        }
                        else if (CurrentCurveRadius <= 600 & CurrentCurveRadius > 500)
                        {
                            SuperelevationM = 0.1016f;  // Assume 4" (or 0.1016m)
                        }
                        // for tighter radius curves assume on branch lines and less superelevation
                        else if (CurrentCurveRadius <= 500 & CurrentCurveRadius > 280)
                        {
                            SuperelevationM = 0.0889f;  // Assume 3" (or 0.0762m)
                        }
                        else if (CurrentCurveRadius <= 280 & CurrentCurveRadius > 0)
                        {
                            SuperelevationM = 0.063500f;  // Assume 2.5" (or 0.063500m)
                        }
                    }

#if DEBUG_USER_SUPERELEVATION
                       Trace.TraceInformation(" ============================================= User SuperElevation (TrainCar.cs) ========================================");
                        Trace.TraceInformation("CarID {0} TrackSuperElevation {1} Curve Radius {2}",  CarID, SuperelevationM, CurrentCurveRadius);
#endif

                    // Calulate equal wheel loading speed for current curve and superelevation - this was considered the "safe" speed to travel around a curve . In this instance the load on the both railes is evenly distributed.
                    // max equal load speed = SQRT ( (superelevation x gravity x curve radius) / track gauge)
                    // SuperElevation is made up of two components = rail superelevation + the amount of sideways force that a passenger will be comfortable with. This is expressed as a figure similar to superelevation.

                    SuperelevationM = MathHelper.Clamp(SuperelevationM, 0.0001f, 0.150f); // If superelevation is greater then 6" (150mm) then limit to this value, having a value of zero causes problems with calculations

                    float SuperElevationAngleRad = (float)Math.Sinh(SuperelevationM); // Total superelevation includes both balanced and unbalanced superelevation

                    MaxCurveEqualLoadSpeedMps = (float)Math.Sqrt((SuperelevationM * GravitationalAccelerationMpS2 * CurrentCurveRadius) / TrackGaugeM); // Used for calculating curve resistance

                    // Railway companies often allow the vehicle to exceed the equal loading speed, provided that the passengers didn't feel uncomfortable, and that the car was not likely to excced the maximum critical speed
                    SuperElevationTotalM = SuperelevationM + UnbalancedSuperElevationM;

                    float SuperElevationTotalAngleRad = (float)Math.Sinh(SuperElevationTotalM); // Total superelevation includes both balanced and unbalanced superelevation

                    float MaxSafeCurveSpeedMps = (float)Math.Sqrt((SuperElevationTotalM * GravitationalAccelerationMpS2 * CurrentCurveRadius) / TrackGaugeM);

                    // Calculate critical speed - indicates the speed above which stock will overturn - sum of the moments of centrifrugal force and the vertical weight of the vehicle around the CoG
                    // critical speed = SQRT ( (centrifrugal force x gravity x curve radius) / Vehicle weight)
                    // centrifrugal force = Stock Weight x factor for movement of resultant force due to superelevation.

                    float SinTheta = (float)Math.Sin(SuperElevationAngleRad);
                    float CosTheta = (float)Math.Cos(SuperElevationAngleRad);
                    float HalfTrackGaugeM = TrackGaugeM / 2.0f;

                    float CriticalMaxSpeedMpS = (float)Math.Sqrt((CurrentCurveRadius * GravitationalAccelerationMpS2 * (CentreOfGravityM.Y * SinTheta + HalfTrackGaugeM * CosTheta)) / (CentreOfGravityM.Y * CosTheta - HalfTrackGaugeM * SinTheta));

                    float Sin2Theta = 0.5f * (1 - (float)Math.Cos(2.0 * SuperElevationAngleRad));
                    float CriticalMinSpeedMpS = (float)Math.Sqrt((GravitationalAccelerationMpS2 * CurrentCurveRadius * HalfTrackGaugeM * Sin2Theta) / (CosTheta * (CentreOfGravityM.Y * CosTheta + HalfTrackGaugeM * SinTheta)));

                    if (CurveSpeedDependent)
                    {

                        // This section not required any more???????????
                        // This section tests for the durability value of the consist. Durability value will non-zero if read from consist files. 
                        // Timetable mode does not read consistent durability values for consists, and therefore value will be zero at this time. 
                        // Hence a large value of durability (10.0) is assumed, thus effectively disabling it in TT mode
                        //                        if (Simulator.CurveDurability != 0.0)
                        //                        {
                        //                            MaxDurableSafeCurveSpeedMpS = MaxSafeCurveSpeedMps * Simulator.CurveDurability;  // Finds user setting for durability
                        //                        }
                        //                        else
                        //                        {
                        //                            MaxDurableSafeCurveSpeedMpS = MaxSafeCurveSpeedMps * 10.0f;  // Value of durability has not been set, so set to a large value
                        //                        }

                        // Test current speed to see if greater then equal loading speed around the curve
                        if (s > MaxSafeCurveSpeedMps)
                        {
                            if (!IsMaxSafeCurveSpeed)
                            {
                                IsMaxSafeCurveSpeed = true; // set flag for IsMaxSafeCurveSpeed reached

                                if (Train.IsPlayerDriven && !Simulator.TimetableMode)    // Warning messages will only apply if this is player train and not running in TT mode
                                {
                                    if (Train.IsFreight)
                                    {
                                        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetStringFmt("You are travelling too fast for this curve. Slow down, your freight car {0} may be damaged. The recommended speed for this curve is {1}", CarID, FormatStrings.FormatSpeedDisplay(MaxSafeCurveSpeedMps, IsMetric)));
                                    }
                                    else
                                    {
                                        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetStringFmt("You are travelling too fast for this curve. Slow down, your passengers in car {0} are feeling uncomfortable. The recommended speed for this curve is {1}", CarID, FormatStrings.FormatSpeedDisplay(MaxSafeCurveSpeedMps, IsMetric))); ;
                                    }

                                    if (dbfmaxsafecurvespeedmps != MaxSafeCurveSpeedMps)//Debrief eval
                                    {
                                        dbfmaxsafecurvespeedmps = MaxSafeCurveSpeedMps;
                                        //ldbfevalcurvespeed = true;
                                        DbfEvalTravellingTooFast++;
                                        train.DbfEvalValueChanged = true;//Debrief eval
                                    }
                                }

                            }
                        }
                        else if (s < MaxSafeCurveSpeedMps - SpeedToleranceMpS)  // Reset notification once spped drops
                        {
                            if (IsMaxSafeCurveSpeed)
                            {
                                IsMaxSafeCurveSpeed = false; // reset flag for IsMaxSafeCurveSpeed reached - if speed on curve decreases


                            }
                        }

                        // If speed exceeds the overturning speed, then indicated that an error condition has been reached.
                        if (s > CriticalMaxSpeedMpS && Train.GetType() != typeof(AITrain) && Train.GetType() != typeof(TTTrain)) // Breaking of brake hose will not apply to TT mode or AI trains)
                        {
                            if (!IsCriticalMaxSpeed)
                            {
                                IsCriticalMaxSpeed = true; // set flag for IsCriticalSpeed reached

                                if (Train.IsPlayerDriven && !Simulator.TimetableMode)  // Warning messages will only apply if this is player train and not running in TT mode
                                {
                                    BrakeSystem.FrontBrakeHoseConnected = false; // break the brake hose connection between cars if the speed is too fast
                                    Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("You were travelling too fast for this curve, and have snapped a brake hose on Car " + CarID + ". You will need to repair the hose and restart."));

                                    dbfEvalsnappedbrakehose = true;//Debrief eval

                                    if (!ldbfevaltrainoverturned)
                                    {
                                        ldbfevaltrainoverturned = true;
                                        DbfEvalTrainOverturned++;
                                        train.DbfEvalValueChanged = true;//Debrief eval
                                    }
                                }
                            }

                        }
                        else if (s < CriticalMaxSpeedMpS - SpeedToleranceMpS) // Reset notification once speed drops
                        {
                            if (IsCriticalMaxSpeed)
                            {
                                IsCriticalMaxSpeed = false; // reset flag for IsCriticalSpeed reached - if speed on curve decreases
                                ldbfevaltrainoverturned = false;

                                if (dbfEvalsnappedbrakehose)
                                {
                                    DbfEvalTravellingTooFastSnappedBrakeHose++;//Debrief eval
                                    dbfEvalsnappedbrakehose = false;
                                    train.DbfEvalValueChanged = true;//Debrief eval
                                }

                            }
                        }


                        // This alarm indication comes up even in shunting yard situations where typically no superelevation would be present.
                        // Code is disabled until a bteer way is determined to work out whether track piees are superelevated or not.

                        // if speed doesn't reach minimum speed required around the curve then set notification
                        // Breaking of brake hose will not apply to TT mode or AI trains or if on a curve less then 150m to cover operation in shunting yards, where track would mostly have no superelevation
                        //                        if (s < CriticalMinSpeedMpS && Train.GetType() != typeof(AITrain) && Train.GetType() != typeof(TTTrain) && CurrentCurveRadius > 150 ) 
                        //                       {
                        //                            if (!IsCriticalMinSpeed)
                        //                            {
                        //                                IsCriticalMinSpeed = true; // set flag for IsCriticalSpeed not reached
                        //
                        //                                if (Train.IsPlayerDriven && !Simulator.TimetableMode)  // Warning messages will only apply if this is player train and not running in TT mode
                        //                                {
                        //                                      Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("You were travelling too slow for this curve, and Car " + CarID + "may topple over."));
                        //                                }
                        //                            }
                        //
                        //                        }
                        //                        else if (s > CriticalMinSpeedMpS + SpeedToleranceMpS) // Reset notification once speed increases
                        //                        {
                        //                            if (IsCriticalMinSpeed)
                        //                            {
                        //                                IsCriticalMinSpeed = false; // reset flag for IsCriticalSpeed reached - if speed on curve decreases
                        //                            }
                        //                        }

#if DEBUG_CURVE_SPEED
                   Trace.TraceInformation("================================== TrainCar.cs - DEBUG_CURVE_SPEED ==============================================================");
                   Trace.TraceInformation("CarID {0} Curve Radius {1} Super {2} Unbalanced {3} Durability {4}", CarID, CurrentCurveRadius, SuperelevationM, UnbalancedSuperElevationM, Simulator.CurveDurability);
                   Trace.TraceInformation("CoG {0}", CentreOfGravityM);
                   Trace.TraceInformation("Current Speed {0} Equal Load Speed {1} Max Safe Speed {2} Critical Max Speed {3} Critical Min Speed {4}", MpS.ToMpH(s), MpS.ToMpH(MaxCurveEqualLoadSpeedMps), MpS.ToMpH(MaxSafeCurveSpeedMps), MpS.ToMpH(CriticalMaxSpeedMpS), MpS.ToMpH(CriticalMinSpeedMpS));
                   Trace.TraceInformation("IsMaxSafeSpeed {0} IsCriticalSpeed {1}", IsMaxSafeCurveSpeed, IsCriticalSpeed);
#endif
                    }

                }
                else
                {
                    // reset flags if train is on a straight - in preparation for next curve
                    IsCriticalMaxSpeed = false;   // reset flag for IsCriticalMaxSpeed reached
                    IsCriticalMinSpeed = false;   // reset flag for IsCriticalMinSpeed reached
                    IsMaxSafeCurveSpeed = false; // reset flag for IsMaxEqualLoadSpeed reached
                }
            }
        }

        #endregion


        #region Calculate friction force in curves

        /// <summary>
        /// Reads current curve radius and computes the CurveForceN friction. Can be overriden by calling
        /// base.UpdateCurveForce();
        /// CurveForceN *= someCarSpecificCoef;     
        /// </summary>
        public virtual void UpdateCurveForce(float elapsedClockSeconds)
        {
            if (CurveResistanceDependent)
            {

                if (CurrentCurveRadius > 0)
                {

                    if (RigidWheelBaseM == 0)   // Calculate default values if no value in Wag File
                    {


                        float Axles = WheelAxles.Count;
                        float Bogies = Parts.Count - 1;
                        float BogieSize = Axles / Bogies;

                        RigidWheelBaseM = 1.6764f;       // Set a default in case no option is found - assume a standard 4 wheel (2 axle) bogie - wheel base - 5' 6" (1.6764m)

                        // Calculate the number of axles in a car

                        if (WagonType != WagonTypes.Engine)   // if car is not a locomotive then determine wheelbase
                        {

                            if (Bogies < 2)  // if less then two bogies assume that it is a fixed wheelbase wagon
                            {
                                if (Axles == 2)
                                {
                                    RigidWheelBaseM = 3.5052f;       // Assume a standard 4 wheel (2 axle) wagon - wheel base - 11' 6" (3.5052m)
                                }
                                else if (Axles == 3)
                                {
                                    RigidWheelBaseM = 3.6576f;       // Assume a standard 6 wheel (3 axle) wagon - wheel base - 12' 2" (3.6576m)
                                }
                            }
                            else if (Bogies == 2)
                            {
                                if (Axles == 2)
                                {
                                    if (WagonType == WagonTypes.Passenger)
                                    {

                                        RigidWheelBaseM = 2.4384f;       // Assume a standard 4 wheel passenger bogie (2 axle) wagon - wheel base - 8' (2.4384m)
                                    }
                                    else
                                    {
                                        RigidWheelBaseM = 1.6764f;       // Assume a standard 4 wheel freight bogie (2 axle) wagon - wheel base - 5' 6" (1.6764m)
                                    }
                                }
                                else if (Axles == 3)
                                {
                                    RigidWheelBaseM = 3.6576f;       // Assume a standard 6 wheel bogie (3 axle) wagon - wheel base - 12' 2" (3.6576m)
                                }
                            }

                        }
                        if (WagonType == WagonTypes.Engine)   // if car is a locomotive and either a diesel or electric then determine wheelbase
                        {
                            if (EngineType != EngineTypes.Steam)  // Assume that it is a diesel or electric locomotive
                            {
                                if (Axles == 2)
                                {
                                    RigidWheelBaseM = 1.6764f;       // Set a default in case no option is found - assume a standard 4 wheel (2 axle) bogie - wheel base - 5' 6" (1.6764m)
                                }
                                else if (Axles == 3)
                                {
                                    RigidWheelBaseM = 3.5052f;       // Assume a standard 6 wheel bogie (3 axle) locomotive - wheel base - 11' 6" (3.5052m)
                                }
                            }
                            else // assume steam locomotive
                            {

                                if (LocoNumDrvAxles >= Axles) // Test to see if ENG file value is too big (typically doubled)
                                {
                                    LocoNumDrvAxles = LocoNumDrvAxles / 2;  // Appears this might be the number of wheels rather then the axles.
                                }

                                //    Approximation for calculating rigid wheelbase for steam locomotives
                                // Wheelbase = 1.25 x (Loco Drive Axles - 1.0) x Drive Wheel diameter

                                RigidWheelBaseM = 1.25f * (LocoNumDrvAxles - 1.0f) * (DriverWheelRadiusM * 2.0f);

                            }

                        }


                    }

                    // Curve Resistance = (Vehicle mass x Coeff Friction) * (Track Gauge + Vehicle Fixed Wheelbase) / (2 * curve radius)
                    // Vehicle Fixed Wheel base is the distance between the wheels, ie bogie or fixed wheels

                    CurveForceN = MassKG * Train.WagonCoefficientFriction * (TrackGaugeM + RigidWheelBaseM) / (2.0f * CurrentCurveRadius);
                    float CurveResistanceSpeedFactor = Math.Abs((MaxCurveEqualLoadSpeedMps - AbsSpeedMpS) / MaxCurveEqualLoadSpeedMps) * StartCurveResistanceFactor;
                    CurveForceN *= CurveResistanceSpeedFactor * CurveResistanceZeroSpeedFactor;
                    CurveForceN *= GravitationalAccelerationMpS2; // to convert to Newtons
                }
                else
                {
                    CurveForceN = 0f;
                }
                //CurveForceNFiltered = CurveForceFilter.Filter(CurveForceN, elapsedClockSeconds);
                CurveForceFilter.Update(elapsedClockSeconds, CurveForceN);
                CurveForceNFiltered = CurveForceFilter.SmoothedValue;
            }
        }

        #endregion

        /// <summary>
        /// Signals an event from an external source (player, multi-player controller, etc.) for this car.
        /// </summary>
        /// <param name="evt"></param>
        public virtual void SignalEvent(Event evt) { }
        public virtual void SignalEvent(PowerSupplyEvent evt) { }
        public virtual void SignalEvent(PowerSupplyEvent evt, int id) { }

        public virtual string GetStatus() { return null; }
        public virtual string GetDebugStatus()
        {
            return String.Format("{0}\t{2}\t{1}\t{3}\t{4:F0}%\t{5}\t\t{6}\t{7}\t",
                CarID,
                Flipped ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(Direction)),
                AcceptMUSignals ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                ThrottlePercent,
                String.Format("{0}", FormatStrings.FormatSpeedDisplay(SpeedMpS, IsMetric)),
                // For Locomotive HUD display shows "forward" motive power (& force) as a positive value, braking power (& force) will be shown as negative values.                
                ControlUnit ? Simulator.Catalog.GetString("Control") : FormatStrings.FormatPower((MotiveForceN) * SpeedMpS, IsMetric, false, false),
                ControlUnit ? "" : String.Format("{0}{1}", FormatStrings.FormatForce(MotiveForceN, IsMetric), WheelSlip ? "!!!" : WheelSlipWarning ? "???" : ""));
        }
        public virtual string GetTrainBrakeStatus() { return null; }
        public virtual string GetEngineBrakeStatus() { return null; }
        public virtual string GetBrakemanBrakeStatus() { return null; }
        public virtual string GetDynamicBrakeStatus() { return null; }
        public virtual bool GetSanderOn() { return false; }
        protected bool WheelHasBeenSet = false; //indicating that the car shape has been loaded, thus no need to reset the wheels

        public TrainCar()
        {
        }

        public TrainCar(Simulator simulator, string wagFile)
        {
            Simulator = simulator;
            WagFilePath = wagFile;
            RealWagFilePath = wagFile;
        }

        // Game save
        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(XRotFinal);
            outf.Write(YRotFinal);
            outf.Write(ZRotFinal);
            outf.Write(PushXFinal);
            outf.Write(PushZFinal);
            outf.Write(XRotFinalMarker);
            outf.Write(YRotFinalMarker);
            outf.Write(ZRotFinalMarker);
            outf.Write(PushXFinalMarker);
            outf.Write(PushZFinalMarker);
            outf.Write(Flipped);
            outf.Write(UiD);
            outf.Write(CarID);
            outf.Write(CarIsFlipped);
            
            outf.Write(MotiveForceN);
            outf.Write(FrictionForceN);
            outf.Write(SpeedMpS);
            outf.Write(CouplerSlackM);
            outf.Write(Headlight[1]);
            outf.Write(Headlight[2]);
            outf.Write(OrgConsist);
            outf.Write(PrevTiltingZRot);
            outf.Write(BrakesStuck);
            outf.Write(IsCarSteamHeatInitial);
            outf.Write(SteamHoseLeakRateRandom);
            outf.Write(CarHeatCurrentCompartmentHeatW);
            outf.Write(CarSteamHeatMainPipeSteamPressurePSI);
            outf.Write(CarHeatCompartmentHeaterOn);
            outf.Write(WagonHasTemperature);
            outf.Write(CarOutsideTempC0);
            outf.Write(WagonTemperature);
            outf.Write(SteamLocoCabTemperatureBase); 
            outf.Write(RDSTBreaker[1]);
            outf.Write(RDSTBreaker[2]);
            outf.Write(CabHeating_OffOn[1]);
            outf.Write(CabHeating_OffOn[2]);
            outf.Write(AuxPowerOff);
            outf.Write(UserPowerOff);
            outf.Write(WheelDamageValue);            
            outf.Write(PowerKeyPosition[1]);
            outf.Write(PowerKeyPosition[2]);
            outf.Write(prevPowerKeyPosition[1]);
            outf.Write(prevPowerKeyPosition[2]);
            outf.Write(AcceptCableSignals);
            outf.Write(AcceptHelperSignals);
            outf.Write(AcceptPowerSignals);
            outf.Write(LightFrontLPosition);
            outf.Write(LightFrontRPosition);
            outf.Write(LightRearLPosition);
            outf.Write(LightRearRPosition);
            outf.Write(WheelBearingTemperatureDegC);
            outf.Write(InitialWheelBearingRiseTemperatureDegC);
            outf.Write(InitialWheelBearingDeclineTemperatureDegC);
            outf.Write(CarHasHeatingReady);
            outf.Write(CarOutsideTempCBase);
            outf.Write(SelectedCar);

            BrakeSystem.Save(outf);
        }

        // Game restore
        public virtual void Restore(BinaryReader inf)
        {
            XRotFinal = inf.ReadSingle();
            YRotFinal = inf.ReadSingle();
            ZRotFinal = inf.ReadSingle();
            PushXFinal = inf.ReadSingle();
            PushZFinal = inf.ReadSingle();
            XRotFinalMarker = inf.ReadSingle();
            YRotFinalMarker = inf.ReadSingle();
            ZRotFinalMarker = inf.ReadSingle();
            PushXFinalMarker = inf.ReadSingle();
            PushZFinalMarker = inf.ReadSingle();
            Flipped = inf.ReadBoolean();
            UiD = inf.ReadInt32();
            CarID = inf.ReadString();
            CarIsFlipped = inf.ReadBoolean();

            MotiveForceN = inf.ReadSingle();
            FrictionForceN = inf.ReadSingle();
            SpeedMpS = inf.ReadSingle();
            _PrevSpeedMpS = SpeedMpS;
            CouplerSlackM = inf.ReadSingle();
            Headlight[1] = inf.ReadInt32();
            Headlight[2] = inf.ReadInt32();
            OrgConsist = inf.ReadString();
            PrevTiltingZRot = inf.ReadSingle();
            BrakesStuck = inf.ReadBoolean();
            IsCarSteamHeatInitial = inf.ReadBoolean();
            SteamHoseLeakRateRandom = inf.ReadSingle();
            CarHeatCurrentCompartmentHeatW = inf.ReadSingle();
            CarSteamHeatMainPipeSteamPressurePSI = inf.ReadSingle();
            CarHeatCompartmentHeaterOn = inf.ReadBoolean();
            WagonHasTemperature = inf.ReadBoolean();
            CarOutsideTempC0 = inf.ReadSingle();
            WagonTemperature = inf.ReadSingle();
            SteamLocoCabTemperatureBase = inf.ReadSingle();
            RDSTBreaker[1] = inf.ReadBoolean();
            RDSTBreaker[2] = inf.ReadBoolean();
            CabHeating_OffOn[1] = inf.ReadBoolean();
            CabHeating_OffOn[2] = inf.ReadBoolean();
            AuxPowerOff = inf.ReadBoolean();
            UserPowerOff = inf.ReadBoolean();
            WheelDamageValue = inf.ReadSingle();            
            PowerKeyPosition[1] = inf.ReadInt32();
            PowerKeyPosition[2] = inf.ReadInt32();
            prevPowerKeyPosition[1] = inf.ReadInt32();
            prevPowerKeyPosition[2] = inf.ReadInt32();
            AcceptCableSignals = inf.ReadBoolean();
            AcceptHelperSignals = inf.ReadBoolean();
            AcceptPowerSignals = inf.ReadBoolean();
            LightFrontLPosition = inf.ReadInt32();
            LightFrontRPosition = inf.ReadInt32();
            LightRearLPosition = inf.ReadInt32();
            LightRearRPosition = inf.ReadInt32();
            WheelBearingTemperatureDegC = inf.ReadSingle();
            InitialWheelBearingRiseTemperatureDegC = inf.ReadSingle();
            InitialWheelBearingDeclineTemperatureDegC = inf.ReadSingle();
            CarHasHeatingReady = inf.ReadBoolean();
            CarOutsideTempCBase = inf.ReadSingle();
            SelectedCar = inf.ReadBoolean();

            BrakeSystem.Restore(inf);
        }

        //================================================================================================//
        /// <summary>
        /// Set starting conditions for TrainCars when initial speed > 0 
        /// 

        public virtual void InitializeMoving()
        {
            BrakeSystem.InitializeMoving();
            //TODO: next if/else block has been inserted to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
            // To achieve the same result with other means, without flipping trainset physics, the if/else block should be deleted and replaced by following instruction:
            //            SpeedMpS = Flipped ? -Train.InitialSpeed : Train.InitialSpeed;
            if (IsDriveable && Train.TrainType == Train.TRAINTYPE.PLAYER)
            {
                var loco = this as MSTSLocomotive;
                SpeedMpS = Flipped ^ loco.UsingRearCab ? -Train.InitialSpeed : Train.InitialSpeed;
            }

            else SpeedMpS = Flipped ? -Train.InitialSpeed : Train.InitialSpeed;
            _PrevSpeedMpS = SpeedMpS;
        }

        public bool HasFrontCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Front;
                if (loco == null || loco.CabViewList.Count <= i || loco.CabViewList[i].CabViewType != CabViewType.Front) return false;
                return (loco.CabViewList[i].ViewPointList.Count > 0);
            }
        }

        public bool HasRearCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Rear;
                if (loco == null || loco.CabViewList.Count <= i) return false;
                return (loco.CabViewList[i].ViewPointList.Count > 0);
            }
        }

        public bool HasFront3DCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Front;
                if (loco == null || loco.CabView3D == null || loco.CabView3D.CabViewType != CabViewType.Front) return false;
                return (loco.CabView3D.ViewPointList.Count > i);
            }
        }

        public bool HasRear3DCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Rear;
                if (loco == null || loco.CabView3D == null) return false;
                return (loco.CabView3D.ViewPointList.Count > i);
            }
        }

        public virtual bool GetCabFlipped()
        {
            return false;
        }

        public virtual float GetCouplerZeroLengthM()
        {
            return 0;
        }

        public virtual float GetSimpleCouplerStiffnessNpM()
        {
            return 2e7f;
        }

        public virtual float GetCouplerStiffness1NpM()
        {
            return 1e7f;
        }

        public virtual float GetCouplerStiffness2NpM()
        {
            return 1e7f;
        }

        public virtual float GetCouplerSlackAM()
        {
            return 0;
        }

        public virtual float GetCouplerSlackBM()
        {
            return 0.1f;
        }

        public virtual bool GetCouplerRigidIndication()
        {
            return false;
        }

        public virtual float GetMaximumSimpleCouplerSlack1M()
        {
            return 0.03f;
        }

        public virtual float GetMaximumSimpleCouplerSlack2M()
        {
            return 0.07f;
        }

        public virtual float GetMaximumCouplerForceN()
        {
            return 1e10f;
        }

        // Advanced coupler parameters

        public virtual float GetCouplerTensionStiffness1N()
        {
            return 1e7f;
        }

        public virtual float GetCouplerTensionStiffness2N()
        {
            return 2e7f;
        }

        public virtual float GetCouplerCompressionStiffness1N()
        {
            return 1e7f;
        }

        public virtual float GetCouplerCompressionStiffness2N()
        {
            return 2e7f;
        }

        public virtual float GetCouplerTensionSlackAM()
        {
            return 0;
        }

        public virtual float GetCouplerTensionSlackBM()
        {
            return 0.1f;
        }

        public virtual float GetCouplerCompressionSlackAM()
        {
            return 0;
        }

        public virtual float GetCouplerCompressionSlackBM()
        {
            return 0.1f;
        }

        public virtual float GetMaximumCouplerTensionSlack1M()
        {
            return 0.05f;
        }

        public virtual float GetMaximumCouplerTensionSlack2M()
        {
            return 0.1f;
        }

        public virtual float GetMaximumCouplerTensionSlack3M()
        {
            return 0.13f;
        }

        public virtual float GetMaximumCouplerCompressionSlack1M()
        {
            return 0.05f;
        }

        public virtual float GetMaximumCouplerCompressionSlack2M()
        {
            return 0.1f;
        }

        public virtual float GetMaximumCouplerCompressionSlack3M()
        {
            return 0.13f;
        }

        public virtual float GetCouplerBreak1N() // Sets the break force????
        {
            return 1e10f;
        }

        public virtual float GetCouplerBreak2N() // Sets the break force????
        {
            return 1e10f;
        }

        public virtual float GetCouplerTensionR0Y() // Sets the break force????
        {
            return 0.0001f;
        }

        public virtual float GetCouplerCompressionR0Y() // Sets the break force????
        {
            return 0.0001f;
        }



        public virtual void CopyCoupler(TrainCar other)
        {
            CouplerSlackM = other.CouplerSlackM;
            CouplerSlack2M = other.CouplerSlack2M;
        }

        public virtual bool GetAdvancedCouplerFlag()
        {
            return false;
        }

        public virtual void CopyControllerSettings(TrainCar other)
        {
            // Icik
            //Headlight = other.Headlight;
        }

        public void AddWheelSet(float offset, int bogieID, int parentMatrix, string wheels, int bogie1Axles, int bogie2Axles)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;

            // Currently looking for rolling stock that has more than 3 axles on a bogie.  This is rare, but some models are like this.
            // In this scenario, bogie1 contains 2 sets of axles.  One of them for bogie2.  Both bogie2 axles must be removed.
            // For the time being, the only rail-car that was having issues had 4 axles on one bogie. The second set of axles had a bogie index of 2 and both had to be dropped for the rail-car to operate under OR.
            if (Parts.Count > 0 && bogie1Axles == 4 || bogie2Axles == 4) // 1 bogie will have a Parts.Count of 2.
            {
                if (Parts.Count == 2)
                    if (parentMatrix == Parts[1].iMatrix && wheels.Length == 8)
                        if (bogie1Axles == 4 && bogieID == 2) // This test is strictly testing for and leaving out axles meant for a Bogie2 assignment.
                            return;

                if (Parts.Count == 3)
                {
                    if (parentMatrix == Parts[1].iMatrix && wheels.Length == 8)
                        if (bogie1Axles == 4 && bogieID == 2) // This test is strictly testing for and leaving out axles meant for a Bogie2 assignment.
                            return;
                    if (parentMatrix == Parts[2].iMatrix && wheels.Length == 8)
                        if (bogie2Axles == 4 && bogieID == 1) // This test is strictly testing for and leaving out axles meant for a Bogie1 assignment.
                            return;
                }

            }

            //some old stocks have only two wheels, but defined to have four, two share the same offset, thus all computing of rotations will have problem
            //will check, if so, make the offset different a bit.
            foreach (var axles in WheelAxles)
                if (offset.AlmostEqual(axles.OffsetM, 0.05f)) { offset = axles.OffsetM + 0.7f; break; }

            // Came across a model where the axle offset that is part of a bogie would become 0 during the initial process.  This is something we must test for.
            if (wheels.Length == 8 && Parts.Count > 0)
            {
                if (wheels == "WHEELS11" || wheels == "WHEELS12" || wheels == "WHEELS13" || wheels == "WHEELS14")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS21" || wheels == "WHEELS22" || wheels == "WHEELS23" || wheels == "WHEELS24")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS31" || wheels == "WHEELS32" || wheels == "WHEELS33" || wheels == "WHEELS34")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS41" || wheels == "WHEELS42" || wheels == "WHEELS43" || wheels == "WHEELS44")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
                // This else will cover additional Wheels added following the proper naming convention.
                else
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
            }
            // The else will cover WHEELS spelling where the length is less than 8.
            else
                WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

        } // end AddWheelSet()

        public void AddBogie(float offset, int matrix, int id, string bogie, int numBogie1, int numBogie2)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;
            foreach (var p in Parts) if (p.bogie && offset.AlmostEqual(p.OffsetM, 0.05f)) { offset = p.OffsetM + 0.1f; break; }
            if (bogie == "BOGIE1")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE2")
            {
                // This was the initial problem.  If the shape file contained only one entry that was labeled as BOGIE2(should be BOGIE1)
                // the process would assign 2 to id, causing it to create 2 Parts entries( or 2 bogies) when one was only needed.  It is possible that
                // this issue created many of the problems with articulated wagons later on in the process.
                // 2 would be assigned to id, not because there were 2 entries, but because 2 was in BOGIE2.
                if (numBogie2 == 1 && numBogie1 == 0)
                {
                    id -= 1;
                    while (Parts.Count <= id)
                        Parts.Add(new TrainCarPart(0, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
                else
                {
                    while (Parts.Count <= id)
                        Parts.Add(new TrainCarPart(0, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
            }
            else if (bogie == "BOGIE3")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE4")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            // The else will cover additions not covered above.
            else
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }

        } // end AddBogie()

        public void SetUpWheels()
        {

#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            WheelHasBeenSet = true;
            // No parts means no bogies (always?), so make sure we've got Parts[0] for the car itself.
            if (Parts.Count == 0)
                Parts.Add(new TrainCarPart(0, 0));
            // No axles but we have bogies.
            if (WheelAxles.Count == 0 && Parts.Count > 1)
            {
                // Fake the axles by pretending each has 1 axle.
                foreach (var part in Parts)
                    WheelAxles.Add(new WheelAxle(part.OffsetM, part.iMatrix, 0));
                Trace.TraceInformation("Wheel axle data faked based on {1} bogies for {0}", WagFilePath, Parts.Count - 1);
            }
            bool articFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articRear = !WheelAxles.Any(a => a.OffsetM > 0);
            // Validate the axles' assigned bogies and count up the axles on each bogie.
            if (WheelAxles.Count > 0)
            {
                foreach (var w in WheelAxles)
                {
                    if (w.BogieIndex >= Parts.Count)
                        w.BogieIndex = 0;
                    if (w.BogieMatrix > 0)
                    {
                        for (var i = 0; i < Parts.Count; i++)
                            if (Parts[i].iMatrix == w.BogieMatrix)
                            {
                                w.BogieIndex = i;
                                break;
                            }
                    }
                    w.Part = Parts[w.BogieIndex];
                    w.Part.SumWgt++;
                }

                // Make sure the axles are sorted by OffsetM along the car.
                // Attempting to sort car w/o WheelAxles will resort to an error.
                WheelAxles.Sort(WheelAxles[0]);
            }

            //fix bogies with only one wheel set:
            // This process is to fix the bogies that did not pivot under the cab of steam locomotives as well as other locomotives that have this symptom.
            // The cause involved the bogie and axle being close by 0.05f or less on the ZAxis.
            // The ComputePosition() process was unable to work with this.
            // The fix involves first testing for how close they are then moving the bogie offset up.
            // The final fix involves adding an additional axle.  Without this, both bogie and axle would never track properly?
            // Note: Steam locomotive modelers are aware of this issue and are now making sure there is ample spacing between axle and bogie.
            for (var i = 1; i < Parts.Count; i++)
            {
                if (Parts[i].bogie == true && Parts[i].SumWgt < 1.5)
                {
                    foreach (var w in WheelAxles)
                    {
                        if (w.BogieMatrix == Parts[i].iMatrix)
                        {
                            if (w.OffsetM.AlmostEqual(Parts[i].OffsetM, 0.6f))
                            {
                                var w1 = new WheelAxle(w.OffsetM - 0.5f, w.BogieIndex, i);
                                w1.Part = Parts[w1.BogieIndex]; //create virtual wheel
                                w1.Part.SumWgt++;
                                WheelAxles.Add(w1);
                                w.OffsetM += 0.5f; //move the original bogie forward, so we have two bogies to make the future calculation happy
                                Trace.TraceInformation("A virtual wheel axle was added for bogie {1} of {0}", WagFilePath, i);
                                break;
                            }
                        }
                    }
                }
            }

            // Count up the number of bogies (parts) with at least 2 axles.
            for (var i = 1; i < Parts.Count; i++)
                if (Parts[i].SumWgt > 1.5)
                    Parts[0].SumWgt++;

            // This check is for the single axle/bogie issue.
            // Check SumWgt using Parts[0].SumWgt.
            // Certain locomotives do not test well when using Part.SumWgt versus Parts[0].SumWgt.
            // Make sure test using Parts[0] is performed after the above for loop.
            if (!articFront && !articRear && (Parts[0].SumWgt < 1.5))
            {
                foreach (var w in WheelAxles)
                {
                    if (w.BogieIndex >= Parts.Count - 1)
                    {
                        w.BogieIndex = 0;
                        w.Part = Parts[w.BogieIndex];

                    }
                }
            }
            // Using WheelAxles.Count test to control WheelAxlesLoaded flag.
            if (WheelAxles.Count > 2)
            {
                WheelAxles.Sort(WheelAxles[0]);
                WheelAxlesLoaded = true;
            }


#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            // Decided to control what is sent to SetUpWheelsArticulation()by using
            // WheelAxlesLoaded as a flag.  This way, wagons that have to be processed are included
            // and the rest left out.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            var carIndex = Train.Cars.IndexOf(this);
            //Certain locomotives are testing as articulated wagons for some reason.
            if (WagonType != WagonTypes.Engine)
                if (WheelAxles.Count >= 2)
                    if (articulatedFront || articulatedRear)
                    {
                        WheelAxlesLoaded = true;
                        SetUpWheelsArticulation(carIndex);
                    }
        } // end SetUpWheels()

        protected void SetUpWheelsArticulation(int carIndex)
        {
            // If there are no forward wheels, this car is articulated (joined
            // to the car in front) at the front. Likewise for the rear.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            // Original process originally used caused too many issues.
            // The original process did include the below process of just using WheelAxles.Add
            //  if the initial test did not work.  Since the below process is working without issues the
            //  original process was stripped down to what is below
            if (articulatedFront || articulatedRear)
            {
                if (articulatedFront && WheelAxles.Count <= 3)
                    WheelAxles.Add(new WheelAxle(-CarLengthM / 2, 0, 0) { Part = Parts[0] });

                if (articulatedRear && WheelAxles.Count <= 3)
                    WheelAxles.Add(new WheelAxle(CarLengthM / 2, 0, 0) { Part = Parts[0] });

                WheelAxles.Sort(WheelAxles[0]);
            }


#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
        } // end SetUpWheelsArticulation()

        public void ComputePosition(Traveller traveler, bool backToFront, float elapsedTimeS, float distance, float speed)
        {
            for (var j = 0; j < Parts.Count; j++)
                Parts[j].InitLineFit();
            var tileX = traveler.TileX;
            var tileZ = traveler.TileZ;
            if (Flipped == backToFront)
            {
                var o = -CarLengthM / 2 - CentreOfGravityM.Z;
                for (var k = 0; k < WheelAxles.Count; k++)
                {
                    var d = WheelAxles[k].OffsetM - o;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    var x = traveler.X + 2048 * (traveler.TileX - tileX);
                    var y = traveler.Y;
                    var z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
                }
                o = CarLengthM / 2 - CentreOfGravityM.Z - o;
                traveler.Move(o);
            }
            else
            {
                var o = CarLengthM / 2 - CentreOfGravityM.Z;
                for (var k = WheelAxles.Count - 1; k >= 0; k--)
                {
                    var d = o - WheelAxles[k].OffsetM;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    var x = traveler.X + 2048 * (traveler.TileX - tileX);
                    var y = traveler.Y;
                    var z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
                }
                o = CarLengthM / 2 + CentreOfGravityM.Z + o;
                traveler.Move(o);
            }

            TrainCarPart p0 = Parts[0];
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                p.FindCenterLine();
                if (p.SumWgt > 1.5)
                    p0.AddPartLocation(1, p);
            }
            p0.FindCenterLine();
            Vector3 fwd = new Vector3(p0.B[0], p0.B[1], -p0.B[2]);
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (fwd.X != 0 && fwd.Y != 0 && fwd.Z != 0)
                fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.Up, fwd);
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (side.X != 0 && side.Y != 0 && side.Z != 0)
                side.Normalize();
            Vector3 up = Vector3.Cross(fwd, side);
            Matrix m = Matrix.Identity;
            m.M11 = side.X;
            m.M12 = side.Y;
            m.M13 = side.Z;
            m.M21 = up.X;
            m.M22 = up.Y;
            m.M23 = up.Z;
            m.M31 = fwd.X;
            m.M32 = fwd.Y;
            m.M33 = fwd.Z;
            m.M41 = p0.A[0];
            m.M42 = p0.A[1] + 0.275f;
            m.M43 = -p0.A[2];
            WorldPosition.XNAMatrix = m;
            WorldPosition.TileX = tileX;
            WorldPosition.TileZ = tileZ;

            UpdatedTraveler(traveler, elapsedTimeS, distance, speed);

            // calculate truck angles
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                if (p.SumWgt < .5)
                    continue;
                if (p.SumWgt < 1.5)
                {   // single axle pony trunk
                    double d = p.OffsetM - p.SumOffset / p.SumWgt;
                    if (-.2 < d && d < .2)
                        continue;
                    p.AddWheelSetLocation(1, p.OffsetM, p0.A[0] + p.OffsetM * p0.B[0], p0.A[1] + p.OffsetM * p0.B[1], p0.A[2] + p.OffsetM * p0.B[2], 0, null);
                    p.FindCenterLine();
                }
                Vector3 fwd1 = new Vector3(p.B[0], p.B[1], -p.B[2]);
                if (fwd1.X == 0 && fwd1.Y == 0 && fwd1.Z == 0)
                {
                    p.Cos = 1;
                }
                else
                {
                    fwd1.Normalize();
                    p.Cos = Vector3.Dot(fwd, fwd1);
                }

                float SinSpeedCoef = 1.0f + (AbsSpeedMpS / 10f);

                // Icik
                if (p.Cos >= .9999f)
                {
                    if (p.Sin > 0.0001)
                    {
                        p.Sin -= SinSpeedCoef * elapsedTimeS * 0.01f;
                    }
                    else
                    if (p.Sin < -0.0001)
                    {
                        p.Sin += SinSpeedCoef * elapsedTimeS * 0.01f;
                    }
                    else
                        p.Sin = 0;
                }
                else
                {
                    float SinFinal = (float)Math.Sqrt(1 - p.Cos * p.Cos);

                    if (fwd.X * fwd1.Z < fwd.Z * fwd1.X)
                        SinFinal = -SinFinal;

                    if (p.Sin < 0.9999f * SinFinal)
                    {
                        p.Sin += SinSpeedCoef * elapsedTimeS * 0.01f;
                    }
                    else
                    if (p.Sin > 1.0001f * SinFinal)
                    {
                        p.Sin -= SinSpeedCoef * elapsedTimeS * 0.01f;
                    }                                        
                    else
                        p.Sin = SinFinal;                    
                }
            }
        }

        #region Traveller-based updates
        public float CurrentCurveRadius;
        public float CurrentCurveAngle;

        internal void UpdatedTraveler(Traveller traveler, float elapsedTimeS, float distanceM, float speedMpS)
        {
            // We need to avoid introducing any unbounded effects, so cap the elapsed time to 0.25 seconds (4FPS).
            if (elapsedTimeS > 0.25f)
                return;

            CurrentCurveRadius = traveler.GetCurveRadius();
            CurrentCurveAngle = traveler.GetCurveAngle();
            UpdateVibrationAndTilting(traveler, elapsedTimeS, distanceM, speedMpS);
            UpdateSuperElevation(traveler, elapsedTimeS);
        }
        #endregion

        #region Super-elevation
        void UpdateSuperElevation(Traveller traveler, float elapsedTimeS)
        {
            if (Simulator.Settings.UseSuperElevation == 0)
                return;
            if (prevElev < -30f) { prevElev += 40f; return; }//avoid the first two updates as they are not valid

            // Because the traveler is at the FRONT of the TrainCar, smooth the super-elevation out with the rear.
            var z = traveler.GetSuperElevation(-CarLengthM);
            if (Flipped)
                z *= -1;
            // TODO This is a hack until we fix the super-elevation code as described in http://www.elvastower.com/forums/index.php?/topic/28751-jerky-superelevation-effect/
            if (prevElev < -10f || prevElev > 10f) prevElev = z;//initial, will jump to the desired value
            else
            {
                z = prevElev + (z - prevElev) * Math.Min(elapsedTimeS, 1);//smooth rotation
                prevElev = z;
            }

            WorldPosition.XNAMatrix = Matrix.CreateRotationZ(z) * WorldPosition.XNAMatrix;
        }
        #endregion

        #region Vibration and tilting
        public Matrix VibrationInverseMatrix = Matrix.Identity;

        // https://en.wikipedia.org/wiki/Newton%27s_laws_of_motion#Newton.27s_2nd_Law
        //   Let F be the force in N
        //   Let m be the mass in kg
        //   Let a be the acceleration in m/s/s
        //   Then F = m * a
        // https://en.wikipedia.org/wiki/Hooke%27s_law
        //   Let F be the force in N
        //   Let k be the spring constant in N/m or kg/s/s
        //   Let x be the displacement in m
        //   Then F = k * x
        // If we assume that gravity is 9.8m/s/s, then the force needed to support the train car is:
        //   F = m * 9.8
        // If we assume that the train car suspension allows for 0.2m (20cm) of travel, then substitute Hooke's law:
        //   m * 9.8 = k * 0.2
        //   k = m * 9.8 / 0.2
        // Finally, we assume a mass (m) of 1kg to calculate a mass-independent value:
        //   k' = 9.8 / 0.2
        //float VibrationSpringConstantPrimepSpS = 9.8f / 0.2f; // 1/s/s

        // 
        //float VibratioDampingCoefficient = 0.02f;

        // This is multiplied by the CarVibratingLevel (which goes up to 3).
        const float VibrationIntroductionStrength = 0.03f;

        // The tightest curve we care about has a radius of 50m. This is used as the basis for the most violent vibrations.
        const float VibrationMaximumCurvaturepM = 1f / 50;

        const float VibrationFactorDistance = 1;
        const float VibrationFactorTrackVectorSection = 1;
        const float VibrationFactorTrackNode = 1;

        Vector3 VibrationOffsetM;
        Vector3 VibrationRotationRad;
        Vector3 VibrationRotationVelocityRadpS;
        Vector2 VibrationTranslationM;
        Vector2 VibrationTranslationVelocityMpS;

        int VibrationTrackNode;
        int VibrationTrackVectorSection;
        float VibrationTrackCurvaturepM;

        float PrevTiltingZRot; // previous tilting angle
        float TiltingZRot; // actual tilting angle

        internal void UpdateVibrationAndTilting(Traveller traveler, float elapsedTimeS, float distanceM, float speedMpS)
        {
            // NOTE: Traveller is at the FRONT of the TrainCar!

            // Don't add vibrations to train cars less than 2.5 meter in length; they're unsuitable for these calculations.
            if (CarLengthM < 2.5f || (this as MSTSWagon).WagonIsServis) return;

            // Vyloučí z vibrací AI vlaky obsahující servis1 nebo servis4
            if (!IsPlayerTrain)
            {
                foreach (TrainCar car in Train.Cars)
                {
                    if ((car as MSTSWagon).WagonIsServis14 || car.AbsSpeedMpS < 0.01f) return;
                }
            }

            if (Simulator.Settings.CarsVibration)            
                Simulator.Settings.CarVibratingLevel = 2;            
            else
                Simulator.Settings.CarVibratingLevel = 0;

            if (Simulator.Settings.CarVibratingLevel != 0)
            {
                //var elapsedTimeS = Math.Abs(speedMpS) > 0.001f ? distanceM / speedMpS : 0;
                if (VibrationOffsetM.X == 0)
                {
                    // Initialize three different offsets (0 - 1 meters) so that the different components of the vibration motion don't align.
                    VibrationOffsetM.X = (float)Simulator.Random.NextDouble();
                    VibrationOffsetM.Y = (float)Simulator.Random.NextDouble();
                    VibrationOffsetM.Z = (float)Simulator.Random.NextDouble();
                }

                if (VibrationTrackVectorSection == 0)
                    VibrationTrackVectorSection = traveler.TrackVectorSectionIndex;
                if (VibrationTrackNode == 0)
                    VibrationTrackNode = traveler.TrackNodeIndex;

                // Apply suspension/spring and damping.
                // https://en.wikipedia.org/wiki/Simple_harmonic_motion
                //   Let F be the force in N
                //   Let k be the spring constant in N/m or kg/s/s
                //   Let x be the displacement in m
                //   Then F = -k * x
                // Given F = m * a, solve for a:
                //   a = F / m
                // Substitute F:
                //   a = -k * x / m
                // Because our spring constant was never multiplied by m, we can cancel that out:
                //   a = -k' * x
                var rotationAccelerationRadpSpS = -VibrationSpringConstantPrimepSpS * VibrationRotationRad;
                var translationAccelerationMpSpS = -VibrationSpringConstantPrimepSpS * VibrationTranslationM;
                // https://en.wikipedia.org/wiki/Damping
                //   Let F be the force in N
                //   Let c be the damping coefficient in N*s/m
                //   Let v be the velocity in m/s
                //   Then F = -c * v
                // We apply the acceleration (let t be time in s, then dv/dt = a * t) and damping (-c * v) to the velocities:
                VibrationRotationVelocityRadpS += rotationAccelerationRadpSpS * elapsedTimeS - VibratioDampingCoefficient * VibrationRotationVelocityRadpS;
                VibrationTranslationVelocityMpS += translationAccelerationMpSpS * elapsedTimeS - VibratioDampingCoefficient * VibrationTranslationVelocityMpS;
                // Now apply the velocities (dx/dt = v * t):
                VibrationRotationRad += VibrationRotationVelocityRadpS * elapsedTimeS;
                VibrationTranslationM += VibrationTranslationVelocityMpS * elapsedTimeS;

                // Add new vibrations every CarLengthM in either direction.
                if (Math.Round((VibrationOffsetM.X + DistanceM) / CarLengthM) != Math.Round((VibrationOffsetM.X + DistanceM + distanceM) / CarLengthM))
                {
                    AddVibrations(VibrationFactorDistance, elapsedTimeS);
                    VibrationType_1 = true;
                }

                // Add new vibrations every track vector section which changes the curve radius.
                if (VibrationTrackVectorSection != traveler.TrackVectorSectionIndex)
                {
                    var curvaturepM = MathHelper.Clamp(traveler.GetCurvature(), -VibrationMaximumCurvaturepM, VibrationMaximumCurvaturepM);
                    if (VibrationTrackCurvaturepM != curvaturepM)
                    {
                        // Use the difference in curvature to determine the strength of the vibration caused.
                        AddVibrations(VibrationFactorTrackVectorSection * Math.Abs(VibrationTrackCurvaturepM - curvaturepM) / VibrationMaximumCurvaturepM, elapsedTimeS);
                        VibrationTrackCurvaturepM = curvaturepM;                        
                        VibrationType_2 = true;
                    }
                    VibrationTrackVectorSection = traveler.TrackVectorSectionIndex;
                }

                // Add new vibrations every track node.
                if (IsOverJunction())
                {
                    AddVibrations(VibrationFactorTrackNode, elapsedTimeS);
                    VibrationTrackNode = traveler.TrackNodeIndex;
                    VibrationType_1 = false;
                    VibrationType_3 = true;
                }

                AddVibrations(VibrationFactorDistance, elapsedTimeS);

                if (Factor_vibration > 0) Factor_vibration--;
            }

            // Naklápění skříně vozu
            if (Train != null && AbsSpeedMpS > 0.1f && !DerailIsOn)
            {
                float MaxSpeedTilting = 60.0f / 3.6f;
                float TiltingMark = AbsSpeedMpS / SpeedMpS;

                // Omezí maximální náklon vozu dle náprav
                switch (WagonNumAxles)
                {
                    case 0:
                    case 1:
                    case 2:
                        MaxSpeedTilting = 60.0f / 3.6f;
                        break;
                    case 4:
                        MaxSpeedTilting = 100.0f / 3.6f;
                        break;
                    case 6:
                        MaxSpeedTilting = 80.0f / 3.6f;
                        break;
                }

                TiltingZRot = traveler.FindTiltedZ(TiltingMark * (MathHelper.Clamp(AbsSpeedMpS, 0, MaxSpeedTilting)));//rotation if tilted, an indication of centrifugal force                                
                TiltingZRot = PrevTiltingZRot + (TiltingZRot - PrevTiltingZRot) * (0.5f + (AbsSpeedMpS * 3.6f / 40.0f / 4.0f)) * elapsedTimeS;//smooth rotation
                PrevTiltingZRot = TiltingZRot;
                //if (this.Flipped) TiltingZRot *= -1f;                
                if (this.Flipped) TiltingMark *= -1f;
                if (TiltingMark < 0) TiltingZRot *= -1f;
            }

            //if (Simulator.Settings.CarVibratingLevel != 0)
            //{
            TrackFactorXYZ(elapsedTimeS);
            Derailment(elapsedTimeS, speedMpS);            

            var rotation = Matrix.CreateFromYawPitchRoll(VibrationRotationRad.Y + TiltingYRot, VibrationRotationRad.X + TiltingXRot, VibrationRotationRad.Z + TiltingZRot * 0.5f);
            if (Train.IsTilting && AbsSpeedMpS > 50 / 3.6f) rotation = Matrix.CreateFromYawPitchRoll(VibrationRotationRad.Y, VibrationRotationRad.X, VibrationRotationRad.Z + TiltingZRot * 0.8f);
            var translation = Matrix.CreateTranslation(VibrationTranslationM.X, VibrationTranslationM.Y, 0);
            WorldPosition.XNAMatrix = rotation * translation * WorldPosition.XNAMatrix;
            VibrationInverseMatrix = Matrix.Invert(rotation * translation);

            if (AbsSpeedMpS < 0.1f)
            {
                if (TiltingZRot > 0)
                    TiltingZRot -= 0.001f * elapsedTimeS;
                if (TiltingZRot < 0)
                    TiltingZRot += 0.001f * elapsedTimeS;
                PrevTiltingZRot = TiltingZRot;
            }                
            //}
        }

        // Vykolejení vlaku        
        public float DerailRotateCoef;
        public float DerailRotateCoefDelta;
        public float TiltingXRot;
        public float TiltingYRot;
        float XRot;
        float YRot;
        float ZRot;
        float XRotFinal;
        float YRotFinal;        
        float ZRotFinal;
        float XRotFinalMarker;
        float YRotFinalMarker;
        float ZRotFinalMarker;
        float HasZRotFinalMarker;
        float PushX;
        float PushY;
        float PushZ;
        float PushXFinal;
        float PushZFinal;
        float PushXFinalMarker;
        float PushZFinalMarker;
        bool ResetAllDerailmentCoef;
        float DerailmentTimer;
        float DerailmentTimer2;
        float DerailmentTimer3;
        float DerailmentTimer4;
        public bool DerailIsOn;        
        bool SetDerailCoef;
        bool IsJunctionCase;
        bool IsCurveCase;
        bool CarIsDecoupled;        
        float prevSpeedMpS;
        float prevDereailAbsSpeedMpS = -1;
        Matrix XNAMatrixDerailed;
        public void Derailment(float elapsedTimeS, float speedMpS)
        {
            if (MPManager.IsMultiPlayer() && ((MPManager.Client != null && MPManager.GetUserName() != MPManager.Client.UserName) || MPManager.IsServer())) return;

            //DerailRotateCoef = 5f;
            ResetAllDerailmentCoef = false;

            if (ResetAllDerailmentCoef)
            {
                DerailRotateCoef = 0;
                DerailRotateCoefDelta = 0;
                TiltingXRot = 0f;
                TiltingYRot = 0f;
                TiltingZRot = 0f;
            }                        

            if (Train.TrainDerailmentTimer > 0)
            {
                Train.TrainDerailmentTimer += elapsedTimeS;
                if (Train.TrainDerailmentTimer > 10.0f) Simulator.CarDerailed = true;
            }

            if (!Train.TrainEndOfRoute)
            {
                XNAMatrixDerailed = WorldPosition.XNAMatrix;
            }

            if (Train.TrainEndOfRoute)
            {
                if (!Train.TrainImpactSoundEvent)
                {
                    Train.TrainImpactSoundEvent = true;
                    SignalEvent(Event.CoupleImpact);
                    MPManager.Notify((new MSGEvent(MPManager.GetUserName(), "TRAINCOUPLE", 2)).ToString());
                }

                if (PushZFinalMarker == 0)
                {
                    PushZFinalMarker = prevSpeedMpS != 0 ? prevSpeedMpS / Math.Abs(prevSpeedMpS) : 1;
                    if ((this is MSTSLocomotive) && (this as MSTSLocomotive).UsingRearCab)
                        PushZFinalMarker = -PushZFinalMarker;
                }

                if (Math.Abs(prevSpeedMpS) > 10.0f / 3.6f)
                {
                    prevDereailAbsSpeedMpS = Math.Abs(prevSpeedMpS);
                    if (Train.IsActualPlayerTrain && Train.TrainDerailmentTimer == 0) Train.TrainDerailmentTimer = 0.01f;
                }
                else
                if (prevDereailAbsSpeedMpS == -1)
                {
                    VibrationRotationRad.X -= Math.Abs(prevSpeedMpS * 3.6f) / 10f * elapsedTimeS;
                    SpeedMpS = -0.01f * PushZFinalMarker;                                        
                    Train.TrainEndOfRoute = false;                    
                }
            }

            if (Train.TrainIsDerailed || prevDereailAbsSpeedMpS > 0 || Train.TrainEndOfRoute)
            {
                DerailIsOn = true;
                // Vypnutí HV na elektrické lokomotivě
                if (this as MSTSElectricLocomotive != null && this is MSTSElectricLocomotive)
                    (this as MSTSElectricLocomotive).HVOff = true;

                if (this as MSTSLocomotive != null && this is MSTSLocomotive)
                    (this as MSTSLocomotive).ThrottleToZero();
                                                
                // Vyšinutí vozu na konci trati
                if (prevDereailAbsSpeedMpS > 0)
                {
                    prevDereailAbsSpeedMpS = Math.Abs(SpeedMpS);
                    PushZ = prevDereailAbsSpeedMpS * elapsedTimeS;
                    PushZFinal = PushZ;
                    VibrationRotationRad.X -= 0.1f * elapsedTimeS;
                    VibrationRotationRad.Y -= 0.1f * elapsedTimeS;
                    VibrationRotationRad.Z -= 0.1f * elapsedTimeS;

                    (this as MSTSWagon).DavisAN = MassKG / 24000f * 120000f;
                    (this as MSTSWagon).StandstillFrictionN = MassKG / 24000f * 120000f;

                    SignalEvent(Event.Derail1);
                    SignalEvent(Event.Derail2);
                    SignalEvent(Event.Derail3);
                }                

                if (PushXFinal > 3.0) Train.TrainOutOfRoute = true;
                var DerailRotationX = Matrix.CreateRotationX(XRotFinal * XRotFinalMarker);
                var DerailRotationY = Matrix.CreateRotationY(YRotFinal * YRotFinalMarker);
                var DerailRotationZ = Matrix.CreateRotationZ(ZRotFinal * ZRotFinalMarker);
                var DerailTranslationX = Matrix.CreateTranslation(-PushXFinal * PushXFinalMarker, 0, 0);
                var DerailTranslationZ = Matrix.CreateTranslation(0, 0, -PushZFinal * PushZFinalMarker);
                if (prevDereailAbsSpeedMpS != 0)
                    XNAMatrixDerailed = DerailRotationX * DerailRotationY * DerailRotationZ * DerailTranslationX * DerailTranslationZ * XNAMatrixDerailed;
                WorldPosition.XNAMatrix = XNAMatrixDerailed;
            }
            else
            {
                if (IsOverJunction())
                {
                    if (AbsSpeedMpS > ActualTrackSpeedMpS + (20f / 3.6f) && CurrentCurveRadius < 200f * (AbsSpeedMpS / ActualTrackSpeedMpS) && !IsJunctionCase)
                    {
                        if (Flipped)
                        {
                            if (CurrentCurveAngle > 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * 0.05f);
                            else
                            if (CurrentCurveAngle < 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (-0.05f));
                            else
                            {
                                int ChanceToDerail = Simulator.Random.Next(0, 20);
                                if (ChanceToDerail == 5)
                                    DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (0.01f));
                                else
                                if (ChanceToDerail == 10)
                                    DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (-0.01f));
                            }
                        }
                        else
                        {
                            if (CurrentCurveAngle < 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * 0.05f);
                            else
                            if (CurrentCurveAngle > 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (-0.05f));
                            else
                            {
                                int ChanceToDerail = Simulator.Random.Next(0, 20);
                                if (ChanceToDerail == 5)
                                    DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (0.01f));
                                else
                                if (ChanceToDerail == 10)
                                    DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (-0.01f));
                            }
                        }
                        DerailRotateCoef = MathHelper.Clamp(DerailRotateCoef, -5f, 5f);
                        if (DerailRotateCoef != 0)
                            IsJunctionCase = true;
                    }
                    //Simulator.Confirmer.Information("Radius " + CurrentCurveRadius);
                }
                else
                {
                    if (AbsSpeedMpS > ActualTrackSpeedMpS + (50f / 3.6f) && CurrentCurveRadius < 200f * (AbsSpeedMpS / ActualTrackSpeedMpS) && !IsCurveCase)
                    {
                        if (Flipped)
                        {
                            if (CurrentCurveAngle < 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * 1.0f);
                            else
                            if (CurrentCurveAngle > 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (-1.0f));
                        }
                        else
                        {
                            if (CurrentCurveAngle > 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * 1.0f);
                            else
                            if (CurrentCurveAngle < 0)
                                DerailRotateCoef = (int)((AbsSpeedMpS - ActualTrackSpeedMpS) * 3.6f * (-1.0f));
                        }
                        DerailRotateCoef = MathHelper.Clamp(DerailRotateCoef, -20f, 20f);
                        if (DerailRotateCoef != 0)
                            IsCurveCase = true;
                    }
                    //Simulator.Confirmer.Information("Radius " + CurrentCurveRadius);
                }

                if (DerailRotateCoef != 0)
                    DerailmentTimer += elapsedTimeS;
                else
                {
                    DerailmentTimer = 0.0f;
                    DerailIsOn = false;
                }

                if (DerailmentTimer > 0.0f)
                    DerailIsOn = true;


                if (DerailIsOn)
                {
                    if (Train.IsActualPlayerTrain && Train.TrainDerailmentTimer == 0) Train.TrainDerailmentTimer = 0.01f;
                    VibrationSpringConstantPrimepSpS = 14f / 0.2f;
                    VibratioDampingCoefficient = 0.04f;
                    if (IsJunctionCase)
                    {
                        if (DerailRotateCoef > DerailRotateCoefDelta)
                        {
                            TiltingXRot -= 0.1f * elapsedTimeS;
                            TiltingYRot += 0.5f * elapsedTimeS;
                            ZRot += 1.0f * elapsedTimeS;
                            VibrationRotationRad.X -= 0.1f * elapsedTimeS;
                            VibrationRotationRad.Y += 0.1f * elapsedTimeS;
                            VibrationRotationRad.Z += 0.1f * elapsedTimeS;
                            DerailRotateCoefDelta++;
                        }
                        else
                        if (DerailRotateCoef < DerailRotateCoefDelta)
                        {
                            TiltingXRot -= 0.1f * elapsedTimeS;
                            TiltingYRot -= 0.5f * elapsedTimeS;
                            ZRot -= 1.0f * elapsedTimeS;
                            VibrationRotationRad.X -= 0.1f * elapsedTimeS;
                            VibrationRotationRad.Y -= 0.1f * elapsedTimeS;
                            VibrationRotationRad.Z -= 0.1f * elapsedTimeS;
                            DerailRotateCoefDelta--;
                        }
                    }
                    if (IsCurveCase)
                    {
                        if (DerailRotateCoef > DerailRotateCoefDelta)
                        {
                            TiltingXRot -= 0.001f * elapsedTimeS;
                            TiltingYRot += 0.01f * elapsedTimeS;
                            ZRot += 4.0f * elapsedTimeS;
                            VibrationRotationRad.X -= 0.1f * elapsedTimeS;
                            VibrationRotationRad.Y += 0.1f * elapsedTimeS;
                            VibrationRotationRad.Z += 0.1f * elapsedTimeS;
                            DerailRotateCoefDelta++;
                        }
                        else
                            if (DerailRotateCoef < DerailRotateCoefDelta)
                        {
                            TiltingXRot -= 0.001f * elapsedTimeS;
                            TiltingYRot -= 0.01f * elapsedTimeS;
                            ZRot -= 4.0f * elapsedTimeS;
                            VibrationRotationRad.X -= 0.1f * elapsedTimeS;
                            VibrationRotationRad.Y -= 0.1f * elapsedTimeS;
                            VibrationRotationRad.Z -= 0.1f * elapsedTimeS;
                            DerailRotateCoefDelta--;
                        }
                    }

                    ZRotFinalMarker = ZRot != 0 ? Math.Abs(ZRot) / ZRot : 1;

                    if (HasZRotFinalMarker == 0 || HasZRotFinalMarker == ZRotFinalMarker)
                    {
                        ZRotFinal = Math.Max(ZRotFinal, Math.Abs(ZRot));
                        PushXFinal = Math.Max(PushXFinal, Math.Abs(PushX));
                        PushXFinalMarker = DerailRotateCoef != 0 ? Math.Abs(DerailRotateCoef) / DerailRotateCoef : 1;
                        XRotFinal = Math.Max(XRotFinal, Math.Abs(XRot));
                        YRotFinal = Math.Max(YRotFinal, Math.Abs(YRot));
                    }

                    if (ZRotFinal > 0.5f && AbsSpeedMpS > 1.0f)
                    {
                        HasZRotFinalMarker = ZRotFinalMarker;
                        PushX += AbsSpeedMpS * 0.1f * elapsedTimeS;
                        if (CarIsDecoupled) XRot += AbsSpeedMpS * 0.001f * elapsedTimeS; // Náchylné na mizení vozů!
                        YRot += AbsSpeedMpS * 0.001f * elapsedTimeS;
                        PushY += 0.0f * elapsedTimeS;
                        if (XRotFinalMarker == 0) XRotFinalMarker = Simulator.Random.Next(-1, 2);
                        if (YRotFinalMarker == 0) YRotFinalMarker = Simulator.Random.Next(-1, 2);
                    }                    

                    if (PushXFinal > 3.0) Train.TrainOutOfRoute = true;
                    var DerailRotationX = Matrix.CreateRotationX(XRotFinal * XRotFinalMarker);
                    var DerailRotationY = Matrix.CreateRotationY(YRotFinal * YRotFinalMarker);
                    var DerailRotationZ = Matrix.CreateRotationZ(ZRotFinal * ZRotFinalMarker);
                    var DerailTranslationX = Matrix.CreateTranslation(-PushXFinal * PushXFinalMarker, 0, 0);
                    var DerailTranslationZ = Matrix.CreateTranslation(0, 0, -PushZFinal * PushZFinalMarker);
                    WorldPosition.XNAMatrix = DerailRotationX * DerailRotationY * DerailRotationZ * DerailTranslationX * DerailTranslationZ * WorldPosition.XNAMatrix;                    

                    // Zpomalení díky vykolejení
                    if (IsJunctionCase)
                    {
                        (this as MSTSWagon).DavisAN = MassKG / 24000f * 60000f;
                        (this as MSTSWagon).StandstillFrictionN = MassKG / 24000f * 60000f;

                        if (Math.Abs(DerailRotateCoef) > 4 || ZRotFinal > 0.5f || PushXFinal > 0.5f)
                        {
                            DerailmentTimer3 += elapsedTimeS;
                            if (DerailmentTimer3 > 1.0f)
                            {
                                DerailmentTimer3 = 0;
                                CarIsDecoupled = true;
                                Train.TrainIsDerailing = true;                                
                                if (Train.Cars.Count > 1)
                                {
                                    var uncoupleBehindCar = Train.Cars[0];
                                    Simulator.UncoupleBehind(uncoupleBehindCar, false);
                                }
                            }
                        }                        
                        SignalEvent(Event.Derail1);
                    }
                    if (IsCurveCase)
                    {
                        // Vypnutí HV na elektrické lokomotivě
                        if (this as MSTSElectricLocomotive != null && this is MSTSElectricLocomotive)
                            (this as MSTSElectricLocomotive).HVOff = true;

                        if (this as MSTSLocomotive != null && this is MSTSLocomotive)
                            (this as MSTSLocomotive).ThrottleToZero();

                        if (Math.Abs(DerailRotateCoef) > 19 || ZRotFinal > 0.5f || PushXFinal > 0.5f)
                        {
                            DerailmentTimer4 += elapsedTimeS;
                            if (DerailmentTimer4 > 1.0f)
                            {
                                DerailmentTimer4 = 0;
                                CarIsDecoupled = true;
                                Train.TrainIsDerailing = true;
                                if (Train.Cars.Count > 1)
                                {                                    
                                    var uncoupleBehindCar = Train.Cars[0];
                                    Simulator.UncoupleBehind(uncoupleBehindCar, false);                                    
                                }
                            }
                        }                                                                     

                        (this as MSTSWagon).DavisAN = MassKG / 24000f * 120000f;
                        (this as MSTSWagon).StandstillFrictionN = MassKG / 24000f * 120000f;

                        SignalEvent(Event.Derail1);
                        SignalEvent(Event.Derail2);
                        SignalEvent(Event.Derail3);
                    }

                    if (AbsSpeedMpS < 0.1f)
                    {
                        SpeedMpS = 0;
                        Train.TrainIsDerailed = true;                        
                    }

                    // Vibrace po pražcích                
                    if (IsJunctionCase)
                    {
                        if (AbsSpeedMpS > 1f)
                        {
                            VibrationSpringConstantPrimepSpS = 100f / 0.2f;
                            VibratioDampingCoefficient = 0.5f;
                            DerailmentTimer2 += elapsedTimeS;
                            if (DerailmentTimer2 > 10f / (AbsSpeedMpS * 3.6f))
                            {
                                VibrationRotationRad.X -= 0.2f * elapsedTimeS;
                                DerailmentTimer2 = 0;
                            }
                            if (DerailmentTimer2 < 10f / (AbsSpeedMpS * 3.6f))
                            {
                                VibrationRotationRad.X += 0.2f * elapsedTimeS;
                            }
                        }
                        else
                        {
                            VibrationSpringConstantPrimepSpS = 100f / 0.2f;
                            VibratioDampingCoefficient = 0.50f;
                        }
                    }
                }                               
            }            
            prevSpeedMpS = SpeedMpS;
        }

        // Úprava síly vibrací dle rychlostníků na trati
        float TrackFactorX = 1;
        float TrackFactorY = 1;
        float TrackFactorZ = 1;
        float AdhCycle = 0;
        bool FirstFrame = true;
        public float ActualTrackSpeedMpS;        
        private void TrackFactorXYZ(float elapsedTimeS)
        {
            float AdhTime = 1;
            AdhCycle += elapsedTimeS;

            ActualTrackSpeedMpS = Train.AllowedMaxSpeedMpS;
            ActualTrackSpeedMpS = MathHelper.Clamp(ActualTrackSpeedMpS, 20 / 3.6f, Train.AllowedMaxSpeedMpS);

            if (ActualTrackSpeedMpS >= 120 / 3.6f) // Koridor
            {
                TrackFactorX = 0.3f;
                TrackFactorY = 0.3f;
                TrackFactorZ = 0.3f;
                TrackFactorValue = 0.30f;
                if ((AdhCycle > AdhTime && AbsSpeedMpS > 0.1f) || FirstFrame)
                {
                    TrackFactor = Simulator.Random.Next(95, 101) / 100f;
                    AdhCycle = 0;
                }                
            }
            else
            if (ActualTrackSpeedMpS >= 100 / 3.6f) // Běžná trať do 120km/h
            {
                TrackFactorX = 0.6f;
                TrackFactorY = 0.6f;
                TrackFactorZ = 0.6f;
                TrackFactorValue = 0.60f;
                if ((AdhCycle > AdhTime && AbsSpeedMpS > 0.1f) || FirstFrame)
                {
                    TrackFactor = Simulator.Random.Next(89, 95) / 100f;
                    AdhCycle = 0;
                }                
            }
            else
            if (ActualTrackSpeedMpS > 50 / 3.6f) // Běžná trať do 100km/h
            {
                TrackFactorX = 0.8f;
                TrackFactorY = 0.8f;
                TrackFactorZ = 0.8f;
                TrackFactorValue = 0.80f;
                if ((AdhCycle > AdhTime && AbsSpeedMpS > 0.1f) || FirstFrame)
                {
                    TrackFactor = Simulator.Random.Next(83, 89) / 100f;
                    AdhCycle = 0;
                }                
            }
            else
            if (ActualTrackSpeedMpS <= 50 / 3.6f && Train.NextRouteSpeedLimit <= 50 / 3.6f) // Běžná trať do 50km/h
            {
                TrackFactorX = 1.0f;
                TrackFactorY = 1.0f;
                TrackFactorZ = 1.0f;
                TrackFactorValue = 1.00f;
                if ((AdhCycle > AdhTime && AbsSpeedMpS > 0.1f) || FirstFrame)
                {
                    TrackFactor = Simulator.Random.Next(77, 83) / 100f;
                    AdhCycle = 0;
                }                
            }
            else
            {
                TrackFactorX = 0.8f;
                TrackFactorY = 0.8f;
                TrackFactorZ = 0.8f;
                TrackFactorValue = 0.80f;
                if ((AdhCycle > AdhTime && AbsSpeedMpS > 0.1f) || FirstFrame)
                {
                    TrackFactor = Simulator.Random.Next(83, 89) / 100f;
                    AdhCycle = 0;
                }                
            }
            float SpeedFactor;
            if (AbsSpeedMpS < ActualTrackSpeedMpS)
                SpeedFactor = MathHelper.Clamp(AbsSpeedMpS / (ActualTrackSpeedMpS / 2.0f), 0.5f, 1.0f);            
            else
                SpeedFactor = MathHelper.Clamp(AbsSpeedMpS / ActualTrackSpeedMpS, 1.0f, 2.5f);

            if (AbsSpeedMpS < 3f)
                SpeedFactor = 0.3f;

            if (AbsSpeedMpS < 1f)
                SpeedFactor = 0;

            TrackFactorX *= SpeedFactor;
            TrackFactorY *= SpeedFactor * 1.5f;
            TrackFactorZ *= SpeedFactor * 0.5f;
            FirstFrame = false;
        }

        float CyklusCouplerImpact;
        float Vibration3Timer;
        private void AddVibrations(float factor, float elapsedTimeS)
        {
            // NOTE: For low angles (as our vibration rotations are), sin(angle) ~= angle, and since the displacement at the end of the car is sin(angle) = displacement/half-length, sin(displacement/half-length) * half-length ~= displacement.
            if (MPManager.IsMultiPlayer() && ((MPManager.Client != null && MPManager.GetUserName() != MPManager.Client.UserName) || MPManager.IsServer())) return;
            if (DerailIsOn) return;
            
            if (CarLengthM >= 30.0f || Simulator.Paused || Simulator.GameSpeed != 1)
            {
                VibrationSpringConstantPrimepSpS = 9.8f / 0.2f;
                VibratioDampingCoefficient = 0.02f;
            }

            if (CarLengthM < 30.0f && !Simulator.Paused && Simulator.GameSpeed == 1)
            {
                int force;                                
                float VibrationMassKG = ((3 + (MassKG / 10000)) / (MassKG / 10000));
                if (VibrationMassKG > 1.5) VibrationMassKG = 1.5f;

                float x;
                if (CarLengthM > 15) x = (CarLengthM - ((CarLengthM - 15) * 0.75f));
                else x = CarLengthM;

                // Vibrace při prokluzu kol
                if (IsPlayerTrain && WheelSlip)
                {
                    VibrationSpringConstantPrimepSpS = 14f / 0.2f;
                    VibratioDampingCoefficient = 0.04f;
                    VibrationRotationVelocityRadpS.X += (factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * 0.50f * VibrationMassKG) / x;
                }

                // Vibrace při nárazu      
                if (IsPlayerTrain && Simulator.CarCoupleSpeedOvercome && !Simulator.CarCoupleMaxSpeedOvercome && !Simulator.CarByUserUncoupled)
                {
                    CyklusCouplerImpact++;
                    if (CyklusCouplerImpact < 3)
                    {
                        VibrationSpringConstantPrimepSpS = 50 / 0.2f;
                        VibratioDampingCoefficient = 0.3f;
                        VibrationRotationVelocityRadpS.X += (VibrationIntroductionStrength * Math.Abs(Simulator.DifferenceSpeedMpS) * 10f * VibrationMassKG) / x;
                    }
                }
                if (IsPlayerTrain && AbsSpeedMpS == 0)
                    CyklusCouplerImpact = 0;

                // Vibrace při zastavení                
                if (IsPlayerTrain && Math.Abs(AccelerationMpSS) > 0.5f && Math.Abs(SpeedMpS) < 0.1f && !WheelSlip)
                {
                    VibrationSpringConstantPrimepSpS = 50 / 0.2f;
                    VibratioDampingCoefficient = 0.3f;
                    if (this is MSTSLocomotive)
                    {
                        if (Flipped)
                        {
                            if ((this as MSTSLocomotive).UsingRearCab)
                                VibrationRotationVelocityRadpS.X -= VibrationIntroductionStrength * AccelerationMpSS * 0.25f;
                            else
                                VibrationRotationVelocityRadpS.X += VibrationIntroductionStrength * AccelerationMpSS * 0.25f;
                        }
                        else
                        {
                            if ((this as MSTSLocomotive).UsingRearCab)
                                VibrationRotationVelocityRadpS.X += VibrationIntroductionStrength * AccelerationMpSS * 0.25f;
                            else
                                VibrationRotationVelocityRadpS.X -= VibrationIntroductionStrength * AccelerationMpSS * 0.25f;
                        }
                    }
                }

                //Vibrace náhodné nerovnosti
                if (VibrationType_1)   //Vibrace na spojích, dle vzdálenosti
                {
                    int y = 10, y1 = 100;
                    switch (WagonNumAxles)
                    {
                        case 0:
                        case 1:
                        case 2:
                            y = 10; y1 = 40;
                            VibratioDampingCoefficient = 0.035f;
                            break;
                        case 4:
                            y = 10; y1 = 75;
                            VibratioDampingCoefficient = 0.035f;
                            break;
                        case 6:
                            y = 10; y1 = 75;
                            VibratioDampingCoefficient = 0.035f;
                            break;
                    }

                    force = Simulator.Random.Next(y, y1);
                    if (force > 25 && force < 31) force -= 25; // Nabývá od 1 do 5
                    else force = 0;

                    if (AbsSpeedMpS < 20 / 3.6f && force > 2) force = 2;

                    if (force != 0)
                    {
                        VibrationSpringConstantPrimepSpS = (12 + (force * 2)) / 0.2f;
                        for (int i = 0; i < TrackFactorX * force * 10 + 5; i++) Factor_vibration = i;
                    }
                    else
                        VibrationSpringConstantPrimepSpS = (12 + (1 * 2)) / 0.2f;

                    //Simulator.Confirmer.Information("Factor_vibration: " + Factor_vibration);
                   
                    VibrationRotationVelocityRadpS.X += (TrackFactorX * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.8f * VibrationMassKG) / x;

                    if (force == 0) force = 1;                   
                    float SpeedFactor = MathHelper.Clamp(1.0f + (AbsSpeedMpS * 3.6f / 40.0f / 5.0f), 1.0f, 1.3f);
                    if (force < 3)
                    {
                        switch (WagonNumAxles)
                        {
                            case 0:
                            case 1:
                            case 2:
                                VibrationRotationVelocityRadpS.Y += SpeedFactor * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.075f;
                                VibrationRotationVelocityRadpS.Z += SpeedFactor * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.100f;
                                break;
                            case 4:                                                            
                            case 6:
                                VibrationRotationVelocityRadpS.Y += SpeedFactor * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.025f;
                                VibrationRotationVelocityRadpS.Z += SpeedFactor * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.075f;
                                break;
                        }
                    }                                                                                  
                }

                if (VibrationType_2)   //Vibrace v oblouku
                {                    
                    float forceF = MathHelper.Clamp(Math.Abs(CurrentCurveAngle) / MathHelper.Clamp(Math.Abs(CurrentCurveRadius) / 200f, 1, 10), 0, 3);                                                            
                    force = (int)forceF;

                    VibratioDampingCoefficient = 0.05f;
                    if (CurrentCurveAngle > 0)
                    {
                        VibrationRotationVelocityRadpS.Y -= (TrackFactorY * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.75f * VibrationMassKG) / x;
                        VibrationRotationVelocityRadpS.Z -= (TrackFactorY * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.75f * VibrationMassKG) / x;
                    }
                    else
                    {
                        VibrationRotationVelocityRadpS.Y += (TrackFactorY * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.75f * VibrationMassKG) / x;
                        VibrationRotationVelocityRadpS.Z += (TrackFactorY * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.75f * VibrationMassKG) / x;
                    }                    
                    //Simulator.Confirmer.Information("VibrationRotationVelocityRadpS.Y " + VibrationRotationVelocityRadpS.Y);
                }

                float ActivateVibrationTime3 = AbsSpeedMpS == 0 ? 0 : ((1.0f / (AbsSpeedMpS * 3.6f)) * 30f);
                //Simulator.Confirmer.Information("ActivateVibrationTime3 " + ActivateVibrationTime3);
                
                if (IsOverJunction())
                {
                    Vibration3Timer += elapsedTimeS;
                    if (Vibration3Timer > ActivateVibrationTime3 + 0.5f)
                        Vibration3Timer = 0;
                }
                else
                    Vibration3Timer = 0;

                if (VibrationType_3 && Vibration3Timer > ActivateVibrationTime3 && Vibration3Timer < ActivateVibrationTime3 + 0.05f)   //Vibrace na výhybce
                {
                    int y = 25, y1 = 31;
                    switch (WagonNumAxles)
                    {
                        case 0:
                        case 1:
                        case 2:
                            y = 28; y1 = 31;
                            VibratioDampingCoefficient = 0.10f;
                            break;
                        case 4:
                            y = 28; y1 = 31;
                            VibratioDampingCoefficient = 0.15f;
                            break;
                        case 6:
                            y = 28; y1 = 31;
                            VibratioDampingCoefficient = 0.15f;
                            break;
                    }

                    force = Simulator.Random.Next(y, y1);
                    if (force > 25 && force < 31) force -= 25; // Nabývá od 1 do 5
                    else force = 1;

                    if (force != 0)
                    {
                        VibrationSpringConstantPrimepSpS = (52 + (force * 2)) / 0.2f;
                        for (int i = 0; i < TrackFactorX * force * 10 + 5; i++) Factor_vibration = i;
                    }
                    else
                        VibrationSpringConstantPrimepSpS = (12 + (1 * 2)) / 0.2f;                    

                    VibrationRotationVelocityRadpS.X -= (TrackFactorX * factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * force * 0.8f * VibrationMassKG) / x;                    

                    //Simulator.Confirmer.Information("force " + force);
                }

                VibrationType_1 = false;
                VibrationType_2 = false;
                VibrationType_3 = false;                                 
            }
        }
        #endregion

        // TODO These three fields should be in the TrainCarViewer.
        public int TrackSoundType = 0;
        public WorldLocation TrackSoundLocation = WorldLocation.None;
        public float TrackSoundDistSquared = 0;


        /// <summary>
        /// Checks if traincar is over trough. Used to check if refill possible
        /// </summary>
        /// <returns> returns true if car is over trough</returns>

        public bool IsOverTrough()
        {
            var isOverTrough = false;
            // start at front of train
            int thisSectionIndex = Train.PresentPosition[0].TCSectionIndex;
            if (thisSectionIndex < 0) return isOverTrough;
            float thisSectionOffset = Train.PresentPosition[0].TCOffset;
            int thisSectionDirection = Train.PresentPosition[0].TCDirection;


            float usedCarLength = CarLengthM;
            float processedCarLength = 0;
            bool validSections = true;

            while (validSections)
            {
                TrackCircuitSection thisSection = Train.signalRef.TrackCircuitList[thisSectionIndex];
                isOverTrough = false;

                // car spans sections
                if ((CarLengthM - processedCarLength) > thisSectionOffset)
                {
                    usedCarLength = thisSectionOffset - processedCarLength;
                }

                // section has troughs
                if (thisSection.TroughInfo != null)
                {
                    foreach (TrackCircuitSection.troughInfoData[] thisTrough in thisSection.TroughInfo)
                    {
                        float troughStartOffset = thisTrough[thisSectionDirection].TroughStart;
                        float troughEndOffset = thisTrough[thisSectionDirection].TroughEnd;

                        if (troughStartOffset > 0 && troughStartOffset > thisSectionOffset)      // start of trough is in section beyond present position - cannot be over this trough nor any following
                        {
                            return isOverTrough;
                        }

                        if (troughEndOffset > 0 && troughEndOffset < (thisSectionOffset - usedCarLength)) // beyond end of trough, test next
                        {
                            continue;
                        }

                        if (troughStartOffset <= 0 || troughStartOffset < (thisSectionOffset - usedCarLength)) // start of trough is behind
                        {
                            isOverTrough = true;
                            return isOverTrough;
                        }
                    }
                }
                // tested this section, any need to go beyond?

                processedCarLength += usedCarLength;
                {
                    // go back one section
                    int thisSectionRouteIndex = Train.ValidRoute[0].GetRouteIndexBackward(thisSectionIndex, Train.PresentPosition[0].RouteListIndex);
                    if (thisSectionRouteIndex >= 0)
                    {
                        thisSectionIndex = thisSectionRouteIndex;
                        thisSection = Train.signalRef.TrackCircuitList[thisSectionIndex];
                        thisSectionOffset = thisSection.Length;  // always at end of next section
                        thisSectionDirection = Train.ValidRoute[0][thisSectionRouteIndex].Direction;
                    }
                    else // ran out of train
                    {
                        validSections = false;
                    }
                }
            }
            return isOverTrough;
        }

        /// <summary>
        /// Checks if traincar is over junction or crossover. Used to check if water scoop breaks
        /// </summary>
        /// <returns> returns true if car is over junction</returns>

        public bool IsOverJunction()
        {            
            // To Do - This identifies the start of the train, but needs to be further refined to work for each carriage.
            var isOverJunction = false;
            // start at front of train
            int thisSectionIndex = Train.PresentPosition[0].TCSectionIndex;
            float thisSectionOffset = Train.PresentPosition[0].TCOffset;
            int thisSectionDirection = Train.PresentPosition[0].TCDirection;

            float usedCarLength = CarLengthM;
            
            if (Train.PresentPosition[0].TCSectionIndex != Train.PresentPosition[1].TCSectionIndex)
            {
                try
                {
                    var copyOccupiedTrack = Train.OccupiedTrack.ToArray();
                    foreach (var thisSection in copyOccupiedTrack)
                    {

                        //                    Trace.TraceInformation(" Track Section - Index {0} Ciruit Type {1}", thisSectionIndex, thisSection.CircuitType);

                        if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction || thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
                        {
                            if (Simulator.TDB.TrackDB.TrackNodes[thisSection.OriginalIndex].UiD == null)
                                return false;
                            // train is on a switch; let's see if car is on a switch too                            
                            WorldLocation switchLocation = TileLocation(Simulator.TDB.TrackDB.TrackNodes[thisSection.OriginalIndex].UiD);
                            var distanceFromSwitch = WorldLocation.GetDistanceSquared(WorldPosition.WorldLocation, switchLocation);
                            if (distanceFromSwitch < CarLengthM * CarLengthM + Math.Min(SpeedMpS * 3, 150))
                            {
                                isOverJunction = true;
                                return isOverJunction;
                            }
                        }
                    }
                }
                catch
                {

                }
            }

            return isOverJunction;
        }


        public static WorldLocation TileLocation(UiD uid)
        {
            return new WorldLocation(uid.TileX, uid.TileZ, uid.X, uid.Y, uid.Z);
        }

        public virtual void SwitchToPlayerControl()
        {
            return;
        }

        public virtual void SwitchToAutopilotControl()
        {
            return;
        }

        public virtual float GetFilledFraction(uint pickupType)
        {
            return 0f;
        }

        public virtual float GetUserBrakeShoeFrictionFactor()
        {
            return 0f;
        }

        public virtual float GetZeroUserBrakeShoeFrictionFactor()
        {
            return 0f;
        }

        public virtual float GetUserRMgShoeFrictionFactor()
        {
            return 0f;
        }

        public virtual float GetZeroUserRMgShoeFrictionFactor()
        {
            return 0f;
        }
    }

    public class WheelAxle : IComparer<WheelAxle>
    {
        public float OffsetM;   // distance from center of model, positive forward
        public int BogieIndex;
        public int BogieMatrix;
        public TrainCarPart Part;
        public WheelAxle(float offset, int bogie, int parentMatrix)
        {
            OffsetM = offset;
            BogieIndex = bogie;
            BogieMatrix = parentMatrix;
        }
        public int Compare(WheelAxle a, WheelAxle b)
        {
            if (a.OffsetM > b.OffsetM) return 1;
            if (a.OffsetM < b.OffsetM) return -1;
            return 0;
        }
    }

    // data and methods used to align trucks and models to track
    public class TrainCarPart
    {
        public float OffsetM;   // distance from center of model, positive forward
        public int iMatrix;     // matrix in shape that needs to be moved
        public float Cos = 1;       // truck angle cosine
        public float Sin = 0;       // truck angle sin
        // line fitting variables
        public double SumWgt;
        public double SumOffset;
        public double SumOffsetSq;
        public double[] SumX = new double[4];
        public double[] SumXOffset = new double[4];
        public float[] A = new float[4];
        public float[] B = new float[4];
        public bool bogie;
        public TrainCarPart(float offset, int i)
        {
            OffsetM = offset;
            iMatrix = i;
        }
        public void InitLineFit()
        {
            SumWgt = SumOffset = SumOffsetSq = 0;
            for (int i = 0; i < 4; i++)
                SumX[i] = SumXOffset[i] = 0;
        }
        public void AddWheelSetLocation(float w, float o, float x, float y, float z, float t, Traveller traveler)
        {
            SumWgt += w;
            SumOffset += w * o;
            SumOffsetSq += w * o * o;
            SumX[0] += w * x;
            SumXOffset[0] += w * x * o;
            SumX[1] += w * y;
            SumXOffset[1] += w * y * o;
            SumX[2] += w * z;
            SumXOffset[2] += w * z * o;
            SumX[3] += w * t;
            SumXOffset[3] += w * t * o;
        }
        public void AddPartLocation(float w, TrainCarPart part)
        {
            SumWgt += w;
            SumOffset += w * part.OffsetM;
            SumOffsetSq += w * part.OffsetM * part.OffsetM;
            for (int i = 0; i < 4; i++)
            {
                float x = part.A[i] + part.OffsetM * part.B[i];
                SumX[i] += w * x;
                SumXOffset[i] += w * x * part.OffsetM;
            }
        }
        public void FindCenterLine()
        {
            double d = SumWgt * SumOffsetSq - SumOffset * SumOffset;
            if (d > 1e-20)
            {
                for (int i = 0; i < 4; i++)
                {
                    A[i] = (float)((SumOffsetSq * SumX[i] - SumOffset * SumXOffset[i]) / d);
                    B[i] = (float)((SumWgt * SumXOffset[i] - SumOffset * SumX[i]) / d);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    A[i] = (float)(SumX[i] / SumWgt);
                    B[i] = 0;
                }
            }
        }
    }

    public class Passenger
    {
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public float Weight { get; set; }
        public float Age { get; set; }
        public enum Genders { Male, Female };
        public Genders Gender;
        public List<string> MaleNames { get; set; }
        public List<string> FemaleNames { get; set; }
        public List<string> MaleSurnames { get; set; }
        public List<string> FemaleSurames { get; set; }
        public bool Boarded { get; set; }
        public int DepartureStation { get; set; }
        public int ArrivalStation { get; set; }
        public string DepartureStationName { get; set; }
        public string ArrivalStationName { get; set; }
        public int DoorsToEnterAndExit { get; set; }
        public float TimeToEnterAndExit { get; set; }
        public double TimeToStartExiting { get; set; }
        public double TimeToStartBoarding { get; set; }
        public int WagonIndex { get; set; }
        public int StationOrderIndex { get; set; }

        public string WagonName { get; set; }

        public Passenger()
        {

        }
        public Passenger(List<string> MaleNames_, List<string> MaleSurnames_, List<string> FemaleNames_, List<string> FemaleSurnames_, Random random)
        {
            Random rnd = random;
            MaleNames = MaleNames_;
            MaleSurnames = MaleSurnames_;
            FemaleNames = FemaleNames_;
            FemaleSurames = FemaleSurnames_;
            if (MaleNames == null)
            {
                Names names = new Names();
                names.InitializeNames();
                MaleNames = names.FirstNameMale;
                FemaleNames = names.FirstNameFemale;
                MaleSurnames = names.SurnameMale;
                FemaleSurames = names.SurnameFemale;
            }
            if (MaleNames.Count == 0)
            {
                Names names = new Names();
                names.InitializeNames();
                MaleNames = names.FirstNameMale;
                FemaleNames = names.FirstNameFemale;
                MaleSurnames = names.SurnameMale;
                FemaleSurames = names.SurnameFemale;
            }
        New:
            int test = rnd.Next(0, 2);
            if (test > 1)
                goto New;
            if (test == 0)
                Gender = Genders.Male;
            else if (test == 1)
                Gender = Genders.Female;

            DoorsToEnterAndExit = rnd.Next(0, 2);
            float timeToExit = rnd.Next(0, 250);
            TimeToEnterAndExit = timeToExit / 25;

            int nameIndex = 0;
            if (Gender == Genders.Male)
            {
                nameIndex = rnd.Next(0, MaleNames.Count - 1);
                FirstName = MaleNames[nameIndex];
                nameIndex = rnd.Next(0, MaleSurnames.Count - 1);
                Surname = MaleSurnames[nameIndex];
            }
            if (Gender == Genders.Female)
            {
                nameIndex = rnd.Next(0, FemaleNames.Count - 1);
                FirstName = FemaleNames[nameIndex];
                nameIndex = rnd.Next(0, FemaleSurames.Count - 1);
                Surname = FemaleSurames[nameIndex];
            }
            Age = rnd.Next(15, 90);
            if (Gender == Genders.Male)
            {
                if (Age < 20)
                {
                    int aduldWeight = rnd.Next(60, 100);
                    int fat = rnd.Next(0, 19);
                    if (fat == 0)
                        aduldWeight = rnd.Next(100, 200);
                    Weight = (aduldWeight / (21 - Age)) + 5;
                    if (Weight < (Age * 2))
                        Weight = Age * 3;

                }
                else
                {
                    int aduldWeight = rnd.Next(60, 100);
                    int fat = rnd.Next(0, 19);
                    if (fat == 0)
                        aduldWeight = rnd.Next(100, 200);
                    Weight = aduldWeight;
                }
            }
            if (Gender == Genders.Female)
            {
                if (Age < 20)
                {
                    int aduldWeight = rnd.Next(40, 90);
                    int fat = rnd.Next(0, 17);
                    if (fat == 0)
                        aduldWeight = rnd.Next(100, 200);
                    Weight = (aduldWeight / (21 - Age)) + 4;
                    if (Weight < (Age * 2))
                        Weight = Age * 3;
                }
                else
                {
                    int aduldWeight = rnd.Next(40, 90);
                    int fat = rnd.Next(0, 17);
                    if (fat == 0)
                        aduldWeight = rnd.Next(100, 200);
                    Weight = aduldWeight;
                }
            }
            if (Weight < 0)
                Weight = Age * 3;
            Weight = (float)Math.Round(Weight, 0);
        }
    }

    public class Names
    {
        private List<NameData> FirstNameDataMale { get; set; }
        private List<NameData> FirstNameDataFeale { get; set; }
        private List<NameData> SurnameDataMale { get; set; }
        private List<NameData> SurnameDataFeMale { get; set; }
        public List<string> FirstNameMale { get; private set; }
        public List<string> FirstNameFemale { get; private set; }
        public List<string> SurnameMale { get; private set; }
        public List<string> SurnameFemale { get; private set; }
        public Names()
        {

        }

        public List<string> GetName(bool Male)
        {
            List<string> ret = new List<string>();
            return ret;
        }
        public void InitializeNames()
        {
            FirstNameDataMale = new List<NameData>();
            string names_male = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "krestni_muzi.csv");
            string[] names_m = names_male.Split('|');
            foreach (string name in names_m)
            {
                string[] pair = name.Split(',');
                FirstNameDataMale.Add(new NameData(int.Parse(pair[0]), pair[1]));
            }
            foreach (NameData nd in FirstNameDataMale)
            {
                for (int i = 0; i < nd.Count; i++)
                {
                    if (FirstNameMale == null)
                        FirstNameMale = new List<string>();
                    FirstNameMale.Add(nd.Name);
                }
            }
            SurnameDataMale = new List<NameData>();
            string surnames_male = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "prijmeni_muzi_1.csv");
            string[] surnames_m = surnames_male.Split('|');
            foreach (string name in surnames_m)
            {
                string[] pair = name.Split(',');
                SurnameDataMale.Add(new NameData(int.Parse(pair[0]), pair[1]));
            }
            foreach (NameData nd in SurnameDataMale)
            {
                for (int i = 0; i < nd.Count; i++)
                {
                    if (SurnameMale == null)
                        SurnameMale = new List<string>();
                    SurnameMale.Add(nd.Name);
                }
            }
            FirstNameDataFeale = new List<NameData>();
            string names_female = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "krestni_zeny.csv");
            string[] names_f = names_female.Split('|');
            foreach (string name in names_f)
            {
                string[] pair = name.Split(',');
                FirstNameDataFeale.Add(new NameData(int.Parse(pair[0]), pair[1]));
            }
            foreach (NameData nd in FirstNameDataFeale)
            {
                for (int i = 0; i < nd.Count; i++)
                {
                    if (FirstNameFemale == null)
                        FirstNameFemale = new List<string>();
                    FirstNameFemale.Add(nd.Name);
                }
            }
            SurnameDataFeMale = new List<NameData>();
            string surnames_female = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "prijmeni_zeny_1.csv");
            string[] surnames_f = surnames_female.Split('|');
            foreach (string name in surnames_f)
            {
                string[] pair = name.Split(',');
                SurnameDataFeMale.Add(new NameData(int.Parse(pair[0]), pair[1]));
            }
            foreach (NameData nd in SurnameDataFeMale)
            {
                for (int i = 0; i < nd.Count; i++)
                {
                    if (SurnameFemale == null)
                        SurnameFemale = new List<string>();
                    SurnameFemale.Add(nd.Name);
                }
            }

        }
    }
    public class NameData
    {
        public int Count { get; set; }
        public string Name { get; set; }
        public NameData(int count, string name)
        {
            Count = count;
            Name = name;
        }
    }
}
