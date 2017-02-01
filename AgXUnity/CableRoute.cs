using System;

namespace AgXUnity
{
  /// <summary>
  /// <summary>
  /// Cable route object containing nodes that initializes a wire.
  /// This object is an IEnumerable, add "using System.Linq" to
  /// get a wide range of "features" such as ToArray().
  /// </summary>
  [Serializable]
  public class CableRoute : Route<CableRouteNode>
  {
  }
}
