using System.Collections.Generic;
using UnityEngine;
using Vanquish.Core;
using Vanquish.Data;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;
using Vanquish.Simulation.Flight;
using Vanquish.Simulation.Guidance;
using Vanquish.Simulation.Sensors;

namespace Vanquish.Combat
{
    /// <summary>
    /// Builds real GameObjects (physics root + visual child, matching the Phase 0
    /// pattern) from a MissileLoadout/DroneLoadout's computed DesignStatsCalculator
    /// output. Used identically by the Workshop's test range and real Combat scenes,
    /// so a design behaves exactly the same in both places.
    /// </summary>
    public static class VehicleFactory
    {
        public static GameObject SpawnMissile(MissileLoadout loadout, Vector3 position, Quaternion rotation,
            Transform target, Team team)
        {
            MissileRuntimeStats stats = DesignStatsCalculator.Calculate(loadout);

            var missile = new GameObject($"Missile_{loadout.designName}");
            missile.transform.SetPositionAndRotation(position, rotation);

            var collider = missile.AddComponent<CapsuleCollider>();
            collider.direction = 2; // Z-axis, matches forward-facing physics root
            collider.radius = 0.3f;
            collider.height = 1.8f;

            var rb = missile.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            var flightBody = missile.AddComponent<FlightBody>();
            flightBody.Configure(stats.massKg, stats.thrustNewtons, stats.dragCoefficient, stats.maxGForce);
            flightBody.isThrusting = true;

            // Depth pass: fuel fill now genuinely limits how long the engine burns —
            // see MissileBurnController's own doc comment. A non-positive burn time
            // (shouldn't happen with any seeded engine, but guards against one) is
            // treated as "never runs out" rather than instantly killing thrust.
            if (stats.effectiveBurnTimeSeconds > 0f)
            {
                var burnController = missile.AddComponent<MissileBurnController>();
                burnController.flightBody = flightBody;
                burnController.burnTimeSeconds = stats.effectiveBurnTimeSeconds;
            }

            if (target != null)
            {
                var guidance = missile.AddComponent<GuidanceController>();
                guidance.SetTarget(target);
                // Phase 2C: pick the guidance law from the missile's seeker type (and
                // datalink, if any) instead of always defaulting to pursuit — see
                // GuidanceLawFactory for the mapping.
                guidance.SetGuidanceLaw(GuidanceLawFactory.Create(loadout));
                // Depth pass: the seeker's own range/FOV/countermeasure-susceptibility
                // now genuinely gate whether guidance can correct at all this tick —
                // see GuidanceController's own tooltips for why "always hits" was true
                // before this was wired.
                guidance.seekerRangeMeters = stats.seekerRangeMeters;
                guidance.seekerFieldOfViewDegrees = stats.seekerFieldOfViewDegrees;
                guidance.countermeasureSusceptibility = stats.countermeasureSusceptibility;
            }

            var signature = missile.AddComponent<DetectableSignature>();
            signature.radarCrossSection = stats.radarCrossSection;
            signature.infraredSignature = stats.infraredSignature;
            signature.team = team;

            var impact = missile.AddComponent<MissileImpact>();
            impact.directDamage = stats.directDamage;
            impact.splashDamage = stats.splashDamage;
            impact.blastRadiusMeters = stats.blastRadiusMeters;

            var sensor = missile.AddComponent<DetectionSensor>();
            sensor.baseRangeMeters = stats.seekerRangeMeters;
            sensor.ownerTeam = team;
            // Phase 2C: seeker jam resistance + any ECCM (JammingDefinition.counterJammingStrength)
            // already folded into stats.jamResistance — thread it into the sensor so
            // enemy jamming actually has something to be resisted against.
            sensor.jamResistance = stats.jamResistance;
            sensor.reacquisitionGraceSeconds = loadout.seeker.reacquisitionTimeSeconds > 0f
                ? loadout.seeker.reacquisitionTimeSeconds
                : sensor.reacquisitionGraceSeconds;

            // Phase 2C: a missile carrying a jamming module actively degrades nearby
            // enemy sensors' detection chance while it flies — see JammerSource's doc
            // comment for why this lives on the missile's own loadout slot rather than
            // the drone's.
            if (loadout.jamming != null)
            {
                var jammer = missile.AddComponent<JammerSource>();
                jammer.jammingStrength = loadout.jamming.jammingStrength;
                jammer.jammingRangeMeters = loadout.jamming.jammingRangeMeters;
                jammer.team = team;
            }

            if (loadout.payload.requiresProximityFuse && stats.blastRadiusMeters > 0f)
            {
                var fuseGo = new GameObject("ProximityFuse");
                fuseGo.transform.SetParent(missile.transform, worldPositionStays: false);
                fuseGo.transform.localPosition = Vector3.zero;
                var fuseCollider = fuseGo.AddComponent<SphereCollider>();
                fuseCollider.isTrigger = true;
                fuseCollider.radius = Mathf.Max(3f, stats.blastRadiusMeters);
                var fuseRelay = fuseGo.AddComponent<ProximityFuseRelay>();
                fuseRelay.owner = impact;
            }

            // Phase 3B: MissileVisualBuilder derives body proportions from the
            // airframe's own stats and gives the nose a seeker-specific shape (see
            // its own doc comment) — team coloring and the tail engine glow are both
            // handled inside it now, replacing the old fixed-size capsule + manual
            // color/glow calls that used to live here.
            MissileVisualBuilder.Build(missile.transform, loadout, team);

            return missile;
        }

