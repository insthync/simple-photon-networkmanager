using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIPhotonGameCreate : UIBase
{
    [System.Serializable]
    public class MapSelection
    {
        public string mapName;
        public SceneNameField scene;
        public Sprite previewImage;
        public BaseNetworkGameRule[] availableGameRules;
    }

    public int maxPlayerCustomizable = 32;
    public InputField inputRoomName;
    public InputField inputMaxPlayer;
    [Header("Match Bot Count")]
    public GameObject containerBotCount;
    public InputField inputBotCount;
    [Header("Match Time")]
    public GameObject containerMatchTime;
    public InputField inputMatchTime;
    [Header("Match Kill")]
    public GameObject containerMatchKill;
    public InputField inputMatchKill;
    [Header("Match Score")]
    public GameObject containerMatchScore;
    public InputField inputMatchScore;
    [Header("Maps")]
    public Image previewImage;
    public MapSelection[] maps;
    public Dropdown mapList;
    [Header("Game rules")]
    public Dropdown gameRuleList;

    private BaseNetworkGameRule[] gameRules;

    public virtual void OnClickCreateGame()
    {
        var networkManager = SimplePhotonNetworkManager.Singleton;

        if (inputRoomName != null)
            networkManager.roomName = inputRoomName.text;

        if (inputMaxPlayer != null)
            networkManager.maxConnections = byte.Parse(inputMaxPlayer.text);
        
        networkManager.CreateRoom();
    }

    public void OnMapListChange(int value)
    {
        if (gameRuleList != null)
            gameRuleList.ClearOptions();

        var selected = GetSelectedMap();
        
        if (selected == null)
        {
            Debug.LogError("Invalid map selection");
            return;
        }

        SimplePhotonNetworkManager.Singleton.SetRoomOnlineScene(selected.scene);

        previewImage.sprite = selected.previewImage;
        gameRules = selected.availableGameRules;

        if (gameRuleList != null)
        {
            gameRuleList.AddOptions(gameRules.Select(a => new Dropdown.OptionData(a.Title)).ToList());
            gameRuleList.onValueChanged.RemoveListener(OnGameRuleListChange);
            gameRuleList.onValueChanged.AddListener(OnGameRuleListChange);
        }

        OnGameRuleListChange(0);
    }

    public void OnGameRuleListChange(int value)
    {
        var selected = GetSelectedGameRule();

        if (selected == null)
        {
            Debug.LogError("Invalid game rule selection");
            return;
        }

        if (containerBotCount != null)
            containerBotCount.SetActive(selected.HasOptionBotCount);

        if (containerMatchTime != null)
            containerMatchTime.SetActive(selected.HasOptionMatchTime);

        if (containerMatchKill != null)
            containerMatchKill.SetActive(selected.HasOptionMatchKill);

        if (containerMatchScore != null)
            containerMatchScore.SetActive(selected.HasOptionMatchScore);

        if (inputBotCount != null)
        {
            inputBotCount.contentType = InputField.ContentType.IntegerNumber;
            inputBotCount.text = selected.DefaultBotCount.ToString();
            inputBotCount.onValueChanged.RemoveListener(OnBotCountChanged);
            inputBotCount.onValueChanged.AddListener(OnBotCountChanged);
        }

        if (inputMatchTime != null)
        {
            inputMatchTime.contentType = InputField.ContentType.IntegerNumber;
            inputMatchTime.text = selected.DefaultMatchTime.ToString();
            inputMatchTime.onValueChanged.RemoveListener(OnMatchTimeChanged);
            inputMatchTime.onValueChanged.AddListener(OnMatchTimeChanged);
        }

        if (inputMatchKill != null)
        {
            inputMatchKill.contentType = InputField.ContentType.IntegerNumber;
            inputMatchKill.text = selected.DefaultMatchKill.ToString();
            inputMatchKill.onValueChanged.RemoveListener(OnMatchKillChanged);
            inputMatchKill.onValueChanged.AddListener(OnMatchKillChanged);
        }

        if (inputMatchScore != null)
        {
            inputMatchScore.contentType = InputField.ContentType.IntegerNumber;
            inputMatchScore.text = selected.DefaultMatchScore.ToString();
            inputMatchScore.onValueChanged.RemoveListener(OnMatchScoreChanged);
            inputMatchScore.onValueChanged.AddListener(OnMatchScoreChanged);
        }

        UpdateNetworkManager();
    }

    public void OnMaxPlayerChanged(string value)
    {
        int maxPlayer = maxPlayerCustomizable;
        if (!int.TryParse(value, out maxPlayer) || maxPlayer > maxPlayerCustomizable)
            inputMaxPlayer.text = maxPlayer.ToString();
    }

    public void OnBotCountChanged(string value)
    {
        int botCount = 0;
        if (!int.TryParse(value, out botCount))
            inputBotCount.text = botCount.ToString();
        UpdateNetworkManager();
    }

    public void OnMatchTimeChanged(string value)
    {
        int matchTime = 0;
        if (!int.TryParse(value, out matchTime))
            inputMatchTime.text = matchTime.ToString();
        UpdateNetworkManager();
    }

    public void OnMatchKillChanged(string value)
    {
        int matchKill = 0;
        if (!int.TryParse(value, out matchKill))
            inputMatchKill.text = matchKill.ToString();
        UpdateNetworkManager();
    }

    public void OnMatchScoreChanged(string value)
    {
        int matchScore = 0;
        if (!int.TryParse(value, out matchScore))
            inputMatchScore.text = matchScore.ToString();
        UpdateNetworkManager();
    }

    protected void UpdateNetworkManager()
    {
        var selected = GetSelectedGameRule();
        var networkGameManager = SimplePhotonNetworkManager.Singleton as BaseNetworkGameManager;
        selected.botCount = inputBotCount == null ? selected.DefaultBotCount : int.Parse(inputBotCount.text);
        selected.matchTime = inputMatchTime == null ? selected.DefaultMatchTime : int.Parse(inputMatchTime.text);
        selected.matchKill = inputMatchKill == null ? selected.DefaultMatchKill : int.Parse(inputMatchKill.text);
        selected.matchScore = inputMatchScore == null ? selected.DefaultMatchScore : int.Parse(inputMatchScore.text);
        networkGameManager.gameRule = selected;
    }

    public override void Show()
    {
        base.Show();

        if (mapList != null)
        {
            mapList.ClearOptions();
            mapList.AddOptions(maps.Select(a => new Dropdown.OptionData(a.mapName)).ToList());
            mapList.onValueChanged.RemoveListener(OnMapListChange);
            mapList.onValueChanged.AddListener(OnMapListChange);
        }

        if (inputMaxPlayer != null)
        {
            inputMaxPlayer.contentType = InputField.ContentType.IntegerNumber;
            inputMaxPlayer.text = maxPlayerCustomizable.ToString();
            inputMaxPlayer.onValueChanged.RemoveListener(OnMaxPlayerChanged);
            inputMaxPlayer.onValueChanged.AddListener(OnMaxPlayerChanged);
        }

        OnMapListChange(0);
    }

    public MapSelection GetSelectedMap()
    {
        var text = mapList.captionText.text;
        return maps.FirstOrDefault(m => m.mapName == text);
    }

    public BaseNetworkGameRule GetSelectedGameRule()
    {
        var text = gameRuleList.captionText.text;
        return gameRules.FirstOrDefault(m => m.Title == text);
    }
}
