using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class SelectGameObjectTool : Tool
  {
    public SelectGameObjectDropdownMenuTool MenuTool { get { return GetChild<SelectGameObjectDropdownMenuTool>(); } }

    public bool SelectionWindowActive { get { return MenuTool.WindowIsActive; } }

    public SelectGameObjectTool( Action<GameObject> onSelectCallback )
    {
      SelectGameObjectDropdownMenuTool menuTool = new SelectGameObjectDropdownMenuTool()
      {
        HideOnCameraControl = true,
        HideOnKeyEscape     = true,
        HideOnClickMiss     = true
      };

      AddChild( menuTool );
      menuTool.OnSelect += onSelectCallback;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( ( !MenuTool.WindowIsActive || MenuTool.Target != Manager.MouseOverObject ) && Manager.HijackLeftMouseClick() ) {
        MenuTool.Target = Manager.MouseOverObject;
        MenuTool.Show();
      }
    }
  }
}
