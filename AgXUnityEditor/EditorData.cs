using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;

namespace AgXUnityEditor
{
  public class EditorData : ScriptComponent
  {
    [Serializable]
    public class SelectedState
    {
      [SerializeField]
      private bool m_selected = false;
      public bool Selected
      {
        get { return m_selected; }
        set
        {
          if ( m_selected == value )
            return;

          m_selected = value;

          if ( Target != null )
            EditorUtility.SetDirty( Target );
        }
      }

      [SerializeField]
      private UnityEngine.Object m_target = null;
      public UnityEngine.Object Target { get { return m_target; } private set { m_target = value; } }

      [SerializeField]
      private string m_identifier = string.Empty;
      public string Identifier { get { return m_identifier; } private set { m_identifier = value; } }

      public uint Key { get { return BuildKey( Target, Identifier ); } }

      public SelectedState( UnityEngine.Object target, string identifier )
      {
        Target = target;
        Identifier = identifier;
      }

      private static int m_localInstanceId = 1234;
      public static uint BuildKey( UnityEngine.Object target, string identifier )
      {
        string targetName = target != null ? target.name : "null";
        int instanceId = target != null ? target.GetInstanceID() : m_localInstanceId++;
        return ( targetName + identifier + instanceId.ToString() ).To32BitFnv1aHash();
      }
    }

    [SerializeField]
    private List<SelectedState> m_selectedStates = new List<SelectedState>();
    private Dictionary<uint, int> m_selectedStatesCache = new Dictionary<uint, int>();

    public SelectedState Selected( UnityEngine.Object target, string identifier, bool defaultSelected = false )
    {
      int index = -1;
      var key = SelectedState.BuildKey( target, identifier );
      if ( m_selectedStatesCache.TryGetValue( key, out index ) )
        return m_selectedStates[ index ];

      SelectedState state = null;
      index = m_selectedStates.FindIndex( s => s.Key == key );
      if ( index < 0 ) {
        index = m_selectedStates.Count;
        state = new SelectedState( target, identifier );
        m_selectedStates.Add( state );
      }
      else
        state = m_selectedStates[ index ];

      m_selectedStatesCache.Add( state.Key, index );

      return state;
    }
  }
}
