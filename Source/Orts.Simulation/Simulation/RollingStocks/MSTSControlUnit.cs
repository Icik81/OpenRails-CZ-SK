using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.MultiPlayer;
using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System.Diagnostics;
using System.IO;
using Event = Orts.Common.Event;
using Orts.Simulation.RollingStocks;
using static Orts.Simulation.RollingStocks.MSTSControlUnit;


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

        public MSTSControlUnit(Simulator simulator, string wagFile) :
            base(simulator, wagFile)
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

        public override void Update(float elapsedClockSeconds)
        {
            ResetControlUnitParameters();
            foreach (var car in Train.Cars.Where(car => car is MSTSLocomotive))
            {
                if (AcceptCableSignals)
                {
                    if (car.PowerUnitWithControl && car is MSTSElectricLocomotive && car.AcceptCableSignals)
                    {
                        ControlUnitType = ControlUnitTypes.Electric;
                        var PU = car as MSTSElectricLocomotive;
                        
                        PowerSupply.CircuitBreaker = PU.PowerSupply.CircuitBreaker;
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

                        switch (SwitchingVoltageMode)
                        {
                            case 0:
                                VoltageDC = PantographVoltageV;
                                break;
                            case 2:
                                VoltageAC = PantographVoltageV;
                                break;
                        }

                        if (!PU.LocoReadyToGo)
                        {
                            PU.HVOn = HVOn; PU.HVOff = HVOff;
                            HVOn = false; HVOff = false;
                            LocoReadyToGo = false;
                        }

                        if (IsLeadLocomotive())
                        {
                            PU.Battery = Battery;
                            PU.BreakPowerButton = BreakPowerButton;
                        }

                        break;
                    }

                    if (car.PowerUnitWithControl && car is MSTSDieselLocomotive && car.AcceptCableSignals)
                    {
                        ControlUnitType = ControlUnitTypes.Diesel;
                        var PU = car as MSTSDieselLocomotive;

                        FakeDieselWaterTemperatureDeg = PU.FakeDieselWaterTemperatureDeg;
                        FakeDieselOilTemperatureDeg = PU.DieselEngines[0].FakeDieselOilTemperatureDeg;
                        RealRPM = PU.DieselEngines[0].RealRPM;

                        break;
                    }
                }
            }



            base.Update(elapsedClockSeconds);
        }        

        public void ResetControlUnitParameters()
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

                    case CABViewControlTypes.PANTOGRAPHS_4:
                    case CABViewControlTypes.PANTOGRAPHS_4C:
                    case CABViewControlTypes.PANTOGRAPH_4_SWITCH:
                        {
                            Pantograph4Enable = true;
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
            }
            #endregion Electric

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
                                if (PU.Pantographs.List.Count == 2)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                if (PU.Pantographs.List.Count == 4)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[3].State)));
                                if (PU.Pantographs.List.Count <= 2)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[2].State)));
                                if (PU.Pantographs.List.Count == 4)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[4].State)));
                            }
                            else
                            {
                                if (PU.Pantographs.List.Count == 2)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[2].State)));
                                if (PU.Pantographs.List.Count == 4)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[4].State)));
                                if (PU.Pantographs.List.Count <= 2)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[1].State)));
                                if (PU.Pantographs.List.Count == 4)
                                    status.AppendFormat("{0} ", Simulator.Catalog.GetParticularString("Pantograph", GetStringAttribute.GetPrettyName(PU.Pantographs[3].State)));
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
