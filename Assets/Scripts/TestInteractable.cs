using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInteractable : Interactable
{
    public override void OnFocus()
    {
        print("Looking at " + gameObject.name);
    }

    public override void OnInteract()
    {
        print("Interacting with " + gameObject.name);
    }

    public override void OnLoseFocus()
    {
        print("Looked away from " + gameObject.name);
    }
}
