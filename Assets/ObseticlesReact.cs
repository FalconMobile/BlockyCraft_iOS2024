using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObseticlesReact : MonoBehaviour
{
    [SerializeField] private Transform target;

    private GameObject camera;
    private void Start()
    {
        camera = GameObject.Find("CameraRotationAnchor");
    }

    private void Update()
    {
        Obsetecles();
    }

    private void Obsetecles()
    {
        // RaycastHit hit;
        //
        // if (Physics.Raycast(target.position, camera.transform.position - target.position, out hit, 10f))
        // {
        //     camera.transform.position = new Vector3(hit.point.x, hit.point.y, hit.point.z);
        // }
    }
}