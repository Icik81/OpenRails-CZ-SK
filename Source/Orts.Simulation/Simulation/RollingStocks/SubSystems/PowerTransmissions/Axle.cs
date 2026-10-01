// COPYRIGHT 2011 by the Open Rails project.
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
using System.IO;

namespace Orts.Simulation.Simulation.RollingStocks.SubSystems.PowerTransmissions
{
    /// <summary>
    /// Axle drive type to determine an input and solving method for axles
    /// </summary>
    public enum AxleDriveType
    {
        /// <summary>
        /// Without any drive
        /// </summary>
        NotDriven = 0,
        /// <summary>
        /// Traction motor connected through gearbox to axle
        /// </summary>
        MotorDriven = 1,
        /// <summary>
        /// Simple force driven axle
        /// </summary>
        ForceDriven = 2
    }

    /// <summary>
    /// Axle class by Matej Pacha (c)2011, University of Zilina, Slovakia (matej.pacha@kves.uniza.sk)
    /// The class is used to manage and simulate axle forces considering adhesion problems.
    /// Basic configuration:
    ///  - Motor generates motive torque what is converted into a motive force (through gearbox)
    ///    or the motive force is passed directly to the DriveForce property
    ///  - With known TrainSpeed the Update(timeSpan) method computes a dynamic model of the axle
    ///     - additional (optional) parameters are weather conditions and correction parameter
    ///  - Finally an output motive force is stored into the AxleForce
    /// </summary>
    public class Axle
    {
        /// <summary>
        /// Brake force covered by BrakeForceN interface
        /// </summary>
        private float brakeRetardForceN;
        /// <summary>
        /// Read/Write positive only brake force to the axle, in Newtons
        /// </summary>
        public float BrakeRetardForceN
        {
            get => brakeRetardForceN;
            set => brakeRetardForceN = Math.Abs(value);
        }

        /// <summary>
        /// Damping force covered by DampingForceN interface
        /// </summary>
        private float dampingNs;
        /// <summary>
        /// Read/Write positive only damping force to the axle, in Newton-second
        /// </summary>
        public float DampingNs
        {
            get => dampingNs;
            set => dampingNs = Math.Abs(value);
        }

        private float frictionN;

        public float FrictionN
        {
            get => frictionN;
            set
            {
                frictionN = Math.Abs(value);

                if(ExtendedPhysics && IsWheelSlip) frictionN *= 10;
            }
        }

        /// <summary>
        /// Axle drive type covered by DriveType interface
        /// </summary>
        private AxleDriveType driveType;
        /// <summary>
        /// Read/Write Axle drive type flag
        /// </summary>
        public AxleDriveType DriveType
        {
            get => driveType;
            set => driveType = value;
        }

        /// <summary>
        /// Axle drive represented by a motor, covered by ElectricMotor interface
        /// </summary>
        private ElectricMotor motor;
        /// <summary>
        /// Read/Write Motor drive parameter.
        /// With setting a value the totalInertiaKgm2 is updated
        /// </summary>
        public ElectricMotor Motor
        {
            get => motor;
            set
            {
                motor = value;

                switch(driveType)
                {
                    case AxleDriveType.NotDriven:
                        break;
                    case AxleDriveType.MotorDriven:
                        //Total inertia considering gearbox
                        totalInertiaKgm2 = inertiaKgm2 + transmissionRatio * transmissionRatio * motor.InertiaKgm2;
                        break;
                    case AxleDriveType.ForceDriven:
                    default:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                }
                rotationalMassCoef = AxleDiameterM * AxleDiameterM / (4f * totalInertiaKgm2);
            }
        }

        /// <summary>
        /// Drive force covered by DriveForceN interface, in Newtons
        /// </summary>
        private float driveForceN;
        /// <summary>
        /// Read/Write drive force used to pass the force directly to the axle without gearbox, in Newtons
        /// </summary>
        public float DriveForceN
        {
            get => driveForceN;
            set => driveForceN = value;
        }

        /// <summary>
        /// Sum of inertia over all axle conected rotating mass, in kg.m^2
        /// </summary>
        float totalInertiaKgm2;

