using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vanquish.EditorTools
{
    /// <summary>
    /// Interactive dev-testing menu for CombatTestSceneBuilder: lets you add/remove
    /// enemy groups (archetype + armed/unarmed + count) and build a combat scene with
    /// that exact composition, without touching any code or the fixed Phase 1 MVP
    /// arena. This is the "can it be a menu, so counts/archetypes are selectable"
    /// dev-testing tool for Phase 2D (and beyond — every future archetype just needs a
    /// case added to CombatTestSceneBuilder's spawn switch to show up here too).
    /// </summary>
    public class CombatTestSceneBuilderWindow : EditorWindow
    {
        private readonly List<EnemySpawnGroup> _enemyGroups = new List<EnemySpawnGroup>
        {
            new EnemySpawnGroup { archetype = TestArchetype.Interceptor, armed = true, count = 1 },
        };

        private string _scenePath = CombatTestSceneBuilder.DefaultTestScenePath;
        private Vector2 _scrollPosition;

        [MenuItem("Vanquish/Debug/Combat Test Scene Builder")]
        public static void Open()
        {
            var window = GetWindow<CombatTestSceneBuilderWindow>("Combat Test Scene");
            window.minSize = new Vector2(360f, 240f);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Builds a combat test scene (player + friendly scout, fixed) against a " +
                "custom enemy roster. Use this to exercise a new AI archetype live " +
                "without editing the Phase 1 MVP arena or any code.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Roster", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            int groupToRemove = -1;
            for (int i = 0; i < _enemyGroups.Count; i++)
            {
                EnemySpawnGroup group = _enemyGroups[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Group {i + 1}", GUILayout.Width(60f));
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    groupToRemove = i;
                EditorGUILayout.EndHorizontal();

                group.archetype = (TestArchetype)EditorGUILayout.EnumPopup("Archetype", group.archetype);

                bool isSamSite = group.archetype == TestArchetype.SamSite;
                // SamSite ignores "armed" entirely — it's always armed via its own
                // BaseDefenseDefinition.missileLoadout (see CombatTestSceneBuilder.
                // SpawnEnemyRoster), not the strike/scout drone loadout toggle below.
                using (new EditorGUI.DisabledScope(isSamSite))
                {
                    group.armed = EditorGUILayout.Toggle("Armed (strike loadout)", group.armed);
                }
                group.count = Mathf.Max(0, EditorGUILayout.IntField("Count", group.count));

                using (new EditorGUI.DisabledScope(!group.armed && !isSamSite))
                {
                    group.fireCooldownSeconds = Mathf.Max(0.05f,
                        EditorGUILayout.FloatField("Fire Cooldown (s/shot)", group.fireCooldownSeconds));
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();

            if (groupToRemove >= 0)
                _enemyGroups.RemoveAt(groupToRemove);

            if (GUILayout.Button("+ Add Enemy Group"))
                _enemyGroups.Add(new EnemySpawnGroup());

            EditorGUILayout.Space();
            _scenePath = EditorGUILayout.TextField("Scene Path", _scenePath);

            int totalEnemies = 0;
            foreach (var group in _enemyGroups)
                totalEnemies += Mathf.Max(0, group.count);
            EditorGUILayout.LabelField($"Total enemies: {totalEnemies}");

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(totalEnemies == 0 || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Build Test Scene", GUILayout.Height(30f)))
                    CombatTestSceneBuilder.BuildScene(_enemyGroups, _scenePath);

                if (GUILayout.Button("Build & Enter Play Mode", GUILayout.Height(30f)))
                {
                    CombatTestSceneBuilder.BuildScene(_enemyGroups, _scenePath);
                    EditorApplication.isPlaying = true;
                }
            }

            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Exit Play mode before rebuilding the scene.", MessageType.Warning);
        }
    }
}
