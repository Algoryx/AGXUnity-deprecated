using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

namespace AgXUnity.Utils
{
  /// <summary>
  /// Extensions to Unity Engine GameObject class.
  /// </summary>
  public static partial class Extensions
  {
    /// <summary>
    /// Add child to parent game object. I.e., the parent transform is
    /// inherited by the child. By default (makeCurrentTransformLocal = true),
    /// the current global position of the child is becoming the local relative
    /// the parent. If makeCurrentTransformLocal = false the child position in
    /// world will be preserved.
    /// </summary>
    /// <param name="parent">Extension.</param>
    /// <param name="child">Child to add.</param>
    /// <param name="makeCurrentTransformLocal">If true, the current global transform of the child
    ///                                         will be moved to be local transform in the parent.
    ///                                         If false, the current global transform of the child
    ///                                         will be preserved.</param>
    /// <returns>The parent (this).</returns>
    /// <example>
    /// GameObject go = new GameObject( "go" );
    /// GameObject child = new GameObject( "child" );
    /// go.AddChild( child.AddChild( new GameObject( "childOfChild" ) ) );
    /// </example>
    public static GameObject AddChild( this GameObject parent, GameObject child, bool makeCurrentTransformLocal = true )
    {
      if ( parent == null || child == null ) {
        Debug.LogWarning( "Parent and/or child is null. Parent: " + parent + ", child: " + child );
        return null;
      }

      Vector3 posBefore = child.transform.position;
      Quaternion rotBefore = child.transform.rotation;

      child.transform.parent = parent.transform;

      if ( makeCurrentTransformLocal ) {
        child.transform.localPosition = posBefore;
        child.transform.localRotation = rotBefore;
      }

      if ( child.GetComponent<Collide.Shape>() != null ) {
        ScriptComponent[] scriptComponents = parent.GetComponents<ScriptComponent>();
        foreach ( ScriptComponent scriptComponent in scriptComponents )
          scriptComponent.OnChildAdded( child );
      }

      return parent;
    }

    /// <summary>
    /// Returns an initialized component - if present.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <returns>Initialized component of type T. Null if not present or not possible to initialize.</returns>
    public static T GetInitializedComponent<T>( this GameObject gameObject ) where T : ScriptComponent
    {
      T component = gameObject.GetComponent<T>();
      if ( component == null )
        return null;
      return component.GetInitialized<T>();
    }

    /// <summary>
    /// Returns a set of initialized components - if any and all components were initialized properly.
    /// </summary>
    /// <remarks>
    /// If one component in the set of components fails to initialize, an exception
    /// is thrown, leaving the rest of the components uninitialized.
    /// </remarks>
    /// <typeparam name="T">Component type.</typeparam>
    /// <returns>
    /// Initialized components of type T. Empty set of none present and throws an exception
    /// if one component fails to initialize.
    /// </returns>
    public static T[] GetInitializedComponents<T>( this GameObject gameObject ) where T : ScriptComponent
    {
      T[] components = gameObject.GetComponents<T>();
      foreach ( T component in components )
        if ( !component.GetInitialized<T>() )
          throw new AgXUnity.Exception( "Unable to initialize component of type: " + typeof( T ).Name );
      return components;
    }

    /// <summary>
    /// Similar to GameObject.GetComponentInChildren but returns an initialized ScriptComponent.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="includeInactive">True to include inactive components, default false.</param>
    /// <returns>Initialized child component of type T. Null if not present or not possible to initialize.</returns>
    public static T GetInitializedComponentInChildren<T>( this GameObject gameObject, bool includeInactive = false ) where T : ScriptComponent
    {
      T component = gameObject.GetComponentInChildren<T>( includeInactive );
      if ( component == null )
        return null;
      return component.GetInitialized<T>();
    }

