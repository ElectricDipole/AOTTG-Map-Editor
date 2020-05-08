﻿using System.Text;
using UnityEngine;

namespace MapEditor
{
    public class RegionObject : MapObject
    {
        #region Fields
        //The underlying value for the RegionName property
        private string regionNameValue;
        //The file path to the billboard prefab
        private const string billboardPrefabPath = "Editor Resources/Editor Prefabs/Billboard";
        //The text component on the billboard script
        private TextMesh billboardContent;
        #endregion

        #region Properties
        //The name of the region if the object is a region
        public string RegionName
        {
            get { return regionNameValue; }

            set
            {
                regionNameValue = value;
                billboardContent.text = value;
            }
        }

        //Prevent the region from being rotated
        public override Quaternion Rotation
        {
            get { return Quaternion.identity; }
            set { }
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            //Intantiate a billboard and set it as a child of the region
            GameObject billboard = Instantiate((GameObject)Resources.Load(billboardPrefabPath));
            billboard.transform.parent = gameObject.transform;

            //Save a reference to the text mesh component
            billboardContent = billboard.GetComponent<TextMesh>();
        }

        //Copy the values from the given object
        public void copyValues(RegionObject originalObject)
        {
            base.copyValues(originalObject);

            RegionName = originalObject.RegionName;
        }

        //Sets all of the object properties except for the type based on the parsed object script
        public override void loadProperties(string[] properties)
        {
            base.loadProperties(properties);

            RegionName = properties[2];
            Scale = parseVector3(properties[3], properties[4], properties[5]);
            Position = parseVector3(properties[6], properties[7], properties[8]);
            Rotation = Quaternion.identity;
        }
        #endregion

        #region Update
        //If the object was rotated, set it back to the default rotation
        private void LateUpdate()
        {
            if (transform.hasChanged)
            {
                transform.rotation = Quaternion.identity;
                transform.hasChanged = false;
            }
        }
        #endregion

        #region Methods
        //Convert the map object into a script
        public override string ToString()
        {
            //Create a string builder to efficiently construct the script
            //Initialize with a starting buffer with enough room to fit a long object script
            StringBuilder scriptBuilder = new StringBuilder(100);

            //Append the object type and name to the script
            scriptBuilder.Append(FullTypeName + "," + ObjectName + "," + RegionName);
            //Append the transform values
            scriptBuilder.Append("," + vector3ToString(Scale) + "," + vector3ToString(Position) + "," + quaternionToString(Rotation) + ";");

            //Get the script string and return it
            return scriptBuilder.ToString();
        }
        #endregion
    }
}