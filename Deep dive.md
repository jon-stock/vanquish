# Vanquish — Subsystem Design Deep Dive

This document details the architectural specifications, mathematical models, data schemas, and implementation logic for core game subsystems in *Vanquish*.

---

## 1. Modular Workshop Visual Assembly

### Node Structure & Hierarchy
Airframe and missile assemblies utilize a root prefab (`GameObject`) containing dedicated child transform nodes with standardized component identifiers:

* `HardpointNode`: External weapon attachment points.
* `NoseNode`: Seeker, radar radome, or nose cone meshes.
* `EngineNode`: Motor nozzle or thruster meshes.
* `BayNode`: Internal payload bay geometry.

When a player selects a part in the Workshop UI, the `WorkshopAssemblyManager` handles mesh swapping and transform anchoring:

```csharp
public class WorkshopAssemblyManager : MonoBehaviour
{
    public void SwapModule(Transform targetNode, GameObject newPartPrefab)
    {
        foreach (Transform child in targetNode)
        {
            Destroy(child.gameObject);
        }

        GameObject instantiatedPart = Instantiate(newPartPrefab, targetNode);
        instantiatedPart.transform.localPosition = Vector3.zero;
        instantiatedPart.transform.localRotation = Quaternion.identity;
    }
}
```

### Continuous Component Visual Scaling
Components governed by continuous UI sliders (e.g., fuel tanks, battery cell stacks) dynamically scale internal geometry during **Internal / X-Ray View**:

* **Fuel Tanks**: Sliders adjust Z-axis local scaling and update fluid fill shader properties (`_FillPercent`) on a cylindrical tank mesh.
* **Batteries**: Sliders programmatically toggle the visibility of individual battery cell meshes arranged in a grid array within the hull cavity.

---

## 2. Dynamic Aerodynamic Drag & Visual Overlay

### 3-DOF Drag Calculation
Drag force ($F_d$) is calculated per frame based on atmospheric density ($\rho$), base drag coefficient ($C_{d\_base}$), additive external hardpoint drag ($C_{d\_hardpoints}$), frontal cross-sectional area ($A$), and velocity magnitude ($v$):

$$F_d = \frac{1}{2} \cdot \rho(h) \cdot v^2 \cdot \left(C_{d\_base} + \sum C_{d\_hardpoints}\right) \cdot A$$

Atmospheric density decays exponentially with altitude ($h$):

$$\rho(h) = \rho_0 \cdot e^{-\frac{h}{H_0}}$$

Where $\rho_0 = 1.225 \text{ kg/m}^3$ (sea-level density) and $H_0 \approx 8500 \text{ m}$ (scale height).

### Workshop Streamline Overlay
When the **Aerodynamic Airflow Overlay** is active:
* A particle stream projects vector lines parallel to the airframe's forward vector.
* **Vertex Shader Impingement Mapping**: Surfaces with normals aligned against the velocity vector render in warm orange/red hues, indicating high-drag regions. Sleek, faired surfaces render in cool cyan/blue hues.

---

## 3. Decoupled C4ISR & Telemetry Data Schema

### TargetTrack Interface
Seekers, fire-control radars, and weapons query abstract data containers (`TargetTrack`) rather than direct engine `Transform` positions. This enables off-board guidance, AWACS datalinks, and passive sensor tracking without refactoring base guidance logic.

```csharp
public struct TargetTrack
{
    public int TrackID;
    public Vector3 EstimatedPosition;
    public Vector3 EstimatedVelocity;
    public float TrackQuality; // 0.0 (fuzzed/uncertain) to 1.0 (fire-control lock)
    public float UncertaintyRadius; // Visualized as targeting circle on map/HUD
    public int ReportingNodeID; // ID of the sensor providing the track
    public double LastUpdatedTimestamp;
}

public interface IDatalinkNode
{
    int NodeID { get; }
    void BroadcastTrack(TargetTrack track);
    List<TargetTrack> GetNetworkTracks();
}
```

### Guidance Processing
* **Onboard Sensor Track**: Onboard radar/IR sensors populate `EstimatedPosition` directly, setting `TrackQuality = 1.0f`.
* **Datalink / Remote Lock**: Missile receivers acquire `TargetTrack` updates via network packets from remote nodes implementing `IDatalinkNode`. Proportional Navigation (PN) guidance algorithms process position and velocity vectors identically regardless of data source.

---

## 4. Component Proximity Damage Model

### Detonation & Ray-Cast Falloff
When a warhead detonates at position $P_{det}$:
1. **Ray-Cast Penetration Check**: Spherical raycasts target sub-component hitboxes (`Engine`, `FuelTank`, `Seeker`, `FlightSurfaces`).
2. **Blast Damage Decay**: Damage applied to sub-component $i$ scales inversely with distance $r$ from $P_{det}$:

$$Damage_i = BaselineDamage \cdot \left(1 - \frac{r}{R_{blast}}\right)^2 \cdot (1 - Armor_{i})$$

```csharp
public class DamageProcessor : MonoBehaviour
{
    public void ProcessExplosion(Vector3 detPosition, float blastRadius, float baseDamage)
    {
        Collider[] hitColliders = Physics.OverlapSphere(detPosition, blastRadius);
        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent<ISubComponent>(out var component))
            {
                float distance = Vector3.Distance(detPosition, hit.transform.position);
                float damage = baseDamage * Mathf.Pow(1f - (distance / blastRadius), 2f);
                component.ApplyDamage(damage);
            }
        }
    }
}
```

