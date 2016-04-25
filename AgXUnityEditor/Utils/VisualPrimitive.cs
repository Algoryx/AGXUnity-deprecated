﻿using UnityEngine;
using UnityEditor;

namespace AgXUnityEditor.Utils
{
  public class VisualPrimitive
  {
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
        m_mouseOverNode = value;
        UpdateColor();
      }
    }

    public delegate void OnMouseClickDelegate();

    public event OnMouseClickDelegate OnMouseClick = delegate {};

    public void FireOnMouseClick() { OnMouseClick(); }

    protected VisualPrimitive( AgXUnity.Rendering.Spawner.Primitive primitiveType, string shader = "Diffuse" )
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
      if ( Node == null )
        return;

      Renderer renderer = Node.GetComponent<Renderer>();
      if ( m_mouseOverNode )
        renderer.sharedMaterial.color = MouseOverColor;
      else
        renderer.sharedMaterial.color = Color;
    }
  }

  public class VisualPrimitiveCylinder : VisualPrimitive
  {
    public void SetTransform( Vector3 start, Vector3 end, float radius, bool constantScreenSize = true )
    {
      if ( Node == null )
        return;

      float r       = constantScreenSize ?
                        radius * Mathf.Max( HandleUtility.GetHandleSize( start ), HandleUtility.GetHandleSize( end ) ) :
                        radius;
      Vector3 dir   = end - start;
      float height  = dir.magnitude;
      dir          /= height;

      Node.transform.localScale = new Vector3( 2.0f * r, 0.5f * height, 2.0f * r );
      Node.transform.rotation   = Quaternion.FromToRotation( Vector3.up, dir );
      Node.transform.position   = 0.5f * ( start + end );
    }

    public VisualPrimitiveCylinder( string shader = "Diffuse" )
      : base( AgXUnity.Rendering.Spawner.Primitive.Cylinder, shader )
    {
    }
  }
}
