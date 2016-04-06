namespace AgXUnity
{
  /// <summary>
  /// Range real object containing min (default -infinity) and
  /// max value (default +infinity).
  /// </summary>
  [System.Serializable]
  public class RangeReal
  {
    /// <summary>
    /// Get or set min value less than max value.
    /// </summary>
    public double Min = double.NegativeInfinity;

    /// <summary>
    /// Get or set max value larger than min value.
    /// </summary>
    public double Max = double.PositiveInfinity;

    /// <summary>
    /// Convert to native type agx.RangeReal given current min and max.
    /// </summary>
    public agx.RangeReal Native { get { return new agx.RangeReal( Min, Max ); } }
  }
}
