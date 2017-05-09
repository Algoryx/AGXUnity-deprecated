using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof( Wire ) )]
  public class WireTool : RouteTool<Wire, WireRouteNode>
  {
    public Wire Wire { get; private set; }

    public WireTool( Wire wire )
      : base( wire, wire.Route )
    {
      Wire = wire;
      NodeVisualRadius += () => { return Wire.Radius; };
    }

    public override void OnPostTargetMembersGUI( GUISkin skin )
    {
      if ( Wire.BeginWinch != null ) {
        GUI.Separator();
        GUILayout.Label( GUI.MakeLabel( "Begin winch", true ), skin.label );
        using ( new GUI.Indent( 12 ) )
          BaseEditor<Wire>.Update( Wire.BeginWinch, Wire, skin );
        GUI.Separator();
      }
      if ( Wire.EndWinch != null ) {
        if ( Wire.BeginWinch == null )
          GUI.Separator();

        GUILayout.Label( GUI.MakeLabel( "End winch", true ), skin.label );
        using ( new GUI.Indent( 12 ) )
          BaseEditor<Wire>.Update( Wire.EndWinch, Wire, skin );
        GUI.Separator();
      }
    }

    protected override void OnPreFrameGUI( WireRouteNode node, GUISkin skin )
    {
      using ( new GUI.Indent( 12 ) ) {
        node.Type = (Wire.NodeType)EditorGUILayout.EnumPopup( GUI.MakeLabel( "Type" ), node.Type, skin.button );

        GUI.Separator();
      }
    }

    protected override void OnNodeCreate( WireRouteNode newNode, WireRouteNode refNode, bool addPressed )
    {
      if ( !addPressed && refNode != null )
        newNode.Type = refNode.Type;
      else
        newNode.Type = Wire.NodeType.FreeNode;
    }
  }
}
