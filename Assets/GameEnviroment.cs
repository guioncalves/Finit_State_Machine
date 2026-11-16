using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEnviroment : MonoBehaviour
{
    private static GameEnviroment instance;
    private List<GameObject> checkpoint = new List<GameObject>();
    public List<GameObject> Checkpoints { get { return Checkpoints; } }

    public static GameEnviroment Singleton
    {
        get
        {
            if(instance == null)
            {
                instance = new GameEnviroment();
                instance.Checkpoints.AddRange(GameObject.FindGameObjectsWithTag("Checkpoint"));
            }
            return instance;
        }

    }
    
}
