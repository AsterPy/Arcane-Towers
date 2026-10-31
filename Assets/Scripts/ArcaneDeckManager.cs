using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ArcaneDeckManager : MonoBehaviour
{
    public static ArcaneDeckManager Instance { get; private set; }

    [Header("Prefabs & References")]
    public ArcaneUnit unitBasePrefab;

    [Header("Deck Setup")]
    public List<CardInfo> initialDeck = new List<CardInfo>();

    [Header("UI Slots")]
    public Button[] handButtons; // 4 buttons
    public Text[] handNameTexts;
    public Text[] handCostTexts;
    public Image[] handBackgrounds;
    public GameObject selectedIndicator;

    [Header("Next Card UI")]
    public Text nextCardNameText;
    public Text nextCardCostText;
    public Image nextCardBackground;

    [Header("Placement Feedback")]
    public GameObject placementGhost;

    private List<CardInfo> currentHand = new List<CardInfo>();
    private Queue<CardInfo> cardQueue = new Queue<CardInfo>();
    private int selectedSlot = -1;

    public CardInfo SelectedCard => (selectedSlot >= 0 && selectedSlot < currentHand.Count) ? currentHand[selectedSlot] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeDeck();
    }

    private void Start()
    {
        EnsureUIReferences();

        if (handButtons != null)
        {
            for (int i = 0; i < handButtons.Length; i++)
            {
                if (handButtons[i] == null) continue;
                int index = i;
                handButtons[i].onClick.AddListener(() => OnCardSlotClicked(index));
            }
        }

        UpdateUI();
    }

    public void EnsureUIReferences()
    {
        bool needsButtons = handButtons == null || handButtons.Length == 0 || handButtons[0] == null;
        if (needsButtons)
        {
            GameObject deckPanel = GameObject.Find("DeckPanel");
            if (deckPanel != null)
            {
                handButtons = new Button[4];
                handBackgrounds = new Image[4];
                for (int i = 0; i < 4; i++)
                {
                    Transform slot = deckPanel.transform.Find($"CardSlot_{i}");
                    if (slot != null)
                    {
                        handButtons[i] = slot.GetComponent<Button>();
                        handBackgrounds[i] = slot.GetComponent<Image>();
                    }
                }

                if (selectedIndicator == null)
                {
                    Transform sel = deckPanel.transform.Find("SelectionIndicator");
                    if (sel != null) selectedIndicator = sel.gameObject;
                }

                if (nextCardBackground == null)
                {
                    Transform nc = deckPanel.transform.Find("NextCardSlot");
                    if (nc != null) nextCardBackground = nc.GetComponent<Image>();
                }
            }
        }
    }

    private void InitializeDeck()
    {
        currentHand.Clear();
        cardQueue.Clear();

        // 4 in hand
        for (int i = 0; i < 4 && i < initialDeck.Count; i++)
        {
            currentHand.Add(initialDeck[i]);
        }

        // 2 in queue
        for (int i = 4; i < initialDeck.Count; i++)
        {
            cardQueue.Enqueue(initialDeck[i]);
        }
    }

    private void Update()
    {
        if (ArcaneGameManager.Instance == null || ArcaneGameManager.Instance.MatchEnded)
            return;

        UpdateEnergyButtonsState();
        HandlePlacementInput();
    }

    private void OnCardSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentHand.Count) return;

        CardInfo card = currentHand[slotIndex];
        if (ArcaneGameManager.Instance != null && ArcaneGameManager.Instance.Energy < card.cost)
        {
            // Not enough energy
            ArcaneAudio.PlayNoEnergy();
            return;
        }

        ArcaneAudio.PlayCardClick();

        if (selectedSlot == slotIndex)
        {
            // Toggle deselect
            selectedSlot = -1;
        }
        else
        {
            selectedSlot = slotIndex;
        }

        UpdateSelectionVisuals();
    }

    private void HandlePlacementInput()
    {
        if (selectedSlot == -1) return;

        CardInfo card = SelectedCard;
        if (card == null)
        {
            selectedSlot = -1;
            UpdateSelectionVisuals();
            return;
        }

        bool leftClick = false;
        bool rightClick = false;
        Vector3 pointerScreenPos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            leftClick = Mouse.current.leftButton.wasPressedThisFrame;
            rightClick = Mouse.current.rightButton.wasPressedThisFrame;
            Vector2 mPos = Mouse.current.position.ReadValue();
            pointerScreenPos = new Vector3(mPos.x, mPos.y, 0f);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (!leftClick && Input.GetMouseButtonDown(0))
        {
            leftClick = true;
            pointerScreenPos = Input.mousePosition;
        }
        if (!rightClick && Input.GetMouseButtonDown(1))
        {
            rightClick = true;
        }
#endif

        // Cancel on right click
        if (rightClick)
        {
            selectedSlot = -1;
            UpdateSelectionVisuals();
            return;
        }

        // Click to deploy
        if (leftClick)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Clicked on UI, let UI handle it
                return;
            }

            Vector3 mouseWorld = Camera.main != null ? Camera.main.ScreenToWorldPoint(pointerScreenPos) : Vector3.zero;
            mouseWorld.z = 0f;

            TryPlayCard(selectedSlot, mouseWorld);
        }
    }

    public bool TryPlayCard(int slotIndex, Vector3 worldPosition)
    {
        if (slotIndex < 0 || slotIndex >= currentHand.Count) return false;
        CardInfo card = currentHand[slotIndex];

        if (ArcaneGameManager.Instance.Energy < card.cost)
            return false;

        if (card.cardType == CardType.Troop)
        {
            // Troop placement allowed only on player's side (y <= 0.2f) and within arena bounds (matching Canvas)
            if (worldPosition.y > 0.2f || Mathf.Abs(worldPosition.x) > 5.4f || worldPosition.y < -7.8f)
            {
                return false;
            }

            SpawnTroop(card, worldPosition, true);
        }
        else if (card.cardType == CardType.Spell)
        {
            // Spell placement allowed anywhere in the arena
            CastSpell(card, worldPosition, true);
        }

        // Spend energy
        ArcaneGameManager.Instance.SpendEnergy(card.cost);

        // Cycle card (hand -> queue, queue -> hand)
        CardInfo played = currentHand[slotIndex];
        CardInfo next = cardQueue.Dequeue();
        currentHand[slotIndex] = next;
        cardQueue.Enqueue(played);

        selectedSlot = -1;
        UpdateSelectionVisuals();
        UpdateUI();
        return true;
    }

    public ArcaneUnit SpawnTroop(CardInfo card, Vector3 position, bool isPlayer)
    {
        ArcaneUnit unit = Instantiate(unitBasePrefab, position, Quaternion.identity);
        unit.gameObject.SetActive(true);

        unit.isPlayer = isPlayer;
        unit.unitId = card.id;
        unit.unitName = card.cardName;
        unit.maxHp = card.maxHp;
        unit.hp = card.maxHp;
        unit.damage = card.damage;
        unit.moveSpeed = card.moveSpeed;
        unit.attackRange = card.attackRange;
        unit.attackInterval = card.attackInterval;
        unit.category = card.unitCategory;
        unit.attackableTargets = card.targetCategories;
        unit.isRanged = card.isRanged;
        unit.projectileSpeed = card.projectileSpeed;
        unit.barOffsetY = card.healthBarOffsetY > 0f ? card.healthBarOffsetY : 0.70f;
        unit.barWidth = card.healthBarWidth > 0f ? card.healthBarWidth : 0.44f;
        float s = card.modelScale > 0f ? card.modelScale : 1f;
        unit.transform.localScale = new Vector3(s, s, 1f);

        // Visual setup using animated frames and ground shadow
        ArcaneVisuals.ApplyUnitVisuals(unit, card.id, isPlayer);

        ArcaneAudio.PlaySpawn(card.id);

        return unit;
    }

    public void CastSpell(CardInfo card, Vector3 position, bool isPlayer)
    {
        if (card.spellType == SpellType.Damage)
        {
            ArcaneSpell.CastFireball(position, card.spellRadius, card.spellPower, isPlayer);
        }
        else if (card.spellType == SpellType.Heal)
        {
            ArcaneSpell.CastHeal(position, card.spellRadius, card.spellPower, isPlayer);
        }
    }

    private void UpdateUI()
    {
        if (handButtons == null) return;

        for (int i = 0; i < handButtons.Length; i++)
        {
            if (handButtons[i] == null) continue;

            if (i < currentHand.Count)
            {
                CardInfo card = currentHand[i];
                handButtons[i].gameObject.SetActive(true);
                if (handNameTexts != null && i < handNameTexts.Length && handNameTexts[i] != null)
                {
                    handNameTexts[i].text = card.cardName;
                    handNameTexts[i].gameObject.SetActive(card.cardSprite == null);
                }
                if (handCostTexts != null && i < handCostTexts.Length && handCostTexts[i] != null)
                {
                    handCostTexts[i].text = card.cost.ToString();
                }
                if (handBackgrounds != null && i < handBackgrounds.Length && handBackgrounds[i] != null)
                {
                    if (card.cardSprite != null)
                    {
                        handBackgrounds[i].sprite = card.cardSprite;
                        handBackgrounds[i].color = Color.white;
                    }
                    else
                    {
                        handBackgrounds[i].sprite = null;
                        handBackgrounds[i].color = card.cardColor * 0.7f;
                    }
                }
            }
            else
            {
                handButtons[i].gameObject.SetActive(false);
            }
        }

        // Next card UI
        if (cardQueue.Count > 0)
        {
            CardInfo next = cardQueue.Peek();
            if (nextCardNameText != null)
            {
                nextCardNameText.text = next.cardName;
                nextCardNameText.gameObject.SetActive(next.cardSprite == null);
            }
            if (nextCardCostText != null)
            {
                nextCardCostText.text = next.cost.ToString();
            }
            if (nextCardBackground != null)
            {
                if (next.cardSprite != null)
                {
                    nextCardBackground.sprite = next.cardSprite;
                    nextCardBackground.color = Color.white;
                }
                else
                {
                    nextCardBackground.sprite = null;
                    nextCardBackground.color = next.cardColor * 0.7f;
                }
            }
        }

        UpdateSelectionVisuals();
    }

    private void UpdateEnergyButtonsState()
    {
        if (handButtons == null) return;
        float currentEnergy = ArcaneGameManager.Instance != null ? ArcaneGameManager.Instance.Energy : 0f;

        for (int i = 0; i < handButtons.Length; i++)
        {
            if (handButtons[i] == null) continue;

            if (i < currentHand.Count)
            {
                bool canAfford = currentEnergy >= currentHand[i].cost;
                handButtons[i].interactable = canAfford;
                if (handBackgrounds != null && i < handBackgrounds.Length && handBackgrounds[i] != null)
                {
                    // Semi-transparent (40% alpha) when not enough elixir, full opacity (100% alpha) when affordable
                    handBackgrounds[i].color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.40f);
                }
            }
        }
    }

    private void UpdateSelectionVisuals()
    {
        if (selectedIndicator == null) return;

        if (selectedSlot >= 0 && handButtons != null && selectedSlot < handButtons.Length && handButtons[selectedSlot] != null)
        {
            selectedIndicator.SetActive(true);
            selectedIndicator.transform.position = handButtons[selectedSlot].transform.position;
        }
        else
        {
            selectedIndicator.SetActive(false);
        }
    }
}
