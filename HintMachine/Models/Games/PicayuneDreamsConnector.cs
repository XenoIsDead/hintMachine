using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HintMachine.Models.GenericConnectors;

namespace HintMachine.Models.Games
{
    public class PicayuneDreamsConnector : IGameConnector
    {
        private ProcessRamWatcher _ram = null;
        private readonly BinaryTarget _game = new BinaryTarget
        {
            Hash = "0675A323FEDD55A728AE76E854C5269DA3E58C620C735C665230680A5498C64E",
            ProcessName = "PICAYUNEDREAMS"
        };


        private  HintQuestCumulative _KillQuest = new HintQuestCumulative
        {
            Name = "Kills",
            GoalValue = 20000,
            GoalIncrease = -1,
            Description = "Every 20000 kills grants a hint."
        };

        public PicayuneDreamsConnector()
        {
            Name = "Picayune Dreams";
            Description = "Picayune Dreams is a roguelike blend of bullet heaven horde survival, and bullet hell gameplay. Adrift across the endless void, you must fend off thousands of nightmarish and otherwordly creatures. Uncover a mysterious and surreal story. And don't forget. You mu [TRANSMISSION INTERRUPTED...]";
            Platform = "PC";
            SupportedVersions.Add("Steam");
            CoverFilename = "nubbysnumberfactory.png";
            Author = "XenoIsDead";

            Quests.Add(_KillQuest);

        }
        protected override bool Connect()
        {
            _ram = new ProcessRamWatcher("PICAYUNEDREAMS");
            // Debug.WriteLine("Hash "+ _ram.GetBinaryHash());
            Debug.WriteLine("BaseAddress " +_ram.BaseAddress);
            return _ram.TryConnect();
        }

        public override void Disconnect()
        {
            _ram = null;
        }

        protected override bool Poll()
        {
            long killsAddress;
            try
            {
                killsAddress = _ram.ResolvePointerPath64(_ram.BaseAddress + 0x015E8690, new int[] { 0x588, 0x2C0, 0x1B0, 0x48, 0x10, 0x630, 0xf30 });
            }
            catch
            {
                Debug.WriteLine("Memory Uninitialized?");
                return false;
            }
                Debug.WriteLine((long)_ram.ReadDouble(killsAddress));
            if (killsAddress != 0)
            {
                _KillQuest.UpdateValue((long)_ram.ReadDouble(killsAddress));
                return true;
            }
            return false;
            
        }
    }
}
