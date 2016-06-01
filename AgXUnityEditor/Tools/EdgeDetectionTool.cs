using System;
using UnityEditor;
using UnityEngine;
using AgXUnity.Collide;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class EdgeDetectionTool : Tool
  {
    public class Data
    {
      public GameObject Target = null;
      public Raycast.ClosestEdgeHit EdgeData = Raycast.ClosestEdgeHit.Invalid;
    }

    private Data m_currentData = null;

    private Utils.VisualPrimitiveCylinder EdgeVisual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveCylinder>( "edgeVisual", "Transparent/Diffuse" ); } }

    public Action<Data> OnEdgeFound = delegate { };

    public EdgeDetectionTool()
    {
      EdgeVisual.OnMouseClick += OnEdgeClick;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( EdgeVisual.Visible && EdgeVisual.MouseOver )
        return;

      if ( m_currentData == null ) {
        if ( GetChild<SelectGameObjectTool>() == null ) {
          SelectGameObjectTool selectGameObjectTool = new SelectGameObjectTool();
          selectGameObjectTool.OnSelect = go =>
          {
            m_currentData = new Data() { Target = go };
          };
          AddChild( selectGameObjectTool );
        }
      }
      else {
        m_currentData.EdgeData = Raycast.Test( m_currentData.Target, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) ).ClosestEdge;
      }

      OnSceneViewGUIChildren( sceneView );

      EdgeVisual.Visible = m_currentData != null && m_currentData.EdgeData.Valid;
      if ( EdgeVisual.Visible ) {
        const float edgeRadius     = 0.035f;
        const float defaultAlpha   = 0.25f;
        const float mouseOverAlpha = 0.65f;

        EdgeVisual.SetTransform( m_currentData.EdgeData.Edge.Start, m_currentData.EdgeData.Edge.End, edgeRadius );

        if ( m_currentData.EdgeData.Edge.Type == MeshUtils.Edge.EdgeType.Triangle ) {
          EdgeVisual.Color = new Color( Color.yellow.r, Color.yellow.g, Color.yellow.b, defaultAlpha );
          EdgeVisual.MouseOverColor = new Color( Color.yellow.r, Color.yellow.g, Color.yellow.b, mouseOverAlpha );
        }
        else if ( m_currentData.EdgeData.Edge.Type == MeshUtils.Edge.EdgeType.Principal ) {
          EdgeVisual.Color = new Color( Color.red.r, Color.red.g, Color.red.b, defaultAlpha );
          EdgeVisual.MouseOverColor = new Color( Color.red.r, Color.red.g, Color.red.b, mouseOverAlpha );
        }
      }
    }

    private void OnEdgeClick( Utils.VisualPrimitive primitive )
    {
      OnEdgeFound( m_currentData );

      PerformRemoveFromParent();
    }
  }
}
