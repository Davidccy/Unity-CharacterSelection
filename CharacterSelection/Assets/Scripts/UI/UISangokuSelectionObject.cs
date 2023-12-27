using UnityEngine;
using UnityEngine.UI;

public class UISangokuSelectionObject : MonoBehaviour {
    #region Serialized Fields
    [SerializeField] private RectTransform _rectRoot = null;
    [SerializeField] private Image _imagePortrait = null;
    [SerializeField] private CanvasGroup _cg = null;
    #endregion

    #region Internal Fields
    private int _index;
    #endregion

    #region APIs
    public void SetPortrait(Sprite sp) {
        _imagePortrait.sprite = sp;
    }

    public void SetColor(Color color) {
        _imagePortrait.color = color;
    }

    public void SetAlpha(float alpha) {
        _cg.alpha = alpha;
    }

    public void SetIndex(int index) {
        _index = index;
    }

    public void SetLocalPosition(Vector3 localPosition) {
        _rectRoot.localPosition = localPosition;
    }

    public void SetLocalScale(Vector3 localScale) {
        _rectRoot.localScale = localScale;
    }

    public void SetParent(RectTransform rectParent) {
        _rectRoot.SetParent(rectParent);
    }

    public void SetAsFirstSibling() {
        _rectRoot.SetAsFirstSibling();
    }
    #endregion
}
