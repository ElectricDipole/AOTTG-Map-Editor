﻿using UnityEngine;
using System.Collections.Generic;
using GILES;

public class ObjectSelection : MonoBehaviour
{
    //A reference to the main object
    [SerializeField]
    private GameObject mainObject;
    //A reference to the editorManager on the main object
    private EditorManager editorManager;
    //A reference to the tool handle
    [SerializeField]
    private GameObject toolHandle;
    //A reference to the selectionHandle script on the tool handle
    private pb_SelectionHandle selectionHandle;
    //A list containing the objects that can be selected
    private List<GameObject> selectableObjects = new List<GameObject>();
    //A list containing the objects currently selected
    private List<GameObject> selectedObjects = new List<GameObject>();
    //The average point of all the selected objects
    private Vector3 selectionAverage;
    //The sum of the points of all the selected objects for calculating the average
    private Vector3 pointTotal;

    //Get references from other scripts
    void Start()
    {
        editorManager = mainObject.GetComponent<EditorManager>();
        selectionHandle = toolHandle.GetComponent<pb_SelectionHandle>();
    }

    //Check for object selections after the pb_SelectionHandle script checks for handle interaction
    void LateUpdate()
    {
        //Check for an object selection if the editor is in edit mode and the tool handle is not being dragged
        if (editorManager.currentMode == EditorMode.Edit && !selectionHandle.draggingHandle)
            checkSelect();
        //Displace the selected objects based on the tool handle
        if(selectionHandle.draggingHandle)
            updateSelection();
    }

    //Update the position, rotation, or scale of the object selections based on the tool handle
    void updateSelection()
    {
        //Get the displacement vector of the tool handle
        Vector3 displacement = selectionHandle.getDisplacement();

        //Don't edit the objects if the tool handle wasn't moved
        if (displacement != Vector3.zero)
        {
            //Iterate through all of the objects and displace them
            foreach (GameObject selectedObject in selectedObjects)
            {
                //Change what to displace based on the handle tool mode
                switch (selectionHandle.tool)
                {
                    case Tool.Translate:
                        selectedObject.transform.position += displacement;
                        break;

                    case Tool.Rotate:
                        //The poit to rotate around
                        Vector3 pivot;

                        //If only one object is selected, rotate it locally
                        if (selectedObjects.Count == 1)
                            pivot = selectedObject.transform.position;
                        //Otherwise, rotate it around the average point
                        else
                            pivot = selectionAverage;

                        //Find the corresponding axis and rotate around it
                        if(displacement.x != 0)
                            selectedObject.transform.RotateAround(pivot, toolHandle.transform.right, displacement.x);
                        else if (displacement.y != 0)
                            selectedObject.transform.RotateAround(pivot, toolHandle.transform.up, displacement.y);
                        else
                            selectedObject.transform.RotateAround(pivot, toolHandle.transform.forward, displacement.z);

                        break;

                    default:
                        selectedObject.transform.localScale += displacement;
                        break;
                }
            }

            //Update the selection average
            switch (selectionHandle.tool)
            {
                case Tool.Translate:
                    pointTotal += displacement * selectedObjects.Count;
                    selectionAverage += displacement;
                    break;
            }
        }
    }

    //Return a reference to the seleceted objects
    public List<GameObject> getSelectedObjects()
    {
        return selectedObjects;
    }

