// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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
using System.Collections.Generic;
using System.IO;

namespace ORTS.Menu
{
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity se nenašel.
    public class Activity
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity se nenašel.
    {
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Name se nenašel.
        public readonly string Name;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Name se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.ActivityID se nenašel.
        public readonly string ActivityID;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.ActivityID se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Description se nenašel.
        public readonly string Description;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Description se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Briefing se nenašel.
        public readonly string Briefing;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Briefing se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.StartTime se nenašel.
        public readonly StartTime StartTime = new StartTime(10, 0, 0);
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.StartTime se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Season se nenašel.
        public readonly SeasonType Season = SeasonType.Summer;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Season se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Weather se nenašel.
        public readonly WeatherType Weather = WeatherType.Clear;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Weather se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Difficulty se nenašel.
        public readonly Difficulty Difficulty = Difficulty.Easy;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Difficulty se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Duration se nenašel.
        public readonly Duration Duration = new Duration(1, 0);
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Duration se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Consist se nenašel.
        public readonly Consist Consist = new Consist("unknown", null);
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Consist se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Path se nenašel.
        public readonly Path Path = new Path("unknown");
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Path se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.FilePath se nenašel.
        public readonly string FilePath;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.FilePath se nenašel.

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Activity(string, Folder, Route) se nenašel.
        protected Activity(string filePath, Folder folder, Route route)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.Activity(string, Folder, Route) se nenašel.
        {
            if (filePath == null && this is DefaultExploreActivity)
            {
                Name = catalog.GetString("- Explore Route -");
            }
            else if (filePath == null && this is ExploreThroughActivity)
            {
                Name = catalog.GetString("+ Explore in Activity Mode +");
            }
            else if (File.Exists(filePath))
            {
                var showInList = true;
                try
                {
                    var actFile = new ActivityFile(filePath);
                    var srvFile = new ServiceFile(System.IO.Path.Combine(System.IO.Path.Combine(route.Path, "SERVICES"), actFile.Tr_Activity.Tr_Activity_File.Player_Service_Definition.Name + ".srv"));
                    // ITR activities are excluded.
                    if (actFile.Tr_Activity.Tr_Activity_Header.RouteID.ToUpper() == route.RouteID.ToUpper())
                    {
                        Name = actFile.Tr_Activity.Tr_Activity_Header.Name.Trim();
                        if (actFile.Tr_Activity.Tr_Activity_Header.Mode == ActivityMode.IntroductoryTrainRide) Name = "Introductory Train Ride";
                        Description = actFile.Tr_Activity.Tr_Activity_Header.Description;
                        Briefing = actFile.Tr_Activity.Tr_Activity_Header.Briefing;
                        StartTime = actFile.Tr_Activity.Tr_Activity_Header.StartTime;
                        Season = actFile.Tr_Activity.Tr_Activity_Header.Season;
                        Weather = actFile.Tr_Activity.Tr_Activity_Header.Weather;
                        Difficulty = actFile.Tr_Activity.Tr_Activity_Header.Difficulty;
                        Duration = actFile.Tr_Activity.Tr_Activity_Header.Duration;
                        Consist = new Consist(System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "CONSISTS"), srvFile.Train_Config + ".con"), folder);
                        Path = new Path(System.IO.Path.Combine(System.IO.Path.Combine(route.Path, "PATHS"), srvFile.PathID + ".pat"));
                        if (!Path.IsPlayerPath)
                        {
                            // Not nice to throw an error now. Error was originally thrown by new Path(...);
                            throw new InvalidDataException("Not a player path");
                        }
                    }
                    else//Activity and route have different RouteID.
                        Name = "<" + catalog.GetString("Not same route:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                if (!showInList) throw new InvalidDataException(catalog.GetStringFmt("Activity '{0}' is excluded.", filePath));
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                if (string.IsNullOrEmpty(Description)) Description = null;
                if (string.IsNullOrEmpty(Briefing)) Briefing = null;
            }
            else
            {
                Name = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            FilePath = filePath;
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.ToString() se nenašel.
        public override string ToString()
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.ToString() se nenašel.
        {
            return Name;
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.GetActivities(Folder, Route) se nenašel.
        public static List<Activity> GetActivities(Folder folder, Route route)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Activity.GetActivities(Folder, Route) se nenašel.
        {
            var activities = new List<Activity>();
            if (route != null)
            {
                activities.Add(new DefaultExploreActivity());
                activities.Add(new ExploreThroughActivity());
                var directory = System.IO.Path.Combine(route.Path, "ACTIVITIES");
                if (Directory.Exists(directory))
                {
                    foreach (var activityFile in Directory.GetFiles(directory, "*.act"))
                    {
                        try
                        {
                            activities.Add(new Activity(activityFile, folder, route));
                        }
                        catch { }
                    }
                }
            }
            return activities;
        }
    }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity se nenašel.
    public class ExploreActivity : Activity
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity se nenašel.
    {
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.StartTime se nenašel.
        public new string StartTime;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.StartTime se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Season se nenašel.
        public new SeasonType Season = SeasonType.Summer;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Season se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Weather se nenašel.
        public new WeatherType Weather = WeatherType.Clear;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Weather se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Consist se nenašel.
        public new Consist Consist = new Consist("unknown", null);
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Consist se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Path se nenašel.
        public new Path Path = new Path("unknown");
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreActivity.Path se nenašel.

        internal ExploreActivity()
            : base(null, null, null)
        {
        }
    }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen DefaultExploreActivity se nenašel.
    public class DefaultExploreActivity : ExploreActivity
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen DefaultExploreActivity se nenašel.
    { }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreThroughActivity se nenašel.
    public class ExploreThroughActivity : ExploreActivity
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen ExploreThroughActivity se nenašel.
    { }
}
