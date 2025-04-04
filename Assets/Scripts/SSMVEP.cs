using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class SSMVEP_1 : MonoBehaviour
{
    public bool rotate = false;
    public bool move = false;

    // Start is called before the first frame update
    public GameObject obj; 
    public float frequency = 1f;
    public float amplitude = 0.2f; 
    private float toggleInterval;
    private Vector3 initialPosition; 
    public Vector3 axis = Vector3.right;
    void Start()
    {
        toggleInterval = 1f / (2f * frequency); //Half-cycle for on/off
        if (obj == null) { 
            obj = GetComponent<GameObject>();
        }
        initialPosition = transform.position;
        if(move) StartCoroutine(MoveObject());
        if(rotate) StartCoroutine(RotateObject());

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //A coroutine runs 1 per frame if yield null
    IEnumerator MoveObject()
    {
        while (true) { 
            
            float sinwave = Mathf.Sin((Time.time%10.0f)*frequency*Mathf.PI*2f);
            float offset = sinwave * amplitude; 

            //Debug.Log(Time.deltaTime);

            //apply rotation in the z axis
            transform.position = initialPosition + axis * offset;

            //wait for next frame
            yield return null;
        }
    }

    IEnumerator RotateObject()
    {
        while (true)
        {
            //deltaTime is the time between frames. This ensures that the rotation is equal in after every frame
            float rotationAngle = 360f * frequency * Time.deltaTime;

            //Debug.Log(Time.deltaTime);

            //apply rotation in the z axis
            transform.Rotate(Vector3.forward, rotationAngle);

            //wait for next frame
            yield return null;
        }
    }

    public void StopRotation()
    {
        if (RotateObject() != null)
        {
            StopCoroutine(RotateObject());

        }
    }

    //Corrutina para controlar el tiempo de juego 

}
