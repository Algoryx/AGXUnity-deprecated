using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class WireRouteTool : Tool
  {
    public Wire Wire { get; private set; }

    public WireRouteTool( Wire wire )
    {
      Wire = wire;
    }

    //public override void OnSceneViewGUI( SceneView sceneView )
    //{
    //  if ( GetChild<FindPointTool>() == null ) {
    //    FindPointTool pointTool = new FindPointTool()
    //    {
    //      RemoveSelfWhenDone = true
    //    };
    //    pointTool.OnNewPointData += OnNewPoint;

    //    AddChild( pointTool );
    //  }

    //  OnSceneViewGUIChildren( sceneView );
    //}

    //private void OnNewPoint( FindPointTool.PointData pointData )
    //{
    //  AddNode( pointData.Parent, pointData.WorldPosition, pointData.WorldRotation );
    //}

    //private void AddNode( GameObject parent, Vector3 position, Quaternion rotation )
    //{
    //  WireRouteNode node = WireRouteNode.Create( Wire.NodeType.FreeNode, parent );
    //  node.Frame.Position = position;
    //  node.Frame.Rotation = rotation;

    //  Wire.Route.Add( node );
    //}
  }
}