        /// <summary>
        /// Axle inertia covered by InertiaKgm2 interface, in kg.m^2
        /// </summary>
        float inertiaKgm2;
        /// <summary>
        /// Read/Write positive non zero only axle inertia, in kg.m^2
        /// By setting this parameter the totalInertiaKgm2 is updated
        /// Throws exception when zero or negative value is passed
        /// </summary>
        public float InertiaKgm2
        {
            get => inertiaKgm2;
            set
            {
                if(value <= 0.0) throw new NotSupportedException("Inertia must be greater than zero");
                inertiaKgm2 = value;

                switch(driveType)
                {
                    case AxleDriveType.NotDriven: 
                        break;
                    case AxleDriveType.MotorDriven:
                        totalInertiaKgm2 = inertiaKgm2 + transmissionRatio * transmissionRatio * motor.InertiaKgm2;
                        break;
                    case AxleDriveType.ForceDriven:
                    default:
                        totalInertiaKgm2 = inertiaKgm2;
                        break;
                }
                rotationalMassCoef = AxleDiameterM * AxleDiameterM / (4f * totalInertiaKgm2);
            }
        }
        
        private float rotationalMassCoef;

        /// <summary>
        /// Transmission ratio on gearbox covered by TransmissionRatio interface
        /// </summary>
        float transmissionRatio;
        /// <summary>
        /// Read/Write positive nonzero transmission ratio, given by n1:n2 ratio
        /// Throws an exception when negative or zero value is passed
        /// </summary>
        public float TransmissionRatio
        {
            get => transmissionRatio;
            set
            {
                if(value <= 0.0) throw new NotSupportedException("Transmission ratio must be greater than zero");
                transmissionRatio = value;
            }
        }

        /// <summary>
        /// Transmission efficiency, relative to 1.0, covered by TransmissionEfficiency interface
        /// </summary>
        float transmissionEfficiency;
        /// <summary>
        /// Read/Write transmission efficiency, relative to 1.0, within range of 0.0 to 1.0 (1.0 means 100%, 0.5 means 50%)
        /// Throws an exception when out of range value is passed
        /// When 0.0 is set the value of 0.99 is used instead
        /// </summary>
        public float TransmissionEfficiency
        {
            get => transmissionEfficiency;
            set
            {
                if(value > 1.0f) throw new NotSupportedException("Value must be within the range of 0.0 and 1.0");
                transmissionEfficiency = value <= 0.0f ? 0.99f : value;
            }
        }

        /// <summary>
        /// Axle diameter value, covered by AxleDiameterM interface, in metric meters
        /// </summary>
        private float axleDiameterM = 1.25f;
        /// <summary>
        /// Read/Write nonzero positive axle diameter parameter, in metric meters
        /// Throws exception when zero or negative value is passed
        /// </summary>
        public float AxleDiameterM
        {
            get => axleDiameterM;
            set
            {
                if(value <= 0.0f) throw new NotSupportedException("Axle diameter must be greater than zero");
                axleDiameterM = value;
                rotationalMassCoef = value * value / (4f * totalInertiaKgm2);
            }
        }

        // Icik
        /// <summary>
        /// Umožňuje nastavení součinitele využití adheze (výchozí hodnota 1.00)
        /// </summary>
        public float AdhesionEfficiencyKoef {set; get;}

        public bool ExtendedPhysics {set; get;}
        public float GameSpeed {set; get;}

        /// <summary>
        /// Read/Write adhesion conditions parameter
        /// Should be set within the range of 0.3 to 1.2 but there is no restriction
        /// - Set 1.0 for dry weather (standard)
        /// - Set 0.7 for wet, rainy weather
        /// </summary>
        public float AdhesionConditions {get; set;} = 1;

        /// <summary>
        /// Curtius-Kniffler equation A parameter
        /// </summary>
        public float CurtiusKnifflerA {set; get;}
        /// <summary>
        /// Curtius-Kniffler equation B parameter
        /// </summary>
        public float CurtiusKnifflerB {set; get;}
        /// <summary>
        /// Curtius-Kniffler equation C parameter
        /// </summary>
        public float CurtiusKnifflerC {set; get;}

