﻿using UnityEngine;
using System;

public class MapObject : MonoBehaviour
{
    #region Data Members
    //The underlying values for properties and default values from the fbx prefab
    private string[] defaultMatNames;
    private string materialValue;
    private Vector2 tilingValue;
    private Color colorValue;
    private Vector3 defaultScale;
    private Vector3 scaleFactor;

    //The number of properties the object has
    private int propertyNumber = 0;
    #endregion

    #region Properties
    //The type of the object
    public objectType Type { get; set; }
    //The actual type name specified in the map script
    public string FullTypeName { get; set; }
    //The specific object
    public string ObjectName { get; set; }
    //The name of the region if the object is a region
    public string RegionName { get; set; }
    //The amount of time until the titan spawns
    public float SpawnTimer { get; set; }
    //Determines if the the spawner will continue to spawn titans
    public bool EndlessSpawn { get; set; }
    //Determines if colored materials are enabled
    public bool ColorEnabled { get; set; }

    //The name of the material applied to the object
    public string Material
    {
        get { return materialValue; }
        set { materialValue = value; setMaterial(value); }
    }

    //How many times the texture will repeat in the x and y directions
    public Vector2 Tiling
    {
        get { return tilingValue; }
        set { tilingValue = value; setTiling(value); }
    }

    //The color of the object, including opacity
    public Color Color
    {
        get { return colorValue; }
        set { colorValue = value; setColor(colorValue); }
    }

    //Shorthand ways of accessing variables in the transform component
    public Vector3 Scale
    {
        get { return scaleFactor; }
        set { scaleFactor = value; scaleByFactor(value); }
    }

    public Vector3 Position
    {
        get { return transform.position; }
        set { gameObject.transform.position = value; }
    }

    public Quaternion Rotation
    {
        get { return transform.rotation; }
        set { gameObject.transform.rotation = value; }
    }
    #endregion

    #region Initialization
    //Copy the values from the given object
    public void copyValues(MapObject originalObject)
    {
        //Hidden data members
        defaultScale = originalObject.defaultScale;
        propertyNumber = originalObject.propertyNumber;

        //MapObject properties
        Type = originalObject.Type;
        FullTypeName = originalObject.FullTypeName;
        ObjectName = originalObject.ObjectName;
        RegionName = originalObject.RegionName;
        SpawnTimer = originalObject.SpawnTimer;
        EndlessSpawn = originalObject.EndlessSpawn;
        ColorEnabled = originalObject.ColorEnabled;

        //GameObject properties
        materialValue = originalObject.Material;
        tilingValue = originalObject.Tiling;
        colorValue = originalObject.Color;
        scaleFactor = originalObject.Scale;
    }
    #endregion

    #region Setters
    //Setters for implementing more complicated varaibles

