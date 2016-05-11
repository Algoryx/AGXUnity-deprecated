﻿using System;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  [Serializable]
  public class Frame
  {
    [SerializeField]
    private GameObject m_parent = null;
    /// <summary>
    /// Current parent.
    /// </summary>
    public GameObject Parent { get { return m_parent; } }

    [SerializeField]
    private Vector3 m_localPosition = Vector3.zero;
    /// <summary>
    /// Local position to parent. Same as world if parent == null.
    /// </summary>
    public Vector3 LocalPosition { get { return m_localPosition; } set { m_localPosition = value; } }

    [SerializeField]
    private Quaternion m_localRotation = Quaternion.identity;
    /// <summary>
    /// Local rotation to parent. Same as world if parent == null.
    /// </summary>
    public Quaternion LocalRotation { get { return m_localRotation; } set { m_localRotation = value; } }

    /// <summary>
    /// Current position of this frame.
    /// </summary>
    public Vector3 Position
    {
      get { return CalculateWorldPosition( Parent, LocalPosition ); }
      set
      {
        LocalPosition = CalculateLocalPosition( Parent, value );
      }
    }

    /// <summary>
    /// Current rotation of this frame.
    /// </summary>
    public Quaternion Rotation
    {
      get { return CalculateWorldRotation( Parent, LocalRotation ); }
      set
      {
        LocalRotation = CalculateLocalRotation( Parent, value );
      }
    }

    /// <summary>
    /// Construct given a parent.
    /// </summary>
    /// <param name="parent">Parent object.</param>
    public Frame( GameObject parent = null )
      : base()
    {
      m_parent = parent;
    }

    /// <summary>
    /// Assign new parent and choose whether the frame will "jump" to
    /// the new object, i.e., keep local transform (inheritWorldTransform = false),
    /// or to calculate a new local transform given the new parent with
    /// inheritWorldTransform = true.
    /// </summary>
    /// <param name="parent">New parent.</param>
    /// <param name="inheritWorldTransform">If true, new local transform will be calculated.
    ///                                     If false, local transform is preserved and new world transform.</param>
    public void SetParent( GameObject parent, bool inheritWorldTransform = true )
    {
      if ( parent == Parent )
        return;

      // New local position/rotation given current world transform.
      if ( inheritWorldTransform ) {
        m_parent = parent;

        LocalPosition = CalculateLocalPosition( Parent, Position );
        LocalRotation = CalculateLocalRotation( Parent, Rotation );
      }
      // New world position/rotation given current local transform.
      else {
        m_parent = parent;

        Position = CalculateWorldPosition( Parent, LocalPosition );
        Rotation = CalculateWorldRotation( Parent, LocalRotation );
      }
    }

    /// <summary>
    /// Calculates current world position in <paramref name="gameObject"/> local frame.
    /// </summary>
    /// <returns></returns>
    public Vector3 CalculateLocalPosition( GameObject gameObject )
    {
      return CalculateLocalPosition( gameObject, Position );
    }

    /// <summary>
    /// Calculate current world rotation in <paramref name="gameObject"/> local frame.
    /// </summary>
    /// <returns></returns>
    public Quaternion CalculateLocalRotation( GameObject gameObject )
    {
      return CalculateLocalRotation( gameObject, Rotation );
    }

    public static Vector3 CalculateLocalPosition( GameObject gameObject, Vector3 worldPosition )
    {
      if ( gameObject == null )
        return worldPosition;

      return gameObject.transform.InverseTransformDirection( worldPosition - gameObject.transform.position );
    }

    public static Vector3 CalculateWorldPosition( GameObject gameObject, Vector3 localPosition )
    {
      if ( gameObject == null )
        return localPosition;

      return gameObject.transform.position + gameObject.transform.TransformDirection( localPosition );
    }

    public static Quaternion CalculateLocalRotation( GameObject gameObject, Quaternion worldRotation )
    {
      if ( gameObject == null )
        return worldRotation;

      return ( Quaternion.Inverse( gameObject.transform.rotation ) * worldRotation ).Normalize();
    }

    public static Quaternion CalculateWorldRotation( GameObject gameObject, Quaternion localRotation )
    {
      if ( gameObject == null )
        return localRotation;

      return ( gameObject.transform.rotation * localRotation ).Normalize();
    }
  }
}
