using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class WireRouteTool : Tool
  {
    public enum ToolMode
    {
      AddNodes
    }

    public ToolMode Mode { get; private set; }

    public Wire Wire { get; private set; }

    private enum AddNodesState
    {
      RaycastTarget,
      Done
    }

    private Utils.VisualPrimitiveSphere RaycastHitVisual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveSphere>( "raycastHitVisual" ); } }

    private GameObject m_currentTarget   = null;
    private Wire.RouteNode m_currentNode = null;
    private AddNodesState m_addNodeState = AddNodesState.RaycastTarget;

    public WireRouteTool( Wire wire, ToolMode mode )
    {
      Mode = mode;
      Wire = wire;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Wire == null || Manager.KeyEscapeDown ) {
        PerformRemoveFromParent();
        return;
      }

      OnSceneViewGUIChildren( sceneView );

      RaycastHitVisual.Visible = false;

      if ( Mode == ToolMode.AddNodes )
        HandleAddNodesMode( sceneView );
    }

    private void HandleAddNodesMode( SceneView sceneView )
    {
      if ( m_addNodeState == AddNodesState.RaycastTarget ) {
        if ( GetChild<SelectGameObjectTool>() == null ) {
          AddChild( new SelectGameObjectTool( OnGameObjectSelected ) );
          RaycastHitVisual.OnMouseClick += OnRaycastClick;
        }

        AgXUnity.Utils.Raycast.Hit hit = AgXUnity.Utils.Raycast.Test( m_currentTarget, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) );
        if ( hit.Triangle.Valid ) {
          RaycastHitVisual.Visible = true;
          RaycastHitVisual.SetTransform( hit.Triangle.Point, Quaternion.LookRotation( hit.Triangle.Normal, hit.ClosestEdge.Edge.Direction ), 0.25f );
        }
      }
      else if ( m_addNodeState == AddNodesState.Done ) {
        RemoveAllChildren();

        if ( m_currentNode != null ) {
          AddChild( new FrameTool( m_currentNode.Frame ) );
          Wire.Route.Add( m_currentNode );
        }

        m_currentNode = null;
        m_currentTarget = null;
        m_addNodeState = AddNodesState.RaycastTarget;
        Utils.GUI.SetWireRouteFoldoutState( Wire, true );
      }
    }
    
    private void OnGameObjectSelected( GameObject gameObject )
    {
      m_currentTarget = gameObject;
      RemoveChild( GetChild<FrameTool>() );
    }

    private void OnRaycastClick( Utils.VisualPrimitive primitive )
    {
      RemoveChild( GetChild<SelectGameObjectTool>() );

      m_currentNode = new Wire.RouteNode( Wire.NodeType.FreeNode, m_currentTarget );
      
      m_currentNode.Frame.Position = primitive.Node.transform.position;
      m_currentNode.Frame.Rotation = primitive.Node.transform.rotation;

      m_addNodeState = AddNodesState.Done;
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      if ( Mode == ToolMode.AddNodes ) {
        if ( m_addNodeState == AddNodesState.RaycastTarget ) {
          if ( m_currentTarget == null )
            GUILayout.Label( Utils.GUI.MakeLabel( "Pick object in scene view to be the <b>target</b> object." ), skin.label );
          else
            GUILayout.Label( Utils.GUI.MakeLabel( "Click surface where the node should be positioned." ), skin.label );
        }
      }
    }
  }
}
