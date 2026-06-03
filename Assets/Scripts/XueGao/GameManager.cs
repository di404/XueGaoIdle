using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XueGao
{
    public class GameManager : MonoBehaviour
    {
        private enum GameState
        {
            Table,
            FocusedEating,
            PrizeReveal
        }

        [SerializeField] private List<IceCreamDefinition> iceCreams = new List<IceCreamDefinition>();
        [SerializeField] private IceCreamEater eater;
        [SerializeField] private IceCreamTable table;
        [SerializeField] private MouthController mouth;
        [SerializeField] private LotterySystem lottery;
        [SerializeField] private GameUI ui;
        [SerializeField] private JuicyFeedbacks feedbacks;
        [SerializeField] private IceCreamStickDefinition defaultStickDefinition;
        [SerializeField] private float stickRevealDelay = 0.45f;
        [SerializeField] private float prizeRevealDelay = 0.65f;
        [SerializeField] private Vector3 focusedIceCreamPosition = new Vector3(0f, -0.28f, 0f);
        [SerializeField] private float focusedTableAlpha = 0.18f;

        private readonly List<string> history = new List<string>();

        private GameState state = GameState.Table;
        private IceCream activeIceCream;
        private int money;
        private int sticks;
        private int mouthLevel = 1;
        private int autoBiteLevel;
        private int luckLevel;
        private float autoBiteTimer;
        private bool resolvingCompletion;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (lottery == null)
            {
                lottery = GetComponent<LotterySystem>();
            }

            if (table == null)
            {
                table = GetComponent<IceCreamTable>();
            }

            if (table != null)
            {
                table.Initialize();
                table.IceCreamClicked += OnTableIceCreamClicked;
            }

            ui.BuildShop(iceCreams, BuyIceCream);
            ui.MouthUpgradeButton.onClick.AddListener(UpgradeMouth);
            ui.AutoBiteUpgradeButton.onClick.AddListener(UpgradeAutoBite);
            ui.LuckUpgradeButton.onClick.AddListener(UpgradeLuck);
            ui.PrizeContinueButton.onClick.AddListener(CompletePrizeReveal);

            eater.ProgressChanged += OnProgressChanged;
            eater.BiteApplied += OnBiteApplied;
            eater.Completed += OnIceCreamCompleted;
            mouth.SetEater(eater);
            mouth.SetLevel(mouthLevel);
            eater.SetMouth(mouth);
            eater.Clear();
            eater.InputEnabled = false;
            eater.gameObject.SetActive(false);
            SetState(GameState.Table);
            RefreshUI();
            initialized = true;
        }

        private void OnDestroy()
        {
            if (table != null)
            {
                table.IceCreamClicked -= OnTableIceCreamClicked;
            }

            if (eater != null)
            {
                eater.ProgressChanged -= OnProgressChanged;
                eater.BiteApplied -= OnBiteApplied;
                eater.Completed -= OnIceCreamCompleted;
            }
        }

        private void Update()
        {
            if (state == GameState.FocusedEating && !resolvingCompletion && autoBiteLevel > 0)
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

        private void BuyIceCream(int index)
        {
            if (state != GameState.Table || index < 0 || index >= iceCreams.Count)
            {
                return;
            }

            if (table == null)
            {
                ui.SetPrizeMessage("缺少桌面组件，无法生成雪糕。");
                RefreshUI();
                return;
            }

            if (table.Count >= table.Capacity)
            {
                ui.SetPrizeMessage("桌子放满了，先吃掉几根雪糕。");
                RefreshUI();
                return;
            }

            IceCreamDefinition definition = iceCreams[index];
            if (money < definition.price)
            {
                ui.SetPrizeMessage("钱不够，先吃几根雪糕碰碰运气。");
                RefreshUI();
                return;
            }

            if (table.TryAdd(definition, out _, out string failureMessage))
            {
                money -= definition.price;
                ui.SetPrizeMessage($"买了一根{definition.displayName}。");
            }
            else if (!string.IsNullOrEmpty(failureMessage))
            {
                ui.SetPrizeMessage(failureMessage);
            }

            RefreshUI();
        }

        private void OnTableIceCreamClicked(IceCream iceCream)
        {
            if (state == GameState.Table && iceCream != null)
            {
                EnterFocusedEating(iceCream);
            }
        }

        private void EnterFocusedEating(IceCream iceCream)
        {
            activeIceCream = iceCream;
            IceCreamDefinition definition = activeIceCream != null ? activeIceCream.CurrentDefinition : null;
            table.SetTableAlpha(focusedTableAlpha, activeIceCream);
            eater.transform.position = focusedIceCreamPosition;
            eater.gameObject.SetActive(true);
            eater.BeginEating(activeIceCream, defaultStickDefinition);
            autoBiteTimer = 0f;
            ui.SetPrizeMessage("正在吃：" + (definition != null ? definition.displayName : "雪糕"));
            SetState(GameState.FocusedEating);
            RefreshUI();
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
            if (resolvingCompletion || state != GameState.FocusedEating)
            {
                return;
            }

            StartCoroutine(ResolveCompletedIceCream());
        }

        private IEnumerator ResolveCompletedIceCream()
        {
            resolvingCompletion = true;
            SetState(GameState.PrizeReveal);
            sticks++;
            RefreshUI();

            IceCreamDefinition definition = activeIceCream != null ? activeIceCream.CurrentDefinition : null;
            PrizeResult result = lottery.Roll(definition != null ? definition.prizeMultiplier : 1, luckLevel);
            eater.RevealStick(result.StickDefinition);
            feedbacks.PlayComplete();
            ui.SetPrizeMessage("雪糕吃完了，正在翻雪糕棍...");
            yield return new WaitForSeconds(stickRevealDelay);

            yield return new WaitForSeconds(prizeRevealDelay);

            int multiplier = definition != null ? definition.prizeMultiplier : 1;
            money += result.FinalAmount;
            string message = result.IsWin ? $"中奖！{result.Label} x{multiplier} = ￥{result.FinalAmount}" : "谢谢参与，下根再来";
            ui.SetPrizeMessage(message);
            ui.ShowPrizeModal(result.IsWin ? "中奖！" : "谢谢参与", message);
            history.Insert(0, message);
            while (history.Count > 5)
            {
                history.RemoveAt(history.Count - 1);
            }

            feedbacks.PlayPrize(result.IsWin);
            RefreshUI();
        }

        private void CompletePrizeReveal()
        {
            if (state != GameState.PrizeReveal || activeIceCream == null)
            {
                return;
            }

            eater.HideStick();
            eater.EndEating();
            table.Remove(activeIceCream);
            activeIceCream = null;
            eater.gameObject.SetActive(false);
            resolvingCompletion = false;
            table.SetTableAlpha(1f);
            table.Relayout();
            ui.HidePrizeModal();
            SetState(GameState.Table);
            RefreshUI();
        }

        private void UpgradeMouth()
        {
            if (state == GameState.PrizeReveal)
            {
                return;
            }

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
            if (state == GameState.PrizeReveal)
            {
                return;
            }

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
            if (state == GameState.PrizeReveal)
            {
                return;
            }

            int cost = GetLuckCost();
            if (money < cost)
            {
                return;
            }

            money -= cost;
            luckLevel++;
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
            IceCreamDefinition displayedDefinition = activeIceCream != null ? activeIceCream.CurrentDefinition : (iceCreams.Count > 0 ? iceCreams[0] : null);
            string iceCreamName = displayedDefinition != null ? displayedDefinition.displayName : "暂无雪糕";
            int multiplier = displayedDefinition != null ? displayedDefinition.prizeMultiplier : 1;
            int tableCount = table != null ? table.Count : 0;
            int tableCapacity = table != null ? table.Capacity : 0;
            ui.SetStats(money, sticks, iceCreamName, multiplier, mouthLevel);
            ui.SetHistory(history);
            ui.SetUpgradeTexts(mouthLevel, GetMouthCost(), CanUseSideMenus() && money >= GetMouthCost(), autoBiteLevel, GetAutoBiteCost(), CanUseSideMenus() && money >= GetAutoBiteCost(), luckLevel, GetLuckCost(), CanUseSideMenus() && money >= GetLuckCost());
            ui.RefreshShop(iceCreams, money, tableCount, tableCapacity, state == GameState.Table);
            ui.SetTableStatus(GetTableStatus());
        }

        private string GetTableStatus()
        {
            if (state == GameState.Table)
            {
                return table != null ? table.GetStatus(true) : "缺少桌面组件";
            }

            if (state == GameState.FocusedEating)
            {
                return "正在吃雪糕";
            }

            return "开奖中";
        }

        private bool CanUseSideMenus()
        {
            return state == GameState.Table || state == GameState.FocusedEating;
        }

        private void SetState(GameState newState)
        {
            state = newState;
            if (table != null)
            {
                bool tableMode = state == GameState.Table;
                table.SetInteractionEnabled(tableMode, tableMode);
            }

            if (eater != null)
            {
                eater.InputEnabled = state == GameState.FocusedEating && !resolvingCompletion;
            }

            ui.SetMode(state == GameState.Table, state == GameState.FocusedEating, state == GameState.PrizeReveal);
        }
    }
}
