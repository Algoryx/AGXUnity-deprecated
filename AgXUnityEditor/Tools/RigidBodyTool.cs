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

    public bool FindTransformGivenPointTool
    {
      get { return GetChild<FindPointTool>() != null; }
      set
      {
        if ( value && !FindTransformGivenPointTool ) {
          RemoveAllChildren();

          var pointTool = new FindPointTool();
          pointTool.OnPointFound = data =>
          {
            Undo.RecordObject( RigidBody.transform, "Rigid body transform" );

            RigidBody.transform.position = data.Triangle.Point;
            RigidBody.transform.rotation = data.Rotation;

            EditorUtility.SetDirty( RigidBody );
          };

          AddChild( pointTool );
        }
        else if ( !value )
          RemoveChild( GetChild<FindPointTool>() );
      }
    }

    public bool FindTransformGivenEdgeTool
    {
      get { return GetChild<EdgeDetectionTool>() != null; }
      set
      {
        if ( value && !FindTransformGivenEdgeTool ) {
          RemoveAllChildren();

          var edgeTool = new EdgeDetectionTool();
          edgeTool.OnEdgeFound = data =>
          {
            Undo.RecordObject( RigidBody.transform, "Rigid body transform" );

            RigidBody.transform.position = data.Position;
            RigidBody.transform.rotation = data.Rotation;

            EditorUtility.SetDirty( RigidBody );
          };

          AddChild( edgeTool );
        }
        else if ( !value )
          RemoveChild( GetChild<EdgeDetectionTool>() );
      }
    }

    public bool ShapeCreateTool
    {
      get { return GetChild<ShapeCreateTool>() != null; }
      set
      {
        if ( value && !ShapeCreateTool ) {
          RemoveAllChildren();

          var shapeCreateTool = new ShapeCreateTool( RigidBody.gameObject );
          AddChild( shapeCreateTool );
        }
        else if ( !value )
          RemoveChild( GetChild<ShapeCreateTool>() );
      }
    }

    public bool ConstraintCreateTool
    {
      get { return GetChild<ConstraintCreateTool>() != null; }
      set
      {
        if ( value && !ConstraintCreateTool ) {
          RemoveAllChildren();

          var constraintCreateTool = new ConstraintCreateTool( RigidBody.gameObject, false );
          AddChild( constraintCreateTool );
        }
        else if ( !value )
          RemoveChild( GetChild<ConstraintCreateTool>() );
      }
    }

    public bool DisableCollisionsTool
    {
      get { return GetChild<DisableCollisionsTool>() != null; }
      set
      {
        if ( value && !DisableCollisionsTool ) {
          RemoveAllChildren();

          var disableCollisionsTool = new DisableCollisionsTool( RigidBody.gameObject );
          AddChild( disableCollisionsTool );
        }
        else if ( !value )
          RemoveChild( GetChild<DisableCollisionsTool>() );
      }
    }

    public RigidBodyTool( RigidBody rb )
    {
      RigidBody = rb;
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      bool toggleFindTransformGivenPoint = false;
      bool toggleFindTransformGivenEdge  = false;
      bool toggleShapeCreate             = false;
      bool toggleConstraintCreate        = false;
      bool toggleDisableCollisions       = false;

      GUILayout.BeginHorizontal();
      {
        GUI.ToolsLabel( skin );
        using ( GUI.ToolButtonData.ColorBlock ) {
          toggleFindTransformGivenPoint = GUI.ToolButton( GUI.Symbols.SelectPointTool, FindTransformGivenPointTool, "Find rigid body transform given point on object.", skin );
          toggleFindTransformGivenEdge  = GUI.ToolButton( GUI.Symbols.SelectEdgeTool, FindTransformGivenEdgeTool, "Find rigid body transform given edge on object.", skin );
          toggleShapeCreate             = GUI.ToolButton( GUI.Symbols.ShapeCreateTool, ShapeCreateTool, "Create shape from visual objects", skin );
          toggleConstraintCreate        = GUI.ToolButton( GUI.Symbols.ConstraintCreateTool, ConstraintCreateTool, "Create constraint to this rigid body", skin );
          toggleDisableCollisions       = GUI.ToolButton( GUI.Symbols.DisableCollisionsTool, DisableCollisionsTool, "Disable collisions against other objects", skin );
        }
      }
      GUILayout.EndHorizontal();

      if ( ShapeCreateTool ) {
        GUI.Separator();

        GetChild<ShapeCreateTool>().OnInspectorGUI( skin );
      }
      if ( ConstraintCreateTool ) {
        GUI.Separator();

        GetChild<ConstraintCreateTool>().OnInspectorGUI( skin );
      }
      if ( DisableCollisionsTool ) {
        GUI.Separator();

        GetChild<DisableCollisionsTool>().OnInspectorGUI( skin );
      }

      GUI.Separator();

      GUILayout.Label( GUI.MakeLabel( "Mass properties", true ), skin.label );
      using ( new GUI.Indent( 12 ) )
        BaseEditor<MassProperties>.Update( RigidBody.MassProperties, skin );
      GUI.Separator();

      if ( toggleFindTransformGivenPoint )
        FindTransformGivenPointTool = !FindTransformGivenPointTool;
      if ( toggleFindTransformGivenEdge )
        FindTransformGivenEdgeTool = !FindTransformGivenEdgeTool;
      if ( toggleShapeCreate )
        ShapeCreateTool = !ShapeCreateTool;
      if ( toggleConstraintCreate )
        ConstraintCreateTool = !ConstraintCreateTool;
      if ( toggleDisableCollisions )
        DisableCollisionsTool = !DisableCollisionsTool;
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
