using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class UISangokumusou2 : MonoBehaviour {
    #region Serialized Fields
    [Header("General")]
    [SerializeField] private UIMainMenu _uiMainMenu = null;

    [SerializeField] private Button _btnBack = null;
    [SerializeField] private Button _btnNextCharacter = null; // To left
    [SerializeField] private Button _btnPreviousCharacter = null; // To right

    [SerializeField] private RectTransform _rectSelectionCharacterFrontRoot = null;
    [SerializeField] private RectTransform _rectSelectionCharacterBackRoot = null;
    [SerializeField] private UISangokuSelectionObject _selectionObjectRes = null;

    [SerializeField] private RectTransform _rectRouteCircle = null;
    [SerializeField] private RectTransform _rectSelectFrame = null;

    [SerializeField] private GameObject _goFocusCharacterFullBodyPortraitRoot = null;
    [SerializeField] private GameObject _goFocusCharacterInfoRoot = null;

    [Header("Timelines")]
    [SerializeField] private PlayableDirector _pdFadeIn = null;
    [SerializeField] private PlayableDirector _pdFadeOut = null;    

    [Header("Focus Character Info")]    
    [SerializeField] private Image _imageFocusCharacterFullBodyPortrait = null;
    [SerializeField] private TextMeshProUGUI _textFocusCharacterName = null;
    [SerializeField] private Image _imageFocusCharacterMaxHP = null;
    [SerializeField] private Image _imageFocusCharacterMaxMP = null;
    [SerializeField] private Image _imageFocusCharacterAttack = null;
    [SerializeField] private Image _imageFocusCharacterDefense = null;

    [Header("Perform Settings")]
    [SerializeField] private float _routeOvalWidth = 20;
    [SerializeField] private float _routeOvalHeight = 10;

    [SerializeField] private float _fadeInFadeOutPeriod = 2.0f; // Seconds
    [SerializeField] private float _fadeInHorOffest = 100;
    [SerializeField] private float _fadeInVerOffest = 100;

    [SerializeField] private float _selectiontPeriod = 0.3f; // Seconds
    #endregion

    #region Internal Fields
    private bool _performing = false;
    private int _characterCount = 0;
    private int _currentcharacterIndex = 0;
    
    private List<CharacterSelectionData> _characterSelectionDataList = new List<CharacterSelectionData>();
    private List<UISangokuSelectionObject> _characterSelectionObjectList = new List<UISangokuSelectionObject>();

    // For testing
    private float _previousRouteOvalWidth = -1;
    private float _previousRouteOvalHeight = -1;
    #endregion

    #region Mono Behaviour Hooks
    private void Awake() {
        _btnBack.onClick.AddListener(ButtonBackOnClick);
        _btnNextCharacter.onClick.AddListener(ButtonNextCharacterOnClick);
        _btnPreviousCharacter.onClick.AddListener(ButtonPreviousCharacterOnClick);
    }

    private void OnDestroy() {
        _btnBack.onClick.RemoveAllListeners();
        _btnNextCharacter.onClick.RemoveAllListeners();
        _btnPreviousCharacter.onClick.RemoveAllListeners();
    }

    private void Update() {
        // For Testing
        CheckRouteSettingAndAdjust();
    }
    #endregion

    #region UI Button Handlings
    private void ButtonBackOnClick() {
        if (_performing) {
            return;
        }

        FinishSelection();
    }

    private void ButtonNextCharacterOnClick() {
        ToNextCharacter();
    }

    private void ButtonPreviousCharacterOnClick() {
        ToPreviousCharacter();
    }
    #endregion

    #region APIs
    public void StarSelection(SOCharacterData soCHaracterData, int characterAmount) {
        if (soCHaracterData == null) {
            return;
        }

        if (characterAmount <= 0) {
            Debug.LogErrorFormat("'characterAmount' can not be 0 or negetive value");
            return;
        }

        _characterCount = characterAmount;
        _currentcharacterIndex = 0;

        InitUI();
        InitCharacterSelectionData(soCHaracterData);
        InitCharacterSelectionObject();

        RefreshOrder();

        PlayFadeIn();
    }

    public void FinishSelection() {
        PlayFadeOut();
    }
    #endregion

    #region Coroutine Handlings
    private IEnumerator CoPlayFadeIn() {
        if (_performing) {
            yield break;
        }

        _performing = true;

        _pdFadeIn.Play();

        float passedTime = 0;
        float interpolation = 0;
        bool isFInish = false;

        // Initialization of selection objects
        for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
            _characterSelectionObjectList[i].gameObject.SetActive(true);
        }

        // Perform
        while (!isFInish) {
            interpolation = Mathf.Clamp01(passedTime / _fadeInFadeOutPeriod);

            for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
                Vector3 oriPos = GetCharacterPositionOnRoute(i, _characterSelectionObjectList.Count);
                Vector3 offset = GetFadeInFadeOutOffset(true, i, _characterSelectionObjectList.Count, interpolation);
                Vector3 performPos = oriPos + offset;
                _characterSelectionObjectList[i].Rect.localPosition = performPos;

                CanvasGroup cgChara = _characterSelectionObjectList[i].GetComponent<CanvasGroup>();
                if (cgChara != null) {
                    cgChara.alpha = interpolation * 0.5f;
                }
            }

            if (passedTime >= _fadeInFadeOutPeriod) {
                isFInish = true;
            }

            yield return new WaitForEndOfFrame();
            passedTime += Time.deltaTime;
        }

        _performing = false;

        RefreshPosition();        
        RefreshFocusCharacterInfo();

        _characterSelectionObjectList[_currentcharacterIndex].SetLocalScale(Vector3.one * 1.5f);
        _characterSelectionObjectList[_currentcharacterIndex].SetAlpha(1.0f);

        ShowFocusCharacterFullBodyPortrait(true);
        ShowFocusCharacterInfo(true);
        ShowFocusCharacterFrame(true);
        ShowButtons(true);
    }

    private IEnumerator CoPlayFadeOut() {
        if (_performing) {
            yield break;
        }

        _performing = true;

        _characterSelectionObjectList[_currentcharacterIndex].SetLocalScale(Vector3.one);

        _pdFadeOut.Play();

        ShowFocusCharacterFullBodyPortrait(false);
        ShowFocusCharacterInfo(false);
        ShowFocusCharacterFrame(false);
        ShowButtons(false);

        float passedTime = 0;
        float interpolation = 0;
        bool isFInish = false;

        while (!isFInish) {
            interpolation = Mathf.Clamp01(passedTime / _fadeInFadeOutPeriod);

            for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
                int positionIndex = i - _currentcharacterIndex;
                if (positionIndex < 0) {
                    positionIndex += _characterCount;
                }

                Vector3 oriPos = GetCharacterPositionOnRoute(positionIndex, _characterSelectionObjectList.Count);
                Vector3 offset = GetFadeInFadeOutOffset(false, positionIndex, _characterSelectionObjectList.Count, interpolation);
                Vector3 performPos = oriPos + offset;
                _characterSelectionObjectList[i].Rect.localPosition = performPos;

                CanvasGroup cgChara = _characterSelectionObjectList[i].GetComponent<CanvasGroup>();
                if (cgChara != null) {
                    cgChara.alpha = (1 - interpolation) * 0.5f;
                }
            }

            if (passedTime >= _fadeInFadeOutPeriod) {
                isFInish = true;
            }

            yield return new WaitForEndOfFrame();
            passedTime += Time.deltaTime;
        }

        _performing = false;

        _uiMainMenu.PlayFadeIn();
    }

    private IEnumerator Perform(int toIndex, bool isClockWise) {
        if (_performing) {
            yield break;
        }

        _performing = true;

        float performInterpolation = 0;
        float passedTime = 0;
        bool isFInish = false;

        int previousIndex = _currentcharacterIndex;
        _currentcharacterIndex = toIndex;
        RefreshOrder();

        _characterSelectionObjectList[previousIndex].SetLocalScale(Vector3.one);
        _characterSelectionObjectList[previousIndex].SetAlpha(0.5f);

        while (!isFInish) {
            performInterpolation = Mathf.Clamp01(passedTime / _selectiontPeriod);

            for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
                float fromPosInterpolation = GetCharacterPositionInterpolation(i, previousIndex);
                float toPosInterpolation = GetCharacterPositionInterpolation(i, _currentcharacterIndex);

                if (isClockWise && toPosInterpolation < fromPosInterpolation) {
                    toPosInterpolation += 1;
                }
                else if (!isClockWise && toPosInterpolation > fromPosInterpolation) {
                    toPosInterpolation -= 1;
                }

                float posInterpolation = Mathf.Lerp(fromPosInterpolation, toPosInterpolation, performInterpolation);
                Vector3 pos = GetPositionOnRoute(posInterpolation);
                _characterSelectionObjectList[i].Rect.localPosition = pos;
            }

            if (passedTime >= _selectiontPeriod) {
                isFInish = true;
            }

            yield return new WaitForEndOfFrame();
            passedTime += Time.deltaTime;
        }

        _performing = false;

        RefreshPosition();
        RefreshFocusCharacterInfo();

        _characterSelectionObjectList[_currentcharacterIndex].SetLocalScale(Vector3.one * 1.5f);
        _characterSelectionObjectList[_currentcharacterIndex].SetAlpha(1.0f);
    }
    #endregion

    #region Internal Methods
    private void InitUI() {
        ShowButtons(false);
        ShowFocusCharacterFullBodyPortrait(false);
        ShowFocusCharacterInfo(false);
        ShowFocusCharacterFrame(false);
    }

    private void InitCharacterSelectionData(SOCharacterData soCharacterData) {
        _characterSelectionDataList = new List<CharacterSelectionData>();
        for (int i = 0; i < soCharacterData.DataArray.Length; i++) {
            // Data from scriptable object
            CharacterSelectionData csData = new CharacterSelectionData();
            csData.SmallPortrait = soCharacterData.DataArray[i].SmallPortrait;
            csData.FullBodyPortrait = soCharacterData.DataArray[i].FullBodyPortrait;
            csData.Name = soCharacterData.DataArray[i].Name;
            // Data from scriptable object

            // Random value
            csData.HP = Random.Range(100, 500);
            csData.MP = Random.Range(100, 500);
            csData.Attack = Random.Range(100, 1000);
            csData.Defense = Random.Range(100, 1000);
            // Random value

            _characterSelectionDataList.Add(csData);
        }
    }

    private void InitCharacterSelectionObject() {
        for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
            Destroy(_characterSelectionObjectList[i].gameObject);
        }
        _characterSelectionObjectList.Clear();

        for (int i = 0; i < _characterCount; i++) {
            UISangokuSelectionObject newObject = Instantiate(_selectionObjectRes, _rectSelectionCharacterFrontRoot);

            newObject.SetPortrait(_characterSelectionDataList[i].SmallPortrait);
            newObject.SetColor(new Color(Random.Range(0f, 1.0f), Random.Range(0f, 1.0f), Random.Range(0f, 1.0f)));
            newObject.Rect.localPosition = GetCharacterPositionOnRoute(i, _characterCount);

            _characterSelectionObjectList.Add(newObject);
        }
    }

    private void ShowButtons(bool show) {
        _btnNextCharacter.gameObject.SetActive(show);
        _btnPreviousCharacter.gameObject.SetActive(show);
    }

    private void ShowFocusCharacterFullBodyPortrait(bool show) {
        _goFocusCharacterFullBodyPortraitRoot.SetActive(show);
    }

    private void ShowFocusCharacterInfo(bool show) {
        _goFocusCharacterInfoRoot.SetActive(show);
    }

    private void ShowFocusCharacterFrame(bool show) {
        _rectSelectFrame.gameObject.SetActive(show);
    }

    private void PlayFadeIn() {
        StartCoroutine(CoPlayFadeIn());
    }

    private void PlayFadeOut() {
        StartCoroutine(CoPlayFadeOut());
    }

    //private void ToCharacter(int characterIndex) {
    //    // TODO
    //    if (characterIndex < 0 || characterIndex >= _characterCount) {
    //        return;
    //    }

    //    //Perform(characterIndex);
    //}

    private void ToNextCharacter() {
        int toIndex = _currentcharacterIndex + 1;
        toIndex %= _characterCount;

        StartCoroutine(Perform(toIndex, false));
    }

    private void ToPreviousCharacter() {
        int toIndex = _currentcharacterIndex - 1;
        if (toIndex < 0) {
            toIndex += _characterCount;
        }

        StartCoroutine(Perform(toIndex, true));
    }

    private void CheckRouteSettingAndAdjust() {
        if (_previousRouteOvalWidth == -1 || _previousRouteOvalHeight == -1) {
            _previousRouteOvalWidth = _routeOvalWidth;
            _previousRouteOvalHeight = _routeOvalHeight;
            return;
        }

        // Check is setting changed or not
        if ((_previousRouteOvalWidth != _routeOvalWidth) || (_previousRouteOvalHeight != _routeOvalHeight)) {
            _previousRouteOvalWidth = _routeOvalWidth;
            _previousRouteOvalHeight = _routeOvalHeight;

            RefreshDecoration();
            RefreshPosition();            
        }
    }

    private void RefreshDecoration() {
        _rectRouteCircle.sizeDelta = new Vector2(_previousRouteOvalWidth * 2, _previousRouteOvalHeight * 2);
        _rectSelectFrame.anchoredPosition = new Vector3(0, -_previousRouteOvalHeight, 0);
    }

    private void RefreshPosition() {
        for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
            float posInterpolation = GetCharacterPositionInterpolation(i, _currentcharacterIndex);
            Vector3 pos = GetPositionOnRoute(posInterpolation);
            _characterSelectionObjectList[i].Rect.localPosition = pos;
        }
    }    

    private void RefreshOrder() {
        List<(float, UISangokuSelectionObject)> dataList = new List<(float, UISangokuSelectionObject)>();
        List<(float, UISangokuSelectionObject)> dataListBack = new List<(float, UISangokuSelectionObject)>();

        for (int characterIndex = 0; characterIndex < _characterSelectionObjectList.Count; characterIndex++) {
            float positionInterpolation = GetCharacterPositionInterpolation(characterIndex, _currentcharacterIndex);
            float diffInterpolation = MathF.Min(positionInterpolation - 0, 1 - positionInterpolation);

            if (diffInterpolation <= 0.25f) {
                _characterSelectionObjectList[characterIndex].Rect.SetParent(_rectSelectionCharacterFrontRoot);
                dataList.Add((diffInterpolation, _characterSelectionObjectList[characterIndex]));
            }
            else {
                _characterSelectionObjectList[characterIndex].Rect.SetParent(_rectSelectionCharacterBackRoot);
                dataListBack.Add((diffInterpolation, _characterSelectionObjectList[characterIndex]));
            }
        }

        dataList.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        dataListBack.Sort((x, y) => x.Item1.CompareTo(y.Item1));

        for (int i = 0; i < dataList.Count; i++) {
            dataList[i].Item2.Rect.SetAsFirstSibling();
        }

        for (int i = 0; i < dataListBack.Count; i++) {
            dataListBack[i].Item2.Rect.SetAsFirstSibling();
        }
    }

    private void RefreshFocusCharacterInfo() {
        CharacterSelectionData csData = _characterSelectionDataList[_currentcharacterIndex];
        _imageFocusCharacterFullBodyPortrait.sprite = csData.FullBodyPortrait;
        _textFocusCharacterName.text = csData.Name;
        _imageFocusCharacterMaxHP.fillAmount = (float) csData.HP / 500;
        _imageFocusCharacterMaxMP.fillAmount = (float) csData.MP / 500;
        _imageFocusCharacterAttack.fillAmount = (float) csData.Attack / 1000;
        _imageFocusCharacterDefense.fillAmount = (float) csData.Defense / 1000;
    }
    
    private Vector3 GetFadeInFadeOutOffset(bool isFadeIn, int posIndex, int totalPosCount, float interpolation) {
        // TODO:
        // Fix Note

        // NOTE:
        // Offset = (0, -100, 0) when interpolation = 0
        // Offset = (75, 0, 0) when interpolation = 0.25
        // Offset = (0, 50, 0) when interpolation = 0.5
        // Offset = (25, 0, 0) when interpolation = 0.75
        // Offset = (0, 0, 0) when interpolation = 1
        // A counterclockwise vortex liked performance, start from angle 270 degree

        float pInterpolation = isFadeIn ? interpolation : 1 - interpolation; // Performance interpolation

        float angle = 180 - (360 * ((float) posIndex / totalPosCount)) + 90 * pInterpolation;
        float offsetX = _fadeInHorOffest * Mathf.Cos(angle / Mathf.Rad2Deg) * (1 - pInterpolation);
        float offsetY = _fadeInVerOffest * Mathf.Sin(angle / Mathf.Rad2Deg) * (1 - pInterpolation);

        return new Vector3(offsetX, offsetY, 0);
    }

    private float GetCharacterPositionInterpolation(int charaIndex, int focusedCharaIndex) {
        // NOTE:
        // If now focus on character X, then where is the position of character Y ?
        //
        // Case 1:                           Case 2:
        //
        //         3                                 1
        //
        //     2       4                         0       2
        //
        //     1       5                         5       3
        //
        //         0                                 4
        //               
        // 0 is focused character            4 is focused character
        //
        // If want to get position of 'Character 2' in case 1, then use 'GetCharacterPositionIndex(2, 0)'
        // If want to get position of 'Character 2' in case 2, then use 'GetCharacterPositionIndex(2, 4)'

        int posIndex = charaIndex - focusedCharaIndex;
        if (posIndex < 0) {
            posIndex += _characterCount;
        }
        else if (posIndex >= _characterCount) {
            posIndex -= _characterCount;
        }

        return (float) posIndex / _characterCount;
    }

    private Vector3 GetCharacterPositionOnRoute(int posIndex, int totalPosCount) {
        float interpolation = (float) posIndex / totalPosCount;

        return GetPositionOnRoute(interpolation);
    }

    private Vector3 GetPositionOnRoute(float interpolation) {
        // NOTE:
        // Convert "interpolation" to angle
        // Clockwise from 270 when interpolation = 0

        interpolation %= 1.0f;
        if (interpolation < 0) {
            interpolation += 1;
        }

        float angle = 270 + (-360 * interpolation);
        float x = _routeOvalWidth * Mathf.Cos(angle / Mathf.Rad2Deg);
        float y = _routeOvalHeight * Mathf.Sin(angle / Mathf.Rad2Deg);

        return new Vector3(x, y, 0);
    }
    #endregion
}
