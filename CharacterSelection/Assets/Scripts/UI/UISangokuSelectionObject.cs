using UnityEngine;
using UnityEngine.UI;

public class UISangokuSelectionObject : MonoBehaviour {
    #region Serialized Fields
    [SerializeField] private Image _imagePortrait = null;
    [SerializeField] private CanvasGroup _cg = null;

    #endregion

    #region Internal Fields
    private int _index;
    #endregion

    #region Properties
    public RectTransform Rect {
        get {
            return this.transform as RectTransform;
        }
    }
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
        Rect.localPosition = localPosition;
    }

    public void SetLocalScale(Vector3 localScale) {
        Rect.localScale = localScale;
    }
    #endregion
}
