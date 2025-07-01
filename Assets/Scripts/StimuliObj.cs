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
    public bool InFieldOfView;
    public int index; 

    public TextMeshPro text; 

    //public GameManager.LocationType Type;
    //public int Index; // 0-11 for outer positions

    /// <summary>
    /// Method called immediately when the object is instantiated or loaded
    /// </summary>
    /*private void Awake()
    {
        text = GetComponentInChildren<TextMeshPro>();
        index = 0;
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        frequency = 0;
        frameCount = 0;
        frameCounter = 0;
    }*/
    private void Start()
    {
        if (text != null) text.text = index+" ("+frequency.ToString()+")";
    }
    public virtual void OnSelected()
    {
        Debug.Log("StimuliSelected");
    }
    public override string ToString()
    {
        return "Stimuli at "+transform.localPosition+"; Freq "+frequency+"; Visibility "+InFieldOfView;
    }
    /*private void OnBecameVisible()
    {
        text.text = frequency.ToString();
        InFieldOfView = true;
        //Show in view
        this.GetComponent<Renderer>().enabled = true;
    }
    private void OnBecameInvisible()
    {
        InFieldOfView = false;
        //hide in view
        this.GetComponent<Renderer>().enabled = false; 
    }*/

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
