using UnityEngine;
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
          ShapeResizeTool shapeResizeTool = new ShapeResizeTool( Shape );
          shapeResizeTool.ActivateKey.HideDefaultHandlesWhenIsDown = true;
          AddChild( shapeResizeTool );
          Manager.RequestSceneViewFocus();
        }
        else if ( !value )
          RemoveChild( GetChild<ShapeResizeTool>() );
      }
    }

    public ShapeTool( Shape shape )
    {
      Shape = shape;
    }

    public override void OnAdd()
    {
    }

    public override void OnRemove()
    {
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      bool guiWasEnabled               = UnityEngine.GUI.enabled;
      const char shapeResizeToolSymbol = '\u21C4';

      bool toggleShapeResizeTool = false;
      EditorGUILayout.BeginHorizontal();
      {
        GUI.ToolsLabel( skin );

        using ( GUI.ToolButtonData.ColorBlock ) {
          UnityEngine.GUI.enabled = Tools.ShapeResizeTool.SupportsShape( Shape );
          toggleShapeResizeTool = GUILayout.Button( GUI.MakeLabel( shapeResizeToolSymbol.ToString(), false, "Shape resize tool" ),
                                                    GUI.ConditionalCreateSelectedStyle( ShapeResizeTool, GUI.ToolButtonData.Style( skin, 24 ) ),
                                                    GUI.ToolButtonData.Width, GUI.ToolButtonData.Height );
          UnityEngine.GUI.enabled = guiWasEnabled;
        }
      }
      EditorGUILayout.EndHorizontal();

      if ( toggleShapeResizeTool )
        ShapeResizeTool = !ShapeResizeTool;
    }
  }
}
