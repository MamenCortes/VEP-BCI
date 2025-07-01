using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System;
using System.Linq;
using JetBrains.Annotations;

public class MenuManager : MonoBehaviour
{

    //TODO: add updateView with subjects info method
    [SerializeField]
    private TMP_InputField inSubN;
    [SerializeField]
    private TMP_InputField inThreshold;
    [SerializeField]
    private TMP_Text txtError;
    [SerializeField]
    private TMP_InputField inFreq1; 
    [SerializeField]
    private TMP_InputField inFreq2; 
    [SerializeField]
    private TMP_InputField inFreq3;
    [SerializeField]
    private TMP_Dropdown ddMesh;
    [SerializeField]
    private TMP_Dropdown ddMotion;
    [SerializeField]
    private TMP_Dropdown ddTexture;
    [SerializeField]
    private UnityEngine.UI.Button btStartTesting;
    [SerializeField]
    private UnityEngine.UI.Button btStartTraining;
    [SerializeField]
    private UnityEngine.UI.Button btSaveSettings;
    private GameManager.Object_Mesh selectedMesh; //shape of the object
    private GameManager.Movements selectedMotion; //movement stimuli
    private GameManager.MaterialTexture selectedTexture; //texture/material of the objects
    private List<float> newfreqs;
    private float subjectNum;
    private float threshold; 
    void Start()
    {

        btStartTesting.onClick.AddListener(startTesting);
        btSaveSettings.onClick.AddListener(updateSettings);
        btStartTraining.onClick.AddListener(startTraining);
        txtError.gameObject.SetActive(false); 
        PopulateDropdownFromEnum<GameManager.Object_Mesh>(ddMesh);
        PopulateDropdownFromEnum<GameManager.Movements>(ddMotion);
        PopulateDropdownFromEnum<GameManager.MaterialTexture>(ddTexture);
        //set default frequencies
        StartCoroutine(showDefaultValuesOnScreen()); 
        //UpdateView(); 
    }

    IEnumerator showDefaultValuesOnScreen()
    {
        yield return new WaitForSeconds(0.1f);
        //newfreqs = new List<float> { 5.5f, 6.6f, 7.5f };
        newfreqs = GameManager.Instance.frequencies.ToList<float>(); 
        inFreq1.text = newfreqs[0].ToString();
        inFreq2.text = newfreqs[1].ToString();
        inFreq3.text = newfreqs[2].ToString();
        inSubN.text = GameManager.Instance.subjectNum.ToString();
        inThreshold.text = GameManager.Instance.threshold.ToString("F2");
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

    public void startTesting()
    {
        updateSettings();
        if (txtError.gameObject.activeSelf) //is there is an error
        {
            Debug.Log("Cannot continue because of setting errors");
            txtError.text = "Please solve all errors before continuing";
        }
        else {
            GameManager.Instance.objectMesh = selectedMesh;
            GameManager.Instance.objectMotion = selectedMotion;
            GameManager.Instance.objectMaterial = selectedTexture; 
            GameManager.Instance.frequencies = newfreqs.ToArray();
            GameManager.Instance.subjectNum = subjectNum; 
            GameManager.Instance.threshold = threshold;
            GameManager.Instance.ChangeScene(GameManager.Scenes.Museum);
        }

    }
    public void updateSettings()
    {
        txtError.gameObject.SetActive(false);
        string mesh_str = ddMesh.options[ddMesh.value].text;
        string motion_str = ddMotion.options[ddMotion.value].text;
        string texture_str = ddTexture.options[ddTexture.value].text;

        // Convert strings back to enum
        selectedMesh = (GameManager.Object_Mesh)Enum.Parse(typeof(GameManager.Object_Mesh), mesh_str);
        selectedMotion = (GameManager.Movements)Enum.Parse(typeof(GameManager.Movements), motion_str);
        selectedTexture = (GameManager.MaterialTexture)Enum.Parse(typeof(GameManager.MaterialTexture), texture_str);

        //Parse the frequencies and check for errors
        if (float.TryParse(inFreq1.text, out float f1))
        {
            newfreqs[0] = f1;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid frequency values (must be a float)";
            return;
        }
        if (float.TryParse(inFreq2.text, out float f2))
        {
            newfreqs[1] = f2;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid frequency values (must be a float)";
            return;
        }
        if (float.TryParse(inFreq3.text, out float f3))
        {
            newfreqs[2] = f3;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid frequency values (must be a float)";
            return;
        }
        //Check subject number and threshold
        if (float.TryParse(inSubN.text, out float f4))
        {
            subjectNum = f4;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid subject number value (must be a float or int)";
            return;
        }
        if (float.TryParse(inThreshold.text, out float f5))
        {
            threshold = f5;
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Invalid threshold value (must be a float or int)";
            return;
        }

        Debug.Log("Selected Mesh: " + selectedMesh + "; Selected Motion: " + selectedMotion+"; Selected Texture; "+selectedTexture+"; Frequencies: ("+newfreqs.ToString()+"); Subject num: "+subjectNum+" Threshold: "+threshold);
    }

    public void startTraining()
    {
        Debug.Log("Start Training"); 
    }
}
