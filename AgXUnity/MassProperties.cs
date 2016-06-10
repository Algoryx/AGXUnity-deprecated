using System;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  /// <summary>
  /// Mass properties of a RigidBody.
  /// </summary>
  [AddComponentMenu( "" )]
  public class MassProperties : ScriptAsset
  {
    [SerializeField]
    private RigidBody m_rb = null;

    [HideInInspector]
    public RigidBody RigidBody
    {
      get { return m_rb; }
      set { m_rb = value; }
    }

    /// <summary>
    /// Mass of the rigid body, holding both calculated and user specified,
    /// paired with property Mass.
    /// </summary>
    [SerializeField]
    private DefaultAndUserValueFloat m_mass = new DefaultAndUserValueFloat();

    /// <summary>
    /// Get or set mass.
    /// </summary>
    [ClampAboveZeroInInspector]
    public DefaultAndUserValueFloat Mass
    {
      get { return m_mass; }
      set
      {
        m_mass = value;
        agx.RigidBody native = GetNative();
        if ( native != null )
          native.getMassProperties().setMass( m_mass.Value );
      }
    }

    /// <summary>
    /// Inertia diagonal of the rigid body, holding both calculated and user specified,
    /// paired with property InertiaDiagonal.
    /// </summary>
    [SerializeField]
    private DefaultAndUserValueVector3 m_inertiaDiagonal = new DefaultAndUserValueVector3();

    /// <summary>
    /// Get or set inertia diagonal.
    /// </summary>
    [ClampAboveZeroInInspector]
    public DefaultAndUserValueVector3 InertiaDiagonal
    {
      get { return m_inertiaDiagonal; }
      set
      {
        m_inertiaDiagonal = value;
        agx.RigidBody native = GetNative();
        if ( native != null )
          native.getMassProperties().setInertiaTensor( m_inertiaDiagonal.Value.ToVec3() );
      }
    }

    [SerializeField]
    private Vector3 m_massCoefficients = new Vector3( 0.0f, 0.0f, 0.0f );
    [ClampAboveZeroInInspector(true)]
    public Vector3 MassCoefficients
    {
      get { return m_massCoefficients; }
      set
      {
        m_massCoefficients = value;
        agx.RigidBody native = GetNative();
        if ( native != null )
          native.getMassProperties().setMassCoefficients( m_massCoefficients.ToVec3() );
      }
    }

    [SerializeField]
    private Vector3 m_inertiaCoefficients = new Vector3( 0.0f, 0.0f, 0.0f );
    [ClampAboveZeroInInspector]
    public Vector3 InertiaCoefficients
    {
      get { return m_inertiaCoefficients; }
      set
      {
        m_inertiaCoefficients = value;
        agx.RigidBody native = GetNative();
        if ( native != null )
          native.getMassProperties().setInertiaTensorCoefficients( m_inertiaCoefficients.ToVec3() );
      }
    }

    public MassProperties()
    {
      // When the user clicks "Update" in the editor we receive
      // a callback to update mass of the body.
      Mass.OnForcedUpdate            += OnForcedMassInertiaUpdate;
      InertiaDiagonal.OnForcedUpdate += OnForcedMassInertiaUpdate;
    }

    protected override void Construct()
    {
    }

    public override void Destroy()
    {
    }

    public void SetDefaultCalculated( agx.RigidBody nativeRb )
    {
      if ( nativeRb == null )
        return;

      Mass.DefaultValue = Convert.ToSingle( nativeRb.getMassProperties().getMass() );
      InertiaDiagonal.DefaultValue = nativeRb.getMassProperties().getPrincipalInertiae().ToVector3();
    }

    protected override bool Initialize()
    {
      if ( RigidBody == null ) {
        Debug.LogError( "RigidBody instance not assigned to MassProperties object." );
        return false;
      }

      RigidBody.GetInitialized<RigidBody>();

      return true;
    }

    private agx.RigidBody GetNative()
    {
      return m_rb != null ? m_rb.Native : null;
    }

    private void OnForcedMassInertiaUpdate()
    {
      if ( RigidBody != null )
        RigidBody.UpdateMassProperties();
    }
  }
}
