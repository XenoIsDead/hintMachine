using HintMachine.Helpers;
using HintMachine.Models.GenericConnectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms.VisualStyles;

namespace HintMachine.Models.Games
{
    public class PicayuneDreamsConnector : IGameConnector
    {
        private ProcessRamWatcher _ram = null;
        private readonly HintQuestCumulative _WinQuest = new HintQuestCumulative
        {
            Name = "Win",
            GoalValue = 1,
            MaxIncrease = 2,
            Description = "Clearing the game grants a hint"
        };
        private readonly HintQuestCumulative _KillQuest = new HintQuestCumulative
        {
            Name = "Kills",
            GoalValue = 10000,
            MaxIncrease = 5,
            Description = "Every 10000 kills grants a hint, up to 5 per run."
        };
        protected List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private string _fileToReadOnNextTick = "";
        protected Dictionary<string, int> _totalWinCounts = new Dictionary<string, int>();
        protected Dictionary<string, int> _totalKillCounts = new Dictionary<string, int>();
        public PicayuneDreamsConnector()
        {
            Name = "Picayune Dreams";
            Description = "Picayune Dreams is a roguelike blend of bullet heaven horde survival, and bullet hell gameplay. Adrift across the endless void, you must fend off thousands of nightmarish and otherwordly creatures. Uncover a mysterious and surreal story. And don't forget. You mu [TRANSMISSION INTERRUPTED...]";
            Platform = "PC";
            SupportedVersions.Add("Steam");
            CoverFilename = "nubbysnumberfactory.png";
            Author = "Stepford, Andyland, milkypossum";

            Quests.Add(_WinQuest);
            Quests.Add(_KillQuest);
        }
        private int ReadTotalWinCount(string pathToFile)
        {
            int totalWinCount = 0;
            //FileStream file = new FileStream(pathToFile, FileMode.Open, FileAccess.Read);
            //using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                // string text = streamReader.ReadToEnd();
                string[] lines = File.ReadAllLines(pathToFile);
                foreach (string line in lines)
                {
                    Debug.WriteLine(line);
                    try
                    {
                        Regex regex = new Regex("=");
                        string[] keyval = regex.Split(line);
                        String stripped = Regex.Replace(keyval[1], @"[^0-9\.]", "");
                        keyval[1] = stripped;
                        Debug.WriteLine(stripped);
                        if (keyval.Length < 2)
                            continue;
                        if (keyval[0].Contains("stat_runs_completed"))
                            totalWinCount += (int)float.Parse(keyval[1]);
                    }
                    catch
                    {
                        Debug.WriteLine("Skipping line");
                    }
                    
                }
            }

            //file.Close();

            return totalWinCount;
        }
        private int ReadTotalKillCount(string pathToFile)
        {
            int totalKillCount = 0;

            //FileStream file = new FileStream(pathToFile, FileMode.Open, FileAccess.Read);
            //using (var streamReader = new StreamReader(file, Encoding.UTF8))
            {
                //string text = streamReader.ReadToEnd();
                string[] lines = File.ReadAllLines(pathToFile);

                foreach (string line in lines)
                {
                    try
                    {
                        Regex regex = new Regex("=");
                        string[] keyval = regex.Split(line);
                        String stripped = Regex.Replace(keyval[1], @"[^0-9\.]", "");
                        keyval[1] = stripped;
                        Debug.WriteLine(stripped);
                        if (keyval.Length < 2)
                            continue;
                        if (keyval[0].Contains("stat_total_kills"))
                            totalKillCount += (int)float.Parse(keyval[1]);
                    }
                    catch
                    {
                        Debug.WriteLine("Skipping Line");
                    }
                }
            }

            //file.Close();

            return totalKillCount;
        }

        protected override bool Connect()
        {
            try
            {
                _ram = new ProcessRamWatcher("PICAYUNEDREAMS");
                if (!_ram.TryConnect())
                    return false;
                // Setup a watcher to be notified when the file is changed
                Debug.WriteLine("Connected");
                string pathToDir = FindSaveData();
                string pathToFile = pathToDir + @"\game.sav";
                FileSystemWatcher watcher = new FileSystemWatcher(pathToDir)
                {
                    Filter = "*.sav",
                    NotifyFilter = NotifyFilters.LastAccess |
                        NotifyFilters.LastWrite |
                        NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                _totalWinCounts[pathToFile] = ReadTotalWinCount(pathToFile);
                _totalKillCounts[pathToFile] = ReadTotalKillCount(pathToFile);
                Debug.WriteLine(pathToFile + " " + _totalWinCounts[pathToFile]);
                Debug.WriteLine(pathToFile + " " + _totalKillCounts[pathToFile]);
                watcher.Changed += new FileSystemEventHandler((object source, FileSystemEventArgs e) => {
                    Debug.WriteLine("Save Changed");
                    _fileToReadOnNextTick = pathToFile;
                });

                _watchers.Add(watcher);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Disconnect()
        {
            foreach (FileSystemWatcher watcher in _watchers)
                watcher.EnableRaisingEvents = false;
            _watchers.Clear();
            _ram = null;
        }

        protected override bool Poll()
        {
            if (!_ram.TestProcess())
                return false;
            if (_fileToReadOnNextTick != "")
            {
                int oldKills = _totalKillCounts[_fileToReadOnNextTick];
                int newKills = ReadTotalKillCount(_fileToReadOnNextTick);
                int oldWins = _totalWinCounts[_fileToReadOnNextTick];
                int newWins = ReadTotalWinCount(_fileToReadOnNextTick);
                Debug.WriteLine("Old = " + oldWins);
                Debug.WriteLine("New = " + newWins);
                if (newWins > oldWins)
                    _WinQuest.CurrentValue += (newWins - oldWins);
                _totalWinCounts[_fileToReadOnNextTick] = newWins;
                if (newKills > oldKills)
                    _KillQuest.CurrentValue += (newKills - oldKills);
                _totalKillCounts[_fileToReadOnNextTick] = newKills;

                _fileToReadOnNextTick = "";
            }
            return true;
        }
        private string FindSaveData()
        {
            string path = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            path += @"\PicayuneDreams";
            Debug.WriteLine(path);
            return path;
        }
    }
}