using System.Runtime.CompilerServices;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private Transform player;
    private Vector3 overworldOffset = new Vector3(0, 0, -10);
    private Vector3 battleOffset = new Vector3(2, 0, -10);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (GameState.state == GameState.State.overworld)
            gameObject.transform.position = player.transform.position + overworldOffset;
        else
            gameObject.transform.position = player.transform.position + battleOffset;
    }
}