        public static GameObject SpawnDrone(DroneLoadout loadout, Vector3 position, Quaternion rotation, Team team)
        {
            DroneRuntimeStats stats = DesignStatsCalculator.Calculate(loadout);

            var drone = new GameObject($"Drone_{loadout.designName}_{team}");
            drone.transform.SetPositionAndRotation(position, rotation);

            var collider = drone.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.5f, 0.8f, 2f);

            var rb = drone.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            var flightBody = drone.AddComponent<FlightBody>();

            // Phase 2B: read the flight model from the design's propulsion choice instead
            // of hardcoding quadcopter behavior for every drone. Electric multirotor
            // propulsion (requiresForwardFlight = false) stays omnidirectional — no
            // constant forward thrust, no auto-orient-to-velocity, no gravity/lift
            // (it gets vertical lift for free from vectored thrust), all movement via
            // vectored steering force (AI guidance or player WASD input), same as a real
            // multirotor's vectored thrust. Fixed-wing/jet propulsion (requiresForwardFlight
            // = true) gets a real (simplified) aerodynamic model instead: constant forward
            // thrust, gravity + speed-squared lift from the wing part's liftCoefficient
            // (via the lift-aware Configure overload), and orients nose to velocity — the
            // right relationship for AI/missile-style guidance (steer laterally, let the
            // body align to the resulting velocity). PlayerDroneController overrides
            // orientToVelocity for the player's own drone specifically (direct player
            // stick control instead — see its class comment for why), reading isThrusting
            // at spawn time (set here) to decide which control scheme applies.
            if (stats.requiresForwardFlight)
                flightBody.Configure(stats.massKg, stats.thrustNewtons, stats.dragCoefficient, stats.maxGForce, stats.liftCoefficient,
                    stats.zeroLiftAoADegrees, stats.referenceAoADegrees, stats.criticalAoADegrees, stats.inducedDragFactor);
            else
                flightBody.Configure(stats.massKg, stats.thrustNewtons, stats.dragCoefficient, stats.maxGForce);

            flightBody.isThrusting = stats.requiresForwardFlight;
            flightBody.orientToVelocity = stats.requiresForwardFlight;

            var signature = drone.AddComponent<DetectableSignature>();
            signature.radarCrossSection = stats.radarCrossSection;
            signature.infraredSignature = stats.infraredSignature;
            signature.team = team;
            // Phase 2D: baked in here (rather than left for a caller to probe via
            // GetComponent<WeaponController>()) so any AI archetype can cheaply tell
            // an armed strike drone apart from an unarmed scout straight off the
            // DetectableSignature it already gets from TeamAwareness/DetectionSensor.
            signature.isArmed = loadout.missileLoadout != null && loadout.missileLoadout.IsComplete;
            // Phase 2D: same idea for the Scout-hunter archetype — a "scout" is
            // whatever the design's own sensor suite says it is (sharesContactsWithTeam),
            // not a separate role flag that could drift out of sync with the part data.
            signature.isScout = loadout.sensorSuite != null && loadout.sensorSuite.sharesContactsWithTeam;

            var sensor = drone.AddComponent<DetectionSensor>();
            sensor.baseRangeMeters = stats.sensorRangeMeters;
            sensor.ownerTeam = team;

            var health = drone.AddComponent<Health>();
            health.SetMaxHealth(stats.maxHealth);

