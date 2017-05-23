using UnityEngine;
using UnityEditor;
using AgXUnity.Collide;
using AgXUnity.Rendering;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof( Shape ) )]
  public class ShapeTool : Tool
  {
    public Shape Shape { get; private set; }

    public bool ShapeResizeTool
    {
      get { return GetChild<ShapeResizeTool>() != null; }
      set
      {
        if ( value && !ShapeResizeTool ) {
          RemoveAllChildren();

          var shapeResizeTool                                            = new ShapeResizeTool( Shape );
          shapeResizeTool.ActivateKey.HideDefaultHandlesWhenIsDown       = true;
          shapeResizeTool.SymmetricScaleKey.HideDefaultHandlesWhenIsDown = true;
          shapeResizeTool.RemoveOnKeyEscape                              = true;

          AddChild( shapeResizeTool );

          Manager.RequestSceneViewFocus();
        }
        else if ( !value )
          RemoveChild( GetChild<ShapeResizeTool>() );
      }
    }

    public bool DisableCollisionsTool
    {
      get { return GetChild<DisableCollisionsTool>() != null; }
      set
      {
        if ( value && !DisableCollisionsTool ) {
          RemoveAllChildren();

          var disableCollisionsTool = new DisableCollisionsTool( Shape.gameObject );
          AddChild( disableCollisionsTool );
        }
        else if ( !value )
          RemoveChild( GetChild<DisableCollisionsTool>() );
      }
    }

    public bool ShapeCreateTool
    {
      get { return GetChild<ShapeCreateTool>() != null; }
      set
      {
        if ( value && !ShapeCreateTool ) {
          RemoveAllChildren();

          var shapeCreateTool = new ShapeCreateTool( Shape.gameObject );
          AddChild( shapeCreateTool );
        }
        else if ( !value )
          RemoveChild( GetChild<ShapeCreateTool>() );
      }
    }

    public bool ShapeVisualCreateTool
    {
      get { return GetChild<ShapeVisualCreateTool>() != null; }
      set
      {
        if ( value && !ShapeVisualCreateTool ) {
          RemoveAllChildren();

          var createShapeVisualTool = new ShapeVisualCreateTool( Shape );
          AddChild( createShapeVisualTool );
        }
        else if ( !value )
          RemoveChild( GetChild<ShapeVisualCreateTool>() );
      }
    }

    public ShapeTool( Shape shape )
    {
      Shape = shape;
    }

    public override void OnAdd()
    {
    }

    public override void OnRemove()
    {
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      bool toggleShapeResizeTool   = false;
      bool toggleShapeCreate       = false;
      bool toggleDisableCollisions = false;
      bool toggleShapeVisualCreate = true;

      GUILayout.BeginHorizontal();
      {
        GUI.ToolsLabel( skin );

        using ( GUI.ToolButtonData.ColorBlock ) {
          using ( new EditorGUI.DisabledGroupScope( !Tools.ShapeResizeTool.SupportsShape( Shape ) ) )
            toggleShapeResizeTool = GUI.ToolButton( GUI.Symbols.ShapeResizeTool, ShapeResizeTool, "Shape resize tool", skin, 24 );

          toggleShapeCreate       = GUI.ToolButton( GUI.Symbols.ShapeCreateTool, ShapeCreateTool, "Create shape from visual objects", skin );
          toggleDisableCollisions = GUI.ToolButton( GUI.Symbols.DisableCollisionsTool, DisableCollisionsTool, "Disable collisions against other objects", skin );

          bool createShapeVisualValid = ShapeVisual.SupportsShapeVisual( Shape ) &&
                                        !ShapeVisual.HasShapeVisual( Shape );
          using ( new EditorGUI.DisabledGroupScope( !createShapeVisualValid ) )
            toggleShapeVisualCreate = GUI.ToolButton( GUI.Symbols.ShapeVisualCreateTool, ShapeVisualCreateTool, "Create visual representation of the physical shape", skin, 14 );
        }
      }
      GUILayout.EndHorizontal();

      GUI.Separator();

      if ( ShapeCreateTool ) {
        GetChild<ShapeCreateTool>().OnInspectorGUI( skin );

        GUI.Separator();
      }
      if ( DisableCollisionsTool ) {
        GetChild<DisableCollisionsTool>().OnInspectorGUI( skin );

        GUI.Separator();
      }
      if ( ShapeVisualCreateTool ) {
        GetChild<ShapeVisualCreateTool>().OnInspectorGUI( skin );

        GUI.Separator();
      }

      if ( toggleShapeResizeTool )
        ShapeResizeTool = !ShapeResizeTool;
      if ( toggleShapeCreate )
        ShapeCreateTool = !ShapeCreateTool;
      if ( toggleDisableCollisions )
        DisableCollisionsTool = !DisableCollisionsTool;
      if ( toggleShapeVisualCreate )
        ShapeVisualCreateTool = !ShapeVisualCreateTool;
    }

    public override void OnPostTargetMembersGUI( GUISkin skin )
    {
      var shapeVisual = ShapeVisual.Find( Shape );
      if ( shapeVisual == null )
        return;

      GUI.Separator();
      if ( !GUI.Foldout( EditorData.Instance.GetData( Shape, "Visual", entry => entry.Bool = false ), GUI.MakeLabel( "Shape Visual" ), skin ) )
        return;

      GUI.Separator();

      var materials = shapeVisual.GetMaterials();
      int materialCounter = 0;
      Material newMaterial = null;
      foreach ( var material in materials ) {
        if ( materials.Length == 1 || GUI.Foldout( EditorData.Instance.GetData( Shape, "VisualMaterial" + ( materialCounter++ ).ToString(), entry => entry.Bool = true ), GUI.MakeLabel( material.name ), skin ) )
          GUI.MaterialEditor( material, skin, mat => newMaterial = mat );
        GUI.Separator();
      }
    }
  }
}
