// <copyright company="ROSEN Swiss AG">
//  Copyright (c) ROSEN Swiss AG
//  This computer program includes confidential, proprietary
//  information and is a trade secret of ROSEN. All use,
//  disclosure, or reproduction is prohibited unless authorized in
//  writing by an officer of ROSEN. All Rights Reserved.
// </copyright>

namespace TheaterControl.Interface.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using TheaterControl.Interface.Constants;
    using TheaterControl.Interface.Models;

    internal class FileReadHelper
    {
        #region Fields

        private const string DURATION = "Duration";

        private const string RELATIVE_PATH_DEVICES = "../../Configuration/Devices.txt";

        private const string RELATIVE_PATH_SCENES = "../../Configuration/Scenes.txt";

        private const string RELATIVE_PATH_SONGS = "../../../TheaterControl.MusicPlayer/Music";

        private static readonly List<string> UNITS_TIME = new List<string>
                                                          {
                                                              "s",
                                                              "sec",
                                                              "seconds",
                                                              "m",
                                                              "min",
                                                              "minutes",
                                                              "h",
                                                              "hours"
                                                          };

        #endregion

        #region Methods

        private static IEnumerable<Device> CreateDeviceFromText(IEnumerable<string> rawValues, IEnumerable<Device> available)
        {
            var values = rawValues.Select(x => x.Trim().Split(' ').Select(y => y.ToLower()).ToList());
            var filtered = values.Where(x => available.Any(device => device.Name == x[0]));
            return filtered.Select(
                value => new Device()
                         {
                             Name = value[0], //
                             Value = value[1], //
                             Topic = $"{Topics.BASE_TOPIC}{value[0]}"
                         });
        }

        private static IEnumerable<Scene> CreateScenesFromLines(IEnumerable<string[]> values, IEnumerable<Device> availableDevices)
        {
            return values.Select(
                value => new Scene
                         {
                             Name = value[0], //
                             Id = FileReadHelper.ParseId(value[0]), //
                             Devices = new ObservableCollection<Device>(FileReadHelper.CreateDeviceFromText(value, availableDevices)), //
                             Duration = FileReadHelper.ParseDuration(value)
                         });
        }

        private static int ParseId(string value)
        {
            if(int.TryParse(value.Split(' ')[0], out var res))
            {
                return res;
            }
            throw new InvalidOperationException($"No Id was found in description '{value[0]}'.");
        }
        private static double ParseDuration(IEnumerable<string> value)
        {
            value = value.Select(x => x.Trim());
            var durationSubstring = value.ToList().Find(x => x.StartsWith(FileReadHelper.DURATION))?.Substring(FileReadHelper.DURATION.Length);
            if (durationSubstring == null)
            {
                return default;
            }

            var words = durationSubstring.Split(' ').Select(word => double.TryParse(word, out var result) ? word : word.ToLower()).ToList();
            var duration = words.Find(word => double.TryParse(word, out var result));
            if (duration == null)
            {
                return default;
            }

            var unit = words.Find(word => FileReadHelper.UNITS_TIME.Contains(word));
            if (unit == null)
            {
                return double.Parse(duration) * 1000;
            }

            switch (unit[0])
            {
                case 's':
                    return double.Parse(duration) * 1000;
                case 'm':
                    return double.Parse(duration) * 1000 * 60;
                case 'h':
                    return double.Parse(duration) * 1000 * 60 * 60;
            }

            return default;
        }

        /// <summary>
        /// Tries to read all lines of the specified file and retries on exceptions for 5 seconds.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static IEnumerable<string> Read(string path)
        {
            var timer = new System.Timers.Timer(5000) { Enabled = true };
            timer.Elapsed += (sender, e) => throw new Exception($"Could not find or read file at {path}");
            timer.Start();
            while (true)
            {
                try
                {
                    var lines = File.ReadAllLines(path).Where(line => !line.StartsWith("//") && line != string.Empty);
                    timer.Stop();
                    return lines;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }

        private static IEnumerable<string[]> ReadAllLines(string path)
        {
            var lines = FileReadHelper.Read(path);
            return lines.Select(line => line.TrimEnd(';').Split(';'));
        }

        public static List<Scene> ReadScenesWithDevices()
        {
            var deviceLines = FileReadHelper.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + FileReadHelper.RELATIVE_PATH_DEVICES);
            var sceneLines = FileReadHelper.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + FileReadHelper.RELATIVE_PATH_SCENES);
            var allAvailableDevices = deviceLines.Select(value => new Device { Name = value[0].ToLower() });
            var scenes = FileReadHelper.CreateScenesFromLines(sceneLines, allAvailableDevices).ToList();
            var ids = scenes.Select(x => x.Id).ToList();
            var distinctIds = ids.Distinct();
            if (ids.Count > distinctIds.Count())
            {
                throw new InvalidOperationException("The Ids of scenes have to be unique.");
            }

            return scenes;
        }

        public static List<string> ReadSongNames()
        {
            return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + FileReadHelper.RELATIVE_PATH_SONGS).Select(Path.GetFileName)
                .ToList();
        }

        #endregion
    }
}