using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

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

    public WireTool( Wire wire )
    {
      Wire = wire;
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
      if ( GUI.Prefs.SetBool( Wire.Route, EditorGUILayout.Foldout( GUI.Prefs.GetOrCreateBool( Wire.Route, true ), GUI.MakeLabel( "Route" ) ) ) ) {
        using ( new GUI.Indent( 12 ) ) {
          foreach ( WireRouteNode node in Wire.Route.ToList() ) {
            Undo.RecordObject( node, "RouteNode" );

            WireRouteNodeTool rnTool = GetOrCreateNodeTool( node );

            EditorGUILayout.BeginHorizontal();
            {
              rnTool.Selected = GUILayout.Button( GUI.MakeLabel( rnTool.Selected ? "-" : "+" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 20 ), GUILayout.Height( 14 ) } ) ? !rnTool.Selected : rnTool.Selected;
              GUILayout.Label( GUI.MakeLabel( node.Type.ToString() + " | " + SelectGameObjectDropdownMenuTool.GetGUIContent( node.Frame.Parent ).text ), skin.label );
              if ( GUILayoutUtility.GetLastRect().Contains( Event.current.mousePosition ) && Event.current.type == EventType.MouseDown && Event.current.button == 0 ) {
                rnTool.Selected = !rnTool.Selected;
                GUIUtility.ExitGUI();
              }
            }
            EditorGUILayout.EndHorizontal();

            if ( rnTool.Selected ) {
              using ( new GUI.Indent( 12 ) ) {
                GUI.Separator();

                Wire.NodeType newNodeType = (Wire.NodeType)EditorGUILayout.EnumPopup( GUI.MakeLabel( "Type" ), node.Type, skin.button );
                if ( newNodeType != node.Type ) {
                  // Changing FROM winch and the wire will destroy the WireWinch component.
                  // If we don't exit GUI we'll get an exception when the editor tries to
                  // render GUI of the removed component.
                  bool exitGUI = node.Type == Wire.NodeType.WinchNode;

                  node.Type = newNodeType;

                  if ( exitGUI )
                    GUIUtility.ExitGUI();
                }

                GUI.HandleFrame( node.Frame, skin, 4.0f );
              }
            }
          }
        }
      }

      //EditorGUILayout.BeginHorizontal();
      //GUILayout.Label( Utils.GUI.MakeLabel( "Tools:", true ), skin.label );
      //GUILayout.FlexibleSpace();
      //Utils.GUI.EnumButtonList<ToolMode>(
      //    e => 
      //    {
      //      if ( e == Mode )
      //        Mode = ToolMode.None;
      //      else
      //        Mode = e;
      //    },
      //    e => { return e != ToolMode.None; },
      //    e => { return Utils.GUI.ConditionalCreateSelectedStyle( e == Mode, skin.button ); },
      //    new GUILayoutOption[] { GUILayout.Height( 16.0f ) }
      //  );
      //EditorGUILayout.EndHorizontal();

      //if ( GUI.Prefs.SetBool( Wire.Route, EditorGUILayout.Foldout( GUI.Prefs.GetOrCreateBool( Wire.Route, true ), GUI.MakeLabel( "Route" ) ) ) ) {
      //  Action newElementSeparator = () =>
      //  {
      //    EditorGUILayout.BeginHorizontal();
      //    GUILayout.Space( 12 );
      //    GUI.Separator();
      //    EditorGUILayout.EndHorizontal();
      //  };

      //  // TODO: Highlight nodes that are wrong!

      //  if ( Wire.Route.NumNodes == 0 )
      //    GUILayout.Label( GUI.MakeLabel( "Empty", true ), skin.label );
      //  else {
      //    // TODO: Optimize GUI, avoid GUILayout.
      //    foreach ( WireRouteNode node in Wire.Route.ToList() ) {
      //      newElementSeparator();

      //      Undo.RecordObject( node, "RouteNode" );

      //      EditorGUILayout.BeginHorizontal();
      //      GUILayout.Space( 18 );
      //      Tools.WireRouteNodeTool rnTool = Tools.WireRouteNodeTool.GetRouteNodeTool( node );
      //      if ( rnTool != null && rnTool.Selected )
      //        EditorGUILayout.BeginVertical( GUI.FadeNormalBackground( skin.label, 0.25f ) );
      //      else
      //        EditorGUILayout.BeginVertical();

      //      Wire.NodeType newNodeType = (Wire.NodeType)EditorGUILayout.EnumPopup( GUI.MakeLabel( "Type" ), node.Type, skin.button );
      //      if ( newNodeType != node.Type ) {
      //        // Changing FROM winch and the wire will destroy the WireWinch component.
      //        // If we don't exit GUI we'll get an exception when the editor tries to
      //        // render GUI of the removed component.
      //        bool exitGUI = node.Type == Wire.NodeType.WinchNode;

      //        node.Type = newNodeType;

      //        if ( exitGUI )
      //          GUIUtility.ExitGUI();
      //      }
      //      //GUI.HandleFrameOld( node.Frame,
      //      //                 skin,
      //      //                 true,
      //      //                 4,
      //      //                 frameTool =>
      //      //                 {
      //      //                   if ( frameTool != null && rnTool != null && frameTool.Frame == rnTool.Node.Frame )
      //      //                     rnTool.Selected = false;
      //      //                   else if ( rnTool != null )
      //      //                     rnTool.Selected = true;
      //      //                 } );
      //      GUI.HandleFrame( node.Frame,
      //                       skin,
      //                       frameTool =>
      //                       {
      //                         if ( frameTool != null && rnTool != null && frameTool.Frame == rnTool.Node.Frame ) {
      //                           rnTool.Selected = false;
      //                           return null;
      //                         }
      //                         else if ( rnTool != null ) {
      //                           rnTool.Selected = true;
      //                           return rnTool.GetChild<FrameTool>();
      //                         }
      //                         return null;
      //                       },
      //                       4.0f );
      //      EditorGUILayout.EndVertical();
      //      EditorGUILayout.EndHorizontal();

      //      //EditorGUILayout.BeginHorizontal();
      //      //GUILayout.Space( 18 );
      //      //EditorGUILayout.BeginVertical();
      //      //if ( rnTool != null && rnTool.Selected )
      //      //  GUI.OnToolInspectorGUI( rnTool, Wire, skin );
      //      //EditorGUILayout.EndVertical();
      //      //EditorGUILayout.EndHorizontal();

      //      EditorGUILayout.BeginHorizontal();
      //      GUILayout.FlexibleSpace();
      //      if ( node == Wire.Route.First() && GUILayout.Button( "Insert new before", skin.button, GUILayout.ExpandWidth( false ) ) ) {
      //        WireRouteNode newNode = WireRouteNode.Create( Wire.NodeType.FreeNode );
      //        newNode.Frame.Position = Vector3.zero;
      //        newNode.Frame.Rotation = Quaternion.identity;
      //        Wire.Route.InsertBefore( newNode, node );
      //      }
      //      if ( node != Wire.Route.Last() && GUILayout.Button( "Insert new after", skin.button, GUILayout.ExpandWidth( false ) ) ) {
      //        WireRouteNode newNode = WireRouteNode.Create( Wire.NodeType.FreeNode );
      //        newNode.Frame.Position = node.Frame.Position;
      //        newNode.Frame.Rotation = node.Frame.Rotation;
      //        Wire.Route.InsertAfter( newNode, node );
      //      }
      //      if ( GUILayout.Button( "Remove", skin.button, GUILayout.ExpandWidth( false ) ) ) {
      //        var frameToolOfNodeToRemove = Tools.FrameTool.FindActive( node.Frame );
      //        if ( frameToolOfNodeToRemove != null )
      //          frameToolOfNodeToRemove.Remove();
      //        Wire.Route.Remove( node );
      //      }
      //      EditorGUILayout.EndHorizontal();
      //    }

      //    newElementSeparator();
      //  }

      //  EditorGUILayout.BeginHorizontal();
      //  GUILayout.Space( 12 );
      //  EditorGUILayout.BeginVertical();
      //  GUILayout.Space( 12 );
      //  if ( GUILayout.Button( "Add node", skin.button ) ) {
      //    WireRouteNode newNode = WireRouteNode.Create( Wire.NodeType.FreeNode );
      //    if ( Wire.Route.NumNodes == 0 ) {
      //      newNode.Frame.Position = Vector3.zero;
      //      newNode.Frame.Rotation = Quaternion.identity;
      //    }
      //    else {
      //      newNode.Frame.Position = Wire.Route.Last().Frame.Position;
      //      newNode.Frame.Rotation = Wire.Route.Last().Frame.Rotation;
      //    }
      //    Wire.Route.Add( newNode );
      //  }
      //  GUILayout.Space( 12 );
      //  EditorGUILayout.EndVertical();
      //  EditorGUILayout.EndHorizontal();
      //}

      //GUI.Separator();
    }

    public static void SetWireRouteFoldoutState( Wire wire, bool unfolded )
    {
      GUI.Prefs.SetBool( wire.Route, unfolded );
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