        /// <summary>
        /// Read/Write correction parameter of adhesion, it has proportional impact on adhesion limit
        /// Should be set to 0.7 for most cases
        /// </summary>
        public float AdhesionK
        {
            get => adhesionK;
            set => adhesionK = (value <= 0 ? 0.7f : value);
        }
        private float adhesionK = 0.7f;

        /// <summary>
        /// Axle speed value, covered by AxleSpeedMpS interface, in metric meters per second
        /// </summary>
        private float axleSpeedMpS;
        /// <summary>
        /// Read only axle speed value, in metric meters per second
        /// </summary>
        public float AxleSpeedMpS
        {
            // used in initialisation at speed > = 0
            get => axleSpeedMpS;
            set => axleSpeedMpS = value;
        }

        /// <summary>
        /// Axle force value, covered by AxleForceN interface, in Newtons
        /// </summary>
        private float axleForceN;
        /// <summary>
        /// Read only axle force value, in Newtons
        /// </summary>
        public float AxleForceN => axleForceN;
        /// <summary>
        /// Read/Write axle weight parameter in Newtons
        /// </summary>
        public float AxleWeightN {set; get;}

        /// <summary>
        /// Read/Write train speed parameter in metric meters per second
        /// </summary>
        public float TrainSpeedMpS {set; get;}

        /// <summary>
        /// Read only wheel slip indicator
        /// - is true when absolute value of SlipSpeedMpS is greater than WheelSlipThresholdMpS, otherwise is false
        /// </summary>
        public bool IsWheelSlip => Math.Abs(SlipSpeedMpS) > WheelSlipThresholdMpS;

        /// <summary>
        /// Read only wheelslip threshold value used to indicate maximal effective slip
        /// - its value si computed as a maximum of slip function:
        ///                 2*K*umax^2 * dV
        ///   f(dV) = u = ---------------------
        ///                umax^2*dV^2 + K^2
        ///   maximum can be found as a derivation f'(dV) = 0
        /// </summary>
        public float WheelSlipThresholdMpS => AdhesionK / (3.6f * GetAdhesionCoef(Math.Abs(TrainSpeedMpS), AdhesionConditions));

        /// <summary>
        /// Read only wheelslip warning indication
        /// - is true when SlipSpeedMpS is greater than zero and 
        ///   SlipSpeedPercent is greater than SlipWarningThresholdPercent in both directions,
        ///   otherwise is false
        /// </summary>
        public bool IsWheelSlipWarning
        {
            get
            {
                var absSlipPercent = Math.Abs(SlipSpeedPercent);

                if(absSlipPercent > SlipWarningTresholdPercent) return true;
                if(absSlipPercent < 0.75f * SlipWarningTresholdPercent) return false;

                return LastStateIsWheelSlipWarning;
            }
        }

        /// <summary>
        /// Read only slip speed value in metric meters per second
        /// - computed as a substraction of axle speed and train speed
        /// </summary>
        public float SlipSpeedMpS => axleSpeedMpS - TrainSpeedMpS;

        /// <summary>
        /// Read only relative slip speed value, in percent
        /// - the value is relative to WheelSlipThreshold value
        /// </summary>
        public float SlipSpeedPercent
        {
            get
            {
                var temp = SlipSpeedMpS / WheelSlipThresholdMpS * 100f;

                if(float.IsNaN(temp)) temp = 0; //avoid NaN on HuD display when first starting OR

                return temp;
            }
        }

        /// <summary>
        /// Slip speed rate of change value, in metric (meters per second) per second
        /// </summary>
        private float slipDerivationMpSS;
        /// <summary>
        /// Slip speed memorized from previous iteration
        /// </summary>
        private float previousSlipSpeedMpS;
        /// <summary>
        /// Read only slip speed rate of change, in metric (meters per second) per second
        /// </summary>
        public float SlipDerivationMpSS => slipDerivationMpSS;

