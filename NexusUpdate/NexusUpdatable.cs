using System;

namespace NexusUpdate
{
    internal class NexusUpdatable
    {
        public string name;
        public int id;
        public Version currentVersion;
        public Version version;

        public NexusUpdatable(string name, int id, Version currentVersion, Version version)
        {
            this.name = name;
            this.id = id;
            this.currentVersion = currentVersion;
            this.version = version;
        }
    }
}