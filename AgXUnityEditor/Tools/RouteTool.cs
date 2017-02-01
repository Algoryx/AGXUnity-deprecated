using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class RouteTool<ParentT, NodeT> : Tool
    where ParentT : ScriptComponent
    where NodeT : RouteNode
  {
    public Func<float> NodeVisualRadius = null;

    public ParentT Parent { get; private set; }
    public Route<NodeT> Route { get; private set; }

    private NodeT m_selected = null;
    public NodeT Selected
    {
      get { return m_selected; }
      set
      {
        if ( value == m_selected )
          return;

        if ( m_selected != null ) {
          GetFoldoutData( m_selected ).Bool = false;
          SelectedTool.FrameTool.TransformHandleActive = false;
        }

        m_selected = value;

        if ( m_selected != null ) {
          GetFoldoutData( m_selected ).Bool = true;
          SelectedTool.FrameTool.TransformHandleActive = true;
          EditorUtility.SetDirty( Parent );
        }
      }
    }

    RouteNodeTool SelectedTool { get { return FindActive<RouteNodeTool>( this, ( tool ) => { return tool.Node == m_selected; } ); } }

    public bool DisableCollisionsTool
    {
      get { return GetChild<DisableCollisionsTool>() != null; }
      set
      {
        if ( value && !DisableCollisionsTool ) {
          var disableCollisionsTool = new DisableCollisionsTool( Parent.gameObject );
          AddChild( disableCollisionsTool );
        }
        else if ( !value )
          RemoveChild( GetChild<DisableCollisionsTool>() );
      }
    }

    public RouteTool( ParentT parent, Route<NodeT> route )
    {
      Parent = parent;
      Route = route;
    }

    public override void OnAdd()
    {
      HideDefaultHandlesEnableWhenRemoved();

      if ( !EditorApplication.isPlaying ) {
        foreach ( var node in Route ) {
          CreateRouteNodeTool( node );
          if ( GetFoldoutData( node ).Bool )
            Selected = node;
        }
      }
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      bool toggleDisableCollisions = false;

      GUILayout.BeginHorizontal();
      {
        GUI.ToolsLabel( skin );

        using ( GUI.ToolButtonData.ColorBlock ) {
          toggleDisableCollisions = GUI.ToolButton( GUI.Symbols.DisableCollisionsTool, DisableCollisionsTool, "Disable collisions against other objects", skin );
        }
      }
      GUILayout.EndHorizontal();

      if ( DisableCollisionsTool ) {
        GetChild<DisableCollisionsTool>().OnInspectorGUI( skin );

        GUI.Separator();
      }

      if ( !EditorApplication.isPlaying )
        RouteGUI( skin );

      if ( toggleDisableCollisions )
        DisableCollisionsTool = !DisableCollisionsTool;
    }

    protected virtual string GetNodeTypeString() { return string.Empty; }
    protected virtual void OnPreFrameGUI( NodeT node, GUISkin skin ) { }
    protected virtual void OnPostFrameGUI( NodeT node, GUISkin skin ) { }
    protected virtual void OnNodeCreate( NodeT newNode, NodeT refNode, bool addPressed ) { }

    private void RouteGUI( GUISkin skin )
    {
      bool addNewPressed        = false;
      bool insertBeforePressed  = false;
      bool insertAfterPressed   = false;
      bool erasePressed         = false;
      NodeT listOpNode          = null;

      GUI.Separator();

      if ( GUI.Foldout( EditorData.Instance.GetData( Parent, "Route", ( entry ) => { entry.Bool = true; } ), GUI.MakeLabel( "Route", true ), skin ) ) {
        GUI.Separator();

        foreach ( var node in Route ) {
          Undo.RecordObject( node, "RouteNode" );

          using ( new GUI.Indent( 12 ) ) {
            if ( GUI.Foldout( GetFoldoutData( node ),
                              GUI.MakeLabel( GetNodeTypeString() + " | " + SelectGameObjectDropdownMenuTool.GetGUIContent( node.Frame.Parent ).text ),
                              skin,
                              ( newState ) =>
                              {
                                Selected = newState ? node : null;
                                EditorUtility.SetDirty( Parent );
                              } ) ) {

              OnPreFrameGUI( node, skin );

              GUI.HandleFrame( node.Frame, skin, 12 );

              OnPostFrameGUI( node, skin );

              GUILayout.BeginHorizontal();
              {
                GUILayout.FlexibleSpace();

                using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.green, 0.1f ) ) ) {
                  insertBeforePressed = GUILayout.Button( GUI.MakeLabel( GUI.Symbols.ListInsertElementBefore.ToString(),
                                                                         16,
                                                                         false,
                                                                         "Insert a new node before this node" ),
                                                          skin.button,
                                                          GUILayout.Width( 20 ),
                                                          GUILayout.Height( 16 ) );
                  insertAfterPressed  = GUILayout.Button( GUI.MakeLabel( GUI.Symbols.ListInsertElementAfter.ToString(),
                                                                         16,
                                                                         false,
                                                                         "Insert a new node after this node" ),
                                                          skin.button,
                                                          GUILayout.Width( 20 ),
                                                          GUILayout.Height( 16 ) );
                }
                using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.red, 0.1f ) ) )
                  erasePressed        = GUILayout.Button( GUI.MakeLabel( GUI.Symbols.ListEraseElement.ToString(),
                                                                         16,
                                                                         false,
                                                                         "Erase this node" ),
                                                          skin.button,
                                                          GUILayout.Width( 20 ),
                                                          GUILayout.Height( 16 ) );

                if ( listOpNode == null && ( insertBeforePressed || insertAfterPressed || erasePressed ) )
                  listOpNode = node;
              }
              GUILayout.EndHorizontal();
            }

            GUI.Separator();
          }

          if ( GUILayoutUtility.GetLastRect().Contains( Event.current.mousePosition ) &&
               Event.current.type == EventType.MouseDown &&
               Event.current.button == 0 ) {
            Selected = node;
          }
        }

        GUILayout.BeginHorizontal();
        {
          GUILayout.FlexibleSpace();

          using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.green, 0.1f ) ) )
            addNewPressed = GUILayout.Button( GUI.MakeLabel( GUI.Symbols.ListInsertElementAfter.ToString(),
                                                             16,
                                                             false,
                                                             "Add new node to route" ),
                                              skin.button,
                                              GUILayout.Width( 20 ),
                                              GUILayout.Height( 16 ) );
          if ( listOpNode == null && addNewPressed )
            listOpNode = Route.LastOrDefault();
        }
        GUILayout.EndHorizontal();
      }

      GUI.Separator();

      if ( addNewPressed || insertBeforePressed || insertAfterPressed ) {
        NodeT newRouteNode = null;
        // Clicking "Add" will not copy data from last node.
        newRouteNode = listOpNode != null ?
                         addNewPressed ?
                           RouteNode.Create<NodeT>( null, listOpNode.Frame.Position, listOpNode.Frame.Rotation ) :
                           RouteNode.Create<NodeT>( listOpNode.Frame.Parent, listOpNode.Frame.LocalPosition, listOpNode.Frame.LocalRotation ) :
                         RouteNode.Create<NodeT>();

        OnNodeCreate( newRouteNode, listOpNode, addNewPressed );

        bool success = false;
        if ( addNewPressed )
          success = Route.Add( newRouteNode );
        if ( insertBeforePressed )
          success = Route.InsertBefore( newRouteNode, listOpNode );
        if ( insertAfterPressed )
          success = Route.InsertAfter( newRouteNode, listOpNode );

        if ( success ) {
          Undo.RegisterCreatedObjectUndo( newRouteNode, "New route node" );

          CreateRouteNodeTool( newRouteNode );
          Selected = newRouteNode;
        }
        else
          ScriptAsset.DestroyImmediate( newRouteNode );
      }
      else if ( listOpNode != null && erasePressed ) {
        Selected = null;
        Route.Remove( listOpNode );
      }
    }

    private void CreateRouteNodeTool( NodeT node )
    {
      AddChild( new RouteNodeTool( node,
                                   Parent,
                                   () => { return Selected; },
                                   ( selected ) => { Selected = selected as NodeT; },
                                   ( n ) => { return Route.Contains( n as NodeT ); },
                                   NodeVisualRadius ) );
    }

    private EditorDataEntry GetData( NodeT node, string identifier, Action<EditorDataEntry> onCreate = null )
    {
      return EditorData.Instance.GetData( node, identifier, onCreate );
    }

    private EditorDataEntry GetFoldoutData( NodeT node )
    {
      return GetData( node, "foldout", ( entity ) => { entity.Bool = false; } );
    }
  }
}
