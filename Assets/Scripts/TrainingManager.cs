using System.Collections.Generic;
using UnityEngine;

public class TrainingManager : MonoBehaviour
{
    //Diamond
    public GameObject diamondPrefab;
    private Quaternion diamond_rotation = Quaternion.Euler(0, 0, 0);
    //public Vector3 diamond_scale = new Vector3(1f, 1f, 1f);
    public Vector3 diamond_scale = new Vector3(0.2f, 0.2f, 0.2f);
    //public float diamond_height = 0.3f;

    //Cube
    public GameObject cubePrefab;
    private Quaternion cube_rotation = Quaternion.Euler(0, 0, 0);
    public Vector3 cube_scale = new Vector3(0.3f, 0.3f, 0.3f);
    //public float cube_height = 0.3f;

    //Arrow
    public GameObject arrowPrefab;
    private Quaternion arrow_rotation = Quaternion.Euler(0, 0, 90);
    public Vector3 arrow_scale = new Vector3(1f, 1f, 1f);
    //public float arrow_height = 0.3f; //0.05
    private float stimuli_height = 0.5f;

    //Selected Stimuli
    private GameObject selected_obj_Prefab; //Mesh
    private Quaternion selected_obj_rotation;
    private Vector3 selected_obj_scale;

    private List<StimuliObj> centerStimuliObj = new List<StimuliObj>();
    private int[,] index_stimuli = {
        { 1, 0, 11 },
        {  4, 3, 2 },
        {  7, 6, 5 },
        {  10, 9,8 }
    };

    public float distanceToPlayer = 2.0f;
    public int numCenterStimuli = 12;


    //Settings 
    private float waitForSeconds = 5f; //wait x seconds before starting to move the stimuli
    private int trainingRounds; //n° of training rounds 
    private float trainingSeconds; //The time each stimuli will be moving
    private GameManager.Object_Mesh selected_mesh;
    private GameManager.Movements selected_movement;

    //Subcribe to Events
    private void OnEnable()
    {
        TrainingMenuManager.OnSceneSet += setTrainingSettings;
        TrainingMenuManager.OnStartTraining += startTraining; 
    }

    private void OnDisable()
    {
        TrainingMenuManager.OnSceneSet -= setTrainingSettings;
        TrainingMenuManager.OnStartTraining -= startTraining;
    }

    void Start()
    {
        GameManager.Instance.player.transform.position = Vector3.zero;
        //setSelectedStimuli();
        /*if (GameManager.Instance.objectMesh == GameManager.Object_Mesh.Arrow) InitializeStimuli(selected_obj_Prefab, selected_obj_rotation, selected_obj_scale, false);
        else InitializeStimuli(selected_obj_Prefab, selected_obj_rotation, selected_obj_scale, true);*/

        //Start movement/stimulation
        //if (centerStimuliObj != null) startSelectedStiuli(centerStimuliObj);
        //else Debug.Log("No center stimuli objects created");
    }

    private void setTrainingSettings(int trounds, float tseconds, float waitfor, GameManager.Object_Mesh mesh, GameManager.Movements movement)
    {
        trainingRounds = trounds;
        trainingSeconds = tseconds;
        waitForSeconds = waitfor;
        selected_mesh = mesh;
        selected_movement = movement;
        setSelectedStimuli(); 
    }


    private void setSelectedStimuli()
    {
        if (selected_mesh == GameManager.Object_Mesh.Cube)
        {
            selected_obj_Prefab = cubePrefab;
            selected_obj_rotation = cube_rotation;
            selected_obj_scale = cube_scale;
            //selected_obj_height = cube_height;

        }
        else if (selected_mesh == GameManager.Object_Mesh.Diamond)
        {
            selected_obj_Prefab = diamondPrefab;
            selected_obj_rotation = diamond_rotation;
            selected_obj_scale = diamond_scale;
            //selected_obj_height=diamond_height;
        }
        else if (selected_mesh == GameManager.Object_Mesh.Arrow)
        {
            selected_obj_Prefab = arrowPrefab;
            selected_obj_rotation = arrow_rotation;
            selected_obj_scale = arrow_scale;
            //selected_obj_height=arrow_height;
            //Debug.Log("Arrow rotation = " + arrow_rotation.eulerAngles);
        }
        Debug.Log("Stimuli selected"); 
    }

    private void startTraining()
    {
        setSelectedStimuli();
        //Set and initialize stimuli objects
        if(GameManager.Instance.frequencies == null)
        {
            Debug.Log("GameManager frequencies == null"); 
        }
        Debug.Log("Stimulation frequencies: " + GameManager.Instance.frequencies[0] + ", " + GameManager.Instance.frequencies[1] + ", " + GameManager.Instance.frequencies[2]);
        Debug.Log("Selected objects: "+selected_obj_Prefab.name+"; "+selected_obj_rotation.eulerAngles+"; "+selected_obj_scale);
        if (selected_mesh == GameManager.Object_Mesh.Arrow) InitializeStimuli(selected_obj_Prefab, selected_obj_rotation, selected_obj_scale, false);
        else InitializeStimuli(selected_obj_Prefab, selected_obj_rotation, selected_obj_scale, true);
    }


    void InitializeStimuli(GameObject prefab, Quaternion rotation, Vector3 scale, bool lookToCenter)
    {
        if (centerStimuliObj != null)
        {
            foreach (StimuliObj obj in centerStimuliObj)
            {
                Destroy(obj.gameObject);
            }
        }

        centerStimuliObj.Clear();
        Vector3[] spawn_locations = GameManager.PoligonCalculator(numCenterStimuli, distanceToPlayer, stimuli_height);
        for (int i = 0; i < spawn_locations.Length; i++)
        { //set and instantiate stimuli at poligon-calculated positions

            //Create instance 
            GameObject d = Instantiate(prefab, spawn_locations[i], rotation);
            //Adjust scale
            d.transform.localScale = scale;

            //Face each stimuli to the center
            Vector3 direction;
            if (lookToCenter) direction = GameManager.Instance.player.transform.position - d.transform.position;
            else direction = -GameManager.Instance.player.transform.position + d.transform.position;
            direction.y = 0; //Only from the x-z axis
                                // First, rotate to face the center
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            // Apply the offset (original rotation based on prefab) after the look rotation
            Vector3 pos = d.transform.position;
            pos.y = stimuli_height;
            d.transform.SetLocalPositionAndRotation(pos, lookRotation * rotation);
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
        }
        Debug.Log("Center Stimuli initialized");
        
    }

}
