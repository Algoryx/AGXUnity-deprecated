using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class ConstraintTool : Tool
  {
    public Constraint Constraint { get; private set; }

    public FrameTool ReferenceFrameTool { get; private set; }
    
    public FrameTool ConnectedFrameTool { get; private set; }

    public ConstraintTool( Constraint constraint )
    {
      Constraint = constraint;
    }

    public override void OnAdd()
    {
      HideDefaultHandlesEnableWhenRemoved();

      ReferenceFrameTool = new FrameTool( Constraint.AttachmentPair.ReferenceFrame ) { OnChangeDirtyTarget = Constraint };
      ConnectedFrameTool = new FrameTool( Constraint.AttachmentPair.ConnectedFrame ) { OnChangeDirtyTarget = Constraint, TransformHandleActive = !Constraint.AttachmentPair.Synchronized };

      AddChild( ReferenceFrameTool );
      AddChild( ConnectedFrameTool );
    }

    public override void OnRemove()
    {
      RemoveChild( ReferenceFrameTool );
      RemoveChild( ConnectedFrameTool );

      ReferenceFrameTool = ConnectedFrameTool = null;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      OnSceneViewGUIChildren( sceneView );
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      GUI.Separator();

      GUILayout.Label( GUI.MakeLabel( Constraint.Type.ToString(), true ) );

      using ( new GUI.Indent( 12 ) ) {
        GUILayout.Label( GUI.MakeLabel( "Reference frame", true ) );
        GUI.HandleFrame( Constraint.AttachmentPair.ReferenceFrame, skin, 4 + 12 );
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space( 12 );
        if ( GUILayout.Button( GUI.MakeLabel( "\u2194", false, "Synchronized with reference frame" ),
                               GUI.ConditionalCreateSelectedStyle( Constraint.AttachmentPair.Synchronized, skin.button ),
                               new GUILayoutOption[] { GUILayout.Width( 24 ), GUILayout.Height( 14 ) } ) ) {
          Constraint.AttachmentPair.Synchronized = !Constraint.AttachmentPair.Synchronized;
          if ( Constraint.AttachmentPair.Synchronized )
            ConnectedFrameTool.TransformHandleActive = false;
        }
        GUILayout.Label( GUI.MakeLabel( "Connected frame", true ) );
        EditorGUILayout.EndHorizontal();
        UnityEngine.GUI.enabled = !Constraint.AttachmentPair.Synchronized;
        GUI.HandleFrame( Constraint.AttachmentPair.ConnectedFrame, skin, 4 + 12 );
        UnityEngine.GUI.enabled = Constraint.AttachmentPair.Synchronized;
      }

      GUI.Separator();
    }
  }
}
