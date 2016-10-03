using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Utils
{
  public class VisualPrimitive
  {
    public class MoveToAction
    {
      public Vector3 TargetPosition  = Vector3.zero;
      public Vector3 CurrentVelocity = Vector3.zero;
      public float ApproxTime        = 1.0f;
    }

    private bool m_visible = false;
    private AgXUnity.Rendering.Spawner.Primitive m_primitiveType = AgXUnity.Rendering.Spawner.Primitive.Cylinder;
    private string m_shaderName = "";

    public GameObject Node { get; private set; }

    public bool Visible
    {
      get { return Node != null && m_visible; }
      set
      {
        bool newStateIsVisible = value;
        // First time to visualize this primitive - load and report to Manager.
        if ( newStateIsVisible && Node == null ) {
          Node = CreateNode();
          UpdateColor();
          Manager.OnVisualPrimitiveNodeCreate( this );
        }
        
        if ( Node != null ) {
          m_visible = newStateIsVisible;
          Node.SetActive( m_visible );
        }

        if ( !newStateIsVisible )
          CurrentMoveToAction = null;
      }
    }

    private Color m_color = Color.yellow;
    public Color Color
    {
      get { return m_color; }
      set
      {
        m_color = value;
        UpdateColor();
      }
    }

    private Color m_mouseOverColor = Color.green;
    public Color MouseOverColor
    {
      get { return m_mouseOverColor; }
      set
      {
        m_mouseOverColor = value;
        UpdateColor();
      }
    }

    private bool m_mouseOverNode = false;
    public bool MouseOver
    {
      get { return m_mouseOverNode; }
      set
      {
        if ( m_mouseOverNode == value )
          return;

        m_mouseOverNode = value;
        UpdateColor();
      }
    }

    private bool m_pickable = true;
    public bool Pickable
    {
      get { return m_pickable; }
      set
      {
        m_pickable = value;
        if ( !m_pickable )
          MouseOver = false;
      }
    }

    public MoveToAction CurrentMoveToAction { get; set; }

    public System.Action<AgXUnity.Utils.Raycast.Hit, VisualPrimitive> OnMouseClick = delegate {};

    public void Destruct()
    {
      Manager.OnVisualPrimitiveNodeDestruct( this );
    }

    AgXUnity.Utils.Raycast.TriangleHit Raycast( Ray ray, float rayLength = 500.0f )
    {
      var result = AgXUnity.Utils.MeshUtils.FindClosestTriangle( Node, ray, rayLength );
      if ( result.Valid )
        result.Target = Node;
      return result;
    }

    public virtual void OnSceneView( SceneView sceneView )
    {
      if ( CurrentMoveToAction != null && Visible ) {
        Node.transform.position = Vector3.SmoothDamp( Node.transform.position, CurrentMoveToAction.TargetPosition, ref CurrentMoveToAction.CurrentVelocity, CurrentMoveToAction.ApproxTime );
        if ( Vector3.Distance( Node.transform.position, CurrentMoveToAction.TargetPosition ) < 1.0E-3f )
          CurrentMoveToAction = null;
      }
    }

    protected VisualPrimitive( AgXUnity.Rendering.Spawner.Primitive primitiveType, string shader = "Unlit/Color" )
    {
      m_primitiveType = primitiveType;
      m_shaderName = shader;
    }

    private GameObject CreateNode()
    {
      string name = ( GetType().Namespace != null ? GetType().Namespace : "" ) + "." + GetType().Name;
      return AgXUnity.Rendering.Spawner.Create( m_primitiveType, name, HideFlags.HideAndDontSave, m_shaderName );
    }

    private void UpdateColor()
    {
      AgXUnity.Rendering.Spawner.Utils.SetColor( Node, m_mouseOverNode ? MouseOverColor : Color );
    }
  }

  public class VisualPrimitiveCylinder : VisualPrimitive
  {
    public void SetTransform( Vector3 start, Vector3 end, float radius, bool constantScreenSize = true )
    {
      AgXUnity.Rendering.Spawner.Utils.SetCylinderTransform( Node, start, end, radius, constantScreenSize );
    }

    public VisualPrimitiveCylinder( string shader = "Unlit/Color" )
      : base( AgXUnity.Rendering.Spawner.Primitive.Cylinder, shader )
    {
    }
  }

  public class VisualPrimitiveSphere : VisualPrimitive
  {
    public void SetTransform( Vector3 position, Quaternion rotation, float radius, bool constantScreenSize = true, float minRadius = 0f, float maxRadius = float.MaxValue )
    {
      AgXUnity.Rendering.Spawner.Utils.SetSphereTransform( Node, position, rotation, radius, constantScreenSize, minRadius, maxRadius );
    }

    public VisualPrimitiveSphere( string shader = "Unlit/Color" )
      : base( AgXUnity.Rendering.Spawner.Primitive.Sphere, shader )
    {
    }
  }

  public class VisualPrimitivePlane : VisualPrimitive
  {
    public void SetTransform( Vector3 position, Quaternion rotation, Vector2 size )
    {
      if ( Node == null )
        return;

      Node.transform.localScale = new Vector3( size.x, 1.0f, size.y );
      Node.transform.position = position;
      Node.transform.rotation = rotation;
    }

    public void MoveDistanceAlongNormal( float distance, float approxTime )
    {
      if ( Node == null )
        return;

      Vector3 currentPosition = CurrentMoveToAction != null ? CurrentMoveToAction.TargetPosition : Node.transform.position;

      Vector3 newTarget = currentPosition + distance * ( Node.transform.rotation * Vector3.up ).normalized;
      if ( CurrentMoveToAction != null )
        CurrentMoveToAction.TargetPosition = newTarget;
      else
        CurrentMoveToAction = new MoveToAction() { TargetPosition = newTarget, ApproxTime = approxTime };
    }

    public VisualPrimitivePlane( string shader = "Unlit/Color" )
      : base( AgXUnity.Rendering.Spawner.Primitive.Plane, shader )
    {
    }
  }

  public class VisualPrimitiveArrow : VisualPrimitive
  {
    public void SetTransform( Vector3 position, Quaternion rotation, Vector3 scale, bool constantScreenSize = true )
    {
      // TODO: Mouse over isn't working. It only highlights cylinder or top?

      if ( Node == null )
        return;

      Node.transform.localScale = AgXUnity.Rendering.Spawner.Utils.ConditionalConstantScreenSize( constantScreenSize, scale, position );
      Node.transform.position   = position;
      Node.transform.rotation   = rotation;
    }

    public VisualPrimitiveArrow( string shader = "Unlit/Color" )
      : base( AgXUnity.Rendering.Spawner.Primitive.Constraint, shader )
    {
    }
  }
}
