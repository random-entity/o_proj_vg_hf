using System.Collections.Generic;
using UnityEngine;

public struct FluidSpawnConfig
{
    public List<(float x, float y, float w, float h)> NormXYWHList;
    public float frameWidth, frameHeight;
    public List<Color> Swatch;
}

public class SVDisplay : MonoBehaviour // SVDisplay.SVList는 SVDisplayManager에 의해 hf.PM2SV[p][m]과 동일 reference의 cloat2 객체로 설정되게 되므로 이 레퍼런스를 건드리지 맙시다. NormalizeValues 할 때 new cloat 만들어서 대체하는 등.
{
    #region field declarations
    public List<cloat2> SVList; // (x = State, y = Value) Pair
    private int count;

    [SerializeField] private List<(float x, float y, float w, float h)> NormXYWHList; // (xy = 왼쪽아래꼭지점의 x좌표) 이건 [0, 1]^3 기준(normalized).

    public Transform BorderBottomLeft, BorderTopRight;
    private Vector3 BorderBottomLeftPosition, BorderWidthHeight;

    private List<Transform> RectList;
    [SerializeField] private Transform RectPrefab;
    [SerializeField] private Transform RectParent;
    private Transform RectForWeightedMean;

    public List<Color> Swatch;

    private FluidSystem fluidSystem;
    #endregion

    private void Awake()
    {
        fluidSystem = FluidSystem.instance;

        RectList = new List<Transform>();
        for (int i = 0; i < Const.MaxSVListCount; i++)
        {
            Transform rect_i = Instantiate(RectPrefab, RectParent);
            rect_i.gameObject.SetActive(i < count);

            Color color_i;
            if (i < Swatch.Count) color_i = Swatch[i];
            else color_i = Color.black;

            rect_i.GetComponent<SpriteRenderer>().color = color_i;
            RectList.Add(rect_i);
        }
        RectForWeightedMean = Instantiate(RectPrefab, RectParent);

        NormXYWHList = new List<(float, float, float, float)>();
    }

    private void Start()
    {
        OnUpdateSVList();
    }

    private void Update()
    {
        // OnUpdateSVList();는 performance를 위해 EventManager.OnUpdatePM2SV가 일어났을 때만 부릅시다.

        if (Input.GetKeyDown(KeyCode.W)) RectForWeightedMean.gameObject.SetActive(!RectForWeightedMean.gameObject.activeInHierarchy); // W for weighted mean
    }

    #region Event Subscription
    private void OnEnable()
    {
        EventManager.OnUpdatePM2SV += OnUpdateSVList;
    }
    private void OnDisable()
    {
        EventManager.OnUpdatePM2SV -= OnUpdateSVList;
    }
    #endregion

    #region OnUpdateSVList
    private void OnUpdateSVList()
    {
        UpdateSVListCount();
        NormalizeValues();
        UpdateXYWHList();
        UpdateRectList();
        UpdateBorder();
        UpdateWeightedMeans();
        MatchRectListTransformToXYWH();
    }

    private void UpdateSVListCount()
    {
        count = SVList.Count;

        if (count == 0)
        {
            Debug.LogWarning("SVList.Count == 0");
        }
    }

    private void NormalizeValues() // UpdateSVListCount를 먼저 하세요
    {
        float sum = 0;

        foreach (cloat2 sv in SVList)
        {
            sum += sv.y;
        }

        if (sum <= 0)
        {
            Debug.LogWarning("sum of values <= 0");
            return;
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                SVList[i].y /= sum;
            }
        }
    }

    private void UpdateXYWHList() // NormalizeValues를 먼저 하세요. 
    {
        NormXYWHList.Clear();

        float x = 0;

        for (int i = 0; i < count; i++)
        {
            NormXYWHList.Add((x, 0f, SVList[i].y, SVList[i].x));
            x += SVList[i].y;
        }
    }

    private void MatchRectListTransformToXYWH() // UpdateRectList와 UpdateBorder를 한 후에 하세요.
    {
        for (int i = 0; i < count; i++)
        {
            MatchTransformToXYWH(RectList[i], NormXYWHList[i]);
        }

        MatchTransformToXYWH(RectForWeightedMean, (0f, 0f, 1f, UpdateWeightedMeans()));
    }

    private void UpdateRectList()
    {
        for (int i = 0; i < Const.MaxSVListCount; i++)
        {
            RectList[i].gameObject.SetActive(i < count);
        }
    }

    private void UpdateBorder()
    {
        BorderBottomLeftPosition = BorderBottomLeft.position;
        BorderWidthHeight = BorderTopRight.position - BorderBottomLeft.position;
    }

    private float UpdateWeightedMeans() //NormalizeValues를 먼저 하세요.
    {
        float m = 0f;
        for (int i = 0; i < count; i++)
        {
            m += SVList[i].y * SVList[i].x;
        }
        return m;
    }
    #endregion

    #region XWH2Vector3
    private Vector3 XYWH2Position((float x, float y, float w, float h) xywh)
    {
        return BorderBottomLeftPosition + new Vector3(BorderWidthHeight.x * (xywh.x + 0.5f * xywh.w), BorderWidthHeight.y * (xywh.y + 0.5f * xywh.h), 0f);
    }

    private Vector3 XYWH2Scale((float x, float y, float w, float h) xywh)
    {
        return new Vector3(BorderWidthHeight.x * xywh.w, BorderWidthHeight.y * xywh.h, 1f); ;
    }

    private void MatchTransformToXYWH(Transform t, (float x, float y, float w, float h) xywh)
    {
        t.localScale = XYWH2Scale(xywh);
        t.position = XYWH2Position(xywh);
    }
    #endregion
}