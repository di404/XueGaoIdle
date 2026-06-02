using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace XueGao
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private List<IceCreamDefinition> iceCreams = new List<IceCreamDefinition>();
        [SerializeField] private IceCreamEater eater;
        [SerializeField] private MouthController mouth;
        [SerializeField] private LotterySystem lottery;
        [SerializeField] private GameUI ui;
        [SerializeField] private JuicyFeedbacks feedbacks;
        [SerializeField] private float stickRevealDelay = 0.45f;
        [SerializeField] private float prizeRevealDelay = 0.65f;

        private readonly List<bool> unlocked = new List<bool>();
        private readonly List<string> history = new List<string>();

        private int money;
        private int sticks;
        private int currentIceCreamIndex;
        private int mouthLevel = 1;
        private int autoBiteLevel;
        private int luckLevel;
        private float autoBiteTimer;
        private bool resolvingCompletion;

        private void Awake()
        {
            if (lottery == null)
            {
                lottery = GetComponent<LotterySystem>();
            }

            for (int i = 0; i < iceCreams.Count; i++)
            {
                unlocked.Add(i == 0);
            }

            ui.BuildShop(iceCreams, BuyOrSelectIceCream);
            ui.MouthUpgradeButton.onClick.AddListener(UpgradeMouth);
            ui.AutoBiteUpgradeButton.onClick.AddListener(UpgradeAutoBite);
            ui.LuckUpgradeButton.onClick.AddListener(UpgradeLuck);

            eater.ProgressChanged += OnProgressChanged;
            eater.BiteApplied += OnBiteApplied;
            eater.Completed += OnIceCreamCompleted;
            mouth.SetLevel(mouthLevel);
            LoadIceCream(0);
            RefreshUI();
        }

        private void Update()
        {
            if (!resolvingCompletion && PointerPressedThisFrame())
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    eater.TryBite(mouth.GetPointerWorld(), mouth.BiteRadiusWorld);
                }
            }

            if (!resolvingCompletion && autoBiteLevel > 0)
            {
                autoBiteTimer += Time.deltaTime;
                float interval = Mathf.Max(0.18f, 1f / autoBiteLevel);
                while (autoBiteTimer >= interval)
                {
                    autoBiteTimer -= interval;
                    Vector3 point = eater.transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-1.0f, 0.9f), 0f);
                    eater.TryBite(point, mouth.BiteRadiusWorld * 0.7f);
                }
            }
        }

        private bool PointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
#endif
            return false;
        }

        private void LoadIceCream(int index)
        {
            currentIceCreamIndex = Mathf.Clamp(index, 0, iceCreams.Count - 1);
            eater.Load(iceCreams[currentIceCreamIndex]);
        }

        private void OnProgressChanged(float progress)
        {
            ui.SetProgress(progress);
        }

        private void OnBiteApplied(Vector3 position)
        {
            feedbacks.PlayBite();
        }

        private void OnIceCreamCompleted()
        {
            if (resolvingCompletion)
            {
                return;
            }

            StartCoroutine(ResolveCompletedIceCream());
        }

        private IEnumerator ResolveCompletedIceCream()
        {
            resolvingCompletion = true;
            sticks++;
            RefreshUI();

            eater.ShowStick();
            feedbacks.PlayComplete();
            ui.SetPrizeMessage("雪糕吃完了，正在翻雪糕棍...");
            yield return new WaitForSeconds(stickRevealDelay);

            PrizeResult result = lottery.Roll(iceCreams[currentIceCreamIndex].prizeMultiplier, luckLevel);
            yield return new WaitForSeconds(prizeRevealDelay);

            money += result.FinalAmount;
            string message = result.IsWin ? $"中奖！{result.Label} x{iceCreams[currentIceCreamIndex].prizeMultiplier} = ￥{result.FinalAmount}" : "谢谢参与，下根再来";
            ui.SetPrizeMessage(message);
            history.Insert(0, message);
            while (history.Count > 5)
            {
                history.RemoveAt(history.Count - 1);
            }

            feedbacks.PlayPrize(result.IsWin);
            RefreshUI();

            yield return new WaitForSeconds(0.85f);
            eater.HideStick();
            LoadIceCream(currentIceCreamIndex);
            resolvingCompletion = false;
            RefreshUI();
        }

        private void UpgradeMouth()
        {
            int cost = GetMouthCost();
            if (money < cost)
            {
                return;
            }

            money -= cost;
            mouthLevel++;
            mouth.SetLevel(mouthLevel);
            RefreshUI();
        }

        private void UpgradeAutoBite()
        {
            int cost = GetAutoBiteCost();
            if (money < cost)
            {
                return;
            }

            money -= cost;
            autoBiteLevel++;
            RefreshUI();
        }

        private void UpgradeLuck()
        {
            int cost = GetLuckCost();
            if (money < cost)
            {
                return;
            }

            money -= cost;
            luckLevel++;
            RefreshUI();
        }

        private void BuyOrSelectIceCream(int index)
        {
            if (!unlocked[index])
            {
                int price = iceCreams[index].price;
                if (money < price)
                {
                    return;
                }

                money -= price;
                unlocked[index] = true;
            }

            if (!resolvingCompletion)
            {
                LoadIceCream(index);
            }

            RefreshUI();
        }

        private int GetMouthCost()
        {
            return Mathf.RoundToInt(20f * Mathf.Pow(1.8f, mouthLevel - 1));
        }

        private int GetAutoBiteCost()
        {
            return Mathf.RoundToInt(50f * Mathf.Pow(2f, autoBiteLevel));
        }

        private int GetLuckCost()
        {
            return Mathf.RoundToInt(80f * Mathf.Pow(1.9f, luckLevel));
        }

        private void RefreshUI()
        {
            IceCreamDefinition current = iceCreams[currentIceCreamIndex];
            ui.SetStats(money, sticks, current.displayName, current.prizeMultiplier, mouthLevel);
            ui.SetHistory(history);
            ui.SetUpgradeTexts(mouthLevel, GetMouthCost(), money >= GetMouthCost(), autoBiteLevel, GetAutoBiteCost(), money >= GetAutoBiteCost(), luckLevel, GetLuckCost(), money >= GetLuckCost());
            ui.RefreshShop(iceCreams, unlocked, currentIceCreamIndex, money);
        }
    }
}
