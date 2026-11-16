using System;
using System.Collections.Generic;

namespace ImprovePlayable
{
    [Serializable]
    public class MonsterConfig
    {
        public string codeName;
        public string displayName;
        public string textureResource;
        public int hp;
        public float speed;
        public int damage;
        public float range;
        public float attackCooldown;
        public string aiType; // Ground, Flying, GroundRanged, FlyingRanged
        public string bulletTexture;
        public float bulletSpeed;
        public float spawnMinY;
        public float spawnMaxY;
        public bool spawnSkyOnly;
    }

    [Serializable]
    public class MonsterConfigContainer
    {
        public List<MonsterConfig> monsters = new List<MonsterConfig>();
    }
}
