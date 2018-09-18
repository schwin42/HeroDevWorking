using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class ProjectileBlast : MonoBehaviour
{
    private const float MAX_INTENSITY = 7f;
    private const float FADE_IN_TIME = 0.2f;
    private const float FADE_OUT_TIME = 1.0f;

    [SerializeField] private Light _light;

    private float _startTime = -1f;
    private float timer = 0f;

    // Use this for initialization
    void Start()
    {
        _startTime = Time.time;
        StartCoroutine(BlastProcedure());
    }

    private IEnumerator BlastProcedure()
    {
        Debug.Log("kaplow");
        while (timer < FADE_IN_TIME + FADE_OUT_TIME)
        {
            timer += Time.deltaTime;
            if (timer < FADE_IN_TIME)
            {
                _light.intensity = Mathf.Lerp(0, MAX_INTENSITY, timer / FADE_IN_TIME);
                yield return 0;
            }
            else
            {
                _light.intensity = Mathf.Lerp(MAX_INTENSITY, 0, timer / (timer - FADE_IN_TIME) / FADE_OUT_TIME);
                yield return 0;
            }
        }

        Destroy(gameObject);
        Debug.Log("DONE!");
        yield break;
    }
}