        /// <summary>
        /// Relative slip rate of change
        /// </summary>
        private float slipDerivationPercentpS;
        /// <summary>
        /// Relativ slip speed from previous iteration
        /// </summary>
        private float previousSlipPercent;
        /// <summary>
        /// Read only relative slip speed rate of change, in percent per second
        /// </summary>
        public float SlipDerivationPercentpS => slipDerivationPercentpS;

        /// <summary>
        /// Read/Write relative slip speed warning threshold value, in percent of maximal effective slip
        /// </summary>
        public float SlipWarningTresholdPercent {set; get;} = 70f;

        public int Steps {get; private set;}
        public readonly int MAX_STEPS = 100;
        
        public double ResetTime = 0;
        
        private bool isSiemens = false;
        
        bool LastStateIsWheelSlipWarning;

        /// <summary>
        /// Nonparametric constructor of Axle class instance
        /// - sets motor parameter to null
        /// - sets TtransmissionEfficiency to 0.99 (99%)
        /// - sets SlipWarningThresholdPercent to 70%
        /// - sets axle DriveType to ForceDriven
        /// - updates totalInertiaKgm2 parameter
        /// </summary>
        public Axle()
        {
            transmissionEfficiency = 0.99f;
            driveType = AxleDriveType.ForceDriven;
            totalInertiaKgm2 = inertiaKgm2;
            rotationalMassCoef = AxleDiameterM * AxleDiameterM / (4f * totalInertiaKgm2);
        }

        /// <summary>
        /// Creates motor driven axle class instance
        /// - sets TransmissionEfficiency to 0.99 (99%)
        /// - sets SlipWarningThresholdPercent to 70%
        /// - sets axle DriveType to MotorDriven
        /// - updates totalInertiaKgm2 parameter
        /// </summary>
        /// <param name="electricMotor">Electric motor connected with the axle</param>
        public Axle(ElectricMotor electricMotor)
        {
            motor = electricMotor;
            motor.AxleConnected = this;
            transmissionEfficiency = 0.99f;
            driveType = AxleDriveType.MotorDriven;
            totalInertiaKgm2 = inertiaKgm2 + transmissionRatio * transmissionRatio * motor.InertiaKgm2;
            rotationalMassCoef = AxleDiameterM * AxleDiameterM / (4f * totalInertiaKgm2); 
        }

        /// <summary>
        /// A constructor that restores the game state.
        /// </summary>
        /// <param name="inf">The save stream to read from.</param>
        public Axle(BinaryReader inf): this()
        {
            previousSlipPercent = inf.ReadSingle();
            previousSlipSpeedMpS = inf.ReadSingle();

            // Icik
            CurtiusKnifflerA = inf.ReadSingle();
            CurtiusKnifflerB = inf.ReadSingle();
            CurtiusKnifflerC = inf.ReadSingle();
            AdhesionK = inf.ReadSingle();
            AdhesionConditions = inf.ReadSingle();
            SlipWarningTresholdPercent = inf.ReadSingle();
            AxleSpeedMpS = inf.ReadSingle();
            TrainSpeedMpS = inf.ReadSingle();
            AxleWeightN = inf.ReadSingle();
        }

        /// <summary>
        /// Save the game state.
        /// </summary>
        /// <param name="outf">The save stream to write to.</param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(previousSlipPercent);
            outf.Write(previousSlipSpeedMpS);

            // Icik
            outf.Write(CurtiusKnifflerA);
            outf.Write(CurtiusKnifflerB);
            outf.Write(CurtiusKnifflerC);
            outf.Write(AdhesionK);
            outf.Write(AdhesionConditions);
            outf.Write(SlipWarningTresholdPercent);
            outf.Write(AxleSpeedMpS);
            outf.Write(TrainSpeedMpS);
            outf.Write(AxleWeightN);
        }


