using System.Linq;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  [ExecuteInEditMode]
  [GenerateCustomEditor]
  public class DebugRenderManager : UniqueGameObject<DebugRenderManager>
  {
    protected override bool Initialize()
    {
      gameObject.transform.position = Vector3.zero;
      gameObject.transform.rotation = Quaternion.identity;
      gameObject.isStatic = true;
      gameObject.hideFlags = HideFlags.None;

      return base.Initialize();
    }

    protected void Update()
    {
      UnityEngine.Object.FindObjectsOfType<Collide.Shape>().ToList().ForEach(
        shape => shape.gameObject.GetOrCreateComponent<ShapeDebugRenderData>().Synchronize()
      );

      UnityEngine.Object.FindObjectsOfType<Constraint>().ToList().ForEach(
        constraint => constraint.gameObject.GetOrCreateComponent<ConstraintDebugRenderData>().Synchronize()
      );
    }
  }
}
