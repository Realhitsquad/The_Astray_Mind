using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInteractable : Interactable
{
    public bool isActive;
    MeshRenderer meshRender;

    public virtual void Update()
    {
        meshRender = GetComponent<MeshRenderer>();
        isActive = meshRender.isVisible;
    }


    public override void OnFocus()
    {
        print("Looking at " + gameObject.name);
    }

    public override void OnInteract()
    {
        if (isActive)
        {
            isActive = !isActive;
            meshRender.enabled = isActive;
            print("Turned off");
        }
        else
        {
            isActive = true;
            meshRender.enabled = isActive;
            print("Turned on");
        }
        print("Interacting with " + gameObject.name);
    }

    public override void OnLoseFocus()
    {
        print("Looked away from " + gameObject.name);
    }
}
