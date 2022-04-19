// COPYRIGHT 2011, 2012, 2013, 2014 by the Open Rails project.
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

using ORTS.Settings;
using System.Collections.Generic;
using System.IO;

namespace ORTS.Menu
{
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder se nenašel.
    public class Folder
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder se nenašel.
    {
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.Name se nenašel.
        public readonly string Name;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.Name se nenašel.
#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.Path se nenašel.
        public readonly string Path;
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.Path se nenašel.

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.Folder(string, string) se nenašel.
        public Folder(string name, string path)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.Folder(string, string) se nenašel.
        {
            Name = name;
            Path = path;
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.ToString() se nenašel.
        public override string ToString()
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.ToString() se nenašel.
        {
            return Name;
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.GetFolders(UserSettings) se nenašel.
        public static List<Folder> GetFolders(UserSettings settings)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.GetFolders(UserSettings) se nenašel.
        {
            var folderDataFile = UserSettings.UserDataFolder + @"\folder.dat";
            var folders = new List<Folder>();

            if (settings.Folders.Folders.Count == 0 && File.Exists(folderDataFile))
            {
                try
                {
                    using (var inf = new BinaryReader(File.Open(folderDataFile, FileMode.Open)))
                    {
                        var count = inf.ReadInt32();
                        for (var i = 0; i < count; ++i)
                        {
                            var path = inf.ReadString();
                            var name = inf.ReadString();
                            folders.Add(new Folder(name, path));
                        }
                    }
                }
                catch { }

                // Migrate from folder.dat to FolderSettings.
                foreach (var folder in folders)
                    settings.Folders.Folders[folder.Name] = folder.Path;
                settings.Folders.Save();
            }
            else
            {
                foreach (var folder in settings.Folders.Folders)
                    folders.Add(new Folder(folder.Key, folder.Value));
            }

            return folders;
        }

#pragma warning disable CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.SetFolders(UserSettings, List<Folder>) se nenašel.
        public static void SetFolders(UserSettings settings, List<Folder> folders)
#pragma warning restore CS1591 // Komentář XML pro veřejně viditelný typ nebo člen Folder.SetFolders(UserSettings, List<Folder>) se nenašel.
        {
            settings.Folders.Folders.Clear();
            foreach (var folder in folders)
                settings.Folders.Folders[folder.Name] = folder.Path;
            settings.Folders.Save();
        }
    }
}
