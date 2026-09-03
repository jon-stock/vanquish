using UnityEngine;
using Vanquish.Combat;
using Vanquish.Core;
using Vanquish.Data.Drones;
using Vanquish.Data.Missiles;

namespace Vanquish.Workshop
{
    /// <summary>
    /// Phase 3B: the Workshop's live 3D design preview — replaces the old plain-text-
    /// only design readout with an actual rendered model, going through the exact
    /// same VehicleFactory/DroneVisualBuilder/MissileVisualBuilder pipeline combat
    /// uses (via VehicleFactory.BuildVisualOnlyDrone / MissileVisualBuilder.Build
    /// directly), so a design's appearance in the Workshop is guaranteed identical to
    /// how it looks once spawned in combat — not a second hand-maintained preview
    /// implementation. Lives on a dedicated "WorkshopPreviewStage" GameObject
    /// positioned away from the rest of the scene and rendered by a culled-by-layer
    /// preview camera into a RenderTexture that WorkshopController displays in a UI
    /// Toolkit Image element (see Phase1WorkshopSceneBuilder for the camera/layer/
    /// RenderTexture wiring).
    ///
    /// Auto-rotates slowly by default so the model is never a static, hard-to-read
    /// silhouette; pauses while the player drags to manually rotate it (mouse-drag)
    /// and resumes automatically a moment after they release — WorkshopController
    /// forwards the actual pointer/wheel events from the UI (UI Toolkit owns pointer
    /// capture, not this GameObject), calling Rotate/Zoom/BeginDrag/EndDrag here.
    ///
    /// Phase 3B follow-up (direct user feedback/idea): while editing the Missile tab
    /// specifically, the preview swaps from the full strike drone to just the
    /// missile itself, auto-framed close so its seeker nose/fin detail is actually
    /// readable — the drone (and its mounted missiles) would otherwise dominate the
    /// frame at that same zoom level. Switching subjects (SetDroneLoadout <->
    /// SetMissileLoadout) resets zoom/rotation to a sensible default for that
    /// subject; repeated calls for the *same* subject (e.g. every keystroke while
    /// editing) never reset the player's own manual zoom/rotation.
    /// </summary>
    public class WorkshopPreviewStage : MonoBehaviour
    {
        [Tooltip("The child transform that spins — parent of the actual spawned preview model.")]
        public Transform modelPivot;

        [Tooltip("The preview camera dollies along its own forward axis for zoom, rather than " +
            "scaling the model, so lighting/perspective stay correct.")]
        public Transform cameraRig;

        public float autoRotateDegreesPerSecond = 18f;
        public float dragRotateDegreesPerPixel = 0.35f;
        public float resumeAutoRotateAfterSeconds = 2.5f;

        // Planform-preset pass: widened again after the three curated planforms were
        // scaled to real-world-referenced proportions — the largest (Flying-Wing
        // Stealth/X-47B-class) is ~14m span / ~9m long, comfortably clipping through
        // the previous 24m max zoom's framing distance headroom. Multirotor drones
        // (~1.8m across) still zoom in comfortably at the low end of the same range.
        public float minZoomDistance = 2f;
        public float maxZoomDistance = 40f;
        public float zoomSensitivity = 0.6f;

        // Default framing distance/camera height applied whenever the previewed
        // *subject* changes (drone <-> missile) — a missile (~1.5-2.6m long) needs a
        // much closer default than a full aircraft (up to ~14m span, post-planform-
        // preset-pass) to actually be readable rather than a speck in the middle of
        // the frame.
        // Planform-preset pass: distance/height raised from 12/5 to 20/8 (same ~22-
        // degree downward viewing angle as before) so the largest curated planform
        // (Flying-Wing Stealth, ~14m span) is framed with margin instead of nearly
        // filling/clipping through the frame — see Phase1WorkshopSceneBuilder's
        // matching PreviewCamera rig position, which must stay in sync with these.
        private const float DroneFramingDistance = 20f;
        private const float DroneFramingHeight = 8f;
        private const float MissileFramingDistance = 3.5f;
        private const float MissileFramingHeight = 0.3f;

        private enum PreviewSubject { None, Drone, Missile }
        private PreviewSubject _currentSubject = PreviewSubject.None;

        private GameObject _currentModel;
        private DroneLoadout _currentDroneLoadout;
        private MissileLoadout _currentMissileLoadout;
        private float _sinceLastDragSeconds;
        private bool _isDragging;
        private float _cameraDistance = DroneFramingDistance;

        private void Update()
        {
            if (_isDragging)
                return;

            _sinceLastDragSeconds += Time.unscaledDeltaTime;
            if (_sinceLastDragSeconds < resumeAutoRotateAfterSeconds)
                return;

            if (modelPivot != null)
                modelPivot.Rotate(Vector3.up, autoRotateDegreesPerSecond * Time.unscaledDeltaTime, Space.World);
        }

