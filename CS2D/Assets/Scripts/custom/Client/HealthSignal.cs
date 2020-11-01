﻿using System.Collections;
using System.Collections.Generic;
using custom;
using custom.Client;
using custom.Server;
using custom.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HealthSignal : MonoBehaviour
{
    private MeshRenderer mesh;
    public Material black, red, yellow, green;
    private Messenger cm;
    public int id;
    
    void Start()
    {
        mesh = this.gameObject.GetComponent<MeshRenderer>();
        if (MasterBehavior.MasterData.serverMode)
        {
            cm = GameObject.Find("ShadowServer").GetComponent<ServerMessenger>();
        }
        else
        {
            cm = GameObject.Find("ShadowClient").GetComponent<ClientMessenger>();
        }
    }

    void Update()
    {
        float lastHealth = cm.getCurrentHealth(id);
        Debug.Log(id + " " + lastHealth);
        if (lastHealth < 0.2f)
        {
            mesh.material = black;
        } 
        else if (lastHealth < 0.4f)
        {
            mesh.material = red;
        }
        else if (lastHealth < 0.7f)
        {
            mesh.material = yellow;
        }
        else
        {
            mesh.material = green;
        }
    }
}
