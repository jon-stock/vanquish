using System.Collections.Generic;
using UnityEngine;
using Vanquish.Core;
using Vanquish.Data.Drones;

namespace Vanquish.Combat
{
    /// <summary>
    /// Procedurally builds a drone's visual mesh entirely from Unity primitives (plus
    /// PrimitiveMeshFactory's hand-authored cone/wing meshes for shapes primitives
    /// can't express) — no imported 3D models needed, matching the project's existing
    /// convention of composing GameObjects from primitives in code.
    ///
    /// Phase 3B: this is where "the model visibly reflects the design" actually
    /// happens (see PLAN.md's Phase 3B goal). Previously almost every part slot was
    /// stat-only with zero visual consequence — wing type, hull material, rotor
    /// material/size, and sensor suite all did nothing to the mesh regardless of
    /// selection. Now: wing type changes the wing's planform (straight/delta/swept),
    /// hull material changes the finish (via TeamColorUtility's hull-aware overload),
    /// rotor material/size changes the rotor blades' scale and finish, and sensor
    /// suite adds a nose pod shaped by whichever sensor (radar/EO-IR/ESM) the suite
    /// leans on hardest. Each Build*Visual method also returns the hardpoint socket
    /// transforms VehicleFactory mounts visible missiles onto, sized to
    /// DroneAirframeDefinition.hardpointCount.
    /// </summary>
    public static class DroneVisualBuilder
    {
        /// <summary>
        /// Fixed-wing/jet-propulsion airframe silhouette (FixedWing, FlyingWingStealth,
        /// CcaScale — anything with rotorCount == 0). Phase 3B follow-up (direct user
        /// feedback: "the flying wing and fixed wing craft look like toy planes rather
        /// than autonomous drones — think X-47B, Predator, not a Christmas cracker"):
        /// this now branches on DroneAirframeDefinition.airframeClass, not just
        /// wingOrPropeller.liftSurfaceType — previously every fixed-wing-family
        /// airframe class got the exact same fuselage-capsule + flat-slab-tailplane
        /// silhouette regardless of which of the three classes was actually selected.
        /// FlyingWingStealth now has no separate fuselage/tail at all (the wing *is*
        /// the airframe, like a real flying wing); CcaScale gets a flat blended
        /// wing-body with no vertical tail (X-47B-style tailless UCAV); FixedWing
        /// keeps a real fuselage but thinner/longer with a canted V-tail instead of a
        /// flat slab, and longer/thinner (higher-aspect-ratio) wings, closer to a
        /// Predator/MALE silhouette than a toy biplane tail. A nose sensor pod (if a
        /// sensor suite is equipped) and one engine glow (if propulsion requires
        /// forward flight) are added after, positioned per body style. Returns the
        /// hardpoint sockets under the wings.
        /// </summary>
        // Planform-preset pass: real dimensions now come from the design's own
        // DroneAirframeDefinition.wingSpanMeters/fuselageLengthMeters (real-world-
        // referenced per planform — a Fury/YFQ-44A-class conventional fighter, a
        // Gambit-class twin-tail recon airframe, and an X-47B-class flying wing are
        // all genuinely different sizes in reality, not one shared "fixed-wing"
        // constant) instead of the old flat 6m/4m default every fixed-wing design
        // used regardless of which real aircraft it was meant to resemble. The
        // 6f/4f fallback below only fires for a null/incomplete loadout (e.g. a
        // mid-assembly Workshop preview).
        public static Transform BuildFixedWingVisual(Transform parent, DroneLoadout loadout, Team team,
            out Transform[] hardpoints, out float missileMountScale)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            DroneAirframeClass airframeClass = loadout?.airframe != null ? loadout.airframe.airframeClass : DroneAirframeClass.FixedWing;
            WingOrPropellerDefinition wing = loadout?.wingOrPropeller;
            float wingSpan = loadout?.airframe != null ? loadout.airframe.wingSpanMeters : 6f;
            float fuselageLength = loadout?.airframe != null ? loadout.airframe.fuselageLengthMeters : 4f;

