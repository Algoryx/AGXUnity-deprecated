using System;
using UnityEngine;

namespace AgXUnity.Utils
{
  public static class Math
  {
    public static Vector3 Clamp( Vector3 v, float minValue )
    {
      return new Vector3( Mathf.Max( v.x, minValue ), Mathf.Max( v.y, minValue ), Mathf.Max( v.z, minValue ) );
    }

    public static T Clamp<T>( T value, T min, T max ) where T : IComparable<T>
    {
      return value.CompareTo( min ) < 0 ? min : value.CompareTo( max ) > 0 ? max : value;
    }

    public static bool EqualsZero( float value, float epsilon = 0.000001f )
    {
      return System.Math.Abs( value ) < epsilon;
    }

    public static float ClampAbove( float value, float minimum )
    {
      return Mathf.Max( value, minimum );
    }
  }
}
