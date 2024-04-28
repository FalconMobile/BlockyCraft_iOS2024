using System;
using UnityEngine;
using VoxelPlay;

[Serializable]
public struct Loot
{
    public ItemDefinition item;
    [Range(0, 1)]
    public float probability;
    public int minAmount;
    public int maxAmount;
}