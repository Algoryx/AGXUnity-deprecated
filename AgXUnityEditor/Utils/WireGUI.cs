using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Utils
{
  public partial class GUI
  {
    public static void TargetEditorEnable( Wire wire, GUISkin skin )
    {
      UnityEditor.Tools.hidden = true;

      if ( !EditorApplication.isPlaying )
        Tools.Tool.ActivateTool( new Tools.WireTool( wire, skin ) );
    }

    public static void TargetEditorDisable( Wire wire )
    {
      UnityEditor.Tools.hidden = false;

      Tools.WireTool wireTool = Tools.Tool.GetActiveTool<Tools.WireTool>();
      if ( wireTool != null && wireTool.Wire == wire )
        Tools.Tool.RemoveActiveTool();
    }

    public static void PreTargetMembers( Wire wire, GUISkin skin )
    {
      Tools.WireTool wireTool = Tools.Tool.GetActiveTool<Tools.WireTool>();

      // TODO: Handle GUI while playing.
      if ( wireTool == null )
        return;

      OnToolInspectorGUI( wireTool, wire, skin );

      if ( Prefs.SetBool( wire.Route, EditorGUILayout.Foldout( Prefs.GetOrCreateBool( wire.Route, true ), MakeLabel( "Route" ) ) ) ) {
        Action newElementSeparator = () =>
        {
          EditorGUILayout.BeginHorizontal();
          GUILayout.Space( 12 );
          Separator();
          EditorGUILayout.EndHorizontal();
        };

        // TODO: Highlight nodes that are wrong!

        if ( wire.Route.NumNodes == 0 )
          GUILayout.Label( MakeLabel( "Empty", true ), skin.label );
        else {
          foreach ( WireRouteNode node in wire.Route.ToList() ) {
            newElementSeparator();

            Undo.RecordObject( node, "RouteNode" );

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space( 18 );
            Tools.WireRouteNodeTool rnTool = Tools.WireRouteNodeTool.GetRouteNodeTool( node );
            if ( rnTool != null && rnTool.Selected == true )
              EditorGUILayout.BeginVertical( FadeNormalBackground( skin.label, 0.25f ) );
            else
              EditorGUILayout.BeginVertical();

            Wire.NodeType newNodeType = (Wire.NodeType)EditorGUILayout.EnumPopup( MakeLabel( "Type" ), node.Type, skin.button );
            if ( newNodeType != node.Type ) {
              // Changing FROM winch and the wire will destroy the WireWinch component.
              // If we don't exit GUI we'll get an exception when the editor tries to
              // render GUI of the removed component.
              bool exitGUI = node.Type == Wire.NodeType.WinchNode;

              node.Type = newNodeType;

              if ( exitGUI )
                GUIUtility.ExitGUI();
            }
            HandleFrame( node.Frame,
                         skin,
                         true,
                         4,
                         frameTool =>
                         {
                           if ( frameTool != null && rnTool != null && frameTool.Frame == rnTool.Node.Frame )
                             rnTool.Selected = false;
                           else if ( rnTool != null )
                             rnTool.Selected = true;
                         } );
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space( 18 );
            EditorGUILayout.BeginVertical();
            if ( rnTool != null && rnTool.Selected )
              OnToolInspectorGUI( rnTool, wire, skin );
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if ( node == wire.Route.First() && GUILayout.Button( "Insert new before", skin.button, GUILayout.ExpandWidth( false ) ) ) {
              WireRouteNode newNode = WireRouteNode.Create( Wire.NodeType.FreeNode );
              newNode.Frame.Position = Vector3.zero;
              newNode.Frame.Rotation = Quaternion.identity;
              wire.Route.InsertBefore( newNode, node );
            }
            if ( node != wire.Route.Last() && GUILayout.Button( "Insert new after", skin.button, GUILayout.ExpandWidth( false ) ) ) {
              WireRouteNode newNode = WireRouteNode.Create( Wire.NodeType.FreeNode );
              newNode.Frame.Position = node.Frame.Position;
              newNode.Frame.Rotation = node.Frame.Rotation;
              wire.Route.InsertAfter( newNode, node );
            }
            if ( GUILayout.Button( "Remove", skin.button, GUILayout.ExpandWidth( false ) ) ) {
              var frameToolOfNodeToRemove = Tools.FrameTool.FindActive( node.Frame );
              if ( frameToolOfNodeToRemove != null )
                frameToolOfNodeToRemove.Remove();
              wire.Route.Remove( node );
            }
            EditorGUILayout.EndHorizontal();
          }

          newElementSeparator();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space( 12 );
        EditorGUILayout.BeginVertical();
        GUILayout.Space( 12 );
        if ( GUILayout.Button( "Add node", skin.button ) ) {
          WireRouteNode newNode = WireRouteNode.Create( Wire.NodeType.FreeNode );
          if ( wire.Route.NumNodes == 0 ) {
            newNode.Frame.Position = Vector3.zero;
            newNode.Frame.Rotation = Quaternion.identity;
          }
          else {
            newNode.Frame.Position = wire.Route.Last().Frame.Position;
            newNode.Frame.Rotation = wire.Route.Last().Frame.Rotation;
          }
          wire.Route.Add( newNode );
        }
        GUILayout.Space( 12 );
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
      }

      Separator();
    }

    public static void SetWireRouteFoldoutState( Wire wire, bool unfolded )
    {
      Prefs.SetBool( wire.Route, unfolded );
    }
  }
}
