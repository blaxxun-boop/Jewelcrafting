using System;
using System.Collections.Generic;
using System.Linq;
using Jewelcrafting.GemEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jewelcrafting.CrystalCase;

public class CrystalCaseUI : MonoBehaviour
{
	public CrystalCaseInteract Interact = null!;
	
	[Header("Root UI")]
	// These are everything found at the root level, below will be their individual variables. Some may be redundant.
	public RectTransform rootTransform = null!;

	public Image buttonCloseBackground = null!;
	public Image background = null!;
	public RectTransform sideBarContent = null!;
	public RectTransform scrollViewArea = null!;

	public Button storeAllButton_Root = null!;

	[Header("Button Close")]
	public Button buttonClose = null!;
	public Image buttonCloseImage = null!; // Not Same as buttonCloseBackground from the root, this is the image on the button itself
	public Image buttonCloseBg = null!; // Same as buttonCloseBackground from the root
	public TextMeshProUGUI buttonCloseText = null!;

	[Header("Background")]
	public Image bg = null!; // Same as background from Root
	public RectTransform bgRect = null!;

	[Header("Sidebar")]
	public RectTransform sidebarTransform = null!; // same as sideBarContent from root
	public VerticalLayoutGroup sidebarVlg = null!;
	public Image sidebarBgImage = null!;

	[Header("Sidebar - Search")]
	public RectTransform searchRectTransform = null!;
	public TMP_InputField searchInputField = null!;
	public Image searchInputFieldImage = null!;
	public RectTransform searchInputFieldTextArea = null!;
	public RectMask2D searchInputFieldTextAreaMask = null!;
	public TextMeshProUGUI searchInputFieldPlaceholder = null!;
	public TextMeshProUGUI searchInputFieldText = null!;
	public RectTransform searchRemoveButtonRectTransform = null!;
	public Button searchRemoveButton = null!;
	public Image searchRemoveButtonImage = null!;
	public TextMeshProUGUI searchRemoveButtonText = null!;

	[Header("Sidebar - Color Filter")]
	public RectTransform filterDropdownRect = null!;
	public TMP_Dropdown filterDropdown = null!;
	public Image filterDropdownImage = null!;
	public RectTransform filterDropdownLabelRect = null!;
	public TextMeshProUGUI filterDropdownLabel = null!;
	public RectTransform filterDropdownArrowRect = null!;
	public Image filterDropdownArrowImage = null!;
	public RectTransform filterDropdownTemplateRect = null!;
	public RectTransform filterDropdownViewportRect = null!;
	public RectTransform filterDropdownContentRect = null!;
	public VerticalLayoutGroup filterDropdownVlg = null!;
	public Image filterDropdownScrollbarImage = null!;
	public RectTransform filterDropdownScrollbarRect = null!;
	public RectTransform filterDropdownScrollbarSlidingArea = null!;
	public RectTransform filterDropdownScrollbarHandle = null!;
	public Image filterDropdownScrollbarHandleImage = null!;

	[Header("Sidebar - Sort By")]
	public RectTransform sortByDropdownRect = null!;
	public TMP_Dropdown sortByDropdown = null!;
	public Image sortByDropdownImage = null!;
	public RectTransform sortByDropdownLabelRect = null!;
	public TextMeshProUGUI sortByDropdownLabel = null!;
	public RectTransform sortByDropdownArrowRect = null!;
	public Image sortByDropdownArrowImage = null!;
	public RectTransform sortByDropdownTemplateRect = null!;
	public RectTransform sortByDropdownViewportRect = null!;
	public RectTransform sortByDropdownContentRect = null!;
	public VerticalLayoutGroup sortByDropdownVlg = null!;
	public Image sortByDropdownScrollbarImage = null!;
	public RectTransform sortByDropdownScrollbarRect = null!;
	public RectTransform sortByDropdownScrollbarSlidingArea = null!;
	public RectTransform sortByDropdownScrollbarHandle = null!;
	public Image sortByDropdownScrollbarHandleImage = null!;