        /// <summary>
        /// Restore the game state.
        /// </summary>
        /// <param name="inf">The save stream to read from.</param>
        public void Restore(BinaryReader inf)
        {
            previousSlipPercent = inf.ReadSingle();
            previousSlipSpeedMpS = inf.ReadSingle();

            // Icik
            CurtiusKnifflerA = inf.ReadSingle();
            CurtiusKnifflerB = inf.ReadSingle();
            CurtiusKnifflerC = inf.ReadSingle();
            AdhesionK = inf.ReadSingle();
            AdhesionConditions = inf.ReadSingle();
            SlipWarningTresholdPercent = inf.ReadSingle();
            AxleSpeedMpS = inf.ReadSingle();
            TrainSpeedMpS = inf.ReadSingle();
            AxleWeightN = inf.ReadSingle();
        }

        public virtual void Update(float timeSpan, bool vectron)
        {
            if(vectron) isSiemens = true;
            Update(timeSpan);
        }

        /// <summary>
        /// Main Update method
        /// - computes slip characteristics to get new axle force
        /// - computes axle dynamic model according to its driveType
        /// - computes wheelslip indicators
        /// </summary>
        /// <param name="timeSpan"></param>
        public virtual void Update(float timeSpan)
        {
            if(timeSpan <= 0f)
            {
                Steps = 0;
                return;
            }
            LastStateIsWheelSlipWarning = IsWheelSlipWarning;

            var driveForce = 0f;

            switch(driveType)
            {
                case AxleDriveType.MotorDriven:
                    motor.RevolutionsRad = axleSpeedMpS * 2f * transmissionRatio / axleDiameterM;
                    motor.Update(timeSpan);
                    driveForce = motor.DevelopedTorqueNm * 2f * transmissionRatio / axleDiameterM * transmissionEfficiency;
                    break;
                case AxleDriveType.ForceDriven:
                    driveForce = DriveForceN * transmissionEfficiency;
                    break;
                case AxleDriveType.NotDriven:
                default:
                    driveForce = 0f;
                    break;
            }

            var steps = (int) Math.Min(MAX_STEPS, Math.Ceiling(timeSpan / 0.01));
            var fixedDeltaTime = timeSpan / steps;
            var halfFixedDeltaTime = 0.5f * fixedDeltaTime;
            var axleForceSum = 0f;

            Steps = steps;

            for(var i = 0; i < steps; i++)
            {
                var k1 = CalcAxleDynamics(axleSpeedMpS, driveForce);
                var k2 = CalcAxleDynamics(axleSpeedMpS + k1.acceleration * halfFixedDeltaTime, driveForce);
                var k3 = CalcAxleDynamics(axleSpeedMpS + k2.acceleration * halfFixedDeltaTime, driveForce);
                var k4 = CalcAxleDynamics(axleSpeedMpS + k3.acceleration * fixedDeltaTime, driveForce);

                var acceleration = (k1.acceleration + 2 * k2.acceleration + 2 * k3.acceleration + k4.acceleration) / 6f;
                var newAxleSpeed = axleSpeedMpS + acceleration * fixedDeltaTime;

                var predictedAxleSpeed = axleSpeedMpS + k1.acceleration * fixedDeltaTime;
                var hasPredictedDirectionChange =  Math.Sign(predictedAxleSpeed) != Math.Sign(axleSpeedMpS);
                var hasDirectionChanged = Math.Sign(newAxleSpeed) != Math.Sign(axleSpeedMpS);

                if(axleSpeedMpS != 0 && (hasDirectionChanged || hasPredictedDirectionChange))
                {
                    /*
                     * Check if motive forces would overpower retardation forces at standstill.
                     * If not stop the axle otherwise let it roll in opposite direction.
                     */
                    var (standstillAccel, _) = CalcAxleDynamics(0f, driveForce);

                    if(standstillAccel == 0) newAxleSpeed = 0f;
                }

                axleSpeedMpS = newAxleSpeed;
                axleForceSum += (k1.railForce + 2 * k2.railForce + 2 * k3.railForce + k4.railForce) / 6f;
            }
            axleForceN = axleForceSum / steps;

            if(timeSpan > 0f)
            {
                slipDerivationMpSS = (SlipSpeedMpS - previousSlipSpeedMpS) / timeSpan;
                previousSlipSpeedMpS = SlipSpeedMpS;

                slipDerivationPercentpS = (SlipSpeedPercent - previousSlipPercent) / timeSpan;
                previousSlipPercent = SlipSpeedPercent;
                if(isSiemens)
                {
                    slipDerivationMpSS = 0;
                    slipDerivationPercentpS = 0;
                }
            }
        }

