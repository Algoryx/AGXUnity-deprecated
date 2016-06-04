using System;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Tools
{
  public class SelectGameObjectTool : Tool
  {
    public SelectGameObjectDropdownMenuTool MenuTool { get { return GetChild<SelectGameObjectDropdownMenuTool>(); } }

    public bool SelectionWindowActive { get { return MenuTool != null && MenuTool.WindowIsActive; } }

    public Action<GameObject> OnSelect = delegate { };

    public SelectGameObjectTool()
    {
      SelectGameObjectDropdownMenuTool menuTool = new SelectGameObjectDropdownMenuTool();
      menuTool.RemoveOnClickMiss = false;
      menuTool.OnSelect = go =>
      {
        OnSelect( go );
        PerformRemoveFromParent();
      };
      AddChild( menuTool );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      OnSceneViewGUIChildren( sceneView );

      if ( MenuTool == null ) {
        PerformRemoveFromParent();
        return;
      }

      if ( ( !MenuTool.WindowIsActive || MenuTool.Target != Manager.MouseOverObject ) && Manager.HijackLeftMouseClick() ) {
        MenuTool.Target = Manager.MouseOverObject;
        MenuTool.Show();
      }
    }
  }
}
