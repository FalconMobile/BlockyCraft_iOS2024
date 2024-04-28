using UnityEngine;
using System.Collections.Generic;
using Mirror;
using VoxelPlay;

public partial class WorldState : NetworkBehaviour
{
    private byte[] chunkData;
    private readonly List<VoxelChunk> changedChunks = new List<VoxelChunk>();

    private void OnChunkChangedMethod(VoxelChunk chunk)
    {
        int length = _env.GetChunkRawData(chunk, chunkData);
        RpcSetChunkData(chunk.position, chunkData, length);
    }

#if MIRROR_32_1_2_OR_NEWER
    [ClientRpc(includeOwner = false)]
#endif
    private void RpcSetChunkData(Vector3 chunkPos, byte[] chunkData, int incLength)
    {
        VoxelChunk currentChunk = _env.GetChunk(chunkPos, true);
        _env.SetChunkRawData(currentChunk, chunkData, incLength, true);
        _env.ChunkRedraw(currentChunk, includeNeighbours: true, refreshLightmap: false);
    }

    /// <summary>
    /// Called on player start
    /// </summary>
#if MIRROR_32_1_2_OR_NEWER
    [Command(requiresAuthority = false)]
#endif
    private void CmdRequestChangedChunks(GameObject requester)
    {
        // Get list of changed chunks
        _env.GetChunks(changedChunks, ChunkModifiedFilter.OnlyModified);
        // Loop through it and send the changes
        for (int i = 0; i < changedChunks.Count; i++)
        {
            // Get the changes
            int length = _env.GetChunkRawData(changedChunks[i], chunkData);
            // Get connection information
            NetworkIdentity opponentIdentity = requester.GetComponent<NetworkIdentity>();
            // Send it to requesting client only using TargetRpc
            TargetUpdateChangedChunks(opponentIdentity.connectionToClient, changedChunks[i].position, chunkData, length);
        }
    }

    [TargetRpc]
    private void TargetUpdateChangedChunks(NetworkConnection connectionToRequester, Vector3 chunkPos, byte[] chunkData, int incLength)
    {
        VoxelChunk currentChunk = _env.GetChunk(chunkPos, true);
        _env.SetChunkRawData(currentChunk, chunkData, incLength, true);
        _env.ChunkRedraw(currentChunk, includeNeighbours: true, refreshLightmap: false);
    }
}