	[Header("ScrollView Area")]
	public RectTransform scrollViewAreaTransform = null!; // same as scrollViewArea from root
	public ScrollRect ScrollViewAreaScrollRect = null!;
	public Image scrollViewAreaImage = null!;
	public RectTransform scrollViewAreaViewportRect = null!;
	public Image scrollViewAreaViewportImage = null!;
	public RectTransform scrollViewAreaContentRect = null!;
	public GridLayoutGroup scrollViewAreaContentGlg = null!;
	public ContentSizeFitter scrollViewAreaContentSizeFitter = null!;
	public GemIconElement gemIconElementPrefab = null!;
	public List<GemIconElement> gemIconElements = new();
	public Scrollbar scrollViewAreaScrollbar = null!;
	public RectTransform scrollViewAreaScrollbarRect = null!;
	public RectTransform scrollViewAreaScrollbarSlidingArea = null!;
	public RectTransform scrollViewAreaScrollbarHandle = null!;
	public Image scrollViewAreaScrollbarHandleImage = null!;
	
	[Header("Store All Button")]
	public Button storeAllButton = null!;
	public Image storeAllButtonImage = null!;
	public TextMeshProUGUI storeAllButtonText = null!;

	private void Awake()
	{
		InitializeUI();
		
		gemIconElementPrefab.gameObject.SetActive(false);
	}

	private void InitializeUI()
	{
		// Last time, you didn't want me to link the buttons to methods in Unity, but I can if you want or you can do this in code.
		buttonClose.onClick.AddListener(OnButtonCloseClicked);
		storeAllButton.onClick.AddListener(OnStoreAllButtonClicked);
		searchInputField.onValueChanged.AddListener(OnSearchInputFieldChanged);
		searchRemoveButton.onClick.AddListener(OnSearchRemoveButtonClicked);
		filterDropdown.onValueChanged.AddListener(OnFilterDropdownChanged);
		sortByDropdown.onValueChanged.AddListener(OnSortByDropdownChanged);

		sortByDropdown.ClearOptions();
		sortByDropdown.MultiSelect = false;
		
		// Initialize other UI components as needed
		foreach (object sortName in Enum.GetValues(typeof(SortByCriteria)))
		{
			sortByDropdown.options.Add(new TMP_Dropdown.OptionData(Localization.instance.Localize($"$jc_crystal_case_{sortName.ToString().ToLower()}")));
		}
		if (Utils.UsesPowerRanges())
		{
			foreach (object location in Enum.GetValues(typeof(GemLocation)))
			{
				sortByDropdown.options.Add(new TMP_Dropdown.OptionData(Localization.instance.Localize($"$jc_socket_slot_{location.ToString().ToLower()}")));
			}
		}
		
		filterDropdown.ClearOptions();

		foreach (object filterName in Enum.GetValues(typeof(FilterCriteria)))
		{
			filterDropdown.options.Add(new TMP_Dropdown.OptionData(Localization.instance.Localize($"$jc_crystal_case_{filterName.ToString().ToLower()}")));
		}
		filterDropdown.value = -1;
	}

	private void OnButtonCloseClicked()
	{
		// Handle the close button click event
		Debug.Log("Close button clicked");
		// Add logic to close the UI
		CrystalCaseInteract.Hide();
	}

	private void OnStoreAllButtonClicked()
	{
		// Handle the store all button click event
		Debug.Log("Store All button clicked");
		// Add logic to store all items
		Interact.StoreGemsInCabinet();
		Interact.UpdateGemIconElements(this);
	}

	private void OnSearchInputFieldChanged(string searchText)
	{
		Debug.Log($"Search text changed: {searchText}");
		FilterGemIconElements();
	}

	private void OnSearchRemoveButtonClicked()
	{
		Debug.Log("Search remove button clicked");
		searchInputField.text = string.Empty;
	}

	private void OnFilterDropdownChanged(int selectedIndex)
	{
		Debug.Log($"Filter dropdown changed: {selectedIndex}");
		FilterGemIconElements();
	}

	private void OnSortByDropdownChanged(int selectedIndex)
	{
		Debug.Log($"Sort By dropdown changed: {selectedIndex}");
		SortByCriteria sortBy = (SortByCriteria)selectedIndex;
		SortGemIconElements(sortBy);
	}

	/// <summary>
	/// Adds a gem icon element to the grid.
	/// </summary>
	/// <param name="gemData">Data for the gem icon element.</param>
	public void AddGemIconElement(GemData gemData)
	{
		GemIconElement newElement = Instantiate(gemIconElementPrefab, scrollViewAreaContentRect);
		newElement.Initialize(this, gemData);
		gemIconElements.Add(newElement);
		newElement.gameObject.SetActive(true);
	}

