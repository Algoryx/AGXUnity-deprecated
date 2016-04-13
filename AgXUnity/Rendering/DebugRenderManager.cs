using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity.Rendering
{
  /// <summary>
  /// Debug render manager singleton object, managing debug render data.
  /// This object is active in editor.
  /// </summary>
  [ExecuteInEditMode]
  [GenerateCustomEditor]
  public class DebugRenderManager : UniqueGameObject<DebugRenderManager>
  {
    /// <summary>
    /// BaseEditor.cs is calling this method when the editor receives
    /// an OnDestroy call and the application isn't playing. This
    /// behavior is assumed to be a "select -> delete".
    /// </summary>
    /// <param name="gameObject"></param>
    public static void OnEditorDestroy()
    {
      if ( !HasInstance )
        return;

      List<GameObject> gameObjectsToDestroy = new List<GameObject>();
      foreach ( Transform childTransform in Instance.gameObject.transform ) {
        GameObject child = childTransform.gameObject;
        OnSelectionProxy selectionProxy = child.GetComponent<OnSelectionProxy>();
        if ( selectionProxy != null )
          gameObjectsToDestroy.Add( selectionProxy.gameObject );
      }

      while ( gameObjectsToDestroy.Count > 0 ) {
        GameObject.DestroyImmediate( gameObjectsToDestroy[ gameObjectsToDestroy.Count - 1 ] );
        gameObjectsToDestroy.RemoveAt( gameObjectsToDestroy.Count - 1 );
      }
    }

    protected override bool Initialize()
    {
      gameObject.transform.position = Vector3.zero;
      gameObject.transform.rotation = Quaternion.identity;
      gameObject.isStatic           = true;
      gameObject.hideFlags          = HideFlags.None;

      return base.Initialize();
    }

    protected void Update()
    {
      gameObject.transform.position = Vector3.zero;
      gameObject.transform.rotation = Quaternion.identity;

      UnityEngine.Object.FindObjectsOfType<Collide.Shape>().ToList().ForEach(
        shape =>
        {
          var data = shape.gameObject.GetOrCreateComponent<ShapeDebugRenderData>();

          data.Synchronize();
          if ( data.Node == null )
            return;

          data.Node.hideFlags = HideFlags.DontSave;
          data.Node.GetOrCreateComponent<OnSelectionProxy>().Target = shape.gameObject;

          if ( data.Node && data.Node.transform.parent != gameObject.transform )
            gameObject.AddChild( data.Node );
        }
      );
    }
  }
}
