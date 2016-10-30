using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Collide;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class RigidBodyTool : Tool
  {
    public RigidBody RigidBody { get; private set; }

    public RigidBodyTool( RigidBody rb )
    {
      RigidBody = rb;
    }

    public override void OnAdd()
    {
    }

    public override void OnRemove()
    {
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      GUILayout.Label( GUI.MakeLabel( "Mass properties", true ), skin.label );
      using ( new GUI.Indent( 12 ) )
        BaseEditor<MassProperties>.Update( RigidBody.MassProperties, skin );
      GUI.Separator();
    }

    public override void OnPostTargetMembersGUI( GUISkin skin )
    {
      GUI.Separator();

      GUIStyle dragDropFieldStyle = new GUIStyle( skin.textArea );
      dragDropFieldStyle.alignment = TextAnchor.MiddleCenter;
      dragDropFieldStyle.richText = true;

      Rect dropArea = new Rect();
      GUILayout.BeginHorizontal();
      {
        GUILayout.Label( GUI.MakeLabel( "Assign Shape Material [" + GUI.AddColorTag( "drop area", Color.Lerp( Color.green, Color.black, 0.4f ) ) + "]",
                                        false,
                                        "Assigns dropped shape material to all shapes in this rigid body." ),
                         dragDropFieldStyle,
                         GUILayout.Height( 22 ) );
        dropArea = GUILayoutUtility.GetLastRect();

        bool resetMaterials = GUILayout.Button( GUI.MakeLabel( "Reset",
                                              false,
                                              "Reset shapes material to null." ),
                               skin.button,
                               GUILayout.Width( 42 ) ) &&
                               EditorUtility.DisplayDialog( "Reset shape materials", "Reset all shapes material to default [null]?", "OK", "Cancel" );
        if ( resetMaterials )
          AssignShapeMaterialToAllShapes( null );
      }
      GUILayout.EndHorizontal();

      if ( ( Event.current.type == EventType.DragPerform || Event.current.type == EventType.DragUpdated ) && dropArea.Contains( Event.current.mousePosition ) ) {
        bool validObject = DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[ 0 ] is ShapeMaterial;
        DragAndDrop.visualMode = validObject ?
                                   DragAndDropVisualMode.Copy :
                                   DragAndDropVisualMode.Rejected;

        if ( Event.current.type == EventType.DragPerform && validObject ) {
          DragAndDrop.AcceptDrag();

          AssignShapeMaterialToAllShapes( DragAndDrop.objectReferences[ 0 ] as ShapeMaterial );
        }
      }

      GUI.Separator();

      OnShapeListGUI( skin );
    }

    private void OnShapeListGUI( GUISkin skin )
    {
      if ( !GUI.Foldout( EditorData.Instance.GetData( RigidBody, "Shapes" ), GUI.MakeLabel( "Shapes", true ), skin ) )
        return;

      Shape[] shapes = RigidBody.GetComponentsInChildren<Shape>();
      if ( shapes.Length == 0 ) {
        using ( new GUI.Indent( 12 ) )
          GUILayout.Label( GUI.MakeLabel( "Empty", true ), skin.label );
        return;
      }

      using ( new GUI.Indent( 12 ) ) {
        foreach ( var shape in shapes ) {
          GUI.Separator();
          if ( !GUI.Foldout( EditorData.Instance.GetData( RigidBody,
                                                          shape.GetInstanceID().ToString() ),
                             GUI.MakeLabel( "[" + GUI.AddColorTag( shape.GetType().Name, Color.Lerp( Color.green, Color.black, 0.4f ) ) + "] " + shape.name ),
                             skin ) )
            continue;

          GUI.Separator();
          using ( new GUI.Indent( 12 ) )
            BaseEditor<Shape>.Update( shape, skin );
        }
      }
    }

    private void AssignShapeMaterialToAllShapes( ShapeMaterial shapeMaterial )
    {
      Shape[] shapes = RigidBody.GetComponentsInChildren<Shape>();
      foreach ( var shape in shapes )
        shape.Material = shapeMaterial;

      RigidBody.UpdateMassProperties();
    }
  }
}
