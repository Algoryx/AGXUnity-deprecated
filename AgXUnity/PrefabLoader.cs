using System.Collections.Generic;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Prefab manager not doing much but possible to extend if load times gets significant.
  /// </summary>
  public class PrefabLoader
  {
    public static GameObject Instantiate( string prefabName )
    {
      GameObject gameObject = (GameObject)GameObject.Instantiate( Resources.Load<GameObject>( prefabName.Replace( '.', '/' ) ), Vector3.zero, Quaternion.identity );
      return gameObject;
    }
  }
}
