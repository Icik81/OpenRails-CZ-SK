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
using System.Windows.Forms;
using Event = Orts.Common.Event;


// Řídící jednotka pro dálkové řízení lokomotivy

namespace Orts.Simulation.RollingStocks.SubSystems
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
        bool ShunterSoundNearToReversePlayed;
        bool ShunterSoundReversePlayed;
        bool ShunterSoundSlowNearToReversePlayed;
        bool ShunterSoundStopReversePlayed;
        float ShunterTimer;
        bool ShunterSoundOff;
        float LastDistanceToOtherTrain;
        float LastDistanceToReverseTrain;
        float ShunterProcessTimer;
        float DistanceToOtherTrain_0 = -1000;
        float ShunterDecideProcessTimer;
        bool ShunterDecideProcess;
        string ShunterDecideMarker;

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
        float[] ShunterTestBrakeCarCheckTime = new float[100];        
        int CarNumber = 1;
        int LastCarConnectedNumber;
        TrainCar LastCarConnected;

        public void Update(float elapsedClockSeconds)
        {
            if (!Locomotive.IsLeadLocomotive()) return;

            #region ShunterFullTestBrake
            if (Locomotive.Simulator.ShunterFullTestBrakeEnable && !Locomotive.Simulator.PlayerLocomotiveChange)
            {
                CabRadioCheck(elapsedClockSeconds);

                LastCarConnectedNumber = 0;
                LastCarConnected = null;
                foreach (TrainCar car in Locomotive.Train.Cars.Where(car => car.CarHasBrakePipeConnected))
                {
                    if (car is MSTSLocomotive && (car as MSTSLocomotive).IsLeadLocomotive()) { }
                    else
                    {
                        LastCarConnectedNumber++;
                        LastCarConnected = car;
                    }
                }

                if (Locomotive.AbsSpeedMpS > 0.01f || Locomotive.Train.Cars.Count == 1 || LastCarConnected == null)
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
                    if (LastCarConnected == null) return;
                }

                if (!ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase5
                    && Locomotive.AbsSpeedMpS < 0.01f && Locomotive.Train.Cars.Count > 1)
                {
                    ShunterFullTestBrakePhase1 = true;
                    Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Full test brake activated. Please follow the instructions."));
                    Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_Start);
                }

                // Požadavek pro fázi 1: Aplikovat brzdy pod 4.5 bar
                if (ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase5)
                {
                    if (ShunterFullTestBrakePhase1Timer > 4.5f && ShunterFullTestBrakePhase1Timer < 5.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please apply the brakes under 4.5 bar and holdon!"));
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_ApplyBrake);
                    }

                    ShunterFullTestBrakePhase1Timer += elapsedClockSeconds;
                    if (ShunterFullTestBrakePhase1Timer > 5.0f + 30.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.5f * 14.50377f)
                    {
                        ShunterFullTestBrakePhase1Timer = 5.0f;
                    }

                    if (!ShunterFullTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.5f * 14.50377f && ShunterFullTestBrakePhase1Timer > 5.0f)
                    {
                        ShunterFullTestBrakePhase1 = false;
                        ShunterFullTestBrakePhase2 = true;
                        ShunterFullTestBrakePhase2Timer = 0;
                        CarNumber = 1;
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_FirstSide);
                    }
                }

                if (ShunterFullTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.5f * 14.50377f)
                {
                    ShunterFullTestBrakePhase2 = false;
                }

                // Požadavek pro fázi 2: Kontrola aplikace brzd od první vozu k poslednímu
                if (ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase5)
                {
                    float ShunterFullTestBrakePhase2Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive)))
                    {
                        CarTimeNumber++;
                        ShunterFullTestBrakePhase2Time += 2.0f + (car.CarLengthM / 2.0f);
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = 2.0f + (car.CarLengthM / 2.0f);                        
                    }

                    ShunterFullTestBrakePhase2Timer += elapsedClockSeconds;

                    if (ShunterFullTestBrakePhase2Timer > 5.0f)
                    {
                        ShunterFullTestBrakePhase2CarCheckTimer += elapsedClockSeconds;
                    }

                    if (ShunterFullTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber <= CarTimeNumber)
                    {
                        ShunterFullTestBrakePhase2CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber++;
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

                    if (!ShunterFullTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI > 4.9f * 14.50377f && ShunterFullTestBrakePhase3Timer > 5.0f)
                    {
                        ShunterFullTestBrakePhase3 = false;
                        ShunterFullTestBrakePhase4 = true;
                        ShunterFullTestBrakePhase4Timer = 0;                        
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_SecondSide);
                    }
                }

                if (ShunterFullTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI < 4.9f * 14.50377f)
                {
                    ShunterFullTestBrakePhase4 = false;
                }

                // Požadavek pro fázi 4: Kontrola uvolnění brzd od posledního vozu k prvnímu
                if (ShunterFullTestBrakePhase4 && !ShunterFullTestBrakePhase1 && !ShunterFullTestBrakePhase2 && !ShunterFullTestBrakePhase3 && !ShunterFullTestBrakePhase5)
                {
                    float ShunterFullTestBrakePhase4Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive)))
                    {
                        CarTimeNumber++;
                        ShunterFullTestBrakePhase4Time += 2.0f + (car.CarLengthM / 2.0f);
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = 2.0f + (car.CarLengthM / 2.0f);                        
                    }

                    ShunterFullTestBrakePhase4Timer += elapsedClockSeconds;
                    if (ShunterFullTestBrakePhase4Timer > 5.0f)
                    {
                        ShunterFullTestBrakePhase4CarCheckTimer += elapsedClockSeconds;
                    }

                    if (ShunterFullTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber > 0)
                    {
                        ShunterFullTestBrakePhase4CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber--;
                    }

                    if (ShunterFullTestBrakePhase4Timer > ShunterFullTestBrakePhase4Time + 5.0f)
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
                    if (ShunterFullTestBrakePhase5Timer > 5.0f && ShunterFullTestBrakePhase5Timer < 5.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Full test brake completed successfully!"));
                        Locomotive.Simulator.FullTestBrakeWindow = true;
                        
                        string TestBrakeWindowMessageProblemCars = "";
                        string TestBrakeWindowMessageNotConnectedCars = "";
                        string TestBrakeWindowMessage1 = "";
                        string TestBrakeWindowMessage2 = "";
                        string TestBrakeWindowMessage3 = "";

                        bool BrakeProblemFound = false;
                        bool ConnectProblemFound = false;
                        CarNumber = 0;
                        foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive)))
                        {
                            CarNumber++;
                            car.BrakeCarStatus();
                            car.ShunterTestBrakeDone = true;
                            if (car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake || car.BrakeSystem.BrakeCarDeactivate)
                            {
                                TestBrakeWindowMessageProblemCars += car.WagonName + " - " + car.CarID + "\n";
                                car.BrakeSystem.BrakeCarDeactivate = true;
                                BrakeProblemFound = true;
                            }
                            if (!car.CarHasBrakePipeConnected)
                            {
                                TestBrakeWindowMessageNotConnectedCars += car.WagonName + " - " + car.CarID + "\n";
                                ConnectProblemFound = true;
                            }                            
                        }
                        TestBrakeWindowMessage1 = Simulator.Catalog.GetString("Full test brake completed successfully!");
                        if (BrakeProblemFound)
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("These Cars have problems with brake and brake was deactivated:");
                        else
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("No problem with brake found in the train!");

                        if (ConnectProblemFound)
                            TestBrakeWindowMessage3 = Simulator.Catalog.GetString("These Cars are not connected:");
                        else
                            TestBrakeWindowMessage3 = Simulator.Catalog.GetString("All Cars are connected!");

                        Locomotive.Simulator.TestBrakeWindowMessage = TestBrakeWindowMessage1 + "\n\n" + TestBrakeWindowMessage2 + "\n\n" + TestBrakeWindowMessageProblemCars + "\n\n" + TestBrakeWindowMessage3 + "\n\n" + TestBrakeWindowMessageNotConnectedCars;
                        Locomotive.SignalEvent(Event.ShunterFullTestBrakeSound_Completed);
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
            }
            #endregion ShunterFullTestBrake

            #region ShunterSimpleTestBrake
            if (Locomotive.Simulator.ShunterSimpleTestBrakeEnable && !Locomotive.Simulator.PlayerLocomotiveChange)
            {
                CabRadioCheck(elapsedClockSeconds);

                LastCarConnectedNumber = 0;
                LastCarConnected = null;
                foreach (TrainCar car in Locomotive.Train.Cars.Where(car => car.CarHasBrakePipeConnected))
                {
                    if (car is MSTSLocomotive && (car as MSTSLocomotive).IsLeadLocomotive()) { }
                    else
                    {
                        LastCarConnectedNumber++;
                        LastCarConnected = car;
                    }
                }

                if (Locomotive.AbsSpeedMpS > 0.01f || Locomotive.Train.Cars.Count == 1 || LastCarConnected == null)
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
                    if (LastCarConnected == null) return;
                }

                if (!ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase5
                    && Locomotive.AbsSpeedMpS < 0.01f && Locomotive.Train.Cars.Count > 1)
                {
                    ShunterSimpleTestBrakePhase1 = true;
                    Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Simple test brake activated. Please follow the instructions."));
                    Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_Start);
                }

                // Požadavek pro fázi 1: Aplikovat brzdy pod 4.5 bar
                if (ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase5)
                {
                    if (ShunterSimpleTestBrakePhase1Timer > 4.5f && ShunterSimpleTestBrakePhase1Timer < 5.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Please apply the brakes under 4.5 bar and holdon!"));
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_ApplyBrake);
                    }

                    ShunterSimpleTestBrakePhase1Timer += elapsedClockSeconds;
                    if (ShunterSimpleTestBrakePhase1Timer > 5.0f + 30.0f && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.5f * 14.50377f)
                    {
                        ShunterSimpleTestBrakePhase1Timer = 5.0f;
                    }

                    if (!ShunterSimpleTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI < 4.5f * 14.50377f && ShunterSimpleTestBrakePhase1Timer > 5.0f)
                    {
                        ShunterSimpleTestBrakePhase1 = false;
                        ShunterSimpleTestBrakePhase2 = true;
                        ShunterSimpleTestBrakePhase2Timer = 0;
                        CarNumber = 1;
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_FirstSide);
                    }
                }

                if (ShunterSimpleTestBrakePhase2 && Locomotive.BrakeSystem.BrakeLine1PressurePSI > 4.5f * 14.50377f)
                {
                    ShunterSimpleTestBrakePhase2 = false;
                }

                // Požadavek pro fázi 2: Kontrola aplikace brzd od první vozu k poslednímu
                if (ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase5)
                {
                    float ShunterSimpleTestBrakePhase2Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive)))
                    {
                        if (CarTimeNumber == 1) break;
                        CarTimeNumber++;                        
                        ShunterSimpleTestBrakePhase2Time += 2.0f + (car.CarLengthM / 2.0f);
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = 2.0f + (car.CarLengthM / 2.0f);                        
                    }

                    ShunterSimpleTestBrakePhase2Timer += elapsedClockSeconds;

                    if (ShunterSimpleTestBrakePhase2Timer > 5.0f)
                    {
                        ShunterSimpleTestBrakePhase2CarCheckTimer += elapsedClockSeconds;
                    }

                    if (ShunterSimpleTestBrakePhase2CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber <= CarTimeNumber)
                    {
                        ShunterSimpleTestBrakePhase2CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber++;
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

                    if (!ShunterSimpleTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI > 4.9f * 14.50377f && ShunterSimpleTestBrakePhase3Timer > 5.0f)
                    {
                        ShunterSimpleTestBrakePhase3 = false;
                        ShunterSimpleTestBrakePhase4 = true;
                        ShunterSimpleTestBrakePhase4Timer = 0;                        
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_SecondSide);
                    }
                }

                if (ShunterSimpleTestBrakePhase4 && LastCarConnected.BrakeSystem.BrakeLine1PressurePSI < 4.9f * 14.50377f)
                {
                    ShunterSimpleTestBrakePhase4 = false;
                }

                // Požadavek pro fázi 4: Kontrola uvolnění brzd od posledního vozu k prvnímu
                if (ShunterSimpleTestBrakePhase4 && !ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase5)
                {
                    float ShunterSimpleTestBrakePhase4Time = 5.0f;
                    int CarTimeNumber = 0;
                    foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive)))
                    {
                        if (CarTimeNumber == 1) break;
                        CarTimeNumber++;
                        ShunterSimpleTestBrakePhase4Time += 2.0f + (car.CarLengthM / 2.0f);
                        ShunterTestBrakeCarCheckTime[CarTimeNumber] = 2.0f + (car.CarLengthM / 2.0f);                        
                    }

                    ShunterSimpleTestBrakePhase4Timer += elapsedClockSeconds;
                    if (ShunterSimpleTestBrakePhase4Timer > 5.0f)
                    {
                        ShunterSimpleTestBrakePhase4CarCheckTimer += elapsedClockSeconds;
                    }

                    if (ShunterSimpleTestBrakePhase4CarCheckTimer > ShunterTestBrakeCarCheckTime[CarNumber] && CarNumber > 0)
                    {
                        ShunterSimpleTestBrakePhase4CarCheckTimer = 0;
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Car ") + CarNumber + Simulator.Catalog.GetString(" brake check done."));
                        CarNumber--;
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

                // Fáze 5: Generuje informaci o vlaku
                if (ShunterSimpleTestBrakePhase5 && !ShunterSimpleTestBrakePhase1 && !ShunterSimpleTestBrakePhase2 && !ShunterSimpleTestBrakePhase3 && !ShunterSimpleTestBrakePhase4)
                {
                    ShunterSimpleTestBrakePhase5Timer += elapsedClockSeconds;
                    if (ShunterSimpleTestBrakePhase5Timer > 5.0f && ShunterSimpleTestBrakePhase5Timer < 5.5f)
                    {
                        Locomotive.Simulator.Confirmer.MSG(Simulator.Catalog.GetString("Shunter: Simple test brake completed successfully!"));
                        Locomotive.Simulator.SimpleTestBrakeWindow = true;

                        string TestBrakeWindowMessageProblemCars = "";
                        string TestBrakeWindowMessageNotConnectedCars = "";
                        string TestBrakeWindowMessage1 = "";
                        string TestBrakeWindowMessage2 = "";
                        string TestBrakeWindowMessage3 = "";

                        bool BrakeProblemFound = false;
                        bool ConnectProblemFound = false;
                        CarNumber = 0;
                        foreach (TrainCar car in Locomotive.Train.Cars.Where(car => !(car is MSTSLocomotive)))
                        {
                            CarNumber++;
                            if (CarNumber > 1) break;
                            car.BrakeCarStatus();
                            car.ShunterTestBrakeDone = true;
                            if (car.BrakesStuck || car.BrakeSystem.CarHasProblemWithBrake || car.BrakeSystem.BrakeCarDeactivate)
                            {
                                TestBrakeWindowMessageProblemCars += car.WagonName + " - " + car.CarID + "\n";
                                car.BrakeSystem.BrakeCarDeactivate = true;
                                BrakeProblemFound = true;
                            }
                            if (!car.CarHasBrakePipeConnected)
                            {
                                TestBrakeWindowMessageNotConnectedCars += car.WagonName + " - " + car.CarID + "\n";
                                ConnectProblemFound = true;
                            }                                                        
                        }
                        TestBrakeWindowMessage1 = Simulator.Catalog.GetString("Simple test brake completed successfully!");
                        if (BrakeProblemFound)
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("This Car has problem with brake and brake was deactivated:");
                        else
                            TestBrakeWindowMessage2 = Simulator.Catalog.GetString("No problem with brake found!");

                        if (ConnectProblemFound)
                            TestBrakeWindowMessage3 = Simulator.Catalog.GetString("This Car is not connected:");                        

                        Locomotive.Simulator.TestBrakeWindowMessage = TestBrakeWindowMessage1 + "\n\n" + TestBrakeWindowMessage2 + "\n\n" + TestBrakeWindowMessageProblemCars + "\n\n" + TestBrakeWindowMessage3 + "\n\n" + TestBrakeWindowMessageNotConnectedCars;
                        Locomotive.SignalEvent(Event.ShunterSimpleTestBrakeSound_Completed);
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
            float DistanceToReverse = -1000;
            if (Locomotive.Simulator.ShunterEnable && !Locomotive.Simulator.PlayerLocomotiveChange)
            {
                CabRadioCheck(elapsedClockSeconds);

                TRAINAHEAD_Mode = Locomotive.Train.EndAuthorityType[0] == Train.END_AUTHORITY.TRAIN_AHEAD ? true : false;
                DistanceToReverse = Locomotive.Simulator.DistanceToReverse < 1 ? -1 : Locomotive.Train.ComputeDistanceToReversalPoint() > 1000 ? -1 : Locomotive.Simulator.DistanceToReverse;
                float DistanceSpeedCorrectionM = MathHelper.Clamp(Locomotive.AbsSpeedMpS * 3.6f / 2f, 0, 10.0f); // Korekce vzdálenosti závislé na rychlosti pro aktivaci hlášek                

                if (Locomotive.Simulator.ShunterDecideMarker != ShunterDecideMarker)
                {
                    Locomotive.Simulator.ShunterEnableChanged = true;
                    ShunterDecideMarker = Locomotive.Simulator.ShunterDecideMarker;
                }

                if (Locomotive.Simulator.CabRadioOn)
                {
                    // Prodleva pro rozhodnutí, jestli posunovač je v procesu najetí k reverzu nebo na vlak                
                    if (Locomotive.Simulator.ShunterProcessReverseActive_Start || Locomotive.Simulator.ShunterProcessTrainActive_Start)
                    {
                        ShunterDecideProcess = false;
                    }
                    if (!ShunterDecideProcess && !Locomotive.Simulator.ShunterProcessReverseActive_Start && !Locomotive.Simulator.ShunterProcessReverseActive_End && !Locomotive.Simulator.ShunterProcessTrainActive_Start && !Locomotive.Simulator.ShunterProcessTrainActive_End)
                    {
                        ShunterDecideProcessTimer += elapsedClockSeconds;

                        if (ShunterDecideProcessTimer > 2.0f) Locomotive.Simulator.ShunterDecideMarker = "ok";
                        else
                            if (ShunterDecideProcessTimer > 1.5f) Locomotive.Simulator.ShunterDecideMarker = "|";
                            else
                                if (ShunterDecideProcessTimer > 1.0f) Locomotive.Simulator.ShunterDecideMarker = "-";
                                else
                                    if (ShunterDecideProcessTimer > 0.5f) Locomotive.Simulator.ShunterDecideMarker = "|";
                                    else
                                        if (ShunterDecideProcessTimer > 0.0f) Locomotive.Simulator.ShunterDecideMarker = "-";

                        if (ShunterDecideProcessTimer > 2.5f)
                        {
                            ShunterDecideProcess = true;
                            ShunterDecideProcessTimer = 0;
                        }
                        else
                            return;
                    }
                }

                // Pokud je posunovač v režimu "vlak před námi" a vzdálenost od jiného vlaku je větší než vzdálenost od reverzu o 50 metrů, režim "vlak před námi" se vypne, aby se zabránilo zbytečným hláškám posunovače, když se vlak přibližuje k reverzu a není tam žádný vlak před ním                
                if (DistanceToReverse != -1 && Locomotive.Train.DistanceToEndNodeAuthorityM[0] - Locomotive.Simulator.DistanceToReverse > 10) TRAINAHEAD_Mode = false;

                if (Locomotive.Simulator.DistanceToOtherTrain == 0) TRAINAHEAD_Mode = false;

                // Volná jízda
                if (Locomotive.Train.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
                {
                    Locomotive.Simulator.DistanceToOtherTrain = Locomotive.Simulator.DistanceToTrainMFreeRide;
                    TRAINAHEAD_Mode = true;
                }                

                // Dokončení procesu najetí k bodu obratu
                if (Locomotive.Simulator.ShunterProcessReverseActive_Start)
                {
                    Locomotive.Simulator.ShunterDecideMarker = "REV";
                    if ((ShunterSoundStopReversePlayed && Locomotive.AbsSpeedMpS < 0.01f) || Locomotive.Train.nextRouteReady)
                    {
                        Locomotive.Simulator.DistanceToReverse = -1;
                        Locomotive.Simulator.ShunterProcessReverseActive_End = true;
                    }
                }
                if (Locomotive.Simulator.ShunterProcessReverseActive_End)
                {
                    ShunterProcessTimer += elapsedClockSeconds;
                    if (ShunterProcessTimer > 2.5f)
                    {
                        Locomotive.Simulator.ShunterProcessReverseActive_Start = false;
                        Locomotive.Simulator.ShunterProcessReverseActive_End = false;
                        ShunterProcessTimer = 0;
                        ShunterSoundReverseReset();
                    }
                }

                // Načtení vzdálenosti od napojovaného vlaku
                // Při změně kabiny lokomotivy se provede test, jestli je napojovaný vlak před námi nebo za námi.
                if ((TRAINAHEAD_Mode && Locomotive.Simulator.DistanceToOtherTrain == -1000) || Locomotive.Simulator.ChangeCabActivated || Locomotive.Simulator.LocoStationChange)
                {
                    Locomotive.Simulator.DistanceToOtherTrain = Locomotive.Train.DistanceToEndNodeAuthorityM[0];
                    DistanceToOtherTrain_0 = Locomotive.Simulator.DistanceToOtherTrain;
                    LastDistanceToOtherTrain = Locomotive.Simulator.DistanceToOtherTrain;
                    Locomotive.Simulator.OtherTrainPositionTest = false;
                    Locomotive.Simulator.DistanceToReverse = -1;
                }

                // Dokončení procesu najetí na vlak                                                    
                if (Locomotive.Simulator.ShunterProcessTrainActive_Start)
                {
                    Locomotive.Simulator.ShunterDecideMarker = "TRAH";
                    if (Locomotive.Train.ControlMode != Train.TRAIN_CONTROL.EXPLORER)
                    {
                        // Určení, jestli je napojovaný vlak před námi nebo za námi, pro správné odpočítávání vzdálenosti od jiného vlaku pro dokončení procesu najetí na vlak.                    
                        if (!Locomotive.Simulator.OtherTrainPositionTest && TRAINAHEAD_Mode && Locomotive.SpeedMpS != 0 && DistanceToOtherTrain_0 != Locomotive.Train.DistanceToEndNodeAuthorityM[0])
                        {
                            int CurrentDistanceToOtherTrain = (int)Locomotive.Train.DistanceToEndNodeAuthorityM[0];
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
                        if (TRAINAHEAD_Mode && Locomotive.Train.DistanceToEndNodeAuthorityM[0] > 1 && Locomotive.Train.DistanceToEndNodeAuthorityM[0] < 300)
                            Locomotive.Simulator.DistanceToOtherTrain = Locomotive.Train.DistanceToEndNodeAuthorityM[0];

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
                    if (DistanceToReverse > 100) ShunterSoundReverseReset();
                    // Hlášky posunovače podle vzdálenosti od reverzu                
                    if (Locomotive.Simulator.CabRadioOn && !TRAINAHEAD_Mode && DistanceToReverse > -1)
                    {
                        Locomotive.Simulator.ShunterProcessReverseActive_Start = true;

                        if (LastDistanceToReverseTrain < DistanceToReverse) // Pokud se vzdálenost od reverzu zvětšuje, hlášky se resetují 
                        {
                            LastDistanceToReverseTrain = DistanceToReverse;
                            ShunterSoundReverseReset();
                            return;
                        }
                        LastDistanceToReverseTrain = DistanceToReverse;

                        if (DistanceToReverse < 50 + DistanceSpeedCorrectionM)
                        {
                            if (!ShunterSoundNearToReversePlayed) Locomotive.SignalEvent(Event.ShunterSound_NearReverse);
                            ShunterSoundNearToReversePlayed = true;
                        }
                        if (DistanceToReverse < 20 + DistanceSpeedCorrectionM)
                        {
                            if (!ShunterSoundSlowNearToReversePlayed) Locomotive.SignalEvent(Event.ShunterSound_SlowNearReverse);
                            ShunterSoundSlowNearToReversePlayed = true;
                        }
                        if (DistanceToReverse < 2 + DistanceSpeedCorrectionM)
                        {
                            if (!ShunterSoundStopReversePlayed) Locomotive.SignalEvent(Event.ShunterSound_StopReverse);
                            ShunterSoundStopReversePlayed = true;
                        }
                    }
                }

                if (!Locomotive.Simulator.ShunterProcessReverseActive_Start && !ShunterSoundDonePlayed)
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

                        // První hláška posunovače
                        if (Locomotive.AbsSpeedMpS < 0.01f && Locomotive.Simulator.DistanceToOtherTrain > 0)
                        {
                            if (!ShunterSoundStartPlayed) Locomotive.SignalEvent(Event.ShunterSound_Start);
                            ShunterSoundStartPlayed = true;
                        }

                        if (LastDistanceToOtherTrain == Locomotive.Simulator.DistanceToOtherTrain)
                            return;

                        LastDistanceToOtherTrain = Locomotive.Simulator.DistanceToOtherTrain;

                        // Hláška "Posunuj" obecná
                        if (Locomotive.AbsSpeedMpS > 10f / 3.6f)
                        {
                            bool DistanceToOtherTrainIsValid = false;
                            DistanceToOtherTrainIsValid =
                                (Locomotive.Simulator.DistanceToOtherTrain > 220 && Locomotive.Simulator.DistanceToOtherTrain > 230)
                                || (Locomotive.Simulator.DistanceToOtherTrain > 170 && Locomotive.Simulator.DistanceToOtherTrain < 180)
                                || (Locomotive.Simulator.DistanceToOtherTrain > 120 && Locomotive.Simulator.DistanceToOtherTrain < 130);

                            ShunterTimer += elapsedClockSeconds;
                            if (!ShunterSoundOff && ShunterTimer > 10 && DistanceToOtherTrainIsValid)
                            {
                                ShunterSoundOff = true;
                                ShunterTimer = 0;
                                Locomotive.SignalEvent(Event.ShunterSound_Shunt);
                            }
                            else
                                ShunterSoundOff = false;
                        }
                        else
                            ShunterSoundOff = false;

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
                                case float n when (n < 5 + DistanceSpeedCorrectionM && n > 4.5f + DistanceSpeedCorrectionM):
                                    if (!ShunterSound5Played) Locomotive.SignalEvent(Event.ShunterSound_5);
                                    ShunterSound5Played = true;
                                    ShunterSound4Played = false;
                                    break;
                                case float n when (n < 4 + DistanceSpeedCorrectionM && n > 3.5f + DistanceSpeedCorrectionM):
                                    if (!ShunterSound4Played) Locomotive.SignalEvent(Event.ShunterSound_4);
                                    ShunterSound4Played = true;
                                    ShunterSound3Played = false;
                                    break;
                                case float n when (n < 3 + DistanceSpeedCorrectionM && n > 2.5f + DistanceSpeedCorrectionM):
                                    if (!ShunterSound3Played) Locomotive.SignalEvent(Event.ShunterSound_3);
                                    ShunterSound3Played = true;
                                    ShunterSound2Played = false;
                                    break;
                                case float n when (n < 2 + DistanceSpeedCorrectionM && n > 1.5f + DistanceSpeedCorrectionM):
                                    if (!ShunterSound2Played) Locomotive.SignalEvent(Event.ShunterSound_2);
                                    ShunterSound2Played = true;
                                    ShunterSound1Played = false;
                                    break;
                                case float n when (n < 1 + DistanceSpeedCorrectionM && n > 0.75f + DistanceSpeedCorrectionM):
                                    if (!ShunterSound1Played) Locomotive.SignalEvent(Event.ShunterSound_1);
                                    ShunterSound1Played = true;
                                    ShunterSoundSlowlyPlayed = false;
                                    break;
                                case float n when (n < 1 + DistanceSpeedCorrectionM && n > 0.5f + DistanceSpeedCorrectionM):
                                    if (!ShunterSoundSlowlyPlayed) Locomotive.SignalEvent(Event.ShunterSound_Slowly);
                                    ShunterSoundSlowlyPlayed = true;
                                    ShunterSoundDonePlayed = false;
                                    break;
                                case float n when (n <= 0 + DistanceSpeedCorrectionM && n > -1000):
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
                ShunterSoundReverseReset();
                ShunterSoundStartPlayed = false;
                Locomotive.Simulator.ShunterProcessReverseActive_Start = false;
                Locomotive.Simulator.ShunterProcessReverseActive_End = false;
                Locomotive.Simulator.ShunterProcessTrainActive_Start = false;
                Locomotive.Simulator.ShunterProcessTrainActive_End = false;
                ShunterProcessTimer = 0;
                Locomotive.Simulator.DistanceToOtherTrain = -1000;
                DistanceToOtherTrain_0 = -1000;
                Locomotive.Simulator.OtherTrainPositionTest = false;
                ShunterSoundDonePlayed = false;
                Locomotive.Simulator.DistanceToReverse = -1;
                Locomotive.Simulator.ShunterDecideMarker = "-";
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

        public void ShunterSoundReverseReset()
        {
            ShunterSoundNearToReversePlayed = false;
            ShunterSoundReversePlayed = false;
            ShunterSoundSlowNearToReversePlayed = false;
            ShunterSoundStopReversePlayed = false;
        }

        public void CabRadioCheck(float elapsedClockSeconds)
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
                ShunterSoundReverseReset();
                ShunterSoundStartPlayed = false;
                Locomotive.Simulator.ShunterProcessReverseActive_Start = false;
                Locomotive.Simulator.ShunterProcessReverseActive_End = false;
                Locomotive.Simulator.ShunterProcessTrainActive_Start = false;
                Locomotive.Simulator.ShunterProcessTrainActive_End = false;
                ShunterProcessTimer = 0;
                Locomotive.Simulator.DistanceToOtherTrain = -1000;
                DistanceToOtherTrain_0 = -1000;
                Locomotive.Simulator.OtherTrainPositionTest = false;
                ShunterSoundDonePlayed = false;
                Locomotive.Simulator.DistanceToReverse = -1;
                return;
            }
            else
                ShunterTimeWithOutRadio = 0;
        }
    }
}
