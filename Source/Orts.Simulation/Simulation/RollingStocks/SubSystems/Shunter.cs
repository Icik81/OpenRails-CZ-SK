using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Simulation.Physics;
using Orts.Simulation.Simulation.Signalling;
using static Orts.Simulation.Simulation.RollingStocks.TrainCar;
using Event = Orts.Simulation.Common.Event;


// Řídící jednotka pro dálkové řízení lokomotivy

namespace Orts.Simulation.Simulation.RollingStocks.SubSystems
{
    public class Shunter
    {
        public Shunter(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
        }
        MSTSLocomotive Locomotive;


        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {

                default:                    
                    break;
            }
        }        

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
            //outf.Write();

        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public void Restore(BinaryReader inf)
        {
            // = inf.ReadBoolean();

        }

        float ShunterTimeWithOutRadio;
        bool ShunterSoundStartPlayed;
        bool ShunterSound250Played;
        bool ShunterSound200Played;
        bool ShunterSound150Played;
        bool ShunterSound100Played;
        bool ShunterSound80Played;
        bool ShunterSound50Played;
        bool ShunterSound30Played;
        bool ShunterSound20Played;
        bool ShunterSound15Played;
        bool ShunterSound10Played;
        bool ShunterSoundSlowPlayed;
        bool ShunterSound5Played;
        bool ShunterSound4Played;
        bool ShunterSound3Played;
        bool ShunterSound2Played;
        bool ShunterSound1Played;
        bool ShunterSoundSlowlyPlayed;
        bool ShunterSoundDonePlayed;
        bool ShunterSoundNearToSTPPlayed;
        bool ShunterSoundSlowToSTPPlayed;
        bool ShunterSoundReversePlayed;
        bool ShunterSoundSlowNearToSTPPlayed;
        bool ShunterSoundStopSTPPlayed;
        float ShunterTimer;
        bool ShunterSoundOff;
        float LastDistanceToOtherTrain;
        float LastDistanceToSTPTrain;
        float ShunterProcessTimer;
        float DistanceToOtherTrain_0 = -1000;
        float ShunterDecideProcessTimer;
        bool ShunterDecideProcess;
        string ShunterDecideMarker;
        float ShunterTimerRandom;
        float CheckDistance;
        string LastShunterDecideMarker = "";
        float TouchingDistanceTimer;

        bool ShunterCarTestBrakePhase1;
        bool ShunterCarTestBrakePhase2;
        float ShunterCarTestBrakePhase1Timer;
        float ShunterCarTestBrakePhase2Timer;
        float ShunterCarTestBrakePhase1CarCheckTimer;
        float ShunterCarTestBrakePhase2CarCheckTimer;        

        bool ShunterFullTestBrakePhase1;
        bool ShunterFullTestBrakePhase2;
        bool ShunterFullTestBrakePhase3;
        bool ShunterFullTestBrakePhase4;
        bool ShunterFullTestBrakePhase5;
        float ShunterFullTestBrakePhase1Timer;
        float ShunterFullTestBrakePhase2Timer;
        float ShunterFullTestBrakePhase3Timer;
        float ShunterFullTestBrakePhase4Timer;
        float ShunterFullTestBrakePhase5Timer;        
        float ShunterFullTestBrakePhase2CarCheckTimer;
        float ShunterFullTestBrakePhase4CarCheckTimer;
        bool ShunterSimpleTestBrakePhase1;
        bool ShunterSimpleTestBrakePhase2;
        bool ShunterSimpleTestBrakePhase3;
        bool ShunterSimpleTestBrakePhase4;
        bool ShunterSimpleTestBrakePhase5;
        float ShunterSimpleTestBrakePhase1Timer;
        float ShunterSimpleTestBrakePhase2Timer;
        float ShunterSimpleTestBrakePhase3Timer;
        float ShunterSimpleTestBrakePhase4Timer;
        float ShunterSimpleTestBrakePhase5Timer;        
        float ShunterSimpleTestBrakePhase2CarCheckTimer;
        float ShunterSimpleTestBrakePhase4CarCheckTimer;
        bool ShunterSimpleTestBrakeHandBrakeActivated;
        float ShunterSimpleTestBrakeHandBrakeTimer;
        float[] ShunterTestBrakeCarCheckTime = new float[100];        
        int CarNumber = 1;
        int LastCarConnectedNumber;
        TrainCar LastCarConnected;        
        TrainCar[] CheckWagonList = new TrainCar[100];        
        bool FirstBoggie;
        bool SecondBoggie;        
        bool[] CheckCarBrakeFault = new bool[100];
        TrainCar CheckCar = null;
        int CheckCarNumber = 0;                
        float ShunterCheckAirPressureTimer;
        bool CheckAuxResBrakeLine;
        float ShunterFullTestBrakePhase1TimeOffset;

        #region ShunterCheckAirPressureInCar
        public void ShunterCheckAirPressureInCar(float elapsedClockSeconds, TrainCar testCar)
        {            
            if (testCar.BrakeSystem.CarHasMaybeProblemWithBrake)
            {
                CheckAuxResBrakeLine = true;
                // Odvzdušní soustavu brzdy
                testCar.BrakeSystem.BleedOffValveOpen = true;                
                if (testCar.BrakeSystem.GetCylPressurePSI() > 0.01f * 14.50377f)
                {                                        
                    goto SkipAuxResPressureCheck;
                }
                else
                {                    
                    testCar.BrakeSystem.BleedOffValveOpen = false;
                    testCar.BrakeSystem.CarHasMaybeProblemWithBrake = false;
                }
            }

            // Opětovná zkouška brzdy vozu
            if (CheckAuxResBrakeLine)            
            {
                float TimeCoef = 2.0f;
                // Kontrola tlaku naplnění pomocné jímky
                if (testCar.BrakeSystem.AuxResPressurePSI < 0.99f * testCar.BrakeSystem.BrakeLine1PressurePSI || testCar.BrakeSystem.AuxResPressurePSI > 1.01f * testCar.BrakeSystem.BrakeLine1PressurePSI)
                {
                    ShunterCheckAirPressureTimer += elapsedClockSeconds * TimeCoef;                    
                    if (ShunterCheckAirPressureTimer > 60.0f)
                    {                        
                        ShunterCheckAirPressureTimer = 0;
                        testCar.BrakeSystem.CarHasReallyProblemWithBrake = true;
                        CheckAuxResBrakeLine = false;
                    }
                    if (ShunterCheckAirPressureTimer > 55.0f && testCar.BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f)
                    {
                        testCar.BrakeSystem.CarHasReallyProblemWithBrake = true;
                    }
                    if (testCar.BrakeSystem.BleedOffValveOpen)
                    {
                        testCar.BrakeSystem.BleedOffValveOpen = false;
                    }
                    if (testCar.BrakeSystem.BrakeCarDeactivate)
                    {
                        testCar.BrakeSystem.BrakeCarDeactivate = false;
                        testCar.BrakeSystem.BrakeCarDeactivateMenu = 0;
                    }
                }
                else
                {
                    if (!ShunterCarTestBrakePhase1 && !ShunterCarTestBrakePhase2)
                    {
                        testCar.BrakeSystem.CarHasMaybeProblemWithBrake = false;

                        if (ShunterCarTestBrakePhase1Timer == 0)
                        {
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please apply the brakes under 4.5 bar and holdon!"));                            
                            if (Locomotive.Simulator.ShunterFullTestBrakeEnable)
                                Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_ApplyBrake_Again);
                            else
                                Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_ApplyBrake_Again);
                        }

                        ShunterCarTestBrakePhase1Timer += elapsedClockSeconds * TimeCoef;
                        if (ShunterCarTestBrakePhase1Timer > 10.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.5f * 14.50377f)
                        {
                            ShunterCarTestBrakePhase1Timer = 0.0f;                            
                        }
                        
                        if (ShunterCarTestBrakePhase1Timer > 3.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.5f * 14.50377f)
                        {
                            ShunterCarTestBrakePhase1Timer = 3.0f;
                            ShunterCarTestBrakePhase1 = true;
                            FirstBoggie = false;
                            SecondBoggie = false;
                        }                        
                    }

                    // Zabrzdit
                    if (ShunterCarTestBrakePhase1 && !ShunterCarTestBrakePhase2)
                    {
                        ShunterCarTestBrakePhase1CarCheckTimer += elapsedClockSeconds * TimeCoef;
                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f)
                        {
                            if (!FirstBoggie && ShunterCarTestBrakePhase1CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterCarTestBrakePhase1CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                SecondBoggie = true;
                            }
                        }
                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() <= 0.1f * 14.50377f)
                        {
                            if (!FirstBoggie && ShunterCarTestBrakePhase1CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterCarTestBrakePhase1CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                SecondBoggie = true;
                                testCar.BrakeSystem.CarHasReallyProblemWithBrake = true;
                            }
                        }

