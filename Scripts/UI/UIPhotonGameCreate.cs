﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class FixMapSelection
{
    public SceneNameField scene;
    [Tooltip("Set its size to 0 if you don't want to fix game rules")]
    public BaseNetworkGameRule[] availableGameRules;
}

public class UIPhotonGameCreate : UIBase
{
    public string defaultRoomName = "Let's play together !!";
    public byte maxPlayerCustomizable = 32;
    public InputField inputRoomName;
    public InputField inputRoomPassword;
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
    public Dropdown mapList;
    public FixMapSelection[] fixMaps;
    [HideInInspector]
    // TODO: Will be deleted later
    public MapSelection[] maps;
    [Header("Game rules")]
    public Dropdown gameRuleList;
    [Header("Objects for difference kind of dialog usage")]
    public GameObject[] createUiObjects;
    public GameObject[] updateUiObjects;

    protected readonly List<MapSelection> fixMapSelections = new List<MapSelection>();
    protected BaseNetworkGameRule[] gameRules;
    protected bool dontApplyUpdates;
    protected bool isForUpdate;

    private void Start()
    {
        if (maps != null && maps.Length > 0)
            UpdateNetworkGameInstanceMaps();
    }

    public virtual void OnClickCreateGame()
    {
        if (isForUpdate)
        {
            Debug.LogWarning("The dialog showed for update, cannot use for create game or room");
            return;
        }
        UpdateNetworkManager();
        SimplePhotonNetworkManager.Singleton.CreateRoom();
        Hide();
    }

    public virtual void OnClickCreateWaitingRoom()
    {
        if (isForUpdate)
        {
            Debug.LogWarning("The dialog showed for update, cannot use for create game or room");
            return;
        }
        UpdateNetworkManager();
        SimplePhotonNetworkManager.Singleton.CreateWaitingRoom();
        Hide();
    }

    public virtual void OnClickUpdateWaitingRoom()
    {
        if (!isForUpdate)
        {
            Debug.LogWarning("The dialog not showed for update, cannot use for update waiting room");
            return;
        }
        UpdateNetworkManager();
        Hide();
    }

    protected void UpdateNetworkManager()
    {
        if (dontApplyUpdates)
            return;

        var networkGameManager = SimplePhotonNetworkManager.Singleton as BaseNetworkGameManager;

        // Set room name
        string roomName = inputRoomName == null ? defaultRoomName : inputRoomName.text;
        networkGameManager.SetRoomName(roomName);

        // Set room password
        string roomPassword = inputRoomPassword == null ? string.Empty : inputRoomPassword.text;
        networkGameManager.SetRoomPassword(roomPassword);

        // Set max player
        string maxPlayerString = inputMaxPlayer == null ? "0" : inputMaxPlayer.text;
        byte maxPlayer = maxPlayerCustomizable;
        if (!byte.TryParse(maxPlayerString, out maxPlayer) || maxPlayer > maxPlayerCustomizable)
            maxPlayer = maxPlayerCustomizable;
        // Force max player to be even number
        byte evenAmount = (byte)((int)maxPlayer / 2 * 2);
        if (maxPlayer != evenAmount)
            maxPlayer = evenAmount;
        networkGameManager.SetMaxConnections(maxPlayer);

        // Set game rule
        var selected = GetSelectedGameRule();
        selected.botCount = inputBotCount == null ? selected.DefaultBotCount : int.Parse(inputBotCount.text);
        selected.matchTime = inputMatchTime == null ? selected.DefaultMatchTime : int.Parse(inputMatchTime.text);
        selected.matchKill = inputMatchKill == null ? selected.DefaultMatchKill : int.Parse(inputMatchKill.text);
        selected.matchScore = inputMatchScore == null ? selected.DefaultMatchScore : int.Parse(inputMatchScore.text);
        networkGameManager.SetGameRule(selected);
    }

    public void OnMapListChange(int value)
    {
        if (gameRuleList != null)
            gameRuleList.ClearOptions();

        var selectedMap = GetSelectedMap();

        if (selectedMap == null)
        {
            Debug.LogError("Invalid map selection");
            return;
        }

        SimplePhotonNetworkManager.Singleton.SetRoomOnlineScene(selectedMap.scene);

        previewImage.sprite = selectedMap.previewImage;
        gameRules = selectedMap.availableGameRules;

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
            inputBotCount.onEndEdit.RemoveListener(OnBotCountChanged);
            inputBotCount.onEndEdit.AddListener(OnBotCountChanged);
        }

