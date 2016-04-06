using System;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// For (external) editor generator to generate editor file for class.
  /// </summary>
  [System.AttributeUsage( System.AttributeTargets.Class, AllowMultiple = true )]
  public class GenerateCustomEditor : System.Attribute
  {
    public GenerateCustomEditor() { }
  }
  
  /// <summary>
  /// In general UnityEngine objects are ignored in our custom inspector.
  /// This attribute enables UnityEngine objects to be shown in the editor.
  /// </summary>
  [System.AttributeUsage( System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false )]
  public class ShowInInspector : System.Attribute
  {
    public ShowInInspector() { }
  }

  /// <summary>
  /// Slider in inspector for float with given min and max value.
  /// </summary>
  [System.AttributeUsage( System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false )]
  public class FloatSliderInInspector : System.Attribute
  {
    private float m_min = 0.0f;
    private float m_max = 0.0f;

    public float Min { get { return m_min; } }
    public float Max { get { return m_max; } }

    public FloatSliderInInspector( float min, float max )
    {
      m_min = min;
      m_max = max;
    }
  }

  /// <summary>
  /// Attribute for method to be executed from a button in the editor.
  /// </summary>
  [System.AttributeUsage( System.AttributeTargets.Method, AllowMultiple = false )]
  public class InvokableInInspector : System.Attribute
  {
  }

  /// <summary>
  /// Attribute for public fields or properties to not receive values
  /// less than (or equal to) zero. It's possible to receive exact
  /// zeros though this is not the default behavior.
  /// </summary>
  [System.AttributeUsage( System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false )]
  public class ClampAboveZeroInInspector : System.Attribute
  {
    private bool m_acceptZero = false;
    public ClampAboveZeroInInspector( bool acceptZero = false ) { m_acceptZero = acceptZero; }

    public bool IsValid( object value )
    {
      Type type = value.GetType();
      if ( type == typeof( Vector4 ) )
        return IsValid( (Vector4)value );
      else if ( type == typeof( Vector3 ) )
        return IsValid( (Vector3)value );
      else if ( type == typeof( Vector2 ) )
        return IsValid( (Vector2)value );
      else if ( type == typeof( DefaultAndUserValueFloat ) ) {
        DefaultAndUserValueFloat val = (DefaultAndUserValueFloat)value;
        return val.Value > 0 || ( m_acceptZero && val.Value == 0 );
      }
      else if ( type == typeof( DefaultAndUserValueVector3 ) ) {
        DefaultAndUserValueVector3 val = (DefaultAndUserValueVector3)value;
        return IsValid( (Vector3)val.Value );
      }
      else if ( type == typeof( int ) )
        return (int)value > 0 || ( m_acceptZero && (int)value == 0 );
      else if ( value is IComparable ) {
        int returnCheck = m_acceptZero ? -1 : 0;
        // CompareTo returns 0 if the values are equal.
        return ( value as IComparable ).CompareTo( 0.0f ) > returnCheck;
      }
      else if ( type == typeof( float ) )
        return (float)value > 0 || ( m_acceptZero && (float)value == 0 );
      else if ( type == typeof( double ) )
        return (double)value > 0 || ( m_acceptZero && (double)value == 0 );
      return true;
    }

    public bool IsValid( Vector4 value )
    {
      return ( value[ 0 ] > 0 || ( m_acceptZero && value[ 0 ] == 0 ) ) &&
             ( value[ 1 ] > 0 || ( m_acceptZero && value[ 1 ] == 0 ) ) &&
             ( value[ 2 ] > 0 || ( m_acceptZero && value[ 2 ] == 0 ) ) &&
             ( value[ 3 ] > 0 || ( m_acceptZero && value[ 3 ] == 0 ) );
    }

    public bool IsValid( Vector3 value )
    {
      return ( value[ 0 ] > 0 || ( m_acceptZero && value[ 0 ] == 0 ) ) &&
             ( value[ 1 ] > 0 || ( m_acceptZero && value[ 1 ] == 0 ) ) &&
             ( value[ 2 ] > 0 || ( m_acceptZero && value[ 2 ] == 0 ) );
    }

    public bool IsValid( Vector2 value )
    {
      return ( value[ 0 ] > 0 || ( m_acceptZero && value[ 0 ] == 0 ) ) &&
             ( value[ 1 ] > 0 || ( m_acceptZero && value[ 1 ] == 0 ) );
    }
  }
}
