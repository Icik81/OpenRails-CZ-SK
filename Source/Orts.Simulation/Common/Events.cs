// COPYRIGHT 2013 by the Open Rails project.
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


namespace Orts.Common
{
    public interface EventHandler
    {
        void HandleEvent(Event evt);
        void HandleEvent(Event evt, object viewer);
    }

    public enum Event
    {
        None,
        BatteryOff,
        BatteryOn,
        BellOff,
        BellOn,
        BlowerChange,
        BrakesStuck,
        CabLightSwitchToggle,
        CabRadioOn,
        CabRadioOff,
        CircuitBreakerOpen,
        CircuitBreakerClosing,
        CircuitBreakerClosed,
        CircuitBreakerClosingOrderOff,
        CircuitBreakerClosingOrderOn,
        CircuitBreakerOpeningOrderOff,
        CircuitBreakerOpeningOrderOn,
        CircuitBreakerClosingAuthorizationOff,
        CircuitBreakerClosingAuthorizationOn,
        CompressorOff,
        CompressorOn,
        ControlError,
        Couple,
        CoupleB, // NOTE: Currently not used in Open Rails.
        CoupleC, // NOTE: Currently not used in Open Rails.
        CrossingClosing,
        CrossingOpening,
        CylinderCocksToggle,
        CylinderCompoundToggle,
        DamperChange,
        Derail1, // NOTE: Currently not used in Open Rails.
        Derail2, // NOTE: Currently not used in Open Rails.
        Derail3, // NOTE: Currently not used in Open Rails.
        DoorClose,
        DoorOpen,
        DynamicBrakeChange,
        DynamicBrakeIncrease, // NOTE: Currently not used in Open Rails.
        DynamicBrakeOff,
        EngineBrakeChange,
        EngineBrakePressureDecrease,
        EngineBrakePressureIncrease,
        EnginePowerOff,
        EnginePowerOn,
        FireboxDoorChange,
        FireboxDoorOpen,
        FireboxDoorClose,
        FuelTowerDown,
        FuelTowerTransferEnd,
        FuelTowerTransferStart,
        FuelTowerUp,
        GearDown,
        GearUp,
        GenericEvent1,
        GenericEvent2,
        GenericEvent3,
        GenericEvent4,
        GenericEvent5,
        GenericEvent6,
        GenericEvent7,
        GenericEvent8,
        HornOff,
        HornOn,
        LightSwitchToggle,
        MirrorClose,
        MirrorOpen,
        Pantograph1Down,
        PantographToggle,
        // Don't modify order of next 7 events
        Pantograph1Up,
        Pantograph2Down,
        Pantograph2Up,
        Pantograph3Down,
        Pantograph3Up,
        Pantograph4Down,
        Pantograph4Up,
        //
        PermissionDenied,
        PermissionGranted,
        PermissionToDepart,
        PowerKeyOff,
        PowerKeyOn,
        ReverserChange,
        ReverserToForwardBackward,
        ReverserToNeutral,
        SanderOff,
        SanderOn,
        SemaphoreArm,
        LargeEjectorChange,
        SmallEjectorChange,
        WaterInjector1Off,
        WaterInjector1On,
        WaterInjector2Off,
        WaterInjector2On,
        BlowdownValveToggle,
        SteamHeatChange,
        SteamPulse1,
        SteamPulse2,
        SteamPulse3,
        SteamPulse4,
        SteamPulse5,
        SteamPulse6,
        SteamPulse7,
        SteamPulse8,
        SteamPulse9,
        SteamPulse10,
        SteamPulse11,
        SteamPulse12,
        SteamPulse13,
        SteamPulse14,
        SteamPulse15,
        SteamPulse16,
        SteamSafetyValveOff,
        SteamSafetyValveOn,
        TakeScreenshot,
        ThrottleChange,
        TrainBrakeChange,
        TrainBrakePressureDecrease,
        TrainBrakePressureIncrease,
        TrainControlSystemActivate,
        TrainControlSystemAlert1,
        TrainControlSystemAlert2,
        TrainControlSystemDeactivate,
        TrainControlSystemInfo1,
        TrainControlSystemInfo2,
        TrainControlSystemPenalty1,
        TrainControlSystemPenalty2,
        TrainControlSystemWarning1,
        TrainControlSystemWarning2,
        MovingTableMovingEmpty,
        MovingTableMovingLoaded,
        MovingTableStopped,
        Uncouple,
        UncoupleB, // NOTE: Currently not used in Open Rails.
        UncoupleC, // NOTE: Currently not used in Open Rails.
        VacuumExhausterOn,
        VacuumExhausterOff,
        VigilanceAlarmOff,
        VigilanceAlarmOn,
        VigilanceAlarmResetPush,
        VigilanceAlarmResetRelease,
        WaterScoopDown,
        WaterScoopUp,
        WiperOff,
        WiperOn,
        _HeadlightDim,
        _HeadlightOff,
        _HeadlightOn,
        _ResetWheelSlip,

