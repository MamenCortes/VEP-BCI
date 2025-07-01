using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class NegateTracking : MonoBehaviour
{
    private Vector3 initialPosition; 
    private Quaternion initialRotation;
    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
    }
}
