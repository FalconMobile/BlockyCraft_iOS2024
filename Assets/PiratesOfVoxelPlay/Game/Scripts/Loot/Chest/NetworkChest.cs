using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using VoxelPlay;


namespace IslandAdventureBattleRoyale {

    /// <summary>
    /// Controls chest
    /// </summary>
    public class NetworkChest : NetworkBehaviour {

        public Loot[] loot;

        NetworkAnimator networkAnimator;
        bool isOpen;

        public void Start() {

            if (!isServer) {
                enabled = false;
                return;
            }

            isOpen = false;
            networkAnimator = GetComponent<NetworkAnimator>();

        }

        /// <summary>
        /// Called when a player enters the trigger sphere of the pickup
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (isOpen || !isServer) return;
            NetworkPlayer networkPlayer = other.transform.root.GetComponentInChildren<NetworkPlayer>();
            if (networkPlayer != null && networkPlayer.isAlive)
            {
                isOpen = true;
                StartCoroutine(OpenChest());
            }
        }


        IEnumerator OpenChest() {
            isOpen = true;
            networkAnimator.ResetTrigger(AnimationKeyword.Close);
            networkAnimator.SetTrigger(AnimationKeyword.Open);
            yield return new WaitForSeconds(1f);

            for (int k = 0; k < loot.Length; k++)
            {

                if (UnityEngine.Random.value < loot[k].probability)
                {
                    int amount = UnityEngine.Random.Range(loot[k].minAmount, loot[k].maxAmount + 1);
                    if (amount > 0)
                    {
                        ThrowLootItem(new InventoryItem { item = loot[k].item, quantity = amount });
                    }
                }
            }
        }

        public void CloseChest() {
            isOpen = false;
            networkAnimator.ResetTrigger(AnimationKeyword.Open);
            networkAnimator.SetTrigger(AnimationKeyword.Close);
        }

        void ThrowLootItem(InventoryItem inventory) {

            // Get a forward vector and multiply by 2 so we can spawn 2 meters away from chest
            Vector3 positionToDrop = Vector3.forward * 2;

            // Rotate the vector around the Up axis, by a fraction of 360
            float rotation = UnityEngine.Random.Range(0, 360);
            positionToDrop = Quaternion.AngleAxis(rotation, Vector3.up) * positionToDrop;

            // Move up drop position to make items fall to ground
            positionToDrop += Vector3.up * 2f;

            // Create the object
            GameObject droppedItem = Instantiate(inventory.item.prefab, transform.position + positionToDrop, transform.rotation);

            // Set quantity, so that for example we don't spawn 30 arrows, but just 1 with quantity 30
            droppedItem.GetComponent<NetworkItem>().quantity = inventory.quantity;

            // Spawn Gameobject on Server
            NetworkServer.Spawn(droppedItem);
        }

    }
}
