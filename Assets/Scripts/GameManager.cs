using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum Object_Mesh{Diamond, Cube, Arrow}
    public enum Movements{Rotation, Zoom}
    public enum Scenes { Menu, Museum, Training };
    public enum MaterialTexture { Pink, Blue };
    public GameObject player;
    public static event Action<Scenes> OnSceneChangedTo;
    public List<string> sequences = new List<string>();

    //Stimulation
    [HideInInspector]
    //public float[] frequencies = { 5.5f, 6.6f, 7.5f };
    public float[] frequencies = new float[3];
    public Object_Mesh objectMesh;
    public Movements objectMotion;
    public MaterialTexture objectMaterial; 
    public float refreshRate; // VR Headset refresh rate
    public float subjectNum; //subject number
    //TODO: save last subject number?
    public float threshold; //classification threshold
    public float nHarmonics; //number harmonics to classify
    public TMP_Text userText;
    void Start()
    {
        frequencies[0] = 5.5f;
        frequencies[1] = 6.6f;
        frequencies[2] = 7.5f;
        threshold = 0.3f; 
        subjectNum = 0;
        nHarmonics = 2;
        refreshRate = 90f; //VR headset refresh rate
        //Debug.Log("Freqs: "+frequencies[0]+", "+frequencies[1]+", "+frequencies[2]);
        //Debug.Log("Threshold " + threshold); 
        //Set the position of the player
        Vector3 playerpos = Vector3.zero;
        playerpos.y = 1f; 
        player.transform.position = playerpos; 
        Debug.Log("Player transform set at "+ player.transform.position);

        //Set default Mesh and Motion
        objectMesh = Object_Mesh.Diamond; 
        objectMotion = Movements.Zoom;

        //Force offset correction of the headset (doesn't work) 
        Vector3 rigOffset = player.transform.position - Camera.main.transform.position;
        rigOffset.y = 0; //Keep original Y height
        player.transform.position -= rigOffset;

        //set default sequences
        sequences.Add("123123123");
        sequences.Add("321321321");
        sequences.Add("111222333"); 
    }

    // Update is called once per frame
    void Update()
    {        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Game is exiting");
            Application.Quit();

        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Back to the menu");
            ChangeScene(Scenes.Menu);
        }
    }

    //When the application in closed
    private void OnApplicationQuit()
    {
        
        StopAllCoroutines(); 
        
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void ChangeScene(Scenes scene_name) //Change between scenes. Check index in Edit >> Buil Settings >>Scenes In Build
    {
        userText.text = "Wait for experimenter to continue"; 
        if (scene_name == Scenes.Menu)
        {
            SceneManager.LoadScene(0);
        }
        else if (scene_name == Scenes.Museum)
        {
            SceneManager.LoadScene(1);
        }
        else if (scene_name == Scenes.Training)
        {
            SceneManager.LoadScene(2);
        }
        OnSceneChangedTo.Invoke(scene_name);
    }
    
    public void MovePlayerTo(Vector3 destination)
    {
        destination.y = player.transform.position.y; //Preserve current height 
        player.transform.position = destination;
    }
    public void MovePlayerTo(Vector3 destination, Quaternion rotation)
    {
        destination.y = player.transform.position.y; //Preserve current height 
        player.transform.position = destination;
        player.transform.rotation = rotation;
    }

    public static Vector3[] PoligonCalculator(int sides, float radius, float height)
    {
        float angleStep = 2 * Mathf.PI / sides;
        Vector3[] vertices = new Vector3[sides];

        //calculate radius from apothem
        //float radius = apothem/Mathf.Cos(Mathf.PI/sides);
        for (int i = 0; i < sides; i++)
        {
            float angle = i * angleStep;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);
            vertices[i] = new Vector3(x, height, z);
        }
        //Debug.Log(string.Join(",", vertices));

        return vertices;
    }
}
