using System;
using UnityEngine;

namespace AgXUnity.Collide
{
  /// <summary>
  /// Capsule shape object given radius and height.
  /// </summary>
  [AddComponentMenu( "AgXUnity/Shapes/Capsule" )]
  [GenerateCustomEditor]
  public sealed class Capsule : Shape
  {
    #region Serialized Properties
    /// <summary>
    /// Radius of this capsule paired with property Radius.
    /// </summary>
    [SerializeField]
    private float m_radius = 0.5f;
    /// <summary>
    /// Height of this capsule paired with property Height.
    /// </summary>
    [SerializeField]
    private float m_height = 1.0f;

    /// <summary>
    /// Get or set radius of this capsule.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Radius
    {
      get { return m_radius; }
      set
      {
        m_radius = AgXUnity.Utils.Math.ClampAbove( value, MinimumLength );

        if ( Native != null )
          Native.setRadius( m_radius );

        SizeUpdated();
      }
    }

    /// <summary>
    /// Get or set height of this capsule.
    /// </summary>
    [ClampAboveZeroInInspector]
    public float Height
    {
      get { return m_height; }
      set
      {
        m_height = AgXUnity.Utils.Math.ClampAbove( value, MinimumLength );

        if ( Native != null )
          Native.setHeight( m_height );

        SizeUpdated();
      }
    }
    #endregion

    /// <summary>
    /// Returns the native capsule object if created.
    /// </summary>
    public agxCollide.Capsule Native { get { return m_shape as agxCollide.Capsule; } }

    /// <summary>
    /// Debug rendering scale is unsupported since debug render object
    /// contains three objects - two spheres and one cylinder.
    /// <see cref="SyncDebugRenderingScale"/>
    /// </summary>
    /// <returns>(1, 1, 1) since debug rendering is handled explicitly.</returns>
    public override Vector3 GetScale()
    {
      return new Vector3( 1, 1, 1 );
    }

    /// <summary>
    /// Creates the native capsule object given current radius and height.
    /// </summary>
    /// <returns>Native capsule object.</returns>
    protected override agxCollide.Shape CreateNative()
    {
      return new agxCollide.Capsule( m_radius, m_height );
    }

    /// <summary>
    /// Synchronizes the two spheres and the cylinder given the current
    /// radius and height of this capusle.
    /// 
    /// The debug render objects assumes to contain two spheres named
    /// "UpperSphere" and "LowerSphere".
    /// </summary>
    protected override void SyncDebugRenderingScale()
    {
      ShapeDebugRenderData debugData = GetComponent<ShapeDebugRenderData>();
      if ( debugData != null && debugData.Node ) {
        GameObject prefab = debugData.Node;
        foreach ( Transform child in prefab.transform ) {
          if ( child.name.Contains( "Sphere" ) ) {
            child.gameObject.transform.localScale = new Vector3( 2.0f * m_radius, 2.0f * m_radius, 2.0f * m_radius );
            float sign = 2.0f * Convert.ToSingle( child.name.Contains( "Upper" ) ) - 1.0f;
            child.gameObject.transform.localPosition = new Vector3( 0, 0.5f * sign * m_height, 0 );
          }
          else
            child.gameObject.transform.localScale = new Vector3( 2.0f * m_radius, 0.5f * m_height, 2.0f * m_radius );
        }
      }
    }
  }
}
