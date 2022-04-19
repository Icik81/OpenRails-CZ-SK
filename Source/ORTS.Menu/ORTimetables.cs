// COPYRIGHT 2014 by the Open Rails project.
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

using GNU.Gettext;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using System;
using System.Collections.Generic;
using System.IO;

namespace ORTS.Menu
{
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo se nenašel.
    public class TimetableInfo
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo se nenašel.
    {
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.ORTTList se nenašel.
        public readonly List<TimetableFileLite> ORTTList = new List<TimetableFileLite>();
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.ORTTList se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Description se nenašel.
        public readonly String Description;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Description se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.fileName se nenašel.
        public readonly String fileName;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.fileName se nenašel.

        // items set for use as parameters, taken from main menu
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Day se nenašel.
        public int Day;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Day se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Season se nenašel.
        public int Season;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Season se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Weather se nenašel.
        public int Weather;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.Weather se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.WeatherFile se nenašel.
        public String WeatherFile;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.WeatherFile se nenašel.

        // note : file is read preliminary only, extracting description and train information
        // all other information is read only when activity is started

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.TimetableInfo(string) se nenašel.
        protected TimetableInfo(string filePath)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.TimetableInfo(string) se nenašel.
        {
            if (File.Exists(filePath))
            {
                try
                {
                    ORTTList.Add(new TimetableFileLite(filePath));
                    Description = String.Copy(ORTTList[0].Description);
                    fileName = String.Copy(filePath);
                }
                catch
                {
                    Description = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
            }
            else
            {
                Description = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.TimetableInfo(string, string) se nenašel.
        protected TimetableInfo(String filePath, String directory)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.TimetableInfo(string, string) se nenašel.
        {
            if (File.Exists(filePath))
            {
                try
                {
                    TimetableGroupFileLite multiInfo = new TimetableGroupFileLite(filePath, directory);
                    ORTTList = multiInfo.ORTTInfo;
                    Description = String.Copy(multiInfo.Description);
                    fileName = String.Copy(filePath);
                }
                catch
                {
                    Description = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
            }
            else
            {
                Description = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.ToString() se nenašel.
        public override string ToString()
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.ToString() se nenašel.
        {
            return Description;
        }

        // get timetable information
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.GetTimetableInfo(Folder, Route) se nenašel.
        public static List<TimetableInfo> GetTimetableInfo(Folder folder, Route route)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen TimetableInfo.GetTimetableInfo(Folder, Route) se nenašel.
        {
            var ORTTInfo = new List<TimetableInfo>();
            if (route != null)
            {
                var actdirectory = System.IO.Path.Combine(route.Path, "ACTIVITIES");
                var directory = System.IO.Path.Combine(actdirectory, "OPENRAILS");

                if (Directory.Exists(directory))
                {
                    foreach (var ORTimetableFile in Directory.GetFiles(directory, "*.timetable_or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORTimetableFile));
                        }
                        catch { }
                    }

                    foreach (var ORTimetableFile in Directory.GetFiles(directory, "*.timetable-or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORTimetableFile));
                        }
                        catch { }
                    }

                    foreach (var ORMultitimetableFile in Directory.GetFiles(directory, "*.timetablelist_or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORMultitimetableFile, directory));
                        }
                        catch { }
                    }

                    foreach (var ORMultitimetableFile in Directory.GetFiles(directory, "*.timetablelist-or"))
                    {
                        try
                        {
                            ORTTInfo.Add(new TimetableInfo(ORMultitimetableFile, directory));
                        }
                        catch { }
                    }
                }
            }
            return ORTTInfo;
        }
    }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo se nenašel.
    public class WeatherFileInfo
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo se nenašel.
    {
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.filedetails se nenašel.
        public FileInfo filedetails;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.filedetails se nenašel.

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.WeatherFileInfo(string) se nenašel.
        public WeatherFileInfo(string filename)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.WeatherFileInfo(string) se nenašel.
        {
            filedetails = new FileInfo(filename);
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.ToString() se nenašel.
        public override string ToString()
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.ToString() se nenašel.
        {
            return (filedetails.Name);
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.GetFullName() se nenašel.
        public string GetFullName()
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.GetFullName() se nenašel.
        {
            return (filedetails.FullName);
        }

        // get weatherfiles
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.GetTimetableWeatherFiles(Folder, Route) se nenašel.
        public static List<WeatherFileInfo> GetTimetableWeatherFiles(Folder folder, Route route)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen WeatherFileInfo.GetTimetableWeatherFiles(Folder, Route) se nenašel.
        {
            var weatherInfo = new List<WeatherFileInfo>();
            if (route != null)
            {
                var directory = System.IO.Path.Combine(route.Path, "WeatherFiles");

                if (Directory.Exists(directory))
                {
                    foreach (var weatherFile in Directory.GetFiles(directory, "*.weather-or"))
                    {
                        weatherInfo.Add(new WeatherFileInfo(weatherFile));
                    }

                }
            }
            return weatherInfo;
        }
    }
}
