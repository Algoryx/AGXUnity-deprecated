using AgXUnity.Utils;
using UnityEngine;

namespace AgXUnity
{
  [GenerateCustomEditor]
  public class ConstraintDebugRenderData : DebugRenderData
  {
    public Constraint GetConstraint() { return gameObject.GetComponent<Constraint>(); }

    public override string GetTypeName()
    {
      return "Constraint";
    }

    public override void Synchronize()
    {
      try {
        TryInitialize();

        Constraint constraint = GetConstraint();
        Node.transform.position = constraint.Frame1.transform.position;
        Node.transform.rotation = ( constraint.Frame1.transform.rotation * Quaternion.FromToRotation( Vector3.up, Vector3.forward ) ).Normalize();

        Node.transform.localScale = new Vector3( 0.3f, 0.3f, 0.3f );
      }
      catch ( System.Exception ) {
      }
    }

    private void TryInitialize()
    {
      if ( Node != null )
        return;

      Constraint constraint = GetComponent<Constraint>();
      if ( constraint == null )
        throw new AgXUnity.Exception( "Constraint is not a component to this game object." );

      if ( !constraint.Valid )
        throw new AgXUnity.Exception( "Constraint with an invalid configuration." );

      Node = PrefabLoader.Instantiate( PrefabName );
      if ( Node == null )
        throw new AgXUnity.Exception( "Prefab not found: " + PrefabName );

      gameObject.AddChild( Node );
    }
  }
}
