#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using OrbitalEscape.Difficulty;
using OrbitalEscape.Orbits.Generators;
using OrbitalEscape.Orbits.Generators.Patterns;
using UnityEditor;
using UnityEngine;

namespace OrbitalEscape.Editor
{
    public sealed class PatternProbabilityWindow : EditorWindow
    {
        private const string ConfigRoot = "Assets/Resources/Configs/";

        [SerializeField] private DifficultyType _difficulty = DifficultyType.Basic;
        [SerializeField] private int _orbitIndex = 1;
        [SerializeField] private int _previousPatternIndex;
        [SerializeField] private int _consecutiveRepeats = 1;
        [SerializeField] private bool _showUnavailable = true;
        [SerializeField] private float _lowerNeighborRiftDistance = 26.18f;
        [SerializeField] private float _upperNeighborRiftDistance = 26.18f;

        private readonly List<ObstaclePatternDefinition> _patternDefinitions = new List<ObstaclePatternDefinition>();
        private Vector2 _scrollPosition;
        private ObstaclePatternGenerationConfig _patternConfig;
        private OrbitConfig _orbitConfig;
        private SpikeConfig _spikeConfig;
        private MiniSpikeConfig _miniSpikeConfig;
        private ImpulseTrapConfig _impulseTrapConfig;
        private GravityWellConfig _gravityWellConfig;
        private RiftConfig _riftConfig;

        [MenuItem("Orbital Escape/Generation/Pattern Probabilities")]
        public static void Open()
        {
            PatternProbabilityWindow window = GetWindow<PatternProbabilityWindow>();
            window.titleContent = new GUIContent("Pattern Probabilities");
            window.minSize = new Vector2(520.0f, 420.0f);
            window.Show();
        }

        private void OnEnable()
        {
            ReloadAssets();
            EditorApplication.projectChanged += ReloadAssets;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= ReloadAssets;
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!HasRequiredConfigs())
            {
                EditorGUILayout.HelpBox(
                    "One or more generation config assets are missing from Assets/Resources/Configs.",
                    MessageType.Error);
                return;
            }

