namespace Vanquish.Core
{
    /// <summary>
    /// Which side a unit belongs to. Lives in Core (not Combat) since it's referenced
    /// by lower-level Simulation components (DetectableSignature, DetectionSensor)
    /// that must stay mode-agnostic and not depend on Combat-specific types.
    /// </summary>
    public enum Team
    {
        Player,
        Enemy,
    }
}
