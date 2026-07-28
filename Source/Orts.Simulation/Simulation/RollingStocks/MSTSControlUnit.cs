using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.MultiPlayer;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Orts.Simulation.RollingStocks.MSTSControlUnit;
using Event = Orts.Common.Event;


// Řídící jednotka pro dálkové řízení lokomotivy

namespace Orts.Simulation.RollingStocks
{
    public class MSTSControlUnit : MSTSLocomotive
    {
        public ScriptedElectricPowerSupply PowerSupply;

        // Icik        
        float PantographVoltageV;
        float VoltageAC;
        float VoltageDC;
        float preVoltageDC;
        bool LocoSwitchACDC;        
        float PreDataVoltageAC;
        float PreDataVoltageDC;
        float PreDataVoltage;
        bool UpdateTimeEnable;
        public float FakeDieselWaterTemperatureDeg;
        public float FakeDieselOilTemperatureDeg;
        public float RealRPM;
        public float FakeDieselWaterTemperatureDeg2;
        public float FakeDieselOilTemperatureDeg2;
        public float RealRPM2;

        public MSTSControlUnit(Simulator simulator, string wagFile, string wagFileBase) :
            base(simulator, wagFile, wagFileBase)
        {
            PowerSupply = new ScriptedElectricPowerSupply(this);
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {

                default:
                    base.Parse(lowercasetoken, stf);
                    break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void Copy(MSTSWagon copy)
        {
            base.Copy(copy);  // each derived level initializes its own variables

            // for example
            //CabSoundFileName = locoCopy.CabSoundFileName;
            //CVFFileName = locoCopy.CVFFileName;
            MSTSControlUnit locoCopy = (MSTSControlUnit)copy;

        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {

            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {

            base.Restore(inf);
        }

        public enum ControlUnitTypes
        {             
            Electric,
            Diesel
        }

        public ControlUnitTypes ControlUnitType;

        public override void Initialize()
        {            

            base.Initialize();
        }

        public TrainCar PowerControlUnit;
        bool MUCableOk;        

        public override void Update(float elapsedClockSeconds)
        {            
            ResetControlUnitParameters();
            MUCableOk = true;
            Battery = false;
            Simulator.ControlUnitIsLead = false;
            PowerControlUnit = null;

            // AI
            if (!IsPlayerTrain && !Battery)
            {
                Battery = true;
                MUCableOk = true;
                foreach (var car in Train.Cars.Where(car => car is MSTSLocomotive))
                {
                    car.AcceptCableSignals = true;
                }
            }

            foreach (var car in Train.Cars.Where(car => car is MSTSLocomotive))
            {
                // Kontrola zapojeného kabelu MU
                if ((car.PowerUnitWithControl && !car.AcceptCableSignals) || !AcceptCableSignals)
                {
                    if (LocoSetUpTimer > 0.1f)
                        MUCableOk = false;
                }                

                // Hnací vozidlo je obsazené
                if (car.PowerUnitWithControl && !(car as MSTSLocomotive).LocoReadyToGo)
                {
                    Battery = MUCableOk && (car as MSTSLocomotive).Battery ? true : false;
                }

                if (wasRestored && Battery)
                {
                    SplashScreen = false;
                }

                #region LocoReadyToGo
                if (LocoReadyToGo && BrakeSystem.IsAirFull)
                {
                    SplashScreen = false;

                    if (Compressor5)
                        CompressorSwitch[LocoStation] = 2;
                    if (CompressorCombined)
                        CompressorSwitch[LocoStation] = 2;
                    if (CompressorCombined2)
                        CompressorSwitch2[LocoStation] = 1;


                    if (CompressorOffAutoOn)
                    {
                        if (CompressorAutoOffOn)
                            CompressorSwitch[LocoStation] = -1;
                        else
                            CompressorSwitch[LocoStation] = 1;
                    }
                    if (CompressorOffAutoOn2)
                    {
                        if (CompressorAutoOffOn2)
                            CompressorSwitch2[LocoStation] = -1;
                        else
                            CompressorSwitch2[LocoStation] = 1;
                    }

                    CompressorMode_OffAuto[LocoStation] = true;
                    CompressorMode2_OffAuto[LocoStation] = true;
                    if (!Compressor5 && !CompressorCombined && !CompressorCombined2 && !CompressorOffAutoOn && !CompressorOffAutoOn2)
                    {
                        CompressorMode_OffAuto[LocoStation] = true;
                        CompressorMode2_OffAuto[LocoStation] = true;
                    }
                    else
                    {
                        if (!Compressor5 && !CompressorCombined && !CompressorOffAutoOn)
                            CompressorMode_OffAuto[LocoStation] = false;
                        if (!Compressor5 && !CompressorCombined2 && !CompressorOffAutoOn2)
                            CompressorMode2_OffAuto[LocoStation] = false;
                    }

                    if (Pantograph4Enable)
                        Pantograph4Switch[LocoStation] = 1;

                    HV4Switch[LocoStation] = 1;
                    Battery = true;                    
                }
                #endregion LocoReadyToGo
                                
                if (MUCableOk)
                {
                    if (car.PowerUnitWithControl && car is MSTSElectricLocomotive)
                    {
                        ControlUnitType = ControlUnitTypes.Electric;
                        var PU = car as MSTSElectricLocomotive;
                        PowerControlUnit = PU;

                        if (!PU.LocoReadyToGo) LocoReadyToGo = false;

                        Pantographs = PU.Pantographs;
                        PantographUp = PU.PantographUp;
                        PantographDown = PU.PantographDown;
                        PowerSupply.CircuitBreaker = PU.PowerSupply.CircuitBreaker;
                        CircuitBreakerOn = PU.CircuitBreakerOn;
                        DriveForceN = PU.DriveForceN;                        
                        MaxCurrentA = PU.MaxCurrentA;
                        MaxForceN = PU.MaxForceN;
                        DynamicBrakeMaxCurrentA = PU.DynamicBrakeMaxCurrentA;
                        DynamicBrakeForceN = PU.DynamicBrakeForceN;
                        MaxDynamicBrakeForceN = PU.MaxDynamicBrakeForceN;
                        DynamicBrakeAvailable = PU.DynamicBrakeAvailable;
                        FakePowerCurrent1 = PU.FakePowerCurrent1;
                        BrakeCurrent1 = PU.BrakeCurrent1;
                        FakePowerCurrent2 = PU.FakePowerCurrent2;
                        BrakeCurrent2 = PU.BrakeCurrent2;
                        PantographVoltageV = PU.PantographVoltageV;
                        PowerSupply.PantographVoltageV = PU.PowerSupply.PantographVoltageV;
                        VoltageAC = PU.VoltageAC;
                        VoltageDC = PU.VoltageDC;
                        preVoltageDC = PU.preVoltageDC;
                        LocoSwitchACDC = PU.LocoSwitchACDC;
                        SwitchingVoltageMode = PU.SwitchingVoltageMode;
                        PowerOn = PU.PowerOn;
                        AuxPowerOn = PU.AuxPowerOn;                        
                        PantoCanHVOffon = PU.PantoCanHVOffon;
                        SwitchingVoltageMode_OffAC = PU.SwitchingVoltageMode_OffAC;
                        SwitchingVoltageMode_OffDC = PU.SwitchingVoltageMode_OffDC;
                        AuxResPressurePSI = PU.AuxResPressurePSI;
                        PantographsCurrent = PU.PantographsCurrent;
                        Simulator.AlternatorOverloadCoef = PU.AlternatorOverloadCoef;
                        Simulator.HeatingOverloadCoef = PU.HeatingOverloadCoef;                                                

                        //Simulator.Confirmer.MSG("Proud sberace PU: " + PantographsCurrent);                        

                        if (LocomotiveFaultyActivated)
                        {
                            PU.HVOff = true;
                        }                        

                        if (LocoType == LocoTypes.Vectron)
                        {
                            // Řídící jednotka není obsazená - přijímá signály z PU
                            if (!IsLeadLocomotive())
                            {
                                SelectingPowerSystem = PU.SelectingPowerSystem;
                                SelectedPowerSystem = PU.SelectedPowerSystem;
                                SystemAnnunciator = PU.SystemAnnunciator;
                                Switch5LightPosition[LocoStation] = PU.Switch5LightPosition[PU.LocoStation];
                                Switch6LightPosition[LocoStation] = PU.Switch6LightPosition[PU.LocoStation];                                                               
                            }
                            
                            // Řídící jednotka je obsazená - přijímá signály z PU a posílá signály do PU
                            if (IsLeadLocomotive())
                            {
                                PU.Switch51LightEnable = Switch51LightEnable;
                                PU.Switch5LightPosition[PU.LocoStation] = Switch5LightPosition[LocoStation];
                                PU.Switch52LightEnable = Switch52LightEnable;
                                PU.Switch6LightPosition[PU.LocoStation] = Switch6LightPosition[LocoStation];
                                SystemAnnunciator = PU.SystemAnnunciator;
                                if (PU.LocoReadyToGo)
                                {
                                    SelectingPowerSystem = PU.SelectingPowerSystem;
                                    SelectedPowerSystem = PU.SelectedPowerSystem;
                                }
                                else
                                {                                    
                                    PU.SelectingPowerSystem = SelectingPowerSystem;
                                    SelectedPowerSystem = PU.SelectedPowerSystem;
                                    PU.ChangePowerSystem();
                                }                                
                                GeneratoricModeActive = PU.GeneratoricModeActive;
                                PU.ForceHandleValue = ForceHandleValue;
                                TractionBlocked = PU.TractionBlocked;
                                InverterTest = PU.InverterTest;
                            }
                        }

                        // Světla
                        if (IsLeadLocomotive())
                        {                            
                            PU.LightFrontLR = LightFrontLR;
                            PU.LightFrontRR = LightFrontRR;
                            PU.LightFrontLW = LightFrontLW;
                            PU.LightFrontRW = LightFrontRW;
                            PU.LightRearLR = LightRearLR;
                            PU.LightRearRR = LightRearRR;
                            PU.LightRearLW = LightRearLW;
                            PU.LightRearRW = LightRearRW;
                        }

                        // Řídící jednotka je obsazená
                        if (IsLeadLocomotive() && !PU.LocoReadyToGo)
                        {                           
                            LocoReadyToGo = false;                            
                            if (StationIsActivated[LocoStation])
                                Simulator.ControlUnitIsLead = true;

                            DynamicBrakeIntervention = PU.DynamicBrakeIntervention;
                            if (DynamicBrakeIntervention > 0 && DynamicBrakeController != null)
                                DynamicBrakeIntervention = MathHelper.Max(PU.DynamicBrakeIntervention, DynamicBrakeController.CurrentValue);

                            PU.StationIsActivated[PU.LocoStation] = StationIsActivated[LocoStation];
                            PU.PowerKey = PowerKey;
                            PU.UpdateTimeEnable = UpdateTimeEnable;
                            PU.Sander = Sander;

                            PU.HVOn = HVOn; PU.HVOff = HVOff;
                            HVOn = false; HVOff = false;                            
                            PU.BreakPowerButton = BreakPowerButton;
                            if (AuxCompressor) PU.AuxCompressor = true;

                            PU.AuxCompressorMode_OffOn[PU.LocoStation] = AuxCompressorMode_OffOn[LocoStation];
                            PU.CompressorMode_OffAuto[PU.LocoStation] = CompressorMode_OffAuto[LocoStation];
                            PU.Compressor_I_HandMode[PU.LocoStation] = Compressor_I_HandMode[LocoStation];                            
                            PU.CompressorMode2_OffAuto[PU.LocoStation] = CompressorMode2_OffAuto[LocoStation];
                            PU.Compressor_II_HandMode[PU.LocoStation] = Compressor_II_HandMode[LocoStation];                            
                        }                        

                        break;
                    }
                    

                    if (car.PowerUnitWithControl && car is MSTSDieselLocomotive)
                    {
                        ControlUnitType = ControlUnitTypes.Diesel;
                        var PU = car as MSTSDieselLocomotive;
                        PowerControlUnit = PU;

                        if (!PU.LocoReadyToGo) LocoReadyToGo = false;

                        DriveForceN = PU.DriveForceN;
                        MaxCurrentA = PU.MaxCurrentA;
                        MaxForceN = PU.MaxForceN;
                        DynamicBrakeMaxCurrentA = PU.DynamicBrakeMaxCurrentA;
                        DynamicBrakeForceN = PU.DynamicBrakeForceN;
                        MaxDynamicBrakeForceN = PU.MaxDynamicBrakeForceN;
                        DynamicBrakeAvailable = PU.DynamicBrakeAvailable;
                        FakePowerCurrent1 = PU.FakePowerCurrent1;
                        BrakeCurrent1 = PU.BrakeCurrent1;
                        FakePowerCurrent2 = PU.FakePowerCurrent2;
                        BrakeCurrent2 = PU.BrakeCurrent2;                                                
                        PowerOn = PU.PowerOn;
                        AuxPowerOn = PU.AuxPowerOn;                        
                        AuxResPressurePSI = PU.AuxResPressurePSI;
                        Simulator.AlternatorOverloadCoef = PU.AlternatorOverloadCoef;
                        Simulator.HeatingOverloadCoef = PU.HeatingOverloadCoef;

                        FakeDieselWaterTemperatureDeg = PU.DieselEngines[0].FakeDieselWaterTemperatureDeg;
                        FakeDieselOilTemperatureDeg = PU.DieselEngines[0].FakeDieselOilTemperatureDeg;
                        RealRPM = PU.DieselEngines[0].RealRPM;                                               

                        if (PU.DieselEngines.Count > 1)
                        {
                            FakeDieselWaterTemperatureDeg2 = PU.DieselEngines[1].FakeDieselWaterTemperatureDeg;
                            FakeDieselOilTemperatureDeg2 = PU.DieselEngines[1].FakeDieselOilTemperatureDeg;
                            RealRPM2 = PU.DieselEngines[1].RealRPM;
                        }

                        if (LocomotiveFaultyActivated)
                        {
                            PU.Battery = false;
                        }

                        // Světla
                        if (IsLeadLocomotive())
                        {
                            PU.LightFrontLR = LightFrontLR;
                            PU.LightFrontRR = LightFrontRR;
                            PU.LightFrontLW = LightFrontLW;
                            PU.LightFrontRW = LightFrontRW;
                            PU.LightRearLR = LightRearLR;
                            PU.LightRearRR = LightRearRR;
                            PU.LightRearLW = LightRearLW;
                            PU.LightRearRW = LightRearRW;
                        }

                        // Řídící jednotka je obsazená - inicializace
                        if (IsLeadLocomotive() && !PU.LocoReadyToGo)
                        {
                            LocoReadyToGo = false;
                            if (StationIsActivated[LocoStation])
                                Simulator.ControlUnitIsLead = true;

                            DynamicBrakeIntervention = PU.DynamicBrakeIntervention;
                            if (DynamicBrakeIntervention > 0 && DynamicBrakeController != null)                                                                                                                             
                                DynamicBrakeIntervention = MathHelper.Max(PU.DynamicBrakeIntervention, DynamicBrakeController.CurrentValue);                                                                                                

                            PU.StationIsActivated[PU.LocoStation] = StationIsActivated[LocoStation];
                            PU.PowerKey = PowerKey;
                            PU.Sander = Sander;

                            PU.BreakPowerButton = BreakPowerButton;
                            if (AuxCompressor) PU.AuxCompressor = true;

                            PU.StartButtonPressed = StartButtonPressed;
                            PU.StartButtonPressed2 = StartButtonPressed2;
                            PU.StopButtonPressed = StopButtonPressed;
                            PU.StopButtonPressed2 = StopButtonPressed2;
                            PU.DieselDirection_Start = DieselDirection_Start;
                            PU.SwitchEngineEnable = SwitchEngineEnable;
                            PU.SwitchEnginePosition[PU.LocoStation] = SwitchEnginePosition[LocoStation];

                            PU.AuxCompressorMode_OffOn[PU.LocoStation] = AuxCompressorMode_OffOn[LocoStation];
                            PU.CompressorMode_OffAuto[PU.LocoStation] = CompressorMode_OffAuto[LocoStation];
                            PU.Compressor_I_HandMode[PU.LocoStation] = Compressor_I_HandMode[LocoStation];
                            PU.CompressorMode2_OffAuto[PU.LocoStation] = CompressorMode2_OffAuto[LocoStation];
                            PU.Compressor_II_HandMode[PU.LocoStation] = Compressor_II_HandMode[LocoStation];                                                                                   
                        }

                        break;
                    }
                                        
                }                
            }
            

            base.Update(elapsedClockSeconds);
        }        

        public void ResetControlUnitParameters()
        {
            if (IsPlayerTrain)
            {
                DriveForceN = 0;
                DynamicBrakeForceN = 0;
                FakePowerCurrent1 = 0;
                BrakeCurrent1 = 0;
                FakePowerCurrent2 = 0;
                BrakeCurrent2 = 0;
                PantographVoltageV = 0;
                PowerSupply.PantographVoltageV = 0;
                VoltageAC = 0;
                VoltageDC = 0;
                preVoltageDC = 0;
                SwitchingVoltageMode = 0;
                PowerOn = false;
                AuxPowerOn = false;
                PantoCanHVOffon = false;
                SwitchingVoltageMode_OffAC = false;
                SwitchingVoltageMode_OffDC = false;                
            }
        }

        public override float GetDataOf(CabViewControl cvc)
        {            
            float data = 0;

            #region Electric
            if (ControlUnitType == ControlUnitTypes.Electric && PowerSupply.CircuitBreaker != null)
            {
                switch (cvc.ControlType)
                {                    
                    case CABViewControlTypes.LINE_VOLTAGE:
                        if (cvc.UpdateTime != 0)
                            UpdateTimeEnable = true;
                        else
                            UpdateTimeEnable = false;
                        cvc.ElapsedTime += elapsedTime;
                        if (cvc.ElapsedTime > cvc.UpdateTime)
                        {
                            data = PantographVoltageV;
                            cvc.ElapsedTime = 0;
                            PreDataVoltage = data;
                        }
                        else
                            data = PreDataVoltage;
                        if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                            data /= 1000;
                        break;

                    case CABViewControlTypes.PANTO_DISPLAY:
                        data = Pantographs.State == PantographState.Up ? 1 : 0;
                        break;

                    case CABViewControlTypes.PANTOGRAPH:
                        data = Pantographs[UsingRearCab && Pantographs.List.Count > 1 ? 2 : 1].CommandUp ? 1 : 0;
                        break;

                    case CABViewControlTypes.PANTOGRAPH2:
                        data = Pantographs[UsingRearCab ? 1 : 2].CommandUp ? 1 : 0;
                        break;

                    case CABViewControlTypes.ORTS_PANTOGRAPH3:
                        data = Pantographs.List.Count > 2 && Pantographs[UsingRearCab && Pantographs.List.Count > 3 ? 4 : 3].CommandUp ? 1 : 0;
                        break;

                    case CABViewControlTypes.ORTS_PANTOGRAPH4:
                        data = Pantographs.List.Count > 3 && Pantographs[UsingRearCab ? 3 : 4].CommandUp ? 1 : 0;
                        break;

                    case CABViewControlTypes.PANTOGRAPHS_5:
                        if (Pantographs[1].CommandUp && Pantographs[2].CommandUp)
                            data = 0; // TODO: Should be 0 if the previous state was Pan2Up, and 4 if that was Pan1Up
                        else if (Pantographs[2].CommandUp)
                            data = 1;
                        else if (Pantographs[1].CommandUp)
                            data = 3;
                        else
                            data = 2;
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                        data = PowerSupply.CircuitBreaker.DriverClosingOrder ? 1 : 0;
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER:
                        data = PowerSupply.CircuitBreaker.DriverOpeningOrder ? 1 : 0;
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION:
                        data = PowerSupply.CircuitBreaker.DriverClosingAuthorization ? 1 : 0;
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE:
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                                data = 0;
                                break;
                            case CircuitBreakerState.Closing:
                                data = 1;
                                break;
                            case CircuitBreakerState.Closed:
                                data = 2;
                                break;
                        }
                        if (!PowerOn)                        
                            data = 0;                                                    

                        if (PantoCanHVOffon)
                            data = 0;
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED:
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                            case CircuitBreakerState.Closing:
                                data = 0;
                                break;
                            case CircuitBreakerState.Closed:
                                data = 1;
                                break;
                        }
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN:
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                            case CircuitBreakerState.Closing:
                                data = 1;
                                break;
                            case CircuitBreakerState.Closed:
                                data = 0;
                                break;
                        }
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_AUTHORIZED:
                        data = PowerSupply.CircuitBreaker.ClosingAuthorization ? 1 : 0;
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AND_AUTHORIZED:
                        data = (PowerSupply.CircuitBreaker.State < CircuitBreakerState.Closed && PowerSupply.CircuitBreaker.ClosingAuthorization) ? 1 : 0;
                        break;

                    // Icik
                    case CABViewControlTypes.SWITCHINGVOLTAGEMODE_OFF_DC:
                        {
                            SwitchingVoltageMode = MathHelper.Clamp(SwitchingVoltageMode, 0, 1);
                            data = SwitchingVoltageMode;
                            break;
                        }

                    case CABViewControlTypes.SWITCHINGVOLTAGEMODE_OFF_AC:
                        {
                            SwitchingVoltageMode = MathHelper.Clamp(SwitchingVoltageMode, 1, 2);
                            data = SwitchingVoltageMode;
                            break;
                        }

                    case CABViewControlTypes.SWITCHINGVOLTAGEMODE_DC_OFF_AC:
                        {
                            if (preVoltageDC > 500 && preVoltageDC < 4000)
                                data = 0;
                            else
                            if (VoltageAC > 5000)
                                data = 2;
                            else
                                data = 1;

                            if (PantographVoltageV == 1 && preVoltageDC == 1)
                                data = 1;
                            if (PantoCanHVOffon)
                                data = 1;
                            break;
                        }

                    case CABViewControlTypes.PANTOGRAPH_3_SWITCH:
                        {
                            Pantograph3Enable = true;
                            switch (Pantograph3Switch[LocoStation])
                            {
                                case -1:
                                    data = 0;
                                    break;
                                case 0:
                                    data = 1;
                                    break;
                                case 1:
                                    data = 2;
                                    break;
                                case 2:
                                    data = 3;
                                    break;
                            }
                            break;
                        }

                    case CABViewControlTypes.PANTOGRAPH_3_SWITCH_SIMPLE:
                        {
                            Pantograph3Enable = true;
                            Pantograph3Enable_Simple = true;
                            switch (Pantograph3Switch[LocoStation])
                            {                                
                                case 0:
                                    data = 0;
                                    break;
                                case 1:
                                    data = 1;
                                    break;
                                case 2:
                                    data = 2;
                                    break;
                            }
                            break;
                        }

                    case CABViewControlTypes.PANTOGRAPHS_4:
                    case CABViewControlTypes.PANTOGRAPHS_4C:
                    case CABViewControlTypes.PANTOGRAPH_4_SWITCH:
                        {
                            Pantograph4Enable = true;
                            data = Pantograph4Switch[LocoStation];
                            break;
                        }
                    case CABViewControlTypes.PANTOGRAPH_4NC_SWITCH:
                        {
                            Pantograph4NCEnable = true;
                            data = Pantograph4Switch[LocoStation];
                            break;
                        }

                    case CABViewControlTypes.PANTOGRAPH_5_SWITCH:
                        {
                            Pantograph5Enable = true;
                            switch (Pantograph5Switch[LocoStation])
                            {
                                case -2:
                                    data = 0;
                                    break;
                                case -1:
                                    data = 1;
                                    break;
                                case 0:
                                    data = 2;
                                    break;
                                case 1:
                                    data = 3;
                                    break;
                                case 2:
                                    data = 4;
                                    break;
                            }
                            break;
                        }
                    
                    case CABViewControlTypes.HV2:
                        {
                            HV2Enable = true;
                            data = HV2Switch;
                            break;
                        }

                    case CABViewControlTypes.HV2BUTTON:
                        {
                            HV2Enable = true;
                            HV2ButtonEnable = true;
                            data = HV2Switch;
                            break;
                        }

                    case CABViewControlTypes.HV3:
                        {
                            HV3Enable = true;
                            data = HV3Switch[LocoStation];
                            LocoSwitchACDC = true;
                            break;
                        }

                    case CABViewControlTypes.HV4:
                        {
                            HV4Enable = true;
                            Pantograph3Enable = true;
                            LocoSwitchACDC = true;
                            switch (HV4Switch[LocoStation])
                            {
                                case -1:
                                    data = 0;
                                    break;
                                case 0:
                                    data = 1;
                                    break;
                                case 1:
                                    data = 2;
                                    break;
                                case 2:
                                    data = 3;
                                    break;
                            }
                            break;
                        }

                    case CABViewControlTypes.HV5:
                        {
                            HV5Enable = true;
                            data = HV5Switch[LocoStation];
                            LocoSwitchACDC = true;
                            break;
                        }

                    case CABViewControlTypes.HV5_DISPLAY:
                        {
                            data = HV5Switch[LocoStation];
                            if (PantoCanHVOffon)
                                data = 2;
                            break;
                        }

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE_MULTISYSTEM:
                        switch (PowerSupply.CircuitBreaker.State)
                        {
                            case CircuitBreakerState.Open:
                                if (SwitchingVoltageMode == 1) // Střed                          
                                    data = 0;
                                if (SwitchingVoltageMode == 0) // levá strana - DC
                                    data = 6;
                                if (SwitchingVoltageMode == 2) // pravá strana - AC
                                    data = 2;
                                break;

                            case CircuitBreakerState.Closing:
                                if (SwitchingVoltageMode_OffAC)
                                    data = 1;
                                if (SwitchingVoltageMode_OffDC)
                                    data = 5;
                                break;

                            case CircuitBreakerState.Closed:
                                if (SwitchingVoltageMode_OffAC)
                                    data = 2;
                                if (SwitchingVoltageMode_OffDC)
                                    data = 6;
                                break;
                        }
                        LocoSwitchACDC = true;
                        break;

                    case CABViewControlTypes.LINE_VOLTAGE15kV_AC:
                        if (LocoType == LocoTypes.Vectron && !Loco15kV)
                            break;

                        if (cvc.UpdateTime != 0)
                            UpdateTimeEnable = true;
                        else
                            UpdateTimeEnable = false;
                        cvc.ElapsedTime += elapsedTime;
                        if (cvc.ElapsedTime > cvc.UpdateTime)
                        {
                            data = VoltageAC;
                            cvc.ElapsedTime = 0;
                            PreDataVoltageAC = data;
                        }
                        else
                            data = PreDataVoltageAC;
                        if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                            data /= 1000;
                        break;

                    case CABViewControlTypes.LINE_VOLTAGE_AC:
                        if (LocoType == LocoTypes.Vectron && Loco15kV)
                            break;

                        if (cvc.UpdateTime != 0)
                            UpdateTimeEnable = true;
                        else
                            UpdateTimeEnable = false;
                        cvc.ElapsedTime += elapsedTime;
                        if (cvc.ElapsedTime > cvc.UpdateTime)
                        {
                            data = VoltageAC;
                            cvc.ElapsedTime = 0;
                            PreDataVoltageAC = data;
                        }
                        else
                            data = PreDataVoltageAC;
                        if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                            data /= 1000;
                        break;

                    case CABViewControlTypes.LINE_VOLTAGE_DC:
                        if (cvc.UpdateTime != 0)
                            UpdateTimeEnable = true;
                        else
                            UpdateTimeEnable = false;
                        cvc.ElapsedTime += elapsedTime;
                        if (cvc.ElapsedTime > cvc.UpdateTime)
                        {
                            data = VoltageDC;
                            cvc.ElapsedTime = 0;
                            PreDataVoltageDC = data;
                        }
                        else
                            data = PreDataVoltageDC;
                        if (cvc.Units == CABViewControlUnits.KILOVOLTS)
                            data /= 1000;
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED_AC:
                        if (SwitchingVoltageMode_OffAC)
                        {
                            switch (PowerSupply.CircuitBreaker.State)
                            {
                                case CircuitBreakerState.Open:
                                case CircuitBreakerState.Closing:
                                    data = 0;
                                    break;
                                case CircuitBreakerState.Closed:
                                    data = 1;
                                    break;
                            }
                        }
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED_DC:
                        if (SwitchingVoltageMode_OffDC)
                        {
                            switch (PowerSupply.CircuitBreaker.State)
                            {
                                case CircuitBreakerState.Open:
                                case CircuitBreakerState.Closing:
                                    data = 0;
                                    break;
                                case CircuitBreakerState.Closed:
                                    data = 1;
                                    break;
                            }
                        }
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AC:
                        if (SwitchingVoltageMode_OffAC)
                        {
                            switch (PowerSupply.CircuitBreaker.State)
                            {
                                case CircuitBreakerState.Open:
                                case CircuitBreakerState.Closing:
                                    data = 1;
                                    break;
                                case CircuitBreakerState.Closed:
                                    data = 0;
                                    break;
                            }
                        }
                        break;

                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_DC:
                        if (SwitchingVoltageMode_OffDC)
                        {
                            switch (PowerSupply.CircuitBreaker.State)
                            {
                                case CircuitBreakerState.Open:
                                case CircuitBreakerState.Closing:
                                    data = 1;
                                    break;
                                case CircuitBreakerState.Closed:
                                    data = 0;
                                    break;
                            }
                        }
                        break;

                    case CABViewControlTypes.HV4PANTOUP:
                        {
                            foreach (var car in Train.Cars.Where(car => car is MSTSLocomotive))
                            {
                                if (AcceptCableSignals)
                                {
                                    if (car.PowerUnitWithControl && car is MSTSElectricLocomotive && car.AcceptCableSignals)
                                    {
                                        var PU = car as MSTSElectricLocomotive;
                                        if (PU.Pantographs[1].State != PantographState.Down || PU.Pantographs[2].State != PantographState.Down)
                                            data = 1;
                                        else
                                            data = 0;
                                    }
                                }
                            }
                        }
                        break;

                    case CABViewControlTypes.HV4VOLTAGESETUP:
                        {
                            data = 1;
                            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed)
                            {
                                if (SwitchingVoltageMode_OffDC)
                                    data = 0;
                                else
                                if (SwitchingVoltageMode_OffAC)
                                    data = 2;
                                else
                                    data = 1;
                            }
                        }
                        break;

                    case CABViewControlTypes.POWER_OFFCLOSINGON:
                        {
                            data = 1;
                            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Open || PowerReductionResult10 == 1)
                                data = 0;
                            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closing)
                                data = 1;
                            if (PowerSupply.CircuitBreaker.State == CircuitBreakerState.Closed && PowerReductionResult10 == 0 && AuxPowerOn)
                                data = 2;
                            if (PantoCanHVOffon)
                                data = 0;
                        }
                        break;

                    case CABViewControlTypes.HIGHVOLTAGE_DCOFFAC:
                        {
                            if (preVoltageDC > 500 && preVoltageDC < 4000 && AuxPowerOn)
                                data = 0;
                            else
                            if (VoltageAC > 5000 && AuxPowerOn)
                                data = 2;
                            else
                                data = 1;

                            if (PowerReductionResult10 == 1)
                                data = 1;
                        }
                        break;
                    
                    default:
                        data = base.GetDataOf(cvc);
                        break;
                }
                return data;
            }
            #endregion Electric

