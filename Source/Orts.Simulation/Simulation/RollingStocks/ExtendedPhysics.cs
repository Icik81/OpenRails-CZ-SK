// COPYRIGHT 2009 - 2021 by the Open Rails project.
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

/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 *  The viewer is a separate class object since there could be multiple 
 *  viewers potentially on different devices for a single car. 
 *  
 */


using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks
{

    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Extended physical motion and behaviour of the car.
    /// </summary>

    public class ExtendedPhysics
    {
        public List<Undercarriage> Undercarriages = new List<Undercarriage>();
        public ExtendedPhysics(TrainCar trainCar)
        {

        }

        public void Parse(string path)
        {
            XmlDocument document = new XmlDocument();
            document.Load(path);
            foreach (XmlNode node in document.ChildNodes)
            {
                if (node.Name == "ExtendedPhysics")
                {
                    foreach (XmlNode main in node.ChildNodes)
                    {
                        if (main.Name.ToLower() == "undercarriage")
                        {
                            Undercarriage undercarriage = new Undercarriage();
                            foreach (XmlNode undercarriageNode in main.ChildNodes)
                            {
                                if (undercarriageNode.Name.ToLower() == "id")
                                    undercarriage.Id = int.Parse(node.Value);
                                if (undercarriageNode.Name.ToLower() == "pivoty")
                                    undercarriage.PivotY = int.Parse(node.Value);
                                if (undercarriageNode.Name.ToLower() == "pivotz")
                                    undercarriage.PivotZ = int.Parse(node.Value);
                                if (undercarriageNode.Name.ToLower() == "axle")
                                {
                                    ExtendedAxle extendedAxle = new ExtendedAxle();
                                    foreach (XmlNode axleNode in undercarriageNode.ChildNodes)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class Undercarriage
    {
        public int Id = -1;
        public int PivotY = 0;
        public int PivotZ = 0;
        public List<ExtendedAxle> Axles = new List<ExtendedAxle>();
        public Undercarriage()
        {
        }
    }

    public class ExtendedAxle
    {
        public List<ElectricMotor> ElectricMotor = new List<ElectricMotor>();
        public int Id = -1;
        public ExtendedAxle()
        {

        }
    }

    public class ElectricMotor
    {
        public int Id = -1;
        public float StatorCurrent = 0;
        public float RotorCurrent = 0;
        public float ForceN = 0;
        protected int inSeriesWith = 1;
        protected float maxRpm = 0;
        protected float gearRatio = 1;
        public ElectricMotor()
        {

        }
    }
}