            // Phase 3B follow-up (direct user feedback, screenshot evidence: mounted
            // missiles rendering as two separate blobs way out past the wingtips,
            // connected by what reads as a thin, barely-visible line): these were
            // 0.48-0.58 * wingSpan — i.e. hardpoints sat almost exactly at the
            // wingtip. Real underwing pylons (Predator/Reaper's Hellfires included)
            // sit inboard, close to the fuselage/wing root, not at the very tip.
            // Moved to 0.14-0.22 * wingSpan so mounted missiles read as attached to
            // the aircraft's body, not as detached objects floating at its extremes.
            float noseZ = fuselageLength * 0.5f;
            float hardpointHalfSpan;
            switch (airframeClass)
            {
                case DroneAirframeClass.FlyingWingStealth:
                    BuildFlyingWingBody(root.transform, wing, wingSpan, fuselageLength);
                    hardpointHalfSpan = wingSpan * 0.2f;
                    break;

                case DroneAirframeClass.CcaScale:
                    BuildBlendedWingBody(root.transform, wing, wingSpan, fuselageLength);
                    hardpointHalfSpan = wingSpan * 0.16f;
                    break;

                case DroneAirframeClass.FixedWing:
                case DroneAirframeClass.Hexacopter: // shouldn't reach this method (rotorCount > 0), fallback only
                case DroneAirframeClass.SmallQuad: // same
                default:
                    BuildConventionalFuselageAndWing(root.transform, wing, wingSpan, fuselageLength);
                    hardpointHalfSpan = wingSpan * 0.14f;
                    break;
            }

            // Team + hull-material finish applied now, before the sensor pod exists —
            // the pod gets its own distinct (non-team) material afterward, same
            // ordering convention MissileVisualBuilder uses for its seeker nose.
            TeamColorUtility.ApplyTeamColor(root.transform, team, loadout?.hullMaterial?.materialType);

            // Visual-polish pass: pulled back from noseZ exactly — that put the sensor
            // pod sphere co-located with the conventional body's nose-cone apex,
            // overlapping/z-fighting with it instead of reading as a distinct chin/nose
            // sensor turret.
            BuildSensorPod(root.transform, loadout?.sensorSuite, new Vector3(0f, 0.05f, noseZ * 0.82f));

            bool requiresForwardFlight = loadout?.propulsion != null && loadout.propulsion.requiresForwardFlight;
            if (requiresForwardFlight)
                AddEngineGlow(root.transform, new Vector3(0f, 0.03f, -noseZ * 0.85f), TeamColorUtility.GetColor(team));

            // Visual-polish pass: the vertical/longitudinal hardpoint offset used to be
            // a flat -0.1f/-0.05f regardless of aircraft size — comfortable clearance
            // under the old ~2m-scale bodies, but nowhere near enough belly clearance
            // once fuselages scaled up to real-world dimensions (a mounted missile
            // visibly clipped into the fuselage box). Now scales with fuselageLength.
            int hardpointCount = loadout?.airframe != null ? loadout.airframe.hardpointCount : 0;
            hardpoints = CreateHardpointSockets(root.transform, hardpointCount,
                halfSpanX: hardpointHalfSpan, y: -fuselageLength * 0.06f, z: -fuselageLength * 0.02f);

            // noseZ*2 is a reasonable proxy for "how big is this aircraft" regardless
            // of body style (see ComputeMissileMountScale's own doc comment for why
            // this exists at all).
            missileMountScale = ComputeMissileMountScale(noseZ * 2f);

            return root.transform;
        }

        /// <summary>
        /// FlyingWingStealth (X-47B-class, e.g. the "Flying-Wing Stealth" planform
        /// preset): no fuselage pod and no tail at all — the wing itself is the
        /// entire airframe, matching a real tailless flying-wing UCAV. Planform-preset
        /// pass: root chord is now derived from the design's actual
        /// fuselageLengthMeters (a tailless flying wing's centerline chord IS
        /// effectively its body length, not a separate fuselage bolted onto a wing),
        /// the wing itself uses whichever mesh liftSurfaceType implies — the curated
        /// preset pairs this airframe with LiftSurfaceType.FlyingWing, giving a real
        /// cranked/kite double-delta planform (see PrimitiveMeshFactory.CreateKiteWing)
        /// instead of a plain triangle — and a low, broad dorsal hump toward the front
        /// third stands in for the real X-47B's distinctive top-mounted engine air
        /// intake, the single most recognisable non-wing detail on the real aircraft.
        /// </summary>
        private static void BuildFlyingWingBody(Transform parent, WingOrPropellerDefinition wing, float wingSpan, float fuselageLength)
        {
            float rootChord = fuselageLength * 0.7f;
            BuildWing(parent, "MainWing", wing, wingSpan, rootChord, position: Vector3.zero, thickness: rootChord * 0.09f);

            GameObject hump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hump.name = "DorsalIntakeHump";
            DestroyCollider(hump);
            hump.transform.SetParent(parent, worldPositionStays: false);
            hump.transform.localPosition = new Vector3(0f, rootChord * 0.1f, fuselageLength * 0.16f);
            hump.transform.localScale = new Vector3(wingSpan * 0.09f, rootChord * 0.14f, fuselageLength * 0.3f);
        }

