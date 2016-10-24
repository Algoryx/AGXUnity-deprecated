using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor
{
  public class EditorData : ScriptableObject
  {
    public static EditorData Instance { get { return GetOrCreateInstance(); } }

    public T GetData<T>( UnityEngine.Object target, string identifier, Action<T> onCreate = null ) where T : EditorDataEntry
    {
      if ( target == null )
        throw new AgXUnity.Exception( "Invalid (null) EditorData target. Target has to be given and has to be valid." );

      var key = EditorDataEntry.CalculateKey( target, identifier );
      int dataIndex = -1;
      if ( !m_dataCache.TryGetValue( key, out dataIndex ) ) {
        dataIndex = m_data.FindIndex( data => data.Key == key );
        if ( dataIndex < 0 ) {
          T instance = CreateInstance<T>();
          instance.Initialize( target, key );
          dataIndex = m_data.Count;

          m_data.Add( instance );

          onCreate?.Invoke( instance );
        }

        m_dataCache.Add( key, dataIndex );
      }

      return m_data[ dataIndex ] as T;
    }

    [SerializeField]
    private List<EditorDataEntry> m_data = new List<EditorDataEntry>();
    private Dictionary<uint, int> m_dataCache = new Dictionary<uint, int>();

    private static EditorData m_instance = null;
    private static EditorData GetOrCreateInstance()
    {
      if ( m_instance != null )
        return m_instance;

      return ( m_instance = EditorSettings.GetOrCreateEditorDataFolderFileInstance<EditorData>( "/Data.asset" ) );
    }
  }

  [CustomEditor( typeof( EditorData ) )]
  public class EditorDataEditor : BaseEditor<EditorData>
  {
    protected override bool OverrideOnInspectorGUI( EditorData target, GUISkin skin )
    {
      return true;
    }
  }
}
