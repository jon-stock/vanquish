using UnityEngine;
using Vanquish.Core;

namespace Vanquish.Combat
{
    /// <summary>
    /// Dev-visibility pass (raised during Phase 2D testing): procedurally colors a
    /// unit's visual mesh by team (bright red enemy / cyan-blue player) instead of
    /// leaving every primitive on Unity's default grey material. On their own, the
    /// tiny prototype primitives (DroneVisualBuilder's fuselage/rotors, VehicleFactory's
    /// missile capsule) are nearly impossible to spot against the grey ground/sky at
    /// real combat distances — color contrast is the cheapest fix that needs no new
    /// art assets, following the same "primitives are fine for now" convention as the
    /// rest of Phase 1/2 visuals (real materials are Phase 3's art pass). A slight
    /// emissive tint is layered on top so units stay readable even when shadowed,
    /// rather than relying on scene lighting alone.
    /// </summary>
    public static class TeamColorUtility
    {
        public static readonly Color PlayerColor = new Color(0.15f, 0.55f, 1f);
        public static readonly Color EnemyColor = new Color(1f, 0.2f, 0.15f);

        private static Material _playerMaterial;
        private static Material _enemyMaterial;

        /// <summary>Assigns the team-appropriate shared material to every Renderer under visualRoot.</summary>
        public static void ApplyTeamColor(Transform visualRoot, Team team)
        {
            if (visualRoot == null)
                return;

            Material material = GetOrCreateMaterial(team);
            foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;
        }

        public static Color GetColor(Team team) => team == Team.Player ? PlayerColor : EnemyColor;

        private static Material GetOrCreateMaterial(Team team)
        {
            if (team == Team.Player)
                return _playerMaterial != null ? _playerMaterial : (_playerMaterial = CreateMaterial(PlayerColor));
            return _enemyMaterial != null ? _enemyMaterial : (_enemyMaterial = CreateMaterial(EnemyColor));
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.35f);
            }
            return material;
        }
    }
}
