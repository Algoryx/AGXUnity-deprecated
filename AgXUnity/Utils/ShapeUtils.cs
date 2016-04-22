using UnityEngine;
using AgXUnity.Collide;

namespace AgXUnity.Utils
{
  public class BoxShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Box box = GetShape<Box>();
      return Vector3.Scale( box.HalfExtents, GetLocalFaceDirection( dir ) );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return true;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Box box = GetShape<Box>();
      box.HalfExtents = box.HalfExtents + Vector3.Scale( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public class CapsuleShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Capsule capsule = GetShape<Capsule>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        return ( capsule.Radius + 0.5f * capsule.Height ) * GetLocalFaceDirection( dir );
      else
        return capsule.Radius * GetLocalFaceDirection( dir );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return ToPrincipal( direction ) != PrincipalAxis.Y;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Capsule capsule = GetShape<Capsule>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        capsule.Height = capsule.Height + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
      else
        capsule.Radius = capsule.Radius + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public class CylinderShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Cylinder capsule = GetShape<Cylinder>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        return 0.5f * capsule.Height * GetLocalFaceDirection( dir );
      else
        return capsule.Radius * GetLocalFaceDirection( dir );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return ToPrincipal( direction ) != PrincipalAxis.Y;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Cylinder cylinder = GetShape<Cylinder>();
      if ( ToPrincipal( dir ) == PrincipalAxis.Y )
        cylinder.Height = cylinder.Height + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
      else
        cylinder.Radius = cylinder.Radius + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public class SphereShapeUtils : ShapeUtils
  {
    public override Vector3 GetLocalFace( Direction dir )
    {
      Sphere sphere = GetShape<Sphere>();
      return sphere.Radius * GetLocalFaceDirection( dir );
    }

    public override bool IsHalfSize( Direction direction )
    {
      return true;
    }

    public override void UpdateSize( Vector3 localChange, Direction dir )
    {
      Sphere sphere = GetShape<Sphere>();
      sphere.Radius = sphere.Radius + Vector3.Dot( GetLocalFaceDirection( dir ), localChange );
    }
  }

  public abstract class ShapeUtils
  {
    public static ShapeUtils Create( Shape shape )
    {
      if ( shape is Box )
        return new BoxShapeUtils() { m_shape = shape };
      else if ( shape is Capsule )
        return new CapsuleShapeUtils() { m_shape = shape };
      else if ( shape is Cylinder )
        return new CylinderShapeUtils() { m_shape = shape };
      else if ( shape is Sphere )
        return new SphereShapeUtils() { m_shape = shape };

      return null;
    }

    public enum PrincipalAxis
    {
      X,
      Y,
      Z
    }

    public enum Direction
    {
      Positive_X,
      Negative_X,
      Positive_Y,
      Negative_Y,
      Positive_Z,
      Negative_Z
    }

    public abstract Vector3 GetLocalFace( Direction direction );

    public abstract bool IsHalfSize( Direction direction );

    public abstract void UpdateSize( Vector3 localChange, Direction dir );

    public Vector3 GetLocalFaceDirection( Direction direction )
    {
      return m_unitFaces[ System.Convert.ToInt32( direction ) ];
    }

    public Vector3 GetWorldFace( Direction direction )
    {
      return m_shape.transform.position + m_shape.transform.TransformDirection( GetLocalFace( direction ) );
    }

    public Vector3 GetWorldFaceDirection( Direction direction )
    {
      return m_shape.transform.TransformDirection( GetLocalFaceDirection( direction ) );
    }

    public PrincipalAxis ToPrincipal( Direction dir )
    {
      return (PrincipalAxis)( System.Convert.ToInt32( dir ) / 2 );
    }

    public float GetSign( Direction dir )
    {
      return 1.0f - 2.0f * ( System.Convert.ToInt32( dir ) % 2 );
    }


    public T GetShape<T>() where T : Shape
    {
      return m_shape as T;
    }

    private static Vector3[] m_unitFaces = new Vector3[]{
                                                          new Vector3(  1,  0,  0 ),
                                                          new Vector3( -1,  0,  0 ),
                                                          new Vector3(  0,  1,  0 ),
                                                          new Vector3(  0, -1,  0 ),
                                                          new Vector3(  0,  0,  1 ),
                                                          new Vector3(  0,  0, -1 )
                                                        };

    private Shape m_shape = null;
  }
}
