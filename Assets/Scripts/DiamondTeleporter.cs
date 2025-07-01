using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Valve.VR.Extras;

public class DiamondTeleporter : MonoBehaviour
{
    public Transform cameraRig;
    private SteamVR_LaserPointer laserPointer; 
    void Awake()
    {
        laserPointer = GetComponent<SteamVR_LaserPointer>();
        laserPointer.PointerClick += OnPointerClick;
        laserPointer.PointerIn += OnPointerIn;
        laserPointer.PointerOut += OnPointerOut; 
    }

    private void OnPointerClick(object sender, PointerEventArgs e)
    {
        if (e.target.CompareTag("TeleportTarget"))
        {
            Vector3 destination = e.target.transform.position;
            destination.y = cameraRig.transform.position.y; //Preserve current height 
            cameraRig.transform.position = destination;
        }
    }

    private void OnPointerOut(object sender, PointerEventArgs e)
    {
        if (e.target.CompareTag("TeleportTarget"))
        {
            Debug.Log("Stoped pointing to a diamond");
        }
    }

    private void OnPointerIn(object sender, PointerEventArgs e)
    {
        if (e.target.CompareTag("TeleportTarget"))
        {
            Debug.Log("Started pointing to a diamond");
        }
    }


    private void OnDestroy()
    {
        if(laserPointer != null)
        {
            laserPointer.PointerClick -= OnPointerClick;
        }
    }
}
