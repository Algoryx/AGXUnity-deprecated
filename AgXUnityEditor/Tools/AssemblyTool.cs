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
    private class ColorPulse
    {
      private static float m_t = 0f;
      private static float m_d = 1f;

      public static void Update( float dt )
      {
        m_t += m_d * dt;
        if ( m_t >= 1f ) {
          m_t =  1f;
          m_d = -1f;
        }
        else if ( m_t <= 0f ) {
          m_t = 0f;
          m_d = 1f;
        }
      }

      public static Color Lerp( Color baseColor, Color maxColor )
      {
        return Color.Lerp( baseColor, maxColor, m_t );
      }
    }

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

    private Mode m_mode = Mode.None;
    private SubMode m_subMode = SubMode.None;

    private List<SelectionEntry> m_selection = new List<SelectionEntry>();
    private RigidBodySelection m_rbSelection = null;

    private Color m_selectedColor            = new Color( 0.15f, 0.9f, 0.25f, 0.15f );
    private Color m_selectedMaxColor         = new Color( 0.8f, 1.0f, 0.95f, 0.25f );
    private Color m_mouseOverColor           = new Color( 0.55f, 0.65f, 0.95f, 0.15f );
    private Color m_mouseOverMaxColor        = new Color( 0.15f, 0.25f, 0.25f, 0.15f );

    public Assembly Assembly { get; private set; }

    public AssemblyTool( Assembly assembly )
    {
      Assembly = assembly;
      Utils.DrawGizmoCallbackHandler.Register( this, component => { return component == Assembly; } );
    }

    public override void OnAdd()
    {
    }

    public override void OnRemove()
    {
      ChangeMode( Mode.None );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( m_mode != Mode.None )
        ColorPulse.Update( 0.0045f );

      if ( m_mode == Mode.RigidBody ) {
        if ( Manager.HijackLeftMouseClick() ) {
          Predicate<GameObject> filter = m_subMode == SubMode.None            ? new Predicate<GameObject>( obj => { return obj != null && obj.GetComponent<AgXUnity.Collide.Shape>() == null && obj.GetComponentInParent<RigidBody>() == null; } ) :
                                         m_subMode == SubMode.SelectRigidBody ? new Predicate<GameObject>( obj => { return obj != null && obj.GetComponentInParent<RigidBody>() != null; } ) :
                                                                                null;

          Debug.Assert( filter != null );

          var hitResults = RaycastAll( filter );
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
          var hitResults = RaycastAll();

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
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
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

      using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) ) {
        if ( m_subMode == SubMode.SelectRigidBody )
          GUILayout.Label( GUI.MakeLabel( "Select rigid body object in scene view.", true ), skin.label );
        else
          GUILayout.Label( GUI.MakeLabel( "Select object(s) in scene view.", true ), skin.label );
      }

      GUI.Separator();

      bool createNewRigidBodyPressed = false;
      bool addToExistingRigidBodyPressed = false;
      GUILayout.BeginHorizontal();
      {
        GUILayout.Space( 12 );
        UnityEngine.GUI.enabled = m_selection.Count > 0;
        createNewRigidBodyPressed = GUILayout.Button( GUI.MakeLabel( "Create new", false, "Create new rigid body with selected objects" ), skin.button, GUILayout.Width( 78 ) );
        UnityEngine.GUI.enabled = m_selection.Count > 0 && Assembly.GetComponentInChildren<RigidBody>() != null;
        addToExistingRigidBodyPressed = GUILayout.Button( GUI.MakeLabel( "Add to existing", false, "Add selected objects to existing rigid body" ), GUI.ConditionalCreateSelectedStyle( m_subMode == SubMode.SelectRigidBody, skin.button ), GUILayout.Width( 100 ) );
        UnityEngine.GUI.enabled = true;
      }
      GUILayout.EndHorizontal();

      GUI.Separator3D();

      // Creates new rigid body and move selected objects to it (as children).
      if ( createNewRigidBodyPressed ) {
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

      using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) ) {
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
        else if ( selection.Object.GetComponentInParent<RigidBody>() != null ) {
          Debug.LogError( "Unable to create rigid body - selected object already part of a rigid body.", selection.Object );
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
      // Toggle mode.
      if ( mode == m_mode )
        mode = Mode.None;

      m_selection.Clear();

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

    private List<AgXUnity.Utils.Raycast.Hit> RaycastAll( Predicate<GameObject> pred = null, float rayLength = 500.0f )
    {
      List<AgXUnity.Utils.Raycast.Hit> result = new List<AgXUnity.Utils.Raycast.Hit>();
      Ray ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );

      Traverse( obj =>
      {
        if ( pred == null || pred( obj ) ) {
          AgXUnity.Utils.Raycast.Hit hit = AgXUnity.Utils.Raycast.Test( obj, ray, rayLength );
          if ( hit.Triangle.Valid )
            result.Add( hit );
        }
      } );

      result.Sort( ( hit1, hit2 ) => { return hit1.Triangle.Distance < hit2.Triangle.Distance ? -1 : 1; } );

      return result;
    }

    private void Traverse( Action<GameObject> gameObjectVisitor )
    {
      if ( gameObjectVisitor == null )
        return;

      Traverse( Assembly.transform, gameObjectVisitor );
    }

    private void Traverse( Transform transform, Action<GameObject> gameObjectVisitor )
    {
      if ( transform == null )
        return;

      gameObjectVisitor( transform.gameObject );

      foreach ( Transform child in transform )
        Traverse( child, gameObjectVisitor );
    }

    public override void OnDrawGizmosSelected( ScriptComponent component )
    {
      Dictionary<MeshFilter, Color> meshColors = new Dictionary<MeshFilter, Color>();

      RigidBody[] bodies = Assembly.GetComponentsInChildren<RigidBody>();
      int orgSeed = UnityEngine.Random.seed;
      {
        foreach ( var rb in bodies ) {
          Color rbColor = UnityEngine.Random.ColorHSV();

          MeshFilter[] rbMeshFilters = rb.GetComponentsInChildren<MeshFilter>();
          foreach ( var rbMeshFilter in rbMeshFilters ) {
            bool inSelected = ( m_rbSelection != null && m_rbSelection.RigidBody == rb ) ||
                              m_selection.FindIndex( selectedEntry => { return selectedEntry.Object.GetComponent<MeshFilter>() == rbMeshFilter; } ) >= 0;
            if ( inSelected )
              meshColors.Add( rbMeshFilter, ColorPulse.Lerp( rbColor, m_selectedColor ) );
            else
              meshColors.Add( rbMeshFilter, rbColor );
          }
        }
      }
      UnityEngine.Random.seed = orgSeed;

      foreach ( var selected in m_selection ) {
        MeshFilter selectedFilter = selected.Object.GetComponent<MeshFilter>();
        Color color;
        if ( !meshColors.TryGetValue( selectedFilter, out color ) )
          meshColors.Add( selectedFilter, ColorPulse.Lerp( m_selectedColor, m_selectedMaxColor ) );
      }

      foreach ( var filterAndColor in meshColors )
        DrawGizmoMesh( filterAndColor.Key, filterAndColor.Value );

      //bool renderMouseOverMesh = m_mode != Mode.None &&
      //                           Manager.MouseOverObject != null &&
      //                           Manager.MouseOverObject.transform.IsChildOf( Assembly.transform ) &&
      //                           m_selection.FindIndex( obj => { return obj.Object == Manager.MouseOverObject; } ) < 0;
      //if ( renderMouseOverMesh )
      //  DrawGizmoMesh( Manager.MouseOverObject.GetComponent<MeshFilter>(), ColorPulse.Lerp( m_mouseOverColor, m_mouseOverMaxColor ) );

      //foreach ( var selected in m_selection )
      //  DrawGizmoMesh( selected.Object.GetComponent<MeshFilter>(), ColorPulse.Lerp( m_selectedColor, m_selectedMaxColor ) );
    }

    private void DrawGizmoMesh( MeshFilter filter, Color color )
    {
      if ( filter == null )
        return;

      Gizmos.color = color;
      Gizmos.matrix = filter.transform.localToWorldMatrix;
      Gizmos.DrawWireMesh( filter.sharedMesh );
    }
  }

  public class AssemblyToolOld : Tool
  {
    public enum CreateModes
    {
      None,
      RigidBody,
      Shape,
      Constraint
    }

    public Assembly Assembly { get; private set; }

    private CreateModes m_createMode = CreateModes.None;
    public CreateModes CreateMode
    {
      get { return m_createMode; }
      set
      {
        if ( m_createMode == value )
          return;

        m_createMode = value;
        m_selected.Clear();
      }
    }

    private bool m_toolsActive = false;
    public bool ToolsActive
    {
      get { return m_toolsActive; }
      set
      {
        if ( m_toolsActive == value )
          return;

        m_toolsActive = value;
        CreateMode = CreateModes.None;
      }
    }

    private List<GameObject> m_selected = new List<GameObject>();
    public GameObject[] Selected { get { return m_selected.ToArray(); } }

    public AssemblyToolOld( Assembly assembly )
    {
      Assembly = assembly;
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( ToolsActive ) {
        // When the tools are active we're hijacking all mouse clicks.
        bool hijackedMouseClick = Manager.HijackLeftMouseClick();
        // Mouse hovers an object part of this assembly.
        bool mouseHoverAssemblyObject = Manager.MouseOverObject != null &&
                                        Manager.MouseOverObject.transform.IsChildOf( Assembly.transform );

        if ( hijackedMouseClick &&
             mouseHoverAssemblyObject &&
             Manager.MouseOverObject.GetComponentInParent<RigidBody>() == null ) {
          if ( m_selected.Contains( Manager.MouseOverObject ) )
            m_selected.Remove( Manager.MouseOverObject );
          else
            m_selected.Add( Manager.MouseOverObject );

          EditorUtility.SetDirty( Assembly );
        }
      }

      //if ( SelectObjects ) {
      //  if ( Manager.HijackLeftMouseClick() &&
      //       Manager.MouseOverObject != null &&
      //       Manager.MouseOverObject.transform.IsChildOf( Assembly.transform ) ) {
      //    if ( m_selected.Contains( Manager.MouseOverObject ) )
      //      m_selected.Remove( Manager.MouseOverObject );
      //    else
      //      m_selected.Add( Manager.MouseOverObject );

      //    EditorUtility.SetDirty( Assembly );
      //  }
      //}

      //if ( Manager.KeyEscapeDown ) {
      //  m_selected.Clear();
      //}

      //if ( Manager.HijackLeftMouseClick() &&
      //     Manager.MouseOverObject != null &&
      //     Manager.MouseOverObject.GetComponentInParent<RigidBody>() == null &&
      //     Manager.MouseOverObject.transform.IsChildOf( Assembly.transform ) ) {
      //  if ( m_selected.Contains( Manager.MouseOverObject ) )
      //    m_selected.Remove( Manager.MouseOverObject );
      //  else
      //    m_selected.Add( Manager.MouseOverObject );

      //  EditorUtility.SetDirty( Assembly );
      //}
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      bool createPressed = false;

      GUI.Separator3D();
      {
        ToolsActive = GUI.Toggle( GUI.MakeLabel( ToolsActive ? "Deactivate tools" : "Activate tools" ), ToolsActive, skin.button, skin.label );
        UnityEngine.GUI.enabled = ToolsActive;
        GUILayout.BeginHorizontal();
        {
          GUILayout.Space( 12 );
          GUI.ToolsLabel( skin );
          using ( GUI.ToolButtonData.ColorBlock ) {
            foreach ( CreateModes createMode in Enum.GetValues( typeof( CreateModes ) ) ) {
              if ( createMode == CreateModes.None )
                continue;

              ToggleCreateMode( createMode,
                                GUILayout.Button( MakeLabel( createMode ), GUI.ConditionalCreateSelectedStyle( CreateMode == createMode, skin.button ), GUI.ToolButtonData.Width, GUI.ToolButtonData.Height ) );
            }
          }
        }
        GUILayout.EndHorizontal();
        UnityEngine.GUI.enabled = true;

        if ( CreateMode != CreateModes.None ) {
          GUI.Separator();
          using ( new GUI.Indent( 12 ) ) {
            using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) ) {
              GUILayout.Label( GUI.MakeLabel( "Select/pick object(s) in scene view", true ) );
              GUI.Separator();
            }

            using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) ) {
              UnityEngine.GUI.enabled = m_selected.Count > 0;
              createPressed = GUILayout.Button( GUI.MakeLabel( "Create" ), skin.button, GUILayout.Width( 92 ) );
              UnityEngine.GUI.enabled = true;
            }
          }
        }
      }
      GUI.Separator3D();

      if ( createPressed ) {
        GameObject rb = null;
        if ( CreateMode == CreateModes.RigidBody ) {
          rb                    = Factory.Create<RigidBody>();
          rb.transform.position = Assembly.transform.position;
          rb.transform.rotation = Assembly.transform.rotation;
          rb.transform.parent   = Assembly.transform;

          Undo.RegisterCreatedObjectUndo( rb, "New assembly rigid body." );
        }

        // Entering this mode if CreateMode == RigidBody as well.
        if ( CreateMode == CreateModes.Shape || rb != null ) {
          foreach ( GameObject go in m_selected ) {
            // Collecting selected objects, non selected children, to be moved to
            // a new parent.
            List<Transform> orphans = new List<Transform>();
            foreach ( Transform child in go.transform ) {
              if ( !m_selected.Contains( child.gameObject ) )
                orphans.Add( child );
            }

            // Moving selected parents (NON-selected) children to a new parent.
            Transform parent = go.transform.parent;
            foreach ( var orphan in orphans )
              Undo.SetTransformParent( orphan, parent, "Moving non-selected child to selected parent." );

            // DebugRenderManager may not create ShapeDebugRenderData component on
            // next update since it'll screw up undo. We have to create it here.
            if ( go.GetComponent<AgXUnity.Collide.Shape>() == null && go.GetComponent<MeshFilter>() != null ) {
              var mesh = Undo.AddComponent<AgXUnity.Collide.Mesh>( go );
              if ( AgXUnity.Rendering.DebugRenderManager.HasInstance )
                Undo.AddComponent<AgXUnity.Rendering.ShapeDebugRenderData>( go );
              mesh.SourceObject = go.GetComponent<MeshFilter>().sharedMesh;
            }

            if ( rb != null )
              Undo.SetTransformParent( go.transform, rb.transform, "Parent of mesh is rigid body." );
          }
        }

        CreateMode = CreateModes.None;
      }

      //bool toggleSelectObjects = false;

      //GUI.Separator3D();
      //using ( new GUI.AlignBlock( GUI.AlignBlock.Alignment.Center ) )
      //  using ( GUI.ToolButtonData.ColorBlock )
      //    toggleSelectObjects = GUILayout.Button( GUI.MakeLabel( "Select in scene view" ), GUI.ConditionalCreateSelectedStyle( SelectObjects, skin.button ), GUILayout.Width( 128 ) );
      //GUI.Separator3D();

      //if ( toggleSelectObjects )
      //  SelectObjects = !SelectObjects;

      //UnityEngine.GUI.enabled  = m_selected.Count > 0;
      //bool createBody          = GUILayout.Button( GUI.MakeLabel( "Create body" ), skin.button, GUILayout.Width( 200 ) );
      //bool createShapes        = GUILayout.Button( GUI.MakeLabel( "Create shapes" ), skin.button, GUILayout.Width( 200 ) );
      //UnityEngine.GUI.enabled  = true;
      //bool createLastOfTheRest = GUILayout.Button( GUI.MakeLabel( "Create last of the rest" ), skin.button, GUILayout.Width( 200 ) );

      //if ( createLastOfTheRest ) {
      //  m_selected.Clear();

      //  Transform[] allTransforms = Assembly.GetComponentsInChildren<Transform>();
      //  foreach ( var t in allTransforms ) {
      //    if ( t.gameObject == Assembly.gameObject )
      //      continue;

      //    if ( t.GetComponentInParent<RigidBody>() == null )
      //      m_selected.Add( t.gameObject );
      //  }

      //  createBody = true;
      //}

      //GameObject rb = null;
      //if ( createBody ) {
      //  rb                    = Factory.Create<RigidBody>();
      //  rb.transform.position = Assembly.transform.position;
      //  rb.transform.rotation = Assembly.transform.rotation;
      //  rb.transform.parent   = Assembly.transform;
      //  Undo.RegisterCreatedObjectUndo( rb, "New assembly rigid body." );

      //  createShapes = true;
      //}

      //if ( createShapes ) {
      //  foreach ( GameObject go in m_selected ) {
      //    List<Transform> orphans = new List<Transform>();
      //    foreach ( Transform child in go.transform ) {
      //      if ( !m_selected.Contains( child.gameObject ) )
      //        orphans.Add( child );
      //    }

      //    Transform parent = go.transform.parent;
      //    foreach ( var orphan in orphans )
      //      Undo.SetTransformParent( orphan, parent, "Moving non-selected child to selected parent." );

      //    if ( go.GetComponent<AgXUnity.Collide.Shape>() == null && go.GetComponent<MeshFilter>() != null ) {
      //      var mesh = Undo.AddComponent<AgXUnity.Collide.Mesh>( go );
      //      if ( AgXUnity.Rendering.DebugRenderManager.HasInstance )
      //        Undo.AddComponent<AgXUnity.Rendering.ShapeDebugRenderData>( go );
      //      mesh.SourceObject = go.GetComponent<MeshFilter>().sharedMesh;
      //    }

      //    if ( rb != null )
      //      Undo.SetTransformParent( go.transform, rb.transform, "Parent of mesh is rigid body." );
      //  }

      //  m_selected.Clear();
      //}
    }

    private GUIContent MakeLabel( CreateModes createMode )
    {
      return GUI.MakeLabel( createMode.ToString().ToLower()[ 0 ].ToString(), true );
    }

    private void ToggleCreateMode( CreateModes createMode, bool toggled )
    {
      if ( !toggled )
        return;

      if ( createMode == CreateMode )
        CreateMode = CreateModes.None;
      else
        CreateMode = createMode;
    }
  }

  public class RenderSelectedGizmoDrawerOld
  {
    private static float m_t   = 0f;
    private static float m_dir = 1.0f;

    [DrawGizmo( GizmoType.Active | GizmoType.Selected )]
    public static void DrawSelectedList( Assembly assembly, GizmoType gizmoType )
    {
      AssemblyToolOld tool = Tool.GetActiveTool<AssemblyToolOld>();
      if ( tool == null )
        return;

      Dictionary<Transform, Color> colors = new Dictionary<Transform, Color>();

      GameObject[] selected = tool.Selected;
      Color selectedColor   = Color.Lerp( Color.green, Color.white, m_t );
      foreach ( var go in selected ) {
        MeshFilter filter = go.GetComponent<MeshFilter>();
        if ( filter == null )
          continue;

        colors.Add( go.transform, selectedColor );
      }

      RigidBody[] bodies      = tool.Assembly.GetComponentsInChildren<RigidBody>();
      UnityEngine.Random.seed = 513;
      foreach ( var rb in bodies ) {
        Color rbColor = UnityEngine.Random.ColorHSV();
        Transform[] transforms = rb.GetComponentsInChildren<Transform>();
        foreach ( Transform transform in transforms )
          if ( !colors.ContainsKey( transform ) && transform.GetComponent<MeshFilter>() != null )
            colors.Add( transform, rbColor );
      }

      Transform[] allTransforms = tool.Assembly.GetComponentsInChildren<Transform>();
      foreach ( Transform transform in allTransforms ) {
        MeshFilter filter = transform.GetComponent<MeshFilter>();
        if ( filter == null )
          continue;

        Color color;
        if ( !colors.TryGetValue( transform, out color ) ) {
          color = Color.white;
          color.a = 0.1f;
        }

        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireMesh( filter.sharedMesh );
      }

      m_t += m_dir * 0.015f;
      if ( m_t > 1f ) {
        m_t   = 1f;
        m_dir = -1f;
      }
      else if ( m_t < 0f ) {
        m_t   = 0f;
        m_dir = 1f;
      }
    }
  }
}