        /// <summary>
        /// CcaScale (Gambit-class twin-tail recon/combat airframe): a flat, wide,
        /// low-profile fuselage blended tightly against the wing (minimal vertical
        /// gap), paired with a cranked/swept wing planform and — unlike the fully
        /// tailless FlyingWingStealth/X-47B silhouette above — a pair of small
        /// outward-canted tails for yaw stability, matching the reference twin-tail
        /// recon/air-to-air variants rather than the earlier "no tail at all" version
        /// of this body style (which conflated every tailless-looking UCAV into one
        /// silhouette; the planform-preset pass is specifically about telling these
        /// apart).
        /// </summary>
        private static void BuildBlendedWingBody(Transform parent, WingOrPropellerDefinition wing, float wingSpan, float fuselageLength)
        {
            GameObject fuselage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fuselage.name = "Fuselage";
            DestroyCollider(fuselage);
            fuselage.transform.SetParent(parent, worldPositionStays: false);
            fuselage.transform.localPosition = new Vector3(0f, -0.02f, fuselageLength * 0.04f);
            fuselage.transform.localScale = new Vector3(fuselageLength * 0.22f, fuselageLength * 0.08f, fuselageLength * 0.62f);

            float rootChord = fuselageLength * 0.6f;
            BuildWing(parent, "MainWing", wing, wingSpan, rootChord, position: new Vector3(0f, -0.02f, -fuselageLength * 0.05f), thickness: fuselageLength * 0.045f);

            BuildTwinCantedTails(parent, fuselageLength, lateralOffset: fuselageLength * 0.12f,
                spanFraction: 0.08f, chordFraction: 0.07f, cantDegrees: 40f);
        }

        /// <summary>
        /// Conventional FixedWing (Fury/YFQ-44A/"Brontanax"-class CCA fighter): a
        /// flattened, chined-look fuselage (wider than tall, built from stretched
        /// cubes rather than a round capsule — a capsule cannot read as "chined")
        /// capped with a pointed nose cone, wings positioned closer to mid-fuselage
        /// (a real fighter's wing root sits roughly at/just aft of the aircraft's
        /// midpoint, not glued to the very front third), and a canted twin tail.
        ///
        /// Visual-polish pass (direct user feedback: "a cereal box with wings"): the
        /// body used to be a single uniform-cross-section box — a real fighter
        /// fuselage necks down toward the tail (engine/tail-boom section is
        /// noticeably slimmer than the avionics/cabin section forward of it). Split
        /// into two segments — a wider forward body and a narrower aft boom — which
        /// breaks up the flat-sided-brick silhouette without needing a full lofted
        /// fuselage mesh.
        /// </summary>
        private static void BuildConventionalFuselageAndWing(Transform parent, WingOrPropellerDefinition wing, float wingSpan, float fuselageLength)
        {
            float bodyLength = fuselageLength * 0.72f;
            float frontFaceZ = fuselageLength * 0.28f; // flush against the nose cone's base, see below
            float forwardLength = bodyLength * 0.58f;
            float boomLength = bodyLength - forwardLength;

            GameObject forwardBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            forwardBody.name = "Fuselage";
            DestroyCollider(forwardBody);
            forwardBody.transform.SetParent(parent, worldPositionStays: false);
            forwardBody.transform.localPosition = new Vector3(0f, 0f, frontFaceZ - forwardLength * 0.5f);
            forwardBody.transform.localScale = new Vector3(fuselageLength * 0.16f, fuselageLength * 0.1f, forwardLength);

            float boomFrontZ = frontFaceZ - forwardLength;
            GameObject aftBoom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aftBoom.name = "AftBoom";
            DestroyCollider(aftBoom);
            aftBoom.transform.SetParent(parent, worldPositionStays: false);
            aftBoom.transform.localPosition = new Vector3(0f, 0f, boomFrontZ - boomLength * 0.5f);
            aftBoom.transform.localScale = new Vector3(fuselageLength * 0.1f, fuselageLength * 0.065f, boomLength);

            GameObject nose = new GameObject("NoseCone");
            nose.transform.SetParent(parent, worldPositionStays: false);
            nose.transform.localPosition = new Vector3(0f, 0f, frontFaceZ);
            nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            nose.AddComponent<MeshFilter>().sharedMesh = PrimitiveMeshFactory.CreateCone(fuselageLength * 0.08f, fuselageLength * 0.22f);
            nose.AddComponent<MeshRenderer>();

            float rootChord = fuselageLength * 0.26f;
            BuildWing(parent, "MainWing", wing, wingSpan, rootChord, position: new Vector3(0f, -0.02f, -fuselageLength * 0.06f), thickness: fuselageLength * 0.03f);

            BuildTwinCantedTails(parent, fuselageLength, lateralOffset: fuselageLength * 0.12f,
                spanFraction: 0.09f, chordFraction: 0.08f, cantDegrees: 35f);
        }

