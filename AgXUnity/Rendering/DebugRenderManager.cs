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
      if ( !IsActiveForSynchronize )
        return;

      Instance.SynchronizeScaleIfNodeExist( shape );
    }

    /// <summary>
    /// Use when Collide.Mesh source objects is updated.
    /// </summary>
    public static void HandleMeshSource( Collide.Mesh mesh )
    {
      if ( !IsActiveForSynchronize )
        return;

      Instance.SynchronizeShape( mesh );
    }

    /// <summary>
    /// Called after Simulation.StepForward from shapes without rigid bodies.
    /// </summary>
    public static void OnPostSynchronizeTransforms( Collide.Shape shape )
    {
      if ( !IsActiveForSynchronize )
        return;

      Instance.SynchronizeShape( shape );
    }

    /// <summary>
    /// Callback from Shape.OnDisable to catch and find disabled shapes,
    /// disabling debug render node.
    /// </summary>
    public static void OnShapeDisable( Collide.Shape shape )
    {
      if ( !IsActiveForSynchronize )
        return;

      var data = shape.GetComponent<ShapeDebugRenderData>();
      if ( data.Node != null )
        data.Node.SetActive( false );
    }

    /// <summary>
    /// Called after Simulation.StepForward from bodies to synchronize debug rendering of the shapes.
    /// </summary>
    public static void OnPostSynchronizeTransforms( RigidBody rb )
    {
      if ( !IsActiveForSynchronize )
        return;

      Collide.Shape[] shapes = rb.GetComponentsInChildren<Collide.Shape>();
      foreach ( Collide.Shape shape in shapes )
        Instance.SynchronizeShape( shape );
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

        transform.hideFlags = gameObject.hideFlags | HideFlags.NotEditable;
      }
    }

    protected override bool Initialize()
    {
      gameObject.hideFlags = HideFlags.None;

      return base.Initialize();
    }

    protected override void OnEnable()
    {
      SetVisible( true );

      base.OnEnable();

      UpdateIsActiveForSynchronize();
    }

    protected override void OnDisable()
    {
      SetVisible( false );

      base.OnDisable();

      UpdateIsActiveForSynchronize();
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

      UpdateIsActiveForSynchronize();

      // When the application is playing we rely on callbacks
      // from the objects when they've synchronized their
      // transforms.
      if ( Application.isPlaying )
        return;

      // Shapes with inactive game objects will be updated below when we're
      // traversing all children.
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
        // FindObjectsOfType will not include the Shape if its game object is inactive.
        // We're handling that shape here instead.
        else if ( !proxy.Target.activeInHierarchy && proxy.Component is Collide.Shape )
          SynchronizeShape( proxy.Component as Collide.Shape );
      }

      while ( gameObjectsToDestroy.Count > 0 ) {
        DestroyImmediate( gameObjectsToDestroy.Last() );
        gameObjectsToDestroy.RemoveAt( gameObjectsToDestroy.Count - 1 );
      }
    }

    private static bool m_isActiveForSynchronize = false;
    private static bool IsActiveForSynchronize { get { return m_isActiveForSynchronize; } }

    private bool UpdateIsActiveForSynchronize()
    {
      return ( m_isActiveForSynchronize = gameObject.activeInHierarchy && enabled );
    }

    private void SynchronizeShape( Collide.Shape shape )
    {
      var data = shape.gameObject.GetOrCreateComponent<ShapeDebugRenderData>();
      bool shapeEnabled = shape.IsEnabledInHierarchy;

      if ( data.hideFlags != HideFlags.HideInInspector )
        data.hideFlags = HideFlags.HideInInspector;

      // Do not create debug render data if the shape is inactive.
      if ( !shapeEnabled && data.Node == null )
        return;

      data.Synchronize( this );
      if ( data.Node != null && shapeEnabled != data.Node.activeSelf )
        data.Node.SetActive( shapeEnabled );
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
