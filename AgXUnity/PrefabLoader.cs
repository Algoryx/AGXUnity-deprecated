using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Prefab manager holding instances to already loaded prefabs.
  /// Mainly used in debug and wire rendering.
  /// </summary>
  public class PrefabLoader
  {
    private static PrefabLoader m_instance = null;
    private Dictionary<string, UnityEngine.Object> m_prefabs = new Dictionary<string, UnityEngine.Object>();

    public static PrefabLoader Instance
    {
      get
      {
        if ( m_instance == null )
          m_instance = new PrefabLoader();
        return m_instance;
      }
    }

    /// <summary>
    /// Instantiate an already loaded Prefab or load it for the first time
    /// from the resources directory. Use '.' for directory separator. E.g.,
    /// to load BoxRenderer.prefab from the DebugRenderers directory let;
    /// prefabName = "DebugRenderers.BoxRenderer".
    /// </summary>
    /// <typeparam name="T">Any UnityEngine.Object type (usually GameObject).</typeparam>
    /// <param name="prefabName">Relative path ('.' separated) plus name, excluding file type.</param>
    /// <returns>Instantiated Prefab - null if something goes wrong.</returns>
    public T InstantiateObject<T>( string prefabName ) where T : UnityEngine.Object
    {
      UnityEngine.Object obj;
      bool found = m_prefabs.TryGetValue( prefabName, out obj );
      if ( found && obj == null )
        m_prefabs.Remove( prefabName );
      else if ( found )
        return GameObject.Instantiate( obj, new Vector3(), new Quaternion() ) as T;

      obj = GameObject.Instantiate( Resources.Load( prefabName.Replace( '.', '/' ) ), new Vector3(), new Quaternion() );

      if ( obj != null )
        m_prefabs.Add( prefabName, obj );
      else
        Debug.Log( "Unable to load Prefab: " + prefabName + " at relative path: " + prefabName.Replace( '.', '/' ) );

      return obj as T;
    }

    public static GameObject Instantiate( string prefabName )
    {
      GameObject gameObject = (GameObject)GameObject.Instantiate( Resources.Load<GameObject>( prefabName.Replace( '.', '/' ) ), Vector3.zero, Quaternion.identity );
      return gameObject;
    }
  }
}
