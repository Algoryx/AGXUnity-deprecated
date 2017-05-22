using System;
using System.Linq;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity.Rendering
{
  /// <summary>
  /// Base class for visualization of shapes.
  /// </summary>
  [ExecuteInEditMode]
  [DoNotGenerateCustomEditor]
  public class ShapeVisual : ScriptComponent
  {
    /// <summary>
    /// Create given shape of supported type (SupportsShapeVisual == true).
    /// </summary>
    /// <param name="shape">Shape to create visual for.</param>
    /// <returns>Game object with ShapeVisual component if successful, otherwise null.</returns>
    public static GameObject Create( Collide.Shape shape )
    {
      if ( !SupportsShapeVisual( shape ) )
        return null;

      return CreateInstance( shape, false );
    }

    /// <summary>
    /// Create render data for given shape. Render data is when you have
    /// your own representation of the mesh and material.
    /// 
    /// The component added will be ShapeVisualRenderData regardless of the
    /// type of the shape.
    /// </summary>
    /// <param name="shape">Shape to create render data for.</param>
    /// <returns>Game object with ShapeVisual component if successful, otherwise null.</returns>
    public static GameObject CreateShapeRenderData( Collide.Shape shape )
    {
      return CreateInstance( shape, true );
    }

    /// <summary>
    /// Find ShapeVisual instance given shape.
    /// </summary>
    /// <param name="shape">Shape.</param>
    /// <returns>ShapeVisual instance if present, otherwise null.</returns>
    public static ShapeVisual Find( Collide.Shape shape )
    {
      return shape?.GetComponentsInChildren<ShapeVisual>().FirstOrDefault( instance => instance.Shape == shape );
    }

    /// <summary>
    /// True if the given shape has a visual instance.
    /// </summary>
    /// <param name="shape">Shape.</param>
    /// <returns>True if the given shape has a visual instance.</returns>
    public static bool HasShapeVisual( Collide.Shape shape )
    {
      return Find( shape ) != null;
    }

    /// <summary>
    /// True if the given shape supports native creation of visual data, otherwise false.
    /// </summary>
    /// <param name="shape">Shape.</param>
    /// <returns>True if the given shape supports native creation of visual data.</returns>
    public static bool SupportsShapeVisual( Collide.Shape shape )
    {
      return shape != null &&
             (
               shape is Collide.Box ||
               shape is Collide.Sphere ||
               shape is Collide.Cylinder ||
               shape is Collide.Capsule ||
               shape is Collide.Plane ||
               shape is Collide.Mesh
             );
    }

    /// <summary>
    /// Creates default material that should be used by default when the visuals are created.
    /// </summary>
    /// <returns>New instance of the default material.</returns>
    public static Material CreateDefaultMaterial()
    {
      var material = new Material( Shader.Find( "Standard" ) );

      material.SetVector( "_Color", Color.Lerp( Color.white, Color.blue, 0.07f ) );
      material.SetFloat( "_Metallic", 0.3f );
      material.SetFloat( "_Glossiness", 0.8f );

      return material;
    }

    /// <summary>
    /// Path to material given Resources.Load.
    /// </summary>
    public static string DefaultMaterialPathResources { get { return @"Materials/ShapeVisualDefaultMaterial"; } }

    /// <summary>
    /// Path to material given "absolute" relative path.
    /// </summary>
    public static string DefaultMaterialPath { get { return @"Assets/AgXUnity/Resources/" + DefaultMaterialPathResources + ".mat"; } }

    /// <summary>
    /// Default material used.
    /// </summary>
    public static Material DefaultMaterial { get { return PrefabLoader.Load<Material>( DefaultMaterialPathResources ); } }

    [SerializeField]
    private Collide.Shape m_shape = null;

    /// <summary>
    /// Shape this object is visualizing.
    /// </summary>
    public Collide.Shape Shape
    {
      get { return m_shape; }
      protected set { m_shape = value; }
    }

    /// <summary>
    /// Assign material to all shared meshes in this object.
    /// </summary>
    /// <param name="material">New material.</param>
    public void SetMaterial( Material material )
    {
      var renderers = GetComponentsInChildren<MeshRenderer>();
      foreach ( var renderer in renderers ) {
        var filter = renderer.GetComponent<MeshFilter>();
        var numMaterials = filter == null || filter.sharedMesh == null ? 1 : filter.sharedMesh.subMeshCount;
        renderer.sharedMaterials = Enumerable.Repeat( material, numMaterials ).ToArray();
      }
    }

    /// <summary>
    /// Callback from Shape when its size has been changed.
    /// </summary>
    public virtual void OnSizeUpdated()
    {
      transform.localScale = GetUnscaledScale();
    }

    /// <summary>
    /// Callback when this component has been added to a game object.
    /// </summary>
    protected virtual void OnConstruct()
    {
    }

    /// <summary>
    /// Execute-in-edit-mode is active - handles default scaling (trying to remove scale).
    /// </summary>
    protected virtual void Update()
    {
      if ( Shape != null && m_lastLossyScale != Shape.transform.lossyScale ) {
        OnSizeUpdated();
        m_lastLossyScale = Shape.transform.lossyScale;
      }
    }

    /// <summary>
    /// Shape scale divided with our parent lossy scale.
    /// </summary>
    /// <returns>shape.GetScale() / shape.parent.lossyScale</returns>
    protected Vector3 GetUnscaledScale()
    {
      if ( Shape == null )
        return Vector3.one;

      var lossyScale = Shape.transform.lossyScale;
      return Vector3.Scale( new Vector3( 1.0f / lossyScale.x, 1.0f / lossyScale.y, 1.0f / lossyScale.z ), Shape.GetScale() );
    }

    /// <summary>
    /// Creates game object and ShapeVisual component given shape and if this is
    /// pure render data or not.
    /// </summary>
    /// <param name="shape">Shape to create ShapeVisual for.</param>
    /// <param name="isRenderData">True if ShapeVisual should be pure render data, i.e., mesh and material is handled explicitly.</param>
    /// <returns>Game object with ShapeVisual component if successful, otherwise null.</returns>
    protected static GameObject CreateInstance( Collide.Shape shape, bool isRenderData )
    {
      if ( shape == null )
        return null;

      GameObject go = null;
      try {
        go = isRenderData || shape is Collide.Mesh ?
               new GameObject( "" ) :
               PrefabLoader.Instantiate<GameObject>( @"Debug/" + shape.GetType().Name + "Renderer" );

        if ( go == null ) {
          Debug.LogWarning( "Unable to find shape visual resource: " + @"Debug/" + shape.GetType().Name + "Renderer", shape );
          return null;
        }

        go.name                = shape.name + "_Visual";
        go.transform.hideFlags = HideFlags.HideInInspector;

        var visual = AddVisualComponent( go, shape, isRenderData );
        if ( visual == null )
          throw new AgXUnity.Exception( "Unsupported shape type: " + shape.GetType().FullName );

        visual.hideFlags = HideFlags.HideInInspector;

        shape.gameObject.AddChild( go );

        go.AddComponent<OnSelectionProxy>().Component = shape;
        foreach ( Transform child in go.transform )
          child.gameObject.GetOrCreateComponent<OnSelectionProxy>().Component = shape;

        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        visual.SetMaterial( DefaultMaterial );
        visual.OnSizeUpdated();
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
        if ( go != null )
          GameObject.DestroyImmediate( go );
      }

      return go;
    }

    /// <summary>
    /// Adds shape visual type given shape type and <paramref name="isRenderData"/>.
    /// </summary>
    /// <param name="go">Game object to add ShapeVisual component to.</param>
    /// <param name="shape">Shape ShapeVisual is referring.</param>
    /// <param name="isRenderData">True if the component should be ShapeVisualRenderData regardless of shape type.</param>
    /// <returns></returns>
    private static ShapeVisual AddVisualComponent( GameObject go, Collide.Shape shape, bool isRenderData )
    {
      ShapeVisual instance = null;
      if ( isRenderData )
        instance = go.AddComponent<ShapeVisualRenderData>();
      else if ( shape is Collide.Box )
        instance = go.AddComponent<ShapeVisualBox>();
      else if ( shape is Collide.Sphere )
        instance = go.AddComponent<ShapeVisualSphere>();
      else if ( shape is Collide.Cylinder )
        instance = go.AddComponent<ShapeVisualCylinder>();
      else if ( shape is Collide.Capsule )
        instance = go.AddComponent<ShapeVisualCapsule>();
      else if ( shape is Collide.Plane )
        instance = go.AddComponent<ShapeVisualPlane>();
      else if ( shape is Collide.Mesh )
        instance = go.AddComponent<ShapeVisualMesh>();

      if ( instance != null ) {
        instance.Shape = shape;
        instance.OnConstruct();
      }

      return instance;
    }

    private Vector3 m_lastLossyScale = Vector3.one;
  }

  /// <summary>
  /// Shape visual for shape type Box.
  /// </summary>
  [DoNotGenerateCustomEditor]
  public class ShapeVisualBox : ShapeVisual
  {
  }

  /// <summary>
  /// Shape visual for shape type Sphere.
  /// </summary>
  [DoNotGenerateCustomEditor]
  public class ShapeVisualSphere : ShapeVisual
  {
  }

  /// <summary>
  /// Shape visual for shape type Cylinder.
  /// </summary>
  [DoNotGenerateCustomEditor]
  public class ShapeVisualCylinder : ShapeVisual
  {
  }

  /// <summary>
  /// Shape visual for shape type Capsule.
  /// </summary>
  [DoNotGenerateCustomEditor]
  public class ShapeVisualCapsule : ShapeVisual
  {
    /// <summary>
    /// Capsule visual is three game objects (2 x half sphere + 1 x cylinder),
    /// the size has to be updated to all of the children.
    /// </summary>
    public override void OnSizeUpdated()
    {
      ShapeDebugRenderData.SetCapsuleSize( gameObject,
                                           ( Shape as Collide.Capsule ).Radius,
                                           ( Shape as Collide.Capsule ).Height );
    }
  }

  /// <summary>
  /// Shape visual for shape type Plane.
  /// </summary>
  [DoNotGenerateCustomEditor]
  public class ShapeVisualPlane : ShapeVisual
  {
  }

  /// <summary>
  /// Shape visual for shape type Mesh.
  /// </summary>
  [DoNotGenerateCustomEditor]
  [RequireComponent( typeof( MeshRenderer ) )]
  [RequireComponent( typeof( MeshFilter ) )]
  public class ShapeVisualMesh : ShapeVisual
  {
    /// <summary>
    /// Callback from Collide.Mesh when a new source has been assigned to
    /// the mesh.
    /// </summary>
    /// <param name="mesh">Mesh shape with updated source object.</param>
    public static void HandleMeshSource( Collide.Mesh mesh )
    {
      var instance = Find( mesh ) as ShapeVisualMesh;
      if ( instance != null )
        instance.HandleMeshSource();
    }

    private MeshRenderer m_renderer = null;
    private MeshFilter m_filter = null;

    /// <summary>
    /// Mesh renderer for this mesh shape visual.
    /// </summary>
    public MeshRenderer MeshRenderer
    {
      get
      {
        if ( m_renderer == null )
          m_renderer = GetComponent<MeshRenderer>();
        return m_renderer;
      }
    }

    /// <summary>
    /// Mesh filter for this mesh shape visual.
    /// </summary>
    public MeshFilter MeshFilter
    {
      get
      {
        if ( m_filter == null )
          m_filter = GetComponent<MeshFilter>();
        return m_filter;
      }
    }

    /// <summary>
    /// Callback when shape size has been changed.
    /// </summary>
    public override void OnSizeUpdated()
    {
      // We don't do anything here since we support any type of scale of the meshes.
    }

    /// <summary>
    /// Callback when constructed, assigning current source object of the mesh shape.
    /// </summary>
    protected override void OnConstruct()
    {
      Collide.Mesh mesh = Shape as Collide.Mesh;
      MeshFilter.sharedMesh = mesh.SourceObject;
    }

    /// <summary>
    /// Callback when our shape has update mesh source. The material used in the last
    /// source will be assigned to the new since we don't know if the number of
    /// sub-meshes are the same.
    /// </summary>
    protected virtual void HandleMeshSource()
    {
      var prevMaterial = MeshRenderer.sharedMaterial ?? DefaultMaterial;
      MeshFilter.sharedMesh = (Shape as Collide.Mesh ).SourceObject;
      SetMaterial( prevMaterial );
    }
  }

  /// <summary>
  /// Shape visual with given mesh data and material.
  /// </summary>
  [DoNotGenerateCustomEditor]
  public class ShapeVisualRenderData : ShapeVisualMesh
  {
    protected override void OnConstruct()
    {
      // Don't do anything. The user handles mesh + materials.
    }

    protected override void HandleMeshSource()
    {
      // We don't want to change sharedMesh given source when we have explicit render data.
    }
  }
}
