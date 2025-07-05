using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System; 
using Valve.VR.InteractionSystem;

public class TestingMenuManager : MonoBehaviour
{
    [SerializeField]
    private MuseumManager m_Manager;
    [SerializeField]
    private UnityEngine.UI.Button addToList; 
    [SerializeField] 
    private UnityEngine.UI.Button startSequence;
    [SerializeField]
    private TMP_InputField inputSequence;
    [SerializeField]
    private Transform scrollViewContent;
    [SerializeField]
    private GameObject buttonPrefab;
    [SerializeField]
    private TMP_Text txtError;
    [SerializeField]
    private TMP_Text txtNextSequence;
    [SerializeField]
    private TMP_Text txtActualSequence;
    [SerializeField]
    private TMP_Text txtSelectedSequence;
    private List<string> sequences = new List<string>();
    private string selectedSequence;
    public static event Action<string> OnSequenceStart;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        addToList.onClick.AddListener(AddToList);
        startSequence.onClick.AddListener(StartSequence);
        selectedSequence = "";
        //txtError.gameObject.SetActive(false);
        UpdateView();
    }
    bool IsValidInput(string input)
    {
        foreach (char c in input)
        {
            if (c != '1' && c != '2' && c != '3')
            {
                return false;
            }
        }
        return true;
    }

    private void AddToList()
    {
        string seq = inputSequence.text;
        if (IsValidInput(seq) ){ 
            selectedSequence = seq;
            GameManager.Instance.sequences.Add(seq);
            UpdateView();
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "The sequence must be numeric and contain only numbers between 1-3"; 
        }
        
    }

    private void UpdateView()
    {
        txtError.gameObject.SetActive(false); 
        inputSequence.text = ""; 
        ClearChildren(scrollViewContent);
        //players = dataManager.GetPlayers();
        sequences = GameManager.Instance.sequences;

        foreach (string s in sequences)
        {
            Debug.Log("Adding button with sequence: "+s);
            GameObject gameObject = Instantiate(buttonPrefab, scrollViewContent);
            Button button = gameObject.GetComponent<Button>();
            button.GetComponentInChildren<TMP_Text>().text = s;
            button.onClick.AddListener(() => SetSelectedSequence(s));
        }
    }

    private void SetSelectedSequence(string sequence)
    {
        selectedSequence = sequence;
        txtNextSequence.text = sequence;
        Debug.Log("Selected Sequence = " + sequence); 
    }
    private void StartSequence()
    {
        if(selectedSequence != "")
        {
            txtError.gameObject.SetActive(false); 
            txtActualSequence.text = selectedSequence;
            txtSelectedSequence.text = ""; 
            OnSequenceStart?.Invoke(selectedSequence);
        }
        else
        {
            txtError.gameObject.SetActive(true);
            txtError.text = "Select a sequence before starting"; 
        }
        //m_Manager.testing = true;
        //Debug.Log("MuseumManager mode"+m_Manager.testing); 
    }

    private void ClearChildren(Transform t)
    {
        var children = t.Cast<Transform>().ToArray();

        foreach (var child in children)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    private void updateSelectedSequence(int num)
    {
        if (m_Manager.testing)
        {
            txtSelectedSequence.text = txtSelectedSequence.text + num.ToString();
        }
    }

    //TODO: listener to LSL manager classification
    private void OnEnable()
    {
        LSLManager.OnClassificationReceived += updateSelectedSequence;
    }

    private void OnDisable()
    {
        LSLManager.OnClassificationReceived -= updateSelectedSequence;
    }
}
