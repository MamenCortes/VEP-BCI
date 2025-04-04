using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    //public static GameManager instance;
    public int fps = 60; 

    void Start()
    {
        //So that the object is not deleted when changing scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        Application.targetFrameRate = fps; //Change and fix the frame rate
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //When the application in closed
    private void OnApplicationQuit()
    {
        StopAllCoroutines();
    }
}
