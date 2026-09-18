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

        [SerializeField] private DifficultyType _difficulty = DifficultyType.Easy;
        [SerializeField] private int _orbitIndex = 1;
        [SerializeField] private int _previousPatternIndex;
        [SerializeField] private int _consecutiveRepeats = 1;

        private readonly List<ObstaclePatternDefinition> _patternDefinitions = new List<ObstaclePatternDefinition>();
        private Vector2 _scrollPosition;
        private ObstaclePatternGenerationCatalog _patternCatalog;
        private ObstaclePatternGenerationConfig _patternConfig;
        private OrbitConfig _orbitConfig;
        private SpikeConfig _spikeConfig;
        private MiniSpikeConfig _miniSpikeConfig;
        private ImpulseTrapConfig _impulseTrapConfig;
        private GravityWellConfig _gravityWellConfig;
        private LaserGateConfig _laserGateConfig;
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

            DifficultyType difficulty = (DifficultyType)EditorGUILayout.EnumPopup("Difficulty", _difficulty);
            if (difficulty != _difficulty)
            {
                _difficulty = difficulty;
                LoadPatternDefinitions();
            }
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
                _riftConfig.RiftLength,
                _laserGateConfig.Width);

            PatternGenerationSettings generationSettings = _patternConfig.CreateSettings(
                _difficulty,
                lengthProvider);
            PatternDifficultyStageSnapshot stage = generationSettings.GetStage(_orbitIndex);
            List<PatternProbabilityRow> rows = CreateRows(lengthProvider, stage);

            float totalWeight = 0.0f;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Available && rows[i].AggregateWeight > 0.0f)
                {
                    totalWeight += rows[i].AggregateWeight;
                }
            }

            EditorGUILayout.Space(8.0f);
            EditorGUILayout.LabelField("Active Stage", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unlocked Slots", stage.UnlockedSlots.ToString());
            EditorGUILayout.LabelField("Same Pattern Multiplier", stage.SamePatternWeightMultiplier.ToString("0.###"));
            EditorGUILayout.LabelField("Same Tag Multiplier", stage.SameTagWeightMultiplier.ToString("0.###"));
            EditorGUILayout.LabelField("Ignore Pattern Orbit Range", stage.IgnorePatternOrbitRange ? "Yes" : "No");
            EditorGUILayout.LabelField("Total Candidate Weight", totalWeight.ToString("0.###"));

            EditorGUILayout.HelpBox(
                "Rift-containing patterns are quota-driven and excluded from these probabilities. " +
                "Runtime cursor space, conditional Safe Padding, object overlaps, band phase, and marker crossings can still remove candidates " +
                "and renormalize the final selection.",
                MessageType.Info);

            if (totalWeight <= 0.0f)
            {
                EditorGUILayout.HelpBox(
                    "No non-rift patterns have a positive probability for the current context.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4.0f);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < rows.Count; i++)
            {
                PatternProbabilityRow row = rows[i];
                if (!row.Available || row.AggregateWeight <= 0.0f)
                {
                    continue;
                }

                DrawRow(row, totalWeight);
            }
            EditorGUILayout.EndScrollView();
        }

        private List<PatternProbabilityRow> CreateRows(
            PatternSlotLengthProvider lengthProvider,
            PatternDifficultyStageSnapshot stage)
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
                if (snapshot.Orbit1RiftCount > 0 || snapshot.Orbit2RiftCount > 0)
                {
                    continue;
                }

                string unavailableReason = GetUnavailableReason(snapshot, stage, circumference);
                float baseEffectiveWeight = unavailableReason == null
                    ? PatternWeightCalculator.GetEffectiveWeight(snapshot, stage, history)
                    : 0.0f;
                int orientations = snapshot.AllowFlip ? 2 : 1;
                float aggregateWeight = unavailableReason == null
                    ? baseEffectiveWeight * orientations
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

            if (!stage.IgnorePatternOrbitRange &&
                (_orbitIndex < pattern.MinOrbitIndex || _orbitIndex > pattern.MaxOrbitIndex))
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
            EditorGUILayout.LabelField(
                $"Base {row.Pattern.BaseWeight:0.###}  |  Difficulty {row.Pattern.DifficultyWeightMultiplier:0.###}  |  " +
                $"Repeat/Tag {repetitionModifier:0.###}  |  " +
                $"Orientations {row.Orientations}  |  " +
                $"Total {row.AggregateWeight:0.###}",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void ReloadAssets()
        {
            _patternCatalog = LoadConfig<ObstaclePatternGenerationCatalog>("ObstaclePatternGenerationCatalog.asset");
            _orbitConfig = LoadConfig<OrbitConfig>("OrbitConfig.asset");
            _spikeConfig = LoadConfig<SpikeConfig>("SpikeConfig.asset");
            _miniSpikeConfig = LoadConfig<MiniSpikeConfig>("MiniSpikeConfig.asset");
            _impulseTrapConfig = LoadConfig<ImpulseTrapConfig>("ImpulseTrapConfig.asset");
            _gravityWellConfig = LoadConfig<GravityWellConfig>("GravityWellConfig.asset");
            _laserGateConfig = LoadConfig<LaserGateConfig>("LaserGateConfig.asset");
            _riftConfig = LoadConfig<RiftConfig>("RiftConfig.asset");

            LoadPatternDefinitions();
            Repaint();
        }

        private void LoadPatternDefinitions()
        {
            _patternConfig = _patternCatalog != null ? _patternCatalog.GetConfig(_difficulty) : null;
            _patternDefinitions.Clear();
            if (_patternConfig != null)
            {
                foreach (ObstaclePatternDefinition pattern in _patternConfig.Patterns)
                {
                    if (pattern != null)
                    {
                        _patternDefinitions.Add(pattern);
                    }
                }
            }

            _previousPatternIndex = Mathf.Clamp(_previousPatternIndex, 0, _patternDefinitions.Count);
        }

        private bool HasRequiredConfigs()
        {
            return _patternCatalog != null &&
                   _patternConfig != null &&
                   _orbitConfig != null &&
                   _spikeConfig != null &&
                   _miniSpikeConfig != null &&
                   _impulseTrapConfig != null &&
                   _gravityWellConfig != null &&
                   _laserGateConfig != null &&
                   _riftConfig != null;
        }

        private float GetOrbitRadius()
        {
            return Mathf.Max(
                0.01f,
                _orbitConfig.FirstOrbitRadius + _orbitIndex * _orbitConfig.OrbitRadiusStep);
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
    }
}
#endif
