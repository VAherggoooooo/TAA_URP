using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TAACamera : MonoBehaviour
{    
    private Camera cam;
    void Start()
    {
        cam = GetComponent<Camera>();
        if(cam == null) return;
        cam.ResetProjectionMatrix();
    }

    private void OnDisable()
    {        
        if(cam == null) return;
        cam.ResetProjectionMatrix();
    }
    
}
