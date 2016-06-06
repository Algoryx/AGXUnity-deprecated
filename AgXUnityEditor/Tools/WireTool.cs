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
    public static WireRouteNodeTool FindSelectedRouteNode()
    {
      return FindActive<WireRouteNodeTool>( routeNodeTool => { return routeNodeTool.Selected; } );
    }

    public static void SetWireRouteFoldoutState( Wire wire, bool unfolded )
    {
      GUI.Prefs.SetBool( wire.Route, unfolded );
    }

    public Wire Wire { get; private set; }

    public WireTool( Wire wire )
    {
      Wire = wire;
    }

    public override void OnAdd()
    {
      HideDefaultHandlesEnableWhenRemoved();

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
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      //const char addNodesToolSymbol = '\u260D';

      if ( !EditorApplication.isPlaying ) {
        using ( new GUI.Indent( 12 ) )
          RouteGUI( skin );
      }

      GUI.Separator();

      if ( Wire.BeginWinch != null ) {
        GUILayout.Label( GUI.MakeLabel( "Begin winch", true ), skin.label );
        using ( new GUI.Indent( 12 ) )
          BaseEditor<WireWinch>.Update( Wire.BeginWinch, skin );
        GUI.Separator();
      }
      if ( Wire.EndWinch != null ) {
        GUILayout.Label( GUI.MakeLabel( "End winch", true ), skin.label );
        using ( new GUI.Indent( 12 ) )
          BaseEditor<WireWinch>.Update( Wire.EndWinch, skin );
        GUI.Separator();
      }
    }

    private static GUI.ColorBlock NodeListButtonColor { get { return new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.green, 0.1f ) ); } }

    private void RouteGUI( GUISkin skin )
    {
      if ( !GUI.Prefs.SetBool( Wire.Route, EditorGUILayout.Foldout( GUI.Prefs.GetOrCreateBool( Wire.Route, true ), GUI.MakeLabel( "Route" ) ) ) )
        return;

      GUIStyle invalidNodeStyle               = new GUIStyle( skin.label );
      invalidNodeStyle.normal.background      = GUI.CreateColoredTexture( 4, 4, Color.Lerp( UnityEngine.GUI.color, Color.red, 0.75f ) );
      GUIStyle toolButtonStyle                = new GUIStyle( skin.button );
      toolButtonStyle.fontSize                = 16;
      WireRouteNode insertNodeBefore          = null;
      WireRouteNode insertNodeAfter           = null;
      WireRouteNode eraseNode                 = null;
      WireRoute.ValidatedRoute validatedRoute = Wire.Route.GetValidated();
      using ( new GUI.Indent( 12 ) ) {
        foreach ( WireRoute.ValidatedNode validatedNode in validatedRoute ) {
          WireRouteNode node = validatedNode.Node;
          Undo.RecordObject( node, "RouteNode" );

          GUI.Separator3D();

          WireRouteNodeTool rnTool = GetOrCreateNodeTool( node );

          if ( validatedNode.Valid )
            EditorGUILayout.BeginHorizontal();
          else
            EditorGUILayout.BeginHorizontal( invalidNodeStyle );
          {
            rnTool.Selected = GUILayout.Button( GUI.MakeLabel( rnTool.Selected ? "-" : "+" ), skin.button, new GUILayoutOption[] { GUILayout.Width( 20 ), GUILayout.Height( 14 ) } ) ? !rnTool.Selected : rnTool.Selected;
            GUILayout.Label( GUI.MakeLabel( node.Type.ToString() + " | " + SelectGameObjectDropdownMenuTool.GetGUIContent( node.Frame.Parent ).text, false, validatedNode.ErrorString ), skin.label, GUILayout.ExpandWidth( true ) );
            if ( GUILayoutUtility.GetLastRect().Contains( Event.current.mousePosition ) && Event.current.type == EventType.MouseDown && Event.current.button == 0 ) {
              rnTool.Selected = !rnTool.Selected;
              GUIUtility.ExitGUI();
            }

            using ( NodeListButtonColor ) {
              if ( rnTool.Selected ) {
                if ( GUILayout.Button( GUI.MakeLabel( '\u21B0'.ToString(), false, "Add new node before this" ), toolButtonStyle, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } ) )
                  insertNodeBefore = node;
                if ( GUILayout.Button( GUI.MakeLabel( '\u21B2'.ToString(), false, "Add new node after this" ), toolButtonStyle, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } ) )
                  insertNodeAfter = node;
                if ( GUILayout.Button( GUI.MakeLabel( 'x'.ToString(), false, "Erase this node" ), toolButtonStyle, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } ) )
                  eraseNode = node;
              }
            }
          }
          EditorGUILayout.EndHorizontal();

          if ( rnTool.Selected ) {
            GUI.Separator();

            using ( new GUI.Indent( 12 ) ) {
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

              GUI.Separator();

              GUI.HandleFrame( node.Frame, skin, 4.0f );
            }
          }
        }

        if ( Wire.Route.NumNodes > 0 )
          GUI.Separator3D();
        else
          GUILayout.Label( GUI.MakeLabel( "Empty", true ) );
      }

      EditorGUILayout.BeginHorizontal();
      {
        GUILayout.FlexibleSpace();
        bool addNewNodeToList = false;
        using ( NodeListButtonColor )
          addNewNodeToList = GUILayout.Button( GUI.MakeLabel( '\u21B2'.ToString(), false, "Add new node to list" ), toolButtonStyle, new GUILayoutOption[] { GUILayout.Width( 26 ), GUILayout.Height( 18 ) } );

        if ( addNewNodeToList && Wire.Route.NumNodes > 0 )
          insertNodeAfter = Wire.Route.Last();
        else if ( addNewNodeToList )
          Wire.Route.Add( WireRouteNode.Create( Wire.NodeType.FreeNode ) );
      }
      EditorGUILayout.EndHorizontal();

      if ( eraseNode != null )
        Wire.Route.Remove( eraseNode );
      else if ( insertNodeAfter != null || insertNodeBefore != null ) {
        WireRouteNode refNode = insertNodeAfter != null ? insertNodeAfter : insertNodeBefore;
        WireRouteNode newNode = WireRouteNode.Create( refNode.Type, refNode.Frame.Parent, refNode.Frame.LocalPosition, refNode.Frame.LocalRotation );
        if ( insertNodeAfter != null )
          Wire.Route.InsertAfter( newNode, insertNodeAfter );
        else
          Wire.Route.InsertBefore( newNode, insertNodeBefore );
      }
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
