using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
public class TrainingMenuManager : MonoBehaviour
{
    //TODO: add updateView with subjects info method
    [SerializeField]
    private TMP_InputField inTRounds;
    [SerializeField]
    private TMP_InputField inTTime;
    [SerializeField]
    private TMP_InputField inWaitTime;
    [SerializeField]
    private TMP_Text txtError;
    [SerializeField]
    private TMP_Dropdown ddMesh;
    [SerializeField]
    private TMP_Dropdown ddMotion;
    [SerializeField]
    private UnityEngine.UI.Button btStart;
    [SerializeField]
    private UnityEngine.UI.Button btSetScene;
    private GameManager.Object_Mesh selectedMesh;
    private GameManager.Movements selectedMotion;
    private int numTRounds = 1;
    private float tTime = 20f;
    private float waitTime = 5f; 

    //int trounds, float tseconds, float waitfor, GameManager.Object_Mesh mesh, GameManager.Movements movement
    public static event Action<int, float, float, GameManager.Object_Mesh, GameManager.Movements> OnSceneSet;
    public static event Action OnStartTraining;


    void Start()
    {

        btStart.onClick.AddListener(startTraining);
        btSetScene.onClick.AddListener(setSceneSettings);
        txtError.gameObject.SetActive(false);
        PopulateDropdownFromEnum<GameManager.Object_Mesh>(ddMesh);
        PopulateDropdownFromEnum<GameManager.Movements>(ddMotion);
        inTRounds.text = numTRounds.ToString();
        inTTime.text = tTime.ToString();
        inWaitTime.text = waitTime.ToString();
        //set default frequencies
        //UpdateView(); 
    }

    static public GameObject getChildGameObject(GameObject fromGameObject, string withName)
    {
        //Author: Isaac Dart, June-13.
        Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
        return null;
    }
    void PopulateDropdownFromEnum<T>(TMP_Dropdown dropdown) where T : Enum
    {
        dropdown.ClearOptions();
        var enumNames = Enum.GetNames(typeof(T)).ToList();
        dropdown.AddOptions(enumNames);
    }

    public void startTraining()
    {
        setSceneSettings();
        if (txtError.gameObject.activeSelf == false) {
            Debug.Log("Start Training");
            OnStartTraining?.Invoke();
        }
        else
        {
            Debug.Log("Cannot start training because of input errors "); 
        }
    }
    public void setSceneSettings()
    {
        txtError.gameObject.SetActive(false);
        string mesh_str = ddMesh.options[ddMesh.value].text;
        string motion_str = ddMotion.options[ddMotion.value].text;

        // Convert strings back to enum
        selectedMesh = (GameManager.Object_Mesh)Enum.Parse(typeof(GameManager.Object_Mesh), mesh_str);
        selectedMotion = (GameManager.Movements)Enum.Parse(typeof(GameManager.Movements), motion_str);

        // Use the selected values
        Debug.Log("Selected Mesh: " + selectedMesh);
        Debug.Log("Selected Motion: " + selectedMotion);

        //txtError.gameObject.SetActive(true);
        //txtError.text = selectedMesh+"; "+selectedMotion;

        if (int.TryParse(inTRounds.text, out int int1))
        {
            numTRounds = int1;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid number of rounds value (must be an int)";
            return; 
        }

        if (float.TryParse(inTTime.text, out float f2))
        {
            tTime = f2;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid training time value (must be a float)";
            return; 
        }

        if (float.TryParse(inWaitTime.text, out float f3))
        {
            waitTime = f3;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid waiting time value (must be a float)";
            return; 
        }
        Debug.Log("Scene set: " + numTRounds + "; " + tTime + "; " + waitTime + "; " + selectedMesh + "; " + selectedMotion);
        OnSceneSet?.Invoke(numTRounds, tTime, waitTime, selectedMesh, selectedMotion);
        
    }


}
