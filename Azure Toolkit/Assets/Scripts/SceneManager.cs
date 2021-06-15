using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour   
{
    public void LoadScene(int i)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(i);
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.Escape))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

}
