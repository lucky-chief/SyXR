using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;

public class FPS : MonoBehaviour
{
    public Text fpsText;
    public float interval = 0.4f;

    private float _elapsed = 0.0f;

    void Start()
    {
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if(_elapsed < interval)
            return;
        _elapsed = 0.0f;
        fpsText.text = "FPS: " + (1.0f / Time.deltaTime).ToString("F2");
    }
}
