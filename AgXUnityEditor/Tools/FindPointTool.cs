using System;
using UnityEngine;
using UnityEditor;
using AgXUnity.Utils;

namespace AgXUnityEditor.Tools
{
  public class FindPointTool : Tool
  {
    public struct PointData
    {
      public static PointData CreateDefault() { return new PointData() { Parent = null, WorldPosition = Vector3.zero, WorldRotation = Quaternion.identity, Valid = false }; }
      public GameObject Parent;
      public Vector3    WorldPosition;
      public Quaternion WorldRotation;
      public bool       Valid;
    }

    public Action<PointData> OnNewPointData = delegate { };

    public Utils.VisualPrimitiveSphere PointVisual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveSphere>( "point", "Transparent/Diffuse" ); } }

    public bool RemoveSelfWhenDone = false;

    public FindPointTool()
    {
      PointVisual.Pickable = false;
      PointVisual.MouseOverColor = PointVisual.Color;

      AddChild( new SelectGameObjectDropdownMenuTool() { Title = "Select point parent", OnSelect = OnGameObjectSelect } );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      SelectGameObjectDropdownMenuTool menuTool = GetChild<SelectGameObjectDropdownMenuTool>();
      OnSceneViewGUIChildren( sceneView );

      if ( m_pointData.Valid ) {
        OnNewPointData( m_pointData );
        m_pointData = PointData.CreateDefault();
        if ( RemoveSelfWhenDone )
          PerformRemoveFromParent();
        return;
      }

      if ( Manager.IsCameraControl || menuTool.WindowIsActive )
        return;

      Raycast.Hit hit = Raycast.Hit.Invalid;
      if ( Manager.MouseOverObject != null )
        hit = Raycast.Test( Manager.MouseOverObject.GetRoot(), HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ), 100.0f, true );

      PointVisual.Visible = hit.Triangle.Valid;
      if ( hit.Triangle.Valid )
        PointVisual.SetTransform( hit.Triangle.Point, Quaternion.LookRotation( hit.Triangle.Normal, hit.Triangle.ClosestEdge.Direction ), 0.1f, true, 0.05f, 0.25f );

      if ( hit.Triangle.Valid && Manager.HijackLeftMouseClick() ) {
        m_pointData.WorldPosition = hit.Triangle.Point;
        m_pointData.WorldRotation = Quaternion.LookRotation( hit.Triangle.Normal, hit.Triangle.ClosestEdge.Direction );
        menuTool.Target           = hit.Triangle.Target;

        menuTool.Show();
      }
    }

    private PointData m_pointData = PointData.CreateDefault();

    private void OnGameObjectSelect( GameObject gameObject )
    {
      m_pointData.Parent = gameObject;
      m_pointData.Valid = true;
    }
  }
}