        /// <summary>
        /// Rebuilds the previewed model from scratch if `loadout` actually differs
        /// from what's already shown — WorkshopController calls this every time any
        /// part picker selection changes (Phase 3A's real-time-preview-updates goal),
        /// which for an incomplete/still-being-edited design can be very frequent, so
        /// this is deliberately cheap to call redundantly (no-ops if nothing changed).
        /// Resets zoom/rotation to a sensible default only when switching *to* this
        /// subject from the missile preview, not on every stat-tweak refresh.
        /// </summary>
        public void SetDroneLoadout(DroneLoadout loadout, Team team)
        {
            if (modelPivot == null)
                return;

            if (_currentSubject == PreviewSubject.Drone && ReferenceEquals(loadout, _currentDroneLoadout))
                return;

            bool subjectChanged = _currentSubject != PreviewSubject.Drone;
            _currentSubject = PreviewSubject.Drone;
            _currentDroneLoadout = loadout;
            _currentMissileLoadout = null;

            if (_currentModel != null)
                Object.Destroy(_currentModel);
            _currentModel = VehicleFactory.BuildVisualOnlyDrone(modelPivot, loadout, team);
            SetLayerRecursively(_currentModel, modelPivot.gameObject.layer);

            if (subjectChanged)
                ResetCameraForSubject(DroneFramingDistance, DroneFramingHeight);
        }

        /// <summary>
        /// Same idea as SetDroneLoadout, but for the missile-only close-up view shown
        /// while the Missile designer tab is active — see class doc comment. Builds
        /// directly via MissileVisualBuilder (not VehicleFactory) since a standalone
        /// missile preview has no drone/hardpoint context at all.
        /// </summary>
        public void SetMissileLoadout(MissileLoadout loadout, Team team)
        {
            if (modelPivot == null)
                return;

            if (_currentSubject == PreviewSubject.Missile && ReferenceEquals(loadout, _currentMissileLoadout))
                return;

            bool subjectChanged = _currentSubject != PreviewSubject.Missile;
            _currentSubject = PreviewSubject.Missile;
            _currentMissileLoadout = loadout;
            _currentDroneLoadout = null;

            if (_currentModel != null)
                Object.Destroy(_currentModel);

            if (loadout != null && loadout.IsComplete)
            {
                _currentModel = MissileVisualBuilder.Build(modelPivot, loadout, team).gameObject;
            }
            else
            {
                // Same "empty preview while incomplete" convention as
                // VehicleFactory.BuildVisualOnlyDrone — nothing to show yet, not a crash.
                _currentModel = new GameObject("EmptyMissilePreview");
                _currentModel.transform.SetParent(modelPivot, worldPositionStays: false);
            }
            SetLayerRecursively(_currentModel, modelPivot.gameObject.layer);

            if (subjectChanged)
                ResetCameraForSubject(MissileFramingDistance, MissileFramingHeight);
        }

        /// <summary>
        /// Applies a subject-appropriate default zoom distance/camera height and
        /// resets model rotation to a clean starting angle — only called when the
        /// previewed subject actually changes (drone <-> missile), so the player's
        /// own manual zoom/rotation is never fought while they're mid-edit.
        /// Re-aims the camera rig at the pivot afterward since repositioning it
        /// along its own local Z (see Zoom) can otherwise leave it looking slightly
        /// off-center once the distance changes substantially between subjects.
        /// </summary>
        private void ResetCameraForSubject(float distance, float height)
        {
            _cameraDistance = distance;
            if (modelPivot != null)
                modelPivot.rotation = Quaternion.identity;

            if (cameraRig == null)
                return;
            cameraRig.localPosition = new Vector3(0f, height, -distance);
            cameraRig.LookAt(modelPivot != null ? modelPivot.position : transform.position);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        public void BeginDrag()
        {
            _isDragging = true;
        }

        public void EndDrag()
        {
            _isDragging = false;
            _sinceLastDragSeconds = 0f;
        }

        public void Rotate(float deltaPixelsX)
        {
            if (modelPivot == null)
                return;
            modelPivot.Rotate(Vector3.up, -deltaPixelsX * dragRotateDegreesPerPixel, Space.World);
        }

        public void Zoom(float wheelDelta)
        {
            if (cameraRig == null)
                return;

            _cameraDistance = Mathf.Clamp(_cameraDistance + wheelDelta * zoomSensitivity, minZoomDistance, maxZoomDistance);
            cameraRig.localPosition = new Vector3(0f, cameraRig.localPosition.y, -_cameraDistance);
        }
    }
}
