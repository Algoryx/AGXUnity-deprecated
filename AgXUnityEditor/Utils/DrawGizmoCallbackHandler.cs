using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using Tool = AgXUnityEditor.Tools.Tool;

namespace AgXUnityEditor.Utils
{
  public static class DrawGizmoCallbackHandler
  {
    public class ToolScriptComponentData
    {
      public Tool Tool { get; private set; }
      public Predicate<ScriptComponent> Predicate { get; private set; }

      public ToolScriptComponentData( Tool tool, Predicate<ScriptComponent> predicate )
      {
        Tool = tool;
        Predicate = predicate;
      }
    }

    private static List<ToolScriptComponentData> m_scriptComponentTools = new List<ToolScriptComponentData>();

    public static void Register( Tool tool, Predicate<ScriptComponent> predicate )
    {
      if ( m_scriptComponentTools.Exists( data => { return data.Tool == tool; } ) )
        return;

      m_scriptComponentTools.Add( new ToolScriptComponentData( tool, predicate ) );
    }

    public static void Unregister( Tool tool )
    {
      int index = m_scriptComponentTools.FindIndex( data => { return data.Tool == tool; } );
      if ( index < 0 )
        return;
      
      m_scriptComponentTools.RemoveAt( index );
    }

    [DrawGizmo( GizmoType.Active | GizmoType.Selected )]
    public static void OnDrawScriptComponent( ScriptComponent component, GizmoType gizmoType )
    {
      foreach ( var data in m_scriptComponentTools )
        if ( data.Predicate( component ) )
          data.Tool.OnDrawGizmosSelected( component );
    }
  }
}
