﻿using System;
using System.Text;
using UnityEngine;

namespace MapEditor
{
    public class TexturedObject : MapObject
    {
        #region Data Members
        private string[] defaultMatNames;
        private string materialValue;
        private Vector2 tilingValue;
        public bool ColorEnabled { get; set; }
        private Color colorValue;
        #endregion

        #region Properties
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
        #endregion

        #region Initialization
        //Copy the values from the given object
        public void copyValues(TexturedObject originalObject)
        {
            //Copy the generic map object values
            base.copyValues(originalObject);

            //Copy the texture related values
            materialValue = originalObject.Material;
            tilingValue = originalObject.Tiling;
            ColorEnabled = originalObject.ColorEnabled;
            colorValue = originalObject.Color;
        }
        #endregion

        #region Setters
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
                    for (int matIndex = 0; matIndex < defaultRenderers.Length; matIndex++)
                        defaultMatNames[matIndex] = defaultRenderers[matIndex].material.name;
                }
                //If the default materials weren't set, apply the saved default materials
                else
                {
                    //Get a list of the renderers currently attached to the game object
                    Renderer[] renderers = GetComponentsInChildren<Renderer>();

                    //Instantiate all of the default materials and assign them to the renderers
                    for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                        renderers[rendererIndex].material = AssetManager.loadRcMaterial(defaultMatNames[rendererIndex]);
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

                for (int colorIndex = 0; colorIndex < colorArray.Length; colorIndex++)
                    colorArray[colorIndex] = (Color)newColor;

                //Apply the colors
                mesh.colors = colorArray;
            }
        }
        #endregion

        #region Methods
        //Takes an array containing a parsed object script and set all of the variables except for the type
        public override void loadProperties(string[] properties)
        {
            base.loadProperties(properties);

            //If the script is too short, only parse the position and rotation
            if (properties.Length < 19)
                loadPropertiesPartial(properties);
            //Otherwise parse all of the object info from the script
            else
                loadPropertiesFull(properties);
        }

        //Load the position and rotation from the object script, and set the texture information to a default value
        private void loadPropertiesPartial(string[] properties)
        {
            Scale = defaultScale;
            Position = parseVector3(properties[2], properties[3], properties[4]);
            Rotation = parseQuaternion(properties[5], properties[6], properties[7], properties[8]);

            //Disable the color by default
            ColorEnabled = false;
            Color = Color.white;

            //Get a list of the renderers in the object hierarchy
            Renderer[] childRenderers = gameObject.GetComponentsInChildren<Renderer>();

            //Loop through the materials and check if they are the same
            for (int rendererIndex = 0; rendererIndex < childRenderers.Length; rendererIndex++)
            {
                //Skip the renderer if it is the particle system of the resupply station
                if (childRenderers[rendererIndex].name.Contains("Particle System") && ObjectName.StartsWith("aot_supply"))
                    continue;

                //Use the material and tiling of the first renderer for the object values
                if (rendererIndex == 0)
                {
                    materialValue = childRenderers[rendererIndex].material.name;
                    tilingValue = childRenderers[rendererIndex].material.mainTextureScale;
                }
                //If a subsequent renderer has different settings than the first, set the material and texture to null
                else if (childRenderers[rendererIndex].material.name != Material ||
                        childRenderers[rendererIndex].material.mainTextureScale != Tiling)
                {
                    materialValue = null;
                    tilingValue = Vector2.zero;
                    break;
                }
            }
        }

        //Load all of the object properties from the object script
        private void loadPropertiesFull(string[] properties)
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
            Position = parseVector3(properties[12], properties[13], properties[14]);
            Rotation = parseQuaternion(properties[15], properties[16], properties[17], properties[18]);
        }

        //Convert the map object into a script
        public override string ToString()
        {
            //Create a string builder to efficiently construct the script
            //Initialize with a starting buffer with enough room to fit a long object script
            StringBuilder scriptBuilder = new StringBuilder(100);

            //Append the object type and name to the script
            scriptBuilder.Append(FullTypeName + "," + ObjectName);
            //Append the material and scale values
            scriptBuilder.Append("," + Material + "," + vector3ToString(Scale) + "," + boolToString(ColorEnabled) + "," + colorToString(Color) + "," + vector2ToString(Tiling));
            //Append the transform values
            scriptBuilder.Append("," + vector3ToString(Position) + "," + quaternionToString(Rotation) + ";");

            //Get the script string and return it
            return scriptBuilder.ToString();
        }
        #endregion
    }
}