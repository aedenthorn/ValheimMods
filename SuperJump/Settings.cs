using UnityModManagerNet;
namespace SuperJump
{
    public class Settings : UnityModManager.ModSettings
    {
        public int MaxJumps = 2;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}