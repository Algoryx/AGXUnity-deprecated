using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AgXUnity.Utils
{
  /// <summary>
  /// This object couples a private serialized field with a
  /// property. When an object has been initialized with a
  /// native reference it's possible to call "Synchronize" and
  /// the class will have all matching properties "set" with
  /// the current value.
  /// 
  /// Following this design pattern enables synchronization of
  /// data with the native ditto seemingly transparent.
  /// </summary>
  public static class PropertySynchronizer
  {
    /// <summary>
    /// Searches for field + property match:
    ///   - Field:    m_example ->
    ///   - Property: Example
    /// of same type and invokes set value in the property with
    /// the field value. This is necessary when fields in general
    /// are easy to serialize and the object doesn't know when
    /// the value is written back.
    /// <example>
    /// [SerializeField]
    /// private float m_radius = 1.0f;
    /// public float Radius
    /// {
    ///   get { return m_radius; }
    ///   set
    ///   {
    ///     m_radius = value;
    ///     if ( sphere != null )
    ///       sphere.SetRadius( m_radius );
    ///   }
    /// }
    /// </example>
    /// </summary>
    public static void Synchronize( object obj )
    {
      if ( obj != null )
        ParseAndUpdateProperties( obj, obj.GetType() );
    }

    /// <summary>
    /// Parses all non-public fields and looks for a matching property to
    /// invoke with the current value of the field.
    /// </summary>
    /// <param name="obj">Object to parse and update.</param>
    /// <param name="type">Type of the object.</param>
    private static void ParseAndUpdateProperties( object obj, Type type )
    {
      FieldInfo[] fields = type.GetFields( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
      foreach ( FieldInfo field in fields ) {
        // Note: Only serialized field.
        if ( field.IsNotSerialized )
          continue;

        // Matches ["m_"][first lower case char][rest of field name].
        // Group: 0   1           2                     3
        // Note that Groups[0] is the actual name if it follows the pattern.
        Match nameMatch = Regex.Match( field.Name, @"\b(m_)([a-z])(\w+)" );
        if ( nameMatch.Success ) {
          // Construct property name as: Group index 2 (first lower case char) to upper.
          //                             Group index 3 (rest of the name).
          string propertyName = nameMatch.Groups[ 2 ].ToString().ToUpper() + nameMatch.Groups[ 3 ];
          PropertyInfo property = type.GetProperty( propertyName );
          // If the property exists and has a "set" defined - execute it.
          if ( property != null && property.GetSetMethod() != null && property.GetCustomAttributes( typeof( IgnoreSynchronization ), false ).Length == 0 )
            property.SetValue( obj, field.GetValue( obj ), null );
        }
      }

      // Unsure if this is necessary to recursively update supported objects...
      if ( type.BaseType != null && type.BaseType != obj.GetType() )
        ParseAndUpdateProperties( obj, type.BaseType );
    }
  }
}
