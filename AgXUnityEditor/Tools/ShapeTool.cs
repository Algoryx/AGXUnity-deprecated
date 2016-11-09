﻿using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Collide;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class ShapeTool : Tool
  {
    public Shape Shape { get; private set; }

    public bool ShapeResizeTool
    {
      get { return GetChild<ShapeResizeTool>() != null; }
      set
      {
        if ( value && GetChild<ShapeResizeTool>() == null ) {
          ShapeResizeTool shapeResizeTool                                = new ShapeResizeTool( Shape );
          shapeResizeTool.ActivateKey.HideDefaultHandlesWhenIsDown       = true;
          shapeResizeTool.SymmetricScaleKey.HideDefaultHandlesWhenIsDown = true;
          shapeResizeTool.RemoveOnKeyEscape                              = true;

          AddChild( shapeResizeTool );

          Manager.RequestSceneViewFocus();
        }
        else if ( !value )
          RemoveChild( GetChild<ShapeResizeTool>() );
      }
    }

    public bool ShapeCreateTool
    {
      get { return GetChild<ShapeCreateTool>() != null; }
      set
      {
        if ( value && !ShapeCreateTool ) {
          RemoveAllChildren();

          var shapeCreateTool = new ShapeCreateTool( Shape.gameObject );
          AddChild( shapeCreateTool );
        }
        else if ( !value )
          RemoveChild( GetChild<ShapeCreateTool>() );
      }
    }

    public ShapeTool( Shape shape )
    {
      Shape = shape;
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      bool guiWasEnabled = UnityEngine.GUI.enabled;
      bool toggleShapeResizeTool = false;
      bool toggleShapeCreate = false;

      EditorGUILayout.BeginHorizontal();
      {
        GUI.ToolsLabel( skin );

        using ( GUI.ToolButtonData.ColorBlock ) {
          UnityEngine.GUI.enabled = Tools.ShapeResizeTool.SupportsShape( Shape );
          toggleShapeResizeTool   = GUI.ToolButton( GUI.Symbols.ShapeResizeTool, ShapeResizeTool, "Shape resize tool", skin, 24 );
          UnityEngine.GUI.enabled = guiWasEnabled;

          toggleShapeCreate = GUI.ToolButton( GUI.Symbols.ShapeCreateTool, ShapeCreateTool, "Create shape from visual objects", skin );
        }
      }
      EditorGUILayout.EndHorizontal();

      if ( ShapeCreateTool ) {
        GUI.Separator();

        GetChild<ShapeCreateTool>().OnInspectorGUI( skin );

        GUI.Separator();
      }

      if ( toggleShapeResizeTool )
        ShapeResizeTool = !ShapeResizeTool;
      if ( toggleShapeCreate )
        ShapeCreateTool = !ShapeCreateTool;
    }
  }
}
