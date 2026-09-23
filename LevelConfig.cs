using UnityEngine;

public class LevelConfig
{
    /*
     * ========================================
     * SCORE
     * ========================================
     */

    public int ScoreToPass
    {
        get;
    }


    /*
     * ========================================
     * BALL TYPES
     * ========================================
     */

    public BallType[] BallTypes
    {
        get;
    }


    /*
     * ========================================
     * COMBO DECAY
     * ========================================
     */

    /// <summary>
    /// Thời gian để Combo giảm 1 điểm.
    ///
    /// Ví dụ:
    /// 1f   = giảm 1 điểm mỗi 1 giây.
    /// 0.5f = giảm 1 điểm mỗi 0.5 giây.
    /// </summary>
    public float ComboDecayInterval
    {
        get;
    }


    /*
     * ========================================
     * LEVEL INTRO TEXT
     * ========================================
     */

    /// <summary>
    /// Text tùy chọn hiển thị ngay khi vào level.
    /// Null/rỗng = không hiển thị.
    /// </summary>
    public string IntroText
    {
        get;
    }


    /*
     * ========================================
     * CONSTRUCTOR
     * ========================================
     */

    public LevelConfig(
        int scoreToPass,
        BallType[] ballTypes,
        float comboDecayInterval = 1f,
        string introText = null)
    {
        ScoreToPass =
            Mathf.Max(
                0,
                scoreToPass
            );


        BallTypes =
            ballTypes;


        ComboDecayInterval =
            Mathf.Max(
                0.01f,
                comboDecayInterval
            );


        IntroText =
            introText;




    }


    /*
     * ========================================
     * RANDOM BALL TYPE
     * ========================================
     */

    public BallType GetRandomBallType()
    {
        if (BallTypes == null ||
            BallTypes.Length == 0)
        {
            return default;
        }


        int randomIndex =
            Random.Range(
                0,
                BallTypes.Length
            );


        return BallTypes[randomIndex];
    }
}
