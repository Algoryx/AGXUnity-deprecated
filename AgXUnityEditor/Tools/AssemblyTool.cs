using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class AssemblyTool : Tool
  {
    private class SelectionEntry
    {
      public GameObject Object { get; private set; }

      public SelectionEntry( GameObject gameObject )
      {
        if ( gameObject == null )
          throw new ArgumentNullException( "Game object is null." );

        Object = gameObject;
      }
    }

    private class RigidBodySelection
    {
      public RigidBody RigidBody { get; private set; }

      public RigidBodySelection( RigidBody rb )
      {
        if ( rb == null ) {
          Debug.LogError( "Rigid body component is null - ignoring selection." );
          return;
        }

        RigidBody = rb;
      }
    }

    private class CreateConstraintData
    {
      public ConstraintType ConstraintType              = ConstraintType.Hinge;
      public string Name                                = string.Empty;
      public ConstraintAttachmentPair AttachmentPair    = null;
      public Constraint.ECollisionsState CollisionState = Constraint.ECollisionsState.DisableRigidBody1VsRigidBody2;

      public void CreateInitialState( string name )
      {
        if ( AttachmentPair != null ) {
          Debug.LogError( "Attachment pair already created. Make sure to clean any previous state before initializing a new one.", AttachmentPair );
          return;
        }

        AttachmentPair = ConstraintAttachmentPair.Create<ConstraintAttachmentPair>();
        Name           = Factory.CreateName( name + "_constraint" );
      }

      public void Reset( bool deleteAttachmentPair )
      {
        if ( AttachmentPair != null && deleteAttachmentPair )
          ScriptComponent.DestroyImmediate( AttachmentPair );
        AttachmentPair = null;
      }
    }

    private enum Mode
    {
      None,
      RigidBody,
      Shape,
      Constraint
    }

    private enum SubMode
    {
      None,
      SelectRigidBody
    }

    private Mode m_mode       = Mode.None;
    private SubMode m_subMode = SubMode.None;

    private CreateConstraintData m_createConstraintData = new CreateConstraintData();

    private List<SelectionEntry> m_selection = new List<SelectionEntry>();
    private RigidBodySelection m_rbSelection = null;

    private Color m_selectedColor     = new Color( 0.15f, 0.9f, 0.25f, 0.15f );
    private Color m_selectedMaxColor  = new Color( 0.8f, 1.0f, 0.95f, 0.25f );
    private Color m_mouseOverColor    = new Color( 0.55f, 0.65f, 0.95f, 0.15f );
    private Color m_mouseOverMaxColor = new Color( 0.15f, 0.25f, 0.25f, 0.15f );

    public Assembly Assembly { get; private set; }

    public AssemblyTool( Assembly assembly )
    {
      Assembly = assembly;
    }

    public override void OnAdd()
    {
      Renderer[] renderers = Assembly.GetComponentsInChildren<Renderer>();
      for ( int i = 0; i < renderers.Length; ++i )
        EditorUtility.SetSelectedWireframeHidden( renderers[ i ], true );
    }

    public override void OnRemove()
    {
      if ( Assembly != null ) {
        Renderer[] renderers = Assembly.GetComponentsInChildren<Renderer>();
        for ( int i = 0; i < renderers.Length; ++i )
          EditorUtility.SetSelectedWireframeHidden( renderers[ i ], false );
      }

      ChangeMode( Mode.None );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      // TODO: This is not responsive.
      if ( Manager.KeyEscapeDown ) {
        ChangeMode( Mode.None );
        EditorUtility.SetDirty( Assembly );
      }

      if ( m_mode == Mode.RigidBody ) {
        if ( Manager.HijackLeftMouseClick() ) {
          Predicate<GameObject> filter = m_subMode == SubMode.None            ? new Predicate<GameObject>( obj => { return obj != null && obj.GetComponent<AgXUnity.Collide.Shape>() == null; } ) :
                                         m_subMode == SubMode.SelectRigidBody ? new Predicate<GameObject>( obj => { return obj != null && obj.GetComponentInParent<RigidBody>() != null; } ) :
                                                                                null;

          Debug.Assert( filter != null );

          var hitResults = Raycast.TestChildren( Assembly.gameObject, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ), 500f, filter );
          if ( hitResults.Count > 0 ) {
            // TODO: If count > 1 - the user should be able to chose which object to select.
            GameObject selected = hitResults[ 0 ].Triangle.Target;
            if ( m_subMode == SubMode.SelectRigidBody ) {
              if ( m_rbSelection != null && m_rbSelection.RigidBody == selected.GetComponentInParent<RigidBody>() )
                m_rbSelection = null;
              else
                m_rbSelection = new RigidBodySelection( selected.GetComponentInParent<RigidBody>() );
            }
            else {
              int selectedIndex = m_selection.FindIndex( entry => { return entry.Object == selected; } );
              // New entry, add it.
              if ( selectedIndex < 0 )
                m_selection.Add( new SelectionEntry( selected ) );
              // Remove selected entry if it already exist.
              else
                m_selection.RemoveAt( selectedIndex );
            }

            EditorUtility.SetDirty( Assembly );
          }
        }
      }
      else if ( m_mode == Mode.Shape ) {
        if ( Manager.HijackLeftMouseClick() ) {
          var hitResults = Raycast.TestChildren( Assembly.gameObject, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) );

          // Find target. Ignoring shapes.
          GameObject selected = null;
          for ( int i = 0; selected == null && i < hitResults.Count; ++i ) {
            if ( hitResults[ i ].Triangle.Target.GetComponent<AgXUnity.Collide.Shape>() == null )
              selected = hitResults[ i ].Triangle.Target;
          }

          m_selection.Clear();
          if ( selected != null )
            m_selection.Add( new SelectionEntry( selected ) );

          EditorUtility.SetDirty( Assembly );
        }
      }
      else if ( m_mode == Mode.Constraint ) {
        if ( m_createConstraintData.AttachmentPair == null ) {
          m_createConstraintData.CreateInitialState( Assembly.name );
          AddChild( new ConstraintAttachmentFrameTool( m_createConstraintData.AttachmentPair, Assembly ) );
        }
      }
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      // TODO: Improvements.
      //   - "Copy-paste" shape.
      //       1. Select object with primitive shape(s)
      //       2. Select object to copy the shape(s) to
      //   - Move from-to existing bodies or create a new body.
      //   - Mesh object operations.
      //       * Simplify assembly
      //       * Multi-select to create meshes
      //   - Inspect element (hold 'i').

      if ( !AgXUnity.Utils.Math.IsUniform( Assembly.transform.lossyScale, 1.0E-3f ) )
        Debug.LogWarning( "Scale of AgXUnity.Assembly transform isn't uniform. If a child rigid body is moving under this transform the (visual) behavior is undefined.", Assembly );

      bool rbButtonPressed         = false;
      bool shapeButtonPressed      = false;
      bool constraintButtonPressed = false;

      GUI.ToolsLabel( skin );
      GUILayout.BeginHorizontal();
      {
        GUILayout.Space( 12 );
        using ( GUI.ToolButtonData.ColorBlock ) {
          rbButtonPressed         = GUILayout.Button( GUI.MakeLabel( "RB", true, "Assembly rigid body tool" ), GUI.ConditionalCreateSelectedStyle( m_mode == Mode.RigidBody, skin.button ), GUILayout.Width( 30f ), GUI.ToolButtonData.Height );
          shapeButtonPressed      = GUILayout.Button( GUI.MakeLabel( "Shape", true, "Assembly shape tool" ), GUI.ConditionalCreateSelectedStyle( m_mode == Mode.Shape, skin.button ), GUILayout.Width( 54f ), GUI.ToolButtonData.Height );
          constraintButtonPressed = GUILayout.Button( GUI.MakeLabel( "Constraint", true, "Assembly constraint tool" ), GUI.ConditionalCreateSelectedStyle( m_mode == Mode.Constraint, skin.button ), GUILayout.Width( 80f ), GUI.ToolButtonData.Height );
        }
      }
      GUILayout.EndHorizontal();

      HandleModeGUI( skin );

      if ( rbButtonPressed )
        ChangeMode( Mode.RigidBody );
      if ( shapeButtonPressed )
        ChangeMode( Mode.Shape );
      if ( constraintButtonPressed )
        ChangeMode( Mode.Constraint );
    }

    private void HandleModeGUI( GUISkin skin )
    {
      if ( m_mode == Mode.RigidBody )
        HandleModeRigidBodyGUI( skin );
      else if ( m_mode == Mode.Shape )
        HandleModeShapeGUI( skin );
      else if ( m_mode == Mode.Constraint )
        HandleModeConstraintGUI( skin );
    }

    private void HandleModeRigidBodyGUI( GUISkin skin )
    {
      GUI.Separator3D();

      using ( GUI.AlignBlock.Center ) {
        if ( m_subMode == SubMode.SelectRigidBody )
          GUILayout.Label( GUI.MakeLabel( "Select rigid body object in scene view.", true ), skin.label );
        else
          GUILayout.Label( GUI.MakeLabel( "Select object(s) in scene view.", true ), skin.label );
      }

      GUI.Separator();

      bool selectionHasRigidBody = m_selection.Find( entry => entry.Object.GetComponentInParent<RigidBody>() != null ) != null;

      bool createNewRigidBodyPressed = false;
      bool addToExistingRigidBodyPressed = false;
      bool moveToNewRigidBodyPressed = false;
      GUILayout.BeginHorizontal();
      {
        GUILayout.Space( 12 );
        UnityEngine.GUI.enabled = m_selection.Count > 0 && !selectionHasRigidBody;
        createNewRigidBodyPressed = GUILayout.Button( GUI.MakeLabel( "Create new", false, "Create new rigid body with selected objects" ), skin.button, GUILayout.Width( 78 ) );
        UnityEngine.GUI.enabled = m_selection.Count > 0 && Assembly.GetComponentInChildren<RigidBody>() != null;
        addToExistingRigidBodyPressed = GUILayout.Button( GUI.MakeLabel( "Add to existing", false, "Add selected objects to existing rigid body" ), GUI.ConditionalCreateSelectedStyle( m_subMode == SubMode.SelectRigidBody, skin.button ), GUILayout.Width( 100 ) );
        UnityEngine.GUI.enabled = selectionHasRigidBody;
        moveToNewRigidBodyPressed = GUILayout.Button( GUI.MakeLabel( "Move to new", false, "Move objects that already contains a rigid body to a new rigid body" ), skin.button, GUILayout.Width( 85 ) );
        UnityEngine.GUI.enabled = true;
      }
      GUILayout.EndHorizontal();

      GUI.Separator3D();

      // Creates new rigid body and move selected objects to it (as children).
      if ( createNewRigidBodyPressed || moveToNewRigidBodyPressed ) {
        CreateOrMoveToRigidBodyFromSelectionEntries( m_selection );
        m_selection.Clear();
      }
      // Toggle to select a rigid body in scene view to move the current selection to.
      else if ( addToExistingRigidBodyPressed ) {
        // This will toggle if sub-mode already is SelectRigidBody.
        ChangeSubMode( SubMode.SelectRigidBody );
      }

      // The user has chosen a rigid body to move the current selection to.
      if ( m_rbSelection != null ) {
        CreateOrMoveToRigidBodyFromSelectionEntries( m_selection, m_rbSelection.RigidBody.gameObject );
        m_selection.Clear();
        ChangeSubMode( SubMode.None );
      }
    }

    private void HandleModeShapeGUI( GUISkin skin )
    {
      GUI.Separator3D();

      using ( GUI.AlignBlock.Center ) {
        GUILayout.Label( GUI.MakeLabel( "Select object(s) in scene view.", true ), skin.label );
      }

      GUI.Separator();

      bool createBoxPressed      = false;
      bool createCylinderPressed = false;
      bool createCapsulePressed  = false;
      bool createSpherePressed   = false;
      bool createMeshPressed     = false;
      GUILayout.BeginHorizontal();
      {
        GUILayout.Space( 12 );
        UnityEngine.GUI.enabled = m_selection.Count > 0;
        using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.red, 0.1f ) ) ) {
          createBoxPressed      = GUILayout.Button( GUI.MakeLabel( "Box", true, "Create new box as parent of the selected object(s)." ),      skin.button, GUILayout.Width( 36 ), GUI.ToolButtonData.Height );
          createCylinderPressed = GUILayout.Button( GUI.MakeLabel( "Cyl", true, "Create new cylinder as parent of the selected object(s)." ), skin.button, GUILayout.Width( 36 ), GUI.ToolButtonData.Height );
          createCapsulePressed  = GUILayout.Button( GUI.MakeLabel( "Cap", true, "Create new capsule as parent of the selected object(s)." ),  skin.button, GUILayout.Width( 36 ), GUI.ToolButtonData.Height );
          createSpherePressed   = GUILayout.Button( GUI.MakeLabel( "Sph", true, "Create new sphere as parent of the selected object(s)." ),   skin.button, GUILayout.Width( 36 ), GUI.ToolButtonData.Height );
          createMeshPressed     = GUILayout.Button( GUI.MakeLabel( "Mes", true, "Create new mesh as parent of the selected object(s)." ),     skin.button, GUILayout.Width( 36 ), GUI.ToolButtonData.Height );
        }
      }
      GUILayout.EndHorizontal();

      // Bounds.Encapsulate!

      if ( createBoxPressed ) {
        CreateShapeFromSelection<AgXUnity.Collide.Box>( m_selection, ( box, data ) =>
        {
          box.HalfExtents = data.LocalExtents;
          data.SetDefaultPositionRotation( box.gameObject );
        } );
      }
      if ( createCylinderPressed ) {
        CreateShapeFromSelection<AgXUnity.Collide.Cylinder>( m_selection, ( cylinder, data ) =>
        {
          // Height is the longest extent.
          cylinder.Height = 2f * data.LocalExtents.MaxValue();
          // Radius is the middle (second longest) extent.
          cylinder.Radius = data.LocalExtents.MiddleValue();

          // Axis along "height" = longest extent (max index).
          Vector3 axis = Vector3.zero;
          axis[ data.LocalExtents.MaxIndex() ] = 1f;

          cylinder.transform.position = data.WorldCenter;
          cylinder.transform.rotation = data.Rotation * Quaternion.FromToRotation( Vector3.up, axis ).Normalize();
        } );
      }
      if ( createCapsulePressed ) {
        CreateShapeFromSelection<AgXUnity.Collide.Capsule>( m_selection, ( capsule, data ) =>
        {
          // Height is the longest extent.
          capsule.Height = 2f * data.LocalExtents.MaxValue();
          // Radius is the middle (second longest) extent.
          capsule.Radius = data.LocalExtents.MiddleValue();

          // Axis along "height" = longest extent (max index).
          Vector3 axis = Vector3.zero;
          axis[ data.LocalExtents.MaxIndex() ] = 1f;

          capsule.transform.position = data.WorldCenter;
          capsule.transform.rotation = data.Rotation * Quaternion.FromToRotation( Vector3.up, axis ).Normalize();
        } );
      }
      if ( createSpherePressed ) {
        CreateShapeFromSelection<AgXUnity.Collide.Sphere>( m_selection, ( sphere, data ) =>
        {
          sphere.Radius = data.LocalExtents.magnitude;
          data.SetDefaultPositionRotation( sphere.gameObject );
        } );
      }
      if ( createMeshPressed ) {
        CreateShapeFromSelection<AgXUnity.Collide.Mesh>( m_selection, ( mesh, data ) =>
        {
          mesh.SourceObject = data.Filter.sharedMesh;
          // We don't want to set the position given the center of the bounds
          // since we're one-to-one with the mesh filter.
          mesh.transform.position = data.Filter.transform.position;
          mesh.transform.rotation = data.Filter.transform.rotation;
        } );
      }

      GUI.Separator3D();
    }

    private void HandleModeConstraintGUI( GUISkin skin )
    {
      ConstraintAttachmentFrameTool attachmentPairTool = GetChild<ConstraintAttachmentFrameTool>();
      if ( attachmentPairTool == null || m_createConstraintData.AttachmentPair == null )
        return;

      GUI.Separator3D();

      using ( new GUI.Indent( 16 ) ) {
        GUILayout.BeginHorizontal();
        {
          GUILayout.Label( GUI.MakeLabel( "Name", true ), skin.label, GUILayout.Width( 64 ) );
          m_createConstraintData.Name = GUILayout.TextField( m_createConstraintData.Name, skin.textField, GUILayout.ExpandWidth( true ) );
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
          GUILayout.Label( GUI.MakeLabel( "Type", true ), skin.label, GUILayout.Width( 64 ) );
          using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.yellow, 0.1f ) ) )
            m_createConstraintData.ConstraintType = (ConstraintType)EditorGUILayout.EnumPopup( m_createConstraintData.ConstraintType, skin.button, GUILayout.ExpandWidth( true ), GUILayout.Height( 18 ) );
        }
        GUILayout.EndHorizontal();
      }

      GUI.Separator3D();

      attachmentPairTool.OnPreTargetMembersGUI( skin );

      m_createConstraintData.CollisionState = ConstraintTool.ConstraintCollisionsStateGUI( m_createConstraintData.CollisionState, skin );

      GUI.Separator3D();

      bool createConstraintPressed = false;
      bool cancelPressed = false;
      GUILayout.BeginHorizontal();
      {
        GUILayout.Space( 12 );

        using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.green, 0.1f ) ) )
          createConstraintPressed = GUILayout.Button( GUI.MakeLabel( "Create", true, "Create the constraint" ), skin.button, GUILayout.Width( 120 ), GUILayout.Height( 26 ) );

        GUILayout.BeginVertical();
        {
          GUILayout.Space( 13 );
          using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.red, 0.1f ) ) )
            cancelPressed = GUILayout.Button( GUI.MakeLabel( "Cancel", false ), skin.button, GUILayout.Width( 96 ), GUILayout.Height( 16 ) );
          GUILayout.EndVertical();
        }
      }
      GUILayout.EndHorizontal();

      GUI.Separator3D();

      if ( createConstraintPressed ) {
        GameObject constraintGameObject                                 = Factory.Create( m_createConstraintData.ConstraintType, m_createConstraintData.AttachmentPair );
        constraintGameObject.name                                       = m_createConstraintData.Name;
        constraintGameObject.GetComponent<Constraint>().CollisionsState = m_createConstraintData.CollisionState;

        constraintGameObject.transform.SetParent( Assembly.transform );

        Undo.RegisterCreatedObjectUndo( constraintGameObject, "New assembly constraint '" + constraintGameObject.name + "' created" );

        m_createConstraintData.Reset( false );
      }

      if ( cancelPressed || createConstraintPressed )
        ChangeMode( Mode.None );
    }

    private void CreateOrMoveToRigidBodyFromSelectionEntries( List<SelectionEntry> selectionEntries, GameObject rbGameObject = null )
    {
      if ( rbGameObject != null && rbGameObject.GetComponent<RigidBody>() == null ) {
        Debug.LogError( "Mandatory AgXUnity.RigidBody component not present in game object. Ignoring 'move to'.", rbGameObject );
        return;
      }

      foreach ( var selection in selectionEntries ) {
        if ( selection.Object == null ) {
          Debug.LogError( "Unable to create rigid body - selection contains null object(s)." );
          return;
        }
      }

      if ( rbGameObject == null ) {
        rbGameObject                    = Factory.Create<RigidBody>();
        rbGameObject.transform.position = Assembly.transform.position;
        rbGameObject.transform.rotation = Assembly.transform.rotation;
        rbGameObject.transform.parent   = Assembly.transform;

        Undo.RegisterCreatedObjectUndo( rbGameObject, "New assembly rigid body" );
      }

      foreach ( var entry in selectionEntries ) {
        // Collecting selected objects, non selected children, to be moved to
        // a new parent.
        List<Transform> orphans = new List<Transform>();
        foreach ( Transform child in entry.Object.transform ) {
          // Do not add shapes to our orphans since they've PROBABLY/HOPEFULLY
          // been created earlier by this tool. This implicit state probably has
          // to be revised.
          bool inSelectedList = child.GetComponent<AgXUnity.Collide.Shape>() != null || selectionEntries.FindIndex( selectedEntry => { return selectedEntry.Object == child.gameObject; } ) >= 0;
          if ( !inSelectedList )
            orphans.Add( child );
        }

        // Moving selected parents (NON-selected) children to a new parent.
        Transform parent = entry.Object.transform.parent;
        foreach ( var orphan in orphans )
          Undo.SetTransformParent( orphan, parent, "Moving non-selected child to selected parent" );

        Undo.SetTransformParent( entry.Object.transform, rbGameObject.transform, "Parent of mesh is rigid body" );
      }
    }

    struct ShapeInitializationData
    {
      public Bounds LocalBounds;
      public Vector3 LocalExtents;
      public Vector3 WorldCenter;
      public Quaternion Rotation;
      public MeshFilter Filter;

      public void SetDefaultPositionRotation( GameObject gameObject )
      {
        gameObject.transform.position = WorldCenter;
        gameObject.transform.rotation = Rotation;
      }
    }

    GameObject CreateShapeFromSelection<T>( List<SelectionEntry> selectionEntries, Action<T, ShapeInitializationData> initializeAction ) where T : AgXUnity.Collide.Shape
    {
      Debug.Assert( selectionEntries != null && selectionEntries.Count == 1 );

      MeshFilter filter = selectionEntries[ 0 ].Object.GetComponent<MeshFilter>();

      Debug.Assert( filter != null );
      Debug.Assert( initializeAction != null );

      GameObject shapeGameObject = Factory.Create<T>();

      Bounds localBounds = filter.sharedMesh.bounds;
      initializeAction( shapeGameObject.GetComponent<T>(),
                        new ShapeInitializationData()
                        {
                          LocalBounds  = localBounds,
                          LocalExtents = filter.transform.InverseTransformDirection( filter.transform.TransformVector( localBounds.extents ) ),
                          WorldCenter  = filter.transform.TransformPoint( localBounds.center ),
                          Rotation     = filter.transform.rotation,
                          Filter       = filter
                        } );

      Undo.RegisterCreatedObjectUndo( shapeGameObject, "New game object with shape component" );
      if ( AgXUnity.Rendering.DebugRenderManager.HasInstance )
        Undo.AddComponent<AgXUnity.Rendering.ShapeDebugRenderData>( shapeGameObject );

      Undo.SetTransformParent( shapeGameObject.transform, filter.transform, "Shape as child to visual" );

      // SetTransformParent assigns some scale given the parent. We're in general not
      // interested in this scale since it will "un-scale" meshes (and the rest of the
      // shapes doesn't support scale so...).
      shapeGameObject.transform.localScale = Vector3.one;

      return shapeGameObject;
    }

    private void ChangeMode( Mode mode )
    {
      // Assembly reference may be lost here when called from OnRemove.

      // Toggle mode.
      if ( mode == m_mode )
        mode = Mode.None;

      m_selection.Clear();
      RemoveAllChildren();

      m_createConstraintData.Reset( true );

      m_mode = mode;
      m_subMode = SubMode.None;
    }

    private void ChangeSubMode( SubMode subMode )
    {
      // Toggle sub-mode.
      if ( subMode == m_subMode )
        subMode = SubMode.None;

      m_rbSelection = null;
      m_subMode = subMode;
    }

    public bool HasActiveSelections()
    {
      return m_selection.Count > 0 || m_rbSelection != null;
    }

    public void OnRenderGizmos( Utils.ObjectsGizmoColorHandler colorHandler )
    {
      if ( Assembly == null )
        return;

      RigidBody[] bodies = Assembly.GetComponentsInChildren<RigidBody>();
      foreach ( var rb in bodies ) {
        colorHandler.Colorize( rb );

        // Mesh filters are not colorized by default - give the color (similar/same as body).
        // NOTE: Shapes debug rendering are not included in these mesh filters.
        colorHandler.ColorizeMeshFilters( rb );
      }

      foreach ( var selected in m_selection ) {
        MeshFilter filter = selected.Object.GetComponent<MeshFilter>();
        colorHandler.Highlight( filter, Utils.ObjectsGizmoColorHandler.SelectionType.VaryingIntensity );
      }

      if ( m_rbSelection != null )
        colorHandler.Highlight( m_rbSelection.RigidBody, Utils.ObjectsGizmoColorHandler.SelectionType.VaryingIntensity );
    }
  }
}