    /// <summary>
    /// Similar to GameObject.GetComponentInParent but returns an initialized ScriptComponent.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <returns>Initialized parent component of type T. Null if not present or not possible to initialize.</returns>
    public static T GetInitializedComponentInParent<T>( this GameObject gameObject ) where T : ScriptComponent
    {
      T component = gameObject.GetComponentInParent<T>();
      if ( component == null )
        return null;
      return component.GetInitialized<T>();
    }

    /// <summary>
    /// Check if parent has child in its children transform.
    /// </summary>
    /// <param name="parent">Parent.</param>
    /// <param name="child">Child.</param>
    /// <returns>true if child has parent as parent.</returns>
    public static bool HasChild( this GameObject parent, GameObject child )
    {
      if ( child == null )
        return false;

      // What's expected when parent == child? Let Unity decide.
      return child.transform.IsChildOf( parent.transform );
    }

    public static T GetOrCreateComponent<T>( this GameObject gameObject ) where T : Component
    {
      return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
    }
  }

  /// <summary>
  /// Extensions to Unity Engine math classes.
  /// </summary>
  public static partial class Extensions
  {
    public static Vector3 ToVector3( this agx.Vec3 v )
    {
      return new Vector3( (float)v.x, (float)v.y, (float)v.z );
    }

    public static agx.Vec3 ToVec3( this Vector3 v )
    {
      return new agx.Vec3( (double)v.x, (double)v.y, (double)v.z );
    }

    public static agx.Vec3f ToVec3f( this Vector3 v )
    {
      return new agx.Vec3f( v.x, v.y, v.z );
    }

    public static Vector3 ToHandedVector3( this agx.Vec3 v )
    {
      return new Vector3( -(float)v.x, (float)v.y, (float)v.z );
    }

    public static agx.Vec3 ToHandedVec3( this Vector3 v )
    {
      return new agx.Vec3( -(double)v.x, (double)v.y, (double)v.z );
    }

    public static Quaternion Normalize( this Quaternion q )
    {
      Quaternion result;
      float sq = q.x * q.x;
      sq += q.y * q.y;
      sq += q.z * q.z;
      sq += q.w * q.w;
      float inv = 1.0f / Mathf.Sqrt( sq );
      result.x = q.x * inv;
      result.y = q.y * inv;
      result.z = q.z * inv;
      result.w = q.w * inv;
      return result;
    }

    public static Quaternion ToHandedQuaternion( this agx.Quat q )
    {
      return new Quaternion( -(float)q.x, (float)q.y, (float)q.z, -(float)q.w );
    }

    public static agx.Quat ToHandedQuat( this Quaternion q )
    {
      return new agx.Quat( -(double)q.x, (double)q.y, (double)q.z, -(double)q.w );
    }

    // TODO: Clean this up.

    //public static Matrix4x4 AsMatrix4x4( this agx.AffineMatrix4x4 m )
    //{
    //  return Matrix4x4.TRS( m.getTranslate().AsVector3(), m.getRotate().AsQuaternion(), new Vector3( 1, 1, 1 ) );
    //}

    //public static agx.AffineMatrix4x4 ToHandedAffineMatrix4x4( this Matrix4x4 m )
    //{
    //  Vector4 x = m.GetColumn( 0 );
    //  Vector4 y = m.GetColumn( 1 );
    //  Vector4 z = m.GetColumn( 2 );
    //  Vector4 p = m.GetColumn( 3 );
    //  return new agx.AffineMatrix4x4( x.x, y.x, z.x, 0,
    //                                  x.y, y.y, z.y, 0,
    //                                  x.z, y.z, z.z, 0,
    //                                  p.x, p.y, p.z, 1 );
    //  //return new agx.AffineMatrix4x4( m.m00, m.m01, m.m02, m.m03,
    //  //                                m.m10, m.m11, m.m12, m.m13,
    //  //                                m.m20, m.m21, m.m22, m.m23,
    //  //                                m.m30, m.m31, m.m32, m.m33 );
    //}

