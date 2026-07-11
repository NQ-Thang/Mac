using UnityEngine;
using System.Collections;

public class VoTri : WalkerPassive
{
    [Header("Player Detect")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectRange = 3f;

    [Header("Question Mark")]
    [SerializeField] private GameObject questionMark;
    [SerializeField] private float surpriseTime = 0.4f;
    [SerializeField] private float questionTime = 1f;

    private bool isRunningAway;
    private bool isSurprised;

    protected override void Start()
    {
        base.Start();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        if (questionMark != null)
            questionMark.SetActive(false);
    }

    protected override void FixedUpdate()
    {
        if (isSurprised)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckPlayer();

        base.FixedUpdate();
    }

    private void CheckPlayer()
    {
        if (player == null) return;

        float distance =
            Vector2.Distance(transform.position, player.position);

        if (distance <= detectRange)
        {
            if (!isRunningAway)
            {
                FaceAwayFromPlayer();
                isRunningAway = true;
            }
        }
        else
        {
            isRunningAway = false;
        }
    }

    private void FacePlayer()
    {
        bool playerRight =
            player.position.x > transform.position.x;

        if (playerRight != movingRight)
            ChangeDirection();
    }

    private void FaceAwayFromPlayer()
    {
        bool playerRight =
            player.position.x > transform.position.x;

        if (playerRight == movingRight)
            ChangeDirection();
    }

    public void OnHit()
    {
        Debug.Log("VoTri Hit!");

        StopAllCoroutines();
        StartCoroutine(SurprisedRoutine());
    }

    private IEnumerator SurprisedRoutine()
    {
        isSurprised = true;

        rb.linearVelocity = Vector2.zero;

        FacePlayer();

        yield return new WaitForSeconds(surpriseTime);

        if (questionMark != null)
            questionMark.SetActive(true);

        yield return new WaitForSeconds(questionTime);

        if (questionMark != null)
            questionMark.SetActive(false);

        FaceAwayFromPlayer();

        isRunningAway = true;
        isSurprised = false;
    }
}