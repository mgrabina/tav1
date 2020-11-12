﻿using System.Collections;
using System.Collections.Generic;
using custom;
using custom.Client;
using custom.Server;
using custom.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Kills : MonoBehaviour
{
    private TextMesh mesh;
    public Messenger cm;
    public int id;
    
    void Start()
    {
        mesh = this.gameObject.GetComponent<TextMesh>();
    }

    void Update()
    {
        if (cm == null)
        {
            return;
        }
        int kills = cm.getKills();
        if (kills.Equals(0))
        {
            mesh.text = "";
        }
        else
        {
            mesh.text = "- " + kills + " -";
        }
    }
}