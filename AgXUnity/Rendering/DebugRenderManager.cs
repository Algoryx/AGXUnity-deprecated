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
  [AddComponentMenu( "" )]
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

    /// <summary>
    /// Called on LateUpdate from shapes without rigid bodies.
    /// </summary>
    public static void OnLateUpdate( Collide.Shape shape )
    {
      if ( !HasInstance )
        return;

      Instance.SynchronizeShape( shape );
    }

    /// <summary>
    /// Called on LateUpdate from bodies to synchronize debug rendering of the shapes.
    /// </summary>
    public static void OnLateUpdate( RigidBody rb )
    {
      if ( !HasInstance )
        return;

      Collide.Shape[] shapes = rb.GetComponentsInChildren<Collide.Shape>();
      foreach ( Collide.Shape shape in shapes )
        Instance.SynchronizeShape( shape );
    }

    [SerializeField]
    private bool m_visible = true;
    public bool Visible
    {
      get { return m_visible; }
      set
      {
        if ( m_visible != value )
          gameObject.SetActive( value );
        m_visible = value;
      }
    }

    protected override bool Initialize()
    {
      gameObject.isStatic  = true;
      gameObject.hideFlags = HideFlags.None;

      return base.Initialize();
    }

    protected void Update()
    {
      gameObject.transform.position   = Vector3.zero;
      gameObject.transform.rotation   = Quaternion.identity;
      // Change parent before scale is set - otherwise scale will be preserved.
      // E.g., move "this" to a parent with scale x, scale will be set,
      // parent = null will remove the parent but the scale will be preserved.
      // Fix - set scale after set parent.
      gameObject.transform.parent     = null;
      gameObject.transform.localScale = Vector3.one;

      // When the application is playing we rely on callbacks
      // from the objects when they've synchronized their
      // transforms.
      if ( Application.isPlaying )
        return;

      UnityEngine.Object.FindObjectsOfType<Collide.Shape>().ToList().ForEach(
        shape => SynchronizeShape( shape )
      );
    }

    private void SynchronizeShape( Collide.Shape shape )
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
  }
}