        TrainBrakePressureStoppedChanging,
        EngineBrakePressureStoppedChanging,
        BrakePipePressureIncrease,
        BrakePipePressureDecrease,
        BrakePipePressureStoppedChanging,
        CylinderCocksOpen,
        CylinderCocksClose,
        SecondEnginePowerOff,
        SecondEnginePowerOn,

        HotBoxBearingOn,
        HotBoxBearingOff,

        BoilerBlowdownOn,
        BoilerBlowdownOff,

        WaterScoopRaiseLower,
        WaterScoopBroken,

        SteamGearLeverToggle,
        AIFiremanSoundOn,
        AIFiremanSoundOff,

        GearPosition0,
        GearPosition1,
        GearPosition2,
        GearPosition3,
        GearPosition4,
        GearPosition5,
        GearPosition6,
        GearPosition7,
        GearPosition8,

        LargeEjectorOn,
        LargeEjectorOff,
        SmallEjectorOn,
        SmallEjectorOff,
        // Jindrich
        CruiseControlSpeedRegulator,
        CruiseControlSpeedSelector,
        CruiseControlMaxForce,
        Alert,
        Alert1,
        AFB,
        KeyboardBeep,
        KeyboardBeep1,

        MirelOn,
        MirelOff,
        MirelTestBegin,
        MirelOverspeedOn,
        MirelOverspeedOff,
        MirelClearSignalAhead,
        MirelZS1B,
        MirelZS3,
        MirelZS3Off,
        ActiveCabSelectorChange,
        MirelBrakeReleasingPipePressure,
        MirelBrakeFillingPipePressure,
        MirelBrakeReleasingPipePressureFast,
        MirelBrakeFillingPipePressureFast,
        MirekBrakeStopReleaseSound,
        MirekBrakeStopFillSound,
        MirekBrakeStopReleaseFastSound,
        MirekBrakeStopFillFastSound,
        MirelUnwantedVigilancy,
        LS90TestComplete,

