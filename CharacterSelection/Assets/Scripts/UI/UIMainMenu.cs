using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class UIMainMenu : MonoBehaviour {
    public enum PageType {
        Sangoku,
        Sengoku,
    }

    #region Serialized Fields
    [SerializeField] private PlayableDirector _pdFadeIn = null;
    [SerializeField] private PlayableDirector _pdFadeOut = null;

    [SerializeField] private SOCharacterData _soCharacterData = null;

    [SerializeField] private Button _btnSangokumusou2 = null;
    [SerializeField] private Button _btnSengokumusou2 = null;

    [SerializeField] private UISangokumusou2 _uiSangokumusou2 = null;
    [SerializeField] private UISengokumusou2 _uiSengokumusou2 = null;
    #endregion

    #region Internal Fields
    private PageType _pageType;
    #endregion

    #region Mono Behaviour Hooks
    private void Awake() {
        _pdFadeIn.stopped += PlayableDirectorFadeInFinished;
        _pdFadeOut.stopped += PlayableDirectorFadeOutFinished;

        _btnSangokumusou2.onClick.AddListener(ButtonSangokumusou2OnClick);
        _btnSengokumusou2.onClick.AddListener(ButtonSengokumusou2OnClick);

        _uiSangokumusou2.gameObject.SetActive(false);
        _uiSengokumusou2.gameObject.SetActive(false);
    }

    private void OnDestroy() {
        _pdFadeIn.stopped += PlayableDirectorFadeInFinished;
        _pdFadeOut.stopped -= PlayableDirectorFadeOutFinished;

        _btnSangokumusou2.onClick.RemoveAllListeners();
        _btnSengokumusou2.onClick.RemoveAllListeners();
    }
    #endregion

    #region Playable Director Handlings
    private void PlayableDirectorFadeInFinished(PlayableDirector pd) {
    }

    private void PlayableDirectorFadeOutFinished(PlayableDirector pd) {
        ToSelection();
    }
    #endregion

    #region UI Button Handlings
    private void ButtonSangokumusou2OnClick() {
        _pageType = PageType.Sangoku;
        PlayFadeOut();
    }

    private void ButtonSengokumusou2OnClick() {
        _pageType = PageType.Sengoku;
        PlayFadeOut();
    }
    #endregion

    #region APIs
    public void PlayFadeIn() {
        _uiSangokumusou2.gameObject.SetActive(false);
        _uiSengokumusou2.gameObject.SetActive(false);

        _pdFadeIn.Play();
    }

    public void PlayFadeOut() {
        _pdFadeOut.Play();
    }
    #endregion

    #region Internal Methods
    private void ToSelection() {
        _uiSangokumusou2.gameObject.SetActive(false);
        _uiSengokumusou2.gameObject.SetActive(false);

        if (_pageType == PageType.Sangoku) {
            _uiSangokumusou2.gameObject.SetActive(true);

            int rndCount = Random.Range(2, 20);
            _uiSangokumusou2.StarSelection(_soCharacterData, rndCount);
        }
        else if (_pageType == PageType.Sengoku) {
            _uiSengokumusou2.gameObject.SetActive(true);

            int rndCount = Random.Range(2, 20);
            _uiSengokumusou2.StarSelection(_soCharacterData, rndCount);
        }
        else {
            Debug.LogErrorFormat("Unexpected page type {0}", _pageType);
        }
    }
    #endregion
}
