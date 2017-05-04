using System;

namespace AgXUnity
{
  /// <summary>
  /// Cable route object containing nodes that initializes a cable.
  /// This object is an IEnumerable, add "using System.Linq" to
  /// get a wide range of "features" such as ToArray().
  /// </summary>
  [Serializable]
  public class CableRoute : Route<CableRouteNode>
  {
  }
}
