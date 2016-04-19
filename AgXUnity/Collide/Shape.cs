using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity.Collide
{
  /// <summary>
  /// Base class for shapes. This object represents agxCollide.Geometry
  /// and agxCollide.Shape. I.e., this object contains both an instance
  /// to a native agxCollide::Geometry and an agxCollide::Shape.
  /// </summary>
  [DisallowMultipleComponent]
  public abstract class Shape : ScriptComponent
  {
    /// <summary>
    /// Native geometry instance.
    /// </summary>
    protected agxCollide.Geometry m_geometry = null;

    /// <summary>
    /// Native shape instance.
    /// </summary>
    protected agxCollide.Shape m_shape = null;

    /// <summary>
    /// Some value of minimum size of a shape.
    /// </summary>
    [HideInInspector]
    public float MinimumLength { get { return 1.0E-5f; } }

    /// <summary>
    /// Shape material instance paired with property Material.
    /// </summary>
    [SerializeField]
    private AgXUnity.ShapeMaterial m_material = null;
    /// <summary>
    /// Get or set shape material instance.
    /// </summary>
    public AgXUnity.ShapeMaterial Material
    {
      get { return m_material; }
      set
      {
        m_material = value;
        if ( m_material != null && m_geometry != null )
          m_geometry.setMaterial( m_material.GetInitialized<ShapeMaterial>().Native );
      }
    }

    /// <summary>
    /// Native geometry object, if initialized.
    /// </summary>
    public agxCollide.Geometry NativeGeometry { get { return m_geometry; } }

    /// <summary>
    /// Native shape objects, if initialized.
    /// </summary>
    public agxCollide.Shape NativeShape { get { return m_shape; } }

    /// <summary>
    /// Abstract scale. Mainly used in debug rendering which uses unit size
    /// and scale. E.g., a sphere with radius 0.3 m should return (0.6, 0.6, 0.6).
    /// </summary>
    /// <returns>Scale of the shape.</returns>
    public abstract Vector3 GetScale();

    /// <summary>
    /// Creates an instance of the native shape and returns it. This method
    /// shouldn't store an instance to this object, simply create a new instance.
    /// E.g., sphere "return new agxCollide.Sphere( Radius );".
    /// </summary>
    /// <returns>An instance to the native shape.</returns>
    protected abstract agxCollide.Shape CreateNative();

    /// <summary>
    /// Used to calculate things related to our shapes, e.g., CM-offset, mass and inertia.
    /// </summary>
    /// <returns>Native shape to be considered temporary (i.e., probably not defined to keep reference to this shape).</returns>
    public virtual agxCollide.Shape CreateTemporaryNative()
    {
      return CreateNative();
    }

    /// <summary>
    /// The relative transform between the shape and the geometry. E.g., height-field may
    /// want to use this transform to map to unity terrain.
    /// </summary>
    /// <returns>Relative transform geometry -> shape.</returns>
    public virtual agx.AffineMatrix4x4 GetNativeGeometryOffset()
    {
      return new agx.AffineMatrix4x4();
    }

    /// <summary>
    /// The relative transform used between a rigid body and this shape.
    /// </summary>
    /// <returns>Relative transform between rigid body (parent) and this shape, in native format.</returns>
    public agx.AffineMatrix4x4 GetNativeRigidBodyOffset( RigidBody rb )
    {
      // If we're on the same level as the rigid body we have by
      // definition no offset to the body.
      if ( rb == null || rb.gameObject == gameObject )
        return new agx.AffineMatrix4x4();

      // Using the world position of the shape - which includes scaling etc.
      agx.AffineMatrix4x4 shapeInWorld = new agx.AffineMatrix4x4( transform.rotation.ToHandedQuat(), transform.position.ToHandedVec3() );
      agx.AffineMatrix4x4 rbInWorld    = new agx.AffineMatrix4x4( rb.transform.rotation.ToHandedQuat(), rb.transform.position.ToHandedVec3() );
      return shapeInWorld.Multiply( rbInWorld.inverse() );
    }

    /// <summary>
    /// Add shape to a rigid body instance.
    /// NOTE: This method is used by the RigidBody object.
    /// </summary>
    /// <param name="rb"></param>
    public void SetRigidBody( RigidBody rb )
    {
      if ( m_geometry == null || m_geometry.getShapes().Count == 0 || m_geometry.getRigidBody() != null )
        return;

      // Search in our game object for rigid body and remove this?
      if ( !rb.gameObject.HasChild( gameObject ) )
        throw new Exception( "RigidBody not parent to Shape." );

      rb.Native.add( m_geometry, GetNativeRigidBodyOffset( rb ) );
    }

    /// <summary>
    /// Call this method when the size of the shape has been changed.
    /// This method will call any rigid body object that this shape
    /// is part, for it to update the mass etc.
    /// </summary>
    public void SizeUpdated()
    {
      // TODO: This method is called a lot during initialize. E.g., profile with 100 shapes.
      SyncDebugRenderingScale();

      SendMessageToAncestor<RigidBody>( RigidBody.UpdateMassMethodName, new object[] { this } );
    }

    /// <summary>
    /// Creates native shape and geometry. Assigns material to the
    /// native geometry if material is present.
    /// </summary>
    /// <returns></returns>
    protected override bool Initialize()
    {
      m_shape = CreateNative();

      if ( m_shape == null )
        return false;

      m_geometry = new agxCollide.Geometry( m_shape, GetNativeGeometryOffset() );
      m_geometry.setName( name );

      if ( Material != null )
        m_geometry.setMaterial( m_material.GetInitialized<ShapeMaterial>().Native );

      SyncNativeTransform();

      GetSimulation().add( m_geometry );

      // Temp hack to get "pulley property" of a RigidBody which name
      // contains the name "sheave".
      //RigidBody rbTmp = Find.FirstParentWithComponent<RigidBody>( gameObject );
      //if ( rbTmp != null && rbTmp.gameObject.name.ToLower().Contains( "sheave" ) ) {
      //  Debug.Log( "Adding pulley property to: " + gameObject.name + " from rb.name = " + rbTmp.gameObject.name );
      //  m_geometry.getPropertyContainer().addPropertyBool( "Pulley", true );
      //}

      return base.Initialize();
    }

    /// <summary>
    /// Removes the native geometry from the simulation.
    /// </summary>
    protected override void OnDestroy()
    {
      if ( m_geometry != null && GetSimulation() != null )
        GetSimulation().remove( m_geometry );

      if ( m_shape != null )
        m_shape.Dispose();
      m_shape = null;

      if ( m_geometry != null )
        m_geometry.Dispose();
      m_geometry = null;

      base.OnDestroy();
    }

    /// <summary>
    /// Late update call from Unity where stepForward can
    /// be assumed to be done.
    /// </summary>
    protected void LateUpdate()
    {
      SyncUnityTransform();

      // If we have a body the debug rendering synchronization is made from that body.
      if ( m_geometry != null && m_geometry.getRigidBody() == null )
        Rendering.DebugRenderManager.OnLateUpdate( this );
    }

    /// <summary>
    /// Synchronizes debug render scale when e.g., the size has been changed.
    /// </summary>
    protected virtual void SyncDebugRenderingScale()
    {
      Rendering.ShapeDebugRenderData debugData = GetComponent<Rendering.ShapeDebugRenderData>();
      if ( debugData != null )
        debugData.Synchronize();
    }

    /// <summary>
    /// "Back" synchronize of transforms given the simulation has
    /// updated the transforms.
    /// </summary>
    private void SyncUnityTransform()
    {
      if ( transform.parent == null && m_geometry != null ) {
        agx.AffineMatrix4x4 t = m_geometry.getTransform();
        transform.position = t.getTranslate().ToHandedVector3();
        transform.rotation = t.getRotate().ToHandedQuaternion();
      }
    }

    /// <summary>
    /// "Forward" synchronize the transform when e.g., the game object
    /// has been moved in the editor.
    /// </summary>
    private void SyncNativeTransform()
    {
      // Automatic synchronization if we have a parent.
      if ( m_geometry != null && m_geometry.getRigidBody() == null )
        m_geometry.setLocalTransform( new agx.AffineMatrix4x4( transform.rotation.ToHandedQuat(), transform.position.ToHandedVec3() ) );
    }
  }
}
