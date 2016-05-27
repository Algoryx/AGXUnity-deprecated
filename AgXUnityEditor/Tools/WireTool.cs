using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class WireTool : Tool
  {
    public enum ToolMode
    {
      None,
      AddNodes
    }

    private ToolMode m_mode = ToolMode.None;
    public ToolMode Mode
    {
      get { return m_mode; }
      set
      {
        if ( m_mode == ToolMode.AddNodes )
          RemoveChild( GetChild<WireRouteTool>() );

        m_mode = value;

        if ( m_mode == ToolMode.AddNodes ) {
          AddChild( new WireRouteTool( Wire ) );
          WireRouteNode lastNode = Wire.Route.LastOrDefault();
          if ( lastNode != null )
            GetOrCreateNodeTool( lastNode ).Selected = true;
        }
      }
    }

    public Wire Wire { get; private set; }

    public GUISkin Skin { get; private set; }

    public WireTool( Wire wire, GUISkin skin )
    {
      Wire = wire;
      Skin = skin;
    }

    public override void OnAdd()
    {
      Wire.Route.OnNodeAdded   += OnNodeAddedToRoute;
      Wire.Route.OnNodeRemoved += OnNodeRemovedFromRoute;
    }

    public override void OnRemove()
    {
      Wire.Route.OnNodeAdded   -= OnNodeAddedToRoute;
      Wire.Route.OnNodeRemoved -= OnNodeRemovedFromRoute;

      foreach ( WireRouteNode node in Wire.Route )
        node.SetEditorData( null );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      // Create new node tools if not present.
      foreach ( WireRouteNode node in Wire.Route ) {
        if ( !node.HasEditorData ) {
          WireRouteNodeTool nodeTool = new WireRouteNodeTool( node );
          AddChild( nodeTool );
        }
      }

      OnSceneViewGUIChildren( sceneView );
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      EditorGUILayout.BeginHorizontal();
      GUILayout.Label( Utils.GUI.MakeLabel( "Tools:", true ), skin.label );
      GUILayout.FlexibleSpace();
      Utils.GUI.EnumButtonList<ToolMode>(
          e => 
          {
            if ( e == Mode )
              Mode = ToolMode.None;
            else
              Mode = e;
          },
          e => { return e != ToolMode.None; },
          e => { return Utils.GUI.ConditionalCreateSelectedStyle( e == Mode, skin.button ); },
          new GUILayoutOption[] { GUILayout.Height( 16.0f ) }
        );
      EditorGUILayout.EndHorizontal();
    }

    private WireRouteNodeTool GetOrCreateNodeTool( WireRouteNode node )
    {
      if ( node == null )
        throw new System.ArgumentNullException( "Node is null" );

      if ( node.HasEditorData )
        return node.GetEditorData<WireRouteNodeTool>();

      WireRouteNodeTool nodeTool = new WireRouteNodeTool( node );
      AddChild( nodeTool );

      return nodeTool;
    }

    private void OnNodeAddedToRoute( WireRouteNode node )
    {
      GetOrCreateNodeTool( node ).Selected = true;
    }

    private void OnNodeRemovedFromRoute( WireRouteNode node, int prevIndex )
    {
      int indexToSelect = prevIndex > 0 ?
                            prevIndex - 1 :
                          Wire.Route.NumNodes > 0 ?
                            0 :
                           -1;

      if ( indexToSelect < 0 )
        return;

      GetOrCreateNodeTool( Wire.Route.ElementAt( indexToSelect ) ).Selected = true;
    }
  }
}
