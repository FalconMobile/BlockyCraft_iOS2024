using Mirror;

namespace IslandAdventureBattleRoyale
{
    public struct CharacterSelectionMessage : NetworkMessage
    {
        public int characterIndex;
    }
}