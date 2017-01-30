using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof( Cable ) )]
  public class CableTool : Tool
  {
    private class CableRouteNodeTool : Tool
    {
      public Cable Cable { get; private set; }

      public CableTool CableTool { get { return GetParent() as CableTool; } }

      public CableRouteNode Node { get; private set; }

      public FrameTool FrameTool
      {
        get { return GetChild<FrameTool>(); }
      }

      public Utils.VisualPrimitiveSphere Visual { get { return GetOrCreateVisualPrimitive<Utils.VisualPrimitiveSphere>( "cableRouteNode" ); } }

      public bool Selected
      {
        get { return CableTool.Selected == Node; }
        set { CableTool.Selected = value ? Node : null; }
      }

      public CableRouteNodeTool( CableRouteNode node, Cable cable )
      {
        Node = node;
        Cable = cable;
        AddChild( new FrameTool( node.Frame ) { OnChangeDirtyTarget = Cable, TransformHandleActive = false } );

        Visual.Color = Color.yellow;
        Visual.MouseOverColor = new Color( 0.1f, 0.96f, 0.15f, 1.0f );
        Visual.OnMouseClick += OnClick;
      }

      public override void OnSceneViewGUI( SceneView sceneView )
      {
        if ( Cable == null || Node == null || !Cable.Route.Contains( Node ) ) {
          PerformRemoveFromParent();
          return;
        }

        float radius = 3f * Cable.Radius;
        Visual.Visible = !EditorApplication.isPlaying;
        Visual.Color = Selected ? Visual.MouseOverColor : Color.yellow;
        Visual.SetTransform( Node.Frame.Position, Node.Frame.Rotation, radius, true, 1.2f * Cable.Radius, Mathf.Max( 1.5f * Cable.Radius, 0.25f ) );
      }

      private void OnClick( AgXUnity.Utils.Raycast.Hit hit, Utils.VisualPrimitive primitive )
      {
        Selected = true;
      }
    }

    public Cable Cable { get; private set; }

    private CableRouteNode m_selected = null;
    public CableRouteNode Selected
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
          EditorUtility.SetDirty( Cable );
        }
      }
    }

    CableRouteNodeTool SelectedTool { get { return FindActive<CableRouteNodeTool>( this, ( tool ) => { return tool.Node == m_selected; } ); } }

    public CableTool( Cable cable )
    {
      Cable = cable;
    }

    public override void OnAdd()
    {
      HideDefaultHandlesEnableWhenRemoved();

      foreach ( var node in Cable.Route ) {
        CreateRouteNodeTool( node );
        if ( GetFoldoutData( node ).Bool )
          Selected = node;
      }
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      bool addNewPressed        = false;
      bool insertBeforePressed  = false;
      bool insertAfterPressed   = false;
      bool erasePressed         = false;
      CableRouteNode listOpNode = null;

      GUI.Separator();

      if ( GUI.Foldout( EditorData.Instance.GetData( Cable, "Route" ), GUI.MakeLabel( "Route", true ), skin ) ) {
        GUI.Separator();

        foreach ( CableRouteNode node in Cable.Route ) {
          Undo.RecordObject( node, "RouteNode" );

          using ( new GUI.Indent( 12 ) ) {
            if ( GUI.Foldout( GetFoldoutData( node ),
                              GUI.MakeLabel( node.Type.ToString() + " | " + SelectGameObjectDropdownMenuTool.GetGUIContent( node.Frame.Parent ).text ),
                              skin,
                              ( newState ) =>
                              {
                                Selected = newState ? node : null;
                                EditorUtility.SetDirty( Cable );
                              } ) ) {
              GUI.HandleFrame( node.Frame, skin, 12 );

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
            listOpNode = Cable.Route.LastOrDefault();
        }
        GUILayout.EndHorizontal();
      }

      GUI.Separator();

      if ( addNewPressed || insertBeforePressed || insertAfterPressed ) {
        CableRouteNode newRouteNode = null;
        newRouteNode = listOpNode != null ?
                         CableRouteNode.Create( listOpNode.Type, listOpNode.Frame.Parent, listOpNode.Frame.LocalPosition, listOpNode.Frame.LocalRotation ) :
                         CableRouteNode.Create();

        bool success = false;
        if ( addNewPressed )
          success = Cable.Route.Add( newRouteNode );
        if ( insertBeforePressed )
          success = Cable.Route.InsertBefore( newRouteNode, listOpNode );
        if ( insertAfterPressed )
          success = Cable.Route.InsertAfter( newRouteNode, listOpNode );

        if ( success ) {
          Undo.RegisterCreatedObjectUndo( newRouteNode, "New route node" );

          CreateRouteNodeTool( newRouteNode );
          Selected = newRouteNode;
        }
        else
          CableRouteNode.DestroyImmediate( newRouteNode );
      }
      else if ( listOpNode != null && erasePressed ) {
        Selected = null;
        Cable.Route.Remove( listOpNode );
      }
    }

    private void CreateRouteNodeTool( CableRouteNode node )
    {
      AddChild( new CableRouteNodeTool( node, Cable ) );
    }

    private EditorDataEntry GetData( CableRouteNode node, string identifier, Action<EditorDataEntry> onCreate = null )
    {
      return EditorData.Instance.GetData( node, identifier, onCreate );
    }

    private EditorDataEntry GetFoldoutData( CableRouteNode node )
    {
      return GetData( node, "foldout", ( entity ) => { entity.Bool = false; } );
    }
  }

  [CustomEditor( typeof( CableProperties ) )]
  public class CablePropertiesEditor : BaseEditor<CableProperties>
  {
    protected override bool OverrideOnInspectorGUI( CableProperties properties, GUISkin skin )
    {
      if ( properties == null )
        return true;

      Undo.RecordObject( properties, "Cable properties" );

      GUI.Separator();

      using ( new GUI.Indent( 12 ) ) {
        foreach ( CableProperties.Direction dir in CableProperties.Directions ) {
          OnPropertyGUI( dir, properties, skin );
          GUI.Separator();
        }
      }

      if ( UnityEngine.GUI.changed )
        EditorUtility.SetDirty( properties );

      return true;
    }

    private void OnPropertyGUI( CableProperties.Direction dir, CableProperties properties, GUISkin skin )
    {
      Undo.RecordObject( properties[ dir ], "Cable property " + dir.ToString() );

      var data = EditorData.Instance.GetData( properties, "CableProperty" + dir.ToString() );
      if ( GUI.Foldout( data, GUI.MakeLabel( dir.ToString() ), skin ) ) {
        using ( new GUI.Indent( 12 ) ) {
          properties[ dir ].YoungsModulus = Mathf.Clamp( EditorGUILayout.FloatField( GUI.MakeLabel( "Young's modulus" ), properties[ dir ].YoungsModulus ), 1.0E-6f, float.PositiveInfinity );
          properties[ dir ].YieldPoint = Mathf.Clamp( EditorGUILayout.FloatField( GUI.MakeLabel( "Yield point" ), properties[ dir ].YieldPoint ), 0.0f, float.PositiveInfinity );
          properties[ dir ].Damping = Mathf.Clamp( EditorGUILayout.FloatField( GUI.MakeLabel( "Spook damping" ), properties[ dir ].Damping ), 0.0f, float.PositiveInfinity );
        }
      }

      if ( UnityEngine.GUI.changed )
        EditorUtility.SetDirty( properties[ dir ] );
    }
  }
}
