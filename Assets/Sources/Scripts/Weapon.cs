using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

    public enum WeaponType
    {
        None,
        Bow = 10,
        Pistol = 20,
        Musket = 30,
        MusketBlade = 31,
        Bone = 40,
        Fork = 41,
        SwordCurved = 50,
        SwordStraight = 51,
        Bomb = 60,
        Voxel = 100
    }


    public static class WeaponTypeExtensions
    {
        public static bool IsBow(this WeaponType weaponType)
        {
            return weaponType == WeaponType.Bow;
        }

        public static bool IsPistol(this WeaponType weaponType)
        {
            return weaponType == WeaponType.Pistol;
        }

        public static bool IsMusket(this WeaponType weaponType)
        {
            return weaponType == WeaponType.Musket || weaponType == WeaponType.MusketBlade;
        }

        public static bool IsVoxel(this WeaponType weaponType)
        {
            return weaponType == WeaponType.Voxel;
        }
    }


/// <summary>
/// Weapon utility class
/// </summary>
public static class Weapon
{
    readonly static Dictionary<string, WeaponType> weaponNames = new Dictionary<string, WeaponType>();

    static Weapon()
    {
        weaponNames["None"] = WeaponType.None;
        weaponNames["Arrow"] = WeaponType.Bow;
        weaponNames["Bow"] = WeaponType.Bow;
        weaponNames["Pistol"] = WeaponType.Pistol;
        weaponNames["Musket"] = WeaponType.Musket;
        weaponNames["Musket_Blade"] = WeaponType.MusketBlade;
        weaponNames["Bone"] = WeaponType.Bone;
        weaponNames["Fork"] = WeaponType.Fork;
        weaponNames["Fork"] = WeaponType.Fork;
        weaponNames["Sword_Curved"] = WeaponType.SwordCurved;
        weaponNames["Sword_Straight"] = WeaponType.SwordStraight;
        weaponNames["Voxel"] = WeaponType.Voxel;
        weaponNames["Bomb"] = WeaponType.Bomb;
    }

    public static WeaponType GetWeaponType(string weaponTypeName)
    {
        if (!string.IsNullOrEmpty(weaponTypeName))
        {
            if (weaponNames.TryGetValue(weaponTypeName, out WeaponType t)) return t;
        }
        return WeaponType.None;
    }

    public static WeaponType GetWeaponType(ItemDefinition item)
    {

        if (item == null) return WeaponType.None;
        if (item.category == ItemCategory.Voxel) return WeaponType.Voxel;

        string weaponTypeName = item.GetPropertyValue<string>("weaponType");
        return GetWeaponType(weaponTypeName);
    }

    public static WeaponAnimationId GetWeaponAnimationId(WeaponType weapontype)
    {
        switch (weapontype)
        {
            case WeaponType.Bow:
                return WeaponAnimationId.Bow;
            case WeaponType.Bone:
                return WeaponAnimationId.OneHand;
            case WeaponType.Fork:
                return WeaponAnimationId.OneHand;
            case WeaponType.SwordCurved:
                return WeaponAnimationId.OneHand;
            case WeaponType.SwordStraight:
                return WeaponAnimationId.OneHand;
            case WeaponType.Musket:
            case WeaponType.MusketBlade:
                return WeaponAnimationId.Musket;
            case WeaponType.Pistol:
                return WeaponAnimationId.Pistol;
        }
        return WeaponAnimationId.None;
    }


}