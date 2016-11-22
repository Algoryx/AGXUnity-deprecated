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
        DestroyImmediate( gameObjectsToDestroy[ gameObjectsToDestroy.Count - 1 ] );
        gameObjectsToDestroy.RemoveAt( gameObjectsToDestroy.Count - 1 );
      }
    }

    /// <summary>
    /// Callback from Collide.Shape when the size of a shape has been changed.
    /// </summary>
    /// <param name="shape"></param>
    public static void SynchronizeScale( Collide.Shape shape )
    {
      if ( !ActiveForSynchronize )
        return;

      Instance.SynchronizeScaleIfNodeExist( shape );
    }

    /// <summary>
    /// Use when Collide.Mesh source objects is updated.
    /// </summary>
    public static void HandleMeshSource( Collide.Mesh mesh )
    {
      if ( !ActiveForSynchronize )
        return;

      Instance.SynchronizeShape( mesh );
    }

    /// <summary>
    /// Called after Simulation.StepForward from shapes without rigid bodies.
    /// </summary>
    public static void OnPostSynchronizeTransforms( Collide.Shape shape )
    {
      if ( !ActiveForSynchronize )
        return;

      Instance.SynchronizeShape( shape );
    }

    /// <summary>
    /// Called after Simulation.StepForward from bodies to synchronize debug rendering of the shapes.
    /// </summary>
    public static void OnPostSynchronizeTransforms( RigidBody rb )
    {
      if ( !ActiveForSynchronize )
        return;

      Collide.Shape[] shapes = rb.GetComponentsInChildren<Collide.Shape>();
      foreach ( Collide.Shape shape in shapes )
        Instance.SynchronizeShape( shape );
    }

    /// <summary>
    /// Checks if <paramref name="gameObject"/> is active in hierarchy that any
    /// shape and rigid body component present are enabled.
    /// </summary>
    public static bool IsActiveShape( GameObject gameObject )
    {
      if ( gameObject == null || !gameObject.activeInHierarchy )
        return false;

      var rb    = gameObject.GetComponentInParent<RigidBody>();
      var shape = gameObject.GetComponent<Collide.Shape>();
      return shape != null && shape.enabled && ( rb == null || rb.enabled );
    }

    /// <summary>
    /// Visualizes shapes and visuals in bodies with different colors (wire frame gizmos).
    /// </summary>
    public bool ColorizeBodies = false;

    /// <summary>
    /// Highlights the shape or visual the mouse is currently hovering in the scene view.
    /// </summary>
    public bool HighlightMouseOverObject = false;

    [SerializeField]
    private bool m_includeInBuild = false;
    public bool IncludeInBuild
    {
      get { return m_includeInBuild; }
      set
      {
        m_includeInBuild = value;
        if ( m_includeInBuild )
          gameObject.hideFlags = HideFlags.None;
        else
          gameObject.hideFlags = HideFlags.DontSaveInBuild;
      }
    }

    protected override bool Initialize()
    {
      gameObject.isStatic  = true;
      gameObject.hideFlags = HideFlags.None;

      return base.Initialize();
    }

    protected override void OnEnable()
    {
      SetVisible( true );

      base.OnEnable();
    }

    protected override void OnDisable()
    {
      SetVisible( false );

      base.OnDisable();
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

      FindObjectsOfType<Collide.Shape>().ToList().ForEach(
        shape => SynchronizeShape( shape )
      );

      FindObjectsOfType<Constraint>().ToList().ForEach(
        constraint => constraint.AttachmentPair.Update()
      );

      List<GameObject> gameObjectsToDestroy = new List<GameObject>();
      foreach ( Transform child in gameObject.transform ) {
        GameObject node        = child.gameObject;
        OnSelectionProxy proxy = node.GetComponent<OnSelectionProxy>();
        if ( proxy == null )
          continue;

        if ( proxy.Target == null )
          gameObjectsToDestroy.Add( node );
        else if ( IsActiveShape( proxy.Target ) != node.activeSelf )
          node.SetActive( !node.activeSelf );
      }

      while ( gameObjectsToDestroy.Count > 0 ) {
        DestroyImmediate( gameObjectsToDestroy.Last() );
        gameObjectsToDestroy.RemoveAt( gameObjectsToDestroy.Count - 1 );
      }
    }

    private static bool ActiveForSynchronize { get { return HasInstance && Instance.gameObject.activeInHierarchy && Instance.isActiveAndEnabled; } }

    private void SynchronizeShape( Collide.Shape shape )
    {
      var data = shape.gameObject.GetOrCreateComponent<ShapeDebugRenderData>();
      data.Synchronize( this );
    }

    private void SynchronizeScaleIfNodeExist( Collide.Shape shape )
    {
      var data = shape.gameObject.GetComponent<ShapeDebugRenderData>();
      if ( data != null )
        data.SynchronizeScale( shape );
    }

    private void SetVisible( bool visible )
    {
      foreach ( Transform child in transform )
        child.gameObject.SetActive( visible );
    }
  }
}
