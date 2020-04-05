﻿using UnityEngine;
using System;
using System.Collections.Generic;
using GILES;

namespace MapEditor
{
    //A singleton class for creating and deleting map objects
    public class MapManager : MonoBehaviour
    {
        #region Data Members
        //A self-reference to the singleton instance of this script
        public static MapManager Instance { get; private set; }

        //A reference to the empty gameobject that contains all of the copied objects
        [SerializeField]
        private GameObject copiedObjectsRoot;
        //A reference to the empty map to add objects to
        [SerializeField]
        private GameObject mapRoot;
        //A reference to the billboard prefab
        [SerializeField]
        private GameObject billboardPrefab;

        //References to the large and small map boundaries
        [SerializeField]
        private GameObject smallMapBounds;
        [SerializeField]
        private GameObject largeMapBounds;

        //A dictionary mapping gameobjects to MapObject scripts
        public static Dictionary<GameObject, MapObject> objectScriptTable { get; private set; }
        //Determines if the small map bounds have been disabled or not
        private static bool boundsDisabled;
        #endregion

        #region Initialization
        //Set this script as the only instance of the MapManager script
        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            objectScriptTable = new Dictionary<GameObject, MapObject>();
        }
        #endregion

        #region Update
        void LateUpdate()
        {
            //If the game is in edit mode, check for keyboard shortcut inputs
            if (EditorManager.currentMode == EditorMode.Edit)
            {
                //Check delete keys
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                    deleteSelection();
                //Check for shortcuts that require the control key to be pressed down
                else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                {
                    if (Input.GetKeyDown(KeyCode.C))
                        copySelection();
                    else if (Input.GetKeyDown(KeyCode.V))
                        pasteSelection();
                }
            }
        }

        //Copy a selection by cloning all of the selected objects and storing them
        private static void copySelection()
        {
            //Get a reference to the list of selected objects
            ref List<GameObject> selectedObjects = ref ObjectSelection.getSelection();

            //If there aren't any objects to copy, return
            if (selectedObjects.Count == 0)
                return;

            //Destroy any previously copied objects
            foreach (Transform copiedObject in Instance.copiedObjectsRoot.transform)
                Destroy(copiedObject.gameObject);

            //Temporary GameObject to disable cloned objects before storing them
            GameObject objectClone;

            //Clone each selected object and save it in the copied objects list
            foreach (GameObject mapObject in selectedObjects)
            {
                //Instantiate and disable the copied objects
                objectClone = Instantiate(mapObject);
                objectClone.SetActive(false);
                //Get a reference to the cloned object's MapObject script
                MapObject mapObjectScript = objectClone.GetComponent<MapObject>();
                //Copy the values of the original map object script
                mapObjectScript.copyValues(mapObject.GetComponent<MapObject>());
                //Set the object as the child of the copied objects root
                objectClone.transform.parent = Instance.copiedObjectsRoot.transform;
            }
        }

        //Paste the copied objects by instantiating them
        private static void pasteSelection()
        {
            //Temporary GameObject to enable cloned objects before storing them
            GameObject objectClone;
            //Reset the current selection
            ObjectSelection.deselectAll();

            //Loop through all of the copied objects
            foreach (Transform copiedObject in Instance.copiedObjectsRoot.transform)
            {
                //Instantiate and enable the cloned object
                objectClone = Instantiate(copiedObject.gameObject);
                objectClone.SetActive(true);
                //Get a reference to the cloned object's MapObject script
                MapObject mapObjectScript = objectClone.GetComponent<MapObject>();
                //Copy the values of the original map object script
                mapObjectScript.copyValues(copiedObject.GetComponent<MapObject>());
                //Add the object to the map and make it selectable
                addObjectToMap(objectClone, mapObjectScript);
                ObjectSelection.selectObject(objectClone);
            }

            //Once the selection is pasted, change the tool type to translate
            ToolButtonManager.setTool(Tool.Translate);
        }

        //Delete the selected objects
        //To-Do: store deleted objects so the delete can be undone
        private static void deleteSelection()
        {
            //Get a reference to the selected objects list
            ref List<GameObject> selectedObjects = ref ObjectSelection.removeSelected();

            //Remove each selected object from the script table and destroy the object
            foreach (GameObject mapObject in selectedObjects)
            {
                objectScriptTable.Remove(mapObject);
                destroyObject(mapObject);
            }

            //Reset the selected objects lsit
            selectedObjects = new List<GameObject>();
        }
        #endregion

        #region Map Methods
        //Delete all of the map objects
        public static void clearMap()
        {
            //Remove all deleted objects from the selection lists
            ObjectSelection.resetSelection();
            //Reset the hash table for MapObject scripts
            objectScriptTable = new Dictionary<GameObject, MapObject>();
            //Reset the boundaries disabled flag and activate the small bounds
            boundsDisabled = false;
            enableLargeMapBounds(false);

            //Iterate over all children objects and delete them
            foreach (Transform child in Instance.mapRoot.GetComponentInChildren<Transform>())
                GameObject.Destroy(child.gameObject);
        }

