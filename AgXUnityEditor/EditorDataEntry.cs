using System;
using UnityEngine;
using UnityEditor;
using AgXUnity.Utils;

namespace AgXUnityEditor
{
  public class EditorDataEntry : ScriptableObject
  {
    public static uint CalculateKey( UnityEngine.Object target, string identifier )
    {
      return ( target.GetInstanceID().ToString() + "_" + identifier ).To32BitFnv1aHash();
    }

    [SerializeField]
    private uint m_key = uint.MaxValue;

    [SerializeField]
    private int m_instanceId = int.MaxValue;

    public uint Key { get { return m_key; } private set { m_key = value; } }

    public int InstanceId { get { return m_instanceId; } private set { m_instanceId = value; } }

    public void Initialize( UnityEngine.Object target, uint key )
    {
      Key = key;
      InstanceId = target.GetInstanceID();

      // This is a trick to avoid this object to be deleted by the GC since
      // this object doesn't "have a root" in the scene.
      hideFlags = HideFlags.HideAndDontSave;
    }
  }

  [Serializable]
  public class EditorDataEntryBool : EditorDataEntry
  {
    [SerializeField]
    private bool m_value = false;

    public bool Value
    {
      get { return m_value; }
      set
      {
        if ( m_value == value )
          return;

        m_value = value;

        EditorUtility.SetDirty( EditorData.Instance );

        UnityEngine.Object obj = EditorUtility.InstanceIDToObject( InstanceId );
        if ( obj != null )
          EditorUtility.SetDirty( obj );
      }
    }
  }
}
