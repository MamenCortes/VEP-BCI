using UnityEngine;
using LSL;
using System;
using System.Collections;
using System.Collections.Generic;
//Check: https://github.com/labstreaminglayer/liblsl-Csharp/blob/master/README-Unity.md
public class LSLManager: Singleton<GameManager>
{
    //Adjust as necessary
    public string OutletStreamName = "Unity.MarkerStream";
    public string OutletStreamType = "Unity.Marker";
    public string OutletStreamID = ""; 
    public string InletStreamType = "Python.Classification";
    public string InletStreamName = "CCA_Classifier_Output";
    [HideInInspector]
    public int ChannelCount = 1;
    private string[] sample = new string[1];

    //LSL outlets
    private StreamOutlet markerOutlet;
    private StreamInfo streamInfo;

    //LSL inlet
    private StreamInlet classifierInlet;
    private StreamInfo[] streamInfos;
    private int channelCount = 0;

    //Action Events
    // Event: Notify others when a classification is received
    public static event Action<int> OnClassificationReceived; 

    // Event: Listen for requests to start stimulus
    public static event Action OnStartStimulus;

    private void Start()
    {

        //Create a hash ID for the stream
        var hash = new Hash128();
        hash.Append(OutletStreamName);
        hash.Append(OutletStreamType);
        hash.Append(gameObject.GetInstanceID());
        OutletStreamID = hash.ToString(); 

        //Create the stimulus event metadata 
        //SStreamInfo(string name, string type, int channel_count = 1, double nominal_srate = LSL.IRREGULAR_RATE, channel_format_t channel_format = channel_format_t.cf_float32, string source_id = "")
        //StreamInfo streamInfo = new StreamInfo(StreamName, StreamType, 1,LSL.LSL.IRREGULAR_RATE, channel_format_t.cf_string, hash.ToString());,

        //Outlet for Unity -> Python
        streamInfo = new StreamInfo(OutletStreamName, OutletStreamType, 1,LSL.LSL.IRREGULAR_RATE,channel_format_t.cf_string,OutletStreamID);
        markerOutlet = new StreamOutlet(streamInfo);
        sample[0] = "Initializing";
        markerOutlet.push_sample(sample);
        Debug.Log("Output stream initialized");

        //send initial configuration values
        //sendInitialConfig();
        //Debug.Log("CONFIG FILE SEND!");

        //Initialize inlet for Python -> Unity
        //StartCoroutine(InitializeClassifierInlet()); 
    }