        /// <summary>
        /// Calculates the circumferential acceleration of the axle based on its
        /// circumferential speed and the forces acting on it.
        /// </summary>
        /// <param name="axleSpeed">Axle circumferential speed [m·s⁻¹]</param>
        /// <param name="driveForce">Driving force produced by the motor  [N]</param>
        /// <returns>Axle circumferential acceleration [m·s⁻²] and force transmitted to rails [N]</returns>
        private (float acceleration, float railForce) CalcAxleDynamics(float axleSpeed, float driveForce)
        {
            var slipSpeed = axleSpeed - TrainSpeedMpS;
            // Force transmitted to rails
            var railForce = AxleWeightN * SlipCharacteristics(slipSpeed, TrainSpeedMpS, AdhesionK, AdhesionConditions);
            var retardationForce = BrakeRetardForceN + FrictionN;
            // Sum of force developed by motor and force from rails
            var motiveForce = driveForce - railForce;
            // Sum of all forces on axle
            var totalForce = 0f;            

            if (axleSpeed == 0)
            {
                if(Math.Abs(motiveForce) <= retardationForce) return (0f, railForce);
                totalForce = motiveForce - Math.Sign(motiveForce) * retardationForce;
            }
            else totalForce = motiveForce - Math.Sign(axleSpeed) * retardationForce;

            var acceleration = totalForce * rotationalMassCoef;

            return (acceleration, railForce);
        }

        /// <summary>
        /// Resets all integral values (set to zero)
        /// </summary>
        public void Reset()
        {
            motor?.Reset();
        }

        /// <summary>
        /// Resets all integral values to given initial condition
        /// </summary>
        /// <param name="initValue">Initial condition</param>
        public void Reset(double resetTime, float initValue)
        {
            ResetTime = resetTime;

            if(motor != null) motor.Reset();
        }

        /// <summary>
        /// Calculates adhesion coefficient using Curtius-Kniffler equation.
        /// </summary>
        /// <param name="speed">Vehicle speed [m·s⁻¹]</param>
        /// <param name="conditions">Relative weather conditions, usually from 0.2 to 1.0</param>
        /// <returns></returns>
        private float GetAdhesionCoef(float speed, float conditions) {
            if(conditions == 0f) conditions = 0.75f;

            var speedKmph = 3.6f * speed;

            return conditions * AdhesionEfficiencyKoef *(CurtiusKnifflerA / (speedKmph + CurtiusKnifflerB) + CurtiusKnifflerC);
        }

        /// <summary>
        /// Slip characteristics computation
        /// - Computes adhesion limit using Curtius-Kniffler formula:
        ///                 7.5
        ///     umax = ---------------------  + 0.161
        ///             speed * 3.6 + 44.0
        /// - Computes slip speed
        /// - Computes relative adhesion force as a result of slip characteristics:
        ///             2*K*umax^2*dV
        ///     u = ---------------------
        ///           umax^2*dv^2 + K^2
        /// </summary>
        /// <param name="slipSpeed">Difference between train speed and wheel speed MpS</param>
        /// <param name="speed">Current speed MpS</param>
        /// <param name="inclinationCoef">Slip speed correction.</param>
        /// <param name="conditions">Relative weather conditions, usually from 0.2 to 1.0</param>
        /// <returns>Relative force transmitted to the rail</returns>
        private float SlipCharacteristics(float slipSpeed, float speed, float inclinationCoef, float conditions)
        {
            var adhesionCoef = GetAdhesionCoef(Math.Abs(speed), conditions);

            slipSpeed *= 3.6f;

            var adhesionCoefSq = adhesionCoef * adhesionCoef;
            var slipSpeedSq = slipSpeed * slipSpeed;
            var inclinationCoefSq = inclinationCoef * inclinationCoef;

            return 2f * inclinationCoef * adhesionCoefSq * slipSpeed / (adhesionCoefSq * slipSpeedSq + inclinationCoefSq);
        }
    }
}