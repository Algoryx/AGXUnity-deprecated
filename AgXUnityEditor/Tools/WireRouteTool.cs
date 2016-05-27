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

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( GetChild<FindPointTool>() == null ) {
        FindPointTool pointTool = new FindPointTool()
        {
          RemoveSelfWhenDone = true
        };
        pointTool.OnNewPointData += OnNewPoint;

        AddChild( pointTool );
      }

      OnSceneViewGUIChildren( sceneView );
    }

    private void OnNewPoint( FindPointTool.PointData pointData )
    {
      AddNode( pointData.Parent, pointData.WorldPosition, pointData.WorldRotation );
    }

    private void AddNode( GameObject parent, Vector3 position, Quaternion rotation )
    {
      WireRouteNode node = WireRouteNode.Create( Wire.NodeType.FreeNode, parent );
      node.Frame.Position = position;
      node.Frame.Rotation = rotation;

      Wire.Route.Add( node );
    }
  }

  public class WireRouteToolOld : Tool
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

    private Utils.VisualPrimitivePlane SupportPlaneVisual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitivePlane>( "supportPlaneVisual", "Transparent/Diffuse" ); } }

    private GameObject m_currentTarget   = null;
    private WireRouteNode m_currentNode = null;
    private AddNodesState m_addNodeState = AddNodesState.RaycastTarget;

    public WireRouteToolOld( Wire wire, ToolMode mode )
    {
      Mode = mode;
      Wire = wire;

      SupportPlaneVisual.Color = SupportPlaneVisual.MouseOverColor = new Color( 0.0f, 1.0f, 1.0f, 0.4f );
      SupportPlaneVisual.OnMouseClick += primitive => OnRaycastClick( RaycastHitVisual );
    }

    public override void OnRemove()
    {
      //Tools.FrameTool frameTool = Manager.GetActiveTool<Tools.FrameTool>();
      //if ( frameTool != null ) {
      //  foreach ( Wire.RouteNode node in wire.Route ) {
      //    if ( node.Frame == frameTool.Frame ) {
      //      frameTool = Manager.ActivateTool<Tools.FrameTool>( null );
      //      break;
      //    }
      //  }
      //}
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

        bool supportPlaneActive = Event.current.control;
        // First time activate - position given current mouse position.
        if ( supportPlaneActive && !SupportPlaneVisual.Visible ) {
          SupportPlaneVisual.Visible = true;
          RaycastHitVisual.Pickable  = false;

          Vector3 supportPlanePosition = Vector3.zero;
          Quaternion supportPlaneRotation = Quaternion.identity;
          Vector2 supportPlaneSize = 0.5f * Vector2.one;

          // No target selected. Position somewhere in world given camera rays.
          if ( m_currentTarget == null ) {
            Ray centerRay = sceneView.camera.ViewportPointToRay( new Vector3( 0.5f, 0.5f, 1.0f ) );
            Ray mouseRay = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
            float distanceToSupportPlane = -10.0f;
            float cosTheta = Vector3.Dot( -centerRay.direction, mouseRay.direction );

            supportPlanePosition = centerRay.origin + ( distanceToSupportPlane / cosTheta ) * mouseRay.direction;
            supportPlaneRotation = sceneView.camera.transform.rotation * Quaternion.FromToRotation( Vector3.up, Vector3.back );
          }
          else {
            AgXUnity.Utils.Raycast.Hit currentTargetHit = AgXUnity.Utils.Raycast.Test( m_currentTarget, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) );
            if ( currentTargetHit.Triangle.Valid ) {
              supportPlanePosition = currentTargetHit.Triangle.Point;
              supportPlaneRotation = Quaternion.LookRotation( currentTargetHit.Triangle.ClosestEdge.Direction, currentTargetHit.Triangle.Normal );
            }
          }

          SupportPlaneVisual.SetTransform( supportPlanePosition,
                                           supportPlaneRotation,
                                           0.5f * Vector2.one );
        }
        else if ( supportPlaneActive ) {
          if ( Event.current.type == EventType.ScrollWheel ) {
            // Extra speed when shift is down.
            float scrollSpeed = Event.current.delta.y * ( 0.005f + 0.1f * System.Convert.ToSingle( Event.current.shift ) );
            SupportPlaneVisual.MoveDistanceAlongNormal( scrollSpeed, 0.5f );
            Event.current.Use();
          }
        }
        else {
          SupportPlaneVisual.Visible = false;
          RaycastHitVisual.Pickable = true;
        }

        GameObject currentTarget = SupportPlaneVisual.Visible ? SupportPlaneVisual.Node : m_currentTarget;

        AgXUnity.Utils.Raycast.Hit hit = AgXUnity.Utils.Raycast.Test( currentTarget, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) );
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

      m_currentNode = WireRouteNode.Create( Wire.NodeType.FreeNode, m_currentTarget );
      
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
