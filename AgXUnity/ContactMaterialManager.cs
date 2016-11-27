﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgXUnity;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Contact material list data.
  /// </summary>
  [System.Serializable]
  public class ContactMaterialEntry
  {
    public ContactMaterial ContactMaterial = null;
  }

  /// <summary>
  /// Contact material manager which enables the user to manage contact materials.
  /// </summary>
  [AddComponentMenu( "" )]
  public class ContactMaterialManager : UniqueGameObject<ContactMaterialManager>
  {
    [SerializeField]
    private List<ContactMaterialEntry> m_contactMaterials = new List<ContactMaterialEntry>();

    [HideInInspector]
    public ContactMaterial[] ContactMaterials
    {
      get
      {
        return ( from entry in m_contactMaterials where entry.ContactMaterial != null select entry.ContactMaterial ).ToArray();
      }
    }

    public void Add( ContactMaterial contactMaterial )
    {
      if ( contactMaterial == null || ContactMaterials.Contains( contactMaterial ) )
        return;

      m_contactMaterials.Add( new ContactMaterialEntry() { ContactMaterial = contactMaterial } );
    }

    public void Remove( ContactMaterial contactMaterial )
    {
      int index = -1;
      while ( ( index = Array.FindIndex( ContactMaterials, cm => { return cm == contactMaterial; } ) ) >= 0 )
        m_contactMaterials.RemoveAt( index );
    }

    protected override bool Initialize()
    {
      foreach ( var entry in m_contactMaterials ) {
        if ( entry.ContactMaterial == null )
          continue;

        ContactMaterial contactMaterial = entry.ContactMaterial.GetInitialized<ContactMaterial>();
        if ( contactMaterial != null && contactMaterial.Native != null )
          GetSimulation().getMaterialManager().add( contactMaterial.Native );
      }
      return base.Initialize();
    }
  }
}
