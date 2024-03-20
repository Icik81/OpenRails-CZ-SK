// COPYRIGHT 2011, 2012 by the Open Rails project.
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

using Orts.Parsers.Msts;
using System;
using System.IO;

// <Comment> This file parses only the shape names for temporary speed restrictions; the other shape names are not needed
// </Comment>
namespace Orts.Formats.Msts
{

    public class SpeedpostDatFileCZSK
    {
        public string[] TempSpeedShapeNamesCZSK = new string[5];
        public string[] TempWarningSpeedShapeNamesCZSK = new string[10];

        public SpeedpostDatFileCZSK(string filePath, string shapePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                // Icik
                // CZ
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("speed_warning_sign_shape_10_cz", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[0] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_20_cz", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[1] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_30_cz", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[2] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_40_cz", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[3] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_50_cz", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[4] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("restricted_shape_cz", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNamesCZSK[1] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("end_restricted_shape_cz", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNamesCZSK[2] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),

                    // SK
                    new STFReader.TokenProcessor("speed_warning_sign_shape_10_sk", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[5] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_20_sk", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[6] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_30_sk", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[7] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_40_sk", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[8] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("speed_warning_sign_shape_50_sk", ()=>
                         {
                             var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempWarningSpeedShapeNamesCZSK[9] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("restricted_shape_sk", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNamesCZSK[3] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("end_restricted_shape_sk", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNamesCZSK[4] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                });
            }
        }

    } // class SpeedpostDatFileCZSK

    public class SpeedpostDatFile
    {
        public string[] TempSpeedShapeNames = new string[3];

        public SpeedpostDatFile(string filePath, string shapePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("speed_warning_sign_shape", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNames[0] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("restricted_shape", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNames[1] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("end_restricted_shape", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNames[2] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                });
            }
        }

    } // class SpeedpostDatFile
}

