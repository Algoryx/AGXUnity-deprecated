﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Tools
{
  public class Tool
  {
    /// <summary>
    /// Color of the x axis.
    /// </summary>
    /// <param name="alpha">Alpha value.</param>
    /// <returns>Color of the x axis.</returns>
    public static UnityEngine.Color GetXAxisColor( float alpha = 1.0f )
    {
      return new UnityEngine.Color( 1.0f, 0, 0, alpha );
    }

    /// <summary>
    /// Color of the y axis.
    /// </summary>
    /// <param name="alpha">Alpha value.</param>
    /// <returns>Color of the y axis.</returns>
    public static UnityEngine.Color GetYAxisColor( float alpha = 1.0f )
    {
      return new UnityEngine.Color( 0, 1.0f, 0, alpha );
    }

    /// <summary>
    /// Color of the z axis.
    /// </summary>
    /// <param name="alpha">Alpha value.</param>
    /// <returns>Color of the z axis.</returns>
    public static UnityEngine.Color GetZAxisColor( float alpha = 1.0f )
    {
      return new UnityEngine.Color( 0, 0, 1.0f, alpha );
    }

    /// <summary>
    /// Color of the center.
    /// </summary>
    /// <param name="alpha">Alpha value.</param>
    /// <returns>Color of the center.</returns>
    public static UnityEngine.Color GetCenterColor( float alpha = 1.0f )
    {
      return new UnityEngine.Color( 0.7f, 0.7f, 0.7f, alpha );
    }

    /// <summary>
    /// Creates a position tool given position and rotation.
    /// </summary>
    /// <param name="position">Current position.</param>
    /// <param name="rotation">Current rotation.</param>
    /// <param name="scale">Scale - default 1.0f.</param>
    /// <param name="alpha">Alpha - default 1.0f.</param>
    /// <returns>New position of the tool.</returns>
    public static Vector3 PositionTool( Vector3 position, Quaternion rotation, float scale = 1.0f, float alpha = 1.0f )
    {
      Vector3 snapSetting = new Vector3( 0.5f, 0.5f, 0.5f );

      Color orgColor = Handles.color;

      float handleSize = HandleUtility.GetHandleSize( position );
      Color color      = Handles.color;
      Handles.color    = GetXAxisColor( alpha );
      position         = Handles.Slider( position, rotation * Vector3.right, scale * handleSize, new Handles.DrawCapFunction( Handles.ArrowCap ), snapSetting.x );
      Handles.color    = GetYAxisColor( alpha );
      position         = Handles.Slider( position, rotation * Vector3.up, scale * handleSize, new Handles.DrawCapFunction( Handles.ArrowCap ), snapSetting.y );
      Handles.color    = GetZAxisColor( alpha );
      position         = Handles.Slider( position, rotation * Vector3.forward, scale * handleSize, new Handles.DrawCapFunction( Handles.ArrowCap ), snapSetting.z );

      float slider2DSize = scale * handleSize * 0.15f;
      Func<Vector3, Vector3, Vector3, Vector3> PlaneHandle = ( normal, dir1, dir2 ) =>
      {
        Vector3 offset = slider2DSize * ( rotation * dir1 + rotation * dir2 );
        Vector3 result = Handles.Slider2D( position + offset, rotation * normal, rotation * dir1, rotation * dir2, slider2DSize, new Handles.DrawCapFunction( Handles.RectangleCap ), snapSetting.x );
        result -= offset;
        return result;
      };

      Handles.color = GetXAxisColor( 0.3f );
      position      = PlaneHandle( Vector3.right,   Vector3.up,    Vector3.forward );
      Handles.color = GetYAxisColor( 0.3f );        
      position      = PlaneHandle( Vector3.up,      Vector3.right, Vector3.forward );
      Handles.color = GetZAxisColor( 0.3f );        
      position      = PlaneHandle( Vector3.forward, Vector3.right, Vector3.up );

      Handles.color = orgColor;

      return position;
    }

    /// <summary>
    /// Creates a rotation tool given position and rotation.
    /// </summary>
    /// <param name="position">Current position.</param>
    /// <param name="rotation">Current rotation.</param>
    /// <param name="scale">Scale - default 1.0f.</param>
    /// <param name="alpha">Alpha - default 1.0f.</param>
    /// <returns>New rotation of the tool.</returns>
    public static Quaternion RotationTool( Vector3 position, Quaternion rotation, float scale = 1.0f, float alpha = 1.0f )
    {
      float snapSetting = 0.5f;

      Color orgColor = Handles.color;

      float handleSize = HandleUtility.GetHandleSize( position );
      Color color = Handles.color;
      Handles.color = GetXAxisColor( alpha );
      rotation = Handles.Disc( rotation, position, rotation * Vector3.right, scale * handleSize, true, snapSetting );
      Handles.color = GetYAxisColor( alpha );
      rotation = Handles.Disc( rotation, position, rotation * Vector3.up, scale * handleSize, true, snapSetting );
      Handles.color = GetZAxisColor( alpha );
      rotation = Handles.Disc( rotation, position, rotation * Vector3.forward, scale * handleSize, true, snapSetting );
      Handles.color = GetCenterColor( 0.6f * alpha );
      rotation = Handles.Disc( rotation, position, Camera.current.transform.forward, scale * handleSize * 1.1f, false, 0f );
      rotation = Handles.FreeRotateHandle( rotation, position, scale * handleSize );

      Handles.color = orgColor;

      return rotation;
    }

    /// <summary>
    /// Creates single direction slider tool.
    /// </summary>
    /// <param name="position">Position of the slider.</param>
    /// <param name="direction">Direction of the slider.</param>
    /// <param name="color">Color of the slider.</param>
    /// <param name="scale">Scale - default 1.0.</param>
    /// <returns>New position of the slider.</returns>
    public static Vector3 SliderTool( Vector3 position, Vector3 direction, Color color, float scale = 1.0f )
    {
      float snapSetting = 0.001f;

      Color prevColor = Handles.color;
      Handles.color = color;
      float handleSize = HandleUtility.GetHandleSize( position );
      position = Handles.Slider( position, direction, scale * handleSize, new Handles.DrawCapFunction( Handles.ArrowCap ), snapSetting );
      Handles.color = prevColor;

      return position;
    }

    /// <summary>
    /// Creates a slider tool but instead of the new position this function returns the movement.
    /// </summary>
    /// <param name="position">Position of the slider.</param>
    /// <param name="direction">Direction of the slider.</param>
    /// <param name="color">Color of the slider.</param>
    /// <param name="scale">Scale - default 1.0.</param>
    /// <returns>How much the slider has been moved.</returns>
    public static Vector3 DeltaSliderTool( Vector3 position, Vector3 direction, Color color, float scale = 1.0f )
    {
      Vector3 newPosition = SliderTool( position, direction, color, scale );
      return newPosition - position;
    }

    /// <summary>
    /// Remove old tool (if present) and activate new. If <paramref name="tool"/> is null
    /// the current active tool is removed.
    /// </summary>
    /// <param name="tool">New top level tool to activate - null is equal to RemoveActiveTool.</param>
    /// <returns>The new tool.</returns>
    public static Tool ActivateTool( Tool tool )
    {
      RemoveActiveTool();

      m_active = tool;
      m_active.OnAdd();

      return m_active;
    }

    /// <summary>
    /// Remove old tool (if present) and activate new. If <paramref name="tool"/> is null
    /// the current active tool is removed.
    /// </summary>
    /// <typeparam name="T">Type of the tool passed to this method.</typeparam>
    /// <param name="tool">New top level tool to activate - null is equal to RemoveActiveTool.</param>
    /// <returns>The new tool.</returns>
    public static T ActivateTool<T>( Tool tool ) where T : Tool
    {
      return ActivateTool( tool ) as T;
    }

    /// <summary>
    /// Remove current, top level, active tool.
    /// </summary>
    public static void RemoveActiveTool()
    {
      if ( m_active != null ) {
        Tool tool = m_active;

        // PerformRemoveFromParent will check if the tool is the current active.
        // If the tool wants to remove itself and is our m_activeToolData then
        // we'll receive a call back to this method from PerformRemoveFromParent.
        m_active = null;

        tool.PerformRemoveFromParent();
      }
    }

    /// <summary>
    /// Fetch current active, top level, tool given type.
    /// </summary>
    /// <typeparam name="T">Type of the tool.</typeparam>
    /// <returns>Active tool of type T.</returns>
    public static T GetActiveTool<T>() where T : Tool
    {
      return m_active as T;
    }

    /// <summary>
    /// Fetch current active, top level, tool.
    /// </summary>
    /// <returns>Current active, top level, tool.</returns>
    public static Tool GetActiveTool()
    {
      return m_active;
    }

    /// <summary>
    /// Searches active tool from top level, depth first, given predicate.
    /// </summary>
    /// <typeparam name="T">Type of the tool.</typeparam>
    /// <param name="pred">Tool predicate.</param>
    /// <returns>Tool given type and predicate if active - otherwise null.</returns>
    public static T FindActive<T>( Predicate<T> pred ) where T : Tool
    {
      return FindActive( m_active, pred );
    }
    /// <summary>
    /// Searches active tool from top level, depth first, given predicate.
    /// </summary>
    /// <typeparam name="T">Type of the tool.</typeparam>
    /// <param name="tool">Parent tool to start from.</param>
    /// <param name="pred">Tool predicate.</param>
    /// <returns>Tool given type and predicate if active - otherwise null.</returns>
    public static T FindActive<T>( Tool tool, Predicate<T> pred ) where T : Tool
    {
      if ( tool == null )
        return null;

      T typedTool = tool as T;
      if ( typedTool != null && pred( typedTool ) )
        return typedTool;

      foreach ( Tool child in tool.GetChildren() ) {
        T result = FindActive( child, pred );
        if ( result != null )
          return result;
      }

      return null;
    }

    private static Tool m_active  = null;
    private List<Tool> m_children = new List<Tool>();
    private Tool m_parent         = null;

    private Dictionary<string, Utils.VisualPrimitive> m_visualPrimitives = new Dictionary<string, Utils.VisualPrimitive>();

    public Tool()
    {
    }

    public virtual void OnSceneViewGUI( SceneView sceneView ) { }

    public virtual void OnInspectorGUI( GUISkin skin ) { }

    public virtual void OnAdd() { }

    public virtual void OnRemove() { }

    public Tool GetParent()
    {
      return m_parent;
    }

    public T GetChild<T>() where T : Tool
    {
      return GetChildren<T>().FirstOrDefault();
    }

    public T[] GetChildren<T>() where T : Tool
    {
      return ( from child in m_children where child.GetType() == typeof( T ) select child as T ).ToArray();
    }

    public Tool[] GetChildren()
    {
      return m_children.ToArray();
    }

    public void PerformRemoveFromParent()
    {
      if ( GetActiveTool() == this ) {
        RemoveActiveTool();
        return;
      }

      PerformRemove();
    }
    public void Remove()
    {
      PerformRemoveFromParent();
    }

    protected void AddChild( Tool child )
    {
      if ( child == null || m_children.Contains( child ) )
        return;

      m_children.Add( child );
      child.m_parent = this;
      child.OnAdd();
    }

    protected void RemoveChild( Tool child )
    {
      if ( child == null || !m_children.Contains( child ) )
        return;

      child.PerformRemoveFromParent();
    }

    protected void RemoveAllChildren()
    {
      while ( m_children.Count > 0 )
        m_children[ m_children.Count - 1 ].PerformRemoveFromParent();
    }

    protected T GetOrCreateVisualPrimitive<T>( string name, string shader = "Unlit/Color" ) where T : Utils.VisualPrimitive
    {
      T primitive = GetVisualPrimitive<T>( name );
      if ( primitive != null )
        return primitive;

      primitive = (T)Activator.CreateInstance( typeof( T ), new object[] { shader } );
      m_visualPrimitives.Add( name, primitive );

      return primitive;
    }

    protected T GetVisualPrimitive<T>( string name ) where T : Utils.VisualPrimitive
    {
      Utils.VisualPrimitive primitive = null;
      // C-cast style cast to throw if the type isn't matching.
      if ( m_visualPrimitives.TryGetValue( name, out primitive ) )
        return (T)primitive;

      return null;
    }

    protected void RemoveVisualPrimitive( string name )
    {
      Utils.VisualPrimitive primitive = null;
      if ( m_visualPrimitives.TryGetValue( name, out primitive ) ) {
        primitive.Destruct();
        m_visualPrimitives.Remove( name );
      }
    }

    protected void RemoveVisualPrimitive( Utils.VisualPrimitive primitive )
    {
      RemoveVisualPrimitive( m_visualPrimitives.First( kvp => kvp.Value == primitive ).Key );
    }

    protected void OnSceneViewGUIChildren( SceneView sceneView )
    {
      // An extra ToList here (i.e., copy of the original) to handle
      // add /remove of children during this update.
      foreach ( var child in m_children.ToList() )
        child.OnSceneViewGUI( sceneView );
    }

    private void PerformRemove()
    {
      // OnRemove virtual callback.
      OnRemove();

      // Remove all windows that hasn't been closed.
      SceneViewWindow.CloseAllWindows( this );

      // Remove all visual primitives that hasn't been removed.
      string[] visualPrimitiveNames = m_visualPrimitives.Keys.ToArray();
      foreach ( string visualPrimitiveName in visualPrimitiveNames )
        RemoveVisualPrimitive( visualPrimitiveName );

      // Remove us from our parent.
      if ( m_parent != null )
        m_parent.m_children.Remove( this );
      m_parent = null;

      // Remove children.
      Tool[] children = m_children.ToArray();
      foreach ( Tool child in children )
        child.PerformRemove();
    }
  }
}
