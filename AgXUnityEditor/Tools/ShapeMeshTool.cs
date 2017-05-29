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

      var sourceObjects = Mesh.SourceObjects;
      var singleSource  = sourceObjects.FirstOrDefault();

      Undo.RecordObjects( Mesh.GetUndoCollection(), "Mesh source" );

      var newSingleSource = GUI.ShapeMeshSourceGUI( singleSource, skin );
      if ( newSingleSource != null )
        Mesh.SetSourceObject( newSingleSource );

      GUI.Separator();
    }
  }
}
