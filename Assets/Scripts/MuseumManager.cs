using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MuseumManager : MonoBehaviour
{
    //Diamond spawning 
    public Material pinkMat;
    public Material blueMat; 
    //Diamond
    public GameObject diamondPrefab;
    //private Quaternion diamond_rotation = Quaternion.Euler(-90, 0, 0);
    private Quaternion diamond_rotation = Quaternion.Euler(0, 0, 0);
    //public Vector3 diamond_scale = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 diamond_scale = new Vector3(1f, 1f, 1f);

    //Cube
    public GameObject cubePrefab;
    private Quaternion cube_rotation = Quaternion.Euler(0, 0, 0);
    public Vector3 cube_scale = new Vector3(0.3f, 0.3f, 0.3f);

    //Arrow
    public GameObject arrowPrefab;
    private Quaternion arrow_rotation = Quaternion.Euler(0, 0, 90);
    //public Vector3 arrow_scale = new Vector3(2f, 12f, 12f);
    public Vector3 arrow_scale = new Vector3(1f, 1f, 1f);
    private float stimuli_height = 0.5f;

    //Stimuli
    public GameObject outerStimuliWalls;
    private List<StimuliObj> centerStimuliObj = new List<StimuliObj>();
    private List<Vector3> posInFrontPictures = new List<Vector3>();
    private StimuliObj[,] outerStimuliObj2;
    private int[,] index_stimuli = {
        { 1, 0, 11 },
        {  4, 3, 2 },
        {  7, 6, 5 },
        {  10, 9,8 }
    };

    //Selected Stimuli
    private GameObject selected_obj_Prefab; //Mesh
    private Quaternion selected_obj_rotation;
    private Vector3 selected_obj_scale;
    private Material selected_obj_material; //The texture of the object
    //private float selected_obj_height;

    private Coroutine stimuliCoroutine;
    [HideInInspector]
    public enum Stimuli_State { CenterStimuliOn, OuterStimuliOn }
    [HideInInspector]
    public Stimuli_State gameState;
    private float distanceToPlayer = 2.0f;
    private int numCenterStimuli = 12;
    private int numOuterStimuli = 3;
    private int selectedCenterStimuliIndex;
    private float distanceBetweenOuterStimuli = 1f; //0.75f
    private float rotation_amplitude = 30f;
    private float zoom_amplitude = 0.4f; 

    //Events
    //public static event Action<string> OnStimuliStart;
    //public static event Action<string> ConfigSub;
    //public static event Action<string, float> SendMarker; 
    public static event Action<string> SendMarker;
    private bool classificationReceived = false;
    public static MuseumManager Instance { get; private set; }

    private List<float> targetFreqs; 
    private int targetIndex; 
    public bool testing;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        //OnStimuliStart = null;
        SendMarker = null;
        if (Instance == this) Instance = null;
    }

    //Subcribe to Events
    private void OnEnable()
    {
        LSLManager.OnClassificationReceived += selectStimuliFromClassification;
        TestingMenuManager.OnSequenceStart += startSequence; 
    }

    private void OnDisable()
    {
        LSLManager.OnClassificationReceived -= selectStimuliFromClassification;
        TestingMenuManager.OnSequenceStart -= startSequence;
    }

    void Start()
    {
        testing = false; 
        gameState = Stimuli_State.CenterStimuliOn;
        GameManager.Instance.player.transform.position = Vector3.zero;
        outerStimuliObj2 = new StimuliObj[numCenterStimuli, numOuterStimuli];
        //Set and initialize stimuli objects
        Debug.Log("Stimulation frequencies: " + GameManager.Instance.frequencies[0] + ", " + GameManager.Instance.frequencies[1] + ", " + GameManager.Instance.frequencies[2]);
        setSelectedStimuli();
        if (GameManager.Instance.objectMesh == GameManager.Object_Mesh.Arrow) InitializeStimuli(selected_obj_Prefab, selected_obj_rotation, selected_obj_scale, false);
        else InitializeStimuli(selected_obj_Prefab, selected_obj_rotation, selected_obj_scale, true);

        targetFreqs = new List<float>();
        GameManager.Instance.userText.text = "Wait for experimenter to continue";
    }

    private void Update()
    {

        if (gameState == Stimuli_State.CenterStimuliOn && testing)
        {
            
            if (GameManager.Instance.player != null)
            {
                Camera cam = GameManager.Instance.player.GetComponentInChildren<Camera>();
                List<(StimuliObj obj, float angle)> visibleStimuli = new List<(StimuliObj, float)>();
                Vector3 viewport;

                foreach (StimuliObj obj in centerStimuliObj)
                {
                    viewport = cam.WorldToViewportPoint(obj.transform.position);
                    if (obj.isVisible(viewport) && obj.gameObject.activeSelf)
                    {
                        Vector3 dirToStim = (obj.transform.position - cam.transform.position).normalized;
                        float angle = Vector3.SignedAngle(cam.transform.forward, dirToStim, Vector3.up);
                        float horizontalDist = Vector3.Dot(cam.transform.forward, dirToStim);
                        visibleStimuli.Add((obj, angle));
                    }
                    if (visibleStimuli.Count == 3) break; //stop searching if i already found 3 stimuli
                }

                if (visibleStimuli.Count < 3) { Debug.Log("Less than 3 objects found in the FOV"); return; }
                // Sort left-to-right by angle
                visibleStimuli.Sort((a, b) => a.angle.CompareTo(b.angle));

                string direction = "None";
                if (visibleStimuli[0].obj.frequency == targetFreqs[targetIndex]) direction = "Look Left";
                else if (visibleStimuli[1].obj.frequency == targetFreqs[targetIndex]) direction = "Look Center";
                else if (visibleStimuli[2].obj.frequency == targetFreqs[targetIndex]) direction = "Look Right";
                if (GameManager.Instance.userText != null)
                {
                    GameManager.Instance.userText.text = direction;
                }
                else Debug.Log("No player in GameManager");
            }
        }
        else if (gameState == Stimuli_State.OuterStimuliOn && testing)
        {
            string direction = "None"; 
            int to_index = selectedCenterStimuliIndex;
            if (outerStimuliObj2[to_index, 0].frequency == targetFreqs[targetIndex])
            {
                direction = "Look Right";
            }else if (outerStimuliObj2[to_index, 1].frequency == targetFreqs[targetIndex])
            {
                direction = "Look Center";
            }
            else if (outerStimuliObj2[to_index, 2].frequency == targetFreqs[targetIndex])
            {
                direction = "Look Left";
            }
            if (GameManager.Instance.userText != null)
            {
                GameManager.Instance.userText.text = direction;
            }
            else Debug.Log("No player in GameManager");
        }
        
    }

    public void startSequence(string sequence)
    {
        //Start movement/stimulation
        
        GameManager.Instance.MovePlayerTo(Vector3.zero);
        targetFreqs.Clear();
        for (int i = 0; i < sequence.Length; i++)
        {
            int num = int.Parse(sequence[i].ToString());
            //Debug.Log(num);
            targetFreqs.Add(GameManager.Instance.frequencies[num-1]);
        }
        testing = true;
        targetIndex = 0;

        Debug.Log("Target frequencies: "+string.Join(", ", targetFreqs));
        //OnStimuliStart?.Invoke("startTest");
        SendMarker?.Invoke("startTest"); 
        SendMarker?.Invoke($"targetSequence:{sequence}"); 
        if (centerStimuliObj != null) startSelectedStiuli(centerStimuliObj);
        else Debug.Log("No center stimuli objects created");

        
    }
    IEnumerator SequenceFinished()
    {
        SendMarker?.Invoke("stopTesting"); 
        testing = false;
        yield return new WaitForSeconds(3);
        resetView();
    }

    public void resetView()
    {
        gameState = Stimuli_State.CenterStimuliOn;
        targetIndex = 0;
        testing = false;
        selectedCenterStimuliIndex = 0;
        classificationReceived = false; 
        for (int i = 0; i < numCenterStimuli; i++) {
            centerStimuliObj[i].gameObject.SetActive(true);
            for (int j = 0; j < numOuterStimuli; j++) {
                outerStimuliObj2[i,j].gameObject.SetActive(false);
            }
        }
        GameManager.Instance.MovePlayerTo(Vector3.zero);
        GameManager.Instance.userText.text = "Wait for experimenter to continue";
    }
    public void selectStimuliFromClassification(int num)
    {
        if (testing)
        {
            classificationReceived = true;
            Debug.Log("Classification received");

            //Set flag classificationReceived to true, stop stimuli
            if (stimuliCoroutine != null)
            {
                Debug.Log("Stopping coroutine");
                //StopCoroutine(stimuliCoroutine);
                //stimuliCoroutine = null;
            }
            else Debug.LogWarning("Coroutine was null!");

            targetIndex++;
            if (targetIndex >= targetFreqs.Count)
            {
                Debug.Log("Testing finished");
                testing = false;
                GameManager.Instance.userText.text = "Sequence finished";
            } //stop testing
              //StopCoroutine(stimuliCoroutine); 

            float workingFreq = GameManager.Instance.frequencies[num - 1];
            Camera camera = GameManager.Instance.player.GetComponentInChildren<Camera>();
            Vector3 viewport;

            //Depending on the state of the game
            if (gameState == Stimuli_State.CenterStimuliOn)
            {
                List<StimuliObj> temp = new List<StimuliObj>();
                //Debug.Log(centerStimuliObj.Count + " center stimuli");
                for (int i = 0; i < centerStimuliObj.Count; i++)
                {
                    //Check if the stimuli is the field of view
                    viewport = camera.WorldToViewportPoint(centerStimuliObj[i].transform.position);
                    if (centerStimuliObj[i].isVisible(viewport) && centerStimuliObj[i].frequency == workingFreq && centerStimuliObj[i].gameObject.activeSelf)
                    {

                        //Move the player in front of a picture
                        GameManager.Instance.MovePlayerTo(posInFrontPictures[i]);
                        Debug.Log("Moving player to position " + centerStimuliObj[i].originalPosition);


                        //Activate stimuli in front of the picture
                        for (int j = 0; j < 3; j++)
                        {
                            //Debug.Log("Changing stimuli: Stimulation frequency = " + outerStimuliObj2[i, j].frequency + "; Framecount +" + outerStimuliObj2[i, j].frameCount);
                            outerStimuliObj2[i, j].gameObject.SetActive(true);
                            temp.Add(outerStimuliObj2[i, j]);
                        }
                        selectedCenterStimuliIndex = i;
                        //startSelectedStiuli(new List<StimuliObj> { outerStimuliObj2[i, 0], outerStimuliObj2[i, 1], outerStimuliObj2[i, 2]});
                        if (testing) { 
                            startSelectedStiuli(temp);
                            Debug.Log("Movement started"); 
                        }
                    }
                    //TODO: reset original position and rotation
                    centerStimuliObj[i].gameObject.SetActive(false);
                }
                gameState = Stimuli_State.OuterStimuliOn; //Change game state
            }
            else if (gameState == Stimuli_State.OuterStimuliOn)
            {
                for (int i = 0; i < numOuterStimuli; i++)
                {
                    //Check which stimuli are in the field of view
                    viewport = camera.WorldToViewportPoint(outerStimuliObj2[selectedCenterStimuliIndex, i].transform.position);

                    //If the stimuli is in the field of view, it's frequency matches the classified frequency and is active in the scene
                    if (outerStimuliObj2[selectedCenterStimuliIndex, i].isVisible(viewport) && outerStimuliObj2[selectedCenterStimuliIndex, i].frequency == workingFreq && outerStimuliObj2[selectedCenterStimuliIndex, i].gameObject.activeSelf)
                    {
                        //Deactivate the stimuli
                        outerStimuliObj2[selectedCenterStimuliIndex, 0].gameObject.SetActive(false);
                        outerStimuliObj2[selectedCenterStimuliIndex, 1].gameObject.SetActive(false);
                        outerStimuliObj2[selectedCenterStimuliIndex, 2].gameObject.SetActive(false);
                        List<StimuliObj> temp = new List<StimuliObj>();

                        //TODO: reset original position and rotation and scale
                        //Debug.Log("Frequency selected " + workingFreq);
                        int to_index = selectedCenterStimuliIndex;
                        //Move the player to the next position
                        if (i == 0)
                        {
                            to_index = to_index + 1;
                            if (to_index > 11) to_index = 0;
                            //GameManager.Instance.MovePlayerTo(centerStimuliObj[to_index].originalPosition);
                            GameManager.Instance.MovePlayerTo(posInFrontPictures[to_index]);
                            for (int j = 0; j < 3; j++)
                            {
                                outerStimuliObj2[to_index, j].gameObject.SetActive(true);
                                temp.Add(outerStimuliObj2[to_index, j]);

                            }
                            //TODO: start stimuli again
                            if (testing)
                            {
                                startSelectedStiuli(temp);
                                Debug.Log("Movement started");
                            }
                            gameState = Stimuli_State.OuterStimuliOn;
                        }
                        else if (i == 1)
                        {
                            GameManager.Instance.MovePlayerTo(Vector3.zero);
                            //Activate all center stimuli
                            foreach (StimuliObj cs in centerStimuliObj) cs.gameObject.SetActive(true);
                            //TODO: start stimuli again
                            if (testing)
                            {
                                startSelectedStiuli(centerStimuliObj);
                                Debug.Log("Movement started");
                            }
                            gameState = Stimuli_State.CenterStimuliOn;
                        }
                        else if (i == 2)
                        {
                            to_index = to_index - 1;
                            if (to_index < 0) to_index = 11;
                            //GameManager.Instance.MovePlayerTo(centerStimuliObj[to_index].originalPosition);
                            GameManager.Instance.MovePlayerTo(posInFrontPictures[to_index]);
                            for (int j = 0; j < 3; j++)
                            {
                                outerStimuliObj2[to_index, j].gameObject.SetActive(true);
                                temp.Add(outerStimuliObj2[to_index, j]);
                            }
                            //TODO: Start stimuli again
                            if (testing)
                            {
                                startSelectedStiuli(temp);
                                Debug.Log("Movement started");
                            }
                            gameState = Stimuli_State.OuterStimuliOn;
                        }

                        selectedCenterStimuliIndex = to_index;
                        Debug.Log("Moving player to position " + centerStimuliObj[selectedCenterStimuliIndex].originalPosition);
                        //break;
                    }
                }

            }
            if (!testing) StartCoroutine(SequenceFinished());
        }
        else Debug.Log("Not in testing mode"); 

    }
    private void setSelectedStimuli()
    {
        if(GameManager.Instance.objectMesh == GameManager.Object_Mesh.Cube)
        {
            selected_obj_Prefab = cubePrefab;
            selected_obj_rotation = cube_rotation;
            selected_obj_scale = cube_scale; 
            //selected_obj_height = cube_height;

        }else if (GameManager.Instance.objectMesh == GameManager.Object_Mesh.Diamond)
        {
            selected_obj_Prefab = diamondPrefab;
            selected_obj_rotation = diamond_rotation; 
            selected_obj_scale = diamond_scale;
            //selected_obj_height=diamond_height;
        }else if (GameManager.Instance.objectMesh == GameManager.Object_Mesh.Arrow)
        {
            selected_obj_Prefab=arrowPrefab;
            selected_obj_rotation = arrow_rotation; 
            selected_obj_scale = arrow_scale;
            //selected_obj_height=arrow_height;
            //Debug.Log("Arrow rotation = " + arrow_rotation.eulerAngles);
        }

        if (GameManager.Instance.objectMaterial == GameManager.MaterialTexture.Pink) {
            selected_obj_material = pinkMat; 
        }else if(GameManager.Instance.objectMaterial == GameManager.MaterialTexture.Blue)
        {
            selected_obj_material = blueMat;
        }
    }
    private void startSelectedStiuli(List<StimuliObj> stimuliObjects)
    {
        //Generalize for all stimuli
        if (GameManager.Instance.objectMotion == GameManager.Movements.Zoom)
        {
            stimuliCoroutine = StartCoroutine(StartZoomInOut(stimuliObjects));
        }
        else if (GameManager.Instance.objectMotion == GameManager.Movements.Rotation)
        {
            stimuliCoroutine = StartCoroutine(StartRotation(stimuliObjects));
        }
    }
    /// <summary>
    /// Initializes the stimuli gameObjects after parameters selected in the menu scene. Sets their position, rotation and scale and only shows the first row of stimuli.
    /// </summary>
    /// <param name="prefab">The prefab of the selected object (cube, diamond, etc)</param>
    /// <param name="rotation">The rotation of the prefab. </param>
    /// <param name="scale">The scale of the prefab. </param>
    /// <param name="lookToCenter">Set true to face the object to the center, set false to face away from the center </param>
    /// <returns>Describe return value.</returns>
    private void InitializeStimuli(GameObject prefab, Quaternion rotation, Vector3 scale, bool lookToCenter)
    {
        centerStimuliObj.Clear();
        Vector3[] spawn_locations = GameManager.PoligonCalculator(numCenterStimuli, distanceToPlayer, stimuli_height);
        for (int i = 0; i < spawn_locations.Length; i++)
        { //set and instantiate stimuli at poligon-calculated positions

            //set the height of the stimuli
            //Vector3 spawnPos  = spawn_locations[i];
            //spawnPos.y = selected_obj_height;
            //spawnPos.z = selected_obj_height;
            //Create instance 
            GameObject d = Instantiate(prefab, spawn_locations[i], rotation);
            //Adjust scale
            d.transform.localScale = scale;

            //Face each stimuli to the center
            Vector3 direction;
            if(lookToCenter) direction = GameManager.Instance.player.transform.position - d.transform.position;
            else direction = -GameManager.Instance.player.transform.position + d.transform.position;
            direction.y = 0; //Only from the x-z axis
            // First, rotate to face the center
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            // Apply the offset (original rotation based on prefab) after the look rotation
            Vector3 pos = d.transform.position;
            pos.y = stimuli_height;
            d.transform.SetLocalPositionAndRotation(pos, lookRotation*rotation); 
            d.SetActive(true);
            StimuliObj stimuli = d.GetComponent<StimuliObj>();
            stimuli.index = i; 
            //Save and create Stimuli Object
            stimuli.setOriginalTransform(d.transform);
            centerStimuliObj.Add(stimuli);
        }

        // Assign frequency based on index_diamonds
        for (int row = 0; row < index_stimuli.GetLength(0); row++)
        {
            for (int col = 0; col < index_stimuli.GetLength(1); col++)
            {
                int index = index_stimuli[row, col];
                centerStimuliObj[index].frequency = GameManager.Instance.frequencies[col];
            }
        }

        // Calculate frameCount for each
        foreach (StimuliObj d in centerStimuliObj)
        {
            d.frameCount = Mathf.RoundToInt(GameManager.Instance.refreshRate / d.frequency);
            ChangeMaterial(d.gameObject, selected_obj_material); //Change material
        }
        Debug.Log("Center Stimuli initialized");

        //Initialize outer row of stimuli
        //Debug.Log("Number of art pieces = "+art_transforms.Count);
        int j = 0; 
        foreach (Transform picture in outerStimuliWalls.transform)
        {
            //set left stimuli position and scale, don�t show in scene yet
            posInFrontPictures.Add(picture.position + picture.forward * distanceToPlayer);
            Vector3 picposition = picture.position;
            //picposition.y = selected_obj_height; 
            picposition.y = stimuli_height; 
            GameObject leftOuterStimuli = Instantiate(selected_obj_Prefab, picposition, picture.rotation);
            leftOuterStimuli.transform.localScale = selected_obj_scale;
            leftOuterStimuli.transform.Translate(Vector3.left * distanceBetweenOuterStimuli, Space.Self);
            leftOuterStimuli.SetActive(false);
            leftOuterStimuli.name = leftOuterStimuli.name + "left "+j; 

            //set right stimuli position and scale, don�t show in scene yet
            GameObject rightOuterStimuli = Instantiate(selected_obj_Prefab, picposition, picture.rotation);
            rightOuterStimuli.transform.localScale = selected_obj_scale;
            rightOuterStimuli.SetActive(false);
            rightOuterStimuli.name = rightOuterStimuli.name + "right "+j;

            //set center stimuli position and scale, don�t show in scene yet
            GameObject goBackStimuli = Instantiate(selected_obj_Prefab, picposition, picture.rotation);
            goBackStimuli.transform.localScale = selected_obj_scale;
            rightOuterStimuli.transform.Translate(Vector3.right * distanceBetweenOuterStimuli, Space.Self);
            goBackStimuli.SetActive(false);
            goBackStimuli.name = goBackStimuli.name + "goBack "+j;

            //Change rotation of arrows to point the proper direction 
            if (GameManager.Instance.objectMesh == GameManager.Object_Mesh.Arrow)
            {
                goBackStimuli.transform.Rotate((new Vector3(0,0, 90)));
                leftOuterStimuli.transform.Rotate(new Vector3(0, -90, 90));
                rightOuterStimuli.transform.Rotate(new Vector3(0, 90, 90));

            }

            outerStimuliObj2[j, 0] = rightOuterStimuli.GetComponent<StimuliObj>(); 
            outerStimuliObj2[j, 1] = goBackStimuli.GetComponent<StimuliObj>();
            outerStimuliObj2[j, 2] = leftOuterStimuli.GetComponent<StimuliObj>(); 

            for (int i = 0; i < 3; i++)
            {
                //Set frequency, framecount and original transform
                outerStimuliObj2[j, i].frequency = GameManager.Instance.frequencies[i];
                outerStimuliObj2[j, i].frameCount = Mathf.RoundToInt(GameManager.Instance.refreshRate / outerStimuliObj2[j, i].frequency);
                outerStimuliObj2[j, i].setOriginalTransform(outerStimuliObj2[j, i].transform);
                ChangeMaterial(outerStimuliObj2[j, i].gameObject, selected_obj_material); //Change material
                //Debug.Log("Frequency obj "+j+" = " + outerStimuliObj2[j, i].frequency);
            }
            j++;

        }

        Debug.Log("Outer stimuli created, j="+j);
        System.Threading.Thread.Sleep(3000); // 3 sec.
        //ConfigSub?.Invoke($"subjectID:{GameManager.Instance.subjectNum}");
        //ConfigFreq?.Invoke($"frequencies:{string.Join(",", GameManager.Instance.frequencies)}");
        //ConfigFreq?.Invoke("config_done");
    }
    IEnumerator StartZoomInOut(List<StimuliObj> stimuliObjects)
    {
        yield return new WaitForSeconds(5);
        Debug.Log("Zoom coroutine started");

        //float duration = 60f; // seconds
        float startTime = Time.time;
        //OnStimuliStart?.Invoke("startStimulation");
        SendMarker?.Invoke("startStimulation");
        float phase = 0;
        float scaleFactor = 0; 
        Vector3 scale = Vector3.one;
        Camera cam = GameManager.Instance.player.GetComponentInChildren<Camera>();
        Vector3 viewport; 

        //while (Time.time - startTime < duration)
        while (!classificationReceived)
        {
            
            foreach (StimuliObj d in stimuliObjects)
            {
                //Animate only if it is inside the field of view
                viewport = cam.WorldToViewportPoint(d.transform.position);
                if (d.isVisible(viewport))
                {
                    d.frameCounter = (d.frameCounter + 1) % d.frameCount;

                    phase = (float)d.frameCounter / d.frameCount;
                    scaleFactor = 1 + Mathf.Sin(2 * Mathf.PI * phase) * zoom_amplitude;

                    d.gameObject.transform.localScale = new Vector3(
                        scaleFactor * d.originalScale.x,
                        scaleFactor * d.originalScale.y,
                        scaleFactor * d.originalScale.z
                    );
                }

            }

            yield return null;
        }

        //Reset flag to false
        classificationReceived = false;
        Debug.Log("Classification flag cleared");

        foreach (StimuliObj d in stimuliObjects)
        {
            d.gameObject.transform.position = d.originalPosition;
            d.gameObject.transform.rotation = d.originalRotation; 
            d.gameObject.transform.localScale = d.originalScale;
        }
            Debug.Log("Zoom ended");
        //The coroutine stops automatically when it finishes running
    }
    IEnumerator StartRotation(List<StimuliObj> stimuliObjects)
    {
        yield return new WaitForSeconds(5);
        Debug.Log("Rotation coroutine started");

        //float duration = 60f; // seconds
        float startTime = Time.time;
        //OnStimuliStart?.Invoke("startStimulation");
        SendMarker?.Invoke("startStimulation");
        float phase = 0;
        float angleOffset = 0;
        Camera cam = GameManager.Instance.player.GetComponentInChildren<Camera>();
        Vector3 viewport;

        //while (Time.time - startTime < duration)
        while (!classificationReceived)
        {
            foreach (StimuliObj d in stimuliObjects)
            {
                //Animate only if it is inside the field of view
                viewport = cam.WorldToViewportPoint(d.transform.position);
                if (d.isVisible(viewport))
                {
                    //Debug.Log("Stimulation frequency = " + d.frequency + "; Framecount +" + d.frameCount); 
                    d.frameCounter = (d.frameCounter + 1) % d.frameCount;

                    phase = (float)d.frameCounter / d.frameCount;
                    angleOffset = Mathf.Sin(2 * Mathf.PI * phase) * rotation_amplitude;
                    d.gameObject.transform.rotation = d.originalRotation * Quaternion.Euler(angleOffset, 0, 0);
                }
            }

            yield return null;
        }

        //Reset flag to false
        classificationReceived = false;
        Debug.Log("Classification flag cleared");

        foreach (StimuliObj d in stimuliObjects)
        {
            d.gameObject.transform.position = d.originalPosition;
            d.gameObject.transform.rotation = d.originalRotation;
            d.gameObject.transform.localScale = d.originalScale;
        }
        Debug.Log("Rotation ended");
    }

    private void ChangeMaterial(GameObject obj, Material newMaterial)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = newMaterial;
        }
        else
        {
            Renderer[] rends = obj.transform.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in rends) { 
                if (r != null)
                {
                    r.material = newMaterial;
                } 
            }
        }
    }

}