        if (inputMatchTime != null)
        {
            inputMatchTime.contentType = InputField.ContentType.IntegerNumber;
            inputMatchTime.text = selected.DefaultMatchTime.ToString();
            inputMatchTime.onEndEdit.RemoveListener(OnMatchTimeChanged);
            inputMatchTime.onEndEdit.AddListener(OnMatchTimeChanged);
        }

        if (inputMatchKill != null)
        {
            inputMatchKill.contentType = InputField.ContentType.IntegerNumber;
            inputMatchKill.text = selected.DefaultMatchKill.ToString();
            inputMatchKill.onEndEdit.RemoveListener(OnMatchKillChanged);
            inputMatchKill.onEndEdit.AddListener(OnMatchKillChanged);
        }

        if (inputMatchScore != null)
        {
            inputMatchScore.contentType = InputField.ContentType.IntegerNumber;
            inputMatchScore.text = selected.DefaultMatchScore.ToString();
            inputMatchScore.onEndEdit.RemoveListener(OnMatchScoreChanged);
            inputMatchScore.onEndEdit.AddListener(OnMatchScoreChanged);
        }
    }

    public void OnMaxPlayerChanged(string value)
    {
        if (dontApplyUpdates)
            return;
        byte maxPlayer = maxPlayerCustomizable;
        if (!byte.TryParse(value, out maxPlayer) || maxPlayer > maxPlayerCustomizable)
        {
            maxPlayer = maxPlayerCustomizable;
            inputMaxPlayer.text = maxPlayer.ToString();
        }
        // Force max player to be even number
        byte evenAmount = (byte)((int)maxPlayer / 2 * 2);
        if (maxPlayer != evenAmount)
        {
            maxPlayer = evenAmount;
            inputMaxPlayer.text = maxPlayer.ToString();
        }
    }

    public void OnBotCountChanged(string value)
    {
        int botCount = 0;
        if (!int.TryParse(value, out botCount))
            inputBotCount.text = botCount.ToString();
    }

    public void OnMatchTimeChanged(string value)
    {
        int matchTime = 0;
        if (!int.TryParse(value, out matchTime))
            inputMatchTime.text = matchTime.ToString();
    }

    public void OnMatchKillChanged(string value)
    {
        int matchKill = 0;
        if (!int.TryParse(value, out matchKill))
            inputMatchKill.text = matchKill.ToString();
    }

    public void OnMatchScoreChanged(string value)
    {
        int matchScore = 0;
        if (!int.TryParse(value, out matchScore))
            inputMatchScore.text = matchScore.ToString();
    }

    private void SetupUIs()
    {
        if (mapList != null)
        {
            mapList.ClearOptions();
            var mapListOptions = new List<Dropdown.OptionData>();
            if (fixMapSelections.Count > 0)
            {
                mapListOptions = fixMapSelections.Select(a => new Dropdown.OptionData(a.mapName)).ToList();
            }
            else
            {
                mapListOptions = BaseNetworkGameInstance.Singleton.maps.Select(a => new Dropdown.OptionData(a.mapName)).ToList();
            }
            mapList.AddOptions(mapListOptions);
            mapList.onValueChanged.RemoveListener(OnMapListChange);
            mapList.onValueChanged.AddListener(OnMapListChange);
        }

        if (inputRoomName != null)
            inputRoomName.text = defaultRoomName;

        if (inputRoomPassword != null)
            inputRoomPassword.text = string.Empty;

        if (inputMaxPlayer != null)
        {
            inputMaxPlayer.contentType = InputField.ContentType.IntegerNumber;
            inputMaxPlayer.text = maxPlayerCustomizable.ToString();
            inputMaxPlayer.onEndEdit.RemoveListener(OnMaxPlayerChanged);
            inputMaxPlayer.onEndEdit.AddListener(OnMaxPlayerChanged);
            OnMaxPlayerChanged(maxPlayerCustomizable.ToString());
        }

        OnMapListChange(0);

        // Set UI objects visibilities
        foreach (var createUiObject in createUiObjects)
        {
            createUiObject.SetActive(true);
        }
        foreach (var updateUiObject in updateUiObjects)
        {
            updateUiObject.SetActive(false);
        }
    }

    public override void Show()
    {
        base.Show();

        fixMapSelections.Clear();
        if (fixMaps != null && fixMaps.Length > 0)
        {
            foreach (var fixMap in fixMaps)
            {
                MapSelection mapSelection;
                if (!fixMap.scene.IsSet() || !BaseNetworkGameInstance.MapListBySceneNames.TryGetValue(fixMap.scene.SceneName, out mapSelection))
                {
                    Debug.LogWarning("There is no map with scene " + fixMap.scene.SceneName + " set in network game instance.");
                    continue;
                }
                MapSelection newMapSelection = new MapSelection();
                newMapSelection.mapName = mapSelection.mapName;
                newMapSelection.scene = mapSelection.scene;
                newMapSelection.previewImage = mapSelection.previewImage;
                if (fixMap.availableGameRules != null && fixMap.availableGameRules.Length > 0)
                    newMapSelection.availableGameRules = fixMap.availableGameRules.Where(a => mapSelection.availableGameRules.Contains(a)).ToArray();
                if (newMapSelection.availableGameRules == null || newMapSelection.availableGameRules.Length == 0)
                    newMapSelection.availableGameRules = mapSelection.availableGameRules;
                fixMapSelections.Add(newMapSelection);
            }
        }

        SetupUIs();
        isForUpdate = false;
    }

    public void ShowForUpdate()
    {
        // Hide this dialog if player is not in room
        if (!PhotonNetwork.isMasterClient || !PhotonNetwork.inRoom)
            Hide();

        base.Show();

        Hashtable oldProperties = PhotonNetwork.room.CustomProperties;

        // Don't apply updates in this function, will allow it to updates later
        dontApplyUpdates = true;

        // Setup events for inputs
        SetupUIs();

        // Set data by customized data
        if (inputRoomName != null)
            inputRoomName.text = (string)oldProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_ROOM_NAME];

        // Set data by customized data
        if (inputRoomPassword != null)
            inputRoomPassword.text = (string)oldProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_ROOM_PASSWORD];

        // Set data by customized data
        if (inputMaxPlayer != null)
            inputMaxPlayer.text = PhotonNetwork.room.MaxPlayers.ToString();

        int indexOfMap = -1;
        object sceneNameObject;
        if (oldProperties.TryGetValue(SimplePhotonNetworkManager.CUSTOM_ROOM_SCENE_NAME, out sceneNameObject))
        {
            for (int i = 0; i < BaseNetworkGameInstance.Singleton.maps.Length; ++i)
            {
                if (BaseNetworkGameInstance.Singleton.maps[i].scene.SceneName == (string)sceneNameObject)
                {
                    indexOfMap = i;
                    break;
                }
            }
        }

        // Setup events for inputs and set data by customized data
        OnMapListChange(indexOfMap);

        int indexOfRule = -1;
        object gameRuleObject;
        if (oldProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE, out gameRuleObject))
        {
            for (int i = 0; i < gameRules.Length; ++i)
            {
                if (gameRules[i].name == (string)gameRuleObject)
                {
                    indexOfRule = i;
                    break;
                }
            }
        }

        // Setup events for inputs and set data by customized data
        OnGameRuleListChange(indexOfRule);

        object botCountObject;
        if (inputBotCount != null && oldProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_BOT_COUNT, out botCountObject))
            inputBotCount.text = botCountObject.ToString();

        object matchTimeObject;
        if (inputMatchTime != null && oldProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_TIME, out matchTimeObject))
            inputMatchTime.text = matchTimeObject.ToString();

        object matchKillObject;
        if (inputMatchKill != null && oldProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_KILL, out matchKillObject))
            inputMatchKill.text = matchKillObject.ToString();

        object matchScoreObject;
        if (inputMatchScore != null && oldProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_SCORE, out matchScoreObject))
            inputMatchScore.text = matchScoreObject.ToString();

        // Allow it to updates to server
        dontApplyUpdates = false;

        // Set UI objects visibilities
        foreach (var createUiObject in createUiObjects)
        {
            createUiObject.SetActive(false);
        }
        foreach (var updateUiObject in updateUiObjects)
        {
            updateUiObject.SetActive(true);
        }
        isForUpdate = true;
    }

    public MapSelection GetSelectedMap()
    {
        var text = mapList.captionText.text;
        if (fixMapSelections.Count > 0)
        {
            return fixMapSelections.FirstOrDefault(m => m.mapName == text);
        }
        return BaseNetworkGameInstance.Singleton.maps.FirstOrDefault(m => m.mapName == text);
    }

    public BaseNetworkGameRule GetSelectedGameRule()
    {
        var text = gameRuleList.captionText.text;
        return gameRules.FirstOrDefault(m => m.Title == text);
    }

    [ContextMenu("Update Network Game Instance Maps")]
    public void UpdateNetworkGameInstanceMaps()
    {
        BaseNetworkGameInstance gameInstance = FindObjectOfType<BaseNetworkGameInstance>();
        if (gameInstance != null && (gameInstance.maps == null || gameInstance.maps.Length == 0))
            gameInstance.maps = maps;
    }
}