    /// <summary>
    /// Extract translation from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Translation offset.
    /// </returns>
    public static Vector3 GetTranslate( this Matrix4x4 matrix )
    {
      Vector3 translate;
      translate.x = matrix.m03;
      translate.y = matrix.m13;
      translate.z = matrix.m23;
      return translate;
    }

    /// <summary>
    /// Extract rotation quaternion from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Quaternion representation of rotation transform.
    /// </returns>
    public static Quaternion GetRotation( this Matrix4x4 matrix )
    {
      Vector3 forward;
      forward.x = matrix.m02;
      forward.y = matrix.m12;
      forward.z = matrix.m22;

      Vector3 upwards;
      upwards.x = matrix.m01;
      upwards.y = matrix.m11;
      upwards.z = matrix.m21;

      return Quaternion.LookRotation( forward, upwards );
    }

    /// <summary>
    /// Extract scale from transform matrix.
    /// </summary>
    /// <param name="matrix">Transform matrix. This parameter is passed by reference
    /// to improve performance; no changes will be made to it.</param>
    /// <returns>
    /// Scale vector.
    /// </returns>
    public static Vector3 GetScale( this Matrix4x4 matrix )
    {
      Vector3 scale;
      scale.x = new Vector4( matrix.m00, matrix.m10, matrix.m20, matrix.m30 ).magnitude;
      scale.y = new Vector4( matrix.m01, matrix.m11, matrix.m21, matrix.m31 ).magnitude;
      scale.z = new Vector4( matrix.m02, matrix.m12, matrix.m22, matrix.m32 ).magnitude;
      return scale;
    }

    public static float MaxValue( this Vector3 v )
    {
      float maxVal = float.NegativeInfinity;
      for ( int i = 0; i < 3; ++i )
        if ( v[ i ] > maxVal )
          maxVal = v[ i ];
      return maxVal;
    }

    public static float MinValue( this Vector3 v )
    {
      float minVal = float.PositiveInfinity;
      for ( int i = 0; i < 3; ++i )
        if ( v[ i ] < minVal )
          minVal = v[ i ];
      return minVal;
    }

    public static Vector3 ClampedElementsAbove( this Vector3 v, float minValue )
    {
      Vector3 ret = new Vector3();
      ret.x = Mathf.Max( v.x, minValue );
      ret.y = Mathf.Max( v.y, minValue );
      ret.z = Mathf.Max( v.z, minValue );
      return ret;
    }

    public static string ToHexStringRGBA( this Color color )
    {
      return "#" + ( (int)( 255 * color.r ) ).ToString( "X2" ) + ( (int)( 255 * color.g ) ).ToString( "X2" ) + ( (int)( 255 * color.b ) ).ToString( "X2" ) + ( (int)( 255 * color.a ) ).ToString( "X2" );
    }

    public static string ToHexStringRGB( this Color color )
    {
      return "#" + ( (int)( 255 * color.r ) ).ToString( "X2" ) + ( (int)( 255 * color.g ) ).ToString( "X2" ) + ( (int)( 255 * color.b ) ).ToString( "X2" );
    }
  }

  /// <summary>
  /// Extensions for system string.
  /// </summary>
  public static partial class Extensions
  {
    public static System.UInt32 To32BitFnv1aHash( this string str )
    {
      IEnumerable<byte> bytes = str.ToCharArray().Select( Convert.ToByte );

      System.UInt32 hash = 2166136261u;
      foreach ( byte val in bytes ) {
        hash ^= val;
        hash *= 16777619u;
      }

      return hash;
    }

    public static string SplitCamelCase( this string str )
    {
      return Regex.Replace(
              Regex.Replace(
                  str,
                  @"(\P{Ll})(\P{Ll}\p{Ll})",
                  "$1 $2"
              ),
              @"(\p{Ll})(\P{Ll})",
              "$1 $2"
            );
    }

    public static string FirstCharToUpperCase( this string str )
    {
      if ( String.IsNullOrEmpty( str ) )
        return str;

      return str.First().ToString().ToUpper() + str.Substring( 1 );
    }
  }
}