    void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("Key 1 pressed"); 
            OnClassificationReceived?.Invoke(1);
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log("Key 2 pressed");
            OnClassificationReceived?.Invoke(2);
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("Key 3 pressed");
            OnClassificationReceived?.Invoke(3);
        }

        // Receive classification result from Python
        if (classifierInlet != null)
        {
            string[] sample = new string[channelCount]; 
            double timestamp = classifierInlet.pull_sample(sample, 0.0f);

            //Tries to pull a sample from the LSL stream into the sample array.
            //The 0.0f timeout means non - blocking: it returns immediately.
            //If a sample is available, it's copied into sample[0], and the LSL timestamp is returned.
            //If no sample is available, it returns 0.0.

            
            if (timestamp != 0.0)
            {
                //Asuming python sends one value at a time (1,2,3)
                int classifiedIndex = int.Parse(sample[0]);
                Debug.Log($"Received classification: {classifiedIndex}");
                // Fire the event in MuseumManager
                OnClassificationReceived?.Invoke(classifiedIndex);
            }
        }
        else { //Search and Open the stream
            //The resolve_stream() function is used to search for streams on the network based on this metadata.
            //The first string, "type", is the metadata field you're querying.
            //The second string, "Markers", is the value you're filtering for.
            //streamInfos = LSL.LSL.resolve_stream("type", InletStreamType, 1, 0.0);
            streamInfos = LSL.LSL.resolve_stream("name", InletStreamName, 1, 0.0);
            if (streamInfos.Length > 0)
            {
                classifierInlet = new StreamInlet(streamInfos[0]);
                channelCount = classifierInlet.info().channel_count();
                classifierInlet.open_stream();
                Debug.Log("LSL Classifier Inlet connected.");
            }
            Debug.LogWarning("No Classifier Inlet stream found.");

        }
    }

    public void SendMarker(string markerLabel)
    {
        string[] sample = new string[] { markerLabel };
        markerOutlet.push_sample(sample);
        Debug.Log($"Sent marker: {markerLabel}");
    }

    public void SendMarker(string markerLabel, float value)
    {
        
        string[] sample = new string[] { markerLabel, value.ToString("F3") }; // 3 decimal places
        markerOutlet.push_sample(sample);
        Debug.Log($"Sent marker: {markerLabel}, Value: {value}");
        
    }

    public void sendInitialConfig()
    {
        //send markers in the form ("key":value)
        //Debug.Log("Sending markers = " + $"subjectID:{GameManager.Instance.subjectNum}" + $"frequencies:{string.Join(",", GameManager.Instance.frequencies)}" +
           // $"n_harmonics:{GameManager.Instance.nHarmonics}" + $"confidenceThreshold:{GameManager.Instance.threshold}");
        SendMarker($"subjectID:{GameManager.Instance.subjectNum}");
        SendMarker($"frequencies:{string.Join(",", GameManager.Instance.frequencies)}");
        SendMarker($"n_harmonics:{GameManager.Instance.nHarmonics}");
        SendMarker($"confidenceThreshold:{GameManager.Instance.threshold}");
        SendMarker("config_done");
        Debug.Log("Configuration sent via LSL_Manager.");
    }

    private void OnSceneLoaded(GameManager.Scenes scene_name)
    {
        if (scene_name == GameManager.Scenes.Museum)
        {
            StartCoroutine(SubscribeToMuseumEvents());
            //send initial configuration once the museum scene is loaded
            sendInitialConfig();
        }
        else
        {
             UnsuscribeMuseumEvents();
        }
    }

    private IEnumerator SubscribeToMuseumEvents()
    {
        // Wait until the scene-local object is initialized
        yield return new WaitUntil(() => MuseumManager.Instance != null);
        //MuseumManager.OnStimuliStart += SendMarker;
        MuseumManager.SendMarker += SendMarker;
        Debug.Log("Suscribed to MuseumManager events");
    }

    private void UnsuscribeMuseumEvents()
    {
        if (MuseumManager.Instance != null)
        {
            //MuseumManager.OnStimuliStart -= SendMarker;
            MuseumManager.SendMarker -= SendMarker;
            Debug.Log("Unsuscribed from MuseumManager events");
        }
    }

    // Subscribe to events
    private void OnEnable()
    {
        GameManager.OnSceneChangedTo += OnSceneLoaded;

    }

    //Unsuscribe to events
    private void OnDisable()
    {
        GameManager.OnSceneChangedTo -= OnSceneLoaded;
        UnsuscribeMuseumEvents ();
    }

    private void OnDestroy()
    {
        if (markerOutlet != null)
        {
            markerOutlet.Close();
            //markerOutlet.Dispose(); // Une when you are completely done with the outlet (ex. onAplicationQuit)
            markerOutlet = null;
            Debug.Log("Marker outlet closed.");
        }

        if (classifierInlet != null)
        {
            classifierInlet.close_stream(); // optional but good practice
            classifierInlet = null;
            Debug.Log("Classifier inlet closed.");
        }
    }

    private IEnumerator InitializeClassifierInlet()
    {
        yield return new WaitForSeconds(2f); // Allow LSL network to initialize
        StreamInfo[] results = LSL.LSL.resolve_stream("type", InletStreamType);

        if (results.Length > 0)
        {
            classifierInlet = new StreamInlet(results[0]);
            Debug.Log("LSL Classifier Inlet connected.");
        }
        else
        {
            Debug.LogWarning("No BCIOutput stream found.");
        }
    }

}
