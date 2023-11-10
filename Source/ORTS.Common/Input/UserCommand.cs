﻿namespace ORTS.Common.Input
{
    /// <summary>
    /// Specifies game commands.
    /// </summary>
    /// <remarks>
    /// <para>The ordering and naming of these commands is important. They are listed in the UI in the order they are defined in the code, and the first word of each command is the "group" to which it belongs.</para>
    /// </remarks>
    public enum UserCommand
    {
        [GetString("Game Pause Menu")] GamePauseMenu,
        [GetString("Game Save")] GameSave,
        [GetString("Game Quit")] GameQuit,
        [GetString("Game Pause")] GamePause,
        [GetString("Game Screenshot")] GameScreenshot,
        [GetString("Game Fullscreen")] GameFullscreen,
        [GetString("Game External Controller (RailDriver)")] GameExternalCabController,
        //        [GetString("Game Switch Ahead")] GameSwitchAhead,
        //        [GetString("Game Switch Behind")] GameSwitchBehind,
        [GetString("Game Facing Switch Ahead")] GameFacingSwitchAhead,
        [GetString("Game Facing Switch Behind")] GameFacingSwitchBehind,
        [GetString("Game Switch Picked")] GameSwitchPicked,
        [GetString("Game Signal Picked")] GameSignalPicked,
        [GetString("Game Trailing Switch Ahead")] GameTrailingSwitchAhead,
        [GetString("Game Trailing Switch Behind")] GameTrailingSwitchBehind,
        [GetString("Game Switch With Mouse")] GameSwitchWithMouse,
        [GetString("Game Uncouple With Mouse")] GameUncoupleWithMouse,
        [GetString("Game Change Cab")] GameChangeCab,
        [GetString("Game Request Control")] GameRequestControl,
        [GetString("Game Multi Player Dispatcher")] GameMultiPlayerDispatcher,
        [GetString("Game Multi Player Texting")] GameMultiPlayerTexting,
        [GetString("Game Switch Manual Mode")] GameSwitchManualMode,
        [GetString("Game Clear Signal Forward")] GameClearSignalForward,
        [GetString("Game Clear Signal Backward")] GameClearSignalBackward,
        [GetString("Game Reset Signal Forward")] GameResetSignalForward,
        [GetString("Game Reset Signal Backward")] GameResetSignalBackward,
        [GetString("Game Autopilot Mode")] GameAutopilotMode,
        [GetString("Game Suspend Old Player")] GameSuspendOldPlayer,
        [GetString("Game Manual Coupling")] GameManualCoupling,


        [GetString("Display Next Window Tab")] DisplayNextWindowTab,
        [GetString("Display Help Window")] DisplayHelpWindow,
        [GetString("Display Track Monitor Window")] DisplayTrackMonitorWindow,
        [GetString("Display HUD")] DisplayHUD,
        [GetString("Display Train Driving Window")] DisplayTrainDrivingWindow,
        [GetString("Display Car Labels")] DisplayCarLabels,
        [GetString("Display Station Labels")] DisplayStationLabels,
        [GetString("Display Switch Window")] DisplaySwitchWindow,
        [GetString("Display Train Operations Window")] DisplayTrainOperationsWindow,
        [GetString("Display Next Station Window")] DisplayNextStationWindow,
        [GetString("Display Compass Window")] DisplayCompassWindow,
        [GetString("Display Basic HUD Toggle")] DisplayBasicHUDToggle,
        [GetString("Display Train List Window")] DisplayTrainListWindow,
        [GetString("Display Passenger List Window")] DisplayPassengerListWindow,

        [GetString("Debug Speed Up")] DebugSpeedUp,
        [GetString("Debug Speed Down")] DebugSpeedDown,
        [GetString("Debug Speed Reset")] DebugSpeedReset,
        [GetString("Debug Overcast Increase")] DebugOvercastIncrease,
        [GetString("Debug Overcast Decrease")] DebugOvercastDecrease,
        [GetString("Debug Fog Increase")] DebugFogIncrease,
        [GetString("Debug Fog Decrease")] DebugFogDecrease,
        [GetString("Debug Precipitation Increase")] DebugPrecipitationIncrease,
        [GetString("Debug Precipitation Decrease")] DebugPrecipitationDecrease,
        [GetString("Debug Precipitation Liquidity Increase")] DebugPrecipitationLiquidityIncrease,
        [GetString("Debug Precipitation Liquidity Decrease")] DebugPrecipitationLiquidityDecrease,
        [GetString("Debug Weather Change")] DebugWeatherChange,
        [GetString("Debug Clock Forwards")] DebugClockForwards,
        [GetString("Debug Clock Backwards")] DebugClockBackwards,
        [GetString("Debug Logger")] DebugLogger,
        [GetString("Debug Lock Shadows")] DebugLockShadows,
        [GetString("Debug Dump Keyboard Map")] DebugDumpKeymap,
        [GetString("Debug Log Render Frame")] DebugLogRenderFrame,
        [GetString("Debug Tracks")] DebugTracks,
        [GetString("Debug Signalling")] DebugSignalling,
        [GetString("Debug Reset Wheel Slip")] DebugResetWheelSlip,
        [GetString("Debug Toggle Advanced Adhesion")] DebugToggleAdvancedAdhesion,
        [GetString("Debug Sound Form")] DebugSoundForm,
        [GetString("Debug Physics Form")] DebugPhysicsForm,
        [GetString("Debug Toggle Confirmations")] DebugToggleConfirmations,

        [GetString("Camera Cab")] CameraCab,
        [GetString("Camera Change Passenger Viewpoint")] CameraChangePassengerViewPoint,
        [GetString("Camera Toggle 3D Cab")] CameraToggleThreeDimensionalCab,
        [GetString("Camera Toggle Show Cab")] CameraToggleShowCab,
        [GetString("Camera Toggle Letterbox Cab")] CameraToggleLetterboxCab,
        [GetString("Camera Head Out Forward")] CameraHeadOutForward,
        [GetString("Camera Head Out Backward")] CameraHeadOutBackward,
        [GetString("Camera Outside Front")] CameraOutsideFront,
        [GetString("Camera Outside Rear")] CameraOutsideRear,
        [GetString("Camera Trackside")] CameraTrackside,
        [GetString("Camera SpecialTracksidePoint")] CameraSpecialTracksidePoint,
        [GetString("Camera Passenger")] CameraPassenger,
        [GetString("Camera Brakeman")] CameraBrakeman,
        [GetString("Camera Free")] CameraFree,
        [GetString("Camera Previous Free")] CameraPreviousFree,
        [GetString("Camera Reset")] CameraReset,
        [GetString("Camera Move Fast")] CameraMoveFast,
        [GetString("Camera Move Slow")] CameraMoveSlow,
        [GetString("Camera Pan (Rotate) Left")] CameraPanLeft,
        [GetString("Camera Pan (Rotate) Right")] CameraPanRight,
        [GetString("Camera Pan (Rotate) Up")] CameraPanUp,
        [GetString("Camera Pan (Rotate) Down")] CameraPanDown,
        [GetString("Camera Zoom In (Move Z)")] CameraZoomIn,
        [GetString("Camera Zoom Out (Move Z)")] CameraZoomOut,
        [GetString("Camera Rotate (Pan) Left")] CameraRotateLeft,
        [GetString("Camera Rotate (Pan) Right")] CameraRotateRight,
        [GetString("Camera Rotate (Pan) Up")] CameraRotateUp,
        [GetString("Camera Rotate (Pan) Down")] CameraRotateDown,
        [GetString("Camera Car Next")] CameraCarNext,
        [GetString("Camera Car Previous")] CameraCarPrevious,
        [GetString("Camera Car First")] CameraCarFirst,
        [GetString("Camera Car Last")] CameraCarLast,
        [GetString("Camera Jumping Trains")] CameraJumpingTrains,
        [GetString("Camera Jump Back Player")] CameraJumpBackPlayer,
        [GetString("Camera Jump See Switch")] CameraJumpSeeSwitch,
        [GetString("Camera Vibrate")] CameraVibrate,
        [GetString("Camera Scroll Right")] CameraScrollRight,
        [GetString("Camera Scroll Left")] CameraScrollLeft,
        [GetString("Camera Browse Backwards")] CameraBrowseBackwards,
        [GetString("Camera Browse Forwards")] CameraBrowseForwards,

        [GetString("TCS Zapnout VZ")] ControlGeneric1,
        [GetString("TCS Přepnout VZ")] ControlGeneric2,

        [GetString("Control_Classic Forwards")] ControlForwards,
        [GetString("Control_Classic Backwards")] ControlBackwards,
        [GetString("Control_Classic Throttle Increase")] ControlThrottleIncrease,
        [GetString("Control_Classic Throttle Decrease")] ControlThrottleDecrease,
        [GetString("Control_Classic Throttle Zero")] ControlThrottleZero,
        [GetString("Control_Classic Gear Up")] ControlGearUp,
        [GetString("Control_Classic Gear Down")] ControlGearDown,
        [GetString("Control_Classic Train Brake Increase")] ControlTrainBrakeIncrease,
        [GetString("Control_Classic Train Brake Decrease")] ControlTrainBrakeDecrease,
        [GetString("Control_Classic Train Brake Zero")] ControlTrainBrakeZero,
        [GetString("Control_Classic Engine Brake Increase")] ControlEngineBrakeIncrease,
        [GetString("Control_Classic Engine Brake Decrease")] ControlEngineBrakeDecrease,
        [GetString("Control_Classic Engine Brake Increase Alternative")] ControlEngineBrakeIncrease1,
        [GetString("Control_Classic Engine Brake Decrease Alternative")] ControlEngineBrakeDecrease1,
        [GetString("Control_Classic Brakeman Brake Increase")] ControlBrakemanBrakeIncrease,
        [GetString("Control_Classic Brakeman Brake Decrease")] ControlBrakemanBrakeDecrease,
        [GetString("Control_Classic Dynamic Brake Increase")] ControlDynamicBrakeIncrease,
        [GetString("Control_Classic Dynamic Brake Decrease")] ControlDynamicBrakeDecrease,
        [GetString("Control_Classic Bail Off")] ControlBailOff,
        [GetString("Control_Classic Initialize Brakes")] ControlInitializeBrakes,
        [GetString("Control_Classic Handbrake Full")] ControlHandbrakeFull,
        [GetString("Control_Classic Handbrake None")] ControlHandbrakeNone,
        [GetString("Control_Classic Odometer Show/Hide")] ControlOdoMeterShowHide,
        [GetString("Control_Classic Odometer Reset")] ControlOdoMeterReset,
        [GetString("Control_Classic Odometer Direction")] ControlOdoMeterDirection,
        [GetString("Control_Classic Retainers On")] ControlRetainersOn,
        [GetString("Control_Classic Retainers Off")] ControlRetainersOff,
        [GetString("Control_Classic Brake Hose Connect")] ControlBrakeHoseConnect,
        [GetString("Control_Classic Brake Hose Disconnect")] ControlBrakeHoseDisconnect,
        [GetString("Control_Classic Alerter")] ControlAlerter,
        [GetString("Control_Classic Emergency Push Button")] ControlEmergencyPushButton,
        [GetString("Control_Classic Sander")] ControlSander,
        [GetString("Control_Classic Sander Toggle")] ControlSanderToggle,
        [GetString("Control_Classic Wiper")] ControlWiper,
        [GetString("Control_Classic Horn")] ControlHorn,
        [GetString("Control_Classic Bell")] ControlBell,
        [GetString("Control_Classic Bell Toggle")] ControlBellToggle,
        [GetString("Control_Classic Door Left")] ControlDoorLeft,
        [GetString("Control_Classic Door Right")] ControlDoorRight,
        [GetString("Control_Classic Mirror")] ControlMirror,
        [GetString("Control_Classic Light")] ControlLight,
        [GetString("Control_Classic Flood Light")] ControlFloodLight,
        [GetString("Control_Classic Pantograph 1")] ControlPantograph1,
        [GetString("Control_Classic Pantograph 2")] ControlPantograph2,
        [GetString("Control_Classic Pantograph 3")] ControlPantograph3,
        [GetString("Control_Classic Pantograph 4")] ControlPantograph4,
        [GetString("Control_Classic Circuit Breaker Closing Order")] ControlCircuitBreakerClosingOrder,
        [GetString("Control_Classic Circuit Breaker Opening Order")] ControlCircuitBreakerOpeningOrder,
        [GetString("Control_Classic Circuit Breaker Closing Authorization")] ControlCircuitBreakerClosingAuthorization,
        [GetString("Control_Classic Diesel Player")] ControlDieselPlayer,
        [GetString("Control_Classic Diesel Helper")] ControlDieselHelper,
        [GetString("Control_Classic Headlight Increase")] ControlHeadlightIncrease,
        [GetString("Control_Classic Headlight Decrease")] ControlHeadlightDecrease,
        [GetString("Control_Classic Injector 1 Increase")] ControlInjector1Increase,
        [GetString("Control_Classic Injector 1 Decrease")] ControlInjector1Decrease,
        [GetString("Control_Classic Injector 1")] ControlInjector1,
        [GetString("Control_Classic Injector 2 Increase")] ControlInjector2Increase,
        [GetString("Control_Classic Injector 2 Decrease")] ControlInjector2Decrease,
        [GetString("Control_Classic Injector 2")] ControlInjector2,
        [GetString("Control_Classic Blowdown Valve")] ControlBlowdownValve,
        [GetString("Control_Classic Blower Increase")] ControlBlowerIncrease,
        [GetString("Control_Classic Blower Decrease")] ControlBlowerDecrease,
        [GetString("Control_Classic Steam Heat Increase")] ControlSteamHeatIncrease,
        [GetString("Control_Classic Steam Heat Decrease")] ControlSteamHeatDecrease,
        [GetString("Control_Classic Damper Increase")] ControlDamperIncrease,
        [GetString("Control_Classic Damper Decrease")] ControlDamperDecrease,
        [GetString("Control_Classic Firebox Open")] ControlFireboxOpen,
        [GetString("Control_Classic Firebox Close")] ControlFireboxClose,
        [GetString("Control_Classic Firing Rate Increase")] ControlFiringRateIncrease,
        [GetString("Control_Classic Firing Rate Decrease")] ControlFiringRateDecrease,
        [GetString("Control_Classic Fire Shovel Full")] ControlFireShovelFull,
        [GetString("Control_Classic Cylinder Cocks")] ControlCylinderCocks,
        [GetString("Control_Classic Large Ejector Increase")] ControlLargeEjectorIncrease,
        [GetString("Control_Classic Large Ejector Decrease")] ControlLargeEjectorDecrease,
        [GetString("Control_Classic Small Ejector Increase")] ControlSmallEjectorIncrease,
        [GetString("Control_Classic Small Ejector Decrease")] ControlSmallEjectorDecrease,
        [GetString("Control_Classic Vacuum Exhauster")] ControlVacuumExhausterPressed,
        [GetString("Control_Classic Cylinder Compound")] ControlCylinderCompound,
        [GetString("Control_Classic Firing")] ControlFiring,
        [GetString("Control_Classic Refill")] ControlRefill,
        [GetString("Control_Classic Water Scoop")] ControlWaterScoop,
        [GetString("Control_Classic ImmediateRefill")] ControlImmediateRefill,
        [GetString("Control_Classic Turntable Clockwise")] ControlTurntableClockwise,
        [GetString("Control_Classic Turntable Counterclockwise")] ControlTurntableCounterclockwise,
        [GetString("Control_Classic Cab Radio")] ControlCabRadio,
        [GetString("Control_Classic AI Fire On")] ControlAIFireOn,
        [GetString("Control_Classic AI Fire Off")] ControlAIFireOff,
        [GetString("Control_Classic AI Fire Reset")] ControlAIFireReset,
        [GetString("Control_Classic Battery")] ControlBattery,
        //[GetString("Control_Classic PowerKey")] ControlPowerKey,

        // Jindrich
        [GetString("Control_CZSK Mirel Off")] SetMirelOff,
        [GetString("Control_CZSK Mirel On")] SetMirelOn,
        [GetString("Control_CZSK Speed Regulator Mode Increase")] ControlSpeedRegulatorModeIncrease,
        [GetString("Control_CZSK Speed Regulator Mode Descrease")] ControlSpeedRegulatorModeDecrease,
        [GetString("Control_CZSK Selected Speed Increase")] ControlSpeedRegulatorSelectedSpeedIncrease,
        [GetString("Control_CZSK Selected Speed Decrease")] ControlSpeedRegulatorSelectedSpeedDecrease,
        [GetString("Control_CZSK Speed Regulator Max Acceleration Increase")] ControlSpeedRegulatorMaxAccelerationIncrease,
        [GetString("Control_CZSK Speed Regulator Max Acceleration Decrease")] ControlSpeedRegulatorMaxAccelerationDecrease,
        [GetString("Control_CZSK Number Of Axles Increase")] ControlNumberOfAxlesIncrease,
        [GetString("Control_CZSK Number Of Axles Decrease")] ControlNumberOfAxlesDecrease,
        [GetString("Control_CZSK Restricted Speed Zone Active")] ControlRestrictedSpeedZoneActive,
        [GetString("Control_CZSK Cruise Control Mode Increase")] ControlCruiseControlModeIncrease,
        [GetString("Control_CZSK Cruise Control Mode Decrease")] ControlCruiseControlModeDecrease,
        [GetString("Control_CZSK Confirm Selected Speed")] ControlConfirmSelectedSpeed,
        [GetString("Control_CZSK Train Type Change (Passenger/Cargo)")] ControlTrainTypePaxCargo,
        [GetString("Control_CZSK Select Speed 10 kph/mph")] ControlSelectSpeed10,
        [GetString("Control_CZSK Select Speed 20 kph/mph")] ControlSelectSpeed20,
        [GetString("Control_CZSK Select Speed 30 kph/mph")] ControlSelectSpeed30,
        [GetString("Control_CZSK Select Speed 40 kph/mph")] ControlSelectSpeed40,
        [GetString("Control_CZSK Select Speed 50 kph/mph")] ControlSelectSpeed50,
        [GetString("Control_CZSK Select Speed 60 kph/mph")] ControlSelectSpeed60,
        [GetString("Control_CZSK Select Speed 70 kph/mph")] ControlSelectSpeed70,
        [GetString("Control_CZSK Select Speed 80 kph/mph")] ControlSelectSpeed80,
        [GetString("Control_CZSK Select Speed 90 kph/mph")] ControlSelectSpeed90,
        [GetString("Control_CZSK Select Speed 100 kph/mph")] ControlSelectSpeed100,
        [GetString("Control_CZSK Select Speed 110 kph/mph")] ControlSelectSpeed110,
        [GetString("Control_CZSK Select Speed 120 kph/mph")] ControlSelectSpeed120,
        [GetString("Control_CZSK Select Speed 130 kph/mph")] ControlSelectSpeed130,
        [GetString("Control_CZSK Select Speed 140 kph/mph")] ControlSelectSpeed140,
        [GetString("Control_CZSK Select Speed 150 kph/mph")] ControlSelectSpeed150,
        [GetString("Control_CZSK Select Speed 160 kph/mph")] ControlSelectSpeed160,
        [GetString("Control_CZSK Select Speed 170 kph/mph")] ControlSelectSpeed170,
        [GetString("Control_CZSK Select Speed 180 kph/mph")] ControlSelectSpeed180,
        [GetString("Control_CZSK Select Speed 190 kph/mph")] ControlSelectSpeed190,
        [GetString("Control_CZSK Select Speed 200 kph/mph")] ControlSelectSpeed200,
        [GetString("Control_CZSK Mirel Key Plus")] MirelKeyPlus,
        [GetString("Control_CZSK Mirel Key Minus")] MirelKeyMinus,
        [GetString("Control_CZSK Mirel Key Enter")] MirelKeyEnter,
        [GetString("Control_CZSK Cab Select Increase")] CabSelectIncrease,
        [GetString("Control_CZSK Cab Select Decrease")] CabSelectDecrease,
        [GetString("Control_CZSK Display Key Position")] DisplayKeyPosition,
        [GetString("Control_CZSK Change Key Position")] ChangeKeyPosition,

        // Icik
        [GetString("Control_CZSK HV2 1-system +")] ControlHV2SwitchUp,
        [GetString("Control_CZSK HV3 2-system +")] ControlHV3SwitchUp,
        [GetString("Control_CZSK HV3 2-system -")] ControlHV3SwitchDown,
        [GetString("Control_CZSK HV4 2-system +")] ControlHV4SwitchUp,
        [GetString("Control_CZSK HV4 2-system -")] ControlHV4SwitchDown,
        [GetString("Control_CZSK HV5 2-system +")] ControlHV5SwitchUp,
        [GetString("Control_CZSK HV5 2-system -")] ControlHV5SwitchDown,
        [GetString("Control_CZSK Pantograph3 +")] ControlPantograph3SwitchUp,
        [GetString("Control_CZSK Pantograph3 -")] ControlPantograph3SwitchDown,
        [GetString("Control_CZSK Pantograph4 +")] ControlPantograph4SwitchUp,
        [GetString("Control_CZSK Pantograph4 -")] ControlPantograph4SwitchDown,
        [GetString("Control_CZSK Pantograph5 +")] ControlPantograph5SwitchUp,
        [GetString("Control_CZSK Pantograph5 -")] ControlPantograph5SwitchDown,
        [GetString("Control_CZSK Compressor I Combined +")] ControlCompressorCombinedUp,
        [GetString("Control_CZSK Compressor I Combined -")] ControlCompressorCombinedDown,
        [GetString("Control_CZSK Compressor II Combined +")] ControlCompressorCombined2Up,
        [GetString("Control_CZSK Compressor II Combined -")] ControlCompressorCombined2Down,
        [GetString("Control_CZSK AuxCompressor Off/On")] ControlAuxCompressorMode_OffOn,
        [GetString("Control_CZSK Compressor I Mode Off/Auto")] ControlCompressorMode_OffAuto,
        [GetString("Control_CZSK Compressor II Mode Off/Auto")] ControlCompressorMode2_OffAuto,
        [GetString("Control_CZSK Heating Off/On")] ControlHeating_OffOn,
        [GetString("Control_CZSK Heating in Cab Off/On")] ControlCabHeating_OffOn,
        [GetString("Control_CZSK Locomotive PowerVoltage 25kV/both/3kV")] ControlRouteVoltage,
        [GetString("Control_CZSK Quickrelease button")] ControlQuickReleaseButton,
        [GetString("Control_CZSK Lowpressurerelease button")] ControlLowPressureReleaseButton,
        [GetString("Control_CZSK Breakpower button")] ControlBreakPowerButton,
        [GetString("Control_CZSK Diesel Controller +")] ControlDieselDirectionControllerUp,
        [GetString("Control_CZSK Diesel Controller -")] ControlDieselDirectionControllerDown,
        [GetString("Control_CZSK Diesel Controller In/Out")] ControlDieselDirectionControllerInOut,
        [GetString("Control_CZSK RDST Breaker")] ControlRDSTBreaker,
        [GetString("Control_CZSK Lap button")] ControlLapButton,
        [GetString("Control_CZSK Disabling EDB button")] ControlBreakEDBButton,        
        [GetString("Control_CZSK Frontlight left white")] ControlLightFrontLDown,
        [GetString("Control_CZSK Frontlight left red")] ControlLightFrontLUp,
        [GetString("Control_CZSK Frontlight right white")] ControlLightFrontRDown,
        [GetString("Control_CZSK Frontlight right red")] ControlLightFrontRUp,
        [GetString("Control_CZSK Rearlight left white")] ControlLightRearLDown,
        [GetString("Control_CZSK Rearlight left red")] ControlLightRearLUp,
        [GetString("Control_CZSK Rearlight right white")] ControlLightRearRDown,
        [GetString("Control_CZSK Rearlight right red")] ControlLightRearRUp,
        [GetString("Control_CZSK Season heating switch")] ControlSeasonSwitch,
        [GetString("Control_CZSK Mirer +")] ControlMirerControllerUp,
        [GetString("Control_CZSK Mirer -")] ControlMirerControllerDown,        
        [GetString("Control_CZSK Powerkey +")] ControlPowerKeyUp,
        [GetString("Control_CZSK Powerkey -")] ControlPowerKeyDown,
        [GetString("Control_CZSK Ventilator +")] ControlVentilationUp,
        [GetString("Control_CZSK Ventilator -")] ControlVentilationDown,
        [GetString("Control_CZSK Automatic start-up button")] ControlAutoDriveButton,
        [GetString("Control_CZSK Automatic start-up SpeedSelector +")] ControlAutoDriveSpeedSelectorUp,
        [GetString("Control_CZSK Automatic start-up SpeedSelector -")] ControlAutoDriveSpeedSelectorDown,
        [GetString("Control_CZSK Axle Counter +")] ControlAxleCounterUp,
        [GetString("Control_CZSK Axle Counter -")] ControlAxleCounterDown,
        [GetString("Control_CZSK Axle Counter Confirmer")] ControlAxleCounterConfirmer,
        [GetString("Control_CZSK Axle Counter Restricted speed zone active button")] ControlAxleCounterRestrictedSpeedZoneActive,
        [GetString("Control_CZSK Horn 2")] ControlHorn2,
        [GetString("Control_CZSK Horn 1 + 2")] ControlHorn12,

        [GetString("Set Power Supply Station Location")] ControlPowerStationLocation,
        [GetString("Set Voltage 25k")] ControlSetVoltage25k,
        [GetString("Set Voltage 15k")] ControlSetVoltage15k,
        [GetString("Set Voltage 3k")] ControlSetVoltage3k,
        [GetString("Set Voltage 0")] ControlSetVoltage0,
        [GetString("Set Delete Voltage Marker")] ControlDeleteVoltageMarker,

        [GetString("Control_CZSK Reload World Object")] ControlRefreshWorld,
        [GetString("Control_CZSK Reload Cab Object")] ControlRefreshCab,
    }
}