        //Parse the given map script and load the map
        public static void loadMap(string mapScript)
        {
            //Remove all of the new lines and spaces in the script
            mapScript = mapScript.Replace("\n", "");
            mapScript = mapScript.Replace("\r", "");
            mapScript = mapScript.Replace("\t", "");
            mapScript = mapScript.Replace(" ", "");

            //Seperate the map by semicolon
            string[] parsedMap = mapScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            //Create each object and add it to the map
            for (int scriptIndex = 0; scriptIndex < parsedMap.Length; scriptIndex++)
            {
                try
                {
                    //If the object script starts with '//' ignore it
                    if (parsedMap[scriptIndex].StartsWith("//"))
                        continue;

                    //Parse the object script and create a new map object
                    MapObject mapObjectScript;
                    GameObject newMapObject = loadObject(parsedMap[scriptIndex], out mapObjectScript);

                    //If the object is defined, add it to the map hierarchy and make it selectable
                    if (newMapObject)
                        addObjectToMap(newMapObject, mapObjectScript);
                }
                catch (Exception e)
                {
                    //If there was an issue parsing the object, log the error and skip it
                    Debug.Log("Skipping object on line " + scriptIndex + "\t(" + parsedMap[scriptIndex] + ")");
                    Debug.Log(e + ":\t" + e.Message);
                }
            }
        }

        //Add the given object to the map hierarchy and make it selectable
        private static void addObjectToMap(GameObject objectToAdd, MapObject objectScript)
        {
            //Make the new object a child of the map root.
            objectToAdd.transform.parent = Instance.mapRoot.transform;
            //Make the new object selectable
            ObjectSelection.addSelectable(objectToAdd);
            //Add the object and its MapObject script to the dictionary
            objectScriptTable.Add(objectToAdd, objectScript);
        }

        //Remove the given object to the map hierarchy and make object selection script
        private static void removeObjectFromMap(GameObject objectToRemove)
        {
            //Remove the object from the object selection script
            ObjectSelection.removeSelectable(objectToRemove);
            //Remove the object from the script dictionary
            objectScriptTable.Remove(objectToRemove);
            //Delete the object itself
            Destroy(objectToRemove);
        }

        //Parse the given object script and instantiate a new GameObject with the data
        private static GameObject loadObject(string objectScript, out MapObject mapObjectScript)
        {
            //Seperate the object script by comma
            string[] parsedObject = objectScript.Split(',');
            //The GameObject loaded from RCAssets corresponding to the object name
            GameObject newObject = null;
            //The type of the object
            objectType type;

            try
            {
                //If the script is "map,disableBounds" then set a flag to disable the map boundries and skip the object
                if (parsedObject[0].StartsWith("map") && parsedObject[1].StartsWith("disablebounds"))
                {
                    boundsDisabled = true;
                    enableLargeMapBounds(true);
                    mapObjectScript = null;

                    return null;
                }

                //If the length of the string is too short, raise an error
                if (parsedObject.Length < 9)
                    throw new Exception("Too few elements in object script");

                //Parse the object type
                type = MapObject.parseType(parsedObject[0]);

                //Use the object name to load the asset
                newObject = createMapObject(type, parsedObject[1]);
                //Get the MapObject script attached to the new GameObject
                mapObjectScript = newObject.GetComponent<MapObject>();

                //Use the parsedObject array to set the reset of the properties of the object
                mapObjectScript.loadProperties(parsedObject);

                //Check if the object is a region
                if (type == objectType.misc && parsedObject[1] == "region")
                {
                    //Give the region a default rotation
                    mapObjectScript.Rotation = Quaternion.identity;

                    //intantiate a billboard and set it as a child of the region
                    GameObject billboard = Instantiate(Instance.billboardPrefab);
                    billboard.GetComponent<TextMesh>().text = mapObjectScript.RegionName;
                    billboard.transform.parent = newObject.transform;
                }

                return newObject;
            }
            //If there was an error converting an element to a float, destroy the object and pass a new exception to the caller
            catch (FormatException)
            {
                destroyObject(newObject);
                throw new Exception("Error conveting data");
            }
            //If there are any other errors, destroy the object and pass them back up to the caller
            catch (Exception e)
            {
                destroyObject(newObject);
                throw e;
            }
        }

        //Convert the map into a script
        public override string ToString()
        {
            //The exported map script
            string mapScript = "";

            //If bounds are disabled, add that script to the beginning of the script
            if (boundsDisabled)
                mapScript += "map,disablebounds;\n";

            //Add the script for each object to the map script
            foreach (MapObject objectScript in objectScriptTable.Values)
                mapScript += objectScript.ToString() + "\n";

            return mapScript;
        }
        #endregion

        #region Parser Helpers
        //Check if the object exists. Then disable and destroy it
        private static void destroyObject(GameObject objectToDestroy)
        {
            if (objectToDestroy)
            {
                objectToDestroy.SetActive(false);
                Destroy(objectToDestroy);
            }
        }

        //Toggle between the small and large map bounds being active
        private static void enableLargeMapBounds(bool enabled)
        {
            Instance.smallMapBounds.SetActive(!enabled);
            Instance.largeMapBounds.SetActive(enabled);
        }

        //Load the GameObject from RCAssets with the corresponding object name and attach a MapObject script to it
        private static GameObject createMapObject(objectType type, string objectName)
        {
            //The GameObject loaded from RCAssets corresponding to the object name
            GameObject newObject;

            //If the object is a vanilla object, instantiate it from the vanilla assets
            if (type == objectType.@base)
            {
                newObject = AssetManager.instantiateVanillaObject(objectName);
            }
            //If the object is a barrier or region, instantiate editor version
            else if (objectName == "barrier" || objectName == "region")
            {
                newObject = AssetManager.instantiateRcObject(objectName + "Editor");
            }
            //Otherwise, instantiate the object from teh RC assets
            else
                newObject = AssetManager.instantiateRcObject(objectName);

            //If the object name wasn't valid, raise an error
            if (!newObject)
                throw new Exception("The object '" + objectName + "' does not exist");

            //Attatch the MapObject script to the new object
            MapObject mapObjectScript = newObject.AddComponent<MapObject>();
            //Set the type of the mapObject
            mapObjectScript.Type = type;

            //Return the new object 
            return newObject;
        }
        #endregion
    }
}