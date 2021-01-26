using System;

namespace FourRoads.TelligentCommunity.MigratorFramework.Entities
{
    public class MigratedObject
    {

        public MigratedObject()
        {
            State = MigratedObjectState.Unknown;
        }

        public string ObjectType { get; set; }
        public string Key { get; set; }
        public MigratedObjectState State{ get; set; }

    }
}