using System;
using UnityEngine;
using UnityEditor;
using AgXUnity.Utils;

namespace AgXUnityEditor
{
  [Serializable]
  public class EditorDataEntry
  {
    public static uint CalculateKey( UnityEngine.Object target, string identifier )
    {
      return ( target.GetInstanceID().ToString() + "_" + identifier ).To32BitFnv1aHash();
    }

    [SerializeField]
    private uint m_key = uint.MaxValue;
    [SerializeField]
    private int m_instanceId = int.MaxValue;

    [SerializeField]
    private bool m_bool = false;
    [SerializeField]
    private int m_int = 0;
    [SerializeField]
    private float m_float = 0f;
    [SerializeField]
    private string m_string = string.Empty;

    public bool Bool
    {
      get { return m_bool; }
      set
      {
        if ( m_bool == value )
          return;

        m_bool = value;

        OnValueChanged();
      }
    }

    public int Int
    {
      get { return m_int; }
      set
      {
        if ( m_int == value )
          return;

        m_int = value;

        OnValueChanged();
      }
    }

    public float Float
    {
      get { return m_float; }
      set
      {
        if ( m_float == value )
          return;

        m_float = value;

        OnValueChanged();
      }
    }

    public string String
    {
      get { return m_string; }
      set
      {
        if ( m_string == value )
          return;

        m_string = value;

        OnValueChanged();
      }
    }

    public uint Key { get { return m_key; } private set { m_key = value; } }

    public int InstanceId { get { return m_instanceId; } private set { m_instanceId = value; } }

    public EditorDataEntry( UnityEngine.Object target, uint key )
    {
      Key = key;
      InstanceId = target.GetInstanceID();
    }

    private void OnValueChanged()
    {
      // Saves our data file.
      EditorUtility.SetDirty( EditorData.Instance );

      // This is to trigger an update of the target GUI when the value has been changed.
      // E.g., clicking expand/collapse on a foldout we'd like the GUI to instantly respond.
      UnityEngine.Object obj = EditorUtility.InstanceIDToObject( InstanceId );
      if ( obj != null )
        EditorUtility.SetDirty( obj );
    }
  }
}