    //Test if any objects were clicked
    private void checkSelect()
    {
        //If the mouse was clicked, check if any objects were selected
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            //If an object was clicked, select it
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                //Select the parent of the object
                GameObject parentObject = getParent(hit.transform.gameObject);

                //Only select the object if it is selectable
                if (selectableObjects.Contains(parentObject))
                {
                    //If left control is not held, deselect all objects and select the clicked object
                    if (!Input.GetKey(KeyCode.LeftControl))
                    {
                        deselectAll();

                        //Select the object that was clicked on
                        selectObject(parentObject);
                    }
                    //If left control is held, select or deselect the object based on if its currently selected
                    else
                    {
                        if (!selectedObjects.Contains(parentObject))
                        {
                            selectObject(parentObject);
                        }
                        else
                            deselectObject(parentObject);
                    }
                }
            }
        }

        //If the 'A' key was pressed, either select or deselect all based on if anything is currently selected
        else if(Input.GetKeyDown(KeyCode.A))
        {
            if (selectedObjects.Count > 0)
                deselectAll();
            else
                selectAll();
        }
    }

    //Return the parent of the given object. If there is no parent, return the given object
    private GameObject getParent(GameObject childObject)
    {
        //The parent object that gets returned
        GameObject parentObject = childObject;
        //The tag of the parent object
        string parentTag = childObject.transform.parent.gameObject.tag;

        //Keep going up the hierarchy until the parent is a map or group
        while (parentTag != "Map" && parentTag != "Group")
        {
            //Move up the hierarchy
            parentObject = parentObject.transform.parent.gameObject;
            //Update the parent tag
            parentTag = parentObject.transform.parent.gameObject.tag;
        }

        return parentObject;
    }

    //Add a point to the total average
    private void addAveragePoint(Vector3 point)
    {
        //Add the point to the total and update the average
        pointTotal += point;
        selectionAverage = pointTotal / selectedObjects.Count;
        toolHandle.transform.position = selectionAverage;

        //If the tool handle is not active, activate it
        toolHandle.SetActive(true);
    }

    //Add all selected objects to the total average
    private void addAverageAll()
    {
        //Reset the total
        pointTotal = Vector3.zero;

        //Count up the total of all the objects
        foreach (GameObject mapObject in selectedObjects)
            pointTotal += mapObject.transform.position;

        //Average the points
        selectionAverage = pointTotal / selectableObjects.Count;
        toolHandle.transform.position = selectionAverage;

        //If the tool handle is not active, activate it
        toolHandle.SetActive(true);
    }

    //Remove a point from the total average
    private void removeAveragePoint(Vector3 point)
    {
        //Subtract the point to the total and update the average
        pointTotal -= point;

        //If there are any objects selected, update the handle position
        if (selectedObjects.Count != 0)
        {
            selectionAverage = pointTotal / selectedObjects.Count;
            toolHandle.transform.position = selectionAverage;
        }
        //Otherwise, disable the tool handle
        else
        {
            toolHandle.SetActive(false);
        }
    }

    //Remove all selected objects from the total average
    private void removeAverageAll()
    {
        //Reset the total and average
        pointTotal = Vector3.zero;
        selectionAverage = Vector3.zero;

        //Hide the tool handle
        toolHandle.SetActive(false);
    }

    //Add the given object to the selectable objects list
    public void addSelectable(GameObject objectToAdd)
    {
        selectableObjects.Add(getParent(objectToAdd));
    }

    //Remove the given object from both the selectable and selected objects lists
    public void removeSelectable(GameObject objectToAdd)
    {
        //If the object is selected, deselect it
        if (selectedObjects.Contains(objectToAdd))
            deselectObject(objectToAdd);

        //Remove the object from the selectable objects list
        selectableObjects.Remove(getParent(objectToAdd));
    }

    public void selectObject(GameObject objectToSelect)
    {
        //Get the parent of the object
        GameObject parentObject = getParent(objectToSelect);

        //Select the object
        selectedObjects.Add(parentObject);
        addOutline(parentObject);

        //Update the position of the tool handle
        addAveragePoint(parentObject.transform.position);
        //Reset the rotation on the tool handle
        resetToolHandleRotation();
    }

    public void selectAll()
    {
        //Select all objects by copying the selectedObjects list
        selectedObjects = new List<GameObject>(selectableObjects);

        //Add the outline to all of the objects
        foreach (GameObject selectedObject in selectedObjects)
            addOutline(selectedObject);

        //Update the tool handle position
        addAverageAll();
        //Reset the rotation on the tool handle
        resetToolHandleRotation(); ;
    }

    public void deselectObject(GameObject objectToDeselect)
    {
        //Get the parent of the object
        GameObject parentObject = getParent(objectToDeselect);

        //Deselect the object
        selectedObjects.Remove(parentObject);
        removeOutline(parentObject);

        //Update the position of the tool handle
        removeAveragePoint(parentObject.transform.position);
        //Reset the rotation on the tool handle
        resetToolHandleRotation();
    }

    public void deselectAll()
    {
        //Remove the outline on all selected objects
        foreach (GameObject selectedObject in selectedObjects)
            removeOutline(selectedObject);

        //Deselect all objects by deleting the selected objects list
        selectedObjects = new List<GameObject>();

        //Update the position of the tool handle
        removeAverageAll();
        //Reset the rotation on the tool handle
        resetToolHandleRotation();
    }

    //Set the rotation of the tool handle based on how many objects are selected
    public void resetToolHandleRotation()
    {
        //If the tool handle is in rotate mode and only one object is selected, use the rotation of that object
        if (selectionHandle.tool == Tool.Rotate && selectedObjects.Count == 1)
            selectionHandle.setRotation(selectedObjects[0].transform.rotation);
        //Otherwise reset the rotation
        else
            selectionHandle.setRotation(Quaternion.Euler(Vector3.up));
    }

    //Resets both the selected and selectable object lists
    public void resetSelections()
    {
        selectedObjects = new List<GameObject>();
        selectableObjects = new List<GameObject>();
        removeAverageAll();
    }

    //Add a green outline around a GameObject
    private void addOutline(GameObject objectToAddOutline)
    {
        //Get the outline script of the parent object
        Outline outlineScript = objectToAddOutline.GetComponent<Outline>();

        //If parent has an outline script, enable it
        if (outlineScript != null)
            outlineScript.enabled = true;

        //Go through the children of the object and enable the outline if it is a selectable object
        foreach (Transform child in objectToAddOutline.transform)
            if (child.gameObject.tag == "Selectable Object")
                child.GetComponent<Outline>().enabled = true;
    }

    //Remove the green outline shader
    private void removeOutline(GameObject objectToRemoveOutline)
    {
        //Get the outline script of the parent object
        Outline outlineScript = objectToRemoveOutline.GetComponent<Outline>();

        //If parent has an outline script, disable it
        if (outlineScript != null)
            outlineScript.enabled = false;

        //Go through the children of the object and disable the outline if it is a selectable object
        foreach (Transform child in objectToRemoveOutline.transform)
            if (child.gameObject.tag == "Selectable Object")
                child.GetComponent<Outline>().enabled = false;
    }
}