            drone.AddComponent<CrashDamage>();

            // Phase 2C: an optional decoy/flare-chaff countermeasure gives this drone a
            // chance to break an inbound missile's lock — see CountermeasureController's
            // doc comment for why this lives on the drone's loadout, not the missile's.
            if (loadout.countermeasure != null)
            {
                var countermeasures = drone.AddComponent<CountermeasureController>();
                countermeasures.decoyChargesRemaining = loadout.countermeasure.decoyCharges;
                countermeasures.decoySuccessChance = loadout.countermeasure.decoySuccessChance;
            }

            WeaponController weapon = null;
            if (loadout.missileLoadout != null && loadout.missileLoadout.IsComplete)
            {
                weapon = drone.AddComponent<WeaponController>();
                weapon.missileLoadout = loadout.missileLoadout;
                // Depth pass: ammoRemaining now uses the bay-capacity-clamped
                // effectiveAmmoCount instead of the raw (previously unclamped)
                // DroneLoadout.ammoCount — see WeaponBayDefinition.maxMunitionCount's
                // own tooltip.
                weapon.ammoRemaining = stats.effectiveAmmoCount;
                weapon.ownerTeam = team;
                // Depth pass (direct user feedback: "the craft should actually get more
                // missiles, with multiple being able to be in flight at once with the
                // right missile tech"): how many of this drone's own missiles can be
                // guided simultaneously depends on the seeker — see
                // WeaponController.maxConcurrentInFlight's own tooltip.
                weapon.maxConcurrentInFlight = ComputeMaxConcurrentInFlight(loadout.missileLoadout.seeker);
            }

            // Procedural visual + hardpoint-mounted missiles — see
            // BuildVisualAndMountedMissiles's own doc comment. Shared with
            // BuildVisualOnlyDrone (the Workshop preview's code path) so "the model
            // looks the same in the Workshop preview and in live combat" is
            // structurally guaranteed by one implementation, not maintained by hand
            // across two.
            BuildVisualAndMountedMissiles(drone, loadout, stats, team, rb, weapon);

            if (CombatManager.Instance != null)
                CombatManager.Instance.RegisterUnit(drone, team);

            return drone;
        }

        /// <summary>
        /// Phase 3B: builds a purely visual (no Rigidbody/Collider/AI/Health/
        /// WeaponController) drone matching exactly what SpawnDrone would build for
        /// this loadout/team — used by the Workshop's live design preview
        /// (WorkshopPreviewStage) so the model shown while designing is guaranteed
        /// identical to what actually spawns in combat, not a second hand-maintained
        /// approximation. Returns an empty (childless) GameObject if the loadout isn't
        /// complete yet (e.g. mid-way through picking parts) rather than throwing —
        /// the preview should show "nothing yet", not crash, while the player is
        /// still assembling a design.
        /// </summary>
        public static GameObject BuildVisualOnlyDrone(Transform parent, DroneLoadout loadout, Team team)
        {
            var preview = new GameObject($"PreviewDrone_{loadout?.designName ?? "Incomplete"}");
            preview.transform.SetParent(parent, worldPositionStays: false);
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.identity;

            if (loadout == null || !loadout.IsComplete)
                return preview;

            DroneRuntimeStats stats = DesignStatsCalculator.Calculate(loadout);
            BuildVisualAndMountedMissiles(preview, loadout, stats, team, rigidbody: null, weapon: null);
            return preview;
        }

        /// <summary>
        /// Depth pass (direct user feedback: "the craft should actually get more
        /// missiles, with multiple being able to be in flight at once with the right
        /// missile tech (seekers)"): how many missiles from the same drone can be
        /// independently guided at once is a real seeker-tech distinction in reality —
        /// a semi-active radar/wire-guided round needs the launching platform's own
        /// continuous guidance for its whole flight (effectively one at a time), while
        /// a true fire-and-forget seeker (active radar, imaging IR, multi-spectral)
        /// needs nothing from the launcher after release, so several can be in the air
        /// simultaneously.
        /// </summary>
        private static int ComputeMaxConcurrentInFlight(SeekerDefinition seeker)
        {
            if (seeker == null)
                return 1;

            switch (seeker.seekerType)
            {
                case SeekerType.ActiveRadar:
                case SeekerType.ImagingInfrared:
                case SeekerType.MultiSpectral:
                    return 4;
                case SeekerType.Infrared:
                case SeekerType.Optical:
                    return 2; // passive, fire-and-forget-ish, but shorter-legged/simpler than the top tier
                case SeekerType.SemiActiveRadar:
                case SeekerType.Laser:
                case SeekerType.WireOrDatalinkGuided:
                case SeekerType.None:
                default:
                    return 1; // needs the launcher's continuous guidance/illumination/LOS for its whole flight
            }
        }

