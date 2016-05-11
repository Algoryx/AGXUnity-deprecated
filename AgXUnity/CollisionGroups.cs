using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  /// <summary>
  /// Collision group identifier containing a name tag.
  /// </summary>
  [System.Serializable]
  public class CollisionGroupEntry
  {
    [SerializeField]
    private string m_tag = "";
    public string Tag
    {
      get { return m_tag; }
      set { m_tag = value; }
    }

    /// <summary>
    /// If <paramref name="obj"/> has method "addGroup( UInt32 )" this method
    /// converts the name tag to an UInt32 using 32 bit FNV1 hash.
    /// </summary>
    /// <param name="obj">Object to execute addGroup on.</param>
    public void AddTo( object obj )
    {
      if ( obj == null )
        return;

      MethodInfo[] methodInfos = obj.GetType().GetMethods( BindingFlags.Public | BindingFlags.Instance );
      foreach ( MethodInfo methodInfo in methodInfos ) {
        if ( methodInfo.Name == "addGroup" && methodInfo.GetParameters().Length > 0 && methodInfo.GetParameters()[ 0 ].ParameterType == typeof( System.UInt32 ) )
          methodInfo.Invoke( obj, new object[] { Tag.To32BitFnv1aHash() } );
      }
    }
  }

  /// <summary>
  /// Component holding a list of name tags for collision groups.
  /// </summary>
  [AddComponentMenu( "AgXUnity/Collisions/CollisionGroups" )]
  public class CollisionGroups : ScriptComponent
  {
    /// <summary>
    /// List of collision groups paired with property Groups.
    /// </summary>
    [SerializeField]
    private List<CollisionGroupEntry> m_groups = new List<CollisionGroupEntry>() { };

    /// <summary>
    /// Get current list of groups.
    /// </summary>
    public List<CollisionGroupEntry> Groups
    {
      get { return m_groups; }
    }

    /// <param name="tag">Name tag to check if it exist in the current set of groups.</param>
    /// <returns>True if the given name tag exists.</returns>
    public bool HasGroup( string tag )
    {
      return m_groups.Find( entry => entry.Tag == tag ) != null;
    }

    /// <summary>
    /// Add new group.
    /// </summary>
    /// <param name="tag">New group tag.</param>
    /// <returns>True if the group was added - otherwise false (e.g., already exists).</returns>
    public bool AddGroup( string tag )
    {
      if ( HasGroup( tag ) )
        return false;

      m_groups.Add( new CollisionGroupEntry() { Tag = tag } );

      return true;
    }

    /// <summary>
    /// Remove group.
    /// </summary>
    /// <param name="tag">Group to remove.</param>
    /// <returns>True if removed - otherwise false.</returns>
    public bool RemoveGroup( string tag )
    {
      int index = m_groups.FindIndex( entry => entry.Tag == tag );
      if ( index < 0 )
        return false;

      m_groups.RemoveAt( index );

      return true;
    }

    /// <summary>
    /// Initialize, finds supported object and executes addGroup to it.
    /// </summary>
    protected override bool Initialize()
    {
      if ( m_groups.Count == 0 )
        return base.Initialize();

      RigidBody rb = GetComponent<RigidBody>();
      if ( rb != null && rb.GetInitialized<RigidBody>() != null )
        AddGroups( rb.Native );

      Wire wire = GetComponent<Wire>();
      if ( wire != null && wire.GetInitialized<Wire>() != null )
        AddGroups( wire.Native );

      //Cable cable = GetComponent<Cable>();
      //if ( cable != null && cable.GetInitialized<Cable>() != null )
      //  AddGroups( cable.Native );

      return base.Initialize();
    }

    private void AddGroups( agx.RigidBody rb )
    {
      if ( rb == null || rb.getGeometries().Count == 0 )
        return;

      foreach ( agxCollide.GeometryRef geometry in rb.getGeometries() )
        AddGroups( (object)geometry.get() );
    }

    private void AddGroups( agxModel.Deformable1D d1d )
    {
      if ( d1d == null || !d1d.initialized() )
        return;

      agxModel.Deformable1DIterator it = d1d.getBeginIterator();
      while ( !it.isEnd() ) {
        AddGroups( it.get().getRigidBody() );
        it.inc();
      }
    }

    private void AddGroups( object obj )
    {
      foreach ( CollisionGroupEntry group in m_groups )
        group.AddTo( obj );
    }
  }
}
