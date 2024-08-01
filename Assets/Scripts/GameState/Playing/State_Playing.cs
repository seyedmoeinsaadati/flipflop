using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FlipFlop
{
    public class State_Playing : GameStateBase
    {
        [SerializeField] private StatePlaying_View view;
        [SerializeField] private GridLayoutGroup grid;

        private void Start()
        {
            view.onHomeClicked += OnBackMenu;
        }

        private void OnEnable()
        {
            ResetView();

            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            int rowCount = Mathf.FloorToInt(Mathf.Sqrt(GameInfo.cardCount));
            rowCount = Mathf.Clamp(rowCount, 1, 4);
            grid.constraintCount = rowCount;

            CardMatchingGameHandler._OnCombo += OnCombo;
            CardMatchingGameHandler._OnGuessWrong += OnMatchHappened;
            CardMatchingGameHandler._OnGuessWrong += OnMatchHappened;
        }

        private void OnDisable()
        {
            CardMatchingGameHandler._OnCombo -= OnCombo;
            CardMatchingGameHandler._OnGuessWrong -= OnMatchHappened;
            CardMatchingGameHandler._OnGuessWrong -= OnMatchHappened;
        }

        public void UpdateTime()
        {
            view.UpdateTime(GameInfo.timer);
        }

        public void OnMatches()
        {
            view.UpdateMatches(GameInfo.matchesCount);
        }

        public void OnMatchHappened()
        {
            view.UpdateTurns(GameInfo.turnCount);
        }

        public void OnCombo()
        {
            view.UpdateCombo(GameInfo.comboCount);
            // TODO: combo effect
        }

        public void OnBackMenu()
        {
            CardMatchingGameHandler.BackToMainMenu();
        }

        private void OnDestroy()
        {
            view.onHomeClicked -= OnBackMenu;
        }

        private void Update()
        {

            if (GameInfo.playing)
            {

                CheckTime();

            }
#if UNITY_EDITOR
            // For test in editor (win/lose)
            if (Input.GetKeyUp(KeyCode.W))
            {
                CardMatchingGameHandler.Win();
            }
            else if (Input.GetKeyUp(KeyCode.Q))
            {
                CardMatchingGameHandler.Lose();
            }
#endif
        }

        private void CheckTime()
        {
            GameInfo.timer -= Time.deltaTime;

            if (GameInfo.timer <= 0)
            {
                CardMatchingGameHandler.Lose();
            }

            UpdateTime();
        }

        public void ResetView()
        {
            StartCoroutine(ResetRoutine());
        }

        private IEnumerator ResetRoutine()
        {
            yield return null;
            view.UpdateLevel(GameInfo.levelNumber);
            view.UpdateTime(0);
            view.UpdateTurns(0);
            view.UpdateMatches(0);
            view.UpdateCombo(0);
        }
    }
}