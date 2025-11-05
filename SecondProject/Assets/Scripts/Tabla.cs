using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tabla : MonoBehaviour
{
    public static Tabla Instance;
    public TextMeshProUGUI textoTabla;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);

    }

    public void ActualizarTabla(string texto)
    {
        textoTabla.text = texto;
    }
}