using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Utils
{
  public partial class GUI
  {
    public static void PreTargetMembers( Wire wire, GUISkin skin )
    {
      Tools.WireRouteTool activeWireTool = Manager.GetActiveTool<Tools.WireRouteTool>();

      EditorGUILayout.BeginHorizontal();
      GUILayout.Label( MakeLabel( "Tools:" ), skin.label );
      EnumButtonList<Tools.WireRouteTool.ToolMode>(
          e =>
          {
            if ( activeWireTool == null || activeWireTool.Wire != wire || activeWireTool.Mode != e )
              activeWireTool = Manager.ActivateTool<Tools.WireRouteTool>( new Tools.WireRouteTool( wire, e ) );
            else if ( activeWireTool != null && activeWireTool.Mode == e )
              activeWireTool = Manager.ActivateTool<Tools.WireRouteTool>( null );
          },
          null,
          e =>
          {
            return ConditionalCreateSelectedStyle( activeWireTool != null &&
                                                   activeWireTool.Wire == wire &&
                                                   activeWireTool.Mode == e,
                                                   skin.button );
          },
          new GUILayoutOption[] { GUILayout.Height( 16.0f ) }
        );
      EditorGUILayout.EndHorizontal();

      OnToolInspectorGUI( activeWireTool, wire, skin );

      if ( Prefs.SetBool( wire.Route, EditorGUILayout.Foldout( Prefs.GetOrCreateBool( wire.Route, true ), MakeLabel( "Route" ) ) ) ) {
        Action newElementSeparator = () =>
        {
          EditorGUILayout.BeginHorizontal();
          GUILayout.Space( 12 );
          Separator();
          EditorGUILayout.EndHorizontal();
        };

        if ( wire.Route.Nodes.Count == 0 )
          GUILayout.Label( MakeLabel( "Empty", true ), skin.label );
        else {
          Wire.RouteNode nodeToRemove = null;
          foreach ( Wire.RouteNode node in wire.Route.Nodes ) {
            newElementSeparator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space( 18 );
            EditorGUILayout.BeginVertical();
            Wire.NodeType newNodeType = (Wire.NodeType)EditorGUILayout.EnumPopup( MakeLabel( "Type" ), node.Type, skin.button );
            if ( newNodeType != node.Type ) {
              // Changing FROM winch and the wire will destroy the WireWinch component.
              // If we don't exit GUI we'll get an exception when the editor tries to
              // render GUI of the removed component.
              bool exitGUI = node.Type == Wire.NodeType.WinchNode;

              node.Type = newNodeType;

              if ( exitGUI )
                EditorGUIUtility.ExitGUI();
            }
            HandleFrame( node.Frame, skin, true, 4 );
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if ( GUILayout.Button( "Remove", skin.button, GUILayout.ExpandWidth( false ) ) )
              nodeToRemove = node;
            EditorGUILayout.EndHorizontal();
          }

          if ( nodeToRemove != null ) {
            var frameToolOfNodeToRemove = Tools.FrameTool.FindActive( nodeToRemove.Frame );
            if ( frameToolOfNodeToRemove != null )
              frameToolOfNodeToRemove.Remove();
            wire.Route.Nodes.Remove( nodeToRemove );
          }

          newElementSeparator();
        }
      }

      Separator();
    }

    public static void TargetEditorDisable( Wire wire )
    {
      Tools.FrameTool frameTool = Manager.GetActiveTool<Tools.FrameTool>();
      if ( frameTool != null ) {
        foreach ( Wire.RouteNode node in wire.Route.Nodes ) {
          if ( node.Frame == frameTool.Frame ) {
            frameTool = Manager.ActivateTool<Tools.FrameTool>( null );
            break;
          }
        }
      }
    }

    public static void SetWireRouteFoldoutState( Wire wire, bool unfolded )
    {
      Prefs.SetBool( wire.Route, unfolded );
    }
  }
}