        // Icik
        AuxCompressorMode_OffOnOn,
        AuxCompressorMode_OffOnOff,
        CompressorMode_OffAutoOn,
        CompressorMode_OffAutoOff,
        Heating_OffOnOn,
        Heating_OffOnOff,
        CabHeating_OffOnOn,
        CabHeating_OffOnOff,
        HVButtonPress,
        HVButtonRelease,
        SwitchingVoltageMode_OffACOn,
        SwitchingVoltageMode_OffACOff,
        PowerOnAC,
        PowerOffAC,
        PowerOnDC,
        PowerOffDC,
        CircuitBreakerOpenAC,
        CircuitBreakerClosingAC,
        CircuitBreakerClosedAC,
        CircuitBreakerOpenDC,
        CircuitBreakerClosingDC,
        CircuitBreakerClosedDC,
        Pantograph1UpAC,
        Pantograph1DownAC,
        Pantograph2UpAC,
        Pantograph2DownAC,
        Pantograph1UpDC,
        Pantograph1DownDC,
        Pantograph2UpDC,
        Pantograph2DownDC,
        CompressorOffAC,
        CompressorOnAC,
        CompressorOffDC,
        CompressorOnDC,
        QuickReleaseButton,
        LowPressureReleaseButton,
        Compressor2Off,
        Compressor2On,
        AuxCompressorOff,
        AuxCompressorOn,
        MaxMainResOverPressureValveOpen,
        MaxMainResOverPressureValveClosed,
        MaxAuxResOverPressureValveOpen,
        MaxAuxResOverPressureValveClosed,
        CompressorBeep,
        BrakePipeFlow,
        BreakPowerButton,
        BreakPowerButtonRelease,
        QuickReleaseButtonRelease,
        LowPressureReleaseButtonRelease,
        HeatingOverCurrentOn,
        HeatingOverCurrentOff,
        ORTS_BailOff,
        ORTS_BailOffRelease,
        TrainBrakeEmergencyActivated,
        DieselDirectionControllerIn,
        DieselDirectionControllerOut,
        StartUpMotor,
        StartUpMotorStop,
        StartUpMotorBreak,
        InitMotorIdle,
        DieselMotorTempWarning,
        DieselMotorTempWarningOff,
        DieselMotorTempDefected,
        DieselMotorWaterCooling,
        DieselMotorWaterCoolingOff,
        CoupleImpact,
        TMFailure,
        RDSTOn,
        RDSTOff,
        LapButton,
        LapButtonRelease,
        DieselMotorOilCooling,
        DieselMotorOilCoolingOff,
        DieselMotorWaterLowCooling,
        DieselMotorWaterLowCoolingOff,
        DieselMotorOilLowCooling,
        DieselMotorOilLowCoolingOff,
        DirectionButtonPressed,
        DirectionButtonReleased,
        BreakEDBButton,
        BreakEDBButtonRelease,
        MotorStopBreak,
        BrakeSkidStart,
        BrakeSkidStop,
        CouplerPull,
        CouplerPush,
        Failure,
        ActiveFrontCab,
        ActiveRearCab,
        SeasonSwitch,
        LTS410On,
        LTS410Off,
        LTS510On,
        LTS510Off,
        PowerKeyIn,
        PowerKeyOut,
        MirerPush,
        MirerLoosen,
        TMCoolingOn,
        TMCoolingOff,        
        CommandCylinderPositionChangeUp,
        CommandCylinderPositionChangeDown,
        CommandCylinderThrottlePositionChangeUp,
        CommandCylinderThrottlePositionChangeDown,
        DRCoolingOn,
        DRCoolingOff,
        AIPermissionToDepart,
        MUWheelSlipWarningOn,
        MUWheelSlipWarningOff,
        MUWheelSlipOn,
        MUWheelSlipOff,
        ReverserToShOn,
        ReverserToShOff,
        VentilationSwitch
    }

    public static class Events
    {
        public enum Source
        {
            None,
            MSTSCar,
            MSTSCrossing,
            MSTSFuelTower,
            MSTSInGame,
            MSTSSignal,
            ORTSTurntable
        }

        // PLEASE DO NOT EDIT THESE FUNCTIONS without references and testing!
        // These numbers are the MSTS sound triggers and must match
        // MSTS/MSTSBin behaviour whenever possible. NEVER return values for
        // non-MSTS events when passed an MSTS Source.

