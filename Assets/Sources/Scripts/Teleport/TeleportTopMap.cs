using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using Sources.Scripts.Managers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sources.Scripts.Teleport
{
    public class TeleportTopMap : MonoBehaviour
    {
        [SerializeField] private float heightTeleport;
        [SerializeField] private float heightRaycastHit;

        private void Start()
        {
            CoroutineContainer.Start(Teport());
        }

        private IEnumerator<WaitForSeconds> Teport()
        {
            yield return Yielders.WaitForSeconds(3f);
            // Bit shift the index of the layer (8) to get a bit mask
            int layerMask = 1 << 8;

            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
            layerMask = ~layerMask;
            
            RaycastHit hit;
            // Пересекает ли луч какие-либо объекты, кроме слоя игрока
            if (Physics.Raycast(new Vector3(transform.position.x,transform.position.y + heightRaycastHit ,transform.position.z), transform.TransformDirection(Vector3.down), out hit, Mathf.Infinity))
            {
                transform.Find("Crouch").position  = new Vector3(hit.point.x, hit.point.y + heightTeleport, hit.point.z);
                Debug.Log($"Попал : {hit.collider.name}");
            }
            else
            {
                Debug.Log("Не попал");
            }
        }
    }
}