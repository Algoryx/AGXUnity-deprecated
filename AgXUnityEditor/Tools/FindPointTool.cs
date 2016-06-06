using System;
using UnityEngine;
using UnityEditor;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class FindPointTool : Tool
  {
    public class Data
    {
      public GameObject Target = null;
      public Raycast.TriangleHit Triangle = Raycast.TriangleHit.Invalid;
    }

    private Data m_currentData = null;

    public Action<Data> OnPointFound = delegate { };

    public Utils.VisualPrimitiveSphere PointVisual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveSphere>( "point", "Transparent/Diffuse" ); } }

    public FindPointTool()
    {
      PointVisual.Pickable       = false;
      PointVisual.MouseOverColor = PointVisual.Color;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
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
        m_currentData.Triangle = Raycast.TriangleHit.Invalid;

        // TODO: Handle world point?
        if ( m_currentData.Target == null ) {
          PerformRemoveFromParent();
          return;
        }

        m_currentData.Triangle = Raycast.Test( m_currentData.Target, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) ).Triangle;
      }

      PointVisual.Visible = m_currentData != null && m_currentData.Triangle.Valid;
      if ( PointVisual.Visible ) {
        PointVisual.SetTransform( m_currentData.Triangle.Point, Quaternion.identity, 0.05f );

        if ( Manager.HijackLeftMouseClick() ) {
          OnPointFound( m_currentData );
          PerformRemoveFromParent();
        }
      }
    }
  }
}
