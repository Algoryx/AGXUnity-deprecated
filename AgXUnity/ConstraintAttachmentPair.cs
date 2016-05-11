using System;
using UnityEngine;

namespace AgXUnity
{
  [Serializable]
  public class ConstraintAttachmentPair
  {
    public GameObject ReferenceObject
    {
      get { return m_referenceFrame.Parent; }
      set
      {
        if ( value != null && value.GetComponentInParent<RigidBody>() == null ) {
          Debug.LogWarning( "Reference object must have a AgXUnity.RigidBody component (or in parents). Ignoring reference object.", value );
          return;
        }

        m_referenceFrame.SetParent( value );
      }
    }

    public GameObject ConnectedObject
    {
      get { return m_connectedFrame.Parent; }
      set
      {
        m_connectedFrame.SetParent( value );
      }
    }

    [SerializeField]
    private Frame m_referenceFrame = new Frame();
    public Frame ReferenceFrame
    {
      get { return m_referenceFrame; }
    }

    [SerializeField]
    private Frame m_connectedFrame = new Frame();
    public Frame ConnectedFrame
    {
      get { return m_connectedFrame; }
    }

    [SerializeField]
    private bool m_synchronized = true;
    public bool Synchronized
    {
      get { return m_synchronized; }
      set { m_synchronized = value; }
    }

    public ConstraintAttachmentPair( bool synchronized = true )
    {
    }

    public void Update()
    {
      if ( Synchronized ) {
        m_connectedFrame.Position = m_referenceFrame.Position;
        m_connectedFrame.Rotation = m_referenceFrame.Rotation;
      }
    }
  }
}
