using System;
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
  [GenerateCustomEditor]
  public class ContactMaterialManager : UniqueGameObject<ContactMaterialManager>
  {
    [SerializeField]
    private List<ContactMaterialEntry> m_contactMaterials = new List<ContactMaterialEntry>();
    public List<ContactMaterialEntry> ContactMaterials
    {
      get { return m_contactMaterials; }
      set { m_contactMaterials = value; }
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