        public static Event From(bool mstsBinEnabled, Source source, int eventID)
        {
            switch (source)
            {
                case Source.MSTSCar:
                    if (mstsBinEnabled)
                    {
                        switch (eventID)
                        {
                            // MSTSBin codes (documented at http://mstsbin.uktrainsim.com/)
                            case 23: return Event.EnginePowerOn;
                            case 24: return Event.EnginePowerOff;
                            case 66: return Event.Pantograph2Up;
                            case 67: return Event.Pantograph2Down;
                            default: break;
                        }
                    }
                    switch (eventID)
                    {
                        // Calculated from inspection of existing engine .sms files and extensive testing.
                        // Event 1 is unused in MSTS.
                        case 2: return Event.DynamicBrakeIncrease;
                        case 3: return Event.DynamicBrakeOff;
                        case 4: return Event.SanderOn;
                        case 5: return Event.SanderOff;
                        case 6: return Event.WiperOn;
                        case 7: return Event.WiperOff;
                        case 8: return Event.HornOn;
                        case 9: return Event.HornOff;
                        case 10: return Event.BellOn;
                        case 11: return Event.BellOff;
                        case 12: return Event.CompressorOn;
                        case 13: return Event.CompressorOff;
                        case 14: return Event.TrainBrakePressureIncrease;
                        case 15: return Event.ReverserChange;
                        case 16: return Event.ThrottleChange;
                        case 17: return Event.TrainBrakeChange; // Event 17 only works first time in MSTS.
                        case 18: return Event.EngineBrakeChange; // Event 18 only works first time in MSTS; MSTSBin fixes this.
                        // Event 19 is unused in MSTS.
                        case 20: return Event.DynamicBrakeChange;
                        case 21: return Event.EngineBrakePressureIncrease; // Event 21 is defined in sound files but never used in MSTS.
                        case 22: return Event.EngineBrakePressureDecrease; // Event 22 is defined in sound files but never used in MSTS.
                        // Event 23 is unused in MSTS.
                        // Event 24 is unused in MSTS.
                        // Event 25 is possibly a vigilance reset in MSTS sound files but is never used.
                        // Event 26 is a sander toggle in MSTS sound files but is never used.
                        case 27: return Event.WaterInjector2On;
                        case 28: return Event.WaterInjector2Off;
                        // Event 29 is unused in MSTS.
                        case 30: return Event.WaterInjector1On;
                        case 31: return Event.WaterInjector1Off;
                        case 32: return Event.DamperChange;
                        case 33: return Event.BlowerChange;
                        case 34: return Event.CylinderCocksToggle;
                        // Event 35 is unused in MSTS.
                        case 36: return Event.FireboxDoorChange;
                        case 37: return Event.LightSwitchToggle;
                        case 38: return Event.WaterScoopDown;
                        case 39: return Event.WaterScoopUp;
                        case 40: return Event.FireboxDoorOpen; // Used in default steam locomotives (Scotsman and 380)
                        case 41: return Event.FireboxDoorClose;
                        case 42: return Event.SteamSafetyValveOn;
                        case 43: return Event.SteamSafetyValveOff;
                        case 44: return Event.SteamHeatChange; // Event 44 only works first time in MSTS.
                        case 45: return Event.Pantograph1Up;
                        case 46: return Event.Pantograph1Down;
                        case 47: return Event.PantographToggle;
                        case 48: return Event.VigilanceAlarmResetPush;
                        case 49: return Event.VigilanceAlarmResetRelease;
                        // Event 50 is unused in MSTS.
                        // Event 51 is an engine brake of some kind in MSTS sound files but is never used.
                        // Event 52 is unused in MSTS.
                        // Event 53 is a train brake normal apply in MSTS sound files but is never used.
                        case 54: return Event.TrainBrakePressureDecrease; // Event 54 is a train brake emergency apply in MSTS sound files but is actually a train brake pressure decrease.
                        // Event 55 is unused in MSTS.
                        case 56: return Event.VigilanceAlarmOn;
                        case 57: return Event.VigilanceAlarmOff; // Event 57 is triggered constantly in MSTS when the vigilance alarm is off.
                        case 58: return Event.Couple;
                        case 59: return Event.CoupleB;
                        case 60: return Event.CoupleC;
                        case 61: return Event.Uncouple;
                        case 62: return Event.UncoupleB;
                        case 63: return Event.UncoupleC;
                        // Event 64 is unused in MSTS.

                        // ORTS only Events
                        case 101: return Event.GearUp; // for gearbox based engines
                        case 102: return Event.GearDown; // for gearbox based engines
                        case 103: return Event.ReverserToForwardBackward; // reverser moved to forward or backward position
                        case 104: return Event.ReverserToNeutral; // reversed moved to neutral
                        case 105: return Event.DoorOpen; // door opened; propagated to all locos and wagons of the consist
                        case 106: return Event.DoorClose; // door closed; propagated to all locos and wagons of the consist
                        case 107: return Event.MirrorOpen;
                        case 108: return Event.MirrorClose;
                        case 109: return Event.TrainControlSystemInfo1;
                        case 110: return Event.TrainControlSystemInfo2;
                        case 111: return Event.TrainControlSystemActivate;
                        case 112: return Event.TrainControlSystemDeactivate;
                        case 113: return Event.TrainControlSystemPenalty1;
                        case 114: return Event.TrainControlSystemPenalty2;
                        case 115: return Event.TrainControlSystemWarning1;
                        case 116: return Event.TrainControlSystemWarning2;
                        case 117: return Event.TrainControlSystemAlert1;
                        case 118: return Event.TrainControlSystemAlert2;
                        case 119: return Event.CylinderCompoundToggle; // Locomotive switched to compound

                        case 120: return Event.BlowdownValveToggle;
                        case 121: return Event.SteamPulse1;
                        case 122: return Event.SteamPulse2;
                        case 123: return Event.SteamPulse3;
                        case 124: return Event.SteamPulse4;
                        case 125: return Event.SteamPulse5;
                        case 126: return Event.SteamPulse6;
                        case 127: return Event.SteamPulse7;
                        case 128: return Event.SteamPulse8;
                        case 129: return Event.SteamPulse9;
                        case 130: return Event.SteamPulse10;
                        case 131: return Event.SteamPulse11;
                        case 132: return Event.SteamPulse12;
                        case 133: return Event.SteamPulse13;
                        case 134: return Event.SteamPulse14;
                        case 135: return Event.SteamPulse15;
                        case 136: return Event.SteamPulse16;

                        case 137: return Event.CylinderCocksOpen;
                        case 138: return Event.CylinderCocksClose;
                        case 139: return Event.TrainBrakePressureStoppedChanging;
                        case 140: return Event.EngineBrakePressureStoppedChanging;
                        case 141: return Event.BrakePipePressureIncrease;
                        case 142: return Event.BrakePipePressureDecrease;
                        case 143: return Event.BrakePipePressureStoppedChanging;

                        case 145: return Event.WaterScoopRaiseLower;
                        case 146: return Event.WaterScoopBroken;

                        case 147: return Event.SteamGearLeverToggle;
                        case 148: return Event.AIFiremanSoundOn;
                        case 149: return Event.AIFiremanSoundOff;

                        case 150: return Event.CircuitBreakerOpen;
                        case 151: return Event.CircuitBreakerClosing;
                        case 152: return Event.CircuitBreakerClosed;
                        case 153: return Event.CircuitBreakerClosingOrderOn;
                        case 154: return Event.CircuitBreakerClosingOrderOff;
                        case 155: return Event.CircuitBreakerOpeningOrderOn;
                        case 156: return Event.CircuitBreakerOpeningOrderOff;
                        case 157: return Event.CircuitBreakerClosingAuthorizationOn;
                        case 158: return Event.CircuitBreakerClosingAuthorizationOff;

                        case 159: return Event.LargeEjectorChange;
                        case 160: return Event.SmallEjectorChange;

                        case 161: return Event.CabLightSwitchToggle;
                        case 162: return Event.CabRadioOn;
                        case 163: return Event.CabRadioOff;

                        case 164: return Event.BrakesStuck;

                        case 165: return Event.VacuumExhausterOn;
                        case 166: return Event.VacuumExhausterOff;
                        case 167: return Event.SecondEnginePowerOn;
                        case 168: return Event.SecondEnginePowerOff;

                        case 169: return Event.Pantograph3Up;
                        case 170: return Event.Pantograph3Down;
                        case 171: return Event.Pantograph4Up;
                        case 172: return Event.Pantograph4Down;

                        case 173: return Event.HotBoxBearingOn;
                        case 174: return Event.HotBoxBearingOff;

                        case 175: return Event.BoilerBlowdownOn;
                        case 176: return Event.BoilerBlowdownOff;

                        case 177: return Event.BatteryOn;
                        case 178: return Event.BatteryOff;

                        case 179: return Event.PowerKeyOn;
                        case 180: return Event.PowerKeyOff;

                        case 181: return Event.GenericEvent1;
                        case 182: return Event.GenericEvent2;
                        case 183: return Event.GenericEvent3;
                        case 184: return Event.GenericEvent4;
                        case 185: return Event.GenericEvent5;
                        case 186: return Event.GenericEvent6;
                        case 187: return Event.GenericEvent7;
                        case 188: return Event.GenericEvent8;
                        //


                        case 200: return Event.GearPosition0;
                        case 201: return Event.GearPosition1;
                        case 202: return Event.GearPosition2;
                        case 203: return Event.GearPosition3;
                        case 204: return Event.GearPosition4;
                        case 205: return Event.GearPosition5;
                        case 206: return Event.GearPosition6;
                        case 207: return Event.GearPosition7;
                        case 208: return Event.GearPosition8;

                        case 210: return Event.LargeEjectorOn;
                        case 211: return Event.LargeEjectorOff;
                        case 212: return Event.SmallEjectorOn;
                        case 213: return Event.SmallEjectorOff;

                        // Jindrich
                        case 300: return Event.CruiseControlSpeedRegulator;
                        case 301: return Event.CruiseControlSpeedSelector;
                        case 302: return Event.CruiseControlMaxForce;
                        case 303: return Event.Alert;
                        case 304: return Event.Alert1;

                        case 305: return Event.KeyboardBeep;
                        case 306: return Event.KeyboardBeep1;

                        case 10169: return Event.MirelOn;
                        case 10170: return Event.MirelOff;
                        case 10171: return Event.MirelTestBegin;
                        case 10172: return Event.MirelOverspeedOn;
                        case 10173: return Event.MirelOverspeedOff;
                        case 10174: return Event.MirelClearSignalAhead;
                        case 10175: return Event.MirelZS1B;
                        case 10177: return Event.MirekBrakeStopReleaseSound;
                        case 10178: return Event.MirelBrakeFillingPipePressure;
                        case 10179: return Event.MirekBrakeStopFillSound;
                        case 10180: return Event.MirelBrakeReleasingPipePressureFast;
                        case 10181: return Event.MirekBrakeStopReleaseFastSound;
                        case 10182: return Event.MirelBrakeFillingPipePressureFast;
                        case 10183: return Event.MirekBrakeStopFillFastSound;
                        case 10184: return Event.MirelUnwantedVigilancy;
                        case 10185: return Event.MirelZS3;
                        case 10186: return Event.MirelZS3Off;
                        case 10188: return Event.ActiveCabSelectorChange;
                        case 10195: return Event.LS90TestComplete;
                        case 10196: return Event.AFB;
                        case 10197: return Event.Failure;

                        // Icik
                        case 20001: return Event.CompressorMode_OffAutoOn;
                        case 20002: return Event.CompressorMode_OffAutoOff;
                        case 20003: return Event.Heating_OffOnOn;
                        case 20004: return Event.Heating_OffOnOff;
                        case 20005: return Event.HVButtonPress;
                        case 20006: return Event.HVButtonRelease;
                        case 20007: return Event.SwitchingVoltageMode_OffACOn;
                        case 20008: return Event.SwitchingVoltageMode_OffACOff;
                        case 20009: return Event.PowerOnAC; // 23
                        case 20010: return Event.PowerOffAC; // 24
                        case 20011: return Event.PowerOnDC; // 23
                        case 20012: return Event.PowerOffDC; // 24
                        case 20013: return Event.CircuitBreakerOpenAC; // 150
                        case 20014: return Event.CircuitBreakerClosingAC; // 151
                        case 20015: return Event.CircuitBreakerClosedAC; // 152
                        case 20016: return Event.CircuitBreakerOpenDC; // 150
                        case 20017: return Event.CircuitBreakerClosingDC; // 151
                        case 20018: return Event.CircuitBreakerClosedDC; // 152
                        case 20019: return Event.Pantograph1UpAC; // 45
                        case 20020: return Event.Pantograph1DownAC; // 46
                        case 20021: return Event.Pantograph2UpAC; // 66
                        case 20022: return Event.Pantograph2DownAC; // 67
                        case 20023: return Event.Pantograph1UpDC; // 45
                        case 20024: return Event.Pantograph1DownDC; // 46
                        case 20025: return Event.Pantograph2UpDC; // 66
                        case 20026: return Event.Pantograph2DownDC; // 67
                        case 20027: return Event.CompressorOnAC; // 12
                        case 20028: return Event.CompressorOffAC; // 13
                        case 20029: return Event.CompressorOnDC; // 12
                        case 20030: return Event.CompressorOffDC; // 13
                        case 20031: return Event.QuickReleaseButton;
                        case 20032: return Event.LowPressureReleaseButton;
                        case 20033: return Event.Compressor2On;
                        case 20034: return Event.Compressor2Off;
                        case 20035: return Event.AuxCompressorMode_OffOnOn;
                        case 20036: return Event.AuxCompressorMode_OffOnOff;
                        case 20037: return Event.AuxCompressorOn;
                        case 20038: return Event.AuxCompressorOff;
                        case 20039: return Event.MaxMainResOverPressureValveOpen;
                        case 20040: return Event.MaxMainResOverPressureValveClosed;
                        case 20041: return Event.MaxAuxResOverPressureValveOpen;
                        case 20042: return Event.MaxAuxResOverPressureValveClosed;
                        case 20043: return Event.CompressorBeep;
                        case 20044: return Event.BrakePipeFlow;
                        case 20045: return Event.BreakPowerButton;
                        case 20046: return Event.BreakPowerButtonRelease;
                        case 20047: return Event.QuickReleaseButtonRelease;
                        case 20048: return Event.LowPressureReleaseButtonRelease;
                        case 20049: return Event.HeatingOverCurrentOn;
                        case 20050: return Event.HeatingOverCurrentOff;
                        case 20051: return Event.CabHeating_OffOnOn;
                        case 20052: return Event.CabHeating_OffOnOff;
                        case 20053: return Event.ORTS_BailOff;
                        case 20054: return Event.ORTS_BailOffRelease;
                        case 20055: return Event.TrainBrakeEmergencyActivated;
                        case 20056: return Event.DieselDirectionControllerIn;
                        case 20057: return Event.DieselDirectionControllerOut;
                        case 20058: return Event.StartUpMotor;
                        case 20059: return Event.StartUpMotorStop;
                        case 20060: return Event.StartUpMotorBreak;
                        case 20061: return Event.InitMotorIdle;
                        case 20062: return Event.DieselMotorTempWarning;
                        case 20063: return Event.DieselMotorTempWarningOff;
                        case 20064: return Event.DieselMotorTempDefected;
                        case 20065: return Event.DieselMotorWaterCooling;
                        case 20066: return Event.DieselMotorWaterCoolingOff;
                        case 20067: return Event.CoupleImpact;
                        case 20068: return Event.TMFailure;
                        case 20069: return Event.RDSTOn;
                        case 20070: return Event.RDSTOff;
                        case 20071: return Event.LapButton;
                        case 20072: return Event.LapButtonRelease;
                        case 20073: return Event.DieselMotorOilCooling;
                        case 20074: return Event.DieselMotorOilCoolingOff;
                        case 20075: return Event.DieselMotorWaterLowCooling;
                        case 20076: return Event.DieselMotorWaterLowCoolingOff;
                        case 20077: return Event.DieselMotorOilLowCooling;
                        case 20078: return Event.DieselMotorOilLowCoolingOff;
                        case 20079: return Event.DirectionButtonPressed;
                        case 20080: return Event.DirectionButtonReleased;
                        case 20081: return Event.BreakEDBButton;
                        case 20082: return Event.BreakEDBButtonRelease;
                        case 20083: return Event.MotorStopBreak;
                        case 20084: return Event.BrakeSkidStart;
                        case 20085: return Event.BrakeSkidStop;
                        case 20086: return Event.CouplerPull;
                        case 20087: return Event.CouplerPush;
                        case 20088: return Event.ActiveFrontCab;
                        case 20089: return Event.ActiveRearCab;
                        case 20090: return Event.SeasonSwitch;                        
                        case 20091: return Event.LTS410On;
                        case 20092: return Event.LTS410Off;
                        case 20093: return Event.LTS510On;
                        case 20094: return Event.LTS510Off;
                        case 20095: return Event.PowerKeyIn;
                        case 20096: return Event.PowerKeyOut;
                        case 20097: return Event.MirerPush;
                        case 20098: return Event.MirerLoosen;
                        case 20099: return Event.TMCoolingOn;
                        case 20100: return Event.TMCoolingOff;
                        case 20101: return Event.CommandCylinderPositionChangeUp;
                        case 20102: return Event.CommandCylinderPositionChangeDown;
                        case 20103: return Event.CommandCylinderThrottlePositionChangeUp;
                        case 20104: return Event.CommandCylinderThrottlePositionChangeDown;
                        case 20105: return Event.DRCoolingOn;
                        case 20106: return Event.DRCoolingOff;
                        case 20107: return Event.AIPermissionToDepart;
                        case 20108: return Event.MUWheelSlipWarningOn;
                        case 20109: return Event.MUWheelSlipWarningOff;
                        case 20110: return Event.MUWheelSlipOn;
                        case 20111: return Event.MUWheelSlipOff;
                        case 20112: return Event.ReverserToShOn;
                        case 20113: return Event.ReverserToShOff;
                        case 20114:return Event.VentilationSwitch;

                        default: return 0;
                    }
                case Source.MSTSCrossing:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing crossing.sms files.
                        case 3: return Event.CrossingClosing;
                        case 4: return Event.CrossingOpening;
                        default: return 0;
                    }
                case Source.MSTSFuelTower:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing *tower.sms files.
                        case 6: return Event.FuelTowerDown;
                        case 7: return Event.FuelTowerUp;
                        case 9: return Event.FuelTowerTransferStart;
                        case 10: return Event.FuelTowerTransferEnd;
                        default: return 0;
                    }
                case Source.MSTSInGame:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing ingame.sms files.
                        //case 10: return Event.ControlError;
                        case 20: return Event.Derail1;
                        case 21: return Event.Derail2;
                        case 22: return Event.Derail3;
                        case 25: return 0; // TODO: What is this event?
                        case 60: return Event.PermissionToDepart;
                        case 61: return Event.PermissionGranted;
                        case 62: return Event.PermissionDenied;
                        default: return 0;
                    }
                case Source.MSTSSignal:
                    switch (eventID)
                    {
                        // Calculated from inspection of existing signal.sms files.
                        case 1: return Event.SemaphoreArm;
                        default: return 0;
                    }
                case Source.ORTSTurntable:
                    switch (eventID)
                    {
                        // related file is turntable.sms
                        case 1: return Event.MovingTableMovingEmpty;
                        case 2: return Event.MovingTableMovingLoaded;
                        case 3: return Event.MovingTableStopped;
                        default: return 0;
                    }
                default: return 0;
            }
        }
    }
}
