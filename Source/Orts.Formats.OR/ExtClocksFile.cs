﻿// COPYRIGHT 2018 by the Open Rails project.
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
using System.Collections.Generic;
using System.IO;


namespace Orts.Formats.OR
{
    public class ExtClockFile
    {
        public ExtClockFile(string filePath, string shapePath, List<ClockList> clockLists)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                var clockBlock = new ClockBlock(stf, shapePath, clockLists, "Default");
            }
        }
    }

    public class ClockList
    {
        public string[] shapeNames; //clock shape names
        public string[] clockType;  //second parameter of the ClockItem is the OR-ClockType -> analog, digital
        public string ListName;
        public ClockList(List<ClockItemData> clockDataItems, string listName)
        {
            shapeNames = new string[clockDataItems.Count];
            clockType = new string[clockDataItems.Count];
            ListName = listName;
            int i = 0;
            foreach (ClockItemData data in clockDataItems)
            {
                shapeNames[i] = data.name;
                clockType[i] = data.clockType;
                i++;
            }
        }
    }

    public class ClockBlock
    {
        public ClockBlock(STFReader stf, string shapePath, List<ClockList> clockLists, string listName)
        {
            var clockDataItems = new List<ClockItemData>();
            {
                var count = stf.ReadInt(null);
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("clockitem", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra ClockItem");
                        else
                        {
                            var dataItem = new ClockItemData(stf, shapePath);
                            if (File.Exists(dataItem.name))
                                clockDataItems.Add(dataItem);
                            else
                                STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem.name));
                        }
                    }),
                });
                if (count > 0)
                    STFException.TraceWarning(stf, count + " missing ClockItem(s)");
            }
            ClockList clockList = new ClockList(clockDataItems, listName);
            clockLists.Add(clockList);
        }

    }

    public class ClockItemData
    {
        public string name;                                    //sFile of OR-Clock
        public string clockType;                               //Type of OR-Clock -> analog, digital
        public ClockItemData(STFReader stf, string shapePath)
        {
            stf.MustMatch("(");
            name = shapePath + stf.ReadString();
            clockType = stf.ReadString();
            stf.SkipRestOfBlock();
        }

    }

}
