using System;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Object handling values (real or vec3) which has one default and
  /// one user defined value. This object enables switching between
  /// the two, e.g., mass and inertia in MassProperties.
  /// </summary>
  [System.Serializable]
  public class DefaultAndUserValue<T> where T : struct
  {
    [SerializeField]
    private bool m_defaultToggle = true;
    [SerializeField]
    private T m_defaultValue;
    [SerializeField]
    private T m_userValue;

    public DefaultAndUserValue( T defaultValue, T userValue )
    {
      m_defaultValue = defaultValue;
      m_userValue = userValue;
    }

    public bool UseDefault { get { return m_defaultToggle; } set { m_defaultToggle = value; } }
    public T DefaultValue { get { return m_defaultValue; } set { m_defaultValue = value; } }
    public T UserValue { get { return m_userValue; } set { m_userValue = value; } }

    /// <summary>
    /// Assigning this property when UseDefault == true will NOT change any value.
    /// Use explicit DefaultValue and UserValue for that. If UseDefault == false
    /// the user value will be changed.
    /// </summary>
    public T Value
    {
      get { return UseDefault ? DefaultValue : UserValue; }
      set
      {
        if ( !UseDefault )
          UserValue = value;
      }
    }
  }

  /// <summary>
  /// Object handling values (real or vec3) which has one default and
  /// one user defined value. This object enables switching between
  /// the two, e.g., mass and inertia in MassProperties.
  /// </summary>
  [System.Serializable]
  public class DefaultAndUserValueFloat : DefaultAndUserValue<float>
  {
    public DefaultAndUserValueFloat()
      : base( 1.0f, 1.0f ) { }
  }

  /// <summary>
  /// Object handling values (real or vec3) which has one default and
  /// one user defined value. This object enables switching between
  /// the two, e.g., mass and inertia in MassProperties.
  /// </summary>
  [System.Serializable]
  public class DefaultAndUserValueVector3 : DefaultAndUserValue<Vector3>
  {
    public DefaultAndUserValueVector3()
      : base( new Vector3( 1, 1, 1 ), new Vector3( 1, 1, 1 ) ) { }
  }
}
