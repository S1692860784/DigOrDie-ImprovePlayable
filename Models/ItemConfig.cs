using System;
using System.Collections.Generic;

namespace ImprovePlayable
{
    [Serializable]
    public class MaterialConfig
    {
        public string codeName;
        public string displayName;
        public string description;
        public int maxStack;
        public string textureResource;
    }

    [Serializable]
    public class OreConfig
    {
        public string codeName;
        public string displayName;
        public string description;
        public int health;
        public string textureResource;
        public string dropItemCodeName;
        public int minDrop;
        public int maxDrop;
        public string replaceBlockCodeName;
        public float spawnMinY;
        public float spawnMaxY;
        public int veinSize;
        public float frequency;
    }

    [Serializable]
    public class WeaponConfig
    {
        public string codeName;
        public string displayName;
        public string description;
        public int damage;
        public float fireRate;
        public float bulletSpeed;
        public float critChance;
        public float critMultiplier;
        public string textureResource;
        public string bulletTexture;
    }

    [Serializable]
    public class BlockConfig
    {
        public string codeName;
        public string displayName;
        public string description;
        public int health;
        public int hardness;
        public float weight;
        public string textureResource;
    }

    [Serializable]
    public class DeviceConfig
    {
        public string codeName;
        public string displayName;
        public string description;
        public string deviceType; // Armor, Teleport, Flashlight
        public float customValue;
        public float cooldown;
        public float duration;
    }

    [Serializable]
    public class DefenseConfig
    {
        public string codeName;
        public string displayName;
        public string description;
        public int hp;
        public float rangeDetection;
        public float attackRange;
        public int attackDamage;
        public int attackNb;
        public float attackCooldown;
        public string bulletTexture;
    }

    [Serializable]
    public class MachineConfig
    {
        public string codeName;
        public string displayName;
        public string description;
        public string templateCodeName;
        public int electricValue; // 正数发电，负数耗电 (单位 W，如 5000 为 5kW)
    }

    [Serializable]
    public class ItemsConfigContainer
    {
        public List<MaterialConfig> materials = new List<MaterialConfig>();
        public List<OreConfig> ores = new List<OreConfig>();
        public List<WeaponConfig> weapons = new List<WeaponConfig>();
        public List<BlockConfig> blocks = new List<BlockConfig>();
        public List<DeviceConfig> devices = new List<DeviceConfig>();
        public List<DefenseConfig> defenses = new List<DefenseConfig>();
        public List<MachineConfig> machines = new List<MachineConfig>();
    }

    [Serializable]
    public class RecipeIngredient
    {
        public string itemCodeName;
        public int count;
    }

    [Serializable]
    public class RecipeConfig
    {
        public string resultCodeName;
        public int machineLevel;
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
    }

    [Serializable]
    public class RecipesConfigContainer
    {
        public List<RecipeConfig> recipes = new List<RecipeConfig>();
    }
}