            data = base.GetDataOf(cvc);
            return data;
        }

        public override string GetStatus()
        {
            var status = new StringBuilder();

            #region Electric
            if (ControlUnitType == ControlUnitTypes.Electric)
            {                
                status.AppendFormat("{0} = ", Simulator.Catalog.GetString("Pantographs"));
                foreach (var car in Train.Cars.Where(car => car is MSTSLocomotive))
                {
                    if (car.PowerUnitWithControl && car is MSTSElectricLocomotive)
                    {
                        var PU = car as MSTSElectricLocomotive;
                        if (PU.AcceptCableSignals && AcceptCableSignals)
                        {
                            if (UsingRearCab)
                            {
                                if (PU.WagonRealPantoCount == 1)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                if (PU.WagonRealPantoCount == 2)
                                {
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[2].State)));
                                }
                                if (PU.WagonRealPantoCount == 4)
                                {
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[3].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[4].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[2].State)));
                                }
                            }
                            else
                            {
                                if (PU.WagonRealPantoCount == 1)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                if (PU.WagonRealPantoCount == 2)
                                {
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[2].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                }
                                if (PU.WagonRealPantoCount == 4)
                                {
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[2].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[4].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[3].State)));
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                }                                
                            }
                            status.AppendLine();

                            status.AppendFormat("{0} = {1}",
                                Simulator.Catalog.GetString("Circuit breaker"),
                                Simulator.Catalog.GetParticularString("CircuitBreaker", GetStringAttribute.GetPrettyName(PowerSupply.CircuitBreaker.State)));
                            status.AppendLine();
                            status.AppendFormat("{0} = {1}",
                                Simulator.Catalog.GetParticularString("PowerSupply", "Power"),
                                Simulator.Catalog.GetParticularString("PowerSupply", GetStringAttribute.GetPrettyName(PU.PowerSupply.State)));
                        }
                        status.AppendLine();
                        if (Battery)
                            status.AppendFormat("{0} = {1}",
                            Simulator.Catalog.GetString("Battery"),
                            Simulator.Catalog.GetParticularString("Battery", Simulator.Catalog.GetString("On")));
                        else
                            status.AppendFormat("{0} = {1}",
                            Simulator.Catalog.GetString("Battery"),
                            Simulator.Catalog.GetParticularString("Battery", Simulator.Catalog.GetString("Off")));

                        break;
                    }
                }
                status.AppendLine();
                if (PowerKeyPosition[LocoStation] == 0)
                    status.AppendFormat("{0} = {1}",
                    Simulator.Catalog.GetString("PowerKey"),
                    Simulator.Catalog.GetParticularString("PowerKey", Simulator.Catalog.GetString("No Powerkey")));
                else
                if (StationIsActivated[LocoStation])
                    status.AppendFormat("{0} = {1}",
                    Simulator.Catalog.GetString("PowerKey"),
                    Simulator.Catalog.GetParticularString("PowerKey", Simulator.Catalog.GetString("On")));
                else
                    status.AppendFormat("{0} = {1}",
                    Simulator.Catalog.GetString("PowerKey"),
                    Simulator.Catalog.GetParticularString("PowerKey", Simulator.Catalog.GetString("Off")));
            }
            #endregion Electric

            #region Diesel
            if (ControlUnitType == ControlUnitTypes.Diesel)
            {                
                foreach (var car in Train.Cars.Where(car => car is MSTSLocomotive))
                {
                    if (car.PowerUnitWithControl && car is MSTSDieselLocomotive)
                    {                        
                        status.AppendLine();
                        if (Battery)
                            status.AppendFormat("{0} = {1}",
                            Simulator.Catalog.GetString("Battery"),
                            Simulator.Catalog.GetParticularString("Battery", Simulator.Catalog.GetString("On")));
                        else
                            status.AppendFormat("{0} = {1}",
                            Simulator.Catalog.GetString("Battery"),
                            Simulator.Catalog.GetParticularString("Battery", Simulator.Catalog.GetString("Off")));

                        break;
                    }
                }
                status.AppendLine();
                if (PowerKeyPosition[LocoStation] == 0)
                    status.AppendFormat("{0} = {1}",
                    Simulator.Catalog.GetString("PowerKey"),
                    Simulator.Catalog.GetParticularString("PowerKey", Simulator.Catalog.GetString("No Powerkey")));
                else
                if (StationIsActivated[LocoStation])
                    status.AppendFormat("{0} = {1}",
                    Simulator.Catalog.GetString("PowerKey"),
                    Simulator.Catalog.GetParticularString("PowerKey", Simulator.Catalog.GetString("On")));
                else
                    status.AppendFormat("{0} = {1}",
                    Simulator.Catalog.GetString("PowerKey"),
                    Simulator.Catalog.GetParticularString("PowerKey", Simulator.Catalog.GetString("Off")));
            }
            #endregion Diesel

            return status.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder(base.GetDebugStatus());
            //status.AppendFormat("{0}\t", Simulator.Catalog.GetString("Control"));
            return status.ToString();
        }
    }
}
