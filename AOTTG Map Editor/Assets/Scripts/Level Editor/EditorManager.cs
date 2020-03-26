﻿using UnityEngine;
using GILES;

namespace MapEditor
{
    //A singleton class for managing the current mode
    public class EditorManager : MonoBehaviour
    {
        //A self-reference to the singleton instance of this script
        public static EditorManager Instance { get; private set; }
        //Determines if the user is in fly more or edit mode
        public static EditorMode currentMode { get; set; }

        void Awake()
        {
            //Set this script as the only instance of the EditorManger script
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            //Set the screen resolution
            Screen.fullScreen = false;
            Screen.SetResolution(800, 600, false);

            //The editor is in edit mode by default
            currentMode = EditorMode.Edit;
        }

        void Update()
        {
            //If the x key is pressed and the tool handle is not being dragged,
            //toggle between edit and fly mode
            if (Input.GetKeyDown(KeyCode.X) && !SelectionHandle.InUse())
            {
                if (currentMode == EditorMode.Fly)
                {
                    currentMode = EditorMode.Edit;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else if (currentMode == EditorMode.Edit)
                {
                    currentMode = EditorMode.Fly;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void LateUpdate()
        {
            SelectionHandle.Instance.lateUpdate();
            ObjectSelection.Instance.lateUpdate();
        }
    }
}