                        if (FirstBoggie && SecondBoggie && ShunterCarTestBrakePhase1CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] + 3.0f)
                        {
                            if (ShunterCarTestBrakePhase2Timer == 0)
                            {
                                Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please release the brakes and holdon!"));
                                if (Locomotive.Simulator.ShunterFullTestBrakeEnable)
                                    Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_ReleaseBrake_Again);
                                else
                                    Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_ReleaseBrake_Again);
                            }

                            ShunterCarTestBrakePhase2Timer += elapsedClockSeconds * TimeCoef;
                            if (ShunterCarTestBrakePhase2Timer > 10.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.5f * 14.50377f)
                            {
                                ShunterCarTestBrakePhase2Timer = 0.0f;
                            }
                            
                            if (ShunterCarTestBrakePhase2Timer > 3.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.9f * 14.50377f)
                            {
                                ShunterCarTestBrakePhase2Timer = 3.0f;
                                ShunterCarTestBrakePhase2 = true;
                                FirstBoggie = false;
                                SecondBoggie = false;
                            }
                        }
                    }

                    // Odbrzdit
                    if (ShunterCarTestBrakePhase2)
                    {
                        ShunterCarTestBrakePhase2CarCheckTimer += elapsedClockSeconds * TimeCoef;
                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f)
                        {
                            if (!FirstBoggie && ShunterCarTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterCarTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                SecondBoggie = true;
                                testCar.BrakeSystem.CarHasReallyProblemWithBrake = true;
                            }
                        }
                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() <= 0.1f * 14.50377f)
                        {
                            if (!FirstBoggie && ShunterCarTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterCarTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                SecondBoggie = true;
                            }
                        }

                        if (FirstBoggie && SecondBoggie && ShunterCarTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] + 3.0f)
                        {
                            if (testCar.BrakeSystem.GetCylPressurePSI() < 0.1f * 14.50377f)
                            {
                                testCar.BrakeSystem.CarHasReallyProblemWithBrake = false;
                            }
                            ShunterCarTestBrakePhase1 = false;
                            ShunterCarTestBrakePhase2 = false;
                            ShunterCarTestBrakePhase1Timer = 0;
                            ShunterCarTestBrakePhase2Timer = 0;
                            ShunterCarTestBrakePhase1CarCheckTimer = 0;
                            ShunterCarTestBrakePhase2CarCheckTimer = 0;                            
                            ShunterCheckAirPressureTimer = 0;
                            CheckAuxResBrakeLine = false;
                        }
                    }
                }
            }
            SkipAuxResPressureCheck: ;
        }
        #endregion ShunterCheckAirPressureInCar

        public void Update(float elapsedClockSeconds)
        {
            if (!Locomotive.IsLeadLocomotive()) return;

            // Plánované spuštění v aktivitě
            if (Locomotive.Simulator.ActivityRun != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.ShunterFullTestBrake)            
            {
                Locomotive.Simulator.ActivityRun.triggeredEventWrapper = null;
                Locomotive.Simulator.ShunterFullTestBrakeEnable = true;
            }
            if (Locomotive.Simulator.ActivityRun != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.ShunterSimpleTestBrake)
            {
                Locomotive.Simulator.ActivityRun.triggeredEventWrapper = null;
                Locomotive.Simulator.ShunterSimpleTestBrakeEnable = true;
            }
            if (Locomotive.Simulator.ActivityRun != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.Shunter)
            {
                Locomotive.Simulator.ActivityRun.triggeredEventWrapper = null;
                Locomotive.Simulator.ShunterEnable = !Locomotive.Simulator.ShunterEnable;
            }
            if (Locomotive.Simulator.ActivityRun != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper != null && Locomotive.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.AutomaticShunter)
            {
                Locomotive.Simulator.ActivityRun.triggeredEventWrapper = null;
                Locomotive.Simulator.AutomaticShunterEnable = !Locomotive.Simulator.AutomaticShunterEnable;
            }

            if (!Locomotive.Simulator.ShunterFullTestBrakeEnable && !Locomotive.Simulator.ShunterSimpleTestBrakeEnable)
            {                                
                CheckAuxResBrakeLine = false;
                TestCarReset();
                for (int i = 0; i < Locomotive.Train.Cars.Count; i++)
                {
                    CheckCarBrakeFault[i] = false;
                }
            }

            #region ShunterFullTestBrake
            if (Locomotive.Simulator.ShunterFullTestBrakeEnable && !Locomotive.Simulator.PlayerLocomotiveChange)
            {
                if (!Locomotive.Simulator.CabRadioOn)
                {
                    ShunterTimeWithOutRadio += elapsedClockSeconds;
                    if (ShunterTimeWithOutRadio > 30f) // Po 30 sekundách bez rádia se zobrazí hláška upozornění posunovačem, že by rádio mělo být zapnuté
                    {
                        ShunterTimeWithOutRadio = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Cab radio should be on!"));
                    }
                    return;
                }
                else
                    ShunterTimeWithOutRadio = 0;

                for (int i = 0; i < 100; i++)
                {
                    ShunterTestBrakeCarCheckTime[i] = 0;                    
                    CheckWagonList[i] = null;
                }

                LastCarConnectedNumber = 0;
                LastCarConnected = null;
                int CheckWagonListIndex = 0;
                int CheckLeadIndex = 0;
                int CheckCarIndex = 0;
                int ConnectedCarCount = 1;
                foreach (TrainCar car in Locomotive.Train.Cars)
                {
                    CheckCarIndex++;
                    if (car is MSTSLocomotive && (car as MSTSLocomotive).IsLeadLocomotive()) { CheckLeadIndex = CheckCarIndex; }
                    else
                    {
                        LastCarConnectedNumber++;
                        LastCarConnected = car;                                                
                    }                 
                }
                foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                {
                    CheckWagonListIndex++;
                    CheckWagonList[CheckWagonListIndex] = car;
                    
                    if (!car.CarHasBrakePipeConnected)
                    {
                        CheckCarBrakeFault[CheckWagonListIndex] = true;
                    }
                    else
                    {
                        ConnectedCarCount++;
                    }
                }             

                if (Locomotive.AbsSpeedMpS > 1.0f / 3.6f || Locomotive.Train.Cars.Count == 1 || LastCarConnected == null)
                {
                    ShunterFullTestBrakePhase1 = false;
                    ShunterFullTestBrakePhase2 = false;
                    ShunterFullTestBrakePhase3 = false;
                    ShunterFullTestBrakePhase4 = false;
                    ShunterFullTestBrakePhase5 = false;
                    ShunterFullTestBrakePhase1Timer = 0;
                    ShunterFullTestBrakePhase2Timer = 0;
                    ShunterFullTestBrakePhase3Timer = 0;
                    ShunterFullTestBrakePhase4Timer = 0;
                    ShunterFullTestBrakePhase5Timer = 0;
                    ShunterFullTestBrakePhase2CarCheckTimer = 0;
                    ShunterFullTestBrakePhase4CarCheckTimer = 0;
                    Locomotive.Simulator.FullTestBrakeWindow = false;
                    ShunterFullTestBrakePhase1TimeOffset = 0;
                    FirstBoggie = false;
                    SecondBoggie = false;
                    if (LastCarConnected == null) return;
                }

                if (!ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase5)
                {
                    if (Locomotive.AbsSpeedMpS > 1.0f / 3.6f || Locomotive.Train.Cars.Count < 2 || Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.9f * 14.50377f)
                    {
                        Locomotive.Simulator.ShunterFullTestBrakeEnable = false;

                        if (Locomotive.AbsSpeedMpS > 1.0f / 3.6f)
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Train speed must be zero to perform the test brake!"));

                        if (Locomotive.Train.Cars.Count < 2)
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Train must have at least two cars to perform the test brake!"));

                        if (Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.9f * 14.50377f)
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Train must have sufficient brakepipe pressure (5 bar) to perform the test brake!"));

                        return;
                    }

                    ShunterFullTestBrakePhase1 = true;
                    Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Full test brake activated. Please follow the instructions."));
                    Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_Start);                    
                }

                if (ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase5)
                {
                    if (ShunterFullTestBrakePhase1Timer > 9.5f && ShunterFullTestBrakePhase1Timer < 10.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please apply the brakes under 4.5 bar and holdon!"));
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_ApplyBrake);
                        ShunterFullTestBrakePhase1TimeOffset = 0;
                    }

                    if (ShunterFullTestBrakePhase1TimeOffset == 0)
                        ShunterFullTestBrakePhase1Timer += elapsedClockSeconds;

                    if (ShunterFullTestBrakePhase1Timer > 10.0f + 30.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.5f * 14.50377f)
                    {
                        ShunterFullTestBrakePhase1Timer = 10.0f;
                    }

                    if (ShunterFullTestBrakePhase1TimeOffset == 0 && !ShunterFullTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.5f * 14.50377f && (ShunterFullTestBrakePhase1Timer > Locomotive.Train.Cars.Count * 5.0f || CheckWagonList[CheckWagonListIndex].BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f))
                    {
                        ShunterFullTestBrakePhase1 = false;
                        ShunterFullTestBrakePhase2 = true;
                        ShunterFullTestBrakePhase2Timer = 0;
                        CarNumber = 1;
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_FirstSide);
                    }

                    // Pokud lokomotiva hráče nebude propojena s vozy, posunovač si ji napojí sám
                    float TrainBrakePipePressure = 0;
                    float TrainAuxPressure = 0;
                    if (ShunterFullTestBrakePhase1TimeOffset > 0)
                    {
                        for (int i = 0; i < Locomotive.Train.Cars.Count; i++)
                        {
                            var car = Locomotive.Train.Cars[i];
                            if (car.CarHasBrakePipeConnected)
                            {
                                TrainBrakePipePressure += car.BrakeSystem.BrakeLine1PressurePSI;
                                TrainAuxPressure += car.BrakeSystem.AuxResPressurePSI;
                            }
                        }
                        if (TrainBrakePipePressure / ConnectedCarCount > 4.9f * 14.50377f && TrainAuxPressure / ConnectedCarCount > 4.9f * 14.50377f)
                        {
                            ShunterFullTestBrakePhase1TimeOffset = 0;
                            ShunterFullTestBrakePhase1Timer = 10.0f;
                        }
                    }

                    TrainCar testCar = Locomotive.Simulator.PlayerLocomotive;                    
                    if (testCar == Locomotive.Train.FirstCar)
                    {
                        if (!testCar.BrakeSystem.AngleCockBOpen)
                        {
                            ShunterFullTestBrakePhase1TimeOffset = Locomotive.Train.Cars.Count * 10.0f;
                        }
                        testCar.BrakeSystem.AngleCockAOpen = false;
                        testCar.BrakeSystem.AngleCockBOpen = true;
                    }
                    else
                        if (testCar == Locomotive.Train.LastCar)
                        {
                            if (!testCar.BrakeSystem.AngleCockAOpen || !testCar.BrakeSystem.FrontBrakeHoseConnected)
                            {
                                ShunterFullTestBrakePhase1TimeOffset = Locomotive.Train.Cars.Count * 10.0f;
                            }
                            testCar.BrakeSystem.AngleCockAOpen = true;
                            testCar.BrakeSystem.AngleCockBOpen = false;
                            testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                        }
                        else
                        {
                            if (!testCar.BrakeSystem.AngleCockAOpen || !testCar.BrakeSystem.AngleCockBOpen || !testCar.BrakeSystem.FrontBrakeHoseConnected)
                            {
                                ShunterFullTestBrakePhase1TimeOffset = Locomotive.Train.Cars.Count * 10.0f;
                            }
                            testCar.BrakeSystem.AngleCockAOpen = true;
                            testCar.BrakeSystem.AngleCockBOpen = true;
                            testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                        }
                }

                if (ShunterFullTestBrakePhase1TimeOffset == 0 && ShunterFullTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.8f * 14.50377f)
                {
                    ShunterFullTestBrakePhase2 = false;
                }                

                // Požadavek pro fázi 2: Kontrola aplikace brzd od prvního vozu k poslednímu
                if (ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase5)
                {
                    float ShunterFullTestBrakePhase2Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                    {
                        CarTimeNumber++;
                        float CarBrakeCheckTime = (((car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake) && !car.BrakeSystem.BrakeCarDeactivate) || car.BrakeSystem.HandBrakeActive) ? Simulator.Random.Next(6, 12) : Simulator.Random.Next(2, 6) + (car.CarLengthM / 2.0f);
                        ShunterFullTestBrakePhase2Time += CarBrakeCheckTime;
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = CarBrakeCheckTime;
                    }
                    
                    ShunterFullTestBrakePhase2Timer += elapsedClockSeconds;                    
                    if (ShunterFullTestBrakePhase2Timer > 5.0f)
                    {
                        ShunterFullTestBrakePhase2CarCheckTimer += elapsedClockSeconds;
                    }

                    TrainCar testCar = CheckWagonList[CarNumber];
                    if (ShunterFullTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber <= CarTimeNumber)
                    {                        
                        if (testCar.BrakesStuck || testCar.BrakeSystem.CarHasProblemWithBrake || testCar.BrakeSystem.CarHasReallyProblemWithBrake)
                        {
                            testCar.BrakeSystem.BrakeCarDeactivate = true;
                            testCar.BrakeSystem.BrakeCarDeactivateMenu = 1;
                            testCar.BrakeSystem.BleedOffValveOpen = true;
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_BrakeDeactivate);
                        }                        
                        if (testCar.BrakeSystem.HandBrakeActive)
                        {
                            testCar.BrakeSystem.HandBrakeDeactive = true;
                            testCar.BrakeSystem.HandBrakeActive = false;
                            testCar.BrakeSystem.SetHandbrakePercent(0);
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_HandBrakeRelease);
                        }
                        if (testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = false;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }                        

                        if (testCar == Locomotive.Train.FirstCar)
                        {
                            testCar.BrakeSystem.AngleCockAOpen = false;
                            testCar.BrakeSystem.AngleCockBOpen = true;
                        }
                        else
                            if (testCar == Locomotive.Train.LastCar)
                            {
                                testCar.BrakeSystem.AngleCockAOpen = true;
                                testCar.BrakeSystem.AngleCockBOpen = false;
                                testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                            }
                            else                                
                                {
                                    testCar.BrakeSystem.AngleCockAOpen = true;
                                    testCar.BrakeSystem.AngleCockBOpen = true;
                                    testCar.BrakeSystem.FrontBrakeHoseConnected = true;                                    
                                }

                        ShunterFullTestBrakePhase2CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber++;
                        FirstBoggie = false;
                        SecondBoggie = false;
                        SetCarMode(testCar);
                    }
                    else
                    { 
                        if (testCar != null && !testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = true;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;                            
                        }
                        if (testCar == null)
                        {
                            TestCarReset();
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }

                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f)
                        {
                            if (!FirstBoggie && ShunterFullTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterFullTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                SecondBoggie = true;
                            }
                        }
                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() <= 0.1f * 14.50377f)
                        {                            
                            if (!FirstBoggie && ShunterFullTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterFullTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                SecondBoggie = true;
                                testCar.BrakeSystem.CarHasMaybeProblemWithBrake = true;
                            }
                        }
                    }

                    if (ShunterFullTestBrakePhase2Timer > ShunterFullTestBrakePhase2Time + 5.0f)
                    {
                        ShunterFullTestBrakePhase2 = false;
                        ShunterFullTestBrakePhase2Timer = 0;
                        ShunterFullTestBrakePhase2CarCheckTimer = 0;
                        CarNumber = CarTimeNumber;
                        ShunterFullTestBrakePhase3 = true;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Full test brake first side completed!"));
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_FirstSideDone);
                    }
                }

                // Požadavek pro fázi 3: Uvolnit brzdy nad 4.9 bar                                   
                if (ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase5)
                {
                    if (ShunterFullTestBrakePhase3Timer > 4.5f && ShunterFullTestBrakePhase3Timer < 5.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please release the brakes and holdon!"));
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_ReleaseBrake);
                    }

                    ShunterFullTestBrakePhase3Timer += elapsedClockSeconds;
                    if (ShunterFullTestBrakePhase3Timer > 5.0f + 30.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI <= 4.5f * 14.50377f)
                    {
                        ShunterFullTestBrakePhase3Timer = 5.0f;
                    }

                    if (!ShunterFullTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI > 4.9f * 14.50377f && (ShunterFullTestBrakePhase3Timer > Locomotive.Train.Cars.Count * 5.0f || CheckWagonList[CheckWagonListIndex].BrakeSystem.GetCylPressurePSI() < 0.1f * 14.50377f))
                    {
                        ShunterFullTestBrakePhase3 = false;
                        ShunterFullTestBrakePhase4 = true;
                        ShunterFullTestBrakePhase4Timer = 0;
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_SecondSide);
                    }
                }

                if (!CheckAuxResBrakeLine && ShunterFullTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI < 4.7f * 14.50377f)
                {
                    ShunterFullTestBrakePhase4 = false;
                    ShunterFullTestBrakePhase3 = true;
                }                

                // Požadavek pro fázi 4: Kontrola uvolnění brzd od posledního vozu k prvnímu
                if (ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase5)
                {
                    float ShunterFullTestBrakePhase4Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                    {
                        CarTimeNumber++;
                        float CarBrakeCheckTime = (((car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake) && !car.BrakeSystem.BrakeCarDeactivate) || car.BrakeSystem.HandBrakeActive) ? Simulator.Random.Next(6, 12) : Simulator.Random.Next(2, 6) + (car.CarLengthM / 2.0f);
                        ShunterFullTestBrakePhase4Time += CarBrakeCheckTime;
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = CarBrakeCheckTime;
                    }

                    if (!CheckAuxResBrakeLine)
                        ShunterFullTestBrakePhase4Timer += elapsedClockSeconds;                    

                    if (ShunterFullTestBrakePhase4Timer > 5.0f && !CheckAuxResBrakeLine)
                    {
                        ShunterFullTestBrakePhase4CarCheckTimer += elapsedClockSeconds;
                    }

                    TrainCar testCar = CheckWagonList[CarNumber];
                    if (ShunterFullTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber > 0)
                    {
                        ShunterCheckAirPressureInCar(elapsedClockSeconds, testCar);
                        if (CheckAuxResBrakeLine) return;

                        if (testCar.BrakesStuck || testCar.BrakeSystem.CarHasProblemWithBrake || testCar.BrakeSystem.CarHasReallyProblemWithBrake)
                        {
                            testCar.BrakeSystem.BrakeCarDeactivate = true;
                            testCar.BrakeSystem.BrakeCarDeactivateMenu = 1;
                            testCar.BrakeSystem.BleedOffValveOpen = true;
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_BrakeDeactivate);
                        }
                        if (testCar.BrakeSystem.HandBrakeActive)
                        {
                            testCar.BrakeSystem.HandBrakeDeactive = true;
                            testCar.BrakeSystem.HandBrakeActive = false;
                            testCar.BrakeSystem.SetHandbrakePercent(0);
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_HandBrakeRelease);
                        }
                        if (testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = false;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }

                        if (testCar == Locomotive.Train.FirstCar)
                        {
                            testCar.BrakeSystem.AngleCockAOpen = false;
                            testCar.BrakeSystem.AngleCockBOpen = true;
                        }
                        else
                            if (testCar == Locomotive.Train.LastCar)
                            {
                                testCar.BrakeSystem.AngleCockAOpen = true;
                                testCar.BrakeSystem.AngleCockBOpen = false;
                                testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                            }
                            else                                
                                {
                                    testCar.BrakeSystem.AngleCockAOpen = true;
                                    testCar.BrakeSystem.AngleCockBOpen = true;
                                    testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                                }

                        ShunterFullTestBrakePhase4CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber--;
                        FirstBoggie = false;
                        SecondBoggie = false;
                        SetCarMode(testCar);
                    }
                    else
                    if (!CheckAuxResBrakeLine)
                    {
                        if (testCar != null && !testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = true;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;                        
                        }
                        if (testCar == null)
                        {
                            TestCarReset();
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }

                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f)
                        {                            
                            if (!FirstBoggie && ShunterFullTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterFullTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                SecondBoggie = true;
                                testCar.BrakeSystem.CarHasMaybeProblemWithBrake = true;
                            }
                        }
                        if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() <= 0.1f * 14.50377f)
                        {
                            if (!FirstBoggie && ShunterFullTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                FirstBoggie = true;
                            }
                            if (!SecondBoggie && ShunterFullTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                            {
                                testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                SecondBoggie = true;
                            }
                        }
                    }

                    if (!CheckAuxResBrakeLine && ShunterFullTestBrakePhase4Timer > ShunterFullTestBrakePhase4Time + 5.0f)
                    {
                        ShunterFullTestBrakePhase4 = false;
                        ShunterFullTestBrakePhase4Timer = 0;
                        ShunterFullTestBrakePhase4CarCheckTimer = 0;
                        CarNumber = Locomotive.Train.Cars.Count - 1 - 1;
                        ShunterFullTestBrakePhase5 = true;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Full test brake second side completed!"));
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_SecondSideDone);
                    }
                }

                // Fáze 5: Generuje informaci o vlaku
                if (ShunterFullTestBrakePhase5 && !ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase4)
                {
                    ShunterFullTestBrakePhase5Timer += elapsedClockSeconds;

                    if (ShunterFullTestBrakePhase5Timer > 15.0f && ShunterFullTestBrakePhase5Timer < 15.5f)
                    {
                        // Bržděnka
                        Locomotive.Simulator.FullTestBrakeWindow = true;
                    }

                    if (ShunterFullTestBrakePhase5Timer > 5.0f && ShunterFullTestBrakePhase5Timer < 5.5f)
                    {                        
                        string TestBrakeWindowMessageProblemCars = "";
                        string TestBrakeWindowMessageNotConnectedCars = "";
                        string TestBrakeWindowMessage1 = "";
                        string TestBrakeWindowMessage2 = "";
                        string TestBrakeWindowMessage3 = "";

                        bool BrakeProblemFound = false;
                        bool ConnectProblemFound = false;
                        CarNumber = 0;
                        foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                        {
                            CarNumber++;
                            car.BrakeCarStatus();
                            car.ShunterTestBrakeDone = true;
                            if (car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake || car.BrakeSystem.BrakeCarDeactivate || car.BrakeSystem.CarHasReallyProblemWithBrake)
                            {
                                TestBrakeWindowMessageProblemCars += car.WagonName + " - " + car.CarID + "\n";
                                BrakeProblemFound = true;
                            }
                            if (CheckCarBrakeFault[CarNumber])
                            {
                                TestBrakeWindowMessageNotConnectedCars += CheckWagonList[CarNumber].WagonName + " - " + CheckWagonList[CarNumber].CarID + "\n";
                                ConnectProblemFound = true;
                            }
                        }
                        TestBrakeWindowMessage1 = Simulator.Catalog.GetString("Full test brake completed successfully!") + "\n\n" + Locomotive.Simulator.TrainOperationsInfoText + "\n" + Locomotive.Simulator.TrainOperationsRealBrakePercentText;
                        if (BrakeProblemFound)
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("These Cars have problems with brake and brake was deactivated:");
                        else
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("No problem with brake found in the train!");

                        if (ConnectProblemFound)
                            TestBrakeWindowMessage3 = Simulator.Catalog.GetString("These Cars were not connected:");
                        else
                            TestBrakeWindowMessage3 = Simulator.Catalog.GetString("All Cars are connected!");

                        Locomotive.Simulator.TestBrakeWindowMessage = TestBrakeWindowMessage1 + "\n\n" + TestBrakeWindowMessage2 + "\n\n" + TestBrakeWindowMessageProblemCars + "\n\n" + TestBrakeWindowMessage3 + "\n\n" + TestBrakeWindowMessageNotConnectedCars;

                        if (BrakeProblemFound)
                        {
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Full test brake completed unsuccessfully!"));
                            Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_CompletedNegative);
                        }
                        else
                        {
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Full test brake completed successfully!"));
                            Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_Completed);                            
                        }
                    }
                }
            }
            else
            {
                ShunterFullTestBrakePhase1 = false;
                ShunterFullTestBrakePhase2 = false;
                ShunterFullTestBrakePhase3 = false;
                ShunterFullTestBrakePhase4 = false;
                ShunterFullTestBrakePhase5 = false;
                ShunterFullTestBrakePhase1Timer = 0;
                ShunterFullTestBrakePhase2Timer = 0;
                ShunterFullTestBrakePhase3Timer = 0;
                ShunterFullTestBrakePhase4Timer = 0;
                ShunterFullTestBrakePhase5Timer = 0;
                ShunterFullTestBrakePhase2CarCheckTimer = 0;
                ShunterFullTestBrakePhase4CarCheckTimer = 0;                
                Locomotive.Simulator.FullTestBrakeWindow = false;
                ShunterFullTestBrakePhase1TimeOffset = 0;
            }
            #endregion ShunterFullTestBrake            

            #region ShunterSimpleTestBrake
            if (Locomotive.Simulator.ShunterSimpleTestBrakeEnable && !Locomotive.Simulator.PlayerLocomotiveChange)
            {
                if (!Locomotive.Simulator.CabRadioOn)
                {
                    ShunterTimeWithOutRadio += elapsedClockSeconds;
                    if (ShunterTimeWithOutRadio > 30f) // Po 30 sekundách bez rádia se zobrazí hláška upozornění posunovačem, že by rádio mělo být zapnuté
                    {
                        ShunterTimeWithOutRadio = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Cab radio should be on!"));
                    }                    
                    return;
                }
                else
                    ShunterTimeWithOutRadio = 0;

                for (int i = 0; i < 100; i++)
                {
                    ShunterTestBrakeCarCheckTime[i] = 0;                    
                    CheckWagonList[i] = null;                    
                }

                LastCarConnectedNumber = 0;
                LastCarConnected = null;
                int CheckWagonListIndex = 0;
                foreach (TrainCar car in Locomotive.Train.Cars)
                {                    
                    if (car is MSTSLocomotive && (car as MSTSLocomotive).IsLeadLocomotive()) { }
                    else
                    {
                        LastCarConnectedNumber++;
                        LastCarConnected = car;
                    }                    
                }

                foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                {
                    CheckWagonListIndex = 1;
                    CheckWagonList[1] = car;
                    
                    if (!car.CarHasBrakePipeConnected)
                    {
                        CheckCarBrakeFault[1] = true;
                    }
                }                

                if (Locomotive.AbsSpeedMpS > 1.0f / 3.6f || Locomotive.Train.Cars.Count == 1 || LastCarConnected == null)
                {
                    ShunterSimpleTestBrakePhase1 = false;
                    ShunterSimpleTestBrakePhase2 = false;
                    ShunterSimpleTestBrakePhase3 = false;
                    ShunterSimpleTestBrakePhase4 = false;
                    ShunterSimpleTestBrakePhase5 = false;
                    ShunterSimpleTestBrakePhase1Timer = 0;
                    ShunterSimpleTestBrakePhase2Timer = 0;
                    ShunterSimpleTestBrakePhase3Timer = 0;
                    ShunterSimpleTestBrakePhase4Timer = 0;
                    ShunterSimpleTestBrakePhase5Timer = 0;
                    ShunterSimpleTestBrakePhase2CarCheckTimer = 0;
                    ShunterSimpleTestBrakePhase4CarCheckTimer = 0;                    
                    Locomotive.Simulator.SimpleTestBrakeWindow = false;
                    FirstBoggie = false;
                    SecondBoggie = false;
                    if (LastCarConnected == null) return;
                }

                if (!ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase5)
                {
                    if (Locomotive.AbsSpeedMpS > 1.0f / 3.6f || Locomotive.Train.Cars.Count < 2 || Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.9f * 14.50377f)
                    {
                        Locomotive.Simulator.ShunterSimpleTestBrakeEnable = false;

                        if (Locomotive.AbsSpeedMpS > 1.0f / 3.6f)
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Train speed must be zero to perform the test brake!"));

                        if (Locomotive.Train.Cars.Count < 2)
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Train must have at least two cars to perform the test brake!"));

                        if (Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.9f * 14.50377f)
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Train must have sufficient brakepipe pressure (5 bar) to perform the test brake!"));

                        return;
                    }

                    ShunterSimpleTestBrakePhase1 = true;
                    Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Simple test brake activated. Please follow the instructions."));
                    Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_Start);
                }

                // Požadavek pro fázi 1: Aplikovat brzdy pod 4.5 bar                
                if (ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase5)
                {
                    if (ShunterSimpleTestBrakePhase1Timer > 9.5f && ShunterSimpleTestBrakePhase1Timer < 10.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please apply the brakes under 4.5 bar and holdon!"));
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_ApplyBrake);
                    }

                    ShunterSimpleTestBrakePhase1Timer += elapsedClockSeconds;
                    if (ShunterSimpleTestBrakePhase1Timer > 10.0f + 30.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.5f * 14.50377f)
                    {
                        ShunterSimpleTestBrakePhase1Timer = 10.0f;
                    }

                    if (!ShunterSimpleTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.5f * 14.50377f && (ShunterSimpleTestBrakePhase1Timer > Locomotive.Train.Cars.Count * 5.0f || CheckWagonList[CheckWagonListIndex].BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f))
                    {
                        ShunterSimpleTestBrakePhase1 = false;
                        ShunterSimpleTestBrakePhase2 = true;
                        ShunterSimpleTestBrakePhase2Timer = 0;
                        CarNumber = 1;
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_FirstSide);
                    }
                }

                if (ShunterSimpleTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.8f * 14.50377f)
                {
                    ShunterSimpleTestBrakePhase2 = false;
                }                

                // Požadavek pro fázi 2: Kontrola aplikace brzd od první vozu k poslednímu
                if (ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase5)
                {
                    float ShunterSimpleTestBrakePhase2Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                    {
                        if (CarTimeNumber == 1) break;
                        CarTimeNumber++;
                        float CarBrakeCheckTime = (((car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake) && !car.BrakeSystem.BrakeCarDeactivate) || car.BrakeSystem.HandBrakeActive) ? Simulator.Random.Next(6, 12) : Simulator.Random.Next(2, 6) + (car.CarLengthM / 2.0f);
                        ShunterSimpleTestBrakePhase2Time += CarBrakeCheckTime;
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = CarBrakeCheckTime;                        
                    }
                    
                    ShunterSimpleTestBrakePhase2Timer += elapsedClockSeconds;                    
                    if (ShunterSimpleTestBrakePhase2Timer > 5.0f)
                    {
                        ShunterSimpleTestBrakePhase2CarCheckTimer += elapsedClockSeconds;
                    }

                    TrainCar testCar = CheckWagonList[CarNumber];
                    if (ShunterSimpleTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber <= CarTimeNumber)
                    {
                        if (testCar.BrakesStuck || testCar.BrakeSystem.CarHasProblemWithBrake || testCar.BrakeSystem.CarHasReallyProblemWithBrake)
                        {
                            testCar.BrakeSystem.BrakeCarDeactivate = true;
                            testCar.BrakeSystem.BrakeCarDeactivateMenu = 1;
                            testCar.BrakeSystem.BleedOffValveOpen = true;
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_BrakeDeactivate);
                        }                        
                        if (testCar.BrakeSystem.HandBrakeActive)
                        {
                            testCar.BrakeSystem.HandBrakeDeactive = true;
                            testCar.BrakeSystem.HandBrakeActive = false;
                            testCar.BrakeSystem.SetHandbrakePercent(0);
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_HandBrakeRelease);
                        }
                        if (testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = false;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }

                        if (testCar == Locomotive.Train.FirstCar)
                        {
                            testCar.BrakeSystem.AngleCockAOpen = false;
                            testCar.BrakeSystem.AngleCockBOpen = true;
                        }
                        else
                            if (testCar == Locomotive.Train.LastCar)
                            {
                                testCar.BrakeSystem.AngleCockAOpen = true;
                                testCar.BrakeSystem.AngleCockBOpen = false;
                                testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                            }
                            else                                
                                {
                                    testCar.BrakeSystem.AngleCockAOpen = true;
                                    testCar.BrakeSystem.AngleCockBOpen = true;
                                    testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                                }

                        ShunterSimpleTestBrakePhase2CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber++;
                        FirstBoggie = false;
                        SecondBoggie = false;
                    }
                    else                    
                    {                        
                        if (testCar != null && !testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = true;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;                            
                        }
                        if (testCar == null || CarNumber > CarTimeNumber)
                        {
                            TestCarReset();
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }

                        if (CarNumber <= CarTimeNumber)
                        {
                            if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f)
                            {                                
                                if (!FirstBoggie && ShunterSimpleTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                    FirstBoggie = true;
                                }
                                if (!SecondBoggie && ShunterSimpleTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                    SecondBoggie = true;
                                }
                            }
                            if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() <= 0.1f * 14.50377f)
                            {                                
                                if (!FirstBoggie && ShunterSimpleTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                    FirstBoggie = true;
                                }
                                if (!SecondBoggie && ShunterSimpleTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                    SecondBoggie = true;
                                    testCar.BrakeSystem.CarHasMaybeProblemWithBrake = true;
                                }
                            }
                        }
                    }

                    if (ShunterSimpleTestBrakePhase2Timer > ShunterSimpleTestBrakePhase2Time + 5.0f)
                    {
                        ShunterSimpleTestBrakePhase2 = false;
                        ShunterSimpleTestBrakePhase2Timer = 0;
                        ShunterSimpleTestBrakePhase2CarCheckTimer = 0;
                        CarNumber = CarTimeNumber;
                        ShunterSimpleTestBrakePhase3 = true;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Simple test brake first side completed!"));
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_FirstSideDone);
                    }
                }

                // Požadavek pro fázi 3: Uvolnit brzdy nad 4.9 bar
                if (ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase5)
                {
                    if (ShunterSimpleTestBrakePhase3Timer > 4.5f && ShunterSimpleTestBrakePhase3Timer < 5.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please release the brakes and holdon!"));
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_ReleaseBrake);
                    }

                    ShunterSimpleTestBrakePhase3Timer += elapsedClockSeconds;
                    if (ShunterSimpleTestBrakePhase3Timer > 5.0f + 30.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI <= 4.5f * 14.50377f)
                    {
                        ShunterSimpleTestBrakePhase3Timer = 5.0f;
                    }

                    if (!ShunterSimpleTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI > 4.9f * 14.50377f && (ShunterSimpleTestBrakePhase3Timer > Locomotive.Train.Cars.Count * 5.0f || CheckWagonList[CheckWagonListIndex].BrakeSystem.GetCylPressurePSI() < 0.1f * 14.50377f))
                    {
                        ShunterSimpleTestBrakePhase3 = false;
                        ShunterSimpleTestBrakePhase4 = true;
                        ShunterSimpleTestBrakePhase4Timer = 0;
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_SecondSide);
                    }

                    //  Pokusí se propojit vůz
                    if (!ShunterSimpleTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI < 4.9f * 14.50377f && ShunterSimpleTestBrakePhase3Timer > Locomotive.Train.Cars.Count * 5.0f)
                    {
                        var testCar1 = CheckWagonList[1];
                        int testCar1Position = -1;
                        for (int i = 0; i < Locomotive.Train.Cars.Count; i++)
                        {
                            if (Locomotive.Train.Cars[i] == testCar1)
                            {
                                testCar1Position = i;
                                break;
                            }
                        }
                        if (testCar1Position - 1 >= 0)
                        {
                            var testCar0 = Locomotive.Train.Cars[testCar1Position - 1];
                            if (testCar0 == Locomotive.Train.FirstCar)
                            {
                                testCar0.BrakeSystem.AngleCockAOpen = false;
                                testCar0.BrakeSystem.AngleCockBOpen = true;
                            }
                            else
                                if (testCar0 == Locomotive.Train.LastCar)
                                {
                                    testCar0.BrakeSystem.AngleCockAOpen = true;
                                    testCar0.BrakeSystem.AngleCockBOpen = false;
                                    testCar0.BrakeSystem.FrontBrakeHoseConnected = true;
                                }
                                else
                                {
                                    testCar0.BrakeSystem.AngleCockAOpen = true;
                                    testCar0.BrakeSystem.AngleCockBOpen = true;
                                    testCar0.BrakeSystem.FrontBrakeHoseConnected = true;
                                }
                        }
                        if (testCar1Position + 1 < Locomotive.Train.Cars.Count)
                        {
                            var testCar0 = Locomotive.Train.Cars[testCar1Position + 1];
                            if (testCar0 == Locomotive.Train.FirstCar)
                            {
                                testCar0.BrakeSystem.AngleCockAOpen = false;
                                testCar0.BrakeSystem.AngleCockBOpen = true;
                            }
                            else
                                if (testCar0 == Locomotive.Train.LastCar)
                                {
                                    testCar0.BrakeSystem.AngleCockAOpen = true;
                                    testCar0.BrakeSystem.AngleCockBOpen = false;
                                    testCar0.BrakeSystem.FrontBrakeHoseConnected = true;
                                }
                                else
                                {
                                    testCar0.BrakeSystem.AngleCockAOpen = true;
                                    testCar0.BrakeSystem.AngleCockBOpen = true;
                                    testCar0.BrakeSystem.FrontBrakeHoseConnected = true;
                                }
                        }
                    }
                }

                if (!CheckAuxResBrakeLine && ShunterSimpleTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI < 4.7f * 14.50377f)
                {
                    ShunterSimpleTestBrakePhase4 = false;
                    ShunterSimpleTestBrakePhase3 = true;
                }                

                // Požadavek pro fázi 4: Kontrola uvolnění brzd od posledního vozu k prvnímu                
                if (ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase5)
                {
                    float ShunterSimpleTestBrakePhase4Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                    {
                        if (CarTimeNumber == 1) break;
                        CarTimeNumber++;
                        float CarBrakeCheckTime = (((car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake) && !car.BrakeSystem.BrakeCarDeactivate) || car.BrakeSystem.HandBrakeActive) ? Simulator.Random.Next(6, 12) : Simulator.Random.Next(2, 6) + (car.CarLengthM / 2.0f);
                        ShunterSimpleTestBrakePhase4Time += CarBrakeCheckTime;
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = CarBrakeCheckTime;                        
                    }

                    if (!CheckAuxResBrakeLine)
                        ShunterSimpleTestBrakePhase4Timer += elapsedClockSeconds;                    

                    if (ShunterSimpleTestBrakePhase4Timer > 5.0f && !CheckAuxResBrakeLine)
                    {
                        ShunterSimpleTestBrakePhase4CarCheckTimer += elapsedClockSeconds;
                    }

                    TrainCar testCar = CheckWagonList[CarNumber];                    
                    if (ShunterSimpleTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber > 0)
                    {
                        CheckCar = testCar;
                        CheckCarNumber = CarNumber;
                        
                        ShunterCheckAirPressureInCar(elapsedClockSeconds, testCar);
                        if (CheckAuxResBrakeLine) return;

                        if (testCar.BrakesStuck || testCar.BrakeSystem.CarHasProblemWithBrake || testCar.BrakeSystem.CarHasReallyProblemWithBrake)
                        {
                            testCar.BrakeSystem.BrakeCarDeactivate = true;
                            testCar.BrakeSystem.BrakeCarDeactivateMenu = 1;
                            testCar.BrakeSystem.BleedOffValveOpen = true;
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_BrakeDeactivate);
                        }
                        if (testCar.BrakeSystem.HandBrakeActive)
                        {
                            testCar.BrakeSystem.HandBrakeDeactive = true;
                            testCar.BrakeSystem.HandBrakeActive = false;
                            testCar.BrakeSystem.SetHandbrakePercent(0);
                            testCar.SignalEvent(Event.ShunterTestBrakeSound_HandBrakeRelease);
                        }
                        if (testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = false;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }

                        if (testCar == Locomotive.Train.FirstCar)
                        {
                            testCar.BrakeSystem.AngleCockAOpen = false;
                            testCar.BrakeSystem.AngleCockBOpen = true;
                        }
                        else
                            if (testCar == Locomotive.Train.LastCar)
                            {
                                testCar.BrakeSystem.AngleCockAOpen = true;
                                testCar.BrakeSystem.AngleCockBOpen = false;
                                testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                            }
                            else                                
                                {
                                    testCar.BrakeSystem.AngleCockAOpen = true;
                                    testCar.BrakeSystem.AngleCockBOpen = true;
                                    testCar.BrakeSystem.FrontBrakeHoseConnected = true;
                                }

                        ShunterSimpleTestBrakePhase4CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber--;                        
                        FirstBoggie = false;
                        SecondBoggie = false;
                    }
                    else
                    if (!CheckAuxResBrakeLine)
                    {
                        if (testCar != null && !testCar.ShunterTestingBrake)
                        {
                            testCar.ShunterTestingBrake = true;
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;                            
                        }
                        if (testCar == null)
                        {
                            TestCarReset();
                            Locomotive.Simulator.ShunterTestingBrakeChanged = true;
                        }

                        if (CarNumber > 0)
                        {
                            if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() > 0.1f * 14.50377f)
                            {                                
                                if (!FirstBoggie && ShunterSimpleTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                    FirstBoggie = true;                                    
                                }
                                if (!SecondBoggie && ShunterSimpleTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeApply);
                                    SecondBoggie = true;
                                    testCar.BrakeSystem.CarHasMaybeProblemWithBrake = true;
                                }
                            }
                            if (testCar != null && testCar.BrakeSystem.GetCylPressurePSI() <= 0.1f * 14.50377f)
                            {                                
                                if (!FirstBoggie && ShunterSimpleTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.25f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                    FirstBoggie = true;
                                }
                                if (!SecondBoggie && ShunterSimpleTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] * 0.75f)
                                {
                                    testCar.SignalEvent(Event.ShunterTestBrakeSound_CheckBrakeRelease);
                                    SecondBoggie = true;
                                }
                            }
                        }
                    }

                    if (ShunterSimpleTestBrakePhase4Timer > ShunterSimpleTestBrakePhase4Time + 5.0f)
                    {
                        ShunterSimpleTestBrakePhase4 = false;
                        ShunterSimpleTestBrakePhase4Timer = 0;
                        ShunterSimpleTestBrakePhase4CarCheckTimer = 0;                        
                        ShunterSimpleTestBrakePhase5 = true;                        
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Simple test brake second side completed!"));
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_SecondSideDone);
                    }
                }

                // Pokud test přesáhne limitní test, tak je ZB neúspěšná
                bool ShunterSimpleTestBrakePhaseTimeOut = false;
                if (ShunterSimpleTestBrakePhase3Timer > 90)
                {
                    CheckCar = CheckWagonList[CarNumber]; 
                    ShunterSimpleTestBrakePhase1 = false;
                    ShunterSimpleTestBrakePhase2 = false;
                    ShunterSimpleTestBrakePhase3 = false;
                    ShunterSimpleTestBrakePhase4 = false;
                    ShunterSimpleTestBrakePhase5 = true;
                    ShunterSimpleTestBrakePhaseTimeOut = true;
                }

                // Kontrola, zda není aktivovaný ruční brzda u některého z vozů, pokud ano, tak se pokusí ruční brzdu uvolnit, aby test mohl pokračovat
                if (ShunterSimpleTestBrakePhase5)
                {
                    ShunterSimpleTestBrakeHandBrakeActivated = false;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive) || (car is MSTSLocomotive && !(car as MSTSLocomotive).IsLeadLocomotive() && !car.AcceptCableSignals && !car.AcceptHelperSignals)))
                    {
                        if (car.BrakeSystem.HandBrakeActive)
                        {
                            ShunterSimpleTestBrakeHandBrakeActivated = true;
                            ShunterSimpleTestBrakeHandBrakeTimer += elapsedClockSeconds;
                            if (ShunterSimpleTestBrakeHandBrakeTimer > car.CarLengthM)
                            {
                                ShunterSimpleTestBrakeHandBrakeTimer = 0;                                
                                car.BrakeSystem.HandBrakeDeactive = true;
                                car.BrakeSystem.HandBrakeActive = false;
                                car.BrakeSystem.SetHandbrakePercent(0);
                                car.SignalEvent(Event.ShunterTestBrakeSound_HandBrakeRelease);
                                break;
                            }
                        }
                    }
                }                

                // Fáze 5: Generuje informaci o vlaku
                if (!ShunterSimpleTestBrakeHandBrakeActivated && ShunterSimpleTestBrakePhase5 && !ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4)
                {                    
                    ShunterSimpleTestBrakePhase5Timer += elapsedClockSeconds;

                    if (ShunterSimpleTestBrakePhase5Timer > 15.0f && ShunterSimpleTestBrakePhase5Timer < 15.5f)
                    {
                        // Bržděnka
                        Locomotive.Simulator.SimpleTestBrakeWindow = true;
                    }

                    if (ShunterSimpleTestBrakePhase5Timer > 5.0f && ShunterSimpleTestBrakePhase5Timer < 5.5f)
                    {                        
                        string TestBrakeWindowMessageProblemCars = "";
                        string TestBrakeWindowMessageNotConnectedCars = "";
                        string TestBrakeWindowMessage1 = "";
                        string TestBrakeWindowMessage2 = "";
                        string TestBrakeWindowMessage3 = "";
                        string TestBrakeWindowMessage4 = "";

                        bool BrakeProblemFound = false;
                        bool ConnectProblemFound = false;
                                                
                        CheckCar.BrakeCarStatus();
                        CheckCar.ShunterTestBrakeDone = true;
                        if (CheckCar.BrakesStuck || CheckCar.BrakeSystem.CarHasProblemWithBrake || CheckCar.BrakeSystem.BrakeCarDeactivate || CheckCar.BrakeSystem.CarHasReallyProblemWithBrake)
                        {
                            TestBrakeWindowMessageProblemCars += CheckCar.WagonName + " - " + CheckCar.CarID + "\n";
                            BrakeProblemFound = true;
                        }
                        if (CheckCarBrakeFault[CheckCarNumber])
                        {
                            TestBrakeWindowMessageNotConnectedCars += CheckWagonList[CheckCarNumber].WagonName + " - " + CheckWagonList[CheckCarNumber].CarID + "\n";
                            ConnectProblemFound = true;
                        }
                        
                        TestBrakeWindowMessage1 = Simulator.Catalog.GetString("Simple test brake completed successfully!") + "\n\n" + Locomotive.Simulator.TrainOperationsInfoText + "\n" + Locomotive.Simulator.TrainOperationsRealBrakePercentText;
                        if (BrakeProblemFound)
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("This Car has problem with brake and brake was deactivated:");
                        else
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("No problem with brake found!");

                        if (ConnectProblemFound)
                        {
                            if (CheckCar.CarHasBrakePipeConnected)
                            {
                                TestBrakeWindowMessage3 = Simulator.Catalog.GetString("This Car was not connected:");
                                ConnectProblemFound = false;
                            }
                            else
                                TestBrakeWindowMessage3 = Simulator.Catalog.GetString("This Car is not connected:");
                        }

                        if (ShunterSimpleTestBrakePhaseTimeOut)
                            TestBrakeWindowMessage4 = Simulator.Catalog.GetString("Test brake phase timed out!");

                        Locomotive.Simulator.TestBrakeWindowMessage = TestBrakeWindowMessage1 + "\n\n" + TestBrakeWindowMessage2 + "\n\n" + TestBrakeWindowMessageProblemCars + "\n\n" + TestBrakeWindowMessage3 + "\n\n" + TestBrakeWindowMessageNotConnectedCars + "\n\n" + TestBrakeWindowMessage4;

                        if (BrakeProblemFound || ConnectProblemFound || ShunterSimpleTestBrakePhaseTimeOut)
                        {
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Simple test brake completed unsuccessfully!"));
                            Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_CompletedNegative);
                        }
                        else
                        {
                            Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Simple test brake completed successfully!"));
                            Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_Completed);
                        }
                    }
                }
            }
            else
            {
                ShunterSimpleTestBrakePhase1 = false;
                ShunterSimpleTestBrakePhase2 = false;
                ShunterSimpleTestBrakePhase3 = false;
                ShunterSimpleTestBrakePhase4 = false;
                ShunterSimpleTestBrakePhase5 = false;
                ShunterSimpleTestBrakePhase1Timer = 0;
                ShunterSimpleTestBrakePhase2Timer = 0;
                ShunterSimpleTestBrakePhase3Timer = 0;
                ShunterSimpleTestBrakePhase4Timer = 0;
                ShunterSimpleTestBrakePhase5Timer = 0;
                ShunterSimpleTestBrakePhase2CarCheckTimer = 0;
                ShunterSimpleTestBrakePhase4CarCheckTimer = 0;                
                Locomotive.Simulator.SimpleTestBrakeWindow = false;
            }        
            #endregion ShunterSimpleTestBrake

            #region Shunter
            bool TRAINAHEAD_Mode;
            float DistanceToSTP = -1000;            
            if (Locomotive.Simulator.ShunterEnable && !Locomotive.Simulator.PlayerLocomotiveChange)
            {                
                if (!Locomotive.Simulator.CabRadioOn)
                {
                    ShunterTimeWithOutRadio += elapsedClockSeconds;
                    if (ShunterTimeWithOutRadio > 30f) // Po 30 sekundách bez rádia se zobrazí hláška upozornění posunovačem, že by rádio mělo být zapnuté
                    {
                        ShunterTimeWithOutRadio = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Cab radio should be on!"));
                    }
                    ShunterSoundToTrainReset();
                    ShunterSoundSTPReset();                    
                    Locomotive.Simulator.ShunterProcessSTPActive_Start = false;
                    Locomotive.Simulator.ShunterProcessSTPActive_End = false;
                    Locomotive.Simulator.ShunterProcessTrainActive_Start = false;
                    Locomotive.Simulator.ShunterProcessTrainActive_End = false;
                    ShunterProcessTimer = 0;
                    Locomotive.Simulator.DistanceToOtherTrain = -1000;
                    DistanceToOtherTrain_0 = -1000;
                    Locomotive.Simulator.OtherTrainPositionTest = false;
                    ShunterSoundDonePlayed = false;                    
                    Locomotive.Simulator.ShunterDecideMarker = "";
                    return;
                }
                else
                    ShunterTimeWithOutRadio = 0;

                // První hláška posunovače
                if (Locomotive.AbsSpeedMpS < 0.01f)
                {
                    if (!ShunterSoundStartPlayed) Locomotive.SignalEvent(Event.ShunterSound_Start);
                    ShunterSoundStartPlayed = true;
                }

                // Detekce vozů před hráčem
                float DistanceToOtherTrain = -1000;
                TRAINAHEAD_Mode = false;
                for (int i = 0; i < Locomotive.Train.EndAuthorityType.Length; i++)
                {
                    if (Locomotive.Train.EndAuthorityType[i] == Train.END_AUTHORITY.TRAIN_AHEAD && Locomotive.Train.DistanceToEndNodeAuthorityM[i] < 1000)
                    {
                        DistanceToOtherTrain = Locomotive.Train.DistanceToEndNodeAuthorityM[i];
                        TRAINAHEAD_Mode = true;
                        if (Locomotive.Train.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
                        {
                            Locomotive.Simulator.DistanceToOtherTrain = DistanceToOtherTrain;
                        }
                        break;
                    }                    
                }

                // Signál stůj
                float DistanceToSIGNAL = 1000;                
                if (Locomotive.Train.NextSignalObject[0] != null)
                {
                    MstsSignalAspect nextAspect = Locomotive.Train.GetNextSignalAspect(0);                    
                    switch (nextAspect)
                    {
                        case MstsSignalAspect.STOP:
                            if (Locomotive.Train.NextSignalObject[0].hasPermission != SignalObject.Permission.Granted)
                            {
                                if (Locomotive.Train.DistanceToSignal.HasValue && Locomotive.Train.DistanceToSignal < 1000)
                                {
                                    DistanceToSIGNAL = Locomotive.Train.DistanceToSignal.Value;                                    
                                }
                            }                            
                            break;                        
                    }
                }

                // Konec tratě
                float DistanceToEOA = 1000;
                if ((Locomotive.Train.EndAuthorityType[0] == Train.END_AUTHORITY.END_OF_TRACK || Locomotive.Train.EndAuthorityType[0] == Train.END_AUTHORITY.END_OF_PATH || Locomotive.Train.EndAuthorityType[0] == Train.END_AUTHORITY.END_OF_AUTHORITY)
                    && Locomotive.Train.DistanceToEndNodeAuthorityM[0] < 10)
                {
                    DistanceToEOA = Locomotive.Train.DistanceToEndNodeAuthorityM[0];
                }

                // Reverzní bod
                Locomotive.Simulator.DistanceToReverse = 1000;
                Locomotive.Train.GetTrainInfo();
                float DistanceToReverse = 1000;
                DistanceToReverse = Locomotive.Simulator.DistanceToReverse > -1 ? Locomotive.Simulator.DistanceToReverse : 1000;

                // Událost s povinností zastavit
                float DistanceToTriggerOnStop = 1000;
                DistanceToTriggerOnStop = Locomotive.Train.ControlMode == Train.TRAIN_CONTROL.EXPLORER ? 1000 : Locomotive.Simulator.DistanceToTriggerOnStop;                

                DistanceToSTP = 1000;                
                DistanceToSTP = Math.Min(DistanceToSIGNAL, DistanceToEOA);
                DistanceToSTP = Math.Min(DistanceToSTP, DistanceToReverse);
                DistanceToSTP = Math.Min(DistanceToSTP, DistanceToTriggerOnStop);                                

                float DistanceSpeedCorrectionM = MathHelper.Clamp(Locomotive.AbsSpeedMpS * 3.6f / 2f, 0, 10.0f); // Korekce vzdálenosti závislé na rychlosti pro aktivaci hlášek                

                if (Locomotive.Simulator.ShunterDecideMarker != ShunterDecideMarker)
                {
                    Locomotive.Simulator.ShunterEnableChanged = true;
                    ShunterDecideMarker = Locomotive.Simulator.ShunterDecideMarker;
                }

                if (Locomotive.Simulator.CabRadioOn)
                {
                    // Prodleva pro rozhodnutí, jestli posunovač je v procesu najetí ke stopu nebo na vlak                
                    if (Locomotive.Simulator.ShunterProcessSTPActive_Start || Locomotive.Simulator.ShunterProcessTrainActive_Start)
                    {
                        ShunterDecideProcess = false;
                    }
                    if (!ShunterDecideProcess && !Locomotive.Simulator.ShunterProcessSTPActive_Start && !Locomotive.Simulator.ShunterProcessSTPActive_End && !Locomotive.Simulator.ShunterProcessTrainActive_Start && !Locomotive.Simulator.ShunterProcessTrainActive_End)
                    {
                        ShunterDecideProcessTimer += elapsedClockSeconds;

                        if (ShunterDecideProcessTimer > 1.0f) Locomotive.Simulator.ShunterDecideMarker = "OK";
                        else
                            if (ShunterDecideProcessTimer > 0.75f) Locomotive.Simulator.ShunterDecideMarker = "|";
                            else
                                if (ShunterDecideProcessTimer > 0.5f) Locomotive.Simulator.ShunterDecideMarker = "-";
                                else
                                    if (ShunterDecideProcessTimer > 0.25f) Locomotive.Simulator.ShunterDecideMarker = "|";
                                    else
                                        if (ShunterDecideProcessTimer > 0.0f) Locomotive.Simulator.ShunterDecideMarker = "-";

                        if (ShunterDecideProcessTimer > 1.5f)
                        {
                            ShunterDecideProcess = true;
                            ShunterDecideProcessTimer = 0;
                        }
                        else
                            return;
                    }
                }                

                // Pokud je posunovač v režimu "vlak před námi" a vzdálenost od jiného vlaku je větší než vzdálenost od stopu o 10 metrů, režim "vlak před námi" se vypne, aby se zabránilo zbytečným hláškám posunovače, když se vlak přibližuje ke stopu a není tam žádný vlak před ním                
                if (DistanceToSTP != 1000 && DistanceToOtherTrain - DistanceToSTP > 10) TRAINAHEAD_Mode = false;

                if (Locomotive.Simulator.DistanceToOtherTrain == 0) TRAINAHEAD_Mode = false;                

                // Kontrola platnosti navádění na vůz                
                if (Locomotive.AbsSpeedMpS > 0.1f)
                {                    
                    if ((TRAINAHEAD_Mode && DistanceToOtherTrain == CheckDistance))
                    {
                        Locomotive.Simulator.ShunterDecideMarker = Simulator.Catalog.GetString("CONFUSED");
                        return;
                    }
                    CheckDistance = DistanceToOtherTrain;
                }
                
                if (LastShunterDecideMarker != Locomotive.Simulator.ShunterDecideMarker && Locomotive.AbsSpeedMpS < 0.1f)
                {
                    LastShunterDecideMarker = Locomotive.Simulator.ShunterDecideMarker;
                    Locomotive.Simulator.ShunterProcessSTPActive_Start = false;
                    Locomotive.Simulator.ShunterProcessSTPActive_End = false;
                    Locomotive.Simulator.ShunterProcessTrainActive_Start = false;
                    Locomotive.Simulator.ShunterProcessTrainActive_End = false;
                    ShunterSoundToTrainReset();
                    ShunterSoundSTPReset();                                        
                    ShunterProcessTimer = 0;
                    Locomotive.Simulator.DistanceToOtherTrain = -1000;
                    DistanceToOtherTrain_0 = -1000;
                    Locomotive.Simulator.OtherTrainPositionTest = false;
                    ShunterSoundDonePlayed = false;                    
                }

                if (DistanceToSTP == 1000 && !TRAINAHEAD_Mode)
                {
                    Locomotive.Simulator.ShunterDecideMarker = "";
                    Locomotive.Simulator.ShunterProcessSTPActive_Start = false;
                    Locomotive.Simulator.ShunterProcessSTPActive_End = false;
                    Locomotive.Simulator.ShunterProcessTrainActive_Start = false;
                    Locomotive.Simulator.ShunterProcessTrainActive_End = false;
                    ShunterSoundToTrainReset();
                    ShunterSoundSTPReset();
                    ShunterProcessTimer = 0;
                    Locomotive.Simulator.DistanceToOtherTrain = -1000;
                    DistanceToOtherTrain_0 = -1000;
                    Locomotive.Simulator.OtherTrainPositionTest = false;
                    ShunterSoundDonePlayed = false;
                }

                if (Locomotive.Simulator.ShunterProcessSTPActive_Start && TRAINAHEAD_Mode)
                {
                    Locomotive.Simulator.ShunterDecideMarker = "";
                    Locomotive.Simulator.ShunterProcessSTPActive_Start = false;
                    Locomotive.Simulator.ShunterProcessSTPActive_End = false;                    
                    ShunterSoundSTPReset();
                }

                // Dokončení procesu najetí k bodu stopu
                float DistanceSTPCorrectionM = 0;
                if (Locomotive.Simulator.ShunterProcessSTPActive_Start)
                {
                    ShunterSoundStartPlayed = true;

                    if ((ShunterSoundStopSTPPlayed || Locomotive.Train.nextRouteReady) && Locomotive.AbsSpeedMpS < 0.01f)
                    {
                        // U reversu ukončí navádění až se obrátí cesta
                        if (Locomotive.Simulator.ShunterDecideMarker == Simulator.Catalog.GetString("REVERS"))
                        {
                            if (Locomotive.Train.nextRouteReady)
                                Locomotive.Simulator.ShunterProcessSTPActive_End = true;
                            else
                                goto SkipShunterDecideMarker;
                        }
                        else
                            Locomotive.Simulator.ShunterProcessSTPActive_End = true;
                    }

                    switch (DistanceToSTP)
                    {
                        case float n when n == DistanceToSIGNAL:
                            Locomotive.Simulator.ShunterDecideMarker = Simulator.Catalog.GetString("SIGNAL");
                            DistanceSTPCorrectionM = 10;
                            break;
                        case float n when n == DistanceToEOA:
                            Locomotive.Simulator.ShunterDecideMarker = Simulator.Catalog.GetString("END TRACK");
                            DistanceSTPCorrectionM = 10;
                            break;
                        case float n when n == DistanceToReverse:
                            Locomotive.Simulator.ShunterDecideMarker = Simulator.Catalog.GetString("REVERS");
                            break;
                        case float n when n == DistanceToTriggerOnStop:
                            Locomotive.Simulator.ShunterDecideMarker = Simulator.Catalog.GetString("TRIGGER");
                            break;
                    }

                    SkipShunterDecideMarker:;

                }
                if (Locomotive.Simulator.ShunterProcessSTPActive_End)
                {
                    ShunterProcessTimer += elapsedClockSeconds;
                    if (ShunterProcessTimer > 2.5f)
                    {
                        Locomotive.Simulator.ShunterProcessSTPActive_Start = false;
                        Locomotive.Simulator.ShunterProcessSTPActive_End = false;
                        ShunterProcessTimer = 0;
                        ShunterSoundSTPReset();
                    }
                }

                // Načtení vzdálenosti od napojovaného vlaku
                // Při změně kabiny lokomotivy se provede test, jestli je napojovaný vlak před námi nebo za námi.
                if ((TRAINAHEAD_Mode && Locomotive.Simulator.DistanceToOtherTrain == -1000) || Locomotive.Simulator.ChangeCabActivated || Locomotive.Simulator.LocoStationChange)
                {
                    Locomotive.Simulator.DistanceToOtherTrain = DistanceToOtherTrain;
                    DistanceToOtherTrain_0 = Locomotive.Simulator.DistanceToOtherTrain;
                    LastDistanceToOtherTrain = Locomotive.Simulator.DistanceToOtherTrain;
                    Locomotive.Simulator.OtherTrainPositionTest = false;                    
                }

                // Dokončení procesu najetí na vlak                                                    
                if (Locomotive.Simulator.ShunterProcessTrainActive_Start)
                {
                    ShunterSoundStartPlayed = true;
                    Locomotive.Simulator.ShunterDecideMarker = Simulator.Catalog.GetString("TRAIN AHEAD");
                    if (Locomotive.Train.ControlMode != Train.TRAIN_CONTROL.EXPLORER)
                    {
                        // Určení, jestli je napojovaný vlak před námi nebo za námi, pro správné odpočítávání vzdálenosti od jiného vlaku pro dokončení procesu najetí na vlak.                    
                        if (!Locomotive.Simulator.OtherTrainPositionTest && TRAINAHEAD_Mode && Locomotive.SpeedMpS != 0 && DistanceToOtherTrain_0 != DistanceToOtherTrain)
                        {
                            int CurrentDistanceToOtherTrain = (int)DistanceToOtherTrain;
                            int BaseDistanceToOtherTrain = (int)DistanceToOtherTrain_0;
                            if (Locomotive.SpeedMpS > 0)
                            {
                                if (BaseDistanceToOtherTrain > CurrentDistanceToOtherTrain)
                                {
                                    Locomotive.Simulator.OtherTrainIsFront = true;
                                    Locomotive.Simulator.OtherTrainPositionTest = true;
                                }

                                if (BaseDistanceToOtherTrain < CurrentDistanceToOtherTrain)
                                {
                                    Locomotive.Simulator.OtherTrainIsFront = false;
                                    Locomotive.Simulator.OtherTrainPositionTest = true;
                                }
                            }
                            if (Locomotive.SpeedMpS < 0)
                            {
                                if (BaseDistanceToOtherTrain > CurrentDistanceToOtherTrain)
                                {
                                    Locomotive.Simulator.OtherTrainIsFront = false;
                                    Locomotive.Simulator.OtherTrainPositionTest = true;
                                }
                                if (BaseDistanceToOtherTrain < CurrentDistanceToOtherTrain)
                                {
                                    Locomotive.Simulator.OtherTrainIsFront = true;
                                    Locomotive.Simulator.OtherTrainPositionTest = true;
                                }
                            }
                            if (!Locomotive.Simulator.OtherTrainPositionTest) return;
                        }

                        // Kalibrace vzdálenosti
                        if (TRAINAHEAD_Mode && DistanceToOtherTrain > 1 && DistanceToOtherTrain < 300)
                            Locomotive.Simulator.DistanceToOtherTrain = DistanceToOtherTrain;

                        // Odpočítávání vzdálenosti od napojovaného vlaku pro dokončení procesu najetí na vlak                                                                                
                        if (Locomotive.Simulator.OtherTrainIsFront) // Napojovaný vlak před námi
                        {
                            if (Locomotive.SpeedMpS > 0)
                                Locomotive.Simulator.DistanceToOtherTrain -= Locomotive.AbsSpeedMpS * elapsedClockSeconds;
                            if (Locomotive.SpeedMpS < 0)
                                Locomotive.Simulator.DistanceToOtherTrain += Locomotive.AbsSpeedMpS * elapsedClockSeconds;
                        }
                        else // Napojovaný vlak za námi
                        {
                            if (Locomotive.SpeedMpS > 0)
                                Locomotive.Simulator.DistanceToOtherTrain += Locomotive.AbsSpeedMpS * elapsedClockSeconds;
                            if (Locomotive.SpeedMpS < 0)
                                Locomotive.Simulator.DistanceToOtherTrain -= Locomotive.AbsSpeedMpS * elapsedClockSeconds;
                        }
                        //Locomotive.Simulator.Confirmer.Information("Locomotive.Simulator.DistanceToOtherTrain: " + Locomotive.Simulator.DistanceToOtherTrain + "   Locomotive.Simulator.OtherTrainIsFront: " + Locomotive.Simulator.OtherTrainIsFront);
                    }

                    if (ShunterSoundDonePlayed && Locomotive.AbsSpeedMpS < 0.01f)
                    {
                        Locomotive.Simulator.DistanceToOtherTrain = -1000;
                        DistanceToOtherTrain_0 = -1000;
                        Locomotive.Simulator.ShunterProcessTrainActive_End = true;
                    }
                }
                if (Locomotive.Simulator.ShunterProcessTrainActive_End)
                {
                    ShunterProcessTimer += elapsedClockSeconds;
                    if (ShunterProcessTimer > 2.5f)
                    {
                        Locomotive.Simulator.ShunterProcessTrainActive_Start = false;
                        Locomotive.Simulator.ShunterProcessTrainActive_End = false;
                        Locomotive.Simulator.OtherTrainPositionTest = false;
                        ShunterSoundDonePlayed = false;
                        ShunterProcessTimer = 0;
                        ShunterSoundToTrainReset();
                    }
                }

                #region ShunterSound                

                if (!Locomotive.Simulator.ShunterProcessTrainActive_Start)
                {
                    if (DistanceToSTP > 175) ShunterSoundSTPReset();
                    // Hlášky posunovače podle vzdálenosti od stopu                
                    if (Locomotive.Simulator.CabRadioOn && !TRAINAHEAD_Mode && DistanceToSTP < 1000)
                    {
                        if (!Locomotive.Simulator.ShunterProcessSTPActive_Start) ShunterTimer = 0;
                        Locomotive.Simulator.ShunterProcessSTPActive_Start = true;

                        if (LastDistanceToSTPTrain < DistanceToSTP) // Pokud se vzdálenost od stopu zvětšuje, hlášky se resetují 
                        {
                            LastDistanceToSTPTrain = DistanceToSTP;
                            ShunterSoundSTPReset();
                            return;
                        }
                        LastDistanceToSTPTrain = DistanceToSTP;

                        // Hláška "Posunuj" obecná
                        if (Locomotive.AbsSpeedMpS > 15f / 3.6f)
                        {
                            bool DistanceToOtherTrainIsValid = false;
                            DistanceToOtherTrainIsValid = DistanceToSTP > 200 && DistanceToSTP < 1000;

                            if (DistanceToOtherTrainIsValid)
                            {
                                ShunterTimerRandom = ShunterTimer == 0 ? Simulator.Random.Next(8, 15) : ShunterTimerRandom;
                                ShunterTimer += elapsedClockSeconds;
                            }
                            if (!ShunterSoundOff && ShunterTimer > ShunterTimerRandom && DistanceToOtherTrainIsValid)
                            {
                                ShunterSoundOff = true;
                                ShunterTimer = 0;
                                if (Locomotive == (Locomotive as MSTSWagon).FirstCarHeadOfTrain)
                                    Locomotive.SignalEvent(Event.ShunterSound_ShuntF);
                                else
                                    Locomotive.SignalEvent(Event.ShunterSound_ShuntB);
                            }
                            else
                                ShunterSoundOff = false;
                        }
                        else
                            ShunterSoundOff = false;
                        
                        if (DistanceToSTP > 149 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM && DistanceToSTP < 150 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM)
                        {
                            if (!ShunterSoundSlowToSTPPlayed) Locomotive.SignalEvent(Event.ShunterSound_Slow);
                            ShunterSoundSlowToSTPPlayed = true;
                        }
                        if (DistanceToSTP > 49 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM && DistanceToSTP < 50 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM)
                        {
                            if (!ShunterSoundNearToSTPPlayed) Locomotive.SignalEvent(Event.ShunterSound_NearSTP);
                            ShunterSoundNearToSTPPlayed = true;
                        }
                        if (DistanceToSTP > 19 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM && DistanceToSTP < 20 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM)
                        {
                            if (!ShunterSoundSlowNearToSTPPlayed) Locomotive.SignalEvent(Event.ShunterSound_SlowNearSTP);
                            ShunterSoundSlowNearToSTPPlayed = true;
                        }
                        if (DistanceToSTP > 1 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM && DistanceToSTP < 2 + DistanceSpeedCorrectionM + DistanceSTPCorrectionM)
                        {
                            if (!ShunterSoundStopSTPPlayed) Locomotive.SignalEvent(Event.ShunterSound_StopSTP);
                            ShunterSoundStopSTPPlayed = true;
                        }
                    }
                }

                if (!Locomotive.Simulator.ShunterProcessSTPActive_Start && !ShunterSoundDonePlayed)
                {
                    // Hlášky posunovače podle vzdálenosti od jiného vlaku
                    if ((Locomotive.Simulator.CabRadioOn && TRAINAHEAD_Mode) || Locomotive.Simulator.ShunterProcessTrainActive_Start)
                    {
                        Locomotive.Simulator.ShunterProcessTrainActive_Start = true;

                        if (LastDistanceToOtherTrain < Locomotive.Simulator.DistanceToOtherTrain) // Pokud se vzdálenost od jiného vlaku zvětšuje, hlášky se resetují 
                        {
                            LastDistanceToOtherTrain = Locomotive.Simulator.DistanceToOtherTrain;
                            ShunterSoundToTrainReset();
                            if (Locomotive.Simulator.DistanceToOtherTrain > 250) // Pokud se vzdálenost od jiného vlaku zvětšuje a je větší než 250 metrů, proces najetí na vlak se ukončí
                            {
                                Locomotive.Simulator.DistanceToOtherTrain = -1000;
                                DistanceToOtherTrain_0 = -1000;
                                Locomotive.Simulator.ShunterProcessTrainActive_End = true;
                                ShunterProcessTimer = 5;
                            }
                            return;
                        }                        

                        if (LastDistanceToOtherTrain == Locomotive.Simulator.DistanceToOtherTrain)
                            return;

                        LastDistanceToOtherTrain = Locomotive.Simulator.DistanceToOtherTrain;

                        // Hláška "Posunuj" obecná
                        if (Locomotive.AbsSpeedMpS > 5f / 3.6f)
                        {
                            bool DistanceToOtherTrainIsValid = false;
                            DistanceToOtherTrainIsValid = Locomotive.Simulator.DistanceToOtherTrain > 300 && Locomotive.Simulator.DistanceToOtherTrain < 1000;

                            ShunterTimerRandom = ShunterTimer == 0 ? Simulator.Random.Next(8, 15) : ShunterTimerRandom;
                            ShunterTimer += elapsedClockSeconds;
                            if (!ShunterSoundOff && ShunterTimer > ShunterTimerRandom && DistanceToOtherTrainIsValid)
                            {
                                ShunterSoundOff = true;
                                ShunterTimer = 0;
                                if (Locomotive == (Locomotive as MSTSWagon).FirstCarHeadOfTrain)
                                    Locomotive.SignalEvent(Event.ShunterSound_ShuntF);
                                else
                                    Locomotive.SignalEvent(Event.ShunterSound_ShuntB);
                            }
                            else
                                ShunterSoundOff = false;
                        }
                        else
                            ShunterSoundOff = false;

                        TouchingDistanceTimer += elapsedClockSeconds;
                        if (TouchingDistanceTimer > 1.0f) TouchingDistanceTimer = 0.0f; 

                        // Odměřovací hlášky posunovače podle vzdálenosti od jiného vlaku
                        if (!ShunterSoundOff)
                        {
                            switch (Locomotive.Simulator.DistanceToOtherTrain)
                            {
                                case float n when (n > 1000):
                                    ShunterSoundToTrainReset();
                                    break;
                                case float n when (n < 250 + DistanceSpeedCorrectionM && n > 225 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound250Played) Locomotive.SignalEvent(Event.ShunterSound_250);
                                    ShunterSound250Played = true;
                                    ShunterSound200Played = false;
                                    break;
                                case float n when (n < 200 + DistanceSpeedCorrectionM && n > 175 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound200Played) Locomotive.SignalEvent(Event.ShunterSound_200);
                                    ShunterSound200Played = true;
                                    ShunterSound150Played = false;
                                    break;
                                case float n when (n < 150 + DistanceSpeedCorrectionM && n > 125 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound150Played) Locomotive.SignalEvent(Event.ShunterSound_150);
                                    ShunterSound150Played = true;
                                    ShunterSound100Played = false;
                                    break;
                                case float n when (n < 100 + DistanceSpeedCorrectionM && n > 90 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound100Played) Locomotive.SignalEvent(Event.ShunterSound_100);
                                    ShunterSound100Played = true;
                                    ShunterSound80Played = false;
                                    break;
                                case float n when (n < 80 + DistanceSpeedCorrectionM && n > 65 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound80Played) Locomotive.SignalEvent(Event.ShunterSound_80);
                                    ShunterSound80Played = true;
                                    ShunterSound50Played = false;
                                    break;
                                case float n when (n < 50 + DistanceSpeedCorrectionM && n > 40 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound50Played) Locomotive.SignalEvent(Event.ShunterSound_50);
                                    ShunterSound50Played = true;
                                    ShunterSound30Played = false;
                                    break;
                                case float n when (n < 30 + DistanceSpeedCorrectionM && n > 25 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound30Played) Locomotive.SignalEvent(Event.ShunterSound_30);
                                    ShunterSound30Played = true;
                                    ShunterSound20Played = false;
                                    break;
                                case float n when (n < 20 + DistanceSpeedCorrectionM && n > 17.5f + DistanceSpeedCorrectionM):
                                    if (!ShunterSound20Played) Locomotive.SignalEvent(Event.ShunterSound_20);
                                    ShunterSound20Played = true;
                                    ShunterSound15Played = false;
                                    break;
                                case float n when (n < 15 + DistanceSpeedCorrectionM && n > 12.5 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound15Played) Locomotive.SignalEvent(Event.ShunterSound_15);
                                    ShunterSound15Played = true;
                                    ShunterSound10Played = false;
                                    break;
                                case float n when (n < 10 + DistanceSpeedCorrectionM && n > 9 + DistanceSpeedCorrectionM):
                                    if (!ShunterSound10Played) Locomotive.SignalEvent(Event.ShunterSound_10);
                                    ShunterSound10Played = true;
                                    ShunterSoundSlowPlayed = false;
                                    break;
                                case float n when (n < 8 + DistanceSpeedCorrectionM && n > 6.5f + DistanceSpeedCorrectionM):
                                    if (!ShunterSoundSlowPlayed) Locomotive.SignalEvent(Event.ShunterSound_Slow);
                                    ShunterSoundSlowPlayed = true;
                                    ShunterSound5Played = false;
                                    break;
                                case float n when (n < 5 + DistanceSpeedCorrectionM && n > 4.5f + DistanceSpeedCorrectionM && TouchingDistanceTimer == 0.0f):
                                    if (!ShunterSound5Played) Locomotive.SignalEvent(Event.ShunterSound_5);
                                    ShunterSound5Played = true;
                                    ShunterSound4Played = false;
                                    break;
                                case float n when (n < 4 + DistanceSpeedCorrectionM && n > 3.5f + DistanceSpeedCorrectionM && TouchingDistanceTimer > 0.9f):
                                    if (!ShunterSound4Played) Locomotive.SignalEvent(Event.ShunterSound_4);
                                    ShunterSound5Played = true;
                                    ShunterSound4Played = true;
                                    ShunterSound3Played = false;
                                    break;
                                case float n when (n < 3 + DistanceSpeedCorrectionM && n > 2.5f + DistanceSpeedCorrectionM && TouchingDistanceTimer == 0.0f):
                                    if (!ShunterSound3Played) Locomotive.SignalEvent(Event.ShunterSound_3);
                                    ShunterSound5Played = true;
                                    ShunterSound4Played = true;
                                    ShunterSound3Played = true;
                                    ShunterSound2Played = false;
                                    break;
                                case float n when (n < 2 + DistanceSpeedCorrectionM && n > 1.5f + DistanceSpeedCorrectionM && TouchingDistanceTimer > 0.9f):
                                    if (!ShunterSound2Played) Locomotive.SignalEvent(Event.ShunterSound_2);
                                    ShunterSound5Played = true;
                                    ShunterSound4Played = true;
                                    ShunterSound3Played = true;
                                    ShunterSound2Played = true;
                                    ShunterSound1Played = false;
                                    break;
                                case float n when (n < 1 + DistanceSpeedCorrectionM && n > 0.75f + DistanceSpeedCorrectionM && TouchingDistanceTimer == 0.0f):
                                    if (!ShunterSound1Played) Locomotive.SignalEvent(Event.ShunterSound_1);
                                    ShunterSound5Played = true;
                                    ShunterSound4Played = true;
                                    ShunterSound3Played = true;
                                    ShunterSound2Played = true;
                                    ShunterSound1Played = true;
                                    ShunterSoundSlowlyPlayed = false;
                                    break;
                                case float n when (n < 0.5f + DistanceSpeedCorrectionM && n > 0.25f + DistanceSpeedCorrectionM && TouchingDistanceTimer > 0.9f):
                                    if (!ShunterSoundSlowlyPlayed) Locomotive.SignalEvent(Event.ShunterSound_Slowly);
                                    ShunterSound5Played = true;
                                    ShunterSound4Played = true;
                                    ShunterSound3Played = true;
                                    ShunterSound2Played = true;
                                    ShunterSound1Played = true;
                                    ShunterSoundSlowlyPlayed = true;
                                    ShunterSoundDonePlayed = false;
                                    break;
                                case float n when (n < 0.0 + DistanceSpeedCorrectionM && n > -0.10f + DistanceSpeedCorrectionM):
                                    if (!ShunterSoundDonePlayed) Locomotive.SignalEvent(Event.ShunterSound_Done);
                                    ShunterSoundDonePlayed = true;
                                    break;
                            }
                        }
                    }
                }
                #endregion ShunterSound
            }
            else
            {
                ShunterSoundToTrainReset();
                ShunterSoundSTPReset();
                ShunterSoundStartPlayed = false;
                Locomotive.Simulator.ShunterProcessSTPActive_Start = false;
                Locomotive.Simulator.ShunterProcessSTPActive_End = false;
                Locomotive.Simulator.ShunterProcessTrainActive_Start = false;
                Locomotive.Simulator.ShunterProcessTrainActive_End = false;
                ShunterProcessTimer = 0;
                Locomotive.Simulator.DistanceToOtherTrain = -1000;
                DistanceToOtherTrain_0 = -1000;
                Locomotive.Simulator.OtherTrainPositionTest = false;
                ShunterSoundDonePlayed = false;                
                Locomotive.Simulator.ShunterDecideMarker = "";
            }
        }
        #endregion Shunter        

        public void ShunterSoundToTrainReset()
        {
            ShunterSound250Played = false;
            ShunterSound200Played = false;
            ShunterSound150Played = false;
            ShunterSound100Played = false;
            ShunterSound80Played = false;
            ShunterSound50Played = false;
            ShunterSound30Played = false;
            ShunterSound20Played = false;
            ShunterSound15Played = false;
            ShunterSound10Played = false;
            ShunterSoundSlowPlayed = false;
            ShunterSound5Played = false;
            ShunterSound4Played = false;
            ShunterSound3Played = false;
            ShunterSound2Played = false;
            ShunterSound1Played = false;
            ShunterSoundSlowlyPlayed = false;
            ShunterTimer = 0;
        }

        public void ShunterSoundSTPReset()
        {
            ShunterSoundSlowToSTPPlayed = false;
            ShunterSoundNearToSTPPlayed = false;
            ShunterSoundReversePlayed = false;
            ShunterSoundSlowNearToSTPPlayed = false;
            ShunterSoundStopSTPPlayed = false;
        }        

        public void TestCarReset()
        {
            foreach (TrainCar car in Locomotive.Train.Cars.Where(car => car.ShunterTestingBrake))
            {
                car.ShunterTestingBrake = false;
            }            
            FirstBoggie = false;
            SecondBoggie = false;
            ShunterCarTestBrakePhase1 = false;
            ShunterCarTestBrakePhase2 = false;
            ShunterCarTestBrakePhase1Timer = 0;
            ShunterCarTestBrakePhase2Timer = 0;
            ShunterCarTestBrakePhase1CarCheckTimer = 0;
            ShunterCarTestBrakePhase2CarCheckTimer = 0;
        }

        public void SetCarMode(TrainCar Wagon)
        {
            TrainCar wagon = Wagon;

            if (wagon.WagonType == WagonTypes.Freight)
            {
                switch (wagon.WagonNumAxles)
                {
                    case int n when n < 4:
                        if (wagon.MassKG > 10000)
                        {
                            wagon.BrakeSystem.BrakeCarModePL = 1;
                            wagon.BrakeSystem.BrakeCarModeTextPL = Simulator.Catalog.GetString("Loaded");
                        }
                        else
                        {
                            wagon.BrakeSystem.BrakeCarModePL = 0;
                            wagon.BrakeSystem.BrakeCarModeTextPL = Simulator.Catalog.GetString("Empty");
                        }
                        break;

                    case int n when n >= 4:
                        if (wagon.MassKG > 40000)
                        {
                            wagon.BrakeSystem.BrakeCarModePL = 1;
                            wagon.BrakeSystem.BrakeCarModeTextPL = Simulator.Catalog.GetString("Loaded");
                        }
                        else
                        {
                            wagon.BrakeSystem.BrakeCarModePL = 0;
                            wagon.BrakeSystem.BrakeCarModeTextPL = Simulator.Catalog.GetString("Empty");
                        }
                        break;
                }
            }
            
            if (wagon.WagonType == WagonTypes.Passenger && wagon.MassKG >= 37000)
            {
                if (wagon.IsPlayerTrain && wagon.Simulator.LeadLocomotive is MSTSSteamLocomotive) return;
                if (!wagon.IsPlayerTrain && wagon.Simulator.AILeadLocomotive is MSTSSteamLocomotive) return;

                switch (wagon.WagonNumAxles)
                {
                    case int n when n < 4:
                        wagon.BrakeSystem.BrakeCarMode = 1;
                        wagon.BrakeSystem.BrakeCarModeText = "P";
                        break;

                    case int n when n >= 4:
                        if (wagon.BrakeSystem.BrakeCarMode != 3)
                        {
                            wagon.BrakeSystem.BrakeCarMode = 2;
                            wagon.BrakeSystem.BrakeCarModeText = "R";
                        }
                        break;
                }
            }
            
        }
    }
}