        /// <summary>
        /// The actual "pick the silhouette, color it, mount missiles on hardpoints"
        /// sequence shared by SpawnDrone (a full gameplay entity) and
        /// BuildVisualOnlyDrone (a static Workshop preview). `rigidbody`/`weapon` are
        /// both nullable: a preview has neither (no tilt-on-bank visual, and mounted
        /// missiles never deplete since nothing ever fires), while a real spawned
        /// drone always has a Rigidbody and has a WeaponController whenever its
        /// missile loadout is complete.
        /// </summary>
        private static Transform BuildVisualAndMountedMissiles(GameObject unitRoot, DroneLoadout loadout, DroneRuntimeStats stats,
            Team team, Rigidbody rigidbody, WeaponController weapon)
        {
            // Multirotor airframes (rotorCount > 0, i.e. SmallQuad/Hexacopter) get the
            // arms+spinning-rotors mesh sized to the airframe's actual rotorCount
            // (Phase 2B quadcopter->hexacopter upgrade path); fixed-wing-style
            // airframes (FixedWing/FlyingWingStealth/CcaScale, rotorCount == 0) get
            // the fuselage+wings silhouette instead so a jet drone doesn't spawn
            // looking like a quadcopter. Phase 3B: both builders take the whole
            // loadout (wing shape, hull material finish, rotor material/size, sensor
            // pod, engine glow, and hardpoint sockets are all handled inside them —
            // see DroneVisualBuilder's own doc comment) and hand back the hardpoint
            // sockets mounted missiles attach to.
            bool isMultirotor = loadout.airframe.rotorCount > 0;
            Transform visual;
            Transform[] hardpoints;
            float missileMountScale;
            if (isMultirotor)
                visual = DroneVisualBuilder.BuildMultirotorVisual(unitRoot.transform, loadout, team, out hardpoints, out missileMountScale);
            else
                visual = DroneVisualBuilder.BuildFixedWingVisual(unitRoot.transform, loadout, team, out hardpoints, out missileMountScale);

            if (isMultirotor && rigidbody != null)
            {
                var tilt = unitRoot.AddComponent<QuadcopterTiltVisual>();
                tilt.body = rigidbody;
                tilt.visualRoot = visual;
            }

            // Depth pass: mount exactly the EXTERNALLY-carried portion of the ammo
            // load (DroneRuntimeStats.externallyMountedAmmoCount — internal-bay
            // capacity used up first, per WeaponBayDefinition.internalCapacity's own
            // tooltip), capped to however many hardpoint sockets this airframe
            // actually has. A fully-internal bay (internalCapacity >=
            // effectiveAmmoCount) mounts nothing visible, same end result as the old
            // isInternal flag but now correct for a MIXED bay too (internal capacity
            // filled first, only the overflow shows up externally). MountedMissileVisuals
            // then keeps the visible count falling as a real drone actually fires
            // (Initialize no-ops its event subscription when `weapon` is null, so
            // a static Workshop preview's mounted missiles simply never deplete).
            bool hasMissileLoadout = loadout.missileLoadout != null && loadout.missileLoadout.IsComplete;
            if (hasMissileLoadout && hardpoints.Length > 0 && stats.externallyMountedAmmoCount > 0)
            {
                var mountedVisuals = new List<Transform>();
                int mountCount = Mathf.Min(stats.externallyMountedAmmoCount, hardpoints.Length);
                for (int i = 0; i < mountCount; i++)
                {
                    // Visual-polish pass: bridges the gap between the airframe body and
                    // a hardpoint sitting below it — see BuildPylon's own doc comment.
                    DroneVisualBuilder.BuildPylon(visual, hardpoints[i].localPosition, team, loadout.hullMaterial?.materialType);
                    mountedVisuals.Add(MissileVisualBuilder.Build(hardpoints[i], loadout.missileLoadout, team, scale: missileMountScale));
                }

                if (mountedVisuals.Count > 0)
                    unitRoot.AddComponent<MountedMissileVisuals>().Initialize(weapon, mountedVisuals);
            }

            return visual;
        }
    }
}
