using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlipFlop
{
    public class CardMatchingGameHandler : MonoBase
    {
        [Space]
        [SerializeField] private GameStateManager gameManager;
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private Card _cardPrefab;

        private float _lastCorrectTime = 0;
        private Coroutine guessCheckingCoroutine;
        private GuessChecking checkingQueue;

        private void Awake()
        {
            _cardPrefab.gameObject.SetActive(false);

            _FirstGuess = new();
            _SecondGuess = new();
            checkingQueue = new GuessChecking(CheckGuess);
        }

        private void LoadLevel()
        {
#if UNITY_EDITOR
            Debug.Log($"Level: {GameInfo.levelNumber}");
            Debug.Log($"Card Counts: {_Config.numCard}");
            Debug.Log($"Level Time: {_Config.timeLimit}");
#endif

            // spawn cards
            _Cards.Clear();

            int spriteCount = _Config.numCard / 2;

            Sprite[] cardSprites = Factory.Sprites.GetSprites(spriteCount);
            for (int id = 0, index = 0; id < spriteCount; id++)
            {
                var card = Instantiate(Instance._cardPrefab, Instance._cardContainer);

                if (card != null)
                {
                    card.Setup(index, id, cardSprites[id])
                        .SetOnClick(PickCard);

                    _Cards.Add(card);
                    index++;
                }

                card = Instantiate(Instance._cardPrefab, Instance._cardContainer);

                if (card != null)
                {
                    card.Setup(index, id, cardSprites[id])
                        .SetOnClick(PickCard);

                    _Cards.Add(card);
                    index++;
                }
            }

            // shuffle cards
            foreach (var card in _Cards)
            {
                card.transform.SetSiblingIndex(UnityEngine.Random.Range(0, _Cards.Count));
            }

            guessCheckingCoroutine = StartCoroutine(CheckingRoutine());

            DelayCall(2, () =>
            {
                GameInfo.playing = true;
            });
        }

        private void Clean()
        {
            StopCoroutine(guessCheckingCoroutine);

            for (int i = 0; i < _Cards.Count; i++)
                Destroy(_Cards[i].gameObject);

            _Cards.Clear();
        }

        private void PickCard(Card card)
        {
            if (!_FirstGuess.Active)
            {
                _FirstGuess.Set(card);
            }
            else if (!_SecondGuess.Active)
            {
                _SecondGuess.Set(card);

                // Add to queue for compare guess
                checkingQueue.Add(_FirstGuess, _SecondGuess);
            }
        }

        private void CheckGuess(MatchCardGuess firstGuess, MatchCardGuess secondGuess)
        {
            Debug.Log($"Checking: {_FirstGuess.Card.Id} and {_SecondGuess.Card.Id}");

            if (firstGuess.Equals(secondGuess))
            {
                Debug.Log("Correct Guess");
                // check combo
                if (Mathf.Abs(Time.time - _lastCorrectTime) < .5f)
                {
                    // COMBO...

                    _OnCombo?.Invoke();
                }

                firstGuess.Clean();
                secondGuess.Clean();

                GameInfo.matchesCount++;
                _lastCorrectTime = Time.time;

                _OnGuessCorrect?.Invoke();

                CheckWinCondition();
            }
            else
            {
                Debug.Log("Wrong Guess");
                _FirstGuess.Reset();
                _SecondGuess.Reset();
                _OnGuessWrong?.Invoke();
            }

            GameInfo.turnCount++;
        }

        private void CheckWinCondition()
        {
            if (GameInfo.matchesCount == GameInfo.cardCount / 2)
            {
                DelayCall(1.5f, Win);
            }
        }

        private IEnumerator CheckingRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                checkingQueue.CheckRequest();
            }
        }

        ///////////////////////////////////////
        /// STATIC MEMEBERS
        ///////////////////////////////////////
        private static List<Card> _Cards = new();
        private static LevelConfig _Config;
        private MatchCardGuess _FirstGuess, _SecondGuess;

        public static event Action _OnCombo = null;
        public static event Action _OnGuessCorrect = null;
        public static event Action _OnGuessWrong = null;

        public static void StartGame()
        {
            GameInfo.Reset();
            GameInfo.levelNumber = PlayerProfile.LevelIndex + 1;

            _Config = Factory.GetLevel(GameInfo.levelNumber);
            GameInfo.timer = _Config.timeLimit;
            GameInfo.time = _Config.timeLimit;
            GameInfo.cardCount = _Config.numCard;

            Instance.gameManager.OpenGamePanel();

            Instance.LoadLevel();
        }

        public static void Win()
        {
            PlayerProfile.LevelIndex++;

            Instance.Clean();
            Instance.gameManager.OpenWinPanel();
        }

        public static void Lose()
        {
            Instance.Clean();
            Instance.gameManager.OpenLosePanel();
        }

        public static void BackToMainMenu()
        {
            Instance.Clean();
            Instance.gameManager.OpenMainMenu();
        }


        // Singleton pattern
        private static CardMatchingGameHandler instance;
        public static CardMatchingGameHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<CardMatchingGameHandler>();
                    if (instance == null)
                    {
                        instance = new GameObject().AddComponent<CardMatchingGameHandler>();
                        instance.gameObject.name = instance.GetType().Name;
                    }
                }

                return instance;
            }
        }

        ///////////////////////////////////////
        /// HELPER CLASS
        ///////////////////////////////////////
        public class GuessChecking
        {
            private Queue<MatchCardGuess> _guesses = new();
            private event Action<MatchCardGuess, MatchCardGuess> _onChecked = null;

            public GuessChecking(Action<MatchCardGuess, MatchCardGuess> onCheck)
            {
                _onChecked = onCheck;
            }

            public void Add(MatchCardGuess firstGuess, MatchCardGuess secondGuess)
            {
                _guesses.Enqueue(firstGuess);
                _guesses.Enqueue(secondGuess);
            }

            public void CheckRequest()
            {
                if (_guesses.Count >= 2)
                {
                    _onChecked?.Invoke(_guesses.Dequeue(), _guesses.Dequeue());
                }
            }
        }

    }
}