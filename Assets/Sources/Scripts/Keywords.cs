using UnityEngine;

public static class AnimationKeyword
{
    public static int Speed = Animator.StringToHash("Speed");
    public static int Weapon = Animator.StringToHash("Weapon");
    public static int Jump = Animator.StringToHash("Jump");
    public static int JumpInPlace = Animator.StringToHash("Jump_InPlace");
    public static int Attack = Animator.StringToHash("Attack");
    public static int Attack2 = Animator.StringToHash("Attack2");
    public static int Hit = Animator.StringToHash("Hit");
    public static int Death = Animator.StringToHash("Death");
    public static int Swimming = Animator.StringToHash("Swimming");
    public static int Open = Animator.StringToHash("Open"); // chest
    public static int Close = Animator.StringToHash("Close"); // chest
    public static int Respawn = Animator.StringToHash("Respawn");
    public static int IsFPS = Animator.StringToHash("IsFPS");
}

public static class SpawnerKeyword
{
    public const string CANNIBALS = "Cannibal";
    public const string BIRDS = "Bird";
    public const string WILDLIFE = "WildAnimal";
}

public static class TerrainKeyword
{
    public const string ISLAND_SIZE = "IslandSize";
}

public static class SceneKeyword
{
    public const string BED_WARS = "BedWars";

    public const string LOBBY = "Lobby";
    public const string GAME = "Game";
}

public enum WeaponAnimationId
{
    None = 0,
    OneHand = 1,
    Pistol = 2,
    Musket = 3,
    Bow = 4
}