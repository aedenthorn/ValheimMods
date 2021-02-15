using UnityEngine;
using UnityModManagerNet;
namespace QuickLoad
{
    public class Settings : UnityModManager.ModSettings
    {
        public int MaxJumps = 2;

        public string HotKey { get; set; } = "f7";

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}