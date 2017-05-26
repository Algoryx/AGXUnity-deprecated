using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof( AgXUnity.Collide.Mesh ) )]
  public class ShapeMeshTool : ShapeTool
  {
    public AgXUnity.Collide.Mesh Mesh { get { return Shape as AgXUnity.Collide.Mesh; } }

    public ShapeMeshTool( AgXUnity.Collide.Shape shape )
      : base( shape )
    {
    }

    public override void OnAdd()
    {
      base.OnAdd();
    }

    public override void OnRemove()
    {
      base.OnRemove();
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      base.OnPreTargetMembersGUI( skin );

      var sourceObjects    = Mesh.SourceObjects;
      var singleSource     = sourceObjects.FirstOrDefault();
      Mesh newSingleSource = null;

      GUILayout.BeginHorizontal();
      {
        GUILayout.Label( GUI.MakeLabel( "Source:" ), skin.label, GUILayout.Width( 76 ) );
        newSingleSource = EditorGUILayout.ObjectField( singleSource, typeof( Mesh ), false ) as Mesh;
      }
      GUILayout.EndHorizontal();

      if ( newSingleSource != singleSource )
        Mesh.SetSourceObject( newSingleSource );

      GUI.Separator();
    }
  }
}