    //Apply the given material as the new material of the object
    private void setMaterial(string newMaterial)
    {
        //Check if the new material is the default materials applied to the prefab
        if (newMaterial == "default")
        {
            //If the default materials haven't been set yet, the currently applied materials are the defaults
            if (defaultMatNames == null)
            {
                //Get a list of the default renderers attached to the game object
                Renderer[] defaultRenderers = GetComponentsInChildren<Renderer>();
                //Create a string array for the material names
                defaultMatNames = new string[defaultRenderers.Length];

                //Iterate through the renderers and save all of the material names
                for (int i = 0; i < defaultRenderers.Length; i++)
                    defaultMatNames[i] = defaultRenderers[i].material.name;
            }
            //If the default materials weren't set, apply the saved default materials
            else
            {
                //Get a list of the renderers currently attached to the game object
                Renderer[] renderers = GetComponentsInChildren<Renderer>();

                //Instantiate all of the default materials and assign them to the renderers
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].material = AssetManager.loadRcMaterial(defaultMatNames[i]);
            }
        }
        //Otherwise Apply the material to all of the children of the object
        else
        {
            foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                //Don't apply the default material and don't apply the material to the particle system of supply stations
                if (!(renderer.name.Contains("Particle System") && ObjectName.StartsWith("aot_supply")))
                    renderer.material = AssetManager.loadRcMaterial(newMaterial);
            }
        }
    }

    //Change the scale factor of the length, width, or height of the object
    public void scaleByFactor(Vector3 scaleFactor)
    {
        gameObject.transform.localScale = new Vector3(defaultScale.x * scaleFactor.x, defaultScale.y * scaleFactor.y, defaultScale.z * scaleFactor.z);
    }

    //Resize the texture on the object
    private void setTiling(Vector2 newTiling)
    {
        //If the material is the default on the gameobject, don't scale the texture
        if (materialValue == "default")
            return;

        //Apply the texture resizing to all of the children of the object
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
            renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * newTiling.x, renderer.material.mainTextureScale.y * newTiling.y);
    }

    //Change the color of the material on the object
    private void setColor(Color newColor)
    {
        //Iterate through all of the filters in the object
        foreach (MeshFilter filter in gameObject.GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = filter.mesh;

            //Create an array filled with the new color to apply to the mesh
            Color[] colorArray = new Color[mesh.vertexCount];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = (Color)newColor;

            //Apply the colors
            mesh.colors = colorArray;
        }
    }
    #endregion

    #region Parsing Utility Methods
    //A set of methods for parsing parts of an object script

    //Return the objectType assosiated with the given string
    public static objectType parseType(string typeString)
    {
        //Make a string array containing the names of each type of object
        string[] objectTypes = Enum.GetNames(typeof(objectType));

        //Check if the string matches any of the types
        foreach (string objectType in objectTypes)
        {
            //If the string matches a type, return that type
            if (typeString.StartsWith(objectType))
                return (objectType)Enum.Parse(typeof(objectType), objectType);
        }

        //If the object type is not valid, raise an error
        throw new Exception("The type '" + typeString + "' does not exist");
    }

    //Create a Color object with the three given color values and opacity
    public static Color parseColor(string r, string g, string b, string a)
    {
        return new Color(Convert.ToSingle(r), Convert.ToSingle(g), Convert.ToSingle(b), Convert.ToSingle(a));
    }

    //Create a vector with the two given strings
    public static Vector2 parseVector2(string x, string y)
    {
        return new Vector2(Convert.ToSingle(x), Convert.ToSingle(y));
    }

    //Create a vector with the three given strings
    public static Vector3 parseVector3(string x, string y, string z)
    {
        return new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
    }

    //Create a quaternion with the three given strings
    public static Quaternion parseQuaternion(string x, string y, string z, string w)
    {
        return new Quaternion(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToSingle(w));
    }
    #endregion

    #region Exporting Utility Methods
    //Convert a boolean to the string 1 or 0
    private string boolToString(bool boolToStringify)
    {
        if (boolToStringify)
            return "1";

        return "0";
    }

    //Convert a color to a script friendly string
    private string colorToString(Color colorToStrinfigy)
    {
        return colorToStrinfigy.r.ToString() + "," +
               colorToStrinfigy.g.ToString() + "," +
               colorToStrinfigy.b.ToString();
    }

    //Convert a vector2 to a script friendly string
    private string vector2ToString(Vector2 vectorToStringify)
    {
        return vectorToStringify.x.ToString() + ","+
               vectorToStringify.y.ToString();
    }
    //Convert a vector2 to a script friendly string
    private string vector3ToString(Vector3 vectorToStringify)
    {
        return vectorToStringify.x.ToString() + "," +
               vectorToStringify.y.ToString() + "," +
               vectorToStringify.z.ToString();
    }

    //Convert a vector2 to a script friendly string
    private string quaternionToString(Quaternion quatToStringify)
    {
        return quatToStringify.x.ToString() + "," +
               quatToStringify.y.ToString() + "," +
               quatToStringify.z.ToString() + "," +
               quatToStringify.w.ToString();
    }
    #endregion

    #region Methods
    //Takes an array containing a parsed object script and set all of the variables except for the type
    public void loadProperties(string[] properties)
    {
        //Save the number of properties the object hsa
        propertyNumber = properties.Length;

        //The position in the properties array where the position data starts.
        //Defaults to 3 for objects that only have a type, name, posiiton, and angle
        int indexOfPosition = 2;

        //Store the full type
        FullTypeName = properties[0];
        //Store the object name
        ObjectName = properties[1];
        //Save the default scale of the object
        defaultScale = transform.localScale;

        //If the object is a titan spawner, store the spawn timer and whether or not it spawns endlessly
        if (Type == objectType.photon && ObjectName.StartsWith("spawn"))
        {
            SpawnTimer = Convert.ToSingle(properties[2]);
            EndlessSpawn = (Convert.ToInt32(properties[3]) != 0);
            indexOfPosition = 4;
        }
        //If the object is a region, store the region name and scale the object
        else if (ObjectName.StartsWith("region"))
        {
            RegionName = properties[2];
            Scale = parseVector3(properties[3], properties[4], properties[5]);
            indexOfPosition = 6;
        }
        //If the object has a material, store the material, color, and tiling information and scale the object
        else if (Type == objectType.custom || propertyNumber >= 15 && (Type == objectType.@base || Type == objectType.photon))
        {
            Material = properties[2];
            Scale = parseVector3(properties[3], properties[4], properties[5]);
            ColorEnabled = (Convert.ToInt32(properties[6]) != 0);

            //If the color is enabled, parse the color and set it
            if (ColorEnabled)
            {
                //If the transparent material is applied, parse the opacity and use it. Otherwise default to fully opaque
                if (Material.StartsWith("transparent"))
                    Color = parseColor(properties[7], properties[8], properties[9], Material.Substring(11));
                else
                    Color = parseColor(properties[7], properties[8], properties[9], "1");
            }
            //Otherwise, use white as a default color
            else
                Color = Color.white;

            Tiling = parseVector2(properties[10], properties[11]);
            indexOfPosition = 12;
        }
        //If the object has scale information just before the position and rotation, scale the object
        else if (Type == objectType.racing || Type == objectType.misc)
        {
            Scale = parseVector3(properties[2], properties[3], properties[4]);
            indexOfPosition = 5;
        }

        //If the object is a spawnpoint, set its default size
        if (Type == objectType.spawnpoint || Type == objectType.photon)
            Scale = new Vector3(1f, 1f, 1f);

        //Set the position and rotation for all objects
        Position = parseVector3(properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++]);
        Rotation = parseQuaternion(properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++]);
    }

    //Convert the map object into a script
    public override string ToString()
    {
        //The exported object script. Every script starts with the type and name
        string objectScript = FullTypeName + "," + ObjectName;

        //Add properties to the script based on what type of object it is
        if (Type == objectType.photon && ObjectName.StartsWith("spawn"))
            objectScript += "," + SpawnTimer + "," + boolToString(EndlessSpawn);
        else if (ObjectName.StartsWith("region"))
            objectScript += "," + RegionName + "," + vector3ToString(Scale);
        else if (Type == objectType.custom || propertyNumber >= 15 && (Type == objectType.@base || Type == objectType.photon))
            objectScript += "," + Material + "," + vector3ToString(Scale) + "," + boolToString(ColorEnabled) + "," + colorToString(Color) + "," + vector2ToString(Tiling);
        else if (Type == objectType.racing || Type == objectType.misc)
            objectScript += "," + vector3ToString(Scale);

        //Add the position and rotation to all objects. Scale the position up by a factor of 10
        objectScript += "," + vector3ToString(Position) + "," + quaternionToString(Rotation) + ";";

        return objectScript;
    }
    #endregion
}