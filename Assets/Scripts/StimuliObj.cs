using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class StimuliObj : MonoBehaviour
{
    //public GameObject gameObject;       // Reference to the object
    public float frequency;             // Hz
    public int frameCount;              // Frames per cycle
    public int frameCounter;            // Frame tracker
    public Vector3 originalScale;       // To reset scale after stimulation
    public Vector3 originalPosition;    // (Optional) if you animate position
    public Quaternion originalRotation; // (Optional) if you animate rotation
    public int index; 
    public override string ToString()
    {
        return "Stimuli at "+transform.localPosition+"; Freq "+frequency;
    }

    public bool isVisible(Vector3 viewport)
    {
        //Vector3 viewport = vrCamera.WorldToViewportPoint(rend.transform.position);
        return viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
        //rend.enabled = inFOV;
    }

    public void setOriginalTransform(Transform transform)
    {
        originalPosition = transform.position; 
        originalScale = transform.localScale;
        originalRotation = transform.rotation;
    }
}
