﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Rendering;
using AgXUnity.Collide;
using Tool = AgXUnityEditor.Tools.Tool;

namespace AgXUnityEditor.Utils
{
  public static class DrawGizmoCallbackHandler
  {
    private static ObjectsGizmoColorHandler m_colorHandler = new ObjectsGizmoColorHandler();

    [DrawGizmo( GizmoType.Active | GizmoType.NotInSelectionHierarchy )]
    public static void OnDrawGizmosDebugRenderManager( DebugRenderManager manager, GizmoType gizmoType )
    {
      if ( !manager.isActiveAndEnabled )
        return;

      // List containing active tools decisions of what could be considered selected.
      var toolsSelections = new List<Tool.VisualizedSelectionData>();
      // Active assembly tool has special rendering needs.
      Tools.AssemblyTool assemblyTool = null;

      Tool.TraverseActive( activeTool =>
      {
        if ( assemblyTool == null && activeTool is Tools.AssemblyTool )
          assemblyTool = activeTool as Tools.AssemblyTool;

        if ( activeTool.VisualizedSelection != null && !toolsSelections.Contains( activeTool.VisualizedSelection ) )
          toolsSelections.Add( activeTool.VisualizedSelection );
      } );

      // Find if we've any active selections.
      bool selectionActive = toolsSelections.Count > 0 ||
                             ( assemblyTool != null && assemblyTool.HasActiveSelections() ) ||
                             Array.Exists( Selection.gameObjects, go => { return go.GetComponent<Shape>() != null || go.GetComponent<RigidBody>() != null; } );
      if ( !selectionActive )
        m_colorHandler.TimeInterpolator.Reset();

      // Early exist if we're not visualizing the bodies nor have active selections.
      bool active = manager.VisualizeBodies || selectionActive || assemblyTool != null;
      if ( !active )
        return;

      try {
        using ( m_colorHandler.BeginEndScope() ) {
          // Create unique colors for each rigid body in the scene.
          {
            var bodies = UnityEngine.Object.FindObjectsOfType<RigidBody>();
            Array.Sort( bodies, ( b1, b2 ) => { return b1.GetInstanceID() > b2.GetInstanceID() ? -1 : 1; } );

            foreach ( var body in bodies ) {
              // Create the color for all bodies for the colors to be consistent.
              m_colorHandler.GetOrCreateColor( body );

              if ( manager.VisualizeBodies )
                m_colorHandler.Colorize( body );
            }
          }

          // An active assembly tool will (atm) render objects in a different
          // way and, e.g., render colorized bodies despite manager.VisualizeBodies.
          if ( assemblyTool != null )
            assemblyTool.OnRenderGizmos( m_colorHandler );

          // Handling objects selected in our tools.
          {
            foreach ( var toolSelection in toolsSelections )
              HandleSelectedGameObject( toolSelection.Object, ObjectsGizmoColorHandler.SelectionType.VaryingIntensity );
          }

          // Handling objects selected in the editor.
          {
            GameObject[] editorSelections = Selection.gameObjects;
            foreach ( var editorSelection in editorSelections )
              HandleSelectedGameObject( editorSelection, ObjectsGizmoColorHandler.SelectionType.ConstantColor );
          }

          foreach ( var filterColorPair in m_colorHandler.ColoredMeshFilters ) {
            Gizmos.color = filterColorPair.Value;
            Gizmos.matrix = filterColorPair.Key.transform.localToWorldMatrix;
            Gizmos.DrawWireMesh( filterColorPair.Key.sharedMesh );
          }
        }
      }
      catch ( System.Exception e ) {
        Debug.LogException( e );
      }
    }

    private static void HandleSelectedGameObject( GameObject selected, ObjectsGizmoColorHandler.SelectionType selectionType )
    {
      if ( selected == null )
        return;

      RigidBody rb      = null;
      Shape shape       = null;
      MeshFilter filter = null;
      if ( ( rb = selected.GetComponent<RigidBody>() ) != null ) {
        m_colorHandler.Highlight( rb, selectionType );
      }
      else if ( ( shape = selected.GetComponent<Shape>() ) != null ) {
        m_colorHandler.Highlight( shape, selectionType );
      }
      else if ( ( filter = selected.GetComponent<MeshFilter>() ) != null ) {
        m_colorHandler.Highlight( filter, selectionType );
      }
    }
  }
}
