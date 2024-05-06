using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlSpeed : MonoBehaviour
{
    public GameObject cube;
    void Start()
    {

        Animation anim = GetComponent<Animation>();
        foreach (AnimationState state in anim)
        {
            state.speed = 2;

        }
        anim.Play();
    }

    public void SetCube111()
    {
        cube.SetActive(!cube.activeInHierarchy);
    }
}