### Sub-Component Damage Penalties
* **Fuel Tank**: Triggers continuous fuel leakage ($\dot{m}_{leak}$), reducing operational range.
* **Flight Surfaces**: Reduces maximum lift coefficient ($C_{L\_max}$) and multiplies turn rate limits by stability factor $S \in [0.1, 1.0]$.
* **Seeker**: Degrades `TrackQuality` or breaks lock entirely, forcing guided missiles into unguided ballistic flight or self-destruction.

---

## 5. Altitude Control Modes & Terrain Collision Physics

### Altitude Modes

Units operate under two primary altitude command structures via `ICommandReceiver`:

* **`AbsoluteMSL` (Mean Sea Level)**:
  * Targets a fixed world Y-axis altitude.
  * Ignores ground terrain variations. Used for low-observable flight behind ridgelines; poses high collision risks in unmapped terrain.
* **`RelativeAGL` (Above Ground Level)**:
  * Targets $Y_{target} = Y_{ground} + Alt_{desired}$ using downward terrain sampling.
  * Constrained by maximum rate-of-climb capabilities ($\dot{y}_{max}$).

### Collision Physics & Climb Rate Limits
Approaching vertical terrain features (e.g., cliffs) requires a climb rate proportional to forward speed ($v_{forward}$), obstacle height ($\Delta h_{cliff}$), and detection distance ($d_{detection}$):

$$\text{Required Climb Rate} = \frac{v_{\text{forward}} \cdot \Delta h_{\text{cliff}}}{d_{\text{detection}}}$$

If $\text{Required Climb Rate} > \dot{y}_{max}$, terrain collision occurs.

```csharp
public struct TerrainCollisionCheck
{
    public bool WillCollide;
    public float DistanceToImpact;
    public Vector3 SurfaceNormal;
    
    public bool IsVerticalCliff(float cliffAngleThreshold = 60f)
    {
        return Vector3.Angle(SurfaceNormal, Vector3.up) > cliffAngleThreshold;
    }
}
```

---

## 6. Landing & Ground Interaction Mechanics

### Touchdown Validation
Touchdown safety depends on downward sink rate ($v_{vertical}$), horizontal ground speed ($v_{horizontal}$), and terrain normal angle ($\theta$):

$$SafeLanding = (v_{vertical} \le v_{max\_vert}) \land (v_{horizontal} \le v_{max\_horiz}) \land (\arccos(\mathbf{n} \cdot \mathbf{k}) \le \theta_{max\_slope})$$

Where $\mathbf{n}$ is the terrain surface normal and $\mathbf{k}$ is the world up vector $(0, 1, 0)$.

### Surface Friction Matrix

| Surface Type | Max Landing Slope ($\theta_{max}$) | Rolling Friction ($\mu_r$) | Static Friction ($\mu_s$) | Risk Profile |
| :--- | :--- | :--- | :--- | :--- |
| **Paved Runway / Helipad** | $15^\circ$ | 0.02 | 0.80 | Nominal |
| **Flat Grass / Soil** | $10^\circ$ | 0.08 | 0.65 | Low (Gear wear) |
| **Uneven / Rock** | $5^\circ$ | 0.25 | 0.50 | Moderate (Propeller strike) |
| **Water / Marsh** | $0^\circ$ | N/A | N/A | Destruction / Vehicle Sink |

---

## 7. Dynamic Deployables & SAM System Footprints

### Placement Validation
Placing ground structures (SAM batteries, radar towers, C2 bunkers) triggers terrain footprint validation checks:

```csharp
public struct PlacementValidation
{
    public bool IsSlopeValid;       // Slope angle <= maxPlacementSlope
    public bool IsAboveSeaLevel;    // Elevation Y > 0
    public bool UnobstructedGrid;   // Target grid cells free of static objects

    public bool CanBuild => IsSlopeValid && IsAboveSeaLevel && UnobstructedGrid;
}
```

### Dynamic Terrain Modification
1. **Vegetation Clearance ($R_{clear}$)**: Suppresses procedural tree instances within a specified radius around position $P_{build}$:

$$\text{Distance}(P_{tree}, P_{build}) \le R_{clear} \implies \text{SuppressTreeInstance()}$$

2. **Emitter Height Offset ($h_{emitter}$)**: Radar nodes evaluate Line-of-Sight (LOS) from an elevated phase-center origin:

$$\mathbf{P}_{radar} = (X, Y_{ground} + h_{emitter}, Z)$$

---

## 8. World Grid Architecture & Spatial Occupancy

### Grid Transformation Formulas
World space position $(X, Y, Z)$ maps to discrete grid coordinates $(g_x, g_z)$ based on cell size $S_{cell}$:

$$g_x = \left\lfloor \frac{X + \frac{1}{2} S_{cell}}{S_{cell}} \right\rfloor, \quad g_z = \left\lfloor \frac{Z + \frac{1}{2} S_{cell}}{S_{cell}} \right\rfloor$$

Snapped position vector $P_{snap}$:

$$P_{snap} = \left( g_x \cdot S_{cell}, \quad Y_{terrain}(P_{snap}), \quad g_z \cdot S_{cell} \right)$$

### Grid Occupancy Schema

```csharp
public enum CellState { Clear, OccupiedStatic, ReservedMobile, BlockedTerrain }

public struct WorldGridCell
{
    public Vector2Int GridCoords;
    public CellState State;
    public float Elevation;
    public float SlopeAngle;
    public int OccupantEntityID;
}
```

### Anchor Protocol
Upon confirming structure placement:
1. Underlying cells transition to `OccupiedStatic` and register `OccupantEntityID`.
2. Dynamic obstacles (`NavMeshObstacle`) update cell boundaries to modify pathfinding routes for ground vehicles.
3. Structure transforms lock to $P_{snap}$, disabling unneeded physics solvers to prevent position drift.
4. 
