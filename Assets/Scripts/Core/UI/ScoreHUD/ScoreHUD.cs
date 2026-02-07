using System;
using OrbitalEscape.Score;
using TMPro;
using UnityEngine;
using Zenject;

namespace OrbitalEscape.UI
{
    public class ScoreHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _newRecordText;
        
        private IScoreService _scoreService;
        
        [Inject]
        public void Construct(IScoreService scoreService) => _scoreService = scoreService;

        private void OnScoreChanged(float score)
        {
            var scoreString = _scoreService.Score.ToString("F1");

            _newRecordText.gameObject.SetActive(score > _scoreService.PreviousBestScore);

            int dotIndex = scoreString.IndexOf('.');
            if (dotIndex != -1 && dotIndex < scoreString.Length - 1 && scoreString[dotIndex + 1] == '0')
                _scoreText.SetText(scoreString.Substring(0, dotIndex));
            else                
                _scoreText.SetText(scoreString);
        }

        private void OnEnable()
        {
            if (_scoreService == null)
                return;
            
            _scoreService.ScoreChanged += OnScoreChanged;
            _scoreText.SetText("0");
        }

        private void OnDestroy()
        {
            if (_scoreService != null)
                _scoreService.ScoreChanged -= OnScoreChanged;
        }
    }
}
