using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SceneRect2D : MonoBehaviour
{
    [SerializeField] private Vector2 size = Vector2.one;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private int sortingOrder;

    private static Sprite squareSprite;

    public Vector2 Size
    {
        get => size;
        set
        {
            size = value;
            Apply(true);
        }
    }

    public Color Color
    {
        get => color;
        set
        {
            color = value;
            Apply(true);
        }
    }

    public int SortingOrder
    {
        get => sortingOrder;
        set
        {
            sortingOrder = value;
            Apply(true);
        }
    }

    private void OnEnable()
    {
        Apply(true);
    }

    private void OnValidate()
    {
        Apply(false);
    }

    private void Apply(bool addMissingRenderer)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null && addMissingRenderer)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = SquareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
        transform.localScale = new Vector3(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y), 1f);
    }

    private static Sprite SquareSprite
    {
        get
        {
            if (squareSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };

                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                squareSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return squareSprite;
        }
    }
}
