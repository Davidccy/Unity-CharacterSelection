using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class UISengokumusou2 : MonoBehaviour {
    #region Serialized Fields
    [Header("General")]
    [SerializeField] private UIMainMenu _uiMainMenu = null;

    [SerializeField] private Button _btnBack = null;
    [SerializeField] private Button _btnNextCharacter = null; // To left
    [SerializeField] private Button _btnPreviousCharacter = null; // To right

    [SerializeField] private RectTransform _rectTitleRoot = null;
    [SerializeField] private RectTransform _rectOperationHintRoot = null;

    [SerializeField] private GameObject _goFocusCharacterPortraitRoot = null;
    [SerializeField] private GameObject _goFocusCharacterInfoRoot = null;

    [SerializeField] private RectTransform _rectCharacterRoot = null;
    [SerializeField] private UISengokuSelectionObject _selectionObjectRes = null;

    [Header("Timelines")]
    [SerializeField] private PlayableDirector _pdFadeIn = null;
    [SerializeField] private PlayableDirector _pdFadeOut = null;
    [SerializeField] private PlayableDirector _pdFocusCharacterPortrait = null;

    [Header("Focus Character Info")]
    [SerializeField] private Image _imageFocusCharacterPortrait = null;
    [SerializeField] private TextMeshProUGUI _textFocusCharacterName = null;
    [SerializeField] private Image _imageFocusCharacterMaxHP = null;
    [SerializeField] private Image _imageFocusCharacterMaxMP = null;
    [SerializeField] private Image _imageFocusCharacterAttack = null;
    [SerializeField] private Image _imageFocusCharacterDefense = null;

    [Header("Performance Settings")]
    [SerializeField] private float _routeOvalWidth = 100;
    [SerializeField] private float _routeOvalHeight = 100;
    [SerializeField] private float _routeOvalHorizontalOffset = 10;

    [SerializeField] private float _fadeInFadeOutPeriod = 1.0f; // Seconds
    [SerializeField] private float _selectiontPeriod = 0.3f; // Seconds
    #endregion

    #region Internal Fields
    private bool _performing = false;
    private int _characterCount = 0;
    private int _currentcharacterIndex = 0;

    private List<CharacterSelectionData> _characterSelectionDataList = new List<CharacterSelectionData>();    
    private List<UISengokuSelectionObject> _characterSelectionObjectList = new List<UISengokuSelectionObject>();

    //private float _previousRouteOvalWidth = -1;
    //private float _previousRouteOvalHeight = -1;
    #endregion

    #region Mono Behaviour Hooks
    private void Awake() {
        InitUI();

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
        // TODO
        //CheckRouteSettingAndAdjust();
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
        RefreshFocusCharacterInfo();

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
        float fadeInInterpolation = 0;
        bool isFInish = false;

        // Initialization of selection objects
        for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
            _characterSelectionObjectList[i].gameObject.SetActive(true);

            float posInterpolation = GetCharacterPositionInterpolation(i, _currentcharacterIndex);
            if (i == 0) {
                _characterSelectionObjectList[i].SetLocalScale(Vector3.one);
            }
            else {
                Vector3 scale = Vector3.one * Mathf.Max(Mathf.Abs(0 - posInterpolation), Mathf.Abs(1 - posInterpolation));
                _characterSelectionObjectList[i].SetLocalScale(scale);
            }

            float alpha = i == 0 ? 1 : 0.5f;
            _characterSelectionObjectList[i].SetAlpha(alpha);

            Color color = i == 0 ? Color.white : Color.Lerp(Color.white, Color.black, (1 - Mathf.Abs(0.5f - posInterpolation)));
            _characterSelectionObjectList[i].SetColor(color);
        }

        // Perform
        while (!isFInish) {
            fadeInInterpolation = Mathf.Clamp01(passedTime / _fadeInFadeOutPeriod);

            for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
                int positionIndex = i;

                Vector3 performPos = GetFadeInFadeOutPosition(true, fadeInInterpolation, i, _characterCount);
                _characterSelectionObjectList[i].SetLocalPosition(performPos);

                if (positionIndex == 0) {
                    _characterSelectionObjectList[i].SetLocalScale(Vector3.one * (1 + 0.5f * fadeInInterpolation));
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

        ShowCharacterSelectionButtons(true);
    }

    private IEnumerator CoPlayFadeOut() {
        if (_performing) {
            yield break;
        }

        _performing = true;

        _pdFocusCharacterPortrait.Stop();
        _pdFadeOut.Play();

        ShowCharacterSelectionButtons(false);

        float passedTime = 0;
        float fadeOutInterpolation = 0;
        bool isFInish = false;

        while (!isFInish) {
            fadeOutInterpolation = Mathf.Clamp01(passedTime / _fadeInFadeOutPeriod);

            for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
                int positionIndex = i - _currentcharacterIndex;
                if (positionIndex < 0) {
                    positionIndex += _characterCount;
                }

                Vector3 performPos = GetFadeInFadeOutPosition(false, fadeOutInterpolation, positionIndex, _characterCount);
                _characterSelectionObjectList[i].SetLocalPosition(performPos);

                if (positionIndex == 0) {
                    _characterSelectionObjectList[i].SetLocalScale(Vector3.one * (1 + 0.5f * (1 - fadeOutInterpolation)));
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
        RefreshFocusCharacterInfo();

        _characterSelectionObjectList[previousIndex].SetAlpha(0.5f);
        _characterSelectionObjectList[_currentcharacterIndex].SetAlpha(1);

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
                _characterSelectionObjectList[i].SetLocalPosition(pos);

                if (i == toIndex) {
                    _characterSelectionObjectList[i].SetLocalScale(Vector3.one * (1 + 0.5f * performInterpolation));

                    Color color = Color.white;
                    _characterSelectionObjectList[i].SetColor(color);
                }
                else {
                    Vector3 scale = Vector3.one * Mathf.Max(Mathf.Abs(0 - posInterpolation), Mathf.Abs(1 - posInterpolation));
                    _characterSelectionObjectList[i].SetLocalScale(scale);

                    Color color = Color.Lerp(Color.white, Color.black, (1 - Mathf.Abs(0.5f - posInterpolation)));
                    _characterSelectionObjectList[i].SetColor(color);
                }
            }

            if (passedTime >= _selectiontPeriod) {
                isFInish = true;
            }

            yield return new WaitForEndOfFrame();
            passedTime += Time.deltaTime;
        }

        _performing = false;

        RefreshPosition();        
    }
    #endregion

    #region Internal Methods
    private void InitUI() {
        _btnBack.gameObject.SetActive(false);
        _rectTitleRoot.gameObject.SetActive(false);
        _rectOperationHintRoot.gameObject.SetActive(false);

        ShowCharacterSelectionButtons(false);
        ShowFocusCharacterPortrait(false);
        ShowFocusCharacterInfo(false);
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
            UISengokuSelectionObject newObject = Instantiate(_selectionObjectRes, _rectCharacterRoot);

            newObject.SetPortrait(_characterSelectionDataList[i].FullBodyPortrait);
            newObject.SetLocalPosition(GetPositionOnRoute(i, _characterCount));

            _characterSelectionObjectList.Add(newObject);
        }
    }

    private void ShowCharacterSelectionButtons(bool show) {
        _btnNextCharacter.gameObject.SetActive(show);
        _btnPreviousCharacter.gameObject.SetActive(show);
    }

    private void ShowFocusCharacterPortrait(bool show) {
        _goFocusCharacterPortraitRoot.SetActive(show);
    }

    private void ShowFocusCharacterInfo(bool show) {
        _goFocusCharacterInfoRoot.SetActive(show);
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

    //private void CheckRouteSettingAndAdjust() {
    //    if (_previousRouteOvalWidth == -1 || _previousRouteOvalHeight == -1) {
    //        _previousRouteOvalWidth = _routeOvalWidth;
    //        _previousRouteOvalHeight = _routeOvalHeight;
    //        return;
    //    }

    //    // Check is setting changed or not
    //    if ((_previousRouteOvalWidth != _routeOvalWidth) || (_previousRouteOvalHeight != _routeOvalHeight)) {
    //        _previousRouteOvalWidth = _routeOvalWidth;
    //        _previousRouteOvalHeight = _routeOvalHeight;

    //        RefreshPosition();
    //    }
    //}

    private void RefreshPosition() {
        for (int i = 0; i < _characterSelectionObjectList.Count; i++) {
            float posInterpolation = GetCharacterPositionInterpolation(i, _currentcharacterIndex);
            Vector3 pos = GetPositionOnRoute(posInterpolation);
            _characterSelectionObjectList[i].SetLocalPosition(pos);
        }
    }

    private void RefreshOrder() {
        List<(float, UISengokuSelectionObject)> dataList = new List<(float, UISengokuSelectionObject)>();

        for (int characterIndex = 0; characterIndex < _characterSelectionObjectList.Count; characterIndex++) {
            float positionInterpolation = GetCharacterPositionInterpolation(characterIndex, _currentcharacterIndex);
            float diffInterpolation = MathF.Min(positionInterpolation - 0, 1 - positionInterpolation);

            _characterSelectionObjectList[characterIndex].SetParent(_rectCharacterRoot);
            dataList.Add((diffInterpolation, _characterSelectionObjectList[characterIndex]));
        }

        dataList.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        for (int i = 0; i < dataList.Count; i++) {
            dataList[i].Item2.SetAsFirstSibling();
        }
    }

    private void RefreshFocusCharacterInfo() {
        CharacterSelectionData csData = _characterSelectionDataList[_currentcharacterIndex];
        _imageFocusCharacterPortrait.sprite = csData.FullBodyPortrait;
        _textFocusCharacterName.text = csData.Name;
        _imageFocusCharacterMaxHP.fillAmount = (float) csData.HP / 500;
        _imageFocusCharacterMaxMP.fillAmount = (float) csData.MP / 500;
        _imageFocusCharacterAttack.fillAmount = (float) csData.Attack / 1000;
        _imageFocusCharacterDefense.fillAmount = (float) csData.Defense / 1000;

        _pdFocusCharacterPortrait.Stop();
        _pdFocusCharacterPortrait.Play();
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

    private Vector3 GetFadeInFadeOutPosition(bool isFadeIn, float fifoInterpolation, int posIndex, int totalPosCount) {
        float fromPosInterpolation = isFadeIn ? 1 : (float) posIndex / totalPosCount;
        float toPosInterpolation = isFadeIn ? (float) posIndex / totalPosCount : 1;

        if (posIndex == 0) {
            fromPosInterpolation = 0;
            toPosInterpolation = 0;
        }

        float fadeInFadeOutInterpolation = Mathf.Lerp(fromPosInterpolation, toPosInterpolation, fifoInterpolation);

        return GetPositionOnRoute(fadeInFadeOutInterpolation);
    }

    private Vector3 GetPositionOnRoute(int charaPosIndex, int totalPosCount) {
        float posInterpolation = (float) charaPosIndex / totalPosCount;

        return GetPositionOnRoute(posInterpolation);
    }

    private Vector3 GetPositionOnRoute(float posInterpolation) {
        posInterpolation = GetSimplifiedPositionInterpolation(posInterpolation);

        float dergee = ConvertPositionInterpolationToDegree(posInterpolation);
        float horizontalOffset = (0.5f - Mathf.Abs(posInterpolation - 0.5f)) / 0.5f * _routeOvalHorizontalOffset;

        float x = _routeOvalWidth * Mathf.Cos(dergee / Mathf.Rad2Deg) + horizontalOffset;
        float y = _routeOvalHeight * Mathf.Sin(dergee / Mathf.Rad2Deg);

        return new Vector3(x, y, 0);
    }

    private float ConvertPositionInterpolationToDegree(float posInterpolation) {
        // NOTE:
        // Convert Position Interpolation to Angle expressed by degree
        // 
        // If 'posInterpolation' = 0, then degree is 270
        // Position moves as 'Clockwise' when interpolation increases

        posInterpolation = GetSimplifiedPositionInterpolation(posInterpolation);

        float dergee = 270 + (-360 * posInterpolation);

        return dergee;
    }

    private float GetSimplifiedPositionInterpolation(float posInterpolation) {
        posInterpolation %= 1.0f;
        if (posInterpolation < 0) {
            posInterpolation += 1;
        }

        return posInterpolation;
    }
    #endregion
}