	/// <summary>
	/// Clears all gem icon elements from the grid.
	/// </summary>
	public void ClearGemIconElements()
	{
		foreach (GemIconElement element in gemIconElements)
		{
			Destroy(element.gameObject);
		}

		gemIconElements.Clear();
	}

	/// <summary>
	/// Updates the UI based on the current list of gem icon elements.
	/// </summary>
	public void UpdateGemIconElements(List<GemData> gems)
	{
		ClearGemIconElements();
		foreach (GemData gem in gems)
		{
			AddGemIconElement(gem);
		}
	}

	/// <summary>
	/// Filters gem icon elements based on the search input.
	/// </summary>
	public void FilterGemIconElements()
	{
		string searchText = searchInputField.text;
		FilterCriteria filterCriteria = (FilterCriteria)filterDropdown.value;
		List<GemIconElement> filteredGems = gemIconElements.Where(g => g.MatchesSearch(searchText) && g.MatchesFilters(filterCriteria)).ToList();
		foreach (GemIconElement element in gemIconElements)
		{
			element.gameObject.SetActive(filteredGems.Contains(element));
		}
	}

	/// <summary>
	/// Sorts gem icon elements based on the selected criteria.
	/// </summary>
	/// <param name="sortBy">Criteria to sort gem icons.</param>
	public void SortGemIconElements(SortByCriteria sortBy)
	{
		gemIconElements = gemIconElements.OrderBy(g => g.GetSortValue(sortBy)).ToList();
		for (int i = 0; i < gemIconElements.Count; ++i)
		{
			gemIconElements[i].transform.SetSiblingIndex(i);
		}
	}
}

[Flags]
public enum FilterCriteria
{
	Simple = 0x1,
	Advanced = 0x2,
	Perfect = 0x4,
	Boss = 0x8,
	Merged = 0x10,
	Unmerged = 0x20,
	Corrupted = 0x40,
	Uncorrupted = 0x80,
}

public enum SortByCriteria
{
	Name,
	Worth,
}

[Serializable]
public class GemData // All just examples.
{
	public string name = null!;
	public string rawValue = null!;
	public Sprite icon = null!;
	public FilterCriteria filters;
	public float worth;
	public string effects = null!;
	public Dictionary<GemLocation, float> strength = new();
}

public class GemIconElement : MonoBehaviour
{
	public RectTransform rectTransform = null!;
	public VerticalLayoutGroup vlg = null!;
	public Image background = null!;

	[Header("Top Elements")]
	public RectTransform topElements = null!;
	public Image iconBkg = null!; // This is defaulted to off, but can be turned on if needed
	public Image icon = null!;
	public Button iconButton = null!;
	public Image iconButtonImg = null!;
	public Image gemNameBkg = null!; // This is defaulted to off, but can be turned on if needed
	public TMP_InputField gemName = null!;
	public RectMask2D gemNameMask = null!;
	public TextMeshProUGUI gemNamePlaceholder = null!;
	public TextMeshProUGUI gemNameText = null!;

	[Header("Bottom Elements")]
	public RectTransform bottomElements = null!;

	public TMP_InputField listOfGemEffects = null!;
	public RectMask2D listOfGemEffectsMask = null!;
	public TextMeshProUGUI listOfGemEffectsPlaceholder = null!;
	public TextMeshProUGUI listOfGemEffectsText = null!;

	private GemData gemData = null!;
	private CrystalCaseUI ui = null!;

	public void Initialize(CrystalCaseUI ui, GemData data)
	{
		this.ui = ui;
		gemData = data;
		gemName.text = Localization.instance.Localize(data.name);
		listOfGemEffects.text = Localization.instance.Localize(data.effects);
		icon.sprite = data.icon;
		iconButton.onClick.AddListener(OnGemIconClicked);
	}

	public bool MatchesSearch(string searchText) => gemName.text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 || listOfGemEffects.text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
	public bool MatchesFilters(FilterCriteria filters) => (gemData.filters & filters) != 0;

	public object GetSortValue(SortByCriteria criteria)
	{
		return criteria switch
		{
			SortByCriteria.Name => gemName.text,
			SortByCriteria.Worth => -gemData.worth,
			_ => gemData.strength.TryGetValue((GemLocation)(1ul << ((int)criteria - 1)), out float strength) ? -strength - gemData.worth : 0,
		};
	}

	public void OnGemIconClicked()
	{
		if (ui.Interact.MoveToInventory(gemData))
		{
			ui.gemIconElements.Remove(this);
			Destroy(gameObject);
		}
	}
}