            DrawContextControls();
            DrawProbabilities();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Pattern appearance probability", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60.0f)))
                {
                    ReloadAssets();
                }
            }
        }

        private void DrawContextControls()
        {
            EditorGUILayout.Space(6.0f);
            EditorGUILayout.LabelField("Current Orbit Context", EditorStyles.boldLabel);

            _difficulty = (DifficultyType)EditorGUILayout.EnumPopup("Difficulty", _difficulty);
            _orbitIndex = Mathf.Max(0, EditorGUILayout.IntField("Orbit Index", _orbitIndex));

            string[] patternNames = GetPatternNames();
            _previousPatternIndex = Mathf.Clamp(
                EditorGUILayout.Popup("Previous Pattern", _previousPatternIndex, patternNames),
                0,
                Math.Max(0, patternNames.Length - 1));

            if (_previousPatternIndex > 0)
            {
                _consecutiveRepeats = EditorGUILayout.IntSlider(
                    "Consecutive Repeats",
                    Mathf.Clamp(_consecutiveRepeats, 1, 8),
                    1,
                    8);
            }

            _showUnavailable = EditorGUILayout.Toggle("Show Unavailable", _showUnavailable);

            EditorGUILayout.Space(6.0f);
            EditorGUILayout.LabelField("Rift Weight Context", EditorStyles.boldLabel);
            float maxNeighborDistance = Mathf.Max(1.0f, _riftConfig.NeighborRiftAvoidanceDistance * 2.0f);
            _lowerNeighborRiftDistance = Mathf.Clamp(
                EditorGUILayout.FloatField("Lower Neighbor Center Distance (units)", _lowerNeighborRiftDistance),
                0.0f,
                maxNeighborDistance);
            _upperNeighborRiftDistance = Mathf.Clamp(
                EditorGUILayout.FloatField("Upper Neighbor Center Distance (units)", _upperNeighborRiftDistance),
                0.0f,
                maxNeighborDistance);

            float radius = GetOrbitRadius();
            EditorGUILayout.LabelField("Radius", radius.ToString("0.##"));
            EditorGUILayout.LabelField("Circumference", (2.0f * Mathf.PI * radius).ToString("0.##"));
        }

        private void DrawProbabilities()
        {
            PatternSlotLengthProvider lengthProvider = new PatternSlotLengthProvider(
                _spikeConfig.SpikeArcLength,
                _miniSpikeConfig.SpikeArcLength,
                _impulseTrapConfig.TrapArcLength,
                _gravityWellConfig.FieldArcLength,
                _riftConfig.RiftLength);

            PatternGenerationSettings generationSettings = _patternConfig.CreateSettings(
                _difficulty,
                lengthProvider);
            PatternDifficultyStageSnapshot stage = generationSettings.GetStage(_orbitIndex);
            RiftGenerationSettings riftSettings = CreateRiftSettings();
            List<PatternProbabilityRow> rows = CreateRows(lengthProvider, stage, riftSettings);

            float totalWeight = 0.0f;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Available)
                {
                    totalWeight += rows[i].AggregateWeight;
                }
            }

            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Active Stage", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unlocked Slots", stage.UnlockedSlots.ToString());
            EditorGUILayout.LabelField("Same Pattern Multiplier", stage.SamePatternWeightMultiplier.ToString("0.###"));
            EditorGUILayout.LabelField("Same Tag Multiplier", stage.SameTagWeightMultiplier.ToString("0.###"));
            EditorGUILayout.LabelField("Total Candidate Weight", totalWeight.ToString("0.###"));

            EditorGUILayout.HelpBox(
                "Probabilities include the configured neighbor-rift distance for each physical lane. " +
                "Runtime cursor space, object overlaps, band phase, and rift reservations can still remove candidates " +
                "and renormalize the final selection.",
                MessageType.Info);

            EditorGUILayout.Space(4.0f);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < rows.Count; i++)
            {
                PatternProbabilityRow row = rows[i];
                if (!row.Available && !_showUnavailable)
                {
                    continue;
                }

                DrawRow(row, totalWeight);
            }
            EditorGUILayout.EndScrollView();
        }

        private List<PatternProbabilityRow> CreateRows(
            PatternSlotLengthProvider lengthProvider,
            PatternDifficultyStageSnapshot stage,
            RiftGenerationSettings riftSettings)
        {
            List<ObstaclePatternSnapshot> snapshots = new List<ObstaclePatternSnapshot>(_patternDefinitions.Count);
            for (int i = 0; i < _patternDefinitions.Count; i++)
            {
                snapshots.Add(_patternDefinitions[i].CreateSnapshot(_difficulty, lengthProvider));
            }

            List<ObstaclePatternSnapshot> history = CreateHistory(snapshots);
            float circumference = 2.0f * Mathf.PI * GetOrbitRadius();
            List<PatternProbabilityRow> rows = new List<PatternProbabilityRow>(_patternDefinitions.Count);

            for (int i = 0; i < _patternDefinitions.Count; i++)
            {
                ObstaclePatternSnapshot snapshot = snapshots[i];
                string unavailableReason = GetUnavailableReason(snapshot, stage, circumference);
                float baseEffectiveWeight = unavailableReason == null
                    ? PatternWeightCalculator.GetEffectiveWeight(snapshot, stage, history)
                    : 0.0f;
                int orientations = snapshot.AllowFlip ? 2 : 1;
                float aggregateWeight = unavailableReason == null
                    ? GetAggregateWeight(snapshot, baseEffectiveWeight, riftSettings)
                    : 0.0f;

                if (unavailableReason == null && aggregateWeight <= 0.0f)
                {
                    unavailableReason = "Effective weight is zero";
                }

                rows.Add(new PatternProbabilityRow(
                    _patternDefinitions[i].name,
                    snapshot,
                    orientations,
                    baseEffectiveWeight,
                    aggregateWeight,
                    unavailableReason));
            }

            return rows;
        }

        private float GetAggregateWeight(
            ObstaclePatternSnapshot pattern,
            float baseEffectiveWeight,
            RiftGenerationSettings riftSettings)
        {
            float weight = GetOrientationWeight(pattern, false, baseEffectiveWeight, riftSettings);
            if (pattern.AllowFlip)
            {
                weight += GetOrientationWeight(pattern, true, baseEffectiveWeight, riftSettings);
            }

            return weight;
        }

        private float GetOrientationWeight(
            ObstaclePatternSnapshot pattern,
            bool flipped,
            float baseEffectiveWeight,
            RiftGenerationSettings riftSettings)
        {
            bool lowerHasRift = pattern.GetLowerLaneRiftCount(flipped) > 0;
            bool upperHasRift = pattern.GetUpperLaneRiftCount(flipped) > 0;
            if (!lowerHasRift && !upperHasRift)
            {
                return baseEffectiveWeight;
            }

            float neighborMultiplier = 1.0f;
            if (lowerHasRift)
            {
                neighborMultiplier = Mathf.Min(
                    neighborMultiplier,
                    riftSettings.GetNeighborWeightMultiplier(_lowerNeighborRiftDistance));
            }

            if (upperHasRift)
            {
                neighborMultiplier = Mathf.Min(
                    neighborMultiplier,
                    riftSettings.GetNeighborWeightMultiplier(_upperNeighborRiftDistance));
            }

            return baseEffectiveWeight * neighborMultiplier;
        }

        private List<ObstaclePatternSnapshot> CreateHistory(IReadOnlyList<ObstaclePatternSnapshot> snapshots)
        {
            List<ObstaclePatternSnapshot> history = new List<ObstaclePatternSnapshot>();
            int snapshotIndex = _previousPatternIndex - 1;
            if (snapshotIndex < 0 || snapshotIndex >= snapshots.Count)
            {
                return history;
            }

            ObstaclePatternSnapshot previous = snapshots[snapshotIndex];
            for (int i = 0; i < Mathf.Clamp(_consecutiveRepeats, 1, 8); i++)
            {
                history.Add(previous);
            }

            return history;
        }

        private string GetUnavailableReason(
            ObstaclePatternSnapshot pattern,
            PatternDifficultyStageSnapshot stage,
            float circumference)
        {
            if (!pattern.Enabled)
            {
                return "Disabled";
            }

            if (pattern.Length <= 0.0f)
            {
                return "No slot length";
            }

            if (pattern.BaseWeight <= 0.0f)
            {
                return "Base weight is zero";
            }

            if (_orbitIndex < pattern.MinOrbitIndex || _orbitIndex > pattern.MaxOrbitIndex)
            {
                return $"Orbit index range {pattern.MinOrbitIndex}-{FormatMaxOrbitIndex(pattern.MaxOrbitIndex)}";
            }

            if (pattern.ContainsLockedSlot(stage.UnlockedSlots))
            {
                return "Contains a locked slot";
            }

            if (pattern.Length > circumference)
            {
                return "Longer than orbit circumference";
            }

            return null;
        }

        private void DrawRow(PatternProbabilityRow row, float totalWeight)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!row.Available)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField(row.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(row.UnavailableReason);
                }

                EditorGUILayout.EndVertical();
                return;
            }

            float probability = totalWeight > 0.0f ? row.AggregateWeight / totalWeight : 0.0f;
            Rect progressRect = GUILayoutUtility.GetRect(1.0f, 22.0f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(
                progressRect,
                probability,
                $"{row.Name}  {probability:P2}");

            float baseDifficultyWeight = row.Pattern.BaseWeight * row.Pattern.DifficultyWeightMultiplier;
            float repetitionModifier = baseDifficultyWeight > 0.0f
                ? row.BaseEffectiveWeight / baseDifficultyWeight
                : 0.0f;
            float riftModifier = row.BaseEffectiveWeight > 0.0f && row.Orientations > 0
                ? row.AggregateWeight / (row.BaseEffectiveWeight * row.Orientations)
                : 0.0f;

            EditorGUILayout.LabelField(
                $"Base {row.Pattern.BaseWeight:0.###}  |  Difficulty {row.Pattern.DifficultyWeightMultiplier:0.###}  |  " +
                $"Repeat/Tag {repetitionModifier:0.###}  |  Rift {riftModifier:0.###}  |  " +
                $"Orientations {row.Orientations}  |  " +
                $"Total {row.AggregateWeight:0.###}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void ReloadAssets()
        {
            _patternConfig = LoadConfig<ObstaclePatternGenerationConfig>("ObstaclePatternGenerationConfig.asset");
            _orbitConfig = LoadConfig<OrbitConfig>("OrbitConfig.asset");
            _spikeConfig = LoadConfig<SpikeConfig>("SpikeConfig.asset");
            _miniSpikeConfig = LoadConfig<MiniSpikeConfig>("MiniSpikeConfig.asset");
            _impulseTrapConfig = LoadConfig<ImpulseTrapConfig>("ImpulseTrapConfig.asset");
            _gravityWellConfig = LoadConfig<GravityWellConfig>("GravityWellConfig.asset");
            _riftConfig = LoadConfig<RiftConfig>("RiftConfig.asset");

            _patternDefinitions.Clear();
            if (_patternConfig != null)
            {
                SerializedProperty patterns = new SerializedObject(_patternConfig).FindProperty("_patterns");
                for (int i = 0; patterns != null && i < patterns.arraySize; i++)
                {
                    if (patterns.GetArrayElementAtIndex(i).objectReferenceValue is ObstaclePatternDefinition pattern)
                    {
                        _patternDefinitions.Add(pattern);
                    }
                }
            }

            _previousPatternIndex = Mathf.Clamp(_previousPatternIndex, 0, _patternDefinitions.Count);
            Repaint();
        }

        private bool HasRequiredConfigs()
        {
            return _patternConfig != null &&
                   _orbitConfig != null &&
                   _spikeConfig != null &&
                   _miniSpikeConfig != null &&
                   _impulseTrapConfig != null &&
                   _gravityWellConfig != null &&
                   _riftConfig != null;
        }

        private float GetOrbitRadius()
        {
            return Mathf.Max(
                0.01f,
                _orbitConfig.FirstOrbitRadius + _orbitIndex * _orbitConfig.OrbitRadiusStep);
        }

        private RiftGenerationSettings CreateRiftSettings()
        {
            return new RiftGenerationSettings(
                _riftConfig.MinCenterDistanceBetweenRifts,
                _riftConfig.RiftLength,
                _riftConfig.RiftCount,
                _riftConfig.BaseRiftCountRadius,
                _riftConfig.AdditionalRiftRadiusStep,
                _riftConfig.NeighborRiftAvoidanceDistance,
                _riftConfig.AlignedNeighborRiftWeightMultiplier,
                _riftConfig.RiftPaddings,
                _riftConfig.MaxPlacementAttempts);
        }

        private string[] GetPatternNames()
        {
            string[] names = new string[_patternDefinitions.Count + 1];
            names[0] = "None";
            for (int i = 0; i < _patternDefinitions.Count; i++)
            {
                names[i + 1] = _patternDefinitions[i].name;
            }

            return names;
        }

        private static T LoadConfig<T>(string fileName) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(ConfigRoot + fileName);
        }

        private static string FormatMaxOrbitIndex(int maxOrbitIndex)
        {
            return maxOrbitIndex == int.MaxValue ? "infinity" : maxOrbitIndex.ToString();
        }

        private sealed class PatternProbabilityRow
        {
            public string Name { get; }
            public ObstaclePatternSnapshot Pattern { get; }
            public int Orientations { get; }
            public float BaseEffectiveWeight { get; }
            public float AggregateWeight { get; }
            public string UnavailableReason { get; }
            public bool Available => string.IsNullOrEmpty(UnavailableReason);

            public PatternProbabilityRow(
                string name,
                ObstaclePatternSnapshot pattern,
                int orientations,
                float baseEffectiveWeight,
                float aggregateWeight,
                string unavailableReason)
            {
                Name = name;
                Pattern = pattern;
                Orientations = orientations;
                BaseEffectiveWeight = baseEffectiveWeight;
                AggregateWeight = aggregateWeight;
                UnavailableReason = unavailableReason;
            }
        }
    }
}
#endif
