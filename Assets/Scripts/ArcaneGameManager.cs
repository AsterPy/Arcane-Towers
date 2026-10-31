using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ArcaneGameManager : MonoBehaviour
{
    public static ArcaneGameManager Instance { get; private set; }

    [Header("Match Settings")]
    public float matchDuration = 240f;
    public float startingEnergy = 5f;
    public float maxEnergy = 10f;
    public float energyPerSecond = 0.5f;
    public float finalMinuteEnergyPerSecond = 1f;

    [Header("UI References")]
    public Text timerText;
    public Text energyText;
    public Image energyBarFill;
    public Image[] energyDrops;
    public Sprite energyDropSprite;
    public Text statusText;
    public GameObject gameOverPanel;
    public Text resultDetailsText;
    public Button topRestartButton;
    public Button gameOverRestartButton;

    public float Energy { get; private set; }
    public float TimeLeft { get; private set; }
    public bool MatchEnded { get; private set; }

    // AI State
    private float aiEnergy = 5f;
    private float aiActionTimer = 3f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Energy = startingEnergy;
        aiEnergy = startingEnergy;
        TimeLeft = matchDuration;
        MatchEnded = false;
    }

    private void Start()
    {
        EnsureEnergyDropsUI();
        if (topRestartButton != null) topRestartButton.onClick.AddListener(RestartMatch);
        if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(RestartMatch);
    }

    public void EnsureEnergyDropsUI()
    {
        if (energyDrops != null && energyDrops.Length == 10 && energyDrops[0] != null)
            return;

        Transform parentTransform = energyText != null ? energyText.transform.parent : null;
        if (parentTransform == null)
        {
            GameObject frame = GameObject.Find("EnergyBarFrame");
            if (frame != null) parentTransform = frame.transform;
        }

        if (parentTransform == null) return;

        if (energyBarFill != null)
        {
            energyBarFill.gameObject.SetActive(false);
        }

        Transform dropsContainer = parentTransform.Find("EnergyDropsContainer");
        if (dropsContainer != null)
        {
            List<Image> dropList = new List<Image>();
            for (int i = 0; i < 10; i++)
            {
                Transform child = dropsContainer.Find($"Drop_{i}");
                if (child == null && i < dropsContainer.childCount) child = dropsContainer.GetChild(i);
                if (child != null)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null) dropList.Add(img);
                }
            }
            if (dropList.Count == 10)
            {
                energyDrops = dropList.ToArray();
            }
        }
    }

    private void Update()
    {
        if (MatchEnded) return;

        // Timer
        TimeLeft = Mathf.Max(0f, TimeLeft - Time.deltaTime);

        // Energy regen
        float currentRate = TimeLeft <= 60f ? finalMinuteEnergyPerSecond : energyPerSecond;
        Energy = Mathf.Min(maxEnergy, Energy + currentRate * Time.deltaTime);
        aiEnergy = Mathf.Min(maxEnergy, aiEnergy + currentRate * Time.deltaTime);

        UpdateUI(currentRate);
        UpdateAI();

        if (TimeLeft <= 0f)
        {
            EvaluateTimeoutWinner();
        }
    }

    private void UpdateUI(float rate)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(TimeLeft / 60f);
            int seconds = Mathf.FloorToInt(TimeLeft % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
            if (TimeLeft <= 60f)
            {
                timerText.color = new Color(1f, 0.4f, 0.4f); // Red in last minute
            }
        }

        if (energyText != null)
        {
            string boost = TimeLeft <= 60f ? " (x2)" : "";
            energyText.text = $"{Mathf.FloorToInt(Energy)}/{maxEnergy}{boost}";
        }

        if (energyDrops != null && energyDrops.Length == 10)
        {
            int currentEnergyInt = Mathf.FloorToInt(Energy);
            for (int i = 0; i < energyDrops.Length; i++)
            {
                if (energyDrops[i] == null) continue;

                if (i < currentEnergyInt)
                {
                    energyDrops[i].color = new Color(1f, 1f, 1f, 1.0f);
                }
                else if (i == currentEnergyInt && currentEnergyInt < 10)
                {
                    float frac = Energy - currentEnergyInt;
                    energyDrops[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 0.85f, frac));
                }
                else
                {
                    energyDrops[i].color = new Color(1f, 1f, 1f, 0.25f);
                }
            }
        }
    }

    public void SpendEnergy(float amount)
    {
        Energy = Mathf.Max(0f, Energy - amount);
    }

    private void UpdateAI()
    {
        aiActionTimer -= Time.deltaTime;
        if (aiActionTimer > 0f) return;

        if (ArcaneDeckManager.Instance == null || ArcaneDeckManager.Instance.initialDeck.Count == 0)
            return;

        List<CardInfo> deck = ArcaneDeckManager.Instance.initialDeck;
        // Filter cards that AI can afford
        List<CardInfo> affordable = new List<CardInfo>();
        foreach (CardInfo c in deck)
        {
            if (aiEnergy >= c.cost) affordable.Add(c);
        }

        if (affordable.Count > 0)
        {
            CardInfo chosen = affordable[Random.Range(0, affordable.Count)];
            aiEnergy -= chosen.cost;

            float laneX = Random.value < 0.5f ? -2.7f : 2.7f;
            Vector3 spawnPos = new Vector3(laneX, 4.5f, 0f);

            if (chosen.cardType == CardType.Troop)
            {
                ArcaneDeckManager.Instance.SpawnTroop(chosen, spawnPos, false);
            }
            else if (chosen.cardType == CardType.Spell)
            {
                if (chosen.spellType == SpellType.Damage)
                {
                    // Fireball targeted at player lane tower
                    Vector3 targetPos = new Vector3(laneX, -3.8f, 0f);
                    ArcaneSpell.CastFireball(targetPos, chosen.spellRadius, chosen.spellPower, false);
                }
                else
                {
                    // Heal targeted at enemy side
                    ArcaneSpell.CastHeal(spawnPos, chosen.spellRadius, chosen.spellPower, false);
                }
            }

            aiActionTimer = Random.Range(3.5f, 5.5f);
        }
        else
        {
            aiActionTimer = 1.0f;
        }
    }

    public void OnTowerDestroyed(ArcaneTower tower)
    {
        if (MatchEnded) return;

        if (tower.isCore)
        {
            // Core destroyed -> instant match finish
            EndMatch(tower.isPlayer ? "ПОРАЗКА" : "ПЕРЕМОГА", tower.isPlayer ? "Вашу фортецю знищено!" : "Фортецю противника знищено!");
        }
    }

    private void EvaluateTimeoutWinner()
    {
        // 1. Count destroyed towers (i.e. remaining towers)
        int playerTowersAlive = 0;
        int enemyTowersAlive = 0;
        float playerTotalHp = 0f;
        float enemyTotalHp = 0f;

        foreach (ArcaneTower tower in FindObjectsByType<ArcaneTower>())
        {
            if (tower.isPlayer)
            {
                if (tower.IsAlive)
                {
                    playerTowersAlive++;
                    playerTotalHp += tower.hp;
                }
            }
            else
            {
                if (tower.IsAlive)
                {
                    enemyTowersAlive++;
                    enemyTotalHp += tower.hp;
                }
            }
        }

        if (playerTowersAlive > enemyTowersAlive)
        {
            EndMatch("ПЕРЕМОГА", $"За кількістю вцілілих башт ({playerTowersAlive} проти {enemyTowersAlive})");
        }
        else if (enemyTowersAlive > playerTowersAlive)
        {
            EndMatch("ПОРАЗКА", $"У противника вціліло більше башт ({enemyTowersAlive} проти {playerTowersAlive})");
        }
        else
        {
            // Equal towers -> compare total HP
            if (playerTotalHp > enemyTotalHp)
            {
                EndMatch("ПЕРЕМОГА", $"За загальним запасом HP ({Mathf.RoundToInt(playerTotalHp)} проти {Mathf.RoundToInt(enemyTotalHp)})");
            }
            else if (enemyTotalHp > playerTotalHp)
            {
                EndMatch("ПОРАЗКА", $"За загальним запасом HP ({Mathf.RoundToInt(enemyTotalHp)} проти {Mathf.RoundToInt(playerTotalHp)})");
            }
            else
            {
                EndMatch("НІЧИЯ", "Рівна кількість башт та HP!");
            }
        }
    }

    public void EndMatch(string title, string details)
    {
        if (MatchEnded) return;
        MatchEnded = true;

        if (statusText != null) statusText.text = title;
        if (resultDetailsText != null) resultDetailsText.text = details;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void RestartMatch()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(currentScene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