        /// <summary>
        /// A pair of outward-canted vertical fins (per the reference fighter/recon
        /// silhouettes — a single flat slab tailplane was the biggest "toy biplane"
        /// tell in an earlier pass). Shared by BuildConventionalFuselageAndWing and
        /// BuildBlendedWingBody with different proportions/cant angles per body
        /// style; FlyingWingStealth/X-47B uses neither (fully tailless).
        ///
        /// Visual-polish pass (direct user feedback: "they all look super janky" —
        /// screenshot evidence showed a big X/bowtie slapped across the middle of
        /// every fixed-wing silhouette): the first version placed both fins at
        /// identical X=0 (only their *rotation* differed, +cant/-cant), which is a
        /// real Bonanza-style single-root V-tail configuration, but combined with a
        /// too-large finHeight (up to 18% of fuselage length) it read as a giant X
        /// crossing through the fuselage rather than a subtle control surface — and
        /// none of the reference images (Fury/Gambit/"Brontanax") actually use a
        /// single-root V-tail; they all show two separate fins mounted apart,
        /// each canted outward from its own root. Fixed both: fins are now offset
        /// sideways by `lateralOffset` before being canted, and are roughly half the
        /// previous size.
        /// </summary>
        private static void BuildTwinCantedTails(Transform parent, float fuselageLength, float lateralOffset,
            float spanFraction, float chordFraction, float cantDegrees)
        {
            float finHeight = fuselageLength * spanFraction;
            float finChord = fuselageLength * chordFraction;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fin.name = "TailFin";
                DestroyCollider(fin);
                fin.transform.SetParent(parent, worldPositionStays: false);
                // Position (with the lateral offset already applied) is set BEFORE
                // rotation is considered here — Unity rotates the fin's own mesh
                // around this already-offset pivot, so the two fins end up canted
                // outward from two genuinely separate roots, not meeting at a single
                // central point.
                fin.transform.localPosition = new Vector3(side * lateralOffset, finHeight * 0.5f, -fuselageLength * 0.42f);
                fin.transform.localRotation = Quaternion.Euler(0f, 0f, side * cantDegrees);
                fin.transform.localScale = new Vector3(fuselageLength * 0.015f, finHeight, finChord);
            }
        }

        /// <summary>
        /// Builds one wing (mirrored left/right) whose planform reflects
        /// liftSurfaceType: FixedWing stays the original straight rectangular slab
        /// (a Cube, exactly as before Phase 3B); DeltaWing/VariableSweepWing use
        /// PrimitiveMeshFactory's tapered/swept wing mesh, since a cube fundamentally
        /// cannot represent a triangular/swept planform; FlyingWing (planform-preset
        /// pass) uses CreateKiteWing's cranked/double-delta hexagonal mesh, matching
        /// the X-47B-class planform's distinctive "broad-shouldered" leading-edge
        /// crank that a plain single-sweep delta can't represent. Propeller-type wing
        /// parts reaching this method (a mismatched pairing the data model doesn't
        /// actually forbid — see DroneAirframeDefinition's doc comment on rotorCount)
        /// fall back to the straight wing rather than building something nonsensical.
        /// </summary>
        private static void BuildWing(Transform parent, string name, WingOrPropellerDefinition wing,
            float span, float rootChord, Vector3 position, float thickness = 0.06f)
        {
            LiftSurfaceType liftSurfaceType = wing != null ? wing.liftSurfaceType : LiftSurfaceType.FixedWing;

            switch (liftSurfaceType)
            {
                case LiftSurfaceType.DeltaWing:
                    BuildMeshWing(parent, name, position,
                        PrimitiveMeshFactory.CreateTaperedWing(span, rootChord * 1.4f, tipChordLength: 0.02f,
                            sweepBack: rootChord * 0.9f, thickness: thickness));
                    return;

                case LiftSurfaceType.VariableSweepWing:
                    // Visual-polish pass: sweepBack was 1.6x rootChord — bigger than the
                    // wing's own root chord — which pushed the tip's trailing edge far
                    // beyond the root's, roughly doubling the wing's actual front-to-back
                    // footprint versus what the airframe's own fuselageLength implied
                    // (measured: a CcaScale design's wing alone spanned ~9.6m of depth
                    // against an intended ~8m fuselage). Reduced to a sweep that keeps
                    // the tip's trailing edge within a sane multiple of the root chord,
                    // matching DeltaWing's (already-correct) proportions.
                    BuildMeshWing(parent, name, position,
                        PrimitiveMeshFactory.CreateTaperedWing(span, rootChord * 1.1f, tipChordLength: rootChord * 0.3f,
                            sweepBack: rootChord * 1.0f, thickness: thickness));
                    return;

                case LiftSurfaceType.FlyingWing:
                    // Visual-polish pass: sweep distances are now fractions of the actual
                    // SPAN traveled at each segment (not the root chord) — sweepback is
                    // fundamentally a function of how far you move spanwise, and scaling
                    // it off rootChord (the first version of this) blew the wing's total
                    // depth out to ~1.7x the airframe's own fuselageLength (measured:
                    // ~16m of wing depth against a 9m-long airframe).
                    float halfSpanForKite = span * 0.5f;
                    float crankSpanDist = halfSpanForKite * 0.4f;
                    float tipSpanDist = halfSpanForKite - crankSpanDist;
                    BuildMeshWing(parent, name, position,
                        PrimitiveMeshFactory.CreateKiteWing(span, rootChordLength: rootChord, crankSpanFraction: 0.4f,
                            inboardSweepBack: crankSpanDist * 1.9f, crankChordLength: rootChord * 0.32f,
                            outboardSweepBack: tipSpanDist * 0.85f, tipChordLength: rootChord * 0.06f, thickness: thickness));
                    return;

                case LiftSurfaceType.FixedWing:
                case LiftSurfaceType.Propeller: // mismatched pairing fallback, see method doc comment
                default:
                    GameObject mainWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    mainWing.name = name;
                    DestroyCollider(mainWing);
                    mainWing.transform.SetParent(parent, worldPositionStays: false);
                    mainWing.transform.localPosition = position;
                    mainWing.transform.localScale = new Vector3(span, thickness, rootChord);
                    return;
            }
        }

        private static void BuildMeshWing(Transform parent, string name, Vector3 position, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.identity;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
        }

        /// <summary>
        /// Multirotor airframe silhouette (SmallQuad/Hexacopter — rotorCount > 0): a
        /// central body, N arms in an "X" configuration sized to the airframe's actual
        /// rotorCount (Phase 2B quadcopter->hexacopter upgrade path), each with a
        /// spinning rotor blade whose scale/finish now reflects
        /// loadout.wingOrPropeller's RotorSize/RotorMaterial (Phase 3B — previously
        /// pure stats with zero visual consequence), a nose sensor pod, and hardpoint
        /// sockets under the body sized to hardpointCount. No engine glow — the
        /// spinning rotors are already this airframe class's visual "it's powered on"
        /// tell.
        /// </summary>
        public static Transform BuildMultirotorVisual(Transform parent, DroneLoadout loadout, Team team,
            out Transform[] hardpoints, out float missileMountScale, float armLength = 0.9f)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            BuildBody(root.transform);

            int rotorCount = loadout?.airframe != null ? loadout.airframe.rotorCount : 4;
            int clampedRotorCount = Mathf.Max(3, rotorCount);

            RotorSize rotorSize = loadout?.wingOrPropeller != null ? loadout.wingOrPropeller.rotorSize : RotorSize.Medium;
            float rotorSizeScale = rotorSize switch
            {
                RotorSize.Small => 0.7f,
                RotorSize.Large => 1.35f,
                _ => 1f,
            };

            float angleStep = 360f / clampedRotorCount;
            for (int i = 0; i < clampedRotorCount; i++)
            {
                // Start at 45 degrees for an "X" configuration (arms between the body's
                // forward/back/left/right axes), the common FPV/multirotor look.
                float angleDeg = 45f + i * angleStep;
                BuildArmAndRotor(root.transform, angleDeg, armLength, rotorSizeScale);
            }

            TeamColorUtility.ApplyTeamColor(root.transform, team, loadout?.hullMaterial?.materialType);

            RotorMaterial rotorMaterial = loadout?.wingOrPropeller != null ? loadout.wingOrPropeller.rotorMaterial : RotorMaterial.Plastic;
            ApplyRotorFinish(root.transform, rotorMaterial);

            BuildSensorPod(root.transform, loadout?.sensorSuite, new Vector3(0f, 0.08f, 0.32f));

            int hardpointCount = loadout?.airframe != null ? loadout.airframe.hardpointCount : 0;
            hardpoints = CreateHardpointSockets(root.transform, hardpointCount,
                halfSpanX: armLength * 0.5f, y: -0.18f, z: 0f);

            // armLength*2 ~= rotor-to-rotor diameter — this platform's own
            // "how big is it" measure, for the same reason BuildFixedWingVisual
            // computes one (see ComputeMissileMountScale's doc comment).
            missileMountScale = ComputeMissileMountScale(armLength * 2f);

            return root.transform;
        }

        /// <summary>
        /// Phase 3B follow-up (direct user feedback, screenshot evidence: a small
        /// quadcopter mounting missiles at their "realistic" ~1.5-2.6m length looked
        /// like two giant rods swallowing the whole drone — the missile-scale-up done
        /// for realism made mounted missiles look correct on a large fixed-wing
        /// aircraft but absurd on a small multirotor). Rather than a flat mount scale
        /// for every carrier (the previous 0.85 constant), this scales a mounted
        /// missile so its displayed full length is a fixed fraction of the carrier's
        /// own characteristic size — a tiny quad gets proportionally tiny mounted
        /// missiles, a large fixed-wing aircraft gets proportionally larger ones,
        /// same as real aircraft ordnance always reads much smaller than the plane
        /// carrying it. Not a full fix for the size-class mismatch noted in
        /// MissileVisualBuilder's own doc comment (a small quad's missile is still
        /// the *same* MissileLoadout/stats as a big aircraft's, just drawn smaller
        /// here) — just makes the visual proportion sane regardless of carrier.
        /// </summary>
        private static float ComputeMissileMountScale(float carrierCharacteristicLength)
        {
            const float targetFractionOfCarrier = 0.45f;
            const float nominalMissileFullLength = 2f; // rough midpoint of MissileVisualBuilder's ~1.5-2.6m range
            // Planform-preset pass: the upper clamp was 1f (never draw a mounted
            // missile larger than its own "realistic" ~2.6m stat-implied length) —
            // fine for the old ~6-8m fixed-wing bodies, but a real Fury/YFQ-44A-class
            // fighter (~6.1m) carries AIM-120s (~3.7m) at roughly 60% of its own body
            // length, which needs scale ~1.8-2 to actually reach. Raised to 2.2f so
            // the now real-world-sized fixed-wing planforms can show believably large
            // underslung munitions instead of clamping to a comparatively tiny 2.6m
            // missile; the low-end 0.15f clamp (protecting tiny multirotors) is unchanged.
            float targetFullLength = carrierCharacteristicLength * targetFractionOfCarrier;
            return Mathf.Clamp(targetFullLength / nominalMissileFullLength, 0.15f, 2.2f);
        }

        private static void BuildBody(Transform parent)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            DestroyCollider(body);
            body.transform.SetParent(parent, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.6f, 0.25f, 0.6f);
        }

        private static void BuildArmAndRotor(Transform parent, float angleDeg, float armLength, float rotorSizeScale)
        {
            Quaternion armRotation = Quaternion.Euler(0f, angleDeg, 0f);
            Vector3 tipPosition = armRotation * Vector3.forward * armLength;

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            DestroyCollider(arm);
            arm.transform.SetParent(parent, worldPositionStays: false);
            arm.transform.localRotation = armRotation;
            arm.transform.localPosition = tipPosition * 0.5f;
            arm.transform.localScale = new Vector3(0.08f, 0.05f, armLength);

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "RotorHub";
            DestroyCollider(hub);
            hub.transform.SetParent(parent, worldPositionStays: false);
            hub.transform.localPosition = tipPosition + Vector3.up * 0.05f;
            hub.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f) * rotorSizeScale;

            // Spin pivot is a sibling of the hub (not its child) so it doesn't inherit
            // the hub's non-uniform scale when positioning/sizing the blade.
            var spinPivot = new GameObject("RotorSpin");
            spinPivot.transform.SetParent(parent, worldPositionStays: false);
            spinPivot.transform.localPosition = tipPosition + Vector3.up * 0.08f;
            var spinner = spinPivot.AddComponent<RotorSpinner>();
            spinner.degreesPerSecond = 1600f;

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blades";
            DestroyCollider(blade);
            blade.transform.SetParent(spinPivot.transform, worldPositionStays: false);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localScale = new Vector3(armLength * 0.55f * rotorSizeScale, 0.01f, 0.05f * rotorSizeScale);
        }

        /// <summary>
        /// Retints just the rotor hubs/blades (matched by GameObject name — see
        /// BuildArmAndRotor) after the blanket team-color pass already ran, so rotor
        /// material choice (Plastic/CarbonFiber/Metal) is visible without fighting
        /// the team-color material assignment. Real-world-motivated: Plastic reads
        /// as a flat/cheap colored finish, CarbonFiber as a dark low-gloss weave,
        /// Metal as bright polished blades.
        /// </summary>
        private static void ApplyRotorFinish(Transform visualRoot, RotorMaterial rotorMaterial)
        {
            Color color;
            float metallic;
            float smoothness;
            switch (rotorMaterial)
            {
                case RotorMaterial.CarbonFiber:
                    color = new Color(0.08f, 0.08f, 0.09f);
                    metallic = 0.2f;
                    smoothness = 0.45f;
                    break;
                case RotorMaterial.Metal:
                    color = new Color(0.75f, 0.76f, 0.78f);
                    metallic = 0.9f;
                    smoothness = 0.7f;
                    break;
                default: // Plastic
                    color = new Color(0.85f, 0.85f, 0.82f);
                    metallic = 0.05f;
                    smoothness = 0.3f;
                    break;
            }

            Material material = CreateDetailMaterial(color, metallic, smoothness);
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name == "RotorHub" || renderer.name == "Blades")
                    renderer.sharedMaterial = material;
            }
        }

        /// <summary>
        /// Phase 3B: a nose sensor "pod" whose shape reflects whichever of the
        /// SensorSuiteDefinition's three range stats dominates — SensorSuiteDefinition
        /// has no discrete sensorType enum, so this is the closest approximation to
        /// "the sensor suite you picked visibly changes the nose": a radar-dominant
        /// suite gets a rounded radome bump, an EO/IR-dominant suite gets a camera/
        /// gimbal-ball look (directly answering the original ask — "a camera on the
        /// front rather than a sensor" — for scout drones, whose sensor suites lean
        /// heavily on eoIrRangeMeters), and an ESM-dominant suite gets a thin antenna
        /// blade instead of a dome at all (a passive RF antenna, not an optic). Slight
        /// size scaling by the dominant range value (capped) so a longer-ranged sensor
        /// reads as a physically bigger pod, not just a better number.
        /// </summary>
        private static void BuildSensorPod(Transform parent, SensorSuiteDefinition sensor, Vector3 localPosition)
        {
            if (sensor == null)
                return;

            float radar = sensor.radarRangeMeters;
            float eoIr = sensor.eoIrRangeMeters;
            float esm = sensor.esmRangeMeters;

            if (radar <= 0f && eoIr <= 0f && esm <= 0f)
                return; // no meaningful sensor data configured — nothing to show

            if (esm >= radar && esm >= eoIr)
            {
                BuildEsmAntenna(parent, localPosition, esm);
                return;
            }

            bool eoIrDominant = eoIr >= radar;
            float dominantRange = eoIrDominant ? eoIr : radar;
            float sizeScale = Mathf.Clamp(0.08f + dominantRange / 20000f, 0.08f, 0.16f);

            GameObject pod = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pod.name = eoIrDominant ? "SensorPod_Camera" : "SensorPod_Radar";
            DestroyCollider(pod);
            pod.transform.SetParent(parent, worldPositionStays: false);
            pod.transform.localPosition = localPosition;
            pod.transform.localScale = Vector3.one * sizeScale;

            Material podMaterial = eoIrDominant
                ? CreateDetailMaterial(new Color(0.05f, 0.05f, 0.06f), metallic: 0.3f, smoothness: 0.75f) // camera/EO ball: dark glassy sphere
                : CreateDetailMaterial(new Color(0.7f, 0.7f, 0.66f), metallic: 0.05f, smoothness: 0.3f); // radome bump: light matte
            pod.GetComponent<Renderer>().sharedMaterial = podMaterial;

            if (eoIrDominant)
            {
                // A small lighter "lens ring" accent in front of the dark ball, so it
                // reads unmistakably as a camera/EO turret rather than a plain sphere.
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "SensorPod_LensRing";
                DestroyCollider(ring);
                ring.transform.SetParent(parent, worldPositionStays: false);
                ring.transform.localPosition = localPosition + new Vector3(0f, 0f, sizeScale * 0.85f);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.transform.localScale = new Vector3(sizeScale * 0.9f, 0.01f, sizeScale * 0.9f);
                ring.GetComponent<Renderer>().sharedMaterial = CreateDetailMaterial(new Color(0.6f, 0.75f, 0.9f), metallic: 0.4f, smoothness: 0.9f);
            }
        }

        private static void BuildEsmAntenna(Transform parent, Vector3 localPosition, float esmRange)
        {
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "SensorPod_EsmAntenna";
            DestroyCollider(blade);
            blade.transform.SetParent(parent, worldPositionStays: false);
            blade.transform.localPosition = localPosition + new Vector3(0f, 0.05f, 0f);
            blade.transform.localRotation = Quaternion.Euler(20f, 0f, 0f); // slight forward rake, like a real RWR blade antenna
            float length = Mathf.Clamp(0.1f + esmRange / 15000f, 0.1f, 0.22f);
            blade.transform.localScale = new Vector3(0.02f, 0.02f, length);
            blade.GetComponent<Renderer>().sharedMaterial = CreateDetailMaterial(new Color(0.15f, 0.15f, 0.16f), metallic: 0.4f, smoothness: 0.4f);
        }

        /// <summary>
        /// Evenly-spaced hardpoint sockets along the local X axis, sized to
        /// `count` (DroneAirframeDefinition.hardpointCount) — empty child transforms
        /// VehicleFactory parents visible mounted-missile visuals to, one per
        /// currently-loaded round of ammo. Returns an empty array (never null) when
        /// count &lt;= 0 so callers never need a null check.
        ///
        /// Visual-polish pass (direct user feedback, screenshot evidence: a
        /// 6-hardpoint CCA-scale design carrying only 2 missiles mounted both of them
        /// on the same wing): VehicleFactory always fills `hardpoints[0..mountCount)`
        /// when mounting fewer missiles than an airframe has hardpoints. The array
        /// used to be strictly left-to-right, so hardpoints 0 and 1 out of 6 were both
        /// the two *leftmost* positions — visibly lopsided. Reordered center-out,
        /// alternating sides (innermost pair first, then the next pair out, etc.) so
        /// any prefix of the array is bilaterally symmetric: 2 missiles use the
        /// innermost pair, 4 use the innermost two pairs, and so on. A 1- or 2-count
        /// airframe is unaffected (already symmetric either way).
        /// </summary>
        private static Transform[] CreateHardpointSockets(Transform parent, int count, float halfSpanX, float y, float z)
        {
            if (count <= 0)
                return System.Array.Empty<Transform>();

            var xPositions = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                xPositions[i] = Mathf.Lerp(-halfSpanX, halfSpanX, t);
            }

            var order = new List<int>(count);
            bool hasCenter = count % 2 == 1;
            int centerIndex = count / 2;
            if (hasCenter)
                order.Add(centerIndex);

            int left = hasCenter ? centerIndex - 1 : centerIndex - 1;
            int right = hasCenter ? centerIndex + 1 : centerIndex;
            while (order.Count < count)
            {
                order.Add(left);
                order.Add(right);
                left--;
                right++;
            }

            var sockets = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                float x = xPositions[order[i]];
                var socket = new GameObject($"Hardpoint_{i}");
                socket.transform.SetParent(parent, worldPositionStays: false);
                socket.transform.localPosition = new Vector3(x, y, z);
                socket.transform.localRotation = Quaternion.identity;
                sockets[i] = socket.transform;
            }
            return sockets;
        }

        /// <summary>
        /// A thin vertical strut bridging from the airframe's body surface (assumed
        /// to sit near local Y=0, matching every body-builder's own convention above)
        /// down to a mounted missile's hardpoint. Visual-polish pass (direct user
        /// feedback: a mounted missile on a thin flying-wing body — nothing else
        /// nearby to visually anchor it to — read as "a mysterious structure floating
        /// underneath" rather than an attached weapon, since hardpoint sockets are
        /// bare empty transforms with no pylon/rack geometry connecting them to the
        /// airframe at all). No-ops for a hardpoint with negligible vertical drop.
        /// </summary>
        public static void BuildPylon(Transform visualRoot, Vector3 hardpointLocalPosition, Team team, HullMaterialType? hullMaterial)
        {
            float drop = Mathf.Abs(hardpointLocalPosition.y);
            if (drop < 0.02f)
                return;

            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pylon.name = "Pylon";
            DestroyCollider(pylon);
            pylon.transform.SetParent(visualRoot, worldPositionStays: false);
            pylon.transform.localPosition = new Vector3(hardpointLocalPosition.x, hardpointLocalPosition.y * 0.5f, hardpointLocalPosition.z);
            float thickness = Mathf.Max(0.04f, drop * 0.3f);
            pylon.transform.localScale = new Vector3(thickness, drop, thickness);
            TeamColorUtility.ApplyTeamColor(pylon.transform, team, hullMaterial);
        }

        /// <summary>
        /// Dev-visibility pass (Phase 2D): a small bright emissive core (plus a
        /// matching Light for actual scene illumination, not just an unlit-looking
        /// bright material) at a thrusting unit's engine position — missiles and
        /// fixed-wing/jet drones, not multirotors (their rotors are already the visual
        /// tell). Without this, a launched missile is a dim 0.4m-wide grey capsule with
        /// nothing to catch the eye before it's already close — see PLAN.md's Phase 2D
        /// technical notes for the full "why draw distance felt bad" writeup. No art
        /// assets: a scaled-down sphere primitive with its collider stripped, same
        /// "primitives for now" convention as the rest of this class.
        /// </summary>
        public static void AddEngineGlow(Transform parent, Vector3 localPosition, Color color, float coreScale = 0.18f)
        {
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "EngineGlow";
            DestroyCollider(core);
            core.transform.SetParent(parent, worldPositionStays: false);
            core.transform.localPosition = localPosition;
            core.transform.localScale = Vector3.one * coreScale;

            var renderer = core.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateDetailMaterial(color, metallic: 0f, smoothness: 0.5f, emissive: true);

            var glowLight = core.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = color;
            glowLight.range = 12f;
            glowLight.intensity = 3f;
        }

        private static Material CreateDetailMaterial(Color color, float metallic, float smoothness, bool emissive = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            else if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);

            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f); // well above 1.0 so it visually pops even in daylight
            }
            return material;
        }

        private static void DestroyCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
